export const MAP_W = 240;
export const MAP_H = 240;
export const TILE_SIZE = 4;          // px per tile -> 960x960 world
export const PLAYER_SPEED = 4.5;     // px per tick
export const TICK_RATE = 20;         // server Hz
export const BROADCAST_RATE = 10;    // delta Hz
export const SPRAY_RANGE = 120;      // px
export const SPRAY_HALF_ANGLE = Math.PI / 14.4; // +/-12.5deg -> 25deg cone
export const INK_DRAIN = 2.5;        // per tick while spraying
export const INK_REFILL = 5;         // per tick on own territory (baseline)
export const INK_MAX = 100;
export const RESPAWN_DELAY = 2000;   // ms
export const ROUND_TIME = 180;       // seconds
export const MAX_PLAYERS = 10;
export const BOT_COUNT = 8;
export const CHECKPOINT_RADIUS = 3;  // tiles (radius, inclusive)

// Map border wall (1 tile thick)
export const WALL_OWNER_INDEX = 255; // reserved owner index for impassable wall tiles
export const WALL_COLOR = '#555555';
export const WALL_THICKNESS_TILES = 1;

// Vivid palette (10 players/bots)
export const PLAYER_COLORS = [
  '#FF2D2D', '#0099FF', '#00EE44', '#FF69B4',
  '#FF8800', '#AA00FF', '#00FFCC', '#FFE000',
  '#00CCFF', '#FF3399'
];

// First 4 checkpoints are placed in corners (tile coords).
export const CORNER_CHECKPOINTS = [
  { x: 15, y: 15 },   // top-left
  { x: 225, y: 15 },  // top-right
  { x: 15, y: 225 },  // bottom-left
  { x: 225, y: 225 }  // bottom-right
];

export const SPAWN_POINTS = [
  { x: 15,  y: 15  }, { x: 225, y: 15  },
  { x: 15,  y: 225 }, { x: 225, y: 225 },
  { x: 120, y: 40  }, { x: 120, y: 200 },
  { x: 40,  y: 120 }, { x: 200, y: 120 },
  { x: 80,  y: 100 }, { x: 160, y: 140 }
];

// Zone tuning (server-side modifiers)
export const ZONE_SPEED_OWN = 1.4;
export const ZONE_SPEED_ENEMY = 0.65;
export const ZONE_DRAIN_OWN = 0.6;
export const ZONE_DRAIN_ENEMY = 1.5;
export const ZONE_REFILL_ENEMY = 0;
