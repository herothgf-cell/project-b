# 콘텐츠·시스템 구성과 데이터 흐름

현재 기준: 2026-10-27. 게임은 Unity의 은계 실행 프로젝트와 공유 지도 패키지를 중심으로 구성된다. 이 문서는 **어느 콘텐츠가 어떤 데이터를 읽고 누구에게 결과를 전달하는지** 설명한다. 변경/검증 히스토리는 [기획 이력](../../../content-design/../design-history/content-design/2026-10-27-design-decisions.md)과 [작업 이력](../../../content-design/../work-history/LATEST.md)에 둔다.

## 1. 플레이 루프와 시스템 연결

`출발/이어하기 → 호출 선택·공차 이동 → 픽업 → 요청·주행·생활 판단 → 하차 정산 → 다음 호출 또는 거점 → 하루 마감 → 다음 날/최종 결과`

| 단계 | 입력·조건 | 처리 주체 | 생성/갱신 데이터 | 다음 소비자 |
|---|---|---|---|---|
| 지도 준비 | 정식 지도 패키지·지역 | ProjectMapStore/ProjectMapPackage·WorldBuilder | 월드·차로·시설·정차 후보·리비전 | 내비·NPC·평가·승객 |
| 영업 시작 | 새 캠페인 또는 유효 저장 | DriveSession·BusinessProgress | 운송/캠페인 ID·하루 장부·초기/복원 상태 | 모든 영업 시스템 |
| 호출 생성 | 접근 가능 후보·지역 가중·후보 ID | PassengerJobs·PassengerStopCatalog·PassengerDemandPolicy | 픽업/하차·예상 운임·기한·승객 난도 | M 지도·내비·운송 |
| 승차 | 방향·속도·위치·청결·점유·졸음 조건 | PassengerJobs·FareTrip | OnTrip·승차 점수·S0·운임 정책·요청 배정 | 요청·주행 관찰·컨디션 |
| 운송 | 물리 이동·신호·타차량·보행자·응답 | TaxiController·DrivingLaw·DrivingManners·요청 모듈 | 실제 법규 사건·M·요청 관찰 | 운송 HUD·정산·저널 |
| 운송 완료 | 첫 유효 도착·최종 하차 | PassengerJobs·PassengerReward·BusinessProgress | 최종 S/R·기본 운임/보너스·승객 결과·회계 거래 | 완료창·F8·하루 장부 |
| 생활/마감 | 지정 거점·확정 입력·빈차/정차 | ServiceStops·EconomyLedger·CampaignAccount | 비용/미납·서비스 시간·연료/회복·마감 상태 | HUD·정산·다음 날 |
| 저장 | 진행 상태·즉시 사건·날짜 경계 | BusinessProgress·CampaignCheckpointWriter·DrivingScoreJournal | 체크포인트·별도 법규 저널 | 재실행·이어하기 |

요청 모듈은 UI·배정·개별 관찰 컴포넌트를 묶어 부르는 설명용 명칭이며 실제 클래스명은 아래 표를 따른다.

## 2. 콘텐츠 카탈로그

| 콘텐츠 자산/단위 | 현재 구성 | 역할 | 연결 기획 |
|---|---|---|---|
| 기본 월드 | 시흥 은계 도로망·국도42, 차로/교차로/노면·게임시설 | 통행 방향·신호·정차 기반 | [06](06-world-map-and-driving.md) |
| 승하차 후보 | 기존12 + 게임 포켓12 =24 | 일반 운송과 코스가 참조하는 안전 정차 위치/방향 | [14](14-passenger-stop-variety.md) |
| 상황 코스 | PS01~PS09 9코스 | 기존 승하차/도로 조건을 재사용하는 짧은 운송 구성 | [09](09-prototype-scenarios.md) |
| 선택/연습 | 우회전R1~R5·출구 정체A/B/C | 조건을 통제한 반복·회고. 영업 PS08 팝업은 제외 | [01](01-driving-situations.md), [03](03-history-and-mastery.md) |
| 승객 요청 | 기본2 + DR5 + CH6 + EV4 =17콘텐츠 | 운행 중 후보16종과 하차 후 EV03 구분, 운송별0~2건 | [11](11-passenger-request-effects.md) |
| 승객 특성 | 요구R·시작 기대·여유/보통/급함·취객 | 시간/만족도/대사·구토/기상 변주 | [04](04-passengers-and-reservations.md), [15](15-drunk-passengers-and-cleanliness.md) |
| 영업 전략 | 주거/업무/상업·공차 지표·점유 대기 | 어디로 이동하고 어떤 호출을 받을지 비교 | [17](17-proto-operation-expansion.md) |
| 생활시설 | 주유소3·식당·기존 주차장·동부 휴식 | 연료·식사·세차·수면·마감 | [05](05-daily-operation.md) |
| 일반 교통/보행 | 최대48대·안전6슬롯 보행 + 상황 보행 | 실시간 교통·양보·횡단 관찰 | [06](06-world-map-and-driving.md), [16](16-ambient-pedestrians-proposal.md) |
| 캠페인 | 3일·예정20분/일·초과 영업 | 수익·생활 선택·기록의 이월 비교 | [07](07-campaign.md) |

코스·정차 후보·요청은 같은 개념이 아니다. 후보는 위치, 코스는 운송 구성, 요청은 탑승 후 응대/행동 목표다. 9코스를 모두 끝내야 다음 날로 가는 구조가 아니다.

## 3. 기능별 코드 소유권

코드는 저장소 상대 경로로 연결한다. 아래는 핵심 진입점이며 전체 파일 목록은 아니다.

| 계층/기능 | 핵심 코드 | 소유하는 책임 | 연결 경계 |
|---|---|---|---|
| 실행 조립 | [DriveSession](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/DriveSession.cs) | 준비·공통 세션·모달/조작 상태 | 개별 운임/법규 산식을 UI에서 복제하지 않음 |
| 월드/지도 | [ProjectMapPackage](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/ProjectMapPackage.cs), [WorldBuilder](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/WorldBuilder.cs) | 정식 문서·형상·게임 데이터 구성 | 지도 리비전·원본/게임 보정 구분 |
| 방향 경로 | [HdRoadGraph](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/HdRoadGraph.cs), [RoadNavigation](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/RoadNavigation.cs), [ServiceDepartureNavigation](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/ServiceDepartureNavigation.cs) | 통행 연결·회전·차로변경·시설 출구 안내 | 직선 최단거리보다 실제 방향/제한 우선 |
| 차량/교통 | [TaxiController](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/TaxiController.cs), [TrafficSystem](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/TrafficSystem.cs) | 플레이어 물리·신호/NPC 경로·양보 | H는 조향 보조, 가감속·정지 판단은 사용자 |
| 표지/보행 | [RoadMapFacilities](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/RoadMapFacilities.cs), [AmbientPedestrianSystem](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/AmbientPedestrianSystem.cs) | 저작 시설 표시·신호 부착·행인 상태 | 시설 표시와 적용 법규/행인 생성은 별개 |
| 승객/운송 | [PassengerJobs](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/PassengerJobs.cs), [FareTrip](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/FareTrip.cs), [PassengerStopCatalog](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/PassengerStopCatalog.cs) | 후보·수락/승하차·목표·기한·도착 잠금 | 동일 운송 ID·정산1회 |
| 요청 | [PassengerRequestUI](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/PassengerRequestUI.cs), [PassengerRequestSchedule](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/PassengerRequestSchedule.cs), [PassengerRequestObservation](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/PassengerRequestObservation.cs) | 배정·안전 제시·응답·개별 관찰/합산 | 미발생/보류0·상충 방지·경로 요청 보호 |
| 점수/운임 | [DrivingLaw](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/DrivingLaw.cs), [DrivingManners](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/DrivingManners.cs), [PassengerRequest / PassengerReward](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/PassengerRequest.cs) | 법규 점수·M·S/보정 약정·최종 지급 | 세 점수를 혼합하지 않고 승차/하차 스냅샷 유지 |
| 수요/공차 | [PassengerDemandPolicy](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/PassengerDemandPolicy.cs), [OperationMetrics](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/OperationMetrics.cs) | 상대 가중·거리/시간 관찰 | 기록이 시계·연료·거래를 다시 만들지 않음 |
| 공간 점유 | [StopOccupancySystem](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/StopOccupancySystem.cs), [StopOccupancyPolicy](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/StopOccupancyPolicy.cs) | 생성/해소·점유 상태·기한 보호 | 검증 대안 없으면 대기만, 같은 사건 재보정 방지 |
| 생활 상태 | [DriverCondition / Drowsiness](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/DriverCondition.cs), [VehicleCare](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/VehicleCare.cs) | 포만감·피로/노출·청결·취객 상태 | 서비스·수면·활성 경과 종류 구분 |
| 경제/캠페인 | [EconomyLedger](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/EconomyLedger.cs), [CampaignAccount](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/CampaignAccount.cs), [BusinessProgress](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/BusinessProgress.cs) | 영업 상태·비용/미납·이월·저장 연결 | 비용/정산/날짜 이월1회, 손상 저장 보존 |
| 시설/UI | [ServiceStops](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/ServiceStops.cs), [FullMapOverlay](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/FullMapOverlay.cs), [DrowsinessHUD](../../../content-design/../../unity/EungyeTaxi/Assets/Scripts/DrowsinessHUD.cs) | 시설별 메뉴·지도·졸음 표시 | 표시가 실제 거래·운송 상태를 우회하지 않음 |

## 4. 상태와 데이터 생명주기

| 데이터 범위 | 생성·고정 시점 | 종료·초기화 | 저장 시 보호 |
|---|---|---|---|
| 캠페인 | 명시 새 캠페인 확인·저장 성공 | 새 캠페인 때만 초기값 | ID·누적 점수·현금/미납·완료 이력 |
| 하루 | 날짜 시작 | 마감1회 정산→다음 날 | 경과/비용/공차·생활·청결 소수 경과 |
| 호출 후보 | 후보 생성/배정 | 만료·수락·명시 취소 | 후보 ID·난도·요청 시드, 재수락 재추첨 방지 |
| 운송 | 수락→픽업→탑승 | 완료/취소1회 | 목표·기한·승차 점수/S0·운임 정책·M 회복 진행 |
| 요청 슬롯 | 후보별 배정, 개별 제시/응답 | 하차 확정·미발생 종료 | 목록·진행 조건·개별 관찰·합계 |
| 법규 사건 | 구역/접근의 실제 관찰 | 사건 종료·기록 유지 | 사건 ID·감점 최대값·준수 지급 여부 |
| 졸음 | 피로100 활성 시간 | 실제 수면·회복 시 노출 해제 | 누적 소수초로 눈감김 위상 유지, 재실행 회복 금지 |
| 점유/행인 | 안전 생성·진행 | 해소/상황 해제·날짜 경계 | 남은 시간/진행 위치·횡단 점유·보정 여부 |

운송 흐름은 FareTrip의 대기/픽업/탑승/완료·취소 상태를 따른다. 경제 흐름은 영업(Open)→마감 대기(Closing)→일일 결과(Summary) 또는 최종 완료(Completed)로 나뉜다. 게임 시간과 실제 메뉴 체류 시간은 동일 지표가 아니다.

## 5. 지도 제작·저장·배포 구조

| 자료 | 역할 | Git/배포 원칙 |
|---|---|---|
| config/maps/project/eungye.project.json | 정식 공유 지도·차로/시설/장면 패키지 | 버전·체크섬 검증, 변경 시 경로·저장 호환 확인 |
| data/raw/·생성 입력 | 개발에 필요한 지도 원자료/중간 산출물 | 일반 Git 제외. 필수 로컬 입력 manifest·distribution/maps ZIP으로 PC 이동 준비 |
| Unity 개인/시험 지도 | 편집·미리보기·시험 실행 | 정식 지도·일반 영업 저장과 분리 |
| 운행 저장 v3 + 사건 저널 | 위치·운송·요청·계정·컨디션·교통 상태 | 지도 충돌/손상 때 원본 보존·자동 덮어쓰기 금지 |
| builds/EungyeTaxi/ | 다른PC에서 바로 실행할 기본 게임 폴더 | 전체 폴더 Git 배포, 변경 시 manifest의 모든 파일 크기/SHA256 재생성·검증 |
| distribution/EungyeTaxi-build-manifest.json | 배포 소스·지역·파일 목록·검증 범위 | 문서만 변경한 이번 작업은 게임/manifest 재빌드하지 않음 |
| artifacts/·Unity Library | 자동 검사·캡처·캐시 | 검증 보조 산출물, 일반 Git 제외 |

## 6. 기획·히스토리·검증의 소유권

| 종류 | 저장 위치 | 담는 것 | 넣지 않는 것 |
|---|---|---|---|
| 현재 규칙 | content-design/00~19 | 조건·행동·결과·영향·예외·현재 제공 범위 | 날짜순 덧붙이기·과거 미구현 상태 |
| 기획 변경 이력 | design-history/content-design | 선택 이유·이전 수치/정책·정리 전 원문·검토용 마인드맵 항목 | 최신 동작의 단일 기준 역할 |
| 실제 개발/검증 | work-history/날짜-주제 | 명령·종료코드·구현/검증/미검증 구분 | 사람 체감을 자동 검사로 완료 처리 |
| 현재 인계 | work-history/LATEST.md | 최신 실행/소스/지도·다음 작업·필수 입력 | 과거 인계를 무한 누적 |
| 구현 추적 | prototype-development-checklist.md | 구현·제한 구현·수동/미구현 항목 | 세부 수치 중복 정의 |
| 사용자 원자료 | 택시 시뮬레이션.mmst·개인 메모 | 사용자가 검토·편집하는 마인드맵 | 이번 작업의 자동 수정/자동 커밋 대상 아님 |

자동 모델·Editor·위치 제어·일부 자동 물리 주행·사람 수동·전체3일 물리 캠페인은 각각 다른 증거다. 현재 규칙 표만으로 검증 완료 범위를 넓히지 않는다.
