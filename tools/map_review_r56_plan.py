"""Markings batch 2 plan (map r55 -> r56, 2026-11-25 map review).

The sheet's pixel numbers turned out unreliable, so positions come from the game itself:
- junction crosswalks: arms found from the lanes around each junction (map_review_marks.arms), placed
  like generated crosswalks (half the widest other road + 3 m) and spanning the lanes they cut;
- midblock crosswalks: the crossing's centre read off the sheet's Kakao image (same 1600x900 frame as
  the game capture), direction and span from the game lanes;
- school-zone paving / "30" / text / diamonds: lanes on a given arm and distance.
Every result is drawn over the Kakao aerial and the game capture for checking (--check).
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r56_plan.py <map-audit.txt of r55 dump> [--check <dir>]
Output: config/maps/review/r56-mark-edits.json (applied by MapReviewCrosswalkEdits.ApplyR56)
"""
import json
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_review_marks as M  # noqa: E402
from map_crosswalk_approaches import approaches  # noqa: E402

OUT = M.ROOT / 'config/maps/review/r56-mark-edits.json'
Y, Wh, BIKE = M.YELLOW, M.WHITE, '자전거횡단도:바깥쪽'

# card: [(arm bearing, colour, bike)], refs
JUNCTION_CROSSWALKS = {
    'J037': ('J037-1', [(289.0, Y, False), (15.4, Wh, False), (187.6, Y, False)]),
    'J042': ('J042-3', [(25.1, Y, False), (112.7, Y, False), (198.0, Y, False)]),
    'J049': ('J049-3', [(160.8, Wh, False), (249.3, Wh, False), (340.9, Wh, False)]),
    'J050': ('J050-2', [(78.8, Wh, False), (170.4, Wh, False), (257.8, Wh, False), (352.7, Wh, False)]),
    'J051': ('J051-4', [(1.0, Y, False), (181.2, Y, False), (270.0, Y, False)]),
    'J052': ('J052-2', [(86.8, Y, False, 14.0), (251.0, Y, False), (318.5, Y, False)]),
    'J055': ('J055-1', [(261.1, Wh, False)]),
    'J061': ('J061-1', [(355.2, Wh, False), (175.1, Wh, False), (252.1, Wh, True)]),
    'J062': ('J062-1', [(355.6, Wh, False), (262.8, Wh, True), (165.1, Wh, False)]),
    'J068': ('J068-1', [(322.5, Wh, False), (24.2, Wh, True), (235.0, Wh, True)]),
    'J072': ('J072-1', [(295.9, Wh, True), (95.9, Wh, True), (192.8, Wh, True)]),
}
# midblock / single crossings: centre on the sheet's Kakao image (u, v)
MIDBLOCK = [
    ('J027-1', 'J027', (525, 525), Wh), ('J027-2', 'J027', (1167, 452), Wh),
    ('J029-1', 'J029', (850, 350), Y),
    ('J054-4', 'J054', (630, 800), Wh), ('J056-4', 'J056', (870, 335), Wh), ('J060-1', 'J060', (725, 419), Wh, 100.0),  # J060: just west of the side entrance, along the main road
]
# school-zone paving: (refs, card, arm bearing, d0, d1, inbound)  inbound None = both directions
PAVING = [
    ('J018-3', 'J018', 270, 19.5, 45, None),
    ('J019-4', 'J019', 180, 13.5, 43, None),
    ('J021-1', 'J021', 0, 17.5, 40, True), ('J021-1', 'J021', 180, 17.5, 40, True),
    ('J021-1', 'J021', 270, 21.5, 45, None), ('J021-1', 'J021', 90, 21.5, 45, None),
    ('J042-4', 'J042', 25.1, 21, 45, None), ('J042-4', 'J042', 198, 20, 45, None),
    ('J051-4', 'J051', 181.2, 13, 35, None),
    ('J052-3', 'J052', 86.8, 13, 35, None), ('J052-3', 'J052', 251.0, 13, 35, None), ('J052-3', 'J052', 318.5, 13, 35, None),
    ('J063-4, J066-3', 'J018', 0, 38, 55, True),   # southbound approach north of J7841 (B4~B5 / D5 of the mid-block cards)
    ('J065-2', 'J018', 0, 14.5, 38, True), ('J065-2', 'J018', 180, 14.5, 40, True),
]
# road text: (refs, card, arm bearing, distance, inbound, text)
TEXT = [
    ('J018-3', 'J018', 270, 26, True, '30'),
    ('J019-4', 'J019', 180, 26, True, '30'),
    *[('J021-1', 'J021', b, 27, True, t) for b in (0, 90, 180, 270) for t in ('30',)],  # paving starts 2.5 m outside each crosswalk
    *[('J021-1', 'J021', b, 31, True, '어린이\n보호구역') for b in (0, 90, 180, 270)],
    *[('J052-3', 'J052', b, 22, True, '어린이\n보호구역') for b in (86.8, 251.0, 318.5)],
    ('J063-4, J066-3', 'J018', 0, 50, True, '30'), ('J063-4, J066-3', 'J018', 0, 57, True, '어린이\n보호구역'),
    ('J063-4, J066-3', 'J019', 180, 50, True, '30'), ('J063-4, J066-3', 'J019', 180, 57, True, '어린이\n보호구역'),
]
# crosswalk-ahead diamonds: (refs, card, arm bearing, distance)  on inbound lanes
DIAMOND = [
    ('J041-4', 'J041', 0, 30), ('J041-4', 'J041', 270, 30),
    ('J049-4', 'J049', 340.9, 30), ('J049-4', 'J049', 160.8, 30),
    ('J055-3', 'J055', 149.0, 28),
]
# J043-4 / J023-3: one diamond near a sheet Kakao point on the nearest lane (no junction arm)
DIAMOND_AT = [('J043-4', 'J043', (420, 450)), ('J023-3', 'J023', (560, 455)), ('J023-3', 'J023', (1080, 445))]
REMOVE = [('J067-1, J019-2', 'J7917_2565', 0, 1)]  # north crosswalk of J7917_2565 (not in the aerial)
DIAGONAL = ('J010-1', 'J5076_2580', 4.0, .8)


def centre(card):
    it = M.ITEMS[card]; return (it['x'], it['z'])


def plan(audit):
    out = []
    for card, (ref, arms_) in JUNCTION_CROSSWALKS.items():
        c = centre(card); found = M.arms(c)
        for bearing, colour, bike, *dist in arms_:
            g = M.arm_crossing(c, bearing, found, dist=dist[0] if dist else None)
            if bike:  # the bicycle strip sits on the outer side: move the stripes 2.3 m toward the junction
                b = math.radians(bearing); g['x'] -= math.sin(b) * 2.3; g['z'] -= math.cos(b) * 2.3
            # +z must face the junction for the bicycle side
            g['yaw'] = (math.degrees(math.atan2(-math.sin(math.radians(bearing)), -math.cos(math.radians(bearing))))) % 360
            out.append(dict(ref=ref, card=card, kind='crosswalk', junctionId='', type=colour, text=BIKE if bike else '게임 설정', **{k: g[k] for k in ('x', 'z', 'yaw', 'width', 'length')}))
    for ref, card, (u, v), colour, *bearing in MIDBLOCK:
        g = M.crossing(M.px(card, u, v), direction=(math.sin(math.radians(bearing[0])), math.cos(math.radians(bearing[0]))) if bearing else None)
        out.append(dict(ref=ref, card=card, kind='crosswalk', junctionId='', type=colour, text='게임 설정', **{k: g[k] for k in ('x', 'z', 'yaw', 'width', 'length')}))
    for ref, card, bearing, d0, d1, inbound in PAVING:
        for g in M.paving_arm(centre(card), bearing, d0, d1, inbound):
            out.append(dict(ref=ref, card=card, kind='line', junctionId='', type=M.PAVING, text='게임 설정', **{k: g[k] for k in ('x', 'z', 'yaw', 'width', 'length')}))
    for ref, card, bearing, dist, inbound, text in TEXT:
        for g in M.lane_points_on_arm(centre(card), bearing, dist, inbound):
            w = g['lanewidth'] * .8
            out.append(dict(ref=ref, card=card, kind='line', junctionId='', type=M.TEXT, text=text, x=g['x'], z=g['z'], yaw=g['yaw'], width=w, length=3.2 if text == '30' else 4.2))
    for ref, card, bearing, dist in DIAMOND:
        for g in M.lane_points_on_arm(centre(card), bearing, dist, True):
            out.append(dict(ref=ref, card=card, kind='line', junctionId='', type=M.AHEAD, text='게임 설정', x=g['x'], z=g['z'], yaw=g['yaw'], width=1.6, length=5.0))
    for ref, card, (u, v) in DIAMOND_AT:
        g = M.on_lane(M.px(card, u, v), width=1.6, length=5.0)
        out.append(dict(ref=ref, card=card, kind='line', junctionId='', type=M.AHEAD, text='게임 설정', **{k: g[k] for k in ('x', 'z', 'yaw', 'width', 'length')}))
    gen = approaches(audit)
    for ref, jid, axis, sign in REMOVE:
        g = gen.get((jid, axis, sign))
        if not g:
            raise SystemExit(f'no generated crosswalk {jid} a{axis} s{sign} to remove')
        out.append(dict(ref=ref, card='J019', kind='crosswalk', junctionId=jid, axis=axis, sign=sign, type=M.REMOVED, text='게임 설정', x=g['x'], z=g['z'], yaw=g['yaw'], width=min(g['width'], 40.0), length=2.4))
    ref, jid, depth, keep = DIAGONAL
    # J5076_2580 has crosswalks on its N (document), E and S arms only; the missing W edge mirrors the E one.
    cw = [v for k, v in gen.items() if k[0] == jid] + [f for f in M.DOC['facilities'] if f['kind'] == 'crosswalk' and f['junctionId'] == jid]
    jc = centre('J010')
    east = max(e['x'] for e in cw if e['x'] > jc[0] + 5) - 1.2
    west = min((e['x'] for e in cw if e['x'] < jc[0] - 5), default=2 * jc[0] - east - 2.4) + 1.2
    north = max(e['z'] for e in cw if e['z'] > jc[1] + 5) - 1.2
    south = min(e['z'] for e in cw if e['z'] < jc[1] - 5) + 1.2
    for k, (a, b) in enumerate([((west, north), (east, south)), ((east, north), (west, south))]):
        L = math.dist(a, b); along = math.degrees(math.atan2(b[0] - a[0], b[1] - a[1]))
        out.append(dict(ref=ref, card='J010', kind='crosswalk', junctionId='', id=f'review:r56:{jid}:diagonal:{k + 1}', type=Wh, text='대각선 횡단',
                        x=(a[0] + b[0]) / 2, z=(a[1] + b[1]) / 2, yaw=(along + 90) % 360, width=L * keep, length=depth))
    for n, e in enumerate(out):
        e.setdefault('id', f"review:r56:{e['card']}:{e['kind']}:{n + 1:03d}")
        e.setdefault('axis', 0); e.setdefault('sign', 1); e.setdefault('existing', '')
        for k in ('x', 'z', 'yaw', 'width', 'length'):
            e[k] = round(float(e[k]), 3)
        e['width'] = min(e['width'], 40.0)
    return out


def check(out, folder):
    from PIL import Image, ImageDraw, ImageFont
    from kakao_aerial_compare import aerial
    folder = Path(folder); folder.mkdir(parents=True, exist_ok=True)
    F = ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', 20)
    colours = {M.PAVING: (255, 40, 40), M.TEXT: (255, 255, 255), M.AHEAD: (255, 0, 255), M.REMOVED: (0, 0, 0)}
    for card in sorted({e['card'] for e in out}):
        it = M.ITEMS[card]; cx, cz, S, P = it['x'], it['z'], 120.0, 1000
        game = Image.open(next((M.ROOT / 'artifacts/map-review-20261124/game').glob(f"{card[1:]}-*-top.jpg"))).convert('RGB')
        k = 1600 / M.W_M; h = S / 2 * k
        game = game.crop((int(800 - h), int(450 - h), int(800 + h), int(450 + h))).resize((P, P))
        air, _ = aerial(cx, cz, S, S, (P, P))
        for im in (air, game):
            d = ImageDraw.Draw(im)
            def Q(x, z): return ((x - cx) / S * P + P / 2, P / 2 - (z - cz) / S * P)
            for e in (x for x in out if x['card'] == card):
                y = math.radians(e['yaw']); f = (math.sin(y), math.cos(y)); r = (f[1], -f[0])
                pts = [Q(e['x'] + r[0] * a * e['width'] / 2 + f[0] * b * e['length'] / 2, e['z'] + r[1] * a * e['width'] / 2 + f[1] * b * e['length'] / 2) for a, b in [(-1, -1), (1, -1), (1, 1), (-1, 1)]]
                col = colours.get(e['type'], (255, 230, 0) if e['type'] == Y else (0, 255, 255))
                d.polygon(pts, outline=col, width=3)
        sheet = Image.new('RGB', (P * 2, P + 30), 'white'); sheet.paste(air, (0, 30)); sheet.paste(game, (P, 30))
        ImageDraw.Draw(sheet).text((8, 4), f'{card} {it["junction"]} 120 m  왼쪽 항공 · 오른쪽 게임(r53)', font=F, fill='black')
        sheet.save(folder / f'{card}.jpg', quality=82)


if __name__ == '__main__':
    out = plan(sys.argv[1])
    OUT.write_text(json.dumps(dict(fromRevision=55, toRevision=56, edits=out), ensure_ascii=False, indent=1), encoding='utf-8')
    if '--check' in sys.argv:
        check(out, sys.argv[sys.argv.index('--check') + 1])
    from collections import Counter
    print(OUT, len(out), Counter((e['kind'], e['type']) for e in out))
