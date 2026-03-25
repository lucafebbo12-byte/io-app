import { MAP_W, MAP_H } from '../shared/constants.js';

export class GameMap {
  constructor() {
    // 0 = neutral, 1-10 = player color index
    this.grid = new Uint8Array(MAP_W * MAP_H);
    this.dirty = []; // [{x, y, owner}] per tick
  }

  idx(x, y) { return y * MAP_W + x; }

  getOwner(x, y) { return this.grid[this.idx(x, y)]; }

  paint(x, y, ownerIndex) {
    const i = this.idx(x, y);
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
}
