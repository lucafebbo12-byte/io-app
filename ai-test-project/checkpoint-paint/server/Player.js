import {
  MAP_W, MAP_H, TILE_SIZE, PLAYER_SPEED,
  INK_DRAIN, INK_REFILL, INK_MAX, SPAWN_POINTS, PLAYER_COLORS
} from '../shared/constants.js';

export class Player {
  constructor(id, index) {
    const spawn = SPAWN_POINTS[index % SPAWN_POINTS.length];
    this.id = id;
    this.index = index;
    this.color = PLAYER_COLORS[index % PLAYER_COLORS.length];
    this.colorIndex = index + 1; // tile owner ID (1-based, 0 = neutral)

    // world position in px
    this.x = spawn.x * TILE_SIZE;
    this.y = spawn.y * TILE_SIZE;
    this.spawnX = this.x;
    this.spawnY = this.y;

    this.aimAngle = 0;
    this.spraying = false;
    this.ink = INK_MAX;
    this.alive = true;
    this.checkpointAlive = true;
    this.score = 0;         // tile count
    this.isBot = false;

    // pending input from client
    this._dx = 0;
    this._dy = 0;
  }

  applyInput({ dx, dy, aimAngle, spraying }) {
    this._dx = dx || 0;
    this._dy = dy || 0;
    this.aimAngle = aimAngle || 0;
    this.spraying = !!spraying;
  }

  move() {
    if (!this.alive) return;
    const len = Math.hypot(this._dx, this._dy);
    if (len === 0) return;
    const nx = this._dx / len;
    const ny = this._dy / len;
    this.x = Math.max(0, Math.min((MAP_W * TILE_SIZE) - 1, this.x + nx * PLAYER_SPEED));
    this.y = Math.max(0, Math.min((MAP_H * TILE_SIZE) - 1, this.y + ny * PLAYER_SPEED));
  }

  tileX() { return Math.floor(this.x / TILE_SIZE); }
  tileY() { return Math.floor(this.y / TILE_SIZE); }

  updateInk(onOwnTile) {
    if (!this.alive) return;
    if (this.spraying && this.ink > 0) {
      this.ink = Math.max(0, this.ink - INK_DRAIN);
    }
    if (onOwnTile) {
      this.ink = Math.min(INK_MAX, this.ink + INK_REFILL);
    }
  }

  serialize() {
    return {
      id: this.id,
      x: this.x,
      y: this.y,
      aimAngle: this.aimAngle,
      spraying: this.spraying,
      color: this.color,
      ink: this.ink,
      alive: this.alive,
      checkpointAlive: this.checkpointAlive,
      score: this.score,
      isBot: this.isBot
    };
  }
}
