"""Loopback-only single-client UDP impairment proxy for the PVP Player test.
This simulates transport conditions; it is not an internet/remote-PC test.
"""
import argparse, heapq, json, random, select, socket, time
p = argparse.ArgumentParser()
p.add_argument('--listen', type=int, required=True)
p.add_argument('--server', type=int, required=True)
p.add_argument('--rtt-ms', type=float, default=100)
p.add_argument('--jitter-ms', type=float, default=10)
p.add_argument('--loss', type=float, default=.01)
p.add_argument('--seconds', type=float, default=30)
p.add_argument('--blackout-start', type=float, default=0)
p.add_argument('--blackout-seconds', type=float, default=0)
p.add_argument('--allow-rebind', action='store_true')
a = p.parse_args()
if not (0 < a.listen < 65536 and 0 < a.server < 65536 and a.listen != a.server and 0 <= a.loss <= 1 and a.rtt_ms >= 0 and a.jitter_ms >= 0 and a.seconds > 0 and a.blackout_start >= 0 and a.blackout_seconds >= 0):
    p.error('invalid proxy parameters')
rng = random.Random(20261029)
server = ('127.0.0.1', a.server)
client = None
queue = []
received = dropped = forwarded = order = rebinds = blackout_dropped = 0
last_client = 0
started = time.monotonic()
def blackout():
    return a.blackout_start <= time.monotonic()-started < a.blackout_start+a.blackout_seconds
with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
    sock.bind(('127.0.0.1', a.listen))
    sock.setblocking(False)
    end = time.monotonic() + a.seconds
    print('PVP_PROXY_READY', flush=True)
    while time.monotonic() < end:
        if select.select([sock], [], [], .002)[0]:
            try:
                data, address = sock.recvfrom(65535)
            except ConnectionResetError:
                continue
            if address == server:
                destination = client
            else:
                if client is None or (a.allow_rebind and address != client and time.monotonic()-last_client > 1):
                    rebinds += int(client is not None)
                    client = address
                if address == client:
                    last_client = time.monotonic()
                destination = server if address == client else None
            if destination is not None:
                received += 1
                if blackout() or rng.random() < a.loss or len(queue) >= 2048:
                    blackout_dropped += int(blackout())
                    dropped += 1
                else:
                    due = time.monotonic() + max(0, a.rtt_ms/2 + rng.uniform(-a.jitter_ms, a.jitter_ms))/1000
                    order += 1
                    heapq.heappush(queue, (due, order, data, destination))
        now = time.monotonic()
        while queue and queue[0][0] <= now:
            _, _, data, destination = heapq.heappop(queue)
            if blackout():
                dropped += 1
                blackout_dropped += 1
            else:
                sock.sendto(data, destination)
                forwarded += 1
print(json.dumps(dict(rtt_ms=a.rtt_ms, jitter_ms=a.jitter_ms, loss=a.loss, received=received, dropped=dropped, forwarded=forwarded, rebinds=rebinds, blackout_dropped=blackout_dropped)))