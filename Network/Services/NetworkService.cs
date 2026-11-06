using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using BilliardsGame.Network.Abstractions;
using BilliardsGame.Network.Protocol;

namespace BilliardsGame.Network.Services
{
    /// <summary>
    /// Thread-safe standard TCP sockets implementation of <see cref="INetworkService"/>.
    /// Manages non-blocking asynchronous socket streams and buffers inbound messages in a concurrent queue
    /// that is safely dispatched onto the main application thread during <see cref="Update"/> invocations.
    /// Supports direct peer-to-peer connections and managed room relays.
    /// </summary>
    public class NetworkService : INetworkService
    {
        // Core network stream and socket instances.
        private TcpListener? _listener;
        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;

        // Thread-safe dispatch queue forwarding network callbacks to the main game loop thread.
        private readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();

        // Synchronization primitive guaranteeing thread-safe sequential writes to the underlying TCP stream.
        private readonly object _sendLock = new object();

        // Reusable receive buffer and stream accumulator state.
        // Initial capacity: 64 KB; dynamically resizable up to MaxReceiveBufferSize to preserve partial TCP packets.
        private const int InitialBufferSize = 65536;
        private const int MaxReceiveBufferSize = 1024 * 1024; // 1 MB hard limit
        private byte[] _receiveBuffer = new byte[InitialBufferSize];
        private int _accumulatedBytes = 0;

        /// <inheritdoc />
        public bool IsConnected => _client != null && _client.Connected && _stream != null;

        /// <inheritdoc />
        public bool IsHost { get; private set; }

        /// <inheritdoc />
        public int LocalPlayerId { get; private set; }

        /// <inheritdoc />
        public string RemoteAddress { get; private set; } = string.Empty;

        /// <inheritdoc />
        public int Port { get; private set; } = 7777;

        /// <inheritdoc />
        public string RoomCode { get; private set; } = string.Empty;

        /// <inheritdoc />
        public event Action<int>? OnConnected;

        /// <inheritdoc />
        public event Action<string>? OnRoomCreated;

        /// <inheritdoc />
        public event Action<string>? OnDisconnected;

        /// <inheritdoc />
        public event Action<string>? OnConnectionFailed;

        /// <inheritdoc />
        public event Action<AimSyncPacket>? OnAimSyncReceived;

        /// <inheritdoc />
        public event Action<ShotExecutionPacket>? OnShotExecutionReceived;

        /// <inheritdoc />
        public event Action<PhysicsSyncPacket>? OnPhysicsSyncReceived;

        /// <inheritdoc />
        public event Action<BallInHandPacket>? OnBallInHandReceived;

        /// <inheritdoc />
        public event Action<TurnStatePacket>? OnTurnStateReceived;

        /// <inheritdoc />
        public event Action<CallPocketPacket>? OnCallPocketReceived;

        /// <inheritdoc />
        public void StartHost(int port = 7777)
        {
            // Terminate existing connections before binding new host socket.
            Disconnect("Starting new host listener");

            Port = port;
            IsHost = true;
            LocalPlayerId = 1;
            RoomCode = string.Empty;
            _cts = new CancellationTokenSource();

            try
            {
                // Bind to all available local IPv4 network interfaces.
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();

                // Spin up non-blocking client accept worker.
                _ = Task.Run(() => AcceptClientLoopAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                _mainThreadQueue.Enqueue(() => OnConnectionFailed?.Invoke($"Failed to bind listener on port {port}: {ex.Message}"));
            }
        }

        /// <inheritdoc />
        public async Task<bool> HostViaRelayAsync(string relayHost, int relayPort = 7777, int timeoutMs = 15000)
        {
            for (int attempt = 1; attempt <= 2; attempt++)
            {
                Disconnect("Connecting to relay server as Host");

                Port = relayPort;
                IsHost = true;
                LocalPlayerId = 1;
                RemoteAddress = relayHost;
                RoomCode = string.Empty;
                _cts = new CancellationTokenSource();

                try
                {
                    _client = new TcpClient
                    {
                        NoDelay = true,
                        ReceiveBufferSize = 65536,
                        SendBufferSize = 65536
                    };

                    using var timeoutCts = new CancellationTokenSource(timeoutMs);
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, timeoutCts.Token);

                    // Prioritize IPv4 resolution to prevent Windows dual-stack IPv6 DNS delays
                    var addresses = await Dns.GetHostAddressesAsync(relayHost, linkedCts.Token);
                    var targetIp = Array.Find(addresses, a => a.AddressFamily == AddressFamily.InterNetwork) ?? addresses[0];

                    await _client.ConnectAsync(targetIp, relayPort, linkedCts.Token);
                    _stream = _client.GetStream();

                    // Start background reader loop.
                    _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));

                    // Send Relay room creation request.
                    SendPacket(NetworkPacketType.RelayControl, new RelayControlPacket
                    {
                        Action = RelayAction.CreateRoom
                    });

                    return true;
                }
                catch (Exception ex)
                {
                    if (attempt == 1)
                    {
                        // Immediate transparent retry if first attempt encountered a cold socket or DNS delay
                        await Task.Delay(200);
                        continue;
                    }
                    _mainThreadQueue.Enqueue(() => OnConnectionFailed?.Invoke($"Relay connection failed: {ex.Message}"));
                    Disconnect("Relay connection attempt failed");
                    return false;
                }
            }
            return false;
        }

        /// <inheritdoc />
        public async Task<bool> JoinViaRelayAsync(string relayHost, int relayPort, string roomCode, int timeoutMs = 15000)
        {
            for (int attempt = 1; attempt <= 2; attempt++)
            {
                Disconnect("Connecting to relay server as Client");

                Port = relayPort;
                IsHost = false;
                LocalPlayerId = 2;
                RemoteAddress = relayHost;
                RoomCode = roomCode.Trim();
                _cts = new CancellationTokenSource();

                try
                {
                    _client = new TcpClient
                    {
                        NoDelay = true,
                        ReceiveBufferSize = 65536,
                        SendBufferSize = 65536
                    };

                    using var timeoutCts = new CancellationTokenSource(timeoutMs);
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, timeoutCts.Token);

                    // Prioritize IPv4 resolution to prevent Windows dual-stack IPv6 DNS delays
                    var addresses = await Dns.GetHostAddressesAsync(relayHost, linkedCts.Token);
                    var targetIp = Array.Find(addresses, a => a.AddressFamily == AddressFamily.InterNetwork) ?? addresses[0];

                    await _client.ConnectAsync(targetIp, relayPort, linkedCts.Token);
                    _stream = _client.GetStream();

                    // Start background reader loop.
                    _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));

                    // Send Relay room join request with 5-digit code.
                    SendPacket(NetworkPacketType.RelayControl, new RelayControlPacket
                    {
                        Action = RelayAction.JoinRoom,
                        RoomCode = RoomCode
                    });

                    return true;
                }
                catch (Exception ex)
                {
                    if (attempt == 1)
                    {
                        await Task.Delay(200);
                        continue;
                    }
                    _mainThreadQueue.Enqueue(() => OnConnectionFailed?.Invoke($"Relay join failed: {ex.Message}"));
                    Disconnect("Relay join attempt failed");
                    return false;
                }
            }
            return false;
        }

        /// <inheritdoc />
        public async Task<bool> ConnectAsync(string address, int port = 7777, int timeoutMs = 5000)
        {
            // Reset existing sockets before initiating connection attempt.
            Disconnect("Connecting to new host");

            Port = port;
            IsHost = false;
            LocalPlayerId = 2;
            RemoteAddress = address;
            RoomCode = string.Empty;
            _cts = new CancellationTokenSource();

            try
            {
                _client = new TcpClient
                {
                    NoDelay = true,
                    ReceiveBufferSize = 65536,
                    SendBufferSize = 65536
                };

                // Asynchronously attempt connection with configured timeout threshold.
                using var timeoutCts = new CancellationTokenSource(timeoutMs);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, timeoutCts.Token);

                await _client.ConnectAsync(address, port, linkedCts.Token);
                _stream = _client.GetStream();

                // Start inbound stream reader loop.
                _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));

                // Dispatch connected event to main thread.
                _mainThreadQueue.Enqueue(() => OnConnected?.Invoke(LocalPlayerId));

                return true;
            }
            catch (Exception ex)
            {
                _mainThreadQueue.Enqueue(() => OnConnectionFailed?.Invoke($"Connection failed: {ex.Message}"));
                Disconnect("Connection attempt failed");
                return false;
            }
        }

        /// <summary>
        /// Background worker accepting an inbound client connection on the authoritative host listener.
        /// </summary>
        /// <param name="token">Cancellation token controlling listener shutdown.</param>
        private async Task AcceptClientLoopAsync(CancellationToken token)
        {
            try
            {
                if (_listener == null) return;
                
                // Await incoming TCP client connection request.
                var client = await _listener.AcceptTcpClientAsync(token);
                client.NoDelay = true;
                client.ReceiveBufferSize = 65536;
                client.SendBufferSize = 65536;

                _client = client;
                _stream = client.GetStream();

                // Send authoritative handshake establishing roles.
                SendPacket(NetworkPacketType.Handshake, new HandshakePacket
                {
                    AssignedPlayerId = 2,
                    PlayerName = "Player 1 (Host)"
                });

                // Spin up inbound reader task.
                _ = Task.Run(() => ReceiveLoopAsync(token));

                // Enqueue connected notification for the local host thread.
                _mainThreadQueue.Enqueue(() => OnConnected?.Invoke(LocalPlayerId));
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _mainThreadQueue.Enqueue(() => OnConnectionFailed?.Invoke($"Listener error: {ex.Message}"));
            }
        }

        /// <summary>
        /// Continuous background stream reader accumulating raw byte chunks and decoding framed messages.
        /// </summary>
        /// <param name="token">Cancellation token controlling reader lifespan.</param>
        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && _stream != null && _client != null && _client.Connected)
                {
                    // Check if accumulator requires expansion to accommodate incoming frame fragments.
                    if (_accumulatedBytes >= _receiveBuffer.Length)
                    {
                        if (_receiveBuffer.Length >= MaxReceiveBufferSize)
                        {
                            throw new InvalidDataException($"Inbound network packet buffer exceeded hard limit of {MaxReceiveBufferSize} bytes.");
                        }

                        int newSize = Math.Min(_receiveBuffer.Length * 2, MaxReceiveBufferSize);
                        Array.Resize(ref _receiveBuffer, newSize);
                    }

                    int spaceRemaining = _receiveBuffer.Length - _accumulatedBytes;

                    int bytesRead = await _stream.ReadAsync(_receiveBuffer.AsMemory(_accumulatedBytes, spaceRemaining), token);
                    if (bytesRead == 0)
                    {
                        // Socket EOF encountered: remote endpoint closed connection.
                        break;
                    }

                    _accumulatedBytes += bytesRead;

                    // Parse all complete framed messages currently available in the accumulator.
                    while (PacketSerializer.TryDeserialize(
                        _receiveBuffer,
                        _accumulatedBytes,
                        out NetworkPacketType packetType,
                        out object? packet,
                        out int bytesConsumed))
                    {
                        if (packet != null)
                        {
                            DispatchPacket(packetType, packet);
                        }

                        // Shift unconsumed trailing bytes to the beginning of the buffer.
                        int remainingBytes = _accumulatedBytes - bytesConsumed;
                        if (remainingBytes > 0)
                        {
                            Buffer.BlockCopy(_receiveBuffer, bytesConsumed, _receiveBuffer, 0, remainingBytes);
                        }
                        _accumulatedBytes = remainingBytes;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _mainThreadQueue.Enqueue(() => OnDisconnected?.Invoke($"Connection lost: {ex.Message}"));
                return;
            }

            _mainThreadQueue.Enqueue(() => OnDisconnected?.Invoke("Peer closed connection."));
        }

        /// <summary>
        /// Routes a deserialized packet to its corresponding event delegate, queued for main thread invocation.
        /// </summary>
        /// <param name="type">Packet discriminator identifier.</param>
        /// <param name="packet">Parsed packet data instance.</param>
        private void DispatchPacket(NetworkPacketType type, object packet)
        {
            switch (type)
            {
                case NetworkPacketType.RelayControl:
                    if (packet is RelayControlPacket relay)
                    {
                        HandleRelayControl(relay);
                    }
                    break;

                case NetworkPacketType.Handshake:
                    if (packet is HandshakePacket handshake && !IsHost)
                    {
                        LocalPlayerId = handshake.AssignedPlayerId;
                    }
                    break;

                case NetworkPacketType.AimSync:
                    if (packet is AimSyncPacket aim)
                    {
                        _mainThreadQueue.Enqueue(() => OnAimSyncReceived?.Invoke(aim));
                    }
                    break;

                case NetworkPacketType.ShotExecution:
                    if (packet is ShotExecutionPacket shot)
                    {
                        _mainThreadQueue.Enqueue(() => OnShotExecutionReceived?.Invoke(shot));
                    }
                    break;

                case NetworkPacketType.PhysicsSync:
                    if (packet is PhysicsSyncPacket phys)
                    {
                        _mainThreadQueue.Enqueue(() => OnPhysicsSyncReceived?.Invoke(phys));
                    }
                    break;

                case NetworkPacketType.BallInHand:
                    if (packet is BallInHandPacket bih)
                    {
                        _mainThreadQueue.Enqueue(() => OnBallInHandReceived?.Invoke(bih));
                    }
                    break;

                case NetworkPacketType.TurnState:
                    if (packet is TurnStatePacket turn)
                    {
                        _mainThreadQueue.Enqueue(() => OnTurnStateReceived?.Invoke(turn));
                    }
                    break;

                case NetworkPacketType.Disconnect:
                    if (packet is DisconnectPacket disc)
                    {
                        _mainThreadQueue.Enqueue(() => OnDisconnected?.Invoke($"Opponent disconnected: {disc.Reason}"));
                    }
                    break;

                case NetworkPacketType.CallPocket:
                    if (packet is CallPocketPacket callPocket)
                    {
                        _mainThreadQueue.Enqueue(() => OnCallPocketReceived?.Invoke(callPocket));
                    }
                    break;
            }
        }

        /// <summary>
        /// Handles room coordination packets sent by the Mikrus Relay Server.
        /// </summary>
        /// <param name="relay">Relay control packet instance.</param>
        private void HandleRelayControl(RelayControlPacket relay)
        {
            switch (relay.Action)
            {
                case RelayAction.RoomCreated:
                    RoomCode = relay.RoomCode;
                    _mainThreadQueue.Enqueue(() => OnRoomCreated?.Invoke(relay.RoomCode));
                    break;

                case RelayAction.JoinedSuccess:
                    RoomCode = relay.RoomCode;
                    LocalPlayerId = 2;
                    break;

                case RelayAction.JoinError:
                    _mainThreadQueue.Enqueue(() => OnConnectionFailed?.Invoke(string.IsNullOrWhiteSpace(relay.Message) ? "Room not found or full!" : relay.Message));
                    Disconnect("Relay join error");
                    break;

                case RelayAction.MatchReady:
                    _mainThreadQueue.Enqueue(() => OnConnected?.Invoke(LocalPlayerId));
                    break;
            }
        }

        /// <inheritdoc />
        public void SendPacket<T>(NetworkPacketType type, T payload)
        {
            if (!IsConnected || _stream == null) return;

            try
            {
                byte[] framedData = PacketSerializer.Serialize(type, payload);
                lock (_sendLock)
                {
                    _stream.Write(framedData, 0, framedData.Length);
                    _stream.Flush();
                }
            }
            catch (Exception ex)
            {
                _mainThreadQueue.Enqueue(() => OnDisconnected?.Invoke($"Send error: {ex.Message}"));
            }
        }

        /// <inheritdoc />
        public void Update()
        {
            // Drain all buffered network messages onto the calling thread.
            while (_mainThreadQueue.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }

        /// <inheritdoc />
        public void Disconnect(string reason = "Session Terminated")
        {
            try
            {
                if (IsConnected && _stream != null)
                {
                    // Attempt clean notification if stream is writeable.
                    try
                    {
                        byte[] disconnectFrame = PacketSerializer.Serialize(NetworkPacketType.Disconnect, new DisconnectPacket
                        {
                            Reason = reason
                        });
                        _stream.Write(disconnectFrame, 0, disconnectFrame.Length);
                        _stream.Flush();
                    }
                    catch { }
                }

                _cts?.Cancel();
                _stream?.Close();
                _client?.Close();
                _listener?.Stop();
            }
            catch { }
            finally
            {
                _stream = null;
                _client = null;
                _listener = null;
                _cts = null;
                _accumulatedBytes = 0;
                if (_receiveBuffer.Length > InitialBufferSize)
                {
                    _receiveBuffer = new byte[InitialBufferSize];
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Disconnect("Disposed");
        }
    }
}
