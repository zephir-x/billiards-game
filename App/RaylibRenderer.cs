using BilliardsGame.Interfaces.Enums;
using BilliardsGame.Interfaces.Models;
using System;
using System.Numerics;
using BilliardsGame.Interfaces;
using Raylib_cs;

namespace BilliardsGame.App
{
    /// <summary>
    /// Identifies the distinct active view state inside the main menu navigation hierarchy.
    /// </summary>
    public enum MainMenuView
    {
        /// <summary>
        /// Root landing view offering Training, Play Online, and Exit options.
        /// </summary>
        Main,

        /// <summary>
        /// Multiplayer lobby hub offering Host Room, Join Room, and Back navigation.
        /// </summary>
        OnlineLobby,

        /// <summary>
        /// Host waiting screen displaying the assigned 5-digit room code and listening status.
        /// </summary>
        HostWaiting,

        /// <summary>
        /// Join room screen providing interactive keyboard input for the 5-digit room code.
        /// </summary>
        JoinPrompt
    }

    /// <summary>
    /// Master visualization mapping component interfacing exclusively with Raylib primitives.
    /// Operates completely disconnected from physical integrations relying solely on structural snapshots passed from the Scene layer.
    /// Decoupled design prevents any UI actions from leaking directly into or modifying the domain models.
    /// </summary>
    public class RaylibRenderer : IRenderer
    {
        private ISceneParameters? _sceneData;

        /// <inheritdoc />
        public bool IsPaused { get; set; }
        
        /// <summary>
        /// Internal global translation offset manipulating all 2D vector outputs ensuring scaling and panning operate seamlessly across various internal screen resolutions.
        /// </summary>
        public Camera2D MainCamera { get; private set; }
        
        /// <inheritdoc />
        public Action? OnPlayClicked { get; set; }

        /// <summary>
        /// Dispatched when the user initiates a local offline training or hotseat match.
        /// </summary>
        public Action? OnTrainingClicked { get; set; }

        /// <summary>
        /// Dispatched when the user clicks Host Game in the online lobby.
        /// </summary>
        public Action? OnHostGameClicked { get; set; }

        /// <summary>
        /// Dispatched when the user confirms joining an online match with a 5-digit room code.
        /// </summary>
        public Action<string>? OnJoinGameClicked { get; set; }

        /// <summary>
        /// Dispatched when the user cancels an active hosting or waiting sequence.
        /// </summary>
        public Action? OnCancelLobbyClicked { get; set; }

        /// <summary>
        /// Dispatched when host cancels waiting for client and leaves the room.
        /// </summary>
        public Action? OnCancelHostClicked { get; set; }

        /// <summary>
        /// Dispatched when client cancels join prompt and returns to lobby.
        /// </summary>
        public Action? OnCancelJoinClicked { get; set; }
        
        /// <inheritdoc />
        public Action? OnExitClicked { get; set; }
        
        /// <inheritdoc />
        public Action? OnContinueClicked { get; set; }
        
        /// <inheritdoc />
        public Action? OnRestartClicked { get; set; }

        /// <summary>
        /// Dispatched when user requests to abort or leave the session and return to the main menu.
        /// </summary>
        public Action? OnGoToMenuClicked { get; set; }

        /// <summary>
        /// Indicates whether the active session is a 1v1 online multiplayer match.
        /// When true, unilateral restart is hidden and menu return cleanly handles peer disconnect.
        /// </summary>
        public bool IsMultiplayerSession { get; set; } = false;

        /// <summary>
        /// Local participant player ID (1 for Host, 2 for Client, or 0 if local/offline).
        /// </summary>
        public int LocalPlayerId { get; set; } = 0;

        /// <summary>
        /// Local explicit trigger hooking audio playback loops strictly isolating the domain UI bindings out of the rendering arrays.
        /// </summary>
        public Action? OnButtonHovered { get; set; }

        /// <summary>
        /// Current sub-view state displayed within the menu sliding overlay.
        /// </summary>
        public MainMenuView CurrentMenuView { get; set; } = MainMenuView.Main;

        /// <summary>
        /// The 5-digit room code assigned to the host room by the relay server.
        /// </summary>
        public string HostRoomCode { get; set; } = string.Empty;

        /// <summary>
        /// Status text displayed on the host waiting screen.
        /// </summary>
        public string HostStatus { get; set; } = "Connecting to server...";

        /// <summary>
        /// Informational or error text displayed on the join prompt screen.
        /// </summary>
        public string JoinStatus { get; set; } = string.Empty;

        /// <summary>
        /// Real-time world position of the remote player's aiming or ball placement cursor.
        /// </summary>
        public Vector2 RemoteCursorWorldPosition { get; set; } = Vector2.Zero;

        /// <summary>
        /// Instantly dismisses active menus without sliding delay to eliminate flickering upon match entry.
        /// </summary>
        public void DismissMenuInstantly()
        {
            _menuXOffset = -1600f;
            _wasShowMenu = false;
            CurrentMenuView = MainMenuView.Main;
            _joinCodeInput = string.Empty;
            JoinStatus = string.Empty;
        }

        // Internal keyboard buffer storing user input for the 5-digit room code.
        private string _joinCodeInput = string.Empty;
        
        private string _lastHoveredId = "none";
        
        // Easing interpolation tracking values mapping menu slide-in transitions.
        private float _menuXOffset = 1600f;
        private float _pauseXOffset = -1600f;
        private float _gameOverXOffset = 1600f;
        
        private bool _wasShowMenu;
        private bool _wasShowPause;
        private bool _wasShowGameOver;

        // Specialized interpolator bypassing Math functions to guarantee Raylib specific vector handling clamps.
        private float Lerp(float start, float end, float amount)
        {
            if (amount > 1f) amount = 1f;
            if (amount < 0f) amount = 0f;
            return start + (end - start) * amount;
        }

        /// <inheritdoc />
        public void Initialize(ISceneParameters sceneData)
        {
            _sceneData = sceneData;
            MainCamera = new Camera2D
            {
                Target = new Vector2(500, 400),
                Offset = new Vector2(1600 / 2f, 900 / 2f + 90),
                Rotation = 0.0f,
                Zoom = 1.33f
            };
        }

        private Color ParseHexColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.RayWhite;
            if (hex.StartsWith("#")) hex = hex.Substring(1);
            if (hex.Length == 6)
            {
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                return new Color((int)r, (int)g, (int)b, 255);
            }
            return Color.RayWhite;
        }

        /// <inheritdoc />
        public void DrawFrame(float interpolationAlpha)
        {
            // Execute hardware lock opening rendering buffer.
            Raylib.BeginDrawing();
            
            float dt = Raylib.GetFrameTime();
            // Restrict catastrophic jump potentials resolving long background executions by locking maximum visual skips.
            if (dt > 0.033f) dt = 0.033f; 
            
            // Map state logic to UI components driving transition evaluations.
            if (_sceneData != null)
            {
                bool showMenu = (_sceneData.CurrentState == GameState.Menu);
                bool showGameOver = (_sceneData.CurrentState == GameState.GameOver);
                bool showPause = (!showMenu && !showGameOver && IsPaused);
                
                if (showMenu)
                {
                    if (!_wasShowMenu) _menuXOffset = 1600f;
                    _menuXOffset = Lerp(_menuXOffset, 0f, dt * 10f);
                }
                else
                {
                    _menuXOffset = Lerp(_menuXOffset, -1600f, dt * 8.5f);
                    if (_menuXOffset < -1590f)
                    {
                        _menuXOffset = -1600f;
                        CurrentMenuView = MainMenuView.Main;
                        _joinCodeInput = string.Empty;
                        JoinStatus = string.Empty;
                    }
                }

                if (showPause && !_wasShowPause) _pauseXOffset = 1600f;
                if (showGameOver && !_wasShowGameOver) _gameOverXOffset = 1600f;
                
                _wasShowMenu = showMenu;
                _wasShowPause = showPause;
                _wasShowGameOver = showGameOver;

                float easeSpeed = 10f;
                _pauseXOffset = Lerp(_pauseXOffset, showPause ? 0f : -1600f, dt * easeSpeed);
                _gameOverXOffset = Lerp(_gameOverXOffset, showGameOver ? 0f : 1600f, dt * easeSpeed);
            }

            Raylib.ClearBackground(new Color(5, 5, 20, 255));
            Raylib.DrawRectangleGradientH(0, 0, 1600, 900, new Color(5, 5, 25, 255), new Color(40, 5, 5, 255));

            Raylib.BeginMode2D(MainCamera);

            // Layer 1. Outer Dark Wood Base Structure
            Rectangle outerWood = new Rectangle(140, 140, 720, 520);
            Raylib.DrawRectangleRounded(outerWood, 0.15f, 30, new Color(60, 30, 10, 255));

            // Layer 2. Inner Green Cloth Definition
            Rectangle cloth = new Rectangle(180, 180, 640, 440);
            Raylib.DrawRectangleRounded(cloth, 0.05f, 10, new Color(20, 105, 50, 255));

            // Layer 3. Pocket Holes dynamically iterating active physics sinks targeting holes spanning through the table mesh.
            if (_sceneData?.Pockets != null)
            {
                foreach (var pocket in _sceneData.Pockets)
                {
                    float visualRadius = 23f; 
                    Raylib.DrawCircleV(pocket.Position, visualRadius, Color.Black);
                    Raylib.DrawCircleLines((int)pocket.Position.X, (int)pocket.Position.Y, visualRadius * 0.9f, new Color(40, 40, 40, 200));
                    Raylib.DrawCircleLines((int)pocket.Position.X, (int)pocket.Position.Y, visualRadius * 0.6f, new Color(20, 20, 20, 100));
                }
            }

            // Process fading ghosts applying isolated life logic independent of main physics iteration.
            if (_sceneData?.GhostBodies != null)
            {
                foreach (var body in _sceneData.GhostBodies)
                {
                    if (body is ICircleBody circleBody)
                    {
                        // Lerp coordinates based strictly on rendering boundaries avoiding simulation jitter.
                        Vector2 renderedPos = (body.PreviousPosition * (1f - interpolationAlpha)) + (body.Position * interpolationAlpha);
                        float alphaFraction = Math.Clamp(circleBody.GhostLifeTime / 1.5f, 0f, 1f);
                        DrawBall(circleBody, renderedPos, (int)(255 * alphaFraction));
                    }
                }
            }

            if (_sceneData?.Bodies != null)
            {
                foreach (var body in _sceneData.Bodies)
                {
                    if (body is ICircleBody circleBody)
                    {
                        Vector2 renderedPos = (body.PreviousPosition * (1f - interpolationAlpha)) + (body.Position * interpolationAlpha);
                        DrawBall(circleBody, renderedPos, 255);
                    }
                }
            }

            // Layer 5. Assemble exact geometric buffers matching the underlying Physics cushion borders mapping exact angles.
            Color railColor = new Color(139, 69, 19, 255);
            Color railBorder = new Color(60, 30, 10, 255);
            
            Action<Vector2, Vector2, Vector2, Vector2, Color> DrawQuad = (v1, v2, v3, v4, col) => 
            {
                Raylib.DrawTriangle(v1, v2, v3, col);
                Raylib.DrawTriangle(v1, v3, v2, col);
                Raylib.DrawTriangle(v1, v3, v4, col);
                Raylib.DrawTriangle(v1, v4, v3, col);
            };
            
            Action<Vector2, Vector2, Vector2, Vector2, float, Color> DrawQuadLines = (v1, v2, v3, v4, t, col) => 
            {
                Raylib.DrawLineEx(v1, v2, t, col);
                Raylib.DrawLineEx(v2, v3, t, col);
                Raylib.DrawLineEx(v3, v4, t, col);
                Raylib.DrawLineEx(v4, v1, t, col);
                Raylib.DrawCircleV(v1, t/2, col);
                Raylib.DrawCircleV(v2, t/2, col);
                Raylib.DrawCircleV(v3, t/2, col);
                Raylib.DrawCircleV(v4, t/2, col);
            };

            DrawQuad(new Vector2(212.5f, 180), new Vector2(232.5f, 200), new Vector2(466f, 200), new Vector2(476f, 180), railColor);
            DrawQuadLines(new Vector2(212.5f, 180), new Vector2(232.5f, 200), new Vector2(466f, 200), new Vector2(476f, 180), 3f, railBorder);

            DrawQuad(new Vector2(524f, 180), new Vector2(534f, 200), new Vector2(767.5f, 200), new Vector2(787.5f, 180), railColor);
            DrawQuadLines(new Vector2(524f, 180), new Vector2(534f, 200), new Vector2(767.5f, 200), new Vector2(787.5f, 180), 3f, railBorder);

            DrawQuad(new Vector2(232.5f, 600), new Vector2(212.5f, 620), new Vector2(476f, 620), new Vector2(466f, 600), railColor);
            DrawQuadLines(new Vector2(232.5f, 600), new Vector2(212.5f, 620), new Vector2(476f, 620), new Vector2(466f, 600), 3f, railBorder);

            DrawQuad(new Vector2(534f, 600), new Vector2(524f, 620), new Vector2(787.5f, 620), new Vector2(767.5f, 600), railColor);
            DrawQuadLines(new Vector2(534f, 600), new Vector2(524f, 620), new Vector2(787.5f, 620), new Vector2(767.5f, 600), 3f, railBorder);

            DrawQuad(new Vector2(180, 212.5f), new Vector2(180, 587.5f), new Vector2(200, 567.5f), new Vector2(200, 232.5f), railColor);
            DrawQuadLines(new Vector2(180, 212.5f), new Vector2(180, 587.5f), new Vector2(200, 567.5f), new Vector2(200, 232.5f), 3f, railBorder);

            DrawQuad(new Vector2(800, 232.5f), new Vector2(800, 567.5f), new Vector2(820, 587.5f), new Vector2(820, 212.5f), railColor);
            DrawQuadLines(new Vector2(800, 232.5f), new Vector2(800, 567.5f), new Vector2(820, 587.5f), new Vector2(820, 212.5f), 3f, railBorder);

            // Execute visual pulse indicators focusing player trajectory logic toward critical 8-Ball destinations.
            if (_sceneData?.CurrentState == GameState.CallingPocket)
            {
                bool isMySelection = !IsMultiplayerSession || (LocalPlayerId == (_sceneData!.CurrentTurnPlayer?.Id ?? 1));
                if (isMySelection)
                {
                    float t = (float)Raylib.GetTime();
                    float blinkAlpha = (MathF.Sin(t * 10f) + 1f) / 2f; 
                    Color blinkCol = new Color(255, 255, 0, (int)(255 * blinkAlpha));
                    
                    if (_sceneData.Pockets != null)
                    {
                        foreach (var pocket in _sceneData.Pockets)
                        {
                            Raylib.DrawCircleLinesV(pocket.Position, 28f, blinkCol);
                            Raylib.DrawCircleLinesV(pocket.Position, 27f, blinkCol);
                        }
                    }
                }
            }

            // Expose the final targeted binding acknowledging user confirmation explicitly.
            if (_sceneData?.TargetPocket != null && (_sceneData.CurrentState == GameState.PlayerTurn || _sceneData.CurrentState == GameState.CallingPocket))
            {
                Raylib.DrawCircleLinesV(_sceneData.TargetPocket.Position, 26f, new Color(255, 215, 0, 150));
            }

            // Map manual spawn controls directly linking cursor validations securely isolated inside the Rule definitions checking boundary collision arrays.
            if (_sceneData?.CurrentState == GameState.BallInHand)
            {
                bool isMyPlacementTurn = !IsMultiplayerSession || (LocalPlayerId == (_sceneData?.CurrentTurnPlayer?.Id ?? 1));
                Vector2 placementPos = isMyPlacementTurn
                    ? Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), MainCamera)
                    : RemoteCursorWorldPosition;

                bool isValid = _sceneData!.IsValidCueBallPlacement(placementPos);
                
                Color placementColor = isValid ? new Color(255, 255, 255, 140) : new Color(255, 50, 50, 150);
                Color ringColor = isValid ? new Color(80, 255, 110, 220) : new Color(255, 45, 45, 230);

                Raylib.DrawCircleV(placementPos, 10f, placementColor);
                Raylib.DrawRing(placementPos, 13.5f, 15.5f, 0f, 360f, 48, ringColor);
            }

            // Interface visualizing exact physical strike sequences based on accumulated kinetic telemetry.
            if (_sceneData?.CueInfo != null && (_sceneData.CurrentState == GameState.ChargingShot || _sceneData.CurrentState == GameState.PlayerTurn))
            {
                var cueBall = GetCueBall();
                var cueInfo = _sceneData.CueInfo;
                if (cueBall != null)
                {
                    Vector2 cueBallRenderedPos = (cueBall.PreviousPosition * (1f - interpolationAlpha)) + (cueBall.Position * interpolationAlpha);
                    float offset = cueBall.Radius + 8f + (cueInfo.Power * 50f);
                    Vector2 cueDir = cueInfo.CueDirection;
                    if (cueDir != Vector2.Zero)
                    {
                        // Draw Aim Assist path exclusively for the active turn player
                        bool isMyAimingTurn = !IsMultiplayerSession || (LocalPlayerId == (_sceneData?.CurrentTurnPlayer?.Id ?? 1));
                        if (isMyAimingTurn)
                        {
                            float baseSpacing = 15f;
                            float dynamicSpacing = baseSpacing + (cueInfo.Power * 15f);
                            for (int i = 0; i < 7; i++)
                            {
                                float d = (cueBall.Radius + 12f) + (i * dynamicSpacing);
                                Vector2 ghostPoint = cueBallRenderedPos + (cueDir * d);
                                Raylib.DrawCircleV(ghostPoint, 2f, new Color(255, 255, 255, 120));
                            }
                        }

                        // Draw Physical Cue Body matching backward retraction paths proportional to accumulated forces.
                        float cueLength = 300f;
                        Vector2 cueStart = cueBallRenderedPos - (cueDir * offset);
                        Vector2 cueEnd = cueBallRenderedPos - (cueDir * (offset + cueLength));

                        Raylib.DrawLineEx(cueStart, cueEnd, 8f, new Color(30, 15, 5, 255));
                        Raylib.DrawLineEx(cueStart, cueEnd, 6f, new Color(222, 184, 135, 255));
                        Raylib.DrawLineEx(cueStart, cueEnd, 2f, new Color(110, 55, 15, 255));
                        
                        float buttLength = 100f;
                        Vector2 cueHandleStart = cueBallRenderedPos - (cueDir * (offset + cueLength - buttLength));
                        Raylib.DrawLineEx(cueHandleStart, cueEnd, 6f, new Color(25, 25, 25, 255));
                        Raylib.DrawLineEx(cueHandleStart, cueEnd, 2f, new Color(50, 50, 50, 255));
                        
                        Vector2 cueTipEnd = cueStart + (cueDir * 4f);
                        Raylib.DrawLineEx(cueStart, cueTipEnd, 6f, new Color(100, 150, 255, 255));
                    }
                }
            }
            Raylib.EndMode2D();

            // Screen space UI Overlay
            if (_sceneData != null)
            {
                if (_sceneData.CurrentState != GameState.Menu && _sceneData.CurrentState != GameState.GameOver)
                {
                    string turnText = _sceneData.CurrentTurnPlayer != null  ? "Turn: " + _sceneData.CurrentTurnPlayer.Name : "Turn: None";
                    int turnTextWidth = Raylib.MeasureText(turnText, 36); 
                    Raylib.DrawText(turnText, 1600 / 2 - turnTextWidth / 2, 20, 36, Color.RayWhite);

                    // Execute robust notification typologies scaling textual values smoothly mapped against logic timers.
                    string notifMsg = _sceneData.NotificationMessage;
                    if (_sceneData.CurrentState == GameState.CallingPocket && IsMultiplayerSession)
                    {
                        bool isMySelection = (LocalPlayerId == (_sceneData.CurrentTurnPlayer?.Id ?? 1));
                        if (!isMySelection)
                        {
                            notifMsg = $"{_sceneData.CurrentTurnPlayer?.Name ?? "Opponent"} is calling an 8-Ball pocket...";
                        }
                    }

                    if (!string.IsNullOrEmpty(notifMsg) && _sceneData.NotificationTimer > 0f)
                    {
                        Font fn = Raylib.GetFontDefault();
                        int fSize = 28;
                        int fWidth = (int)Raylib.MeasureTextEx(fn, notifMsg, fSize, 1f).X;
                        
                        float currentTimer = _sceneData.NotificationTimer;
                        float alphaFraction = 1f;

                        if (currentTimer < 0.5f) { alphaFraction = currentTimer / 0.5f; }
                        else if (3f - currentTimer < 0.5f) { alphaFraction = (3f - currentTimer) / 0.5f; }
                        
                        byte alphaByte = (byte)(255 * Math.Clamp(alphaFraction, 0f, 1f));
                        Color baseColor = _sceneData.NotificationType switch {
                            NotificationType.Foul => new Color(255, 0, 0, 255),
                            NotificationType.Decision => new Color(255, 255, 0, 255),
                            NotificationType.Info => new Color(255, 255, 0, 255),
                            _ => new Color(255, 255, 255, 255)
                        };
                        Color drawColor = new Color((int)baseColor.R, (int)baseColor.G, (int)baseColor.B, (int)alphaByte);

                        Vector2 origin = new Vector2(fWidth / 2f, fSize / 2f);
                        Vector2 pos = new Vector2(1600 / 2f, 125f);
                        
                        Raylib.DrawTextPro(fn, notifMsg, pos, origin, 0f, fSize, 1f, drawColor);
                    }

                    // Render rounded, glassmorphic HUD player cards with centered stats and integrated flush turn timer.
                    var p1 = _sceneData.Player1;
                    var p2 = _sceneData.Player2;
                    int boxW = 290; int boxH = 152; int pad = 16;
                    int p1X = pad; int p1Y = 14;
                    int p2X = 1600 - boxW - pad; int p2Y = 14;

                    DrawPlayerHUDCard(p1X, p1Y, boxW, boxH, p1, 1, "Player 1");
                    DrawPlayerHUDCard(p2X, p2Y, boxW, boxH, p2, 2, "Player 2");

                    // Dynamic Power bar overlay matching cue states natively reflecting valid/broken overheat mechanics.
                    if (_sceneData.CueInfo != null)
                    {
                        int barWidth = 600; int barHeight = 24;
                        int barX = 1600/2 - barWidth / 2; int barY = 70;

                        Raylib.DrawRectangle(barX, barY, barWidth, barHeight, Color.DarkGray);
                        
                        float p = _sceneData.CueInfo.Power;
                        int fillWidth = (int)(barWidth * p);
                        Color fillColor;
                        
                        if (_sceneData.CueInfo.IsOverheated) {
                            fillColor = Color.Red;
                        } 
                        else 
                        {
                            int r = (int)(100 + (255 - 100) * p);
                            int g = (int)(150 + (140 - 150) * p);
                            int b = (int)(255 + (0 - 255) * p);
                            fillColor = new Color(r, g, b, 255);
                        }
                        
                        if (fillWidth > 0) Raylib.DrawRectangle(barX, barY, fillWidth, barHeight, fillColor);
                        Raylib.DrawRectangleLines(barX, barY, barWidth, barHeight, Color.LightGray);
                    }
                }
                
                if (_menuXOffset > -1590f) DrawMenuContainer(_menuXOffset);
                if (_gameOverXOffset < 1599f) DrawGameOverMenu(_gameOverXOffset);
                if (_pauseXOffset > -1599f) DrawPauseMenu(_pauseXOffset);
            }

            Raylib.EndDrawing();
        }

        /// <summary>
        /// Renders an aesthetically polished glassmorphic HUD card for a player with rounded corners,
        /// dynamic active turn glowing indicators, ball type visualization, and pocketed ball pips.
        /// </summary>
        /// <param name="x">X-coordinate of the card's top-left origin.</param>
        /// <param name="y">Y-coordinate of the card's top-left origin.</param>
        /// <param name="width">Card width in pixels.</param>
        /// <param name="height">Card height in pixels.</param>
        /// <param name="player">Player domain instance containing assigned type and score.</param>
        /// <param name="playerId">Numeric player identifier (1 or 2).</param>
        /// <param name="playerLabel">Display name or role label.</param>
        private void DrawPlayerHUDCard(int x, int y, int width, int height, IPlayer? player, int playerId, string playerLabel)
        {
            Rectangle cardRect = new Rectangle(x, y, width, height);
            bool isCurrentTurn = (_sceneData?.CurrentTurnPlayer?.Id == playerId);
            bool isLocalParticipant = IsMultiplayerSession && (LocalPlayerId == playerId);
            int pocketed = player?.Score ?? 0;
            bool is8BallReady = (pocketed == 7);
            bool isPlayer1 = (playerId == 1);

            // 1. Player-specific glassmorphic body background (Blue for P1, Orange/Crimson for P2)
            Color bodyBg;
            if (is8BallReady)
            {
                bodyBg = isPlayer1 ? new Color(34, 38, 18, 245) : new Color(38, 34, 16, 245);
            }
            else
            {
                bodyBg = isPlayer1 ? new Color(14, 22, 38, 235) : new Color(36, 18, 16, 235);
            }
            Raylib.DrawRectangleRounded(cardRect, 0.18f, 16, bodyBg);

            // 2. Subtle top header bar tint matching respective player identity
            Rectangle headerBar = new Rectangle(x, y, width, 34);
            Color headerColor;
            if (is8BallReady)
            {
                headerColor = isPlayer1 ? new Color(85, 75, 22, 245) : new Color(88, 70, 20, 245);
            }
            else if (isCurrentTurn)
            {
                headerColor = isPlayer1 ? new Color(28, 55, 95, 240) : new Color(85, 40, 26, 240);
            }
            else
            {
                headerColor = isPlayer1 ? new Color(18, 30, 52, 240) : new Color(46, 22, 18, 240);
            }
            Raylib.DrawRectangleRounded(headerBar, 0.18f, 16, headerColor);

            // 3. Dynamic border styling matching active turn, 8-ball ready status, and player theme
            if (isCurrentTurn)
            {
                float pulse = (float)Math.Sin(Raylib.GetTime() * 4.5) * 0.5f + 0.5f;
                Color glowBorder;
                if (is8BallReady)
                {
                    glowBorder = new Color(255, (int)(210 + 45 * pulse), 50, 255);
                }
                else
                {
                    glowBorder = isPlayer1
                        ? new Color((int)(50 + 40 * pulse), (int)(165 + 60 * pulse), 255, 255)
                        : new Color(255, (int)(150 + 50 * pulse), (int)(30 + 40 * pulse), 255);
                }
                Raylib.DrawRectangleRoundedLines(cardRect, 0.18f, 16, glowBorder);

                // Pulsing Active Turn indicator pip on upper-right corner
                int pipX = x + width - 18;
                int pipY = y + 17;
                Color activePipColor = isPlayer1
                    ? new Color(60, 200, 255, (int)(160 + 95 * pulse))
                    : new Color(255, 175, 30, (int)(160 + 95 * pulse));
                Raylib.DrawCircle(pipX, pipY, 5f + 2f * pulse, activePipColor);
                Raylib.DrawCircle(pipX, pipY, 2.5f, Color.RayWhite);
            }
            else if (is8BallReady)
            {
                Raylib.DrawRectangleRoundedLines(cardRect, 0.18f, 16, new Color(230, 190, 50, 220));
            }
            else if (isLocalParticipant)
            {
                Color localBorder = isPlayer1 ? new Color(90, 160, 230, 200) : new Color(240, 140, 50, 200);
                Raylib.DrawRectangleRoundedLines(cardRect, 0.18f, 16, localBorder);
            }
            else
            {
                Color inactiveBorder = isPlayer1 ? new Color(35, 60, 95, 180) : new Color(90, 45, 35, 180);
                Raylib.DrawRectangleRoundedLines(cardRect, 0.18f, 16, inactiveBorder);
            }

            // 4. Player Title text (Centered)
            int titleSize = 21;
            Color titleColor;
            if (is8BallReady)
            {
                titleColor = Color.Gold;
            }
            else if (isCurrentTurn)
            {
                titleColor = Color.RayWhite;
            }
            else
            {
                titleColor = isPlayer1 ? new Color(195, 220, 255, 255) : new Color(255, 215, 195, 255);
            }
            int titleW = Raylib.MeasureText(playerLabel, titleSize);
            Raylib.DrawText(playerLabel, x + (width - titleW) / 2, y + 7, titleSize, titleColor);

            // 5. Ball Type section (Centered text, clean modern typography)
            string typeStr;
            if (player?.AssignedType == BallType.Solid)
            {
                typeStr = "Type: Solid Balls";
            }
            else if (player?.AssignedType == BallType.Striped)
            {
                typeStr = "Type: Striped Balls";
            }
            else
            {
                typeStr = "Table Open";
            }

            Color typeColor;
            if (isPlayer1)
            {
                typeColor = (player?.AssignedType != null)
                    ? new Color(90, 190, 255, 255)
                    : new Color(160, 195, 225, 255);
            }
            else
            {
                typeColor = (player?.AssignedType != null)
                    ? new Color(255, 160, 50, 255)
                    : new Color(225, 185, 175, 255);
            }
            int valSize = 18;
            int typeW = Raylib.MeasureText(typeStr, valSize);
            Raylib.DrawText(typeStr, x + (width - typeW) / 2, y + 42, valSize, typeColor);

            // 6. Visual Ball Pips progress bar (7 circular pips - Themed colors for P1 vs P2)
            int pipRowY = y + 75;
            int totalPips = 7;
            float pipSpacing = 28f;
            float pipsStartX = x + (width - (totalPips - 1) * pipSpacing) / 2f;

            Color sunkPipColor = isPlayer1
                ? new Color(75, 185, 255, 255)
                : new Color(255, 155, 40, 255);
            Color socketBgColor = isPlayer1
                ? new Color(18, 30, 48, 220)
                : new Color(45, 22, 18, 220);
            Color socketBorderColor = isPlayer1
                ? new Color(45, 75, 115, 180)
                : new Color(110, 55, 40, 180);

            for (int i = 0; i < totalPips; i++)
            {
                float px = pipsStartX + (i * pipSpacing);
                if (i < pocketed)
                {
                    // Sunk ball: glowing themed pip (Blue for P1, Orange for P2) with specular glint
                    Raylib.DrawCircle((int)px, pipRowY, 5.5f, sunkPipColor);
                    Raylib.DrawCircle((int)px - 1, pipRowY - 1, 1.8f, Color.RayWhite);
                }
                else
                {
                    // Remaining ball: subtle dark socket with themed outline
                    Raylib.DrawCircle((int)px, pipRowY, 4f, socketBgColor);
                    Raylib.DrawCircleLines((int)px, pipRowY, 4.5f, socketBorderColor);
                }
            }

            // 7. Divider line before integrated timer dock
            Color dividerColor = isPlayer1 ? new Color(35, 60, 95, 180) : new Color(90, 45, 35, 180);
            Raylib.DrawLine(x + 10, y + 99, x + width - 10, y + 99, dividerColor);

            // 8. Integrated Timer Dock (flush borders aligned exactly with the main card)
            Rectangle timerDock = new Rectangle(x + 8, y + 106, width - 16, 36);
            if (isCurrentTurn)
            {
                float remainingSeconds = _sceneData?.TurnTimer ?? 15.0f;
                if (remainingSeconds < 0f) remainingSeconds = 0f;

                Color timerColor = isPlayer1 ? new Color(100, 205, 255, 255) : Color.Gold;
                Color dockBg = isPlayer1 ? new Color(18, 38, 65, 230) : new Color(55, 28, 20, 230);
                if (remainingSeconds <= 5.0f)
                {
                    float pulse = (float)Math.Sin(Raylib.GetTime() * 12.0) * 0.5f + 0.5f;
                    timerColor = new Color(255, (int)(50 + 100 * pulse), 50, 255);
                    dockBg = new Color(60, 20, 25, 230);
                }

                Raylib.DrawRectangleRounded(timerDock, 0.28f, 10, dockBg);
                Raylib.DrawRectangleRoundedLines(timerDock, 0.28f, 10, timerColor);

                string timerText = $"TIME: {remainingSeconds:F1}s";
                int timerFontSize = 18;
                int timerTextW = Raylib.MeasureText(timerText, timerFontSize);
                Raylib.DrawText(timerText, (int)(timerDock.X + (timerDock.Width - timerTextW) / 2), (int)(timerDock.Y + (timerDock.Height - timerFontSize) / 2), timerFontSize, timerColor);
            }
            else
            {
                Color waitingDockBg = isPlayer1 ? new Color(12, 18, 30, 180) : new Color(24, 14, 14, 180);
                Color waitingTextColor = isPlayer1 ? new Color(90, 115, 150, 180) : new Color(145, 105, 95, 180);
                Raylib.DrawRectangleRounded(timerDock, 0.28f, 10, waitingDockBg);
                string waitingText = "WAITING...";
                int waitFontSize = 16;
                int waitW = Raylib.MeasureText(waitingText, waitFontSize);
                Raylib.DrawText(waitingText, (int)(timerDock.X + (timerDock.Width - waitW) / 2), (int)(timerDock.Y + (timerDock.Height - waitFontSize) / 2), waitFontSize, waitingTextColor);
            }
        }
        
        private bool DrawModernButton(Rectangle rect, string text, bool isHoverED)
        {
            Rectangle shadowRect = new Rectangle(rect.X + 4, rect.Y + 4, rect.Width, rect.Height);
            Raylib.DrawRectangleRounded(shadowRect, 0.4f, 16, new Color(0, 0, 0, 100));

            Color baseColor = isHoverED ? new Color(74, 82, 102, 255) : new Color(45, 52, 65, 255);
            Raylib.DrawRectangleRounded(rect, 0.4f, 16, baseColor);

            if (isHoverED)
            {
                Rectangle outline = new Rectangle(rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4);
                Raylib.DrawRectangleRounded(outline, 0.4f, 16, new Color(255, 190, 20, 200));
                Raylib.DrawRectangleRounded(rect, 0.4f, 16, baseColor); 
            }

            int fontSize = 32;
            int textW = Raylib.MeasureText(text, fontSize);
            Vector2 textPos = new Vector2(rect.X + rect.Width / 2f - textW / 2f, rect.Y + rect.Height / 2f - fontSize / 2f);
            Raylib.DrawText(text, (int)textPos.X, (int)textPos.Y, fontSize, isHoverED ? Color.White : Color.LightGray);
            
            return isHoverED;
        }

        /// <summary>
        /// Routes rendering of the sliding menu container to the active sub-view according to <see cref="CurrentMenuView"/>.
        /// </summary>
        private void DrawMenuContainer(float offsetX)
        {
            switch (CurrentMenuView)
            {
                case MainMenuView.Main:
                    DrawMainMenu(offsetX);
                    break;
                case MainMenuView.OnlineLobby:
                    DrawOnlineLobbyMenu(offsetX);
                    break;
                case MainMenuView.HostWaiting:
                    DrawHostWaitingMenu(offsetX);
                    break;
                case MainMenuView.JoinPrompt:
                    DrawJoinPromptMenu(offsetX);
                    break;
            }
        }

        /// <summary>
        /// Renders the root main menu view offering Play Online, Training (local hotseat), and Exit.
        /// </summary>
        private void DrawMainMenu(float offsetX)
        {
            int screenW = 1600; int screenH = 900;
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, (int)Math.Clamp(210 * (1f - Math.Abs(offsetX)/1600f), 0, 255)));
            Vector2 mousePos = Raylib.GetMousePosition();
            bool isClick = Raylib.IsMouseButtonReleased(MouseButton.Left);

            int btnWidth = 280; int btnHeight = 65; int gap = 20;
            int startX = (screenW - btnWidth) / 2 + (int)offsetX;
            int startY = screenH / 2 - 40;

            int titleW = Raylib.MeasureText("2D Billiards", 100);
            Raylib.DrawText("2D Billiards", (int)offsetX + screenW / 2 - titleW / 2, 170, 100, Color.RayWhite);
            
            Raylib.DrawText("Zephir Edition", (int)offsetX + screenW / 2 - Raylib.MeasureText("Zephir Edition", 30) / 2, 280, 30, Color.Gold);

            Rectangle onlineRect = new Rectangle(startX, startY, btnWidth, btnHeight);
            Rectangle trainRect = new Rectangle(startX, startY + btnHeight + gap, btnWidth, btnHeight);
            Rectangle exitRect = new Rectangle(startX, startY + (btnHeight + gap) * 2, btnWidth, btnHeight);

            bool onlineHover = Raylib.CheckCollisionPointRec(mousePos, onlineRect);
            bool trainHover = Raylib.CheckCollisionPointRec(mousePos, trainRect);
            bool exitHover = Raylib.CheckCollisionPointRec(mousePos, exitRect);

            string currentHover = onlineHover ? "online" : (trainHover ? "train" : (exitHover ? "exit" : "none"));
            if (currentHover != "none" && currentHover != _lastHoveredId) OnButtonHovered?.Invoke();
            _lastHoveredId = currentHover;

            DrawModernButton(onlineRect, "Play Online", onlineHover);
            DrawModernButton(trainRect, "Training", trainHover);
            DrawModernButton(exitRect, "Exit", exitHover);

            if (onlineHover && isClick) CurrentMenuView = MainMenuView.OnlineLobby;
            if (trainHover && isClick) OnTrainingClicked?.Invoke();
            if (exitHover && isClick) OnExitClicked?.Invoke();
        }

        /// <summary>
        /// Renders the online multiplayer lobby screen with Host, Join, and Back navigation.
        /// </summary>
        private void DrawOnlineLobbyMenu(float offsetX)
        {
            int screenW = 1600; int screenH = 900;
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, (int)Math.Clamp(210 * (1f - Math.Abs(offsetX)/1600f), 0, 255)));
            Vector2 mousePos = Raylib.GetMousePosition();
            bool isClick = Raylib.IsMouseButtonReleased(MouseButton.Left);

            int titleW = Raylib.MeasureText("ONLINE MULTIPLAYER", 60);
            Raylib.DrawText("ONLINE MULTIPLAYER", (int)offsetX + screenW / 2 - titleW / 2, 180, 60, Color.RayWhite);

            int btnWidth = 320; int btnHeight = 65; int gap = 24;
            int startX = (screenW - btnWidth) / 2 + (int)offsetX;
            int startY = screenH / 2 - 40;

            Rectangle hostRect = new Rectangle(startX, startY, btnWidth, btnHeight);
            Rectangle joinRect = new Rectangle(startX, startY + btnHeight + gap, btnWidth, btnHeight);
            Rectangle backRect = new Rectangle(startX, startY + (btnHeight + gap) * 2, btnWidth, btnHeight);

            bool hostHover = Raylib.CheckCollisionPointRec(mousePos, hostRect);
            bool joinHover = Raylib.CheckCollisionPointRec(mousePos, joinRect);
            bool backHover = Raylib.CheckCollisionPointRec(mousePos, backRect);

            string currentHover = hostHover ? "host" : (joinHover ? "join" : (backHover ? "back" : "none"));
            if (currentHover != "none" && currentHover != _lastHoveredId) OnButtonHovered?.Invoke();
            _lastHoveredId = currentHover;

            DrawModernButton(hostRect, "Host Match", hostHover);
            DrawModernButton(joinRect, "Join Match", joinHover);
            DrawModernButton(backRect, "Back", backHover);

            if (hostHover && isClick)
            {
                CurrentMenuView = MainMenuView.HostWaiting;
                HostRoomCode = "Connecting...";
                HostStatus = "Connecting to server...";
                OnHostGameClicked?.Invoke();
            }
            if (joinHover && isClick)
            {
                CurrentMenuView = MainMenuView.JoinPrompt;
                _joinCodeInput = string.Empty;
                JoinStatus = string.Empty;
            }
            if (backHover && isClick) CurrentMenuView = MainMenuView.Main;
        }

        /// <summary>
        /// Renders the host waiting screen with an animated radar indicator and 5-digit room code display.
        /// </summary>
        private void DrawHostWaitingMenu(float offsetX)
        {
            int screenW = 1600; int screenH = 900;
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, (int)Math.Clamp(220 * (1f - Math.Abs(offsetX)/1600f), 0, 255)));
            Vector2 mousePos = Raylib.GetMousePosition();
            bool isClick = Raylib.IsMouseButtonReleased(MouseButton.Left);

            int titleW = Raylib.MeasureText("HOSTING ROOM", 60);
            Raylib.DrawText("HOSTING ROOM", (int)offsetX + screenW / 2 - titleW / 2, 160, 60, Color.RayWhite);

            // Large 5-Digit Room Code presentation banner
            int boxW = 500; int boxH = 110;
            int boxX = (screenW - boxW) / 2 + (int)offsetX;
            int boxY = 250;
            Rectangle codeBox = new Rectangle(boxX, boxY, boxW, boxH);
            Raylib.DrawRectangleRounded(codeBox, 0.3f, 16, new Color(25, 30, 40, 240));
            Raylib.DrawRectangleRoundedLines(codeBox, 0.3f, 16, Color.Gold);

            string codeTitle = "ROOM CODE:";
            int codeTitleW = Raylib.MeasureText(codeTitle, 22);
            Raylib.DrawText(codeTitle, boxX + (boxW - codeTitleW) / 2, boxY + 14, 22, Color.LightGray);

            int codeW = Raylib.MeasureText(HostRoomCode, 48);
            Raylib.DrawText(HostRoomCode, boxX + (boxW - codeW) / 2, boxY + 45, 48, Color.Gold);

            // Pulsing status text with red error coloring on failure
            float pulse = (float)Math.Sin(Raylib.GetTime() * 4.0) * 0.5f + 0.5f;
            Color statusColor = HostStatus.StartsWith("Failed", StringComparison.OrdinalIgnoreCase)
                ? new Color(255, 90, 90, 255)
                : new Color(200, 220, 255, (int)(150 + 105 * pulse));
            int statusW = Raylib.MeasureText(HostStatus, 26);
            Raylib.DrawText(HostStatus, (int)offsetX + screenW / 2 - statusW / 2, 400, 26, statusColor);

            int btnWidth = 260; int btnHeight = 65;
            int btnX = (screenW - btnWidth) / 2 + (int)offsetX;
            int btnY = 500;
            Rectangle cancelRect = new Rectangle(btnX, btnY, btnWidth, btnHeight);
            bool cancelHover = Raylib.CheckCollisionPointRec(mousePos, cancelRect);

            if (cancelHover && _lastHoveredId != "cancel") OnButtonHovered?.Invoke();
            _lastHoveredId = cancelHover ? "cancel" : "none";

            DrawModernButton(cancelRect, "Cancel", cancelHover);

            if (cancelHover && isClick)
            {
                OnCancelHostClicked?.Invoke();
                OnCancelLobbyClicked?.Invoke();
                CurrentMenuView = MainMenuView.OnlineLobby;
            }
        }

        /// <summary>
        /// Renders the join room prompt capturing keyboard digits for room pairing.
        /// </summary>
        private void DrawJoinPromptMenu(float offsetX)
        {
            int screenW = 1600; int screenH = 900;
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, (int)Math.Clamp(220 * (1f - Math.Abs(offsetX)/1600f), 0, 255)));
            Vector2 mousePos = Raylib.GetMousePosition();
            bool isClick = Raylib.IsMouseButtonReleased(MouseButton.Left);

            int titleW = Raylib.MeasureText("JOIN MATCH", 60);
            Raylib.DrawText("JOIN MATCH", (int)offsetX + screenW / 2 - titleW / 2, 160, 60, Color.RayWhite);

            string prompt = "Enter 5-digit room code:";
            int promptW = Raylib.MeasureText(prompt, 26);
            Raylib.DrawText(prompt, (int)offsetX + screenW / 2 - promptW / 2, 250, 26, Color.LightGray);

            // Input handling: digits only, maximum 5 characters
            int key = Raylib.GetCharPressed();
            while (key > 0)
            {
                if (char.IsDigit((char)key) && _joinCodeInput.Length < 5)
                {
                    _joinCodeInput += (char)key;
                }
                key = Raylib.GetCharPressed();
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && _joinCodeInput.Length > 0)
            {
                _joinCodeInput = _joinCodeInput.Substring(0, _joinCodeInput.Length - 1);
            }

            // Interactive input box
            int boxW = 340; int boxH = 80;
            int boxX = (screenW - boxW) / 2 + (int)offsetX;
            int boxY = 300;
            Rectangle inputBox = new Rectangle(boxX, boxY, boxW, boxH);
            Raylib.DrawRectangleRounded(inputBox, 0.3f, 16, new Color(30, 35, 45, 255));
            Raylib.DrawRectangleRoundedLines(inputBox, 0.3f, 16, Color.Gold);

            string displayText = string.IsNullOrEmpty(_joinCodeInput) ? "_____" : _joinCodeInput;
            Color textColor = string.IsNullOrEmpty(_joinCodeInput) ? Color.DarkGray : Color.Gold;
            int textW = Raylib.MeasureText(displayText, 46);
            Raylib.DrawText(displayText, boxX + (boxW - textW) / 2, boxY + (boxH - 46) / 2, 46, textColor);

            // Status or error message
            if (!string.IsNullOrEmpty(JoinStatus))
            {
                int statW = Raylib.MeasureText(JoinStatus, 22);
                Color statusColor = (JoinStatus.StartsWith("Connecting", StringComparison.OrdinalIgnoreCase) ||
                                     JoinStatus.StartsWith("Joined", StringComparison.OrdinalIgnoreCase) ||
                                     JoinStatus.StartsWith("Ready", StringComparison.OrdinalIgnoreCase))
                    ? Color.Gold
                    : (JoinStatus.StartsWith("Failed", StringComparison.OrdinalIgnoreCase) ||
                       JoinStatus.StartsWith("Code must", StringComparison.OrdinalIgnoreCase))
                        ? Color.Red
                        : Color.Gold;
                Raylib.DrawText(JoinStatus, (int)offsetX + screenW / 2 - statW / 2, 410, 22, statusColor);
            }

            // Connect & Back buttons
            int btnW = 220; int btnH = 65; int gap = 20;
            int btnStartX = (screenW - (btnW * 2 + gap)) / 2 + (int)offsetX;
            int btnY = 470;

            Rectangle connectRect = new Rectangle(btnStartX, btnY, btnW, btnH);
            Rectangle backRect = new Rectangle(btnStartX + btnW + gap, btnY, btnW, btnH);

            bool connectHover = Raylib.CheckCollisionPointRec(mousePos, connectRect);
            bool backHover = Raylib.CheckCollisionPointRec(mousePos, backRect);

            string currentHover = connectHover ? "connect" : (backHover ? "back" : "none");
            if (currentHover != "none" && currentHover != _lastHoveredId) OnButtonHovered?.Invoke();
            _lastHoveredId = currentHover;

            DrawModernButton(connectRect, "Connect", connectHover);
            DrawModernButton(backRect, "Back", backHover);

            if (Math.Abs(offsetX) < 10f)
            {
                if ((connectHover && isClick) || (Raylib.IsKeyPressed(KeyboardKey.Enter) && _joinCodeInput.Length == 5))
                {
                    if (_joinCodeInput.Length == 5)
                    {
                        JoinStatus = "Connecting...";
                        OnJoinGameClicked?.Invoke(_joinCodeInput);
                    }
                    else
                    {
                        JoinStatus = "Code must be 5 digits.";
                    }
                }

                if (backHover && isClick)
                {
                    OnCancelJoinClicked?.Invoke();
                    CurrentMenuView = MainMenuView.OnlineLobby;
                }
            }
        }
        
		private void DrawPauseMenu(float offsetX)
        {
            int screenW = 1600; int screenH = 900;
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, (int)Math.Clamp(210 * (1f - Math.Abs(offsetX)/1600f), 0, 255)));
            
            int titleW = Raylib.MeasureText("PAUSED", 80);
            Raylib.DrawText("PAUSED", (int)offsetX + screenW / 2 - titleW / 2, 180, 80, Color.RayWhite);

            int btnWidth = 340; int btnHeight = 65; int gap = 20;
            int startX = (int)offsetX + screenW / 2 - btnWidth / 2;

            Vector2 mousePos = Raylib.GetMousePosition();
            bool isClick = Raylib.IsMouseButtonReleased(MouseButton.Left);

            if (IsMultiplayerSession)
            {
                // In multiplayer: Continue, Back to Menu, Exit (Restart is completely omitted)
                int totalH = btnHeight * 3 + gap * 2;
                int startY = screenH / 2 - totalH / 2 + 50;

                Rectangle continueRect = new Rectangle(startX, startY, btnWidth, btnHeight);
                Rectangle menuRect = new Rectangle(startX, startY + btnHeight + gap, btnWidth, btnHeight);
                Rectangle exitRect = new Rectangle(startX, startY + (btnHeight + gap) * 2, btnWidth, btnHeight);

                bool continueHover = Raylib.CheckCollisionPointRec(mousePos, continueRect);
                bool menuHover = Raylib.CheckCollisionPointRec(mousePos, menuRect);
                bool exitHover = Raylib.CheckCollisionPointRec(mousePos, exitRect);

                string currentHover = continueHover ? "continue" : (menuHover ? "menu" : (exitHover ? "exit" : "none"));
                if (currentHover != "none" && currentHover != _lastHoveredId) OnButtonHovered?.Invoke();
                _lastHoveredId = currentHover;

                DrawModernButton(continueRect, "Continue", continueHover);
                DrawModernButton(menuRect, "Back to Menu", menuHover);
                DrawModernButton(exitRect, "Exit", exitHover);

                if (continueHover && isClick) OnContinueClicked?.Invoke();
                if (menuHover && isClick) OnGoToMenuClicked?.Invoke();
                if (exitHover && isClick) OnExitClicked?.Invoke();
            }
            else
            {
                // In singleplayer/training: Continue, Restart, Back to Menu, Exit
                int totalH = btnHeight * 4 + gap * 3;
                int startY = screenH / 2 - totalH / 2 + 50;

                Rectangle continueRect = new Rectangle(startX, startY, btnWidth, btnHeight);
                Rectangle restartRect = new Rectangle(startX, startY + btnHeight + gap, btnWidth, btnHeight);
                Rectangle menuRect = new Rectangle(startX, startY + (btnHeight + gap) * 2, btnWidth, btnHeight);
                Rectangle exitRect = new Rectangle(startX, startY + (btnHeight + gap) * 3, btnWidth, btnHeight);

                bool continueHover = Raylib.CheckCollisionPointRec(mousePos, continueRect);
                bool restartHover = Raylib.CheckCollisionPointRec(mousePos, restartRect);
                bool menuHover = Raylib.CheckCollisionPointRec(mousePos, menuRect);
                bool exitHover = Raylib.CheckCollisionPointRec(mousePos, exitRect);

                string currentHover = continueHover ? "continue" : (restartHover ? "restart" : (menuHover ? "menu" : (exitHover ? "exit" : "none")));
                if (currentHover != "none" && currentHover != _lastHoveredId) OnButtonHovered?.Invoke();
                _lastHoveredId = currentHover;

                DrawModernButton(continueRect, "Continue", continueHover);
                DrawModernButton(restartRect, "Restart", restartHover);
                DrawModernButton(menuRect, "Back to Menu", menuHover);
                DrawModernButton(exitRect, "Exit", exitHover);

                if (continueHover && isClick) OnContinueClicked?.Invoke();
                if (restartHover && isClick) OnRestartClicked?.Invoke();
                if (menuHover && isClick) OnGoToMenuClicked?.Invoke();
                if (exitHover && isClick) OnExitClicked?.Invoke();
            }
        }        
        private void DrawGameOverMenu(float offsetX)
        {
            int screenW = 1600; int screenH = 900;
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, (int)Math.Clamp(230 * (1f - Math.Abs(offsetX)/1600f), 0, 255)));

            string matchComplTxt = "Match Completed";
            int mcTitleW = Raylib.MeasureText(matchComplTxt, 40);
            Raylib.DrawText(matchComplTxt, (int)offsetX + screenW / 2 - mcTitleW / 2, 160, 40, Color.LightGray);
            
            string winnerTxt = (_sceneData?.Winner != null) ? _sceneData.Winner.Name + " Wins!" : "Draw!";
            int titleW = Raylib.MeasureText(winnerTxt, 90);
            Raylib.DrawText(winnerTxt, (int)offsetX + screenW / 2 - titleW / 2, 220, 90, Color.Gold);

            int btnWidth = 340; int btnHeight = 65; int gap = 20;
            int startX = (int)offsetX + screenW / 2 - btnWidth / 2;

            Vector2 mousePos = Raylib.GetMousePosition();
            bool isClick = Raylib.IsMouseButtonReleased(MouseButton.Left);

            if (IsMultiplayerSession)
            {
                // In multiplayer: Back to Menu, Exit (Play Again / Restart is omitted)
                int startY = screenH / 2 + 10;
                Rectangle menuRect = new Rectangle(startX, startY, btnWidth, btnHeight);
                Rectangle exitRect = new Rectangle(startX, startY + btnHeight + gap, btnWidth, btnHeight);

                bool menuHover = Raylib.CheckCollisionPointRec(mousePos, menuRect);
                bool exitHover = Raylib.CheckCollisionPointRec(mousePos, exitRect);
                string currentHover = menuHover ? "menu" : (exitHover ? "exit" : "none");
                if (currentHover != "none" && currentHover != _lastHoveredId) OnButtonHovered?.Invoke();
                _lastHoveredId = currentHover;

                DrawModernButton(menuRect, "Back to Menu", menuHover);
                DrawModernButton(exitRect, "Exit", exitHover);

                if (menuHover && isClick) OnGoToMenuClicked?.Invoke();
                if (exitHover && isClick) OnExitClicked?.Invoke();
            }
            else
            {
                // In singleplayer/training: Restart, Back to Menu, Exit
                int startY = screenH / 2 - 20;
                Rectangle restartRect = new Rectangle(startX, startY, btnWidth, btnHeight);
                Rectangle menuRect = new Rectangle(startX, startY + btnHeight + gap, btnWidth, btnHeight);
                Rectangle exitRect = new Rectangle(startX, startY + (btnHeight + gap) * 2, btnWidth, btnHeight);

                bool restartHover = Raylib.CheckCollisionPointRec(mousePos, restartRect);
                bool menuHover = Raylib.CheckCollisionPointRec(mousePos, menuRect);
                bool exitHover = Raylib.CheckCollisionPointRec(mousePos, exitRect);
                string currentHover = restartHover ? "restart" : (menuHover ? "menu" : (exitHover ? "exit" : "none"));
                if (currentHover != "none" && currentHover != _lastHoveredId) OnButtonHovered?.Invoke();
                _lastHoveredId = currentHover;

                DrawModernButton(restartRect, "Restart", restartHover);
                DrawModernButton(menuRect, "Back to Menu", menuHover);
                DrawModernButton(exitRect, "Exit", exitHover);

                if (restartHover && isClick) OnRestartClicked?.Invoke();
                if (menuHover && isClick) OnGoToMenuClicked?.Invoke();
                if (exitHover && isClick) OnExitClicked?.Invoke();
            }
        }
        
        // Internal loop helpers isolated below the scope definitions keeping DrawFrame clean.
        private ICircleBody? GetCueBall()
        {
            if (_sceneData?.Bodies == null) return null;
            return (ICircleBody?)System.Linq.Enumerable.FirstOrDefault(_sceneData.Bodies, b => b is ICircleBody cb && cb.BallType == BallType.Cue);
        }
        
        private void DrawBall(ICircleBody circleBody, Vector2 renderedPos, int alpha)
        {
            Color ballColor = Color.White;
            int n = circleBody.Number;
            
            if (circleBody.BallType == BallType.Cue) 
            {
                ballColor = new Color(255, 255, 255, alpha);
            }
            else if (circleBody.BallType == BallType.Black)
            {
                ballColor = new Color(20, 20, 20, alpha);
            }
            else
            {
                int colorIndex = n > 8 ? n - 8 : n;
                var baseColor = colorIndex switch 
                {
                    1 => new Color(255, 215, 0, 255),
                    2 => new Color(0, 0, 255, 255),
                    3 => new Color(255, 0, 0, 255),
                    4 => new Color(128, 0, 128, 255),
                    5 => new Color(255, 140, 0, 255),
                    6 => new Color(0, 128, 0, 255),
                    7 => new Color(128, 0, 0, 255),
                    _ => Color.White
                };
                ballColor = new Color(baseColor.R, baseColor.G, baseColor.B, alpha);
            }

            Color rayWhiteAlpha = new Color(245, 245, 245, alpha);
            float rotPhase = circleBody.RotationAngle;
            float rotPhaseDeg = rotPhase * (180f / MathF.PI);
            
            if (circleBody.BallType == BallType.Cue)
            {
                Raylib.DrawCircleV(renderedPos, circleBody.Radius, rayWhiteAlpha);
            }
            else if (circleBody.BallType == BallType.Solid || circleBody.BallType == BallType.Black)
            {
                Raylib.DrawCircleV(renderedPos, circleBody.Radius, ballColor);
                Raylib.DrawCircleV(renderedPos, circleBody.Radius * 0.55f, rayWhiteAlpha);
            }
            else if (circleBody.BallType == BallType.Striped)
            {
                Raylib.DrawCircleV(renderedPos, circleBody.Radius, rayWhiteAlpha);
                
                float cosA = MathF.Cos(rotPhase);
                float sinA = MathF.Sin(rotPhase);
                
                float stripeHalf = circleBody.Radius * 0.65f;
                for (float dy = -stripeHalf; dy <= stripeHalf; dy += 0.5f)
                {
                    float chordX = MathF.Sqrt(circleBody.Radius * circleBody.Radius - dy * dy);
                    Vector2 p1 = new Vector2(renderedPos.X + (-chordX) * cosA - dy * sinA, renderedPos.Y + (-chordX) * sinA + dy * cosA);
                    Vector2 p2 = new Vector2(renderedPos.X + chordX * cosA - dy * sinA, renderedPos.Y + chordX * sinA + dy * cosA);
                    Raylib.DrawLineEx(p1, p2, 1f, ballColor);
                }
                Raylib.DrawCircleV(renderedPos, circleBody.Radius * 0.55f, rayWhiteAlpha);
            }
            
            if (circleBody.BallType != BallType.Cue)
            {
                string numStr = n.ToString();
                Font font = Raylib.GetFontDefault();
                float fontSize = 9f;
                float spacing = 1.0f;
                Vector2 textSize = Raylib.MeasureTextEx(font, numStr, fontSize, spacing);
                Vector2 origin = new Vector2(textSize.X / 2f, textSize.Y / 2f);
                
                Raylib.DrawTextPro(font, numStr, renderedPos, origin, rotPhaseDeg, fontSize, spacing, new Color(0, 0, 0, alpha));
            }
        }
    }
}
