namespace BilliardsGame.Network.Protocol
{
    /// <summary>
    /// Telemetry packet broadcast at high frequency during aiming phases to mirror the active player's cue rotation,
    /// cursor coordinate mappings, and accumulated shot charge percentage onto the remote peer's viewport.
    /// </summary>
    public class AimSyncPacket
    {
        /// <summary>
        /// X-component of the normalized directional aim vector pointing along the cue stick axis.
        /// </summary>
        public float CueDirectionX { get; set; }

        /// <summary>
        /// Y-component of the normalized directional aim vector pointing along the cue stick axis.
        /// </summary>
        public float CueDirectionY { get; set; }

        /// <summary>
        /// X-coordinate of the active participant's cursor in world physics coordinates.
        /// </summary>
        public float MouseWorldX { get; set; }

        /// <summary>
        /// Y-coordinate of the active participant's cursor in world physics coordinates.
        /// </summary>
        public float MouseWorldY { get; set; }

        /// <summary>
        /// Normalized potential charge scalar ranging from 0.0 (rest) to 1.0 (maximum force).
        /// </summary>
        public float Power { get; set; }

        /// <summary>
        /// Indicates whether the active participant is currently sustaining primary click to charge their strike.
        /// </summary>
        public bool IsCharging { get; set; }

        /// <summary>
        /// Parameterless constructor required for standard serialization pipelines.
        /// </summary>
        public AimSyncPacket() { }

        /// <summary>
        /// Parameterized constructor initializing complete aiming telemetry metrics.
        /// </summary>
        /// <param name="cueDirectionX">Normalized horizontal aim vector.</param>
        /// <param name="cueDirectionY">Normalized vertical aim vector.</param>
        /// <param name="mouseWorldX">Cursor world coordinate X.</param>
        /// <param name="mouseWorldY">Cursor world coordinate Y.</param>
        /// <param name="power">Accumulated charge intensity [0.0 - 1.0].</param>
        /// <param name="isCharging">Whether charge sequence is active.</param>
        public AimSyncPacket(float cueDirectionX, float cueDirectionY, float mouseWorldX, float mouseWorldY, float power, bool isCharging)
        {
            CueDirectionX = cueDirectionX;
            CueDirectionY = cueDirectionY;
            MouseWorldX = mouseWorldX;
            MouseWorldY = mouseWorldY;
            Power = power;
            IsCharging = isCharging;
        }
    }
}
