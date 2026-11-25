"""Roadview-confirmed yellow school-zone crosswalks (map r61 -> r62, 2026-11-26).

- J044-2/3 (J2608_13908): the roadview shows yellow crosswalks on the junction's arms but the game has none
  (an unsignalized junction the traffic system does not model), so paint-only yellow crossings go on every
  arm, placed like the r56 junction crossings (map_review_marks.arms / arm_crossing).
- J005-2 (J2564_12237): the north-west and south-east arm crossings (generated a1 s+1 / s-1) turn yellow.
Usage: python tools/map_review_r62_plan.py <r60/r61 dump map-audit.txt>  -> config/maps/review/r62-yellow-crossings.json
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_review_marks as M  # noqa: E402
from map_crosswalk_approaches import approaches  # noqa: E402

OUT = M.ROOT / 'config/maps/review/r62-yellow-crossings.json'


def main(audit):
    out = []
    it = M.ITEMS['J044']; c = (it['x'], it['z']); found = M.arms(c)
    print('J044 arms', found)
    for b in found:
        g = M.arm_crossing(c, b, found)
        out.append(dict(refs='J044-2, J044-3', kind='crosswalk', junctionId='', axis=0, sign=1, existing='', id=f'review:r62:J044:arm:{b:g}',
                        type=M.YELLOW, text='게임 설정', **{k: round(g[k], 3) for k in ('x', 'z', 'yaw', 'width', 'length')}))
    gen = approaches(audit)
    for a, s in ((1, 1), (1, -1)):
        g = gen[('J2564_12237', a, s)]
        out.append(dict(refs='J005-2', kind='crosswalk', junctionId='J2564_12237', axis=a, sign=s, existing='', id=f'review:r62:J2564_12237:a{a}:s{s}:crosswalk',
                        type=M.YELLOW, text='게임 설정', x=g['x'], z=g['z'], yaw=g['yaw'], width=min(g['width'], 40.0), length=2.4))
    OUT.write_text(json.dumps(dict(fromRevision=61, toRevision=62, edits=out), ensure_ascii=False, indent=1), encoding='utf-8')
    print(OUT, len(out), [round(e['width'], 1) for e in out])


if __name__ == '__main__':
    main(sys.argv[1])
