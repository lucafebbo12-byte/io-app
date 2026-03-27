import Phaser from 'phaser';
import { INK_MAX } from '../../shared/constants.js';

export class HUDScene extends Phaser.Scene {
  constructor() {
    super({ key: 'HUDScene' });
  }

  create() {
    const W = this.scale.width;
    const H = this.scale.height;

    this.inkLevel = INK_MAX;
    this.inkVisual = INK_MAX;
    this.timeLeft = 0;
    this.zoneType = 'neutral';
    this.selfColorInt = 0x00aaff;
    this.selfColorHex = '#00aaff';

    // --- Timer (top-center)
    this.timerText = this.add
      .text(W / 2, 14, '', {
        fontSize: '32px',
        color: '#ffffff',
        stroke: '#000',
        strokeThickness: 6
      })
      .setOrigin(0.5, 0)
      .setScrollFactor(0)
      .setDepth(50);

    // --- Scoreboard (top-right)
    this.scoreRows = [];
    const scoreX = W - 12;
    const scoreY = 70;
    for (let i = 0; i < 5; i++) {
      const rowY = scoreY + i * 20;
      const dot = this.add
        .circle(scoreX - 130, rowY + 8, 5, 0xffffff)
        .setOrigin(0.5)
        .setScrollFactor(0)
        .setDepth(50);

      const text = this.add
        .text(scoreX, rowY, '', {
          fontSize: '14px',
          color: '#ffffff',
          stroke: '#000',
          strokeThickness: 4,
          align: 'right'
        })
        .setOrigin(1, 0)
        .setScrollFactor(0)
        .setDepth(50);

      this.scoreRows.push({ dot, text });
    }

    // --- Ink "spray can" bar (bottom-center)
    this.inkLabel = this.add
      .text(W / 2, H - 64, 'INK', { fontSize: '12px', color: '#ffffff' })
      .setOrigin(0.5)
      .setScrollFactor(0)
      .setDepth(50);

    this.zoneText = this.add
      .text(W / 2, H - 18, 'NEUTRAL', { fontSize: '12px', color: '#ffffff' })
      .setOrigin(0.5)
      .setScrollFactor(0)
      .setDepth(50);

    this.inkGfx = this.add.graphics().setScrollFactor(0).setDepth(50);

    // --- Kill feed (top-left)
    this.feed = [];
    this.feedMax = 5;

    // Events from GameScene
    const game = this.scene.get('GameScene');
    game.events.on('ink_update', (ink) => {
      this.inkLevel = ink ?? this.inkLevel;
    });
    game.events.on('score_update', (scores) => this._renderScores(scores || []));
    game.events.on('time_update', (t) => {
      this.timeLeft = t ?? this.timeLeft;
      this.timerText.setText(`${this.timeLeft}s`);
    });
    game.events.on('zone_update', (z) => {
      this.zoneType = z || 'neutral';
      this._renderZone();
    });
    game.events.on('kill_feed', ({ victimId }) => {
      const name = this._formatName(victimId);
      this._addFeed(`${name} ☠️`, '#ffffff');
    });
    game.events.on('checkpoint_feed', ({ playerId }) => {
      const name = this._formatName(playerId);
      this._addFeed(`🏳️ ${name} base destroyed!`, '#ff4444');
    });
    game.events.on('self_color', ({ color }) => {
      if (!color) return;
      this.selfColorHex = color;
      this.selfColorInt = parseInt(color.replace('#', ''), 16);
    });

    this._renderZone();
  }

  update() {
    // Smooth ink bar
    this.inkVisual = Phaser.Math.Linear(this.inkVisual, this.inkLevel, 0.2);
    this._renderInkBar();
    this._renderTimerPulse();
  }

  _renderInkBar() {
    const W = this.scale.width;
    const H = this.scale.height;

    const canW = 230;
    const canH = 18;
    const x = W / 2 - canW / 2;
    const y = H - 46;

    const pad = 3;
    const innerW = canW - pad * 2;
    const pct = Phaser.Math.Clamp(this.inkVisual / INK_MAX, 0, 1);
    const fillW = Math.max(0, innerW * pct);

    this.inkGfx.clear();

    // Can background
    this.inkGfx.fillStyle(0x0b0b12, 0.7);
    this.inkGfx.fillRoundedRect(x, y, canW, canH, 8);

    // Fill
    this.inkGfx.fillStyle(this.selfColorInt, 1);
    this.inkGfx.fillRoundedRect(x + pad, y + pad, fillW, canH - pad * 2, 6);

    // Outline (glow when full)
    const isFull = pct >= 0.999;
    if (isFull) {
      this.inkGfx.lineStyle(4, this.selfColorInt, 0.45);
      this.inkGfx.strokeRoundedRect(x - 1, y - 1, canW + 2, canH + 2, 9);
    }

    this.inkGfx.lineStyle(2, 0xffffff, 0.55);
    this.inkGfx.strokeRoundedRect(x, y, canW, canH, 8);

    // Top cap
    this.inkGfx.fillStyle(0xffffff, 0.2);
    this.inkGfx.fillRoundedRect(W / 2 - 26, y - 9, 52, 8, 4);
  }

  _renderTimerPulse() {
    if (!this.timerText) return;
    if (this.timeLeft > 0 && this.timeLeft < 30) {
      const t = this.time.now * 0.01;
      const s = 1 + Math.sin(t) * 0.06;
      this.timerText.setColor('#ff4444');
      this.timerText.setScale(s);
    } else {
      this.timerText.setColor('#ffffff');
      this.timerText.setScale(1);
    }
  }

  _renderScores(scores) {
    const top = scores.slice(0, 5);
    for (let i = 0; i < 5; i++) {
      const row = this.scoreRows[i];
      const s = top[i];
      if (!s) {
        row.text.setText('');
        row.dot.setVisible(false);
        continue;
      }
      row.dot.setVisible(true);
      const c = parseInt(String(s.color || '#ffffff').replace('#', ''), 16);
      row.dot.setFillStyle(c, 1);
      row.text.setText(`${i + 1}. ${this._formatName(s.id)}  ${Math.floor(s.score || 0)}`);
    }
  }

  _renderZone() {
    if (!this.zoneText) return;
    if (this.zoneType === 'own') {
      this.zoneText.setText('OWN ZONE ⚡').setColor('#ffffff');
    } else if (this.zoneType === 'enemy') {
      this.zoneText.setText('ENEMY ZONE ⚠️').setColor('#ff6666');
    } else {
      this.zoneText.setText('NEUTRAL').setColor('#ffffff');
    }
  }

  _addFeed(text, color) {
    const x = 12;
    const y = 60;
    const lineH = 18;

    // Shift existing lines down
    for (const item of this.feed) item.text.y += lineH;

    const t = this.add
      .text(x, y, text, { fontSize: '14px', color, stroke: '#000', strokeThickness: 4 })
      .setOrigin(0, 0)
      .setScrollFactor(0)
      .setDepth(50);

    this.feed.unshift({ text: t });
    while (this.feed.length > this.feedMax) {
      this.feed.pop().text.destroy();
    }

    this.tweens.add({
      targets: t,
      alpha: 0,
      duration: 250,
      delay: 1750,
      onComplete: () => {
        const idx = this.feed.findIndex(f => f.text === t);
        if (idx >= 0) this.feed.splice(idx, 1);
        t.destroy();
      }
    });
  }

  _formatName(id) {
    if (!id) return '???';
    const s = String(id);
    if (s.startsWith('bot_')) {
      const n = s.slice(4);
      return `BOT_${n}`;
    }
    return s.slice(0, 6);
  }
}
