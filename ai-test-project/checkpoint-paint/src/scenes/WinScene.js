import Phaser from 'phaser';

export class WinScene extends Phaser.Scene {
  constructor() { super({ key: 'WinScene' }); }

  init(data) { this.data2 = data; }

  create() {
    const W = this.scale.width;
    const H = this.scale.height;
    const { winnerId, winnerColor, scores } = this.data2 || {};

    this.add.rectangle(W / 2, H / 2, W, H, 0x000000, 0.85);

    this.add.text(W / 2, H * 0.2, '🏆 GAME OVER', {
      fontSize: '36px', color: winnerColor || '#ffff00', stroke: '#000', strokeThickness: 5
    }).setOrigin(0.5);

    this.add.text(W / 2, H * 0.35, `Winner: ${winnerId || '???'}`, {
      fontSize: '24px', color: '#ffffff'
    }).setOrigin(0.5);

    // Scores list
    if (scores) {
      scores.sort((a, b) => b.score - a.score).slice(0, 5).forEach((s, i) => {
        this.add.text(W / 2, H * 0.48 + i * 28, `${i + 1}. ${s.id.slice(0, 8)} — ${s.score} tiles`, {
          fontSize: '16px', color: s.color
        }).setOrigin(0.5);
      });
    }

    // Restart button
    const btn = this.add.text(W / 2, H * 0.85, '[ PLAY AGAIN ]', {
      fontSize: '22px', color: '#00ff88', stroke: '#000', strokeThickness: 3
    }).setOrigin(0.5).setInteractive({ useHandCursor: true });

    btn.on('pointerdown', () => window.location.reload());
    btn.on('pointerover', () => btn.setColor('#ffffff'));
    btn.on('pointerout', () => btn.setColor('#00ff88'));
  }
}
