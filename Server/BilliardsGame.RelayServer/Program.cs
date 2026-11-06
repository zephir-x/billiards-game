using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using BilliardsGame.Network.Protocol;

namespace BilliardsGame.RelayServer
{
    /// <summary>
    /// Represents an active 2-player relay room holding Host and Client sockets.
    /// Manages non-blocking transparent byte forwarding ("Blind Relay") and auto-expiration timers.
    /// </summary>
    public class RelayRoom
    {
        public string RoomCode { get; }
        public DateTime CreatedAtUtc { get; }
        public TcpClient HostClient { get; }
        public TcpClient? GuestClient { get; set; }
        public bool IsMatched { get; set; }
        public DateTime LastActivityUtc { get; set; }

        public RelayRoom(string roomCode, TcpClient hostClient)
        {
            RoomCode = roomCode;
            CreatedAtUtc = DateTime.UtcNow;
            LastActivityUtc = DateTime.UtcNow;
            HostClient = hostClient;
        }
    }

    /// <summary>
    /// Lightweight, zero-allocation relay proxy server daemon.
    /// Mediates peer-to-peer 1v1 sessions across NAT/Firewalls via 5-digit room codes.
    /// Operates on the "Blind Relay" principle: once paired, raw byte chunks are piped directly
    /// between sockets without deserializing or parsing game telemetry payloads.
    /// </summary>
    public class Program
    {
        private const int DefaultPort = 7777;
        private const int MaxRooms = 100;
        private static readonly TimeSpan RoomWaitTimeout = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan InactivityTimeout = TimeSpan.FromMinutes(10);

        private static readonly ConcurrentDictionary<string, RelayRoom> Rooms = new();
        private static readonly Random RandomGenerator = new();
        private static readonly object RandomLock = new();

        public static async Task Main(string[] args)
        {
            int port = DefaultPort;
            string? envPort = Environment.GetEnvironmentVariable("RELAY_PORT");
            if (!string.IsNullOrWhiteSpace(envPort) && int.TryParse(envPort, out int parsedPort))
            {
                port = parsedPort;
            }

            Console.WriteLine("==================================================");
            Console.WriteLine("  Billiards 2D - Zephir Edition: Blind Relay Server");
            Console.WriteLine($"  Port: {port} (TCP) | Max Rooms: {MaxRooms}");
            Console.WriteLine("==================================================");

            // Spin up background room cleaner task enforcing timeout protection.
            using var cts = new CancellationTokenSource();
            _ = Task.Run(() => CleanupExpiredRoomsLoopAsync(cts.Token));

            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine($"[INFO] Listening for peer connections on 0.0.0.0:{port}...");

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync(cts.Token);
                    client.NoDelay = true;
                    _ = Task.Run(() => HandleIncomingClientAsync(client, cts.Token));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Console.WriteLine($"[ERROR] Accept client error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Reads the initial control frame from a newly connected client and either creates or joins a room.
        /// </summary>
        private static async Task HandleIncomingClientAsync(TcpClient client, CancellationToken token)
        {
            NetworkStream stream = client.GetStream();
            byte[] headerBuffer = new byte[4];

            try
            {
                // Read 4-byte frame length header.
                int bytesRead = await ReadExactAsync(stream, headerBuffer, 4, token);
                if (bytesRead < 4)
                {
                    client.Close();
                    return;
                }

                int frameDataLength = BinaryPrimitives.ReadInt32BigEndian(headerBuffer);
                if (frameDataLength <= 1 || frameDataLength > 16384)
                {
                    client.Close();
                    return;
                }

                byte[] frameBuffer = new byte[frameDataLength];
                int bodyRead = await ReadExactAsync(stream, frameBuffer, frameDataLength, token);
                if (bodyRead < frameDataLength)
                {
                    client.Close();
                    return;
                }

                byte packetTypeByte = frameBuffer[0];
                if ((NetworkPacketType)packetTypeByte != NetworkPacketType.RelayControl)
                {
                    client.Close();
                    return;
                }

                byte[] fullBuffer = new byte[4 + frameDataLength];
                Buffer.BlockCopy(headerBuffer, 0, fullBuffer, 0, 4);
                Buffer.BlockCopy(frameBuffer, 0, fullBuffer, 4, frameDataLength);

                if (!PacketSerializer.TryDeserialize(fullBuffer, fullBuffer.Length, out _, out object? packet, out _) || packet is not RelayControlPacket control)
                {
                    client.Close();
                    return;
                }

                if (control.Action == RelayAction.CreateRoom)
                {
                    await ProcessCreateRoomAsync(client, stream, token);
                }
                else if (control.Action == RelayAction.JoinRoom)
                {
                    await ProcessJoinRoomAsync(client, stream, control.RoomCode, token);
                }
                else
                {
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Client handshake error: {ex.Message}");
                client.Close();
            }
        }

        /// <summary>
        /// Registers a new room with a unique 5-digit code and waits for a guest to join.
        /// </summary>
        private static async Task ProcessCreateRoomAsync(TcpClient hostClient, NetworkStream hostStream, CancellationToken token)
        {
            if (Rooms.Count >= MaxRooms)
            {
                byte[] errorFrame = PacketSerializer.Serialize(NetworkPacketType.RelayControl, new RelayControlPacket
                {
                    Action = RelayAction.JoinError,
                    Message = "Server room capacity reached. Try again later."
                });
                await hostStream.WriteAsync(errorFrame, token);
                hostClient.Close();
                return;
            }

            string code = GenerateUniqueRoomCode();
            var room = new RelayRoom(code, hostClient);
            Rooms[code] = room;

            Console.WriteLine($"[ROOM CREATED] Code: {code} | Active Rooms: {Rooms.Count}");

            // Notify host of the assigned room code.
            byte[] responseFrame = PacketSerializer.Serialize(NetworkPacketType.RelayControl, new RelayControlPacket
            {
                Action = RelayAction.RoomCreated,
                RoomCode = code,
                AssignedPlayerId = 1
            });
            await hostStream.WriteAsync(responseFrame, token);
            await hostStream.FlushAsync(token);
        }

        /// <summary>
        /// Matches a client with an existing room and triggers bidirectional blind relay piping.
        /// </summary>
        private static async Task ProcessJoinRoomAsync(TcpClient guestClient, NetworkStream guestStream, string roomCode, CancellationToken token)
        {
            string cleanCode = roomCode.Trim();
            if (!Rooms.TryGetValue(cleanCode, out var room) || room.IsMatched)
            {
                byte[] errorFrame = PacketSerializer.Serialize(NetworkPacketType.RelayControl, new RelayControlPacket
                {
                    Action = RelayAction.JoinError,
                    Message = "Invalid or expired room code."
                });
                await guestStream.WriteAsync(errorFrame, token);
                guestClient.Close();
                return;
            }

            lock (room)
            {
                if (room.IsMatched)
                {
                    guestClient.Close();
                    return;
                }
                room.GuestClient = guestClient;
                room.IsMatched = true;
            }

            Console.WriteLine($"[MATCH PAIRED] Room {cleanCode} paired! Starting blind relay.");

            NetworkStream hostStream = room.HostClient.GetStream();

            // Acknowledge join to guest.
            byte[] guestJoinFrame = PacketSerializer.Serialize(NetworkPacketType.RelayControl, new RelayControlPacket
            {
                Action = RelayAction.JoinedSuccess,
                RoomCode = cleanCode,
                AssignedPlayerId = 2
            });
            await guestStream.WriteAsync(guestJoinFrame, token);
            await guestStream.FlushAsync(token);

            // Notify both peers match is ready.
            byte[] matchReadyFrame = PacketSerializer.Serialize(NetworkPacketType.RelayControl, new RelayControlPacket
            {
                Action = RelayAction.MatchReady,
                RoomCode = cleanCode
            });
            await hostStream.WriteAsync(matchReadyFrame, token);
            await hostStream.FlushAsync(token);
            await guestStream.WriteAsync(matchReadyFrame, token);
            await guestStream.FlushAsync(token);

            // Launch concurrent bidirectional raw byte piping between Host and Guest.
            _ = Task.Run(async () =>
            {
                try
                {
                    var taskA = PipeStreamsAsync(room, hostStream, guestStream, token);
                    var taskB = PipeStreamsAsync(room, guestStream, hostStream, token);
                    await Task.WhenAny(taskA, taskB);
                }
                finally
                {
                    CloseRoom(cleanCode);
                }
            }, token);
        }

        /// <summary>
        /// Pure Blind Relay forwarding loop: streams raw bytes directly from source to destination socket.
        /// Performs zero decoding or deserialization of game payloads.
        /// </summary>
        private static async Task PipeStreamsAsync(RelayRoom room, NetworkStream source, NetworkStream destination, CancellationToken token)
        {
            byte[] buffer = new byte[8192];
            try
            {
                while (!token.IsCancellationRequested)
                {
                    int read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
                    if (read == 0) break;

                    room.LastActivityUtc = DateTime.UtcNow;
                    await destination.WriteAsync(buffer.AsMemory(0, read), token);
                    await destination.FlushAsync(token);
                }
            }
            catch { }
        }

        /// <summary>
        /// Closes and cleans up a room and its associated client sockets.
        /// </summary>
        private static void CloseRoom(string roomCode)
        {
            if (Rooms.TryRemove(roomCode, out var room))
            {
                Console.WriteLine($"[ROOM CLOSED] Code: {roomCode} released.");
                try { room.HostClient.Close(); } catch { }
                try { room.GuestClient?.Close(); } catch { }
            }
        }

        /// <summary>
        /// Background maintenance loop purging stale or abandoned rooms.
        /// </summary>
        private static async Task CleanupExpiredRoomsLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), token);
                    DateTime now = DateTime.UtcNow;

                    foreach (var kvp in Rooms)
                    {
                        var room = kvp.Value;
                        if (!room.IsMatched && (now - room.CreatedAtUtc) > RoomWaitTimeout)
                        {
                            Console.WriteLine($"[CLEANUP] Room {room.RoomCode} expired waiting for player.");
                            CloseRoom(room.RoomCode);
                        }
                        else if (room.IsMatched && (now - room.LastActivityUtc) > InactivityTimeout)
                        {
                            Console.WriteLine($"[CLEANUP] Room {room.RoomCode} expired due to inactivity.");
                            CloseRoom(room.RoomCode);
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CLEANUP ERROR] {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Generates an available, random 5-digit room code between 10000 and 99999.
        /// </summary>
        private static string GenerateUniqueRoomCode()
        {
            lock (RandomLock)
            {
                for (int attempt = 0; attempt < 1000; attempt++)
                {
                    string code = RandomGenerator.Next(10000, 100000).ToString();
                    if (!Rooms.ContainsKey(code))
                    {
                        return code;
                    }
                }
                return Guid.NewGuid().ToString().Substring(0, 5).ToUpperInvariant();
            }
        }

        /// <summary>
        /// Reads exactly count bytes from stream or until stream EOF.
        /// </summary>
        private static async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken token)
        {
            int total = 0;
            while (total < count)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(total, count - total), token);
                if (read == 0) return total;
                total += read;
            }
            return total;
        }
    }
}
