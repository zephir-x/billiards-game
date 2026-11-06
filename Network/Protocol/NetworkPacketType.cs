namespace BilliardsGame.Network.Protocol
{
    /// <summary>
    /// Explicit enumeration identifying semantic packet payloads serialized across peer network streams.
    /// Provides unambiguous type discrimination for message framing and binary dispatchers.
    /// </summary>
    public enum NetworkPacketType : byte
    {
        /// <summary>
        /// Handshake negotiation establishing participant identity and assigning authoritative player roles (Host = 1, Client = 2).
        /// </summary>
        Handshake = 1,

        /// <summary>
        /// Real-time continuous streaming of cue aiming orientation, cursor world coordinates, and power charging state.
        /// </summary>
        AimSync = 2,

        /// <summary>
        /// Discrete impulse execution command fired upon cue release applying kinetic energy to the cue ball.
        /// </summary>
        ShotExecution = 3,

        /// <summary>
        /// Authoritative physics world snapshot emitted by the host during active ball simulations.
        /// </summary>
        PhysicsSync = 4,

        /// <summary>
        /// Spatial coordinate synchronization during scratch penalty ball-in-hand positioning.
        /// </summary>
        BallInHand = 5,

        /// <summary>
        /// Authoritative game state transition synchronization including scores, foul alerts, and victory resolution.
        /// </summary>
        TurnState = 6,

        /// <summary>
        /// Graceful termination packet emitted during voluntary disconnects or session aborts.
        /// </summary>
        Disconnect = 7,

        /// <summary>
        /// Control packet exchanged with the Relay Server for 5-digit room registration and peer pairing.
        /// </summary>
        RelayControl = 8
    }
}
