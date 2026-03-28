import {
  MAP_W, MAP_H, TILE_SIZE, PLAYER_SPEED,
  INK_DRAIN, INK_REFILL, INK_MAX, SPAWN_POINTS, PLAYER_COLORS,
  ZONE_SPEED_OWN, ZONE_SPEED_ENEMY, ZONE_DRAIN_OWN, ZONE_DRAIN_ENEMY, ZONE_REFILL_ENEMY,
  WALL_OWNER_INDEX, PLAYER_MAX_HP
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
    this.hp = PLAYER_MAX_HP;
    this.checkpointAlive = true;
    this.score = 0;         // tile count
    this.isBot = false;
    this.zoneMultiplier = { speed: 1.0, drainMult: 1.0, refillMult: 1.0 };
    this.zoneType = 'neutral'; // 'own', 'enemy', 'neutral'

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

  /**
   * Move with blob-boundary wall collision + sliding.
   * @param {GameMap|null} map
   */
  move(map) {
    if (!this.alive) return;
    const len = Math.hypot(this._dx, this._dy);
    if (len === 0) return;
    const nx = this._dx / len;
    const ny = this._dy / len;
    const speed = PLAYER_SPEED * this.zoneMultiplier.speed;

    const newX = this.x + nx * speed;
    const newY = this.y + ny * speed;

    if (!map) {
      // Fallback: simple rectangular clamp
      this.x = Math.max(4, Math.min((MAP_W - 1) * TILE_SIZE - 1, newX));
      this.y = Math.max(4, Math.min((MAP_H - 1) * TILE_SIZE - 1, newY));
      return;
    }

    const tX = (v) => Math.floor(v / TILE_SIZE);
    const tY = (v) => Math.floor(v / TILE_SIZE);
    const blocked = (wx, wy) => map.getOwner(tX(wx), tY(wy)) === WALL_OWNER_INDEX;

    if (!blocked(newX, newY)) {
      this.x = newX;
      this.y = newY;
    } else if (!blocked(newX, this.y)) {
      // Slide along X
      this.x = newX;
    } else if (!blocked(this.x, newY)) {
      // Slide along Y
      this.y = newY;
    }
    // else: fully blocked — don't move
  }

  tileX() { return Math.floor(this.x / TILE_SIZE); }
  tileY() { return Math.floor(this.y / TILE_SIZE); }

  takeDamage(amount) {
    this.hp = Math.max(0, this.hp - amount);
    return this.hp <= 0; // returns true if died
  }

  updateZone(map) {
    if (!map) {
      this.zoneMultiplier = { speed: 1, drainMult: 1, refillMult: 1 };
      this.zoneType = 'neutral';
      return;
    }

    const tileOwner = map.getOwner(this.tileX(), this.tileY());
    if (tileOwner === this.colorIndex) {
      this.zoneMultiplier = { speed: ZONE_SPEED_OWN, drainMult: ZONE_DRAIN_OWN, refillMult: 2 };
      this.zoneType = 'own';
    } else if (tileOwner !== 0) {
      this.zoneMultiplier = {
        speed: ZONE_SPEED_ENEMY,
        drainMult: ZONE_DRAIN_ENEMY,
        refillMult: ZONE_REFILL_ENEMY
      };
      this.zoneType = 'enemy';
    } else {
      this.zoneMultiplier = { speed: 1, drainMult: 1, refillMult: 1 };
      this.zoneType = 'neutral';
    }
  }

  updateInk() {
    if (!this.alive) return;

    if (this.spraying && this.ink > 0) {
      this.ink = Math.max(0, this.ink - INK_DRAIN * this.zoneMultiplier.drainMult);
    }
    // always refill (capped), scaled by refillMult
    if (this.zoneMultiplier.refillMult > 0) {
      this.ink = Math.min(INK_MAX, this.ink + INK_REFILL * this.zoneMultiplier.refillMult);
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
      hp: this.hp,
      checkpointAlive: this.checkpointAlive,
      score: this.score,
      isBot: this.isBot,
      zoneType: this.zoneType
    };
  }
}
