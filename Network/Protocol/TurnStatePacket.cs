using BilliardsGame.Interfaces.Enums;

namespace BilliardsGame.Network.Protocol
{
    /// <summary>
    /// Authoritative game state transition message broadcast by host upon turn changes, foul determinations, and match conclusions.
    /// Ensures both participants share the identical domain state machine progression, clocks, and scoring tallies.
    /// </summary>
    public class TurnStatePacket
    {
        /// <summary>
        /// Authoritative game lifecycle state machine value.
        /// </summary>
        public GameState State { get; set; }

        /// <summary>
        /// Identifier of the participant currently awarded table control (1 or 2).
        /// </summary>
        public int ActivePlayerId { get; set; }

        /// <summary>
        /// Current pocketed balls score count for Player 1.
        /// </summary>
        public int Player1Score { get; set; }

        /// <summary>
        /// Current pocketed balls score count for Player 2.
        /// </summary>
        public int Player2Score { get; set; }

        /// <summary>
        /// Ball grouping assigned to Player 1 (null, Solid, or Striped).
        /// </summary>
        public BallType? Player1AssignedType { get; set; }

        /// <summary>
        /// Ball grouping assigned to Player 2 (null, Solid, or Striped).
        /// </summary>
        public BallType? Player2AssignedType { get; set; }

        /// <summary>
        /// Remaining turn countdown seconds from host clock.
        /// </summary>
        public float RemainingTurnTime { get; set; }

        /// <summary>
        /// High-priority notification banner string (e.g. "FOUL: Time Out", "FOUL: Scratch").
        /// </summary>
        public string NotificationMessage { get; set; } = string.Empty;

        /// <summary>
        /// Semantic category determining UI coloring and acoustic cues.
        /// </summary>
        public NotificationType NotificationType { get; set; }

        /// <summary>
        /// Winning player identifier (1 or 2) if game is concluded; otherwise null.
        /// </summary>
        public int? WinnerId { get; set; }

        /// <summary>
        /// Parameterless constructor required for standard serialization pipelines.
        /// </summary>
        public TurnStatePacket() { }

        /// <summary>
        /// Parameterized constructor initializing complete turn state synchronization metrics.
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
        public TurnStatePacket(
            GameState state,
            int activePlayerId,
            int p1Score,
            int p2Score,
            BallType? p1Type,
            BallType? p2Type,
            float remainingTurnTime,
            string notification,
            NotificationType notificationType,
            int? winnerId)
        {
            State = state;
            ActivePlayerId = activePlayerId;
            Player1Score = p1Score;
            Player2Score = p2Score;
            Player1AssignedType = p1Type;
            Player2AssignedType = p2Type;
            RemainingTurnTime = remainingTurnTime;
            NotificationMessage = notification;
            NotificationType = notificationType;
            WinnerId = winnerId;
        }
    }
}
