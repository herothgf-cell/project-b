"""Targeted rain status update; preserve every original node, note and link."""
import json, sys, collections
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
MAP=ROOT/'docs/content-design/택시 시뮬레이션.mmst'
BACK=ROOT/'docs/design-history/content-design/2026-11-07-mindmap-before-rain.mmst'
DOC='https://github.com/herothgf-cell/project-b/blob/main/docs/content-design/18-rain-prototype.md'
def walk(n):
    yield n
    for c in n.get('children',[]):yield from walk(c)
def outline(data):
    lines=['# 택시 시뮬레이션 마인드맵 목차·진척도','','[원본](택시%20시뮬레이션.mmst) · [비 프로토 기획](18-rain-prototype.md) · [관리 기준](06-mindmap-review.md)','','50% 구현 / 25% 기획 완료·미구현 / 0% 기획 필요. 구현 단계는 수동 검증 완료율이 아니다. 비만 프로토에 포함하며 눈/낮밤/확률 미끄러짐 활성화는 후속이다.','']
    def emit(n,depth):
        lines.append('  '*depth+'- '+n['topic']+' — '+next((t[3:] for t in n.get('tags',[]) if t.startswith('단계:')),'설명·참조'))
        for c in n.get('children',[]):emit(c,depth+1)
    for n in data['root']['children']:emit(n,0)
    return '\n'.join(lines)+'\n'
if '--check' not in sys.argv:
    if BACK.exists():raise SystemExit('Snapshot already exists; use --check')
    BACK.write_bytes(MAP.read_bytes())
    d=json.loads(MAP.read_text(encoding='utf-8'));nodes={n['id']:n for n in walk(d['root'])}
    changes={
      '49e370b7-882f-44ff-a527-0826df8d3fb1':('날씨 · 비 프로토 / 눈·낮밤 후속',None),
      'e96c40dd-57a8-4761-bcaa-1c62aa761f0d':('비 제동 계수·전후 접지 영향 / 정차 유지 보존',.5),
      '11ecab5e-f568-4faf-b0df-7d7d744b0786':('1인칭 전방 자동 와이퍼 / 수동·유리 물기 후속',.5),
      'review-20261107-weather-types':('맑음/비 고정 시험 선택 / 자동 전환 후속',.5),
      'review-20261107-weather-visibility':('비 파티클·흐린 하늘/조명 / 소리·젖은 도로 표현 후속',.5),
      'review-20261107-weather-mode':('날씨 설정 저장/이어하기·LAN 호스트 공유',.5),
      'f37dccf5-b756-5fce-a322-a49b7cb59696':('추천3 · 비 프로토 채택 / 눈·야간 후속 참조',None)}
    for key,(topic,stage) in changes.items():
        n=nodes[key];n['topic']=topic
        if stage is not None:n.setdefault('task',{})['progress']=stage
        n['note']=n.get('note','')+'\n\n## 2026-11-07 비 프로토 승인 후 갱신\n'+topic+'\n기존 메모는 이전 상태이며 이번 기획18/작업 이력을 우선한다.\n'+DOC
        if DOC not in n.setdefault('hyperlinks',[]):n['hyperlinks'].append(DOC)
    parent=nodes['49e370b7-882f-44ff-a527-0826df8d3fb1']
    for key,title,stage in [('boarding','성공 승차 추가 오염·중복 저장 방지',.5),('tool','툴 날씨 탭 · 비/강도/와이퍼/오염/제동/교통 계수',.5),('slip','조건+확률 미끄러짐 · 검증 후 개발/활성화',0),('snow','눈·결빙·타이어/체인 · 프로토 제외',0)]:
        parent['children'].append(dict(id='rain-20261107-'+key,topic=title,children=[],task={'progress':stage},tags=[],note='비 프로토 승인 범위. 상세는 기획18을 따른다.',hyperlink=DOC))
    def refresh(n):
        count=collections.Counter()
        for c in n.get('children',[]):count.update(refresh(c))
        stage=n.get('task',{}).get('progress')
        if stage is not None:count[stage]+=1
        label=('50% 구현' if stage==.5 else '25% 기획 완료·미구현' if stage==.25 else '0% 기획 필요') if stage is not None else ('하위 구현 %d / 기획 %d / 미정 %d'%(count[.5],count[.25],count[0]) if count else '설명·참조')
        n['tags']=[t for t in n.get('tags',[]) if not t.startswith('단계:')]+['단계:'+label]
        return count
    refresh(d['root']);MAP.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
    (MAP.parent/'택시 시뮬레이션-목차.md').write_text(outline(d),encoding='utf-8',newline='\n')
before=json.loads(BACK.read_text(encoding='utf-8'));after=json.loads(MAP.read_text(encoding='utf-8'));nodes={n['id']:n for n in walk(after['root'])}
assert len(nodes)==len(list(walk(after['root'])))
for old in walk(before['root']):
    new=nodes[old['id']];assert old.get('note','') in new.get('note','');assert old.get('collapsed')==new.get('collapsed')
    oldids=[c['id'] for c in old.get('children',[])];assert [c['id'] for c in new.get('children',[]) if c['id'] in oldids]==oldids
    for link in [old.get('hyperlink'),*old.get('hyperlinks',[])]:
        if link:assert link in [new.get('hyperlink'),*new.get('hyperlinks',[])]
assert (MAP.parent/'택시 시뮬레이션-목차.md').read_text(encoding='utf-8')==outline(after)
print('RAIN_MINDMAP_OK original nodes/notes/order/folds/links preserved; nodes='+str(len(nodes)))
