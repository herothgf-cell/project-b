/* Semantic view over existing profile IDs. No gameplay settings are copied here. */
(function () {
 'use strict';
 const quietRequestEffect='후속 제한은 확률을 낮추는 설정이 아닙니다. 이 운송에서 신호 대기·흔들림 등 추가 상황 대화와 취객의 일반 수다를 표시하지 않습니다. 별도로 배정된 다음 요청과 필수 안전·승하차 안내는 유지합니다.';
 const requestFacts={
 Quiet:['수락하면 추가 상황 대화와 취객의 일반 수다를 제한합니다. 운임과 기한은 유지합니다.','수락 이후 기사가 불필요한 잡담을 시작하면 실패 근거를 기록하고 하차 때 실패 효과를 한 번 반영합니다. 필수 안전 안내는 위반이 아닙니다.'],
 Eta:['현재 내비 ETA를 안내합니다. 빠르게/보통/여유롭게 중 하나를 고릅니다.','빠르게와 여유롭게는 ETA의 10%, 최대 30초를 빼거나 더합니다. 실제 도착과 차이를 안내하며 ETA 차이 자체 감점은 없습니다.'],
 Comfort:['수락 이후 불필요한 급가속·급제동과 충돌을 관찰합니다.','하차 때 수락 뒤 급가감속·충돌이 없으면 이행, 있으면 미이행입니다. 필요한 제동은 제외합니다. 급가감속 M 감점은 요청 결과와 별도입니다.'],
 OnTime:['원래 도착 기한을 지키는지 관찰하며 기한을 늘리지 않습니다.','기한 안에 도착하고 운전 위반이 없으면 이행합니다. 위반 동반 미이행은 실패, 위반 없는 합법적 지연은 판정 불가로 0입니다.'],
 LargeRoad:['큰길 대안이 실제로 있는 경우만 후보가 됩니다. 큰길 경로를 적용합니다.','폭 12m 이상 도로의 실제 주행 비율이 안내 비율에서 15%p 이내이면 이행합니다. 이동 1m 이하나 비율 미달은 관찰 부족으로 0이며 실패값을 적용하지 않습니다.'],
 ShortRoad:['합법 최단 경로를 적용합니다. 운임과 기한은 유지합니다.','실제 이동이 안내 거리 × 1.25 + 30m 이내이고 1m 초과이면 이행합니다. 미달은 합법 우회와 구분할 수 없어 판정 불가 0입니다.'],
 SafeStop:['실제로 연결되는 안전한 대체 승하차 지점이 필요합니다. 법규 연습 구역 활성 중에는 제외됩니다.','제안한 목적지·운임·기한을 미리 확인한 뒤 수락합니다. 지정 안전 목적지 도착 여부로 판정합니다.'],
 Empathy:['공감하거나 조용히 배려하는 대화입니다.','두 응대 모두 같은 요청 이행 만족도를 사용합니다. 조용한 배려는 후속 대사를 억제합니다. 안전한 응답 기회 뒤 미응답은 미응답 만족도를 적용합니다.'],
 RouteAdvice:['빠른 길 또는 짧은 길을 적용합니다. 운임과 기한은 유지합니다.','두 추천 모두 같은 요청 결과를 사용합니다. 이후 경로 설정이 달라져 교통·안전 사유를 알 수 없으면 판정 불가 0입니다.'],
 Interview:['격려하거나 조용히 준비하도록 배려합니다.','두 응대는 같은 이행 만족도를 사용하며 실제 면접 결과를 평가하지 않습니다. 안전한 응답 기회 뒤 미응답은 미응답 만족도를 적용합니다.'],
 Music:['현재 프로토타입은 음악 변경 없이 대화만 제공합니다.','음악 재생·변경 효과는 없습니다. 응대 결과만 하차 때 반영합니다.'],
 FareHelp:['약정 요금 또는 하차 결제 확인을 안내합니다.','두 선택 모두 원래 약정과 지급 규칙을 유지합니다. 할인·추가 부채를 만들지 않으며 응대 결과만 반영합니다.'],
 LocalChat:['지역 대화 또는 안전 운행 안내로 답합니다.','두 응대 모두 같은 이행 만족도를 사용합니다. 운임과 목적지는 유지합니다. 안전한 응답 기회 뒤 미응답은 미응답 만족도를 적용합니다.'],
 DestinationChange:['실제로 연결되는 안전한 대체 승하차 지점이 필요합니다. 법규 연습 구역 활성 중에는 제외됩니다.','제안 목적지·운임·기한 확인 뒤 수락하면 같은 운송의 목적지를 변경합니다. 지정 목적지 하차로 판정합니다.'],
 ViaStop:['안전한 경유지와 원래 목적지까지 연결 경로가 필요합니다. 법규 연습 구역 활성 중에는 제외됩니다.','제안 약정 수락 뒤 경유지에서 F로 안전 정차하고 정해진 용무 시간을 무료로 기다린 뒤 원래 목적지로 갑니다. 경유 미완료는 실패입니다. {errandSeconds}는 용무 시간으로 치환됩니다.'],
 LostItem:['운송 중 요청 추첨과 분리된 하차 후 텍스트 사건입니다.','사용 시 후보 식별자에 따라 하차 후 10%로 지갑 발견·보관·신고 안내를 기록합니다. 가중치와 응답 필드는 일반 운송 중 추첨에 사용하지 않습니다. 물품 생성·반환 경로·반환 퀘스트가 없습니다.'],
 RejectIllegal:['불법 신호 요구를 안전하게 거절합니다.','현재 거절 응대의 이행·미이행 점수는 읽기 전용 0점입니다. 실제 위반은 독립 법규·M 기록으로 평가하며 위반이 있으면 요청 결과는 판정 불가 0입니다.']
 };
 const representative={Comfort:['Comfort'],Hurried:['Eta','OnTime'],RouteChecking:['LargeRoad','ShortRoad','RouteAdvice'],Errand:['ViaStop'],PrecisionStop:['SafeStop']};
 const bindings={Comfort:'편안한 운행 요청을 첫 요청에 연결합니다.',Hurried:'변형 0은 ETA, 변형 1·2는 제시간 도착 요청을 첫 요청에 연결합니다.',RouteChecking:'변형 0·1·2는 각각 큰길·짧은 길·경로 추천 요청에 연결합니다.',Errand:'경유 요청을 첫 요청에 연결하며 변형 0·1·2의 진행 기준은 0·25·50%입니다. 설정된 최대 진행도를 넘지 않습니다.',PrecisionStop:'안전 하차 요청을 첫 요청에 연결합니다.'};
 const typeChoices={
 General:[], Drunk:['불편을 살피며 운행합니다. 조용한 귀가 변형은 후속 수다를 줄입니다.','조용히 배려하며 후속 대화를 줄입니다.'],
 Sleeping:['도착 안내를 약속합니다. 잠든 동안 일반 요청을 보류하며 하차 때 깨우기가 필요합니다.','조용히 배려합니다. 수면 중간 위치 질문과 흔들림 뒤 다시 잠드는 상태는 별도로 진행합니다.'],
 Comfort:['부드러운 운행을 안내합니다. 실제 약속·점수는 연결된 대표 요청에서 판정합니다.','운전에 집중한다고 안내하며 후속 대화를 줄입니다.'],
 Hurried:['내비 ETA 확인 대화입니다. 실제 시간 약속은 대표 요청에서 판정합니다.','신호를 지키겠다고 안내하며 후속 대화를 줄입니다.'],
 RouteChecking:['내비 경로를 확인합니다. 실제 경로 적용은 대표 요청에서 수행합니다.','경로가 달라지면 안내한다고 답하며 후속 대화를 줄입니다.'],
 Errand:['경유 요청을 확인합니다. 이 대답만으로 경유 약정이 생기지 않습니다.','별도 요청에서 약정을 확인한다고 안내하며 후속 대화를 줄입니다.'],
 Luggage:['안전 정차 중 적재 준비와 도착 후 하역 시간이 필요합니다. 만족도 변화는 0입니다.','짐을 싣지 않습니다. 적재·하역 시간과 만족도 변화는 0입니다.'],
 MuddyLuggage:['깔개 준비 시간이 필요합니다. 추가 오염과 만족도 변화는 0입니다.','보호 없이 적재하고 하차 때 흙 오염을 한 번 반영합니다. 만족도 변화는 0입니다.','짐을 싣지 않습니다. 추가 오염과 만족도 변화는 0입니다.'],
 Food:['밀봉 보관합니다. 추가 만족도·청결도 변화가 없습니다.','승차 후 허락 만족도를 반영합니다. 실제 섭취 시 부스러기 오염, 이후 흘림 발생 시 추가 오염이 생깁니다.','안전 정차 중 정리 시간이 필요합니다. 이후 먹지 않고 보관하며 추가 오염과 만족도 변화는 0입니다.'],
 PrecisionStop:['안전 승하차 장소를 확인합니다. 실제 목적지 변경은 대표 요청에서 결정합니다.','보행자 동선을 배려한다고 안내하며 후속 대화를 줄입니다.'],
 Talkative:['변형에 따라 고민·지역·남은 경로에 답하고 후속 대화를 유지합니다.','조용히 배려하며 후속 대화를 줄입니다.']
 };
 const typeReplyLabels={General:[],Drunk:['불편하시면 말씀해 주세요. 천천히 가겠습니다','편히 쉬세요. 조용히 모시겠습니다'],Sleeping:['편히 주무세요. 도착하면 깨워 드릴게요','편히 쉬세요. 조용히 모시겠습니다'],Comfort:['급하게 조작하지 않고 부드럽게 가겠습니다','말씀 감사합니다. 운전에 집중하겠습니다'],Hurried:['내비 도착 예상 시간을 확인해 드릴게요','신호를 지키며 서둘러 가겠습니다'],RouteChecking:['내비 경로를 확인하며 모시겠습니다','길이 달라지면 다시 안내해 드릴게요'],Errand:['경유 요청을 확인하고 안내해 드릴게요','경유 약속은 별도 요청에서 확인할게요'],Luggage:['안전하게 짐을 싣고 내려드릴게요','이번에는 짐 적재가 어려워요'],MuddyLuggage:['보호 깔개를 준비할게요','그대로 실어드릴게요','이번에는 짐 적재가 어려워요'],Food:['밀봉해서 보관해 주세요','드셔도 괜찮아요','안전하게 정차한 동안 정리할게요'],PrecisionStop:['안전한 승하차 장소를 확인해 드릴게요','보행자 통행을 막지 않는 곳에 세울게요'],Talkative:['속상하셨겠어요. 편하게 말씀하세요 / 이 주변을 다니고 있어요. 편하게 물어보세요 / 내비에서 남은 경로를 확인해 드릴게요','편히 쉬세요. 조용히 모시겠습니다']};
 const typeEffects={Food:['food'],Luggage:['luggage'],MuddyLuggage:['muddy'],Sleeping:['sleep'],Errand:['errand']};
 const effectFacts={food:['음식 허락·섭취·흘림·정리','허락 선택의 만족도는 승차 후 반영합니다. 이동 시간과 진행도 조건을 둘 다 충족하고 운송 진행 85% 전이어야 실제 섭취·부스러기 오염이 생깁니다. 흘림 예정은 승객 식별자로 정하며, 섭취 뒤 이동 지연 또는 물리 흔들림에서 추가 오염을 한 번 반영합니다.'],luggage:['큰 짐 적재·하역','적재 허용 시 안전 정차 동안 준비하고, 도착 후 안전 정차 동안 하역합니다. 만족도 자체는 바뀌지 않습니다.'],muddy:['흙 묻은 짐 보호·오염','깔개를 고르면 준비 시간만 사용합니다. 보호 없이 실으면 하차 때 오염을 한 번 반영합니다.'],sleep:['흔들림 뒤 다시 수면','물리 흔들림으로 깨어난 뒤 진정 시간이 지나고 응답 대기가 끝나면 다시 잠듭니다. 만족도 가산은 없습니다.'],resecure:['흔들린 짐 재고정','짐이 실린 상태에서 흔들림·필요 제동 반응의 첫 답변을 고르면 안전 정차 동안 재고정합니다. 연료·영업시간은 소모하고 비용과 만족도 변화는 0입니다.'],errand:['경유 용무 대기','경유 요청을 수락한 뒤 경유지 F 안전 정차로 시작합니다. 안전 정차 중에만 무료 용무 대기 시간이 줄어듭니다.']};
 const lawFacts={
 flashStop:['적색 점멸 정지','기존 관찰 사건의 운전 점수 감점입니다. 기본값 15점. 실제 법정 벌점과 별도입니다.'],
 permissiveTurn:['비보호 좌회전 상황','기존 관찰 사건의 운전 점수 감점입니다. 기본값 15점. 실제 법정 벌점과 별도입니다.'],
 blockedExit:['출구 막힘 진입','기존 관찰 사건의 운전 점수 감점입니다. 기본값 15점. 실제 법정 벌점과 별도입니다.'],
 personMinor:['보행자 사고 경미','기존 관찰 사건의 운전 점수 감점입니다. 기본값 10점. 실제 법정 벌점과 별도입니다.'],
 personModerate:['보행자 사고 중간','기존 관찰 사건의 운전 점수 감점입니다. 기본값 25점. 실제 법정 벌점과 별도입니다.'],
 personMajor:['보행자 사고 중대','기존 관찰 사건의 운전 점수 감점입니다. 기본값 40점. 실제 법정 벌점과 별도입니다.'],
 vehicleMinor:['차량·시설 사고 경미','기존 관찰 사건의 운전 점수 감점입니다. 기본값 3점. 실제 법정 벌점과 별도입니다.'],
 vehicleModerate:['차량·시설 사고 중간','기존 관찰 사건의 운전 점수 감점입니다. 기본값 8점. 실제 법정 벌점과 별도입니다.'],
 vehicleMajor:['차량·시설 사고 중대','기존 관찰 사건의 운전 점수 감점입니다. 기본값 15점. 실제 법정 벌점과 별도입니다.'],

 redStraight:['적색 직진·좌회전·유턴 조건','진입 때 관측한 진행 방향 신호와 유턴 보조 조건으로 판단합니다. 불명확한 관찰은 확정 위반으로 만들지 않습니다.'],
 redRight:['적색 우회전 전 일시정지','우회전 전용 신호가 없는 적색 전방에서 일시정지 의무를 판단합니다. 보행자 보호·전용 신호는 별도 조건입니다.'],
 dedicatedRed:['우회전 전용 신호','우회전 전용 신호가 있을 때 해당 신호의 허용 여부를 판단합니다.'],
 pedestrian:['횡단보도 보행자 보호','보행·횡단 의사와 차량 진행을 함께 관찰합니다. 보행자 보호 위반은 사건의 다른 판정과 함께 평가합니다.'],
 insertion:['정지 대기열 끼어들기','신호 대기열이 확인되고 차로를 바꾸어 앞부분에 진입하면 위반입니다. 대기열 불명확은 보류, 차로 변경이 없으면 기회 없음입니다.'],
 interference:['신호 대기열 합류 방해','신호 대기열이 확인되고 차로를 바꾸어 합류했을 때 상대 진행 방해를 관찰합니다. 대기열 불명확은 보류, 차로 변경이 없으면 기회 없음입니다.'],
 stopLine:['정지선 침범','알려진 적색 정지 의무 신호에서 정지선을 넘어 정차했을 때 판정합니다. 선 통과만으로 직진 신호 위반과 우회전을 구분하지 않습니다.'],
 wrongWay:['방향 차로 역주행','방향이 정해진 차로 안에서 반대 방향 전진 2초·8m를 관찰합니다. 같은 역주행 사건의 반복 기록을 억제합니다.'],
 editorSpeed:['지도 제한속도 초과','현재 지도 차로의 제한속도에 허용 여유를 더한 속도를 설정 시간 넘겨 계속 초과하면 위반입니다.'],
 slowTurn:['우회전 서행','우회전 중 기준속도와 허용 여유를 초과한 지속 시간을 관찰합니다. 위험 회피에 필요한 조작은 보류합니다.'],
 recovery:['법규 준수 회복','준수 판정의 회복량입니다. 법규 점수 회복은 운전 만족도 M 회복과 별도입니다.']};
 const mannerFacts={motion:['불필요한 급가속·급제동','필요한 제동을 제외한 급가감속을 관찰합니다. 편안한 운행 수락 중에는 별도 강화 감점을 사용합니다.'],comfortMotion:['편안한 운행 중 급가감속','편안한 운행 요청을 수락한 뒤 불필요한 급가속·급제동에 적용합니다. 요청의 하차 이행 결과와 별도입니다.'],collision:['충돌','이동 속도·상대 충돌 속도와 접촉 방향을 확인하고 중복 충돌 감점을 억제합니다.'],sidewalk:['도로 경계·인도 침범','차량 표본 중 둘 이상이 도로 밖인 상태로 이동하며 지속될 때 관찰합니다.'],yield:['회전 양보 위반','비보호 좌회전·우회전·유턴의 실제 회전 때 상대 통행 양보를 관찰합니다.'],redMovement:['진행 방향 신호·유턴 조건','실제 교차로 이탈 방향에 맞는 신호·유턴 허용 조건을 판정합니다. 황색 등 불확실한 상태는 제외합니다.'],rightStop:['우회전 전 일시정지','전용 신호 없는 적색 우회전에서 보행이 비어 있어도 필요한 사전 정지를 생략한 경우입니다.'],wrongWay:['역주행 M 감점','방향 차로 역주행 확정 때 법규 사건과 별도로 M을 감점합니다.'],recovery:['안정 주행 회복','새 구간을 안정 주행하여 정해진 거리 또는 시간에 도달하면 급가감속 손실 범위 안에서 회복합니다. 영구 손실을 지우지 않으며 회복 상한을 적용합니다. M 회복이 없으면 요구 만족도까지 안심 회복을 별도로 계산합니다.']};
 function build(catalog){
  const entries=catalog.entries||[];const byId=new Map(entries.map(e=>[e.id,e]));const out=[];
  const ids=prefix=>entries.filter(e=>e.id.startsWith(prefix)).map(e=>e.id);
  const keep=a=>[...new Set(a.filter(id=>byId.has(id)))];
  const section=(title,description,fieldIds,kind,cards)=>({title,description,fieldIds:keep(fieldIds),...(kind?{kind}:{}),...(cards?{cards}: {})});
  function add(id,group,title,summary,kind,sections,notes=[],links=[],extra={}){const seen=new Set();const unique=values=>values.filter(value=>{if(seen.has(value))return false;seen.add(value);return true});for(const s of sections){s.fieldIds=unique(s.fieldIds);for(const c of s.cards||[])c.fieldIds=unique(c.fieldIds)}const fieldIds=keep(sections.flatMap(s=>[...s.fieldIds,...(s.cards||[]).flatMap(c=>c.fieldIds)]));const entity={id,group,title,summary,kind,fieldIds,sections,notes,links,...extra};out.push(entity);return entity;}
  if(byId.has('passenger.presets'))add('passenger.presets','승객 프리셋','승객 프리셋','승객의 특성과 공유 행동을 조합합니다.','presets',[section('승객 프리셋','전용 편집 화면을 사용합니다.',['passenger.presets'])]);
  const schedule=ids('schedule.');
  for(const [kind,facts]of Object.entries(requestFacts)){
   const prefix=`request.${kind}.`;const own=ids(prefix);if(!own.length)continue;
   const cards=own.filter(id=>id.includes('.reply.')&&!/\.SmallTalk(?:Violation)?$/.test(id)).map(id=>{
    const reply=id.split('.').at(-1);let description=facts[1];
    if(reply==='Defer')description='요청을 수락하지 않고 원래 운행을 유지합니다. 안전상 보류이며 만족도 변화는 0입니다. 실패 점수를 적용하지 않습니다.';
    if(reply.startsWith('Eta'))description=reply==='EtaFaster'?'현재 내비 ETA보다 10%, 최대 30초 빠르게 예상합니다. 운행 기한은 바꾸지 않습니다.':reply==='EtaRelaxed'?'현재 내비 ETA보다 10%, 최대 30초 여유롭게 예상합니다. 운행 기한은 바꾸지 않습니다.':'현재 내비 ETA로 안내합니다. 예상 차이 자체 감점은 없습니다.';
    if(reply==='Alternative'&&['Empathy','Interview'].includes(kind))description='조용히 배려하며 후속 대사를 억제합니다. 일반 응대와 같은 이행 만족도 설정을 사용합니다.';
    if(reply==='Alternative'&&kind==='RouteAdvice')description='합법 최단 경로를 적용합니다. 빠른 길 선택과 같은 요청 결과 설정을 사용합니다.';
    const resultFields=reply==='Defer'?[]:[prefix+'success',prefix+'failure'];
    if(kind==='Quiet'&&reply==='Accept'||['Empathy','Interview'].includes(kind)&&reply==='Alternative')description+=' '+quietRequestEffect;
    return {title:reply==='Defer'?'안전상 보류':reply==='Alternative'?'다른 응대':reply==='EtaFaster'?'빠른 예상':reply==='EtaNormal'?'내비 예상':reply==='EtaRelaxed'?'여유 있는 예상':'요청 수락',description,fieldIds:keep([id,...(kind==='Comfort'&&reply!=='Defer'?['manner.comfortMotion.penalty']:[]),...(kind==='ViaStop'&&reply!=='Defer'?['effect.errand.seconds']:[])]),resultIds:keep(resultFields),notes:reply==='Defer'?[]:['이행·미이행 점수는 아래 운송 종료 평가에서 한 번만 편집합니다. 여러 수락 답변이 같은 점수를 공유하며, 선택지마다 추가로 더하지 않습니다.']};
   });
   const links=[...(kind==='LostItem'?[]:['common.request','common.schedule']),...Object.entries(representative).filter(([,k])=>k.includes(kind)).map(([t])=>'type.'+t)];
   if(kind==='Comfort')links.push('manner.comfortMotion');if(kind==='ViaStop')links.push('effect.errand');
   const notes=[facts[0],facts[1]];
   if(['Quiet','Empathy','Interview'].includes(kind))notes.push(quietRequestEffect);
   if(kind!=='LostItem')notes.push('실제 지도 경로가 없으면 ETA와 경로 요청을 제외합니다. 시스템 강제 대기·우회는 경로 요청 미이행에서 제외합니다. 같은 요청 중복·경로 요청끼리·조용함과 대화 요청의 동시 조합을 제외합니다. 대표 승객 유형은 첫 요청을 해당 요청으로 바꿀 수 있습니다.');
   add('request.'+kind,'승객 요청',byId.get(prefix+'enabled')?.section||kind,facts[0],'request',[
    section('등장 설정',kind==='LostItem'?'사용 여부만 하차 후 사건 발생에 적용합니다. 가중치는 이 사건의 10%를 바꾸지 않습니다.':'가중치는 같은 후보 추첨 안의 상대 비중입니다. 실제 운송 전체 등장 확률이 아닙니다.',[prefix+'enabled',prefix+'weight'],'appearance'),
    ...(['Quiet','LocalChat'].includes(kind)?[section('기사 잡담 사건 대사','기사가 불필요한 잡담을 직접 시작했을 때 표시합니다. 안전 안내는 제외하며 조용히 요청 수락 뒤에는 실패 근거로 남습니다.',own.filter(id=>/\.SmallTalk(?:Violation)?$/.test(id)),'dialogue')]:[]),
    section('승객 말풍선','현재 한국어 문구를 직접 편집합니다.',[prefix+'prompt'],'prompt'),
    section('선택지와 실제 결과',kind==='LostItem'?'아래 문구는 진단용 단일 요청 경로에도 사용합니다. 일반 운송은 하차 후 요약 사건만 기록합니다.':'대답을 고르면 표시 문구와 실제 처리 결과를 함께 확인합니다.',[],'choices',cards),
    section('운송 종료 평가','수락한 요청의 실제 이행 여부를 확인해 하차 때 한 번 반영합니다. 선택지 미리보기와 같은 점수이며 추가 보상이 아닙니다. '+facts[1]+(kind==='Eta'?' 현재 예상 도착 안내의 이행·미이행·미응답 점수는 모두 읽기 전용 0점입니다. 선택지별 점수 편집은 아직 지원하지 않습니다.':!['Quiet','Comfort','OnTime','SafeStop','DestinationChange','ViaStop'].includes(kind)?' 현재 실패값은 고정/중립입니다.':''),[prefix+'success',prefix+'failure'],'outcomes'),
    section('답하지 않았을 때',kind==='Quiet'?'안전한 응답 기회 후 끝까지 미응답이면 하차 때 M(운전 만족도)을 감점합니다. 요청 이행 점수와 별도이며, 답할 기회가 없으면 감점하지 않습니다.':['Empathy','Interview','LocalChat'].includes(kind)?'안전하게 답할 기회가 있었는데 끝까지 답하지 않으면 하차 때 아래 요청 만족도를 반영합니다. 답할 기회가 없으면 감점하지 않습니다.':'현재 이 요청은 답하지 않아도 추가 감점이 없습니다. 아래 값은 고정/미사용 결과입니다.',[prefix+'unanswered'],'unanswered'),
    section(kind==='LostItem'?'공통 설정 적용 여부':'모든 요청에 적용되는 공통 설정',kind==='LostItem'?'하차 후 사건은 운송 중 요청 합계 상한과 등장 시점 설정을 사용하지 않습니다.':'이 요청 전용 값이 아닙니다. 운송 중 일반 요청 전체(분실물 제외)에 영향을 줍니다. 요청 합계 상한은 하차 점수 합산 때, 개수·시점은 새 운송의 요청 배정과 표시 때 사용합니다.',kind==='LostItem'?[]:['common.requestCap',...schedule],'shared')
   ],notes,links,{requestKind:kind,enabledId:prefix+'enabled',weightId:prefix+'weight',promptId:prefix+'prompt'});
  }
  for(const type of Object.keys(typeChoices)){
   const own=ids('type.'+type+'.');if(!own.length)continue;const reps=representative[type]||[];
   const effects=(typeEffects[type]||[]).flatMap(e=>ids('effect.'+e+'.'));
   const cards=own.filter(id=>id.includes('.choice.')).map(id=>{const n=Number(id.split('.').at(-1));let linked=[];
    if(type==='Food')linked=n===1?ids('effect.food.').filter(x=>!x.endsWith('cleanupSeconds')):n===2?['effect.food.cleanupSeconds']:[];
    if(type==='Luggage'&&n===0)linked=ids('effect.luggage.');
    if(type==='MuddyLuggage')linked=n===0?['effect.muddy.prepareSeconds']:n===1?['effect.muddy.dirt']:[];
    return {title:typeReplyLabels[type][n]||`응대 ${n+1}`,description:typeChoices[type][n]||'후속 대화 응답',fieldIds:keep([id,...linked]),notes:['빈 문구는 게임에 내장된 한국어 응답을 사용합니다.']};
   });
   const links=[...reps.map(r=>'request.'+r),...(typeEffects[type]||[]).map(e=>'effect.'+e)];
   if(['Food','Sleeping','Luggage','MuddyLuggage'].includes(type))links.push('situation.Jolt','situation.NecessaryBrake');
   if(['Luggage','MuddyLuggage'].includes(type))links.push('effect.resecure');
   const binding=bindings[type]||'유형 대화 자체에는 별도 요청 이행 점수가 없습니다.';
   add('type.'+type,'대화 선택지',byId.get(own[0]).section,binding,'dialogue',[
    section('배정과 연결 요청',type==='General'||type==='Drunk'?'기존 일반·취객 배정 보장 규칙으로 결정하며 특수 유형 가중치 추첨과 구분합니다.':'특수 유형 후보 안에서 상대 가중치를 사용합니다. 대표 요청을 사용할 수 없거나 요청 수가 0이면 대표 유형 배정이 제한됩니다.',own.filter(id=>id.endsWith('.weight')||type==='Drunk'&&!['intro','choice','followup'].some(k=>id.includes('.'+k+'.'))),'appearance'),
    section('승차 때 대사','후보 식별자로 변형이 정해집니다. 같은 번호의 도입·후속 대사가 한 변형입니다.',own.filter(id=>id.includes('.intro.')),'prompt'),
    section('선택지와 실제 결과','아래 문구와 처리 조건은 같은 유형 행동에 연결됩니다.',[],'choices',cards),
    section('운행 중 후속 대사','빈 문구는 행동·실제 관찰 결과에 맞는 내장 대사를 사용합니다. 조용한 배려·수면·작업 중에는 표시가 억제됩니다.',own.filter(id=>id.includes('.followup.')),'dialogue'),
    section('연결된 약속과 효과',binding,[...reps.flatMap(r=>ids('request.'+r+'.')),...effects],'shared')
   ],[binding,'적재·정리·하역 등 물리 작업은 안전 정차 중에만 진행합니다. 대화 가능 여부와 별도입니다.'],links,{...(own.includes('type.'+type+'.weight')?{weightId:'type.'+type+'.weight'}:{})});
  }
  for(const [key,facts]of Object.entries(effectFacts))add('effect.'+key,'효과',facts[0],facts[1],'effect',[section('발생 조건과 결과',facts[1],ids('effect.'+key+'.'),'effects')],['같은 설정을 유형·요청·상황 화면에서 편집해도 하나의 프로필 값으로 반영합니다.'],Object.entries(typeEffects).filter(([,v])=>v.includes(key)).map(([t])=>'type.'+t));
  for(const [key,title,description]of [['nausea','메스꺼움 배려','배정 때 사건 확률을 한 번 추첨합니다. 운행·흔들림으로 불편이 쌓이고 안전 정차 휴식이나 봉투가 이후 오염에 영향을 줍니다.'],['temperature','차내 온도 배려','따뜻하게·시원하게·유지 선택을 실제 차내 상태로 기록합니다. 선호와 유지 시간으로 평가하고 조정 후 결과를 교체합니다. 외부 날씨와 별개입니다.'],['umbrella','젖은 우산 보호','안전 정차 중 감싸기를 완료하면 오염을 예방합니다. 보호 없이 둔 우산은 청결 손실을 한 번 반영합니다.']])add('effect.'+key,'승객 특성',title,description,'effect',[section('공통 확률·효과',description,ids('effect.'+key+'.'),'effects'),section('공통 응대 대사','승객별 개별 설정으로 덮어쓸 수 있습니다.',ids('trait.'+key+'.'),'dialogue')],[],[]);
  const situations={SignalWait:['신호 대기','정지한 신호 대기가 18초 이어지면 반응할 수 있습니다. 신호가 바뀌면 출발한다고 안내합니다.'],Jolt:['차량 흔들림','급가속·급제동 관찰 뒤 반응합니다. 유형에 따라 수면 깨기·음식 흘림·짐 상태 설명이 달라집니다.'],NecessaryBrake:['필요한 제동','위험 회피를 위한 제동은 불필요한 급제동 감점에서 제외하며 안전 조작을 설명합니다.']};
  for(const [key,facts]of Object.entries(situations))add('situation.'+key,'대화 선택지',facts[0],facts[1],'dialogue',[
   section('관찰 조건',facts[1],[],'condition'),
   section('답변과 실제 효과','상황 응대 자체는 만족도를 추가하지 않습니다.',[],'choices',[{title:'상황 설명 또는 짐 재고정',description:key==='SignalWait'?'신호 대기를 설명합니다. 추가 효과는 없습니다.':'수면 승객은 다시 수면, 실은 짐이 있으면 안전 정차 재고정, 그 외에는 상황만 설명합니다.',fieldIds:keep(['situation.'+key+'.reply.0',...(key==='SignalWait'?[]:['effect.resecure.seconds'])])},{title:'편히 쉬도록 배려',description:'만족도 변화 0 · 후속 질문을 줄입니다. 깨어난 수면 승객은 다시 잠듭니다.',fieldIds:keep(['situation.'+key+'.reply.1'])}])
  ],['한 운송에서 반응 최대 2회, 예약 대화 포함 최대 3회이며 반응 사이 20초가 필요합니다. 조용함·수면·다른 대화·재고정 중에는 새 반응을 억제합니다.'],key==='SignalWait'?[]:['effect.resecure','type.Sleeping','type.Food','type.Luggage','type.MuddyLuggage']);
  const lawNote='법규 점수와 운전 만족도 M은 별도입니다. 같은 사건의 중복 방지와 사건 내 최대 감점을 적용하며 보행자 보호·위험 회피 보류는 고정 의미입니다.';
  for(const [key,facts]of Object.entries(lawFacts)){
   const linked=key==='wrongWay'?['manner.wrongWay.penalty']:key==='redRight'?['manner.rightStop.penalty']:['redStraight','dedicatedRed'].includes(key)?['manner.redMovement.penalty']:[];
   add('law.'+key,'법규',facts[0],facts[1],'law',[section('판정 상황과 입력 가능한 조건',facts[1],ids('law.'+key+'.').filter(id=>!id.endsWith('.penalty')&&!id.endsWith('.points')),'condition'),section(key==='recovery'?'법규 점수 회복':'법규 점수 감점',key==='recovery'?'준수 관찰 중 위반이 없는 사건만 법규 점수를 회복합니다. 실제 위반은 감점값이 0이어도 위반이므로 회복하지 않습니다.':'기록되는 법규 점수의 양입니다. 실제 사건에서는 함께 발생한 판정의 최대 감점 규칙을 적용합니다.',ids('law.'+key+'.').filter(id=>id.endsWith('.penalty')||id.endsWith('.points')),'outcomes'),section('같이 관찰하는 M','동일 사건이라도 법규 감점값과 M 감점값은 서로 다른 설정입니다.',linked,'shared')],[lawNote],linked.map(id=>id.replace('.penalty','')));
  }
  for(const [key,facts]of Object.entries(mannerFacts))add('manner.'+key,'법규',facts[0]+' · M',facts[1],'law',[section('운전 만족도 관찰과 결과',facts[1],ids('manner.'+key+'.'),'outcomes'),...(key==='recovery'?[section('M 회복 뒤 안심 회복','M 회복량이 없을 때 시작 만족도 부족분을 단계·상한 안에서 회복합니다.',['common.comfortStep','common.comfortCap'],'shared')]:[])],[lawNote,'승차 때 M은 100으로 시작하며 하차 만족도에는 M−100이 주행 변화량으로 합산됩니다.'],key==='comfortMotion'?['request.Comfort']:[]);
  const common=[
   ['schedule','요청 개수와 등장 시점','운송별 요청 0·1·2개를 정합니다. count0 경계, count0+count1 경계로 분기하며 나머지가 2개입니다. count2는 프로필 합계 검증에 사용합니다. 첫 요청 일부는 승차 때, 나머지는 진행도·이동시간·대화 가능 조건을 충족하면 주행 중에도 등장합니다. 기존 선택이나 작업 중에는 새 요청을 기다립니다. 마감 진행도 이후 미발생 요청은 중립입니다.',schedule],
   ['request','요청 만족도 합계','모든 요청 결과를 합산한 뒤 양·음 방향 모두 이 상한으로 제한합니다.',['common.requestCap']],
   ['time','안전한 조기 도착','유효한 기준 ETA와 실제 시간을 비교합니다. 위험 운전이 없을 때만 시간 계수 × (기준−실제)/기준을 반올림하고 0부터 상한까지 가산합니다.',['common.timeCap','common.timeFactor']],
   ['comfort','안정 주행 안심 회복','M 회복이 더 없고 만족도가 요구치보다 낮을 때 단계·누적 상한·부족분 중 작은 값만 회복합니다.',['common.comfortStep','common.comfortCap']],
   ['fare','운임과 보너스','최종 정산 운임·보너스에 각각 배수를 적용하고 10원 단위로 내립니다. 도착 기한 초과는 운임 0, 요구 만족도 미달은 만족도/요구치 비율 지급입니다.',['common.fareMultiplier','common.bonusMultiplier',...ids('common.required.')]],
   ['deadline','약정 도착 기한','새 약정의 기준 시간에 기한 배수와 여유 시간을 적용합니다. 제시간 도착 요청을 수락하는 것만으로 기한을 연장하지 않습니다.',['common.deadlineMultiplier','common.deadlineSlack']],
   ['start','시작 만족도와 청결','시작 부족분과 청결 가산의 배수입니다. 시작 만족도에는 청결이 포함되며 하차 때 다시 더하지 않습니다.',['common.startDeficitMultiplier','common.cleanBonusMultiplier']]
  ];
  for(const [key,title,description,fields]of common)add('common.'+key,'공통 영향 요소',title,description,'common',[section(title,description,fields,'shared')],[],[]);
  if(entries.some(e=>e.id.startsWith('vehicle.')))add('vehicle.purchase','차량','차량 구매·꾸미기','3일 회차 안 차량 교체 1회 · 시험 가격','vehicle',[section('가격(시험값)','보상 판매액을 뺀 금액이 실제 지출입니다. 사람 플레이 수익 중앙값 R 측정 전의 시험값입니다.',ids('vehicle.'),'shared')],['차종 특성(연료·트렁크·승차감·내구도·회전)은 코드 고정값이며 PVP에는 적용하지 않습니다.'],[]);
  add('weather.rain','날씨','비 운행','맑음/비 시험 · 확률 미끄러짐 잠금','weather',[section('비 설정','기존 저장은 당시 설정 유지. 확률 미끄러짐은 검증 후 별도 개발/활성화합니다.',ids('weather.'),'shared')],[],[]);
  for(const e of entries.filter(e=>e.id.startsWith('constraint.')))add(e.id,'공통 영향 요소',e.title,e.defaultValue,'common',[section('현재 게임의 고정 조건',e.defaultValue,[e.id],'condition')],[e.description||''],[]);
  const covered=new Set(out.flatMap(e=>e.fieldIds));for(const e of entries)if(!covered.has(e.id)&&!e.id.startsWith('choice.'))add('field.'+e.id,e.group,e.title,e.description,'field',[section(e.section,e.description,[e.id])],[],[]);
  for(const ref of catalog.references||[])add(ref.id,'법규',ref.title,ref.description,'reference',[section('자료의 범위',ref.description,[],'reference')],[ref.status||'',ref.basis||'',ref.source?'출처: '+ref.source:'',ref.sourceDate?'자료 기준일: '+ref.sourceDate:''].filter(Boolean),[]);
  const consumers={
   'effect.resecure':['type.Luggage','type.MuddyLuggage','situation.Jolt','situation.NecessaryBrake'],
   'effect.sleep':['situation.Jolt','situation.NecessaryBrake'],
   'effect.errand':['request.ViaStop'],
   'common.schedule':Object.keys(requestFacts).filter(k=>k!=='LostItem').map(k=>'request.'+k),
   'common.request':Object.keys(requestFacts).filter(k=>k!=='LostItem').map(k=>'request.'+k),
   'common.time':['request.OnTime','common.fare'],
   'common.comfort':['manner.recovery'],
   'common.fare':['request.FareHelp'],
   'common.deadline':['request.OnTime','request.SafeStop','request.DestinationChange','request.ViaStop'],
   'common.start':['common.fare']
  };
  for(const e of out)if(consumers[e.id])e.links=[...new Set([...e.links,...consumers[e.id]])];
  const scheduleEntity=out.find(e=>e.id==='common.schedule');
  if(scheduleEntity){const conditions=section('요청 조합과 표시 조건','요청을 뽑은 뒤의 조합 제외와 안전한 표시 기회는 실제 등장 횟수를 바꿉니다.',['constraint.requestCompatibility','constraint.requestCount'],'condition');scheduleEntity.sections.push(conditions);scheduleEntity.fieldIds=keep([...scheduleEntity.fieldIds,...conditions.fieldIds]);}
  const validIds=new Set(out.map(e=>e.id));for(const e of out)e.links=e.links.filter(id=>validIds.has(id));
  const entityById=new Map(out.map(e=>[e.id,e]));
  for(const e of out)for(const id of [...e.links]){const target=entityById.get(id);if(!target.links.includes(e.id))target.links.push(e.id);}
  if(entries.some(e=>e.id.startsWith('choice.'))){
   const currentWording=text=>String(text||'').replace(/두 응대 모두 같은 (?:요청 )?이행 만족도를 사용합니다\./g,'각 응답의 이행 만족도는 해당 선택지의 운송 종료 효과에서 독립적으로 설정합니다.').replace('두 응대는 같은 이행 만족도를 사용하며','각 응답의 이행 만족도를 독립적으로 설정하며').replace('두 추천 모두 같은 요청 결과를 사용합니다.','각 추천 응답의 종료 효과를 독립적으로 설정합니다.').replace(/일반 응대와 같은 이행 만족도 설정을 사용합니다\.|빠른 길 선택과 같은 요청 결과 설정을 사용합니다\./g,'각 응답의 종료 효과를 독립적으로 설정합니다.').replace('판정 불가 0입니다.','판정 불가 종료 효과를 적용합니다.').replace('현재 거절 응대의 이행·미이행 점수는 읽기 전용 0점입니다.','거절 응대의 이행·판정 불가 종료 효과를 독립적으로 설정합니다.').replace('응대 결과만 하차 때 반영합니다.','선택 응답의 즉시·운행 중·종료 효과는 아래에서 독립적으로 설정합니다.').replace('만족도 자체는 바뀌지 않습니다.','만족도·피로 효과는 각 선택지에서 설정합니다.').replace('만족도 가산은 없습니다.','만족도·피로 효과는 각 선택지에서 설정합니다.').replace('비용과 만족도 변화는 0입니다.','추가 비용은 없으며 만족도·피로 효과는 각 선택지에서 설정합니다.').replace('상황 응대 자체는 만족도를 추가하지 않습니다.','각 상황 응답의 만족도·피로 효과를 독립적으로 설정합니다.').replace('추가 효과는 없습니다.','만족도·피로 효과는 이 선택지에서 설정합니다.').replace('판정 불가 0이며','판정 불가 종료 효과를 적용하며').replace('판정 불가로 0입니다.','판정 불가 종료 효과를 적용합니다.');

   const sleeping=out.find(e=>e.id==='type.Sleeping');if(sleeping)sleeping.sections.push(section('수면 중 위치 질문 응답','실제 위치 질문 뒤의 두 응답입니다.',[],'choices',[{title:'남은 위치는 내비 안내로 확인해 주세요',description:'남은 위치는 내비 안내로 확인하도록 안내하고 다시 수면합니다.',choiceId:'type.SleepingFollowup.0',fieldIds:[]},{title:'운전에 집중하겠습니다. 편히 쉬세요',description:'다시 수면하고 후속 질문을 줄입니다.',choiceId:'type.SleepingFollowup.1',fieldIds:[]}]));
   const foodEffect=out.find(e=>e.id==='effect.food');if(foodEffect){for(const block of foodEffect.sections)block.fieldIds=block.fieldIds.filter(id=>id!=='effect.food.satisfaction');foodEffect.sections.push(section('음식 허용 선택의 즉시 효과','음식 허용 응답의 현재 효과입니다.',['choice.type.Food.1.immediate.satisfaction','choice.type.Food.1.immediate.fatigue','choice.type.Food.1.immediate.satisfactionPercent','choice.type.Food.1.immediate.fatiguePercent'],'effects'),section('이관 원본 (참조)','현재 선택지 효과에서 편집합니다.',['effect.food.satisfaction'],'shared'));foodEffect.fieldIds=keep(foodEffect.sections.flatMap(s=>s.fieldIds));}
   const common=out.find(e=>e.id==='common.request');if(common){common.sections.push(section('선택 효과 버전','현재 선택 효과 정의 버전입니다.',['choice.effects.version'],'shared'));common.fieldIds=keep([...common.fieldIds,'choice.effects.version']);}
   for(const entity of out){
    for(const block of entity.sections){
     if(block.kind==='outcomes'&&entity.kind==='request'){block.legacy=true;block.title='이관 원본 (참조)';block.description='이전 프로필 이관에 사용한 값입니다. 현재 선택지 효과에서 편집합니다. '+currentWording(requestFacts[entity.requestKind]?.[1]);}
     for(const card of block.cards||[]){
      const textId=card.fieldIds.find(id=>/\.(reply|choice)\./.test(id));if(!textId&&!card.choiceId)continue;
      const choiceId=card.choiceId||textId.replace('.reply.','.').replace('.choice.','.');
      const prefix='choice.'+choiceId+'.',effectIds=ids(prefix);if(!effectIds.length)continue;
      card.choiceId=choiceId;card.resultIds=[];card.description=currentWording(card.description);if(choiceId.endsWith('.Defer'))card.description='요청을 수락하지 않고 원래 운행을 유지합니다. 안전상 보류입니다. 즉시·운행 중·판정 불가/보류 종료 효과는 이 선택지에서 독립적으로 설정합니다. 미이행 종료 분기는 적용하지 않습니다.';
      card.notes=(card.notes||[]).filter(note=>!note.includes('같은 점수'));
      card.description=card.description.replace(/일반 응대와 같은 이행 만족도 설정을 사용합니다\./g,'이 응답의 독립 효과를 적용합니다.').replace(/빠른 길 선택과 같은 요청 결과 설정을 사용합니다\./g,'이 응답의 독립 효과를 적용합니다.');
      card.description=card.description.replace(/[^.]*만족도 변화[^.]*[.]/g,'만족도·피로 효과는 아래 설정을 적용합니다.').replace('추가 만족도·청결도 변화가 없습니다.','추가 청결도 변화가 없습니다.').replace(/\.([가-힣])/g,'. $1');if(!card.description.includes('독립'))card.description+=' 이 선택지의 즉시·운행 중·종료 효과는 아래에서 독립적으로 설정합니다.';
      card.fieldIds=card.fieldIds.filter(id=>!isLegacyField(id));
      const stages=[['immediate','즉시','선택 확정 때 한 번 적용합니다. 승차 전 응답은 승차 확정 때 적용합니다. 변화량은 점 증감 + 적용 직전 현재값 × 변화율/100이며 적용 후 0~100으로 제한합니다.'],['during','운행 중','선택 활성화부터 정상 하차·취소·운송 중단까지 게임 영업 경과 시간으로 누적합니다. %/분은 선택 활성화 때 고정한 기준값을 사용합니다. 메뉴 일시정지 중에는 진행하지 않습니다. 0은 이 선택지의 추가 효과가 없다는 뜻이며 자연 피로와 기본 진행은 유지합니다.'],['end.success','운송 종료 · 이행','실제 이행 판정 때 한 번 적용합니다. 유형·상황은 정상 하차 때 적용합니다.'],['end.failure','운송 종료 · 미이행','실제 요청 미이행 판정 때 한 번 적용합니다.'],['end.neutral','운송 종료 · 판정 불가/보류','실제 중립 판정 때 한 번 적용합니다.']];
      card.effectGroups=stages.map(([stage,title,description])=>({stage,title,description:description+(choiceId.startsWith('request.')&&stage.startsWith('end.')?' 요청 종료 만족도는 점+변화율 결과를 선택지마다 −100~100점으로 제한하고, 원시 소수값을 합산해 한 번 반올림한 뒤 요청 종료 합계 상한을 적용합니다.':''),fieldIds:effectIds.filter(id=>id.startsWith(prefix+stage+'.'))})).filter(g=>g.fieldIds.length);
      for(const stage of card.effectGroups){if(stage.stage==='end.success')stage.title='운송 종료 · 이행 성공';if(stage.stage==='end.failure')stage.title='운송 종료 · 이행 실패 (미이행)';}
      if(!card.effectGroups.some(stage=>stage.stage==='end.failure'))card.failureUnavailable=choiceId.endsWith('.Defer')?'이행 실패 · 보류 선택은 판정 불가로 처리합니다. 미응답과는 별개입니다.':choiceId.startsWith('request.')?'이행 실패 · 이 요청은 수락 후 별도 실패 판정이 없습니다. 미응답 결과는 아래의 답하지 않았을 때에서 확인합니다.':'이행 실패 · 이 응대는 정상 하차 때 종료 효과를 적용하며 별도 실패 판정이 없습니다.';
      entity.fieldIds=keep([...entity.fieldIds,...effectIds]);
     }
    }
    entity.notes=entity.notes.map(note=>note.replace('유형 대화 자체에는 별도 요청 이행 점수가 없습니다.','유형 응답 효과는 각 선택지에서 독립적으로 편집합니다.').replace(/두 응대 모두 같은 요청 이행 만족도를 사용합니다\./g,'각 응답의 독립 효과를 사용합니다.').replace(/두 응대는 같은 이행 만족도를 사용하며/g,'각 응답의 독립 효과를 사용하며').replace(/두 추천 모두 같은 요청 결과를 사용합니다\./g,'각 추천의 독립 효과를 사용합니다.'));
    entity.summary=currentWording(entity.summary);entity.notes=entity.notes.map(currentWording);
    for(const block of entity.sections)block.description=currentWording(block.description).replace('유형 대화 자체에는 별도 요청 이행 점수가 없습니다.','유형 응답 효과는 각 선택지에서 독립적으로 편집합니다.');
    if(entity.id==='type.Food')for(const block of entity.sections)block.fieldIds=block.fieldIds.filter(id=>id!=='effect.food.satisfaction');
   }
  }
  return out;
 }
 function weightInfo(entity,profile,catalog){
  const defaults=new Map((catalog.entries||[]).map(e=>[e.id,e.defaultValue]));const values=new Map((profile?.values||[]).map(e=>[e.id,e.value]));
  const value=id=>values.has(id)?values.get(id):defaults.get(id);const enabled=id=>!id||String(value(id)).toLowerCase()==='true';
  if(entity.requestKind==='LostItem')return {text:enabled(entity.enabledId)?'하차 후 조건부 10%':'사용 안함 · 하차 후 사건 제외',explanation:'하차 후 후보 식별자로 정하는 별도 10% 텍스트 사건입니다. 일반 요청 추첨에서 제외하며 가중치로 10%를 바꾸지 않습니다.'};
  if(entity.id==='type.Drunk')return {text:'등장 '+value('type.Drunk.spawnPercent')+'% · 하루 최대 '+value('type.Drunk.dailyMax')+'명',explanation:'배정 가능 차례의 확률이며 전체 승객 비율이 아닙니다. 특수 승객 뒤 다음 탑승은 일반 승객입니다. 하루 최대 0은 취객 없음입니다. 강제 등장 '+(value('type.Drunk.forceEnabled')==='true'?'사용 · 취객 없이 '+value('type.Drunk.forceAfterMisses')+'회 탑승 후 다음 가능한 차례':'사용 안함')+'. 잠듦 '+value('type.Drunk.sleepPercent')+'%, 구토 예정 '+value('type.Drunk.vomitPercent')+'%는 각각 독립 추첨합니다.'};
  if(!entity.weightId)return {text:'',explanation:''};
  if(!enabled(entity.enabledId))return {text:'사용 안함 · 후보 제외',explanation:'사용하지 않는 요청은 가중치와 관계없이 후보에서 제외합니다.'};
  const weight=Math.max(0,Number(value(entity.weightId))||0);if(!weight)return {text:'가중치 0 · 후보 제외',explanation:'양수 가중치를 가진 사용 중 후보만 추첨합니다.'};
  const request=entity.kind==='request';const prefix=request?'request.':'type.';
  const fields=(catalog.entries||[]).filter(e=>e.id.startsWith(prefix)&&e.id.endsWith('.weight')&&e.id!=='request.LostItem.weight');
  const total=fields.reduce((sum,e)=>{const flag=e.id.replace(/\.weight$/,'.enabled');return sum+(!request||enabled(flag)?Math.max(0,Number(value(e.id))||0):0);},0);
  const percent=total?Math.round(weight/total*10000)/100:0;
  const example='같은 후보군에서 가중치가 모두 1이면 같은 비중입니다. 1·1·2이면 25%·25%·50%이며, 0은 제외됩니다. ';
  return {text:`기본 후보 비중 ${percent}% (${weight} / ${total})`,explanation:example+(request?'사용 중인 양수 가중치 요청을 모두 후보로 놓은 기준 비중입니다. 실제 전체 등장 확률이 아닙니다. 지도·안전 대안, 요청 개수 0~2, 조합 제외, 등장 시점·대화 가능 조건, 대표 유형 첫 요청 연결로 실제 후보와 결과가 달라집니다.':'특수 유형 후보를 모두 놓은 기준 비중입니다. 일반·취객의 배정 보장과 연속 특수 승객 제한, 대표 요청 자격 조건이 있어 실제 전체 승객 등장 확률과 다릅니다.')};
 }
 function isLegacyField(id){return /^request\.[^.]+\.(success|failure)$/.test(id)||id==='effect.food.satisfaction'||id==='manner.comfortMotion.penalty';}
 function fieldInfo(id,catalog){
  if(id.startsWith('choice.')){
   const field=id.split('.').at(-1),during=id.includes('.during.'),end=id.includes('.end.');
   const percent=field.includes('Percent'),unit=field.endsWith('PerMinute')?(percent?'%/분':'점/분'):percent?'%':'점';
   const stat=field.startsWith('satisfaction')?'만족도 S':'기사 피로',base=field.startsWith('satisfaction')?70:20,delta=base/10;
   return {when:during?'선택 활성화부터 운송 종료/중단까지':end?'정상 운송 종료의 실제 결과 판정 때 한 번':'선택 확정 또는 승차 확정 때 한 번',affects:field.startsWith('satisfaction')?'이 응답을 선택한 승객의 만족도 S':field==='motionPenaltyDelta'?'활성 중 불필요한 급가감속 M 감점':'이 응답을 선택한 운송의 기사 피로',detail:unit+' · 0 = 이 선택지의 추가 효과 없음. '+(field==='fatigueGrowthPercent'?'기본 자연 피로 증가 × (1 + 추가율/100)입니다. 싱글 포만감 25 초과의 기본 +4점/분에 +50%는 +6점/분, -50%는 +2점/분, -100%는 자연 증가 0입니다. 0%는 원래 증가율 유지입니다. 현재 기사 피로의 %가 아닙니다. 아래 기본 자연 피로 표에서 허기·모드별 수치를 확인하세요.':field==='motionPenaltyDelta'?'M의 추가 변화량입니다. -2는 기본 감점에 2점 추가 감점, +2는 감점 2점 완화입니다. 0은 기본 감점 유지이며 최종 감점은 0 아래로 내려가 보상이 되지 않습니다.':during&&percent?`즉시 효과 뒤 선택 활성화 때 고정한 ${stat} 기준값 × 변화율/100을 매분 더합니다. 기준 ${base}점에 +10%/분은 +${delta}점/분이며 30초는 +${delta/2}점입니다. 복리가 아니며 음수는 감소입니다. 고정 점/분 효과와 기존 자연 피로는 별도로 더합니다.`:percent?`적용 직전 현재 ${stat} × 변화율/100을 한 번 더합니다. 현재 ${base}점에 +10%는 +${delta}점, -10%는 -${delta}점입니다. 고정 증감(점)도 함께 더하며 0~100점으로 제한합니다.`:'양수는 증가, 음수는 감소하는 증감량입니다.')+(end?' 취소·사고·견인 등 운송 중단에는 종료 효과를 주지 않습니다.':'')};
  }
  if(isLegacyField(id)&&(catalog.entries||[]).some(e=>e.id.startsWith('choice.')))return {when:'이전 프로필 이관 시',affects:'이관 원본 (참조)',detail:'현재 선택지 효과에서 편집합니다. 새 효과는 이 값과 독립적입니다.',legacy:true};
  const entry=(catalog.entries||[]).find(e=>e.id===id)||{};
  const [domain,name,field]=id.split('.');
  const requestTitle=kind=>(catalog.entries||[]).find(e=>e.id===`request.${kind}.enabled`)?.section||kind;
  const typeTitle=kind=>(catalog.entries||[]).find(e=>e.id.startsWith(`type.${kind}.`))?.section||kind;
  const info=(when,affects,detail='')=>({when,affects,detail});
  const allRequests='운송 중 일반 요청 전체 16종(분실물 제외)';
  if(domain==='request'){
   const target=requestTitle(name)+' 요청';
   if(name==='LostItem')return info('하차 후 별도 사건을 기록할 때','분실물 텍스트 사건만',field==='enabled'?'사용 여부만 조건부 10% 사건을 켜거나 끕니다.':'일반 운송 중에는 사용하지 않는 고정/진단용 값입니다.');
   if(field==='enabled'||field==='weight')return info('새 운송에서 요청 후보를 배정할 때',target+'의 후보 포함·추첨 비중','가중치는 조건이 같은 후보 사이의 상대 비중입니다. 별도로 배정되는 대표 요청과 실제 표시 기회도 영향을 줍니다.');
   if(field==='prompt')return info('이 요청을 승객에게서 제시할 때',target+'의 승객 대사만');
   if(field==='reply')return info('해당 선택지를 보여주고 기사 답변을 기록할 때',target+'의 해당 답변 문구만','문구를 바꿔도 선택 행동과 점수 판정 방식은 유지합니다.');
   if(field==='failure'&&!entry.editable)return {...info('정상 운송에서는 적용하지 않는 참고값',target+'의 미사용 실패 항목','이 요청은 실제 실패 판정 경로가 없습니다. 답하지 않았을 때의 감점은 별도 미응답 설정을 사용합니다.'),unused:true};
   if(field==='unanswered')return info('답하지 않은 채 하차·운송 종료할 때',target+'만',name==='Quiet'?'안전한 응답 기회 후 미응답일 때 M 변화량입니다. -2를 입력하면 M 2점 감점, 0이면 감점 없음입니다. 입력 범위는 -100~0점이며 요청 합계 상한에는 들어가지 않습니다.':['Empathy','Interview','LocalChat'].includes(name)?'안전한 응답 기회 후 미응답일 때 요청 만족도에 한 번 반영합니다. 기회가 없으면 0입니다.':'현재 이 요청에서는 사용하지 않는 고정/중립 값입니다.');
   return info(entry.editable?'요청을 수락한 뒤 하차 때 이행 여부를 평가할 때':'현행 운송의 고정 결과를 설명할 때',target+'의 수락 답변 전체','현재는 요청 종류별 평가 점수입니다. 선택지별 독립 점수는 지원하지 않으며, 버튼을 누를 때와 하차 때 두 번 더하지 않습니다.');
  }
  if(domain==='schedule')return info(name.startsWith('count')?'새 운송의 요청 개수를 배정할 때':'새 운송의 등장 시점을 정하고 운행 중 표시 기회를 확인할 때',allRequests,name.startsWith('count')?'0·1·2건 비중 합계는 100입니다. 개수 배정 뒤 각 요청의 가중치를 사용합니다.':name==='movingSeconds'?'운행 중 요청을 표시하기 전 필요한 실제 주행 시간입니다. 주행 중에도 대화가 가능하면 등장하며 다른 선택·작업 중에는 기다립니다.':'진행률은 0~1 값입니다. 예: 0.25는 25%. 주행 시간과 대화 가능 조건도 함께 충족해야 합니다. 적재·휴식 등 실제 작업은 안전 정차가 필요합니다.');
  if(domain==='type'&&name==='Drunk'&&['spawnPercent','dailyMax','forceEnabled','forceAfterMisses','sleepPercent','vomitPercent'].includes(field))return info(['sleepPercent','vomitPercent'].includes(field)?'새 취객의 상태를 생성할 때':'새 승객 유형을 배정할 때','취객 등장·잠듦·구토 상태',entry.description+' 다음 새 캠페인/새 PVP 방부터 적용하며 이미 배정된 상태는 다시 추첨하지 않습니다.');
  if(domain==='type'&&name==='Sleeping'&&field==='weight')return info('일반 승객 차례·취객 우선 추첨 뒤 특수 유형을 배정할 때','잠자는 승객 유형의 등장 빈도','취객이 배정되지 않은 차례의 특수 유형 추첨(30% 기준)에서 후보 간 상대 가중치를 사용합니다. 가중치 1은 전체 승객의 1%가 아닙니다. 특수 승객 뒤 다음 탑승은 일반 승객이며, 취객의 잠듦 확률은 취객 배정 설정에서 별도로 편집합니다.');
  if(domain==='type')return info(field==='weight'?'새 승객의 유형을 배정할 때':field==='intro'?'해당 유형의 첫 대화를 보여줄 때':field==='choice'?'기사 응답 선택지를 보여줄 때':'해당 유형의 운행 중 후속 대화를 보여줄 때',typeTitle(name)+' 승객만',field==='weight'?'특수 유형 후보 사이의 상대 비중입니다. 일반·취객 보장과 대표 요청 자격은 별도입니다.':'빈 답변·후속 문구는 현재 상태에 맞는 내장 대사를 사용합니다.');
  if(domain==='situation')return info('해당 상황에 대한 기사 답변을 보여줄 때',({SignalWait:'신호 대기',Jolt:'차량 흔들림',NecessaryBrake:'필요한 제동'})[name]+' 상황 대화만','후속 질문 제한 선택은 이 운송의 새로운 추가 상황 대화를 막습니다. 별도 예약 요청은 취소하지 않습니다.');
  if(domain==='trait'||domain==='effect'&&['nausea','temperature','umbrella'].includes(name))return info('해당 특성이 있는 승객의 상태·응대를 처리할 때',({nausea:'메스꺼움',temperature:'온도 민감',umbrella:'젖은 우산'})[name],entry.description);
  if(domain==='effect'){
   const effects={
    'food.satisfaction':['음식 허용을 선택하고 승차한 뒤','음식 승객의 섭취 허용 선택','요청 합계와 별도의 승객 유형 효과입니다.'],
    'food.crumbs':['운행 중 실제로 음식을 섭취했을 때','음식 승객의 섭취 허용 선택','청결 변화량입니다. -2는 청결 2점 감소, 0은 감소 없음입니다. 허락만 하고 아직 먹지 않았다면 적용하지 않습니다.'],
    'food.spill':['실제 섭취 후 흘림이 발생했을 때','음식 승객의 섭취 허용 선택','섭취 효과에 추가되는 청결 변화량입니다. -2는 2점 추가 감소, 0은 추가 감소 없음입니다.'],
    'food.spillPercent':['새 음식 승객의 흘림 예정 여부를 정할 때','음식 승객의 섭취 허용 선택','실제 섭취와 흘림 조건이 충족될 때 청결에 반영합니다.'],
    'food.spillDelaySeconds':['섭취 뒤 흘림까지 주행 시간을 셀 때','음식 승객의 섭취 허용 선택','물리 흔들림이 먼저 오면 예정된 흘림이 더 일찍 발생할 수 있습니다.'],
    'food.consumeMovingSeconds':['섭취 시작 조건을 확인할 때','음식 승객의 섭취 허용 선택','주행 시간과 진행률을 모두 충족하고 진행률 85% 전이어야 합니다.'],
    'food.consumeProgress':['섭취 시작 조건을 확인할 때','음식 승객의 섭취 허용 선택','주행 시간과 진행률을 모두 충족합니다. 예: 0.5는 50%.'],
    'food.cleanupSeconds':['정리 선택 뒤 안전 정차 중','음식 승객의 정리 선택','정리 후 먹지 않고 보관합니다.'],
    'luggage.prepareSeconds':['적재 허용 뒤 안전 정차 중','큰 짐 승객의 적재 허용 선택'],
    'luggage.unloadSeconds':['목적지 도착 뒤 안전 정차 중','큰 짐 승객의 적재 허용 선택'],
    'muddy.prepareSeconds':['깔개 보호 선택 뒤 안전 정차 중','오염 짐 승객의 깔개 보호 선택'],
    'muddy.dirt':['보호 없이 실은 짐을 내리거나 운송을 중도 종료할 때','차량 청결도 · 보호 없이 적재한 오염 짐','차량 청결도(0~100)에 한 번 적용합니다. -5이면 100→95, 0이면 추가 감소 없음입니다. 승객 만족도·기사 피로도는 직접 바뀌지 않습니다. 청결도 80 이상은 승차 만족도 기본 +3, 30 미만은 세차가 필요합니다.'],
    'sleep.calmSeconds':['흔들림으로 깨어난 뒤 다시 잠들 때까지','수면 승객','응답 대기 중에는 시간이 끝나도 바로 잠들지 않습니다.'],
    'resecure.seconds':['짐 재고정 응답 뒤 안전 정차 중','큰 짐·오염 짐을 실제로 실은 승객','흔들림·필요한 제동 상황의 재고정 선택이 공유합니다.'],
    'errand.seconds':['경유 약정 수락 후 경유지에서 F로 안전 정차했을 때','경유 용무 요청','그 자리에서 무료 대기하며, 원래 목적지까지 같은 운송을 계속합니다.']
   };
   const value=effects[name+'.'+field];if(value)return info(...value);
  }
  if(domain==='common'){
   const common={
    requestCap:['하차 때 요청 점수를 합산한 뒤',allRequests,'상한이 10일 때 +8점과 +5점은 합계 +13점 대신 +10점으로 반영합니다. M·청결·시간 가산은 이 상한과 별도입니다.'],
    timeCap:['하차 때 안전한 조기 도착 가산을 계산할 때','유효한 기준 ETA가 있는 모든 운송'],
    timeFactor:['하차 때 안전한 조기 도착 가산을 계산할 때','유효한 기준 ETA가 있는 모든 운송'],
    comfortStep:['안정 주행 관찰 후 M 회복량이 없고 만족도가 요구치보다 낮을 때','운송 중 안심 회복','요청 하나의 이행 보상이 아닌 별도 회복입니다.'],
    comfortCap:['안정 주행 관찰 후 안심 회복을 누적할 때','운송 중 안심 회복','한 운송의 누적 상한입니다.'],
    fareMultiplier:['하차 정산 때 기본 지급을 계산할 때','모든 운송의 기본 운임','특정 요청 전용 값이 아닙니다.'],
    bonusMultiplier:['하차 정산 때 보너스를 계산할 때','모든 운송의 보너스'],
    deadlineMultiplier:['새 호출·목적지 변경·경유 약정을 만들 때','모든 신규 운송 약정; 제시간 도착·안전 하차·목적지 변경·경유 용무 요청'],
    deadlineSlack:['새 호출·목적지 변경·경유 약정을 만들 때','모든 신규 운송 약정; 제시간 도착·안전 하차·목적지 변경·경유 용무 요청'],
    startDeficitMultiplier:['새 운송의 시작 만족도를 정할 때','모든 승객의 시작 만족도'],
    cleanBonusMultiplier:['승차 때 청결 가산을 계산할 때','모든 승객의 시작 만족도','청결은 시작값에 포함되며 하차 때 다시 더하지 않습니다.'],
    required:['새 일반 호출의 요구 만족도를 정할 때','해당 난도의 신규 일반 호출','현재 운송 중인 약정을 소급해서 바꾸지 않습니다.']
   };if(common[name])return info(...common[name]);
  }
  if(domain==='law')return info(name==='recovery'?'위반 없이 준수한 법규 사건을 기록할 때':'해당 위반 상황을 관찰·확정할 때',name==='recovery'?'모든 운전의 법규 점수 회복':'해당 상황의 법규 점수; 제시간 도착 요청은 실제 위반 유무도 확인',(field==='penalty'?'-2는 법규 점수 2점 감점, 0은 감점 없음입니다. ':'')+'법규 점수와 운전 만족도 M은 서로 다른 지표입니다.');
  if(domain==='manner')return info(name==='recovery'?'새 구간에서 안정 주행의 거리 또는 시간 조건에 도달할 때':'해당 운전 사건을 관찰할 때',name==='comfortMotion'?'편안한 운행 요청 수락 뒤의 M':name==='recovery'?'모든 승객 운송의 M 회복':'모든 승객 운송의 운전 만족도 M',(field==='penalty'?'-2는 M 2점 감점, 0은 감점 없음입니다. ':'')+'M 변화는 하차 만족도의 주행 변화량에 반영합니다. 요청 이행 점수와 별도입니다.');
  return info('설명된 상황·조건을 확인할 때',entry.section||entry.title||'현재 게임의 고정 조건',entry.editable?'':'현재 구현의 고정 조건이며 값을 편집하지 않습니다.');
 }
 function displayCatalog(catalog){
  const titles={'effect.muddy.dirt':'오염 짐 · 차량 청결도 변화','effect.food.crumbs':'음식 부스러기 · 차량 청결도 변화','effect.food.spill':'음식 흘림 · 차량 청결도 추가 변화'};
  for(const e of catalog.entries||[])if(titles[e.id])e.title=titles[e.id];
  return catalog;
 }
 globalThis.ContentModel={build,weightInfo,fieldInfo,isLegacyField,displayCatalog};
})();
