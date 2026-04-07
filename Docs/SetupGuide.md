# VoidRogues – Setup Guide

## Requirements

| Tool | Version |
|------|---------|
| Unity Editor | 2022.3.62f1 (LTS) |
| Photon Fusion SDK | 2.x (latest) |
| Unity Physics 2D | Built-in (2022.3) |
| TextMeshPro | 3.0.x (via Package Manager) |
| Input System | 1.7.x (via Package Manager) |

---

## 1  Create the Unity Project

1. Open **Unity Hub → New Project**.
2. Choose the **2D (Core)** template.
3. Name it `VoidRogues`, point the location at this repository root.
4. Click **Create Project** – Unity generates the standard `Assets/`, `ProjectSettings/`, and `Packages/` folders.

> The `.gitignore` already excludes `Library/`, `Temp/`, `Obj/`, and `Build/` folders.

---

## 2  Install Required Packages

### 2a  Via Package Manager (Window → Package Manager)

| Package | Source | Notes |
|---------|--------|-------|
| **Input System** | Unity Registry | Enable "Both" backend in Player Settings when prompted |
| **TextMeshPro** | Unity Registry | Accept TMP Essential Resources when prompted |
| **2D Sprite** | Unity Registry | Usually pre-installed with 2D template |
| **2D Tilemap Editor** | Unity Registry | Needed for level layouts |
| **Cinemachine** | Unity Registry | Smooth camera for twin-stick view |

### 2b  Install Photon Fusion 2

**Install via `.unitypackage` from the Photon dashboard**

1. Log in at <https://dashboard.photonengine.com> and create a free Fusion app if you
   do not have one.
2. Download the **Photon Fusion 2 SDK** `.unitypackage` from the dashboard.
3. In Unity, go to **Assets → Import Package → Custom Package** and select the
   downloaded `.unitypackage`.  The files will be placed in `Assets/Photon/Fusion/`.
4. Also download and import the **Fusion Physics Addon** `.unitypackage` to get
   `NetworkRigidbody2D` and lag-compensated hitboxes.
5. Open **Fusion → Fusion Hub** and enter your **App ID**.
6. Confirm the `Fusion` assembly is available in the Project window.

> **Note:** `com.unity.nuget.newtonsoft-json` is already included in
> `Packages/manifest.json` as it is required by Fusion.

### 2c  Verify Physics 2D Settings

1. **Edit → Project Settings → Physics 2D**
2. Ensure **Simulation Mode** is set to `Fixed Update`.
3. Set **Gravity** to `(0, -20)` (heavier feel for a top-down 2D game use `(0,0)`).
   - VoidRogues uses a **top-down perspective** with zero gravity; set Y to `0`.
4. **Layers**: Create the following collision layers:

| Layer | Index | Notes |
|-------|-------|-------|
| Player | 8 | Local and remote player characters |
| Enemy | 9 | All enemies |
| Projectile | 10 | Bullets / projectiles |
| Props | 11 | Destructible environment objects |
| Level | 12 | Static level geometry |

5. In **Layer Collision Matrix**, disable Player↔Player and Projectile↔Projectile collisions.

---

## 3  Photon App Configuration

1. Open `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset`.
2. Set **App Id Fusion** to your Photon App ID.
3. Set **Region** to the closest Photon cloud region (e.g., `us`, `eu`, `asia`).
4. Leave **Fixed Region** empty unless you need a specific datacenter.

---

## 4  Input System Setup

1. **Edit → Project Settings → Player → Active Input Handling** → `Both` (or `Input System Package (New)`).
2. Create an **Input Actions asset** at `Assets/Settings/VoidRoguesInputActions.inputactions`:
   - Action Map: **Gameplay**
     - `Move` – Value, Vector2, bound to WASD + Left Stick
     - `Aim` – Value, Vector2, bound to Mouse Delta + Right Stick
     - `Fire` – Button, bound to Mouse Left Button + Right Trigger
     - `Interact` – Button, bound to E + South button
   - Action Map: **UI**
     - Standard UI actions (auto-generated)
3. Generate the C# wrapper class (**Generate C# Class** checkbox in the Inspector).

---

## 5  Assembly Definition Files

Each script folder contains an `.asmdef` file so that compile times stay fast and
dependencies are explicit:

```
Assets/Scripts/Network/VoidRogues.Network.asmdef
Assets/Scripts/Player/VoidRogues.Player.asmdef
Assets/Scripts/Enemies/VoidRogues.Enemies.asmdef
Assets/Scripts/Props/VoidRogues.Props.asmdef
Assets/Scripts/Projectiles/VoidRogues.Projectiles.asmdef
Assets/Scripts/GameFlow/VoidRogues.GameFlow.asmdef
Assets/Scripts/UI/VoidRogues.UI.asmdef
```

All assemblies should reference `Fusion.Runtime` and `Unity.InputSystem`.

---

## 6  Scene Setup

### 6a  Bootstrap Scene (`Assets/Scenes/Bootstrap.unity`)
- Only **one** object: `NetworkBootstrap` (add the `NetworkBootstrap.cs` component).
- Set **Build Index 0**.
- This scene loads immediately on launch and handles Fusion connection.

### 6b  Ship Scene (`Assets/Scenes/Ship.unity`)
- Persistent hub scene.
- Contains: `ShipManager`, `ShipUI`, customization rooms, and the mission lobby terminal.

### 6c  Mission Scene (`Assets/Scenes/Mission.unity`)  *(template)*
- Level geometry (Tilemap), `EnemyManager`, `PropsManager`, `ProjectileManager`.
- Specific mission variants are additive scenes loaded on top of this template.

### 6d  Build Settings
Add scenes in this order:

| Build Index | Scene |
|-------------|-------|
| 0 | Bootstrap |
| 1 | Ship |
| 2 | Mission |

---

## 7  Photon Fusion Network Config

1. In `Assets/Photon/Fusion/Resources/` create (or edit) `NetworkProjectConfig.asset`:
   - **Tick Rate**: `64` (64 ticks/s for responsive 2D)
   - **Delta Time**: `0.015625` (1/64)
   - **Physics2D Integration**: `Enabled`
   - **Area Of Interest**: disabled (all enemies need to stay active)
   - **Lag Compensation**: `Enabled`
   - **Replication Mode**: `EventualConsistency` for enemy/prop managers

2. Fusion `NetworkRunner` prefab (`Assets/Prefabs/Network/NetworkRunner.prefab`):
   - Add `NetworkRunner` component.
   - Set **Topology** to `ClientServer`.
   - Enable `ProvideInput` for the local player.

---

## 8  Prefab Setup

### 8a  Player Prefab (`Assets/Prefabs/Player/PlayerCharacter.prefab`)
- `NetworkObject` component (set **Object Provider** to Scene)
- `NetworkRigidbody2D` component
- `Rigidbody2D` – Body Type: Kinematic (movement driven by Fusion)
- `CapsuleCollider2D` – Direction: Horizontal, size matches sprite feet
- `PlayerController` script
- `PlayerShooter` script
- `SpriteRenderer` (character sprite)
- Child `GunPivot` empty GameObject for aim rotation
  - Child `GunSprite` SpriteRenderer

### 8b  NetworkRunner Prefab (`Assets/Prefabs/Network/NetworkRunner.prefab`)
- `NetworkRunner` component
- `NetworkBootstrap` script (or wire up in Bootstrap scene)

### 8c  Enemy Visual Prefab (`Assets/Prefabs/Enemies/EnemyVisual.prefab`)
- **Not** a NetworkObject – purely visual, driven by `EnemyManager`
- `SpriteRenderer`, `Animator`
- Spawned/despawned locally by `EnemyManager` to mirror networked state

### 8d  Prop Visual Prefab (`Assets/Prefabs/Props/BarrelVisual.prefab`)
- **Not** a NetworkObject
- `SpriteRenderer`, particle system for explosion
- Driven by `PropsManager`

---

## 9  Running the Game

### In-Editor (single machine)
1. Open `Bootstrap` scene.
2. Press **Play**.
3. The `NetworkBootstrap` starts as Host by default in the Editor.
4. Open a second instance via **ParrelSync** (recommended) or a standalone build to
   test client connections.

### Build
1. **File → Build Settings** – add all 3 scenes.
2. Select target platform (Windows/Mac/Linux).
3. Click **Build**.
4. First launched instance auto-hosts; subsequent instances connect as clients.

---

## 10  Recommended Third-Party Tools

| Tool | Purpose | Status |
|------|---------|--------|
| **ParrelSync** | Run multiple Unity Editor instances for local multiplayer testing | ✅ Pre-configured in `manifest.json` |
| **LeanTween** or **DOTween** | UI animations on the Ship screen | Install manually |
| **Odin Inspector** *(optional)* | Better Editor tooling for manager configs | Install manually |
