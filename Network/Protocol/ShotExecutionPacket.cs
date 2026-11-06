namespace BilliardsGame.Network.Protocol
{
    /// <summary>
    /// Discrete shot trigger payload broadcast upon cue release to apply immediate impulse physics to the cue ball.
    /// Encapsulates exact impulse vectors and charge multipliers computed at the instant of release.
    /// </summary>
    public class ShotExecutionPacket
    {
        /// <summary>
        /// X-component of the computed impulse vector applied directly to the cue ball.
        /// </summary>
        public float ImpulseX { get; set; }

        /// <summary>
        /// Y-component of the computed impulse vector applied directly to the cue ball.
        /// </summary>
        public float ImpulseY { get; set; }

        /// <summary>
        /// X-component of the normalized directional strike vector.
        /// </summary>
        public float CueDirectionX { get; set; }

        /// <summary>
        /// Y-component of the normalized directional strike vector.
        /// </summary>
        public float CueDirectionY { get; set; }

        /// <summary>
        /// Normalized power intensity at strike moment.
        /// </summary>
        public float Power { get; set; }

        /// <summary>
        /// Parameterless constructor required for standard serialization pipelines.
        /// </summary>
        public ShotExecutionPacket() { }

        /// <summary>
        /// Parameterized constructor initializing complete shot execution parameters.
        /// </summary>
        /// <param name="impulseX">Horizontal impulse force.</param>
        /// <param name="impulseY">Vertical impulse force.</param>
        /// <param name="cueDirectionX">Normalized horizontal strike vector.</param>
        /// <param name="cueDirectionY">Normalized vertical strike vector.</param>
        /// <param name="power">Power percentage utilized.</param>
        public ShotExecutionPacket(float impulseX, float impulseY, float cueDirectionX, float cueDirectionY, float power)
        {
            ImpulseX = impulseX;
            ImpulseY = impulseY;
            CueDirectionX = cueDirectionX;
            CueDirectionY = cueDirectionY;
            Power = power;
        }
    }
}
