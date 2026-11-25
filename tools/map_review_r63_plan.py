"""Roadview-confirmed school-zone marks (map r62 -> r63, 2026-11-26).

Roadview (2026-06) confirmed these; positions are read off a gridded Kakao aerial in world metres
(the sheet's pixel numbers are unreliable), and each crossing spans the game lanes it cuts
(map_review_marks.crossing / arm_crossing).
- yellow crosswalks: J048-3 (J3063 south-east arm), J065-4/J070-2 (Eunbit school road, two across the
  road and one across the J8761 alley mouth), J033-1/J035-1 (Eungye library road, two across the main
  road and two across the side roads joining from the west);
- raised red crossings (red paving 3 m deep across the road; the white triangles are not drawn):
  J067-4/J071-2/J073-1 at the alley junctions J8385, J8762, J9150 and J065-4 at the J8314 alley mouth;
- red bands just outside the J037 west and south crosswalks (J037-2);
- school-zone text: J048-3 behind its new crossing (text then "30"), J029-3 west of J1310.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r63_plan.py [--check <dir>]
Output: config/maps/review/r63-school-marks.json (MapReviewCrosswalkEdits.ApplyR63)
"""
import json
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_review_marks as M  # noqa: E402

OUT = M.ROOT / 'config/maps/review/r63-school-marks.json'
E, N = (1.0, 0.0), (0.0, 1.0)
WNW = (-.97, .24)  # side roads joining the library road from the west
# (refs, card, point (x, z), road direction)
YELLOW = [
    ('J065-4, J070-2', 'J070', (865.5, 86.5), E), ('J065-4, J070-2', 'J070', (884.5, 86.5), E),
    ('J070-2', 'J070', (876.0, 92.5), N),
    ('J033-1, J035-1', 'J035', (185.0, 153.0), N), ('J033-1', 'J033', (184.5, 213.5), N),
    ('J033-1', 'J033', (178.5, 191.0), WNW), ('J035-1', 'J035', (178.5, 143.5), WNW),
]
RAISED = [
    ('J067-4', 'J067', (827.8, 257.5), E), ('J067-4, J071-2', 'J067', (847.5, 257.5), E),
    ('J071-2', 'J071', (865.6, 257.5), E), ('J071-2', 'J071', (885.0, 257.5), E), ('J071-2', 'J071', (876.0, 245.8), N),
    ('J073-1', 'J073', (886.0, 167.5), E), ('J073-1', 'J073', (904.4, 167.5), E), ('J073-1', 'J073', (915.0, 155.5), N),
    ('J065-4', 'J065', (833.5, 95.5), N),
]


def centre(card):
    it = M.ITEMS[card]; return (it['x'], it['z'])


def plan():
    out = []
    for ref, card, p, d in YELLOW:
        g = M.crossing(p, direction=d)
        out.append(dict(refs=ref, card=card, kind='crosswalk', type=M.YELLOW, text='게임 설정', **{k: g[k] for k in ('x', 'z', 'yaw', 'width', 'length')}))
    for ref, card, p, d in RAISED:
        g = M.crossing(p, depth=3.0, direction=d, pad=.3)
        out.append(dict(refs=ref, card=card, kind='line', type=M.PAVING, text='게임 설정', **{k: g[k] for k in ('x', 'z', 'yaw', 'width', 'length')}))
    c = centre('J048'); found = M.arms(c); b = min(found, key=lambda a: abs((a - 140.9 + 180) % 360 - 180))
    g = M.arm_crossing(c, b, found, dist=4.5)  # the aerial crossing sits right at the junction mouth
    out.append(dict(refs='J048-3', card='J048', kind='crosswalk', type=M.YELLOW, text='게임 설정', **{k: g[k] for k in ('x', 'z', 'yaw', 'width', 'length')}))
    dc = math.dist(c, (g['x'], g['z']))
    for dist, text in ((dc + 6, '어린이\n보호구역'), (dc + 11, '30')):
        for q in M.lane_points_on_arm(c, b, dist, True):
            out.append(dict(refs='J048-3', card='J048', kind='line', type=M.TEXT, text=text, x=q['x'], z=q['z'], yaw=q['yaw'], width=q['lanewidth'] * .8, length=3.2 if text == '30' else 4.2))
    # J037: red bands just outside the west and south crosswalks (the aerial shows red there, white stripes on the north arm)
    for p in [q for b in (289.0, 187.6) for q in M.paving_arm(centre('J037'), b, 12.3, 15.8, None)]:
        out.append(dict(refs='J037-2', card='J037', kind='line', type=M.PAVING, text='게임 설정', **{k: p[k] for k in ('x', 'z', 'yaw', 'width', 'length')}))
    q = M.on_lane((125.5, 1247.5), width=None, length=4.2)
    out.append(dict(refs='J029-3', card='J029', kind='line', type=M.TEXT, text='어린이\n보호구역', x=q['x'], z=q['z'], yaw=q['yaw'], width=q['width'], length=4.2))
    for n, e in enumerate(out):
        e.update(id=f"review:r63:{e['card']}:{e['kind']}:{n + 1:03d}", junctionId='', axis=0, sign=1, existing='')
        for k in ('x', 'z', 'yaw', 'width', 'length'):
            e[k] = round(float(e[k]), 3)
        e['width'] = min(e['width'], 40.0)
    return out


def check(out, folder):
    from PIL import Image, ImageDraw, ImageFont
    from kakao_aerial_compare import aerial
    folder = Path(folder); folder.mkdir(parents=True, exist_ok=True)
    F = ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', 18)
    colours = {M.PAVING: (255, 40, 40), M.TEXT: (255, 255, 255), M.YELLOW: (255, 230, 0)}
    for card in sorted({e['card'] for e in out}):
        pts = [(e['x'], e['z']) for e in out if e['card'] == card]
        cx = (min(p[0] for p in pts) + max(p[0] for p in pts)) / 2; cz = (min(p[1] for p in pts) + max(p[1] for p in pts)) / 2
        S, P = 90.0, 900
        im, _ = aerial(cx, cz, S, S, (P, P)); d = ImageDraw.Draw(im)
        def Q(x, z): return ((x - cx) / S * P + P / 2, P / 2 - (z - cz) / S * P)
        for l in M.LANES:
            q = [Q(p['x'], p['z']) for p in l['points']]
            d.line(q, fill=(255, 0, 255), width=1)
        for e in out:
            y = math.radians(e['yaw']); f = (math.sin(y), math.cos(y)); r = (f[1], -f[0])
            poly = [Q(e['x'] + r[0] * a * e['width'] / 2 + f[0] * b * e['length'] / 2, e['z'] + r[1] * a * e['width'] / 2 + f[1] * b * e['length'] / 2) for a, b in [(-1, -1), (1, -1), (1, 1), (-1, 1)]]
            d.polygon(poly, outline=colours.get(e['type'], (0, 255, 255)), width=3)
        d.text((8, 6), f'{card} r63 계획(노랑 횡단보도·빨강 포장·흰 글자)', font=F, fill='white')
        im.save(folder / f'{card}.jpg', quality=82)


if __name__ == '__main__':
    out = plan()
    OUT.write_text(json.dumps(dict(fromRevision=62, toRevision=63, edits=out), ensure_ascii=False, indent=1), encoding='utf-8')
    if '--check' in sys.argv:
        check(out, sys.argv[sys.argv.index('--check') + 1])
    from collections import Counter
    print(OUT, len(out), Counter((e['kind'], e['type']) for e in out))
    for e in out:
        print(e['refs'], e['kind'], e['type'], e['x'], e['z'], e['yaw'], e['width'], e['length'])
