using System;
using System.Numerics;
using BilliardsGame.Interfaces;
using BilliardsGame.Network.Abstractions;
using BilliardsGame.Network.Protocol;

namespace BilliardsGame.Network.Input
{
    /// <summary>
    /// Implements <see cref="IInputProvider"/> proxy integrating network multiplayer into the domain input pipeline.
    /// When the active turn belongs to the local participant, hardware peripheral telemetry is returned and broadcast to the remote peer.
    /// When the active turn belongs to the remote participant, local hardware input is suppressed and replaced by inbound network telemetry.
    /// </summary>
    public class NetworkInputProvider : IInputProvider
    {
        private readonly IInputProvider _localInputProvider;
        private readonly INetworkService _networkService;
        private readonly Func<int> _activePlayerIdProvider;
        private readonly Action _onForceResetCue;

        // Remote aim and placement coordinates received across network stream.
        private Vector2 _remoteMousePosition = Vector2.Zero;
        private bool _remotePressed = false;
        private bool _remoteDown = false;
        private bool _remoteReleased = false;

        // Rate limiter state for outgoing aim synchronization packets.
        private float _aimSyncTimer = 0f;
        private const float AimSyncInterval = 1f / 60f; // 60Hz telemetry stream

        /// <summary>
        /// Synchronized shot charge power received from remote opponent during opponent's turn.
        /// </summary>
        public float RemotePower { get; private set; }

        /// <summary>
        /// Instantiates the network input proxy with decoupled local input, network dependencies, and cue reset delegate.
        /// </summary>
        /// <param name="localInputProvider">Local hardware peripheral input provider.</param>
        /// <param name="networkService">Active peer network session supervisor.</param>
        /// <param name="activePlayerIdProvider">Delegate returning the current active player ID (1 or 2).</param>
        /// <param name="onForceResetCue">Delegate invoked to forcefully reset cue aim and charge state upon shot release.</param>
        public NetworkInputProvider(
            IInputProvider localInputProvider,
            INetworkService networkService,
            Func<int> activePlayerIdProvider,
            Action onForceResetCue)
        {
            _localInputProvider = localInputProvider ?? throw new ArgumentNullException(nameof(localInputProvider));
            _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
            _activePlayerIdProvider = activePlayerIdProvider ?? throw new ArgumentNullException(nameof(activePlayerIdProvider));
            _onForceResetCue = onForceResetCue ?? throw new ArgumentNullException(nameof(onForceResetCue));

            // Hook inbound network packet listeners.
            _networkService.OnAimSyncReceived += HandleRemoteAimSync;
            _networkService.OnBallInHandReceived += HandleRemoteBallInHand;
            _networkService.OnShotExecutionReceived += HandleRemoteShotExecution;
        }

        /// <summary>
        /// Evaluates whether the currently active turn belongs to the local machine participant.
        /// In offline mode, both participants share the machine and evaluate to true.
        /// </summary>
        public bool IsMyTurn
        {
            get
            {
                if (!_networkService.IsConnected) return true;
                return _networkService.LocalPlayerId == _activePlayerIdProvider();
            }
        }

        /// <inheritdoc />
        public void SetCoordinateMapper(Func<Vector2, Vector2> screenToWorldMapper)
        {
            _localInputProvider.SetCoordinateMapper(screenToWorldMapper);
        }

        /// <inheritdoc />
        public Vector2 MouseWorldPosition
        {
            get
            {
                // When local participant is aiming, read local cursor; otherwise read synchronized remote cursor.
                return IsMyTurn ? _localInputProvider.MouseWorldPosition : _remoteMousePosition;
            }
        }

        /// <inheritdoc />
        public bool WasLeftMousePressed
        {
            get
            {
                return IsMyTurn ? _localInputProvider.WasLeftMousePressed : _remotePressed;
            }
        }

        /// <inheritdoc />
        public bool IsLeftMouseDown
        {
            get
            {
                return IsMyTurn ? _localInputProvider.IsLeftMouseDown : _remoteDown;
            }
        }

        /// <inheritdoc />
        public bool WasLeftMouseReleased
        {
            get
            {
                return IsMyTurn ? _localInputProvider.WasLeftMouseReleased : _remoteReleased;
            }
        }

        /// <summary>
        /// Clears all residual simulated remote click states, preventing stuck mouse down or phantom releases.
        /// </summary>
        public void ClearRemoteInputStates()
        {
            RemotePower = 0f;
            _remotePressed = false;
            _remoteDown = false;
            _remoteReleased = false;
        }

        /// <summary>
        /// Processes frame updates, clearing one-shot remote trigger flags and broadcasting local telemetry when applicable.
        /// </summary>
        /// <param name="deltaTime">Elapsed frame duration in seconds.</param>
        /// <param name="cueDirection">Current cue aiming direction vector.</param>
        /// <param name="power">Current cue charging power scalar.</param>
        /// <param name="isCharging">Whether local participant is charging shot.</param>
        /// <param name="isOverheated">Whether the cue charge is currently in an overheated penalty state.</param>
        /// <param name="canShoot">Whether the game is currently in an aiming/charging state eligible to execute a shot.</param>
        public void Update(float deltaTime, Vector2 cueDirection, float power, bool isCharging, bool isOverheated = false, bool canShoot = true)
        {
            // Clear one-shot edge-triggered remote states.
            if (_remotePressed) _remotePressed = false;
            if (_remoteReleased) _remoteReleased = false;

            // In online session during local turn, stream telemetry to remote peer.
            if (_networkService.IsConnected && IsMyTurn)
            {
                // Detect local shot release trigger and broadcast shot execution packet ONLY from Client to Host.
                // On Authoritative Host, strokes are executed directly by GameManager domain logic locally;
                // Host never dispatches ShotExecution nor wipes cue charge before GameManager consumes it.
                if (!_networkService.IsHost && _localInputProvider.WasLeftMouseReleased)
                {
                    if (isOverheated)
                    {
                        // Overheat penalty: cancel strike without dispatching shot.
                        // Preserve cooling animation; cue controller will smoothly decay power back to 0.0.
                    }
                    else if (canShoot && power > 0.02f)
                    {
                        var impulse = cueDirection * (power * 500f);
                        var shotPacket = new ShotExecutionPacket(impulse.X, impulse.Y, cueDirection.X, cueDirection.Y, power);
                        _networkService.SendPacket(NetworkPacketType.ShotExecution, shotPacket);
                        // Immediately reset local cue charge and clear power after dispatching shot
                        _onForceResetCue?.Invoke();
                    }
                }

                _aimSyncTimer += deltaTime;
                if (_aimSyncTimer >= AimSyncInterval)
                {
                    _aimSyncTimer = 0f;
                    var localMouse = _localInputProvider.MouseWorldPosition;
                    var packet = new AimSyncPacket(
                        cueDirection.X,
                        cueDirection.Y,
                        localMouse.X,
                        localMouse.Y,
                        power,
                        isCharging);
                    
                    _networkService.SendPacket(NetworkPacketType.AimSync, packet);
                }
            }
        }

        /// <summary>
        /// Synchronizes ball-in-hand coordinates transmitted during white ball placement.
        /// </summary>
        /// <param name="pos">Proposed placement coordinates.</param>
        /// <param name="isPlaced">True if ball was formally placed on table.</param>
        public void BroadcastBallInHand(Vector2 pos, bool isPlaced)
        {
            if (_networkService.IsConnected && IsMyTurn)
            {
                var packet = new BallInHandPacket(pos.X, pos.Y, isPlaced);
                _networkService.SendPacket(NetworkPacketType.BallInHand, packet);
            }
        }

        /// <summary>
        /// Ingests remote aiming telemetry received across network socket.
        /// </summary>
        /// <param name="packet">Aiming telemetry data contract.</param>
        private void HandleRemoteAimSync(AimSyncPacket packet)
        {
            if (!IsMyTurn)
            {
                RemotePower = packet.Power;
                _remoteMousePosition = new Vector2(packet.MouseWorldX, packet.MouseWorldY);
                if (packet.IsCharging && !_remoteDown)
                {
                    _remotePressed = true;
                }
                _remoteDown = packet.IsCharging;
            }
        }

        /// <summary>
        /// Ingests remote ball-in-hand coordinates received across network socket.
        /// </summary>
        /// <param name="packet">Ball-in-hand placement contract.</param>
        private void HandleRemoteBallInHand(BallInHandPacket packet)
        {
            if (!IsMyTurn)
            {
                _remoteMousePosition = new Vector2(packet.X, packet.Y);
                if (packet.IsPlaced)
                {
                    _remoteReleased = true;
                }
            }
        }

        /// <summary>
        /// Ingests remote shot execution trigger applying simulated click release to launch the stroke.
        /// Immediately clears remote mouse down flags and invokes cue reset to eliminate stuck cue renderings.
        /// </summary>
        /// <param name="packet">Shot impulse execution contract.</param>
        private void HandleRemoteShotExecution(ShotExecutionPacket packet)
        {
            if (!IsMyTurn)
            {
                RemotePower = 0f;
                _remoteDown = false;
                _remoteReleased = true;
                _onForceResetCue?.Invoke();
            }
        }
    }
}
