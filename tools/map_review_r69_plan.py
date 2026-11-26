"""J8066-J7801 arterial and J8066 west arm re-laid (map r68 -> r69, 2026-11-26). See tools/map_geometry_edit.py.

J021-3 and J017-1, read off gridded Kakao aerials:
- the arterial between J8066 (north mouth z 656) and J7801 (south mouth z 940) has three lanes each way at
  5.7-5.9 m spacing in the game (33-34 m between kerbs). The aerial (z 820) has three southbound lanes of about
  3.6 m, a 3.8 m yellow-hatched median and three northbound lanes of about 3.4 m, east kerb near x 819, the west
  kerb where the game has it (a bicycle strip runs along it): about 29 m, as J017-1 reads for the J7801 south arm,
  and the J8066 north arm kerb about 4 m in, as J021-3 reads. Offsets are taken from the median (midway between
  the inner lanes); the northbound side and the east kerb move along the whole stretch, the southbound lanes
  only north of z 715 (south of it the five survey54 southbound lanes stay).
- west arm (x 745-784, tapering back over 20 m toward the scramble crossing at x 728): carriageway z 630-647 in the game, z 634.3-644.2 in the aerial
  (one westbound, two eastbound lanes): the south kerb comes 4.4 m north (the eastbound lane hugs it), the north kerb 2.2 m south, the lanes
  follow (eastbound 635.8 / 639.0, westbound 642.4 at x 770).
The sidewalks keep their outer edges and widen into the freed strip.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r69_plan.py [--check <dir>]
Output: config/maps/review/r69-cross-sections.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r69-cross-sections.json'


def median():
    """midpoints between the inner southbound and inner northbound lanes, north (J7801) to south (J8066)"""
    out = []
    for z in [935 - 10 * k for k in range(28)] + [662]:
        s, n = [], []
        for l in E.DOC['lanes']:
            if l.get('disabled') or l['id'].startswith(('join', 'change')):
                continue
            P = l['points']
            for a, b in zip(P, P[1:]):
                if (a['z'] - z) * (b['z'] - z) <= 0 and a['z'] != b['z']:
                    x = a['x'] + (z - a['z']) / (b['z'] - a['z']) * (b['x'] - a['x'])
                    if 765 < x < 830:
                        (n if b['z'] > a['z'] else s).append(x)
        out.append(((max(s) + min(n)) / 2, z))
    return out


REF = median()  # runs south (z 935 -> 662; beyond the ends the line extends straight): + = east


def station(z):
    """distance along REF to the point at z"""
    for i, (a, b) in enumerate(zip(REF, REF[1:])):
        if (a[1] - z) * (b[1] - z) <= 0:
            return E.length(REF[:i + 1]) + G.math.dist(a, b) * (a[1] - z) / (a[1] - b[1])
EDITS = [
    ('J021-3 north', (812, 685), G.Edit(REF, s0=-5, s1=station(662) + 6, name='NB',
                                        controls=[(-1.5, -1.5), (2.85, 3.15), (8.65, 6.55), (14.35, 9.95), (16.3, 12.0)], taper=(8, 6))),
    ('J017-1', (800, 820), G.Edit(REF, s0=-5, s1=station(715), name='SB',
                                  controls=[(-16.25, -16.25), (-14.25, -11.45), (-8.55, -7.85), (-2.85, -4.25), (-1.5, -1.5)], taper=(8, 15))),
    # west arm, reference runs east along the westbound lane: + = north
    ('J021-3 west', (760, 640), G.Edit([(725.0, 646.4), (786.0, 643.9)], s0=20, s1=59,
                                       controls=[(-15.16, -10.76), (-13.26, -8.76), (-9.96, -5.56), (-0.06, -2.16), (1.94, -0.26)], taper=(20, 6))),
]


def build():
    return E.LanePatch([e for _, _, e in EDITS], 68, 69)


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, {k: len(v) for k, v in r.items() if isinstance(v, list)}, 'ref', [(round(x, 1), z) for x, z in REF[::6]])
    if '--check' in sys.argv:
        out = Path(sys.argv[sys.argv.index('--check') + 1]); out.mkdir(parents=True, exist_ok=True)
        for name, c, _ in EDITS:
            P.check(out / (name.replace(' ', '-') + '.jpg'), c, 60, 'r69 ' + name)
        P.check(out / 'j7801-south.jpg', (785, 925), 60, 'r69 J7801 south mouth')
        P.check(out / 'arterial.jpg', (800, 800), 200, 'r69 arterial')
