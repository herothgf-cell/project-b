# 2026-11-24 08 5장 후보 7종 설계안 (구현하지 않음)

## 요구

자리 비움 일괄 작업(3차)의 2단계. 아래 일곱 항목은 지도·교통 AI·경제에 영향이 커서 사용자 결정이 필요하다. 항목별로 필요한 코드 변경 범위, 재사용 가능한 기존 시스템, 위험, 최소 시험판을 한 쪽씩 정리하고 비용 대비 효과 순으로 추천 순서를 붙인다. **구현은 하지 않았다.**

## 방법과 한계

- 저장소의 코드·지도 자료·기획 문서를 읽기만 했다. 지도 자료는 수정하지 않았다. 빌드 실행과 플레이는 하지 않았다.
- 코드 인용은 `파일` `클래스.메서드` 형식이고 모두 `unity/EungyeTaxi/Assets/Scripts/` 기준이다. 조사는 보조 에이전트가 했고, 핵심 사실 6건(제한속도 표지 0건, 과속 판정 구조, NPC 순항 속도 함수, 가장자리 승하차점 7곳, 방지턱 조사 기록, 암전 함수)은 직접 다시 대조했다. 나머지 인용은 구현 착수 전에 다시 확인해야 한다.
- 새 수치의 ID는 제안 이름이고 카탈로그에 없다. 모든 시험판은 기본값 0(꺼짐) 스위치로 넣는 것을 전제로 한다.
- 요약 표와 추천 순서는 [08 재미 보강 5.5](../content-design/08-fun-improvements.md)에도 있다.

## 먼저 알아 둘 사실 (항목 여러 개에 걸침)

| 사실 | 영향을 받는 항목 |
|---|---|
| **은계에서는 지금 과속이 판정되지 않는다.** 과속 판정은 지도 편집기의 "제한속도" 표지가 있을 때만 동작하는데 은계 지도에 그 표지가 0건이다. | 유혹과 단속 카메라, 도로 구간, 방지턱 |
| **돈으로 내는 법규 벌금이 없다.** 위반은 운전 점수 감점뿐이다. | 단속 카메라 |
| **일반 차량(NPC)은 차로 중심선만 따라간다.** 추월, 옆으로 비켜 주기, 비상등 반응, 연석 정차가 없고 저장되지 않는다. | 구급차, 끼어들기, 다른 택시, 따라가기 미션 |
| **버스가 없다.** 코드에도 게임 지도에도 버스·정류장·노선이 없다. | "저 버스 따라가 주세요" |
| **지도에 도로 등급·제한속도·방지턱·단속 카메라·택시 승강장 자료가 없다.** 도로는 이름으로만 구분된다(국도42 = "수인로", 골목 = "…번길"). | 도로 구간, 방지턱, 카메라, 승강장 |
| **사고 판정에 과실 구분이 없다.** | 돌발 상황 전부 |
| **승차감 판정은 앞뒤 가속도만 본다.** 위아래 충격은 재지 않는다. | 방지턱 |

공통 전제(모든 항목에 적용):

- 싱글 전용 분기는 기존 방식을 쓴다: 호출·미션은 `PassengerJobsCallBoard.cs` `PassengerJobs.CallBoardSingle`, 교통은 `TrafficSystem.EungyeSingle`. `TrafficCar`, `TrafficSystem`, `DrivingManners`, `DrivingLaw`, `TaxiController`는 PvP와 같이 쓰는 코드다.
- 새 수치는 `config/content/catalog.json`과 `Assets/Resources/Content/catalog.json`에 기존 `mission.*`, `chain.*`와 같은 형식(`modes:["single"]`)으로 등록하고 `ContentTuning.Number/Int(id, 기본값)`으로 읽는다.
- 저장 호환은 기존 방식대로 "새 필드는 0/false가 기존 동작"으로 둔다(`JobOffer.variety`, `JobOffer.mission`, `PassengerJobsSnapshot.hasChained`가 같은 방식). 일반 차량(NPC)은 저장되지 않으므로 NPC에 붙는 상태는 이어하기 때 사라진다.
- 지도 문서(`config/maps/project/eungye.project.json`)나 `eungye-road-facilities.json`을 바꾸면 `config/maps/local-input-manifest.json` 해시와 배포 manifest를 다시 만들어야 한다. 아래에서 "지도 변경 없음"이라고 쓴 안은 이 파일들을 건드리지 않는 안이다.

---

## 1. 돌발 상황 (구급차 양보, 킥보드, 문 여는 주차 차량, 끼어드는 차)

### 필요한 코드 변경 범위

- 새 파일 `RoadSurprises.cs`(가칭): `RoadConditions`처럼 `DriveSession`에 붙는 컴포넌트. 발생 추첨, 연출 물체 생성, 대처 판정, 손님 반응을 맡는다.
- 문 여는 차·킥보드는 새 물체만 있으면 된다. 지도 변경 없음, `tools/` 변경 없음. 규모 **중**.
- 끼어드는 차는 `TrafficCar.cs` `TrafficCar.LaneChangeYieldGap`(지금은 옆·뒤 차가 있으면 차로 변경 시작점에서 최대 6초 기다림)을 일부 차량이 건너뛰게 해야 한다. PvP 공용 코드라 `system.EungyeSingle` 조건이 필요하다. 규모 **중**.
- 구급차는 규모 **대**. NPC는 `TrafficRoute.Position(distance)`를 따라 차로 중심선 위만 움직이는 키네마틱 물체라 옆으로 비켜 주지 못하고, 구급차도 앞차를 추월하지 못한다. 신호를 무시하게 하려면 `TrafficSystem.CanEnter/MayEnter`의 교차로 예약 규칙에 예외가 필요하다.

### 재사용 가능한 기존 시스템

- 정차 차량: `RoadConditions.cs` `RoadConditions.TryCreateEvent/CreateVisuals`가 이미 90초마다(동시 2건까지) 차로 위에 "비상 정차 차량"·"정체 대기" 상자 차를 세운다. 문 여는 차는 이 차에 문 상자 하나를 더하면 된다.
- 무단횡단: `AmbientPedestrianSystem.cs`의 `AmbientPedestrianState.jaywalking`. 지금은 J5076의 한 명만 쓴다(기획 01). 킥보드는 이 행인의 빠른 변형으로 만들 수 있다.
- 필요한 급제동 면제: `DrivingManners.NecessaryBraking`이 전방(레이어 8·9) 물체를 감지하면 급제동 감점을 빼 준다. 돌발 물체를 레이어 9로 만들면 그대로 적용된다.
- 잘한 대처 반응: `DrivingManners.Praise`, `GoodDriving.cs` `GoodDrivingRules.Line`, 팁 점수(`PassengerJobs.SettleGoodDriving`).
- 스크립트 차량: `DistrictTraffic.CreateRoute(시작 차로, 위치, 목표 차로, 위치)`로 경로를 만들고 `TrafficSystem.Spawn` 또는 `car.route=...; car.Place(at)`으로 원하는 길을 달리게 할 수 있다.
- 비상등: `TaxiSignals.ToggleHazards`(G 키)가 있다. NPC가 비상등에 반응하는 코드는 없다.

### 위험

- **사고 판정에 과실 구분이 없다.** `AccidentController`에서 과실을 가르는 코드를 찾지 못했고, 기획 04도 "현실 과실 판정 제외"라고 적었다. 끼어든 차나 열린 문에 닿으면 플레이어 사고(수리비·감점)로 처리될 가능성이 높다. 돌발이 "억울한 벌"이 되지 않게 접촉 예외를 따로 정해야 한다.
- 킥보드는 사람 사고(`AccidentKind.Person`, 감점 10~40)로 이어진다. 기획 01은 보행자를 "수익 방해물이 아니라 보호 대상"으로 정했다. 갑자기 튀어나오는 연출은 이 방향과 부딪힐 수 있다.
- 구급차·끼어들기는 교차로 예약(`TrafficJunction.reservations`)과 차로 변경 대기에 손을 대므로 교착 위험이 있다. `JunctionGridlockProbe`, `TrafficFlowProbe` 재검증이 필요하다.
- PvP 공용 코드(`TrafficCar`)를 바꾸면 LAN 버전을 올려야 한다.

### 최소 시험판

- **넣는 것:** "문 여는 정차 차량" 한 종류. 길가 쪽 차로에 선 정차 차량 옆을 플레이어가 지나가려 할 때 문이 열린다. 멈추거나 차로를 바꿔 피하면 손님 한마디(가끔 팁 점수), 닿으면 기존 충돌 규칙.
- **빼는 것:** 구급차, 킥보드, 끼어드는 차, 사고 과실 예외.
- **수치 ID:** `surprise.enabled`(기본 0), `surprise.perShiftMax`(1), `surprise.door.triggerMetres`(18), `surprise.door.openSeconds`(4), `surprise.reaction.chance`(50).
- **확인:** 위치 제어 검사로 "문 열림 → 정지 → 급제동 감점 없음(`necessaryBrakeEvents` 증가)", 사람이 20분 플레이해 놀람이 재미인지 짜증인지 기록.

---

## 2. 성격이 다른 도로 구간 (골목 지름길, 국도42 구간)

### 필요한 코드 변경 범위

- 지도에 도로 등급 값이 없다. `MapData.cs` `MapRoad`는 `id, name, width, sourceLanes, lanes, oneWay`뿐이고 `HdEdge`에는 `roadName`만 있다. 구분은 도로 이름으로 해야 한다(국도42 = `roadName` "수인로", 골목 = "…번길").
- 새 파일 `RoadCharacter.cs`(가칭): 도로 이름 → 성격(간선/일반/골목) 표. 지도 파일은 안 바꾼다.
- 국도42: `TrafficCar.DistrictCruiseSpeed`가 은계 싱글의 모든 도로에서 순항 13m/s(약 47km/h) 고정이다. 수인로만 올리려면 이 메서드에 도로별 상한을 넣는다(공용 코드, `EungyeSingle` 조건). 규모 **소~중**.
- 골목 주차 차량: 상시 주차 차량 물체가 게임에 없다. 새로 만들어 골목 차로 가장자리에 세워야 한다. 규모 **중**. `tools/` 변경 없음.
- 대안으로 지도 편집기의 "제한속도" 표지(`RoadMapFacility.speed`)를 쓰면 NPC 속도(`TrafficSystem.EditorSpeedLimit`), 길안내 예상 시간(`RoadMapRules.TravelSeconds`), 과속 판정(`DrivingLaw.CheckEditorSpeed`)이 한 번에 붙는다. 다만 지도 문서 변경이라 해시 재생성이 필요하고, 실제 제한속도 조사가 먼저다.

### 재사용 가능한 기존 시스템

- 골목 표시: 지도 문서에 "차선 없는 생활도로" 표시가 23건 있고(`ReviewedUnmarkedRoadPaint.cs`), "번길" 이름의 차로가 합계 약 3km 있다.
- 긁힘 비용: `VehicleDamageController`(내구도·수리), `AccidentController`(시설 사고 경미 3점).
- 지름길의 가치: 길안내가 예상 시간으로 길을 고르는 구조(`RoadNavigation.cs`)와 급행 미션(`MissionRules.ExpressTarget`).
- 시원한 감각: 2026-11-24에 넣은 바람·엔진 부하 소리(`DriveFeelAudio.cs`).
- 회전교차로(J5074_877, `ReviewedRoundaboutControl.cs`)와 고가(`MayuOverpass.cs`)는 이미 있다.

### 위험

- **교착:** NPC는 경로 위 폭 1.9m 상자로 전방을 살핀다(`TrafficCar.Tick`). 주차 차량이 이 상자에 걸리면 NPC가 그 뒤에 계속 선다(추월 없음). 차로 폭 3.5m 골목에 폭 1.8m 주차 차량을 걸치지 않게 세울 자리가 실제로 있는지 구간별 확인이 필요하다.
- `DistrictTraffic`는 경로의 정적 장애물 검사 결과를 캐시한다(`cleared/blocked`). 주차 차량을 나중에 놓으면 캐시를 비워야 한다(`DistrictTraffic.LaneAvailabilityChanged`가 같은 처리를 함).
- 국도42는 게임 안에서 구간 길이 약 1.2km이고 신호 교차로가 있다. NPC 속도만 올려도 신호에 다시 걸리면 "시원함"이 안 생길 수 있다.
- 준법 방향: 지금 은계는 과속을 판정하지 않는다(끝의 사실 목록 참고). "속도를 낼 수 있는 구간"을 만들면 제한속도를 얼마로 볼지 결정이 따라온다. 실제 수인로 제한속도는 확인하지 못했다.
- 실제로 없는 주차 차량 배치는 게임 연출임을 밝혀야 한다(임의 배치 금지 원칙의 경계).

### 최소 시험판

- **넣는 것:** 수인로 직진 차로의 NPC 순항 상한만 올린다. 플레이어 쪽 규칙·물리는 그대로.
- **빼는 것:** 골목 주차 차량, 제한속도 표지, 과속 판정, 지도 변경.
- **수치 ID:** `road.character.enabled`(0), `road.route42.npcCruiseKmh`(60), `road.alley.npcCruiseKmh`(25, 2차).
- **확인:** `TrafficFlowProbe`·`JunctionGridlockProbe`로 교착 없음, 사람이 수인로 구간을 달려 "여기는 다르다"가 느껴지는지. 느껴지면 2차로 골목 1곳(조사 기록이 있는 P12 x876열)에 주차 차량 4~6대를 놓는다.

---

## 3. 시외 장거리 콜 (지도 밖 구간은 암전 + 시간 경과)

### 필요한 코드 변경 범위

- 새 partial `PassengerJobsOutOfTown.cs`(가칭) + 호출 성격 상수 추가(`JobOffer.variety`의 0~4는 `CallBoardRules`, 5·6은 `IdleTimeRules`가 사용 중이므로 7).
- 흐름: 호출 수락 → 지도 가장자리 승하차점까지 실제 운전 → 정차 후 암전 → 시계·연료·피로 일괄 경과 → 같은 지점 반대편 연석에서 재개 → 운임 정산.
- 암전 화면과 재배치 코드가 필요하다. 지도 변경 없음, `tools/` 변경 없음. 규모 **중**.

### 재사용 가능한 기존 시스템

- 가장자리 지점: 승하차 카탈로그에 `outer-link` 분류 7곳이 이미 있다(`config/maps/eungye-passenger-stops.json`, 전부 `game-only-field-unverified`). 반대편 연석은 `PassengerStopCatalog.OppositeIndex`.
- 시간 경과: `EconomyLedger.AdvanceOperation(초, 미터, 탑승 여부)` 한 번으로 영업 시계, 연료(`CampaignAccount.Consume`), 피로(`DriverCondition.Advance`), 청결, 공차/탑승 집계(`OperationMetrics.Observe`)가 같이 간다. 갈 때는 탑승, 올 때는 공차로 두 번 부른다.
- 암전: `AccidentController.DrawFade`와 `VehicleDamageController`가 검은 전체 화면을 `fadeUntil`까지 그리는 방식을 쓴다.
- 재배치: `TaxiController.SetSpawn` + `ResetTaxi` + `DistrictTraffic.ClearPlayerOverlap`(이어하기 복원에서 쓰는 조합).
- 사전 차단: `PassengerJobs.OrdinaryOffer`가 `CampaignAccount.FuelRequired`로 연료가 모자란 호출을 걸러낸다. 같은 자리에서 남은 영업 시간(`EconomyLedger.SecondsLeft`)도 걸러낼 수 있다.
- 정산: `BusinessProgress.RecordFare` → `CampaignAccount.Fare("fare:<trip>")`. 영수증 줄은 팁·미션과 같은 방식.

### 위험

- **경제:** 기획 04 점검에서 1km 안팎 호출이 5,500~8,400원으로 이미 시흥 요율보다 15~75% 높고, 차 한 대가 22,000원이다. 시외 콜 운임을 거리로 계산하면 한 건으로 차를 산다. 운임은 "쓰는 영업 시간 × 평소 분당 수입 × 1.0~1.3"으로 묶어야 한다.
- 영업 시간은 10/20/30/60분 선택이다(`PrototypeSettings.ShiftMinuteChoices`). 10분 영업에서는 시외 콜이 하루를 통째로 먹는다. 영업 길이에 비례한 상한이 필요하다.
- 시계가 마감을 넘으면 `EconomyLedger.BudgetCrossed`가 동작한다. "퇴근 전 마지막 한 콜"·연속 배차·살아 있는 미터기(`meter.*`)와 겹치는 경우를 정해야 한다.
- 암전 중 저장: 경과 처리와 재배치를 한 번에 끝낸 뒤 `SaveCheckpoint`를 불러야 이어하기에서 두 번 경과되지 않는다.
- 암전 동안 라디오 일정(`RadioSchedule`), 손님 요청, 시간대 수요 단계가 건너뛴다. 처리 방침이 필요하다.
- 가장자리 지점 7곳은 실측이 아닌 게임 전용 지점이다. "○○ 방면" 같은 실제 지명 표기는 근거를 확인한 뒤에 붙인다.

### 최소 시험판

- **넣는 것:** 호출판에 하루 최대 1건 "시외 왕복" 카드(쓰는 시간·운임·돌아오는 지점 표시). 가장자리 지점 1곳 고정. 암전 3초와 "시외 구간 · 영업 시간 N분 경과" 한 줄. 반대편 연석에서 재개.
- **빼는 것:** 암전 중 대화·이벤트, 통행료, 시외 손님 전용 이야기, 여러 방면.
- **수치 ID:** `offer.outOfTown.enabled`(0), `.dailyMax`(1), `.offMapMetres`(8000), `.secondsPerKm`(40), `.farePerMinute`(600), `.maxShiftShare`(0.35), `.minSecondsLeft`.
- **확인:** 위치 제어 검사로 경과 전후 시계·연료·피로·장부(`fare:` 한 줄)와 이어하기 뒤 한 번만 반영되는지, 20분 영업 3일 모의 장부로 일반 호출만 돈 날과 수입 비교.

---

## 4. 유혹과 단속 카메라 ("좀 밟아 주세요" + 카메라 벌금)

### 필요한 코드 변경 범위

- 유혹: `PassengerRequest.cs` `RequestKind` 끝에 `Hurry` 추가(저장값이 정수라 끝에만 추가). 선택지·대사는 `PassengerRequestUI.cs`, 판정은 `PassengerRequestObservation.cs`. 규모 **소~중**.
- 과속 판정: 은계에는 지금 과속 판정이 없다. `DrivingLaw.CheckEditorSpeed`는 지도 편집기의 "제한속도" 표지가 있을 때만 동작하는데 은계 지도 문서에 그 표지가 0건이다. 도로별 제한속도 값부터 필요하다.
- 카메라: 새 파일 `SpeedCameras.cs`(가칭) + 위치 자료 파일(새 `config/maps/eungye-speed-cameras.json` 또는 지도 문서 시설). 지도 입력이 늘어나므로 manifest 등록·해시 재생성 대상이다. 규모 **중 + 현장 조사**.
- 벌금: 돈으로 내는 법규 벌금이 지금 없다. 위반은 `LawLedger` 운전 점수 감점뿐이고 그 점수가 다음 승차 운임을 보정한다. `CampaignAccount`에 `Fine(id, day, amount)`를 새로 넣어야 한다(`CampaignAccount.Accident`처럼 `Mandatory`를 쓰면 현금이 모자랄 때 미납으로 넘어간다).

### 재사용 가능한 기존 시스템

- 불법 요구 선례: `RequestKind.RejectIllegal`("신호는 지켜야 해요" 한 가지 응답, 수락 선택지 없음).
- 요청 출현 조절: `PassengerRequestSchedule.cs`의 `request.<Kind>.enabled` / `.weight`.
- 급한 손님: `JobOffer.urgency`, 호출판 "급한 손님"(`CallBoardRules.Rush`), 급행 미션의 "움직인 시간" 기준(`MissionRules.ExpressTarget`, `PassengerJobs.TickMission`).
- 팁 지급: `BusinessProgress.RecordTip` → `CampaignAccount.Tip`. 단 장부 ID가 `tip:<trip>` 하나라서 같은 운송의 두 번째 팁은 거부된다. 유혹 사례금은 `hurry:<trip>` 같은 별도 ID가 필요하다.
- 과속 관찰 수치: `law.editorSpeed.toleranceKmh`(3), `.seconds`(2), `.penalty`(5)가 이미 카탈로그에 있다.
- 다음 날 고지: 하루 준비 화면(`DayPreparationPresentation.cs`).

### 위험

- **준법 방향:** 수락하면 돈을 주는 구조는 과속을 권한다. 기획 01은 "단속 카메라는 현재 적용 범위와 혼합하지 않는다"고 적었고, 08 3장은 같은 이유로 타임어택을 뺐다. 소유자 결정이 필요한 지점이다.
- **임의 배치 금지:** OSM 점검 자료(`data/inspection/osm-eungye.json`)의 단속 카메라는 1건이고 고속도로(제한 100) 위라 게임 도로가 아니다. 일반도로 `maxspeed` 태그는 0건이다. 카메라 위치와 제한속도 모두 현장·거리뷰 조사 없이는 넣을 수 없다.
- 경제: 벌금액이 팁 기대값보다 작으면 항상 수락, 크면 항상 거절이 된다. 운임 5,500~8,400원 규모에 맞춰야 한다.
- 누적 등급 금지와의 관계: 벌금은 일회성 장부 줄로만 남기고 벌점 누적·면허 정지 같은 등급은 만들지 않는다.
- 유혹만 넣고 카메라가 없으면 걸릴 일이 없다(08 5장 정리와 같은 지적).

### 최소 시험판

- **넣는 것:** 카메라 없이 유혹만. 손님이 "좀 빨리 가 주세요, 더 드릴게요"를 말하고 수락/거절을 고른다. 수락 후 급행 미션과 같은 "움직인 시간" 목표 안에 도착하면 사례금. 대가는 기존 규칙(급가감속 M 감점, 신호·사고 감점)뿐이다. 거절하면 손님 반응만.
- **빼는 것:** 카메라, 돈 벌금, 제한속도 표지, 과속 판정.
- **수치 ID:** `request.Hurry.enabled`(0), `request.Hurry.weight`(1), `hurry.targetFactor`(0.9), `hurry.rewardPercent`(20), `hurry.maxPerShift`(1).
- **확인:** 수락/거절 비율이 한쪽으로 쏠리는지, 수락한 운송의 위반·사고 건수가 늘어나는지(늘면 준법 방향과 충돌하므로 폐기 근거). 2차(카메라)는 조사 결과가 나온 뒤 별도 승인.

---

## 5. 다른 택시와 승강장

### 필요한 코드 변경 범위

- 승강장: 새 파일 `TaxiRank.cs`(가칭). 대기 줄(정지한 택시 모형 0~3대), 플레이어 순번, 손님 도착 간격, 손님 호출 생성. 규모 **중**.
- 위치 자료: 지도와 OSM 어디에도 택시 승강장이 없다(OSM `amenity=taxi` 0건). 실제 위치를 조사해 넣으면 지도 입력 변경이다. 시험판은 기존 게임 전용 지점을 써서 지도 변경을 피한다.
- 다른 택시: NPC 차량은 전부 같은 상자 차(`TrafficCar.Initialize`, 4색)이고 택시 모양·연석 정차 동작이 없다. 길가 손님을 채 가게 하려면 `TrafficCar`에 정차 동작을 넣어야 한다(공용 코드). 규모 **대**.
- `tools/` 변경은 실제 승강장을 넣을 때만(승하차 카탈로그 생성기 `tools/build_passenger_stop_catalog.py`).

### 재사용 가능한 기존 시스템

- 정차 차량과 보호: `StopOccupancySystem.cs`가 게임 전용 승하차면에 "일시 정차 차량"을 세우고, 화면 밖 생성·주변 차량 확인·기한 보호(`FareTrip.ExtendForSystemDelay`)를 이미 한다. 줄 선 택시 모형에 그대로 쓸 수 있다.
- 길가 손님: `PassengerJobs.RefreshWaitingPassengers`(`PassengerWaitingPolicy.Present`, 120초마다 정류장별 35%), `cooldown[]`, `people[]`.
- 손님 호출 만들기: `PassengerJobs.Make(pickup, drop)`, 길가 승차 `PassengerJobs.AcceptRoadside`.
- 수요 가중: `PassengerDemandPolicy.Weight(지역, 시간대)`, `DailyConditions.DemandMultiplier`.
- 대기 집계: `OperationMetrics`의 `emptyWaitingSeconds`가 정차 대기 시간을 이미 따로 센다.
- 대기 중 볼거리: 2026-11-23 "정차 중 모아 보기"(`IdleTimeRules.StopBreak*`), 라디오.
- 게임 시설 선례: "은계 동부 휴식 주차구역 (게임시설)"처럼 게임 전용임을 이름에 밝힌 시설이 있다.

### 위험

- **임의 배치 금지:** 실제 승강장 위치 조사 전에는 "게임 전용 승강장"으로 표기해야 한다. 08 5.1은 3곳 후보를 적었지만 조사 전이다.
- 경제·균형: "줄 서면 좋은 손님 보장"은 대기만 하면 되는 우세 전략이 될 수 있다. 08 5.1의 "배회·호출이 항상 손해가 되면 안 됨"과 운임 점검(기획 04)을 같이 봐야 한다. 대기는 연료를 거의 안 쓰므로 수입/비용이 한쪽으로 기운다.
- 재미: 줄에서 기다리는 시간은 아무것도 안 하는 시간이다. 08의 진단 3("운전 중 빈 시간이 길다")을 악화시킬 수 있다.
- 다른 택시가 차로에 서면 뒤 NPC가 막힌다(NPC는 추월하지 않음). 연석 정차 동작 없이 차로 끝에서 멈추게 하면 교착이 난다.
- PvP에는 실제 경쟁 택시(`PvpSimulationBot`)가 있지만 PvP 전용 세션 구조라 싱글에 싸게 옮길 수 없다.

### 최소 시험판

- **넣는 것:** 게임 전용 승강장 1곳. 앞에 선 택시 0~3대가 보이고 일정 간격으로 한 대씩 빠진다. 플레이어가 줄 끝 칸에 서면 순번이 줄고, 차례가 되면 먼 목적지 손님 1명이 탄다. 언제든 나갈 수 있다(순번 잃음).
- **빼는 것:** 길가 손님을 채 가는 NPC 택시, 승강장 여러 곳, 숙련도 혼잡 표시, 대기 중 피로 회복.
- **수치 ID:** `rank.enabled`(0), `rank.queueMax`(3), `rank.arrivalSeconds`(45), `rank.longRideMinMetres`(1200), `rank.fareMultiplier`(1.0).
- **확인:** 대기 시간 포함 분당 수입을 호출·배회와 비교(`OperationMetrics` + 장부), 사람이 "기다릴지 돌지"를 실제로 고민하는지. 승강장만 쓰는 것이 항상 이기면 폐기 또는 손님 간격 조정.

---

## 6. 미션 "저 버스 따라가 주세요"

### 필요한 코드 변경 범위

- 버스가 없다. 스크립트 폴더 전체에서 bus/버스 검색 결과가 0건이고, 게임 지도에도 버스 정류장·노선 자료가 없다.
- 버스 크기 차량은 어렵다. `TrafficCar`의 충돌 상자(1.8×4.2m)와 전방·차로 변경 감지 상자가 승용차 크기로 코드에 고정돼 있어, 버스 크기로 바꾸면 회전·교차로 여유 검증을 다시 해야 한다. 규모 **대**.
- "저 차 따라가 주세요"(승용차 크기 표적)로 바꾸면 규모 **중**: `PassengerJobsMissions.cs` `MissionRules`에 `Follow=6` 추가(5는 같은 날 넣은 `KnowRoad`), `PassengerJobs.ApplyMissions`의 추첨 범위(`MissionRules.KindCount`) 확장, `TickMission`에 거리 판정, `JobOffer`에 누적 필드 추가, 표적 차량 생성·관리 코드.
- 지도 변경 없음, `tools/` 변경 없음.

### 재사용 가능한 기존 시스템

- 표적을 정해진 길로 보내기: `DistrictTraffic.CreateRoute`로 승차점→하차점 경로를 만들고 `TrafficSystem.Spawn(route, 0, index)`로 차를 낸다. 신호·앞차·보행자 규칙은 `TrafficCar.Tick`이 그대로 지킨다.
- 미션 틀: 호출판 표시(`PassengerJobs.MissionCardLine`), 성공 판정(`SettleMission`), 지급(`PayMission` → `CampaignAccount.Mission("mission:<trip>")`), 실패해도 기본 운임 유지.
- 목적지 숨김: `CallBoardRules.Hidden`과 `PassengerJobs.DestinationHidden`. "어디로 가는지 모르고 따라간다"에 맞는다.
- 방향지시등: 표적이 회전 전에 깜빡이를 켠다(`TrafficCar.CurrentTurn`). 플레이어가 미리 읽을 단서가 된다.
- 버스 정류장 이름: OSM 점검 자료에 정류장 노드 55개가 이름과 함께 있다(현장 미확인, 게임 지도에 안 들어감). 노선 자료는 수집돼 있지 않다.

### 위험

- **준법 방향:** 표적이 황색에 통과하고 플레이어가 적색에 걸리면 신호 위반 유혹이 생긴다. 표적이 뒤를 기다려 주는 규칙(거리가 벌어지면 감속)이 필요하고, 놓쳤을 때는 손님이 목적지를 말해 일반 운송으로 넘어가게 해야 한다(실패 벌칙 없음 원칙).
- 표적 차량이 관리 로직에 치인다: `DistrictTraffic.Update`는 화면 밖 차를 5초마다 다른 곳으로 옮기고, `LaneAvailabilityChanged`는 차를 회수한다. 표적은 제외 처리가 필요하다.
- 표적이 정체에 갇히거나 경로가 막히면 미션이 성립하지 않는다. 기한(`JobOffer.seconds`)은 표적 속도 기준으로 다시 잡아야 한다.
- NPC는 저장되지 않는다. 미션 도중 저장·이어하기 때 표적을 다시 만드는 규칙이 필요하다(또는 이어하면 "놓침" 처리).
- 따라붙기는 안전거리와 반대다. 판정 거리를 "보이는 거리 15~60m"로 넓게 잡아야 꼬리 물기를 권하지 않는다.

### 최소 시험판

- **넣는 것:** "저 차 따라가 주세요" 미션. 호출판에서 고르는 선택형, 드물게(미션 추첨 중 낮은 비중). 승차 후 승용차 크기의 눈에 띄는 색 표적이 앞에서 출발해 하차점까지 간다. 운송 시간의 일정 비율 이상 60m 안에 있으면 성공.
- **빼는 것:** 버스 모형·정류장·노선, 표적이 여러 번 서는 연출, 이야기 분기.
- **수치 ID:** `mission.follow.enabled`(0), `mission.follow.weight`, `mission.follow.maxGapMetres`(60), `mission.follow.requiredShare`(0.7), `mission.follow.waitGapMetres`(45), `mission.follow.bonusPercent`(30).
- **확인:** `MissionProbe` 방식의 위치 제어 검사로 성공/실패/놓침 뒤 일반 운송 전환, 사람이 따라가는 동안 신호 위반이 늘어나는지 `LawLedger` 건수 비교.

---

## 7. 과속 방지턱

### 필요한 코드 변경 범위

- 지도에 방지턱 자료가 없다. 지도 문서 시설 종류에도, `eungye-road-facilities.json`(난간·볼라드·분리대 36건)에도 없다.
- 다만 조사 기록에는 있다: `docs/content-design/road-survey/2026-11-16-pink-field-check.md`의 P12 x876열(차로 `local:8785:940:8785:1578`) "황백 고원형 감속부", P24 x834열 "황백 과속방지턱", P23 "방지턱 예고". 치수와 정확한 위치는 `2026-11-16-coverage.md`에 "남은 항목"으로 적혀 있다.
- 위치 자료를 `eungye-road-facilities.json`에 새 `kind`로 넣거나 새 설정 파일로 둔다. 어느 쪽이든 manifest 해시 재생성 대상이다. 그리는 코드는 `ReviewedRoadFacilities.cs`에 추가.
- 판정: 새 파일 `SpeedBumps.cs`(가칭). 구역 통과 속도로 판정하고 `DrivingManners.Deduct`를 부른다. 규모 **소~중**(위치 2~3곳 기준).

### 재사용 가능한 기존 시스템

- 차량 물리: `TaxiController.cs`가 `WheelCollider` 4개(서스펜션 거리 0.25m, 스프링 35,000, 댐퍼 4,500, 차체 1,400kg)를 쓴다. 턱을 실제 충돌체로 만들면 차가 물리적으로 반응한다.
- 승차감 감점: `DrivingManners.Deduct(key, 점수, 사유, 쿨다운)`. 새 키는 `MannerLedger.Penalty`에서 넘긴 값 그대로 쓰인다.
- 패드 진동: `DriveFeelAudio.cs`의 `feel.rumble.road` / `feel.rumble.contact`.
- 잘 넘었을 때: `DrivingManners.Praise`(부드러운 운전 칭찬), 손님 한마디.
- 길안내 비용: 턱 구간에 지연을 주려면 `RoadConditions.ExtraSeconds`와 같은 "구간 추가 초" 방식을 쓸 수 있다.

### 위험

- **지금 승차감 판정은 앞뒤 가속도만 본다.** `DrivingManners.FixedUpdate`는 전진 방향 가속도를 0.25초 필터한 값으로 급가감속을 가린다(`DriveMath.HarshPassengerMotion` 기준: 가속 4.5 초과, 감속 −6 미만). 위아래 충격은 재지 않으므로 턱을 놓는 것만으로는 승차감 판정과 이어지지 않는다. 별도 판정이 필요하다.
- 턱 앞에서 세게 밟으면 기존 급제동 감점이 먼저 나온다. `DrivingManners.NecessaryBraking`은 전방 물체·신호 등만 면제하므로 "턱 때문에 제동"은 면제 대상이 아니다. 벌이 이중이 되지 않게 해야 한다.
- 충돌체 턱이 사고·손상 판정(`AccidentController`, `VehicleDamageController`)에 "시설 접촉"으로 잡히는지 확인하지 못했다. 잡히면 턱을 넘을 때마다 사고가 난다.
- "커피 든 손님" 미션(`DrivingManners.TripHarsh==0`)과 겹친다. 턱 감점을 `accel/brake` 키로 넣으면 미션 실패 조건이 바뀐다. 새 키로 분리해야 한다.
- NPC는 키네마틱이라 턱을 그냥 통과한다. 느려지게 하려면 `TrafficCar.DistrictCruiseSpeed`(공용 코드)에 구간 상한을 넣어야 한다.
- 턱 충돌체는 은계 싱글 장면에만 생성한다(PvP 지도·공용 물리 불변).

### 최소 시험판

- **넣는 것:** 조사 기록이 있는 2곳(P12, P24)에 턱 표시(도색 + 낮은 융기)와 통과 속도 판정. 손님을 태우고 기준 속도보다 빠르게 넘으면 M 감점과 손님 한마디, 진동. 천천히 넘으면 가끔 칭찬.
- **빼는 것:** NPC 감속, 길안내 비용, 차량 손상, 학교 앞 보호구역 규칙, 나머지 위치.
- **수치 ID:** `bump.enabled`(0), `bump.comfortKmh`(25), `bump.penalty`(2), `bump.cooldownSeconds`(6), `bump.heightMetres`(0.08).
- **확인:** 위치 제어 검사로 20/40km/h 통과 시 감점 0/1회, 사고·손상 미발생, 커피 미션 판정 불변. 사람이 1인칭·3인칭에서 턱이 미리 보이는지(안 보이면 함정이 된다).

---

## 추천 순서 (비용 대비 효과)

효과는 "20분 안의 판단이 얼마나 늘어나는가", 비용은 코드 규모에 지도 조사·교착 위험·경제 재조정을 더해 봤다. 사람 플레이 근거가 없는 추정이다.

1. **과속 방지턱** — 조사 기록이 이미 있고 2곳만 넣으면 되며, 골목의 성격(2번)을 가장 싸게 만든다. 단 위아래 충격 판정은 새로 만들어야 한다.
2. **시외 장거리 콜** — 지도·교통 AI를 안 건드리고 기존 시계·연료·장부 함수로 끝난다. 호출 사이 차이를 가장 크게 만든다. 운임 상한 결정이 선행.
3. **성격이 다른 도로 구간(국도42 NPC 속도만)** — 수치 한두 개로 시험할 수 있다. 골목 주차 차량은 교착 위험 때문에 2차.
4. **유혹(카메라 없이)** — 요청 종류 하나 추가로 싸다. 다만 준법 방향과 부딪히는지 소유자 결정이 먼저이고, 카메라는 조사 전 불가.
5. **돌발 상황(문 여는 정차 차량만)** — 정차 차량이 이미 있어 싸지만, 사고 과실 구분이 없어 "억울함" 위험이 크다.
6. **미션 "저 차 따라가 주세요"** — 표적 차량 관리(재배치 제외, 기다려 주기, 놓침 처리)에 손이 많이 가고 드물게 나오는 미션이라 회수가 느리다. 버스는 자료·모형 모두 없어 보류.
7. **다른 택시와 승강장** — 위치 조사, 대기 줄, 경제 균형, NPC 정차 동작까지 필요하고 "기다리는 시간"이 재미인지 불확실하다. 승강장 1곳 시험판도 중 규모.

---

## 코드에서 확인한 사실

**일반 차량(NPC)**

- 생성: `DistrictTraffic.Initialize` → `SeedDistributed`가 승하차점 쌍 사이 경로에서 최대 72대(`PoolCap`)를 뿌리고, `DistrictTraffic.Update`가 5초마다 플레이어 주변 500m에 18대(`LocalTarget`)가 되도록 화면 밖 차를 옮긴다.
- 주행: `TrafficCar.Tick`이 `TrafficRoute` 위 거리만 늘리는 키네마틱 방식이다. 차로 중심선을 벗어나지 못하고 추월·비켜 주기가 없다. 은계 싱글 순항 속도는 13m/s 고정(`TrafficCar.DistrictCruiseSpeed`), 성향 3종(`TrafficDriverProfile`).
- 정해진 경로 주행 가능: `DistrictTraffic.CreateRoute` + `TrafficSystem.Spawn` 또는 `car.route` 교체 + `TrafficCar.Place`. 경로 끝에서는 `DistrictTraffic.Continue`가 다음 목적지를 고른다.
- 차로 변경 때 옆·뒤 차(플레이어 포함)에 양보한다(`TrafficCar.LaneChangeYieldGap`, 최대 6초).
- 비상등에 반응하는 NPC 코드는 없다. `TaxiSignals.ToggleHazards`는 표시와 깜빡이 취소에만 쓰인다.
- NPC 상태는 저장되지 않는다(교통 스냅샷 클래스 없음, 이어하기 뒤 `DistrictTraffic.ClearPlayerOverlap`만 호출).
- 버스 NPC·노선·정류장: 코드와 게임 지도에 없다.
- 주차 차량: 상시 물체는 없다. 임시로 서는 차는 두 가지가 있다 — `RoadConditions`의 "비상 정차 차량/정체 대기"(차로 위, 65~90초), `StopOccupancySystem`의 "일시 정차 차량"(게임 전용 승하차면).

**지도 자료**

- `MapRoad`: `id, name, width, sourceLanes, lanes, oneWay, points`. `HdEdge`: `roadName, status, turn, change, next` 등. 도로 등급·제한속도 필드는 없다.
- 지도 문서(`eungye.project.json`) 시설 2,186건의 종류: 차선·중앙선, 정지선, 횡단보도, 신호기, 표지(유턴 15, 비보호 좌회전 6), 회전교차로 1, "차선 없는 생활도로" 23 등. **제한속도 표지 0건, 방지턱·단속 카메라·택시 승강장·버스 정류장 0건.**
- 지도 편집기는 "제한속도" 표지를 지원한다(`RoadMapEditorSimple.cs`, 5~100km/h). 놓으면 NPC 속도·길안내 시간·과속 판정에 쓰인다.
- 국도42 = 도로명 "수인로". 게임 안 차로 길이 합 약 4.9km(구간 길이로는 약 1.2km, x 45~1142). 승하차점 이름에 "국도42 승하차 1"이 있다.
- OSM 점검 자료(`data/inspection/osm-eungye.json`, Git 제외 로컬 파일, 현장 미확인): 버스 정류장 노드 55, 단속 카메라 1(고속도로 위), `maxspeed`는 고속도로만, `amenity=taxi` 0. 도로 등급 태그(primary/residential/service)는 있으나 게임 지도로 옮기지 않는다(`config/maps/eungye-osm-supplement.json`은 도로명 412·통행규제 2·횡단보도 39만 담음). 점 형태 `traffic_calming`은 점검 도구(`tools/inspect_osm_eungye.py`)가 수집하지 않아 알 수 없다(고원식 횡단보도 2건만 선 자료로 보임).
- 국토지리정보원 정밀도로지도 원본(`data/raw/ngii/hdmap/eungye/`)의 국도42 패키지에는 `a2_link`의 `maxspeed` 필드, `b1_safetysign`, `c4_speedbump`(전체 18건) 레이어가 들어 있다. `tools/import_hdmap.py`는 노드·링크·노면선·노면표시·신호등만 읽고 이 값들을 쓰지 않는다. **이 레이어의 객체가 은계 지도 범위 안에 있는지는 확인하지 못했다**(좌표 변환 라이브러리 pyproj가 이 PC의 Python에 없었다).
- 승하차 후보 27곳은 전부 `game-only-field-unverified`이고 그중 `outer-link` 7곳이 있다.

**과속·위반·벌금**

- 과속 판정은 `DrivingLaw.CheckEditorSpeed` 하나다(표지 제한 + 3km/h를 2초, 5점). 은계에는 표지가 없어 **현재 은계에서는 과속이 판정되지 않는다.**
- 위반 기록: `DrivingLaw` → `LawLedger.Record(LawEvent)`, 점수는 `LawLedger.TotalScore`(0~100). 승차 때 `PassengerReward.AdjustedFare`로 운임을 보정한다. 손님 만족도 쪽은 `DrivingManners.Deduct` → `MannerLedger.Penalize`.
- **돈으로 내는 법규 벌금은 없다.** 장부 종류는 fare, tip, mission, fuel, meal, rest, wash, repair, tow, accident, home-rent, settlement, personal-purchase 등이다. 운전 때문에 돈이 나가는 것은 사고 처리비·수리비뿐이다.
- 장부는 `CampaignAccount.Post(id, kind, day, cash, debt)`가 같은 ID를 거부해 중복 지급을 막는다. `kind`는 자유 문자열이라 새 종류를 넣어도 옛 저장은 그대로 읽힌다.

**시계·암전·연료**

- 영업 길이: 10/20/30/60분 선택(`PrototypeSettings.ShiftMinuteChoices`), 기본 1,200초. 시계는 `EconomyLedger.Elapsed`, 남은 시간 `SecondsLeft`.
- 시간 일괄 경과: `EconomyLedger.Advance(초, 미터)`가 시계·연료·피로를 같이 올린다. 휴식·식사는 `ServiceTime(50~100초)`으로 시계만 올린다.
- 암전: `AccidentController.DrawFade`, `VehicleDamageController`가 `OnGUI`에서 검은 전체 화면을 그린다. 휴식·수면 전용 암전 연출은 찾지 못했다(졸음 시야 가림 `DrowsinessHUD`는 별개).

**차량 물리·승차감**

- `TaxiController`: `WheelCollider` 4개, 서스펜션 0.25m. 턱 같은 지형에 물리적으로 반응할 구조는 있다. 방지턱·요철 전용 코드는 없다.
- 승차감 판정은 앞뒤 가속도만 본다(`DrivingManners.FixedUpdate`, `DriveMath.TravelAcceleration`). 위아래 충격은 재지 않는다.
- 도로 표면이 자체 충돌체를 갖는지, 턱 충돌체가 사고 판정에 잡히는지는 확인하지 못했다.

**미션·호출**

- 미션은 호출판을 새로 만들 때 `PassengerJobs.ApplyMissions`가 한 건에 붙이고(`JobOffer.mission`, `missionBonus`), `TickMission`·`ObserveSignalEntry`로 관찰, `SettleMission`으로 판정, `PayMission`으로 지급한다. 종류는 급행·초록불·커피·칼정차와 같은 날 넣은 길 아는 기사(`KnowRoad=5`)까지 5종이다.
- 호출 성격은 `JobOffer.variety`(0 일반, 1 근거리, 2 급한 손님, 3 큰 짐, 4 목적지 비공개, 5·6 마지막 콜). 연속 배차는 `PassengerJobsChain.cs`.
- 요청 종류 `RequestKind`는 17종이고 `RejectIllegal`은 거절 응답 한 가지만 있다.

**확인하지 못한 것(추측하지 않음)**

- 실제 수인로·은계 생활도로의 제한속도, 실제 단속 카메라·택시 승강장 위치.
- 정밀도로지도 `c4_speedbump`·`maxspeed`가 은계 범위에 해당하는지.
- `AccidentController`가 낮은 턱 충돌체나 NPC 쪽에서 와서 닿은 접촉을 어떻게 분류하는지(과실 구분 코드를 찾지 못했다는 것만 확인).
- 카탈로그에 없는 ID를 `ContentTuning.Number`로 읽을 때의 동작(기존 코드는 모두 기본값을 함께 넘김).
- 사람 플레이에서의 재미. 위 시험판은 전부 미실행 제안이다.

## 사용자가 결정할 것

- 어느 시험판을 어떤 순서로 만들지(위 추천 순서는 제안이다).
- **준법 방향과 부딪히는 두 항목**(유혹, 따라가기 미션)을 넣을지. 넣는다면 위반이 늘어나는지 재는 조건을 함께 정한다.
- **실제 조사가 먼저인 항목**(단속 카메라, 승강장 실제 위치, 제한속도, 방지턱 치수)을 조사할지, 게임 전용 시설로 이름에 밝히고 넣을지.
- 시외 콜의 운임 상한(영업 시간 대비 수입 배수)과 10분 영업에서의 허용 여부.
- 돌발 상황에서 사고 과실 예외를 만들지(안 만들면 끼어들기·문 열림에 닿았을 때 플레이어 사고가 된다).

## 변경 파일

- `docs/work-history/2026-11-24-next-candidates-design.md`(이 파일)
- `docs/content-design/08-fun-improvements.md` 5.5(요약 표, 추천 순서)

코드·지도·빌드 변경 없음. 실행 검사 없음.
