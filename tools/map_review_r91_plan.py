"""Median rails onto the medians, smooth J7876 centre line (map r90 -> r91, 2026-11-28).

The "black seam" lines of J056-2 / J063-2 / J066-2 are reviewed median post-rails (black posts, yellow bands,
rails; config/maps/eungye-road-facilities.json, kind post-rail) left on old median lines:
- eungyejungang-Y10-west-median-rail (J5852 west arm): still on the pre-r87 median, inside the eastbound
  carriageway. It moves onto the r87 hatched median (map_review_r87_plan.MEDIAN_T) at the same stations.
- survey-r43-g10-g11-median (J7876 / J8346, z 138-226) and the r47 centre line there (reviewed:r47:A:median:*)
  follow the pre-r68 lanes: the line waves about 2 m through J7876 and the rail runs up to 1.5 m inside the
  southbound inner lane. Both move onto one smooth line midway between the opposing inner lanes (J063-2 /
  J066-2), and the single line becomes a double yellow line 0.36 m apart (J063-3; the rail is the median
  barrier the item asks for).
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r91_plan.py
Output: config/maps/review/r91-median-rails.json (MapReviewGeometryEdit.ApplyFromCommandLine) and an
updated config/maps/eungye-road-facilities.json (copied to Unity Resources; local-input-manifest refreshed).
"""
import hashlib
import json
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402
import map_review_r87_plan as P87  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r91-median-rails.json'
CATALOG = G.ROOT / 'config/maps/eungye-road-facilities.json'
RESOURCE = G.ROOT / 'unity/EungyeTaxi/Assets/Resources/eungye-road-facilities.json'
MANIFEST = G.ROOT / 'config/maps/local-input-manifest.json'
OLD_LINES = [f['id'] for f in E.DOC['facilities'] if f['id'].startswith('reviewed:r47:A:median')]
LINE_SRC = 'reviewed:r47:A:median:north'
# midline between the opposing inner lanes (r90 lanes; inside J7876, z 150-180, a straight join)
KNOTS = [(120, 786.28), (144, 787.57), (204, 789.13), (228, 790.19)]
GAP = 0.18                                     # each yellow line this far from the midline


def mid_x(z):
    """Catmull-Rom through KNOTS (clamped ends)"""
    k = KNOTS
    for i in range(len(k) - 1):
        if k[i][0] <= z <= k[i + 1][0]:
            p0, p1, p2, p3 = k[max(0, i - 1)][1], k[i][1], k[i + 1][1], k[min(len(k) - 1, i + 2)][1]
            t = (z - k[i][0]) / (k[i + 1][0] - k[i][0])
            return .5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3)
    return k[0][1] if z < k[0][0] else k[-1][1]


def build():
    assert E.DOC['revision'] == 90, E.DOC['revision']
    P = E.LanePatch([], 90, 91)
    P.remove_mark(OLD_LINES)
    z0, z1 = 126.0, 207.0                      # the old line's span
    n = int((z1 - z0) / 4)
    zs = [z0 + (z1 - z0) * i / n for i in range(n + 1)]
    for side, sg in (('west', -1), ('east', 1)):
        for k, (a, b) in enumerate(zip(zs, zs[1:])):
            pa, pb = (mid_x(a) + sg * GAP, a), (mid_x(b) + sg * GAP, b)
            item = P.copy_mark(LINE_SRC, f'review:r91:J063:centre:{side}:{k:02d}')
            item.update(x=round((pa[0] + pb[0]) / 2, 4), z=round((pa[1] + pb[1]) / 2, 4), width=.12,
                        yaw=round(math.degrees(math.atan2(pb[0] - pa[0], pb[1] - pa[1])) % 360, 3), length=round(math.dist(pa, pb) + .05, 4))
    return P


def rails():
    """edit only the two rails' point values in the hand-formatted catalog text (other lines stay byte-identical)"""
    import re
    lines = CATALOG.read_text(encoding='utf-8').split('\n')
    def block(fid):
        i = next(k for k, l in enumerate(lines) if l.strip() == f'"id": "{fid}",')
        j = next(k for k in range(i + 1, len(lines)) if lines[k].strip().startswith('"id": "'))
        return i, j
    num = re.compile(r'^(\s*"(x|z)": )(-?[0-9.]+)(,?)$')
    for fid in ('survey-r43-g10-g11-median', 'eungyejungang-Y10-west-median-rail'):
        i, j = block(fid)
        k = i
        while k < j:
            mx = num.match(lines[k]); mz = num.match(lines[k + 1]) if k + 1 < j else None
            if mx and mz and mx.group(2) == 'x' and mz.group(2) == 'z':
                x, z = float(mx.group(3)), float(mz.group(3))
                if fid.startswith('survey'):
                    x = round(mid_x(z), 2)
                else:
                    sst = (x - P87.A[0]) * P87.U[0] + (z - P87.A[1]) * P87.U[1]
                    x, z = (round(v, 3) for v in P87.W(sst, P87.MEDIAN_T))
                lines[k] = f'{mx.group(1)}{x:g}{mx.group(4)}'; lines[k + 1] = f'{mz.group(1)}{z:g}{mz.group(4)}'
                k += 2
            else:
                k += 1
    return '\n'.join(lines)


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, len(r['addFacilities']), 'lines,', len(r['removeFacilities']), 'removed')
    if '--rails' in sys.argv:
        text = rails()
        CATALOG.write_text(text, encoding='utf-8', newline='\n'); RESOURCE.write_bytes(CATALOG.read_bytes())
        import re
        data = CATALOG.read_bytes(); m = MANIFEST.read_bytes().decode('utf-8')
        m = re.sub(r'("path": "config/maps/eungye-road-facilities.json",\s*"bytes": )\d+(,\s*"sha256": ")[0-9a-f]{64}',
                   lambda g: f'{g.group(1)}{len(data)}{g.group(2)}{hashlib.sha256(data).hexdigest()}', m)
        MANIFEST.write_bytes(m.encode('utf-8'))
        print('rails moved; catalog', len(data), hashlib.sha256(data).hexdigest()[:12])
