"""Roadview survey helper for the remaining map review items (2026-11-26).

targets: writes artifacts/map-review-20261124/roadview/targets.json with, per RV item, the world centre of its
  sheet cells, lat/lon and a map.kakao.com/link/roadview URL (opens the nearest panorama).
pan <urlX> <urlY> <ref>: heading (deg) from a panorama (WCONGNAMUL urlX/urlY from the page URL) to the item,
  and the URL that opens that panorama looking at it.
record <ref> <verdict> <note>: appends a judgement to artifacts/.../roadview/results.json (survives restarts).
Kakao imagery stays in Git-ignored artifacts/; only the verdicts go to config/maps/review later.
"""
import json
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_review_marks as M  # noqa: E402
from kakao_aerial_compare import cong  # noqa: E402

OUT = M.ROOT / 'artifacts/map-review-20261124/roadview'


def targets():
    OUT.mkdir(parents=True, exist_ok=True)
    work = json.loads((M.ROOT / 'config/maps/review/2026-11-26-remaining-work.json').read_text(encoding='utf-8'))['rows']
    out = []
    for r in work:
        if r['package'] != 'RV':
            continue
        card, n = r['ref'].split('-'); iss = M.ITEMS[card]['issues'][int(n) - 1]
        try:
            x0, z0, x1, z1 = M.cells(card, iss['cells']); x, z = (x0 + x1) / 2, (z0 + z1) / 2
        except Exception:
            x, z = M.ITEMS[card]['x'], M.ITEMS[card]['z']
        cx, cy, lat, lon = cong(x, z)
        out.append(dict(ref=r['ref'], category=r['category'], x=round(x, 1), z=round(z, 1), cx=cx, cy=cy, lat=lat, lon=lon,
                        link=f'https://map.kakao.com/link/roadview/{lat:.7f},{lon:.7f}', after=r['after']))
    (OUT / 'targets.json').write_text(json.dumps(out, ensure_ascii=False, indent=1), encoding='utf-8')
    print(len(out))


def pan(url, ref, _=None):
    import re
    m = re.search(r'urlX=([\d.]+)&urlY=([\d.]+)', url); pid = re.search(r'panoid=(\d+)', url).group(1)
    ux, uy = m.group(1), m.group(2)
    _pan(ux, uy, ref)
    print(f'https://map.kakao.com/?panoid={pid}&pan={{pan}}&tilt=10&zoom=0&map_type=TYPE_MAP&map_attribute=ROADVIEW&urlX={ux}&urlY={uy}')


def _pan(ux, uy, ref):
    t = {x['ref']: x for x in json.loads((OUT / 'targets.json').read_text(encoding='utf-8'))}[ref]
    px, py = float(ux) / 2.5, float(uy) / 2.5
    h = math.degrees(math.atan2(t['cx'] - px, t['cy'] - py)) % 360
    print(round(h, 1), round(math.hypot(t['cx'] - px, t['cy'] - py), 1), t['after'][:90])


def record(ref, verdict, note):
    path = OUT / 'results.json'
    data = json.loads(path.read_text(encoding='utf-8')) if path.exists() else {}
    data[ref] = dict(verdict=verdict, note=note)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=1), encoding='utf-8')
    print(len(data))


def export():
    """Copy the verdicts (text only, no imagery) next to the decision list."""
    data = json.loads((OUT / 'results.json').read_text(encoding='utf-8'))
    dst = M.ROOT / 'config/maps/review/2026-11-26-roadview-verdicts.json'
    dst.write_text(json.dumps(dict(made='2026-11-26', source='카카오 로드뷰(내장 브라우저), 촬영월은 각 메모', verdicts=data), ensure_ascii=False, indent=1), encoding='utf-8')
    print(dst, len(data))


if __name__ == '__main__':
    if sys.argv[1] == 'export':
        export(); sys.exit()
    {'targets': lambda: targets(), 'pan': lambda: pan(*sys.argv[2:5]), 'record': lambda: record(*sys.argv[2:5])}[sys.argv[1]]()
