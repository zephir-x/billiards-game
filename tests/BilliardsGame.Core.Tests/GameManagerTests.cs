using System;
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
            Assert.Equal(30.0f, gameManager.RemainingTurnTime);

            // Step domain frame by 5.5 seconds.
            gameManager.UpdateLogic(5.5f);

            // Assert timer reduced accurately by delta elapsed.
            Assert.Equal(24.5f, gameManager.RemainingTurnTime, 3);
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
            Assert.Equal(28.0f, gameManager.RemainingTurnTime, 3);

            // Maintain sustained mouse button depression simulating prolonged charge hold.
            inputMock.SetupGet(i => i.WasLeftMousePressed).Returns(false);
            inputMock.SetupGet(i => i.IsLeftMouseDown).Returns(true);
            gameManager.UpdateLogic(3.0f);

            Assert.Equal(GameState.ChargingShot, gameManager.CurrentState);
            Assert.Equal(25.0f, gameManager.RemainingTurnTime, 3);
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
            Assert.Equal(30.0f, gameManager.RemainingTurnTime);
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
            Assert.Equal(18.0f, gameManager.RemainingTurnTime, 3);
            Assert.Equal(1, gameManager.ActivePlayer.Id);

            // Conclude turn legally.
            gameManager.EndTurn();

            // Countdown must be restored to default 30s and active player toggled to Player 2.
            Assert.Equal(30.0f, gameManager.RemainingTurnTime);
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
            gameManager.UpdateLogic(30.0f);

            // Assert foul events and semantic banner broadcasting.
            Assert.True(scratchFoulFired, "OnScratchFoul should have been invoked on timeout.");
            Assert.Equal(NotificationType.Foul, notificationTypeFired);
            Assert.Equal("FOUL: Time Out", gameManager.NotificationMessage);
            Assert.Equal(NotificationType.Foul, gameManager.NotificationType);
            Assert.Equal(GameState.BallInHand, gameManager.CurrentState);
            Assert.Equal(2, gameManager.ActivePlayer.Id);
            Assert.Equal(30.0f, gameManager.RemainingTurnTime);

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
            gameManager.UpdateLogic(29.0f);

            // Verify state transition and reset of cue charge.
            Assert.Equal(GameState.BallInHand, gameManager.CurrentState);
            Assert.Equal(2, gameManager.ActivePlayer.Id);
            cueMock.Verify(c => c.ResetCharge(), Times.Once);
        }
    }
}
