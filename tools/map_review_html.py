"""Write the self-contained review page artifacts/map-review-20261124/sheet/index.html.

Embeds items.json, zone list and the questions so the page works from file:// (no server).
Reviewer state (status, feedback, attached images, drawings) lives in the browser's
localStorage and is exported/imported as JSON with the buttons on the page.
"""
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SHEET = ROOT / 'artifacts/map-review-20261124/sheet'

QUESTIONS = [
    ('Q1', '유턴 표지 위치 원칙', '차로 한가운데에 선 유턴 조건 표지는 바닥 기둥을 없애고 신호등 가로대(차로 위)에 매다는 쪽으로 시안을 그렸습니다. 한국 교차로 실제 모습과 같습니다. 이 원칙으로 일괄 적용해도 될까요? (각 P 항목에서 승인/반려로 답해 주세요)'),
    ('Q2', '항공사진과 게임 원본의 시점 차이', '게임은 2020년 정밀도로지도 측량, 카카오 항공·로드뷰는 더 최신(로드뷰 2026-06)입니다. 둘이 다르면 "최신 실제"에 맞추는 것으로 시안을 그렸습니다. 공사로 바뀐 곳도 최신에 맞출까요?'),
    ('Q3', '차로 수를 늘리는 수정', '차로 수 부족(N) 항목은 도로 폭과 승하차·경로·신호까지 바뀝니다. 승인한 항목만 한 곳씩 고치고, 고칠 때마다 교통 검사와 실행본 회귀를 돌리는 순서를 제안합니다. 괜찮을까요?'),
    ('Q4', '도로 번호 체계', '구역 A~H와 구역 안 번호(예: C-01 수인로)로 나눴습니다. 게임 M지도에 이 번호를 표시(개발용 켜고 끄기)할지 정해 주세요.'),
    ('Q5', '로드뷰 확인 범위', '항공사진만으로 확실하지 않은 항목은 "로드뷰 필요"로 표시했습니다. 밤사이 일부만 로드뷰로 직접 확인했습니다. 나머지를 다음 작업에서 모두 로드뷰로 확인할까요, 아니면 승인한 항목만 확인할까요?'),
]

PAGE = r'''<!doctype html>
<html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>은계 지도 수정 검토</title>
<style>
:root{--bg:#f4f3ef;--card:#fff;--ink:#1d1f24;--mute:#666b75;--line:#dcdad3;--acc:#1d63d6;--ok:#1e9a52;--warn:#d98a00;--bad:#d23a2e;--hold:#7a59c9}
@media (prefers-color-scheme:dark){:root{--bg:#16171a;--card:#202227;--ink:#ecebe6;--mute:#9a9ea8;--line:#34363d;--acc:#6fa0ff}}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--ink);font:14px/1.55 "Malgun Gothic","Apple SD Gothic Neo",system-ui,sans-serif}
header{position:sticky;top:0;z-index:20;background:var(--card);border-bottom:1px solid var(--line);padding:10px 16px;display:flex;flex-wrap:wrap;gap:8px;align-items:center}
header h1{font-size:17px;margin:0 12px 0 0}select,input[type=search],button{font:inherit;border:1px solid var(--line);background:var(--card);color:var(--ink);border-radius:7px;padding:5px 9px}
button{cursor:pointer}button.primary{background:var(--acc);color:#fff;border-color:var(--acc)}
.stat{color:var(--mute);font-size:13px;margin-left:auto}
main{max-width:1480px;margin:0 auto;padding:16px}
section.block{background:var(--card);border:1px solid var(--line);border-radius:12px;padding:16px;margin-bottom:16px}
h2{font-size:16px;margin:0 0 10px}.mute{color:var(--mute)}
.map{position:relative;max-width:1000px}.map img{width:100%;display:block;border-radius:8px}
.map a{position:absolute;width:18px;height:18px;margin:-9px 0 0 -9px;border-radius:50%;border:2px solid #fff;box-shadow:0 0 0 1px #0005}
.legend span{display:inline-block;margin:2px 10px 2px 0}.dot{display:inline-block;width:10px;height:10px;border-radius:50%;margin-right:4px;vertical-align:middle}
.q{border-left:4px solid var(--hold);padding:6px 10px;margin:8px 0;background:color-mix(in srgb,var(--hold) 8%,transparent)}
.q textarea{width:100%;min-height:44px;margin-top:6px}
article{scroll-margin-top:110px;background:var(--card);border:1px solid var(--line);border-radius:12px;margin-bottom:18px;overflow:hidden}
article.st-approve{border-left:6px solid var(--ok)}article.st-review{border-left:6px solid var(--warn)}article.st-reject{border-left:6px solid var(--bad)}article.st-hold{border-left:6px solid var(--hold)}
.head{padding:12px 16px;display:flex;flex-wrap:wrap;gap:8px;align-items:center;border-bottom:1px solid var(--line)}
.head b.id{font-size:18px}.badge{border-radius:20px;padding:1px 9px;font-size:12px;color:#fff}
.b-심각{background:var(--bad)}.b-보통{background:var(--warn)}.b-경미{background:#7d8b2a}.b-일치{background:var(--ok)}.b-미판정{background:#888}
.chip{border:1px solid var(--line);border-radius:6px;padding:0 6px;font-size:12px;color:var(--mute)}
.body{display:grid;grid-template-columns:1fr 1fr;gap:12px;padding:12px 16px}
@media (max-width:900px){.body{grid-template-columns:1fr}}
figure{margin:0}figcaption{font-weight:700;margin-bottom:4px;display:flex;gap:6px;align-items:center;flex-wrap:wrap}
.stage{position:relative}.stage img{width:100%;display:block;border-radius:6px}.stage canvas{position:absolute;inset:0;width:100%;height:100%;cursor:crosshair;touch-action:none}
.tools{display:flex;flex-wrap:wrap;gap:4px;margin-top:6px}.tools button{padding:2px 8px;font-size:12px}.tools button.on{background:var(--acc);color:#fff}
.tools input[type=color]{width:32px;height:26px;padding:0;border:none;background:none}
table{border-collapse:collapse;width:100%;font-size:13px}td,th{border-bottom:1px solid var(--line);padding:4px 6px;text-align:left;vertical-align:top}
.issues{padding:0 16px 8px}.issue{display:grid;grid-template-columns:34px 1fr 1fr 130px;gap:8px;border-top:1px solid var(--line);padding:8px 0}
@media (max-width:900px){.issue{grid-template-columns:34px 1fr}}
.num{width:28px;height:28px;border-radius:7px;background:var(--bad);color:#fff;display:flex;align-items:center;justify-content:center;font-weight:700}
.sev-중 .num{background:var(--warn)}.sev-하 .num{background:#c7b300;color:#000}
.lbl{font-size:12px;color:var(--mute)}
.review{padding:12px 16px;border-top:1px solid var(--line);display:grid;grid-template-columns:auto 1fr;gap:10px 16px;align-items:start}
@media (max-width:900px){.review{grid-template-columns:1fr}}
.statusbar button{margin:0 4px 4px 0}.statusbar button.on.approve{background:var(--ok);color:#fff}.statusbar button.on.review{background:var(--warn);color:#fff}.statusbar button.on.hold{background:var(--hold);color:#fff}.statusbar button.on.reject{background:var(--bad);color:#fff}
textarea{width:100%;min-height:70px;font:inherit;border:1px solid var(--line);border-radius:7px;padding:6px;background:var(--card);color:var(--ink)}
.drop{border:2px dashed var(--line);border-radius:8px;padding:8px;color:var(--mute);font-size:12px;margin-top:6px}
.thumbs{display:flex;flex-wrap:wrap;gap:6px;margin-top:6px}.thumbs div{position:relative}.thumbs img{height:80px;border-radius:5px;border:1px solid var(--line)}
.thumbs button{position:absolute;top:2px;right:2px;padding:0 5px;font-size:11px}
a{color:var(--acc)}.hidden{display:none}
</style></head><body>
<header>
 <h1>은계 지도 수정 검토 (2026-11-24)</h1>
 <select id="fZone"><option value="">모든 구역</option></select>
 <select id="fCat"><option value="">모든 유형</option></select>
 <select id="fMatch"><option value="">모든 판정</option><option>심각</option><option>보통</option><option>경미</option><option>일치</option></select>
 <select id="fStatus"><option value="">모든 상태</option><option value="none">미검토</option><option value="approve">승인</option><option value="review">다시 검토</option><option value="hold">보류</option><option value="reject">반려</option></select>
 <input type="search" id="fText" placeholder="번호·도로명·내용 검색">
 <button id="bExport" class="primary">검토 결과 내보내기(JSON)</button>
 <button id="bImport">가져오기</button><input type="file" id="importFile" accept=".json" class="hidden">
 <span class="stat" id="stat"></span>
</header>
<main>
<section class="block">
 <h2>사용법</h2>
 <div class="mute">항목마다 <b>전(게임 현재)</b>과 <b>후(수정 시안)</b>를 나란히 보여 줍니다. 후 그림은 실제 카카오 항공사진 위에 지금 게임 차로 가장자리(하늘색 선)를 겹친 것입니다. 하늘색 선이 사진 속 도로와 어긋나는 곳이 고칠 곳이고, 초록 번호 상자가 아래 수정 내용 번호입니다. P 항목은 차로 안에 선 기둥의 평면 도면입니다.<br>
 · <b>승인 / 다시 검토 / 보류 / 반려</b>를 누르고 의견을 적습니다. 이미지는 끌어다 놓기·붙여넣기(Ctrl+V)·파일 선택으로 첨부합니다.<br>
 · 후 그림 위에 <b>펜·화살표·상자·글자</b>로 직접 표시할 수 있습니다(되돌리기·지우기 가능).<br>
 · 모든 내용은 이 브라우저에 자동 저장됩니다. 다 보신 뒤 <b>“검토 결과 내보내기”</b>로 JSON 파일을 받아 저장소 <code>docs/work-history/</code> 옆이나 채팅에 주시면 그대로 이어서 고칩니다.</div>
</section>
<section class="block">
 <h2>구역·도로 번호 지도 (빨간 점 = 대조한 교차로, 누르면 해당 항목으로 이동)</h2>
 <div class="legend" id="zoneLegend"></div>
 <div class="map" id="map"><img src="img/zones.png" alt="구역 지도"></div>
</section>
<section class="block"><h2>확인이 필요한 질문 (답을 적어 주세요)</h2><div id="questions"></div></section>
<section class="block"><h2>유형별 요약</h2><div id="summary"></div></section>
<div id="list"></div>
</main>
<script>
const ITEMS=__ITEMS__;const ZONES=__ZONES__;const QUESTIONS=__QUESTIONS__;const CATS=__CATS__;
const KEY='eungye-map-review-20261124';
let state={};try{state=JSON.parse(localStorage.getItem(KEY)||'{}')}catch(e){state={}}
function save(){try{localStorage.setItem(KEY,JSON.stringify(state));}catch(e){alert('브라우저 저장 공간이 가득 찼습니다. “검토 결과 내보내기”로 JSON을 저장한 뒤 큰 첨부 이미지를 지워 주세요.');}}
function S(id){return state[id]||(state[id]={status:'',note:'',issues:{},attach:[],marks:[]});}
const $=s=>document.querySelector(s);const el=(t,a={},...c)=>{const e=document.createElement(t);for(const k in a){if(k==='class')e.className=a[k];else if(k.startsWith('on'))e.addEventListener(k.slice(2),a[k]);else e.setAttribute(k,a[k]);}for(const x of c)e.append(x);return e;};
// filters
ZONES.forEach(z=>$('#fZone').append(el('option',{value:z.id},z.id+' '+z.name)));
Object.entries(CATS).forEach(([k,v])=>$('#fCat').append(el('option',{value:k},k+' '+v)));
const colours={A:'#f0a050',B:'#d6b800',C:'#3b82f6',D:'#2e9e5b',E:'#b35fd0',F:'#6366f1',G:'#888',H:'#e05a5a'};
ZONES.forEach(z=>$('#zoneLegend').append(el('span',{},el('span',{class:'dot',style:'background:'+colours[z.id]}),z.id+' '+z.name+' — '+z.description)));
// map markers (overview scale 0.8, 1680x1400)
const seenIdx=new Set();
ITEMS.filter(i=>i.kind==='junction').forEach(i=>{if(seenIdx.has(i.index))return;seenIdx.add(i.index);
 const sev={심각:'#d23a2e',보통:'#d98a00',경미:'#a9b200',일치:'#1e9a52'}[i.match]||'#888';
 const a=el('a',{href:'#'+i.id,title:i.index+' '+i.junction+' — '+(i.summary||''),style:`left:${i.x*0.8/1680*100}%;top:${(1400-i.z*0.8)/1400*100}%;background:${sev}`});$('#map').append(a);});
// questions
QUESTIONS.forEach(([id,t,body])=>{const s=S(id);const ta=el('textarea',{placeholder:'답변'});ta.value=s.note||'';ta.addEventListener('input',()=>{s.note=ta.value;save();});
 $('#questions').append(el('div',{class:'q'},el('b',{},id+' '+t),el('div',{},body),ta));});
// summary
(function(){const by={};ITEMS.forEach(i=>(i.issues||[]).forEach(x=>{const k=x.cat||'X';by[k]=by[k]||{상:0,중:0,하:0};by[k][x.severity]=(by[k][x.severity]||0)+1;}));
 const t=el('table',{},el('tr',{},el('th',{},'유형'),el('th',{},'상'),el('th',{},'중'),el('th',{},'하')));
 Object.keys(CATS).forEach(k=>{if(!by[k])return;t.append(el('tr',{},el('td',{},k+' '+CATS[k]),el('td',{},String(by[k].상||0)),el('td',{},String(by[k].중||0)),el('td',{},String(by[k].하||0))));});
 const m={};ITEMS.forEach(i=>m[i.match]=(m[i.match]||0)+1);
 $('#summary').append(el('div',{class:'mute'},`항목 ${ITEMS.length}개 (교차로 ${ITEMS.filter(i=>i.kind==='junction').length} + 차로 안 기둥 ${ITEMS.filter(i=>i.kind==='post').length}) · 판정 `+Object.entries(m).map(([k,v])=>k+' '+v).join(' · ')),t);})();
// drawing
function attachCanvas(stage,img,item){
 const cv=el('canvas');stage.append(cv);const s=S(item.id);let tool='pen',colour='#ff2d2d',drawing=null;
 function size(){cv.width=img.naturalWidth||1200;cv.height=img.naturalHeight||675;redraw();}
 function P(e){const r=cv.getBoundingClientRect();return[(e.clientX-r.left)/r.width*cv.width,(e.clientY-r.top)/r.height*cv.height];}
 function arrow(c,a,b){c.beginPath();c.moveTo(a[0],a[1]);c.lineTo(b[0],b[1]);c.stroke();const g=Math.atan2(b[1]-a[1],b[0]-a[0]),h=22;c.beginPath();c.moveTo(b[0],b[1]);c.lineTo(b[0]-h*Math.cos(g-.45),b[1]-h*Math.sin(g-.45));c.lineTo(b[0]-h*Math.cos(g+.45),b[1]-h*Math.sin(g+.45));c.closePath();c.fill();}
 function paint(c,m){c.strokeStyle=c.fillStyle=m.c;c.lineWidth=5;c.lineCap=c.lineJoin='round';
  if(m.t==='pen'){c.beginPath();m.p.forEach((p,i)=>i?c.lineTo(p[0],p[1]):c.moveTo(p[0],p[1]));c.stroke();}
  else if(m.t==='arrow')arrow(c,m.p[0],m.p[1]);else if(m.t==='box'){const[a,b]=m.p;c.strokeRect(a[0],a[1],b[0]-a[0],b[1]-a[1]);}
  else if(m.t==='text'){c.font='bold 30px Malgun Gothic,sans-serif';c.lineWidth=6;c.strokeStyle='#000';c.strokeText(m.s,m.p[0][0],m.p[0][1]);c.fillText(m.s,m.p[0][0],m.p[0][1]);}}
 function redraw(){const c=cv.getContext('2d');c.clearRect(0,0,cv.width,cv.height);s.marks.forEach(m=>paint(c,m));if(drawing)paint(c,drawing);}
 cv.addEventListener('pointerdown',e=>{const p=P(e);if(tool==='text'){const t=prompt('표시할 글자');if(t){s.marks.push({t:'text',c:colour,p:[p],s:t});save();redraw();}return;}cv.setPointerCapture(e.pointerId);drawing={t:tool,c:colour,p:[p,p]};if(tool==='pen')drawing.p=[p];});
 cv.addEventListener('pointermove',e=>{if(!drawing)return;const p=P(e);if(drawing.t==='pen')drawing.p.push(p);else drawing.p[1]=p;redraw();});
 cv.addEventListener('pointerup',()=>{if(drawing){s.marks.push(drawing);drawing=null;save();redraw();}});
 if(img.complete)size();else img.addEventListener('load',size);
 const bar=el('div',{class:'tools'});const btns={};
 [['pen','펜'],['arrow','화살표'],['box','상자'],['text','글자']].forEach(([k,n])=>{const b=el('button',{onclick:()=>{tool=k;Object.values(btns).forEach(x=>x.classList.remove('on'));b.classList.add('on');}},n);btns[k]=b;bar.append(b);});btns.pen.classList.add('on');
 const col=el('input',{type:'color',value:colour});col.addEventListener('input',()=>colour=col.value);bar.append(col);
 bar.append(el('button',{onclick:()=>{s.marks.pop();save();redraw();}},'되돌리기'),el('button',{onclick:()=>{if(confirm('이 그림의 표시를 모두 지울까요?')){s.marks=[];save();redraw();}}},'표시 지우기'),
  el('button',{onclick:()=>{const c=el('canvas');c.width=cv.width;c.height=cv.height;const x=c.getContext('2d');x.drawImage(img,0,0,c.width,c.height);x.drawImage(cv,0,0);const a=el('a',{href:c.toDataURL('image/png'),download:item.id+'-시안-표시.png'});document.body.append(a);a.click();a.remove();}},'표시 포함 이미지 저장'));
 return bar;}
function shrink(file,cb){const r=new FileReader();r.onload=()=>{const im=new Image();im.onload=()=>{const k=Math.min(1,1200/Math.max(im.width,im.height));const c=el('canvas');c.width=im.width*k;c.height=im.height*k;c.getContext('2d').drawImage(im,0,0,c.width,c.height);cb(c.toDataURL('image/jpeg',.82));};im.src=r.result;};r.readAsDataURL(file);}
const STATUS=[['approve','승인'],['review','다시 검토'],['hold','보류'],['reject','반려']];
function card(item){const s=S(item.id);
 const art=el('article',{id:item.id});
 const roads=(item.roads||[]).join(', ');
 art.append(el('div',{class:'head'},el('b',{class:'id'},item.id),el('span',{class:'badge b-'+(item.match||'미판정')},item.match||'미판정'),
  el('span',{class:'chip'},'구역 '+(item.zone||'?')),el('span',{class:'chip'},item.junction||''),roads?el('span',{class:'chip'},'도로 '+roads):'',
  el('span',{},item.summary||''),el('span',{style:'margin-left:auto'},el('a',{href:item.links.skyview,target:'_blank'},'카카오 항공'),' · ',el('a',{href:item.links.roadview,target:'_blank'},'카카오 로드뷰'))));
 const body=el('div',{class:'body'});
 const f1=el('figure',{},el('figcaption',{},'전 — '+(item.kind==='post'?'지금 기둥 위치(도면)':'게임 현재 (위에서 본 화면)')),el('div',{class:'stage'},el('img',{src:item.before,loading:'lazy'})));
 const img2=el('img',{src:item.after,loading:'lazy'});const st2=el('div',{class:'stage'},img2);
 const cap2=el('figcaption',{},'후 — 수정 시안 '+(item.kind==='post'?'(도면)':'(실제 항공 + 게임 차로 하늘색)'));
 if(item.real){const t=el('button',{onclick:()=>{const r=img2.dataset.real==='1';img2.src=r?item.after:item.real;img2.dataset.real=r?'0':'1';t.textContent=r?'항공사진만 보기':'시안 보기';}},'항공사진만 보기');cap2.append(t);}
 const f2=el('figure',{},cap2,st2);f2.append(attachCanvas(st2,img2,item));
 body.append(f1,f2);art.append(body);
 if(item.evidence&&item.evidence.length){const ev=el('div',{class:'body'});item.evidence.forEach(e=>ev.append(el('figure',{},el('figcaption',{},'실제 근거: '+e.caption),el('a',{href:e.src,target:'_blank'},el('img',{src:e.src,loading:'lazy',style:'width:100%;border-radius:6px'})))));art.append(ev);}
 if(item.lanes&&item.lanes.length){const t=el('table',{},el('tr',{},el('th',{},'방향'),el('th',{},'게임 차로'),el('th',{},'실제 차로'),el('th',{},'메모')));item.lanes.forEach(l=>t.append(el('tr',{},el('td',{},l.arm||''),el('td',{},l.game||''),el('td',{},l.real||''),el('td',{},l.note||''))));art.append(el('div',{class:'issues'},t));}
 const iss=el('div',{class:'issues'});
 (item.issues||[]).forEach((x,n)=>{const key=String(n+1);const sel=el('select',{},...[['','이 항목: 미검토'],['approve','승인'],['review','다시 검토'],['hold','보류'],['reject','반려']].map(([v,t])=>{const o=el('option',{value:v},t);if((s.issues[key]||'')===v)o.selected=true;return o;}));
  sel.addEventListener('change',()=>{s.issues[key]=sel.value;save();});
  iss.append(el('div',{class:'issue sev-'+x.severity},el('div',{class:'num'},key),
   el('div',{},el('div',{class:'lbl'},`${x.cat} ${CATS[x.cat]||''} · 심각도 ${x.severity} · 확신 ${x.confidence||''}${x.roadview?' · 로드뷰 확인 필요':''}${x.cells&&x.cells.length?' · 칸 '+x.cells.join(','):''}`),el('div',{},el('b',{},'전: '),x.before||'')),
   el('div',{},el('div',{class:'lbl'},'고칠 모습'),el('div',{},el('b',{},'후: '),x.after||'')),sel));});
 if(!(item.issues||[]).length)iss.append(el('div',{class:'mute',style:'padding:8px 0'},'고칠 점이 발견되지 않았습니다.'));
 art.append(iss);
 // review
 const bar=el('div',{class:'statusbar'});const bs={};
 STATUS.forEach(([k,n])=>{const b=el('button',{class:k+(s.status===k?' on':''),onclick:()=>{s.status=s.status===k?'':k;Object.values(bs).forEach(x=>x.classList.remove('on'));if(s.status)bs[s.status].classList.add('on');art.className=s.status?'st-'+s.status:'';save();stat();}},n);bs[k]=b;bar.append(b);});
 if(s.status)art.className='st-'+s.status;
 const ta=el('textarea',{placeholder:'피드백 (예: 2번은 실제로 편도 3차로가 맞음, 횡단보도는 그대로 둘 것)'});ta.value=s.note||'';ta.addEventListener('input',()=>{s.note=ta.value;save();});
 const thumbs=el('div',{class:'thumbs'});function drawThumbs(){thumbs.innerHTML='';s.attach.forEach((d,i)=>thumbs.append(el('div',{},el('a',{href:d,target:'_blank'},el('img',{src:d})),el('button',{onclick:()=>{s.attach.splice(i,1);save();drawThumbs();}},'×'))));}
 function add(files){[...files].filter(f=>f.type.startsWith('image/')).forEach(f=>shrink(f,d=>{s.attach.push(d);save();drawThumbs();}));}
 const fi=el('input',{type:'file',accept:'image/*',multiple:''});fi.addEventListener('change',()=>add(fi.files));
 const drop=el('div',{class:'drop',tabindex:'0'},'이미지 첨부: 여기로 끌어다 놓기 · 클릭 후 Ctrl+V 붙여넣기 · ',fi);
 drop.addEventListener('dragover',e=>e.preventDefault());drop.addEventListener('drop',e=>{e.preventDefault();add(e.dataTransfer.files);});
 drop.addEventListener('paste',e=>add([...e.clipboardData.items].map(i=>i.getAsFile()).filter(Boolean)));
 ta.addEventListener('paste',e=>{const f=[...e.clipboardData.items].map(i=>i.getAsFile()).filter(Boolean);if(f.length)add(f);});
 drawThumbs();
 art.append(el('div',{class:'review'},el('div',{},el('div',{class:'lbl'},'전체 판정'),bar),el('div',{},ta,drop,thumbs)));
 art.dataset.search=[item.id,item.junction,item.summary,roads,(item.issues||[]).map(x=>x.before+' '+x.after).join(' ')].join(' ').toLowerCase();
 return art;}
const list=$('#list');const cards=ITEMS.map(i=>{const c=card(i);list.append(c);return[i,c];});
function apply(){const z=$('#fZone').value,c=$('#fCat').value,m=$('#fMatch').value,st=$('#fStatus').value,t=$('#fText').value.toLowerCase();
 cards.forEach(([i,e])=>{const s=S(i.id);let ok=(!z||i.zone===z)&&(!c||(i.issues||[]).some(x=>x.cat===c))&&(!m||i.match===m)&&(!t||e.dataset.search.includes(t));
  if(st)ok=ok&&(st==='none'?!s.status:s.status===st);e.classList.toggle('hidden',!ok);});}
['#fZone','#fCat','#fMatch','#fStatus'].forEach(s=>$(s).addEventListener('change',apply));$('#fText').addEventListener('input',apply);
function stat(){const n={};ITEMS.forEach(i=>{const s=(state[i.id]||{}).status||'미검토';n[s]=(n[s]||0)+1;});$('#stat').textContent=`승인 ${n.approve||0} · 다시 검토 ${n.review||0} · 보류 ${n.hold||0} · 반려 ${n.reject||0} · 미검토 ${n['미검토']||0}`;}
stat();
$('#bExport').onclick=()=>{const out={exportedAt:new Date().toISOString(),sheet:'map-review-20261124',items:ITEMS.map(i=>({id:i.id,junction:i.junction,zone:i.zone,...(state[i.id]||{})})),questions:QUESTIONS.map(([id,t])=>({id,title:t,answer:(state[id]||{}).note||''}))};
 const a=el('a',{href:URL.createObjectURL(new Blob([JSON.stringify(out,null,1)],{type:'application/json'})),download:'map-review-feedback-'+new Date().toISOString().slice(0,10)+'.json'});document.body.append(a);a.click();a.remove();};
$('#bImport').onclick=()=>$('#importFile').click();
$('#importFile').onchange=e=>{const f=e.target.files[0];if(!f)return;f.text().then(t=>{const d=JSON.parse(t);(d.items||[]).forEach(i=>{const{id,junction,zone,...rest}=i;state[id]=rest;});(d.questions||[]).forEach(q=>{S(q.id).note=q.answer;});save();location.reload();});};
</script></body></html>'''


def main():
    items = json.loads((SHEET / 'items.json').read_text(encoding='utf-8'))
    zones = json.loads((ROOT / 'config/maps/eungye-road-zones.json').read_text(encoding='utf-8'))['zones']
    shutil.copy(ROOT / 'artifacts/map-review-20261124/overview/zones.png', SHEET / 'img/zones.png')
    from map_review_sheet import CATS
    html = (PAGE.replace('__ITEMS__', json.dumps(items, ensure_ascii=False))
            .replace('__ZONES__', json.dumps(zones, ensure_ascii=False))
            .replace('__QUESTIONS__', json.dumps(QUESTIONS, ensure_ascii=False))
            .replace('__CATS__', json.dumps(CATS, ensure_ascii=False)))
    (SHEET / 'index.html').write_text(html, encoding='utf-8')
    print(SHEET / 'index.html', len(items))


if __name__ == '__main__':
    main()
