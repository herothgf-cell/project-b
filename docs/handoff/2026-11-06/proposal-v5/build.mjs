import fs from 'node:fs/promises';
import path from 'node:path';
import {Presentation,PresentationFile} from 'file:///C:/Users/DRI/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
import {finalizePresentation} from 'file:///C:/Users/DRI/.codex/plugins/cache/openai-primary-runtime/presentations/26.1030.10813/skills/presentations/container_tools/artifact_tool_utils.mjs';
const ROOT='C:/git/project-b', TMP=ROOT+'/artifacts/proposal-v5-20261106', OUT=ROOT+'/docs/proposals';
const SKILL='C:/Users/DRI/.codex/plugins/cache/openai-primary-runtime/presentations/26.1030.10813/skills/presentations';
const p=Presentation.create({slideSize:{width:1280,height:720}}),font='Malgun Gothic',tables=[];
p.theme.defaultFont=font;
const C={navy:'#142D46',blue:'#147CB2',yellow:'#F4BD32',ink:'#183247',muted:'#526A7B',bg:'#F9FAF6',white:'#FFFFFF'};
function text(s,t,x,y,w,h,size=27,color=C.ink,bold=false){const a=s.shapes.add({geometry:'textbox',position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});a.text=t;a.text.style={typeface:font,fontSize:size,color,bold};return a;}
function slide(title,note='',dark=false){const s=p.slides.add();s.background.fill=dark?C.navy:C.bg;text(s,title,56,40,1168,72,42,dark?C.white:C.ink,true);text(s,String(p.slides.items.length).padStart(2,'0'),1170,672,55,27,16,dark?'#BDD0DD':C.muted);s.speakerNotes.text=note;return s;}
function cap(s,t){text(s,t,56,638,1135,30,17,C.muted);}
async function img(s,file,x,y,w,h,fit='contain'){const b=await fs.readFile(file);s.images.add({blob:b.buffer.slice(b.byteOffset,b.byteOffset+b.byteLength),contentType:'image/png',alt:path.basename(file),fit,position:{left:x,top:y,width:w,height:h}});}
function table(s,values,widths,y=170,h=380,size=25){const t=s.tables.add({rows:values.length,columns:values[0].length,left:56,top:y,width:1168,height:h,columnWidths:widths,values});t.borders.assign({fill:'#D8E2E6',width:1});for(let r=0;r<values.length;r++)for(let c=0;c<values[0].length;c++){const cell=t.getCell(r,c);cell.fill=r===0?C.navy:(r%2?C.white:'#EDF3F4');cell.text.style={typeface:font,fontSize:size,color:r===0?C.white:C.ink,bold:r===0};}tables.push(p.slides.items.length);}
function rows(s,items){items.forEach(([a,b],i)=>{text(s,a,56,170+i*135,340,60,31,C.blue,true);text(s,b,430,170+i*135,780,100,29);});}
async function visual(title,file,lines,note,foot='생성 콘셉트 시안 · 실제 게임 화면 아님'){let s=slide(title,note);await img(s,file,56,128,1168,430);lines.forEach((line,i)=>text(s,line,56+i*(1168/lines.length),575,1168/lines.length-12,54,25,C.ink,true));cap(s,foot);return s;}
const A=OUT+'/assets/v4/';
const src='기준 2026-11-06. docs/content-design/00~05, prototype-scope.md, prototype-development-checklist.md 및 기존 배포 이력. 이번 PPT 작업은 게임 실행/개발 검증을 수행하지 않음.';
const life='자료: docs/content-design/10-restarting-driver-synopsis.md, 11-personal-life-and-growth.md, 12-career-growth-stages.md, 13-three-day-room-and-first-purchase.md, 14-personal-life-design-guide.md. 작은 방/첫 구매 핵심은 2026-11-06-personal-room-implementation.md의 구현 기록을 반영. 처음방 비교/전체 과거방 탐색은 후속. 미커밋 동시 변경은 배포 완료로 간주하지 않음. 첫 검증은 방1개/조명·러그2종 각2스타일. 장기 교체/이사는 별도. 실명 인물·실제 지역 재현이 아님.';
const art='이미지: docs/proposals/assets/v4/prompts.md. 내장 image_gen 신규 생성. 사용자 제공 밝은 한국 택시 도시 콘셉트를 텍스트로 기술한 예시이며 완성 게임/확정 에셋이 아님. 서울 타워는 분위기용이고 은계 랜드마크 구현 약속이 아님.';
const comp='기존 제안서 공식 제품 설명 확인 기준 2026-10-27. https://store.steampowered.com/app/1351240/Taxi_Life_A_City_Driving_Simulator/ ; https://store.steampowered.com/app/71230/Crazy_Taxi/ ; https://store.steampowered.com/app/227300/Euro_Truck_Simulator_2/ . 장단점은 본 게임의 타겟 적합성 관점, 판매성과나 기능 부재 주장 아님.';
let s;
s=slide('오늘도 택시',src+' 가제. 기존 생성 시안 taxi-life-concept-v2.png, 출처 assets/concept-prompts-v2.md.',true);
text(s,'동네를 달리고,\n내 생활을 꾸리는\n택시 시뮬레이션',56,160,495,220,43,C.white,true);
await img(s,OUT+'/assets/taxi-life-concept-v2.png',570,125,655,440,'cover');
text(s,'PROJECT-B\n게임 개발 착수 제안 · 5차\n2026.11.06',56,493,495,110,25,'#D1E2ED');
text(s,'개발 목표 시안',590,585,600,30,18,'#D1E2ED');

s=slide('어떤 게임인가',src+'\n'+life+' 사용자 첨부 원본 이미지. 이미지의 터치 입력/서울 지역은 개발 확정 범위가 아님.');
text(s,'손님을 모시고 번 돈으로\n내가 원하는 차와 집을\n마련하는 게임',56,150,440,150,35,C.blue,true);
text(s,'장르  운전 + 생활 시뮬레이션\n플랫폼  PC Windows 우선\n핵심 모드  싱글 커리어',56,355,440,130,25);
text(s,'타겟 가설\n20~40대 · 성별 제한 없음\n운전·생활 시뮬레이션 선호층',56,510,440,108,24);
await img(s,'C:/Users/DRI/AppData/Local/Temp/codex-clipboard-aee11604-f9d8-4e3d-aae2-cae051467e19.png',530,130,695,505);
cap(s,'첨부 이미지: 아트·HUD 방향 참고 / 현재 은계 기반 프로토타입과 구분');

await visual('이야기: 실직 후 시작하는 새로운 하루',A+'restart-story.png',['작은 방과 중고 택시에서 출발','손님을 모시며 영업을 익힌다','내가 고른 생활을 만든다'],life+'\n'+art+' 경영 악화로 실직 후 준비를 거쳐 첫 영업 시작. 강제 사고/빚/퇴거를 넣지 않음. 주인공 나이·성별·가족은 미정이며 그림의 외형은 예시. 승객은 매번 짧게 만나는 일상 인물.','현대 한국의 은계 기반 생활권 / 실직 후 재기하는 기사와 짧게 만나는 일상 승객 / 생성 시안');

s=slide('코어 루프와 핵심 재미',src+'\n'+life);
const steps=[['01','손님 선택','위치와 공차 판단'],['02','운전·응대','안전과 요청 이행'],['03','정산·관리','수입과 비용 확인'],['04','내 생활 선택','구매 또는 저축']];
steps.forEach(([n,t,b],i)=>{let x=56+i*295;text(s,n,x,175,260,75,55,C.blue,true);text(s,t,x,280,275,65,34,C.ink,true);text(s,b,x,365,275,60,25);});
text(s,'선택한 공간과 차를 사용하며 다음 영업을 시작한다',56,510,1168,70,35,C.blue,true);
cap(s,'운전 판단의 성취 + 승객마다 다른 상황 + 내 선택이 남는 생활 / 개인 소비 핵심 구현 / 장기 차량·주거는 제안');


s=slide('승객 요청: 말에 답하고 운행으로 보여준다',src+' docs/content-design/02-passengers-and-fares.md, docs/content-tuning-request-outcomes.md. 승객 유형과 개별 요청은 별개. 유형 확인은 대표 요청의 수락과 다르며 실제 약속 관찰과 단발 응대 완료를 구분.');
table(s,[['승객의 말·상태','플레이어의 대응','운행 중·하차 반응'],['“부드럽게 부탁해요”','요청 수락 후 가감속·회전 조절','승차감 관찰과 요청 결과 반영'],['“조용히 가고 싶어요”','조용한 응대 선택','일반 후속 수다 억제·배려 반응'],['취객의 이야기','짧게 확인하거나 조용히 안내','선택을 기억한 대사·감사'],['잠든 승객','수면 배려·목적지 정차 후 깨우기','깨어난 뒤 안내·배려에 감사']],[350,400,418],160,380,25);
text(s,'요청 확인 → 내 선택 → 상태·대사 변화 → 하차 피드백',56,570,1168,55,31,C.blue,true);
cap(s,'현재 기반의 요약 / 모든 응답이 실제 행동 검증은 아님 / 표정·연출은 아래 목표 시안');

s=slide('취객: 짧은 응대에도 달라지는 반응',src+' docs/content-design/02-passengers-and-fares.md 취객 변주/하차. 이미지 assets/v5/drunk-passenger.png, 내장 image_gen. 대표 말은 기획 예시. 공통 확인/집중 안내는 실제 유형 버튼, 대표 요청 수락과 별개. 실제 구토 시 운송 유지·신규 호출 잠금, 하차 뒤 청결 저하·세차. 구토/수면은 조건부이며 매회 강제하지 않음.');
await img(s,OUT+'/assets/v5/drunk-passenger.png',56,145,850,478);
text(s,'승객',942,160,282,42,28,C.blue,true);
text(s,'“조금 취했네요.\n폐를 끼치면 미안해요.”',942,210,282,90,25);
text(s,'내 선택',942,325,282,42,28,C.blue,true);
text(s,'짧게 확인·설명\n또는 조용히 안내',942,375,282,85,26);
text(s,'운송 뒤',942,490,282,42,28,C.blue,true);
text(s,'배려를 기억한 감사\n오염 발생 시 세차',942,540,282,75,26);
cap(s,'생성 반응 시안 / 실제 구토 시에만 청결 관리로 연결 / 취객이라고 매번 사건이 발생하지 않음');

s=slide('잠자는 승객: 조용한 운행과 도착 후 배려',src+' docs/content-design/02-passengers-and-fares.md 잠자는 승객4변주. 실제 PassengerJobs.BehaviorAsleep/ShowWakeWait/CompleteDelivery 확인. 잠자는 승객은 비취객, 수면 대화미응답 중립. 기존 수락 약속은 유지. 첫 유효 도착 운임 확정, 기상15초 후F재호출/30초 안전정차 자동하차 정책. 이미지 assets/v5/sleeping-passenger.png 생성 시안.');
await img(s,OUT+'/assets/v5/sleeping-passenger.png',56,145,850,478);
text(s,'승객',942,160,282,42,28,C.blue,true);
text(s,'“도착하면\n깨워 주세요.”',942,210,282,90,26);
text(s,'내 대응',942,325,282,42,28,C.blue,true);
text(s,'조용히 운행\n안전 정차 후 깨우기',942,375,282,85,26);
text(s,'운송 뒤',942,490,282,42,28,C.blue,true);
text(s,'안내와 감사 반응\n기상 대기 후 하차',942,540,282,75,26);
cap(s,'생성 반응 시안 / 수면 중 대답 없음은 감점하지 않음 / 깨우는 시간은 지각·추가요금으로 소급하지 않음');

s=slide('운행 사이에도 판단이 있는 생활',src+' 기존 생성 시안 taxi-garage-concept-v2.png. 차고 이미지는 차량 관리 분위기 예시이며 차종/다중 차고 약속 아님.');
await img(s,OUT+'/assets/taxi-garage-concept-v2.png',56,135,805,460);
text(s,'차량 상태',900,155,310,50,31,C.blue,true);text(s,'주유 · 세차\n사고 처리 · 수리',900,218,310,90,29);
text(s,'기사 상태',900,345,310,50,31,C.blue,true);text(s,'식사 · 휴식\n한 번 더 운행할까?',900,410,310,100,29);
cap(s,'시설 선택은 비용·시간·컨디션에 영향 / 이미지의 차종 판매는 현재 구현 증빙이 아님');

await visual('작은 방 꾸미기: 첫 구매가 눈에 남는다',A+'room.png',['미리 놓아보고','마음에 들면 구매','다음 귀가에도 그대로'],life+'\n'+art+' 고정 시점 2D 방과 고정 슬롯. 조명/러그만 첫 검증, 종류별1회 구매 후2스타일 무료 전환. 보관/배치 무료. 이미지의 이동 아이콘은 자유배치 기능 약속이 아님. 방 방문과 구매 선택, 능력 보너스 없음.','목표 아트 시안 · 방·첫 구매 핵심 구현 / 조명·러그 각 2스타일 / 고정 위치 배치');

s=slide('첫 3일: 고르고, 사용하고, 기록한다',life+' 기존 생성 목업: docs/content-design/images/three-day-room-mockup.png. 화면 속 금액은 설명용 예시/시험가격이며 확정 경제가 아님. 구매는 정산 뒤, 영업 순수익과 개인 지출 분리.');
await img(s,ROOT+'/docs/content-design/images/three-day-room-mockup.png',56,125,545,500);
text(s,'1일차  원하는 물건 고르기',650,170,565,55,31,C.blue,true);text(s,'구매해도, 찜만 해도 괜찮다.',650,232,565,55,27);
text(s,'2일차  어제의 선택 사용하기',650,330,565,55,31,C.blue,true);text(s,'스타일을 바꾸거나 다음 영업으로.',650,392,565,55,27);
text(s,'3일차  달라진 방 남기기',650,490,565,55,31,C.blue,true);text(s,'산 물건이 없어도 정상적인 마무리.',650,552,565,55,27);
cap(s,'기획 목업 · 핵심 구매/저장 구현, 처음 방 비교는 후속 / 3일 체험과 장기 소유 이월 구분');

await visual('차량 구매: 매일 타고 싶은 운전석',A+'taxi-purchase.png',['외관·실내 취향 비교','견적과 남는 현금 확인','다음 영업에서 직접 운전'],life+'\n'+art+' 장기 첫 범위는 교체1회, 후보2개, 동일 기본 주행/요금. 호환 장식 이동/비호환 보관. 영업차1대. 견적은 신차가·구차매도액·차액·잔액. 현금/미납 정책 검증 필요. 금융/성능단계/다중차고 제외.','장기 개발 제안 · 밝은 실내 / 차분한 실내 예시 / 추가 운임 배율 없이 소유 만족 검증');

await visual('이사: 좋아하던 물건을 새집으로',A+'moving-home.png',['작은 방에서','내가 고른 채광과 전망으로','처음 산 물건도 함께'],life+'\n'+art+' 작은 방에서 작은 아파트로 첫 이사1회, 후보2개 취향. 일회성 주거전환 비용이며 현실 부동산 가격 재현 아님. 물건은 보관 후 호환 슬롯 배치. 차와 집 순서 자유, 둘다 사야 성공인 구조 없음.','장기 개발 제안 · 차와 집의 구매 순서는 자유 / 넓어진 공간은 수익·수면 보너스를 주지 않음');

s=slide('기능의 범위: 현재 기반과 다음 개발',src+'\n'+life);
table(s,[['영역','플레이어가 하는 일','단계'],['운송·교통','호출·안전 운전·요청 응대','현재 기반'],['영업·관리','식사·주유·수리·마감·저장','현재 기반'],['개인 생활','방·미리보기·구매·배치','핵심 구현·체감 검증'],['장기 성장','연속 영업·차량 교체·첫 이사','후속 개발 제안']],[260,620,288],160,390,26);
cap(s,'개인 지출과 영업 순수익 분리 / 의무 집안일·관계 성장·자동 대출은 범위 밖');

s=slide('콘텐츠 라인업',src+' PVP 참고 캡처 docs/content-design/pvp/previews/26-native-city-drive.png. 현재 그래픽 수준 참고. 생활안은 PVP 미적용.');
text(s,'싱글 커리어',56,160,430,55,33,C.blue,true);text(s,'현재 3일 영업 기반\n생활·차량·주거로 확장 제안',56,225,430,100,28);
text(s,'경쟁과 연습',56,380,430,55,33,C.blue,true);text(s,'자체 도시 PVP · LAN 기반\n상황 연습 · 운전 기록',56,445,430,100,28);
await img(s,ROOT+'/docs/content-design/pvp/previews/26-native-city-drive.png',520,155,705,420);
cap(s,'우측: 기존 PVP 개발 캡처 / 개인 생활은 싱글 대상 / 협동·장편 스토리 모드는 미확정');

s=slide('조작과 시점',src+' 기존 캡처 docs/ui/2026-11-05-hud-preparation-navigation/08-pvp-cockpit.png. 코드 TaxiController/DrivingCamera/TaxiSignals. 자세한 사용법 project-b-prototype-manual-v3.md.');
await img(s,ROOT+'/docs/ui/2026-11-05-hud-preparation-navigation/08-pvp-cockpit.png',56,150,790,450);
text(s,'1인칭 / 3인칭',890,160,330,55,32,C.blue,true);
text(s,'WASD  운전\nSpace  제동\nC  시점 전환\nM  지도·호출\nF  승하차\nF7  시설 메뉴',890,245,330,290,27);
cap(s,'키보드 중심 PC 시연 / H 조향 보조, 가감속·정차는 직접 수행 / 패드 수용은 별도 검증');

s=slide('아트와 화면 방향',art+' 목표: 스타일라이즈드3D 카툰, 따뜻한 한국 생활권. 실제 게임과 품질 격차 존재. UI 요청은 안전 정차 조건, 결과 이유는 상세 기록.');
rows(s,[['도시와 캐릭터','스타일라이즈드 3D 카툰\n한글 상점·둥근 차량·따뜻한 햇빛'],['생활 공간','밝고 편안한 기본 방\n작은 구매로 눈에 띄는 변화'],['UI / UX','운전 중에는 도로와 길안내 우선\n정산 후 구매, 개인 공간은 건너뛰기 가능']]);
cap(s,'앞선 시안은 목표 분위기 / 실제 제작은 대표 거리·차량·방 한 세트로 품질과 공수 검증');

s=slide('차별화와 경쟁작 레퍼런스',comp+' 한계는 각 게임의 결함이 아니라 본 제안 타겟/리듬과의 차이.');
table(s,[['비교 대상','강점 / 벤치마킹','본 제안 관점의 한계','USP'],['Taxi Life 대비','도시 택시 역할과\n명료한 운송 흐름','해외 도시 배경으로\n한국 동네 공감과 거리','한국 생활권의\n일상과 공간'],['Crazy Taxi 대비','즉각적인 운송 동기와\n짧은 결과 피드백','시간 경쟁 중심으로\n차분한 생활과 다른 리듬','안전 운송과\n승객 응대'],['ETS2 대비','운전의 몰입과\n운송 후 성장 기대감','장거리 화물 중심으로\n승객 응대와 다른 호흡','짧은 영업과\n개인 차·집 목표']],[250,310,340,268],165,370,23);
text(s,'경쟁력은 타겟 이용자의 실제 체험으로 검증한다',56,565,1168,55,31,C.blue,true);
cap(s,'ETS2: Euro Truck Simulator 2 / Crazy Taxi는 역사적 레퍼런스 / 장단점은 타겟 적합성 해석');

s=slide('시장 진입과 비즈니스 모델',comp+' 기존 제안 가설 유지. 가격·시장규모·매출 실적은 제시하지 않음.');
rows(s,[['PC 유료 패키지','가격은 체험 반응과 제작비 확인 후 결정'],['짧은 영상과 데모','운송 장면과 방·차·집의 변화를 함께 보여준다'],['수요 확인','타겟 체험·커뮤니티 의견·위시리스트 전환 측정']]);
cap(s,'출시 매출·다운로드 예측은 실측 근거 확보 후 산정 / 지역 확장은 본편 재미 확인 후 검토');

s=slide('개발 로드맵',src+'\n'+life+' 회사 승인 후12개월 가정. 핵심6명+공유/외주, 은계1지역 우선. M2에 채택 범위와 인월 재산정.');
table(s,[['단계','기간 가정','핵심 산출물'],['프로토타입','M1~M2 / 8주','운송 안정화·작은 방·첫 구매 검증'],['알파','M3~M6 / 4개월','연속 영업·차량/주거 대표 흐름'],['베타','M7~M9 / 3개월','경제·저장·호환성·성능 조정'],['정식 출시','M10~M12 / 3개월','QA·현지화·스토어·출시 심의']],[300,330,538],165,370,26);
cap(s,'확정 일정 아님 / M2 검증 결과로 범위·예산 재심의 / 글로벌 동시 출시는 지원 조건 확정 후 결정');

s=slide('개발 조직과 비용',src+' 회사 심의용 인력 가정. 실제 제안 참여자와 별개. 회사 표준 단가 미확인.');
table(s,[['직무','필요 인원안','담당'],['기획 / PM','1명','콘텐츠·경제·일정·검증'],['프로그래밍','2명','운전·교통 / 생활·UI·저장'],['아트','3명','환경 / 차량·인물 / UI·TA'],['QA·사운드·사업','공유 또는 외주','품질·음향·유통·현지화']],[330,300,538],145,340,25);
text(s,'8주 검증  약 12인월     /     12개월  약 72인월',56,525,1168,60,32,C.blue,true);
cap(s,'예산 산정: 직무별 인월 × 회사 단가 + 공유·외주·장비·유통·예비비');

s=slide('제안 참여자와 기여 역할','실제 실명·직군·총인원·기여 확인 필요. 사용자가 회사 제출 전 확인 필요 표시로 완성하도록 지시한 상태 유지.');
table(s,[['참여 구분','이름 / 인원','제안 단계 역할'],['책임 / 기획','회사 제출 전 확인 필요','콘셉트·범위·승인 요청'],['기술 검토','회사 제출 전 확인 필요','프로토타입·가능성·일정'],['아트 / UI','회사 제출 전 확인 필요','시안·제작 방향·공수'],['사업 / QA','회사 제출 전 확인 필요','타겟·비용·검증 설계']],[310,420,438],165,370,25);
cap(s,'실제 참여자 정보와 향후 필요 개발 인력은 별도 / 총 참여 인원도 확인 필요');

s=slide('착수 요청: 8주로 판단할 것',life+' 6명/8주·20명·80%/70%는 심의용 제안, 실적이나 업계 기준 아님. 생활4~6명은 정성 관찰이며 통계적 성공률 추정 아님.');
rows(s,[['운전의 재미','첫 운송 자력 완료·재플레이 의향 확인'],['생활의 동기','구매를 지시하지 않고 선택 이유와 다음 영업 관찰'],['제작 가능성','대표 아트 공수·거래/저장 안정성 확인']]);
text(s,'요청  책임자 지정 · 핵심 6명 · 8주 예산 한도 승인',56,591,1168,48,28,C.blue,true);
s.speakerNotes.text+=' KPI 제안: 타겟20명 이상 첫 운송 도움없이80% 이상, 재플레이5점중4~5점70% 이상. 생활 우선4~6명3일 관찰. 저장손상/중복구매 중대결함0. 운전 피로/외형 매력/가격 원인을 구분, 미달시 개선·범위축소·투자 재심의. 차와 집 후보 매력은 작은 구매와 별도 검증.';

s=slide('프로토타입과 시연 범위',src+' 기존 캡처 docs/ui/2026-11-05-hud-preparation-navigation/01-vacant.png. 게임 실행/새3일 체감 검증 없음. 현재 배포 이력의 BuildHybrid/은계/A2PVP/LANv9 기준. 작은 방 핵심 구현 이력 확인. 동시 변경/장기 기획과 구분.');
await img(s,ROOT+'/docs/ui/2026-11-05-hud-preparation-navigation/01-vacant.png',56,140,790,440);
text(s,'확보한 기반',890,150,330,45,30,C.blue,true);text(s,'운송·요청·교통\n시설·사고·수리\n3일 영업·작은 방·저장',890,213,330,135,27);
text(s,'추가 검증',890,390,330,45,30,C.blue,true);text(s,'구매 동기·장기 성장\n목표 아트 제작비\n전체 플레이·다른 PC',890,452,330,135,26);
cap(s,'실행: builds/EungyeTaxi/EungyeTaxi.exe / 별도 조작 문서: project-b-prototype-manual-v3.md');
s.speakerNotes.text+=' 시연: 은계 싱글 선택→호출→승차→운전/요청→하차/운임→시설→마감. 입력 WASD/Space/C/M/F/F7/F8/H, 상세는 별도 Manual. 현재3일x예정20분. 개인 생활 핵심 구현, 처음방 비교 후속. 연속 경력/차량 교체/이사는 미구현 제안. 맵/아트/음원/폰트 이용조건 외부공개 전 점검. 기존 자동/물리검사와 사람의 재미 평가를 구분.';

await fs.mkdir(TMP+'/renders',{recursive:true});
await (await PresentationFile.exportPptx(p)).save(TMP+'/candidate.pptx');
for(let i=0;i<p.slides.items.length;i++){let b=await p.export({slide:p.slides.items[i],format:'png',scale:1});await fs.writeFile(TMP+'/renders/'+String(i+1).padStart(2,'0')+'.png',new Uint8Array(await b.arrayBuffer()));console.log('render',i+1);}
await fs.writeFile(TMP+'/outline.json',JSON.stringify(await p.inspect({kind:'slide,textbox,table',maxChars:1000000}),null,2));
const result=await finalizePresentation({workspaceDir:ROOT,candidatePath:TMP+'/candidate.pptx',finalPath:OUT+'/project-b-game-proposal-v5.pptx',pythonExecutable:'C:/Users/DRI/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe',integrityValidatorPath:SKILL+'/container_tools/inspect_presentation_package_integrity.py',layoutValidatorPath:SKILL+'/container_tools/inspect_presentation_layout_geometry.py',layoutArgs:['--expected-slide-size-emu','12192000,6858000','--validate-bullet-geometry','--validate-heading-fit',...tables.flatMap(n=>['--require-native-table-slide',String(n)])],explicitTotalSlideCount:23,requiredNativeTableOwnerSlides:tables,fontPolicy:{basis:'design',families:[font]},verifyArtifactToolImport:true,receiptPath:TMP+'/validation.json'});
console.log(JSON.stringify({finalPath:result.finalPath,sha256:result.finalSha256,bytes:result.byteCount,integrity:result.packageIntegrity.status,layoutFindings:result.presentationLayout.findingCount,imported:result.firstPartyImport.passed}));
