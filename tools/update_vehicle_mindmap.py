"""2026-11-10 기획22 구현(사람 검증 전)을 마인드맵에 반영: 차종 특성 3노드와 차량 교체·보상 판매·PvP 외형 노드 25% -> 50%.
update_growth_mindmap.py와 같은 방식: 변경 전 원본 백업, 기존 노드 ID·메모·접힘·순서·링크 보존, 목차 재생성.
실행: python tools/update_vehicle_mindmap.py        (최초 1회, 변경 전 원본 백업)
검사: python tools/update_vehicle_mindmap.py --check
"""
import json, sys, collections, time
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
MAP = ROOT / 'docs/content-design/택시 시뮬레이션.mmst'
TOC = MAP.parent / '택시 시뮬레이션-목차.md'
BACK = ROOT / 'docs/design-history/content-design/2026-11-10-mindmap-before-vehicle-impl.mmst'
GH = 'https://github.com/herothgf-cell/project-b/blob/main/docs/'
DOC22 = GH + 'content-design/22-growth-direction-and-vehicle-purchase.md'
HISTORY = GH + 'work-history/2026-11-10-vehicle-purchase-3day.md'
TARGETS = {
    '1b0cd003-5246-4ae6-8098-eb0690be91a9': '연료 소모 계수(시작 1.0 / 하이브리드 0.65 / LPG 0.95)를 계정 연료 소모에 적용.',
    '62626c82-cce5-424f-8e7b-a54688761506': '하이브리드는 짐 많은 승객·흙 묻은 짐 승객 호출을 받지 않음. 시작·LPG는 기존과 같음.',
    '7d909695-f534-4bd1-a5ba-4bad1b7f1809': 'LPG 승차감 1.08: 급가감속 판정 기준을 넓혀 "부드럽게" 요청 감점이 덜 남.',
    'growth-20261110-tradein': '차고에서 견적(새 차 가격·내구도 반영 보상 판매·최종 지출·남는 현금) 후 현금 구매 1회.',
    'growth-20261110-car-swap': '내 공간 → 차고 탭. 후보 2대, 색 2종, 대시보드 화분. 방 소품과 같은 개인 지출.',
    'growth-20261110-pvp-skin': '해금 목록 별도 파일(pvp-vehicle-unlocks.json), PvP "내 차" 선택, 봇 무작위 외형, LAN v17.',
}


def walk(n):
    yield n
    for c in n.get('children', []):
        yield from walk(c)


def outline(data):
    lines = ['# 택시 시뮬레이션 마인드맵 목차·진척도', '',
             '[원본](택시%20시뮬레이션.mmst) · [비 프로토 기획](18-rain-prototype.md) · [성장·차량 구매](22-growth-direction-and-vehicle-purchase.md) · [면허 경로(보류)](23-license-career-path.md) · [관리 기준](06-mindmap-review.md)', '',
             '50% 구현 / 25% 기획 완료·미구현 / 0% 기획 필요. 구현 단계는 수동 검증 완료율이 아니다. 비만 프로토에 포함하며 눈/낮밤/확률 미끄러짐 활성화는 후속이다. 2026-11-10: 차종 특성·3일 안 차량 교체·PvP 외형은 구현(사람 검증 전, 50%), 기사 평점·면허 경로는 보류.', '']

    def emit(n, depth):
        lines.append('  ' * depth + '- ' + n['topic'] + ' — ' + next((t[3:] for t in n.get('tags', []) if t.startswith('단계:')), '설명·참조'))
        for c in n.get('children', []):
            emit(c, depth + 1)
    for n in data['root']['children']:
        emit(n, 0)
    return '\n'.join(lines) + '\n'


def refresh(n):
    count = collections.Counter()
    for c in n.get('children', []):
        count.update(refresh(c))
    stage = n.get('task', {}).get('progress')
    if stage is not None:
        count[stage] += 1
    label = ('50% 구현' if stage == .5 else '25% 기획 완료·미구현' if stage == .25 else '0% 기획 필요') if stage is not None else (
        '하위 구현 %d / 기획 %d / 미정 %d' % (count[.5], count[.25], count[0]) if count else '설명·참조')
    n['tags'] = [t for t in n.get('tags', []) if not t.startswith('단계:')] + ['단계:' + label]
    return count


if '--check' not in sys.argv:
    if BACK.exists():
        raise SystemExit('Snapshot already exists; use --check')
    BACK.parent.mkdir(parents=True, exist_ok=True)
    BACK.write_bytes(MAP.read_bytes())
    d = json.loads(MAP.read_text(encoding='utf-8'))
    nodes = {n['id']: n for n in walk(d['root'])}
    for key, extra in TARGETS.items():
        n = nodes[key]
        if n.get('task', {}).get('progress') != .25:
            raise SystemExit('Unexpected stage for ' + key)
        n['task'] = {'progress': .5}
        n['modifiedAt'] = int(time.time() * 1000)
        n['note'] = n.get('note', '') + '\n\n## 2026-11-10 구현(사람 검증 전, 기획22 §5·§6)\n' + extra + '\n위치 제어·자동 검사만 수행. 수동 플레이·다른 PC LAN 미검증.\n' + HISTORY
        for link in (DOC22, HISTORY):
            if link not in n.setdefault('hyperlinks', []):
                n['hyperlinks'].append(link)
    refresh(d['root'])
    MAP.write_text(json.dumps(d, ensure_ascii=False, indent=2), encoding='utf-8', newline='\n')
    TOC.write_text(outline(d), encoding='utf-8', newline='\n')

before = json.loads(BACK.read_text(encoding='utf-8'))
after = json.loads(MAP.read_text(encoding='utf-8'))
nodes = {n['id']: n for n in walk(after['root'])}
assert len(nodes) == len(list(walk(after['root']))), 'duplicate id'
assert len(nodes) == len(list(walk(before['root']))), 'no nodes added or removed'
for old in walk(before['root']):
    new = nodes[old['id']]
    assert old['topic'] == new['topic']
    assert old.get('note', '') in new.get('note', '')
    assert old.get('collapsed') == new.get('collapsed')
    assert [c['id'] for c in old.get('children', [])] == [c['id'] for c in new.get('children', [])]
    for link in [old.get('hyperlink'), *old.get('hyperlinks', [])]:
        if link:
            assert link in [new.get('hyperlink'), *new.get('hyperlinks', [])]
    stage = new.get('task', {}).get('progress')
    assert stage == (.5 if old['id'] in TARGETS else old.get('task', {}).get('progress')), old['id']
assert TOC.read_text(encoding='utf-8') == outline(after)
print('VEHICLE_MINDMAP_OK nodes=%d raised=%d (25%%->50%%); ids/notes/order/folds/links preserved' % (len(nodes), len(TARGETS)))
