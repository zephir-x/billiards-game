using System.Numerics;
using Moq;
using Xunit;
using BilliardsGame.Core;
using BilliardsGame.Interfaces;
using BilliardsGame.Interfaces.Models;

namespace BilliardsGame.Core.Tests
{
    /// <summary>
    /// Verification suite for CueController enforcing exact vector translations mapping player inputs onto deterministic force vectors targeting active ball arrays.
    /// </summary>
    public class CueControllerTests
    {
        /// <summary>
        /// Validates that aim calculation derives a normalized directional vector pointing from mouse towards cue ball.
        /// </summary>
        [Fact]
        public void UpdateAim_CalculatesCorrectNormalizedVector()
        {
            var controller = new CueController();
            var ballPos = new Vector2(100, 100);
            var mousePos = new Vector2(50, 100);

            // Expect correct normalized vector extraction matching origin constraints matching (1, 0) logic scaling arrays.
            controller.UpdateAim(ballPos, mousePos);

            Assert.Equal(1f, controller.CueDirection.X, 4);
            Assert.Equal(0f, controller.CueDirection.Y, 4);
        }

        /// <summary>
        /// Validates that coincident coordinates for aim resolve to zero vector without numerical divergence.
        /// </summary>
        [Fact]
        public void UpdateAim_ZeroDistance_HandlesGracefully()
        {
            var controller = new CueController();
            var pos = new Vector2(100, 100);

            // Verifies edge constraints protecting rendering logic from generating NaN values breaking projection lines.
            controller.UpdateAim(pos, pos);

            Assert.Equal(Vector2.Zero, controller.CueDirection);
        }

        /// <summary>
        /// Validates linear shot charge progression and triggers thermal overheat lockout at maximum capacity.
        /// </summary>
        [Fact]
        public void ChargeShot_IncreasesPowerLinearly_AndEntersOverheatOnMax()
        {
            var controller = new CueController();
            
            // Validates continuous charging curves mimicking actual analog stick pulls translating timing onto mapped boundaries.
            controller.ChargeShot(0.5f);
            Assert.Equal(0.5f, controller.Power, 4);

            controller.ChargeShot(0.4f);
            Assert.Equal(0.9f, controller.Power, 4);

            // Validates hard-capped limits triggering domain level penalties blocking subsequent modifications implicitly.
            controller.ChargeShot(0.2f);
            Assert.Equal(1.0f, controller.Power, 4);
            Assert.True(controller.IsOverheated);
            
            // Validates exponential decays mapped onto the overheat tick limits forcing player wait interactions.
            controller.UpdateOverheat(0.1f);
            // Power should decrease matching delta thresholds precisely matching logic (new Power = 0.95 rounded -> 0.935 exactly derived locally).
            Assert.Equal(0.935f, controller.Power, 4);
            
            // Re-asserts locked controls mimicking frozen interaction layers during active timeouts.
            controller.ChargeShot(0.5f);
            Assert.Equal(0.935f, controller.Power, 4);
        }

        /// <summary>
        /// Validates that calling ResetCharge on an overheated cue immediately dissipates stored energy and revokes the overheat lockout.
        /// </summary>
        [Fact]
        public void ResetCharge_WhenOverheated_ResetsPowerAndClearsOverheat()
        {
            var controller = new CueController();
            
            // Charge cue to maximum potential to force thermal overheat condition.
            controller.ChargeShot(1.0f);
            Assert.True(controller.IsOverheated);
            Assert.Equal(1.0f, controller.Power);

            // Execute explicit reset sequence.
            controller.ResetCharge();

            // Validate that both thermal penalty and accumulated power have been completely wiped.
            Assert.False(controller.IsOverheated);
            Assert.Equal(0.0f, controller.Power);
        }

        /// <summary>
        /// Validates that executing a shot applies linear impulse to the physical ball entity and resets potential to zero.
        /// </summary>
        [Fact]
        public void ExecuteShot_AppliesCorrectImpulse_AndResetsPower()
        {
            var controller = new CueController();
            var ballMock = new Mock<IPhysicsBody>();
            controller.UpdateAim(new Vector2(10, 0), new Vector2(0, 0)); 
            
            // Executes targeted strike logic projecting exact geometric calculations natively interfacing with abstract rigid definitions mimicking cue strikes exactly.
            Vector2 expectedImpulse = new Vector2(250f, 0f);
            controller.ChargeShot(0.5f);
            
            controller.ExecuteShot(ballMock.Object);
            
            ballMock.Verify(b => b.ApplyImpulse(expectedImpulse), Times.Once);
            Assert.Equal(0.0f, controller.Power);
        }
    }
}
