import { MAP_W, MAP_H, WALL_OWNER_INDEX, WALL_THICKNESS_TILES } from '../shared/constants.js';

export class GameMap {
  constructor() {
    // 0 = neutral, 1-10 = player color index
    this.grid = new Uint8Array(MAP_W * MAP_H);
    this.dirty = []; // [{x, y, owner}] per tick
  }

  idx(x, y) { return y * MAP_W + x; }

  getOwner(x, y) {
    if (x < 0 || y < 0 || x >= MAP_W || y >= MAP_H) return WALL_OWNER_INDEX;
    return this.grid[this.idx(x, y)];
  }

  seedQuadrants(quadrantOwners) {
    const halfW = Math.floor(MAP_W / 2);
    const halfH = Math.floor(MAP_H / 2);
    const tl = quadrantOwners[0] ?? 1;
    const tr = quadrantOwners[1] ?? 2;
    const bl = quadrantOwners[2] ?? 3;
    const br = quadrantOwners[3] ?? 4;

    for (let y = 0; y < MAP_H; y++) {
      for (let x = 0; x < MAP_W; x++) {
        const i = this.idx(x, y);

        const onWall =
          x < WALL_THICKNESS_TILES ||
          y < WALL_THICKNESS_TILES ||
          x >= MAP_W - WALL_THICKNESS_TILES ||
          y >= MAP_H - WALL_THICKNESS_TILES;

        if (onWall) {
          this.grid[i] = WALL_OWNER_INDEX;
          continue;
        }

        const left = x < halfW;
        const top = y < halfH;
        this.grid[i] = top ? (left ? tl : tr) : (left ? bl : br);
      }
    }
  }

  paint(x, y, ownerIndex) {
    if (x < 0 || y < 0 || x >= MAP_W || y >= MAP_H) return;
    const i = this.idx(x, y);
    if (this.grid[i] === WALL_OWNER_INDEX) return;
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
    const all = new Array(MAP_W * MAP_H);
    let k = 0;
    for (let y = 0; y < MAP_H; y++) {
      for (let x = 0; x < MAP_W; x++) {
        all[k++] = { x, y, owner: this.grid[this.idx(x, y)] };
      }
    }
    return all;
  }
}
