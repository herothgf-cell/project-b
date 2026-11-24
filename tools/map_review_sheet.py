"""Build the 2026-11-24 Eungye map review sheet (before/after images + data for the HTML).

Inputs (all under artifacts/map-review-20261124/, Git-ignored because of Kakao imagery):
  game/NNN-*-top.jpg, kakao/NNN-*-kakao.jpg + pairs.json, judge/*.json, poles-near-lanes.json,
  overview/zones.json. Lane geometry: config/maps/project/eungye.project.json (document lanes).
Outputs: sheet/img/*.jpg, sheet/items.json
"""
import json
import math
import re
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / 'artifacts/map-review-20261124'
OUT = BASE / 'sheet'
IMG = OUT / 'img'
W_M, H_M = 2 * 75 * math.tan(math.radians(30)) * 16 / 9, 2 * 75 * math.tan(math.radians(30))
FONT = 'C:/Windows/Fonts/malgunbd.ttf'
CATS = dict(W='폭·인도·횡단보도', L='선형(자연스러움)', M='선 종류', F='차도 위 시설', N='차로 수', C='연결·교차로 모양', G='노면 표시', X='기타')
POST_EVIDENCE = [dict(src='img/rv-j866-southwest-arm.jpg', caption='카카오 로드뷰 2026-06 은행사거리(J866) 남서쪽: 표지는 신호등 가로대(차로 위)에 매달려 있고 기둥은 모서리 인도에 있다'),
                 dict(src='img/rv-j866-north.jpg', caption='같은 교차로 북쪽: 신호등·표지 기둥은 모두 인도 위')]
SEV_COL = {'상': (230, 40, 40), '중': (245, 150, 20), '하': (240, 220, 40)}

doc = json.loads((ROOT / 'config/maps/project/eungye.project.json').read_text(encoding='utf-8-sig'))['document']
LANES = [l for l in doc['lanes'] if not l.get('disabled') and len(l['points']) > 1]
SEG, WID = [], []
for l in LANES:
    for a, b in zip(l['points'], l['points'][1:]):
        SEG.append((a['x'], a['z'], b['x'], b['z'])); WID.append(l['width'])
SEG, WID = np.array(SEG), np.array(WID)


def margin(x, z):
    ax, az, bx, bz = SEG.T
    dx, dz = bx - ax, bz - az
    t = np.clip(((x - ax) * dx + (z - az) * dz) / (dx * dx + dz * dz + 1e-9), 0, 1)
    return np.hypot(x - ax - t * dx, z - az - t * dz) - WID / 2


def font(n):
    return ImageFont.truetype(FONT, n)


def cell_box(cells, w=1600, h=900):
    xs, ys = [], []
    for c in cells or []:
        m = re.match(r'([A-H])([1-5])', c.strip().upper())
        if m:
            i, j = 'ABCDEFGH'.index(m.group(1)), int(m.group(2)) - 1
            xs += [i * w / 8, (i + 1) * w / 8]; ys += [j * h / 5, (j + 1) * h / 5]
    return (min(xs), min(ys), max(xs), max(ys)) if xs else None


def lane_outline(draw, cx, cz, w=1600, h=900, colour=(0, 230, 255)):
    sx, sz = w / W_M, h / H_M
    P = lambda x, z: (w / 2 + (x - cx) * sx, h / 2 - (z - cz) * sz)
    for l in LANES:
        pts = l['points']
        if l['id'].startswith('join'):
            continue
        if all(abs(p['x'] - cx) > W_M and abs(p['z'] - cz) > H_M for p in pts):
            continue
        for side in (1, -1):
            line = []
            for i, p in enumerate(pts):
                a, b = pts[max(0, i - 1)], pts[min(len(pts) - 1, i + 1)]
                dx, dz = b['x'] - a['x'], b['z'] - a['z']; n = math.hypot(dx, dz) or 1
                ox, oz = dz / n * l['width'] / 2 * side, -dx / n * l['width'] / 2 * side
                line.append(P(p['x'] + ox, p['z'] + oz))
            draw.line(line, fill=colour, width=1)


def boxes(img, issues, after):
    d = ImageDraw.Draw(img)
    for n, it in enumerate(issues, 1):
        b = cell_box(it.get('cells'))
        if not b:
            continue
        col = (40, 210, 90) if after else SEV_COL.get(it.get('severity'), (230, 40, 40))
        d.rectangle([b[0] + 3, b[1] + 3, b[2] - 3, b[3] - 3], outline=col, width=5)
        d.rounded_rectangle([b[0] + 6, b[1] + 6, b[0] + 50, b[1] + 44], 8, fill=col)
        d.text((b[0] + 13, b[1] + 6), str(n), font=font(28), fill=(0, 0, 0) if col[1] > 200 else (255, 255, 255))
    return img


def junction_images(p, issues):
    g = Image.open(BASE / 'game' / p['game']).convert('RGB')
    k = Image.open(BASE / 'kakao' / p['kakao']).convert('RGB')
    before = boxes(g.copy(), issues, False)
    after = Image.blend(k, Image.new('RGB', k.size, (255, 255, 255)), .25)
    lane_outline(ImageDraw.Draw(after), p['x'], p['z'])
    after = boxes(after, issues, True)
    i = p['index']
    before.resize((1200, 675)).save(IMG / f'{i}-before.jpg', quality=80)
    after.resize((1200, 675)).save(IMG / f'{i}-after.jpg', quality=80)
    k.resize((1200, 675)).save(IMG / f'{i}-real.jpg', quality=80)
    return f'img/{i}-before.jpg', f'img/{i}-after.jpg', f'img/{i}-real.jpg'


def move_out(x, z):
    """Nearest point outside every lane with 0.6 m clearance (search rings)."""
    best = None
    for r in np.arange(.25, 3.01, .25):
        for a in range(0, 360, 10):
            px, pz = x + r * math.cos(math.radians(a)), z + r * math.sin(math.radians(a))
            if margin(px, pz).min() >= .6:
                best = (px, pz, r); break
        if best:
            break
    return best


def plan(name, x, z, target, kakao_pair, title):
    """Before/after plan drawing (36 x 36 m) of a post standing in a lane."""
    size, span = 700, 36.0
    s = size / span
    out = []
    for stage in ('before', 'after'):
        im = Image.new('RGB', (size, size), (214, 222, 206))
        d = ImageDraw.Draw(im)
        P = lambda px, pz: (size / 2 + (px - x) * s, size / 2 - (pz - z) * s)
        for l in LANES:
            pts = l['points']
            if all(math.dist((q['x'], q['z']), (x, z)) > span for q in pts):
                continue
            d.line([P(q['x'], q['z']) for q in pts], fill=(70, 72, 78), width=max(2, int(l['width'] * s)))
        for l in LANES:
            pts = l['points']
            if all(math.dist((q['x'], q['z']), (x, z)) > span for q in pts):
                continue
            d.line([P(q['x'], q['z']) for q in pts], fill=(140, 140, 150), width=1)
        f = font(18)
        if stage == 'before':
            X, Y = P(x, z)
            d.ellipse([X - 9, Y - 9, X + 9, Y + 9], fill=(230, 30, 30), outline=(255, 255, 255), width=3)
            d.text((X + 12, Y - 26), '지금: 차로 안', font=f, fill=(255, 255, 255), stroke_width=3, stroke_fill=(150, 0, 0))
        else:
            X, Y = P(x, z)
            d.ellipse([X - 7, Y - 7, X + 7, Y + 7], outline=(230, 30, 30), width=2)
            if target:
                TX, TY = P(target[0], target[1])
                d.line([X, Y, TX, TY], fill=(40, 200, 90), width=3)
                d.ellipse([TX - 9, TY - 9, TX + 9, TY + 9], fill=(40, 200, 90), outline=(255, 255, 255), width=3)
                d.text((TX + 12, TY - 26), f'인도로 {target[2]:.1f} m 이동', font=f, fill=(255, 255, 255), stroke_width=3, stroke_fill=(0, 110, 40))
            else:
                d.line([X - 12, Y - 12, X + 12, Y + 12], fill=(230, 30, 30), width=4); d.line([X - 12, Y + 12, X + 12, Y - 12], fill=(230, 30, 30), width=4)
                d.rounded_rectangle([X - 150, Y - 80, X + 150, Y - 24], 10, fill=(20, 120, 60))
                d.text((X - 140, Y - 76), '바닥 기둥 삭제 →', font=f, fill=(255, 255, 255))
                d.text((X - 140, Y - 52), '신호등 가로대(차로 위)에 표지 부착', font=f, fill=(255, 255, 255))
        d.rectangle([0, size - 34, size, size], fill=(0, 0, 0))
        d.text((8, size - 31), f'{title} · {"전" if stage == "before" else "후(시안)"} · 36 m 범위 · 북쪽 위', font=font(17), fill=(255, 255, 255))
        d.line([(size - 120, 20), (size - 120 + 10 * s, 20)], fill=(255, 255, 0), width=5)
        d.text((size - 120, 26), '10 m', font=f, fill=(0, 0, 0))
        path = IMG / f'{name}-{stage}.jpg'
        im.save(path, quality=85)
        out.append(f'img/{name}-{stage}.jpg')
    return out


def main():
    IMG.mkdir(parents=True, exist_ok=True)
    pairs = {p['index']: p for p in json.loads((BASE / 'kakao/pairs.json').read_text(encoding='utf-8'))}
    zones = {j['index']: j for j in json.loads((BASE / 'overview/zones.json').read_text(encoding='utf-8'))['junctions']}
    judged = {}
    for f in sorted((BASE / 'judge').glob('*.json')):
        for row in json.loads(f.read_text(encoding='utf-8-sig')):
            judged[str(row['index']).zfill(3)] = row
    items = []
    for idx, p in sorted(pairs.items()):
        row = judged.get(idx, dict(match='미판정', summary='판정 없음', issues=[], lanes=[]))
        issues = row.get('issues', [])
        before, after, real = junction_images(p, issues)
        z = zones.get(idx, {})
        items.append(dict(id=f'J{idx}', kind='junction', index=idx, junction=p['junction'], zone=z.get('zone', ''), roads=z.get('roads', []),
                          match=row.get('match'), summary=row.get('summary'), lanes=row.get('lanes', []), issues=issues,
                          before=before, after=after, real=real, x=p['x'], z=p['z'], links=dict(skyview=p['skyview'], roadview=p['roadview'])))
        print(idx, flush=True)
    poles = [h for h in json.loads((BASE / 'poles-near-lanes.json').read_text(encoding='utf-8')) if h['margin'] < 0]
    for n, h in enumerate(sorted(poles, key=lambda h: h['margin']), 1):
        owner = h['path'].split('/')[-2]
        kind = '신호등 기둥' if 'signal' in owner and 'uturn' not in owner else '유턴 조건 표지 기둥'
        near = min(zones.values(), key=lambda j: math.dist((j['x'], j['z']), (h['x'], h['z'])))
        target = move_out(h['x'], h['z'])
        b, a = plan(f'P{n:02d}', h['x'], h['z'], target, None, f'P{n:02d} {kind}')
        items.append(dict(id=f'P{n:02d}', kind='post', zone=near['zone'], roads=near['roads'], index=near['index'], junction=near['junction'],
                          match='심각' if h['margin'] < -1 else '보통', summary=f'{kind}이 차로 안 {-h["margin"]:.1f} m 들어와 있음 (차로 폭 {h["width"]} m)',
                          issues=[dict(cat='F', severity='상' if h['margin'] < -1 else '중', cells=[],
                                       before=f'{kind}({owner})의 받침 기둥이 게임 좌표 ({h["x"]:.1f}, {h["z"]:.1f})에 있다. 가장 가까운 차로 {h["lane"]}의 가장자리에서 {-h["margin"]:.2f} m 안쪽이라 차가 기둥에 부딪힌다.',
                                       after=(f'기둥을 ({target[0]:.1f}, {target[1]:.1f})로 {target[2]:.1f} m 옮겨 모든 차로 가장자리에서 0.6 m 이상 떨어진 인도에 세운다. 표지판은 기둥 위에서 차로 쪽을 바라보게 둔다.' if target else '3 m 안에 차로 밖 자리가 없다(차로 한가운데). 바닥 기둥을 없애고, 실제 한국 교차로처럼 같은 방향 신호등 가로대(차로 위 4.5 m 이상)에 유턴 보조표지를 매단다. 신호등 기둥 자체는 인도에 있어야 한다.'),
                                       confidence='높음', roadview=False)],
                          before=b, after=a, real=None, evidence=POST_EVIDENCE, x=h['x'], z=h['z'], links=dict(skyview=pairs[near['index']]['skyview'], roadview=pairs[near['index']]['roadview'])))
    ev_path = BASE / 'evidence.json'
    if ev_path.exists():
        extra = json.loads(ev_path.read_text(encoding='utf-8'))
        for it in items:
            it.setdefault('evidence', [])
            it['evidence'] = (it.get('evidence') or []) + extra.get(it['id'], [])
    (OUT / 'items.json').write_text(json.dumps(items, ensure_ascii=False, indent=1), encoding='utf-8')
    print('items', len(items))


if __name__ == '__main__':
    main()
