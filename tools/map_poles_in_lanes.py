"""List ground-standing poles that sit inside a driving lane.

Reads a player -mapAuditDump output (OBJECT lines with world centre and size) and the lanes in
config/maps/project/eungye.project.json. A pole is a renderer at least 1.2 m tall and at most 0.8 m
thick whose bottom is within 0.5 m of the road (overhead brackets above signal heads do not count:
the 2026-11-24 review sheet counted 15 of those by mistake). Margin < 0 means inside the lane.

Usage: python tools/map_poles_in_lanes.py <map-audit.txt> [out.json]
"""
import json
import math
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OBJ = re.compile(r'OBJECT (.+?) center=\(([-\d.]+), ([-\d.]+), ([-\d.]+)\) size=\(([-\d.]+), ([-\d.]+), ([-\d.]+)\)')


def seg(p, a, b):
    dx, dz = b[0] - a[0], b[1] - a[1]
    L = dx * dx + dz * dz
    t = 0 if L == 0 else max(0, min(1, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dz) / L))
    return math.dist(p, (a[0] + t * dx, a[1] + t * dz))


def main(audit, out=None):
    lanes = json.loads((ROOT / 'config/maps/project/eungye.project.json').read_text(encoding='utf-8-sig'))['document']['lanes']
    lanes = [(l['id'], l.get('width', 3.5), [(p['x'], p['z']) for p in l['points']]) for l in lanes if not l.get('disabled') and len(l['points']) > 1]
    hits, poles = [], 0
    for line in Path(audit).read_text(encoding='utf-8').splitlines():
        m = OBJ.match(line.strip())
        if not m:
            continue
        x, y, z, sx, sy, sz = map(float, m.groups()[1:])
        if sy < 1.2 or max(sx, sz) > 0.8 or y - sy / 2 > 0.5:
            continue
        poles += 1
        best = (99.0, '', 0.0)
        for lid, w, pts in lanes:
            if min(abs(x - q[0]) + abs(z - q[1]) for q in pts[::4] + [pts[-1]]) > 60:
                continue
            d = min(seg((x, z), a, b) for a, b in zip(pts, pts[1:])) - w / 2
            if d < best[0]:
                best = (d, lid, w)
        if best[0] < 0:
            hits.append(dict(margin=round(best[0], 2), x=round(x, 1), z=round(z, 1), bottom=round(y - sy / 2, 2), path=m.group(1), lane=best[1], width=best[2]))
    hits.sort(key=lambda h: h['margin'])
    print(f'ground poles {poles}, inside a lane {len(hits)}')
    for h in hits:
        print(f"  {h['margin']:6.2f} m  ({h['x']}, {h['z']})  {h['path']}  lane={h['lane']}")
    if out:
        Path(out).write_text(json.dumps(hits, ensure_ascii=False, indent=1), encoding='utf-8')
    return hits


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else None)
