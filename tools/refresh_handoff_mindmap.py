"""Refresh the November 3 handoff without restoring deleted user branches."""
import copy
import json
import uuid
from collections import Counter
from pathlib import Path

from organize_content_mindmap import ROOT, MAP, OUTLINE, BASE, read, walk, validate
from annotate_mindmap_progress import MARKER, outline

START = '<!-- handoff-2026-11-03:start -->'
END = '<!-- handoff-2026-11-03:end -->'


def note(node, text, doc):
    value = node.get('note', '')
    if START in value:
        before, tail = value.split(START, 1)
        _, after = tail.split(END, 1)
        value = before.rstrip() + after
    node['note'] = value.rstrip() + '\n\n' + START + '\n## 2026-11-03 현재 기준\n\n' + text + '\n\n' + BASE + doc + '\n' + END + '\n'
    links = node.setdefault('hyperlinks', [])
    if BASE + doc not in links:
        links.append(BASE + doc)


def refresh(data):
    nodes = {n['id']: n for n in walk(data['root'])}
    modes = nodes['15ed4ff4-7d78-51d5-86d7-a5bf8d3424a0']
    ui = nodes['001a8e26-f1a3-488e-8a4c-031d84844ba9']
    drive = nodes['cdf84a46-1b3b-4c36-9f73-866e25d4eb84']
    changes = [
        (modes, 'PvP 닉네임 입력·호스트 공유·대기실/순위 표시', 'pvp/30-native-lan.md', '최대12자, 빈 입력 차단·로컬 설정 저장, 호스트 room 메시지로 이름 배포. 기존 예측 통신v2 유지. 캠페인 저장과 분리.'),
        (modes, 'PvP 참가자 자기 차 예측·서버 보정·바퀴 동기화', 'pvp/30-native-lan.md', '자기 차 WheelCollider 로컬 예측/번호 입력/서버 보정, 다른 차량·NPC 보간. 승객·법규·정산은 서버 권한. 다른PC/장시간의 사람 체감은 검증 잔여.'),
        (modes, '10분 이하 PvP: 단거리 신규 호출·목록 보충', 'pvp/30-native-lan.md', '8공유 호출+3길가 유지. 각 승차지의 실제 연결된80m 이상 도착지 중 최근접3곳 우선, 휴지30초. 수락된 약정 유지. 10분 운송 횟수/경제 체감 검증은 별도.'),
        (ui, '승객 요청·선택 버튼·효과 분리와 운행 중 답변', '07-passenger-dialogue.md', '승객 말풍선/기사 선택/효과 행, 효과 없음 표시. 구체적인 유형·상황별 대사, Y/I/O/U·클릭 응답. 실제 적재/정리/재고정/용무는 안전 정차 조건 유지.'),
        (ui, '네비: 승차·경유·하차 잔여 거리와 신호 변경 시간', '05-interface-and-navigation.md', '다음 회전 거리와 실제 경로의 목표 잔여 거리를 구분한다. 신호 요약은 변경까지N초, 조건부 시간은 미정. 상충 신호 규칙을 UI에서 완화하지 않음.'),
        (drive, '기본 신호: 양방향 직진·방향별 보호 좌회전', '01-driving-and-traffic.md', '기본 보호 신호만 직진10/황색2/정리2, 허용 좌회전 방향별4/2/2, 보행10/정리2. 없는 좌회전 단계 생략. 최장72초/좌회전 없는 사거리40초. 편집 신호·교육/실제 비보호 권한 보존.'),
        (drive, '방향키·패드·LAN 공통 조향 입력 반응 개선', '01-driving-and-traffic.md', '입력 램프9~6/s, 0→최대 약0.11~0.17초. 저·중속 수동 각도 가산, 최대52도·30km/h 이상 기존 각도/제동/충돌 유지. 사람 조작감은 별도 수용.'),
    ]
    for parent, topic, doc, detail in changes:
        key = str(uuid.uuid5(uuid.NAMESPACE_URL, 'project-b/handoff-2026-11-03/' + topic))
        node = nodes.get(key)
        if node is None:
            node = dict(id=key, topic=topic, children=[], collapsed=True,
                        task={'progress': .5}, tags=['단계:50% 구현'],
                        hyperlink=BASE + 'docs/content-design/' + doc,
                        note='<!-- mindmap-progress:start -->\n## 사용자 지정 단계: 50% 구현\n\n실제 구현 단계이며 전체 QA 완료율이 아닙니다.\n' + MARKER)
            parent['children'].append(node)
        note(node, detail, 'docs/content-design/' + doc)
    modes_node = nodes['6aa84373-b43c-5b9b-9520-f26bb8353940']
    modes_node['topic'] = '자체 도시 PvP(A2): 봇3 또는 LAN 최대4명·경기시간 입력'
    note(modes_node, '현재 배포는 A2. A3는 적용 이력 후 사용자 지시로 복귀한 상태이며 현재 맵이 아니다. 단일 택시 시뮬레이션 실행본에서 은계 싱글/PvP 선택.', 'docs/CURRENT-STATE.md')
    condition = nodes['443801f6-31f9-5f47-bfc9-ebbe96b88a08']
    condition['topic'] = 'PvP 컨디션 적용값: 피로1.25배·식사15초·무료휴식45초'
    note(condition, '후보안이 아니라 실제 적용됨. 간편식1,000원/15초/+60, 무료휴식45초/피로−40. 싱글과 별도 프로필. 기존 설명은 이전 검토 당시 기록이다.', 'docs/content-design/03-life-and-economy.md#pvp-driver-condition')
    note(nodes['2d37aadc-4be8-5135-8718-3a8382bb0687'], '현재 LAN/네이티브 예측·호스트 권한 구현. 50%는 구현 단계이며 다른PC4인/장시간 수용 완료가 아니다.', 'docs/content-design/pvp/30-native-lan.md')
    note(nodes['b2974b8c-f1f9-553b-8d2c-733f408dfcec'], '선택한 호출/시설 비교 예측은 구현. 운송 중 상시 HUD의 예상 도착 상태 카드는 후속 UI 지시로 숨김. 계산 모델과 현재 화면 노출을 구분한다.', 'docs/CURRENT-STATE.md')
    proposals = nodes['45aa7402-c9e7-5611-b743-fb6d212708ae']
    proposals['topic'] = '확장 아이디어: 구현된 P1/P2와 보류·후속 제안'
    note(proposals, 'P1/P2 다수는 후속 승인으로 구현50%. 음악·에어컨·창문 실제 조작은0% 보류. 추천 문구는 최초 출처이며 현재 채택 여부는 개별 단계로 확인한다.', 'docs/future-design/2026-11-02-simulation-priorities.md')

    def counts(node):
        values = Counter()
        for child in node.get('children', []):
            values.update(counts(child))
        stage = node.get('task', {}).get('progress')
        if stage is not None:
            values[stage] += 1
        if node.get('children') and values:
            label = f'하위 집계: 50% {values[.5]} / 25% {values[.25]} / 0% {values[0]}'
            node['tags'] = [t for t in node.get('tags', []) if not t.startswith('단계:')] + ['단계:' + label]
            # Only the generated heading changes; the original note after MARKER stays intact.
            if MARKER in node.get('note', ''):
                prefix, original = node['note'].split(MARKER, 1)
                lines = prefix.splitlines()
                if len(lines) > 1:
                    lines[1] = '## 사용자 지정 단계: ' + label
                node['note'] = '\n'.join(lines) + '\n' + MARKER + original
        return values
    result = counts(data['root'])
    data.setdefault('meta', {})['handoffReviewDate'] = '2026-11-03'
    return result


def main():
    before = read(MAP)
    data = copy.deepcopy(before)
    result = refresh(data)
    old = {n['id']: n for n in walk(before['root'])}
    new = {n['id']: n for n in walk(data['root'])}
    for key, node in old.items():
        assert key in new
        assert node.get('collapsed') == new[key].get('collapsed')
        assert node.get('task') == new[key].get('task')
        suffix = node.get('note', '').split(MARKER, 1)[-1]
        assert suffix in new[key].get('note', ''), 'User note lost'
        assert [c['id'] for c in node.get('children', [])] == [c['id'] for c in new[key].get('children', []) if c['id'] in old]
    validate(data)
    candidate = copy.deepcopy(data)
    refresh(candidate)
    assert candidate == data, 'Refresh must be idempotent'
    MAP.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8', newline='\n')
    OUTLINE.write_text(outline(data), encoding='utf-8', newline='\n')
    print(f'HANDOFF_MAP_OK old={len(old)} new={len(new)} stages={dict(result)} folds/tasks/notes/order=preserved')


if __name__ == '__main__':
    main()
