"""Straight centre lines (map r58 -> r59, 2026-11-25 map review, alignment items with a wavy yellow line).

For each card's junction arm the drawn yellow centre-line pieces ("PAINT Centre line" in a player
-mapAuditDump) between `d0` and `d1` m are read. The straight axis runs between the average piece
positions in the first and last 4 m, so it meets the untouched line at both ends. It is kept only if
every lane on the arm (both directions, connectors excluded) stays clear of it by at least CLEAR m;
otherwise the case is reported and skipped (that one needs lane geometry work).
Output: config/maps/review/r59-centre-lines.json (MapReviewCrosswalkEdits.ApplyR59), a
"lanePresentation" band of type ReviewedCentreLine.Type per straightened stretch.
Usage: python tools/map_review_r59_plan.py <r58 dump map-audit.txt>
"""
import json
import math
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_review_marks as M  # noqa: E402

OUT = M.ROOT / 'config/maps/review/r59-centre-lines.json'
TYPE = '검토 중앙선 직선'
CLEAR = .15
# (refs, card, arm bearing (deg), d0, d1)
# J010's east arm is J012's west arm (one band, both refs). J066's card centre is 48 m off the road it
# describes (x≈786 between J7841 and J7917), so that stretch is measured from J7841's north arm.
CASES = [('J002-1', 'J002', 90, 12, 72), ('J010-2, J012-1', 'J010', 90, 12, 62), ('J012-1', 'J012', 90, 12, 72),
         ('J023-2', 'J023', 270, 12, 75), ('J024-2', 'J024', 0, 4, 45), ('J025-2', 'J025', 270, 12, 70), ('J025-2', 'J025', 90, 12, 70),
         ('J063-2', 'J063', 0, -30, 30), ('J066-2', 'J018', 0, 20, 60)]


def pieces(audit):
    out = []
    for line in Path(audit).read_text(encoding='utf-8', errors='replace').splitlines():
        m = re.match(r'PAINT Centre line at=\(([-\d.]+), [-\d.]+, ([-\d.]+)\) dir=\(([-\d.]+), [-\d.]+, ([-\d.]+)\)', line)
        if m:
            out.append(tuple(map(float, m.groups())))
    return out


def plan(audit):
    ps = pieces(audit); out = []
    for ref, card, bearing, d0, d1 in CASES:
        it = M.ITEMS[card]; c = (it['x'], it['z']); b = math.radians(bearing); ax = (math.sin(b), math.cos(b)); rx = (ax[1], -ax[0])
        sel = []
        for x, z, dx, dz in ps:
            along = (x - c[0]) * ax[0] + (z - c[1]) * ax[1]; side = (x - c[0]) * rx[0] + (z - c[1]) * rx[1]
            if d0 <= along <= d1 and abs(side) <= 9 and abs(dx * ax[0] + dz * ax[1]) > .7:
                sel.append((along, side, x, z))
        if len(sel) < 8:
            print(f'{ref} {card} arm {bearing}: {len(sel)} centre pieces, skipped'); continue
        sel.sort(); lo, hi = sel[0][0], sel[-1][0]
        e0 = [s for s in sel if s[0] <= lo + 4]; e1 = [s for s in sel if s[0] >= hi - 4]
        p0 = (sum(s[2] for s in e0) / len(e0), sum(s[3] for s in e0) / len(e0)); p1 = (sum(s[2] for s in e1) / len(e1), sum(s[3] for s in e1) / len(e1))
        f = M.unit(p0, p1); r = (f[1], -f[0]); L = math.dist(p0, p1)
        wiggle = max(abs((s[2] - p0[0]) * r[0] + (s[3] - p0[1]) * r[1]) for s in sel)
        clear = 99.0; worst = ''
        for l in M.LANES:
            if l.get('turn', 0) != 0 or str(l['id']).startswith('join:'):
                continue
            w = l.get('width', 3.5) / 2
            for q in l['points']:
                u = (q['x'] - p0[0]) * f[0] + (q['z'] - p0[1]) * f[1]
                if not 0 <= u <= L:
                    continue
                v = abs((q['x'] - p0[0]) * r[0] + (q['z'] - p0[1]) * r[1]) - w
                if v < clear:
                    clear, worst = v, l['id']
        ok = clear >= CLEAR and wiggle > .4
        print(f'{ref} {card} arm {bearing}: pieces {len(sel)}, length {L:.1f}, wiggle {wiggle:.2f} m, lane clearance {clear:.2f} m ({worst[:40]}) -> {"band" if ok else "skip"}')
        if ok:  # the map validator caps facility length at 40 m: split the band along the same axis
            n = math.ceil((L + .5) / 39.0); piece = (L + .5) / n
            for k in range(n):
                at = -.25 + piece * (k + .5); mid = (p0[0] + f[0] * at, p0[1] + f[1] * at)
                out.append(dict(refs=ref, card=card, kind='lanePresentation', type=TYPE, text='게임 설정', junctionId='', axis=0, sign=1, existing='',
                                id=f'review:r59:{card}:centre:{bearing:g}:{k + 1}', x=round(mid[0], 3), z=round(mid[1], 3), yaw=round(M.yaw_of(f), 3),
                                width=round(min(12.0, 2 * wiggle + 1.2), 3), length=round(piece + .02, 3)))
    return out


if __name__ == '__main__':
    out = plan(sys.argv[1])
    OUT.write_text(json.dumps(dict(fromRevision=58, toRevision=59, edits=out), ensure_ascii=False, indent=1), encoding='utf-8')
    print(OUT, len(out))
