# VoidRogues – Gameplay Systems

## 1  Player Character

### Collider Layout
```
  ┌───────────────────────┐
  │   SpriteRenderer      │  ← full body sprite
  │                       │
  │    [CapsuleCollider2D] │  ← horizontal capsule at feet
  │     ══════════════    │     Direction: Horizontal
  └───────────────────────┘     Offset: (0, -0.3), Size: (0.9, 0.35)
```

The **horizontal capsule** at the feet is used for:
- Ground contact / overlap checks
- Enemy bump collisions
- Explosion knockback queries

The sprite pivot should be at the **feet** so the capsule naturally sits at the base.

### Input → Movement
1. `OnInput` (INetworkRunnerCallbacks) reads Unity Input System `Move` action (Vector2).
2. `PlayerController.FixedUpdateNetwork` consumes the input and sets `Rigidbody2D.velocity`.
3. `NetworkRigidbody2D` reconciles position across clients.

Movement is **top-down** (no gravity); the `Rigidbody2D` is set to **Kinematic** to avoid
unwanted physics drag.  `MovePosition` is used rather than directly setting `velocity` to
preserve Physics2D contact callbacks.

### Aim & Fire (Twin-Stick)
- **Aim**: Mouse world-position is subtracted from player world-position to produce an
  aim direction vector, normalised.
- The `GunPivot` child is rotated to face the aim direction every `Render()` frame
  (visual only; authoritative angle stored in `PlayerNetworkData`).
- **Fire**: When the Fire button is held, `PlayerShooter` calls
  `ProjectileManager.RequestFire()` on every applicable tick.

---

## 2  Projectile System

### Design Goals
- No per-projectile `GameObject` spawned on the network.
- Visual GameObjects (bullet sprites) are managed by a local pool in `ProjectileManager`.
- State lives entirely in `NetworkArray<ProjectileState>`.

### ProjectileState Struct
```csharp
public struct ProjectileState : INetworkStruct
{
    public NetworkBool  IsActive;
    public Vector2      Position;
    public Vector2      Velocity;
    public byte         OwnerId;
    public byte         WeaponTypeIndex;  // maps to WeaponDefinition ScriptableObject
    public int          SpawnTick;
    public NetworkBool  DidHit;           // set true on host the tick it hits
}
```

### Lifecycle
```
Player presses Fire
  → PlayerShooter.FixedUpdateNetwork detects input
  → (host) ProjectileManager.SpawnProjectile(ownerRef, position, direction, weaponType)
        writes into next free slot in _projectiles array
  → each tick: ProjectileManager.FixedUpdateNetwork
        advances all active projectiles by velocity * DeltaTime
        runs Physics2D.CircleCast for hit detection (host only)
        on hit: sets DidHit=true, IsActive=false, applies damage
  → Render(): local pool syncs visual GOs to array positions
        new IsActive=true → activate pooled bullet GO
        IsActive=false + DidHit=true → play hit VFX, deactivate GO
```

### Weapon Types (ScriptableObject)
```
Assets/Data/Weapons/
  Pistol.asset
  Shotgun.asset
  MachineGun.asset
  RocketLauncher.asset
```

Each `WeaponDefinition` SO contains: `FireRate`, `ProjectileSpeed`, `Damage`, `Spread`,
`ProjectileCount`, `ProjectileSprite`, `MuzzleFlashVFX`, `HitVFX`.

---

## 3  Enemy System

### Enemy Types
Define enemy variants via `EnemyDefinition` ScriptableObjects:
```
Assets/Data/Enemies/
  ZombieGrunt.asset
  FastRunner.asset
  HeavyBrute.asset
  Ranged.asset
```

`EnemyDefinition` contains: `MaxHealth`, `MoveSpeed`, `AttackDamage`, `AttackRange`,
`ScoreValue`, `VisualPrefab`, `AnimatorController`.

### EnemyState Struct
```csharp
public struct EnemyState : INetworkStruct
{
    public NetworkBool  IsActive;
    public Vector2      Position;
    public Vector2      Velocity;
    public byte         TypeIndex;
    public short        Health;
    public byte         AnimState;     // Idle=0, Walk=1, Attack=2, Death=3
    public int          TargetPlayer;  // PlayerRef value (-1 = no target)
}
```

### AI (Host-Only)
`EnemyAI.cs` is a plain C# class (not MonoBehaviour) called each tick by `EnemyManager`
on the host:

```
Per enemy (active):
  1. Find nearest player position (O(players) lookup, players ≤ 4)
  2. Steer toward target using simple steering (seek + separation)
  3. If within attack range → trigger attack, deal damage to player
  4. Write new position & velocity back to EnemyState
```

**Separation** between enemies uses a spatial hash grid (`SpatialHash2D`) rebuilt each
tick from enemy positions to avoid O(n²) pair checks.

### Wave Controller
`WaveController` (on the host) runs a coroutine that:
1. Reads the current mission's `WaveDefinition[]` (ScriptableObject).
2. Each wave: picks spawn points from `SpawnPoint[]` tagged in the level, calls
   `EnemyManager.ActivateEnemy(typeIndex, spawnPos)`.
3. Advances to next wave when `EnemyManager.ActiveEnemyCount == 0`.

---

## 4  Destructible Props

### Barrel Explosion Chain
```
Player bullet hits barrel
  → PropsManager receives damage (host)
  → PropState.Health -= damage
  → if Health ≤ 0:
      PropState.Exploding = true
      Physics2D.OverlapCircleAll(position, explosionRadius, EnemyLayer | PlayerLayer)
      → Apply explosive damage to all overlapping colliders
      → PropsManager.DeactivateProp(index) after delay
Clients:
  → ChangeDetector sees Exploding flip to true
  → PropsManager spawns/plays explosion VFX on visual prefab
  → after VFX completes, disables visual GO
```

### Chained Explosions
Because `Physics2D.OverlapCircle` can hit other barrels, the damage loop can trigger more
`PropState.Health` reductions, resulting in chain explosions fully driven by the host
simulation and replicated to clients via the struct array.

---

## 5  Game Flow

### States

```
CONNECTING
    ↓ Fusion session established
IN_SHIP
    ↓ Host calls MissionManager.LoadMission(id)
LOADING_MISSION
    ↓ scene loaded, managers spawned
IN_MISSION
    ↓ all waves cleared (or timer expired, etc.)
RETURNING
    ↓ scene unloaded, Ship scene restored
IN_SHIP
```

### Ship Scene

The ship contains multiple **rooms**:
- **Armoury** – weapon loadout selection
- **Medibay** – passive upgrades/skills
- **Engineering** – ship cosmetics (post-mission unlock)
- **Bridge** – mission selection terminal + lobby

`ShipManager` tracks per-player customisation in a `[Networked]` dictionary keyed by
`PlayerRef`.  The Bridge terminal shows a lobby UI when all players are "ready"; the host
then calls `MissionManager.LoadMission`.

### Mission Scene

`MissionManager` (host authority) drives:
1. Spawn `EnemyManager`, `PropsManager`, `ProjectileManager` prefabs.
2. Start `WaveController`.
3. Monitor win/loss conditions.
4. On completion: award loot, call `MissionManager.ReturnToShip()`.

**Loot** is defined in `LootTable` ScriptableObjects; the host rolls and distributes
items, stored in `[Networked]` arrays on each player's `PlayerNetworkData`.

---

## 6  Camera

Use **Cinemachine Virtual Camera** in the Mission scene:
- `CinemachineVirtualCamera` with `CinemachineTransposer`
- **Follow target**: midpoint of all active player transforms (updated each frame)
- **Confiner2D**: polygon collider bounding the level

For the Ship scene, a static camera suffices; transition between scenes uses a simple
fade-to-black via a Canvas overlay.

---

## 7  Audio

- Use Unity's **AudioMixer** with three groups: `Master / SFX / Music`.
- `AudioManager` (singleton MonoBehaviour) exposes `PlaySFX(clip, position)` and
  `PlayMusic(clip)`.
- Enemy sounds are triggered by `EnemyManager.Render()` when `AnimState` changes; no
  per-enemy AudioSource is needed – a shared pool of 16 `AudioSource` components handles
  concurrent SFX.

---

## 8  Art & Animation

### Sprite Guidelines
- Characters: 32×32 px at 32 PPU, pivot at feet (x=0.5, y=0).
- Enemy sprites: same PPU for consistent scaling.
- Props (barrels): 16×24 px at 32 PPU.

### Animator Controllers
Each enemy type has an `AnimatorController` with the following states and triggers:

| State | Transition |
|-------|-----------|
| Idle | Default |
| Walk | `animState == 1` |
| Attack | `animState == 2` |
| Death | `animState == 3` |
| Death_End | After death animation completes; `EnemyVisual` deactivates itself |

The `EnemyManager.Render()` loop reads `EnemyState.AnimState` and calls
`animator.SetInteger("State", animState)` on each active visual.

---

## 9  Saving & Progression

Persistent data (unlocks, ship upgrades, run history) is **not** networked – each player
stores it locally in `Application.persistentDataPath` as JSON using `JsonUtility`.

`SaveManager` exposes `Load<T>()` / `Save<T>()` helpers.  Sensitive fields (if any) should
be encrypted with `System.Security.Cryptography.AesCryptoServiceProvider`.
