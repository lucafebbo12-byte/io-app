export const MAP_W = 240;
export const MAP_H = 240;
export const TILE_SIZE = 8;          // px per tile -> 1920x1920 world
export const PLAYER_SPEED = 6.5;     // px per tick (faster = feels more Paper.io)
export const TICK_RATE = 20;         // server Hz
export const BROADCAST_RATE = 10;    // delta Hz
export const SPRAY_RANGE = 200;      // px (bigger spray coverage)
export const SPRAY_HALF_ANGLE = Math.PI / 7.2; // +/-25deg -> 50deg wide cone
export const INK_DRAIN = 2.5;        // per tick while spraying
export const INK_REFILL = 5;         // per tick on own territory (baseline)
export const INK_MAX = 100;
export const RESPAWN_DELAY = 2000;   // ms
export const ROUND_TIME = 180;       // seconds
export const MAX_PLAYERS = 10;
export const BOT_COUNT = 3;          // 3 bots + 1 human = 4 corner players
export const CHECKPOINT_RADIUS = 3;  // tiles (radius, inclusive)
export const CHECKPOINT_HP = 25;     // spray hits to destroy a checkpoint
export const CHECKPOINT_SPAWN_RADIUS = 8; // pre-painted circle radius (tiles) around each spawn

// Map border wall
export const WALL_OWNER_INDEX = 255;
export const WALL_COLOR = '#222222'; // dark walls (Die Hard aesthetic)
export const WALL_THICKNESS_TILES = 2;

// Bullet / HP constants
export const BULLET_SPEED = 14;       // px per tick
export const BULLET_DAMAGE = 1;
export const PLAYER_MAX_HP = 3;
export const BULLET_MAX_TICKS = 120;  // bullet lifetime in ticks
export const BULLET_PAINT_RADIUS = 2; // tiles painted on bullet impact

// Vivid palette (10 players/bots)
export const PLAYER_COLORS = [
  '#FF2D2D', '#0099FF', '#00EE44', '#FF69B4',
  '#FF8800', '#AA00FF', '#00FFCC', '#FFE000',
  '#00CCFF', '#FF3399'
];

// ─── Rectangular Map ───────────────────────────────────────────────────────
// The playable arena is a rectangle with 2-tile thick border walls and a
// central cross with openings to create four quadrant rooms.
//
/** Returns true if tile (tx, ty) is a wall (impassable) */
export function isWall(tx, ty) {
  // Border walls (2 tiles thick on each edge)
  if (tx < 2 || tx >= MAP_W - 2 || ty < 2 || ty >= MAP_H - 2) return true;
  // Central cross walls with openings
  const cx = 120;
  const cy = 120;
  const gap = 22;
  if (ty >= cy - 2 && ty <= cy + 1 && (tx < cx - gap || tx > cx + gap)) return true;
  if (tx >= cx - 2 && tx <= cx + 1 && (ty < cy - gap || ty > cy + gap)) return true;
  return false;
}

// ─── Checkpoints ───────────────────────────────────────────────────────────
// 4 checkpoints — one per corner. Indices 0-2 = bots, index 3 = human player.
export const CORNER_CHECKPOINTS = [
  { x: 8,   y: 8   },  // top-left corner (Bot InkBot)
  { x: 231, y: 8   },  // top-right corner (Bot SplatBot)
  { x: 8,   y: 231 },  // bottom-left corner (Bot PaintBot)
  { x: 231, y: 231 },  // bottom-right corner (human player)
];

// Spawn positions (tile coords) — index matches checkpoint index
export const SPAWN_POINTS = [
  { x: 8,   y: 8   },  // top-left
  { x: 231, y: 8   },  // top-right
  { x: 8,   y: 231 },  // bottom-left
  { x: 231, y: 231 },  // bottom-right
  // Mid-area fallbacks for extra players
  { x: 60,  y: 60  }, { x: 180, y: 60 }, { x: 60, y: 180 }, { x: 180, y: 180 },
  { x: 120, y: 60  }, { x: 120, y: 180 },
];

// ─── Zone tuning ───────────────────────────────────────────────────────────
export const ZONE_SPEED_OWN = 1.4;
export const ZONE_SPEED_ENEMY = 0.65;
export const ZONE_DRAIN_OWN = 0.6;
export const ZONE_DRAIN_ENEMY = 1.5;
export const ZONE_REFILL_ENEMY = 0;
export const ZONE_REFILL_OWN = 2;
