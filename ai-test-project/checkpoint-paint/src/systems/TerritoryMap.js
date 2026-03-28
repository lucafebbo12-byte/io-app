import Phaser from 'phaser';
import { TILE_SIZE } from '../../shared/constants.js';

export class TerritoryMap {
  constructor(scene, mapW, mapH, colors) {
    this.scene = scene;
    this.mapW = mapW;
    this.mapH = mapH;
    this.colors = colors; // colorIndex -> hex string

    this._animOriginX = null;
    this._animOriginY = null;
    this.useSmoothPaint = true;
    this.showTileFallback = false;
    this.maxStampsPerFrame = 500;
    this._stampQueue = [];
    this.worldW = mapW * TILE_SIZE;
    this.worldH = mapH * TILE_SIZE;

    this._ensureFloorTexture();
    this.floor = scene.add
      .image(0, 0, `floor_bg_${mapW}x${mapH}`)
      .setOrigin(0, 0)
      .setDepth(-0.5);

    // Off-screen render texture for the full territory grid
    this.rt = scene.add
      .renderTexture(0, 0, this.worldW, this.worldH)
      .setOrigin(0, 0)
      .setDepth(0);
    this.rt.setVisible(this.showTileFallback);

    // Smooth visual paint layer (separate from logical tile ownership renderer)
    this.smoothPaintRT = scene.add
      .renderTexture(0, 0, this.worldW, this.worldH)
      .setOrigin(0, 0)
      .setDepth(0.5);

    this._tileKeyByOwner = {};
    this._ensureTileTextures();
    this._ensureBrushTexture();
    this._ownerTintByIndex = {};
    for (const [idxStr, hex] of Object.entries(colors)) {
      this._ownerTintByIndex[Number(idxStr)] = parseInt(hex.replace('#', ''), 16);
    }

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

      if (idx === 255) {
        // Void / wall tile — near-black, no shine, blends with canvas background
        g.fillStyle(0x111118, 1);
        g.fillRect(0, 0, TILE_SIZE, TILE_SIZE);

      } else if (idx === 0) {
        // Neutral tile fallback (main floor is now a large premium background texture)
        g.fillStyle(0x1e2035, 1);
        g.fillRect(0, 0, TILE_SIZE, TILE_SIZE);
        g.fillStyle(0x2a2b48, 0.55);
        g.fillRect(Math.floor(TILE_SIZE / 2), Math.floor(TILE_SIZE / 2), 1, 1);

      } else {
        // ── Dye Hard–style glossy paint tile ──────────────────────────
        // 1. Dark "grout" background (1 px gap between tiles)
        g.fillStyle(0x111118, 1);
        g.fillRect(0, 0, TILE_SIZE, TILE_SIZE);

        // 2. Main color fill (inset 0.5 px so grout shows on all edges)
        g.fillStyle(base, 1);
        g.fillRect(0.5, 0.5, TILE_SIZE - 1, TILE_SIZE - 1);

        // 3. Top-left highlight (Dye Hard specular look, 55% white)
        g.fillStyle(0xffffff, 0.55);
        g.fillTriangle(0.5, 0.5, TILE_SIZE - 0.5, 0.5, 0.5, TILE_SIZE - 0.5);

        // 4. Bottom-right shadow (20% black for depth)
        g.fillStyle(0x000000, 0.20);
        g.fillTriangle(TILE_SIZE - 0.5, 0.5, TILE_SIZE - 0.5, TILE_SIZE - 0.5, 0.5, TILE_SIZE - 0.5);
      }

      g.generateTexture(key, TILE_SIZE, TILE_SIZE);
      g.destroy();
    }
  }

  _ensureFloorTexture() {
    const key = `floor_bg_${this.mapW}x${this.mapH}`;
    if (this.scene.textures.exists(key)) return;

    const tex = this.scene.textures.createCanvas(key, this.worldW, this.worldH);
    const ctx = tex.getContext();

    const cx = this.worldW * 0.5;
    const cy = this.worldH * 0.5;
    const maxR = Math.hypot(cx, cy);
    const grad = ctx.createRadialGradient(cx, cy, maxR * 0.1, cx, cy, maxR);
    grad.addColorStop(0, '#262944');
    grad.addColorStop(1, '#171a2d');
    ctx.fillStyle = grad;
    ctx.fillRect(0, 0, this.worldW, this.worldH);

    ctx.fillStyle = 'rgba(58, 61, 92, 0.25)';
    const spacing = 24;
    for (let y = 12; y < this.worldH; y += spacing) {
      for (let x = 12; x < this.worldW; x += spacing) {
        const jitterX = ((x + y) % 3) - 1;
        const jitterY = ((x * y) % 3) - 1;
        ctx.fillRect(x + jitterX, y + jitterY, 2, 2);
      }
    }
    tex.refresh();
  }

  _ensureBrushTexture() {
    const key = 'brush_soft';
    if (this.scene.textures.exists(key)) return;
    const radius = 24;
    const size = radius * 2;
    const g = this.scene.make.graphics({ add: false });
    for (let i = radius; i > 0; i -= 2) {
      const alpha = (i / radius) * 0.92;
      g.fillStyle(0xffffff, alpha);
      g.fillCircle(radius, radius, i);
    }
    g.generateTexture(key, size, size);
    g.destroy();
  }

  _stampSmooth(owner, worldX, worldY, alpha) {
    if (owner === 0 || owner === 255) return;
    const tint = this._ownerTintByIndex[owner] ?? 0xffffff;
    const jitterX = Phaser.Math.Between(-2, 2);
    const jitterY = Phaser.Math.Between(-2, 2);
    const scale = Phaser.Math.FloatBetween(0.85, 1.15);
    this.smoothPaintRT.draw(
      'brush_soft',
      worldX + jitterX,
      worldY + jitterY,
      alpha,
      tint,
      scale,
      scale
    );
  }

  _drawTiles(tiles, ownerOverride = null, animate = false, predictedAlpha = 0.6) {
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
      const cx = x + TILE_SIZE * 0.5;
      const cy = y + TILE_SIZE * 0.5;
      const inView =
        cx >= view.x - margin &&
        cy >= view.y - margin &&
        cx <= view.right + margin &&
        cy <= view.bottom + margin;

      if (!this.useSmoothPaint || this.showTileFallback) {
        this.rt.draw(key, x, y);
      }

      if (this.useSmoothPaint && inView) {
        const alpha = ownerOverride !== null ? predictedAlpha : 1.0;
        this._stampQueue.push({ owner, x: cx, y: cy, alpha });
      }

      if (!animate) continue;

      // Only animate tiles near local player, and only near camera view.
      if (hasOrigin) {
        const dx = cx - ox;
        const dy = cy - oy;
        if (dx * dx + dy * dy > maxDistSq) continue;
      } else {
        continue;
      }

      if (!inView) continue;

      this._spawnSplat(key, x, y);
    }

    if (this.useSmoothPaint) this._flushStampQueue();
  }

  _flushStampQueue() {
    let budget = this.maxStampsPerFrame;
    while (budget > 0 && this._stampQueue.length > 0) {
      const s = this._stampQueue.shift();
      this._stampSmooth(s.owner, s.x, s.y, s.alpha);
      budget--;
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
