"""Split the Eungye map into review zones and number every road piece (as seen on the M map).

Reads the unchanged baseline roads in config/maps/project/eungye.project.json.
Writes config/maps/eungye-road-zones.json (numbering, committed) and an overview PNG.
Usage: python tools/map_review_zones.py [overview.png] [audit map-audit.txt]
"""
import json
import math
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'config/maps/eungye-road-zones.json'

ZONES = {
    'A': ('남서 상업지', '검바위로·은계남로 서쪽 끝 블록'),
    'B': ('남부 중심가 격자', '은계중앙로 남쪽, 은계번영길·은계남로 격자'),
    'C': ('수인로 축', '수인로 큰길과 바로 붙은 교차로'),
    'D': ('중부 주거·공원', '은계로 중간, 은계호수로 남쪽 고리'),
    'E': ('북서 은행로 블록', '은행로·은행고길 주거 골목'),
    'F': ('북부 중앙', '대은로·은계로 대각선·계수로'),
    'G': ('동부 외곽', '마유로·동쪽 연결로'),
    'H': ('서부 은행로', '은행로 남쪽 구간'),
}


def zone(x, z):
    if x >= 1000:
        return 'G'
    if z < 330:
        return 'A' if x < 470 else 'B'
    if z < 600:
        return 'C'
    if z < 1050:
        return 'H' if x < 450 else 'D'
    if x < 435 and z < 1470:
        return 'E'
    return 'F'


def length(points):
    return sum(math.dist((a['x'], a['z']), (b['x'], b['z'])) for a, b in zip(points, points[1:]))


def build():
    roads = [r for r in json.loads((ROOT / 'config/maps/project/eungye.project.json').read_text(encoding='utf-8-sig'))['baseline']['roads'] if r['name'] != '인천-동해선']
    pieces = []
    for r in roads:
        p = r['points']
        mid = p[len(p) // 2]
        pieces.append(dict(id=r['id'], name=r['name'], zone=zone(mid['x'], mid['z']), width=r['width'], length=length(p), points=p))
    # union pieces of the same zone and same name (unnamed: only when their ends touch)
    parent = list(range(len(pieces)))

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i
    ends = [((p['points'][0]['x'], p['points'][0]['z']), (p['points'][-1]['x'], p['points'][-1]['z'])) for p in pieces]
    for i, a in enumerate(pieces):
        for j in range(i + 1, len(pieces)):
            b = pieces[j]
            if a['zone'] != b['zone'] or a['name'] != b['name']:
                continue
            if any(math.dist(u, v) < 4 for u in ends[i] for v in ends[j]):
                parent[find(i)] = find(j)
    groups = {}
    for i, p in enumerate(pieces):
        groups.setdefault(find(i), []).append(p)
    numbered = []
    for zid in ZONES:
        zg = [g for g in groups.values() if g[0]['zone'] == zid]
        zg.sort(key=lambda g: (g[0]['name'] == '', -sum(p['length'] for p in g)))
        for n, g in enumerate(zg, 1):
            total = sum(p['length'] for p in g)
            longest = max(g, key=lambda p: p['length'])
            mid = longest['points'][len(longest['points']) // 2]
            numbered.append(dict(number=f'{zid}-{n:02d}', zone=zid, name=g[0]['name'] or '(이름 없음)', lengthM=round(total), widthM=round(max(p['width'] for p in g), 1),
                                 label={'x': round(mid['x'], 1), 'z': round(mid['z'], 1)}, pieces=[p['id'] for p in g]))
    result = dict(schemaVersion=1, createdAt='2026-11-24', source='config/maps/project/eungye.project.json baseline.roads (철도 제외)',
                  note='리뷰용 구역·도로 번호. 게임 좌표(m), x 동쪽 z 북쪽. 구역 경계 규칙은 tools/map_review_zones.py zone().',
                  zones=[dict(id=k, name=v[0], description=v[1]) for k, v in ZONES.items()], roads=numbered)
    OUT.write_text(json.dumps(result, ensure_ascii=False, indent=1) + '\n', encoding='utf-8')
    return roads, numbered, pieces


def near(numbered, pieces_by_id, x, z, radius=30):
    hits = []
    for r in numbered:
        for pid in r['pieces']:
            pts = pieces_by_id[pid]
            if any(math.dist((p['x'], p['z']), (x, z)) < radius for p in pts):
                hits.append(r['number']); break
    return hits


def draw(png, audit):
    from PIL import Image, ImageDraw, ImageFont
    roads, numbered, pieces = build()
    by_id = {p['id']: p['points'] for p in pieces}
    s = 0.8
    W, H = int(2100 * s), int(1750 * s)
    colours = dict(A=(255, 236, 214), B=(255, 247, 200), C=(220, 236, 255), D=(222, 245, 222), E=(244, 226, 248), F=(230, 230, 250), G=(236, 236, 236), H=(255, 228, 228))
    im = Image.new('RGB', (W, H), (250, 250, 248))
    px = im.load()
    for y in range(0, H, 4):
        for x in range(0, W, 4):
            c = colours[zone(x / s, (H - y) / s)]
            for dy in range(4):
                for dx in range(4):
                    if x + dx < W and y + dy < H:
                        px[x + dx, y + dy] = c
    d = ImageDraw.Draw(im)
    P = lambda p: (p['x'] * s, H - p['z'] * s)
    for r in roads:
        d.line([P(p) for p in r['points']], fill=(120, 120, 120), width=max(2, int(r['width'] * s * .8)))
    big = ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', 40)
    f = ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', 13)
    centres = {}
    for x in range(0, 2100, 20):
        for z in range(0, 1750, 20):
            centres.setdefault(zone(x, z), []).append((x, z))
    for k, pts in centres.items():
        cx = sum(p[0] for p in pts) / len(pts); cz = sum(p[1] for p in pts) / len(pts)
        d.text((cx * s - 40, H - cz * s - 30), f'{k} {ZONES[k][0]}', font=ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', 22), fill=(90, 90, 120))
    for r in numbered:
        if r['lengthM'] < 25:
            continue
        x, y = r['label']['x'] * s, H - r['label']['z'] * s
        t = r['number'] + ('' if r['name'] == '(이름 없음)' else ' ' + r['name'])
        d.text((x, y), t, font=f, fill=(0, 0, 0), stroke_width=2, stroke_fill=(255, 255, 255))
    junctions = []
    if audit:
        for line in Path(audit).read_text(encoding='utf-8').splitlines():
            m = re.match(r'JUNCTION (\d+)-(.+?) pos=\(([-\d.]+), [-\d.]+, ([-\d.]+)\)', line)
            if m:
                x, z = float(m.group(3)), float(m.group(4))
                junctions.append(dict(index=m.group(1), junction=m.group(2), x=x, z=z, zone=zone(x, z), roads=near(numbered, by_id, x, z)))
                X, Y = x * s, H - z * s
                d.ellipse([X - 9, Y - 9, X + 9, Y + 9], fill=(220, 30, 30), outline=(255, 255, 255), width=2)
                d.text((X + 10, Y - 20), m.group(1), font=f, fill=(200, 0, 0), stroke_width=2, stroke_fill=(255, 255, 255))
    im.save(png)
    return junctions


if __name__ == '__main__':
    if len(sys.argv) > 1:
        js = draw(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else None)
        Path(sys.argv[1]).with_suffix('.json').write_text(json.dumps(dict(scale=0.8, width=1680, height=1400, junctions=js), ensure_ascii=False, indent=1), encoding='utf-8')
    else:
        build()
    print(OUT)
