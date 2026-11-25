"""First cross-section edits with the geometry tool (map r65 -> r66, 2026-11-26).

Curb positions are read off gridded Kakao aerials (2 m grid, world metres); the review sheet's metre
estimates set the direction and rough size. Each edit lists (old offset -> new offset) pairs about a
reference line (positive = left of its direction); see tools/map_geometry_edit.py.
- J033-2 / J037-3 (south): library road north of J1832, 13 m -> about 10.4 m, both curbs 1.3 m in; the
  lanes stay, the shoulders shrink;
- J037-3 (north): J1906 north arm, west curb 2.3 m east (the aerial shows the bollard sidewalk there) and both
  lanes 1.4-2.6 m east toward the aerial lanes (the crosswalk diamond marks the southbound lane about 3 m east
  of the game lane); the east curb stays because the arm bends there and the northbound lane needs the room;
- J067-3 / J071-1: the school back street east of J8385 (x 840-905), curb to curb 9.8-11.9 m -> about 8 m
  including the north parking row;
- J070-1: Eunbit school road (x 838-905), curb to curb 9.5 m -> 7.1 m, the south curb moving 2.4 m north
  (wide sidewalk) and the north curb staying.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r66_plan.py [--check <dir>]
Output: config/maps/review/r66-cross-sections.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r66-cross-sections.json'
EDITS = [
    # (refs, check centre, edit)
    ('J033-2, J037-3', (187, 214), G.Edit([(184.2, 199.7), (186.9, 214.0), (189.7, 228.4), (190.6, 233.1)], s0=8, s1=21,
                                          controls=[(-6.5, -5.2), (-4, -4), (4, 4), (6.5, 5.2)], taper=6)),
    ('J037-3', (200, 265), G.Edit([(196.8, 248.0), (204.4, 288.8)], s0=3, s1=30,
                                  controls=[(-5.4, -5.4), (-0.5, -1.9), (4.0, 1.4), (5.4, 3.1)], taper=(6, 16))),
    # controls are set on the sidewalk curbs (inside the road strip edges); the far sidewalk edge, more
    # than `fade` beyond the outermost control, stays put. x 845-870 and x 881-905 differ, so two edits.
    ('J067-3, J071-1', (868, 257), G.Edit([(830.0, 256.9), (910.0, 256.3)], s0=10, s1=33,
                                          controls=[(-5.0, -3.7), (-2.7, -1.75), (3.1, 2.25), (5.2, 4.3)], taper=4, fade=1.4)),
    ('J071-1', (890, 257), G.Edit([(830.0, 256.9), (910.0, 256.3)], s0=57, s1=73,
                                  controls=[(-6.3, -3.45), (-2.6, -1.5), (2.2, 2.5), (5.9, 4.55)], taper=4, fade=1.4)),
    ('J070-1', (870, 86), G.Edit([(830.0, 85.5), (912.0, 85.4)], s0=8, s1=75,
                                 controls=[(-5.0, -2.5), (-2.85, -0.6), (2.65, 3.05), (4.65, 4.65)], taper=6)),
]


def edits():
    return [e for _, _, e in EDITS]


if __name__ == '__main__':
    p = G.patch(edits(), 65, 66, OUT)
    print(OUT, {k: len(v) for k, v in p.items()})
    if '--check' in sys.argv:
        out = Path(sys.argv[sys.argv.index('--check') + 1]); out.mkdir(parents=True, exist_ok=True)
        for refs, c, e in EDITS:
            G.check(edits(), p, out / f"{refs.split(',')[0]}.jpg", c, 80, f'r66 {refs}')
