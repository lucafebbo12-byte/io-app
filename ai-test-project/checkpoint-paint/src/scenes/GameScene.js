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
    this.playerSprites = new Map(); // id → {body, glow, label, baseY}
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
      if (sprites.glow) {
        sprites.glow.x = sprites.body.x;
        sprites.glow.y = sprites.body.y;
      }
      sprites.label.setPosition(sprites.body.x, sprites.body.y - 22);
      sprites.body.setAlpha(p.alive ? 1 : 0.2);
      if (sprites.glow) sprites.glow.setAlpha(p.alive ? 0.55 : 0);

      // Zone visual feedback (tint + subtle size change)
      const zoneType = p.zoneType || 'neutral';
      if (zoneType === 'own') {
        sprites.body.clearTint();
        sprites.body.setScale(1.1);
        if (sprites.glow) sprites.glow.setVisible(true).setScale(1.25);
      } else if (zoneType === 'enemy') {
        sprites.body.setTint(0xff6666);
        sprites.body.setScale(0.9);
        if (sprites.glow) sprites.glow.setVisible(false);
      } else {
        sprites.body.clearTint();
        sprites.body.setScale(1.0);
        if (sprites.glow) sprites.glow.setVisible(false);
      }

      if (p.id === this.playerId) this.events.emit('zone_update', zoneType);

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
    // Lighten body color for highlight
    const r = (colorInt >> 16) & 0xff;
    const gv = (colorInt >> 8) & 0xff;
    const b = colorInt & 0xff;
    const lightInt = (Math.min(255, r + 60) << 16) | (Math.min(255, gv + 60) << 8) | Math.min(255, b + 60);
    const darkInt  = (Math.max(0, r - 40) << 16) | (Math.max(0, gv - 40) << 8) | Math.max(0, b - 40);

    const texKey = 'char_' + p.color.replace('#', '');
    if (!this.textures.exists(texKey)) {
      const g = this.make.graphics({ add: false });
      // Drop shadow
      g.fillStyle(0x000000, 0.35);
      g.fillEllipse(14, 33, 20, 6);
      // Body: rounded rect, gradient-ish using two layers
      g.fillStyle(colorInt, 1);
      g.fillRoundedRect(3, 11, 22, 22, 5);
      g.fillStyle(lightInt, 0.4);
      g.fillRoundedRect(3, 11, 22, 10, { tl: 5, tr: 5, bl: 0, br: 0 });
      g.fillStyle(darkInt, 0.3);
      g.fillRoundedRect(3, 23, 22, 10, { tl: 0, tr: 0, bl: 5, br: 5 });
      // Head: circle skin tone
      g.fillStyle(0xFFCC99, 1);
      g.fillCircle(14, 9, 8);
      // Head highlight
      g.fillStyle(0xffffff, 0.35);
      g.fillCircle(12, 7, 4);
      // Eyes: white sclera
      g.fillStyle(0xffffff, 1);
      g.fillCircle(11, 8, 2.5);
      g.fillCircle(17, 8, 2.5);
      // Pupils: big friendly eyes
      g.fillStyle(0x222222, 1);
      g.fillCircle(11, 9, 1.5);
      g.fillCircle(17, 9, 1.5);
      // Eye shine
      g.fillStyle(0xffffff, 1);
      g.fillCircle(12, 8, 0.7);
      g.fillCircle(18, 8, 0.7);
      // Gun barrel: colored dark, right side
      g.fillStyle(0x222222, 1);
      g.fillRoundedRect(20, 19, 12, 5, 2);
      // Gun nozzle: bright accent
      g.fillStyle(lightInt, 0.9);
      g.fillRect(30, 20, 3, 3);
      g.generateTexture(texKey, 34, 36);
      g.destroy();
    }

    const body = this.add.image(p.x, p.y, texKey).setDepth(5).setOrigin(0.5);
    const isOwn = p.id === this.playerId;
    const label = this.add.text(p.x, p.y - 26, isOwn ? 'YOU' : p.id.slice(0, 6), {
      fontSize: '10px',
      color: isOwn ? p.color : '#fff',
      stroke: '#000',
      strokeThickness: 2,
      fontStyle: isOwn ? 'bold' : 'normal'
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
      .setAlpha(0.95)
      .setTint(parseInt((owner?.color ?? '#ffffff').replace('#', ''), 16));
    // Pulse animation
    this.tweens.add({
      targets: star,
      scaleX: 1.8, scaleY: 1.8,
      alpha: 0.7,
      duration: 900,
      yoyo: true,
      repeat: -1,
      ease: 'Sine.easeInOut'
    });
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
