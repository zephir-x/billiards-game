using BilliardsGame.Interfaces.Enums;
using System.Collections.Generic;
using System.Numerics;

namespace BilliardsGame.Interfaces
{
    /// <summary>
    /// Operates as a completely safe read-only payload wrapper mapping domain complexities into pure visual presentation references.
    /// Exposes state snapshots to decoupled rendering pipelines without permitting state modification.
    /// </summary>
    public interface ISceneParameters
    {
        /// <summary>
        /// Global state representing the currently active phase of the game loop mapping UI layers.
        /// </summary>
        GameState CurrentState { get; }

        /// <summary>
        /// Maps identity rendering semantics (like names or scores) of the player controlling the flow.
        /// </summary>
        IPlayer? CurrentTurnPlayer { get; }

        /// <summary>
        /// Captures terminal states showing the ultimate player reference owning the win condition.
        /// </summary>
        IPlayer? Winner { get; }

        /// <summary>
        /// Physical aggregation representing valid bodies on the play field.
        /// </summary>
        IReadOnlyCollection<IPhysicsBody>? Bodies { get; }

        /// <summary>
        /// In-memory temporal objects mapped exclusively for sinking/dying animation loops.
        /// </summary>
        IReadOnlyCollection<IPhysicsBody>? GhostBodies { get; }

        /// <summary>
        /// Represent positional constraints mapped on static cushions representing table voids.
        /// </summary>
        IReadOnlyCollection<IPocket>? Pockets { get; }

        /// <summary>
        /// Read-only cue constraints used to visualize aim trajectory vectors and power intensities.
        /// </summary>
        ICueController? CueInfo { get; }

        /// <summary>
        /// Remaining seconds allocated for the active participant's turn to be formatted onto the HUD circular gauge.
        /// </summary>
        float TurnTimer { get; }

        /// <summary>
        /// Immutable reference to the primary local player instance.
        /// </summary>
        IPlayer Player1 { get; }

        /// <summary>
        /// Immutable reference to the primary local opposing player instance.
        /// </summary>
        IPlayer Player2 { get; }

        /// <summary>
        /// Textual payload of the current active broadcast notification.
        /// </summary>
        string NotificationMessage { get; }

        /// <summary>
        /// Active duration remaining in seconds for the display banner.
        /// </summary>
        float NotificationTimer { get; }

        /// <summary>
        /// Semantic category determining coloration and animation patterns of active UI notifications.
        /// </summary>
        NotificationType NotificationType { get; }

        /// <summary>
        /// Currently designated pocket for the game-winning 8-Ball shot, if applicable.
        /// </summary>
        IPocket? TargetPocket { get; }

        /// <summary>
        /// Checks if a candidate position is free of overlap for cue ball repositioning during ball-in-hand.
        /// </summary>
        /// <param name="position">Candidate 2D world coordinates.</param>
        /// <returns>True if placement is legally valid; otherwise false.</returns>
        bool IsValidCueBallPlacement(Vector2 position);
    }
}