"""Median hatch zones read off the aerial (map r76 -> r77, 2026-11-26). See tools/map_lane_edit.py.

J009-1 (J5074 roundabout): the east-west road has one lane each way 10 m apart (6.6 m between the lanes) and
one yellow centre line; the aerial shows a 3 m yellow-hatched median on both sides of the roundabout (west arm
x 452-480, east arm x 533-555, between the crosswalks). Each zone gets two yellow outline lines 1.5 m either
side of the game centre (midway between the lanes), diagonal strokes every 2 m (the r49 Suin-ro hatch form) and
a paint-replacement mask over its band so the single synthetic centre line inside it is not drawn.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r77_plan.py [--check <dir>]
Output: config/maps/review/r77-marks.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r77-marks.json'
HATCH, MEDIAN, MASK = 'r49:suinro:J6478:median:hatch:0', 'reviewed:r47:A:median:north', 'reviewed:r47:A:mask:1'


def centre_z(x, z0, z1):
    """midway between the eastbound and westbound lanes at x"""
    east, west = [], []
    for l in E.DOC['lanes']:
        if l.get('disabled') or l['change'] or l['turn'] or l['id'].startswith('join'):
            continue
        P = l['points']
        for a, b in zip(P, P[1:]):
            if (a['x'] - x) * (b['x'] - x) <= 0 and a['x'] != b['x']:
                z = a['z'] + (x - a['x']) / (b['x'] - a['x']) * (b['z'] - a['z'])
                if z0 < z < z1:
                    (east if b['x'] > a['x'] else west).append(z)
    return (max(east) + min(west)) / 2


def stroke(P, src, new_id, a, b, width):
    item = P.copy_mark(src, new_id)
    item.update(x=round((a[0] + b[0]) / 2, 4), z=round((a[1] + b[1]) / 2, 4),
                yaw=round(math.degrees(math.atan2(b[0] - a[0], b[1] - a[1])) % 360, 3), width=width, length=round(math.dist(a, b), 4))
    return item


def zone(P, tag, x0, x1, z0, z1, half=1.5):
    c0, c1 = centre_z(x0, z0, z1), centre_z(x1, z0, z1)
    for side, s in (('south', -1), ('north', 1)):
        stroke(P, MEDIAN, f'review:r77:{tag}:edge:{side}', (x0, c0 + s * half), (x1, c1 + s * half), .15)
    k = 0; x = x0 + .5
    while x + 2 * half <= x1:
        c = centre_z(x, z0, z1); c2 = centre_z(x + 2 * half, z0, z1)
        stroke(P, HATCH, f'review:r77:{tag}:hatch:{k:03d}', (x, c - half), (x + 2 * half, c2 + half), .22)
        k += 1; x += 2.0
    m = P.copy_mark(MASK, f'review:r77:{tag}:mask')
    m.update(x=round((x0 + x1) / 2, 4), z=round((c0 + c1) / 2, 4), yaw=round(math.degrees(math.atan2(x1 - x0, c1 - c0)) % 360, 3),
             width=2 * half + .4, length=round(math.dist((x0, c0), (x1, c1)), 4))


def build():
    P = E.LanePatch([], 76, 77)
    zone(P, 'J009:west', 452.0, 480.0, 75, 100)
    zone(P, 'J009:east', 533.0, 555.0, 75, 100)
    return P


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, {k: len(v) for k, v in r.items() if isinstance(v, list)})
    if '--check' in sys.argv:
        out = Path(sys.argv[sys.argv.index('--check') + 1]); out.mkdir(parents=True, exist_ok=True)
        P.check(out / 'j009.jpg', (505, 88), 120, 'r77 J009-1 hatch')
