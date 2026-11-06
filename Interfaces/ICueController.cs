using System;
using System.Numerics;

namespace BilliardsGame.Interfaces
{
    /// <summary>
    /// Handles player interactions, aim calculations and power generation for the cue stick.
    /// </summary>
    public interface ICueController
    {
        /// <summary>
        /// Current shot power charge ranging from [0.0, 1.0].
        /// </summary>
        float Power { get; }

        /// <summary>
        /// Normalized direction vector targeting from the cue ball towards the cursor.
        /// </summary>
        Vector2 CueDirection { get; }

        /// <summary>
        /// Determines if the cue has exceeded maximum charge threshold and is penalized.
        /// </summary>
        bool IsOverheated { get; }

        /// <summary>
        /// Adjusts the aim direction based on the relation between the cue ball and mouse position.
        /// </summary>
        /// <param name="cueBallPosition">Current position of the cue ball.</param>
        /// <param name="mousePosition">Current cursor coordinate in world space.</param>
        void UpdateAim(Vector2 cueBallPosition, Vector2 mousePosition);

        /// <summary>
        /// Accumulates shot power over time.
        /// </summary>
        /// <param name="deltaTime">Time passed since the last frame.</param>
        void ChargeShot(float deltaTime);

        /// <summary>
        /// Cancels the current charge, bringing power back to zero.
        /// </summary>
        void ResetCharge();

        /// <summary>
        /// Explicitly sets the remote cue charge power synchronized from the remote opponent during their turn.
        /// </summary>
        /// <param name="power">Remote power scalar clamped to [0.0, 1.0].</param>
        void SetRemotePower(float power);

        /// <summary>
        /// Converts accumulated power into a physical impulse and strikes the given cue ball.
        /// </summary>
        /// <param name="cueBall">The body representing the cue ball.</param>
        void ExecuteShot(IPhysicsBody cueBall);

        /// <summary>
        /// Dispatched synchronously when the cue strikes the ball. Parameter represents strike power.
        /// </summary>
        event Action<float>? OnCueHit;

        /// <summary>
        /// Evaluates and ticks the overheat penalty cool-down state.
        /// </summary>
        /// <param name="deltaTime">Time passed since the last frame.</param>
        void UpdateOverheat(float deltaTime);
    }
}
