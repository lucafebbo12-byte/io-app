import { TILE_SIZE } from '../../shared/constants.js';

export class TerritoryMap {
  constructor(scene, mapW, mapH, colors) {
    this.scene = scene;
    this.mapW = mapW;
    this.mapH = mapH;
    this.colors = colors; // colorIndex -> hex string

    this._animOriginX = null;
    this._animOriginY = null;

    // Off-screen render texture for the full territory grid
    this.rt = scene.add
      .renderTexture(0, 0, mapW * TILE_SIZE, mapH * TILE_SIZE)
      .setOrigin(0, 0)
      .setDepth(0);

    this._tileKeyByOwner = {};
    this._ensureTileTextures();

    // Splat pool (short-lived additive images on top of rt)
    this._splatPool = [];
    this._splatLayer = scene.add.container(0, 0).setDepth(1);
  }

  setAnimOrigin(x, y) {
    this._animOriginX = x;
    this._animOriginY = y;
  }

  applyDelta(changedTiles) {
    this._drawTiles(changedTiles, null, true);
  }

  applyPredictedTiles(tileCoords, owner) {
    this._drawTiles(tileCoords, owner, false);
  }

  _ensureTileTextures() {
    for (const [idxStr, hex] of Object.entries(this.colors)) {
      const idx = Number(idxStr);
      const key = `tile_${idx}`;
      this._tileKeyByOwner[idx] = key;
      if (this.scene.textures.exists(key)) continue;

      const g = this.scene.make.graphics({ add: false });
      const base = parseInt(hex.replace('#', ''), 16);

      if (idx === 0) {
        // Neutral tile: dark grid look
        g.fillStyle(0x0d0d1a, 1);
        g.fillRect(0, 0, TILE_SIZE, TILE_SIZE);
        g.lineStyle(1, 0x1e1e2e, 0.8);
        g.beginPath();
        g.moveTo(0, 0);
        g.lineTo(TILE_SIZE, 0);
        g.moveTo(0, 0);
        g.lineTo(0, TILE_SIZE);
        g.strokePath();
      } else {
        g.fillStyle(base, 1);
        g.fillRect(0, 0, TILE_SIZE, TILE_SIZE);
      }

      // Shine triangle (skip for neutral)
      if (idx !== 0) {
        g.fillStyle(0xffffff, idx === 255 ? 0.18 : 0.35);
        g.fillTriangle(0, 0, TILE_SIZE, 0, 0, TILE_SIZE);
      }

      g.generateTexture(key, TILE_SIZE, TILE_SIZE);
      g.destroy();
    }
  }

  _drawTiles(tiles, ownerOverride = null, animate = false) {
    const cam = this.scene.cameras.main;
    const view = cam.worldView;
    const margin = 100;

    const hasOrigin = this._animOriginX !== null && this._animOriginY !== null;
    const ox = this._animOriginX ?? 0;
    const oy = this._animOriginY ?? 0;
    const maxDist = 80;
    const maxDistSq = maxDist * maxDist;

    for (const t of tiles) {
      const owner = ownerOverride ?? t.owner;
      const key = this._tileKeyByOwner[owner] ?? this._tileKeyByOwner[0];
      const x = t.x * TILE_SIZE;
      const y = t.y * TILE_SIZE;
      this.rt.draw(key, x, y);

      if (!animate) continue;

      const cx = x + TILE_SIZE * 0.5;
      const cy = y + TILE_SIZE * 0.5;

      // Only animate tiles near local player, and only near camera view.
      if (hasOrigin) {
        const dx = cx - ox;
        const dy = cy - oy;
        if (dx * dx + dy * dy > maxDistSq) continue;
      } else {
        continue;
      }

      if (
        cx < view.x - margin ||
        cy < view.y - margin ||
        cx > view.right + margin ||
        cy > view.bottom + margin
      ) {
        continue;
      }

      this._spawnSplat(key, x, y);
    }
  }

  _spawnSplat(tileKey, x, y) {
    const img =
      this._splatPool.pop() ||
      this.scene.add.image(0, 0, tileKey).setOrigin(0, 0).setBlendMode('ADD');

    if (!img.parentContainer) this._splatLayer.add(img);

    img.setTexture(tileKey);
    img.setPosition(x, y);
    img.setVisible(true);
    img.setAlpha(0.55);
    img.setScale(0.3);

    this.scene.tweens.add({
      targets: img,
      scaleX: 1,
      scaleY: 1,
      alpha: 0,
      duration: 80,
      onComplete: () => {
        img.setVisible(false);
        this._splatPool.push(img);
      }
    });
  }
}
