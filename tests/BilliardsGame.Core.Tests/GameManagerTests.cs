using System.Numerics;
﻿using System;
using System.Collections.Generic;
using Moq;
using Xunit;
using BilliardsGame.Core;
using BilliardsGame.Interfaces;
using BilliardsGame.Interfaces.Enums;
using BilliardsGame.Interfaces.Models;

namespace BilliardsGame.Core.Tests
{
    /// <summary>
    /// Testing matrix verifying the primary state manager mapping complex transitions mapping strokes across rule verifications validating state mutations exclusively.
    /// </summary>
    public class GameManagerTests
    {
        /// <summary>
        /// Validates that sinking the cue ball triggers a scratch foul event and enters BallInHand state.
        /// </summary>
        [Fact]
        public void UpdateLogic_SimulatingBalls_NoCueBall_BallInHandFoul()
        {
            // Initializes mock structures encapsulating disconnected Physics engine dependencies locking evaluations securely down testing pure domain logic only.
            var physicsMock = new Mock<IPhysicsEngine>();
            var cueMock = new Mock<ICueController>();
            var inputMock = new Mock<IInputProvider>();
            
            var blackBallMock = new Mock<ICircleBody>();
            blackBallMock.SetupGet(b => b.Id).Returns(8);
            blackBallMock.SetupGet(b => b.BallType).Returns(BallType.Black);
            
            // Spoofs the array explicitly removing the Cue Ball mocking a scratch mapping perfectly onto engine conditions simulating rules properly.
            physicsMock.Setup(p => p.GetBodies()).Returns(new List<IPhysicsBody> { blackBallMock.Object });
            physicsMock.Setup(p => p.AreAllBodiesAtRest(It.IsAny<float>())).Returns(true);
            
            var strokeData = new StrokeData { FirstBallHitId = 8, RailsHitAfterContact = 1, SunkBallsIds = new List<int> { 0 } };
            physicsMock.Setup(p => p.CurrentStrokeData).Returns(strokeData);

            var gameManager = new GameManager(physicsMock.Object, cueMock.Object, inputMock.Object);
            
            bool eventFired = false;
            gameManager.OnScratchFoul += delegate { eventFired = true; };

            // Triggers state transitions stepping forward from initialization into targeted resolving states ensuring rule paths execute cleanly.
            gameManager.StartGame();
            inputMock.SetupGet(i => i.WasLeftMouseReleased).Returns(true);
            
            var cueBallMock = new Mock<ICircleBody>();
            cueBallMock.SetupGet(b => b.Id).Returns(0);
            cueBallMock.SetupGet(b => b.BallType).Returns(BallType.Cue);
            physicsMock.SetupSequence(p => p.GetBodies())
                .Returns(new List<IPhysicsBody> { cueBallMock.Object, blackBallMock.Object }) 
                .Returns(new List<IPhysicsBody> { cueBallMock.Object, blackBallMock.Object }) 
                .Returns(new List<IPhysicsBody> { cueBallMock.Object, blackBallMock.Object })
                .Returns(new List<IPhysicsBody> { blackBallMock.Object })
                .Returns(new List<IPhysicsBody> { blackBallMock.Object })
                .Returns(new List<IPhysicsBody> { blackBallMock.Object })
                .Returns(new List<IPhysicsBody> { blackBallMock.Object });

            inputMock.SetupGet(i => i.WasLeftMousePressed).Returns(true);
            gameManager.UpdateLogic(0.016f); // To ChargingShot
            inputMock.SetupGet(i => i.IsLeftMouseDown).Returns(false);
            gameManager.UpdateLogic(0.016f); // To SimulatingBalls

            // Actuate the target validation phase.
            gameManager.UpdateLogic(0.016f); 

            // Assert exact state change mappings validating transition handlers reacting successfully against logic anomalies explicitly missing cue subsets.
            Assert.True(eventFired);
            Assert.Equal(GameState.BallInHand, gameManager.CurrentState);
        }

        /// <summary>
        /// Validates that legally or illegally sinking the black ball concludes the match with Game Over state.
        /// </summary>
        [Fact]
        public void UpdateLogic_SimulatingBalls_NoBlackBall_GameOver()
        {
            // Arranges identical proxy stubs mocking engine outputs matching standard simulation closures checking Game Over conditionals accurately matching active ball bounds.
            var physicsMock = new Mock<IPhysicsEngine>();
            var cueMock = new Mock<ICueController>();
            var inputMock = new Mock<IInputProvider>();
            
            var cueBallMock = new Mock<ICircleBody>();
            cueBallMock.SetupGet(b => b.Id).Returns(0);
            cueBallMock.SetupGet(b => b.BallType).Returns(BallType.Cue);
            
            var blackBallMock = new Mock<ICircleBody>();
            blackBallMock.SetupGet(b => b.Id).Returns(8);
            blackBallMock.SetupGet(b => b.BallType).Returns(BallType.Black);

            var strokeData = new StrokeData { FirstBallHitId = 8, RailsHitAfterContact = 1, SunkBallsIds = new List<int> { 8 } };
            physicsMock.Setup(p => p.CurrentStrokeData).Returns(strokeData);

            // Sequences exact logic pipelines projecting expected output limits cleanly mapping into ending frames validating array deletions structurally triggering end menus.
            physicsMock.SetupSequence(p => p.GetBodies())
                .Returns(new List<IPhysicsBody> { cueBallMock.Object, blackBallMock.Object }) 
                .Returns(new List<IPhysicsBody> { cueBallMock.Object, blackBallMock.Object }) 
                .Returns(new List<IPhysicsBody> { cueBallMock.Object, blackBallMock.Object }) 
                .Returns(new List<IPhysicsBody> { cueBallMock.Object })
                .Returns(new List<IPhysicsBody> { cueBallMock.Object })
                .Returns(new List<IPhysicsBody> { cueBallMock.Object });

            physicsMock.Setup(p => p.AreAllBodiesAtRest(It.IsAny<float>())).Returns(true);

            var gameManager = new GameManager(physicsMock.Object, cueMock.Object, inputMock.Object);
            gameManager.StartGame();
            inputMock.SetupGet(i => i.WasLeftMouseReleased).Returns(true);
            inputMock.SetupGet(i => i.WasLeftMousePressed).Returns(true);
            gameManager.UpdateLogic(0.016f);
            inputMock.SetupGet(i => i.IsLeftMouseDown).Returns(false);
            gameManager.UpdateLogic(0.016f); 

            // Actuates closing logic boundaries resolving empty arrays successfully transitioning onto final overlays accurately validating missing elements cleanly avoiding crashes.
            gameManager.UpdateLogic(0.016f); 
            Assert.Equal(GameState.GameOver, gameManager.CurrentState);
        }

        /// <summary>
        /// Validates that turn timer continuously decrements in proportion to delta time while the active player is in standard aiming state.
        /// </summary>
        [Fact]
        public void UpdateLogic_DecrementsRemainingTurnTime_InPlayerTurn()
        {
            // Prepare isolated domain environment with mock peripherals.
            var physicsMock = new Mock<IPhysicsEngine>();
            var cueMock = new Mock<ICueController>();
            var inputMock = new Mock<IInputProvider>();

            var cueBallMock = new Mock<ICircleBody>();
            cueBallMock.SetupGet(b => b.Id).Returns(0);
            physicsMock.Setup(p => p.GetBodies()).Returns(new List<IPhysicsBody> { cueBallMock.Object });

            var gameManager = new GameManager(physicsMock.Object, cueMock.Object, inputMock.Object);
            gameManager.StartGame();

            // Assert baseline beginning timer state.
            Assert.Equal(GameState.PlayerTurn, gameManager.CurrentState);
            Assert.Equal(15.0f, gameManager.RemainingTurnTime);

            // Step domain frame by 5.5 seconds.
            gameManager.UpdateLogic(5.5f);

            // Assert timer reduced accurately by delta elapsed.
            Assert.Equal(9.5f, gameManager.RemainingTurnTime, 3);
        }

        /// <summary>
        /// Validates that turn timer continues decrementing while holding the shot charge, penalizing indecision.
        /// </summary>
        [Fact]
        public void UpdateLogic_DecrementsRemainingTurnTime_InChargingShot()
        {
            // Assemble mock dependencies.
            var physicsMock = new Mock<IPhysicsEngine>();
            var cueMock = new Mock<ICueController>();
            var inputMock = new Mock<IInputProvider>();

            var cueBallMock = new Mock<ICircleBody>();
            cueBallMock.SetupGet(b => b.Id).Returns(0);
            physicsMock.Setup(p => p.GetBodies()).Returns(new List<IPhysicsBody> { cueBallMock.Object });

            var gameManager = new GameManager(physicsMock.Object, cueMock.Object, inputMock.Object);
            gameManager.StartGame();

            // Transition explicitly to ChargingShot state via left mouse press.
            inputMock.SetupGet(i => i.WasLeftMousePressed).Returns(true);
            gameManager.UpdateLogic(2.0f);
            Assert.Equal(GameState.ChargingShot, gameManager.CurrentState);
            Assert.Equal(13.0f, gameManager.RemainingTurnTime, 3);

            // Maintain sustained mouse button depression simulating prolonged charge hold.
            inputMock.SetupGet(i => i.WasLeftMousePressed).Returns(false);
            inputMock.SetupGet(i => i.IsLeftMouseDown).Returns(true);
            gameManager.UpdateLogic(3.0f);

            Assert.Equal(GameState.ChargingShot, gameManager.CurrentState);
            Assert.Equal(10.0f, gameManager.RemainingTurnTime, 3);
        }

        /// <summary>
        /// Validates that inactive states such as Menu freeze the turn countdown timer completely.
        /// </summary>
        [Fact]
        public void UpdateLogic_DoesNotDecrementRemainingTurnTime_InMenu()
        {
            // Instantiate pristine manager without starting game match.
            var physicsMock = new Mock<IPhysicsEngine>();
            var cueMock = new Mock<ICueController>();
            var inputMock = new Mock<IInputProvider>();

            var gameManager = new GameManager(physicsMock.Object, cueMock.Object, inputMock.Object);
            Assert.Equal(GameState.Menu, gameManager.CurrentState);

            // Propagate time delta while in root menu.
            gameManager.UpdateLogic(5.0f);

            // Timer should remain frozen at maximum allocation.
            Assert.Equal(15.0f, gameManager.RemainingTurnTime);
        }

        /// <summary>
        /// Asserts that a legal turnover safely refreshes the countdown window back to 30 seconds for the opposing player.
        /// </summary>
        [Fact]
        public void EndTurn_ResetsRemainingTurnTime()
        {
            // Setup domain supervisor.
            var physicsMock = new Mock<IPhysicsEngine>();
            var cueMock = new Mock<ICueController>();
            var inputMock = new Mock<IInputProvider>();

            var gameManager = new GameManager(physicsMock.Object, cueMock.Object, inputMock.Object);
            gameManager.StartGame();
            gameManager.UpdateLogic(12.0f);

            // Validate that Player 1 consumed part of their allocated turn time.
            Assert.Equal(3.0f, gameManager.RemainingTurnTime, 3);
            Assert.Equal(1, gameManager.ActivePlayer.Id);

            // Conclude turn legally.
            gameManager.EndTurn();

            // Countdown must be restored to default 30s and active player toggled to Player 2.
            Assert.Equal(15.0f, gameManager.RemainingTurnTime);
            Assert.Equal(2, gameManager.ActivePlayer.Id);
        }

        /// <summary>
        /// Verifies that running out of turn time triggers an authoritative timeout foul, removes the cue ball,
        /// awards ball-in-hand to the opponent, and reinitializes the timer.
        /// </summary>
        [Fact]
        public void UpdateLogic_TurnTimerExpires_TriggersFoulAndSwitchesToOpponentBallInHand()
        {
            // Build mock simulation structure holding an active cue ball.
            var physicsMock = new Mock<IPhysicsEngine>();
            var cueMock = new Mock<ICueController>();
            var inputMock = new Mock<IInputProvider>();

            var cueBallMock = new Mock<ICircleBody>();
            cueBallMock.SetupGet(b => b.Id).Returns(0);
            physicsMock.Setup(p => p.GetBodies()).Returns(new List<IPhysicsBody> { cueBallMock.Object });

            var gameManager = new GameManager(physicsMock.Object, cueMock.Object, inputMock.Object);
            gameManager.StartGame();

            bool scratchFoulFired = false;
            NotificationType? notificationTypeFired = null;

            gameManager.OnScratchFoul += () => scratchFoulFired = true;
            gameManager.OnNotificationEvent += (type) => notificationTypeFired = type;

            // Advance logical timeline past the full 30.0-second threshold.
            gameManager.UpdateLogic(15.0f);

            // Assert foul events and semantic banner broadcasting.
            Assert.True(scratchFoulFired, "OnScratchFoul should have been invoked on timeout.");
            Assert.Equal(NotificationType.Foul, notificationTypeFired);
            Assert.Equal("FOUL: Time Out", gameManager.NotificationMessage);
            Assert.Equal(NotificationType.Foul, gameManager.NotificationType);
            Assert.Equal(GameState.BallInHand, gameManager.CurrentState);
            Assert.Equal(2, gameManager.ActivePlayer.Id);
            Assert.Equal(15.0f, gameManager.RemainingTurnTime);

            // Assert that cue ball removal was requested to enforce ball-in-hand placement.
            physicsMock.Verify(p => p.RemoveBody(cueBallMock.Object), Times.Once);
            cueMock.Verify(c => c.ResetCharge(), Times.Once);
        }

        /// <summary>
        /// Verifies that when a timeout foul strikes while the player is charging shot and overheated,
        /// the cue controller charge and overheat states are unconditionally cleaned up.
        /// </summary>
        [Fact]
        public void UpdateLogic_TurnTimerExpires_WhileChargingAndOverheated_ResetsCueCharge()
        {
            // Configure mock dependencies.
            var physicsMock = new Mock<IPhysicsEngine>();
            var cueMock = new Mock<ICueController>();
            var inputMock = new Mock<IInputProvider>();

            cueMock.SetupGet(c => c.IsOverheated).Returns(true);

            var gameManager = new GameManager(physicsMock.Object, cueMock.Object, inputMock.Object);
            gameManager.StartGame();

            // Enter charging state.
            inputMock.SetupGet(i => i.WasLeftMousePressed).Returns(true);
            gameManager.UpdateLogic(1.0f);

            // Expire remaining time while overheated.
            gameManager.UpdateLogic(14.0f);

            // Verify state transition and reset of cue charge.
            Assert.Equal(GameState.BallInHand, gameManager.CurrentState);
            Assert.Equal(2, gameManager.ActivePlayer.Id);
            cueMock.Verify(c => c.ResetCharge(), Times.Once);
        }

        /// <summary>
        /// Verifies that ResetToMenu transitions the manager back to Menu state, resets turn timers, and cleans up cue charge.
        /// </summary>
        [Fact]
        public void ResetToMenu_ResetsStateAndCue()
        {
            var physicsMock = new Mock<IPhysicsEngine>();
            var cueMock = new Mock<ICueController>();
            var inputMock = new Mock<IInputProvider>();

            var gm = new GameManager(physicsMock.Object, cueMock.Object, inputMock.Object);
            gm.StartGame();
            Assert.Equal(GameState.PlayerTurn, gm.CurrentState);

            bool transitionFired = false;
            gm.OnStateTransition += () => transitionFired = true;

            gm.ResetToMenu();

            Assert.Equal(GameState.Menu, gm.CurrentState);
            Assert.Equal(15.0f, gm.RemainingTurnTime);
            Assert.Null(gm.Winner);
            Assert.True(transitionFired);
            cueMock.Verify(c => c.ResetCharge(), Times.Once);
        }

        /// <summary>
        /// Verifies that ConcludeMatchByForfeit awards instant victory to the designated player and fires OnGameOver.
        /// </summary>
        [Fact]
        public void ConcludeMatchByForfeit_SetsWinnerAndFiresGameOver()
        {
            var physicsMock = new Mock<IPhysicsEngine>();
            var cueMock = new Mock<ICueController>();
            var inputMock = new Mock<IInputProvider>();

            var gm = new GameManager(physicsMock.Object, cueMock.Object, inputMock.Object);
            gm.StartGame();

            bool gameOverFired = false;
            bool transitionFired = false;
            gm.OnGameOver += () => gameOverFired = true;
            gm.OnStateTransition += () => transitionFired = true;

            gm.ConcludeMatchByForfeit(1, "Opponent Left - You Win by Forfeit!");

            Assert.Equal(GameState.GameOver, gm.CurrentState);
            Assert.NotNull(gm.Winner);
            Assert.Equal(1, gm.Winner.Id);
            Assert.Equal("Opponent Left - You Win by Forfeit!", gm.NotificationMessage);
            Assert.True(gameOverFired);
            Assert.True(transitionFired);
            cueMock.Verify(c => c.ResetCharge(), Times.Once);
        }

        /// <summary>
        /// Verifies that ApplyAuthoritativeState accurately maps all snapshot parameters onto the game manager.
        /// </summary>
        [Fact]
        public void ApplyAuthoritativeState_SetsAllDomainProperties()
        {
            var physicsMock = new Mock<IPhysicsEngine>();
            var cueMock = new Mock<ICueController>();
            var inputMock = new Mock<IInputProvider>();

            var gm = new GameManager(physicsMock.Object, cueMock.Object, inputMock.Object);
            gm.StartGame();

            gm.ApplyAuthoritativeState(
                GameState.SimulatingBalls,
                activePlayerId: 2,
                p1Score: 3,
                p2Score: 5,
                p1Type: BallType.Solid,
                p2Type: BallType.Striped,
                remainingTurnTime: 11.4f,
                notification: "Great Shot!",
                notificationType: NotificationType.Info,
                winnerId: null);

            Assert.Equal(GameState.SimulatingBalls, gm.CurrentState);
            Assert.Equal(2, gm.ActivePlayer.Id);
            Assert.Equal(3, gm.Player1.Score);
            Assert.Equal(5, gm.Player2.Score);
            Assert.Equal(BallType.Solid, gm.Player1.AssignedType);
            Assert.Equal(BallType.Striped, gm.Player2.AssignedType);
            Assert.Equal(11.4f, gm.RemainingTurnTime);
            Assert.Equal("Great Shot!", gm.NotificationMessage);
        }

        /// <summary>
        /// Validates that repeating the same notification in ApplyAuthoritativeState does not reset the decaying notification timer,
        /// preventing flickering and visual loops across periodic network updates.
        /// </summary>
        [Fact]
        public void ApplyAuthoritativeState_RepeatedNotification_DoesNotResetTimer()
        {
            var physicsMock = new Mock<IPhysicsEngine>();
            var cueMock = new Mock<ICueController>();
            var inputMock = new Mock<IInputProvider>();

            var gm = new GameManager(physicsMock.Object, cueMock.Object, inputMock.Object);
            gm.StartGame();

            // First packet with notification establishes 3.0s timer
            gm.ApplyAuthoritativeState(
                GameState.PlayerTurn, 1, 0, 0, null, null, 15f,
                notification: "FOUL: Scratch", notificationType: NotificationType.Foul, winnerId: null);

            Assert.Equal("FOUL: Scratch", gm.NotificationMessage);
            Assert.Equal(3.0f, gm.NotificationTimer);

            // Time progresses by 1.0s on client
            gm.UpdateLogic(1.0f, isAuthoritative: false);
            Assert.Equal(2.0f, gm.NotificationTimer);

            // Subsequent periodic packet with the SAME notification must NOT reset timer to 3.0s
            gm.ApplyAuthoritativeState(
                GameState.PlayerTurn, 1, 0, 0, null, null, 14.5f,
                notification: "FOUL: Scratch", notificationType: NotificationType.Foul, winnerId: null);

            Assert.Equal(2.0f, gm.NotificationTimer);
            Assert.Equal("FOUL: Scratch", gm.NotificationMessage);
        }

        /// <summary>
        /// Validates that an empty notification string in ApplyAuthoritativeState clears the active notification on the client.
        /// </summary>
        [Fact]
        public void ApplyAuthoritativeState_EmptyNotification_ClearsNotification()
        {
            var physicsMock = new Mock<IPhysicsEngine>();
            var cueMock = new Mock<ICueController>();
            var inputMock = new Mock<IInputProvider>();

            var gm = new GameManager(physicsMock.Object, cueMock.Object, inputMock.Object);
            gm.StartGame();

            gm.ApplyAuthoritativeState(
                GameState.PlayerTurn, 1, 0, 0, null, null, 15f,
                notification: "FOUL: Scratch", notificationType: NotificationType.Foul, winnerId: null);

            Assert.Equal("FOUL: Scratch", gm.NotificationMessage);

            // Host clears notification
            gm.ApplyAuthoritativeState(
                GameState.PlayerTurn, 1, 0, 0, null, null, 12f,
                notification: "", notificationType: NotificationType.Info, winnerId: null);

            Assert.Equal(string.Empty, gm.NotificationMessage);
            Assert.Equal(0f, gm.NotificationTimer);
        }

        /// <summary>
        /// Validates that releasing the mouse button while overheated in ChargingShot resets cue charge, returns to PlayerTurn,
        /// and does NOT execute a shot or transition to SimulatingBalls (preventing accidental fouls).
        /// </summary>
        [Fact]
        public void UpdateLogic_WhenChargingShotAndOverheated_ReleasingMouseCancelsShotAndReturnsToPlayerTurn()
        {
            var physicsMock = new Mock<IPhysicsEngine>();
            var cueMock = new Mock<ICueController>();
            var inputMock = new Mock<IInputProvider>();

            cueMock.SetupGet(c => c.IsOverheated).Returns(true);
            inputMock.SetupGet(i => i.WasLeftMouseReleased).Returns(true);

            var gm = new GameManager(physicsMock.Object, cueMock.Object, inputMock.Object);
            gm.StartGame();

            // Force state to ChargingShot
            inputMock.SetupGet(i => i.WasLeftMousePressed).Returns(true);
            gm.UpdateLogic(0.016f); // Transitions to ChargingShot
            Assert.Equal(GameState.ChargingShot, gm.CurrentState);

            inputMock.SetupGet(i => i.WasLeftMousePressed).Returns(false);

            // In ChargingShot, mouse release occurs while overheated
            gm.UpdateLogic(0.016f);

            // Assert that shot was never executed, ResetCharge was NOT called (cooling animation must play out), and state remains ChargingShot
            cueMock.Verify(c => c.ExecuteShot(It.IsAny<IPhysicsBody>()), Times.Never);
            cueMock.Verify(c => c.ResetCharge(), Times.Never);
            Assert.Equal(GameState.ChargingShot, gm.CurrentState);

            // When cooling finishes inside UpdateOverheat (IsOverheated becomes false), state transitions back to PlayerTurn
            cueMock.Setup(c => c.UpdateOverheat(It.IsAny<float>())).Callback(() =>
            {
                cueMock.SetupGet(c => c.IsOverheated).Returns(false);
            });
            gm.UpdateLogic(0.016f);
            Assert.Equal(GameState.PlayerTurn, gm.CurrentState);
        }

        /// <summary>
        /// Validates that clicking within the extended pocket click radius (e.g., 35px from a 24px pocket) successfully selects the target pocket.
        /// </summary>
        [Fact]
        public void UpdateLogic_WhenCallingPocket_ClickWithinExtendedRadius_SelectsPocket()
        {
            var physicsMock = new Mock<IPhysicsEngine>();
            var cueMock = new Mock<ICueController>();
            var inputMock = new Mock<IInputProvider>();

            var mockPocket = new Mock<IPocket>();
            mockPocket.SetupGet(p => p.Position).Returns(new Vector2(200f, 200f));
            mockPocket.SetupGet(p => p.Radius).Returns(24f);

            physicsMock.Setup(p => p.GetPockets()).Returns(new List<IPocket> { mockPocket.Object });

            // Click at (230, 200) -> distance is 30px (which is > 24px radius, but within extended click tolerance of 45px)
            inputMock.SetupGet(i => i.WasLeftMousePressed).Returns(true);
            inputMock.SetupGet(i => i.MouseWorldPosition).Returns(new Vector2(230f, 200f));

            var gm = new GameManager(physicsMock.Object, cueMock.Object, inputMock.Object);
            gm.StartGame();

            // Set state to CallingPocket
            gm.ApplyAuthoritativeState(GameState.CallingPocket, 1, 7, 0, null, null, 15f, notification: "", notificationType: NotificationType.Info, winnerId: null);
            Assert.Equal(GameState.CallingPocket, gm.CurrentState);

            gm.UpdateLogic(0.016f, isAuthoritative: true);

            // Assert pocket was selected and state transitioned to PlayerTurn
            Assert.Equal(mockPocket.Object, gm.TargetPocket);
            Assert.Equal(GameState.PlayerTurn, gm.CurrentState);
        }

        [Fact]
        public void UpdateLogic_WhenCallingPocket_TimerDecrementsAndTriggersTimeout()
        {
            var physicsMock = new Mock<IPhysicsEngine>();
            var cueMock = new Mock<ICueController>();
            var inputMock = new Mock<IInputProvider>();

            var gm = new GameManager(physicsMock.Object, cueMock.Object, inputMock.Object);
            gm.StartGame();

            gm.ApplyAuthoritativeState(GameState.CallingPocket, 1, 7, 0, null, null, 2.0f, notification: "", notificationType: NotificationType.Info, winnerId: null);
            Assert.Equal(GameState.CallingPocket, gm.CurrentState);
            Assert.Equal(2.0f, gm.RemainingTurnTime);

            // Step 1: Decrement timer
            gm.UpdateLogic(0.5f, isAuthoritative: true);
            Assert.Equal(1.5f, gm.RemainingTurnTime, 2);
            Assert.Equal(GameState.CallingPocket, gm.CurrentState);

            // Step 2: Timeout expiration triggers foul and switches turn to opponent with BallInHand
            gm.UpdateLogic(1.6f, isAuthoritative: true);
            Assert.Equal(GameState.BallInHand, gm.CurrentState);
            Assert.Equal(15.0f, gm.RemainingTurnTime);
            Assert.Equal(2, gm.ActivePlayer.Id);
        }
    }
}
