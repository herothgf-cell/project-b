"""Line-type batch (map r57 -> r58, 2026-11-25 map review): left-turn guide lines and a zigzag approach.

- Left-turn guide lines (J004-2, J007-1, J011-3, J013-2, J016-2, J017-3, J021-4): white dashes (1 m on,
  1 m off, 0.15 m wide) along both edges of every left-turn connector lane (turn -1) of the junction,
  so the dotted curves follow the paths cars actually drive. Edges shared by two adjacent left lanes
  are drawn once.
- J007-3 (J4396_13458 north-west approach): white zigzag lines on the edges of the inbound lanes for
  30 m before the junction and red anti-skid paving on those lanes (aerial shows both).
Paint only ("line" facilities); lanes, stop lines and signals are untouched.
Usage: python tools/map_review_r58_plan.py <deploy r57 dump map-audit.txt>   -> config/maps/review/r58-line-marks.json (MapReviewCrosswalkEdits.ApplyR58)
"""
import json
import math
import sys
from pathlib import Path
import re

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_review_marks as M  # noqa: E402

OUT = M.ROOT / 'config/maps/review/r58-line-marks.json'
GUIDE = '좌회전 유도선'
GUIDES = [('J004-2', 'J004'), ('J007-1', 'J007'), ('J011-3', 'J011'), ('J013-2', 'J013'), ('J016-2', 'J016'), ('J017-3', 'J017'), ('J021-4', 'J021')]
DASH, GAP, STROKE = 1.0, 1.0, .15


def walk(points, offset):
    """Points of a polyline shifted sideways by offset (right positive), with running distance."""
    out = []
    for i, p in enumerate(points):
        a = points[max(0, i - 1)]; b = points[min(len(points) - 1, i + 1)]
        d = M.unit(a, b); r = (d[1], -d[0])
        out.append((p[0] + r[0] * offset, p[1] + r[1] * offset))
    return out


def dashes(line, start=1.0, end_trim=1.0):
    total = sum(math.dist(a, b) for a, b in zip(line, line[1:]))
    out = []; at = start
    while at + DASH <= total - end_trim:
        p0 = point_at(line, at); p1 = point_at(line, at + DASH)
        out.append(((p0[0] + p1[0]) / 2, (p0[1] + p1[1]) / 2, M.yaw_of(M.unit(p0, p1)), math.dist(p0, p1)))
        at += DASH + GAP
    return out


def point_at(line, s):
    for a, b in zip(line, line[1:]):
        L = math.dist(a, b)
        if s <= L:
            t = s / L if L else 0
            return (a[0] + t * (b[0] - a[0]), a[1] + t * (b[1] - a[1]))
        s -= L
    return line[-1]


def blocked_areas(audit):
    """Crosswalk (with a 3.5 m margin for bicycle strips and stop lines) footprints a guide dash must avoid."""
    from map_crosswalk_approaches import approaches
    areas = [dict(x=g['x'], z=g['z'], yaw=g['yaw'], width=g['width'], length=2.4) for g in approaches(audit).values()]
    areas += [dict(x=f['x'], z=f['z'], yaw=f['yaw'], width=f['width'], length=f['length']) for f in M.DOC['facilities'] if f['kind'] in ('crosswalk', 'stopLine') and f['type'] != M.REMOVED]
    return areas


def inside_area(p, areas, margin=3.5):
    for a in areas:
        y = math.radians(a['yaw']); f = (math.sin(y), math.cos(y)); r = (f[1], -f[0])
        dx, dz = p[0] - a['x'], p[1] - a['z']
        if abs(dx * r[0] + dz * r[1]) <= a['width'] / 2 + .5 and abs(dx * f[0] + dz * f[1]) <= a['length'] / 2 + margin:
            return True
    return False


def plan():
    areas = blocked_areas(sys.argv[1])
    out = []
    for ref, card in GUIDES:
        jid = M.ITEMS[card]['junction']
        lefts = [l for l in M.DOC['lanes'] if not l.get('disabled') and l.get('turn') == -1 and l.get('ruleId') == jid and len(l['points']) > 1]
        drawn = []
        for l in lefts:
            pts = [(p['x'], p['z']) for p in l['points']]; w = l.get('width', 3.5) / 2
            for side in (-1, 1):
                edge = walk(pts, side * w)
                mid = edge[len(edge) // 2]
                if any(math.dist(mid, m) < .8 for m in drawn):  # shared edge of two adjacent left lanes
                    continue
                drawn.append(mid)
                for x, z, yaw, length in dashes(edge):
                    if inside_area((x, z), areas):
                        continue
                    out.append(dict(refs=ref, card=card, kind='line', type=GUIDE, text='게임 설정', x=x, z=z, yaw=yaw, width=STROKE, length=length))
        print(card, jid, 'left connectors', len(lefts), 'edges', len(drawn))
    # J007-3: zigzag + red paving on the north-west approach (bearing about 315 deg from the junction)
    c = (M.ITEMS['J007']['x'], M.ITEMS['J007']['z'])
    for g in M.paving_arm(c, 316, 20, 50, inbound=True):
        out.append(dict(refs='J007-3', card='J007', kind='line', type=M.PAVING, text='게임 설정', **{k: g[k] for k in ('x', 'z', 'yaw', 'width', 'length')}))
    inside, keep = M.arm_lanes(c, 316, 20, 50, 16.0, True)
    for l in M.LANES:
        pts = [(p['x'], p['z']) for p in l['points']]
        run = [p for a, p in zip(pts, pts[1:]) if keep(M.unit(a, p)) and inside(p)]
        if len(run) < 4:
            continue
        w = l.get('width', 3.5) / 2
        for side in (-1, 1):
            edge = walk(run, side * (w - .25))
            total = sum(math.dist(a, b) for a, b in zip(edge, edge[1:])); s = 0.0; k = 0
            while s + 2.5 <= total:
                a = point_at(edge, s); b = point_at(edge, s + 2.5); d = M.unit(a, b); r = (d[1], -d[0])
                o0, o1 = (.3, -.3) if k % 2 == 0 else (-.3, .3)
                p0 = (a[0] + r[0] * o0, a[1] + r[1] * o0); p1 = (b[0] + r[0] * o1, b[1] + r[1] * o1)
                out.append(dict(refs='J007-3', card='J007', kind='line', type='지그재그 서행선', text='게임 설정',
                                x=(p0[0] + p1[0]) / 2, z=(p0[1] + p1[1]) / 2, yaw=M.yaw_of(M.unit(p0, p1)), width=STROKE, length=math.dist(p0, p1)))
                s += 2.5; k += 1
    for n, e in enumerate(out):
        e.update(id=f"review:r58:{e['card']}:{n + 1:04d}", junctionId='', axis=0, sign=1, existing='')
        for k in ('x', 'z', 'yaw', 'width', 'length'):
            e[k] = round(float(e[k]), 3)
    return out


if __name__ == '__main__':
    out = plan()
    OUT.write_text(json.dumps(dict(fromRevision=57, toRevision=58, edits=out), ensure_ascii=False, indent=1), encoding='utf-8')
    from collections import Counter
    print(OUT, len(out), Counter((e['card'], e['type']) for e in out))
