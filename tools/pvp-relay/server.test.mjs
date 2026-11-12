import assert from 'node:assert/strict';
import { once } from 'node:events';
import { setTimeout as delay } from 'node:timers/promises';
import { test } from 'node:test';
import WebSocket from 'ws';
import { startRelay } from './server.mjs';

const access = 'relay-integration-test-token';
const authorization = { Authorization: `Bearer ${access}` };
const map = 'a'.repeat(64);

function frame(kind, id, payload = '') {
  const data = Buffer.alloc(5 + Buffer.byteLength(payload));
  data[0] = kind;
  data.writeUInt32LE(id, 1);
  data.write(payload, 5);
  return data;
}

async function connect(t, url, headers) {
  const socket = new WebSocket(url, { headers });
  t.after(() => socket.terminate());
  const pending = [];
  socket.on('message', (data, binary) => pending.push({ data, binary }));
  await once(socket, 'open');
  async function read() {
    if (!pending.length) await once(socket, 'message');
    return pending.shift();
  }
  return { socket, read };
}

test('lists an authorized room and relays only between host and selected guest', { timeout: 10000 }, async t => {
  const relay = await startRelay({ access, port: 0 });
  t.after(() => relay.close());
  const http = `http://127.0.0.1:${relay.port}`;
  const ws = `ws://127.0.0.1:${relay.port}`;
  const list = async () => (await fetch(`${http}/rooms`, { headers: authorization })).json();
  assert.equal((await fetch(`${http}/healthz`)).status, 204);
  assert.equal((await fetch(`${http}/rooms`)).status, 401);
  assert.deepEqual((await list()).rooms, []);

  const host = await connect(t, `${ws}/host`, {
    ...authorization,
    'X-Pvp-Name': encodeURIComponent('은계 택시'),
    'X-Pvp-Protocol': 'native-city-lan-v17',
    'X-Pvp-Map': map,
    'X-Pvp-Seconds': '900',
  });
  const room = JSON.parse((await host.read()).data.toString());
  assert.deepEqual((await list()).rooms, [room]);
  assert.equal(room.name, '은계 택시');
  assert.equal(room.players, 1);

  const guest = await connect(t, `${ws}/join/${room.id}`, authorization);
  assert.equal(JSON.parse((await guest.read()).data).id, room.id);
  const arrival = (await host.read()).data;
  const id = arrival.readUInt32LE(1);
  assert.deepEqual(arrival, frame(1, id));
  assert.notEqual(id, 0);
  assert.equal((await list()).rooms[0].players, 2);

  guest.socket.send(frame(3, 0, 'input'));
  assert.deepEqual((await host.read()).data, frame(3, id, 'input'));
  host.socket.send(frame(3, id, 'snapshot'));
  assert.deepEqual((await guest.read()).data, frame(3, 0, 'snapshot'));

  host.socket.send(frame(4, 0));
  let rooms = (await list()).rooms;
  for (let attempt = 0; !rooms[0]?.started && attempt < 20; attempt++) {
    await delay(10);
    rooms = (await list()).rooms;
  }
  assert.deepEqual(rooms.map(entry => entry.started), [true]);
  guest.socket.close();
  assert.deepEqual((await host.read()).data, frame(2, id));
  assert.equal((await list()).rooms[0].players, 1);
  const nextGuest = await connect(t, `${ws}/join/${room.id}`, authorization);
  await nextGuest.read();
  assert.notEqual((await host.read()).data.readUInt32LE(1), id);

  let lastGuest;
  let lastGuestId;
  for (let count = 2; count <= 3; count++) {
    const extra = await connect(t, `${ws}/join/${room.id}`, authorization);
    assert.equal(JSON.parse((await extra.read()).data).players, count + 1);
    const extraArrival = (await host.read()).data;
    assert.equal(extraArrival[0], 1);
    lastGuest = extra;
    lastGuestId = extraArrival.readUInt32LE(1);
  }
  await assert.rejects(connect(t, `${ws}/join/${room.id}`, authorization), /409/);
  assert.equal((await list()).rooms[0].players, 4);

  // A stalled guest cannot acknowledge the close handshake. Its seat is available immediately.
  lastGuest.socket.pause();
  host.socket.send(frame(2, lastGuestId));
  rooms = (await list()).rooms;
  for (let attempt = 0; rooms[0]?.players === 4 && attempt < 20; attempt++) {
    await delay(10);
    rooms = (await list()).rooms;
  }
  assert.equal(rooms[0].players, 3);
  const replacement = await connect(t, `${ws}/join/${room.id}`, authorization);
  assert.equal(JSON.parse((await replacement.read()).data).players, 4);
  assert.equal((await host.read()).data[0], 1);
  lastGuest.socket.resume();

  const hostClose = once(nextGuest.socket, 'close');
  host.socket.close();
  await hostClose;
  assert.deepEqual((await list()).rooms, []);
});
