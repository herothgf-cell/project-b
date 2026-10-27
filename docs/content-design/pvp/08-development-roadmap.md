# PVP 개발 로드맵 Implementation Plan

상태: 기획 초안 / PVP 미구현  
작성일: 2026-10-27

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기존 은계 시뮬레이션과 분리된 자체 경기장에서 실제 사용자들이 운송·운전 판단으로 경쟁하는 PVP를 단계적으로 검증한다.

**Architecture:** 초기에는 같은 저장소·Unity 프로젝트에 전용 시작 씬·차량·맵·저장·빌드 경로를 둔다. 권한 서버가 경기·승객·교통·정산을 확정하며 클라이언트는 자기 차량을 예측한다. Fusion 2 Host는 P2 검증을 위한 추천 후보이고 공개 랭킹용 Dedicated Server는 후속 단계다.

**Tech Stack:** Unity `2022.3.62f2`, C#, Windows64 클라이언트, 선택 후 고정할 네트워크 SDK, 후속 Linux Dedicated Server.

**Spec:** [00 목표](00-mode-overview.md), [01 경기 정책](01-match-flow-and-rules.md), [02 주행](02-driving-and-traffic.md), [03 맵](03-map-and-content.md), [04 승객](04-passengers-and-fares.md), [05 점수](05-scoring-and-results.md), [06 UI](06-ui-and-feedback.md), [07 구조](07-network-and-build.md). 설정과 수락 기준은 [09](09-decisions-and-balance.md), [10](10-playtest-and-acceptance.md)을 함께 읽는다.

## Global Constraints

- 기준 main은 `60b15edd6094d9a6383de4d0c75c4a40d49eda40`, 작업 브랜치는 `feature/pvp-prototype`다. 초기 프로젝트는 `unity/EungyeTaxi`다.
- 이번 작업은 문서 작성까지다. 아래 코드·파일·인터페이스·테스트는 **후속 신규 제안이며 아직 구현·실행되지 않았다**.
- 현재 게임 코드·씬·지도·패키지·실행파일·배포 manifest·사용자 `.mmst`는 이번 문서 커밋에서 변경하지 않는다.
- 자체 제작 맵을 사용하고 기존 은계 맵을 PVP 경기장으로 사용하지 않는다. 전면 공통화·새 엔진·영구 복제 브랜치를 선행 조건으로 두지 않는다.
- `MatchDurationSeconds`는 필수 양수 설정이며 제품 기본 경기 시간은 미정이다. `FinishGraceSeconds` 등 후보값은 09에만 둔다.
- 참가 인원·충돌·네트워크 SDK·배점은 사용자 확정 방향과 구분한다. 초기 초대방에 공개 매칭·계정·영구 랭킹을 선행 요구하지 않는다.
- 운임·위반·원장 정책은 04·05, 마감·재접속·결과 확정 정책은 01을 구현한다. 이 로드맵에서 별도 산식을 만들지 않는다.
- 모든 제품 작업 체크박스는 미완료다. 문서 검토 통과, 자동검사, 실제 온라인 플레이, 배포 완료를 각각 증거로 갱신한다.

## Review Focus

- 다른 맵·설정·프로토콜 또는 훔친 참가자 ID로 접속해도 참가 상태를 바꾸지 않아야 한다: Task 3의 `PvpAdmissionTests`.
- 같은 승객의 동시 요청·재시도·종료 상태 재사용이 한 운송만 만들어야 한다: Task 4의 `PvpPassengerRaceTests`.
- 재계산·재접속·원장 재전송이 돈·감점·완료 건수·알림을 중복 생성하지 않아야 한다: Task 5의 `PvpSettlementTests`, Task 7의 `PvpHudTests`.
- 마감 tick·단절·하차·위반이 겹쳐도 01의 처리 순서와 같은 결과여야 한다: Task 4의 `PvpRoundBoundaryTests`.
- 로컬 메뉴·프레임 저하·서버 위치 복구가 다른 참가자의 공통 시계·신호·운임에 영향을 주지 않아야 한다: Task 2의 `PvpMapValidationTests`, Task 6의 `PvpTrafficAuthorityTests`.

---

## 단계와 의존성

| 단계 | 진입 조건 | 검토 가능한 산출물 | 다음 단계 게이트 |
|---|---|---|---|
| P0 문서 정리 | 사용자 요구와 기준 코드 확인 | 문서·결정 대장·검증 계획 | 문서 전용 차이, 규칙 소유권·링크 확인 |
| P1 독립 오프라인 주행 | P0 검토 | 전용 실행 경로, 자체 맵, 민첩한 조작 후보 | QA-ISO-01, QA-MAP-01/02, QA-DRV-01 |
| P2 실제 네트워크 기술 검증 | P1 조작·맵 후보 | 독립 클라이언트 두 개, 입력 예측·권한 상태·호환성 | QA-NET-01/02/03; SDK 채택·보류 근거 |
| P3 공용 승객과 경기 완주 | P2 통과 | 공유 승객→운송→원장→공통 마감·결과 | QA-PAX-01/02, QA-SCORE-01/02, QA-END-01/02, QA-NET-04 |
| P4 교통·규칙·UI·템포 | P3 완주 가능 | 권한 NPC/신호, 법규 피드백, 2인→4인 비교 플레이 | QA-ABUSE-01, QA-UX-01, QA-BAL-01, QA-BUILD-01 |
| P5 전용 서버·공개 경쟁 후속 | P4 수락과 운영 방향 결정 | Linux 전용 서버, 신뢰 가능한 결과 제출 계획·검증 | QA-OPS-01; 별도 운영·랭킹 수락 |

Task 1→2→3이 기술 위험을 먼저 줄인다. Task 4→5가 경기의 최소 완주 경로이며, Task 6→7이 실제 플레이 품질을 검증한다. Task 8은 후속 운영 게이트다.
주·일 단위 일정은 추정하지 않는다. 각 Task는 자신의 증거와 검토를 남기는 작은 커밋으로 마무리하고 실패한 게이트의 원인부터 해결한다.

여러 단계에 걸친 QA ID는 선행 단계에서 일부만 검사하고 남은 항목을 미수행으로 남긴다. P1에서는 지도·복구의 기하 조건과 저장/빌드 격리, P2에서는 실제 차량·입장 호환성과 시험 상태 복원을 확인한다. 승객·감점·원장까지 포함한 QA-MAP-02·QA-NET-03/04·QA-END-01/02의 전체 검증은 Task 5 이후에 수행한다. 선행 단계의 부분 통과를 해당 QA 전체 완료로 표시하지 않는다.

플레이에 필요한 최소 조작 화면은 기능과 함께 만든다. Task 1에서 시작·나가기, Task 3에서 방 생성/참가·연결 상태, Task 4에서 준비·목표 선택·승하차·서버 승인, Task 5에서 잠정 성적·결과를 표시한다. Task 7은 이미 연결된 기능을 06의 최종 HUD·지도·피드백으로 정리하는 단계다. 먼저 필요한 연결용 화면의 제안 경로는 `unity/EungyeTaxi/Assets/Pvp/Runtime/UI/PvpPrototypePanel.cs`이며 사용자 판단 없이 자동 운송하는 대체 검증으로 처리하지 않는다.

## 파일·타입 계약의 읽는 방법

아래 `Create`는 전부 신규 제안 경로이고 `Modify`는 해당 선행 Task에서 만든 PVP 파일에 대한 후속 수정이다. 현재 파일 존재를 뜻하지 않는다.
런타임 네임스페이스는 `Pvp`, 빌드는 `Pvp.Editor`, 테스트는 `Pvp.Tests.EditMode` 또는 `Pvp.Tests.PlayMode`를 제안한다. SDK의 네트워크 직렬화 타입은 Network 폴더 경계에서 변환한다.
문자열 ID는 경기 안에서 안정적으로 유지하고, `PvpStateSnapshot`은 외부에서 수정할 수 없는 전달용 상태다. 구체 필드는 01·04·05·07의 계약을 모아 정의하며 공급/법규/정산 책임을 한 클래스에 몰지 않는다.

### Task 0 · P0: 문서와 구현 경계 확정

**Files:** `docs/content-design/pvp/`의 문서, `docs/content-design/README.md`, 날짜별 `docs/work-history/` 기록과 `LATEST.md`만 이번 반영 대상이다.
**Interfaces:** 입력은 기준 코드·사용자 요구, 산출물은 시스템별 규칙 소유 문서와 09의 결정 상태, 10의 QA ID다.

- [x] 각 문서의 사용자 확정·초안·미정 표시와 01/04/05의 상태·정산 계약을 대조한다.
- [x] 07의 기존 코드 근거와 이 계획의 제안 파일을 구분하고 상대 링크를 확인한다.
- [x] 변경 파일 목록이 문서 범위 안에 있는지 확인하고 실행 코드·Unity 검사·사람 플레이를 완료로 표시하지 않는다.
- [x] 실제 수행한 문서 검사와 제품 검증의 미수행 범위를 작업 이력에 남긴다.

**Gate:** 문서 검토 가능 상태. P0의 문서 작성·검사만 완료했으며 P1 이후 게임 기능이 만들어졌다는 의미가 아니다. 게시 결과는 작업 브랜치의 실제 커밋으로 확인한다.

### Task 1 · P1: 시작·빌드·저장 경로 분리

**Files — Create:**
- `unity/EungyeTaxi/Assets/Pvp/Scenes/PvpBootstrap.unity`, `unity/EungyeTaxi/Assets/Pvp/Runtime/PvpBootstrap.cs`.
- `unity/EungyeTaxi/Assets/Pvp/Runtime/Core/PvpSessionConfig.cs`, `unity/EungyeTaxi/Assets/Pvp/Runtime/Persistence/PvpLocalStore.cs`.
- `unity/EungyeTaxi/Assets/Pvp/Runtime/UI/PvpPrototypePanel.cs` — 초기 시작·나가기 화면, 후속 Task에서 실제 기능을 연결.
- `unity/EungyeTaxi/Assets/Pvp/Editor/PvpBuild.cs`, `unity/EungyeTaxi/Assets/Pvp/Tests/EditMode/PvpIsolationTests.cs`.

**Interfaces:** `PvpSessionConfig.Freeze(string matchId, string mapHash) : PvpMatchSettings`; `PvpBootstrap.Initialize(PvpMatchSettings settings) : void`; `PvpLocalStore.Resolve(string fileName) : string`; `PvpBuild.BuildClient() : void`.
`PvpMatchSettings`는 같은 설정 파일에 정의한 불변 값 묶음이다. `Freeze`는 필수 설정 누락을 오류로 처리하며 경기 ID·맵 hash·설정 hash를 함께 고정한다.

- [ ] `PvpIsolationTests`에 `MissingDuration_IsRejected`, `SavePath_DoesNotTouchCampaign`, `BuildFailure_RestoresSettings`의 기대 실패를 먼저 고정한다.
- [ ] 테스트 실행 환경을 확인하고 해당 테스트의 실패를 기록한다. 필요한 테스트 의존성·assembly 설정은 이 기능을 검증하는 최소 범위로 추가한다.
- [ ] 전용 시작 경로와 저장 폴더, PVP 씬을 명시한 빌드를 구현한다. `DriveSession`·`CampaignSaveLocation.Resolve`를 호출하지 않는다.
- [ ] 테스트와 실제 독립 실행에서 캠페인 저장·기존 씬 목록·기본 실행 폴더 보존, 두 프로세스 진단 파일의 비충돌을 확인한다.
- [ ] QA-ISO-01·QA-BUILD-01의 시작·저장·출력 분리 증거를 기록하고 이 분리 단위를 커밋한다. 실제 맵 로드·다른 PC 배포 검증은 선행 기능이 준비된 뒤 완료한다.

**Gate:** 독립 시작 화면과 전용 실행물이 재현 가능하다. 아직 맵·차량·온라인 경기는 완료 상태가 아니다.

### Task 2 · P1: 자체 맵과 빠른 조작 후보

**Files — Create:**
- `unity/EungyeTaxi/Assets/Pvp/Runtime/Map/PvpMapDefinition.cs`, `unity/EungyeTaxi/Assets/Pvp/Runtime/Map/PvpMapLoader.cs`, `unity/EungyeTaxi/Assets/Pvp/Content/Maps/PvpArena01.asset`.
- `unity/EungyeTaxi/Assets/Pvp/Runtime/Map/PvpRoutePlanner.cs`, `unity/EungyeTaxi/Assets/Pvp/Tests/EditMode/PvpRoutePlannerTests.cs`.
- `unity/EungyeTaxi/Assets/Pvp/Runtime/Driving/PvpVehicleInput.cs`, `unity/EungyeTaxi/Assets/Pvp/Runtime/Driving/PvpVehicleMotor.cs`.
- `unity/EungyeTaxi/Assets/Pvp/Tests/EditMode/PvpMapValidationTests.cs`, `unity/EungyeTaxi/Assets/Pvp/Tests/PlayMode/PvpVehicleModelTests.cs`.
**Modify:** Task 1의 `unity/EungyeTaxi/Assets/Pvp/Runtime/PvpBootstrap.cs`에 지도·차량 생성을 연결한다.

**Interfaces:** `PvpMapLoader.Build(PvpMapDefinition definition) : PvpMapRuntime`; `PvpVehicleMotor.Simulate(in PvpVehicleInput input, float tickDelta) : void`; `Capture() : PvpVehicleState`; `Restore(in PvpVehicleState state) : void`.
`PvpMapRuntime`은 로더 파일의 읽기 전용 도달성·정차점 조회 결과다. `PvpVehicleState`는 차량 파일에서 정의하며 재계산에 필요한 운동 상태를 빠짐없이 포함한다. 입력은 축·버튼이며 점수·최종 위치를 포함하지 않는다.

**길안내 계약:** `PvpRoutePlanner.FindRoute(PvpRouteQuery query) : PvpRouteResult`. Query·Result는 같은 파일의 전달용 타입이며 현재 차로·목표 정차 방향·거리/예상시간 우선 모드와 경로/접근 불가 사유를 담는다. 03의 방향 차로·회전·정차 진출입을 공유하고 서버 기본운임에는 04의 고정 기준 경로만 사용한다.

- [ ] `PvpMapValidationTests`에 출발점→승차점→하차점 합법 연결과 복구 지점의 비전진성 실패 사례를, `PvpVehicleModelTests`에 상태 저장·복원과 입력 범위 검사를 만든다.
- [ ] 실패를 확인한 뒤 03의 최소 콘텐츠와 02의 차량 후보를 구현한다. 자체 맵을 은계 보정 경로로 불러오지 않는다.
- [ ] 길안내가 반대 차로·금지 회전·중앙분리대를 통과하지 않는 실패 사례를 확인하고, 합법 최단/예상 최속 경로와 경로 없음 처리를 구현한다. 신호·교통을 추가한 예상시간 검증은 Task 6에서 이어 간다.
- [ ] 동일 코스에서 가속·저속 회전·급한 방향 전환·제동·코너 탈출을 비교하고 자동 상태 검사와 사람 조작감 기록을 분리한다.
- [ ] QA-MAP-01·QA-DRV-01과 QA-MAP-02의 기하·복구 위치 검사 증거, 09의 `VehicleHandlingProfile`·`ArenaScaleProfile` 선택 근거를 남기고 커밋한다. 점수·운송 보존은 Task 5 이후 검증한다.

**Gate:** 오프라인 주행과 유효 운송 경로가 성립하고, 최고속도 외의 반응성을 실제 조작으로 확인한다. 최종 맵 규모와 물리 모델은 P2 결과까지 열어 둔다.

### Task 3 · P2: 실제 두 클라이언트의 차량 예측 검증

**Files — Create:**
- `unity/EungyeTaxi/Assets/Pvp/Runtime/Network/PvpFusionSession.cs`, `unity/EungyeTaxi/Assets/Pvp/Runtime/Network/PvpNetworkVehicle.cs`, `unity/EungyeTaxi/Assets/Pvp/Runtime/Network/PvpConnectionContracts.cs`.
- `unity/EungyeTaxi/Assets/Pvp/Runtime/Core/PvpStateSnapshot.cs`, `unity/EungyeTaxi/Assets/Pvp/Tests/EditMode/PvpAdmissionTests.cs`, `unity/EungyeTaxi/Assets/Pvp/Tests/PlayMode/PvpPredictionProbe.cs`.
**Dependencies:** Task 2의 `PvpVehicleMotor`와 지도. SDK를 선택한 뒤에만 실제 패키지·설정 변경 경로를 확정하고 기록한다.

**Interfaces:** `PvpFusionSession.ConnectAsync(PvpJoinRequest request) : Task<PvpJoinResult>`; `SubmitInput(PvpVehicleInput input) : void`; `ReadAuthoritativeSnapshot() : PvpStateSnapshot`.
`PvpJoinRequest`·`PvpJoinResult`는 ConnectionContracts 파일에 정의한다. 호환성 정보·방 식별·재접속 자격을 입력받고 승인 여부·거절 사유를 반환하며 클라이언트 임의 점수를 입력받지 않는다.

- [ ] `PvpAdmissionTests`에 맵/설정/프로토콜 불일치·위조 신원 거절을 만들고 실패를 확인한다. 버전별 SDK 요구사항·Force Text·현재 Unity 빌드 호환성도 확인한다.
- [ ] Fusion 후보로 Host/Client 접속과 tick 입력·예측·보정을 연결한다. 공용 이동 장애물·신호 시험 상태도 권한 측에서만 바꾼다.
- [ ] 독립 Player 두 개로 Host/Client 역할을 교환하고 별도 PC·네트워크에서 실행한다. 같은 Editor의 차량 두 대만으로 통과시키지 않는다.
- [ ] `NetworkTestProfile`별 반응·보정·누적 오차·tick 비용을 기록한다. 일시 단절·재접속·정상 입력과 비정상 입력의 차이도 확인한다.
- [ ] 예측 루프 재실행 시 시험 이벤트가 중복 확정되지 않는지 확인한다. 실제 운임·감점 검증은 Task 5에서 이어 간다.
- [ ] QA-NET-01/02 및 QA-NET-03의 입장·신원·차량 복원 부분 근거로 SDK·차량 모델을 09에 기록하고 커밋한다. 실제 승객·감점 복원은 Task 5 이후 검증한다. 미달이면 NGO의 필요한 예측 구현 범위를 같은 시험으로 비교한다.

**Gate:** 실제 원격 참가자가 목표 조작감을 유지하고 권한 상태로 수렴한다. 실패 원인을 해결하기 전에 P3의 승객·점수 기능을 확대하지 않는다.

### Task 4 · P3: 공통 경기 상태와 승객의 단일 소유권

**Files — Create:**
- `unity/EungyeTaxi/Assets/Pvp/Runtime/Match/PvpRoundController.cs`, `unity/EungyeTaxi/Assets/Pvp/Runtime/Core/PvpCommandContext.cs`.
- `unity/EungyeTaxi/Assets/Pvp/Runtime/Passengers/PvpPassengerAuthority.cs`, `unity/EungyeTaxi/Assets/Pvp/Runtime/Passengers/PvpPassengerCommands.cs`.
- `unity/EungyeTaxi/Assets/Pvp/Tests/EditMode/PvpPassengerRaceTests.cs`, `unity/EungyeTaxi/Assets/Pvp/Tests/EditMode/PvpRoundBoundaryTests.cs`.
**Modify:** Task 3의 `PvpStateSnapshot.cs`에 01·04의 경기·참가·운송 상태를 연결한다.

**Interfaces:** `PvpRoundController.Advance(int authorityTick) : void`; `PvpPassengerAuthority.TryPickup(PvpPickupRequest request, PvpCommandContext context) : PvpCommandResult`; `TryDropoff(PvpDropoffRequest request, PvpCommandContext context) : PvpCommandResult`.
요청·결과 DTO는 PassengerCommands 파일에 정의한다. Context는 서버가 확인한 경기·선수·tick이고, 요청에는 안정된 요청 ID·대상 ID·기대 revision을 담는다. 완료 후보는 Task 5의 원자적 정산 경계로 넘긴다.

- [ ] `SimultaneousPickup_HasOneOwner`, `Retry_ReturnsSameTrip`, `TerminalPassenger_CannotReopen`의 실패 테스트를 먼저 만든다.
- [ ] `PvpRoundBoundaryTests`에 01의 시작/본 종료/최종 종료 경계와 같은 tick 지도·위반·승하차 순서, 재접속 유예 만료 경계, 정산 지연과 무관한 자격 동결, Host 상실의 기대 결과를 고정한다.
- [ ] 실패 확인 후 서버 공통 tick·동결 설정·공유 공급·약정 기본운임·원자적 선점을 구현한다. 현장 승차 검증과 동일 tick 중재는 04를 그대로 따른다.
- [ ] 재접속 스냅샷에 기존 소유권·요청 처리 결과·만료 상태를 포함하고 클라이언트 로컬 저장의 덮어쓰기를 거절한다.
- [ ] Task 2의 길안내에 승객 접근·하차 목표를 연결하고, 최소 화면에서 선수가 직접 목표를 선택하고 승하차 요청·거절 사유를 확인하게 한다.
- [ ] QA-PAX-01과 QA-PAX-02·QA-END-01/02·QA-NET-03의 상태 전이 부분에서 같은 승인·거절 결과를 확인하고 커밋한다. 운임 지급·감점·최종 순위까지 포함한 검사는 Task 5에서 완료한다.

**Gate:** 두 참가자가 같은 승객을 놓고 경쟁하며 같은 경기 단계로 마감한다. 완료 상태·운임 지급의 결합은 Task 5 통과 전까지 미완료다.

### Task 5 · P3: 원자적 정산·음수 점수·결과

**Files — Create:**
- `unity/EungyeTaxi/Assets/Pvp/Runtime/Scoring/PvpScoreLedger.cs`, `unity/EungyeTaxi/Assets/Pvp/Runtime/Scoring/PvpLedgerEntry.cs`, `unity/EungyeTaxi/Assets/Pvp/Runtime/Scoring/PvpSettlementService.cs`.
- `unity/EungyeTaxi/Assets/Pvp/Runtime/Scoring/PvpResultSnapshot.cs`, `unity/EungyeTaxi/Assets/Pvp/Tests/EditMode/PvpSettlementTests.cs`.
**Modify:** Task 4의 `PvpPassengerAuthority.cs`, `PvpRoundController.cs`와 Task 3의 `PvpStateSnapshot.cs`에 확정 원장·결과를 연결한다.

**Interfaces:** `PvpScoreLedger.TryAppend(PvpLedgerEntry entry) : bool`; `PvpSettlementService.TryCompleteTrip(string tripId, int authorityTick) : PvpCommandResult`; `ApplyIncident(string playerId, string incidentId, int revision, long confirmedPenalty) : PvpCommandResult`; `BuildResult() : PvpResultSnapshot`.
원장 항목·고유 키·감점 상향분·정수 환산은 05의 계약을 사용한다. `TryCompleteTrip`은 운송 완료 상태와 모든 완료 가산을 같은 처리 단위로 확정하고, `BuildResult` 호출 자격은 01이 결정한다.

- [ ] `CompletionReplay_IsIdempotent`, `CompletionFailure_IsAtomic`, `IncidentUpgrade_AppliesOnlyDelta`, `NoDelivery_IsNotWinner`를 05의 가상 산술 사례로 만들고 실패를 확인한다.
- [ ] 정수 범위 초과, 운송별 내림, 음수·동점·공동 순위, 취소 후 감점 유지, 지난 운송의 비소급을 검사하고 최소 구현한다.
- [ ] 스냅샷 복원·재계산·재접속·동일 완료 통지 재전송을 반복하여 원장 합계·완료 건수·적용 감점이 변하지 않는지 확인한다.
- [ ] 실제 두 클라이언트로 경기 종료까지 진행해 같은 원장 마지막 순번·`ResultVersion`·결과를 확인한다.
- [ ] Task 6의 실제 법규 감지 전에는 권한 측 시험 사건으로 감점·보너스·마감 정산을 검증했음을 기록한다. 화면은 잠정 성적과 확정 결과를 구분하며 실제 주행 위반의 검증 완료로 표시하지 않는다.
- [ ] QA-SCORE-01/02·QA-NET-04·QA-END-01/02의 증거를 기록하고 커밋한다.

**Gate:** 운송과 위반이 한 번만 정산되고 실제 온라인 한 경기가 결과까지 끝난다. Host 결과는 공개 랭킹의 신뢰 근거로 승격하지 않는다.

### Task 6 · P4: 공유 NPC·신호·법규 판정

**Files — Create:** `unity/EungyeTaxi/Assets/Pvp/Runtime/Traffic/PvpTrafficAuthority.cs`, `unity/EungyeTaxi/Assets/Pvp/Runtime/Rules/PvpRuleAuthority.cs`, `unity/EungyeTaxi/Assets/Pvp/Tests/PlayMode/PvpTrafficAuthorityTests.cs`.
**Modify:** Task 3의 `PvpStateSnapshot.cs`와 Task 5의 `PvpSettlementService.cs` 연결부. 기존 캠페인 `TrafficSystem`·`DrivingLaw`를 무조건 공통화하지 않는다.

**Interfaces:** `PvpTrafficAuthority.Advance(int authorityTick) : void`; `PvpRuleAuthority.Evaluate(int authorityTick) : void`. 서버의 차량·공용 교통 스냅샷을 읽고 02가 정의한 고유 사건을 Task 5의 `ApplyIncident`로 전달한다.

- [ ] `ClientsObserveSameSignalAndNpc`, `LocalMenu_DoesNotPauseAuthority`, `RecoveryMovement_DoesNotScore`, `RepeatedSensors_DoNotDuplicateIncident`를 실패 시나리오로 고정한다.
- [ ] 03의 최소 교통 프리셋을 권한 측에서 생성하고 신호 위상·다음 전환 tick·NPC 상태를 전달한다. 클라이언트별 독립 AI 판정을 제거한다.
- [ ] 02의 관찰 근거·보류·동일 사건 규칙을 구현하고 빈차 위반도 같은 정산 경로에 연결한다.
- [ ] Task 2의 예상시간 길안내에 공통 신호·교통을 연결하고, 실제 주행 사건으로 Task 5의 안전 구간·감점·하차 동시 처리 회귀를 확인한다.
- [ ] 지연 조건에서 신호 화면과 판정, 피해자 귀속, 복구 중 비정상 이득을 확인한다. QA-ABUSE-01과 QA-NET-01/04 관련 근거를 기록하고 커밋한다.

**Gate:** 교통이 공통 장면으로 작동하며 판정 근거·원장·클라이언트 표시가 같은 사건을 가리킨다. 교통량은 09의 비교 결과로 정한다.

### Task 7 · P4: UI와 2~4인 경기 템포 조정

**Files — Create:** `unity/EungyeTaxi/Assets/Pvp/Runtime/UI/PvpHudPresenter.cs`, `unity/EungyeTaxi/Assets/Pvp/Runtime/UI/PvpResultPresenter.cs`, `unity/EungyeTaxi/Assets/Pvp/Tests/PlayMode/PvpHudTests.cs`.
**Modify:** PVP 시작 씬과 09의 시험 설정 자산. 실제 설정 자산 경로는 `unity/EungyeTaxi/Assets/Pvp/Content/Config/PvpPlaytestConfig.asset`을 신규 제안한다.

**Interfaces:** `PvpHudPresenter.Render(PvpStateSnapshot snapshot) : void`; `PvpResultPresenter.Render(PvpResultSnapshot result) : void`. 06의 표시 모델을 구현하며 UI가 점수를 재계산하거나 서버 상태를 수정하지 않는다.

- [ ] `RepeatedSnapshot_DoesNotRepeatReward`, `PendingPickup_IsNotConfirmedFare`, `Menu_DoesNotFreezeOtherPlayer`를 UI 동작 검사로 고정한다.
- [ ] 로비·시작·운송·거절 이유·위반·마감·결과를 최소 화면으로 연결하고 키보드/패드 조작과 안내 우선순위를 확인한다.
- [ ] 실제 2인 플레이를 반복한 뒤 09의 확대 후보 4인을 비교한다. 한 회차에는 조작·공급·경기 길이·교통 중 판단 가능한 변수 묶음을 변경한다.
- [ ] 운송·빈차·정차·대기 시간, 운임 우위의 승패 반영, 선점 편향, 운전 판단 가독성, 재경기 의향을 10의 양식에 기록한다.
- [ ] QA-UX-01·QA-BAL-01·QA-BUILD-01 및 앞 단계 회귀를 확인하고 설정 hash·실행 빌드와 함께 커밋한다.

**Gate:** 빠른 조작과 운송 경쟁이 실제 사용자에게 성립하며, 점수 획득을 위한 대기·반복 주행이 유리하지 않다. 출시 시간·인원은 시험 근거가 있을 때 09에서 결정한다.

### Task 8 · P5 후속: Linux 전용 서버와 랭킹 신뢰 경계

**Files — Create:** `unity/EungyeTaxi/Assets/Pvp/Scenes/PvpServerBootstrap.unity`, `unity/EungyeTaxi/Assets/Pvp/Runtime/Server/PvpServerBootstrap.cs`, `unity/EungyeTaxi/Assets/Pvp/Tests/PlayMode/PvpDedicatedServerTests.cs`, `docs/technical-reference/pvp-dedicated-operations.md`.
**Modify:** Task 1의 `PvpBuild.cs`, Task 3의 `PvpFusionSession.cs`에 전용 서버 실행 경로를 추가한다.
**Interfaces:** `PvpBuild.BuildLinuxServer() : void`; `PvpServerBootstrap.StartServer(PvpMatchSettings settings) : void`. 권한 서버가 확정한 Task 5의 `PvpResultSnapshot`만 후속 결과 서비스에 제출할 수 있다.

- [ ] P4 수락·공개 경쟁 추진 여부를 확인한 뒤 Linux 지원 모듈, 별도 호스팅, 서버 자격·결과 제출 경로를 운영 문서에 확정한다.
- [ ] `HeadlessServer_CompletesMatch`, `ClientCannotSubmitRankedResult`, `RepeatedResult_IsIdempotent`, `ServerLoss_DoesNotInventWinner`의 통합 시나리오를 먼저 정의한다.
- [ ] 카메라·로컬 입력 없는 전용 서버로 기존 클라이언트 경기 전체를 실행하고, 결과 수신 측의 인증·멱등 키·버전·미완료 경기 처리를 검증한다.
- [ ] 실제 서버 자원·전송량·종료 복구·빌드 교체를 측정한다. 게임 서버 호스팅과 네트워크 서비스 비용을 별도로 산정한다.
- [ ] QA-OPS-01을 수락하기 전에는 공개 랭킹을 운영하지 않는다. 백엔드·계정·매칭의 구체 구현은 선택한 운영 계약을 입력으로 별도 계획과 검토 단위를 만든다.
- [ ] 전용 서버 산출물·서버/클라이언트 버전 조합·운영 미검증 범위를 기록하고 커밋한다.

**Gate:** 전용 서버 실행과 결과 신뢰 계약을 실제로 검증한다. 공급자·인프라·계정 설계는 P5 진입 전 선택할 후속 계약이며 현재 존재하거나 비용이 확정됐다고 간주하지 않는다.

## 후속 구현자가 사용할 검증 명령 예시

**아래 명령은 계획이며 이번 문서 작업에서 실행하지 않았다.** 테스트 파일·Category `PVP`·빌드 메서드가 구현되고 해당 Unity 및 테스트 패키지가 준비된 뒤 저장소 루트에서 사용한다. `$PvpUnityEditor`는 그 PC의 Unity `2022.3.62f2` 실행파일 절대 경로다.
실패 테스트 확인은 같은 명령에 해당 테스트의 완전한 이름을 `-testFilter`로 지정한다. 예: `Pvp.Tests.EditMode.PvpSettlementTests.CompletionReplay_IsIdempotent`.

```powershell
New-Item -ItemType Directory -Force artifacts/pvp
& $PvpUnityEditor -batchmode -projectPath unity/EungyeTaxi -runTests -testPlatform EditMode -testCategory PVP -testResults artifacts/pvp/editmode.xml -logFile artifacts/pvp/editmode.log
& $PvpUnityEditor -batchmode -projectPath unity/EungyeTaxi -runTests -testPlatform PlayMode -testCategory PVP -testResults artifacts/pvp/playmode.xml -logFile artifacts/pvp/playmode.log
& $PvpUnityEditor -batchmode -quit -projectPath unity/EungyeTaxi -executeMethod Pvp.Editor.PvpBuild.BuildClient -logFile artifacts/pvp/build-client.log
```

```powershell
# P5에서만 사용하는 후속 전용 서버 빌드 예시
& $PvpUnityEditor -batchmode -quit -projectPath unity/EungyeTaxi -buildTarget Linux64 -standaloneBuildSubtarget Server -executeMethod Pvp.Editor.PvpBuild.BuildLinuxServer -logFile artifacts/pvp/build-server.log
```

합격은 로그의 실제 오류·종료 상태와 테스트 결과 XML의 실패·누락·실행 개수를 함께 확인한다. 테스트가 하나도 발견되지 않은 결과나 빌드 성공만으로 온라인·사람 검증을 통과시키지 않는다.
Editor 자동검사 뒤 독립 Player·별도 PC에서 10의 시나리오를 수행한다. 의도한 기존 게임 코드 경계를 건드린 경우 해당 영향의 캠페인 회귀와 기본 빌드를 별도로 확인한다.
명령 형식의 근거: [Unity Test Framework 명령행](https://docs.unity3d.com/Packages/com.unity.test-framework@1.1/manual/reference-command-line.html), [Unity 2022.3 Dedicated Server 빌드](https://docs.unity3d.com/2022.3/Documentation/Manual/dedicated-server-build.html). 실제 패키지 버전 문서와 실행 결과를 우선한다.

## 작업별 인계와 완료 표시

각 Task는 [저장소 작업 규칙](../../../AGENTS.md)에 따라 요구·변경 파일·실제 명령·로그 위치·자동/사람/배포 결과·미검증 항목을 날짜별 이력에 남기고 `LATEST.md`를 갱신한다.
PVP 작업을 기존 게임의 전체 수동 검증 완료로 표시하지 않는다. 원격 반영된 커밋 해시는 실제 확인 후 기록하고, 작업 이력에 자기 자신을 포함할 해시를 추측하지 않는다.
다음 실행자는 먼저 현재 작업 트리와 최신 인계를 확인하고 첫 미완료 Task의 게이트부터 진행한다. 이번 문서 전달은 구현 착수를 뜻하지 않는다.
