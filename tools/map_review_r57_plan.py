"""Crosswalk moves with their stop lines (map r56 -> r57, 2026-11-25 map review).

Positions read off 100 m check images (artifacts/map-review-20261124/r57-check, Kakao aerial beside
the game view, 10 px = 1 m) made from the r56 dump. A moved generated crosswalk becomes a document
crosswalk with the same junction/axis/sign; its stop line moves by the same distance (plus the
bicycle strip) as a document stop line, so vehicles keep stopping before the stripes
(TrafficSystem reads document stop lines as the stop distance). A crosswalk the aerial does not show
becomes type "없음" (RoadSurfaceMarks).
Usage: python tools/map_review_r57_plan.py <r56 dump map-audit.txt>
Output: config/maps/review/r57-crosswalk-moves.json (MapReviewCrosswalkEdits.ApplyR57)
"""
import json
import math
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_review_marks as M  # noqa: E402
from map_crosswalk_approaches import approaches  # noqa: E402

OUT = M.ROOT / 'config/maps/review/r57-crosswalk-moves.json'
BIKE, BIKE_SHIFT = '자전거횡단도:바깥쪽', 2.3

# (refs, junction, axis, sign) generated crosswalks with no counterpart in the aerial
REMOVE = [('J001-1', 'J1882_11791', 0, 1), ('J001-2', 'J1882_11791', 0, -1),
          ('J005-1', 'J2564_12237', 0, 1), ('J005-1', 'J2564_12237', 0, -1),
          ('J006-1', 'J3516_12881', 1, -1)]
# (refs, junction, axis, sign, metres outward (negative = toward the junction), bicycle strip)
SHIFT_GENERATED = [('J003-1', 'J1927_5181', 1, -1, 13.0, False), ('J003-2', 'J1927_5181', 1, 1, 12.0, True),
                   ('J077-4', 'J866_5285', 0, -1, 8.0, False), ('J022-4', 'J866_5285', 0, 1, 4.0, False)]
# existing document crosswalk moved along its approach with its stop line:
# (refs, crosswalk id, metres outward (negative = toward the junction), new depth or None)
# A deeper (two-row) crossing grows toward the junction so the stop-line gap stays.
SHIFT_DOCUMENT = [('J011-1', 'reviewed:Y05:44:0:1:zebra', -8.0, None),
                  ('J024-1', 'reviewed:P22:56:south:crosswalk', 4.8, None),
                  ('J004-4', 'review:r55:J2145_15970:a0:s1:crosswalk', 10.0, None),
                  ('J008-1', 'reviewed:J5057-N:crosswalk', -12.0, None),
                  ('J008-2', 'review:r55:J5057_4449:a1:s1:crosswalk', -5.0, None), ('J008-2', 'review:r55:J5057_4449:a1:s-1:crosswalk', -5.0, None),
                  ('J020-1', 'review:r55:J7973_3744:a1:s-1:crosswalk', -6.0, None),
                  ('J020-2', 'review:r55:J7973_3744:a0:s1:crosswalk', -3.0, None), ('J020-2', 'review:r55:J7973_3744:a0:s-1:crosswalk', -3.5, None),
                  ('J018-2', 'review:r55:J7841_867:a1:s1:crosswalk', 0.0, 6.0), ('J018-2', 'review:r55:J7841_867:a1:s-1:crosswalk', 0.0, 6.0),
                  ('J019-1', 'review:r55:J7917_2565:a0:s-1:crosswalk', 0.0, 6.0),
                  ('J021-2', 'review:r55:J8066_6364:a1:s-1:crosswalk', 0.0, 6.0), ('J021-2', 'review:r55:J8066_6364:a1:s1:crosswalk', 0.0, 6.0),
                  ('J021-2', 'review:r55:J8066_6364:a0:s-1:crosswalk', 0.0, 5.0), ('J021-2', 'review:r55:J8066_6364:a0:s1:crosswalk', 0.0, 6.0)]
# generated crosswalk replaced by a straight one read off the check image: (refs, card, junction, axis, sign, (u, v) centre, yaw, width, depth)
REPLACE = [('J022-3, J077-4', 'J022', 'J866_5285', 1, 1, (580, 325), 0.0, 28.0, 6.0),
           ('J077-4', 'J022', 'J866_5285', 1, -1, (495, 685), 0.0, 13.0, 4.0)]
# new paint-only crossings at a check-image point, direction and span from the game lanes: (refs, card, (u, v), depth)
ADD = [('J005-1', 'J005', (870, 260), 6.0), ('J006-1', 'J006', (165, 770), 4.0)]


def img(card, u, v):
    it = M.ITEMS[card]
    return it['x'] + (u - 500) * .1, it['z'] - (v - 30 - 500) * .1


def stop_lines(audit):
    out = []
    for line in Path(audit).read_text(encoding='utf-8', errors='replace').splitlines():
        m = re.match(r'PAINT Stop line (.+?) at=\(([-\d.]+), [-\d.]+, ([-\d.]+)\) dir=\(([-\d.]+), [-\d.]+, ([-\d.]+)\) w=([-\d.]+)', line)
        if m:
            out.append(dict(junction=m[1], x=float(m[2]), z=float(m[3]), dx=float(m[4]), dz=float(m[5]), width=float(m[6])))
    return out


def stop_for(stops, jid, g):
    """The generated stop line of the approach whose crosswalk is g (same direction, nearest)."""
    y = math.radians(g['yaw']); f = (math.sin(y), math.cos(y))
    cands = [s for s in stops if s['junction'] == jid and s['dx'] * f[0] + s['dz'] * f[1] > .9]
    return min(cands, key=lambda s: math.dist((s['x'], s['z']), (g['x'], g['z'])), default=None)


def doc_stop(jid, axis, sign):
    return next((f for f in M.DOC['facilities'] if f['kind'] == 'stopLine' and f['junctionId'] == jid and f['axis'] == axis and f['sign'] == sign), None)


def plan(audit):
    gen = approaches(audit); stops = stop_lines(audit); out = []

    def add(**e):
        e.setdefault('existing', ''); e.setdefault('id', ''); e.setdefault('text', '게임 설정'); e.setdefault('type', M.WHITE)
        for k in ('x', 'z', 'yaw', 'width', 'length'):
            e[k] = round(float(e[k]), 3)
        e['width'] = min(e['width'], 40.0); out.append(e)

    for ref, jid, a, s in REMOVE:
        g = gen[(jid, a, s)]
        add(refs=ref, kind='crosswalk', junctionId=jid, axis=a, sign=s, type=M.REMOVED, x=g['x'], z=g['z'], yaw=g['yaw'], width=g['width'], length=2.4)
    for ref, jid, a, s, d, bike in SHIFT_GENERATED:
        g = gen[(jid, a, s)]; y = math.radians(g['yaw']); f = (math.sin(y), math.cos(y))
        add(refs=ref, kind='crosswalk', junctionId=jid, axis=a, sign=s, text=BIKE if bike else '게임 설정',
            x=g['x'] - f[0] * d, z=g['z'] - f[1] * d, yaw=g['yaw'], width=g['width'], length=2.4)
        st = doc_stop(jid, a, s); move = d + (BIKE_SHIFT if bike else 0)
        if st:
            add(refs=ref, kind='stopLine', junctionId=jid, axis=a, sign=s, existing=st['id'], x=st['x'] - f[0] * move, z=st['z'] - f[1] * move, yaw=st['yaw'], width=st['width'], length=st['length'])
        else:
            p = stop_for(stops, jid, g)
            if not p:
                raise SystemExit(f'no stop line for {jid} a{a} s{s}')
            add(refs=ref, kind='stopLine', junctionId=jid, axis=a, sign=s, x=p['x'] - f[0] * move, z=p['z'] - f[1] * move, yaw=g['yaw'], width=p['width'], length=.4)
    centres = {i['junction'].replace('_', ':') if i['junction'].startswith('reviewed_') else i['junction']: (i['x'], i['z']) for i in M.ITEMS.values() if i.get('kind') == 'junction'}  # post cards (P01..) share junction names
    for ref, cid, d, depth in SHIFT_DOCUMENT:
        c = next(f for f in M.DOC['facilities'] if f['id'] == cid); y = math.radians(c['yaw']); f = (math.sin(y), math.cos(y))
        jc = centres.get(c['junctionId']) or centres[c['junctionId'].replace(':', '_')]
        if (jc[0] - c['x']) * f[0] + (jc[1] - c['z']) * f[1] < 0:  # f points toward the junction
            f = (-f[0], -f[1])
        L = depth or c['length']; grow = (L - c['length']) / 2
        add(refs=ref, kind='crosswalk', junctionId=c['junctionId'], axis=c['axis'], sign=c['sign'], existing=cid, type=c['type'], text=c['text'],
            x=c['x'] - f[0] * (d - grow), z=c['z'] - f[1] * (d - grow), yaw=c['yaw'], width=c['width'], length=L)
        if d <= 0:  # depth-only changes and inward moves keep the stop line, which stays outside the stripes
            continue
        st = doc_stop(c['junctionId'], c['axis'], c['sign'])
        if st:
            add(refs=ref, kind='stopLine', junctionId=st['junctionId'], axis=st['axis'], sign=st['sign'], existing=st['id'],
                x=st['x'] - f[0] * d, z=st['z'] - f[1] * d, yaw=st['yaw'], width=st['width'], length=st['length'])
        else:
            p = stop_for(stops, c['junctionId'], dict(yaw=math.degrees(math.atan2(f[0], f[1])), x=c['x'], z=c['z']))
            if not p:
                raise SystemExit(f'no stop line for {cid}')
            add(refs=ref, kind='stopLine', junctionId=c['junctionId'], axis=c['axis'], sign=c['sign'], x=p['x'] - f[0] * d, z=p['z'] - f[1] * d,
                yaw=math.degrees(math.atan2(f[0], f[1])) % 360, width=p['width'], length=.4)
    for ref, card, jid, a, s, (u, v), yaw, width, depth in REPLACE:
        g = gen[(jid, a, s)]; x, z = img(card, u, v)
        gy = math.radians(g['yaw']); f = (math.sin(gy), math.cos(gy))
        y = yaw if (math.sin(math.radians(yaw)) * f[0] + math.cos(math.radians(yaw)) * f[1]) > 0 else (yaw + 180) % 360
        add(refs=ref, kind='crosswalk', junctionId=jid, axis=a, sign=s, x=x, z=z, yaw=y, width=width, length=depth)
        p = stop_for(stops, jid, g); fy = (math.sin(math.radians(y)), math.cos(math.radians(y)))
        if p:  # keep the stop line 1 m outside the new stripes, on the same side of the road as before
            along = (p['x'] - x) * fy[0] + (p['z'] - z) * fy[1]; want = -(depth / 2 + 1.0)
            if along > want:
                add(refs=ref, kind='stopLine', junctionId=jid, axis=a, sign=s, x=p['x'] + fy[0] * (want - along), z=p['z'] + fy[1] * (want - along), yaw=y, width=p['width'], length=.4)
    for ref, card, (u, v), depth in ADD:
        g = M.crossing(img(card, u, v), depth=depth)
        add(refs=ref, kind='crosswalk', junctionId='', axis=0, sign=1, id=f'review:r57:{card}:crosswalk:{u}-{v}', **{k: g[k] for k in ('x', 'z', 'yaw', 'width', 'length')})
    for n, e in enumerate(out):
        if not e['id'] and not e['existing']:
            e['id'] = f"review:r57:{e['junctionId']}:a{e['axis']}:s{e['sign']}:{e['kind']}"
    return out


if __name__ == '__main__':
    out = plan(sys.argv[1])
    OUT.write_text(json.dumps(dict(fromRevision=56, toRevision=57, edits=out), ensure_ascii=False, indent=1), encoding='utf-8')
    for e in out:
        print(e['refs'], e['kind'], e['type'], e['junctionId'], e['axis'], e['sign'], e['existing'] or e['id'], round(e['x'], 1), round(e['z'], 1), round(e['width'], 1))
