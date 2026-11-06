namespace BilliardsGame.Network.Protocol
{
    /// <summary>
    /// Telemetry packet transmitted during scratch penalty ball-in-hand phases.
    /// Synchronizes candidate cursor placement coordinates or final placement commits across peers.
    /// </summary>
    public class BallInHandPacket
    {
        /// <summary>
        /// Horizontal world coordinate of the proposed or placed cue ball.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Vertical world coordinate of the proposed or placed cue ball.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// False indicates continuous real-time hovering preview; true indicates final confirmed placement drop.
        /// </summary>
        public bool IsPlaced { get; set; }

        /// <summary>
        /// Parameterless constructor required for standard serialization pipelines.
        /// </summary>
        public BallInHandPacket() { }

        /// <summary>
        /// Parameterized constructor initializing ball-in-hand placement parameters.
        /// </summary>
        /// <param name="x">Horizontal coordinate.</param>
        /// <param name="y">Vertical coordinate.</param>
        /// <param name="isPlaced">Placement confirmation flag.</param>
        public BallInHandPacket(float x, float y, bool isPlaced)
        {
            X = x;
            Y = y;
            IsPlaced = isPlaced;
        }
    }
}
