export const DRUNK_SETTINGS=[
 ['spawnPercent','취객 · 등장 추첨 확률','integer','30',0,100,'%','일반 승객 차례를 제외한 배정 가능 차례의 확률입니다. 강제 등장은 별도이며 최대 인원이 우선합니다.'],
 ['dailyMax','취객 · 하루 최대 인원','integer','2',0,100,'명','0이면 취객을 배정하지 않습니다. 특수 승객 뒤 일반 승객 차례는 유지합니다.'],
 ['forceEnabled','취객 · 강제 등장 사용','boolean','true',0,1,'','취객 없이 탑승한 횟수가 기준에 도달하면 다음 배정 가능 차례에 취객을 배정합니다.'],
 ['forceAfterMisses','취객 · 강제 등장 전 취객 없는 탑승 횟수','integer','3',1,1000,'회','수락 취소는 세지 않고 실제 탑승을 셉니다. 일반 승객 차례와 하루 최대 인원이 우선합니다.'],
 ['sleepPercent','취객 · 잠드는 확률','integer','30',0,100,'%','배정된 취객의 하차 시 깨우기 상태를 정하는 확률입니다. 수면 승객 유형의 등장 가중치와 별개입니다.'],
 ['vomitPercent','취객 · 구토 예정 확률','integer','25',0,100,'%','취객 배정 때 구토 예정을 정합니다. 경고 후 도착 전에 시간이 지나야 실제 구토가 발생합니다. 잠듦과 독립 추첨입니다.']
].map(([key,title,type,defaultValue,min,max,unit,description])=>({id:'type.Drunk.'+key,group:'대화 선택지',section:'취객',title,type,defaultValue,min,max,unit,description,affects:['취객 배정·상태'],modes:['single','pvp'],status:'implemented',consumer:'VehicleCare.ReservePassenger / DrunkRideState.CreateAssignedDrunk',editable:true}));
