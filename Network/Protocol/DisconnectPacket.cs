namespace BilliardsGame.Network.Protocol
{
    /// <summary>
    /// Explicit termination packet broadcast when a participant voluntarily leaves a session or forfeits.
    /// Provides clear contextual error descriptions allowing polite disconnection dialogs.
    /// </summary>
    public class DisconnectPacket
    {
        /// <summary>
        /// Reason explaining why the connection session was terminated.
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Parameterless constructor required for standard serialization pipelines.
        /// </summary>
        public DisconnectPacket() { }

        /// <summary>
        /// Parameterized constructor initializing disconnect reason.
        /// </summary>
        /// <param name="reason">Contextual reason string.</param>
        public DisconnectPacket(string reason)
        {
            Reason = reason;
        }
    }
}
