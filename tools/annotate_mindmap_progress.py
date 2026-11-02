"""Apply the user's 50/25/0 stages; do not turn recommendation into implementation."""
import argparse
import copy
import json
import subprocess
import time
import uuid
from collections import Counter
from pathlib import Path

from organize_content_mindmap import BASE, MAP, OUTLINE, ROOT, read, walk

VERSION = '2026-11-02-progress'
BACKUP = ROOT / 'docs/design-history/content-design/2026-11-02-mindmap-before-progress.mmst'
PRIORITIES = 'docs/future-design/2026-11-02-simulation-priorities.md'
MARKER = '<!-- mindmap-progress:end -->\n'
IMPLEMENTED_P1P2 = {
    '수동 주행 회전 후 방향지시등 자동 해제': ('TaxiSignals.cs', '실제 회전35도·직진 조향1초, 정차/비상등/보조 운전 및 저장 경계'),
    '서행 의무 자동 감점 기준': ('DrivingLawSlowTurns.cs', '명시한 게임 관찰 구역만10km/h·여유2·0.75초. 법정 고정속도 주장 없음, 필요 제동 중립'),
    '운전 험하게 하는 차량': ('TrafficCar.cs', '차분/보통/성급 성향. 적색·보행·차량 점유 안전 경계 유지'),
    '추천1 · 실제 상황 반응형 승객': ('PassengerJobsActivities.cs', '실제 신호 대기·충격, 유형별 반응·대화 예산·재수면·음식 청결 사건'),
    '추천1 · 다음 운송·시설 이용의 상태 예측': ('OperationForecastAdapter.cs', '기존 모델 복사·경로/시간 기반 예측. 원본 상태/돈/운임 불변'),
    '추천1·지도 선행 · 안전 대체 승하차': ('StopOccupancyAlternatives.cs', '작성된 게임용 포켓과 보행 경로만 사용. 실제 도로 정차 허용 근거와 구분'),
    '추천2 · 경유 중 용무 대기': ('PassengerJobsActivities.cs', '합의한20초 무료 용무 대기, 안전 정차에서 진행·동일 운송 유지'),
    '추천2 · 시간대별 수요·교통 흐름': ('SimulationDemandClock.cs', '아침/낮/저녁 방향 가중, 수락 호출 유지·초과 영업 분산'),
    '추천2 · 시설 혼잡·대안 비교': ('ServiceStopsPlanning.cs', '공유 시설 물리 진입 순서·명시적 취소·이동/대기/사용시간과 견적 비교'),
    '추천2 · PVP 봇 식사·서비스 손익 판단': ('PvpSimulationBotServices.cs', '플레이어와 같은 비용/시간/효과, 방문당 한 작업·긴급 상태에서 호출 금지'),
}

def apply_p1p2(data):
    def update(node):
        records=[]
        for child in node.get('children', []):
            records.extend(update(child))
        topic=node['topic']
        match=next((value for prefix,value in IMPLEMENTED_P1P2.items() if topic.startswith(prefix)),None)
        if not node.get('children') and match:
            source,reason=match
            node.setdefault('task', {})['progress']=.5
            prefix,original=node.get('note','').split(MARKER,1)
            node['note']='<!-- mindmap-progress:start -->\n## 사용자 지정 단계: 50% 구현\n\n'+reason+'\n\n구현 근거: unity/EungyeTaxi/Assets/Scripts/'+source+'\n50%는 구현 단계이며 전체 수동 검증/출시 완료율이 아닙니다.\n'+MARKER+original
            node['tags']=[t for t in node.get('tags',[]) if not t.startswith('단계:')]+['단계:50% 구현']
        if not node.get('children') and '실제 음악 장치 조작' in topic:
            node['tags']=[t for t in node.get('tags',[]) if t!='보류']+['보류']
        stage=node.get('task',{}).get('progress')
        if stage is not None:records.append(stage)
        if node.get('children') and records:
            counts=Counter(records);label=f'하위 집계: 50% {counts[.5]} / 25% {counts[.25]} / 0% {counts[0]}'
            node['tags']=[t for t in node.get('tags',[]) if not t.startswith('단계:')]+['단계:'+label]
            prefix,original=node['note'].split(MARKER,1)
            prefix=prefix.split('\n',2);node['note']=prefix[0]+'\n## 사용자 지정 단계: '+label+'\n'+prefix[2]+MARKER+original
        return records
    update(data['root'])
    return data
DOCS = {
    '맵': ('05-interface-and-navigation.md', 'RoadMapEditor.cs'),
    '운전자': ('03-life-and-economy.md', 'DriverCondition.cs'),
    '경제': ('03-life-and-economy.md', 'EconomyLedger.cs'),
    '차량': ('03-life-and-economy.md', 'VehicleCare.cs'),
    '주행 & 운송': ('01-driving-and-traffic.md', 'TrafficSystem.cs'),
    '타이틀': ('05-interface-and-navigation.md', 'UnifiedModeBootstrap.cs'),
    'UI/UX': ('05-interface-and-navigation.md', 'DriveHUD.cs'),
    '캠페인': ('04-campaign-and-feedback.md', 'BusinessProgress.cs'),
    '운전 점수': ('01-driving-and-traffic.md', 'DrivingLaw.cs'),
    '승객&보행자': ('02-passengers-and-fares.md', 'PassengerBehavior.cs'),
}


def new(topic):
    return dict(id=str(uuid.uuid5(uuid.NAMESPACE_URL, VERSION + '/' + topic)), topic=topic, children=[])


def priority(path):
    if any(x in path for x in ('버스 정류소', '회전 교차로', '설정')):
        return 'P3'
    if any(x in path for x in ('추천1', '방향지시등', '서행', '비보호')):
        return 'P1'
    if any(x in path for x in ('추천2', '차량 내부 조작', '운전 험하게', '봇 식사')):
        return 'P2'
    if any(x in path for x in ('추천3', '비 / 눈', '단골', '예약', '추가 아이디어',
                               '버스 같은', '원격', 'PVP', '주유비', '실제 주유')):
        return 'P3'
    return 'P4'


def annotate(data, stamp):
    if data.get('meta', {}).get('progressReviewVersion') == VERSION:
        def refresh(n, parents):
            path = ' > '.join(parents + [n['topic']])
            if n.get('task', {}).get('progress') in (0, .25):
                old = next((t[3:] for t in n['tags'] if t.startswith('우선:')), '')
                rank = priority(path)
                if old and rank != old:
                    n['tags'] = [t.replace(' · ' + old, ' · ' + rank) for t in n['tags'] if not t.startswith('우선:')] + ['우선:' + rank]
                    prefix, original_note = n['note'].split(MARKER, 1)
                    n['note'] = prefix.replace(' · ' + old + '\n', ' · ' + rank + '\n') + MARKER + original_note
            for child in n.get('children', []):
                refresh(child, parents + [n['topic']])
        refresh(data['root'], [])
        return apply_p1p2(data)
    root = data['root']

    def split(topic, title, topics):
        matches = [n for n in walk(root) if n['topic'] == topic]
        assert len(matches) == 1, topic
        node = matches[0]
        node['topic'] = title
        assert not node.get('children'), 'Only split a compound leaf'
        node['children'] = [new(t) for t in topics]

    split('모드별 세션/저장 분리·기존 원격4유저 수용은 별도', '모드별 저장과 원격 대전의 경계',
          ['모드별 세션/저장 분리 (현재 구현)', '기존 차량 물리의 원격4유저 통합·수용 (미구현)'])
    split('우회전/좌회전 깜빡이 후 진입 후 자동으로 꺼지기', '방향지시등 조작과 회전 후 해제',
          ['Q/E 방향지시등 조작·보조 운전 경계 처리 (구현)', '수동 주행 회전 후 방향지시등 자동 해제 (기획 필요)'])
    split('서행 판단 안내·자동 감점 기준 미확정', '서행 안내와 자동 감점의 경계',
          ['서행/우회전 판단 안내 (구현)', '서행 의무 자동 감점 기준·관찰 구역 확정 (기획 필요)'])
    split('비보호 좌회전 신호·대향차 판단', '비보호 판단 로직과 현장 근거 배치',
          ['비보호 좌회전 신호·대향차 판단 로직/표시 (구현)', '현장 표지 확인 접근의 실제 비보호 지도 반영 (기획·근거 필요)'])
    proposals = next(n for n in root['children'] if n['topic'].startswith('추가 제안'))
    proposals['children'].append(new('추천2 · PVP 봇 식사·서비스 손익 판단 [기존 생활 기능 확장]'))

    for node in walk(root):
        # Never use obsolete completion percentages as implementation evidence.
        if isinstance(node.get('task'), dict):
            node['task'].pop('progress', None)
            if not node['task']:
                node.pop('task')

    def visit(node, ancestors, doc='docs/content-design/README.md', source=''):
        topic = node['topic']
        path = ' > '.join(ancestors + [topic])
        if topic in DOCS:
            doc, source = DOCS[topic]
            doc = 'docs/content-design/' + doc
            source = 'unity/EungyeTaxi/Assets/Scripts/' + source
        if topic.startswith('실행·모드 선택'):
            doc = 'docs/content-design/pvp/27-flow-unified-mode.md'
            source = 'unity/EungyeTaxi/Assets/Scripts/UnifiedModeBootstrap.cs'
        children = node.get('children', [])
        records = []
        for child in children:
            records.extend(visit(child, ancestors + [topic], doc, source))
        evidence, why, stage, rank = doc, '', None, None
        reference = (any(x in path for x in ('제작·검토 참고', '게임의 목표')) or topic in (
            'PVP 피로·포만 개편은 후보안·현재 수치 변경 없음',
            '보험·부품별 파손·성능 저하는 후속 구상',
            '취객은 진상의 하위 분류가 아닌 독립 승객 유형',
            '재촉·간섭 응대 구상 (현재 승객 유형 참고)',
            '운송·생활 상태는 담당 기획 참조'))
        if not children and not reference:
            future = '기존 아이디어·후속 구상' in path or '추가 제안' in path
            gap = topic in ('버스 정류소', '회전 교차로', '설정') or any(x in topic for x in (
                '원격4유저 통합', '기획 필요', '기획·근거 필요'))
            if future or gap:
                stage, rank, evidence = 0, priority(path), PRIORITIES
                why = '아이디어/검토는 있으나 실제 구현을 위한 조건·효과·예외 또는 지도/통합 계약의 추가 기획이 필요함'
                if '상태 예측' in topic:
                    stage, rank = .25, 'P1'
                    why = '이번 구현용 표시 계약으로 기획 정리: 기존 모델 복사·ETA 없음 처리·마감/모드 경계·갱신·부작용 없음. 구현 전/미채택'
                if topic == '회전 교차로':
                    why = '기존 도로 연결과 별도로 회전교차로 전용 지도/양보 규칙의 현재 구현 근거·범위 확인 필요'
                if topic == '버스 정류소':
                    why = '택시 승하차 후보와 구분한 버스 정류소 전용 동작/배치 범위의 기획·구현 근거 확인 필요'
                if topic == '설정':
                    why = '현재 모드/조작 전환과 구분한 별도 설정 메뉴의 항목·저장·초기화 범위 기획 필요'
            else:
                stage = .5
                why = '현재 기획과 기존 구현/작업 이력에 존재하는 해당 동작. 새 확장·새 UI 스타일·전체 검증 완료를 포함하지 않음'
                if '음악' in topic:
                    why += ' (실제 음악 조작은 제외)'
            node.setdefault('task', {})['progress'] = stage
            records = [dict(id=node['id'], topic=topic, path=path, progress=stage,
                            priority=rank, evidence=evidence, source=source if stage == .5 else '', reason=why)]
            label = {0: '0% 기획 필요', .25: '25% 기획 완료·미구현', .5: '50% 구현'}[stage]
            if rank:
                label += ' · ' + rank
        elif children and records:
            counts = Counter(r['progress'] for r in records)
            label = f'하위 집계: 50% {counts[.5]} / 25% {counts[.25]} / 0% {counts[0]}'
            why = '분류/부모 가지는 평균이나 완료율로 표시하지 않음. 실제 작업 체크는 하위 기능 항목에 적용'
        else:
            label, why = '설명·참조 (단계 집계 제외)', '아이디어 출처/설명 또는 제작 참고이며 별도 구현 기능으로 중복 계산하지 않음'
        old_note = node.get('note', '')
        if MARKER in old_note:
            old_note = old_note.split(MARKER, 1)[1]
        node['note'] = (f'<!-- mindmap-progress:start -->\n## 사용자 지정 단계: {label}\n\n{why}\n\n'
                        f'판정/우선순위: {BASE}{PRIORITIES}\n'
                        f'기획 근거: {BASE}{evidence}\n' + (f'구현 근거: {source}\n' if stage == .5 and source else '') +
                        '50%는 구현 단계를 뜻하며 100% 검증/출시 완료가 아닙니다. 기획 완료와 채택 승인은 별개입니다.\n'
                        + MARKER + old_note)
        node.setdefault('hyperlink', BASE + doc)
        node.setdefault('hyperlinks', [])
        node['tags'] = [t for t in node.get('tags', []) if not t.startswith(('단계:', '우선:'))]
        node['tags'].append('단계:' + label)
        if rank:
            node['tags'].append('우선:' + rank)
            node['hyperlinks'] = list(dict.fromkeys([*node.get('hyperlinks', []), BASE + PRIORITIES]))
        node['modifiedAt'] = stamp
        return records

    records = visit(root, [])
    data.setdefault('meta', {}).update(progressReviewVersion=VERSION, updatedAt=stamp)
    return apply_p1p2(data)


def outline(data):
    lines = ['# 택시 시뮬레이션 마인드맵 목차·진척도', '',
             '[마인드맵 원본](택시%20시뮬레이션.mmst) · [미구현 우선순위](../future-design/2026-11-02-simulation-priorities.md)', '',
             '**50% 구현 / 25% 기획 완료·미구현 / 0% 기획 필요**. 부모는 하위 집계, 설명/참조는 집계 제외. 검증 완료율이 아니다.', '']
    def emit(n, depth):
        tag = next(t[3:] for t in n['tags'] if t.startswith('단계:'))
        lines.append('  ' * depth + '- ' + n['topic'].strip() + ' — ' + tag)
        for child in n.get('children', []):
            emit(child, depth + 1)
    for n in data['root']['children']:
        emit(n, 0)
    return '\n'.join(lines) + '\n'


def validate(data, preserve_folds=False):
    old = read(BACKUP)
    original = {n['id']: n for n in walk(old['root'])}
    current = {n['id']: n for n in walk(data['root'])}
    assert len(current) == len(list(walk(data['root'])))
    assert set(original) <= set(current), 'Current user nodes lost'
    counts = Counter()
    for n in current.values():
        stage = n.get('task', {}).get('progress')
        if stage is not None:
            assert stage in (0, .25, .5) and not n.get('children'), 'Only functional leaves have stage ticks'
            counts[stage] += 1
        if n['id'] in original:
            before = original[n['id']]
            assert before.get('note', '') in n.get('note', ''), 'Original note lost'
            if preserve_folds:
                assert before.get('collapsed') == n.get('collapsed'), 'User folded state changed'
    print(f'MINDMAP_PROGRESS_OK nodes={len(current)} implemented50={counts[.5]} planned25={counts[.25]} needsPlan0={counts[0]} currentUserIDs/notes=preserved')


def write(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8', newline='\n')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--validate-only', action='store_true')
    args = parser.parse_args()
    if args.validate_only:
        validate(read(MAP))
        return
    before = MAP.read_bytes()
    if not BACKUP.exists():
        BACKUP.write_bytes(before)
    working = json.loads(before.decode('utf-8-sig'))
    stamp = (working['meta']['updatedAt'] if working.get('meta', {}).get('progressReviewVersion') == VERSION
             else int(time.time() * 1000))
    result = annotate(copy.deepcopy(working), stamp)
    validate(result)
    # The working map may have newer user folds than the historical backup.
    before_folds={n['id']:n.get('collapsed') for n in walk(working['root'])}
    assert before_folds=={n['id']:n.get('collapsed') for n in walk(result['root'])}, 'Current folded state changed'
    # Build a separate staged candidate: pre-existing user deletions/folds stay unstaged.
    head = subprocess.run(['git', 'show', 'HEAD:docs/content-design/택시 시뮬레이션.mmst'], capture_output=True, check=True).stdout
    (ROOT / 'artifacts/mindmap-progress-head-base.mmst').write_bytes(head)
    staged = annotate(json.loads(head.decode('utf-8-sig')), stamp)
    assert MAP.read_bytes() == before, 'Concurrent map edit; retry'
    write(MAP, result)
    OUTLINE.write_text(outline(result), encoding='utf-8', newline='\n')
    write(ROOT / 'artifacts/mindmap-progress-index.mmst', staged)
    (ROOT / 'artifacts/mindmap-progress-outline-index.md').write_text(outline(staged), encoding='utf-8', newline='\n')


if __name__ == '__main__':
    main()
