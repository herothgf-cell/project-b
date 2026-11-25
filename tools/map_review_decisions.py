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
    'J019-3': 'r55 동서 황색',
    # r61 north pedestrian cross (J1897)
    **{r: 'r61 보행 십자 J1897 광장으로(차로 끔, 광장 재질), 로드뷰 확인' for r in ('J005-3', 'J032-1', 'J036-1', 'J036-2', 'J039-1', 'J039-2')},
    # r60 pedestrian plaza (roadview-confirmed)
    **{r: 'r60 보행 광장으로(차로 끔, 광장 재질), 로드뷰 확인' for r in ('J001-3', 'J038-2', 'J038-3', 'J040-2', 'J043-1', 'J043-2', 'J045-1', 'J046-2', 'J047-2')},
    # 2026-11-26 closed without a map change (checked)
    'J043-3': '변경 없음: 시트도 "유지"(게임 위치가 실제와 거의 같음)', 'J077-5': '변경 없음: 덤프에 그 자리 세계 물체가 없음(캡처 화면에 겹친 UI)',
    'J072-4': '변경 없음: 곡선 포장면은 보도 재질(도로변 보도)이라 실제 공원 산책로와 맞음',
    'J024-4': 'Q6 규칙: 횡단보도·확장 안 함', 'J025-4': 'Q6 규칙: 진입 공간 폭 확대 안 함', 'J044-4': 'Q6 규칙: 아파트 입구 횡단보도 안 넣음',
    'J075-2': 'Q6 규칙: 골목 연장 안 함', 'J075-3': 'Q6 규칙: 입구 횡단보도·정차금지지대 안 넣음',
    # r59 straight centre lines
    'J002-1': 'r59 동쪽 팔 중앙선 직선', 'J010-2': 'r59 동쪽 팔 중앙선 직선', 'J012-1': 'r59 서쪽 중앙선 직선(동쪽은 차로가 걸려 차로 형상 작업)',
    'J025-2': 'r59 양쪽 중앙선 직선',
    # r58 line types
    'J004-2': 'r58 좌회전 유도선', 'J007-1': 'r58 좌회전 유도선', 'J011-3': 'r58 좌회전 유도선', 'J013-2': 'r58 좌회전 유도선',
    'J016-2': 'r58 좌회전 유도선', 'J017-3': 'r58 좌회전 유도선(직진 연장 점선 제외)', 'J021-4': 'r58 좌회전 유도선',
    'J007-3': 'r58 북서 진입 지그재그 서행선·적색 포장(Q9: 항공사진으로 확인)',
    # r57 crosswalk moves with stop lines
    'J001-1': 'r57 교차로 앞 큰길 횡단보도 삭제(실제 넓은 횡단보도는 북동쪽에 이미 있음, 폭 확대 제외)', 'J001-2': 'r57 남서 큰길 횡단보도 삭제',
    'J003-1': 'r57 서쪽 13 m 이동+정지선', 'J003-2': 'r57 동쪽 12 m 이동+자전거횡단도+정지선',
    'J005-1': 'r57 교차로 앞 2곳 삭제, 북동쪽 횡단보도 추가(남서쪽은 기존)', 'J006-1': 'r57 남서 삭제, 남서쪽 횡단보도 추가',
    'J011-1': 'r57 서쪽 8 m 교차로 쪽', 'J022-3': 'r57 북쪽 수평 2열(28 m)+정지선', 'J024-1': 'r57 4.8 m 북쪽+정지선',
    'J077-4': 'r57 J866 북·남 수평, 동쪽 8 m 이동+정지선', 'J004-4': 'r57 북서 10 m 바깥+정지선', 'J008-1': 'r57 북쪽 12 m 남쪽',
    'J008-2': 'r57 동쪽 5 m 안쪽(서쪽은 유턴 대기 문제로 유지)', 'J018-2': 'r57 남북 2열 깊이 6 m', 'J019-1': 'r57 남쪽 2열 깊이 6 m', 'J020-1': 'r57 남쪽 6 m 북쪽',
    'J020-2': 'r57 서 3 m·동 3.5 m 안쪽(방향 보정 제외)', 'J021-2': 'r57 네 곳 2열 깊이 5~6 m', 'J022-4': 'r57 서쪽 4 m 바깥+정지선(2열 깊이 제외)',
    # r56 markings batch 2
    'J010-1': 'r56 대각선 횡단 2개', 'J018-3': 'r56 서쪽 팔 적색 포장·30', 'J019-2': 'r56 북쪽 횡단보도 삭제', 'J067-1': 'r56 북쪽 횡단보도 삭제',
    'J019-4': 'r56 남쪽 팔 적색 포장·30', 'J023-3': 'r56 예고 마름모 2', 'J027-1': 'r56 횡단보도(정지선 제외)', 'J027-2': 'r56 횡단보도',
    'J029-1': 'r56 황색 횡단보도(정지선 제외)', 'J037-1': 'r56 서·북·남 횡단보도 3곳(북측 추가 1곳·먼 서쪽 1곳 제외)',
    'J041-4': 'r56 예고 마름모', 'J042-3': 'r56 황색 횡단보도 3곳(게임에 북서 가지 없음)', 'J042-4': 'r56 남북 적색 포장', 'J043-4': 'r56 예고 마름모',
    'J049-3': 'r56 교차로 세 가지 횡단보도(먼 B3·A1 제외)', 'J049-4': 'r56 예고 마름모 2', 'J050-2': 'r56 네 가지 흰 횡단보도(C3·B2 황색 제외)',
    'J051-4': 'r56 황색 횡단보도 3곳·남쪽 적색 포장', 'J052-2': 'r56 황색 횡단보도 3곳(G2~H1 제외)', 'J052-3': 'r56 세 가지 적색 포장·글자',
    'J054-4': 'r56 횡단보도', 'J055-1': 'r56 서쪽 가지 횡단보도', 'J055-3': 'r56 예고 마름모', 'J056-4': 'r56 횡단보도(정지선 제외)', 'J060-1': 'r56 횡단보도',
    'J061-1': 'r56 횡단보도 3곳(서쪽 자전거횡단도)', 'J062-1': 'r56 횡단보도 3곳(서쪽 자전거횡단도)', 'J068-1': 'r56 횡단보도 3곳(자전거횡단도 2)',
    'J072-1': 'r56 횡단보도 3곳(자전거횡단도 3)', 'J063-4': 'r56 적색 포장·30·글자', 'J065-2': 'r56 남북 진입 적색 포장', 'J066-3': 'r56 적색 포장·30·글자', 'J067-2': 'r55 서·동·남 황색 (남쪽 폭 확대·적색 띠 제외)', 'J021-1': 'r55 4곳 황색, r56 적색 포장·30·어린이보호구역 글자',
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
