"""Pair -mapAudit overhead captures with Kakao skyview at the same centre and scale.

Game capture: camera 75 m up, vertical FOV 60, 1600x900, north up -> 154.0 x 86.6 m.
Kakao tiles: CONGNAMUL (Bessel TM, no datum shift, minus a measured offset), tile origin
(-30000, -60000), L0 = 0.125 m/px, 256 px tiles. Offset measured 2026-11-24 from two
map.kakao.com/link/map conversions (J7973 and the west edge): (1.79, 6.60) m.

Kakao imagery is for review only and stays in Git-ignored artifacts/.
Usage: python tools/kakao_aerial_compare.py <capture folder> <output folder>
"""
import io
import json
import math
import re
import sys
import time
import urllib.request
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont
from pyproj import Transformer

ROOT = Path(__file__).resolve().parents[1]
X0, Z0 = json.loads((ROOT / 'config/maps/eungye.json').read_text(encoding='utf-8-sig'))['bounds'][:2]
TO_LL = Transformer.from_crs(5186, 4326, always_xy=True)
TO_CONG = Transformer.from_crs(4326, '+proj=tmerc +lat_0=38 +lon_0=127 +k=1 +x_0=200000 +y_0=500000 +ellps=bessel +units=m +no_defs', always_xy=True)
OFFSET = (1.79, 6.60)
W_M, H_M = 2 * 75 * math.tan(math.radians(30)) * 16 / 9, 2 * 75 * math.tan(math.radians(30))
RES, TILE = 0.125, 256
CACHE = ROOT / 'artifacts/kakao-skyview-cache'


def cong(x, z):
    lon, lat = TO_LL.transform(X0 + x, Z0 + z)
    cx, cy = TO_CONG.transform(lon, lat)
    return cx - OFFSET[0], cy - OFFSET[1], lat, lon


def tile(row, col):
    CACHE.mkdir(parents=True, exist_ok=True)
    path = CACHE / f'L0_{row}_{col}.jpg'
    if not path.exists():
        url = f'https://map.kakaocdn.net/map_skyview/L0/{row}/{col}.jpg?v=160114'
        req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0', 'Referer': 'https://map.kakao.com/'})
        for attempt in range(3):
            try:
                path.write_bytes(urllib.request.urlopen(req, timeout=20).read()); break
            except Exception:
                time.sleep(1 + attempt)
        else:
            return Image.new('RGB', (TILE, TILE), (60, 60, 60))
    return Image.open(path).convert('RGB')


def aerial(x, z, w_m=W_M, h_m=H_M, out=(1600, 900)):
    cx, cy, lat, lon = cong(x, z)
    left, right, bottom, top = cx - w_m / 2, cx + w_m / 2, cy - h_m / 2, cy + h_m / 2
    span = TILE * RES
    c0, c1 = int((left + 30000) // span), int((right + 30000) // span)
    r0, r1 = int((bottom + 60000) // span), int((top + 60000) // span)
    mosaic = Image.new('RGB', ((c1 - c0 + 1) * TILE, (r1 - r0 + 1) * TILE))
    for r in range(r0, r1 + 1):
        for c in range(c0, c1 + 1):
            mosaic.paste(tile(r, c), ((c - c0) * TILE, (r1 - r) * TILE))
    ox, oy = c0 * span - 30000, (r1 + 1) * span - 60000  # mosaic top-left in CONG metres
    box = ((left - ox) / RES, (oy - top) / RES, (right - ox) / RES, (oy - bottom) / RES)
    return mosaic.crop(tuple(round(v) for v in box)).resize(out, Image.LANCZOS), (cx, cy, lat, lon)


def links(cx, cy, lat, lon):
    wx, wy = round(cx * 2.5), round(cy * 2.5)
    return dict(skyview=f'https://map.kakao.com/?urlX={wx}&urlY={wy}&urlLevel=1&map_type=TYPE_SKYVIEW',
                roadview=f'https://map.kakao.com/?map_type=TYPE_MAP&map_attribute=ROADVIEW&urlX={wx}&urlY={wy}&urlLevel=2',
                latitude=round(lat, 7), longitude=round(lon, 7))


def main(src, dst):
    src, dst = Path(src), Path(dst)
    dst.mkdir(parents=True, exist_ok=True)
    ledger = {j['id']: j for j in json.loads((ROOT / 'config/maps/eungye-intersection-review.json').read_text(encoding='utf-8-sig'))['junctions']}
    rows = []
    for line in (src / 'map-audit.txt').read_text(encoding='utf-8').splitlines():
        m = re.match(r'JUNCTION (\d+)-(.+?) pos=\(([-\d.]+), [-\d.]+, ([-\d.]+)\)', line)
        if not m:
            continue
        idx, jid = m.group(1), m.group(2)
        x, z = (ledger[jid]['x'], ledger[jid]['z']) if jid in ledger else (float(m.group(3)), float(m.group(4)))
        img, (cx, cy, lat, lon) = aerial(x, z)
        img.save(dst / f'{idx}-{jid}-kakao.jpg', quality=85)
        rows.append(dict(index=idx, junction=jid, x=x, z=z, game=f'{idx}-{jid}-top.jpg', kakao=f'{idx}-{jid}-kakao.jpg', **links(cx, cy, lat, lon)))
        print(idx, jid, flush=True)
    (dst / 'pairs.json').write_text(json.dumps(rows, ensure_ascii=False, indent=1), encoding='utf-8')


if __name__ == '__main__':
    main(*sys.argv[1:3])
