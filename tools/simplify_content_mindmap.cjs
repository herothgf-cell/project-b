// 2026-11-13: overview map + lossless source backup + readable detail indexes.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const ROOT = path.resolve(__dirname, '..');
const DIR = 'docs/content-design/';
const MAP = DIR + '택시 시뮬레이션.mmst';
const BACKUP = 'docs/design-history/content-design/2026-11-13-mindmap-before-simplify.mmst';
const BASE = 'https://github.com/herothgf-cell/project-b/blob/main/';
const read = p => fs.readFileSync(path.join(ROOT, p), 'utf8').replace(/^\uFEFF/, '');
const write = (p, s) => fs.writeFileSync(path.join(ROOT, p), s, 'utf8');
const walk = n => [n, ...(n.children || []).flatMap(walk)];
const url = p => BASE + p;
const link = (label, p) => `[${label}](${p.replaceAll(' ', '%20')})`;
function detailTarget(u) {
  if (u.startsWith(BASE)) return path.posix.relative(DIR, u.slice(BASE.length));
  if (!/^[a-z]+:/i.test(u) && fs.existsSync(path.join(ROOT, 'docs', u.split('#')[0]))) return '../' + u;
  return u;
}
const files = [
  ['24-passenger-detail-index.md', '승객·대화·요청 세부 기획', ['02-passengers-and-fares.md', '07-passenger-dialogue.md', '15-passenger-presets-and-traits.md', '16-passenger-preset-overrides.md', '19-passenger-trait-expansion.md', '2026-11-13-passenger-current-inventory.md']],
  ['25-driving-rule-detail-index.md', '주행·법규·운전 기록 세부 기획', ['01-driving-and-traffic.md', '20-roadview-signals-passenger-feedback.md', '21-lane-movement-roadview-followup.md', '../content-tuning-tool-guide.md']],
  ['26-system-detail-index.md', '모드·지도·생활·차량·화면 세부 기획', ['00-game-overview.md', '03-life-and-economy.md', '04-campaign-and-feedback.md', '05-interface-and-navigation.md', '08-accident-and-workshop-proposal.md', '13-three-day-room-and-first-purchase.md', '22-growth-direction-and-vehicle-purchase.md', 'pvp/30-native-lan.md']],
  ['27-future-detail-index.md', '성장·후속 구상 세부 기획', ['11-personal-life-and-growth.md', '12-career-growth-stages.md', '18-rain-prototype.md', '22-growth-direction-and-vehicle-purchase.md', '23-license-career-path.md', '../future-design/README.md']],
];
function validate() {
  const old = JSON.parse(read(BACKUP)), data = JSON.parse(read(MAP));
  const nodes = walk(data.root);
  assert.equal(new Set(nodes.map(n => n.id)).size, nodes.length);
  const depth = n => 1 + Math.max(0, ...(n.children || []).map(depth));
  assert(depth(data.root) <= 4);
  assert(nodes.length <= 60);
  let links = 0;
  for (const n of nodes) for (const u of [n.hyperlink, ...(n.hyperlinks || [])].filter(Boolean)) {
    assert(u.startsWith(BASE));
    const p = decodeURI(u.slice(BASE.length).split('#')[0]);
    assert(fs.existsSync(path.join(ROOT, p)), p); links++;
  }
  const detail = files.map(([p]) => read(DIR + p)).join('\n');
  const originals = walk(old.root).filter(n => n !== old.root);
  for (const n of originals) assert(detail.includes(`<!-- source-node:${n.id} -->`), n.topic);
  assert.equal((detail.match(/<!-- source-node:/g) || []).length, originals.length);
  const outline = read(DIR + '택시 시뮬레이션-목차.md');
  for (const n of nodes.slice(1)) assert(outline.includes(n.topic), n.topic);
  let documentLinks = 0;
  for (const file of [DIR + '택시 시뮬레이션-목차.md', ...files.map(([p]) => DIR + p)]) {
    for (const match of read(file).matchAll(/\]\(([^)]+)\)/g)) {
      const u = match[1];
      if (/^(https?:|#)/.test(u)) continue;
      assert(fs.existsSync(path.resolve(ROOT, path.dirname(file), decodeURIComponent(u.split('#')[0]))), `${file}: ${u}`);
      documentLinks++;
    }
  }
  console.log(`PASS nodes=${walk(old.root).length}->${nodes.length} depth=${depth(data.root)-1} links=${links} detailRecords=${originals.length}`);
  console.log(`PASS documentLinks=${documentLinks}`);
  console.log('BACKUP_SHA256 ' + crypto.createHash('sha256').update(fs.readFileSync(path.join(ROOT, BACKUP))).digest('hex'));
}
if (process.argv.includes('--check')) { validate(); process.exit(0); }
assert(!fs.existsSync(path.join(ROOT, BACKUP)), 'Backup exists; use --check, never overwrite source history.');
const data = JSON.parse(read(MAP));
fs.copyFileSync(path.join(ROOT, MAP), path.join(ROOT, BACKUP));
const buckets = [[], [], [], []];
for (const group of data.root.children) {
  if (group.topic !== '현재 플레이 기획') { buckets[3].push(group); continue; }
  // Keep the group record once as well as every individual source node.
  buckets[2].push({...group, children: []});
  for (const branch of group.children) {
    buckets[branch.topic === '승객&보행자' ? 0 : ['주행 & 운송', '운전 점수'].includes(branch.topic) ? 1 : 2].push(branch);
  }
}
files.forEach(([file, title, refs], i) => {
  const out = [`# ${title}`, '', '[간단 마인드맵 목차](택시%20시뮬레이션-목차.md) · [기획 목록](README.md)', '',
    '마인드맵에서 옮긴 세부 항목을 분야별로 찾는 문서다. **아래 이관 목록은 2026-11-13 정리 직전의 항목명·단계 기록이며, 최신 규칙은 담당 기획을 우선한다.** 과거 수치·승객 수·LAN 버전을 현재 값으로 다시 채택하지 않는다.', '', '## 현재 담당 기획', '',
    ...refs.map(p => `- ${link(read(path.posix.normalize(DIR + p)).split(/\r?\n/)[0].replace(/^#\s*/, ''), p)}`), '',
    ...(i === 0 ? ['승객 유형·개별 대사·선택·효과는 최신 현황표에서 확인한다. 기본 프리셋 30종 중 29종 활성 상태이며, 이관 목록의 24명/23활성 표기는 과거 기록이다.', ''] : []),
    ...(i === 1 ? ['법규별 판정·감점·운전 점수 기록은 01에서, 편집은 콘텐츠 관리 안내에서 확인한다. 게임 내 평가 규칙이며 현실 법률 안내를 새로 작성한 문서가 아니다.', ''] : []),
    ...(i === 3 ? ['차량 교체·차종 특성·PvP 외형과 비 프로토는 이미 구현된 범위가 있다. 예전 후속 가지에 있더라도 22·18의 현재 상태를 우선한다. 기사 평점·면허 경로와 눈·낮밤은 보류/후속이다.', ''] : []),
    '## 마인드맵에서 이관한 세부 항목', '', '50% = 당시 구현 단계, 25% = 당시 기획 완료, 0% = 당시 기획 필요/보류. 사람 플레이 검증 완료율이 아니다. 원래 메모·태그·작업 속성은 [정리 전 원본](../design-history/content-design/2026-11-13-mindmap-before-simplify.mmst)에 모두 보존했다.', ''];
  function emit(n, level) {
    const status = typeof n.task?.progress === 'number' ? ` — 당시 ${Math.round(n.task.progress * 100)}%` : '';
    const refs = [...new Set([n.hyperlink, ...(n.hyperlinks || [])].filter(Boolean))];
    out.push(`${'  '.repeat(level)}- ${n.topic.trim()}${status} <!-- source-node:${n.id} -->${refs.length ? ' · ' + refs.map((u, j) => link(`참조${j+1}`, detailTarget(u))).join(' · ') : ''}`);
    (n.children || []).forEach(c => emit(c, level + 1));
  }
  buckets[i].forEach(n => emit(n, 0));
  write(DIR + file, out.join('\n') + '\n');
});
const now = Date.now();
function node(topic, doc, children = [], extras = []) {
  return {id: crypto.randomUUID(), topic, children, createdAt: now, modifiedAt: now, collapsed: false,
    hyperlink: url(path.posix.normalize(DIR + doc)), hyperlinks: extras.map(p => url(path.posix.normalize(DIR + p))),
    note: '상세 항목은 Detail → Links에서 확인합니다. 마인드맵은 탐색용이며 구현·검증 상태는 담당 기획과 최신 작업 이력을 따릅니다.'};
}
data.root.children = [
  node('현재 플레이', '00-game-overview.md', [
    node('실행·모드', '26-system-detail-index.md', [node('은계 싱글 · 3일 캠페인', '04-campaign-and-feedback.md'), node('자체 도시 PvP · 봇 / LAN', 'pvp/30-native-lan.md')]),
    node('지도·시설', '26-system-detail-index.md', [node('도로·지역·지도 편집', '05-interface-and-navigation.md'), node('식사·주유·세차·휴식 거점', '03-life-and-economy.md')]),
    node('주행·교통·법규', '25-driving-rule-detail-index.md', [node('조작·신호·차로·길안내', '01-driving-and-traffic.md'), node('법규 판정·운전 점수·개별 기록', '25-driving-rule-detail-index.md', [], ['01-driving-and-traffic.md'])]),
    node('승객·운송', '24-passenger-detail-index.md', [node('호출·승하차·운임', '02-passengers-and-fares.md'), node('승객 유형·특성·개별 설정', '24-passenger-detail-index.md', [], ['2026-11-13-passenger-current-inventory.md']), node('요청·대화·선택 결과', '07-passenger-dialogue.md', [], ['2026-11-13-passenger-current-inventory.md'])]),
    node('기사 생활·경제', '03-life-and-economy.md', [node('피로·포만·시설 이용', '03-life-and-economy.md'), node('영업 수익·지출·정산', '04-campaign-and-feedback.md')]),
    node('차량·사고', '26-system-detail-index.md', [node('연료·청결·내구도', '03-life-and-economy.md'), node('사고 처리·수리·견인', '08-accident-and-workshop-proposal.md')]),
    node('개인 공간·차량 성장', '22-growth-direction-and-vehicle-purchase.md', [node('작은 방·소품 구매', '13-three-day-room-and-first-purchase.md'), node('차량 교체·차종 특성·PvP 외형', '22-growth-direction-and-vehicle-purchase.md')]),
    node('화면·설정', '05-interface-and-navigation.md', [node('HUD·지도·안내', '05-interface-and-navigation.md'), node('운전 설정·콘텐츠 관리', '../content-tuning-tool-guide.md')]),
    node('날씨 · 비 프로토', '18-rain-prototype.md'),
  ]),
  node('성장·후속 구상', '27-future-detail-index.md', [
    node('장기 경력·이사·생활 확장', '12-career-growth-stages.md', [], ['27-future-detail-index.md']),
    node('승객·예약·차내 장치 확장', '27-future-detail-index.md'),
    node('날씨·교통·차량·보험 확장', '27-future-detail-index.md'),
    node('기사 평점·면허 경로 · 보류', '23-license-career-path.md', [], ['22-growth-direction-and-vehicle-purchase.md']),
  ]),
  node('기획·검증 문서', 'README.md', [
    node('게임 개요·이야기', '00-game-overview.md', [], ['10-restarting-driver-synopsis.md']),
    node('세부 기획 목록', 'README.md', [], files.map(f => f[0])),
    node('현재 구현·검증·작업 이력', '../CURRENT-STATE.md', [], ['../prototype-development-checklist.md', '../work-history/LATEST.md']),
  ]),
];
data.root.note = '핵심 구조만 펼친 개요입니다. 승객 유형·법규별 기록·수치·세부 조건은 각 가지의 Detail → Links에 연결된 기획문서를 확인하세요.';
data.root.collapsed = false;
data.root.hyperlink = url(DIR + '택시 시뮬레이션-목차.md');
data.root.hyperlinks = [url(BACKUP)];
delete data.root.task; data.root.tags = ['개요·문서 탐색'];
data.meta = {...data.meta, updatedAt: now, overviewVersion: '2026-11-13', detailArchive: BACKUP};
write(MAP, JSON.stringify(data, null, 2) + '\n');
const outline = ['# 택시 시뮬레이션 마인드맵 목차', '', '[마인드맵](택시%20시뮬레이션.mmst) · [기획 목록](README.md) · [관리 기준](06-mindmap-review.md)', '',
  '핵심 구조만 3단계까지 표시한다. 개별 승객·법규·수치·조건은 연결된 기획문서에서 확인한다. 이 목차는 마인드맵과 같은 구조이며 완료율을 집계하지 않는다.', '',
  '마인드맵의 각 노드에서 **Detail → Links**로 담당 문서를 연다. 앱 링크는 기존 방식인 GitHub main 문서 주소를 사용하며, 로컬 열람은 아래 상대 링크를 이용한다. 새 문서는 커밋·푸시 후 원격 링크에 반영된다.', ''];
function emitOutline(n, level) {
  outline.push('  '.repeat(level) + '- ' + link(n.topic, path.posix.relative(DIR, n.hyperlink.slice(BASE.length))));
  n.children.forEach(c => emitOutline(c, level + 1));
}
data.root.children.forEach(n => emitOutline(n, 0));
outline.push('', '## 세부 항목 문서', '', ...files.map(([p, t]) => '- ' + link(t, p)), '', '정리 전의 노드·메모·진척도는 [원본 보존본](../design-history/content-design/2026-11-13-mindmap-before-simplify.mmst)에 남겼다. 현재 구현·미검증 상태는 [최신 이력](../work-history/LATEST.md)을 따른다.');
write(DIR + '택시 시뮬레이션-목차.md', outline.join('\n') + '\n');
validate();
