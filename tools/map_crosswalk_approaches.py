"""List the generated crosswalk of every junction approach from a player -mapAuditDump.

Each PAINT line is one stripe named "Crosswalk <junction> a<axis> s<sign>" (TrafficSystem.AddApproach).
Stripes of one approach are grouped into centre, width and yaw so a map edit can replace the generated
crosswalk with a document facility of the same junction/axis/sign (that facility then wins).
Usage: python tools/map_crosswalk_approaches.py <map-audit.txt> [junction ...]
"""
import json
import math
import re
import sys
from collections import defaultdict
from pathlib import Path

PAINT = re.compile(r'PAINT Crosswalk (.+?) a(\d) s(-?\d) at=\(([-\d.]+), [-\d.]+, ([-\d.]+)\) dir=\(([-\d.]+), [-\d.]+, ([-\d.]+)\)')
STEP, FIRST, LAST = .95, .65, .3  # TrafficSystem: x from -hw+.65 while x < hw-.3, step .95


def approaches(audit):
    groups = defaultdict(list)
    for line in Path(audit).read_text(encoding='utf-8').splitlines():
        m = PAINT.match(line)
        if m:
            j, a, s, x, z, dx, dz = m.groups()
            groups[(j, int(a), int(s))].append((float(x), float(z), float(dx), float(dz)))
    out = {}
    for key, pts in groups.items():
        dx, dz = pts[0][2], pts[0][3]
        rx, rz = dz, -dx  # TrafficSystem.Right(direction) = (dz, 0, -dx)
        t = sorted((p[0] * rx + p[1] * rz, p) for p in pts)
        lo, hi = t[0][0], t[-1][0]
        hw = (hi - lo + FIRST + LAST + STEP / 2) / 2
        mid = lo - FIRST + hw  # centre along "right"
        base = t[0][1]
        cx = base[0] + (mid - lo) * rx
        cz = base[1] + (mid - lo) * rz
        yaw = math.degrees(math.atan2(dx, dz)) % 360
        out[key] = dict(junction=key[0], axis=key[1], sign=key[2], x=round(cx, 2), z=round(cz, 2), yaw=round(yaw, 2), width=round(hw * 2, 2), stripes=len(pts))
    return out


def compass(dx, dz):
    a = math.degrees(math.atan2(dx, dz)) % 360
    return ['N', 'NE', 'E', 'SE', 'S', 'SW', 'W', 'NW'][int((a + 22.5) // 45) % 8]


if __name__ == '__main__':
    res = approaches(sys.argv[1])
    want = set(sys.argv[2:])
    for k, v in sorted(res.items()):
        if not want or k[0] in want:
            print(json.dumps(v))
