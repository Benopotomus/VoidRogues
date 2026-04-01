# VoidRogues – Photon Fusion 2 Networking Guide

## Fusion 2 Core Concepts Used

| Concept | How VoidRogues Uses It |
|---------|----------------------|
| `NetworkRunner` | Single runner per client/host session |
| `INetworkRunnerCallbacks` | Connection events, player join/leave, scene load |
| `NetworkBehaviour` | Player characters, manager singletons |
| `SimulationBehaviour` | High-frequency tick work without network state |
| `NetworkObject` | Player characters only |
| `NetworkArray<T>` | Enemy, Prop, Projectile state arrays |
| `[Networked]` | Player health, score, weapon stats |
| `ChangeDetector` | Efficient dirty-check on struct arrays |
| `NetworkInput` / `INetworkInput` | Player input struct polled each tick |
| `LagCompensation` | Hit-scan and proximity damage resolution |
| `Hitbox` / `HitboxManager` | Per-player lag-compensated hitboxes |
| `NetworkRigidbody2D` | Authoritative player physics sync |
| `Runner.Spawn` / `Runner.Despawn` | Player prefab lifecycle |
| `IPlayerJoined` / `IPlayerLeft` | Spawn/cleanup player on join |

---

## 1  Session Start (`NetworkBootstrap`)

```csharp
// Pseudocode – see NetworkBootstrap.cs for full implementation
var args = new StartGameArgs
{
    GameMode        = isHost ? GameMode.Host : GameMode.Client,
    SessionName     = sessionName,
    PlayerCount     = 4,
    Scene           = SceneRef.FromIndex(1),         // Ship scene
    SceneManager    = gameObject.GetComponent<NetworkSceneManagerDefault>(),
};
await runner.StartGame(args);
```

- `GameMode.Host` – starts a hosted session; this machine runs simulation + renders.
- `GameMode.Client` – connects to an existing session by `SessionName`.
- `GameMode.AutoHostOrClient` – useful for quick testing; first player hosts.

### Region Selection
Set `StartGameArgs.CustomLobbyName` or use the default global lobby.  
For production, let players choose a region via a dropdown bound to
`StartGameArgs.CustomRegion`.

---

## 2  Input System

### NetworkInputData Struct

```csharp
// Assets/Scripts/Network/NetworkInputData.cs
public struct NetworkInputData : INetworkInput
{
    public Vector2    Move;       // WASD normalised
    public Vector2    AimDir;     // world-space aim direction (mouse pos - player pos)
    public NetworkButtons Buttons; // Fire, Interact, Reload …
}
```

### Polling Input

```csharp
// In a MonoBehaviour that implements INetworkRunnerCallbacks
public void OnInput(NetworkRunner runner, NetworkInput input)
{
    var data = new NetworkInputData
    {
        Move   = _inputActions.Gameplay.Move.ReadValue<Vector2>(),
        AimDir = GetAimDirection(),   // world-space
    };
    data.Buttons.Set(InputButton.Fire,     _inputActions.Gameplay.Fire.IsPressed());
    data.Buttons.Set(InputButton.Interact, _inputActions.Gameplay.Interact.IsPressed());
    input.Set(data);
}
```

### Consuming Input in Simulation

```csharp
// In PlayerController.FixedUpdateNetwork()
if (Runner.TryGetInputForPlayer<NetworkInputData>(Object.InputAuthority, out var input))
{
    _rb.velocity = input.Move * MoveSpeed;
    // forward aim direction to PlayerShooter
}
```

---

## 3  Player Spawning

```csharp
// NetworkBootstrap implements IPlayerJoined
public void PlayerJoined(NetworkRunner runner, PlayerRef player)
{
    if (runner.IsServer)
    {
        var spawnPos = GetSpawnPosition(player);
        runner.Spawn(_playerPrefab, spawnPos, Quaternion.identity, player);
    }
}
```

The `inputAuthority` parameter ties the spawned `NetworkObject` to `player`, so Fusion
automatically routes that player's `NetworkInputData` to this object.

---

## 4  Struct-Based Manager Pattern

### Why Not One NetworkObject Per Enemy?

Fusion charges bandwidth per `NetworkObject` delta.  With 500 enemies, even idle deltas
accumulate.  A single `NetworkBehaviour` with a `NetworkArray<EnemyState>` amortises the
overhead: Fusion only serialises changed array elements each tick.

### NetworkArray Declaration

```csharp
public class EnemyManager : NetworkBehaviour
{
    [Networked, Capacity(512)]
    private NetworkArray<EnemyState> _enemies { get; }

    // ChangeDetector tells us which slots changed this tick
    private ChangeDetector _changes;

    public override void Spawned()   => _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
    public override void Render()    => SyncVisuals();

    private void SyncVisuals()
    {
        foreach (var change in _changes.DetectChanges(this))
        {
            if (change == nameof(_enemies))
                RefreshAllEnemyVisuals();
        }
    }
}
```

### EnemyState Struct Rules

- Must be **blittable** (no managed references).
- Use `NetworkBool` instead of `bool`.
- Keep size ≤ 32 bytes per element to stay within Fusion's efficient serialisation path.
- Access the array via `_enemies.Set(index, newState)` (copy-on-write); do **not** try
  to mutate a retrieved value in-place.

```csharp
// Correct mutation pattern
var state = _enemies[i];
state.Health -= damage;
state.IsAlive = state.Health > 0;
_enemies.Set(i, state);
```

---

## 5  Lag Compensation

### Hit-Scan Weapons

```csharp
// In ProjectileManager or PlayerShooter (host-only path)
if (Runner.IsServer)
{
    var hitOptions = HitOptions.IncludePhysX | HitOptions.SubtickAccuracy;
    if (Runner.LagCompensation.Raycast(
            origin, direction, distance,
            Object.InputAuthority,
            out var hit,
            LayerMask.GetMask("Enemy", "Props"),
            hitOptions))
    {
        ProcessHit(hit);
    }
}
```

### Player Hitboxes

Each player prefab needs a `Hitbox` component (capsule shape matching the collider) so
that `Runner.LagCompensation` can rewind player positions for client-side hit queries.

```
PlayerCharacter (NetworkObject)
  └─ Hitbox (CapsuleHitbox2D)
       Shape: Capsule
       HitboxRoot: reference to HitboxRoot component on same GO
```

### Enemy Hitboxes (Approximated)

Because enemies are not `NetworkObject`s, their positions are reconstructed from
`EnemyState.Position` in the `EnemyManager`'s `LagCompensation.Raycast` call.  Use a
physics layer mask restricted to `Enemy` so that swept tests work efficiently.

---

## 6  Physics 2D Integration

Fusion 2 can drive Unity's `Physics2D` in tick-sync mode:

1. In **NetworkProjectConfig**: enable `Physics2D → Enabled`.
2. Fusion will call `Physics2D.Simulate(deltaTime)` inside its tick loop.
3. **Do not** call `Physics2D.Simulate` manually in `FixedUpdate`.
4. Use `Runner.GetPhysicsScene2D()` to access the physics scene for manual queries.

Player `Rigidbody2D` is controlled by `NetworkRigidbody2D`; the movement code writes
`velocity` in `FixedUpdateNetwork`, which Fusion reconciles across clients.

---

## 7  Scene Management

Fusion ships `NetworkSceneManagerDefault`.  Attach it to the `NetworkRunner` object.

```csharp
// Load Mission scene from host
if (Runner.IsServer)
{
    Runner.LoadScene(SceneRef.FromIndex(2), LoadSceneMode.Single);
}
```

All clients receive the scene-load instruction and load simultaneously.  The `IPlayerLeft`
callback fires if a client disconnects mid-load.

---

## 8  Serialisation Tips

- **NetworkString** for player names (fixed capacity: `NetworkString<_64>`).
- `FixedString` types for any longer strings.
- Avoid `string` in networked structs.
- Use `byte` / `short` for compact enum-like values in structs.
- Float precision: Fusion quantises floats to 1/100 by default; override per-property
  with `[Accuracy(AccuracyDefaults.POSITION)]` when sub-centimetre precision is needed.

---

## 9  Testing Locally with ParrelSync

1. Install **ParrelSync** via the UPM git URL:  
   `https://github.com/VeriorPies/ParrelSync.git?path=/ParrelSync`
2. **ParrelSync → Clones Manager → Add Clone**.
3. Open the clone in a second Editor instance.
4. In the **original** Editor, press Play – it starts as Host.
5. In the **clone** Editor, press Play – it starts as Client and auto-connects.

Both instances share the same `Assets/` folder (symlinked) so code changes propagate
instantly.

---

## 10  Common Pitfalls

| Pitfall | Solution |
|---------|---------|
| Mutating `NetworkArray` element in-place | Always copy-modify-Set |
| Calling `Physics2D.Simulate` manually | Remove; Fusion calls it |
| Using `Update()` for networked logic | Use `FixedUpdateNetwork()` |
| `[Networked]` on a `MonoBehaviour` | Must be `NetworkBehaviour` |
| Object.HasStateAuthority vs IsServer | Use `HasStateAuthority` for per-object checks |
| Spawning enemies as NetworkObjects | Use struct array in `EnemyManager` instead |
| Forgetting `ChangeDetector.Source` | Use `SimulationState` for `[Networked]` props |
