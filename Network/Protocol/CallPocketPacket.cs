namespace BilliardsGame.Network.Protocol
{
    /// <summary>
    /// Telemetry packet transmitted when the active player calls their designated target pocket for the 8-ball.
    /// </summary>
    public class CallPocketPacket
    {
        /// <summary>
        /// Zero-based index of the designated pocket in the table's pocket collection.
        /// </summary>
        public int PocketIndex { get; set; }

        /// <summary>
        /// Default parameterless constructor required for JSON serialization.
        /// </summary>
        public CallPocketPacket()
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified pocket index.
        /// </summary>
        /// <param name="pocketIndex">Zero-based index of the selected pocket.</param>
        public CallPocketPacket(int pocketIndex)
        {
            PocketIndex = pocketIndex;
        }
    }
}
