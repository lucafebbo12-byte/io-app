import Phaser from 'phaser';
import VirtualJoystickPlugin from 'phaser3-rex-plugins/plugins/virtualjoystick-plugin.js';
import { GameScene } from './scenes/GameScene.js';
import { HUDScene } from './scenes/HUDScene.js';
import { WinScene } from './scenes/WinScene.js';

const config = {
  type: Phaser.AUTO,
  width: window.innerWidth,
  height: window.innerHeight,
  backgroundColor: '#111118',
  antialias: true,
  pixelArt: false,
  roundPixels: false,
  input: {
    keyboard: true,
    mouse: true,
    touch: true,
    gamepad: false
  },
  plugins: {
    global: [{
      key: 'rexVirtualJoystick',
      plugin: VirtualJoystickPlugin,
      start: true
    }]
  },
  scene: [GameScene, HUDScene, WinScene],
  scale: {
    mode: Phaser.Scale.RESIZE,
    autoCenter: Phaser.Scale.CENTER_BOTH
  }
};

const game = new Phaser.Game(config);

// Also launch HUD in parallel with GameScene
game.events.on('ready', () => {
  game.scene.start('GameScene');
  game.scene.start('HUDScene');
});
