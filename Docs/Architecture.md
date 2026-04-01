# VoidRogues – System Architecture

## Overview

VoidRogues is a top-down 2D twin-stick shooter roguelike with online co-op powered by
**Photon Fusion 2** in **Host/Client** topology.  The architecture is designed around
three principles:

1. **Struct-driven networking** – Hundreds of enemies and props are networked as compact
   structs in a single `NetworkBehaviour`, avoiding the overhead of one `NetworkObject`
   per entity.
2. **Separation of simulation and presentation** – All game-state lives in Fusion's tick
   simulation; visual GameObjects are purely presentation driven by that state.
3. **Scene-based game flow** – Ship (lobby/customization) and Mission are separate Unity
   scenes; a lightweight `GameManager` singleton coordinates transitions.

---

## High-Level Diagram

```
┌─────────────────────────────────────────────────────┐
│                    NetworkRunner                    │
│            (Fusion Host / Client session)           │
└───────┬─────────────────────────────────┬───────────┘
        │ spawns                          │ spawns
        ▼                                 ▼
┌───────────────┐                ┌─────────────────────┐
│  PlayerObject  │ (1 per player) │  ManagerHub (scene) │
│  NetworkObject │                │  NetworkBehaviour   │
│  ─────────── │                └──────┬──────────────┘
│  PlayerCtrl   │                      │ contains refs to
│  PlayerShooter│                ┌─────▼──────────────────────────┐
└───────────────┘                │  EnemyManager   (1 instance)   │
                                 │  PropsManager   (1 instance)   │
                                 │  ProjectileManager (1 instance)│
                                 └────────────────────────────────┘
```

---

## Assemblies & Folder Structure

```
Assets/
  Scenes/
    Bootstrap.unity        ← index 0 – connection/loader
    Ship.unity             ← index 1 – persistent hub
    Mission.unity          ← index 2 – template mission scene
  Prefabs/
    Network/
      NetworkRunner.prefab
    Player/
      PlayerCharacter.prefab
    Enemies/
      EnemyVisual.prefab   ← presentation only, NOT NetworkObject
    Props/
      BarrelVisual.prefab  ← presentation only
  Scripts/
    Network/               → VoidRogues.Network.asmdef
      NetworkBootstrap.cs
      NetworkEvents.cs
    Player/                → VoidRogues.Player.asmdef
      PlayerController.cs
      PlayerShooter.cs
      PlayerNetworkData.cs
    Enemies/               → VoidRogues.Enemies.asmdef
      EnemyManager.cs
      EnemyState.cs        ← struct
      EnemyAI.cs
    Props/                 → VoidRogues.Props.asmdef
      PropsManager.cs
      PropState.cs         ← struct
    Projectiles/           → VoidRogues.Projectiles.asmdef
      ProjectileManager.cs
      ProjectileState.cs   ← struct
    GameFlow/              → VoidRogues.GameFlow.asmdef
      GameManager.cs
      ShipManager.cs
      MissionManager.cs
    UI/                    → VoidRogues.UI.asmdef
      ShipUI.cs
      HUD.cs
  Settings/
    VoidRoguesInputActions.inputactions
  Resources/
    PhotonAppSettings.asset
```

---

## Core Systems

### 1  Network Bootstrap

`NetworkBootstrap` sits in the Bootstrap scene and starts the `NetworkRunner`.

- **Editor**: always starts as Host for fast iteration.
- **Build**: shows a minimal connect menu; first player hosts, subsequent players
  connect as clients.
- After the session is running it loads the Ship scene additively.

### 2  Player

Each connected player owns exactly **one** `NetworkObject` (the player character).

| Component | Role |
|-----------|------|
| `NetworkRigidbody2D` | Position/rotation synchronized by Fusion |
| `PlayerController` | Reads Fusion `NetworkInput`, moves `Rigidbody2D` |
| `PlayerShooter` | Converts aim input into `FireRequest` sent to `ProjectileManager` |
| `PlayerNetworkData` | `[Networked]` struct: health, ammo, score |

**Input flow (Fusion):**
```
Local machine          →  INetworkRunnerCallbacks.OnInput()
                              ↓ fills NetworkInputData struct
NetworkRunner sends input → Host simulation
                              ↓ PlayerController.FixedUpdateNetwork()
                                  reads Runner.GetInput<NetworkInputData>()
```

### 3  EnemyManager

A single `NetworkBehaviour` with a fixed-size `NetworkArray<EnemyState>` (capacity 512).

- **Host** runs `EnemyAI` logic each tick, writes updated positions/states into the array.
- **Clients** read the array changes via `ChangeDetector` and update `EnemyVisual`
  presentation GameObjects.
- Spawn/death is tracked by `EnemyState.IsAlive` flag (bool in struct) so no
  `NetworkObject` pooling is needed.
- Lag compensation: enemy positions are tick-stamped; `LagCompensatedHit` is used for
  hit-scan weapons.

```
NetworkArray<EnemyState>[512]
  EnemyState {
    NetworkBool  IsAlive;
    NetworkBool  IsActive;
    Vector2      Position;
    Vector2      Velocity;
    byte         TypeIndex;     // maps to EnemyDefinition SO
    short        Health;
    byte         AnimState;
  }
```

### 4  PropsManager

Mirrors `EnemyManager` but for destructible environment objects.

```
NetworkArray<PropState>[256]
  PropState {
    NetworkBool  IsAlive;
    Vector2      Position;
    byte         TypeIndex;
    short        Health;
    NetworkBool  Exploding;    // triggers explosion VFX on clients
  }
```

- On **Health ≤ 0** the host sets `Exploding = true`; clients detect the change via
  `ChangeDetector` and play the explosion particle effect before hiding the visual.
- Physics overlap queries use **Physics2D.OverlapCircle** on the host to apply explosion
  damage to nearby enemies and players.

### 5  ProjectileManager

```
NetworkArray<ProjectileState>[256]
  ProjectileState {
    NetworkBool  IsActive;
    Vector2      Position;
    Vector2      Velocity;
    byte         OwnerId;      // player index
    byte         WeaponType;
    float        SpawnTick;
  }
```

- **Tick-correct movement**: each FixedUpdateNetwork advances active projectiles by
  `Velocity * Runner.DeltaTime`.
- **Hit detection**: Fusion `Physics2D.OverlapCircle` (lag-compensated) is called on the
  host; on hit the projectile's `IsActive` is set to `false`.
- Clients extrapolate position for visual smoothness via `Render()`.

### 6  GameManager & Flow

```
Bootstrap (scene 0)
   └─ starts Fusion session
   └─ loads Ship scene (scene 1)
Ship (scene 1)
   └─ ShipManager: customization rooms, lobby UI
   └─ When all players ready → MissionManager.LoadMission(missionId)
Mission (scene 2)
   └─ Host spawns EnemyManager, PropsManager, ProjectileManager
   └─ Waves of enemies via WaveController
   └─ On completion → MissionManager.ReturnToShip()
```

State machine (lives in `GameManager`):

```
CONNECTING → IN_SHIP → LOADING_MISSION → IN_MISSION → RETURNING → IN_SHIP
```

---

## Networking Topology Detail

### Host/Client

- The **Host** runs the full simulation (authoritative physics, AI, damage).
- **Clients** receive state snapshots and interpolate visuals.
- Input is sent client→host each tick via `NetworkRunner.GetInput<T>`.
- The host applies inputs in `FixedUpdateNetwork`.

### State Synchronisation

| System | Sync Method |
|--------|-------------|
| Player position/rotation | `NetworkRigidbody2D` |
| Player custom state | `[Networked]` properties + `ChangeDetector` |
| Enemies (all) | `NetworkArray<EnemyState>` in `EnemyManager` |
| Props (all) | `NetworkArray<PropState>` in `PropsManager` |
| Projectiles (all) | `NetworkArray<ProjectileState>` in `ProjectileManager` |

### Lag Compensation

- Player fire is sent as input; the host resolves hit at the **correct tick** using
  `HitboxManager` and `Runner.LagCompensation.Raycast / OverlapSphere`.
- Enemy visuals on clients are rendered at the **interpolation time** provided by Fusion.
- `NetworkRigidbody2D` handles player-to-player collision resolution automatically.

---

## Performance Targets

| Metric | Target |
|--------|--------|
| Active enemies | 500+ |
| Active projectiles | 256 |
| Destructible props | 256 |
| Tick rate | 64 Hz |
| Target framerate | 60 fps |
| Max players | 4 |

The struct-based managers allow all enemy/prop/projectile state to fit within a small
number of Fusion state pages, minimising delta serialisation overhead.
