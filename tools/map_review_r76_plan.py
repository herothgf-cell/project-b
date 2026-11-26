"""Daeeun-ro median hatch and closed mark items (map r75 -> r76, 2026-11-26). See tools/map_lane_edit.py.

J075-4: from the footbridge (x 280) eastward the aerial shows a yellow-hatched median between two yellow lines,
about 4 m wide to x 335 and narrowing after it; the game leaves the same gap between the surveyed inner lanes
(x 300: z 490.7-494.5) but paints it empty. Diagonal yellow strokes (as the r49 Suin-ro hatch) fill the band
between the aerial edges, kept 0.15 m inside the inner lane edges, every 2 m from x 284 to x 360 (east of it
the game lanes close in faster than the aerial median, so the hatch stops there).
J060-1 / J060-2 (J6481): the r56 mid-block crossing moves about 7 m east onto the aerial stripes, and the
yellow-hatched median east of it is filled between the game lanes with two outline lines (west of the crossing
a reviewed left-turn lane into the P20 car park leaves no median, so no hatch there).
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r76_plan.py [--check <dir>]
Output: config/maps/review/r76-marks.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r76-marks.json'
HATCH = 'r49:suinro:J6478:median:hatch:0'
MEDIAN = 'reviewed:r47:A:median:north'


def aerial_upper(x):
    return 499.0 + (487.3 - 499.0) * (x - 280.0) / 52.0


def aerial_lower(x):
    return 495.1 + (483.7 - 495.1) * (x - 280.0) / 52.0


def lane_edges(x, z0=465, z1=505, status='ngii'):
    """north edge of the inner eastbound lane and south edge of the inner westbound one at x"""
    east, west = [], []
    for l in E.DOC['lanes']:
        if l.get('disabled') or (status and l.get('status') != status) or l['change'] or l['turn']:
            continue
        P = l['points']
        for a, b in zip(P, P[1:]):
            if (a['x'] - x) * (b['x'] - x) <= 0 and a['x'] != b['x']:
                z = a['z'] + (x - a['x']) / (b['x'] - a['x']) * (b['z'] - a['z'])
                if z0 < z < z1:
                    (east if b['x'] > a['x'] else west).append((z, l['width']))
    e = max(east); w = min(west)
    return e[0] + e[1] / 2, w[0] - w[1] / 2


def build():
    P = E.LanePatch([], 75, 76)
    n = 0
    for k in range(39):
        x = 284.0 + 2.0 * k
        lo_lane, hi_lane = lane_edges(x)
        lo = max(aerial_lower(x), lo_lane + .15); hi = min(aerial_upper(x), hi_lane - .15)
        if hi - lo < .6:
            continue
        slope = (aerial_upper(x + 1) - aerial_upper(x - 1)) / 2
        d = (1 / math.hypot(1, slope), slope / math.hypot(1, slope)); nrm = (-d[1], d[0])
        w = hi - lo
        a = (x, lo); b = (x + d[0] * w, hi + d[1] * w)  # 45 deg: as far along the road as across it
        c = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
        yaw = math.degrees(math.atan2(b[0] - a[0], b[1] - a[1])) % 360
        item = P.copy_mark(HATCH, f'review:r76:J075:hatch:{n:03d}')
        item.update(x=round(c[0], 4), z=round(c[1], 4), yaw=round(yaw, 3), width=0.22, length=round(math.dist(a, b), 4))
        n += 1
    # J060-1: the r56 mid-block crossing at J6481 lies about 7 m west of the aerial stripes; it moves onto them
    cw = 'review:r56:J060:crosswalk:038'; f = E.FACILITIES[cw]
    P.facilities[cw] = dict(id=cw, x=647.8, z=334.6, yaw=112.0, width=f['width'], length=f['length'])
    # J060-2: east of that crossing (x 651-663) the aerial has a yellow-hatched median; it fills the gap between
    # the game lanes (they run about 3 m south of the aerial ones here), outlined by two yellow lines that the
    # r47 centre line runs into. West of the crossing a reviewed left-turn lane into the P20 car park fills the
    # middle of the road, so no hatch there.
    edges = []
    for k in range(13):
        x = 651.0 + k
        lo, hi = lane_edges(x, 320, 345, None)
        edges.append((x, lo + .15, hi - .15))
    for j, (x, lo, hi) in enumerate(edges[::2]):
        w = hi - lo; a = (x, lo); b = (x + w, hi)
        c = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
        item = P.copy_mark(HATCH, f'review:r76:J060:hatch:{j:03d}')
        item.update(x=round(c[0], 4), z=round(c[1], 4), yaw=round(math.degrees(math.atan2(b[0] - a[0], b[1] - a[1])) % 360, 3), width=0.22, length=round(math.dist(a, b), 4))
    for side, idx in (('south', 1), ('north', 2)):
        a = (edges[0][0], edges[0][idx]); b = (edges[-1][0], edges[-1][idx])
        item = P.copy_mark(MEDIAN, f'review:r76:J060:hatch-edge:{side}')
        item.update(x=round((a[0] + b[0]) / 2, 4), z=round((a[1] + b[1]) / 2, 4), yaw=round(math.degrees(math.atan2(b[0] - a[0], b[1] - a[1])) % 360, 3),
                    width=0.15, length=round(math.dist(a, b), 4))
    return P


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, {k: len(v) for k, v in r.items() if isinstance(v, list)})
    if '--check' in sys.argv:
        out = Path(sys.argv[sys.argv.index('--check') + 1]); out.mkdir(parents=True, exist_ok=True)
        P.check(out / 'hatch.jpg', (320, 489), 60, 'r76 J075-4 hatch')
        P.check(out / 'j060.jpg', (652, 334), 40, 'r76 J060')
