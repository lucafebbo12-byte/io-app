import Phaser from 'phaser';
import { INK_MAX } from '../../shared/constants.js';

export class HUDScene extends Phaser.Scene {
  constructor() { super({ key: 'HUDScene' }); }

  create() {
    const W = this.scale.width;
    const H = this.scale.height;

    // Ink bar background
    this.add.rectangle(W / 2, H - 30, 200, 16, 0x333333).setOrigin(0.5).setScrollFactor(0).setDepth(15);
    this.inkBar = this.add.rectangle(W / 2 - 100, H - 30, 200, 16, 0x00aaff).setOrigin(0, 0.5).setScrollFactor(0).setDepth(16);
    this.add.text(W / 2, H - 50, 'INK', { fontSize: '12px', color: '#aaaaff' }).setOrigin(0.5).setScrollFactor(0).setDepth(16);

    // Zone indicator text below ink bar
    this.zoneText = this.add.text(W / 2, H - 12, 'NEUTRAL', {
      fontSize: '11px', color: '#aaaaaa', stroke: '#000', strokeThickness: 2
    }).setOrigin(0.5).setScrollFactor(0).setDepth(16);

    this.inkLevel = INK_MAX;
    this.playerColor = 0x00aaff;
    this.zoneType = 'neutral';
    this.timeLeft = 999;

    const gameScene = this.scene.get('GameScene');

    gameScene.events.on('ink_update', (ink) => { this.inkLevel = ink; });

    gameScene.events.on('zone_update', (zone) => {
      this.zoneType = zone;
      if (zone === 'own') {
        this.zoneText.setText('ZONE OWN').setColor('#00ff88');
      } else if (zone === 'enemy') {
        this.zoneText.setText('ZONE ENEMY').setColor('#ff4444');
      } else {
        this.zoneText.setText('NEUTRAL').setColor('#aaaaaa');
      }
    });

    // Sync player color from GameScene after init
    gameScene.events.on('ink_update', (ink) => {
      this.inkLevel = ink;
      // Try to get player color from GameScene
      const gs = this.scene.get('GameScene');
      if (gs && gs.playerId && gs.playerData) {
        const pd = gs.playerData.get(gs.playerId);
        if (pd && pd.color) {
          this.playerColor = parseInt(pd.color.replace('#', ''), 16);
        }
      }
    });

    this.scores = [];
    this.scoreText = this.add.text(W - 10, 10, '', {
      fontSize: '12px',
      color: '#ffffff',
      stroke: '#000',
      strokeThickness: 3,
      align: 'right'
    }).setOrigin(1, 0).setScrollFactor(0).setDepth(16);

    gameScene.events.on('score_update', (scores) => {
      this.scores = scores || [];
      this._renderScores();
    });

    gameScene.events.on('time_update', (t) => { this.timeLeft = t; });

    // Kill feed: top-left, last 3 kills
    this._killFeedEntries = [];
    this.killFeedTexts = [];
    for (let i = 0; i < 3; i++) {
      const t = this.add.text(10, 10 + i * 18, '', {
        fontSize: '11px', color: '#ff4444', stroke: '#000', strokeThickness: 2
      }).setScrollFactor(0).setDepth(16).setAlpha(0);
      this.killFeedTexts.push(t);
    }

    gameScene.events.on('kill_feed', ({ victimId }) => {
      this._addKillFeed(`${(victimId || '').slice(0, 6)} eliminated`);
    });

    gameScene.events.on('checkpoint_feed', ({ playerId }) => {
      this._addKillFeed(`${(playerId || '').slice(0, 6)} checkpoint down`);
    });
  }

  _addKillFeed(msg) {
    this._killFeedEntries.unshift(msg);
    if (this._killFeedEntries.length > 3) this._killFeedEntries.length = 3;
    for (let i = 0; i < 3; i++) {
      const t = this.killFeedTexts[i];
      const entry = this._killFeedEntries[i];
      if (entry) {
        t.setText(entry).setAlpha(1);
        this.tweens.killTweensOf(t);
        this.tweens.add({ targets: t, alpha: 0, delay: 2500, duration: 400 });
      } else {
        t.setAlpha(0);
      }
    }
  }

  update() {
    const pct = this.inkLevel / INK_MAX;
    this.inkBar.setDisplaySize(200 * pct, 16);
    // Use player color for bar, fallback to red when low
    if (pct <= 0.3) {
      this.inkBar.setFillStyle(0xff4444);
    } else {
      this.inkBar.setFillStyle(this.playerColor);
    }

    // Timer pulse red when < 30s — update GameScene timer text color
    const gs = this.scene.get('GameScene');
    if (gs && gs.timerText) {
      gs.timerText.setColor(this.timeLeft < 30 ? '#ff4444' : '#ffffff');
    }
  }

  _renderScores() {
    if (!this.scoreText) return;
    const lines = ['TOP 5'];
    for (let i = 0; i < Math.min(5, this.scores.length); i++) {
      const s = this.scores[i];
      const name = (s.id || '').slice(0, 6);
      const score = Math.floor(s.score || 0);
      lines.push(`${i + 1}. ${name}  ${score}`);
    }
    this.scoreText.setText(lines.join('\n'));
  }
}
