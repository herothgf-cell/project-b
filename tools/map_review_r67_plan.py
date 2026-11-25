"""Cross-section edits, second batch (map r66 -> r67, 2026-11-26). See tools/map_geometry_edit.py.

Curbs read off gridded Kakao aerials (2 m grid, world metres). J054-1 (J495 south arm, west sidewalk about
2 m further east in the aerial) was tried and dropped: its southbound lane runs past the map edge (x < 0), the
validator rejects any change to a lane with points outside the map, and the lane leaves no room to move the curb.
- J065-3: the Eunbit school road west of J8314 (x 803-826) continues the r66 J070 change: south curb from
  z 81.35 to 83.0 (wide sidewalk and bicycle path), lanes 1.6 m / 0.2 m in, the north curb stays.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r67_plan.py [--check <dir>]
Output: config/maps/review/r67-cross-sections.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r67-cross-sections.json'
EDITS = [
    ('J065-3', (815, 86), G.Edit([(801.1, 86.4), (826.0, 85.9)], s0=6, s1=21,
                                 controls=[(-4.85, -3.2), (-2.8, -1.2), (2.7, 2.5), (4.7, 4.7)], taper=4)),
]


def edits():
    return [e for _, _, e in EDITS]


if __name__ == '__main__':
    p = G.patch(edits(), 66, 67, OUT)
    print(OUT, {k: len(v) for k, v in p.items()})
    if '--check' in sys.argv:
        out = Path(sys.argv[sys.argv.index('--check') + 1]); out.mkdir(parents=True, exist_ok=True)
        for refs, c, e in EDITS:
            G.check(edits(), p, out / f"{refs.split(',')[0]}.jpg", c, 60, f'r67 {refs}')
