using System.Collections.Generic;
using System.Numerics;
using BilliardsGame.Interfaces;
using BilliardsGame.Interfaces.Enums;

namespace BilliardsGame.App
{
    /// <summary>
    /// Implements a strictly read-only proxy facade binding dynamic domain engines logic limits to the stateless UI rendering pipeline.
    /// This pattern actively prevents graphical systems from altering physics states or evaluating domain rules.
    /// </summary>
    public class SceneParameters : ISceneParameters
    {
        private readonly IGameManager _gameManager;
        private readonly IPhysicsEngine _physicsEngine;
        private readonly ICueController _cueController;

        /// <summary>
        /// Explicit mapper generating unified snapshot interfaces for the underlying architecture.
        /// </summary>
        /// <param name="gameManager">Core state machine managing lifecycle progress.</param>
        /// <param name="physicsEngine">Deterministic spatial integration tracker.</param>
        /// <param name="cueController">Telemetry controller managing active stroke parameters.</param>
        public SceneParameters(IGameManager gameManager, IPhysicsEngine physicsEngine, ICueController cueController)
        {
            _gameManager = gameManager;
            _physicsEngine = physicsEngine;
            _cueController = cueController;
        }

        /// <inheritdoc />
        public GameState CurrentState => _gameManager.CurrentState;
        
        /// <inheritdoc />
        public IPlayer CurrentTurnPlayer => _gameManager.ActivePlayer;
        
        /// <inheritdoc />
        public IPlayer? Winner => _gameManager.Winner;
        
        /// <inheritdoc />
        public IReadOnlyCollection<IPhysicsBody> Bodies => _physicsEngine.GetBodies();
        
        /// <inheritdoc />
        public IReadOnlyCollection<IPhysicsBody> GhostBodies => _physicsEngine.GetGhostBodies();
        
        /// <inheritdoc />
        public IReadOnlyCollection<IPocket> Pockets => _physicsEngine.GetPockets();
        
        /// <inheritdoc />
        public ICueController CueInfo => _cueController;

        /// <inheritdoc />
        public float TurnTimer => _gameManager.RemainingTurnTime;
        
        /// <inheritdoc />
        public IPlayer Player1 => _gameManager.Player1;
        
        /// <inheritdoc />
        public IPlayer Player2 => _gameManager.Player2;
        
        /// <inheritdoc />
        public string NotificationMessage => _gameManager.NotificationMessage;
        
        /// <inheritdoc />
        public float NotificationTimer => _gameManager.NotificationTimer;
        
        /// <inheritdoc />
        public NotificationType NotificationType => _gameManager.NotificationType;
        
        /// <inheritdoc />
        public IPocket? TargetPocket => _gameManager.TargetPocket;
        
        /// <inheritdoc />
        public bool IsValidCueBallPlacement(Vector2 position) => _gameManager.IsValidCueBallPlacement(position);
    }
}
