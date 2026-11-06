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
    }
}
