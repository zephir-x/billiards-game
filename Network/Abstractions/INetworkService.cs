using System;
using System.Threading.Tasks;
using BilliardsGame.Network.Protocol;

namespace BilliardsGame.Network.Abstractions
{
    /// <summary>
    /// Core networking abstraction isolating TCP socket lifecycles, message framing, and thread synchronization.
    /// Manages non-blocking message dispatching guaranteeing all packet callbacks execute deterministically on the main game thread.
    /// </summary>
    public interface INetworkService : IDisposable
    {
        /// <summary>
        /// Indicates whether an active peer connection is currently established.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Indicates whether this local instance acts as the authoritative host (Player 1).
        /// </summary>
        bool IsHost { get; }

        /// <summary>
        /// Local participant player index (1 for Host, 2 for Client, 0 for offline training).
        /// </summary>
        int LocalPlayerId { get; }

        /// <summary>
        /// Remote peer network endpoint or hostname.
        /// </summary>
        string RemoteAddress { get; }

        /// <summary>
        /// Listening or destination TCP port.
        /// </summary>
        int Port { get; }

        /// <summary>
        /// Active 5-digit room code if connected via relay server (empty for direct P2P).
        /// </summary>
        string RoomCode { get; }

        /// <summary>
        /// Initiates an authoritative hosting session, binding a TCP listener on the specified port.
        /// </summary>
        /// <param name="port">TCP port to bind (default 7777).</param>
        void StartHost(int port = 7777);

        /// <summary>
        /// Asynchronously connects to the Mikrus Relay Server and requests the creation of a new 5-digit room.
        /// </summary>
        /// <param name="relayHost">Relay server address.</param>
        /// <param name="relayPort">Relay server TCP port.</param>
        /// <param name="timeoutMs">Timeout in milliseconds.</param>
        Task<bool> HostViaRelayAsync(string relayHost, int relayPort = 7777, int timeoutMs = 5000);

        /// <summary>
        /// Asynchronously connects to the Mikrus Relay Server and joins an existing room using a 5-digit code.
        /// </summary>
        /// <param name="relayHost">Relay server address.</param>
        /// <param name="relayPort">Relay server TCP port.</param>
        /// <param name="roomCode">5-digit room code.</param>
        /// <param name="timeoutMs">Timeout in milliseconds.</param>
        Task<bool> JoinViaRelayAsync(string relayHost, int relayPort, string roomCode, int timeoutMs = 5000);

        /// <summary>
        /// Asynchronously attempts to establish a client TCP connection to a remote host.
        /// </summary>
        /// <param name="address">Target hostname or IPv4 address.</param>
        /// <param name="port">Target TCP port.</param>
        /// <param name="timeoutMs">Timeout duration in milliseconds before failing.</param>
        /// <returns>True if connection succeeded and handshake completed; otherwise false.</returns>
        Task<bool> ConnectAsync(string address, int port = 7777, int timeoutMs = 5000);

        /// <summary>
        /// Terminates the active session gracefully, notifying the peer and releasing socket resources.
        /// </summary>
        /// <param name="reason">Contextual reason describing why session ended.</param>
        void Disconnect(string reason = "Session Terminated");

        /// <summary>
        /// Serializes and transmits a typed message packet across the TCP network stream.
        /// </summary>
        /// <typeparam name="T">Packet contract type.</typeparam>
        /// <param name="type">Packet discriminator identifier.</param>
        /// <param name="payload">Packet data payload instance.</param>
        void SendPacket<T>(NetworkPacketType type, T payload);

        /// <summary>
        /// Flushes the thread-safe incoming packet queue on the calling thread, triggering relevant event delegates.
        /// Must be called once per main game loop frame.
        /// </summary>
        void Update();

        /// <summary>
        /// Event invoked when connection is established and player role is determined.
        /// Passes the assigned local player identifier (1 or 2).
        /// </summary>
        event Action<int>? OnConnected;

        /// <summary>
        /// Event invoked when room creation completes on the Relay server and 5-digit code is ready.
        /// </summary>
        event Action<string>? OnRoomCreated;

        /// <summary>
        /// Event invoked when connection is dropped or gracefully closed. Passes reason description.
        /// </summary>
        event Action<string>? OnDisconnected;

        /// <summary>
        /// Event invoked when connection attempt fails due to timeout or unreachable host.
        /// </summary>
        event Action<string>? OnConnectionFailed;

        /// <summary>
        /// Event invoked when aiming telemetry is received from the remote player.
        /// </summary>
        event Action<AimSyncPacket>? OnAimSyncReceived;

        /// <summary>
        /// Event invoked when shot execution impulse is received from the remote player.
        /// </summary>
        event Action<ShotExecutionPacket>? OnShotExecutionReceived;

        /// <summary>
        /// Event invoked when authoritative physics simulation snapshot is received from host.
        /// </summary>
        event Action<PhysicsSyncPacket>? OnPhysicsSyncReceived;

        /// <summary>
        /// Event invoked when ball-in-hand coordinates are received from the remote player.
        /// </summary>
        event Action<BallInHandPacket>? OnBallInHandReceived;

        /// <summary>
        /// Event invoked when authoritative game state transition is received from host.
        /// </summary>
        event Action<TurnStatePacket>? OnTurnStateReceived;
    }
}
