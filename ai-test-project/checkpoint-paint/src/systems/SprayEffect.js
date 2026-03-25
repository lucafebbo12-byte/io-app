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

    const particles = this.scene.add.particles(0, 0, 'dot', {
      speed: { min: 80, max: 200 },
      scale: { start: 0.6, end: 0 },
      lifespan: { min: 200, max: 400 },
      quantity: 3,
      tint: parseInt(color.replace('#', ''), 16),
      emitting: false,
      depth: 3
    });

    this._emitters.set(playerId, particles);
    return particles;
  }

  update(playerId, x, y, aimAngle, spraying, color) {
    const emitter = this.getOrCreate(playerId, color);
    if (spraying) {
      emitter.setPosition(x, y);
      emitter.setAngle({
        min: Phaser.Math.RadToDeg(aimAngle) - 12,
        max: Phaser.Math.RadToDeg(aimAngle) + 12
      });
      emitter.emitting = true;
      emitter.explode(3);
    } else {
      emitter.emitting = false;
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
      e.destroy();
      this._emitters.delete(playerId);
    }
  }
}
