import Phaser from 'phaser';
import { TILE_SIZE, PLAYER_COLORS, WALL_OWNER_INDEX, WALL_COLOR } from '../../shared/constants.js';
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

    // Checkpoint icon (white star)
    const star = this.make.graphics({ add: false });
    star.fillStyle(0xffffff, 1);
    star.fillStar(10, 10, 5, 4, 9);
    star.generateTexture('checkpoint_star', 20, 20);
    star.destroy();
  }

  create() {
    this.playerId = null;
    this.playerSprites = new Map(); // id → {body, label, baseY}
    this.checkpointSprites = new Map();
    this.playerData = new Map();
    this.tileSize = TILE_SIZE;
    this.colorIndexByHex = {};
    this.localInk = 100;
    this._lastCountdown = 0;

    this.network = new NetworkManager(this);
    this.spray = new SprayEffect(this);

    // HUD text
    this.timerText = this.add.text(this.scale.width / 2, 20, '', {
      fontSize: '20px', color: '#ffffff', stroke: '#000', strokeThickness: 3
    }).setOrigin(0.5, 0).setScrollFactor(0).setDepth(20);

    this.msgText = this.add.text(this.scale.width / 2, this.scale.height * 0.4, '', {
      fontSize: '28px', color: '#ffff00', stroke: '#000', strokeThickness: 4
    }).setOrigin(0.5).setScrollFactor(0).setDepth(20).setAlpha(0);

    // Countdown text
    this.countdownText = this.add.text(this.scale.width / 2, this.scale.height * 0.35, '', {
      fontSize: '72px', color: '#ffffff', stroke: '#000', strokeThickness: 6
    }).setOrigin(0.5).setScrollFactor(0).setDepth(30).setAlpha(0);
  }

  // ─── Network callbacks ─────────────────────────────────────────────────────

  onInit({ playerId, mapW, mapH, tileSize, initialTiles = [], players, checkpoints, timeLeft, countdown }) {
    this.playerId = playerId;
    this.tileSize = tileSize;
    this._lastCountdown = countdown ?? 0;

    // Build color lookup: colorIndex → hex
    const colorMap = {};
    colorMap[0] = '#1a1a2e'; // neutral
    PLAYER_COLORS.forEach((c, i) => {
      colorMap[i + 1] = c;
      this.colorIndexByHex[c] = i + 1;
    });
    colorMap[WALL_OWNER_INDEX] = WALL_COLOR;

    this.territory = new TerritoryMap(this, mapW, mapH, colorMap);
    this.territory.applyDelta(initialTiles);

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

  onDelta({ players, changedTiles, timeLeft, countdown }) {
    this.timeLeft = timeLeft;
    this.territory?.applyDelta(changedTiles);
    this.timerText.setText(`⏱ ${timeLeft}s`);
    this.events.emit('time_update', timeLeft);

    // Countdown display
    if (countdown !== undefined && countdown > 0) {
      if (countdown !== this._lastCountdown) {
        this._lastCountdown = countdown;
        this.countdownText.setText(String(countdown)).setAlpha(1);
        this.tweens.killTweensOf(this.countdownText);
        this.tweens.add({ targets: this.countdownText, alpha: 0, duration: 800, delay: 200 });
      }
    } else if (this._lastCountdown > 0) {
      this._lastCountdown = 0;
      this.countdownText.setText('GO!').setAlpha(1);
      this.tweens.add({ targets: this.countdownText, alpha: 0, duration: 600, delay: 400 });
    }

    const top5 = [...players].sort((a, b) => (b.score || 0) - (a.score || 0)).slice(0, 5);
    this.events.emit('score_update', top5);

    for (const p of players) {
      this.playerData.set(p.id, p);
      const sprites = this.playerSprites.get(p.id);
      if (!sprites) { this._createPlayerSprite(p); continue; }

      // Smooth lerp toward server position
      sprites.body.x = Phaser.Math.Linear(sprites.body.x, p.x, 0.3);
      sprites.body.y = Phaser.Math.Linear(sprites.body.y, p.y, 0.3);
      sprites.baseY = sprites.body.y;
      sprites.label.setPosition(sprites.body.x, sprites.body.y - 22);
      sprites.body.setAlpha(p.alive ? 1 : 0.2);

      // Zone tint for own player
      if (p.id === this.playerId) {
        if (p.zoneType === 'enemy') {
          sprites.body.setTint(0xff8888);
        } else {
          sprites.body.clearTint();
        }
        this.events.emit('zone_update', p.zoneType);
      }

      // Spray FX
      this.spray.update(p.id, sprites.body.x, sprites.body.y, p.aimAngle, p.spraying && p.alive, p.color);

      // Camera follow own player
      if (p.id === this.playerId) {
        this.cameras.main.centerOn(sprites.body.x, sprites.body.y);
        this.localInk = p.ink;
        this.events.emit('ink_update', p.ink);
      }
    }
  }

  onPlayerJoined(p) { this._createPlayerSprite(p); }
  onPlayerLeft(id) { this._removePlayer(id); }

  onPlayerHit({ playerId }) {
    const s = this.playerSprites.get(playerId);
    if (s) s.body.setAlpha(0.2);
    if (playerId === this.playerId) this._showMsg('💀 You died! Respawning...');
    this.events.emit('kill_feed', { victimId: playerId });
  }

  onPlayerRespawn({ playerId }) {
    const s = this.playerSprites.get(playerId);
    if (s) s.body.setAlpha(1);
    if (playerId === this.playerId) this._showMsg('✅ Respawned!');
  }

  onPlayerEliminated({ playerId }) {
    const s = this.playerSprites.get(playerId);
    if (s) { s.body.destroy(); s.label.destroy(); this.playerSprites.delete(playerId); }
    if (playerId === this.playerId) this._showMsg('☠️ You are eliminated!');
  }

  onCheckpointDestroyed({ playerId }) {
    const s = this.checkpointSprites.get(playerId);
    if (s) { s.destroy(); this.checkpointSprites.delete(playerId); }
    const p = this.playerData.get(playerId);
    if (p) this._showMsg(`🏳️ ${playerId === this.playerId ? 'Your' : 'A'} checkpoint destroyed!`);
    this.events.emit('checkpoint_feed', { playerId });
  }

  onGameOver({ winnerId, winnerColor, scores }) {
    const msg = winnerId === this.playerId ? '🏆 YOU WIN!' : `Game Over! Winner: ${winnerId}`;
    this._showMsg(msg, 0);
    this.time.delayedCall(4000, () => this.scene.start('WinScene', { winnerId, winnerColor, scores }));
  }

  // ─── Update ────────────────────────────────────────────────────────────────

  update(time) {
    if (!this.playerId || !this.moveStick) return;
    const input = readJoysticks(this.moveStick, this.aimStick);
    this.network.sendInput(input);

    const local = this.playerData.get(this.playerId);
    const ownerIndex = this.colorIndexByHex[local?.color] || 1;
    const sprites = this.playerSprites.get(this.playerId);
    if (sprites) {
      this.spray.predictLocalPaint(
        sprites.body.x,
        sprites.body.y,
        input.aimAngle,
        input.spraying,
        ownerIndex,
        local?.ink ?? this.localInk ?? 100
      );
    }

    // Walking bobble for all player sprites
    for (const [id, s] of this.playerSprites) {
      const baseY = s.baseY ?? s.body.y;
      s.body.y = baseY + Math.sin(time * 0.008) * 2;
    }
  }

  // ─── Helpers ───────────────────────────────────────────────────────────────

  _createPlayerSprite(p) {
    const colorInt = parseInt(p.color.replace('#', ''), 16);
    const texKey = 'char_' + p.id;

    // Draw Paper.io-style character into a texture
    const g = this.make.graphics({ add: false });
    // Body: rounded rect 16×20, player color
    g.fillStyle(colorInt, 1);
    g.fillRoundedRect(4, 10, 16, 20, 4);
    // Head: circle 10px, skin tone
    g.fillStyle(0xFFCC99, 1);
    g.fillCircle(12, 8, 7);
    // Eyes: white
    g.fillStyle(0xffffff, 1);
    g.fillCircle(9, 7, 2);
    g.fillCircle(15, 7, 2);
    // Pupils: black dot
    g.fillStyle(0x000000, 1);
    g.fillCircle(9, 7, 1);
    g.fillCircle(15, 7, 1);
    // Gun: small rect, dark, centered on body
    g.fillStyle(0x333333, 1);
    g.fillRect(16, 17, 10, 4);
    g.generateTexture(texKey, 28, 32);
    g.destroy();

    const body = this.add.image(p.x, p.y, texKey).setDepth(5).setOrigin(0.5);
    const label = this.add.text(p.x, p.y - 22, p.id.slice(0, 6), {
      fontSize: '10px', color: '#fff', stroke: '#000', strokeThickness: 2
    }).setOrigin(0.5).setDepth(6);
    this.playerSprites.set(p.id, { body, label, baseY: p.y });
    this.playerData.set(p.id, p);
  }

  _createCheckpointSprite(c) {
    const owner = this.playerData.get(c.ownerId);
    const ownerIndex = this.colorIndexByHex[owner?.color] ?? 1;

    // 5x5 base zone in owner's color (drawn into the territory render texture)
    const baseTiles = [];
    for (let dy = -2; dy <= 2; dy++) {
      for (let dx = -2; dx <= 2; dx++) {
        baseTiles.push({ x: c.tileX + dx, y: c.tileY + dy, owner: ownerIndex });
      }
    }
    this.territory?.applyDelta(baseTiles);

    // Big star icon on top (separate sprite so we can remove it)
    const px = (c.tileX + 0.5) * this.tileSize;
    const py = (c.tileY + 0.5) * this.tileSize;
    const star = this.add.image(px, py, 'checkpoint_star')
      .setDepth(2)
      .setScale(1.4)
      .setAlpha(0.95);
    this.checkpointSprites.set(c.ownerId, star);
  }

  _removePlayer(id) {
    const s = this.playerSprites.get(id);
    if (s) { s.body.destroy(); s.label.destroy(); }
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
