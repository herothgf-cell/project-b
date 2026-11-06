// Refresh published features while preserving existing user structure and notes.
const fs = require('node:fs'), assert = require('node:assert/strict');
const file = 'docs/content-design/택시 시뮬레이션.mmst';
const base = 'https://github.com/herothgf-cell/project-b/blob/main/';
const data = JSON.parse(fs.readFileSync(file, 'utf8').replace(/^\uFEFF/, ''));
const walk = n => [n, ...(n.children || []).flatMap(walk)];
const before = structuredClone(data), nodes = new Map(walk(data.root).map(n => [n.id,n]));
function update(id, topic, detail, doc, progress) {
  const n = nodes.get(id); assert(n, id); n.topic = topic;
  const marker = '\n\n<!-- published-2026-11-06 -->\n';
  n.note = (n.note || '').split(marker)[0] + marker + detail + '\n' + base + doc;
  n.hyperlinks = [...new Set([...(n.hyperlinks || []), base + doc])];
  if (progress !== undefined) {
    n.task = {...n.task, progress};
    n.tags = (n.tags || []).filter(t => !t.startsWith('단계:'));
    n.tags.push('단계:' + (progress === .5 ? '50% 구현' : '0% 기획 필요'));
  }
}
function add(parent, id, topic, detail, doc, progress = .5) {
  if (!nodes.has(id)) {
    const n = {id, topic, children:[], collapsed:true, tags:[], hyperlink:base+doc};
    nodes.get(parent).children.push(n); nodes.set(id,n);
  }
  update(id,topic,detail,doc,progress);
}
update('8affb820-317d-403a-b442-d9f1a5a7f46f','사고 처리 구현 참조 · 보험·과실·현장 구조는 후속','사고 처리는 현재 플레이의 사고·공업사 가지를 따른다. 이 기존 가지의 보험·과실·현장 구조 확장은 미구현이다.','docs/content-design/08-accident-and-workshop-proposal.md');
update('a9743d4c-325d-4cd3-9d56-2cc308c4201e','보행자 사고의 보험·치료비 산정 (후속)','현재 게임 처리비와 보험/치료비 산정은 별개다. 보험·과실·구급차는 미구현.','docs/content-design/08-accident-and-workshop-proposal.md');
add('3dc16dc7-8870-4115-9350-a10c5591eb7b','published-20261106-accidents','보행자·차량·시설 사고: 3등급·운송 중단·비용/시간/감점','싱글/PVP 공통. 보행자 3,000/8,000/15,000원·45/120/240초, 차량·시설 1,000/3,000/6,000원·15/45/90초. 확정 운임 보호, 중대 공업사 이동 포함, 추가 피로0+통상 경과. 사건당 결제/저장·호스트 복제.','docs/content-design/08-accident-and-workshop-proposal.md');
add('3dc16dc7-8870-4115-9350-a10c5591eb7b','published-20261106-person-damage','보행자 사고 내구도 손상: 경미0~2·중간0~5·중대0~10','호스트에서 사건당 한 번 선택·적용·저장. 기존 진행 사고 소급 없음. D0 유료 수리 필요.','docs/work-history/2026-11-06-pedestrian-durability.md');
add('84d7d5b6-b1b6-4ae9-822b-effa684f63ac','published-20261106-workshops','은계·A2 공업사 각2곳: 수리·견인·대기·지도 길안내','수리 내구도1점당200원·10+0.5×손실초, 일반 견인3,000원/30초. PVP 실제 타이머·칸 예약, 취소 서비스 미청구. 중대 사고 이동은 사고비에 포함.','docs/content-design/08-accident-and-workshop-proposal.md');
add('001a8e26-f1a3-488e-8a4c-031d84844ba9','published-20261106-editor','별도 설정 관리창: 대사·선택지71개·단계별 효과·법규 편집','설정 관리.cmd. 즉시/운행 중/운송 종료 후 효과, 성공/실패/판정 불가 구분, 프로필/복원·실제 C# 미리보기. 기존 운송 snapshot 및 LAN 호스트 권한. 새 행동/법규 감지기 생성 아님.','docs/content-tuning-tool-guide.md');
add('e24273c4-4578-44e8-a8bc-513846ded489','published-20261106-room','싱글 정산→내 공간: 조명·러그 구매와 소유 저장','조명5,000/러그8,000원·각2스타일, 미리보기/찜/보관/조명조작. 개인 지출과 영업 수익 분리, 다음 날/재실행 소유 유지·최종 방 읽기 전용. 처음방 비교·전체 과거방 탐색은 후속.','docs/work-history/2026-11-06-personal-room-implementation.md');
add('8b9fb6f6-146f-4bc3-a32c-6b294034eb5f','published-20261106-growth','개인 생활 확장: 차량 구매·꾸미기·이사·장기 재출발','10~14 기획 문서/시안은 작성됨. 현재 구현은 싱글 작은 방·조명/러그 범위. 차량 구매·차량 물품·이사·장기 경제는 미구현이며 가격/진행 검증과 구현 범위 결정이 남아0%로 유지.','docs/content-design/14-personal-life-design-guide.md',0);
update('3693287f-e2c9-4439-9dcd-8bb60215ed04','게임 내 일반 설정 확장 (별도 콘텐츠 관리창과 구분)','별도 콘텐츠 관리창은 구현됐으며 UI/UX 가지 참조. 현재 main에 게시되지 않은 동시 운전 설정 작업은 이 맵의 구현 근거에 포함하지 않는다.','docs/content-tuning-tool-guide.md');
update('18c1c9aa-86eb-4adb-9d51-40fa1d9f581f','승하차 점유 대기·기한 보호·작성된 안전 대체 쌍','게임용으로 작성한 안전 대체 쌍 구현. 전체 지도 임의 장소 자동 생성은 아님.','docs/CURRENT-STATE.md');
update('a4579c31-e8de-5074-9bf6-9f985e0d85dc','용무·경유형: 합의한 용무 대기 구현','합의한20초 용무 대기와 경유 처리. 모든 임의 용무 확장이 아님.','docs/CURRENT-STATE.md');
for(const n of nodes.values()) if(n.topic === '수동 주행 회전 후 방향지시등 자동 해제 (기획 필요)') n.topic='수동 주행 회전 후 방향지시등 자동 해제 (구현)';
function count(n) {
  const c={0:0,0.25:0,0.5:0};
  for(const child of n.children||[]) {const k=count(child);for(const p in c)c[p]+=k[p];}
  if(n.task?.progress!==undefined){assert(!n.children?.length);assert(n.task.progress in c);c[n.task.progress]++;}
  if(n.children?.length && Object.values(c).some(Boolean)) {
    const label=`하위 집계: 50% ${c[.5]} / 25% ${c[.25]} / 0% ${c[0]}`;
    n.tags=(n.tags||[]).filter(t=>!t.startsWith('단계:')).concat('단계:'+label);
    n.note=(n.note||'').replace(/## 사용자 지정 단계: 하위 집계:[^\n]*/, '## 사용자 지정 단계: '+label);
  }
  return c;
}
const counts=count(data.root);
for(const old of walk(before.root)) {
 const n=nodes.get(old.id); assert(n); assert.equal(n.collapsed,old.collapsed);
 assert.deepEqual(n.task,old.task);
 assert.deepEqual((n.children||[]).filter(c=>walk(before.root).some(o=>o.id===c.id)).map(c=>c.id),(old.children||[]).map(c=>c.id));
 const normalize=s=>s.replace(/## 사용자 지정 단계: 하위 집계:[^\n]*/g,'aggregate');
 assert(normalize(n.note||'').includes(normalize((old.note||'').split('\n\n<!-- published-2026-11-06 -->\n')[0])));
 for(const link of [old.hyperlink,...old.hyperlinks||[]].filter(Boolean))assert([n.hyperlink,...n.hyperlinks||[]].includes(link));
}
let links=0; for(const n of nodes.values()) for(const url of [n.hyperlink,...n.hyperlinks||[]].filter(Boolean))if(url.startsWith(base)){assert(fs.existsSync(decodeURIComponent(url.slice(base.length).split('#')[0])),url);links++;}
data.meta.publishedReviewDate='2026-11-06';
const lines=['# 택시 시뮬레이션 마인드맵 목차·진척도','','[마인드맵 원본](택시%20시뮬레이션.mmst) · [최신 정리 기준](06-mindmap-review.md)','','**50% 구현 / 25% 기획 완료·미구현 / 0% 기획 필요**. 부모는 하위 집계, 설명/참조는 집계 제외. 검증 완료율이 아니다.',''];
function emit(n,d){lines.push('  '.repeat(d)+'- '+n.topic.trim()+' — '+n.tags.find(t=>t.startsWith('단계:')).slice(3));for(const c of n.children||[])emit(c,d+1);}
data.root.children.forEach(n=>emit(n,0));
fs.writeFileSync(file,JSON.stringify(data,null,2)+'\n');
fs.writeFileSync('docs/content-design/택시 시뮬레이션-목차.md',lines.join('\n')+'\n');
console.log(JSON.stringify({nodes:nodes.size,links,counts,preserved:'existing IDs/order/folds/tasks/notes/links'}));
