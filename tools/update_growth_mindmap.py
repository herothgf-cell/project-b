"""2026-11-10 성장 방향(기획22)·면허 경로(기획23) 결정을 마인드맵에 반영.
기존 노드 ID·메모·접힘·순서·링크를 보존하고 새 노드만 추가한다.
실행: python tools/update_growth_mindmap.py        (최초 1회, 변경 전 원본 백업)
검사: python tools/update_growth_mindmap.py --check
"""
import json, sys, collections, time
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
MAP = ROOT / 'docs/content-design/택시 시뮬레이션.mmst'
TOC = MAP.parent / '택시 시뮬레이션-목차.md'
BACK = ROOT / 'docs/design-history/content-design/2026-11-10-mindmap-before-growth.mmst'
GH = 'https://github.com/herothgf-cell/project-b/blob/main/docs/content-design/'
DOC22 = GH + '22-growth-direction-and-vehicle-purchase.md'
DOC23 = GH + '23-license-career-path.md'
IDEA = '기존 아이디어·후속 구상 (미구현/추가 결정)'
PLAY = '현재 플레이 기획'


def walk(n):
    yield n
    for c in n.get('children', []):
        yield from walk(c)


def outline(data):
    lines = ['# 택시 시뮬레이션 마인드맵 목차·진척도', '',
             '[원본](택시%20시뮬레이션.mmst) · [비 프로토 기획](18-rain-prototype.md) · [성장·차량 구매](22-growth-direction-and-vehicle-purchase.md) · [면허 경로(보류)](23-license-career-path.md) · [관리 기준](06-mindmap-review.md)', '',
             '50% 구현 / 25% 기획 완료·미구현 / 0% 기획 필요. 구현 단계는 수동 검증 완료율이 아니다. 비만 프로토에 포함하며 눈/낮밤/확률 미끄러짐 활성화는 후속이다. 2026-11-10: 차종 특성·3일 안 차량 교체·PvP 외형은 기획 확정(미구현), 기사 평점·면허 경로는 보류.', '']

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


def node(key, topic, stage, tag, note, link, children=None):
    now = int(time.time() * 1000)
    n = dict(id='growth-20261110-' + key, topic=topic, createdAt=now, modifiedAt=now, collapsed=False,
             children=children or [], tags=[tag], note=note, hyperlink=link, hyperlinks=[link])
    if stage is not None:
        n['task'] = {'progress': stage}
    return n


if '--check' not in sys.argv:
    if BACK.exists():
        raise SystemExit('Snapshot already exists; use --check')
    BACK.parent.mkdir(parents=True, exist_ok=True)
    BACK.write_bytes(MAP.read_bytes())
    d = json.loads(MAP.read_text(encoding='utf-8'))
    nodes = {n['id']: n for n in walk(d['root'])}

    # 1) 차종 특성 3개: 0% -> 25%
    for key, extra in [('1b0cd003-5246-4ae6-8098-eb0690be91a9', '하이브리드·LPG 2종 연료 소모 차이. 전기 충전은 제외.'),
                       ('62626c82-cce5-424f-8e7b-a54688761506', '트렁크 수용으로 짐 승객 수락 여부가 달라짐.'),
                       ('7d909695-f534-4bd1-a5ba-4bad1b7f1809', '승차감 계수로 "부드럽게" 요청 결과가 달라짐.')]:
        n = nodes[key]
        n['task'] = {'progress': .25}
        n['note'] = n.get('note', '') + '\n\n## 2026-11-10 차종 특성 채택(기획22 §3-2)\n' + extra + '\n운임 배율·속도 상승 없음. 구현 전.\n' + DOC22
        if DOC22 not in n.setdefault('hyperlinks', []):
            n['hyperlinks'].append(DOC22)

    # 2) 구매 방식: 보상 판매 교체
    nodes['f2fd8e55-c12e-456a-b459-a29e6a855dcc']['children'].append(
        node('tradein', '보상 판매 교체 1회·현금 구매 (3일 안, 기획22)', .25, IDEA,
             '2일차 정산 후 구매 → 3일차 새 차 운행이 목표. 할부·신차/중고 시장은 제외(기존 노드 0% 유지).', DOC22))

    # 3) 개인 생활 확장: 차량 교체·PvP 외형
    g = nodes['published-20261106-growth']
    g['children'] += [
        node('car-swap', '3일 안 택시 교체 1회·차량 꾸미기 (기획22)', .25, IDEA,
             '후보 2대(하이브리드 세단/넓은 LPG), 색 2종·대시보드 장식 1슬롯. 방 소품과 개인 지출 예산 공유.', DOC22),
        node('pvp-skin', '싱글 차량 외형 PvP 사용 (기획22, LAN v17)', .25, IDEA,
             '외형·색·장식만. 성능은 PvP 표준. 한 번 구매한 외형은 PvP 목록에 영구 추가.', DOC22)]
    g['note'] = g.get('note', '') + '\n\n## 2026-11-10\n차량 교체·PvP 외형은 하위 노드로 기획 확정(25%). 이사·장기 재출발은 이 노드의 0% 그대로.\n' + DOC22

    # 4) 기존 아이디어: 기사 평점(보류), 면허 경로(보류)
    idea = nodes['8b9fb6f6-146f-4bc3-a32c-6b294034eb5f']
    todos = ['시작 형태 결정: 법인 시작 / 개인 시작 / 선택형', '법인택시 정산: 회사 납입금·월급제 방식과 금액',
             '무사고 기록 규칙: 기간·초기화 조건(사고 등급·벌점)', '면허 자격 조건 단순화: 영업일·운전 점수·교육 이벤트',
             '면허 가격과 저축 페이스(R 배수)', '면허 취득 연출과 개인 영업 전환(차량 구매 연결)',
             '현실 제도 재조사(교통안전공단·지자체 최신 기준)', '시놉시스(10)·성장 단계(12) 개정 범위']
    idea['children'] += [
        node('rating', '기사 평점 (기획22 §3-1, 구현 보류)', 0, IDEA,
             '최근 20회 운전 점수+만족도 별점, 높으면 장거리 호출 추가. 2026-11-10 사용자 결정으로 기획만 유지·구현 보류.', DOC22),
        node('license', '개인택시 면허 경로 (기획23, 본편 보류)', None, IDEA,
             '법인택시 → 무사고 기록 → 개인택시 면허. 2026-11-10 사용자 결정으로 본편 기획 단계로 보류, 할거리만 등록.', DOC23,
             [node('license-%d' % (i + 1), t, 0, IDEA, '기획23 §5 할거리.', DOC23) for i, t in enumerate(todos)])]

    # 5) 게임의 목표 > 기획 메모: 목표 구조 설명
    nodes['bbe2384d-76b1-558d-9ffa-ec5b5561386a']['children'].append(
        node('goal-structure', '목표 구조: 내 택시(3일 안 교체) · 내 집(장기) · 면허(본편 보류)', None, PLAY,
             '기획22 §3. 설명·참조 노드이며 집계 제외.', DOC22))

    refresh(d['root'])
    MAP.write_text(json.dumps(d, ensure_ascii=False, indent=2), encoding='utf-8', newline='\n')
    TOC.write_text(outline(d), encoding='utf-8', newline='\n')

before = json.loads(BACK.read_text(encoding='utf-8'))
after = json.loads(MAP.read_text(encoding='utf-8'))
nodes = {n['id']: n for n in walk(after['root'])}
assert len(nodes) == len(list(walk(after['root']))), 'duplicate id'
for old in walk(before['root']):
    new = nodes[old['id']]
    assert old.get('note', '') in new.get('note', '')
    assert old.get('collapsed') == new.get('collapsed')
    oldids = [c['id'] for c in old.get('children', [])]
    assert [c['id'] for c in new.get('children', []) if c['id'] in oldids] == oldids
    for link in [old.get('hyperlink'), *old.get('hyperlinks', [])]:
        if link:
            assert link in [new.get('hyperlink'), *new.get('hyperlinks', [])]
# The legend line is owned by the newest mindmap script (update_vehicle_mindmap.py); compare the outline body.
assert TOC.read_text(encoding='utf-8').split('\n')[6:] == outline(after).split('\n')[6:]
added = len(nodes) - len(list(walk(before['root'])))
print('GROWTH_MINDMAP_OK original nodes/notes/order/folds/links preserved; nodes=%d added=%d' % (len(nodes), added))
