// Index values persist in saved profiles. Append new artwork at the end of each list.
export const backgrounds={diner:'기사식당',fuel:'주유소',wash:'세차장',workshop:'공업사',rest:'휴식 구역',parking:'야외 주차장',garage:'덕성프라자 1층 주차장',room:'기사 숙소'};
export const meals={pork:'제육백반',stew:'된장찌개 정식',soup:'소고기국밥'};
const rows=[];
function field(id,group,section,title,type,value,min,max,description,consumer,modes=['single','pvp']){
 rows.push({id,group,section,title,type,defaultValue:String(value),min,max,unit:'',description,affects:[section],modes,status:'implemented',consumer,editable:true});
}
for(const [category,items] of Object.entries({background:backgrounds,meal:meals})){
 const choices=Object.entries(items);
 choices.forEach(([key,label],index)=>{
  field('display.'+category+'.'+key,'생활 시설',category==='meal'?'메뉴 그림':'시설 배경',label,'integer',index,0,choices.length-1,'이 자리에 표시할 승인 그림을 선택합니다.','FacilityArt');
  if(category==='meal')field('display.menuName.'+key,'생활 시설','메뉴 이름',label+' 이름','text',label,null,null,'선택 카드와 식사 안내에 표시합니다.','ServiceStops');
 });
}
for(const [key,title] of Object.entries({engine:'주행음',brake:'브레이크음',horn:'경적 · 빵빵',rain:'빗소리'}))
 field('display.sound.'+key,'차량','차량 효과음',title,'boolean',true,null,null,'프로필에서 사용하고 운전자의 게임 소리 설정도 켜져 있을 때 재생합니다.','VehicleAudio');
const areas={
 scenario:['수인로 서쪽','계수로 동쪽','은행로173번길','은계중앙로 동쪽','수인로 남쪽 일대','은계로 북쪽','은계로 남쪽','은계남로 중부','계수로 서쪽','대은로104번길','검바위1로','은계번영길'],
 bay:['은계중앙1길','은계남로 서쪽','은계중앙로 남동쪽','은행고길 북쪽','은계로 남단 일대','은계호수로 인근','은계중앙로 남쪽','은계중앙로 북동쪽','계수로 중부','은계로 중부','은행고길 남쪽','은계남로 동쪽'],
 alternate:['은계남로 서쪽 대체 정차면']
};
for(const [kind,labels] of Object.entries(areas))labels.forEach((label,i)=>field('stopName.'+kind+'.'+(i+1),'승하차 지명','은계 승하차 지명',label,'text',label,null,null,'같은 지점의 반대편은 이 이름 뒤에 반대편을 붙입니다.','KoreanText.Stop',['single']));
function wish(key,title,type,value,min,max,description){
 field('cabin.wish.'+key,'승객 요청','창문·온도 조작 요청',title,type,value,min,max,description,'PassengerJobs / PassengerBehavior',['single']);
}
wish('window','창문 요청 사용','boolean',true,null,null,'배정된 손님이 승차 때 창문을 열거나 닫아 달라고 합니다.');
wish('temperature','온도 요청 사용','boolean',true,null,null,'배정된 손님이 희망 온도로 맞춰 달라고 합니다.');
wish('oneIn','일반 승객 중 추첨 비율 (1/N)','integer',3,1,100,'1이면 모든 적격 손님, 3이면 적격 손님의 약 3분의 1입니다.');
wish('reward','이행 만족도','integer',2,0,20,'정확한 조작을 완료한 운송에 한 번 반영합니다.');
wish('openLine','창문 열기 대사','text','조금 답답하네요. 창문 좀 내려 주시겠어요?',null,null,'조작 키 안내는 게임이 붙입니다.');
wish('closeLine','창문 닫기 대사','text','바람이 차네요. 창문 좀 올려 주시겠어요?',null,null,'조작 키 안내는 게임이 붙입니다.');
wish('temperatureLine','온도 요청 대사','text','실내 온도를 {온도}도로 맞춰 주시겠어요?',null,null,'{온도}에 손님의 목표 온도가 표시됩니다.');
wish('thanks','이행 후 대사','text','네, 딱 좋아요. 고맙습니다!',null,null,'승객이 요청을 이행한 뒤 하는 말입니다.');
export const SERVICE_SETTINGS=rows;
