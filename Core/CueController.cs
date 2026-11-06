using System;
using System.Numerics;
using BilliardsGame.Interfaces;

namespace BilliardsGame.Core
{
    /// <summary>
    /// Interactive domain controller responsible for evaluating physical peripheral telemetry (mouse cursors), 
    /// orchestrating vector arithmetic to target valid shot trajectories, and dynamically accumulating kinetic energy mapping to physical impacts.
    /// </summary>
    public class CueController : ICueController
    {
        /// <inheritdoc />
        public float Power { get; private set; }

        /// <inheritdoc />
        public Vector2 CueDirection { get; private set; }

        /// <inheritdoc />
        public bool IsOverheated { get; private set; }

        // Defines the fixed time accumulation scalar governing how quickly the player reaches maximum striking force.
        private const float PowerChargeRate = 1.0f;

        // Represents the absolute top-end kinetic impulse multiplier translated directly into velocity bounds within the Physics Engine.
        private const float BaseForce = 500f; 

        /// <inheritdoc />
        public event Action<float>? OnCueHit;

        /// <inheritdoc />
        public void UpdateAim(Vector2 cueBallPosition, Vector2 mousePosition)
        {
            var diff = cueBallPosition - mousePosition;

            // Epsilon check limits division-by-zero artifacts eliminating jitter when intersecting direct coordinates.
            if (diff.LengthSquared() > 0.0001f)
            {
                CueDirection = Vector2.Normalize(diff);
            }
            else
            {
                CueDirection = Vector2.Zero;
            }
        }

        /// <inheritdoc />
        public void ChargeShot(float deltaTime)
        {
            // Block power modifications strictly if the cue has exceeded safe kinetic thresholds.
            if (IsOverheated) return;

            // Continually increment linear potential energy bounded towards the explicit 100% threshold limit.
            Power += deltaTime * PowerChargeRate;

            // Instantly transition local context into an exhausted penalty phase restricting strike actions.
            if (Power >= 1.0f)
            {
                Power = 1.0f;
                IsOverheated = true;
            }
        }

        /// <inheritdoc />
        public void UpdateOverheat(float deltaTime)
        {
            if (IsOverheated)
            {
                // Penalize fault consequences by actively forcing the UI decay loop to retract 35% slower than standard charging.
                Power -= deltaTime * PowerChargeRate * 0.65f; 
                
                // Allow safe recovery and release lock states completely.
                if (Power <= 0f)
                {
                    Power = 0f;
                    IsOverheated = false;
                }
            }
        }

        /// <inheritdoc />
        public void ResetCharge()
        {
            // Nullify stored kinetic potential completely, neutralizing any buffered strike impulse.
            Power = 0.0f;

            // Unconditionally revoke thermal penalty lockouts restoring immediate aiming and charge readiness.
            IsOverheated = false;
        }

        /// <inheritdoc />
        public void SetRemotePower(float power)
        {
            Power = Math.Clamp(power, 0f, 1f);
            IsOverheated = false;
        }

        /// <inheritdoc />
        public void ExecuteShot(IPhysicsBody cueBall)
        {
            // Fully block execution if penalty flags are active.
            if (IsOverheated) return;

            // Validate constraints asserting kinetic payloads resolve upon actual vectors and valid engine objects.
            if (Power > 0f && cueBall != null && CueDirection != Vector2.Zero)
            {
                // Translate accumulated contextual multipliers directly into simulated integrated force constraints.
                Vector2 impulse = CueDirection * (Power * BaseForce);
                
                // Pass metric intensity into boundary event listeners serving Audio SFX pipelines.
                OnCueHit?.Invoke(Power);
                
                // Inject mechanical stress to the target interface.
                cueBall.ApplyImpulse(impulse);
            }
            
            // Obliterate active potentials to completely normalize states following a legal stroke.
            Power = 0.0f;
        }
    }
}
