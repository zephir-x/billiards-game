using System.Collections.Generic;

namespace BilliardsGame.Network.Protocol
{
    /// <summary>
    /// Authoritative physics world frame broadcast continuously (30-60Hz) by the host during SimulatingBalls state.
    /// Overrides local client ball simulations with authoritative kinematic values preventing floating-point desync.
    /// </summary>
    public class PhysicsSyncPacket
    {
        /// <summary>
        /// Collection of kinematic snapshots representing all active balls remaining on the table surface.
        /// </summary>
        public List<BallSnapshot> Balls { get; set; } = new List<BallSnapshot>();

        /// <summary>
        /// Indicates whether all physical bodies have reached complete rest according to host evaluation.
        /// </summary>
        public bool AreAllAtRest { get; set; }

        /// <summary>
        /// Parameterless constructor required for standard serialization pipelines.
        /// </summary>
        public PhysicsSyncPacket() { }

        /// <summary>
        /// Parameterized constructor initializing complete snapshot frame.
        /// </summary>
        /// <param name="balls">List of ball kinematic snapshots.</param>
        /// <param name="areAllAtRest">True if simulation has settled.</param>
        public PhysicsSyncPacket(List<BallSnapshot> balls, bool areAllAtRest)
        {
            Balls = balls;
            AreAllAtRest = areAllAtRest;
        }
    }
}
