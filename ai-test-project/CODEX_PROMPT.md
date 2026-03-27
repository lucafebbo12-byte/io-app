# Checkpoint Paint.io — Codex Handoff Prompt

## Who you are
You are the **lead engineer** for Checkpoint Paint.io, a mobile-first multiplayer .io game (Splatoon x Paper.io x Dye Hard). The codebase is at `https://github.com/lucafebbo12-byte/io-app.git`. Clone it, work in `checkpoint-paint/`, and follow the agent playbook below.

---

## Game Overview
- **Genre**: Multiplayer mobile .io — spray-gun territory control
- **Win condition**: Last checkpoint standing OR most territory when 3-min timer runs out
- **Controls**: Twin-stick touch — left joystick = move, right joystick = aim + spray
- **Ink tank**: 0–100, drains while spraying, refills faster on own territory
- **Zone system**: Own territory = faster + ink refill boost. Enemy territory = slow + no refill
- **Checkpoints**: 1 per player, placed in corners. Destroy enemy checkpoint = they can never respawn

---

## Repo layout (what is already built)

```
checkpoint-paint/
├── server/
│   ├── index.js          <- Express + Socket.IO, port 3001
│   ├── GameRoom.js       <- Core game loop (20Hz tick, 10Hz broadcast)
│   ├── Player.js         <- Movement, ink, zone detection
│   ├── Bot.js            <- AI: orbit behavior, targets nearest enemy
│   ├── Map.js            <- Uint8Array territory grid + seedQuadrants()
│   ├── SprayCone.js      <- re-exports shared/sprayCone.js
│   └── Checkpoint.js     <- 3x3 tile checkpoint, destruction logic
├── src/
│   ├── main.js           <- Phaser 3 game config, scenes: GameScene + HUDScene
│   ├── scenes/
│   │   ├── GameScene.js  <- RenderTexture territory, Paper.io characters, camera
│   │   ├── HUDScene.js   <- Ink bar, scoreboard, kill feed, zone indicator
│   │   └── WinScene.js   <- Winner screen + play again
│   └── systems/
│       ├── JoystickInput.js    <- phaser3-rex-plugins dual virtual joystick
│       ├── NetworkManager.js   <- Socket.IO client, delta apply
│       ├── TerritoryMap.js     <- RenderTexture, shine tiles, applyDelta()
│       └── SprayEffect.js      <- Double emitter particles, predictLocalPaint()
├── shared/
│   ├── constants.js      <- ALL tuning values
│   └── sprayCone.js      <- getConeTiles() ray-cast cone geometry
├── vite.config.js        <- dev proxy: /socket.io -> localhost:3001
└── capacitor.config.json <- appId: io.checkpointpaint.game
```

---

## How to run

```bash
cd checkpoint-paint
npm install
# Terminal 1 — game server:
node server/index.js
# Terminal 2 — Vite dev:
npm run dev
# Open http://localhost:5173
```

---

## Key constants (shared/constants.js)

| Constant | Value | Notes |
|----------|-------|-------|
| MAP_W / MAP_H | 240 | tiles |
| TILE_SIZE | 4 | px per tile — 960x960 world |
| PLAYER_SPEED | 4.5 | px/tick |
| TICK_RATE | 20 | server Hz |
| BROADCAST_RATE | 10 | delta Hz |
| SPRAY_RANGE | 120 | px |
| SPRAY_HALF_ANGLE | PI/14.4 | +/-12.5 deg = 25 deg cone |
| INK_DRAIN | 2.5 | per tick while spraying |
| INK_REFILL | 5 | per tick on own territory |
| CHECKPOINT_RADIUS | 3 | tiles (3x3 area) |
| BOT_COUNT | 8 | AI bots |
| ROUND_TIME | 180 | seconds |

---

## What works right now

- Server: 20Hz game loop, 10Hz delta broadcast, bot AI (orbit + target)
- Territory: Uint8Array grid, pre-painted 4-color quadrants, dirty tracking
- Spray: server cone geometry + client particle FX + predictLocalPaint() client prediction
- Combat: player hit on spray, checkpoint destruction, respawn + elimination
- Win: last checkpoint wins OR time -> territory count
- HUD: ink bar with live value, kill feed, zone indicator, colored scoreboard %
- Visuals: Paper.io-style characters (gradient body, eyes, gun), pulsing checkpoint stars
- Countdown: 3-2-1 -> GO! before bots start
- Vite proxy + Capacitor config ready for mobile build

---

## Your tasks — visual polish to Dye Hard / Splatoon level

### Priority 1: Spray FX

**A. Spray arc indicator**
- Draw a thin transparent cone outline in front of the player showing aim direction
- Update it each frame to follow the right joystick angle
- File: `src/scenes/GameScene.js` — add a `Phaser.GameObjects.Graphics` overlay

**B. Paint splat on impact**
- When spray lands at max range, spawn a brief fading circle (paint blob) at that position
- Use a Phaser RenderTexture or a fading circle Graphics object at depth 1 (above territory)
- File: `src/systems/SprayEffect.js`

**C. Bigger spray stream**
- Increase particle `scale.start` to 1.4, `quantity` to 6 for main stream
- Add a glow by tinting a larger semi-transparent particle behind the stream

### Priority 2: Characters

**D. Spray recoil animation**
- When spraying: add a small tween `scaleX: 0.9` yoyo on the gun image at the body position
- File: `src/scenes/GameScene.js` — in the `update()` walking loop

**E. Ink-empty warning**
- When player ink < 20: rapidly flash the character alpha (0.5 <-> 1.0, 150ms interval)
- Stop flash when ink > 20
- File: `src/scenes/GameScene.js`

**F. Death burst**
- On `onPlayerHit()`: explode 20 particles in player color at their position, camera shake 200ms
- File: `src/scenes/GameScene.js`

### Priority 3: Map feel

**G. Neutral tile grid**
- Replace flat dark neutral tile with a subtle grid: dark background + slightly lighter grid lines every 4px
- File: `src/systems/TerritoryMap.js` — neutral Graphics object

**H. Tile cell borders**
- When drawing a tile, also draw a 1px darker border so cells look distinct
- File: `src/systems/TerritoryMap.js` — in the pre-built `_tiles` Graphics loop

### Priority 4: Mobile

**I. Android build**
```bash
cd checkpoint-paint
npm run build
npx cap add android
npx cap sync
npx cap run android
```

**J. App icon**
- 1024x1024: spray gun with colorful splatter, vivid colors
- Use `@capacitor/assets` CLI to generate all sizes

---

## Agent playbook

| Task | Agent |
|------|-------|
| Implement Phaser 3 FX, particles, tweens | `coder` |
| Multi-file architecture decisions | `apex` |
| Long autonomous multi-file sessions | `claudette` |
| Research Phaser 3 / Capacitor docs | `general-purpose` |

---

## Rules when editing

1. Always `Read` a file before editing it
2. Run `npm run build` in `checkpoint-paint/` after each change — build must stay clean
3. Test server + browser before committing
4. `git add <specific files>` — never `git add .`
5. Commit message format: `Paint: <what changed>`
6. Push after each commit: `git push origin main`

---

## Phaser 3 patterns

**Burst particles:**
```js
const burst = this.add.particles(x, y, 'dot', {
  speed: { min: 60, max: 180 },
  scale: { start: 0.9, end: 0 },
  lifespan: 450,
  quantity: 20,
  tint: parseInt(color.replace('#',''), 16)
});
this.time.delayedCall(600, () => burst.destroy());
```

**Camera shake:**
```js
this.cameras.main.shake(200, 0.012);
```

**Tween yoyo:**
```js
this.tweens.add({
  targets: sprite,
  scaleX: 0.85, scaleY: 1.1,
  duration: 80,
  yoyo: true,
  ease: 'Sine.easeOut'
});
```

**Emit event to HUD:**
```js
// GameScene
this.events.emit('my_event', { data });
// HUDScene.create()
gameScene.events.on('my_event', ({ data }) => { ... });
```

---

## Common issues

| Issue | Fix |
|-------|-----|
| Port 3001 in use | `taskkill /IM node.exe /F` (Windows) |
| Vite port taken | Kill node processes, Vite picks next port |
| Particles behind territory | Set emitter `depth: 4` (territory is depth 0) |
| RenderTexture flickers | Only call `applyDelta()` on changed tiles |
| Socket not connecting | Check vite.config.js proxy target = `http://localhost:3001` |

---

## Target aesthetic

Reference: **Dye Hard** (iOS) + **Paper.io** + **Splatoon**

- Paper.io: simple boxy cartoon characters, flat vivid colors
- Dye Hard: spray gun particles, paint blobs, splatter on walls
- Splatoon: cone spray, ink territory mechanics, saturated neon palette

Current palette: `#FF2D2D #0099FF #00EE44 #FF69B4 #FF8800 #AA00FF #00FFCC #FFE000 #00CCFF #FF3399`

Keep colors vivid. Tiles should look glossy/shiny (white triangle highlight already in TerritoryMap).
