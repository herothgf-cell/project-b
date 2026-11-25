"""Split the remaining 2026-11 map review items into work packages (after markings batches r55-r59).

Packages:
  RV   needs a roadview look before anything is changed (the approved text says so, or it is flagged)
  NET  road network change: add/remove/extend roads, plazas, islands (new lanes, connectors, junction
       rules, scene road/sidewalk objects, routes, signals, stops)
  GEO  lane/kerb geometry on an existing road: widths, kerb lines, lane counts, alignment, safety zones
  PAINT remaining paint that still fits the facility tools
Output: config/maps/review/2026-11-26-remaining-work.json and a printed summary.
Usage: python tools/map_review_triage.py
"""
import json
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
D = json.loads((ROOT / 'config/maps/review/2026-11-25-map-review-decisions.json').read_text(encoding='utf-8'))
PAIRS = {p['junction']: p for p in json.loads((ROOT / 'artifacts/map-review-20261124/kakao/pairs.json').read_text(encoding='utf-8'))}
NET_WORDS = ('추가', '삭제', '연장', '연결', '교통섬', '광장', '진입로', '골목', '입체', '측도', '회차')


def package(r):
    if r['roadview'] or r['decision'] == 'roadview' or '로드뷰' in r['after']:
        return 'RV'
    if r['category'] == 'C' or (r['category'] in ('X',) and any(w in r['after'] for w in NET_WORDS)):
        return 'NET'
    if r['category'] in ('W', 'N', 'L') or (r['category'] == 'M' and ('빗금' in r['after'] or '중앙선' in r['after'])):
        return 'GEO'
    return 'PAINT'


def main():
    rows = []
    for r in D['issues']:
        if r['decision'] not in ('approve', 'rule', 'roadview') or r['done']:
            continue
        p = PAIRS.get(r['junction'], {})
        rows.append(dict(ref=r['ref'], junction=r['junction'], category=r['category'], package=package(r), decision=r['decision'],
                         after=r['after'], note=r['note'], roadview=p.get('roadview', ''), skyview=p.get('skyview', '')))
    out = ROOT / 'config/maps/review/2026-11-26-remaining-work.json'
    out.write_text(json.dumps(dict(made='2026-11-26', rows=rows), ensure_ascii=False, indent=1), encoding='utf-8')
    c = Counter(r['package'] for r in rows); by = defaultdict(Counter)
    for r in rows:
        by[r['package']][r['category']] += 1
    print(out, len(rows), dict(c), {k: dict(v) for k, v in by.items()})
    return rows


if __name__ == '__main__':
    main()
