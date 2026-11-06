"""Reconcile published content and planning gaps; preserve user tree data."""
import argparse
import collections
import copy
import hashlib
import json
from pathlib import Path
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[1]
MAP = ROOT / 'docs/content-design/택시 시뮬레이션.mmst'
OUTLINE = ROOT / 'docs/content-design/택시 시뮬레이션-목차.md'
BACKUP = ROOT / 'docs/design-history/content-design/2026-11-07-mindmap-before-status-review.mmst'
GAPS = ROOT / 'docs/future-design/2026-11-07-planning-gaps-and-weather.md'
BASE = 'https://github.com/herothgf-cell/project-b/blob/main/'
START = '<!-- status-review-2026-11-07:start -->'
END = '<!-- status-review-2026-11-07:end -->'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def walk(node, parents=()):
    yield node, parents
    for child in node.get('children', []):
        yield from walk(child, parents + (node['topic'],))


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def append_review(node, text, doc):
    original = node.get('note', '')
    if START in original:
        before, rest = original.split(START, 1)
        _, after = rest.split(END, 1)
        original = before + after
    node['note'] = original + '\n\n' + START + '\n## 2026-11-07 현재 판정\n\n' + text + '\n' + BASE + doc + '\n' + END
    links = node.setdefault('hyperlinks', [])
    if BASE + doc not in links:
        links.append(BASE + doc)


def counts(node):
    found = collections.Counter()
    for child in node.get('children', []):
        found.update(counts(child))
    value = node.get('task', {}).get('progress')
    if value is not None:
        assert not node.get('children'), 'Functional stages belong to leaves'
        assert value in (0, .25, .5)
        found[value] += 1
    return found


def outline(data):
    lines = ['# 택시 시뮬레이션 마인드맵 목차·진척도', '',
             '[마인드맵 원본](택시%20시뮬레이션.mmst) · [관리 기준](06-mindmap-review.md) · [미정/미구현 전체 정리](../future-design/2026-11-07-planning-gaps-and-weather.md)', '',
             '**50% 구현 / 25% 기획 완료·미구현 / 0% 기획 필요**. 설명·중복 참조·대체된 이전 원안은 집계 제외. 제품 완성도나 검증 완료율이 아니다.', '',
             '날씨(기상)는 상세 기획/구현 없음. 잠든 승객을 깨우는 대기·야간 피로 회복과 구분한다.', '']

    def emit(node, depth):
        stage = next(t[3:] for t in node.get('tags', []) if t.startswith('단계:'))
        lines.append('  ' * depth + '- ' + node['topic'].strip() + ' — ' + stage)
        for child in node.get('children', []):
            emit(child, depth + 1)
    for child in data['root']['children']:
        emit(child, 0)
    return '\n'.join(lines) + '\n'


def refresh(data):
    nodes = {n['id']: n for n, _ in walk(data['root'])}

    def update(key, topic, detail, doc, progress='keep'):
        n = nodes[key]
        n['topic'] = topic
        if progress != 'keep':
            task = n.setdefault('task', {})
            if progress is None:
                task.pop('progress', None)
                if not task:
                    n.pop('task', None)
                stage = '설명·참조 (단계 집계 제외)'
            else:
                task['progress'] = progress
                stage = {.5: '50% 구현', .25: '25% 기획 완료·미구현', 0: '0% 기획 필요'}[progress]
            n['tags'] = [t for t in n.get('tags', []) if not t.startswith('단계:')] + ['단계:' + stage]
        append_review(n, detail, doc)

    def add(parent, key, topic, detail, doc, stage):
        if key not in nodes:
            node = dict(id=key, topic=topic, children=[], collapsed=True, tags=[], hyperlink=BASE + doc)
            nodes[parent]['children'].append(node)
            nodes[key] = node
        update(key, topic, detail, doc, stage)

    update('aa01a7cd-4f01-5bd4-84c5-3fde4c9fa772',
           '승객 프리셋·특성·행동 (기존11행동·34변주 재사용)',
           '기존 단일 유형 분류는 행동 자료의 출처다. 현재 프리셋 조합·개별 대사/확률/효과·다양화1차가 구현됐다. 아래 기존 행동 노드와 신규 제작 기능을 구분한다.',
           'docs/content-design/15-passenger-presets-and-traits.md')
    update('23a411f2-4d96-560d-9d20-7db982c15fd1',
           '음주 특성은 공격성·불법 요구와 별개 (기존 취객 행동 재사용)',
           '현재 음주는 프리셋의 신체 특성이다. 취객이라는 단일 주 유형으로만 고정하는 과거 설명을 현재 조합 제약으로 읽지 않는다.',
           'docs/content-design/15-passenger-presets-and-traits.md')
    add('98b78122-12dd-496b-bf32-2c6ecdd736ab', 'review-20261107-composition',
        '승객 프리셋 제작·특성 조합·미지원 조합 차단',
        '기존 행동을 재사용하는 프리셋 생성/복제/사용/가중치·지원 특성 조합 구현. 수면+소지품 등 차단된 독립 행동은 미구현으로 별도 구분. 실제 검증은 연결 이력 참조.',
        'docs/work-history/2026-11-06-passenger-composition-implementation.md', .5)
    add('98b78122-12dd-496b-bf32-2c6ecdd736ab', 'review-20261107-overrides',
        '승객별 대사·선택 효과·확률·작업/오염 개별 설정',
        '공통 상속+개별 덮어쓰기/복원·현재 운송 snapshot·호스트 권한 구현. LANv13. 같은PC 자동 검사와 사람3일/다른PC 수용은 별개.',
        'docs/work-history/2026-11-07-passenger-overrides.md', .5)
    add('98b78122-12dd-496b-bf32-2c6ecdd736ab', 'review-20261107-roster',
        '승객 다양화1차: 총24명·23활성·신규12명',
        '기존 프리셋 보존·경로 확인 비활성, ShortRoad/RouteAdvice 기본 후보 제외. 구 저장/사용자 프로필은 강제 교체하지 않음. 기획17·소스6ef7d83/출하309e49e의 실제 이력 기준.',
        'docs/work-history/2026-11-07-passenger-roster-v1.md', .5)
    add('9837d539-e010-4f6f-a6cd-1077e8f63094', 'review-20261107-driving-settings',
        '개인 운전 설정5종: 조향·반응·복원·데드존·브레이크',
        '일시정지 설정의5종·적용/취소/복원·PC별 저장 구현. 일반 설정 전체 미구현이라는 예전 설명을 적용하지 않는다. H/봇/안전 제동 별개.',
        'docs/content-design/01-driving-and-traffic.md#개인-운전-설정', .5)
    update('3693287f-e2c9-4439-9dcd-8bb60215ed04',
           '일반 설정 추가 항목 (구현된 운전 설정5종 제외)',
           '운전 설정5종과 콘텐츠 관리창은 구현이다. 그 밖의 일반 설정 항목/저장 범위는 상세 기획이 필요한 추가안이며 새 필수 개발 지시가 아니다.',
           'docs/future-design/2026-11-07-planning-gaps-and-weather.md')
    update('73118cc4-0126-55b2-8725-3037c01c7f90',
           '승객 추가 상황·복수 행동 확장 (기존 신호/충격 반응 제외)',
           '기존 신호/충격 반응과 지원 특성 조합은 구현. 수면+음식/짐·대화+소지품 등의 독립 행동 동시 진행, 새 메스꺼움/지원 행동은 기획 필요. 모든 상황 반응이 미구현이라는 과거 문구를 정정한다.',
           'docs/content-design/15-passenger-presets-and-traits.md')
    add('01d05ca1-a7f4-469c-b412-eb3860acede9', 'review-20261107-request-observation',
        '승객 약속 위반·실패 관찰의 추가 범위',
        'Quiet의 약속 위반 관찰, 교통 사유와 경로 실패의 구분, 목적지/경유 실패 도달 구조는 기존 판정표의 제한. 새 판정 조건·예외·중복 방지 기획 필요. 현재 비활성 ShortRoad/RouteAdvice를 자동 복원하지 않는다.',
        'docs/content-tuning-request-outcomes.md', 0)
    # Existing future notes contain ambiguous names; clarify without choosing new rules.
    for key, topic, text, doc in [
        ('83fdeda2-af07-46e9-8f36-879afe12683a', '사고 직후 자동/일괄 수리 추가안 (현재 공업사 전량수리와 구분)', '원래 질문을 미정 추가안으로 보존. 현재 공업사 전량 수리/견인은 이미 구현이며 사고 즉시 자동 수리 정책을 승인한 것이 아니다.', 'docs/content-design/08-accident-and-workshop-proposal.md'),
        ('ce1634e5-64ae-44e6-91a2-da5451e6db22', '현장 렉카 차량·출동 연출 (현재 견인 서비스와 구분)', '실제 현장 출동 차량/애니메이션은 미기획·미구현. 현재 고장 견인과 중대 사고 공업사 이동은 별도 구현.', 'docs/content-design/08-accident-and-workshop-proposal.md'),
        ('000cc463-ccdd-4125-a92c-32c397c79b71', '사고 현장 통제·정체 발생 (일반 정체와 구분)', '도로 사고 현장을 장기 통제해 교통을 바꾸는 추가안. 현재 대기열/일반 정체/시설 점유를 미구현으로 되돌리지 않는다.', 'docs/content-design/08-accident-and-workshop-proposal.md'),
        ('09b15c28-894f-4aca-9723-84fd034ab55b', '과식에 따른 졸음 영향 (기존 피로 졸음과 별개)', '과식 효과 자체는 미기획. 기존 피로100의 눈감김/안전 정차가 구현된 사실과 구분하며 수치나 감점을 새로 정하지 않는다.', 'docs/content-design/03-life-and-economy.md'),
        ('82f03b64-3b53-4d42-94a0-540afc0e462c', '깜빡이 미사용·위협 운전 추가 판정 (기존 진로방해 평가 제외)', '현재 진로 변경 방해·끼어들기/필요 제동과 새 깜빡이/위협 판정 범위는 다르다. 새 관찰 사실·중복/예외·게임 효과는 기획 필요.', 'docs/content-design/01-driving-and-traffic.md'),
    ]:
        update(key, topic, text, doc)

    weather_doc = 'docs/future-design/2026-11-07-planning-gaps-and-weather.md#날씨기상와-낮밤'
    update('49e370b7-882f-44ff-a527-0826df8d3fb1', '날씨·낮밤 (상세 기획/구현 없음)',
           '기존 비/눈·제동 영향·와이퍼는 아이디어뿐이다. 기본 실행은 고정 조명, 가상 시각/수요·야간 피로 회복은 실제 날씨/낮밤 표현 구현이 아니다. 아래0%는 결정할 항목이며 채택된 날씨 설계가 아니다.', weather_doc)
    update('e96c40dd-57a8-4761-bcaa-1c62aa761f0d', '날씨의 제동/노면 영향 (적용 여부·강도 미정)',
           '젖음/눈/빙판이 차량 조작을 바꿀지부터 미정. 변화량·안전 제동·난도·점수 예외가 없으므로25% 기획 완료로 올리지 않는다.', weather_doc)
    update('11ecab5e-f568-4faf-b0df-7d7d744b0786', '와이퍼·유리 표현 (수동/자동·가시성 기준 미정)',
           '현재 와이퍼 동작 없음. 조작/자동화·빗방울/결로·1/3인칭 공정성·성능을 정해야 하며 새 버튼/수치는 이번에 정하지 않는다.', weather_doc)
    for key, topic, detail in [
        ('weather-types', '날씨 종류·강도·발생/전환 조건', '비/눈 중 범위·강도·지속시간·전환 시점·수동 선택/무작위 여부 미정.'),
        ('weather-visibility', '강수·소리·노면/시야 표현의 범위', '비/눈 연출·소리·노면 젖음/적설·시인성·최소 성능 기준 미정. 안개 등 추가 종류도 채택 전 후보.'),
        ('day-night', '낮밤 조명·가로등·차량 등화와 가상 시각 연결', '게임 시각은 구현이나 실시간 낮밤 조명은 미구현. 시계 연결/고정 시간 선택·등화/신호·보행자 시인성 기준 미정.'),
        ('weather-mode', '날씨의 영업 영향·싱글 저장/PVP 공통 조건', '승객/수요/시간/비용에 영향이 있을지, 모드별 공정성·재개 상태·전환/성능 경계 미정. 효과를 자동 추가하지 않음.'),
    ]:
        add('49e370b7-882f-44ff-a527-0826df8d3fb1', 'review-20261107-' + key, topic, detail, weather_doc, 0)

    # Small-room followups: detailed contracts can be 25%, idea-only work stays 0%.
    add('8b9fb6f6-146f-4bc3-a32c-6b294034eb5f', 'review-20261107-room-followups',
        '작은 방의 남은 화면 기능 (현재 방/구매 구현과 분리)',
        '아래 미구현 화면만 구분. 현재 소유/배치·개인 구매·영업 회계·날짜 이월·읽기 전용 완료 방은 구현.',
        'docs/content-design/13-three-day-room-and-first-purchase.md', None)
    for key, topic, detail, stage in [
        ('room-compare', '처음 방/완료 방 직접 비교', '13에 동일 화면 전환·성적화 없음·구매0개 처리·읽기 전용을 정의. 미구현이며 사람 수용 완료 아님.', .25),
        ('room-next-day', '다음 날 찜/구매 카드·숨김', '13에 찜 우선/없으면 마지막 구매/둘 다 없으면 생략, 출발 클릭 추가 없음, 숨김은 소유 삭제와 별개를 정의. 미구현.', .25),
        ('room-archive', '전체 과거 회차 방 탐색 확장', '현재 최신 이전 회차만 열람. 전체 목록/탐색·구 기록·저장/보존 범위 상세는 추가 기획 필요.', 0),
        ('room-product-art', '상품별 카드 이미지·아트 범위 확정', '현재 기본 방/물건 레이어와 별개 상품 카드 자산의 목록·스타일/크기·제작 범위 확정 필요.', 0),
    ]:
        add('review-20261107-room-followups', 'review-20261107-' + key, topic, detail,
            'docs/content-design/13-three-day-room-and-first-purchase.md', stage)

    # The legacy remote score proposal is not a missing version of today's LAN.
    for key in ('5d40ab4c-ab70-4652-b553-b8a31102aeb1', 'cb77bf10-4c51-4102-9305-4125df564899'):
        update(key, nodes[key]['topic'] + ' · 대체된 이전 원안 참조',
               '현재 A2 native LAN과 순수익 경쟁은 구현. 이전 독립 운전 총점 산식은 보존 원안이며 현행 미구현 필수 목록에서 제외한다.',
               'docs/content-design/pvp/README.md', None)
    update('fda8bf36-2128-5b71-a722-f930ec9e61fc', '이전 원격 모드·점수 원안 (현재 LAN 구현 참조)',
           '원안은 보존하지만 현재 LAN 미구현이라고 표시하거나 새 독립 경쟁 산식을 자동 채택하지 않는다.', 'docs/content-design/pvp/README.md')
    # All ten recommendation nodes duplicate current/future owner branches.
    for n in nodes['45aa7402-c9e7-5611-b743-fb6d212708ae']['children']:
        topic = n['topic']
        if n['id'] == 'f37dccf5-b756-5fce-a322-a49b7cb59696':
            topic = '추천3 · 날씨·야간 [기존 아이디어·상세 기획/구현 없음]'
        update(n['id'], topic,
               '중복 추천 참조이며 별도 기능으로 집계하지 않는다. 현재 구현/보류/기획 필요는 담당 가지와 이번 전체 미정 목록을 따른다. 기존 추천 숫자는 현재 개발 우선순위 확정이 아니다.',
               'docs/future-design/2026-11-07-planning-gaps-and-weather.md', None)

    for n in list(nodes.values()):
        topic = n['topic'].replace('기상 지연', '잠든 승객 깨우기 지연').replace('기상 대기', '잠든 승객 깨우기 대기')
        if topic != n['topic']:
            update(n['id'], topic, '여기서 기상은 날씨가 아니라 잠든 승객이 일어나는 것을 뜻한다. 용어 혼동을 피하려고 깨우기로 표기한다.', 'docs/content-design/02-passengers-and-fares.md')

    # Parents carry counts, not task percentages. Never rewrite old user notes.
    for n, _ in reversed(list(walk(data['root']))):
        if n.get('children'):
            c = counts(n)
            label = f'하위 집계: 50% {c[.5]} / 25% {c[.25]} / 0% {c[0]}'
            if not sum(c.values()):
                label = '설명·참조 (단계 집계 제외)'
            n['tags'] = [t for t in n.get('tags', []) if not t.startswith('단계:')] + ['단계:' + label]
            append_review(n, label + '\n현재 말단 단계의 집계다. 아래 이전 작성 메모의 수치는 당시 기록이며 최신 집계가 아니다. 제품 완료율·독립 기능 개수와 구분한다.',
                          'docs/future-design/2026-11-07-planning-gaps-and-weather.md')
    data.setdefault('meta', {})['statusReviewDate'] = '2026-11-07'
    return data


def validate(data):
    before = read(BACKUP)
    originals = {n['id']: n for n, _ in walk(before['root'])}
    current = {n['id']: n for n, _ in walk(data['root'])}
    assert len(current) == len(list(walk(data['root']))), 'Duplicate IDs'
    assert originals.keys() <= current.keys(), 'Original user node lost'
    removed = {'5d40ab4c-ab70-4652-b553-b8a31102aeb1', 'cb77bf10-4c51-4102-9305-4125df564899'}
    removed.update(n['id'] for n in originals['45aa7402-c9e7-5611-b743-fb6d212708ae']['children'])
    changed = []
    for key, old in originals.items():
        new = current[key]
        assert old.get('collapsed') == new.get('collapsed'), 'User fold changed'
        assert old.get('note', '') in new.get('note', ''), 'Original note was rewritten'
        assert [n['id'] for n in new.get('children', []) if n['id'] in originals] == [n['id'] for n in old.get('children', [])], 'Original order/parent changed'
        a, b = old.get('task', {}), new.get('task', {})
        assert {k: v for k, v in a.items() if k != 'progress'} == {k: v for k, v in b.items() if k != 'progress'}, 'Other task data changed'
        if a.get('progress') != b.get('progress'):
            assert key in removed and b.get('progress') is None, 'Unexpected stage change'
            changed.append(key)
        oldlinks = {old.get('hyperlink'), *old.get('hyperlinks', [])} - {None, ''}
        assert oldlinks <= {new.get('hyperlink'), *new.get('hyperlinks', [])}, 'Existing link removed'
        for field in old:
            if field not in ('topic', 'note', 'tags', 'hyperlinks', 'children', 'task'):
                assert old[field] == new[field], f'Unrelated metadata changed: {key}/{field}'
    links = 0
    for n in current.values():
        for url in [n.get('hyperlink', ''), *n.get('hyperlinks', [])]:
            if url.startswith(BASE):
                target = ROOT / unquote(url[len(BASE):].split('#')[0])
                assert target.is_file(), 'Missing local Detail-Link: ' + url
                links += 1
    assert OUTLINE.read_text(encoding='utf-8') == outline(data), 'Outline differs'
    c = counts(data['root'])
    for key in ('e96c40dd-57a8-4761-bcaa-1c62aa761f0d', '11ecab5e-f568-4faf-b0df-7d7d744b0786'):
        assert current[key]['task']['progress'] == 0, 'Weather promoted without a design'
    assert len(changed) == 12, 'Expected ten duplicate recommendations plus two retired PVP leaves'
    result = dict(nodes=len(current), original_nodes=len(originals), added_nodes=len(current)-len(originals),
                  implemented50=c[.5], planned25=c[.25], needs_design0=c[0], reference_reclassifications=len(changed),
                  detail_links=links, backup_sha256=digest(BACKUP), preserved='IDs/parents/order/folds/original notes/links/other task data')
    print(json.dumps(result, ensure_ascii=False))
    return result


def inventory(data):
    lines = ['## 마인드맵의 전체 미정·미구현 말단 목록', '',
             '아래는 기존 원본의 말단을 빠짐없이 대조한 탐색 목록이다. 동일 주제의 여러 세부 항목/원안이 있으므로 행 수를 독립 기능 수로 읽지 않는다. 현재 범위 밖/보류도0%로 남기되 포함 여부는 위 분류를 따른다.', '',
             '| 위치 | 항목 | 단계 |', '|---|---|---|']
    for n, parents in walk(data['root']):
        value = n.get('task', {}).get('progress')
        if value not in (0, .25):
            continue
        path = ' → '.join(parents[1:])
        lines.append('| ' + path.replace('|', '/') + ' | ' + n['topic'].replace('|', '/') + ' | ' + ('기획 완료·미구현25%' if value == .25 else '기획 필요/보류0%') + ' |')
    return '\n'.join(lines) + '\n'


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    if args.check:
        validate(read(MAP))
        return
    if BACKUP.exists():
        raise SystemExit('Backup already exists; use --check. Do not overwrite user edits.')
    BACKUP.write_bytes(MAP.read_bytes())
    data = refresh(copy.deepcopy(read(MAP)))
    MAP.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8', newline='\n')
    OUTLINE.write_text(outline(data), encoding='utf-8', newline='\n')
    # The written review owns the prose; only replace its generated appendix.
    t = GAPS.read_text(encoding='utf-8')
    assert '<!-- inventory:start -->' in t and '<!-- inventory:end -->' in t
    a, tail = t.split('<!-- inventory:start -->', 1)
    _, b = tail.split('<!-- inventory:end -->', 1)
    GAPS.write_text(a + '<!-- inventory:start -->\n' + inventory(data) + '<!-- inventory:end -->' + b, encoding='utf-8', newline='\n')
    validate(data)


if __name__ == '__main__':
    main()
