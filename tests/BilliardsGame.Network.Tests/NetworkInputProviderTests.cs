using System.Numerics;
using BilliardsGame.Interfaces;
using BilliardsGame.Network.Abstractions;
using BilliardsGame.Network.Input;
using BilliardsGame.Network.Protocol;
using Moq;
using Xunit;

namespace BilliardsGame.Network.Tests
{
    /// <summary>
    /// Test suite verifying the domain input proxy switching between local peripheral hardware and synchronized remote telemetry.
    /// </summary>
    public class NetworkInputProviderTests
    {
        /// <summary>
        /// Validates that when disconnected in offline or training mode, the proxy transparently forwards all local hardware input.
        /// </summary>
        [Fact]
        public void OfflineMode_PassesLocalHardwareInputDirectly()
        {
            var localInputMock = new Mock<IInputProvider>();
            var networkMock = new Mock<INetworkService>();
            networkMock.SetupGet(n => n.IsConnected).Returns(false);

            localInputMock.SetupGet(i => i.MouseWorldPosition).Returns(new Vector2(123f, 456f));
            localInputMock.SetupGet(i => i.WasLeftMousePressed).Returns(true);
            localInputMock.SetupGet(i => i.IsLeftMouseDown).Returns(true);
            localInputMock.SetupGet(i => i.WasLeftMouseReleased).Returns(false);

            var provider = new NetworkInputProvider(localInputMock.Object, networkMock.Object, () => 1, () => { });

            Assert.True(provider.IsMyTurn);
            Assert.Equal(new Vector2(123f, 456f), provider.MouseWorldPosition);
            Assert.True(provider.WasLeftMousePressed);
            Assert.True(provider.IsLeftMouseDown);
            Assert.False(provider.WasLeftMouseReleased);
        }

        /// <summary>
        /// Validates that during local turn in an online session, local mouse coordinates and clicks are active.
        /// </summary>
        [Fact]
        public void OnlineMode_LocalTurn_RoutesLocalInput()
        {
            var localInputMock = new Mock<IInputProvider>();
            var networkMock = new Mock<INetworkService>();
            networkMock.SetupGet(n => n.IsConnected).Returns(true);
            networkMock.SetupGet(n => n.LocalPlayerId).Returns(1);

            localInputMock.SetupGet(i => i.MouseWorldPosition).Returns(new Vector2(200f, 300f));
            localInputMock.SetupGet(i => i.WasLeftMousePressed).Returns(true);

            // Active player is Player 1 (same as LocalPlayerId).
            var provider = new NetworkInputProvider(localInputMock.Object, networkMock.Object, () => 1, () => { });

            Assert.True(provider.IsMyTurn);
            Assert.Equal(new Vector2(200f, 300f), provider.MouseWorldPosition);
            Assert.True(provider.WasLeftMousePressed);
        }

        /// <summary>
        /// Validates that during remote opponent turn in an online session, local hardware clicks are suppressed,
        /// and cursor coordinates reflect incoming telemetry from remote player.
        /// </summary>
        [Fact]
        public void OnlineMode_RemoteTurn_SuppressesLocalClicksAndAppliesRemoteCoordinates()
        {
            var localInputMock = new Mock<IInputProvider>();
            var networkMock = new Mock<INetworkService>();
            networkMock.SetupGet(n => n.IsConnected).Returns(true);
            networkMock.SetupGet(n => n.LocalPlayerId).Returns(1);

            // Local user moves their own mouse and clicks on their local machine.
            localInputMock.SetupGet(i => i.MouseWorldPosition).Returns(new Vector2(999f, 999f));
            localInputMock.SetupGet(i => i.WasLeftMousePressed).Returns(true);
            localInputMock.SetupGet(i => i.IsLeftMouseDown).Returns(true);

            // Active player is Player 2 (Remote opponent).
            var provider = new NetworkInputProvider(localInputMock.Object, networkMock.Object, () => 2, () => { });

            Assert.False(provider.IsMyTurn);
            // Local clicks must be unconditionally suppressed!
            Assert.False(provider.WasLeftMousePressed);
            Assert.False(provider.IsLeftMouseDown);

            // Simulate incoming AimSyncPacket from remote opponent.
            var remoteAimPacket = new AimSyncPacket(1f, 0f, 400f, 500f, 0.5f, true);
            networkMock.Raise(n => n.OnAimSyncReceived += null, remoteAimPacket);

            // MouseWorldPosition and IsLeftMouseDown must now reflect remote opponent telemetry.
            Assert.Equal(new Vector2(400f, 500f), provider.MouseWorldPosition);
            Assert.True(provider.IsLeftMouseDown);
        }

        /// <summary>
        /// Validates that when a remote shot execution packet arrives, the provider simulates a release stroke trigger.
        /// </summary>
        [Fact]
        public void OnlineMode_RemoteShotExecution_TriggersSimulatedRelease()
        {
            var localInputMock = new Mock<IInputProvider>();
            var networkMock = new Mock<INetworkService>();
            networkMock.SetupGet(n => n.IsConnected).Returns(true);
            networkMock.SetupGet(n => n.LocalPlayerId).Returns(1);

            // Active player is Player 2 (Remote opponent).
            var provider = new NetworkInputProvider(localInputMock.Object, networkMock.Object, () => 2, () => { });

            bool cueResetCalled = false;
            provider = new NetworkInputProvider(localInputMock.Object, networkMock.Object, () => 2, () => { cueResetCalled = true; });

            // Remote shot packet arrives.
            var shotPacket = new ShotExecutionPacket(100f, 0f, 1f, 0f, 0.8f);
            networkMock.Raise(n => n.OnShotExecutionReceived += null, shotPacket);

            Assert.True(provider.WasLeftMouseReleased);
            Assert.True(cueResetCalled);
        }

        /// <summary>
        /// Validates that releasing the mouse button while the cue is in an overheated state does NOT transmit a shot packet
        /// and instead invokes the cue reset action to cancel the strike attempt.
        /// </summary>
        [Fact]
        public void OnlineMode_LocalTurn_WhenOverheated_CancelsShotAndPreservesCoolingAnimation()
        {
            var localInputMock = new Mock<IInputProvider>();
            var networkMock = new Mock<INetworkService>();
            networkMock.SetupGet(n => n.IsConnected).Returns(true);
            networkMock.SetupGet(n => n.IsHost).Returns(false);
            networkMock.SetupGet(n => n.LocalPlayerId).Returns(2);

            localInputMock.SetupGet(i => i.WasLeftMouseReleased).Returns(true);

            bool cueResetInvoked = false;
            var provider = new NetworkInputProvider(localInputMock.Object, networkMock.Object, () => 2, () => { cueResetInvoked = true; });

            // Call Update with power > 0 but isOverheated = true
            provider.Update(0.016f, new Vector2(1f, 0f), 0.7f, isCharging: false, isOverheated: true);

            // Assert that ShotExecution packet was NEVER sent and cue charge was NOT abruptly cleared (so cooling animation plays out)
            networkMock.Verify(n => n.SendPacket(NetworkPacketType.ShotExecution, It.IsAny<ShotExecutionPacket>()), Times.Never);
            Assert.False(cueResetInvoked);
        }

        /// <summary>
        /// Validates that releasing the mouse button while charging with valid power and no overheat transmits a ShotExecution packet.
        /// </summary>
        [Fact]
        public void OnlineMode_ClientTurn_WhenValidPower_SendsShotExecutionPacket()
        {
            var localInputMock = new Mock<IInputProvider>();
            var networkMock = new Mock<INetworkService>();
            networkMock.SetupGet(n => n.IsConnected).Returns(true);
            networkMock.SetupGet(n => n.IsHost).Returns(false);
            networkMock.SetupGet(n => n.LocalPlayerId).Returns(2);

            localInputMock.SetupGet(i => i.WasLeftMouseReleased).Returns(true);

            bool cueResetInvoked = false;
            var provider = new NetworkInputProvider(localInputMock.Object, networkMock.Object, () => 2, () => { cueResetInvoked = true; });

            // Call Update with power 0.8 and isOverheated = false
            provider.Update(0.016f, new Vector2(1f, 0f), 0.8f, isCharging: false, isOverheated: false);

            // Assert that ShotExecution packet was sent exactly once and cue charge was cleanly reset
            networkMock.Verify(n => n.SendPacket(NetworkPacketType.ShotExecution, It.IsAny<ShotExecutionPacket>()), Times.Once);
            Assert.True(cueResetInvoked);
        }

        /// <summary>
        /// Validates that when canShoot is false (such as in BallInHand or CallingPocket), mouse release does NOT transmit ShotExecution packet.
        /// </summary>
        [Fact]
        public void OnlineMode_LocalTurn_WhenCannotShoot_SuppressesShotExecutionPacket()
        {
            var localInputMock = new Mock<IInputProvider>();
            var networkMock = new Mock<INetworkService>();
            networkMock.SetupGet(n => n.IsConnected).Returns(true);
            networkMock.SetupGet(n => n.LocalPlayerId).Returns(1);

            localInputMock.SetupGet(i => i.WasLeftMouseReleased).Returns(true);

            bool cueResetInvoked = false;
            var provider = new NetworkInputProvider(localInputMock.Object, networkMock.Object, () => 1, () => { cueResetInvoked = true; });

            // Call Update with power 0.8 but canShoot = false
            provider.Update(0.016f, new Vector2(1f, 0f), 0.8f, isCharging: false, isOverheated: false, canShoot: false);

            networkMock.Verify(n => n.SendPacket(NetworkPacketType.ShotExecution, It.IsAny<ShotExecutionPacket>()), Times.Never);
            Assert.False(cueResetInvoked);
        }
        /// <summary>
        /// Validates that on Authoritative Host, releasing mouse with valid power does NOT send a ShotExecution packet
        /// and does NOT wipe cue power locally, allowing GameManager domain logic to execute the strike directly.
        /// </summary>
        [Fact]
        public void OnlineMode_HostTurn_WhenValidPower_DoesNotSendShotExecutionPacket_AndPreservesCueCharge()
        {
            var localInputMock = new Mock<IInputProvider>();
            var networkMock = new Mock<INetworkService>();
            networkMock.SetupGet(n => n.IsConnected).Returns(true);
            networkMock.SetupGet(n => n.IsHost).Returns(true);
            networkMock.SetupGet(n => n.LocalPlayerId).Returns(1);

            localInputMock.SetupGet(i => i.WasLeftMouseReleased).Returns(true);

            bool cueResetInvoked = false;
            var provider = new NetworkInputProvider(localInputMock.Object, networkMock.Object, () => 1, () => { cueResetInvoked = true; });

            // Call Update with power 0.8 on Host
            provider.Update(0.016f, new Vector2(1f, 0f), 0.8f, isCharging: false, isOverheated: false);

            // Assert that Host does NOT send shot packet and does NOT reset cue power prematurely
            networkMock.Verify(n => n.SendPacket(NetworkPacketType.ShotExecution, It.IsAny<ShotExecutionPacket>()), Times.Never);
            Assert.False(cueResetInvoked);
        }
    }
}
