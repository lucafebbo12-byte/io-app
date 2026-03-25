import Phaser from 'phaser';
import { TILE_SIZE, PLAYER_COLORS } from '../../shared/constants.js';
import { NetworkManager } from '../systems/NetworkManager.js';
import { TerritoryMap } from '../systems/TerritoryMap.js';
import { SprayEffect } from '../systems/SprayEffect.js';
import { createJoysticks, readJoysticks, PLUGIN_KEY } from '../systems/JoystickInput.js';

export class GameScene extends Phaser.Scene {
  constructor() { super({ key: 'GameScene' }); }

  preload() {
    // Create a tiny dot texture for particles
    const g = this.make.graphics({ add: false });
    g.fillStyle(0xffffff);
    g.fillCircle(4, 4, 4);
    g.generateTexture('dot', 8, 8);
    g.destroy();
  }

  create() {
    this.playerId = null;
    this.playerSprites = new Map(); // id → {circle, label}
    this.checkpointSprites = new Map();
    this.playerData = new Map();
    this.tileSize = TILE_SIZE;

    this.network = new NetworkManager(this);
    this.spray = new SprayEffect(this);

    // HUD text
    this.timerText = this.add.text(this.scale.width / 2, 20, '', {
      fontSize: '20px', color: '#ffffff', stroke: '#000', strokeThickness: 3
    }).setOrigin(0.5, 0).setScrollFactor(0).setDepth(20);

    this.msgText = this.add.text(this.scale.width / 2, this.scale.height * 0.4, '', {
      fontSize: '28px', color: '#ffff00', stroke: '#000', strokeThickness: 4
    }).setOrigin(0.5).setScrollFactor(0).setDepth(20).setAlpha(0);
  }

  // ─── Network callbacks ─────────────────────────────────────────────────────

  onInit({ playerId, mapW, mapH, tileSize, players, checkpoints, timeLeft }) {
    this.playerId = playerId;
    this.tileSize = tileSize;

    // Build color lookup: colorIndex → hex
    const colorMap = {};
    colorMap[0] = '#1a1a2e'; // neutral
    PLAYER_COLORS.forEach((c, i) => { colorMap[i + 1] = c; });

    this.territory = new TerritoryMap(this, mapW, mapH, colorMap);

    // Camera
    const worldW = mapW * tileSize;
    const worldH = mapH * tileSize;
    this.cameras.main.setBounds(0, 0, worldW, worldH);
    this.cameras.main.setZoom(2);

    players.forEach(p => this._createPlayerSprite(p));
    checkpoints.forEach(c => this._createCheckpointSprite(c));

    // Joysticks (fixed to camera)
    const { moveStick, aimStick } = createJoysticks(this);
    this.moveStick = moveStick;
    this.aimStick = aimStick;

    this.timerText.setText(`⏱ ${timeLeft}s`);
  }

  onDelta({ players, changedTiles, timeLeft }) {
    this.territory?.applyDelta(changedTiles);
    this.timerText.setText(`⏱ ${timeLeft}s`);

    for (const p of players) {
      this.playerData.set(p.id, p);
      const sprites = this.playerSprites.get(p.id);
      if (!sprites) { this._createPlayerSprite(p); continue; }

      // Smooth lerp toward server position
      sprites.circle.x = Phaser.Math.Linear(sprites.circle.x, p.x, 0.3);
      sprites.circle.y = Phaser.Math.Linear(sprites.circle.y, p.y, 0.3);
      sprites.label.setPosition(sprites.circle.x, sprites.circle.y - 20);
      sprites.circle.setAlpha(p.alive ? 1 : 0.2);

      // Spray FX
      this.spray.update(p.id, sprites.circle.x, sprites.circle.y, p.aimAngle, p.spraying && p.alive, p.color);

      // Camera follow own player
      if (p.id === this.playerId) {
        this.cameras.main.centerOn(sprites.circle.x, sprites.circle.y);
      }
    }
  }

  onPlayerJoined(p) { this._createPlayerSprite(p); }
  onPlayerLeft(id) { this._removePlayer(id); }

  onPlayerHit({ playerId }) {
    const s = this.playerSprites.get(playerId);
    if (s) s.circle.setAlpha(0.2);
    if (playerId === this.playerId) this._showMsg('💀 You died! Respawning...');
  }

  onPlayerRespawn({ playerId }) {
    const s = this.playerSprites.get(playerId);
    if (s) s.circle.setAlpha(1);
    if (playerId === this.playerId) this._showMsg('✅ Respawned!');
  }

  onPlayerEliminated({ playerId }) {
    const s = this.playerSprites.get(playerId);
    if (s) { s.circle.destroy(); s.label.destroy(); this.playerSprites.delete(playerId); }
    if (playerId === this.playerId) this._showMsg('☠️ You are eliminated!');
  }

  onCheckpointDestroyed({ playerId }) {
    const s = this.checkpointSprites.get(playerId);
    if (s) { s.destroy(); this.checkpointSprites.delete(playerId); }
    const p = this.playerData.get(playerId);
    if (p) this._showMsg(`🏳️ ${playerId === this.playerId ? 'Your' : 'A'} checkpoint destroyed!`);
  }

  onGameOver({ winnerId, winnerColor, scores }) {
    const msg = winnerId === this.playerId ? '🏆 YOU WIN!' : `Game Over! Winner: ${winnerId}`;
    this._showMsg(msg, 0);
    this.time.delayedCall(4000, () => this.scene.start('WinScene', { winnerId, winnerColor, scores }));
  }

  // ─── Update ────────────────────────────────────────────────────────────────

  update() {
    if (!this.playerId || !this.moveStick) return;
    const input = readJoysticks(this.moveStick, this.aimStick);
    this.network.sendInput(input);
  }

  // ─── Helpers ───────────────────────────────────────────────────────────────

  _createPlayerSprite(p) {
    const color = parseInt(p.color.replace('#', ''), 16);
    const circle = this.add.circle(p.x, p.y, 10, color).setDepth(5);
    const label = this.add.text(p.x, p.y - 20, p.id.slice(0, 6), {
      fontSize: '10px', color: '#fff', stroke: '#000', strokeThickness: 2
    }).setOrigin(0.5).setDepth(6);
    this.playerSprites.set(p.id, { circle, label });
    this.playerData.set(p.id, p);
  }

  _createCheckpointSprite(c) {
    const g = this.add.graphics().setDepth(2);
    g.fillStyle(0xffffff, 0.8);
    g.fillStar(c.tileX * this.tileSize, c.tileY * this.tileSize, 5, 6, 12);
    this.checkpointSprites.set(c.ownerId, g);
  }

  _removePlayer(id) {
    const s = this.playerSprites.get(id);
    if (s) { s.circle.destroy(); s.label.destroy(); }
    this.playerSprites.delete(id);
    this.spray.remove(id);
  }

  _showMsg(text, duration = 2000) {
    this.msgText.setText(text).setAlpha(1);
    if (duration > 0) {
      this.tweens.add({ targets: this.msgText, alpha: 0, delay: duration, duration: 500 });
    }
  }
}
