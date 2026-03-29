# Paint Game — Unity 6 LTS Setup Guide

## 1. Open in Unity Hub

1. Open **Unity Hub**
2. Click **Add project from disk**
3. Select this folder: `paint-game/`
4. Unity Hub will detect it needs **Unity 6 LTS** (6000.0.x)
5. If not installed: Unity Hub → Installs → Install Editor → Unity 6 LTS
   - Add modules: **Android Build Support**, **Android SDK & NDK**, **OpenJDK**, **Windows Build Support (IL2CPP)**

## 2. First Launch

Unity will import all packages from `Packages/manifest.json` automatically:
- Universal Render Pipeline (URP)
- Cinemachine 3
- TextMeshPro
- Input System
- Unity Collections (for NativeArray)

When prompted about the **new Input System** → click **Yes** (restart required).

## 3. Scene Setup (Manual — do once)

After Unity opens, create the **Game** scene manually:

### A. URP Setup
- `Edit → Project Settings → Graphics → Scriptable Render Pipeline Settings`
- Assign the `URP_2D_Renderer.asset` from `Assets/Settings/`

### B. Create Render Texture
- `Assets/_Project/Textures/` → right-click → `Create → Render Texture`
- Name: `PaintRT`
- Size: 1920 × 1920
- Format: `RGBA32`
- No mipmaps
- Depth buffer: None

### C. Floor Quad Setup
- Create a `Quad` in the scene (`GameObject → 3D → Quad`)
- Scale it to `(1920, 1920, 1)`
- Position: `(960, 960, 0)` (centred on map)
- Create a new **Unlit material** → assign `_PaintRT` as its texture
- Set the material on the Quad's MeshRenderer

### D. GameManager GameObject
Create an empty `GameManager` with these components:
- `GameManager`
- `MatchManager`
- `ScoreTracker`
- Drag `TerritorySystem` child to it (or create separate):

### E. TerritorySystem GameObject
Create empty `TerritorySystem`:
- `TerritoryMap`
- `TerritoryRenderer` → assign `PaintRT`, assign stamp material, assign Floor Quad
- `SpawnZoneSeeder`

### F. PoolRegistry GameObject
Create empty `PoolRegistry`:
- `PoolRegistry` component
- Assign prefab references

### G. Checkpoints (6 GameObjects)
For each player (1-6), create `Checkpoint` at spawn tile world position:
- Spawn tile → world: `tile * 8 + 4` (e.g. tile 20,20 → world 164,164)
- `CheckpointController` component
- `CheckpointVisuals` component
- `LineRenderer` for HP ring

### H. Camera
- Create `CinemachineCamera`
- Body: `CinemachinePositionComposer`
- Lens: Orthographic, Size: 9
- Follow target: Player (set after spawn)
- Add `CinemachineConfiner2D` with a PolygonCollider2D matching world bounds
- Add `CameraController` on a separate GO with `CinemachineImpulseSource`

### I. HUD Canvas
- Canvas (Screen Space - Overlay)
- `HUDController` component with references:
  - TimerUI (TMP text)
  - InkBarUI (Image fill)
  - HPDisplayUI (3 Images)
  - ScoreboardUI
  - CountdownUI
  - KillFeedUI

## 4. Weapon ScriptableObjects

Create two assets:
- `Assets/_Project/ScriptableObjects/Shotgun.asset` → `WeaponConfigSO` → context menu "Set Shotgun Defaults"
- `Assets/_Project/ScriptableObjects/AK.asset` → `WeaponConfigSO` → context menu "Set AK Defaults"

## 5. Player Prefab

- Body: Circle sprite (tinted at runtime)
- Gun pivot child → sprite
- Components: `PlayerController`, `PlayerInput`, `PlayerStats`, `PlayerVisuals`, `ShotgunWeapon` (or `AKWeapon`), `AimConeRenderer`
- Sorting Layer: **Players**, Order: 5

## 6. Bot Prefab

- Variant of Player prefab
- Replace `PlayerInput` with `BotController`
- Same weapon components

## 7. Build Settings (Android)

- `File → Build Settings → Android`
- Switch Platform
- `Player Settings`:
  - Scripting Backend: **IL2CPP**
  - Target Architectures: **ARM64**
  - Texture Compression: **ASTC**
  - `Application.targetFrameRate = 60` (set in any Awake)
  - Screen orientation: **Landscape**

## 8. Test

Press **Play** in Unity Editor. You should see:
1. White floor with coloured corner zones
2. 3-2-1-GO countdown
3. Human player moves with WASD
4. Mouse aim + left-click sprays paint
5. 5 bots orbit, roam, attack
6. Checkpoints at corners
7. 90 second timer
8. Win screen at match end

## Folder Reference

```
Assets/_Project/Scripts/
  Core/         — GameManager, GameConstants, GameEvents, SceneLoader
  Territory/    — TerritoryMap, TerritoryRenderer, SpawnZoneSeeder
  Player/       — PlayerController, PlayerInput, PlayerStats, PlayerVisuals, PlayerSpawnManager
  Weapons/      — WeaponBase, ShotgunWeapon, AKWeapon, SprayCone, BulletProjectile, WeaponConfigSO
  Checkpoint/   — CheckpointController, CheckpointVisuals
  AI/           — BotController
  Pooling/      — ObjectPool, PoolRegistry
  FX/           — SplatEffect, ImpactFlash, DeathBurst, AimConeRenderer
  UI/           — HUDController, TimerUI, InkBarUI, HPDisplayUI, ScoreboardUI,
                   ScoreRowUI, CountdownUI, KillFeedUI, VirtualJoystick, WinScreenUI
  Camera/       — CameraController
  Match/        — MatchManager, ScoreTracker
```
