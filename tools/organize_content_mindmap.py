"""Organize the native content map without dropping original node identities."""
import argparse
import hashlib
import json
import shutil
import time
import uuid
from pathlib import Path
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[1]
MAP = ROOT / 'docs/content-design/택시 시뮬레이션.mmst'
BACKUP = ROOT / 'docs/design-history/content-design/2026-11-02-mindmap-before-organize.mmst'
OLD = ROOT / 'docs/design-history/content-design/2026-10-27-mindmap-before-update.mmst'
OUTLINE = ROOT / 'docs/content-design/택시 시뮬레이션-목차.md'
BASE = 'https://github.com/herothgf-cell/project-b/blob/main/'
VERSION = '2026-11-02'
REVIEW = 'docs/future-design/2026-11-02-simulation-review.md'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def walk(node):
    yield node
    for child in node.get('children', []):
        yield from walk(child)


def validate(data):
    nodes = list(walk(data['root']))
    ids = [n['id'] for n in nodes]
    assert len(ids) == len(set(ids)), 'Duplicate node IDs'
    by_id = {n['id']: n for n in nodes}
    for archive in (OLD, BACKUP):
        if not archive.exists():
            continue
        originals = list(walk(read(archive)['root']))
        assert {n['id'] for n in originals} <= set(ids), f'Original node lost: {archive.name}'
        if archive == BACKUP:
            for old in originals:
                new = by_id[old['id']]
                assert old.get('task') == new.get('task'), f'Original task changed: {old["topic"]}'
                for link in [old.get('hyperlink', ''), *old.get('hyperlinks', [])]:
                    if link and not link.startswith(BASE):
                        assert link in [new.get('hyperlink'), *new.get('hyperlinks', [])], 'External link lost'
                note = old.get('note', '')
                if note and not generated_note(note):
                    assert note in new.get('note', ''), f'User note lost: {old["topic"]}'
    links = 0
    for node in nodes:
        for url in [node.get('hyperlink', ''), *node.get('hyperlinks', [])]:
            if url.startswith(BASE):
                target = ROOT / unquote(url[len(BASE):].split('#')[0])
                assert target.is_file(), f'Missing Detail-Link: {url}'
                links += 1
    assert len(data['root']['children']) == 4, 'Expected four reading groups'
    print(f'MINDMAP_ORGANIZED_OK nodes={len(nodes)} links={links} originalIDs/tasks/notes/externalLinks=preserved')


def generated_note(note):
    return (note.startswith(('## 현재 플레이 기획\n', '## 후속 구상·현재 Proto 제외\n',
                             '## 제작/검토 참고·플레이 규칙 아님\n'))
            and '상세 행동·결과·영향은 Detail → Links' in note
            and note.endswith('마인드맵 보존본에 남겼습니다.'))


def organize():
    data = read(MAP)
    if data.get('meta', {}).get('organizationVersion') == VERSION:
        validate(data)
        return
    if BACKUP.exists():
        assert BACKUP.read_bytes() == MAP.read_bytes(), 'Existing backup differs; preserve edits before reorganizing'
    else:
        shutil.copyfile(MAP, BACKUP)
    backup_hash = hashlib.sha256(BACKUP.read_bytes()).hexdigest()
    root = data['root']
    now = int(time.time() * 1000)

    def find(topic):
        matches = [n for n in walk(root) if n['topic'] == topic]
        assert len(matches) == 1, f'Expected one topic: {topic}'
        return matches[0]

    def new(topic, children=None):
        return dict(id=str(uuid.uuid5(uuid.NAMESPACE_URL, 'project-b/mindmap/2026-11-02/' + topic)),
                    topic=topic, children=children or [], createdAt=now, modifiedAt=now, collapsed=True)

    def move(node, parent):
        for owner in list(walk(root)):
            owner['children'] = [c for c in owner.get('children', []) if c['id'] != node['id']]
        parent.setdefault('children', []).append(node)

    future = find('후속 구상 (현재 Proto 제외)')
    reference = find('제작·검토 참고 (플레이 기획과 분리)')
    current = new('현재 플레이 기획')
    current['collapsed'] = False
    current['children'] = [n for n in root['children'] if n not in (future, reference)]
    proposals = new('추가 제안 (2026-11-02·미채택)')
    future['topic'] = '기존 아이디어·후속 구상'
    root['children'] = [current, future, proposals, reference]

    # Keep old ideas, while moving the implemented durability and repair under vehicle care.
    damage = find('내구도?')
    damage['topic'] = '충돌 내구도·수리·견인 (구현)'
    move(damage, find('차량 관리'))
    paid_repair = find('수리비 지불')
    paid_repair['topic'] = '수리비·시간 지불 (현재 구현)'
    move(paid_repair, damage)
    damage['children'] += [new('충돌 강도별 손상·D0 운행/신규 승차 불가'),
                           new('D0 이전 조작 성능 유지·지원 시설 수리/안전 견인'),
                           new('보험·부품별 파손·성능 저하는 후속 구상')]

    passengers = find('승객&보행자')
    types = new('승객 유형 (일반 + 특수11유형·34변주)')
    passengers['children'].insert(0, types)
    drunk = find('취객')
    move(drunk, types)
    drunk['children'] += [new('취객은 진상의 하위 분류가 아닌 독립 승객 유형'),
                         new('배정30%·미등장 보완·하루 최대2명·일반 승객 휴식 구간'),
                         new('구토 전 예고·구토 후 즉시 호출 잠금·세차로 해제')]
    for topic in ('일반 승객 (유형 사건 없이 기존 요청)', '잠자는 승객', '승차감 민감형',
                  '재촉·불만형', '경로 확인형', '용무·경유형 (별도 용무 대기는 후속)',
                  '짐 많은형', '오염 가능 짐형', '음식·음료 소지형', '정밀 승하차형', '대화·반복 질문형'):
        types['children'].append(new(topic))
    find('진상 승객')['topic'] = '재촉·간섭 응대 구상 (현재 승객 유형 참고)'
    find('승객 대화')['children'] += [new('현재 후속은 이동 시간·응답·운전M 기준·개별 상황 반응은 추가 제안'),
                                   new('선택의 만족도·청결·시간 효과 표시·정당한 보류 중립')]

    mode = new('실행·모드 선택 (단일 EungyeTaxi.exe)', [
        new('은계 싱글: 3일 캠페인·기존 저장 유지'),
        new('자체 도시 PVP: 나1+봇3·공용8호출·경기시간 직접 입력'),
        new('모드별 세션/저장 분리·기존 원격4유저 수용은 별도'),
        new('PVP 피로·포만 개편은 후보안·현재 수치 변경 없음')])
    current['children'].insert(0, mode)
    find('PVP 모드')['topic'] = '원격 사용자 PVP 원안 (현재 나1+봇3과 구분)'
    find('PVP 모드 승패에 영향')['topic'] = '운전 점수의 원격 PVP 영향 원안 (현재 모드 규칙과 구분)'
    find('날씨')['topic'] = '기본 환경 (현재 맑음)'

    # Group existing future ideas; no old node is deleted.
    buckets = [new('승객·예약 확장'), new('차내 장치·차량·용품'),
               new('생활·금융·보험'), new('날씨·교통·사고·법규'), new('원격 모드 구상')]
    future_nodes = list(future['children'])
    future['children'] = buckets
    for node in future_nodes:
        topic = node['topic'].strip()
        if topic.startswith('단골'):
            bucket = 0
        elif 'PVP' in topic:
            bucket = 4
        elif topic in ('화장실', '너무 배부름', '대출 및 카드 대금 처리 방식', '보험 시스템',
                       '보험료 할인', '보행자 사고 패널티 (보험, 치료비 등 지불됨)'):
            bucket = 2
        elif topic in ('이벤트 요소', '비 / 눈', '운전 험하게 하는 차량', '버스 같은 차량 추가', '추가 아이디어'):
            bucket = 3
        else:
            bucket = 1
        buckets[bucket]['children'].append(node)

    candidates = [
        ('실제 상황 반응형 승객', '기존 승객 대화 확장', '추천1'),
        ('다음 운송·시설 이용의 상태 예측', '이번 신규 제안', '추천1'),
        ('안전 대체 승하차', '기존 후속 구체화·안전 장소 쌍 선행', '추천1·지도 선행'),
        ('경유 중 용무 대기', '기존 경유 확장·대기/요금 계약 추가', '추천2'),
        ('시간대별 수요·교통 흐름', '기존 지역/수요 확장', '추천2'),
        ('시설 혼잡·대안 비교', '이번 신규 제안', '추천2'),
        ('실제 차내 장치 응대', '기존 음악·에어컨·창문 아이디어', '추천2'),
        ('야간·날씨 확장', '비/눈은 기존·야간 운행 환경은 신규 제안', '추천3'),
        ('단골·개인 예약 확장', '기존 예약 아이디어', '추천3')]
    for topic, origin, priority in candidates:
        node = new(f'{priority} · {topic} [{origin}]')
        node['note'] = f'출처: {origin}\n검토 우선순위이며 채택·구현 지시가 아닙니다. 기존 가지는 보존하고 구체화 제안만 별도 관리합니다.'
        proposals['children'].append(node)

    docs = {'맵': '05-interface-and-navigation.md', '운전자': '03-life-and-economy.md',
            '경제': '03-life-and-economy.md', '차량': '03-life-and-economy.md',
            '주행 & 운송': '01-driving-and-traffic.md', '운전 점수': '01-driving-and-traffic.md',
            '승객&보행자': '02-passengers-and-fares.md', '캠페인': '04-campaign-and-feedback.md',
            'UI/UX': '05-interface-and-navigation.md'}

    def refresh(node, state, doc, depth=0):
        topic = node['topic']
        if node is current:
            state, doc = '현재 플레이 기획', 'docs/content-design/README.md'
        elif node is future:
            state, doc = '기존 아이디어·후속 구상 (미구현/추가 결정)', 'docs/future-design/README.md'
        elif node is proposals:
            state, doc = '추가 제안 (미채택·출처 구분)', REVIEW
        elif node is reference:
            state, doc = '제작·검토 참고 (플레이 규칙과 분리)', 'docs/technical-reference/README.md'
        if state == '현재 플레이 기획':
            if topic in docs:
                doc = 'docs/content-design/' + docs[topic]
            elif node is mode:
                doc = 'docs/content-design/pvp/27-flow-unified-mode.md'
            elif topic in ('주요 시설', '지역') or node is damage:
                doc = 'docs/content-design/03-life-and-economy.md'
            elif topic in ('운임 요금 규칙', '안전 승차·하차 판정', '운송 제한 시간'):
                doc = 'docs/content-design/02-passengers-and-fares.md'
            elif topic in ('내비게이션·경로 선택', '운송 후 생활 목적지 추천'):
                doc = 'docs/content-design/05-interface-and-navigation.md'
        old_note = node.get('note', '')
        if generated_note(old_note):
            old_note = ''
        note = f'## {state}\n\n{topic}\n\n상세 규칙은 Detail → Links의 담당 문서를 따릅니다. 기존 작업 체크/진척도는 보존했으며 구현 완료 판정은 개발 체크리스트를 확인합니다.'
        if old_note:
            note += '\n\n### 보존 메모\n\n' + old_note
        node['note'] = note
        external = [url for url in [node.get('hyperlink', ''), *node.get('hyperlinks', [])]
                    if url and not url.startswith(BASE)]
        node['hyperlink'], node['hyperlinks'] = BASE + doc, list(dict.fromkeys(external))
        node['tags'] = list(dict.fromkeys([t for t in node.get('tags', []) if t not in ('Proto', '후속/참고')] + [state]))
        node['modifiedAt'] = now
        if depth in (1, 2):
            node['collapsed'] = node is not current
        for child in node.get('children', []):
            refresh(child, state, doc, depth + 1)

    refresh(root, '기획 읽기: 현재 / 기존 후속 / 추가 제안 / 제작 참고', 'docs/content-design/README.md')
    data.setdefault('meta', {}).update(organizationVersion=VERSION, updatedAt=now)
    validate(data)
    assert hashlib.sha256(BACKUP.read_bytes()).hexdigest() == backup_hash
    MAP.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8', newline='\n')
    lines = ['# 택시 시뮬레이션 마인드맵 목차', '',
             '[마인드맵 파일](택시%20시뮬레이션.mmst) · [정리 기준](06-mindmap-review.md)', '',
             '현재 기획·기존 후속·추가 제안·제작 참고를 구분했다. 기존 아이디어와 노드 ID/작업 체크는 보존했다. 상세는 각 노드의 Detail → Links를 따른다.', '']

    def outline(node, depth):
        lines.append('  ' * depth + '- ' + node['topic'].strip())
        if depth < 3:
            for child in node.get('children', []):
                outline(child, depth + 1)

    for node in root['children']:
        outline(node, 0)
    OUTLINE.write_text('\n'.join(lines) + '\n', encoding='utf-8', newline='\n')
    print(f'BACKUP_SHA256 {backup_hash}')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--validate-only', action='store_true')
    args = parser.parse_args()
    validate(read(MAP)) if args.validate_only else organize()
