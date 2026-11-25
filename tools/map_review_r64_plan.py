"""Mid-block crossings clear in the aerial (map r63 -> r64, 2026-11-26).

Positions are read off a gridded Kakao aerial in world metres; each crossing spans the game lanes it cuts
(map_review_marks.crossing). The game had no crossing (document or generated) within 25 m of any of them.
- J002-2: red raised crossing with yellow stripes about 45 m east of J1919 (red paving 6 m deep under a
  yellow crosswalk);
- J009-2: the west and east approaches of the J9 roundabout have ochre crossings; drawn yellow;
- J046-4: white crosswalk across the north-south road at the J046 side junction.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r64_plan.py [--check <dir>]
Output: config/maps/review/r64-midblock-crossings.json (MapReviewCrosswalkEdits.ApplyR64)
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_review_marks as M  # noqa: E402
import map_review_r63_plan as R63  # noqa: E402

OUT = M.ROOT / 'config/maps/review/r64-midblock-crossings.json'
E = (1.0, 0.0)
# (refs, card, point, road direction or None (nearest lane), crosswalk colour, red paving depth or 0)
CROSSINGS = [
    ('J002-2', 'J002', (237.0, 89.0), E, M.YELLOW, 6.0),
    ('J009-2', 'J009', (452.0, 88.5), E, M.YELLOW, 0), ('J009-2', 'J009', (559.0, 88.5), E, M.YELLOW, 0),
    ('J046-4', 'J046', (282.2, 1173.7), (.505, -.863), M.WHITE, 0),
]


def plan():
    out = []
    for ref, card, p, d, colour, red in CROSSINGS:
        if red:
            g = M.crossing(p, depth=red, direction=d, pad=.3)
            out.append(dict(refs=ref, card=card, kind='line', type=M.PAVING, text='게임 설정', **{k: g[k] for k in ('x', 'z', 'yaw', 'width', 'length')}))
        g = M.crossing(p, direction=d)
        out.append(dict(refs=ref, card=card, kind='crosswalk', type=colour, text='게임 설정', **{k: g[k] for k in ('x', 'z', 'yaw', 'width', 'length')}))
    for n, e in enumerate(out):
        e.update(id=f"review:r64:{e['card']}:{e['kind']}:{n + 1:03d}", junctionId='', axis=0, sign=1, existing='')
        for k in ('x', 'z', 'yaw', 'width', 'length'):
            e[k] = round(float(e[k]), 3)
        e['width'] = min(e['width'], 40.0)
    return out


if __name__ == '__main__':
    out = plan()
    OUT.write_text(json.dumps(dict(fromRevision=63, toRevision=64, edits=out), ensure_ascii=False, indent=1), encoding='utf-8')
    if '--check' in sys.argv:
        R63.check(out, sys.argv[sys.argv.index('--check') + 1])
    print(OUT, len(out))
    for e in out:
        print(e['refs'], e['kind'], e['type'], e['x'], e['z'], e['yaw'], e['width'], e['length'])
