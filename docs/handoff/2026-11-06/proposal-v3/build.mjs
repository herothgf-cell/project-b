import fs from 'node:fs/promises';
import path from 'node:path';
import {Presentation,PresentationFile} from 'file:///C:/Users/DRI/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
import {finalizePresentation} from 'file:///C:/Users/DRI/.codex/plugins/cache/openai-primary-runtime/presentations/26.1030.10813/skills/presentations/container_tools/artifact_tool_utils.mjs';
const ROOT='C:/git/project-b', TMP=ROOT+'/artifacts/proposal-v3-20261106', OUT=ROOT+'/docs/proposals';
const SKILL='C:/Users/DRI/.codex/plugins/cache/openai-primary-runtime/presentations/26.1030.10813/skills/presentations';
const p=Presentation.create({slideSize:{width:1280,height:720}});
const C={navy:'#142D46',blue:'#147CB2',yellow:'#F4BD32',ink:'#183247',muted:'#526A7B',bg:'#F9FAF6',white:'#FFFFFF'};
const font='Malgun Gothic'; p.theme.defaultFont=font; const tables=[];
function text(s,t,x,y,w,h,size=27,color=C.ink,bold=false){const a=s.shapes.add({geometry:'textbox',position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});a.text=t;a.text.style={typeface:font,fontSize:size,color,bold};return a;}
function slide(title,note='',dark=false){const s=p.slides.add();s.background.fill=dark?C.navy:C.bg;text(s,title,64,48,1152,76,43,dark?C.white:C.ink,true);text(s,String(p.slides.items.length).padStart(2,'0'),1170,665,55,28,17,dark?'#BDD0DD':C.muted);s.speakerNotes.text=note;return s;}
function caption(s,t){text(s,t,64,620,1120,40,18,C.muted);}
function sections(s,arr){arr.forEach(([h,b],i)=>{const y=158+i*137;text(s,h,64,y,325,70,29,C.blue,true);text(s,b,410,y,800,112,27);});}
function table(s,values,widths,y=155,h=405,size=24){const t=s.tables.add({rows:values.length,columns:values[0].length,left:64,top:y,width:1152,height:h,columnWidths:widths,values});t.borders.assign({fill:'#D8E2E6',width:1});for(let r=0;r<values.length;r++)for(let c=0;c<values[0].length;c++){const cell=t.getCell(r,c);cell.fill=r===0?C.navy:(r%2?C.white:'#EDF3F4');cell.text.style={typeface:font,fontSize:size,color:r===0?C.white:C.ink,bold:r===0};}tables.push(p.slides.items.length);return t;}
async function img(s,file,x,y,w,h,fit='contain'){const b=await fs.readFile(file);s.images.add({blob:b.buffer.slice(b.byteOffset,b.byteOffset+b.byteLength),contentType:'image/png',alt:path.basename(file),fit,position:{left:x,top:y,width:w,height:h}});}
const ref='C:/Users/DRI/AppData/Local/Temp/codex-clipboard-aee11604-f9d8-4e3d-aae2-cae051467e19.png';
const src='자료: docs/content-design/00~05, docs/prototype-scope.md, docs/prototype-development-checklist.md. 기준 2026-11-06. 생활/시놉시스: docs/content-design/10-restarting-driver-synopsis.md, 11-personal-life-and-growth.md, 12-career-growth-stages.md, 13-three-day-room-and-first-purchase.md. 실제 게임 구현과 제안 사항을 구분.';
const comp='공식 제품 설명 확인 2026-10-27. https://store.steampowered.com/app/1351240/Taxi_Life_A_City_Driving_Simulator/ ; https://store.steampowered.com/app/71230/Crazy_Taxi/ ; https://store.steampowered.com/app/227300/Euro_Truck_Simulator_2/ . 경쟁작의 장단점 및 적합성 평가는 본 제안의 기획적 해석이며 판매성과 비교가 아님.';

let s;
const life='자료: docs/content-design/10-restarting-driver-synopsis.md, 11-personal-life-and-growth.md, 12-career-growth-stages.md, 13-three-day-room-and-first-purchase.md. 2026-11-06 기획 제안. 작은 방/구매·차량 교체·이사는 미구현이며 단계별 검증 후 개발 심의.';
const future='마인드맵: docs/content-design/택시 시뮬레이션.mmst의 후속 구상. 제안서용 목표 설계이며 현재 미구현·개발 채택 및 수치 미확정. 기존 Proto 범위를 변경하지 않음.';
s=slide('오늘도 택시',src+' 사용자 제공 콘셉트 이미지. 가제·아트 목표이며 현재 게임 화면이 아님.',true);
text(s,'한국의 동네를 달리는\n택시 생활 시뮬레이션',64,160,540,170,42,C.white,true);
text(s,'PROJECT-B  |  게임 개발 착수 제안\n회사 내부 검토용 3차안 · 2026.11.06',64,430,540,100,24,'#D1E2ED');
await img(s,ref,660,145,555,417);text(s,'아트 방향 참고 · 현재 구현 화면 아님',660,580,555,35,18,'#D1E2ED');

s=slide('프로젝트 개요',src+' 타겟/유통은 제안 가설.');
text(s,'실직 후 시작한 택시 영업으로 생활을 다시 꾸리고,\n내가 고른 차와 돌아가고 싶은 집을 마련하는 시뮬레이션.',64,150,1140,105,36,C.blue,true);
table(s,[['항목','개발 제안'],['게임명 / 장르','오늘도 택시(가제) / 운전 시뮬레이션 + 생활·영업 관리'],['플랫폼 / 플레이','PC Windows 우선 / 싱글 중심, 경쟁 모드 기반 보유'],['핵심 타겟 가설','20~40대, 성별 제한 없음 / 운전·생활 시뮬레이션 선호층'],['플레이 규모','현재 3일 영업 / 제안: 첫 구매 체험과 장기 차량·주거 성장']],[270,882],295,280,24);

s=slide('제안 참여 인원 및 기여 역할','실제 참여자 실명·총인원·직군별 기여는 사용자 지시에 따라 회사 제출 전 확인 필요로 명시. 아래는 기여 확인 항목이며 현재 인력 배치 사실이 아님.');
table(s,[['참여 구분','이름 / 인원','제안 단계 기여 확인 항목'],['제안 책임 / 기획','회사 제출 전 확인 필요','하이 콘셉트·범위·게임 규칙·승인 요청'],['프로그래밍 / 기술 검토','회사 제출 전 확인 필요','프로토타입·기술 가능성·일정 추정'],['아트 / UI 방향 검토','회사 제출 전 확인 필요','콘셉트·대표 장면·제작 공수 검토'],['사업 / QA 검토','회사 제출 전 확인 필요','타겟·BM·비용·검증 설계']],[360,350,442],155,360,23);
caption(s,'실제 제안 참여 인원과 향후 필요 개발 인력 6명은 별개. 총 참여 인원도 회사 제출 전 확인 필요');

s=slide('핵심 재미',src+'\n'+life);
sections(s,[['운전 판단의 성취','신호·보행자·정체를 읽고 안전한 경로를 선택한다.\n동네에 익숙해지면서 내 방식의 영업을 찾는다.'],['승객마다 다른 운행','차멀미, 정시 도착, 경유 등 요청에 대응한다.\n짧은 만남 속 선택이 실제 주행과 하차 결과로 이어진다.'],['내 선택이 남는 생활','번 돈을 남겨두거나 원하는 물건과 차, 집에 쓴다.\n직접 고른 공간과 운전석을 다음 날에도 사용한다. [제안]']]);

s=slide('핵심 셀링 포인트',src+'\n'+comp);
sections(s,[['한국 동네의 일상성','Taxi Life 대비, 바르셀로나 관광 도시 경험 대신\n은계 기반 도로·한글 표지·생활시설에 집중하는 방향.'],['안전 운전과 승객 응대','Crazy Taxi 대비, 과격한 시간 경쟁보다\n안전한 운송과 약속 이행을 재미의 중심에 둔다.'],['짧은 영업의 생활 전략','Euro Truck Simulator 2 대비, 장거리 화물 운송보다\n동네 안의 짧은 운송·식사·휴식·청결 판단에 집중한다.']]);
caption(s,'차별화 방향에 대한 제안이며, 경쟁 우위의 이용자 검증은 8주 단계에서 수행');

s=slide('완성될 게임: 나만의 택시 생활',life);
sections(s,[['동네에서 일하기','호출 위치·공차·승객 요청을 읽으며 하루를 운영한다.\n영업 숙련은 더 편하게 일할 수 있는 판단으로 쌓인다.'],['돌아온 공간 바꾸기','정산 후 작은 방에서 물건을 미리 보고 구매하거나 저축한다.\n내가 고른 조명과 러그가 다음 귀가에도 남는다.'],['차와 집 직접 고르기','원하는 택시로 교체하거나 작은 아파트로 이사한다.\n순서는 자유이며 한 가지 큰 목표만 이뤄도 성취로 남긴다.']]);
caption(s,'현재 운송·관리 기반에 생활 성장을 추가하는 제안. 상품 구매·차량 교체·주거는 아직 미구현');

s=slide('코어 루프',src+'\n'+life);
text(s,'호출 선택  →  승차  →  운전·응대  →  하차·운임',64,165,1152,75,37,C.blue,true);
text(s,'다음 운송 / 시설 관리  →  마감·정산  →  다음 날',64,285,1152,75,37,C.ink,true);
text(s,'생활 확장: 정산 후 선택적으로 내 공간 방문\n물건 미리보기·구매 또는 저축 → 달라진 공간 사용',64,420,1152,110,31,C.blue);
caption(s,'생활 화면은 선택 사항. 영업 순수익과 개인 지출을 분리하고, 방 방문·구매를 다음 날의 필수 조건으로 두지 않음');

s=slide('플레이 예시: 오늘 하루의 선택',src+'\n'+life);
table(s,[['상황','내 선택','다음 플레이에 생기는 변화'],['영업 시작','수요 위치까지 이동할지 근처에서 기다릴지','공차 비용과 호출 기회 비교 [현재]'],['운행 중','차멀미 손님에게 부드러운 운전 약속','주행 방식과 만족도·운임 연결 [현재]'],['운행 사이','주유·식사·수리·휴식의 우선순위 결정','상태·비용·영업시간 변화 [현재]'],['하루 마감','조명 미리보기 후 구매 또는 찜','내 방의 변화 또는 남겨둔 현금 [제안]'],['다음 날','전날 고른 물건을 보고 영업 시작','개인 선택이 다음 영업의 동기 [검증 대상]']],[230,445,477],155,420,23);
caption(s,'목표 금액·필수 구매·출석 과제 없이 자신의 선택을 이어가는 경험');

s=slide('조작과 플레이 시점',src+'\n코드: unity/EungyeTaxi/Assets/Scripts/TaxiController.cs, DrivingCamera.cs, TaxiSignals.cs, DriveSession.cs.');
sections(s,[['직접 운전','W/S 또는 ↑/↓: 전진·후진 입력, A/D 또는 ←/→: 조향\nSpace: 제동 · Q/E: 방향지시등 · C: 1/3인칭 전환'],['보조와 상호작용','H: 조향 보조 · F: 승차·경유·하차\n보조 중에도 가감속·정지·양보는 플레이어가 수행.'],['지도와 기록','M: 전체지도·호출 · F7: 정차 시설 메뉴 · F8: 상세 기록\n키보드/패드 입력 구현. 실물 패드 검증은 별도 필요.']]);
caption(s,'첨부 이미지의 터치 페달은 아트 참고 요소이며, 모바일 개발은 이번 착수 범위에 포함하지 않음');

s=slide('주요 시스템: 운송과 교통',src);
sections(s,[['호출·수요·공차','현재 제공되는 호출 정보와 승차 위치를 보고 수락.\n지역 상대 수요와 공차/탑승 성과로 영업 위치를 판단.'],['요청과 만족도','요청 콘텐츠 17종, 운송당 0~2건 무작위 배정.\n주행 결과와 응대 이행을 반영해 실제 운임을 정산.'],['도로와 상황','신호·보행자·회전 조건, 정체·우회, 승하차 공간 점유.\n상황 안내에 따라 대기하거나 안전 대체 승하차를 진행한다.']]);

s=slide('주요 시스템: 생활과 하루 운영',src+'\n'+life);
sections(s,[['일하는 동안의 관리','연료·청결·포만감·피로를 보며 주유·세차·식사·수면.\n사고와 차량 손상은 처리비·수리·견인으로 연결된다. [현재]'],['일을 마친 뒤의 생활','정산에서 다음 날 또는 내 공간을 선택한다.\n방에서 물건을 살펴보고 구매·배치·보관한다. [제안]'],['내 생활의 기록','영업 수입·비용·순수익과 개인 소비·남은 현금을 구분한다.\n물건을 샀어도 이미 끝난 영업 성적은 바뀌지 않는다. [제안]']]);
caption(s,'생활 화면에서는 영업시간·허기·피로가 진행하지 않으며, 추가 회복 보너스도 제공하지 않음');

s=slide('차량 성장: 내가 고른 운전석',life);
sections(s,[['원하는 차 비교','첫 장기 범위는 큰 차량 교체 1회, 외관·실내 취향 후보 2개.\n실제 운전 시점의 미리보기로 매일 타고 싶은 차를 고른다.'],['견적 확인과 교체','새 차 가격·현재 차 매도액·최종 지출·남는 현금을 확인한다.\n하루 사이에 교체하고 다음 영업에서 직접 운전한다.'],['소유의 만족','기본 주행·요금 체계를 공유하고 외형과 실내로 차이를 준다.\n호환 장식은 옮기고 나머지는 보관한다. 영업 차량은 한 대다.']]);
caption(s,'장기 미구현 제안. 차종 성능 등급·추가 수익 배율·할부·다중 차고는 첫 범위에 포함하지 않음');

s=slide('첫 3일 생활 체험: 작은 방과 첫 구매',life);
table(s,[['시점','플레이 경험','선택이 남기는 것'],['첫날','영업 후 조명·러그를 방에 미리 놓아본다','구매·찜·저축 또는 바로 다음 날'],['둘째 날','어제의 방을 다시 보고 스타일을 바꾼다','내 공간이 선택을 기억하는 경험'],['셋째 날','마지막 정산 후 처음과 지금의 방을 비교한다','실제 상태를 회차 기록으로 보존'],['최소 제작 범위','고정 시점 방 1개 / 조명·러그 각 2스타일','종류별 1회 구매, 고정 위치에 배치']],[230,490,432],155,380,23);
caption(s,'미구현 검증안. 구매 없이 끝내도 정상이며 차량 교체·이사는 3일 안에 압축하지 않음');

s=slide('도시의 변화: 같은 길에서도 다른 판단',src+'\n'+life);
sections(s,[['현재의 운송 상황','시간대 수요·신호·정체·승객 요청에 맞춰 운행한다.\n사고 처리와 공업사 복귀까지 하루 운영에 포함한다.'],['장기 영업의 변주','날짜별 수요 구성을 달리하는 확장안을 검토한다.\n공차 이동·현 위치 대기·시설 동선을 매일 비교하게 한다.'],['개인 목표와의 연결','차 또는 집을 목표로 정해도 운임·요청을 특별 보정하지 않는다.\n운전의 재미를 유지하면서 원하는 것에 쓸 돈을 남긴다.']]);
caption(s,'날짜별 수요 변주는 신규 제안. 날씨·보험·차내 장치 조작은 별도 검토이며 생활 검증 범위 밖');

s=slide('경제와 생활: 번 돈을 어디에 쓸 것인가',life);
sections(s,[['영업과 개인 소비','주유·식사·세차·수리 등의 영업 비용을 정산한다.\n그 후 물건을 사면 현금은 줄지만 영업 순수익은 유지한다.'],['지금 구매 또는 저축','물건을 미리 보고 살지, 차량·이사를 위해 남길지 선택한다.\n개인 구매는 정산 후만 가능하며 미납은 먼저 상환한다.'],['부담 없는 개인 생활','집 방문·꾸미기·구매는 건너뛸 수 있고 물건은 계속 판매한다.\n청소·요리·매일 월세·퇴거 위협·회복 보너스는 넣지 않는다.']]);
caption(s,'가격은 실제 영업 수익 관찰 후 조정. 개인 소비에 자동 대출·할부를 붙이지 않음');

s=slide('콘텐츠 라인업과 플레이 방식',src+'\n'+life);
table(s,[['콘텐츠','플레이 경험','현재 / 개발 제안'],['은계 싱글','운송·요청·교통·시설·사고·정산','현재 3일 영업 기반'],['개인 생활','작은 방·물건 미리보기·구매·배치','첫 3일 검증안 / 미구현'],['장기 커리어','연속 영업·차량 교체·첫 이사','생활 동기 검증 후 별도 개발'],['연습·회고','상황 재도전·운전 및 영업 기록 비교','현재 연습·기록 기반'],['경쟁 모드','자체 도시에서 수익·운전 점수 경쟁','통합 실행본의 PVP·LAN 기반 보유']],[240,475,437],155,420,23);
caption(s,'생활·주거 성장안은 싱글 대상. PVP에 개인 소비와 방 상태를 적용하지 않음');

s=slide('세계관과 시놉시스: 다시 시작하는 기사',life);
text(s,'남의 목적지로 운전하는 일을 통해,\n자신의 생활을 다시 선택하게 되는 사람의 이야기.',64,145,1152,110,35,C.blue,true);
text(s,'출발',64,300,300,45,29,C.blue,true);
text(s,'경영 악화로 실직한 주인공이 준비를 거쳐 택시 일을 시작한다.\n정상 운행 가능한 중고 택시와 작은 임대 방에서 첫날을 맞는다.',410,300,800,110,27);
text(s,'변화와 성취',64,455,300,45,29,C.blue,true);
text(s,'짧게 만나는 손님들을 모시며 내 방식의 영업을 익힌다.\n원하는 차나 집을 고르고, 처음 산 물건을 새 공간으로 가져간다.',410,455,800,110,27);
caption(s,'현대 한국의 은계 기반 생활권. 주인공 나이·성별·가족은 고정하지 않으며 반복 인물·관계 성장 축은 제외');

s=slide('아트 콘셉트: 밝고 친근한 한국 도시','사용자 제공 이미지 원본을 아트 참고로 사용. 이미지 속 서울역·남산·강남은 은계 구현 공간과 다르며 게임 화면/기능 증빙이 아님.');
await img(s,ref,64,142,735,505);
text(s,'채택할 방향',840,160,360,48,31,C.blue,true);
text(s,'스타일라이즈드 3D 카툰\n둥근 차량·표정이 읽히는 인물\n따뜻한 햇빛·녹색 가로수\n한글 상점·생활감 있는 거리\n고대비 내비·짧은 결과 피드백',840,236,360,235,26);
text(s,'개발 목표 이미지\n현재 빌드와 시각 품질 차이 존재\n서울 랜드마크 도입은 미확정',840,505,360,105,22,C.muted);

s=slide('생활 거점 시안: 운행 사이의 기사 생활','내장 image_gen 생성 예시. 사용자 콘셉트의 따뜻한 한국 도시 방향을 바탕으로 제작. 실제 은계 지도·현재 게임 화면이 아니며 실내 보행 구현을 뜻하지 않음.');
await img(s,ROOT+'/docs/proposals/assets/taxi-life-concept-v2.png',64,145,1152,442);
caption(s,'개발 목표 시안 · 기사 식당에서 식사 → 주유·세차 → 그늘진 거점에서 휴식. 실제 지도 배치와는 다름');

s=slide('차량 아트 시안: 내 취향으로 고르는 한 대','내장 image_gen 생성 예시. 차량 외형 후보 분위기 참고. 다중 차고나 성능별 차종 제공을 약속하지 않음. 실제 게임 화면·확정 차종·완성 에셋이 아님.');
await img(s,ROOT+'/docs/proposals/assets/taxi-garage-concept-v2.png',64,145,1152,442);
caption(s,'외형 후보 참고 시안 · 초기 차량 교체는 취향 비교 중심. 이미지의 다중 차고·차종별 성능은 개발 범위가 아님');

s=slide('UI/UX 방향',src+' 레이아웃 정제는 제안 사항.');
sections(s,[['운전 중에는 도로 우선','속도·다음 회전·주요 위험만 빠르게 읽는다.\n상태 게이지는 구분하고 요청 응답은 안전 정차에서 제공.'],['탐색과 결정을 분리','지도에서 호출·경로·시설을 비교한 뒤 명시적으로 확정.\n주행 화면에 긴 계산식과 여러 팝업을 동시에 노출하지 않는다.'],['결과는 이유까지 표시','하차 직후 예상 운임·조정·실수령을 짧게 보여 준다.\n왜 감점·변동했는지는 상세 기록에서 확인한다.']]);

s=slide('경쟁작과 벤치마킹',comp);
table(s,[['레퍼런스','강점 / 한계 (기획 관점)','벤치마킹 포인트'],['Taxi Life','강점: 도시 택시·회사 운영\n한계: 한국 동네 공감과 다른 배경','호출→운행→결과의 명료성\n도시 속 운전자 역할'],['Crazy Taxi','강점: 즉각적 운송·시간 경쟁\n한계: 생활·안전 중심 목표와 거리','짧은 운행의 동기와 피드백\n※ 판매 중단된 역사적 레퍼런스'],['Euro Truck Simulator 2','강점: 장거리 운송·사업 성장\n한계: 짧은 승객 응대와 다른 호흡','안정적인 운전 감각\n운송 후 수익·성장 피드백']],[270,470,412],155,380,23);
caption(s,'공식 제품 설명 기반 기획적 비교. 경쟁작 기능 부재·시장점유율·판매량을 주장하는 자료가 아님');

s=slide('시장 진입 가설과 사업 모델',comp+' BM·마케팅은 제안이며 승인/매출 전망이 아님.');
sections(s,[['고객 가설','PC 운전·생활 시뮬레이션 이용자 중, 한국 동네의 일상과\n짧은 영업 단위에 흥미를 느끼는 층을 우선 검증한다.'],['수익 모델 제안','PC 유료 패키지 우선. 데모 반응과 제작비를 보고 가격 결정.\n지역·시나리오 확장은 본편 반복 재미 확인 후 검토한다.'],['유입 전략 제안','짧은 운행 영상과 체험 데모 → 타겟 플레이테스트 →\n커뮤니티 피드백·위시리스트 전환 측정으로 수요 확인.']]);
caption(s,'시장 규모·판매량·매출 목표는 근거 확보 전 미기재. 매출 전망은 가격·비용·전환 실측 후 산정');

s=slide('8주 검증 개발과 착수 판단 기준',life+' 8주·20명·정량 기준은 회사 심의용 제안. 생활 4~6명은 문제/동기 관찰용이며 통계적 성공률 추정이 아님.');
table(s,[['검증 질문','측정 설계','다음 단계 판단안'],['처음부터 운전 가능한가','타겟 20명 이상, 첫 운송 관찰','도움 없이 첫 운송 완료 ≥80%'],['한 번 더 하고 싶은가','20분 플레이 후 재플레이 의향','5점 척도 4~5점 응답 ≥70%'],['생활 선택이 영업 동기가 되는가','우선 4~6명, 구매 지시 없이 3일 관찰','선택 이유·구매 후 사용·다음 영업 관찰'],['소비와 기록이 안전한가','개인 거래·잔액·저장·완료 기록 검사','중복 구매·성적 변동·저장 손상 0건']],[320,430,402],155,380,23);
caption(s,'작은 구매의 동기와 큰 차·집의 매력은 별도 검증. 차량 운전석·주거 미리보기 평가 후 장기 제작 판단');

s=slide('기능 우선순위와 확장 경계',src+'\n'+life);
table(s,[['단계','우선 기능','범위·판단 기준'],['확보한 기반','운송·관리·사고/수리·3일·PVP','현재 빌드와 기존 검증 기록'],['첫 생활 검증','방 1개·조명/러그·개인 거래·저장','자발적 선택과 다음 영업의 동기 관찰'],['생활 확장 후보','화분·차량 소품·실내 색','첫 검증 뒤 한 방향씩 채택'],['장기 커리어 제안','연속 영업·차량 교체 1회·첫 이사','후보 매력·가격·소유 이월 검증']],[280,475,397],155,380,23);
caption(s,'차량/집 구매 순서 자유. 날씨·보험·금융·실제 차내 장치 조작은 이번 생활 검증과 분리');

s=slide('개발 로드맵 — 착수 승인 후 12개월 가정',src+' 핵심 6명+공유 인력, 1개 지역 우선의 일정 가정. 확정 일정 아님. M은 승인 후 월.');
table(s,[['단계','일정 가정','산출물 / 다음 단계 조건'],['프로토타입 고도화','M1~M2 · 8주','운송 안정화·작은 방/첫 구매 / 자발적 동기 관찰'],['알파','M3~M6 · 4개월','연속 영업·차량/주거 대표 흐름 / 채택 범위 전체 진행'],['베타','M7~M9 · 3개월','경제·장기 동기·호환성·성능 / 주요 결함 해소'],['출시 준비·정식 출시','M10~M12 · 3개월','QA·현지화·스토어·운영 준비 / 출시 심의 통과']],[320,270,562],155,380,23);
caption(s,'확장 범위는 M2 심의에서 확정하고 일정·인월 재산정. 글로벌 동시 출시는 현지화·지원 조건 확정 후 결정');

s=slide('개발 조직과 비용 산정 방식','인원·기간·MM은 제안 가정. MM=인월. 회사 표준 단가·공유 인력·외주 금액 미확인.');
table(s,[['직무','필요 인원안','주요 책임'],['기획 / PM','1명','범위·경제·승객 콘텐츠·검증 운영'],['프로그래밍','2명','운전·교통 / 영업·UI·저장·도구'],['아트','3명','환경 1 / 차량·캐릭터 1 / UI·TA 1'],['QA·사운드·사업','공유 또는 외주','호환성·음향·스토어·현지화 지원']],[330,210,612],150,340,24);
text(s,'8주: 핵심 6명 × 2개월 ≈ 12인월\n12개월: 핵심 6명 유지 시 72인월 + 공유·외주 비용',64,520,1130,85,28,C.blue,true);
caption(s,'승인 예산 = 직무별 투입 인월 × 회사 표준 단가 + 외주·장비·유통비 + 회사 기준 예비비');

s=slide('주요 리스크와 대응',src);
table(s,[['리스크','개발 대응','판단 시점'],['시뮬레이션이 피로해질 가능성','주행·생활 부담을 분리 관찰, 빈도·비용 조정','8주 이용자 테스트'],['콘셉트와 현 빌드의 품질 격차','대표 거리·택시·승객 한 세트로 제작비 검증','알파 착수 전'],['도로·교통·판정의 복잡성','은계 한 지역 우선, 차량·주거 확장은 M2 채택 심의','프로토타입~베타'],['시장 반응·사업성 미확인','데모 반응 및 전환 측정 후 가격·판매 목표 산정','본개발/출시 심의'],['배포·자료 이용 조건','지도·아트·폰트·음원 출처와 이용 조건 점검','외부 데모 공개 전']],[350,570,232],155,420,23);

s=slide('프로토타입 화면 예시와 아트 제작 과제',src+'\n화면: artifacts/nav-tip-signs.png, 2026-10-27 개발 검사 캡처. 최신 화면 증빙이 아님. 이번 제안서 작업 중 새 플레이를 수행한 화면이 아님.');
await img(s,ROOT+'/artifacts/nav-tip-signs.png',64,155,820,461);
text(s,'기반 기능',925,165,290,50,29,C.blue,true);text(s,'실제 도로 연결\n교통·신호·내비\n영업 HUD',925,225,290,145,25);
text(s,'추가 제작',925,400,290,50,29,C.blue,true);text(s,'환경·차량·인물 아트\n조명·애니메이션\nUI 정리와 최적화',925,460,290,145,25);

s=slide('프로토타입 구현 범위와 개발 과제',src+'\n기존 검증: docs/work-history/2026-11-06-independent-choice-effects.md 및 LATEST. 본 제안서 수정에서 게임 테스트 재실행 없음.');
sections(s,[['현재 구현 기반','은계 싱글 3일 영업·승객 요청·시설·피로·사고·수리·저장.\n통합 메뉴에서 자체 도시 PVP와 LAN 경쟁도 제공한다.'],['새로 개발할 생활','작은 방·상품 구매/배치·개인 소비 회계·완료 공간 기록.\n장기 차량 교체·이사는 연속 경력과 소유 저장까지 추가한다.'],['출시 판단 전 확인','자동·물리 검사의 기존 기록과 사람의 재미 평가는 구분한다.\n전체 3일 생활 체감·장기 소유 동기·다른 PC 수용을 확인한다.']]);
caption(s,'실행: builds/EungyeTaxi/EungyeTaxi.exe / PrototypeBuild.BuildHybrid / 본 PPT에는 실행 파일 미포함');

s=slide('시연 순서와 조작 요약',src+' 별도 문서: docs/proposals/project-b-prototype-manual-v3.md.');
text(s,'① 시작·호출 비교  →  ② 승차·운행  →  ③ 요청 대응',64,165,1150,75,34,C.blue,true);
text(s,'④ 하차·운임 확인  →  ⑤ 시설 관리  →  ⑥ 마감·회고',64,275,1150,75,34,C.ink,true);
text(s,'WASD / 방향키: 운전     Space: 제동     C: 시점\nM: 지도·호출     F: 승하차     F7: 시설     F8: 기록\nEsc: 일시정지     H: 조향 보조',64,415,1150,150,29);
caption(s,'정차 후 상호작용. 별도 Manual 문서에 실행 준비·실수하기 쉬운 조건·전체 조작을 수록');

s=slide('의사결정 요청','본 페이지의 인력·기간·검증 게이트는 제안. 승인 여부는 회사 결정.',true);
text(s,'8주 동안 검증할 것',64,165,1150,70,38,C.yellow,true);
text(s,'운전이 재미있는가\n내 공간·원하는 차와 집이 다음 영업의 동기가 되는가\n목표 아트를 감당 가능한 공수로 제작할 수 있는가',64,265,1150,205,36,C.white);
text(s,'요청: 책임자 지정 · 핵심 인력 6명 배정 · 8주 예산 한도 승인\n결과: 이용자 테스트와 대표 빌드로 본개발 투자 여부 재심의',64,540,1150,95,26,'#D1E2ED');
await fs.mkdir(TMP+'/renders',{recursive:true});
await (await PresentationFile.exportPptx(p)).save(TMP+'/candidate.pptx');
for(let i=0;i<p.slides.items.length;i++){const b=await p.export({slide:p.slides.items[i],format:'png',scale:1});await fs.writeFile(TMP+'/renders/'+String(i+1).padStart(2,'0')+'.png',new Uint8Array(await b.arrayBuffer()));console.log('render',i+1);}
await fs.writeFile(TMP+'/outline.json',JSON.stringify((await p.inspect({kind:'slide,textbox,table',maxChars:1000000})),null,2));
const result=await finalizePresentation({workspaceDir:ROOT,candidatePath:TMP+'/candidate.pptx',finalPath:OUT+'/project-b-game-proposal-v3.pptx',pythonExecutable:'C:/Users/DRI/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe',integrityValidatorPath:SKILL+'/container_tools/inspect_presentation_package_integrity.py',layoutValidatorPath:SKILL+'/container_tools/inspect_presentation_layout_geometry.py',layoutArgs:['--expected-slide-size-emu','12192000,6858000','--validate-bullet-geometry','--validate-heading-fit',...tables.flatMap(n=>['--require-native-table-slide',String(n)])],explicitTotalSlideCount:32,requiredNativeTableOwnerSlides:tables,fontPolicy:{basis:'design',families:[font]},verifyArtifactToolImport:true,receiptPath:TMP+'/validation.json'});
console.log(JSON.stringify(result));
