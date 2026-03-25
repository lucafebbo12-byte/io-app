export const MAP_W = 240;
export const MAP_H = 240;
export const TILE_SIZE = 4;          // px per tile → 960×960 world
export const PLAYER_SPEED = 4.5;     // px per tick
export const TICK_RATE = 20;         // server Hz
export const BROADCAST_RATE = 10;    // delta Hz
export const SPRAY_RANGE = 120;      // px
export const SPRAY_HALF_ANGLE = Math.PI / 14.4; // ±12.5° → 25° cone
export const INK_DRAIN = 2.5;        // per tick while spraying
export const INK_REFILL = 5;         // per tick on own territory
export const INK_MAX = 100;
export const RESPAWN_DELAY = 2000;   // ms
export const ROUND_TIME = 180;       // seconds
export const MAX_PLAYERS = 10;
export const BOT_COUNT = 8;
export const CHECKPOINT_RADIUS = 3;  // tiles (3×3 area)

export const PLAYER_COLORS = [
  '#FF4136', '#0074D9', '#2ECC40', '#FF69B4',
  '#FF851B', '#B10DC9', '#01FF70', '#FFDC00',
  '#7FDBFF', '#F012BE'
];

export const SPAWN_POINTS = [
  { x: 60,  y: 60  }, { x: 180, y: 60  },
  { x: 60,  y: 180 }, { x: 180, y: 180 },
  { x: 120, y: 40  }, { x: 120, y: 200 },
  { x: 40,  y: 120 }, { x: 200, y: 120 },
  { x: 80,  y: 100 }, { x: 160, y: 140 }
];
