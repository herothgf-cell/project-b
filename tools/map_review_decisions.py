"""Merge the 2026-11-25 review answers into one decision list per issue (the map-fix work queue).

Inputs: the sheet items (artifacts/map-review-20261124/sheet/items.json, Git-ignored), the reviewer's
feedback JSON (per-issue decision, blank = card decision) and the confirmation answers Q1-Q10.
The confirmation answers override the disputed issues listed in ANSWERS. Issues already fixed are
listed in DONE with the map revision. Output: config/maps/review/2026-11-25-map-review-decisions.json
Usage: python tools/map_review_decisions.py <feedback.json> <answers.json>
"""
import json
import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'config/maps/review/2026-11-25-map-review-decisions.json'
CATS = dict(W='폭·인도·횡단보도', L='선형', M='선 종류', F='차도 위 시설', N='차로 수', C='연결·교차로 모양', G='노면 표시', X='기타')
# Q answers (map-review-answers-2026-11-25.json) -> decision per issue
ANSWERS = {
    'Q1': {'J029-2': 'reject', 'J031-1': 'reject', 'J034-1': 'reject', 'J039-3': 'reject'},   # all speed bumps, game kept
    'Q2': {'J041-1': 'reject', 'J045-3': 'reject'},                                            # speed bump, game kept
    'Q3': {'J045-2': 'reject', 'J047-3': 'reject'},                                            # speed caution marking, no crosswalk
    'Q4': {'J066-4': 'reject', 'J069-1': 'reject'},                                            # no red raised crosswalk
    'Q5': {'J066-5': 'reject', 'J069-2': 'reject'},                                            # speed bump, not moved
    'Q6': {'J024-4': 'rule', 'J025-4': 'rule', 'J044-4': 'rule', 'J075-2': 'rule', 'J075-3': 'rule'},  # short entrance, no centre line, no widening/crosswalk/extension
    'Q7': {'J064-1': 'reject'},
    'Q8': {'J022-1': 'approve'},
    'Q9': {'J004-3': 'roadview', 'J007-3': 'roadview'},
    'Q10': {'J008-4': 'approve', 'J020-4': 'approve'},                                         # four corner traffic islands
}
RULE_Q6 = '아파트 입구: 중앙선 없는 짧은 진입로로만 정리, 넓히거나 횡단보도·연장 안 함'
DONE = {  # issue -> revision and what was done (partial fixes say so)
    'J004-1': 'r55 횡단보도 4곳 황색 + 바깥쪽 자전거횡단도 (적색 노면 제외)',
    'J007-2': 'r55 자전거횡단도 4곳(실제는 바깥쪽)', 'J008-3': 'r55 자전거횡단도 4곳', 'J011-2': 'r55 자전거횡단도 2곳(실제는 바깥쪽)',
    'J013-1': 'r55 자전거횡단도 3곳', 'J016-1': 'r55 자전거횡단도 4곳(남쪽 포함)', 'J017-2': 'r55 자전거횡단도 4곳', 'J020-3': 'r55 자전거횡단도 4곳',
    'J018-1': 'r55 황색 대각선 횡단보도 2개', 'J018-4': 'r55 4곳 황색', 'J065-1': 'r55 4곳 황색 + 대각선 2개 (남북 폭 8 m 확대 제외)',
    'P01-1': 'r54 신호등 기둥을 인도로 옮김',
    **{f'P{n:02d}-1': '고칠 것 없음: 높이 4.45~6.95 m의 표지 받침대(바닥 기둥 아님, r54 기록)' for n in range(2, 17)},
    'J019-3': 'r55 동서 황색', 'J067-2': 'r55 서·동·남 황색 (남쪽 폭 확대·적색 띠 제외)', 'J021-1': 'r55 4곳 황색 (적색 노면·글자 제외)',
}


def main(feedback, answers):
    items = json.loads((ROOT / 'artifacts/map-review-20261124/sheet/items.json').read_text(encoding='utf-8'))
    fb = {i['id']: i for i in json.loads(Path(feedback).read_text(encoding='utf-8'))['items']}
    ans = {a['id']: a for a in json.loads(Path(answers).read_text(encoding='utf-8'))['answers']}
    override = {ref: (q, d) for q, m in ANSWERS.items() for ref, d in m.items()}
    rows = []
    for it in items:
        f = fb[it['id']]
        for k, iss in enumerate(it['issues'], 1):
            ref = f"{it['id']}-{k}"
            dec = f['issues'].get(str(k)) or f['status'] or ''
            source = 'issue' if f['issues'].get(str(k)) else 'card'
            if ref in override:
                q, dec = override[ref]; source = f"{q}: {ans[q]['choice']}"
            rows.append(dict(ref=ref, junction=it.get('junction', ''), zone=it.get('zone', ''), category=iss['cat'], categoryName=CATS.get(iss['cat'], ''),
                             confidence=iss.get('confidence', ''), roadview=bool(iss.get('roadview')), decision=dec, source=source,
                             before=iss['before'], after=iss['after'], note=f.get('note', ''), done=DONE.get(ref, '')))
    OUT.write_text(json.dumps(dict(made='2026-11-25', rule_Q6=RULE_Q6, issues=rows), ensure_ascii=False, indent=1), encoding='utf-8')
    c = Counter(r['decision'] for r in rows)
    todo = Counter(r['category'] for r in rows if r['decision'] in ('approve', 'rule', 'roadview') and not r['done'])
    print(OUT, dict(c), 'done', sum(1 for r in rows if r['done']), 'todo by category', dict(todo))


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
