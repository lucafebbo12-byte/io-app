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

    this.inkLevel = INK_MAX;
    this.scene.get('GameScene').events.on('ink_update', (ink) => { this.inkLevel = ink; });

    this.scores = [];
    this.scoreText = this.add.text(W - 10, 10, '', {
      fontSize: '12px',
      color: '#ffffff',
      stroke: '#000',
      strokeThickness: 3,
      align: 'right'
    }).setOrigin(1, 0).setScrollFactor(0).setDepth(16);

    this.scene.get('GameScene').events.on('score_update', (scores) => {
      this.scores = scores || [];
      this._renderScores();
    });
  }

  update() {
    const pct = this.inkLevel / INK_MAX;
    this.inkBar.setDisplaySize(200 * pct, 16);
    this.inkBar.setFillStyle(pct > 0.3 ? 0x00aaff : 0xff4444);
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
