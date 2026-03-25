import { SPRAY_RANGE } from '../../shared/constants.js';

export class SprayEffect {
  constructor(scene) {
    this.scene = scene;
    this._emitters = new Map(); // playerId → particles emitter
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

  remove(playerId) {
    const e = this._emitters.get(playerId);
    if (e) { e.destroy(); this._emitters.delete(playerId); }
  }
}
