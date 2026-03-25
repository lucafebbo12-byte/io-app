import express from 'express';
import { createServer } from 'http';
import { Server } from 'socket.io';
import { GameRoom } from './GameRoom.js';

const app = express();
const httpServer = createServer(app);
const io = new Server(httpServer, {
  cors: { origin: '*' }
});

const room = new GameRoom(io);

io.on('connection', (socket) => {
  room.addPlayer(socket);

  socket.on('input', (data) => room.handleInput(socket.id, data));
  socket.on('disconnect', () => room.removePlayer(socket.id));
});

const PORT = 3001;
httpServer.listen(PORT, () => console.log(`Server running on http://localhost:${PORT}`));
