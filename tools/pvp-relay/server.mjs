import { createServer } from 'node:http';
import { randomUUID, timingSafeEqual } from 'node:crypto';
import { pathToFileURL } from 'node:url';
import { WebSocket, WebSocketServer } from 'ws';

const headerSize = 5;
const maxPacket = 1024 * 1024 + 65536;
const maxQueue = 4 * 1024 * 1024;
const maxRooms = 100;
const [arrived, left, data, started] = [1, 2, 3, 4];

function packet(kind, id, payload = Buffer.alloc(0)) {
  const result = Buffer.allocUnsafe(headerSize + payload.length);
  result[0] = kind;
  result.writeUInt32LE(id, 1);
  payload.copy(result, headerSize);
  return result;
}

function send(socket, message) {
  if (socket.readyState !== WebSocket.OPEN) return;
  if (socket.bufferedAmount + Buffer.byteLength(message) > maxQueue) {
    socket.terminate();
    return;
  }
  socket.send(message);
}

function descriptor(room) {
  return { ...room.info, players: 1 + room.guests.size, started: room.started };
}

function roomInfo(headers) {
  const name = decodeURIComponent(headers['x-pvp-name'] ?? '').trim();
  const protocol = headers['x-pvp-protocol'];
  const map = headers['x-pvp-map'];
  const seconds = Number(headers['x-pvp-seconds']);
  if (!name || name.length > 24 || /[\p{Cc}\p{Cf}]/u.test(name) ||
      !/^native-city-lan-v[0-9]{1,6}$/.test(protocol) || !/^[0-9a-f]{64}$/.test(map) ||
      !Number.isInteger(seconds) || seconds < 30 || seconds > 7200) {
    throw new Error('Invalid room settings');
  }
  return { id: randomUUID().replaceAll('-', ''), name, protocol, map, seconds };
}

function reject(socket, status) {
  socket.end(`HTTP/1.1 ${status}\r\nConnection: close\r\nContent-Length: 0\r\n\r\n`);
}

export async function startRelay({ access, port = 8080, bind = '127.0.0.1' } = {}) {
  if (typeof access !== 'string' || !/^[!-~]{24,256}$/.test(access)) {
    throw new Error('PVP_RELAY_ACCESS_TOKEN must contain 24–256 printable ASCII characters without spaces.');
  }
  const expected = Buffer.from(`Bearer ${access}`);
  function authorized(request) {
    const value = Buffer.from(request.headers.authorization ?? '');
    return value.length === expected.length && timingSafeEqual(value, expected);
  }

  const rooms = new Map();
  let probes = 0;
  const server = createServer((request, response) => {
    if (request.method !== 'GET' || request.url !== '/rooms') return response.writeHead(404).end();
    if (!authorized(request)) return response.writeHead(401).end();
    response.writeHead(200, { 'Content-Type': 'application/json', 'Cache-Control': 'no-store' });
    response.end(JSON.stringify({ rooms: [...rooms.values()].map(descriptor) }));
  });
  const websockets = new WebSocketServer({ noServer: true, maxPayload: maxPacket, perMessageDeflate: false });

  function hostRoom(socket, info) {
    const room = { info, host: socket, guests: new Map(), started: false, nextGuest: 1 };
    rooms.set(info.id, room);
    send(socket, JSON.stringify(descriptor(room)));
    socket.on('message', (message, binary) => {
      if (!binary || message.length < headerSize) return socket.close(1003);
      const id = message.readUInt32LE(1);
      const target = room.guests.get(id);
      switch (message[0]) {
        case data:
          if (target) send(target, packet(data, 0, message.subarray(headerSize)));
          break;
        case left:
          if (target && message.length === headerSize) {
            room.guests.delete(id);
            target.close(1000);
          }
          break;
        case started:
          if (id === 0 && message.length === headerSize) room.started = true;
          break;
        default:
          socket.close(1003);
      }
    });
    socket.on('close', () => {
      rooms.delete(info.id);
      for (const guest of room.guests.values()) guest.close(1001, 'Host left');
    });
  }

  function joinRoom(socket, room) {
    const id = room.nextGuest++;
    room.guests.set(id, socket);
    send(socket, JSON.stringify(descriptor(room)));
    send(room.host, packet(arrived, id));
    socket.on('message', (message, binary) => {
      if (!room.guests.has(id)) return;
      if (!binary || message.length < headerSize || message[0] !== data || message.readUInt32LE(1) !== 0) {
        return socket.close(1003);
      }
      send(room.host, packet(data, id, message.subarray(headerSize)));
    });
    socket.on('close', () => {
      if (room.guests.delete(id)) send(room.host, packet(left, id));
    });
  }

  function echo(socket) {
    probes++;
    const timeout = setTimeout(() => socket.terminate(), 5000);
    socket.once('close', () => { clearTimeout(timeout); probes--; });
    socket.once('message', (message, binary) => {
      if (!binary && message.length === 49 && /^pvp-connectivity-[0-9a-f]{32}$/.test(message.toString())) {
        send(socket, message.toString());
        socket.close(1000);
      } else socket.close(1003);
    });
  }

  server.on('upgrade', (request, socket, head) => {
    let attach;
    if (request.url === '/probe') {
      if (probes >= 8) return reject(socket, '503 Service Unavailable');
      attach = echo;
    } else {
      if (!authorized(request)) return reject(socket, '401 Unauthorized');
      if (request.url === '/host') {
        if (rooms.size >= maxRooms) return reject(socket, '503 Service Unavailable');
        try {
          const info = roomInfo(request.headers);
          attach = connection => hostRoom(connection, info);
        } catch {
          return reject(socket, '400 Bad Request');
        }
      } else {
        const id = /^\/join\/([0-9a-f]{32})$/.exec(request.url)?.[1];
        const room = rooms.get(id);
        if (!room || room.host.readyState !== WebSocket.OPEN) return reject(socket, '404 Not Found');
        if (room.guests.size >= 3) return reject(socket, '409 Conflict');
        attach = connection => joinRoom(connection, room);
      }
    }
    websockets.handleUpgrade(request, socket, head, connection => {
      connection.alive = true;
      connection.on('error', () => connection.terminate());
      connection.on('pong', () => { connection.alive = true; });
      attach(connection);
    });
  });

  const heartbeat = setInterval(() => {
    for (const socket of websockets.clients) {
      if (!socket.alive) socket.terminate();
      else { socket.alive = false; socket.ping(); }
    }
  }, 20000).unref();
  try {
    await new Promise((resolve, rejectListen) => {
      server.once('error', rejectListen);
      server.listen(port, bind, resolve);
    });
  } catch (error) {
    clearInterval(heartbeat);
    websockets.close();
    throw error;
  }
  return {
    port: server.address().port,
    close: () => {
      clearInterval(heartbeat);
      for (const socket of websockets.clients) socket.terminate();
      websockets.close();
      return new Promise(resolve => server.close(resolve));
    },
  };
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const service = await startRelay({
    access: process.env.PVP_RELAY_ACCESS_TOKEN,
    port: Number(process.env.PORT ?? 8080),
    bind: process.env.PVP_RELAY_BIND ?? '127.0.0.1',
  });
  console.log(`PvP relay listening on ${service.port}`);
}
