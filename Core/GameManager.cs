using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BilliardsGame.Interfaces;
using BilliardsGame.Interfaces.Enums;
using BilliardsGame.Interfaces.Models;

namespace BilliardsGame.Core
{
    /// <summary>
    /// Implements the central application state machine mapping the strictly enforced Clean Architecture Domain Core.
    /// Operates as the highest-level orchestrator supervising logical flow, delegating kinesthetic limits to the Physics Engine, 
    /// tracking active sequence participants (Players), and actively feeding evaluation datasets to functional Rule Validators.
    /// </summary>
    public class GameManager : IGameManager
    {
        /// <inheritdoc />
        public GameState CurrentState { get; private set; }
        
        /// <inheritdoc />
        public IPlayer ActivePlayer { get; private set; }
        
        /// <inheritdoc />
        public IPlayer? Winner { get; private set; }
        
        /// <inheritdoc />
        public IPlayer Player1 => _player1;
        
        /// <inheritdoc />
        public IPlayer Player2 => _player2;

        /// <inheritdoc />
        public float RemainingTurnTime { get; private set; } = 30.0f;
        
        /// <inheritdoc />
        public string NotificationMessage { get; private set; } = "";
        
        /// <inheritdoc />
        public float NotificationTimer { get; private set; } = 0f;
        
        /// <inheritdoc />
        public NotificationType NotificationType { get; private set; } = NotificationType.Info;

        /// <inheritdoc />
        public IPocket? TargetPocket { get; private set; }
        
        /// <inheritdoc />
        public event Action? OnScratchFoul;
        
        /// <inheritdoc />
        public event Action<Vector2>? OnPlaceCueBall;
        
        /// <inheritdoc />
        public event Action? OnGameOver;
        
        /// <inheritdoc />
        public event Action<NotificationType>? OnNotificationEvent;

        // Decoupled sub-system references injected via primary constraints.
        private readonly IPhysicsEngine _physicsEngine;
        private readonly ICueController _cueController;
        private readonly IInputProvider _inputProvider;

        // Immutable stateless functional module translating deterministic aftermaths into rule assertions.
        private readonly RuleValidator _ruleValidator;

        private readonly IPlayer _player1;
        private readonly IPlayer _player2;

        // Tracks a localized temporal snapshot of specific physics states validating explicit score changes upon stroke resolution.
        private List<ICircleBody> _ballsAtStartOfTurn = new List<ICircleBody>();

        /// <summary>
        /// Instantiates the core domain controller assigning completely explicit hardware interfaces mapping boundary layers securely.
        /// </summary>
        /// <param name="physicsEngine">Mathematical backend processing integration boundaries.</param>
        /// <param name="cueController">Input interaction parser resolving human vectors into impulse payloads.</param>
        /// <param name="inputProvider">Peripheral polling unit capturing synchronous commands preventing UI layer coupling.</param>
        public GameManager(
            IPhysicsEngine physicsEngine,
            ICueController cueController,
            IInputProvider inputProvider)
        {
            _physicsEngine = physicsEngine;
            _cueController = cueController;
            _inputProvider = inputProvider;
            _ruleValidator = new RuleValidator();

            _player1 = new Player(1, "Player 1");
            _player2 = new Player(2, "Player 2");

            CurrentState = GameState.Menu;
            ActivePlayer = _player1;
            RemainingTurnTime = 30.0f;
        }

        /// <summary>
        /// Triggers temporary textual interface banners assigning semantic intent used downstream to format graphic visualizers.
        /// </summary>
        /// <param name="message">The absolute string characters broadcast to the UI boundaries.</param>
        /// <param name="type">Semantic identifier deciding if rendering engines should map warning (red) or info (yellow) styling behaviors.</param>
        /// <param name="time">Floating duration limiting the banner lifetime in active frames.</param>
        public void SetNotification(string message, NotificationType type, float time)
        {
            NotificationMessage = message;
            NotificationType = type;
            NotificationTimer = time;
            OnNotificationEvent?.Invoke(type);
        }

        /// <inheritdoc />
        public void StartGame()
        {
            // Fully discard all dirty states and memory fragments, establishing absolute zero ground for a formal game entry.
            _player1.AssignedType = null;
            _player2.AssignedType = null;
            _player1.Score = 0;
            _player2.Score = 0;
            Winner = null;
            NotificationMessage = "";
            NotificationTimer = 0f;
            TargetPocket = null;
            RemainingTurnTime = 30.0f;
            
            ActivePlayer = _player1;
            SetTurnState();
        }

        /// <inheritdoc />
        public void EndTurn()
        {
            // Switch pointer targets forcing sequential player operations resolving legal turnovers.
            TargetPocket = null;
            ActivePlayer = ActivePlayer == _player1 ? _player2 : _player1;
            RemainingTurnTime = 30.0f;
            SetTurnState();
        }

        /// <summary>
        /// Validates player point progression deciding whether the logical pipeline should enter standardized aiming phases 
        /// or lock input requesting distinct manual 8-Ball pocket definitions protecting terminal match routes.
        /// </summary>
        private void SetTurnState()
        {
            RemainingTurnTime = 30.0f;
            if (ActivePlayer.Score == 7)
            {
                TargetPocket = null;
                CurrentState = GameState.CallingPocket;
                SetNotification("Select a pocket for the 8-Ball!", NotificationType.Decision, 3f);
            }
            else
            {
                CurrentState = GameState.PlayerTurn;
            }
        }

        /// <inheritdoc />
        public bool IsValidCueBallPlacement(Vector2 position)
        {
            // Stage 1: Explicitly guarantee coordinates satisfy generic field table constraints restricting off-map exploitation.
            var bounds = _physicsEngine.GetPlayfieldBounds();
            bool isInsideBounds = position.X >= bounds.Left + 10f && position.X <= bounds.Right - 10f &&
                                  position.Y >= bounds.Top + 10f && position.Y <= bounds.Bottom - 10f;
            if (!isInsideBounds) return false;

            // Stage 2: Ensure bodies do not magically teleport clipping active geometric targets mapping table sinks.
            var pockets = _physicsEngine.GetPockets();
            foreach (var pocket in pockets)
            {
                if ((pocket.Position - position).LengthSquared() < pocket.Radius * pocket.Radius)
                {
                    return false;
                }
            }

            // Stage 3: Resolve physical overlap overlaps against tangible dynamic bodies shielding intersecting artifacts.
            var bodies = _physicsEngine.GetBodies();
            if (bodies != null)
            {
                foreach (var body in bodies)
                {
                    if (body is ICircleBody circle)
                    {
                        float minDist = circle.Radius + 10f;
                        if ((circle.Position - position).LengthSquared() < minDist * minDist)
                        {
                            return false;
                        }
                    }
                }
            }
            // State is entirely secure for spawning.
            return true;
        }

        /// <inheritdoc />
        public void SelectTargetPocket(IPocket pocket)
        {
            // Acknowledges discrete input commands assigning the winning boundary.
            if (CurrentState == GameState.CallingPocket)
            {
                TargetPocket = pocket;
                CurrentState = GameState.PlayerTurn;
                RemainingTurnTime = 30.0f;
            }
        }

        /// <inheritdoc />
        public void PlaceCueBall(Vector2 position)
        {
            if (CurrentState == GameState.BallInHand && IsValidCueBallPlacement(position))
            {
                OnPlaceCueBall?.Invoke(position);
                RemainingTurnTime = 30.0f;
                SetTurnState();
            }
        }

        /// <summary>
        /// Executes authoritative timeout penalty resolution when the active turn countdown reaches zero during an aiming or decision phase.
        /// Broadcasts semantic foul notifications, aborts accumulated kinetic potentials on the cue, strips the table of the active cue ball,
        /// toggles turn authority to the opposing participant, and instantiates an explicit ball-in-hand recovery phase with a fresh 30-second window.
        /// </summary>
        private void HandleTimeout()
        {
            // Emit high-priority semantic foul notification triggering audio-visual alerts across boundaries.
            SetNotification("FOUL: Time Out", NotificationType.Foul, 3.5f);
            
            // Abort accumulated kinetic impulse potentials and clear any active overheat lockout flags on the cue stick.
            _cueController.ResetCharge();

            // Notify downstream listeners to prepare cue ball respawn lifecycles.
            OnScratchFoul?.Invoke();

            // Authoritatively eradicate cue ball (Id 0) from the physical world simulating scratch removal.
            var currentBodies = _physicsEngine.GetBodies();
            var existingCueBall = currentBodies?.FirstOrDefault(b => b.Id == 0);
            if (existingCueBall != null)
            {
                _physicsEngine.RemoveBody(existingCueBall);
            }

            // Invalidate previously declared 8-ball pockets and hand over table authority.
            TargetPocket = null;
            ActivePlayer = ActivePlayer == _player1 ? _player2 : _player1;
            CurrentState = GameState.BallInHand;
            RemainingTurnTime = 30.0f;
        }

        /// <inheritdoc />
        public void UpdateLogic(float deltaTime)
        {
            // Actively tick active UI banners completely decoupled from rendering pipeline frame-rates.
            if (NotificationTimer > 0f)
            {
                NotificationTimer -= deltaTime;
                if (NotificationTimer <= 0f) NotificationMessage = "";
            }

            // Turn Countdown Timer: active strictly in decision/aiming states
            if (CurrentState == GameState.PlayerTurn || CurrentState == GameState.ChargingShot || CurrentState == GameState.BallInHand)
            {
                RemainingTurnTime -= deltaTime;
                if (RemainingTurnTime <= 0f)
                {
                    RemainingTurnTime = 0f;
                    HandleTimeout();
                    return;
                }
            }

            // Core State Machine matching explicitly determined active game lifecycle sequential phases
            switch (CurrentState)
            {
                case GameState.Menu:
                    break;

                case GameState.CallingPocket:
                {
                    // Waits specifically for a valid pocket designation acknowledging Match Point.
                    if (_inputProvider.WasLeftMousePressed)
                    {
                        var mousePos = _inputProvider.MouseWorldPosition;
                        var pockets = _physicsEngine.GetPockets();
                        foreach (var pocket in pockets)
                        {
                            if ((pocket.Position - mousePos).LengthSquared() < pocket.Radius * pocket.Radius)
                            {
                                SelectTargetPocket(pocket);
                                break;
                            }
                        }
                    }
                    break;
                }

                case GameState.BallInHand:
                {
                    // Extracts internal valid placement checks ensuring strictly clean drops resolving foul penalties.
                    if (_inputProvider.WasLeftMouseReleased)
                    {
                        var mousePos = _inputProvider.MouseWorldPosition;
                        if (IsValidCueBallPlacement(mousePos))
                        {
                            PlaceCueBall(mousePos);
                        }
                    }
                    break;
                }

                case GameState.PlayerTurn:
                {
                    var cueBall = GetCueBall();
                    if (cueBall != null)
                    {
                        _cueController.UpdateAim(cueBall.Position, _inputProvider.MouseWorldPosition);
                    }

                    // Acknowledge a continuous confirmation sequence stepping into force multipliers.
                    if (_inputProvider.WasLeftMousePressed)
                    {
                        CurrentState = GameState.ChargingShot;
                    }
                    break;
                }

                case GameState.ChargingShot:
                {
                    bool wasOverheated = _cueController.IsOverheated;
                    _cueController.UpdateOverheat(deltaTime);

                    // Penalty cooling phase transitions directly back if previously exhausted potentials are neutralized.
                    if (wasOverheated && !_cueController.IsOverheated)
                    {
                        CurrentState = GameState.PlayerTurn;
                        break;
                    }

                    var cueBall = GetCueBall();

                    if (!_cueController.IsOverheated)
                    {
                        if (_inputProvider.IsLeftMouseDown)
                        {
                            _cueController.ChargeShot(deltaTime);
                        }
                        else if (!_inputProvider.WasLeftMouseReleased)
                        {
                            // Safely aborts charging interactions failing to resolve explicit release sequences.
                            CurrentState = GameState.PlayerTurn;
                        }
                        
                        // Fire mechanism generating immediate mechanical impacts leveraging the aggregated kinetic vectors.
                        if (_inputProvider.WasLeftMouseReleased)
                        {
                            _physicsEngine.ResetStrokeData();

                            if (cueBall != null)
                            {
                                _cueController.ExecuteShot(cueBall);
                            }
                            
                            // Immediately capture pre-computation snapshot boundaries mapping bodies prior to collision outputs.
                            var prev_bodies = _physicsEngine.GetBodies();
                            _ballsAtStartOfTurn = (prev_bodies != null ? prev_bodies : Array.Empty<IPhysicsBody>())
                                .OfType<ICircleBody>()
                                .ToList();

                            CurrentState = GameState.SimulatingBalls;
                        }
                    }
                    break;
                }

                case GameState.SimulatingBalls:
                {
                    // Suspend logic controls exclusively polling until mathematical inertia equates zero.
                    if (_physicsEngine.AreAllBodiesAtRest())
                    {
                        var currentBodies = (_physicsEngine.GetBodies() ?? Array.Empty<IPhysicsBody>())
                            .OfType<ICircleBody>().ToList();
                        
                        // Differential mapping mapping explicitly destroyed ball objects comparing pre and post snapshots.
                        var pocketedBalls = _ballsAtStartOfTurn
                            .Where(b => !currentBodies.Any(cb => cb.Id == b.Id))
                            .ToList();

                        var strokeData = _physicsEngine.CurrentStrokeData;

                        BallType? firstHitType = null;
                        if (strokeData.FirstBallHitId.HasValue)
                        {
                            var firstHit = _ballsAtStartOfTurn.FirstOrDefault(b => b.Id == strokeData.FirstBallHitId.Value);
                            firstHitType = firstHit?.BallType;
                        }
                        
                        // Dynamically analyze contextual subsets resolving constraints around remaining participant balls.
                        int ownBallsRemaining = 0;
                        if (ActivePlayer.AssignedType != null)
                        {
                            ownBallsRemaining = _ballsAtStartOfTurn.Count(b => b.BallType == ActivePlayer.AssignedType);
                        }
                        else
                        {
                            ownBallsRemaining = 7; 
                        }

                        // Determine explicitly critical 8-Ball win-loss terminating path constraints.
                        bool is8Sunk = pocketedBalls.Any(b => b.Id == 8);
                        bool is8SunkInTarget = false;
                        if (is8Sunk && TargetPocket != null)
                        {
                            // Strictly authenticate specific sink identity mappings avoiding false collision paths.
                            if (strokeData.SunkBallsToPockets.TryGetValue(8, out var sunkPocket) && sunkPocket == TargetPocket)
                            {
                                is8SunkInTarget = true;
                            }
                        }

                        // Encapsulate and seal explicit execution results within a pure functional packet decoupled from the machine.
                        var ctx = new RuleContext
                        {
                            PlayerAssignedType = ActivePlayer.AssignedType,
                            FirstHitBallId = strokeData.FirstBallHitId,
                            FirstHitBallType = firstHitType,
                            RailsHitAfterContact = strokeData.RailsHitAfterContact,
                            IsCueBallSunk = pocketedBalls.Any(b => b.Id == 0),
                            Is8BallSunk = is8Sunk,
                            Is8BallSunkInTarget = is8SunkInTarget,
                            AreAllOwnBallsSunkBeforeShot = (ownBallsRemaining == 0),
                            PocketedBallTypes = pocketedBalls.Where(b => b.Id != 0 && b.Id != 8).Select(b => b.BallType).ToList(),
                            PocketedBallIds = pocketedBalls.Select(b => b.Id).ToList()
                        };

                        // Offload analytical boundaries directly inside isolated domain services checking strict 8-Ball semantics.
                        var ruleResult = _ruleValidator.Validate(ctx);

                        // Initial rule alignments granting designated types (Solid/Stripes) binding identity to ongoing sessions.
                        if (ruleResult == RuleResult.Continue || ruleResult == RuleResult.TurnLost)
                        {
                            if (ActivePlayer.AssignedType == null)
                            {
                                // Force dynamic session updates isolating subsets into exact domain groupings.
                                var validPocketed = pocketedBalls.FirstOrDefault(b => b.BallType == BallType.Solid || b.BallType == BallType.Striped);
                                if (validPocketed != null)
                                {
                                    ActivePlayer.AssignedType = validPocketed.BallType;
                                    IPlayer opponent = ActivePlayer == _player1 ? _player2 : _player1;
                                    opponent.AssignedType = validPocketed.BallType == BallType.Solid ? BallType.Striped : BallType.Solid;
                                    SetNotification($"Player {ActivePlayer.Id} assigned to {validPocketed.BallType}s!", NotificationType.Decision, 3f);
                                }
                            }
                        }

                        // Synchronize logical progression indexing active subsets tracking score limits incrementally.
                        _player1.Score = _player1.AssignedType != null ? 7 - currentBodies.Count(b => b is ICircleBody cb && cb.BallType == _player1.AssignedType) : 0;
                        _player2.Score = _player2.AssignedType != null ? 7 - currentBodies.Count(b => b is ICircleBody cb && cb.BallType == _player2.AssignedType) : 0;

                        // Sequentially resolve explicit sequence states reacting deterministically to constraint validations.
                        switch (ruleResult)
                        {
                            case RuleResult.Foul:
                                // Map appropriate user context alerts reflecting explicit foul parameters.
                                if (ctx.IsCueBallSunk) { SetNotification("FOUL: Scratch", NotificationType.Foul, 3f); }
                                else if (ctx.FirstHitBallId == null) { SetNotification("FOUL: Missed", NotificationType.Foul, 3f); }
                                else if (ctx.PocketedBallIds.Count == 0 && ctx.RailsHitAfterContact == 0) { SetNotification("FOUL: No Rail Contact", NotificationType.Foul, 3f); }
                                else { SetNotification("FOUL: Wrong Ball First", NotificationType.Foul, 3f); }
                                
                                OnScratchFoul?.Invoke();
                                var existingCueBall = currentBodies.FirstOrDefault(b => b.Id == 0);
                                if (existingCueBall != null)
                                {
                                    _physicsEngine.RemoveBody(existingCueBall);
                                }
                                TargetPocket = null;
                                ActivePlayer = ActivePlayer == _player1 ? _player2 : _player1;
                                CurrentState = GameState.BallInHand;
                                RemainingTurnTime = 30.0f;
                                break;
                            case RuleResult.Continue:
                                SetTurnState();
                                break;
                            case RuleResult.TurnLost:
                                EndTurn();
                                break;
                            case RuleResult.GameOverWin:
                                Winner = ActivePlayer;
                                CurrentState = GameState.GameOver;
                                OnGameOver?.Invoke(); 
                                break;
                            case RuleResult.GameOverLose:
                                Winner = ActivePlayer == _player1 ? _player2 : _player1;
                                CurrentState = GameState.GameOver;
                                OnGameOver?.Invoke(); 
                                break;
                        }
                    }
                    break;
                }
                
                case GameState.GameOver:
                    break;
            }
        }

        /// <summary>
        /// Reads isolated target identifiers retrieving the physical cue ball context directly out of the integration pipeline.
        /// </summary>
        /// <returns>Matched valid physics constraint interface; otherwise null.</returns>
        private IPhysicsBody? GetCueBall()
        {
            var bodies = _physicsEngine.GetBodies();
            if (bodies == null) return null;
            return bodies.FirstOrDefault(b => b.Id == 0);
        }
    }
}
