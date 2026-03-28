import { TILE_SIZE, CHECKPOINT_RADIUS, CHECKPOINT_HP } from '../shared/constants.js';

export class Checkpoint {
  constructor(player, tileX = null, tileY = null) {
    this.ownerId = player.id;
    this.ownerIndex = player.colorIndex;
    this.tileX = tileX ?? Math.floor(player.spawnX / TILE_SIZE);
    this.tileY = tileY ?? Math.floor(player.spawnY / TILE_SIZE);
    this.alive = true;
    this.hp = CHECKPOINT_HP;
    this.maxHp = CHECKPOINT_HP;
  }

  /** Returns true if tile (tx, ty) is inside this checkpoint's protected radius */
  containsTile(tx, ty) {
    return (
      Math.abs(tx - this.tileX) <= CHECKPOINT_RADIUS &&
      Math.abs(ty - this.tileY) <= CHECKPOINT_RADIUS
    );
  }

  /**
   * Count enemy hits on this checkpoint this tick and reduce HP.
   * Returns { destroyed: bool, damaged: bool }
   */
  checkDestruction(changedTiles, ownerIndex) {
    if (!this.alive) return { destroyed: false, damaged: false };

    let hits = 0;
    for (const t of changedTiles) {
      if (t.owner !== 0 && t.owner !== ownerIndex && this.containsTile(t.x, t.y)) {
        hits++;
      }
    }

    if (hits === 0) return { destroyed: false, damaged: false };

    this.hp = Math.max(0, this.hp - hits);
    if (this.hp <= 0) {
      this.alive = false;
      return { destroyed: true, damaged: true };
    }
    return { destroyed: false, damaged: true };
  }

  serialize() {
    return {
      ownerId: this.ownerId,
      tileX: this.tileX,
      tileY: this.tileY,
      alive: this.alive,
      hp: this.hp,
      maxHp: this.maxHp
    };
  }
}
