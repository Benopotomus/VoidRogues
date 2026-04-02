# VoidRogues — Developer Guide
> **Instruction Prompt — Senior Developer**
>
> Use this document as the authoritative technical reference when writing code, reviewing PRs, or architecting new systems. All code must follow the conventions here. When introducing a new pattern, update this document first and get buy-in before implementation.

---

## 1. Environment Setup

### Requirements
| Tool | Version |
|------|---------|
| Unity Hub | Latest |
| Unity Editor | 2022.3 LTS (2D URP template) |
| .NET SDK | 6.0+ (for tooling; Unity uses built-in Mono/IL2CPP) |
| JetBrains Rider or VS Code | Latest |
| Git | 2.40+ |
| Git LFS | 3.x (for binary assets) |

### First-Time Setup
```bash
# 1. Clone
git clone https://github.com/Benopotomus/VoidRogues.git
cd VoidRogues

# 2. Install Git LFS and pull tracked assets
git lfs install
git lfs pull

# 3. Open in Unity Hub
#    → Add project from disk → select VoidRogues/
#    → Open with Unity 2022.3 LTS

# 4. Wait for asset import to complete (first open may take a few minutes)
# 5. Open Assets/Scenes/MainMenu.unity and press Play ▶
```

### Git LFS Tracked Types
`.png`, `.jpg`, `.psd`, `.aseprite`, `.wav`, `.ogg`, `.mp3`, `.fbx`, `.prefab`, `.unity`, `.asset`

---

## 2. Project Structure

```
Assets/
├── Animations/          # Animator controllers (.controller) + animation clips (.anim)
├── Audio/
│   ├── Music/           # Background tracks
│   └── SFX/             # Sound effects
├── Materials/           # URP Lit/Unlit materials, shader graphs
├── Prefabs/
│   ├── Enemies/         # One prefab per enemy variant
│   ├── Items/           # One prefab per item
│   ├── Player/          # PlayerCharacter.prefab + sub-prefabs
│   ├── Projectiles/     # Bullet/projectile variants
│   └── Rooms/           # Room template prefabs
├── Resources/           # Assets loaded at runtime via Resources.Load<T>()
│   ├── Items/           # ScriptableObject item definitions
│   └── Enemies/         # ScriptableObject enemy definitions
├── Scenes/
│   ├── MainMenu.unity
│   ├── Game.unity       # Single persistent game scene (rooms load additively)
│   └── GameOver.unity
├── Scripts/             # All C# source files (no code outside this folder)
│   ├── Combat/
│   ├── Core/
│   ├── Dungeon/
│   ├── Enemies/
│   ├── Items/
│   ├── Player/
│   └── UI/
└── Sprites/
    ├── Enemies/
    ├── Player/
    ├── Tiles/
    └── UI/
```

### Scene Architecture
- **One game scene** (`Game.unity`) is always loaded. Room prefabs are loaded/unloaded additively.
- `GameManager` is a persistent singleton that survives scene transitions (`DontDestroyOnLoad`).
- UI canvases live in the game scene; the HUD is activated/deactivated per state.

---

## 3. Coding Conventions

### Naming
| Element | Convention | Example |
|---------|-----------|---------|
| Class | PascalCase | `PlayerController` |
| Method | PascalCase | `ApplyDamage()` |
| Property | PascalCase | `MaxSanity` |
| Private field | `_camelCase` | `_currentSanity` |
| Public field / SerializeField | camelCase | `moveSpeed` |
| Constant | UPPER_SNAKE | `MAX_CORRUPTION` |
| Interface | `I` prefix | `IDamageable` |
| ScriptableObject | `SO` suffix | `ItemDataSO` |
| Event | `On` prefix | `OnPlayerDeath` |

### File Rules
- **One class per file.** File name must match class name exactly.
- No `using` directives for namespaces you don't need.
- All scripts live under `Assets/Scripts/` in the appropriate subfolder.

### Namespace
```csharp
namespace VoidRogues.Core        // Core singletons & managers
namespace VoidRogues.Player      // Player-specific systems
namespace VoidRogues.Enemies     // Enemy AI & data
namespace VoidRogues.Combat      // Weapons, projectiles, damage
namespace VoidRogues.Dungeon     // Room generation & sector logic
namespace VoidRogues.Items       // Item data, pickup, effects
namespace VoidRogues.UI          // HUD, menus, overlays
```

### Comments
- Write comments that explain **why**, not **what** (the code shows what).
- All public methods and properties must have XML doc-comments:
```csharp
/// <summary>Applies damage to this entity, clamped to zero.</summary>
/// <param name="amount">Raw damage before defense calculation.</param>
public void ApplyDamage(int amount) { ... }
```

---

## 4. Architecture Overview

### Core Systems

```
GameManager (singleton)
  ├─ RunData          // current run state (sector, items, stats)
  ├─ EventBus         // global events (OnPlayerDeath, OnRoomCleared, etc.)
  └─ SceneLoader      // async scene / room transitions

Player
  ├─ PlayerController   // movement, input, dodge roll
  ├─ PlayerCombat       // weapon firing, ability activation
  ├─ HealthSystem       // Sanity (HP) tracking, death
  └─ InventorySystem    // held items, effect application

Dungeon
  ├─ SectorGenerator    // builds sector map (room graph)
  ├─ RoomController     // manages door locks, enemy spawns, rewards
  └─ RoomData (SO)      // configuration for a room template

Enemies
  ├─ EnemyBase          // shared state machine, pathfinding, health
  ├─ EnemyAI_*          // concrete AI behaviour per archetype
  └─ EnemyDataSO        // stats, loot table, prefab reference

Items
  ├─ ItemDataSO         // definition: effect, tier, sprite, lore
  ├─ ItemPickup         // world pickup behaviour
  └─ ItemEffect (base)  // abstract class for item runtime effects

UI
  ├─ HUDController      // Sanity bar, Fragment counter, ability icon
  ├─ PauseMenuController
  ├─ ItemTooltip
  └─ DeathScreenController
```

### Event Bus Pattern
Use `EventBus` for cross-system communication — **never** grab a direct reference to an unrelated system.

```csharp
// Publishing
EventBus.Publish(new PlayerDeathEvent { lastPosition = transform.position });

// Subscribing
EventBus.Subscribe<PlayerDeathEvent>(OnPlayerDeath);

// Unsubscribing (always do this in OnDestroy)
EventBus.Unsubscribe<PlayerDeathEvent>(OnPlayerDeath);
```

### Singleton Pattern
Only **managers** are singletons. Use this base:
```csharp
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }
    protected virtual void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = (T)(MonoBehaviour)this;
        DontDestroyOnLoad(gameObject);
    }
}
```

---

## 5. Data-Driven Design (ScriptableObjects)

All static game data (enemies, items, room templates) is stored in `ScriptableObject` assets under `Assets/Resources/`.

- **ItemDataSO** — one asset per item in the game
- **EnemyDataSO** — one asset per enemy type
- **WeaponDataSO** — one asset per weapon variant

Runtime systems hold a reference to these SOs. **Never mutate SO data at runtime** — copy values into mutable run-state classes instead.

---

## 6. Input System

Use Unity's **Input System package** (not the legacy Input class).

- Input actions are defined in `Assets/Settings/VoidRoguesInputActions.inputactions`
- The `PlayerController` reads from a generated `VoidRoguesInputActions` C# class
- Supported bindings: WASD/arrows (move), left-click (fire), right-click/Space (dodge), E (interact), Escape (pause)

---

## 7. Physics & Collision Layers

| Layer | Index | Used For |
|-------|-------|---------|
| Default | 0 | Static environment |
| Player | 6 | Player collider |
| Enemy | 7 | Enemy colliders |
| PlayerProjectile | 8 | Bullets fired by player |
| EnemyProjectile | 9 | Bullets fired by enemies |
| Pickup | 10 | Item pickups, fragments |
| Wall | 11 | Room walls (blocks all) |

Physics matrix: Player projectiles hit Enemies + Walls only. Enemy projectiles hit Player + Walls only. Pickups trigger on Player only.

---

## 8. Procedural Dungeon Generation

**Algorithm: BSP (Binary Space Partitioning) + Corridor stitching**

1. `SectorGenerator` divides a fixed-size grid into BSP leaf nodes.
2. Each leaf becomes a room slot; a `RoomData` prefab is randomly selected (weighted by room type budget).
3. Corridors connect leaf centroids; doors are placed at corridor entry points.
4. Special rooms (shop, event, boss) are placed at guaranteed positions in the graph (shop = mid-sector, boss = terminal node).

Room templates are prefabs with empty `EnemySpawnPoint` and `RewardSpawnPoint` markers; `RoomController` instantiates content at runtime.

---

## 9. Performance Guidelines

- **Object Pooling** is mandatory for projectiles and VFX. Use the `ObjectPool<T>` utility in `Core/`.
- Target **60 FPS** on mid-range hardware (GTX 1060 / equivalent).
- Keep draw calls < 100 per frame (use sprite atlases, limit dynamic lights).
- No `FindObjectOfType<T>()` calls at runtime — cache references in `Awake()` or inject via the inspector.
- Use `Physics2D.OverlapCircleNonAlloc` (non-alloc variants) for all overlap queries.

---

## 10. Testing Strategy

### Unit Tests (EditMode)
- Location: `Assets/Tests/EditMode/`
- Test pure logic: damage calculations, item effect math, dungeon graph validity
- Run with Unity Test Runner (Window → General → Test Runner)

### Play Mode Tests
- Location: `Assets/Tests/PlayMode/`
- Test scene transitions, player state after X actions, enemy AI state machines

### Manual QA Checklist (per PR)
- [ ] Run starts and ends without exceptions
- [ ] Player can move, attack, dodge in all 4 directions
- [ ] Enemy spawns, attacks, and dies correctly
- [ ] Item pickup applies its effect
- [ ] Death screen shows and restart works
- [ ] No obvious frame-rate drops in combat

---

## 11. Git Workflow

```
main          ← production-ready, tagged releases only
develop       ← integration branch, always stable
feat/*        ← new features (branch from develop)
fix/*         ← bug fixes (branch from develop or main for hotfixes)
refactor/*    ← refactors (branch from develop)
```

### Commit Message Format (Conventional Commits)
```
<type>(<scope>): <short description>

feat(player): add dodge roll invincibility frames
fix(dungeon): prevent duplicate boss room spawn
refactor(enemies): extract shared AI state machine to EnemyBase
docs(design): update corruption system thresholds
```

### PR Requirements
- Passes all unit + PlayMode tests
- No new compiler warnings
- Reviewed by at least 1 other contributor
- Branch is up-to-date with `develop`

---

## 12. Build & Release

```bash
# Build via Unity CLI (CI/CD)
Unity.exe -quit -batchmode \
  -projectPath . \
  -buildTarget StandaloneWindows64 \
  -buildWindowsPlayer Builds/VoidRogues.exe \
  -logFile build.log
```

Build output goes to `Builds/` (gitignored). CI runs on every push to `develop` and `main`.

---

## 13. Dependency List

| Package | Purpose | Version |
|---------|---------|---------|
| Photon Fusion 2 | Multiplayer networking (Host/Client) | 2.0.6 |
| Photon Fusion Physics Addon | `NetworkRigidbody2D`, lag-comp hitboxes | 2.0.6 |
| Newtonsoft JSON | JSON serialisation (required by Fusion) | 3.2.1 |
| ParrelSync | Multi-editor local multiplayer testing | latest (git) |
| Input System | Player input | 1.7.x |
| Universal RP (URP) | 2D rendering | 14.x |
| 2D Tilemap Extras | Extended tilemap tools | 3.x |
| TextMeshPro | UI text rendering | 3.x (included) |
| Cinemachine | Camera management | 2.9.x |

All packages are declared in `Packages/manifest.json` and resolved automatically by Unity on first open. The Photon packages use the scoped registry at `https://package.photon.io`; a Photon App ID (free tier available at <https://dashboard.photonengine.com>) is required at runtime.

---

## 14. Common Pitfalls

| Pitfall | Solution |
|---------|---------|
| Mutating ScriptableObject data at runtime | Copy SO values into a mutable RunData POCO |
| `FindObjectOfType` in Update | Cache in Awake / use EventBus |
| Forgetting to unsubscribe events | Always unsubscribe in `OnDestroy` |
| Hardcoded magic numbers | Define as named constants or SO fields |
| Skipping object pooling for projectiles | Always pool — projectiles fire every frame |
| Physics in Update instead of FixedUpdate | Use `Rigidbody2D` + `FixedUpdate` for movement |
