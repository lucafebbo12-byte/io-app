import { io } from 'socket.io-client';

export class NetworkManager {
  constructor(gameScene) {
    this.scene = gameScene;
    this.socket = io(window.location.origin);
    this.playerId = null;
    this._bind();
  }

  _bind() {
    this.socket.on('init', (data) => {
      this.playerId = data.playerId;
      // Decode compact binary tile format into {x, y, owner} objects
      if (data.binaryTiles && Array.isArray(data.initialTiles)) {
        const flat = data.initialTiles;
        const decoded = [];
        for (let i = 0; i < flat.length; i += 3) {
          decoded.push({ x: flat[i], y: flat[i + 1], owner: flat[i + 2] });
        }
        data = { ...data, initialTiles: decoded };
      }
      this.scene.onInit(data);
    });
    this.socket.on('delta', (data) => this.scene.onDelta(data));
    this.socket.on('player_joined', (p) => this.scene.onPlayerJoined(p));
    this.socket.on('player_left', (id) => this.scene.onPlayerLeft(id));
    this.socket.on('player_hit', (d) => this.scene.onPlayerHit(d));
    this.socket.on('player_respawn', (d) => this.scene.onPlayerRespawn(d));
    this.socket.on('player_eliminated', (d) => this.scene.onPlayerEliminated(d));
    this.socket.on('checkpoint_damaged', (d) => this.scene.onCheckpointDamaged(d));
    this.socket.on('checkpoint_destroyed', (d) => this.scene.onCheckpointDestroyed(d));
    this.socket.on('game_over', (d) => this.scene.onGameOver(d));
    this.socket.on('bullet_fired', (d) => this.scene.onBulletFired(d));
    this.socket.on('bullet_removed', (d) => this.scene.onBulletRemoved(d));
    this.socket.on('player_damaged', (d) => this.scene.onPlayerDamaged(d));
  }

  sendInput(input) {
    this.socket.emit('input', input);
  }

  sendShoot(angle) {
    this.socket.emit('shoot', { angle });
  }
}
