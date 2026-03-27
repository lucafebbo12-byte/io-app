import {
  BOT_COUNT, MAX_PLAYERS, ROUND_TIME, TILE_SIZE,
  TICK_RATE, BROADCAST_RATE, RESPAWN_DELAY, MAP_W, MAP_H,
  SPAWN_POINTS
} from '../shared/constants.js';
import { Player } from './Player.js';
import { Bot } from './Bot.js';
import { GameMap } from './Map.js';
import { Checkpoint } from './Checkpoint.js';
import { getConeTiles } from './SprayCone.js';

const TICK_MS = 1000 / TICK_RATE;
const BROADCAST_EVERY = TICK_RATE / BROADCAST_RATE; // every 2 ticks

export class GameRoom {
  constructor(io) {
    this.io = io;
    this.players = new Map();      // id → Player
    this.checkpoints = new Map();  // playerId → Checkpoint
    this.map = new GameMap();
    this.timeLeft = ROUND_TIME;
    this.gameOver = false;
    this._tick = 0;
    this._playerIndex = 0;

    this.countdown = 3 * TICK_RATE; // 3-second countdown before bots start

    this._addBots();
    this._prePaintSpawns();
    this._loop = setInterval(() => this._step(), TICK_MS);
  }

  _addBots() {
    for (let i = 0; i < BOT_COUNT; i++) {
      const bot = new Bot(`bot_${i}`, this._playerIndex++);
      this.players.set(bot.id, bot);
      this.checkpoints.set(bot.id, new Checkpoint(bot));
    }
  }

  _prePaintSpawns() {
    const PAINT_HALF = 15; // 30x30 tile square
    for (const p of this.players.values()) {
      const spawnIdx = p.index % SPAWN_POINTS.length;
      const sp = SPAWN_POINTS[spawnIdx];
      const cx = sp.x;
      const cy = sp.y;
      for (let dy = -PAINT_HALF; dy < PAINT_HALF; dy++) {
        for (let dx = -PAINT_HALF; dx < PAINT_HALF; dx++) {
          const tx = cx + dx;
          const ty = cy + dy;
          if (tx >= 0 && tx < MAP_W && ty >= 0 && ty < MAP_H) {
            this.map.paint(tx, ty, p.colorIndex);
          }
        }
      }
    }
    // Flush the pre-paint dirty tiles so they are sent on first delta
  }

  addPlayer(socket) {
    if (this.players.size >= MAX_PLAYERS) return;
    const player = new Player(socket.id, this._playerIndex++);
    this.players.set(socket.id, player);
    this.checkpoints.set(socket.id, new Checkpoint(player));

    // send initial state
    socket.emit('init', {
      playerId: socket.id,
      mapW: MAP_W,
      mapH: MAP_H,
      tileSize: TILE_SIZE,
      players: this._serializePlayers(),
      checkpoints: this._serializeCheckpoints(),
      timeLeft: this.timeLeft
    });
    socket.broadcast.emit('player_joined', player.serialize());
  }

  removePlayer(id) {
    this.players.delete(id);
    this.checkpoints.delete(id);
    this.io.emit('player_left', id);
  }

  handleInput(id, data) {
    const p = this.players.get(id);
    if (p && !p.isBot) p.applyInput(data);
  }

  _step() {
    if (this.gameOver) return;
    this._tick++;

    // Countdown before bots start
    if (this.countdown > 0) this.countdown--;

    const allPlayers = [...this.players.values()];
    const changedTiles = [];

    // bots think only after countdown
    for (const p of allPlayers) {
      if (p.isBot && p.alive && this.countdown <= 0) p.think(allPlayers);
    }

    // move all players
    for (const p of allPlayers) p.move();

    // spray paint
    for (const p of allPlayers) {
      if (!p.alive || !p.spraying || p.ink <= 0) continue;
      const tiles = getConeTiles(p.x, p.y, p.aimAngle);
      for (const t of tiles) {
        this.map.paint(t.x, t.y, p.colorIndex);
      }
    }

    const dirty = this.map.flushDirty();
    changedTiles.push(...dirty);

    // update ink (check own tile, pass map for zone detection)
    for (const p of allPlayers) {
      const onOwn = this.map.getOwner(p.tileX(), p.tileY()) === p.colorIndex;
      p.updateInk(onOwn, this.map);
      p.score = this.map.countTiles(p.colorIndex);
    }

    // check player hits (spray touched player tile)
    const hitSet = new Set();
    for (const painted of changedTiles) {
      for (const p of allPlayers) {
        if (!p.alive || hitSet.has(p.id)) continue;
        if (painted.owner !== p.colorIndex && painted.owner !== 0 &&
            painted.x === p.tileX() && painted.y === p.tileY()) {
          hitSet.add(p.id);
          this._killPlayer(p);
        }
      }
    }

    // check checkpoint destruction
    for (const [pid, cp] of this.checkpoints) {
      if (!cp.alive) continue;
      if (cp.checkDestruction(changedTiles, cp.ownerIndex)) {
        const owner = this.players.get(pid);
        if (owner) owner.checkpointAlive = false;
        this.io.emit('checkpoint_destroyed', { playerId: pid });
      }
    }

    // timer
    if (this._tick % TICK_RATE === 0) {
      this.timeLeft = Math.max(0, this.timeLeft - 1);
    }

    // win check
    this._checkWin();

    // broadcast delta at BROADCAST_RATE
    if (this._tick % BROADCAST_EVERY === 0) {
      this.io.emit('delta', {
        players: this._serializePlayers(),
        changedTiles,
        timeLeft: this.timeLeft,
        countdown: this.countdown > 0 ? Math.ceil(this.countdown / TICK_RATE) : 0
      });
    }
  }

  _killPlayer(player) {
    player.alive = false;
    this.io.emit('player_hit', { playerId: player.id });
    if (player.checkpointAlive) {
      setTimeout(() => {
        if (!this.players.has(player.id)) return;
        const cp = this.checkpoints.get(player.id);
        if (cp && cp.alive) {
          player.x = player.spawnX;
          player.y = player.spawnY;
          player.ink = 100;
          player.alive = true;
          this.io.emit('player_respawn', { playerId: player.id });
        } else {
          this.io.emit('player_eliminated', { playerId: player.id });
        }
      }, RESPAWN_DELAY);
    } else {
      this.io.emit('player_eliminated', { playerId: player.id });
    }
  }

  _checkWin() {
    if (this.gameOver) return;

    const aliveCheckpoints = [...this.checkpoints.values()].filter(c => c.alive);
    if (aliveCheckpoints.length === 1) {
      const winner = this.players.get(aliveCheckpoints[0].ownerId);
      this._endGame(winner);
      return;
    }

    if (this.timeLeft <= 0) {
      let best = null, bestScore = -1;
      for (const p of this.players.values()) {
        if (p.score > bestScore) { bestScore = p.score; best = p; }
      }
      this._endGame(best);
    }
  }

  _endGame(winner) {
    this.gameOver = true;
    clearInterval(this._loop);
    this.io.emit('game_over', {
      winnerId: winner?.id,
      winnerColor: winner?.color,
      scores: this._serializePlayers().map(p => ({ id: p.id, score: p.score, color: p.color }))
    });
  }

  _serializePlayers() {
    return [...this.players.values()].map(p => p.serialize());
  }

  _serializeCheckpoints() {
    return [...this.checkpoints.values()].map(c => c.serialize());
  }
}
