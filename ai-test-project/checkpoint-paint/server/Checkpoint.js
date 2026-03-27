import { TILE_SIZE, CHECKPOINT_RADIUS } from '../shared/constants.js';

export class Checkpoint {
  constructor(player, tileX = null, tileY = null) {
    this.ownerId = player.id;
    this.ownerIndex = player.colorIndex;
    // checkpoint at given tile position (or spawn tile position)
    this.tileX = tileX ?? Math.floor(player.spawnX / TILE_SIZE);
    this.tileY = tileY ?? Math.floor(player.spawnY / TILE_SIZE);
    this.alive = true;
  }

  /** Returns true if tile (tx, ty) is inside this checkpoint's 3×3 area */
  containsTile(tx, ty) {
    return (
      Math.abs(tx - this.tileX) <= CHECKPOINT_RADIUS &&
      Math.abs(ty - this.tileY) <= CHECKPOINT_RADIUS
    );
  }

  /** Check if any changedTile belongs to an enemy and overlaps this checkpoint */
  checkDestruction(changedTiles, ownerIndex) {
    if (!this.alive) return false;
    for (const t of changedTiles) {
      if (t.owner !== 0 && t.owner !== ownerIndex && this.containsTile(t.x, t.y)) {
        this.alive = false;
        return true;
      }
    }
    return false;
  }

  serialize() {
    return {
      ownerId: this.ownerId,
      tileX: this.tileX,
      tileY: this.tileY,
      alive: this.alive
    };
  }
}
