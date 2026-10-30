# PVP 개발 로드맵 Implementation Plan

## 2026-10-30 기존 시뮬레이션 재사용 전환

사용자가 승인한 확장 A를 기존 MapData로 구현하고, 기존 DriveSession·차량·승객·내구도·화면에 봇 경기 규칙을 연결했다. 아래 평면 모터/독립 HUD 검증은 이전 구현 기록이다. 실제 은계 운송·새 맵·서비스·재시작·배포 검증은 [이번 이력](../../work-history/2026-10-30-pvp-simulation-integration.md)과 콘텐츠25를 따른다. 소스 b5b5304의 Windows 실행 폴더139파일(115,076,876바이트)을 검증했다. 은계 고정 유효 운송·확장 A 기본 호출·혼잡 경로 도착·주유·실제 충돌·재시작·두 Player 격리 검사는 통과했다. 기본 은계 임의 호출의 혼잡/수익 밸런스와 사람 조작·다른 PC·기존 물리의 유저 간 네트워크 대전은 남아 있다. 실제 게시 영수증은 작업 이력을 따른다.


> 2026-10-30 사용자 방향 수정이 아래 원계획에 우선한다. 기존 시뮬레이션의 실제 화면/차량/콘텐츠를 재사용하고 경기 규칙을 연결한다. 은계 맵으로 우선 검증할 수 있으며 전용 맵은 시안 검토 후 제작한다. 독립 차량/화면/맵 확장 경로를 계속 진행하지 않는다. [검토·맵 후보·새 순서](../../work-history/2026-10-30-pvp-simulation-realignment.md). 이 문단은 전환 전 의사결정 기록이며 현재 구현·검증 상태는 위 최신 기록을 따른다.


상태: 개발 중 / 확장 자체 맵·은계형 PVP 화면·거점/주유·한 창 봇 경기 자동 통합/실제 프레임 확인, 사람/다른PC 전체 수용 별도
작성일: 2026-10-27

진척 갱신: 2026-10-29. 최신은 [한 창 봇 경기](21-solo-bot-mode.md)와 [작업 이력](../../work-history/2026-10-29-pvp-solo-bot.md)을 따른다. Task1의 Editor/분리 Player/Windows 자동 게이트와 평면 모터 입력 운송·공유 NPC/법규/정산 검증을 통과했다. P2 전체/사람 조작감·다른PC 게이트는 미완료다. 이하 원계획/날짜별 후속에서 당시 상태와 최신 결과를 구분한다.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기존 은계 시뮬레이션과 분리된 자체 경기장에서 실제 사용자들이 운송·운전 판단으로 경쟁하는 PVP를 단계적으로 검증한다.

**Architecture:** 초기에는 같은 저장소·Unity 프로젝트에 전용 시작 씬·차량·맵·저장·빌드 경로를 둔다. 권한 서버가 경기·승객·교통·정산을 확정하며 클라이언트는 자기 차량을 예측한다. Fusion 2 Host는 P2 검증을 위한 추천 후보이고 공개 랭킹용 Dedicated Server는 후속 단계다.

**Tech Stack:** Unity `2022.3.62f2`, C#, Windows64 클라이언트, 선택 후 고정할 네트워크 SDK, 후속 Linux Dedicated Server.

**Spec:** [00 목표](00-mode-overview.md), [01 경기 정책](01-match-flow-and-rules.md), [02 주행](02-driving-and-traffic.md), [03 맵](03-map-and-content.md), [04 승객](04-passengers-and-fares.md), [05 점수](05-scoring-and-results.md), [06 UI](06-ui-and-feedback.md), [07 구조](07-network-and-build.md). 설정과 수락 기준은 [09](09-decisions-and-balance.md), [10](10-playtest-and-acceptance.md)을 함께 읽는다.

## Global Constraints

- 기준 main은 `60b15edd6094d9a6383de4d0c75c4a40d49eda40`, 작업 브랜치는 `feature/pvp-prototype`다. 초기 프로젝트는 `unity/EungyeTaxi`다.
- 2026-10-27의 문서 단계는 완료했다. 이후 구현 여부는 각 Task의 체크와 작업 이력에서 확인하며, 아직 착수하지 않은 Task의 파일·인터페이스는 후속 제안이다.
- PVP 개발은 전용 경로에 추가한다. 기존 캠페인 소스·씬·지도·배포 실행파일·manifest·사용자 `.mmst`를 함께 덮어쓰지 않는다. 필요한 의존성 변경은 해당 Task에서 이유와 영향을 기록한다.
- 자체 제작 맵을 사용하고 기존 은계 맵을 PVP 경기장으로 사용하지 않는다. 전면 공통화·새 엔진·영구 복제 브랜치를 선행 조건으로 두지 않는다.
- `MatchDurationSeconds`는 필수 양수 설정이며 제품 기본 경기 시간은 미정이다. `FinishGraceSeconds` 등 후보값은 09에만 둔다.
- 참가 인원·충돌·네트워크 SDK·배점은 사용자 확정 방향과 구분한다. 초기 초대방에 공개 매칭·계정·영구 랭킹을 선행 요구하지 않는다.
- 운임·위반·원장 정책은 04·05, 마감·재접속·결과 확정 정책은 01을 구현한다. 이 로드맵에서 별도 산식을 만들지 않는다.
- 문서 검토, 순수 코드 자동검사, Unity 검사, 실제 온라인 플레이, 배포 완료를 각각 증거로 갱신한다. 소스 추가만으로 실행 검증 체크를 완료하지 않는다.

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

아래 `Create`는 각 Task에서 추가할 경로이고 `Modify`는 선행 Task에서 만든 PVP 파일의 후속 수정이다. 실제 생성·검증 여부는 Task별 진척과 작업 이력을 확인한다.
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

- [x] `PvpIsolationTests`에 `MissingDuration_IsRejected`, `SavePath_DoesNotTouchCampaign`, `BuildFailure_RestoresSettings`를 포함한 8개 행동 검사 그룹을 먼저 작성하고 의도한 실패를 확인한다.
- [x] 순수 C# 검사의 실패→통과와 실제 두 .NET 검사 프로세스의 로그 분리를 기록한다. 기존 저장소의 정적 `Run()`/CLI 방식으로 구현해 새 테스트 패키지는 추가하지 않는다.
- [x] 전용 시작 경로·설정 동결·저장 폴더·PVP 씬을 명시한 빌드 소스를 추가한다. `DriveSession`·`CampaignSaveLocation.Resolve`를 호출하지 않는다. 런타임 소스는 실제 Unity Player 참조 DLL로 컴파일을 확인한다.
- [x] QA-ISO-01의 설정·경로·로그·복원 코드 증거와 엔진 검증의 미수행 범위를 작업 이력에 기록한다.
- [x] Unity Editor에서 새 씬/스크립트 import와 `PvpEditorIsolationTests.Run`을 실행한다. 실제 PlayerSettings·씬 목록·InputManager·수정 중 씬 보호를 확인한다.
- [x] 별도 Windows 실행파일을 빌드하고 두 Unity Player를 실행해 진단 파일 분리와 실제 캠페인 저장·기존 실행 폴더 보존을 확인한다. 화면·입력·종료를 직접 확인한다.
- [ ] QA-ISO-01·QA-BUILD-01의 실제 실행 부분을 통과시킨다. 맵 로드·다른 PC 배포 검증은 해당 선행 기능이 준비된 뒤 완료한다.

**현재 상태:** 기반 소스와 순수 코드 검사는 완료했으나 Unity 실행 검증은 미수행이다. `Runtime/Core/PvpBuildTransaction.cs`, Editor 전용 검사, 명시적 Player 진단 probe, `tools/test_pvp.py`·`tools/test_pvp.ps1`를 보조 경로로 추가했다. 상세 내용과 실행 결과는 2026-10-29 작업 이력을 따른다.

**Gate:** 독립 시작 화면과 전용 실행물이 재현되어야 한다. **2026-10-29 이 PC의 Task 1 게이트 통과**. [실제 검사 이력](../../work-history/2026-10-29-pvp-review-validation.md). Task 2로 진행하며 QA-BUILD-01 다른 PC·맵 로드는 미완료다. 아직 맵·차량·온라인 경기는 완료 상태가 아니다.

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

## 현재 구현된 검증 진입점

현재 PVP 검사는 기존 저장소처럼 정적 `Run()`/CLI 방식이다. Unity Test Framework 패키지·NUnit 검색·`PVP` Category는 추가하지 않았으므로 `-runTests -testCategory PVP`를 현재 검사 명령으로 사용하지 않는다. 후속 Task에서 검사 방식을 바꾸면 실제 의존성과 명령을 함께 갱신한다.

### 순수 코드 검사 — 실제 수행

저장소 루트에서 아래 중 사용 가능한 환경의 명령을 실행한다. Windows 스크립트는 .NET SDK 8+/Python 조합 또는 기존 .NET Framework C# 컴파일러를 사용한다. 이번 실행 환경에서는 Python/.NET SDK 경로를 수행했고 Windows PowerShell 경로 자체는 미수행이다.

```powershell
powershell -ExecutionPolicy Bypass -File tools/test_pvp.ps1
```

```bash
python tools/test_pvp.py
# dotnet이 PATH에 없는 경우
python tools/test_pvp.py --dotnet /absolute/path/to/dotnet
```

기대 결과는 `PVP_ISOLATION_OK cases=8`과 `PVP_TWO_PROCESS_DIAGNOSTICS_OK`다. 후자는 두 .NET 검사 프로세스의 파일 분리이며, Unity Player 두 대의 온라인/물리 검증이 아니다. Python 러너는 성공 결과를 `artifacts/pvp-tests/result.txt`에 남긴다. Windows .NET Framework 대체 경로는 콘솔 출력과 종료 코드를 확인한다.

Python 러너는 검증 시작 시 이전 성공 기록을 지운다. 실패한 재실행 뒤 오래된 성공 기록이 남지 않는지 확인하는 회귀 검사는 아래와 같으며, 이번 작업에서 1/1 통과했다.

```bash
python -m unittest discover -s tests -p test_pvp_runner.py -v
```

### Unity Editor 검사와 Windows 빌드 — 준비됨, 미수행

`$PvpUnityEditor`를 실제 Unity `2022.3.62f2` 실행파일 절대 경로로 지정한다. Windows64 빌드 지원 모듈이 필요하다. 같은 프로젝트를 다른 Unity 프로세스에서 닫고, 작업 중인 씬은 먼저 저장한 뒤 저장소 루트에서 실행한다. 빌드 메서드는 실행 중 플랫폼을 바꾸지 않으므로 `-buildTarget Win64`를 반드시 지정한다. 에디터 메뉴를 사용할 때도 Windows x86_64로 전환·재컴파일을 마친 뒤 실행한다.

```powershell
New-Item -ItemType Directory -Force artifacts/pvp | Out-Null
& $PvpUnityEditor -batchmode -quit -projectPath unity/EungyeTaxi -buildTarget Win64 -executeMethod Pvp.Tests.EditMode.PvpEditorIsolationTests.Run -logFile artifacts/pvp/editor-isolation.log
& $PvpUnityEditor -batchmode -quit -projectPath unity/EungyeTaxi -buildTarget Win64 -executeMethod Pvp.Editor.PvpBuild.BuildClient -logFile artifacts/pvp/build-client.log
```

각 명령의 종료 코드 0과 실제 로그의 `PVP_EDITOR_ISOLATION_OK` / `PVP_BUILD_OK`를 함께 확인한다. Editor 검사는 실제 설정을 바꾸고 복원하지만 비용이 큰 BuildPipeline 호출만 대체한다. 그 검사 성공이 실제 Player 빌드 성공을 대신하지 않는다. 빌드 메뉴는 `Taxi > PVP > Build isolated Windows client`다.

### 별도 실행과 사람 확인 — 빌드 후 수행

다음 PowerShell 예시는 만들어진 PVP 실행파일을 서로 다른 로그로 두 번 실행한다. 새 실행파일은 이번 작업에서 생성하지 않았다.

```powershell
$PvpExe = (Resolve-Path builds/PvpPrototype/PvpPrototype.exe).Path
$PvpLogA = Join-Path (Get-Location) 'artifacts/pvp/player-a.log'
$PvpLogB = Join-Path (Get-Location) 'artifacts/pvp/player-b.log'
$PvpA = Start-Process $PvpExe -ArgumentList @('-pvpIsolationCheck', '-logFile', ('"' + $PvpLogA + '"')) -PassThru
$PvpB = Start-Process $PvpExe -ArgumentList @('-pvpIsolationCheck', '-logFile', ('"' + $PvpLogB + '"')) -PassThru
$PvpA.WaitForExit()
$PvpB.WaitForExit()
$PvpA.ExitCode
$PvpB.ExitCode
```

각 Player 종료 코드 0과 `PVP_RUNTIME_ISOLATION_OK`를 확인하고 표시된 진단 파일 경로가 다른지 대조한다. 실제 캠페인 저장/백업과 기존 실행 폴더의 전후 해시도 비교한다. 마지막으로 진단 인자 없이 실행해 한글 표시·설정 입력·오류 안내·나가기를 직접 확인한다. 이 단계도 맵 주행·네트워크 접속 완료를 뜻하지 않는다.

### 후속 전용 서버

P5의 `PvpBuild.BuildLinuxServer()`는 아직 구현하지 않았다. P5 구현 때 실제 Linux Dedicated Server 지원 모듈과 서버 진입점을 검증한 뒤 명령을 추가한다.

기술 근거: [Unity 2022.3 BuildPipeline.BuildPlayer](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/BuildPipeline.BuildPlayer.html), [빌드 타깃 전환과 batch mode 제한](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/EditorUserBuildSettings.SwitchActiveBuildTarget.html). 실행 가능한 명령과 실제 종료 결과를 완료 근거로 남긴다.

## 작업별 인계와 완료 표시

각 Task는 [저장소 작업 규칙](../../../AGENTS.md)에 따라 요구·변경 파일·실제 명령·로그 위치·자동/사람/배포 결과·미검증 항목을 날짜별 이력에 남기고 `LATEST.md`를 갱신한다.
PVP 작업을 기존 게임의 전체 수동 검증 완료로 표시하지 않는다. 원격 반영된 커밋 해시는 실제 확인 후 기록하고, 작업 이력에 자기 자신을 포함할 해시를 추측하지 않는다.
다음 실행자는 먼저 현재 작업 트리와 최신 인계를 확인하고 첫 미완료 Task의 게이트부터 진행한다. 이미 추가된 기반 코드를 다시 생성하지 말고, 남은 Unity 실행 검증부터 확인한다.

## 2026-10-29 후속 검증

[Task 1 실제 Unity 검사·빌드·두 Player·초기 UI 결과](../../work-history/2026-10-29-pvp-review-validation.md)와 [콘텐츠별 검토표](11-content-elements-review.md)를 추가했다. 본문 과거 미수행 설명은 첫 기반 커밋 당시 상태이며, 이번 실제 결과를 우선한다. Task 2 계약은 [구체 구현 계획](12-task2-implementation-plan.md)을 따른다.

## Task 2 후속 진척 (2026-10-29)

- [x] 자체 지도·방향 연결·도달성·복구 비전진과 차량 입력/상태 복원 자동 검사.
- [x] 회색 상자 시험장·차량A/B·최단/예상최속 합법 경로·오프라인 시작 연결.
- [x] Unity Editor 통합 및 실제 Player 입력 모델 검사. [정확한 결과와 한계](../../work-history/2026-10-29-pvp-arena-integration.md).
- [ ] 실제 화면·키보드/패드·사람 조작감·전체 코스 비교. 사용자 요청으로 PC UI 자동 조작 중단.
- [ ] QA-MAP-01/02·QA-DRV-01 전체 수용 및 P2 진입. 소스 구현/자동 검사를 전체 게이트 통과로 올리지 않는다.

실제 파일은 계획의 이름과 일부 다르다: PvpMapRuntime.cs에 도로 런타임/경로 계산, Editor/PvpMapTests.cs·PvpVehicleTests.cs·PvpTask2Tests.cs에 기존 정적 Run 방식 검사를 둔다. 새 테스트 패키지를 추가하지 않았다.


## 직접 IP 네트워크 후속 (2026-10-29)

[실제 작업·검증 기록](../../work-history/2026-10-29-pvp-direct-ip-network.md)과 [실행 계획](13-direct-ip-network-plan.md)을 따른다.

- Task2: 표시된 실제 Player 화면에서 한글HUD·차량·도로 방향·경로·복구 안내 확인. 자동 주행 입력이며 사람 조작감·실물 패드·전체 코스 수용은 미완료.
- Task3 대안: 사용자 Fusion SDK/App ID 미준비 및 대안 요청에 따라 NGO1.12.0 직접 IP 주행 시험 구현. Host/Client 2인, 서버50Hz, 입력 예측·ACK 재계산, 동일 buildGUID/프로토콜/지도/규칙 승인, 입력 폭주 병합. 공유 장애물/신호·재접속은 미구현.
- 실제 독립 Player·지연/손실 측정값은 날짜별 이력에만 기록한다. SDK 최종 선택과 P2 전체 게이트를 자동 통과시키지 않는다.
- 다음: 별도PC LAN 접속과 사람 조작감 비교, 주행 중 보정/원격차량 보간·단절/재접속 복원·부하 측정. 검증 결과로 Fusion/NGO 채택 판단. 승객·내구도 연동은 다른 작업 완료 커밋 이후.

## 재접속·완료 콘텐츠 연동 후속 (2026-10-29)

- [x] 동일 프로세스/생존 호스트 재접속, 서버 슬롯/epoch 검증, 위조·중복·만료·명시 퇴장 규칙, 원격 차량 표시 보간.
- [x] Unity Editor/clean Windows 빌드, 실제 Player6시나리오, RTT150ms/손실3%에서4초 패킷 차단 후 복귀·재가속·정지, 기존 네트워크 회귀. [실제 수치·명령·범위](../../work-history/2026-10-29-pvp-reconnect-content.md).
- [x] 완료 main9d05a40 승객 공통 모델37변주 미리보기·서버 선택 복원. [유형·효과·내구도 적용 표](15-content-application-review.md).
- [ ] 다른PC LAN/인터넷, 실제 사람 조작감/패드/전체 코스, 공유 신호·장애물/부하 수용. P2 전체 게이트는 아직 미통과.
- [ ] P3 실제 승객 소유권·운송·정산 후 준비/응답/오염 연결. 내구도는 캠페인에서도 미구현이므로 완료 소스가 생긴 뒤 별도 검토.

현재 SDK 직접IP NGO와 차량A·무접촉·30초 유예는 시험 설정이다. 최종 SDK/인원/배점/충돌 정책과 경기 시간은 확정하지 않았다.

## 공유 교통→승객/정산 후속 (2026-10-29)

사용자가 다른PC/사람 수용을 별도로 정리하고 후속 구현까지 진행하도록 지시했다. Task3의 공통 신호/이동 장애물 최소 장면과 Task4/5 기본2인 운송 경로를 구현했으며, 정확한 실제 결과는 [날짜별 이력](../../work-history/2026-10-29-pvp-shared-competition.md)을 따른다.

- 공유 상태·단일 소유권·경계·원장·재접속/이탈·결과 일치는 자동 검사 범위에서 확인한다.
- Task4/5의 최소 화면은 시간 설정/준비·목표/승하차·거절·점수 구성·결과를 제공한다.
- P2 전체(다른PC/사람), 전체 코스 물리 운송, 공유 일반NPC/법규 전체·부하/템포는 계속 미완료다.
- 다음은 [환경별 잔여 목록](17-competition-acceptance.md)에 따라 실제 LAN/사람 운전과 Task6 교통 판정 확장을 진행한다. 유형별 준비/수면/오염과 내구도는 별도 후속이다.

## 2026-10-29 실제 입력 주행·공유 교통 후속

[최신 수용·환경별 잔여 표](19-driving-npc-rules-acceptance.md)와 [실제 검사 이력](../../work-history/2026-10-29-pvp-input-driving-npc-rules.md)을 따른다. 위치 제어 없이 실제 모터 입력으로 운송하고 서버 NPC2대/보행1명·신호/우회전/지속 속도·방향/접촉 책임·보류를 연결했다. 이전의 해당 미구현/미검증 설명은 당시 기록이다. 다른PC·사람/화면 시야·복합 교통/금지차로 자산은 별도 수용으로 남긴다.

## 최종 배포 검증 추가 — 2026-10-29

실제 소스32b2331에서 최신 main HUD를 포함해 Editor 검사·BuildClient를 다시 통과했다. 전체 PVP139파일93,199,109바이트의 복사본으로 실행 격리와 두 Player 입력 운송을 검증했다(각199.541/319.404m, fixture/도로 경계 차단0, 동일 정산/NPC 상태). PVP·기존 싱글 manifest 모두 검증0. 위치 제어 회귀·입력 주행·사람 플레이를 구분한 결과와 남은 수용은 [19](19-driving-npc-rules-acceptance.md)를 따른다.

## 한 창 봇 경기 — 2026-10-29

기존2참가자 모델에 로컬 봇을 넣어 사람 한 명이 한 실행 창에서 직접 운송/NPC/법규/정산을 시험한다. 모델 각7회·실제 Player 정상/위반 각2회 운송, fixture/경계0·위반3감점·메뉴 복귀/재시작과 기존2Player/재접속/격리 회귀를 확인했다. 사용법/요소별 범위는 [21](21-solo-bot-mode.md), 명령/실패 보완/배포는 [이력](../../work-history/2026-10-29-pvp-solo-bot.md). 시간은 입력값이며10분으로 확정하지 않는다. 봇 난도/수익 경쟁·사람/화면·다른PC·장기 교통 수용은 별도다.
## 확장 맵·은계형 UI·주유 시설 — 2026-10-29

사용자 지시대로 화면/UI→지도·법규 데이터→자체 시험장 확장/승객 거점→서비스 시설을 일괄 구현한 뒤 통합 검사를 실행했다. 기존 지도 반입이 아니라 PVP 외곽1,760m·전체520×440m 확장이다. [23번 요소별 표/사용법](23-expanded-district.md), [실제 실패·수정·검증 이력](../../work-history/2026-10-29-pvp-expanded-district.md)을 최신 기준으로 따른다. 합류 접근로도 같은 신호 사건으로 연결했고 주유 비용은 위반과 분리한다. 제품 경기 시간·배점/교통량/서비스 후보의 최종 확정이나 사람/다른PC 전체 수용으로 표시하지 않는다.

