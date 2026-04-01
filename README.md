# VoidRogues
Roguelike 2D twin-stick shooter with online co-op powered by Photon Fusion 2.

## Quick Links

| Document | Description |
|----------|-------------|
| [Docs/SetupGuide.md](Docs/SetupGuide.md) | Unity + Photon Fusion 2 installation and project setup |
| [Docs/Architecture.md](Docs/Architecture.md) | System architecture, folder layout, networking topology |
| [Docs/NetworkingGuide.md](Docs/NetworkingGuide.md) | Photon Fusion 2 patterns used in this project |
| [Docs/GameplaySystems.md](Docs/GameplaySystems.md) | Gameplay systems: player, enemies, props, projectiles, game flow |

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Engine | Unity 2022.3.62f1 (LTS) |
| Networking | Photon Fusion 2 – Host/Client |
| Physics | Unity Physics 2D (tick-synced via Fusion) |
| Input | Unity Input System 1.7+ |
| UI | TextMeshPro |
| Camera | Cinemachine |

## Key Design Decisions

- **Struct-based networking** – Hundreds of enemies and props are tracked in
  `NetworkArray<EnemyState>` / `NetworkArray<PropState>` inside single
  `NetworkBehaviour` managers, avoiding per-entity `NetworkObject` overhead.
- **Top-down 2D with horizontal capsule colliders** at character feet for
  accurate ground/bump detection.
- **Twin-stick controls** – keyboard moves, mouse aims and fires.
- **Host/Client topology** – the host runs all AI, damage, and physics; clients
  receive state deltas and drive visual-only GameObjects.

## Game Flow

```
Bootstrap (scene 0) → Ship hub (scene 1) → Mission (scene 2) → Ship hub
```

The **Ship** is a persistent hub where players customise characters and ready up
for a mission.  Missions are swarms of enemies; completing all waves returns
players to the Ship with loot.

## Getting Started

See **[Docs/SetupGuide.md](Docs/SetupGuide.md)** for full setup instructions.

TL;DR:
1. Open the project in Unity 2022.3.62f1.
2. Import the Photon Fusion 2 SDK and enter your App ID.
3. Open the `Bootstrap` scene and press Play.
