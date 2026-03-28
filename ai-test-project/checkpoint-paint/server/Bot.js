import { MAP_W, MAP_H, TILE_SIZE, SPRAY_RANGE } from '../shared/constants.js';
import { Player } from './Player.js';


const BOT_NAMES = ['InkBot', 'SplatBot', 'PaintBot', 'DripBot', 'SprayBot', 'BlastBot', 'GushBot', 'FloodBot'];

export class Bot extends Player {
  constructor(id, index) {
    super(id, index);
    this.isBot = true;
    this.name = BOT_NAMES[index % BOT_NAMES.length];
    this.id = this.name;

    this._changeDirIn = 0;
    this._roamAngle = Math.random() * Math.PI * 2;
    this._orbitDir = Math.random() < 0.5 ? -1 : 1;
    this._retargetIn = 0;
    this._targetId = null;
    this._targetCheckpoint = null; // { x, y } world coords of enemy checkpoint to rush

    this._dx = Math.cos(this._roamAngle);
    this._dy = Math.sin(this._roamAngle);
    this.aimAngle = this._roamAngle;
    this.spraying = true;
    this.ink = 100;
  }

  /** Call with players array and optional checkpoints Map<playerId, Checkpoint> */
  think(players, checkpoints) {
    this._retargetIn--;
    if (this._retargetIn <= 0) {
      this._retargetIn = 20 + Math.floor(Math.random() * 20);
      if (Math.random() < 0.15) this._orbitDir *= -1;

      // 25% chance: target nearest alive enemy checkpoint instead of a player
      if (checkpoints && Math.random() < 0.25) {
        let bestCp = null;
        let bestDist = Infinity;
        for (const [ownerId, cp] of checkpoints) {
          if (!cp.alive || ownerId === this.id) continue;
          const wx = cp.tileX * TILE_SIZE;
          const wy = cp.tileY * TILE_SIZE;
          const d = Math.hypot(wx - this.x, wy - this.y);
          if (d < bestDist) { bestDist = d; bestCp = cp; }
        }
        this._targetCheckpoint = bestCp ? { x: bestCp.tileX * TILE_SIZE, y: bestCp.tileY * TILE_SIZE } : null;
        this._targetId = null;
      } else {
        this._targetCheckpoint = null;
        let best = null;
        let bestDist = Infinity;
        for (const p of players) {
          if (p.id === this.id || p.isBot || !p.alive) continue;
          const d = Math.hypot(p.x - this.x, p.y - this.y);
          if (d < bestDist) { bestDist = d; best = p; }
        }
        this._targetId = best?.id ?? null;
      }
    }

    const worldW = MAP_W * TILE_SIZE;
    const worldH = MAP_H * TILE_SIZE;

    // Edge avoidance force (push inward near borders).
    const margin = 10 * TILE_SIZE; // ~10 tiles
    const edgeStrength = 2.2;
    const left = this.x;
    const right = worldW - this.x;
    const top = this.y;
    const bottom = worldH - this.y;

    const edgeFactor = (d) => {
      const t = Math.max(0, Math.min(1, (margin - d) / margin));
      return t * t; // ease-in
    };

    const edgeX = (edgeFactor(left) - edgeFactor(right)) * edgeStrength;
    const edgeY = (edgeFactor(top) - edgeFactor(bottom)) * edgeStrength;

    // Resolve target: either a specific checkpoint position or a player
    const cpTarget = this._targetCheckpoint;
    const target = cpTarget
      ? null
      : (this._targetId ? players.find(p => p.id === this._targetId && p.alive && !p.isBot) : null);

    let moveX = 0;
    let moveY = 0;

    // Rush directly toward a checkpoint target (no orbit, just advance and spray)
    if (cpTarget && !target) {
      const vx = cpTarget.x - this.x;
      const vy = cpTarget.y - this.y;
      const dist = Math.max(1, Math.hypot(vx, vy));
      this.aimAngle = Math.atan2(vy, vx);
      moveX = vx / dist;
      moveY = vy / dist;
    } else if (target) {
      const vx = target.x - this.x;
      const vy = target.y - this.y;
      const dist = Math.max(1, Math.hypot(vx, vy));
      const nx = vx / dist;
      const ny = vy / dist;

      // Aim directly at the enemy.
      this.aimAngle = Math.atan2(vy, vx);

      // Circle behavior: tangential movement + keep a preferred distance band.
      const preferred = Math.max(60, SPRAY_RANGE * 0.55);
      const tooClose = dist < preferred * 0.8;
      const tooFar = dist > preferred * 1.6;

      const tangentX = -ny * this._orbitDir;
      const tangentY = nx * this._orbitDir;

      const approach = tooFar ? 1.0 : 0.15;
      const retreat = tooClose ? 1.0 : 0.0;
      const orbit = Math.max(0, Math.min(1, 1 - dist / (preferred * 2.2))) * 1.35 + 0.25;

      moveX = nx * approach - nx * retreat + tangentX * orbit;
      moveY = ny * approach - ny * retreat + tangentY * orbit;

      // If we're very close, bias to dodge laterally more than backing up.
      if (tooClose) {
        moveX = moveX * 0.6 + tangentX * 0.8;
        moveY = moveY * 0.6 + tangentY * 0.8;
      }
    } else {
      // Roam: change direction every 40-90 ticks, with a gentle random drift.
      this._changeDirIn--;
      if (this._changeDirIn <= 0) {
        this._changeDirIn = 40 + Math.floor(Math.random() * 50);
        this._roamAngle += (Math.random() - 0.5) * 1.6;
      } else {
        this._roamAngle += (Math.random() - 0.5) * 0.05;
      }

      moveX = Math.cos(this._roamAngle);
      moveY = Math.sin(this._roamAngle);
      this.aimAngle = this._roamAngle;
    }

    // Combine movement with edge avoidance, then normalize.
    moveX += edgeX;
    moveY += edgeY;

    const len = Math.hypot(moveX, moveY);
    if (len > 0.0001) {
      this._dx = moveX / len;
      this._dy = moveY / len;
    }

    // bots have infinite ink
    this.ink = 100;
    this.spraying = true;
  }
}
