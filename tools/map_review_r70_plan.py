"""Suin-ro west of J866 re-laid (map r69 -> r70, 2026-11-26). See tools/map_geometry_edit.py.

J077-1 / J022-1 / J022-5 / J077-2: west of J866 (x 0-66) the game has three provisional lanes each way 5.8 m
apart and kerbs 35 m apart (z 513 / 548); the "five westbound lanes" on the sheet were the wide lanes and their
synthetic dividers. Near J866 (x 45-66) the surveyed NGII lanes (four eastbound, three westbound, 3.3-3.5 m)
and the r48 reviewed paint (edge lines z 518.4 / 542.0, double yellow centre) already match the aerial.
The aerial (gridded, offset by about 1 m against the r48 paint) shows three lanes each way of about 3.4 m west of
the junction, the north kerb about 6 m and the south kerb about 7-9 m inside the game kerbs.
Offsets are taken from the r48 centre line (heading 92.4 deg, z 531.93 at x 47.6), + = north:
- the six provisional lanes go to the NGII lane offsets (+1.55 / +5.05 / +8.55, -1.85 / -5.05 / -8.35), so they
  continue the surveyed lanes and the centre line runs straight (J077-2);
- north kerb +16.2 -> +10.5 (just outside the r48 edge line); south kerb -19.0 -> -10.2 west of x 30 and
  -13.75 from x 42 (outside the fourth, surveyed eastbound lane that starts at x 45);
- NGII lanes and the r48 / r57 marks stay; the sidewalks keep their outer edges and widen.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r70_plan.py [--check <dir>]
Output: config/maps/review/r70-cross-sections.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r70-cross-sections.json'
SLOPE = math.tan(math.radians(92.4 - 90))  # z drops eastward along the r48 heading


def centre(x):
    return 531.93 - SLOPE * (x - 47.6)


REF = [(-10.0, centre(-10.0)), (75.0, centre(75.0))]  # runs east: + = north
LANES = [(-15.9, -8.35), (-10.1, -5.05), (-4.25, -1.85), (1.55, 1.55), (7.45, 5.05), (13.3, 8.55), (16.2, 10.5)]
SECTIONS = [(0.0, [(-19.0, -10.2)] + LANES), (40.0, [(-19.0, -10.2)] + LANES), (52.0, [(-19.0, -13.75)] + LANES)]
SKIP = [l['id'] for l in E.DOC['lanes'] if l.get('status') == 'ngii'] + \
       [f['id'] for f in E.DOC['facilities'] if f['id'].startswith(('r48:suinro', 'review:r57:J866'))]
EDITS = [
    # station = x + 10; full from the map edge to x 66, back to the old line inside J866 over 6 m
    ('J077-1, J022-1', (35, 532), G.Edit(REF, s0=-5, s1=76, controls=SECTIONS, taper=(0, 6), skip=SKIP)),
]


def build():
    return E.LanePatch([e for _, _, e in EDITS], 69, 70)


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, {k: len(v) for k, v in r.items() if isinstance(v, list)})
    if '--check' in sys.argv:
        out = Path(sys.argv[sys.argv.index('--check') + 1]); out.mkdir(parents=True, exist_ok=True)
        P.check(out / 'west.jpg', (25, 532), 50, 'r70 J077 west')
        P.check(out / 'junction.jpg', (60, 532), 50, 'r70 J022 near J866')
        P.check(out / 'wide.jpg', (45, 532), 100, 'r70 Suin-ro')
