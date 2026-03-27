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
    this.timerText.setVisible(false);

    this.msgText = this.add.text(this.scale.width / 2, this.scale.height * 0.4, '', {
      fontSize: '28px', color: '#ffff00', stroke: '#000', strokeThickness: 4
    }).setOrigin(0.5).setScrollFactor(0).setDepth(20).setAlpha(0);

    // Aim cone indicator (spray arc)
    this.aimCone = this.add.graphics().setDepth(4).setVisible(false);

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

    this._ensureCharacterTextures();

    players.forEach(p => this._createPlayerSprite(p));
    checkpoints.forEach(c => this._createCheckpointSprite(c));

    // Joysticks (fixed to camera)
    const { moveStick, aimStick } = createJoysticks(this);
    this.moveStick = moveStick;
    this.aimStick = aimStick;

    this.timerText.setText(`⏱ ${timeLeft}s`);

    const me = players.find(pp => pp.id === playerId);
    if (me?.color) this.events.emit('self_color', { color: me.color });
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
      sprites.container.x = Phaser.Math.Linear(sprites.container.x, p.x, 0.3);
      sprites.container.y = Phaser.Math.Linear(sprites.container.y, p.y, 0.3);
      sprites.baseY = sprites.container.y;
      sprites.label.setPosition(sprites.container.x, sprites.container.y - 26);

      if (p.alive) {
        sprites.container.setVisible(true);
        sprites.container.setAlpha(1);
      } else if (sprites.container.visible) {
        sprites.container.setAlpha(0.2);
      }

      // Zone visual feedback (tint + subtle size change)
      const zoneType = p.zoneType || 'neutral';
      if (zoneType === 'own') {
        sprites.base.clearTint();
        sprites.gun.clearTint();
        sprites.container.setScale(1.1);
        sprites.glow.setVisible(true);
      } else if (zoneType === 'enemy') {
        sprites.base.setTint(0xff6666);
        sprites.gun.setTint(0xff6666);
        sprites.container.setScale(0.9);
        sprites.glow.setVisible(false);
      } else {
        sprites.base.clearTint();
        sprites.gun.clearTint();
        sprites.container.setScale(1.0);
        sprites.glow.setVisible(false);
      }

      if (p.id === this.playerId) this.events.emit('zone_update', zoneType);

      // Aim + spray animation (gun only)
      sprites.gun.setRotation(p.aimAngle);
      const gunKick = p.spraying && p.alive ? 2 : 0;
      sprites.gun.x = sprites.gunBaseX + Math.cos(p.aimAngle) * gunKick;
      sprites.gun.y = sprites.gunBaseY + Math.sin(p.aimAngle) * gunKick;

      // Spray FX
      this.spray.update(p.id, sprites.container.x, sprites.container.y, p.aimAngle, p.spraying && p.alive, p.color);

      // Camera follow own player
      if (p.id === this.playerId) {
        this.cameras.main.centerOn(sprites.container.x, sprites.container.y);
        this.localInk = p.ink;
        this.events.emit('ink_update', p.ink);
      }
    }
  }

  onPlayerJoined(p) { this._createPlayerSprite(p); }
  onPlayerLeft(id) { this._removePlayer(id); }

  onPlayerHit({ playerId }) {
    const s = this.playerSprites.get(playerId);
    const p = this.playerData.get(playerId);

    // Death burst particles
    const bx = s?.container?.x ?? p?.x ?? 0;
    const by = s?.container?.y ?? p?.y ?? 0;
    const burstColorInt = parseInt(String(p?.color ?? '#ffffff').replace('#', ''), 16);
    const burst = this.add.particles(bx, by, 'dot', {
      speed: { min: 80, max: 220 },
      scale: { start: 1.1, end: 0 },
      lifespan: 500,
      tint: burstColorInt,
      emitting: false
    });
    burst.setDepth(6);
    burst.explode(20, bx, by);
    this.time.delayedCall(650, () => burst.destroy());

    if (playerId === this.playerId) this.cameras.main.shake(180, 0.012);

    if (s) {
      this.tweens.killTweensOf(s.container);
      s.container.setVisible(true).setAlpha(1);
      s.container.angle = 0;
      this.tweens.add({
        targets: s.container,
        angle: 360,
        scaleX: 0,
        scaleY: 0,
        alpha: 0,
        duration: 300,
        ease: 'Cubic.easeIn',
        onComplete: () => s.container.setVisible(false)
      });
    }
    if (playerId === this.playerId) this._showMsg('💀 You died! Respawning...');
    this.events.emit('kill_feed', { victimId: playerId });
  }

  onPlayerRespawn({ playerId }) {
    const s = this.playerSprites.get(playerId);
    if (s) {
      this.tweens.killTweensOf(s.container);
      s.container.setVisible(true).setAlpha(1);
      s.container.setScale(1);
      s.container.angle = 0;
    }
    if (playerId === this.playerId) this._showMsg('✅ Respawned!');
  }

  onPlayerEliminated({ playerId }) {
    const s = this.playerSprites.get(playerId);
    if (s) { s.container.destroy(); s.label.destroy(); this.playerSprites.delete(playerId); }
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
      this.territory?.setAnimOrigin(sprites.container.x, sprites.container.y);
      this.spray.predictLocalPaint(
        sprites.container.x,
        sprites.container.y,
        input.aimAngle,
        input.spraying,
        ownerIndex,
        local?.ink ?? this.localInk ?? 100
      );
    }

    // Spray arc cone indicator
    this.aimCone.clear();
    const canSpray = !!sprites && !!local?.alive && input.spraying && (local?.ink ?? this.localInk ?? 100) > 0;
    if (canSpray) {
      const range = 60;
      const tipWidth = 28;
      const px = sprites.container.x;
      const py = sprites.container.y;
      const a = input.aimAngle;
      const tx = px + Math.cos(a) * range;
      const ty = py + Math.sin(a) * range;
      const perpX = -Math.sin(a) * (tipWidth / 2);
      const perpY = Math.cos(a) * (tipWidth / 2);

      const colorInt = parseInt(String(local?.color ?? '#ffffff').replace('#', ''), 16);
      this.aimCone.setVisible(true);
      this.aimCone.fillStyle(colorInt, 0.25);
      this.aimCone.fillTriangle(px, py, tx + perpX, ty + perpY, tx - perpX, ty - perpY);
    } else {
      this.aimCone.setVisible(false);
    }

    // Walking bobble for all player sprites
    for (const [id, s] of this.playerSprites) {
      const baseY = s.baseY ?? s.container.y;
      const bob = Math.sin(time * 0.008) * 2;
      s.container.y = baseY + bob;
      if (s.glow?.visible) s.glow.setScale(1.15 + Math.sin(time * 0.01) * 0.05);
    }
  }

  // ─── Helpers ───────────────────────────────────────────────────────────────

  _ensureCharacterTextures() {
    if (!this.textures.exists('char_gun')) {
      const g = this.make.graphics({ add: false });
      g.fillStyle(0x333333, 1);
      g.fillRoundedRect(0, 2, 12, 4, 2);
      g.fillStyle(0xffffff, 0.25);
      g.fillTriangle(0, 2, 12, 2, 0, 6);
      g.generateTexture('char_gun', 12, 8);
      g.destroy();
    }

    for (const hex of PLAYER_COLORS) {
      const colorInt = parseInt(hex.replace('#', ''), 16);
      const r = (colorInt >> 16) & 0xff;
      const gv = (colorInt >> 8) & 0xff;
      const b = colorInt & 0xff;
      const lightInt =
        (Math.min(255, r + 60) << 16) | (Math.min(255, gv + 60) << 8) | Math.min(255, b + 60);
      const darkInt =
        (Math.max(0, r - 40) << 16) | (Math.max(0, gv - 40) << 8) | Math.max(0, b - 40);

      const texKey = 'char_base_' + hex.replace('#', '');
      if (this.textures.exists(texKey)) continue;

      const g = this.make.graphics({ add: false });
      // Shadow
      g.fillStyle(0x000000, 0.28);
      g.fillEllipse(17, 33, 20, 6);

      // Body (rounded rect + simple highlight)
      g.fillStyle(colorInt, 1);
      g.fillRoundedRect(6, 12, 22, 22, 6);
      g.fillStyle(lightInt, 0.45);
      g.fillRoundedRect(6, 12, 22, 10, { tl: 6, tr: 6, bl: 0, br: 0 });
      g.fillStyle(darkInt, 0.25);
      g.fillRoundedRect(6, 24, 22, 10, { tl: 0, tr: 0, bl: 6, br: 6 });

      // Head + eyes
      g.fillStyle(0xffcc99, 1);
      g.fillCircle(17, 10, 8);
      g.fillStyle(0xffffff, 0.35);
      g.fillCircle(15, 8, 4);

      g.fillStyle(0xffffff, 1);
      g.fillCircle(14, 9, 2.5);
      g.fillCircle(20, 9, 2.5);
      g.fillStyle(0x222222, 1);
      g.fillCircle(14, 10, 1.4);
      g.fillCircle(20, 10, 1.4);
      g.fillStyle(0xffffff, 1);
      g.fillCircle(15, 9, 0.7);
      g.fillCircle(21, 9, 0.7);

      g.generateTexture(texKey, 34, 36);
      g.destroy();
    }
  }

  _createPlayerSprite(p) {
    this._ensureCharacterTextures();

    const baseKey = 'char_base_' + p.color.replace('#', '');

    const glow = this.add.image(0, 0, baseKey)
      .setOrigin(0.5)
      .setBlendMode(Phaser.BlendModes.ADD)
      .setAlpha(0.55)
      .setScale(1.15)
      .setVisible(false);

    const base = this.add.image(0, 0, baseKey).setOrigin(0.5);

    const gun = this.add.image(0, 0, 'char_gun')
      .setOrigin(0, 0.5)
      .setTint(0x333333);

    const container = this.add.container(p.x, p.y, [glow, base, gun]).setDepth(5);

    const gunBaseX = 8;
    const gunBaseY = 4;
    gun.setPosition(gunBaseX, gunBaseY);

    const isOwn = p.id === this.playerId;
    const label = this.add.text(p.x, p.y - 26, isOwn ? 'YOU' : p.id.slice(0, 6), {
      fontSize: '10px',
      color: isOwn ? p.color : '#fff',
      stroke: '#000',
      strokeThickness: 2,
      fontStyle: isOwn ? 'bold' : 'normal'
    }).setOrigin(0.5).setDepth(6);

    this.playerSprites.set(p.id, { container, glow, base, gun, gunBaseX, gunBaseY, label, baseY: p.y });
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
    if (s) { s.container.destroy(); s.label.destroy(); }
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
