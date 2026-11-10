// Plan 22 trial prices. Not a confirmed balance: R (human 20-minute net median) is not measured yet.
// Defaults assume R≈10,000원 (3-day goal 30,000원 ÷ 3): replacement spend ≈1.2~1.6R, decoration 0.15~0.2R.
export const VEHICLE_SETTINGS=[
 ['hybrid.price','하이브리드 세단 · 새 차 가격','integer','26000',0,1000000,'원','보상 판매액을 뺀 금액이 실제 지출입니다(기본 16,000원 ≈ 1.6R).'],
 ['lpg.price','넓은 LPG 택시 · 새 차 가격','integer','22000',0,1000000,'원','보상 판매액을 뺀 금액이 실제 지출입니다(기본 12,000원 ≈ 1.2R).'],
 ['tradeIn.base','시작 차 보상 판매 기준액','integer','10000',0,1000000,'원','내구도 100이면 전액, 내구도 0이면 절반을 받습니다(10원 단위 내림).'],
 ['color.price','차량 색상 · 두 번째 색 가격','integer','1500',0,100000,'원','차량별 1회. 방 소품과 같은 개인 지출입니다.'],
 ['ornament.price','대시보드 장식 가격','integer','2000',0,100000,'원','1회 구매 후 차를 바꿔도 옮겨 답니다.']
].map(([key,title,type,defaultValue,min,max,unit,description])=>({id:'vehicle.'+key,group:'차량',section:'차량 구매',title,type,defaultValue,min,max,unit,description:description+' 시험 기본값이며 확정 밸런스가 아닙니다. 다음 새 3일 회차 또는 다음 구매 견적부터 적용합니다.',affects:['차고 견적','개인 지출'],modes:['single'],status:'implemented',consumer:'VehicleCatalog.Price / TradeIn / ColorPrice / OrnamentPrice',editable:true}));
