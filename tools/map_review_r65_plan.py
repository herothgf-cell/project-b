"""Roadview-confirmed school-zone approach paving (map r64 -> r65, 2026-11-26).

- J004-3 (J2145_15970, 은계초 앞): roadview shows every lane of the south-east arm (both directions) paved
  red; the aerial shows it from just outside the bicycle crossing to about 50 m out.
- J025-3 (r47 P51): the red lanes are school-zone paving with "어린이보호구역" and "30", not bus lanes;
  the aerial shows them on the approach lanes into the junction (west arm south side, east arm north side).
Paving pieces follow the game lanes (map_review_marks.paving_arm); text sits on the inbound lanes.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r65_plan.py [--check <dir>]
Output: config/maps/review/r65-school-paving.json (MapReviewCrosswalkEdits.ApplyR65)
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_review_marks as M  # noqa: E402
import map_review_r63_plan as R63  # noqa: E402

OUT = M.ROOT / 'config/maps/review/r65-school-paving.json'
# (refs, card, arm bearing, d0, d1, inbound (None = both directions))
PAVING = [('J004-3', 'J004', 133.0, 16.0, 50.0, None),
          ('J025-3', 'J025', 262.6, 17.0, 58.0, True), ('J025-3', 'J025', 82.6, 18.0, 55.0, True)]
# (refs, card, arm bearing, distance, text) on inbound lanes
# (J004: roadview showed no road text there, so none is added)
TEXT = [('J025-3', 'J025', 262.6, 24.0, '30'), ('J025-3', 'J025', 262.6, 29.0, '어린이\n보호구역'),
        ('J025-3', 'J025', 82.6, 24.0, '30'), ('J025-3', 'J025', 82.6, 29.0, '어린이\n보호구역')]


def plan():
    out = []
    for ref, card, b, d0, d1, inbound in PAVING:
        for p in M.paving_arm(R63.centre(card), b, d0, d1, inbound):
            out.append(dict(refs=ref, card=card, kind='line', type=M.PAVING, text='게임 설정', **{k: p[k] for k in ('x', 'z', 'yaw', 'width', 'length')}))
    for ref, card, b, dist, text in TEXT:
        for q in M.lane_points_on_arm(R63.centre(card), b, dist, True):
            out.append(dict(refs=ref, card=card, kind='line', type=M.TEXT, text=text, x=q['x'], z=q['z'], yaw=q['yaw'], width=q['lanewidth'] * .8, length=3.2 if text == '30' else 4.2))
    for n, e in enumerate(out):
        e.update(id=f"review:r65:{e['card']}:{e['kind']}:{n + 1:03d}", junctionId='', axis=0, sign=1, existing='')
        for k in ('x', 'z', 'yaw', 'width', 'length'):
            e[k] = round(float(e[k]), 3)
        e['width'] = min(e['width'], 40.0)
    return out


if __name__ == '__main__':
    out = plan()
    OUT.write_text(json.dumps(dict(fromRevision=64, toRevision=65, edits=out), ensure_ascii=False, indent=1), encoding='utf-8')
    if '--check' in sys.argv:
        R63.check(out, sys.argv[sys.argv.index('--check') + 1])
    from collections import Counter
    print(OUT, len(out), Counter((e['card'], e['type']) for e in out))
