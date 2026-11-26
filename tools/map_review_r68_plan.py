"""Lane-count edit, first batch (map r67 -> r68, 2026-11-26). See tools/map_lane_edit.py.

J063-1 / J066-1: the north-south arterial between J7917 (z 238.5) and the G04 section (z 184) has three
southbound lanes at 3.8 m spacing in the game; the aerial shows four southbound and four northbound lanes of
about 3.25 m between the same curbs (x 776 / 803). The whole cross-section is re-laid at 3.25 m: the three
southbound lanes and the median move east, the four northbound lanes close up (the outer one stays), and a
fourth southbound lane is added on the kerb side. The curbs stay (the aerial kerbs match the game).
At z 184 the four lanes pair with the four G04 lanes (left, straight-1, straight-2, right) one to one -- the
G04 lane starts are moved to the new lane ends and blend back over 15 m; before, the inner lane fed both the
left and straight-1 lanes. The new lane takes the west-arm right turn at J7917 (a copy of the connector into
the old kerb lane) and copies of that lane's school-zone paving; the reviewed r47 dashes between z 169 and 184
are redrawn between the blended lanes.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r68_plan.py [--check <dir>]
Output: config/maps/review/r68-lanes.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r68-lanes.json'
N1, N2, N3 = 'local:7871:2385:7846:1840', 'reviewed:J7917:middle:local:7795:2388:7771:1843', 'local:7795:2388:7771:1843'
LEFT, S1, S2, RIGHT = 'editor:segment:bG9jYWw6NzgzMDoxNDc2Ojc4MTE6MTA1MQ:g04-sb-left', 'reviewed:G04:SB:straight-1', 'reviewed:G04:SB:straight-2', 'reviewed:G04:SB:right'
N4 = 'review:r68:J7917:SB:kerb'
# r47 dashes south of z 184 (between the G04 lanes): redrawn after the lane starts move
OLD_DASHES = ['reviewed:r47:A:1:66', 'reviewed:r47:A:1:72', 'reviewed:r47:A:2:66', 'reviewed:r47:A:2:72', 'reviewed:r47:A:west-inner:66', 'reviewed:r47:A:west-inner:72']
SPACING = 3.25
# old lateral offset -> new, metres left (east) of the old inner southbound lane: west kerb (parallel, -9.52),
# SB 3/2/1, median, NB 1-4; the new kerb lane is at -7.82 (0.2 m off the kerb)
CONTROLS = [(-9.52, -9.52), (-7.61, -4.57), (-3.8, -1.32), (0.0, 1.93), (1.71, 3.6), (4.31, 5.25), (7.61, 8.5), (11.41, 11.75), (15.22, 15.0)]
REF = [(787.07, 238.5), (784.65, 184.0)]
EDIT = G.Edit(REF, s0=0.0, s1=G.math.dist(*REF), controls=CONTROLS, taper=(10, 15), skip=[LEFT, S1, S2, RIGHT] + OLD_DASHES)


def build():
    P = E.LanePatch([EDIT], 67, 68)
    # the new kerb lane: the moved kerb-side lane, 3.25 m further west
    kerb = P.offset(N3, SPACING)
    P.add(N4, N3, kerb, 3.0, 4, next=[RIGHT])
    for lane in (N1, N2, N3):
        P.set_points(lane, P.pts(lane), 3.0)
    # one-to-one pairing with the G04 lanes, whose starts move to the new lane ends
    for up, down in ((N1, LEFT), (N2, S1), (N3, S2), (N4, RIGHT)):
        P.blend(down, P.pts(up)[-1], 15.0)
        P.exits(up, [down])
    # J7917 west-arm right turn also into the new lane
    P.connector('review:r68:J7917:right-into-kerb', 'editor:segment:bG9jYWw6NzMyMjoyNTAyOjc3MzQ6MjUwMA:P29-approach', N4,
                'join:local:7322:2502:7734:2500:1', turn=1)
    # school-zone paving of the old kerb lane (z 216-236) copied onto the new lane
    for k in (63, 64, 65, 66):
        P.copy_mark(f'review:r56:J019:line:{k:03d}', f'review:r68:J019:paving:{k:03d}', d=SPACING)
    # r47 dashes, z 184-207: the deformation moves the two old dividers between the moved lanes; add the third
    for k in (78, 84, 90, 96):
        P.copy_mark(f'reviewed:r47:A:1:{k}', f'review:r68:r47:A:kerb:{k}', d=-SPACING)
    # z 169-184: dashes between the blended G04 lanes at the old stations
    P.remove_mark(OLD_DASHES)
    pairs = ((RIGHT, S2, 'a'), (S2, S1, 'b'), (S1, LEFT, 'c'))
    for z in (173.4, 179.4):
        for a, b, tag in pairs:
            pa, pb = P.pts(a), P.pts(b)
            ca = mid_at(pa, z); cb = mid_at(pb, z)
            c = ((ca[0] + cb[0]) / 2, z)
            heading = (mid_at(pa, z - 1.5)[0] + mid_at(pb, z - 1.5)[0]) / 2, (mid_at(pa, z + 1.5)[0] + mid_at(pb, z + 1.5)[0]) / 2
            yaw = math.degrees(math.atan2(heading[1] - heading[0], 3.0)) % 360
            item = P.copy_mark('reviewed:r47:A:1:66', f'review:r68:r47:A:{tag}:{z:.0f}')
            item.update(x=round(c[0], 4), z=round(c[1], 4), yaw=round(yaw, 3))
    return P


def mid_at(pts, z):
    """x of a roughly north-south polyline at z"""
    for a, b in zip(pts, pts[1:]):
        if (a[1] - z) * (b[1] - z) <= 0 and a[1] != b[1]:
            return (a[0] + (z - a[1]) / (b[1] - a[1]) * (b[0] - a[0]), z)
    raise ValueError(z)


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, {k: len(v) for k, v in r.items() if isinstance(v, list)})
    if '--check' in sys.argv:
        out = Path(sys.argv[sys.argv.index('--check') + 1]); out.mkdir(parents=True, exist_ok=True)
        P.check(out / 'north.jpg', (789, 228), 40, 'r68 J063-1/J066-1 north')
        P.check(out / 'south.jpg', (787, 188), 40, 'r68 J063-1/J066-1 south')
        P.check(out / 'wide.jpg', (789, 205), 90, 'r68 J063-1/J066-1')
