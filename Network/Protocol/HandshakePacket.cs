namespace BilliardsGame.Network.Protocol
{
    /// <summary>
    /// Payload exchanged immediately upon TCP connection establishment to assign authoritative player roles.
    /// Host acts as Player 1 while remote connecting peer is assigned Player 2.
    /// </summary>
    public class HandshakePacket
    {
        /// <summary>
        /// Explicit player slot identifier (1 for Host, 2 for Client).
        /// </summary>
        public int AssignedPlayerId { get; set; }

        /// <summary>
        /// Human-readable participant display handle.
        /// </summary>
        public string PlayerName { get; set; } = string.Empty;

        /// <summary>
        /// 5-digit room verification code binding the connection session.
        /// </summary>
        public string RoomCode { get; set; } = string.Empty;

        /// <summary>
        /// Parameterless constructor required for standard JSON serialization.
        /// </summary>
        public HandshakePacket() { }

        /// <summary>
        /// Parameterized constructor initializing complete handshake payload fields.
        /// </summary>
        /// <param name="assignedPlayerId">Authoritative player slot assigned.</param>
        /// <param name="playerName">Participant screen handle.</param>
        /// <param name="roomCode">Room verification code.</param>
        public HandshakePacket(int assignedPlayerId, string playerName, string roomCode)
        {
            AssignedPlayerId = assignedPlayerId;
            PlayerName = playerName;
            RoomCode = roomCode;
        }
    }
}
