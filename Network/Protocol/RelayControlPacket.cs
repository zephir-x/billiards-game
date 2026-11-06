namespace BilliardsGame.Network.Protocol
{
    /// <summary>
    /// Explicit enumeration identifying relay session operations and room lifecycle commands.
    /// Used by <see cref="RelayControlPacket"/> to register rooms, assign 5-digit codes, and pair peers.
    /// </summary>
    public enum RelayAction : byte
    {
        /// <summary>
        /// Request emitted by Host to create a new relay room on the server.
        /// </summary>
        CreateRoom = 1,

        /// <summary>
        /// Response emitted by Relay Server to Host containing the generated 5-digit room code.
        /// </summary>
        RoomCreated = 2,

        /// <summary>
        /// Request emitted by Client to join an existing relay room using the 5-digit room code.
        /// </summary>
        JoinRoom = 3,

        /// <summary>
        /// Confirmation emitted by Relay Server to Client upon successful room attachment.
        /// </summary>
        JoinedSuccess = 4,

        /// <summary>
        /// Rejection emitted by Relay Server if the room does not exist, is full, or expired.
        /// </summary>
        JoinError = 5,

        /// <summary>
        /// Notification dispatched to both peers when pairing is established and transparent data piping commences.
        /// </summary>
        MatchReady = 6
    }

    /// <summary>
    /// Data contract for control frames exchanged between game clients and the Relay Server during room coordination.
    /// </summary>
    public class RelayControlPacket
    {
        /// <summary>
        /// Specific control action requested or acknowledged.
        /// </summary>
        public RelayAction Action { get; set; }

        /// <summary>
        /// 5-digit unique room identification code (e.g., "48192").
        /// </summary>
        public string RoomCode { get; set; } = string.Empty;

        /// <summary>
        /// Assigned player role (1 = Host / Player 1, 2 = Client / Player 2).
        /// </summary>
        public int AssignedPlayerId { get; set; }

        /// <summary>
        /// Error message or status descriptor in case of rejection or state notification.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
