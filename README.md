<div align="center">

# 🎱 2D Billiards - Zephir Edition

[![.NET 9](https://img.shields.io/badge/.NET_9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23_13-239120?style=for-the-badge&logo=c-sharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Raylib](https://img.shields.io/badge/Raylib_cs-000000?style=for-the-badge&logo=c&logoColor=white)](https://github.com/ChrisDill/Raylib-cs)
[![Architecture](https://img.shields.io/badge/Pattern-Clean_Architecture_%7C_DDD-FF7139?style=for-the-badge)](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
[![Multiplayer](https://img.shields.io/badge/Multiplayer-1v1_Online_Relay-00C7B7?style=for-the-badge&logo=fastapi&logoColor=white)](https://github.com/zephir-x/billards-game)
[![Docker](https://img.shields.io/badge/Docker-GHCR_Alpine_Image-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://ghcr.io/zephir-x/billards-game-relay)
[![Testing](https://img.shields.io/badge/xUnit-100%25_Domain_Coverage-512BD4?style=for-the-badge)](https://xunit.net/)

*A highly optimized, custom-built 2D physics billiards engine written entirely from scratch in C# 13. Engineered with strict Domain-Driven Design (DDD) principles, blazingly fast Raylib graphics, and seamless online 1v1 multiplayer powered by a dedicated high-throughput Blind Relay Server.*

</div>

---

<details>
<summary>📖 Table of Contents</summary>

- [About the Project](#-about-the-project)
- [For Gamers (How to Play)](#-for-gamers-how-to-play)
- [Multiplayer & Online Architecture](#-multiplayer--online-architecture)
- [Key Features](#-key-features)
- [Architectural Marvels (Tech Deep Dive)](#-architectural-marvels-tech-deep-dive)
- [Algorithm Data Flow & Engine Loop](#-algorithm-data-flow--engine-loop)
- [Technology Stack](#-technology-stack)
- [Project Structure](#-project-structure)
- [Relay Server Deployment (Docker & Mikrus)](#-relay-server-deployment-docker--mikrus)
- [Getting Started (Local Development)](#-getting-started-local-development)
- [Author / Contact](#-author--contact)

</details>

---

## 💡 About the Project

**2D Billiards - Zephir Edition** is a pool simulator showcasing how to build a proprietary, deterministic physics engine from the ground up without relying on heavy third-party frameworks like Box2D or Unity. 

It implements official 8-Ball rules, dynamic kinetic force transfers, accurate ball-to-cushion rebounding, turn adjudication timeouts, and robust online multiplayer strictly decoupled from the presentation layer using **Clean Architecture** and **Domain-Driven Design (DDD)**. The visual presentation and spatial audio are powered directly by native C-bindings via Raylib-cs for minimum latency.

---

<div align="center">
  <img src="Assets/demo_gameplay.gif" alt="Gameplay Demo" style="border-radius: 10px; box-shadow: 0 4px 8px rgba(0,0,0,0.5); width: 80%;"/>
  <p><i>🎱 Accurate momentum transfer, real-time kinetic visual mapping, and online synchronization.</i></p>
</div>

---

## 🎮 For Gamers (How to Play)

You don't need any programming knowledge or external runtimes to play the game!

### Quick Start
1. **Download:** Go to the [Releases](../../releases) tab of this repository.
2. **Extract:** Download the latest `2D_Billiards_Game.zip` and extract its contents anywhere on your PC.
3. **Play:** Double-click `BilliardsGame.exe` and you are ready to play!

### Game Modes
* **Play Online (1v1 Multiplayer):** 
  * **Host Match:** Generates a unique 5-digit room code (e.g. `48192`) through our dedicated relay server. Share this code with a friend!
  * **Join Match:** Enter your friend's 5-digit room code and jump straight into the match with real-time aim and physics synchronization.
* **Training (Hotseat Mode):** Play offline on a single screen to practice trick shots, bank shots, and break angles.

### Controls & Rules
* **Aiming:** Move your cursor around the cue ball. An intelligent aim-assist dynamically visualizes trajectory guides proportional to your strike power.
* **Shooting:** Click and hold the **Left Mouse Button** to pull back and charge your cue stick. Release to strike!
* **Global Turn Countdown Timer:** Each player has strictly **30.0 seconds** per turn. An animated HUD badge flashes red when under 5 seconds. If the timer reaches zero, a **Time Out Foul** is awarded and your opponent receives **Ball in Hand**.
* **Ball In Hand:** Following any foul (scratch, timeout, or illegal hit), you can pick up the white cue ball and place it anywhere on the table.
* **8-Ball Rules:** Standard official rules apply. Claim your assigned group (Solids or Stripes), pocket all balls of your group, and pot the 8-Ball Black cleanly into a pocket to win.

---

## 🌐 Multiplayer & Online Architecture

Online play is engineered around an **Authoritative Host + Blind Relay Server** architecture, ensuring low latency, zero NAT-traversal friction, and ironclad stability:

```mermaid
sequenceDiagram
    autonumber
    actor P1 as Player 1 (Host)
    participant Relay as Blind Relay Server (Mikrus VPS)
    actor P2 as Player 2 (Client)

    Note over P1,Relay: Room Creation Phase
    P1->>Relay: RelayControl [CreateRoom]
    Relay-->>P1: RelayControl [RoomCreated: 5-digit Code]

    Note over P2,Relay: Room Joining Phase
    P2->>Relay: RelayControl [JoinRoom: Code]
    Relay-->>P1: RelayControl [MatchReady]
    Relay-->>P2: RelayControl [MatchReady]

    Note over P1,P2: Blind Relay Pipe (Transparent Byte Forwarding)
    P1->>Relay: HandshakePacket (Assign Player 2)
    Relay->>P2: HandshakePacket
    P2->>Relay: AimSyncPacket (Remote Aim Telemetry)
    Relay->>P1: AimSyncPacket
    P1->>Relay: PhysicsSyncPacket (60Hz Authoritative Coordinates)
    Relay->>P2: PhysicsSyncPacket
```

### Key Network Design Pillars:
* **Dedicated High-Throughput Blind Relay:** The relay server (`Server/BilliardsGame.RelayServer`) acts as an in-memory pipeline. Once sockets are paired, bytes are piped transparently without JSON serialization overhead, minimizing CPU usage and latency.
* **Authoritative Physics Host:** Host (Player 1) executes the deterministic 480Hz physics simulation and streams 60Hz coordinate snapshots (`PhysicsSyncPacket`) to Player 2 to completely eliminate synchronization drift.
* **Thread-Safe Binary Framing:** All packets are encoded with Big-Endian length headers and dispatched via synchronized socket writes (`_sendLock`) to prevent stream interleaving.
* **Self-Healing TCP Buffering:** Inbound streams dynamically expand their memory capacity up to 1 MB to seamlessly handle fragmented packet frames without stream desynchronization.

---

## 🚀 Key Features

| Feature | Description |
| :--- | :--- |
| **Custom Kinematics Engine (480Hz)** | Built purely in C# without Box2D. Features sub-step micro-stepping (`dt = 1/480s`) to prevent tunneling, accurate restitution matrices, and realistic momentum transfers. |
| **Online 1v1 Peer-to-Peer Relay** | Play over the internet using quick 5-digit room codes routed through our lightweight production relay server with zero port-forwarding requirements for players. |
| **30-Second Turn Timer Subsystem** | Dynamic domain-driven countdown timer enforcing competitive pace with turn adjudication fouls and pulsating UI alerts. |
| **Strict 8-Ball Referee State Machine** | Intelligent rule validation evaluating first-contact legality, rail contacts, pocket drops, scratch fouls, and winner determination. |
| **Pure Raylib Hardware Integration** | Procedural vector drawing for balls, felt, cushions, and dynamic icons (generated purely in RAM and bound to Windows Shell). Interpolated 144Hz+ rendering. |
| **100% Domain Test Coverage** | 62 comprehensive unit tests covering physics math, cue controllers, referee decision branches, and network packet framing. |

---

## 🏛️ Architectural Marvels (Tech Deep Dive)

This codebase serves as an enterprise blueprint for architecting scalable, easily testable game engines:

* **🛡️ Clean Architecture & DDD:** Strict boundary segregation isolating Domain logic (`Core/`, `Physics/`, `Network/`) from Infrastructure (`App/`). The UI consumes read-only data contracts (`ISceneParameters`) and dispatches commands via loose delegates.
* **⚡ Production Relay Microservice:** `BilliardsGame.RelayServer` runs as an ultra-lightweight Alpine container (~50MB) with strict resource limits (`cpus: 0.5`, `memory: 64M`) supporting up to 100 concurrent rooms with automatic 3-minute lobby idle timeouts.
* **🧠 Deterministic Context Evaluator:** `RuleValidator.cs` functions as a pure functional pipeline receiving immutable telemetry and yielding exact match-state transitions.
* **⚙️ Hardware-Agnostic Timing:** A decoupled spinlock accumulator guarantees fixed physics ticks (`1/480s`) regardless of display frame-rates, paired with scalar linear interpolation (`alpha = accumulator / dt`) for buttery smooth 144Hz+ monitors.

---

## 🔄 Algorithm Data Flow & Engine Loop

```mermaid
graph TD
  subgraph Client Hardware Layer
    RawInput[Raylib Mouse / Keyboard]
    Renderer[RaylibRenderer : DrawFrame]
    Audio[AudioManager : miniaudio]
  end

  subgraph Network Multiplexing Layer
    NetService[NetworkService : TCP Socket]
    NetInput[NetworkInputProvider : Remote/Local Proxy]
  end

  subgraph Game Core Layer
    GM[GameManager : Turn State Machine]
    Timer[Turn Countdown Timer : 30s]
    Referee[RuleValidator : Foul Adjudicator]
    Cue[CueController : Strike Impulse]
  end

  subgraph Pure Physics Domain
    Engine[PhysicsEngine : Locked 480Hz Step]
    Resolver[Collision & Cushion Elastic Resolver]
  end

  RawInput -->|Hardware Clicks| NetInput
  NetService <-->|Sync Telemetry / Snapshots| NetInput
  NetInput -->|Mapped World Coordinates| Cue
  Cue -->|Impulse Vector| GM
  GM -->|Apply Impulse| Engine
  Engine -->|Micro-Ticks| Resolver
  Resolver -->|Kinematic Bodies| GM
  GM <-->|Countdown / Foul Penalty| Timer
  GM -->|Telemetry Packet| Referee
  Referee -->|Turn Outcome| GM
  GM -->|Authoritative Snapshots| NetService
  GM -->|Scene Parameters| Renderer
  GM -->|Sound Events| Audio
```

---

## 💻 Technology Stack

| Layer | Technologies & Tools |
| :--- | :--- |
| **Language & Runtime** | C# 13, .NET 9.0 SDK, MSBuild |
| **Graphics & Audio** | Raylib-cs (OpenGL bindings, miniaudio native backend) |
| **Network Protocol** | Custom TCP Length-Prefixed Framing, System.Text.Json Contracts |
| **Server & Container** | .NET 9 Alpine Linux, Docker Compose, GitHub Actions (GHCR) |
| **Architecture** | Clean Architecture, Domain-Driven Design (DDD), Composition Root DI |
| **Automated Testing** | xUnit, Moq (62 Automated Domain & Network Tests) |
| **Distribution** | Windows Native Self-Contained / Single-File Publishing |

---

## 📁 Project Structure

<details>
<summary><strong>Click to expand the Architecture Tree</strong></summary>

```text
billards-game/
├── .github/workflows/
│   └── docker-publish.yml             # Builds & publishes Relay Server to GHCR
├── App/                               # Presentation & Hardware Composition Root
│   ├── RaylibRenderer.cs              # Full multi-view lobby, HUD timer & table rendering
│   ├── RaylibInputProvider.cs         # Raw mouse/keyboard hardware adapter
│   ├── SceneParameters.cs             # UI presentation data contract adapter
│   ├── AudioManager.cs                # High-fidelity miniaudio BGM/SFX pipeline
│   └── Program.cs                     # Composition Root wiring all subsystems
├── Core/                              # Match Rules & Domain State Machine
│   ├── GameManager.cs                 # Match supervisor, turn states & 30s timer
│   ├── CueController.cs               # Aim vector calculations & shot power charging
│   └── RuleValidator.cs               # Pure functional 8-ball referee evaluator
├── Physics/                           # High-Performance Kinematics Simulator
│   ├── PhysicsEngine.cs               # Math loop evaluating 480Hz micro-steps
│   ├── Ball.cs / Cushion.cs           # Kinematic circular bodies & static segment cushions
│   └── Pocket.cs                      # Table hole boundary detection
├── Network/                           # Online Multiplayer Subsystem
│   ├── Abstractions/                  # INetworkService contracts
│   ├── Services/                      # NetworkService (TCP framing, socket threads)
│   ├── Protocol/                      # Data contracts (Aim, PhysicsSync, Handshake, etc.)
│   └── Input/                         # NetworkInputProvider (Remote vs. Local multiplexer)
├── Server/
│   └── BilliardsGame.RelayServer/     # Standalone lightweight 5-digit room pairing demon
├── Interfaces/                        # Shared models, enumerations and contracts
├── Assets/                            # Demo GIFs, features illustrations & audio assets
└── tests/                             # Comprehensive xUnit automated test suites
    ├── BilliardsGame.Core.Tests       # 28 tests: turn transitions, timers & referee
    ├── BilliardsGame.Physics.Tests    # 17 tests: momentum transfer & cushion rebound
    └── BilliardsGame.Network.Tests    # 17 tests: TCP framing, serialization & buffers
```

</details>

---

## 🐳 Relay Server Deployment (Docker & Mikrus)

The production relay server runs as a standalone Docker container. It requires zero database dependencies and is fully automated via GitHub Actions (`ghcr.io/zephir-x/billards-game-relay:latest`).

### Running via Docker Compose
Create a `docker-compose.yml` file on your server (e.g. VPS / Mikrus):

```yaml
services:
  billiards-relay:
    image: ghcr.io/zephir-x/billards-game-relay:latest
    container_name: billiards_relay
    restart: unless-stopped
    ports:
      - "20117:7777"
    environment:
      - RELAY_PORT=7777
      - DOTNET_gcServer=0
    deploy:
      resources:
        limits:
          cpus: "0.50"
          memory: 64M
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
```

Start the service:
```bash
docker compose pull
docker compose up -d
```

---

## 🛠️ Getting Started (Local Development)

### 1. Clone the repository:
```bash
git clone https://github.com/zephir-x/billards-game.git
cd billards-game
```

### 2. Build the solution (.NET 9 required)
```bash
dotnet build BilliardsGame.sln
```

### 3. Run the Game Engine
```bash
dotnet run --project App/BilliardsGame.App.csproj
```

### 4. Run the Full Test Suite
```bash
dotnet test
```

### 5. Build Standalone Release Package (.EXE)
To create a portable release package ready for distribution:
```powershell
dotnet publish App/BilliardsGame.App.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o ./dist
```
Copy the `Audio/` directory into `./dist` and you have a self-contained game ready for any Windows PC!

---

## 👨‍💻 Author / Contact

Engineered by **Kacper**.

[![LinkedIn](https://img.shields.io/badge/LinkedIn-0077B5?style=for-the-badge&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/kacper-gumulak-dev)
[![GitHub](https://img.shields.io/badge/GitHub-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/zephir-x)
