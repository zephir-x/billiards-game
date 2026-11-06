# Billiards 2D - Architecture & Game Design

## Core Principles
The project is a 2D billiards simulation built in .NET, focusing on precise physics modeling and clean architecture. The game strictly adheres to SOLID principles, ensuring complete separation between the mathematical physics engine and the rendering loop.

### SOLID Principles Application
- **Single Responsibility Principle (SRP):** Each module (`Physics`, `Core`, `Renderer`, `App`) has only one reason to change. The physics engine is completely unaware of rendering or input methods; the renderer only displays the state without mutating game rules.
- **Dependency Inversion Principle (DIP):** High-level modules communicate with underlying mechanisms exclusively through interfaces (e.g., `IPhysicsEngine`, `IInputProvider`, `IRenderer`), never through concrete implementations.
- **Separation of Concerns:** The physical state and game logic (Model) are entirely independent of the platform drawing the application (View).

## Game Mechanics & Context
- **Turn-based Gameplay:** Player 1 and Player 2 alternate turns. The UI explicitly communicates the active player's turn.
- **Local Hotseat & Training:** The game can be played locally on a single machine as a training/hotseat mode, or over a network in 1v1 multiplayer.
- **Main Menu Navigation:** Features `Play Online` (Multiplayer lobby & room code), `Training` (local hotseat play), and `Exit`.
- **Aiming (Cue Control):** The cue is kinematically linked to the cursor. The cue tip maintains a fixed distance from the cue ball's center, while the cue butt tracks the mouse cursor's directional vector.
- **Shot Mechanics (Overheat System):** The player holds the Left Mouse Button (LMB) to charge shot power, which is visualized by a dedicated Power Bar located outside the billiard table. Releasing the button before reaching the maximum executes the shot. However, if the power reaches the critical maximum (1.0), the cue becomes **Overheated/Blocked**, and a shot can no longer be executed. The power bar will then slowly drain back to 0.0. Only when it completely resets to zero does the cue become available for aiming and shooting again.
- **Turn Timer:** An active 30-second countdown for the current aiming turn. If the countdown reaches zero before shot release, an automatic foul penalty is levied (`FOUL: Time Out`) and turn control transfers.

## Tech Stack
- **Language:** C# (.NET 9)
- **Framework (hidden behind facades):** Raylib-cs for hardware bindings, rendering, and input tracking.
- **Testing:** xUnit and Moq for deterministic physics and game state verification without graphical context (100% Core & Physics domain coverage).
- **Networking:** Zero-dependency .NET standard sockets (`System.Net.Sockets`) with non-blocking async TCP message frames (Authoritative Host model).

---

## Development Roadmap (Execution Phases)

- **Phase 1: Physics Foundations & Scaffolding [Completed]**
  - [x] Solution and project scaffolding (`BilliardsGame.sln`, `BilliardsGame.Physics.csproj`, `BilliardsGame.Physics.Tests.csproj`).
  - [x] Core interfaces: `IPhysicsEngine`, `IPhysicsBody` (Extracted to `BilliardsGame.Interfaces.csproj`).
  - [x] Basic entities: `Ball` (dynamic circle body), `Cushion` (static boundary segment).
  - [x] Unit test suite for initial entity instantiation and impulse response.

- **Phase 2: Physics Implementation & Verification [Completed]**
  - [x] Numerical integration (Backward Euler) and table friction/drag decay.
  - [x] Collision detection & resolution: Ball-to-Ball (circle vs circle) and Ball-to-Cushion (circle vs segment) via polimorphic traits `ICircleBody`, `ISegmentBody`.
  - [x] Bilateral elastic impulse resolution ($j \cdot \vec{n}$).
  - [x] Motion sleep threshold (`AreAllBodiesAtRest`).
  - [x] Comprehensive unit tests for physics determinism and impulse conservation (Tested on generic `IEnumerable`).

- **Phase 3: Game Core & Input [Completed]**
  - [x] Module `/Core` setup (`BilliardsGame.Core.csproj`).
  - [x] Entities & Contracts: `IPlayer`, `Player`.
  - [x] State machine: `IGameManager` (`Menu`, `PlayerTurn`, `ChargingShot`, `SimulatingBalls`, `GameOver`).
  - [x] Turn coordinator (switching turns after all balls stop moving, foul handling stub).
  - [x] Aiming & power charging controller: `ICueController` & `CueController`.
  - [x] Hardware input abstraction: `IInputProvider`.
  - [x] Core logic unit tests testing the state machine, turns, and cue interactions without a rendering context (using Moq).

- **Phase 4: Rendering & Presentation [Completed]**
  - [x] Module `/App` setup (`BilliardsGame.App.csproj`).
  - [x] Rendering DIP contracts (`IRenderer`, `ISceneParameters`).
  - [x] Frame drawing facade and Alpha-blending interpolation for mapping physics updates across varying hardware refresh rates.
  - [x] Object drawing: Minimal UI overlay, dynamically targeted aiming mechanics.
  - [x] Integration adapter implemented via Raylib-cs mappings and fixed x64 emulation configurations.

- **Phase 5: Physics Scale & Game Feel [Completed]**
  - [x] Fine-tune visual and physical scales: Adjust ball radii, cue rendering thickness, and window projections to maintain readable proportions on high-res displays.
  - [x] Recalibrate shot power: Drastically increase the fundamental `ExecuteShot` impulse multiplier or tweak entity mass so balls correctly traverse the table with momentum.
  - [x] Table proportions: Overhaul `Program.cs` instantiation to form a clean, perfectly aligned inner rectangular playfield without messy segment overlaps.

- **Phase 6: Core Mechanics (UI Pockets & Overheat) [Completed]**
  - [x] Engine integration: Introduce `IPocket` / `Hole` geometric entities explicitly to `PhysicsEngine` via dependency injection list.
  - [x] Spatial mapping: Place 6 standard pockets systematically around the cushion vertices.
  - [x] Sinking logic: If a ball's center converges with a pocket radius, immediately remove it from rendering and physics resolution (`_bodies`).
  - [x] Overheat mechanic: Modifying `CueController` so that hitting `Power > 1.0` blocks shooting and triggers a slow cooldown drain back to `0.0`.
  - [x] UI Power Bar: Render a dynamic Power Bar (filling up and changing color) completely outside the physical table boundaries.

- **Phase 7: Win States, Logic Bugs & Menus [Completed]**
  - [x] **Urgent Bug Fixes - White Ball Sinking (Scratch/Foul):** Currently, sinking the white ball permanently despawns it, soft-locking the `GameManager` in `SimulatingBalls` or crashing aiming calculations. The engine must track the white ball state and respawn it upon turn end if sank (Foul penalty logic).
  - [x] **Urgent Bug Fixes - Black Ball Sinking (Win Condition):** Sinking the object ball (black ball) currently does nothing but despawn it. The Engine must broadcast a win event, ending the session and jumping the state to `GameOver`.
  - [x] Main Menu Flow: Add a starting splash screen capturing user input for `Play` or `Exit` buttons explicitly blocking game loop start.
  - [x] In-Game Menu overlay: Override `ESC` key handling (preventing the default Raylib abrupt application exit) to raise an in-game pause overlay screen with clickable buttons: `Continue`, `Restart`, and `Exit`.
  - [x] HUD Implementation: Build a Top-Center GUI mapping the current scores, balls left, and formatted `Turn: Player X` tag.

- **Phase 8: Table UI Polish, Visual Feedback & Collision Hitboxes [Completed]**
  - [x] **Hitbox/Visual Synchronization:** Recalibrated rendering alignment explicitly using custom quad geometry mapping over 16 independent jaw buffers preventing physics tunneling while mimicking accurate pool boundaries.
  - [x] **Table Aesthetics:** Thickened rails, inner bevels, custom cutouts per pocket.
  - [x] **Ghost Guide (Aim Line):** Ethereal dashed rendering for cue prediction modeling vectors against physics overlaps.
  - [x] **UI/HUD Overhaul:** Segmented dedicated scoring zones (Player 1 left, Player 2 right) explicitly awaiting Phase 9 Ball entities.

- **Phase 9: 8-Ball Game Mode & Tiers (Full Rack Setup) [Completed]**
  - [x] **Instancing Full Rack:** Foot spot algorithmic deployment covering true-to-life 8-Ball structure generation overlapping arrays preventing zero-lapses.
  - [x] **Entity Differentiation (Solid vs Striped):** `BallType` enums introduced mapped to complex rendering properties distinguishing base colors & stripped patterns. 
  - [x] **Complex Turn Logic (Basic):** Sinking an assigned type continues player turn. Unassigned table assignments dictating suites based on primary sink events.

- **Phase 10: Advanced Adjudication Engine & Testing Framework [Completed]**
  - [x] **A. Physics Snapshot Metadata Hooking:** The physics engine (PhysicsEngine) must track and output metadata exactly per stroke (StrokeData context block during SimulatingBalls):
    - FirstBallHit: Which specific ball Id the white ball collided with first.
    - RailsHit: How many rails were touched *after* the initial ball-to-ball contact.
    - SunkBalls: A chronological queue of pocketed ball Ids resolving simultaneous sinks.
  - [x] **B. Foul Validation System (IRulesEngine):** Decouple dirty rule checking out of GameManager creating a dedicated isolated RuleValidator. It evaluates stroke data against the table pool state to determine legal strokes:
    - *Open Table Fault:* Hitting the 8-Ball primary when the table assignment is open.
    - *Wrong Tier Fault:* Hitting an opposing stripe/solid explicitly first.
    - *No Rail Fault:* Soft defensive taps preventing game flow (if nothing sunk, ball must eventually hit a rail after strike).
    - *Scratch & Sink Fault:* Sinking the white ball concurrently whilst sinking a correct object ball (should invalidate the continuation logic immediately).
  - [x] **C. Ball-in-Hand Interactive Placement:** Overwrite hardcoded white ball spawn mechanisms with an interactive state GameState.BallInHand. After a foul, the player can dynamically ghost-place the white ball with mouse cursor clicking anywhere on the board validated safe (Vector2 overlaps via safe collision check).
  - [x] **D. Automated Theory Testing Framework (The "How-To-Test" Solution):** To prevent manual QA-attrition reproducing a billion billiard outcomes, BilliardsGame.Core.Tests MUST introduce data-driven xUnit [Theory] definitions parsing a robust set of edge case matrix states mimicking instantaneous shot outcomes. Mock the PhysicsEngine.StrokeData property to return rigged histories and enforce RuleValidator behaviors to assert Turn and Foul conditions rapidly.

- **Phase 11: Gameplay Polish & Visual UX [Completed]**
  - [x] **Ball Rotation (2D Illusion):** Stored a visual RotationAngle inside ICircleBody. Increment this angle relative to the velocity vector magnitude and delta time ($ \Delta \theta = (||v|| / r) \cdot dt $). Numbers and stripe lines rotate perfectly without a 3D engine.
  - [x] **Dynamic Power Bar:** Interpolated the power bar color from Blue (0.0) to Orange (0.99) dynamically as it charges. Matches pure Red (1.0) during Overheat cooldown penalty.
  - [x] **Aim Lock Constraint:** Modified GameManager and CueController so the aim trajectory locks onto the target point during the charge sequence, avoiding mis-clicks slipping the aim line.
  - [x] **Overheat Penalty Tuning:** Adjusted cooldown multiplier retaining punishment factor but escaping excessive wait lengths.
  - [x] **Sink Animation & Rendering Orders:** Converted sunk balls into IsGhost mode crushing its momentum by 98%. Interpolated their fading over 1.5s via Alpha blending. Placed cushion rendering natively covering ball depths eliminating table clipping.
  - [x] **Match Point Validation & Ball In Hand Rendering:** Validated Ball placement mapping red cursor indicators on overlaps. At 7 pockets filled, golden UI triggers activating visually distinct text warnings.
  - [x] **Foul Wobble Tooltips:** Replaced aggressive end screens with "MATCH COMPLETE". Rendered soft wobbling "FOUL" contextual hints natively bounded between UI power bar and table geometry.

- **Phase 12: Audio-Visual Overhaul & Release Delivery [Completed]**
  - [x] **Menu Sliding Capabilities:** Revamp the strict UI block transitions to utilize viewport easing matrices (sliding menus left/right into play space smoothly). Bounded delta time (max 0.033s) to prevent erratic slides. Overhauled button styles and background dimming gradients.\n  - [x] **Audio Implementation:** Utilize Raylib-cs audio device module. Added OnNotificationEvent hooked explicitly for rendering feedback SFX (fouls, assignments). Eased volume scales preventing audio peaking.
  - [x] **Codebase Standardization:** Implement broad Roslyn fixes unifying XML <summary> formatting, dropping stale Polish comments explicitly favoring robust structural C# documentation per interface. Format spaces and generic layouts globally.
  - [x] **Repository & Release Distribution:** Formalize a README.md containing topics, installation parameters, architecture mapping. Structure a dotnet publish -c Release -r win-x64 --self-contained workflow delivering a bundled .exe zip archive with procedural in-memory icon rendering.

- **Phase 13: Turn Timer & Online Multiplayer Integration [Completed]**
  - [x] **A. Global Turn Countdown Timer & Expanded Foul Subsystem:**
    - Introduce `TurnTimer` (configurable, default 30.0s) inside `IGameManager` active strictly during aiming states (`PlayerTurn`, `ChargingShot`, `BallInHand`).
    - Reset timer to 30.0s upon every valid turn handoff.
    - Decoupled tick propagation in `UpdateLogic(float deltaTime)` with state broadcast to `ISceneParameters`.
    - **Timeout Penalty System:** If `RemainingTurnTime <= 0f`:
      - Broadcast semantic notification: `SetNotification("FOUL: Time Out", NotificationType.Foul, 3.5f);`
      - Trigger audio feedback via `OnNotificationEvent?.Invoke(NotificationType.Foul)`.
      - Invoke `OnScratchFoul?.Invoke()` to grant the opponent `GameState.BallInHand`.
      - Transfer turn execution cleanly to the opposing player.
    - **UI & Presentation:** Circular or linear HUD gauge in `RaylibRenderer` displaying remaining seconds with urgency color shifts (Normal gold -> pulsing red at `<5s`) accompanied by warning tick audio pulses.
    - **100% Test Coverage Requirement:**
      - Unit tests in `BilliardsGame.Core.Tests/GameManagerTests.cs` validating:
        1. Timer decrements accurately with `deltaTime`.
        2. Timer resets to 30s when turn switches.
        3. Exceeding 30s triggers `FOUL: Time Out` notification, fires `OnScratchFoul`, switches player, and sets `GameState.BallInHand`.
        4. Overheat state during timeout correctly resets cue charge.
  - [x] **B. Menu Flow & Mode Narrative Overhaul:**
    - Shift previous local match flow to a dedicated **`Training`** button (Local Hotseat / Solo practice).
    - Introduce primary **`Play Online`** option leading to a sleek lobby overlay:
      - **Host Session:** Starts a local listening socket, generates/displays a 5-digit room code or IP address, and waits for a remote peer.
      - **Join Session:** Displays an input modal to enter the peer IP/Code and establishes connection.
  - [x] **C. Dedicated Network Module (`BilliardsGame.Network`):**
    - Zero-dependency, lightweight .NET sockets implementation (`TcpListener` / `TcpClient` or non-blocking async network streams).
    - Compact binary/JSON packet protocol:
      - `HandshakeMsg`: Player identity and role assignment (Host = Player 1, Client = Player 2).
      - `AimSyncMsg`: Real-time streaming of cue rotation angle and charge percentage from the active player.
      - `ShotExecutionMsg`: Precise shot impulse vector applied at the exact frame.
      - `PhysicsSyncMsg`: Authoritative snapshot streaming (30-60Hz) of active ball positions `(Id, X, Y, Vx, Vy)` emitted by Host during `SimulatingBalls`.
      - `BallInHandMsg`: Cursor position updates during white ball placement.
      - `TurnStateMsg`: Turn transitions, fouls (including Timeouts), and game over synchronization.
  - [x] **D. Authoritative Host Architecture Integration:**
    - The **Host** maintains absolute authority over `PhysicsEngine` and `RuleValidator`.
    - The **Client** acts as an interpolated proxy: overrides its local physics during simulation by lerping to received authoritative ball snapshots, preventing floating-point desynchronization.
    - Implement a `NetworkInputProvider` conforming to `IInputProvider`: when it is the remote opponent's turn, local mouse input is bypassed and replaced with inbound `AimSyncMsg` coordinates.
  - [x] **E. Reconnection & Disconnect Fault Handling:**
    - Graceful connection drop detection: if a peer disconnects, prompt an informational UI overlay ("Opponent disconnected") with options to forfeit or return to the main menu without thread crash or application freeze.

- **Phase 14: Multiplayer Synchronization Hardening & Game Experience Polish [To Do]**
  - [x] **Step 1: Network Synchronization & Turn State Lockstep Resolution (Critical Priority)**
    - **Problem Identified:** Severe desynchronization occurs after the first shot. The remote opponent continues rendering the cue stick as if aiming/charging even after the shot has released; ball movements are jerky/unsynced; turn states, clocks, and active player indicators diverge completely.
    - **A. Shot Execution Propagation & Cue State Clearing:**
      - When `ExecuteShot` is triggered by the active player, the client/host must immediately transmit `ShotExecutionPacket` AND forcefully reset local/remote cue rendering state (`CueController.ResetCharge()`).
      - In `NetworkInputProvider`, when receiving `ShotExecutionPacket` or when state transitions to `SimulatingBalls`, ensure `IsLeftMouseDown` and `WasLeftMouseReleased` are cleanly pulsed and reset to prevent "stuck aim/charge" ghost inputs.
    - **B. Authoritative State Machine & Turn Synchronization:**
      - The **Host** is the sole authoritative referee and physics arbiter. The Host must emit `TurnStatePacket` upon ANY state transition (`PlayerTurn`, `ChargingShot`, `SimulatingBalls`, `BallInHand`, `GameOver`, score update, foul notification, active player handoff).
      - The **Client** must strictly synchronize its `GameManager.CurrentState`, `ActivePlayer`, `Scores`, and `RemainingTurnTime` from the authoritative Host `TurnStatePacket`, rather than running an independent asynchronous turn timer/state machine that drifts.
    - **C. Telemetry Lock & Turn Timer Alignment:**
      - Synchronize `RemainingTurnTime` from Host to Client at tick boundaries to keep clocks strictly identical across screens.
      - Clamp client physics simulation strictly to Host snapshots during `SimulatingBalls` to eliminate local collision divergence.

  - [x] **Step 2: Match Flow, Menu Ergonomics & UI Refinements**
    - **A. Pause & Game Over Screen Adaptations:**
      - **Multiplayer Mode:** Remove the `Restart` button completely from both Pause (`ESC`) and Game Over screens (a live 1v1 online session cannot be unilaterally restarted).
      - **Both Modes:** Add a prominent, styled `Go Back to Menu` button (cleanly disconnecting active network sockets if in multiplayer, resetting session state, and returning smoothly to `MainMenuView.Main`).
    - **B. Turn Timer Duration Tuning:**
      - Shorten the global turn timer countdown from `30.0s` to **`15.0s`** across `GameManager` and unit tests to ensure dynamic, fast-paced pool action.
    - **C. Player Role Clarification at Match Start:**
      - Display a clear, prominent announcement / banner at the beginning of an online match (e.g. `You are Player 1 (Host)` vs `You are Player 2 (Client)`).
      - Highlight the local player's identity on the scoreboard panel throughout the entire match.
    - **D. Join Room Status Feedback:**
      - Fix the join prompt status label color: change from alarming red to neutral/positive green (`Color.Lime`) or golden-yellow (`Color.Gold`) when waiting/attempting connection with a valid 5-digit code.
    - **E. Clean Forfeit & Disconnect Win Condition:**
      - If an opponent disconnects mid-match, do not leave the remaining player stranded with a foul notification. Immediately trigger `GameState.GameOver` declaring the remaining connected player the victor by forfeit (`Opponent Left - You Win!`) with a direct button to return to the Main Menu.

  - [x] **Step 3: Standalone Portable Packaging (.NET Runtime-Independent Release)**
    - **Problem Identified:** Users running the game on bare Windows machines without the .NET 9 SDK/Runtime installed receive an execution failure ("A fatal error occurred. The required library hostfxr.dll could not be found").
    - **Solution:** Package a truly self-contained, native bundle incorporating all required CoreCLR/hostfxr native binaries.
    - **Execution Parameters:**
      - Use `dotnet publish App/BilliardsGame.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true`
      - Include asset bundling script that guarantees `Audio/` and all Raylib native c-libs (`raylib.dll`) are structured in the distribution archive for 100% "out-of-the-box" execution on any clean Windows machine.

---

## Module Specifications & Interface Contracts

### 1. Interfaces & Physics Engine (`/Interfaces`, `/Physics`)
The core of the simulation. Handles deterministic mathematical resolution of positions and velocities.

#### Fixed Time Step
The engine abstracts completely from the rendering framerate (FPS). The simulation steps forward by a constant `FixedDeltaTime` to prevent physics inconsistencies across different hardware speeds.

```csharp
public interface IPhysicsEngine
{
    // Calculates positions, movement, and collisions after a fixed time step (e.g., 16ms)
    void Step(float fixedDeltaTime); 
    void AddBody(IPhysicsBody body);
    void RemoveBody(IPhysicsBody body);
    IReadOnlyCollection<IPhysicsBody> GetBodies();
    bool AreAllBodiesAtRest(float sleepVelocityThreshold = 0.001f);
}
```

#### Physics Body Contracts
Defines shapes participating in collisions. Cushions/walls are static bodies; balls are dynamic circles with restitution.

```csharp
public interface IPhysicsBody
{
    int Id { get; }
    Vector2 Position { get; set; }
    Vector2 PreviousPosition { get; set; } // Critical for smooth rendering interpolation
    Vector2 Velocity { get; set; }
    float Mass { get; }
    float Restitution { get; } // Collision elasticity [0.0, 1.0]
    bool IsStatic { get; }     // True for static geometry
    
    void ApplyImpulse(Vector2 impulse);
}
```

#### Mathematics - Friction & Integration
In every time step, moving bodies decelerate due to drag:

Static drag application:
$$v_{new} = v_{current} \cdot (1 - \mu \cdot dt)$$
*(Where $\mu$ is the constant friction coefficient of the table cloth).*

Position update (Backward Euler Integration):
$$p_{new} = p_{current} + v_{new} \cdot dt$$

#### Mathematics - Elastic Collisions
The resolution phase generates a bilateral impulse force if bodies overlap:

1. Normal vector $\vec{n}$:
$$\vec{n} = \frac{\vec{p}_A - \vec{p}_B}{\|\vec{p}_A - \vec{p}_B\|}$$

2. Relative velocity $\vec{v}_{rel}$:
$$\vec{v}_{rel} = \vec{v}_A - \vec{v}_B$$

3. Speed of penetration $v_n$:
$$v_n = \vec{v}_{rel} \cdot \vec{n}$$
*(If $v_n > 0$, the bodies are already separating; impulse is aborted).*

4. Impulse scalar $j$ based on restitution $e$:
$$j = \frac{-(1 + e) \cdot v_n}{\frac{1}{m_A} + \frac{1}{m_B}}$$

5. Final impulse vector $\vec{J}$:
$$\vec{J} = j \cdot \vec{n}$$

6. Applying forces to velocities based on mass:
$$\vec{v}_A' = \vec{v}_A + \frac{\vec{J}}{m_A}$$
$$\vec{v}_B' = \vec{v}_B - \frac{\vec{J}}{m_B}$$

---

### 2. Game Logic (`/Core`)
Coordinates game rules and handles platform input via adapter patterns.

#### Player Contract
```csharp
public interface IPlayer
{
    int Id { get; }
    string Name { get; }
}
```

#### State Machine
```csharp
public enum GameState { Menu, PlayerTurn, ChargingShot, SimulatingBalls, GameOver }

public interface IGameManager
{
    GameState CurrentState { get; }
    IPlayer ActivePlayer { get; }
    float RemainingTurnTime { get; } // Phase 13 Timer contract
    void UpdateLogic(float deltaTime); // Propagates logic updates
    void StartGame();
    void EndTurn();
}
```

#### Cue Controller
Manages aiming and shot power mechanics:

```csharp
public interface ICueController
{
    float Power { get; } // Scale from 0.0 to 1.0 (Critical max)
    Vector2 CueDirection { get; }
    
    // Sets cue alignment based on mouse and cue ball position
    void UpdateAim(Vector2 cueBallPosition, Vector2 mousePosition);
    
    // Increases power (resets if exceeding 1.0)
    void ChargeShot(float deltaTime);
    void ResetCharge();
    
    // Applies final shot force to the cue ball
    void ExecuteShot(IPhysicsBody cueBall);
}

// Abstracts physical input devices from graphics libraries
public interface IInputProvider
{
    Vector2 MouseWorldPosition { get; set; }
    bool IsLeftMouseDown { get; set; }
    bool WasLeftMouseReleased { get; set; }
}
```

---

### 3. Rendering Engine (`/App`)
Receives a read-only state for visualization. Never mutates position vectors to avoid race conditions and coupling.

#### Presentation Contract
```csharp
public interface IRenderer
{
    void Initialize(ISceneParameters sceneData);
    // Alpha is the time remainder for inter-frame smoothness [0, 1)
    void DrawFrame(float interpolationAlpha); 
}

public interface ISceneParameters
{
    GameState CurrentState { get; } // For rendering UI/Menu context
    IPlayer CurrentTurnPlayer { get; }
    IReadOnlyCollection<IPhysicsBody> Bodies { get; }
    ICueController CueInfo { get; }
    float TurnTimer { get; } // Exposes remaining turn seconds for the HUD
}
```

#### Video Interpolation
While `/Physics` calculates at a rigid $dt = 0.016s$, a 144Hz monitor draws frames every $0.007s$. To prevent visual stuttering, we apply Alpha-blending interpolation:

$$R_{Pos} = (Pos_{previous} \cdot (1 - \alpha)) + (Pos_{current} \cdot \alpha)$$

- **$Pos_{previous}$:** Physics state from the previous step.
- **$Pos_{current}$:** The newly calculated physics state.
- **$\alpha$:** The time fraction elapsed since the last physics step.
