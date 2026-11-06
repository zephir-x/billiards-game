using System;
using System.Numerics;
using BilliardsGame.Interfaces.Enums;

namespace BilliardsGame.Interfaces
{
    /// <summary>
    /// Acts as the core domain supervisor managing the game states, rule validators, and turn progression.
    /// Encapsulates all stateful authority governing match rules, player switching, and foul adjudication.
    /// </summary>
    public interface IGameManager
    {
        /// <summary>
        /// Global state representing the currently active phase of the game loop.
        /// </summary>
        GameState CurrentState { get; }

        /// <summary>
        /// Represents the player entity whose turn is currently active.
        /// </summary>
        IPlayer ActivePlayer { get; }

        /// <summary>
        /// Contains the winning player reference if the game has concluded, otherwise null.
        /// </summary>
        IPlayer? Winner { get; }
        
        /// <summary>
        /// Read-only access to the primary player entity.
        /// </summary>
        IPlayer Player1 { get; }

        /// <summary>
        /// Read-only access to the secondary opponent player entity.
        /// </summary>
        IPlayer Player2 { get; }
        
        /// <summary>
        /// Explicit countdown duration in seconds allocated for the active participant to execute their aiming or ball placement decision.
        /// Reaching zero triggers an automatic timeout foul and transfers table control to the opponent.
        /// </summary>
        float RemainingTurnTime { get; }

        /// <summary>
        /// High-level text payload of the active notification banner.
        /// </summary>
        string NotificationMessage { get; }

        /// <summary>
        /// Remaining active duration in seconds for the current notification banner.
        /// </summary>
        float NotificationTimer { get; }

        /// <summary>
        /// Indicates the semantic intent context of the UI notification banner.
        /// </summary>
        NotificationType NotificationType { get; }

        /// <summary>
        /// The explicitly called pocket chosen by a player during the 8-Ball ending phase.
        /// </summary>
        IPocket? TargetPocket { get; }

        /// <summary>
        /// Event invoked when a scratch foul occurs, requesting cue ball respawn via ball-in-hand placement.
        /// </summary>
        event Action? OnScratchFoul;

        /// <summary>
        /// Event invoked when the cue ball is placed in the designated position.
        /// </summary>
        event Action<Vector2>? OnPlaceCueBall;

        /// <summary>
        /// Event invoked when match-ending conditions are met and a victor is established.
        /// </summary>
        event Action? OnGameOver;

        /// <summary>
        /// Event invoked when a notification banner is broadcast, passing its semantic category to downstream listeners.
        /// </summary>
        event Action<NotificationType>? OnNotificationEvent;

        /// <summary>
        /// Event invoked upon any domain state transition, allowing authoritative host synchronization across network peers.
        /// </summary>
        event Action? OnStateTransition;

        /// <summary>
        /// Propagates frame delta updates through domain logic, ticking active banners, evaluating decision timers, and triggering timeout fouls.
        /// </summary>
        /// <param name="deltaTime">Time elapsed in seconds since the previous update frame.</param>
        /// <param name="isAuthoritative">If true (default), runs authoritative referee rules and turn timeouts. If false (e.g. client in online match), only ticks visual presentation timers.</param>
        void UpdateLogic(float deltaTime, bool isAuthoritative = true);

        /// <summary>
        /// Resets scoring, cleans up previous state structures, and initializes a fresh match instance.
        /// </summary>
        void StartGame();

        /// <summary>
        /// Concludes the current turn sequence and transfers table authority to the opposing participant.
        /// </summary>
        void EndTurn();

        /// <summary>
        /// Positions the cue ball during a ball-in-hand phase if spatial boundaries permit placement.
        /// </summary>
        /// <param name="position">Target 2D coordinates for cue ball placement.</param>
        void PlaceCueBall(Vector2 position);

        /// <summary>
        /// Verifies whether the specified coordinates represent a legal cue ball placement without colliding with existing bodies or cushions.
        /// </summary>
        /// <param name="position">Candidate 2D world coordinates to evaluate.</param>
        /// <returns>True if the position is free of geometric and physical collisions; otherwise false.</returns>
        bool IsValidCueBallPlacement(Vector2 position);

        /// <summary>
        /// Designates the target pocket for an 8-Ball game-winning shot attempt.
        /// </summary>
        /// <param name="pocket">The target pocket chosen by the active player.</param>
        void SelectTargetPocket(IPocket pocket);

        /// <summary>
        /// Resets the game manager state back into initial menu mode, halting active timers and match logic.
        /// </summary>
        void ResetToMenu();

        /// <summary>
        /// Instantly concludes the match awarding forfeit victory to the specified player (e.g. when opponent disconnects).</param>
        /// <param name="winnerPlayerId">Identifier of the victorious remaining player (1 or 2).</param>
        /// <param name="reason">Human-readable explanation of the forfeit victory.</param>
        void ConcludeMatchByForfeit(int winnerPlayerId, string reason);

        /// <summary>
        /// Synchronizes authoritative game state directly onto client instance from host network packet.
        /// </summary>
        void ExecuteRemoteShot(Vector2 impulse, float power);

        /// <summary>
        /// Synchronizes authoritative game state directly onto client instance from host network packet.
        /// </summary>
        /// <param name="state">Domain lifecycle state.</param>
        /// <param name="activePlayerId">Active player ID.</param>
        /// <param name="p1Score">Score of Player 1.</param>
        /// <param name="p2Score">Score of Player 2.</param>
        /// <param name="p1Type">Assigned ball type for Player 1.</param>
        /// <param name="p2Type">Assigned ball type for Player 2.</param>
        /// <param name="remainingTurnTime">Authoritative remaining turn time in seconds.</param>
        /// <param name="notification">Notification banner text.</param>
        /// <param name="notificationType">Notification category.</param>
        /// <param name="winnerId">Winner player ID if game ended.</param>
        /// <param name="targetPocketIndex">Targeted pocket index for 8-ball sink if nominated; otherwise null.</param>
        void ApplyAuthoritativeState(
            GameState state,
            int activePlayerId,
            int p1Score,
            int p2Score,
            BallType? p1Type,
            BallType? p2Type,
            float remainingTurnTime,
            string notification,
            NotificationType notificationType,
            int? winnerId,
            int? targetPocketIndex = null);
    }
}
