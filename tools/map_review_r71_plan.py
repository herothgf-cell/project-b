"""Daeeun-ro north kerb between J1927 and J5057 (map r70 -> r71, 2026-11-26). See tools/map_geometry_edit.py.

J075-1: the sheet read five to six westbound lanes and a north kerb 5-8 m too far north. The surveyed NGII
westbound lanes (four, 3.3 m) match the aerial; outside them the game has a provisional westbound lane
(local:4861:4677:2204:5298, 5.3 m north of the outer NGII lane) and the kerb 3.7 m further north, everywhere
between x 250 and x 470. The aerial kerb is about 2.3 m outside the outer NGII lane (x 350: z 496.5).
- north kerb: 6.7 m south (offset +3.7 -> -3.0 about the provisional lane; the sidewalks keep their outer
  edges and widen) between x 250 and x 380, back to the old line by x 405; the side street mouth keeps its
  corners around the surveyed right-turn lanes, and east of it an earlier review's planting strip (x 434-478)
  already marks the kerb; every lane is left out of this deformation;
- the provisional lane moves 5.35 m south onto the outer NGII lane. It stays an active lane without exits:
  it is the closed westbound approach that sets the J1927 signal head (ApproachSignalHeadTests), so its J1927
  end and heading are kept (the move tapers out over 25 m before x 250);
- its partner local:2123:4953:4780:4331 (eastbound, the other half of the synthetic yellow centre line between
  them) moves 5.35 m north by the same weight, so that centre line stays where it is.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r71_plan.py [--check <dir>]
Output: config/maps/review/r71-north-kerb.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r71-north-kerb.json'
WEST, EAST = 'local:4861:4677:2204:5298', 'local:2123:4953:4780:4331'
SHIFT = 5.35
REF = [(p['x'], p['z']) for p in E.LANES[WEST]['points']][::-1]  # runs east from J1927: + = north


def station_x(x):
    for i, (a, b) in enumerate(zip(REF, REF[1:])):
        if (a[0] - x) * (b[0] - x) <= 0:
            return E.length(REF[:i + 1]) + G.math.dist(a, b) * abs(x - a[0]) / max(1e-9, abs(b[0] - a[0]))


# full between x 250 and x 395: the side street mouth (x 405-427) keeps its corners around the surveyed right-turn
# lanes A2218G611367/370; east of it an earlier review's kerb-side planting strip (sidewalk:reviewed:A4218G610178,
# x 434-478) already marks the kerb and the provisional lane runs behind it
S0, S1 = 30.0, station_x(380.0)
SKIP = list(E.LANES) + [o['id'] for o in E.DOC['scene']['objects'] if o['id'].startswith('road:polish:')]
KERB = G.Edit(REF, s0=S0, s1=S1, controls=[(-3.5, -3.5), (3.7, -3.0)], taper=(25, 25), skip=SKIP)


def shifted(lane_id, d):
    """move a lane d metres north of the reference, with the kerb edit's station weight"""
    out = []; pts = E.resample([(q['x'], q['z']) for q in E.LANES[lane_id]['points']], 2.0); L = E.length(pts)
    for k, p in enumerate(pts):
        # weight by x (the station of the reference point at the same x), so both lanes of the pair move over the
        # same stretch and the synthetic centre line between them stays put; both lane ends stay on their connectors
        s = station_x(p[0]); _, t, n = KERB.project(p); at = L * k / (len(pts) - 1)
        w = (KERB.weight(s) if s is not None else 0.0) * G.smooth(min(at, L - at) / 12.0)
        out.append((p[0] + n[0] * d * w, p[1] + n[1] * d * w))
    return out


def build():
    P = E.LanePatch([KERB], 70, 71)
    P.set_points(WEST, shifted(WEST, -SHIFT))
    P.set_points(EAST, shifted(EAST, SHIFT))
    return P


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, {k: len(v) for k, v in r.items() if isinstance(v, list)}, 'ref length', round(E.length(REF), 1))
    if '--check' in sys.argv:
        out = Path(sys.argv[sys.argv.index('--check') + 1]); out.mkdir(parents=True, exist_ok=True)
        P.check(out / 'west.jpg', (330, 495), 70, 'r71 J075 west')
        P.check(out / 'sidestreet.jpg', (415, 485), 60, 'r71 J075 side street')
        P.check(out / 'j1927.jpg', (240, 515), 60, 'r71 J1927 end')
        P.check(out / 'wide.jpg', (350, 485), 200, 'r71 Daeeun-ro')
