import { TILE_SIZE } from '../../shared/constants.js';

const WALL_OWNER = 255;
const NEUTRAL_OWNER = 0;
// Soft brush is 16x16; half = 8. We center it on the tile center.
const BRUSH_SIZE = 16;
const BRUSH_HALF = BRUSH_SIZE / 2;

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

    this._createSoftBrush(scene);
    this._createWallTexture(scene);
    this._createNeutralTexture(scene);

    // Splat pool (short-lived additive images on top of rt)
    this._splatPool = [];
    this._splatLayer = scene.add.container(0, 0).setDepth(1);
  }

  /**
   * Creates a 16x16 radial-gradient white circle texture named 'softBrush'.
   * Tint it with player color at draw time for fluid soft-edged paint.
   */
  _createSoftBrush(scene) {
    if (scene.textures.exists('softBrush')) return;
    const g = scene.make.graphics({ add: false });
    const half = BRUSH_HALF;
    // Draw concentric filled circles from edge inward so center is opaque.
    for (let r = half; r >= 0; r -= 1) {
      const alpha = 1.0 - (r / half); // 0 at edge, 1 at center
      g.fillStyle(0xffffff, alpha * 0.9);
      g.fillCircle(half, half, r);
    }
    g.generateTexture('softBrush', BRUSH_SIZE, BRUSH_SIZE);
    g.destroy();
  }

  /**
   * Solid dark wall tile texture.
   */
  _createWallTexture(scene) {
    if (scene.textures.exists('wallTile')) return;
    const g = scene.make.graphics({ add: false });
    g.fillStyle(0x1a1a2e, 1);
    g.fillRect(0, 0, TILE_SIZE, TILE_SIZE);
    g.lineStyle(1, 0x16213e, 1);
    g.strokeRect(0, 0, TILE_SIZE, TILE_SIZE);
    g.generateTexture('wallTile', TILE_SIZE, TILE_SIZE);
    g.destroy();
  }

  /**
   * Neutral floor tile — dark grey with subtle grid lines.
   */
  _createNeutralTexture(scene) {
    if (scene.textures.exists('neutralTile')) return;
    const g = scene.make.graphics({ add: false });
    g.fillStyle(0x3a3a3a, 1);
    g.fillRect(0, 0, TILE_SIZE, TILE_SIZE);
    g.lineStyle(1, 0x2a2a2a, 1);
    g.beginPath();
    g.moveTo(TILE_SIZE - 1, 0);
    g.lineTo(TILE_SIZE - 1, TILE_SIZE);
    g.moveTo(0, TILE_SIZE - 1);
    g.lineTo(TILE_SIZE, TILE_SIZE - 1);
    g.strokePath();
    g.generateTexture('neutralTile', TILE_SIZE, TILE_SIZE);
    g.destroy();
  }

  /** Returns the hex color string for an owner index, or null if unknown. */
  _colorForOwner(owner) {
    return this.colors[owner] ?? null;
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
      const tx = t.x * TILE_SIZE;
      const ty = t.y * TILE_SIZE;

      if (owner === WALL_OWNER) {
        // Walls: draw solid dark tile
        this.rt.draw('wallTile', tx, ty);
        continue;
      }

      if (owner === NEUTRAL_OWNER) {
        // Neutral: erase paint back to neutral floor tile
        this.rt.draw('neutralTile', tx, ty);
        continue;
      }

      // Color tile: stamp soft brush centered on tile, tinted with player color
      const hexColor = this._colorForOwner(owner);
      if (!hexColor) {
        this.rt.draw('neutralTile', tx, ty);
        continue;
      }
      const colorInt = parseInt(hexColor.replace('#', ''), 16);
      // Center the 16px brush on the 8px tile center
      const cx = tx + TILE_SIZE / 2 - BRUSH_HALF;
      const cy = ty + TILE_SIZE / 2 - BRUSH_HALF;
      this.rt.draw('softBrush', cx, cy, 1, colorInt);

      if (!animate) continue;

      const tileCX = tx + TILE_SIZE * 0.5;
      const tileCY = ty + TILE_SIZE * 0.5;

      if (hasOrigin) {
        const dx = tileCX - ox;
        const dy = tileCY - oy;
        if (dx * dx + dy * dy > maxDistSq) continue;
      } else {
        continue;
      }

      if (
        tileCX < view.x - margin ||
        tileCY < view.y - margin ||
        tileCX > view.right + margin ||
        tileCY > view.bottom + margin
      ) {
        continue;
      }

      this._spawnSplat(cx, cy, colorInt);
    }
  }

  _spawnSplat(cx, cy, colorInt) {
    // Reuse a pooled image or create a new one using softBrush tinted
    let img = this._splatPool.pop();
    if (!img) {
      img = this.scene.add.image(0, 0, 'softBrush')
        .setOrigin(0, 0)
        .setBlendMode('ADD');
    }

    if (!img.parentContainer) this._splatLayer.add(img);

    img.setTexture('softBrush');
    img.setTint(colorInt);
    img.setPosition(cx, cy);
    img.setVisible(true);
    img.setAlpha(0.7);
    img.setScale(0.4);

    this.scene.tweens.add({
      targets: img,
      scaleX: 1.8,
      scaleY: 1.8,
      alpha: 0,
      duration: 90,
      onComplete: () => {
        img.setVisible(false);
        this._splatPool.push(img);
      }
    });
  }
}
