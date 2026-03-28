import { TILE_SIZE, BULLET_SPEED, BULLET_MAX_TICKS, WALL_OWNER_INDEX } from '../shared/constants.js';

let _bulletId = 0;

export class Bullet {
  constructor(ownerId, ownerIndex, x, y, angle) {
    this.id = `b_${++_bulletId}`;
    this.ownerId = ownerId;
    this.ownerIndex = ownerIndex;
    this.x = x;
    this.y = y;
    this.angle = angle;
    this.vx = Math.cos(angle) * BULLET_SPEED;
    this.vy = Math.sin(angle) * BULLET_SPEED;
    this.alive = true;
    this.age = 0;
    this.hitPlayerId = null; // set if it hit a player
  }

  update(map) {
    if (!this.alive) return;
    this.x += this.vx;
    this.y += this.vy;
    this.age++;
    const tx = Math.floor(this.x / TILE_SIZE);
    const ty = Math.floor(this.y / TILE_SIZE);
    if (this.age > BULLET_MAX_TICKS || map.getOwner(tx, ty) === WALL_OWNER_INDEX) {
      this.alive = false;
    }
  }

  serialize() {
    return { id: this.id, x: this.x, y: this.y, ownerId: this.ownerId };
  }
}
