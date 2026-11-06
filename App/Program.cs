using BilliardsGame.Interfaces;
using BilliardsGame.Interfaces.Enums;
using BilliardsGame.Interfaces.Models;
using System;
using System.Numerics;
using System.Linq;
using BilliardsGame.App;
using BilliardsGame.Core;
using BilliardsGame.Physics;
using BilliardsGame.Network.Abstractions;
using BilliardsGame.Network.Services;
using BilliardsGame.Network.Input;
using BilliardsGame.Network.Protocol;
using Raylib_cs;

namespace BilliardsGame.App
{
    /// <summary>
    /// Acts as the primary Composition Root explicitly defining dependency injection wiring and controlling the main execution sequence thread limiters.
    /// </summary>
    class Program
    {
        static void Main()
        {
            // Stage 1: Absolute graphical engine initialization configuring platform integrations.
            Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint);
            Raylib.SetTraceLogLevel(TraceLogLevel.None);
            Raylib.SetTraceLogLevel(TraceLogLevel.None);
            Raylib.InitWindow(1600, 900, "2D Billiards Game");
            Image icon = Raylib.GenImageColor(128, 128, Color.Blank);
            Raylib.ImageDrawCircle(ref icon, 64, 64, 60, new Color(20, 20, 20, 255));
            Raylib.ImageDrawCircle(ref icon, 64, 64, 30, Color.RayWhite);
            Raylib.ImageDrawText(ref icon, "8", 54, 45, 40, Color.Black);
            Raylib.SetWindowIcon(icon);
            Raylib.UnloadImage(icon);
            Raylib.SetTargetFPS(144); 
            Raylib.SetExitKey(KeyboardKey.Null);

            // Reusable lambda orchestrating temporary geometry eliminating Windows standard white-screen tearing buffers.
            Action<string> DrawLoadingFrame = (text) =>
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(5, 5, 12, 255));
                Raylib.DrawRectangleGradientV(0, 0, 1600, 900, new Color(5, 5, 25, 255), new Color(40, 5, 5, 255));
                
                int fSize = 36;
                int wText = Raylib.MeasureText(text, fSize);
                Raylib.DrawText(text, 1600 / 2 - wText / 2, 900 / 2 - 80, fSize, Color.RayWhite);
                
                float t = (float)Raylib.GetTime() * 360f;
                
                // Emulate animated buffer sequence calculating temporal offsets.
                Raylib.DrawRing(new Vector2(1600 / 2, 900 / 2 + 20), 20f, 26f, t, t + 100f, 32, Color.Gold);
                Raylib.DrawRing(new Vector2(1600 / 2, 900 / 2 + 20), 20f, 26f, t + 180f, t + 280f, 32, Color.Gold);
                Raylib.EndDrawing();
            };

            // Commit a hard UI flush allocating the drawing context correctly into hardware viewports before audio mounts lock the thread.
            DrawLoadingFrame("Initializing Engine");

            // Setup underlying C-language bindings mapping OS sound channels.
            var audioManager = new AudioManager();
            audioManager.Initialize();
            
            // Build linear execution pipeline translating blocking I/O calls (HDD->RAM) into a continuous sequence updating the UI frame buffer intermittently.
            Action[] loadSteps = new Action[]
            {
                () => audioManager.LoadBGM("menu", "Audio/BGM/bgm_menu.mp3"),
                () => audioManager.LoadBGM("gameplay", "Audio/BGM/bgm_gameplay.mp3"),
                () => audioManager.LoadSFX("collision", "Audio/SFX/sfx_collision.mp3"),
                () => audioManager.LoadSFX("cushion", "Audio/SFX/sfx_cushion.mp3"),
                () => audioManager.LoadSFX("pocket", "Audio/SFX/sfx_pocket.mp3"),
                () => audioManager.LoadSFX("cue_hit", "Audio/SFX/sfx_cue_hit.mp3"),
                () => audioManager.LoadSFX("game_over", "Audio/SFX/sfx_game_over.mp3"),
                () => audioManager.LoadSFX("ui_click", "Audio/SFX/sfx_ui_click.mp3"),
                () => audioManager.LoadSFX("foul", "Audio/SFX/sfx_faul.mp3"),
                () => audioManager.LoadSFX("announcement", "Audio/SFX/sfx_announcement.mp3")
            };

            for (int i = 0; i < loadSteps.Length; i++)
            {
                DrawLoadingFrame("Loading Resources");
                loadSteps[i].Invoke(); // Lock specific thread step safely isolated over rendering ticks.
            }

            // Bind explicitly the domain simulation core establishing absolute rule-set parameters.
            audioManager.PlayBGM("menu", 0.6f);
            var physicsEngine = new PhysicsEngine(1.2f, 8.0f);
            
            // Direct structural couplings hooking decoupled simulation collisions straight into concrete hardware Audio execution limits.
            physicsEngine.OnCollisionOccurred += audioManager.PlayCollision;
            physicsEngine.OnCushionHit += audioManager.PlayCushionHit;
            physicsEngine.OnBallPocketed += audioManager.PlayPocketed;
            
            // Construct mapping defining strict table interaction geometries.
            physicsEngine.AddBody(new Cushion(10, new Vector2(230, 200), new Vector2(470, 200), 0.9f));
            physicsEngine.AddBody(new Cushion(11, new Vector2(530, 200), new Vector2(770, 200), 0.9f));
            physicsEngine.AddBody(new Cushion(12, new Vector2(800, 230), new Vector2(800, 570), 0.9f));
            physicsEngine.AddBody(new Cushion(13, new Vector2(770, 600), new Vector2(530, 600), 0.9f));
            physicsEngine.AddBody(new Cushion(14, new Vector2(470, 600), new Vector2(230, 600), 0.9f));
            physicsEngine.AddBody(new Cushion(15, new Vector2(200, 570), new Vector2(200, 230), 0.9f));

            physicsEngine.AddBody(new Cushion(20, new Vector2(200, 230), new Vector2(180, 210), 0.9f));
            physicsEngine.AddBody(new Cushion(21, new Vector2(210, 180), new Vector2(230, 200), 0.9f));
            physicsEngine.AddBody(new Cushion(22, new Vector2(470, 200), new Vector2(480, 175), 0.9f));
            physicsEngine.AddBody(new Cushion(23, new Vector2(520, 175), new Vector2(530, 200), 0.9f));
            physicsEngine.AddBody(new Cushion(24, new Vector2(770, 200), new Vector2(790, 180), 0.9f));
            physicsEngine.AddBody(new Cushion(25, new Vector2(820, 210), new Vector2(800, 230), 0.9f));
            physicsEngine.AddBody(new Cushion(26, new Vector2(800, 570), new Vector2(820, 590), 0.9f));
            physicsEngine.AddBody(new Cushion(27, new Vector2(790, 620), new Vector2(770, 600), 0.9f));
            physicsEngine.AddBody(new Cushion(28, new Vector2(530, 600), new Vector2(520, 625), 0.9f));
            physicsEngine.AddBody(new Cushion(29, new Vector2(480, 625), new Vector2(470, 600), 0.9f));
            physicsEngine.AddBody(new Cushion(30, new Vector2(230, 600), new Vector2(210, 620), 0.9f));
            physicsEngine.AddBody(new Cushion(31, new Vector2(180, 590), new Vector2(200, 570), 0.9f));

            // Define absolute overlapping void destinations verifying rule conditions.
            physicsEngine.AddPocket(new Hole(new Vector2(190, 190), 24f));
            physicsEngine.AddPocket(new Hole(new Vector2(500, 180), 24f));
            physicsEngine.AddPocket(new Hole(new Vector2(810, 190), 24f));
            physicsEngine.AddPocket(new Hole(new Vector2(190, 610), 24f));
            physicsEngine.AddPocket(new Hole(new Vector2(500, 620), 24f));
            physicsEngine.AddPocket(new Hole(new Vector2(810, 610), 24f));

            // Encapsulated setup sequence forcing correct triangular coordinate alignment upon every Match Start or Reset invocation.
            Action setupBalls = () => 
            {
                var bodies = physicsEngine.GetBodies().ToList();
                foreach (var b in bodies)
                {
                    if (b is BilliardsGame.Interfaces.ICircleBody)
                        physicsEngine.RemoveBody(b);
                }

                float r = 10f;
                float startX = 650f;
                float startY = 400f;
                float dx = r * MathF.Sqrt(3);
                float dy = r;

                var numbers = new int[] 
                {
                    1,
                    9, 2,
                    10, 8, 3,
                    11, 4, 12, 5,
                    6, 14, 7, 15, 13
                };

                int idx = 0;
                for (int col = 0; col < 5; col++)
                {
                    float x = startX + col * dx;
                    float yStart = startY - col * dy;

                    for (int row = 0; row <= col; row++)
                    {
                        float y = yStart + row * 2 * dy;
                        int number = numbers[idx++];
                        BallType type;
                        if (number == 8) type = BallType.Black;
                        else if (number >= 1 && number <= 7) type = BallType.Solid;
                        else type = BallType.Striped;

                        var ball = new Ball(number, new Vector2(x, y), 0.15f, r, 0.8f, type, number);
                        physicsEngine.AddBody(ball);
                    }
                }

                var whiteBall = new Ball(0, new Vector2(350, 400), 0.15f, r, 0.8f, BallType.Cue, 0);
                physicsEngine.AddBody(whiteBall);
            };

            setupBalls();

            // Dependency Injection sequence explicitly registering subsystems onto the Domain supervisor.
            var rawInputProvider = new RaylibInputProvider();
            var networkService = new NetworkService();
            var cueController = new CueController();
            
            // Relay endpoint resolution: defaults to production Mikrus Relay Server (zack117.mikrus.xyz:20117),
            // while preserving override capability via environment variables for local testing.
            string relayHost = Environment.GetEnvironmentVariable("RELAY_HOST") ?? "zack117.mikrus.xyz";
            int relayPort = int.TryParse(Environment.GetEnvironmentVariable("RELAY_PORT"), out int p) ? p : 20117;

            // Forward declaration to resolve active player ID provider for input multiplexing.
            GameManager? gameManager = null;
            var networkInputProvider = new NetworkInputProvider(
                rawInputProvider,
                networkService,
                () => gameManager?.ActivePlayer?.Id ?? 1,
                () => cueController.ResetCharge());

            gameManager = new GameManager(physicsEngine, cueController, networkInputProvider);
            
            cueController.OnCueHit += audioManager.PlayCueHit;

            // Map subjective logical states directly to Audio feedback.
            gameManager.OnNotificationEvent += (type) => 
            {
                if (type == BilliardsGame.Interfaces.Enums.NotificationType.Foul) audioManager.PlaySFX("foul", 0.08f);
                else audioManager.PlaySFX("announcement", 0.08f);
            };
            
            // Inject new body dynamically resolving Ball-in-Hand penalties.
            gameManager.OnPlaceCueBall += (pos) => 
            {
                var whiteBall = new Ball(0, pos, 0.15f, 10f, 0.8f, BallType.Cue, 0);
                physicsEngine.AddBody(whiteBall);
                audioManager.PlaySFX("collision", 0.7f);

                if (networkService.IsConnected && networkInputProvider.IsMyTurn)
                {
                    networkService.SendPacket(NetworkPacketType.BallInHand, new BallInHandPacket(pos.X, pos.Y, true));
                }
            };

            gameManager.OnGameOver += () => 
            { 
                audioManager.StopBGM("gameplay"); 
                audioManager.PlayGameOver(); 
                audioManager.PlayBGM("menu", 0.6f); 
            };

            // Authoritative Host: broadcast authoritative state upon domain transitions
            Action broadcastTurnState = () =>
            {
                if (networkService.IsConnected && networkService.IsHost)
                {
                    var turnPacket = new TurnStatePacket(
                        gameManager.CurrentState,
                        gameManager.ActivePlayer.Id,
                        gameManager.Player1.Score,
                        gameManager.Player2.Score,
                        gameManager.Player1.AssignedType,
                        gameManager.Player2.AssignedType,
                        gameManager.RemainingTurnTime,
                        gameManager.NotificationMessage,
                        gameManager.NotificationType,
                        gameManager.Winner?.Id);
                    networkService.SendPacket(NetworkPacketType.TurnState, turnPacket);
                }
            };

            gameManager.OnStateTransition += () =>
            {
                cueController.ResetCharge();
                networkInputProvider.ClearRemoteInputStates();
                broadcastTurnState();
            };
            
            // Presentational View linking exposing purely UI dependencies and masking physical constraints.
            var sceneData = new SceneParameters(gameManager, physicsEngine, cueController);
            var renderer = new RaylibRenderer();
            renderer.Initialize(sceneData);
            
            renderer.OnButtonHovered = () => audioManager.PlayUIHover();
            
            // Link input system to dynamic UI camera space mappings resolving click vectors accurately regardless of resolution.
            networkInputProvider.SetCoordinateMapper((screenPos) => Raylib.GetScreenToWorld2D(screenPos, renderer.MainCamera));

            bool isPaused = false;
            bool shouldExit = false;

            // Helper resetting game state into local or online gameplay session.
            Action startGameSession = () =>
            {
                audioManager.PlayUIClick(); 
                audioManager.StopBGM("menu"); 
                audioManager.PlayBGM("gameplay", 0.4f);
                rawInputProvider.ConsumeClickForUI();
                isPaused = false;
                setupBalls();
                gameManager.StartGame();
                renderer.CurrentMenuView = MainMenuView.Main;
            };

            Action returnToMainMenu = () =>
            {
                audioManager.PlayUIClick();
                audioManager.StopBGM("gameplay");
                audioManager.PlayBGM("menu", 0.6f);
                rawInputProvider.ConsumeClickForUI();
                isPaused = false;
                if (networkService.IsConnected)
                {
                    networkService.Disconnect("Player returned to menu");
                }
                renderer.IsMultiplayerSession = false;
                renderer.LocalPlayerId = 0;
                renderer.HostRoomCode = string.Empty;
                renderer.HostStatus = "Connecting to Relay...";
                renderer.JoinStatus = string.Empty;
                renderer.CurrentMenuView = MainMenuView.Main;
                gameManager.ResetToMenu();
            };

            // Wire UI Delegates executing domain changes bypassing tightly coupled loop architectures.
            renderer.OnPlayClicked = () => 
            { 
                if (networkService.IsConnected) networkService.Disconnect("Switched to Local Play");
                renderer.IsMultiplayerSession = false;
                renderer.LocalPlayerId = 0;
                startGameSession();
            };

            renderer.OnTrainingClicked = () => 
            { 
                if (networkService.IsConnected) networkService.Disconnect("Switched to Training");
                renderer.IsMultiplayerSession = false;
                renderer.LocalPlayerId = 0;
                startGameSession();
            };

            renderer.OnHostGameClicked = () =>
            {
                audioManager.PlayUIClick();
                renderer.HostStatus = $"Connecting to {relayHost}:{relayPort}...";
                _ = networkService.HostViaRelayAsync(relayHost, relayPort);
            };

            renderer.OnJoinGameClicked = (code) =>
            {
                audioManager.PlayUIClick();
                renderer.JoinStatus = $"Connecting to room {code}...";
                _ = networkService.JoinViaRelayAsync(relayHost, relayPort, code);
            };

            renderer.OnCancelLobbyClicked = () =>
            {
                audioManager.PlayUIClick();
                networkService.Disconnect("Host cancelled waiting");
                renderer.HostRoomCode = string.Empty;
                renderer.CurrentMenuView = MainMenuView.OnlineLobby;
            };

            renderer.OnExitClicked = () => 
            { 
                audioManager.PlayUIClick();
                networkService.Disconnect("Client exited");
                shouldExit = true;
            };

            renderer.OnContinueClicked = () => 
            { 
                audioManager.PlayUIClick(); 
                audioManager.StopBGM("menu"); 
                audioManager.PlayBGM("gameplay");
                rawInputProvider.ConsumeClickForUI();
                isPaused = false;
            };

            renderer.OnGoToMenuClicked = () =>
            {
                returnToMainMenu();
            };

            renderer.OnRestartClicked = () => 
            { 
                if (renderer.IsMultiplayerSession)
                {
                    // In multiplayer, unilateral restart is not permitted
                    return;
                }
                audioManager.PlayUIClick(); 
                audioManager.StopBGM("menu"); 
                audioManager.PlayBGM("gameplay", 0.4f);
                rawInputProvider.ConsumeClickForUI();
                isPaused = false;
                setupBalls();
                gameManager.StartGame();
            };

            // Wire Network Service Callbacks into Renderer and Game lifecycle.
            networkService.OnRoomCreated += (roomCode) =>
            {
                renderer.HostRoomCode = roomCode;
                renderer.HostStatus = "Room created! Share code with Player 2.";
            };

            networkService.OnConnected += (assignedPlayerId) =>
            {
                renderer.IsMultiplayerSession = true;
                renderer.LocalPlayerId = assignedPlayerId;
                audioManager.PlaySFX("announcement", 0.6f);
                startGameSession();
                string roleName = networkService.IsHost ? "Player 1 (Host)" : "Player 2 (Client)";
                gameManager.SetNotification($"MATCH STARTED! You are {roleName}", NotificationType.Info, 5.0f);
            };

            networkService.OnConnectionFailed += (reason) =>
            {
                renderer.JoinStatus = $"Failed: {reason}";
                renderer.HostStatus = $"Failed: {reason}";
                audioManager.PlaySFX("foul", 0.3f);
            };

            networkService.OnDisconnected += (reason) =>
            {
                if (renderer.IsMultiplayerSession &&
                    gameManager.CurrentState != GameState.Menu &&
                    gameManager.CurrentState != GameState.GameOver)
                {
                    // Opponent disconnected mid-match: award instant forfeit victory to local player!
                    int localId = networkService.LocalPlayerId;
                    gameManager.ConcludeMatchByForfeit(localId, "Opponent Left - You Win by Forfeit!");
                    audioManager.PlaySFX("announcement", 0.8f);
                }
                else
                {
                    gameManager.SetNotification($"Opponent disconnected ({reason})", NotificationType.Foul, 5.0f);
                    audioManager.PlaySFX("foul", 0.4f);
                }
            };

            // Client turn state synchronization: adopt authoritative host referee state.
            networkService.OnTurnStateReceived += (turnPacket) =>
            {
                if (!networkService.IsHost && turnPacket != null)
                {
                    gameManager.ApplyAuthoritativeState(
                        turnPacket.State,
                        turnPacket.ActivePlayerId,
                        turnPacket.Player1Score,
                        turnPacket.Player2Score,
                        turnPacket.Player1AssignedType,
                        turnPacket.Player2AssignedType,
                        turnPacket.RemainingTurnTime,
                        turnPacket.NotificationMessage,
                        turnPacket.NotificationType,
                        turnPacket.WinnerId);
                    cueController.ResetCharge();
                    networkInputProvider.ClearRemoteInputStates();
                }
            };

            // Client physics synchronization: override local ball bodies with authoritative host snapshots.
            networkService.OnPhysicsSyncReceived += (syncPacket) =>
            {
                if (!networkService.IsHost && syncPacket?.Balls != null)
                {
                    var bodies = physicsEngine.GetBodies().OfType<ICircleBody>().ToDictionary(b => b.Id);
                    foreach (var snapshot in syncPacket.Balls)
                    {
                        if (bodies.TryGetValue(snapshot.Id, out var body))
                        {
                            body.Position = new Vector2(snapshot.X, snapshot.Y);
                            body.Velocity = new Vector2(snapshot.Vx, snapshot.Vy);
                        }
                    }
                }
            };

            // Main Execution Frame Configuration mappings:
            float accumulator = 0f;
            const float dt = 1f / 480f;
            float physicsSyncTimer = 0f;
            const float physicsSyncInterval = 1f / 60f; // 60Hz physics stream
            float turnSyncTimer = 0f;

            // Application Spinlock Sequence
            while (!Raylib.WindowShouldClose() && !shouldExit)
            {
                rawInputProvider.Update();
                networkService.Update();

                if (gameManager.CurrentState != GameState.Menu && 
                    gameManager.CurrentState != GameState.GameOver)
                {
                    if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                    {
                        isPaused = !isPaused;
                        if (isPaused) { audioManager.StopBGM("gameplay"); audioManager.PlayBGM("menu"); }
                        else { audioManager.StopBGM("menu"); audioManager.PlayBGM("gameplay"); }
                    }
                }
                else
                {
                    isPaused = false;
                }

                // Protect systems against freeze spikes disrupting calculations.
                float frameTime = Raylib.GetFrameTime();
                if (frameTime > 0.1f) frameTime = 0.1f;
                
                renderer.IsPaused = isPaused;

                if (!isPaused)
                {
                    // Update network input proxy to stream telemetry or consume remote aim.
                    networkInputProvider.Update(frameTime, cueController.CueDirection, cueController.Power, (cueController.Power > 0f));

                    accumulator += frameTime;
                    
                    // Trigger abstract domain processes resolving turn rules based solely on delta scales.
                    gameManager.UpdateLogic(frameTime);

                    // Execute deterministically strict physics sub-steps protecting collision resolution depths regardless of display lags.
                    while (accumulator >= dt)
                    {
                        physicsEngine.Step(dt);
                        accumulator -= dt;
                    }

                    // Authoritative Host: stream continuous physics state during SimulatingBalls.
                    if (networkService.IsConnected && networkService.IsHost)
                    {
                        if (gameManager.CurrentState == GameState.SimulatingBalls)
                        {
                            physicsSyncTimer += frameTime;
                            if (physicsSyncTimer >= physicsSyncInterval)
                            {
                                physicsSyncTimer = 0f;
                                var snapshots = physicsEngine.GetBodies()
                                    .OfType<ICircleBody>()
                                    .Select(b => new BallSnapshot(b.Id, b.Position.X, b.Position.Y, b.Velocity.X, b.Velocity.Y))
                                    .ToList();
                                var syncPacket = new PhysicsSyncPacket(snapshots, physicsEngine.AreAllBodiesAtRest());
                                networkService.SendPacket(NetworkPacketType.PhysicsSync, syncPacket);
                            }
                        }
                        else
                        {
                            // Authoritative Host: periodically refresh turn state and remaining timer on client.
                            turnSyncTimer += frameTime;
                            if (turnSyncTimer >= 0.5f)
                            {
                                turnSyncTimer = 0f;
                                broadcastTurnState();
                            }
                        }
                    }
                }

                audioManager.Update();
                
                // Expose visual accumulation blending physics limits gracefully against uncapped native monitor FPS.
                float alpha = isPaused ? 0f : (accumulator / dt);
                renderer.DrawFrame(alpha);
            }

            // Cleanup cycle unbinding C-pointers safely matching the OS shutdown sequences.
            networkService.Dispose();
            audioManager.Deinitialize();
            Raylib.CloseWindow();
        }
    }
}
