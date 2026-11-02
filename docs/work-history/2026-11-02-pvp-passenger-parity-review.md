# 2026-11-02 싱글 승객 요소의 PVP 반영 점검

## 범위와 기준
사용자 요청: 싱글 승객 요소가 PVP에 모두 반영됐는지 확인. HEAD/최신원격main f907e68, 시작작업트리clean. 기획02/프로토범위/체크리스트와 현재코드 정적대조. 게임실행·PC입력·코드수정·새빌드·새자동검사 없음. 기존검사기록은새실행검사로표시하지않는다. 다른체크아웃6244be9/미추적7항목보존.

## 요소별 결과
| 요소 | 현재 PVP 연결 | 제한/판정 |
|---|---|---|
| 승객 유형 | 일반 + 특수11유형/34변주, 같은 PassengerBehaviorState/VehicleCare.ReservePassenger | 코드연결됨. 실제 PVP에서전유형/변주전수수용은없음 |
| 승객 요청 | 같은17종 RequestKind·0~2건배정·안전정차응답·요청합산평가 | LostItem은하차후텍스트사건. 큰길/경유/목적지변경/안전하차는후보조건에따라제외, 싱글도같은안전규칙 |
| 취객 | 구토/기상대기/청결저하/세차전승차거부 | 같은DrunkRideState/VehicleCare/UpdateCare경로 |
| 수면/대화 | 같은유형선택/소개/후속/조용한배려·공통대화예산 | 코드연결, 실제 PVP 전유형대화전수미검증 |
| 짐/오염짐/음식 | 적재/하역·보호선택·안전작업·섭취/흘림/청결사건 | 같은BehaviorWorkProtected/AdvanceBehavior/ApplyBehaviorDirt. 봇선택은고정 |
| 만족도/운임 | 승차시작상태·청결보너스·M/S/R·실제주행/요청·운임/지각0원 | 같은Board/CompleteDelivery·PVP Ledger에중복없이실제운임정산 |
| 승하차/기한 | 방향/연석/저속·픽업기한·운송기한·경유F·피로/고장취소 | 같은PassengerJobs.18개자체도시거점, 은계지점그대로복제아님 |
| 호출 | 공용8개/선점·보충·해제·짧은/장거리교대 | 싱글3호출과다른PVP전용배정 |
| 호출밖 길가승객 | 미반영 | HasWaitingPassenger는simulationMatch에서roadsideWaiting을사용하지않음. F는공유호출에있는승객만수락/탑승가능 |
| 지역/단계별 수요 | 미반영 | CreateSharedCandidate는serial순회. NormalRank/OfferRank/PassengerDemandPolicy 우선순위미사용. 기존지역표도EG-GAME-ID전용, PVP S01~S18은neutral |
| 임시 승하차공간 점유 | PVP도시에서미작동 | human에StopOccupancySystem은있으나Catalog추가검증지점에만Spawn가능. PVP18지점은전부legacy이고추가지점없음. 봇세션에는해당컴포넌트도없음 |
| 은계 전용 상황운송 | 미적용 | ServiceLawZones.Start는mapId==eungye조건. PVP도시에는은계PS상황/고정관찰코스없음. 일반주행법규/승객요청은별도공통경로 |
| 봇 응대 | 부분 반영 | 같은승객상태/요청컴포넌트는있으나SafeToRespond때requests.Respond(false), Behavior.Respond(0) 고정. 요청보류·유형첫선택이라사람과같은전략응대아님 |
| 캠페인 저장/날짜/예약 재개 | 의도적분리 | 경기메모리전용·SavePath빈값. 승객예약/일일한도상태는경기내ledger사용, 싱글날짜/저장이어하기미적용 |

## 근거 위치
- unity/EungyeTaxi/Assets/Scripts/PassengerJobs.cs: HasWaitingPassenger·Generate·CreateSharedCandidate·Interact·Board·CompleteDelivery·AdvanceBehavior.
- PassengerBehavior.cs·PassengerRequestUI.cs/PassengerRequestObservation.cs·VehicleCare.cs: 유형/요청/사건공통로직.
- PvpSimulationMatch.cs/CreateBot: 각봇의PassengerJobs/BusinessProgress/PassengerRequestUI/PassengerBehaviorUI연결.
- PvpSimulationBot.cs/FixedUpdate: 요청false/유형0고정응답.
- PassengerStopCatalog.cs/Apply: eungye이외18legacy지점만, StopOccupancySystem.cs/Spawn의IsAdditional조건.
- PassengerDemandPolicy.cs/Region과PassengerJobs.CreateSharedCandidate: PVP지역수요미연결.
- ServiceLawZones.cs/Start: eungye전용.

## 결론/후속
“모든승객요소반영완료”로판정할수없다. 핵심유형·운송·요청·만족도·취객/짐/음식공통로직은연결됐지만, 수요/독립길가승객/공간점유와봇응대는차이또는누락이다. PVP전용기획으로공유규칙을정한후연동해야한다. 공유호출에는타입/선택예약스냅샷이없고승차차량의Care가유형을예약하므로, 동일호출의유형을모든경쟁자에게공통확정하는계약도추가검토대상이다.

우선순위: PVP도시권역수요/공간점유공유와봇응대정책 → 공유호출/길가승객통합여부 → 실제PVP전유형·요청·사건수용. 기존4대장기검사는봇1교통정체로실패했으며유형전수검증근거가아니다. 이번작업은점검기록만변경하며실행본/manifest/지도/싱글소스유지.
