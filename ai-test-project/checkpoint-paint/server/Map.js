import {
  MAP_W, MAP_H,
  WALL_OWNER_INDEX,
  CHECKPOINT_SPAWN_RADIUS,
  isWall
} from '../shared/constants.js';

export class GameMap {
  constructor() {
    // 0 = neutral, 1-10 = player color index, 255 = wall/void
    this.grid = new Uint8Array(MAP_W * MAP_H);
    this.dirty = []; // [{x, y, owner}] per tick
  }

  idx(x, y) { return y * MAP_W + x; }

  getOwner(x, y) {
    if (x < 0 || y < 0 || x >= MAP_W || y >= MAP_H) return WALL_OWNER_INDEX;
    return this.grid[this.idx(x, y)];
  }

  /**
   * Seeds the map as a neutral blob-shaped arena.
   * - Tiles inside the blob shape start as 0 (neutral).
   * - Tiles outside the blob are set to WALL_OWNER_INDEX (void/impassable).
   * - A circular spawn zone is pre-painted around each player's starting tile.
   *
   * @param {Array<{tileX, tileY, ownerIndex}>} spawnZones
   */
  seedNeutral(spawnZones) {
    // Step 1: fill entire grid — playable tiles = neutral, wall tiles = WALL_OWNER_INDEX
    for (let y = 0; y < MAP_H; y++) {
      for (let x = 0; x < MAP_W; x++) {
        this.grid[this.idx(x, y)] = isWall(x, y) ? WALL_OWNER_INDEX : 0;
      }
    }

    // Step 2: paint a circular base zone around each spawn point
    const r = CHECKPOINT_SPAWN_RADIUS;
    const rSq = r * r;
    for (const { tileX, tileY, ownerIndex } of spawnZones) {
      for (let dy = -r; dy <= r; dy++) {
        for (let dx = -r; dx <= r; dx++) {
          if (dx * dx + dy * dy > rSq) continue;
          const x = tileX + dx;
          const y = tileY + dy;
          // Only paint playable (non-wall) tiles
          if (!isWall(x, y) && x >= 0 && y >= 0 && x < MAP_W && y < MAP_H) {
            this.grid[this.idx(x, y)] = ownerIndex;
          }
        }
      }
    }
  }

  paint(x, y, ownerIndex) {
    if (x < 0 || y < 0 || x >= MAP_W || y >= MAP_H) return;
    const i = this.idx(x, y);
    if (this.grid[i] === WALL_OWNER_INDEX) return; // can't paint void/wall
    if (this.grid[i] !== ownerIndex) {
      this.grid[i] = ownerIndex;
      this.dirty.push({ x, y, owner: ownerIndex });
    }
  }

  countTiles(ownerIndex) {
    let count = 0;
    for (let i = 0; i < this.grid.length; i++) {
      if (this.grid[i] === ownerIndex) count++;
    }
    return count;
  }

  flushDirty() {
    const d = this.dirty;
    this.dirty = [];
    return d;
  }

  serializeAll() {
    const all = [];
    for (let y = 0; y < MAP_H; y++) {
      for (let x = 0; x < MAP_W; x++) {
        all.push({ x, y, owner: this.grid[this.idx(x, y)] });
      }
    }
    return all;
  }

  /**
   * Returns a flat array [x0,y0,owner0, x1,y1,owner1, ...] containing only
   * non-neutral, non-wall tiles. Dramatically smaller than serializeAll() for
   * early-game state where most tiles are neutral.
   */
  serializeBinary() {
    const nonNeutral = [];
    for (let y = 0; y < MAP_H; y++) {
      for (let x = 0; x < MAP_W; x++) {
        const owner = this.grid[y * MAP_W + x];
        if (owner !== 0 && owner !== 255) { // skip neutral + walls
          nonNeutral.push(x, y, owner); // 3 numbers per tile
        }
      }
    }
    return nonNeutral; // flat array [x0,y0,owner0, x1,y1,owner1, ...]
  }
}
