import { TILE_SIZE } from '../../shared/constants.js';

export class TerritoryMap {
  constructor(scene, mapW, mapH, colors) {
    this.scene = scene;
    this.mapW = mapW;
    this.mapH = mapH;
    this.colors = colors; // colorIndex → hex string

    // Off-screen render texture for the full territory grid
    this.rt = scene.add.renderTexture(0, 0, mapW * TILE_SIZE, mapH * TILE_SIZE)
      .setOrigin(0, 0)
      .setDepth(0);

    // Pre-build colored rectangles for each player index
    this._tiles = {};
    for (const [idx, hex] of Object.entries(colors)) {
      const g = scene.add.graphics();
      g.fillStyle(parseInt(hex.replace('#', ''), 16), 1);
      g.fillRect(0, 0, TILE_SIZE, TILE_SIZE);
      this._tiles[idx] = g;
    }
    // Neutral tile (erase = transparent)
    const neutral = scene.add.graphics();
    neutral.fillStyle(0x1a1a2e, 1);
    neutral.fillRect(0, 0, TILE_SIZE, TILE_SIZE);
    this._tiles[0] = neutral;
  }

  applyDelta(changedTiles) {
    this._drawTiles(changedTiles);
  }

  applyPredictedTiles(tileCoords, owner) {
    this._drawTiles(tileCoords, owner);
  }

  _drawTiles(tiles, ownerOverride = null) {
    for (const t of tiles) {
      const owner = ownerOverride ?? t.owner;
      const gfx = this._tiles[owner] || this._tiles[0];
      this.rt.draw(gfx, t.x * TILE_SIZE, t.y * TILE_SIZE);
    }
  }
}
