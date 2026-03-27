import { getConeTiles } from '../../shared/sprayCone.js';

export class SprayEffect {
  constructor(scene) {
    this.scene = scene;
    this._emitters = new Map(); // playerId → particles emitter
    this._lastPredictAt = 0;
    this._predictedKeys = new Set();
  }

  getOrCreate(playerId, color) {
    if (this._emitters.has(playerId)) return this._emitters.get(playerId);

    const tint = parseInt(color.replace('#', ''), 16);
    // Main spray stream: medium blobs flying in aim direction
    const stream = this.scene.add.particles(0, 0, 'dot', {
      speed: { min: 120, max: 260 },
      scale: { start: 1.0, end: 0 },
      lifespan: { min: 250, max: 480 },
      quantity: 4,
      tint,
      emitting: false,
      depth: 4,
      alpha: { start: 0.9, end: 0 }
    });
    // Drip splatter: tiny drops that fall shorter range
    const drips = this.scene.add.particles(0, 0, 'dot', {
      speed: { min: 30, max: 70 },
      scale: { start: 0.4, end: 0 },
      lifespan: { min: 150, max: 300 },
      quantity: 2,
      tint,
      emitting: false,
      depth: 3,
      alpha: { start: 0.7, end: 0 }
    });

    this._emitters.set(playerId, { stream, drips });
    return this._emitters.get(playerId);
  }

  update(playerId, x, y, aimAngle, spraying, color) {
    const emitter = this.getOrCreate(playerId, color);
    const { stream, drips } = emitter;
    if (spraying) {
      const deg = Phaser.Math.RadToDeg(aimAngle);
      const spread = 14;
      stream.setPosition(x, y);
      stream.setAngle({ min: deg - spread, max: deg + spread });
      stream.emitting = true;
      stream.explode(4);

      drips.setPosition(x, y);
      drips.setAngle({ min: deg - spread * 2, max: deg + spread * 2 });
      drips.emitting = true;
      drips.explode(2);
    } else {
      stream.emitting = false;
      drips.emitting = false;
    }
  }

  predictLocalPaint(x, y, aimAngle, spraying, ownerIndex, ink) {
    if (!spraying || !ownerIndex || ink <= 0) return;
    const territory = this.scene.territory;
    if (!territory) return;

    const now = this.scene.time?.now ?? Date.now();
    if (now - this._lastPredictAt < 50) return;
    this._lastPredictAt = now;

    if (this._predictedKeys.size > 4000) this._predictedKeys.clear();

    const tiles = getConeTiles(x, y, aimAngle, undefined, undefined, territory.mapW, territory.mapH);
    const changedTiles = [];
    for (const t of tiles) {
      const key = t.x * 1000 + t.y;
      if (this._predictedKeys.has(key)) continue;
      this._predictedKeys.add(key);
      changedTiles.push({ x: t.x, y: t.y, owner: ownerIndex });
    }

    territory.applyDelta(changedTiles);
  }

  remove(playerId) {
    const e = this._emitters.get(playerId);
    if (e) {
      e.stream?.destroy();
      e.drips?.destroy();
      this._emitters.delete(playerId);
    }
  }
}
