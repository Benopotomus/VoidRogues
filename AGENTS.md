# VoidRogues — Agent Session Context

This file is the persistent memory store for Copilot agents working on this repo.
Update the relevant sections whenever meaningful architectural decisions are made or
a session ends.  Commit this file so that future agents can read it immediately
without relying on the ephemeral memory system.

---

## Project Overview

Top-down Vampire-Survivors-style roguelite using:

- **Unity** (game engine)
- **Photon Fusion 2** (networking — authoritative server / replicated state)
- **A\* Pathfinding Pro 5.4.6** (`com.arongranberg.astar`) for NPC pathfinding
- **Cinemachine 2.x** for the camera
- **Unity Input System** for input handling

Namespace: `VoidRogues`  
Assembly definition: `Assets/Scripts/VoidRogues.asmdef`  
Editor-only asmdef: `Assets/Scripts/Editor/VoidRogues.Editor.asmdef` (references VoidRogues GUID `4b761423bfdd0d644a01c908a6e87add`)

---

## Core Architecture

### SceneContext pattern (from LichLord)

- `SceneContext` is a `[Serializable]` plain C# class (not a MonoBehaviour), serialized
  inline inside `NetworkedScene`.
- Managers are `ContextBehaviour` (extends `NetworkBehaviour` + `IContextBehaviour`).
- Context is auto-injected by `NetworkSceneManager.OnSceneLoaded()`.
- Active fields: `Runner`, `NetworkGame`, `PlayerSpawnManager`, `Camera`,
  `NonPlayerCharacterManager`.  Many more are commented out for future porting from
  LichLord.

### GameplayScene hierarchy

Root scene GameObjects: `Light`, `Camera`, `GameplayScene`, `StandaloneManager`,
`Global`, `InputManager`, `EventSystem`, `Plane`.

`GameplayScene` NetworkObject children: `NetworkGame`, `ObjectCache`,
`PlayerSpawnManager`, `SceneCamera`, `NonPlayerCharacterManager`.

A\* Pathfinding root GO (`AstarPath`, `scanOnStartup = true`) lives as a standalone
root GameObject; it has **no** SceneContext field reference.

---

## NPC System

### Data layer

`FNonPlayerCharacterData` — 21-byte `[StructLayout(Explicit)] INetworkStruct`:

| Offset | Size | Field | Notes |
|--------|------|-------|-------|
| 0 | 4 B | `_configuration` (int) | DefinitionID (bits 0–11), SpawnType (bits 12–15), TeamID (bits 16–17), IsMoving (bit 18) |
| 4 | 9 B | `_transform` (FWorldTransform) | Position (7 B) + Rotation/Yaw (2 B) |
| 13 | 1 B | `_condition` (byte) | NPCState (bits 0–3), animation bits, hit bits |
| 14 | 2 B | `_events` (ushort) | Health (12 bits) + storage |
| 16–20 | — | padding | — |

`TargetPlayerIndex` is packed into `_transform.RawCompressedYaw` as `value + 240`
(values 241–255 → player indices 1–15; ≤ 240 = normal yaw).

Max NPC count: `NonPlayerCharacterConstants.MAX_NPC_REPS = 800`.

### Manager (`NonPlayerCharacterManager : ContextBehaviour`)

- `[Networked, Capacity(800)] NetworkArray<FNonPlayerCharacterData> _npcDatas`
- `[Networked] int _dataCount`
- `FixedUpdateNetwork`: server-authoritative NPC tick + **player-NPC separation pass**
  (`ApplyPlayerNPCSeparation`) — push NPCs away from players; players never deflected
  (Vampire Survivors style).
- `Render`: interpolation between `fromBuffer` / `toBuffer` snapshots using
  `TryGetSnapshotsBuffers`. Ring-buffer indexing: `key % MAX_NPC_REPS`.
  After interpolation, calls `ApplyPredictiveClientSeparation()` on non-authority clients.
- `_views`: `Dictionary<int, NPCViewEntry>` (view-index → entry).
- Spawning: async via `NonPlayerCharacterSpawner`, callback `OnNPC_Loaded`.

### Player-NPC Separation

Two complementary passes keep NPCs visually separated from players:

**Server-side (`ApplyPlayerNPCSeparation`, runs in `FixedUpdateNetwork`):**
- Iterates all active NPCs and all `PlayerCharacter` instances via `Runner.GetAllBehaviours`.
- XZ-only distance check with `combined = _playerSeparationRadius + _npcSeparationRadius + _separationSkinWidth`.
- On overlap: accumulates a push vector per NPC, writes the new position into
  `FNonPlayerCharacterData.Position`, and calls `entry.NPC.TeleportToPosition(newPos)` so
  the `FollowerEntity` internal state stays consistent.
- Only NPCs are pushed; players are never deflected.
- Tunable via serialized fields: `_playerSeparationRadius`, `_npcSeparationRadius`,
  `_separationSkinWidth`, `_pushStrength`.

**Client-side predictive (`ApplyPredictiveClientSeparation`, runs in `Render`):**
- Runs only when `!hasAuthority` (non-server clients).
- After snapshot interpolation places all NPC transforms, applies the same push math
  against `Context.LocalPlayerCharacter.transform.position` — the KCC-predicted player
  position that is already client-side predicted with no round-trip delay.
- Writes directly to `CachedTransform.position`. **Does not mutate any
  `FNonPlayerCharacterData` fields** — purely visual; overwritten by interpolation
  on the next frame.
- Eliminates the ~RTT (≈150 ms) visible delay where NPCs appear to overlap the player
  before the server correction arrives.
- Only the *local* player is used; remote player positions carry the same network delay
  and would not improve perceived latency.

#### Three-Phase Algorithm (current implementation — PRs #50 + #51)

`ApplyPredictiveClientSeparation` uses `_npcDisplayPositions: Dictionary<int, Vector3>`
to track the full visual XZ position per NPC (not an offset).  Three sub-phases per NPC
per render frame:

| Phase | Condition | Action |
|-------|-----------|--------|
| **Push** | `networkPos` inside circle AND `displayPos` inside circle | Snap `displayPos` to boundary: `pushDir * overlap * _pushStrength` |
| **Flight** | `networkPos` inside circle AND `displayPos` at/past boundary | Advance `displayPos` with a steering velocity (see below) |
| **Decay** | `networkPos` outside circle | `MoveTowards(displayPos, networkPos, _separationDecaySpeed * dt)` + convergence removal |

**Flight-phase steering velocity (RVO-style flocking):**

```
primary = outwardDir * flightSpeed                     // away from player
for each other NPC in _npcDisplayPositions:            // previous-frame positions
    weight = max(0, (_npcFlockingRadius - dist) / _npcFlockingRadius)
    avoidance += (displayPos - otherPos).normalized * flightSpeed * weight
velocity = clamp(primary + avoidance, flightSpeed)     // redirect, never accelerate
```

This makes NPCs spread **sideways** as they flock away rather than piling up radially.
`_npcFlockingRadius` (default 1.2 units, serialized) controls the personal-space bubble;
larger values give looser, more spread-out flocks.

**Key properties:**
- Entries seeded only when `networkPos` enters the circle; removed when `displayPos`
  converges to `networkPos` (outside circle, distance² < `CONVERGENCE_THRESHOLD_SQUARED`).
- No `FNonPlayerCharacterData` fields mutated — purely cosmetic.
- On `ReturnView`, the entry is cleared so recycled NPC indices start fresh.

#### Previous Bug (fixed in PR #50): Stationary-Player NPC Boundary Lock

**Symptom:** After a push, NPCs froze at the exclusion-circle boundary and never moved
away; they piled up on top of each other while the player was stationary.

**Root cause:** The old push-phase code only fired when `overlap > 0`.  Once `displayPos`
reached the boundary (`overlap ≤ 0`) and `networkPos` was still inside the circle, the
code did nothing — no outward movement, no convergence.  NPCs were permanently locked at
the boundary radius until the server's push finally arrived.

**Fix:** Added the **flight sub-phase** (`overlap ≤ 0` while `networkInsideCircle`):
advance `displayPos` outward at the NPC's natural speed instead of leaving it static.
Subsequent PR added NPC-NPC avoidance steering to the flight phase for RVO-like flocking.

### NPC prefab components (`NonPlayerCharacter : DWDObjectPoolObject`)

The prefab at `Assets/NPCs/NonPlayerCharacter.prefab` has these serialized component
references:

`Movement`, `State`, `Brain`, `HitReact`, `Health`, `Weapons`, `AnimationController`,
`SpawningComponent`, `Lifetime`, `MeleeHitTracker`, `CachedTransform`, `Collider`.

### Data definitions hierarchy

`NonPlayerCharacterDataDefinition` (ScriptableObject base)  
→ `InvaderDataDefinition`, `WorkerDataDefinition`, `CommandedUnitDataDefinition`

`NonPlayerCharacterDefinition` maps `ENPCSpawnType → DataDefinition` via
`SpawnTypeDataDefinitionEntry[]`.

`NonPlayerCharacterTable` provides static definition lookup.

Dialog system: **fully removed** (as of commit `8048221`). Five dialog bits in
`_configuration` are kept as `RESERVED_DIALOG` for binary compatibility.

### Movement component (`NonPlayerCharacterMovementComponent`)

- Uses `IAstarAI` interface (not `FollowerEntity` directly) to stay compatible whether
  or not `MODULE_ENTITIES` is defined.
- Server: `FollowerEntity` drives the transform; position written into
  `FNonPlayerCharacterData.Position` each `OnFixedNetworkUpdate`.
- Client: `FollowerEntity` movement disabled (`updatePosition/updateRotation/simulateMovement = false`);
  `OnRender` lerps the transform from snapshot data.
- `TeleportToPosition(Vector3, clearPath: false)` used by the separation pass so the
  agent's internal state stays consistent after server pushes.

### A\* Pathfinding notes (5.4.6)

- `NNConstraint.Default` is `[Obsolete]` — use `NNConstraint.Walkable`.
- `NNInfo.clampedPosition` is `[Obsolete]` — use `.position`.
- `FollowerEntity` requires `MODULE_ENTITIES` define; without it the class is a stub.
  Always code against `IAstarAI`.

---

## Camera System

`SceneCamera` uses explicit `SetCameraFollow(Transform)` pattern (from LichLord).
`PlayerCharacter.Spawned()` calls `Context.Camera.SetCameraFollow(transform)`.
`SceneCamera.OnTick()` uses private `_followTransform` as primary, falls back to
`Context.ObservedPlayerCharacter`.

`TopDownCamera` (Cinemachine):
- Body: `CinemachineFramingTransposer` — `CameraDistance=3`, `ScreenY=0.725`
- Aim: `CinemachineHardLookAt`
- Extension: `CinemachineCollider` — `DistanceLimit=3`, `CameraRadius=0.25`
- Transform rotated 60° around X-axis.

Cinemachine 2.x component GUIDs:
- `FramingTransposer` = `6ad980451443d70438faac0bc6c235a0`
- `HardLookAt` = `a4c41ac9245b87c4192012080077d830`
- `CinemachineCollider` = `e501d18bb52cf8c40b1853ca4904654f`
- `CinemachinePipeline` = `ac0b09e7857660247b1477e93731de29`

---

## Input System

`InputManager` (`Assets/Scripts/Input/InputManager.cs`) is a `DontDestroyOnLoad`
singleton referencing `VoidRoguesInputActions`.

GameplayScene GOs: `InputManager` (PlayerInput + InputManager script) and `EventSystem`
(EventSystem + InputSystemUIInputModule).

Unity Input System GUIDs:
- `PlayerInput` = `62899f850307741f2a39c98a8b639597`
- `InputSystemUIInputModule` = `01614664b831546d2ae94a42149d80ac`
- `EventSystem` = `76c392e42b5098c458856cdf6ecaaaa1`
- `InputActionImporter` = `8404be70184654265930450def6a9037`

---

## Recent Work (as of April 2026)

| PR | Branch | Description |
|----|--------|-------------|
| #47 | `copilot/improve-latency-for-npcs` | NPC latency improvements |
| #48 | `copilot/fix-npc-flickering-issue` | Removed client-side NPC prediction offset that caused flip/flicker |
| #49 | `copilot/add-predictive-pushing` | Client-side predictive NPC separation (`ApplyPredictiveClientSeparation`) to eliminate ~150 ms push latency |
| #50 | `copilot/fix-npc-separation-convergence` | Fix stationary-player NPC convergence: replaced offset dict with display-position dict; added flight sub-phase so NPCs advance outward instead of freezing at push boundary |
| #51 | `copilot/fix-npcs-push-position-lock` | RVO-style NPC-NPC avoidance steering in flight phase: NPCs spread sideways as they disperse (via `_npcFlockingRadius` + linear-falloff repulsion), no longer pile up in radial column |

---

## Known TODOs / Pending Ports from LichLord

These systems are commented out in `SceneContext.cs` pending porting:

`ProjectileManager`, `PropManager`, `WorldSaveLoadManager`, `PlayerSaveLoadManager`,
`WorldManager`, `ChunkManager`, `SceneUI`, `LairManager`, `InvasionManager`,
`ContainerManager`, `MissionManager`, `DebugConsole`, `VFXManager`, `ObjectCache`,
`Matchmaking`, `SceneInput`, `ActorEventManager`, `ImpactManager`,
`GameplayEffectManager`, `LevelManager`, `CreatureManager`, `HitManager`.

`NonPlayerCharacterBrainComponent` has `IChunkTrackable` support commented out
(`TODO: Port from LichLord`).

`NonPlayerCharacter` has `IHitTarget` / `IHitInstigator` / `IChunkTrackable` commented
out.

---

## Conventions

- All game code in `VoidRogues` namespace.
- Managers live under `Assets/Scripts/SceneContext/{ManagerName}/`.
  - Exception: `NonPlayerCharacterManager` and NPC-related code now live under
    `Assets/Scripts/NonPlayerCharacters/` (refactored from `SceneContext/`).
- `ContextBehaviour` for networked scene managers; `CoreBehaviour` for local helpers.
- LichLord is the reference project — port patterns from there when extending.
