namespace BilliardsGame.Network.Protocol
{
    /// <summary>
    /// Kinematic snapshot representation of a discrete billiard ball on the table.
    /// Used by authoritative host streaming to enforce deterministic positioning across network boundaries.
    /// </summary>
    public class BallSnapshot
    {
        /// <summary>
        /// Unique integer identifier of the ball (0 = Cue ball, 8 = Black 8-Ball, 1-7 = Solids, 9-15 = Stripes).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Absolute horizontal 2D world position.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Absolute vertical 2D world position.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Linear horizontal velocity component in world units per second.
        /// </summary>
        public float Vx { get; set; }

        /// <summary>
        /// Linear vertical velocity component in world units per second.
        /// </summary>
        public float Vy { get; set; }

        /// <summary>
        /// Parameterless constructor required for standard serialization pipelines.
        /// </summary>
        public BallSnapshot() { }

        /// <summary>
        /// Parameterized constructor initializing complete kinematic snapshot metrics.
        /// </summary>
        /// <param name="id">Ball identity index.</param>
        /// <param name="x">Horizontal coordinate.</param>
        /// <param name="y">Vertical coordinate.</param>
        /// <param name="vx">Horizontal velocity.</param>
        /// <param name="vy">Vertical velocity.</param>
        public BallSnapshot(int id, float x, float y, float vx, float vy)
        {
            Id = id;
            X = x;
            Y = y;
            Vx = vx;
            Vy = vy;
        }
    }
}
