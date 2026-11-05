# 사고·공업사 Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans and test-driven-development. Independent model and workshop domains are delegated under dispatching-parallel-agents; root owns integration.

**Goal:** 승인된 사고 처리와 공업사를 싱글/PVP에 적용하고 검증한 통합 실행본을 갱신한다.
**Architecture:** 엔진 독립 사고/회계 모델, 차량별 Unity 사고 controller, 공유 공업사 레이아웃을 분리한다. 기존 Live checkpoint/LAN detail에 사고 상태를 포함한다.
**Tech Stack:** Unity2022.3.62f2/C#, PowerShell, 기존 PhysX/JSON/LAN.
**Spec:** docs/content-design/08-accident-and-workshop-proposal.md (사용자 구현 승인).

## Global Constraints

사람 사고3,000원/45초·추가피로0, 기존 P15 수리/견인 유지. 은계/A2 공업사 각2곳. 차량 구매/물품·보험/과실·구급차 제외. 사용자 세이브·다른 작업 소스 보존. BuildHybrid 통합 exe/manifest 갱신.

## Review Focus

- 사고 직후/비용청구 뒤 재시작으로 처리시간 또는 비용 회피 없음.
- 도착확정/하역과 사고 동시 발생시 확정 운임 보존.
- 사람 접촉 연속 프레임/다중 collider/재접속에서 감점·청구 중복 없음.
- 경기 종료 직전 사고 비용이 최종 점수에 포함되고 다른 차량 시간 계속.
- 기존 주유소 유료 정비 예약 유지, 공업사 모든 칸 점유시 겹쳐 이동 금지.

## Task 1: 사고 모델·장부
- [x] tests/AccidentTests.cs + tools/test_accidents.ps1에 비용·미납·멱등·45초·공복경계·연료/청결 불변 RED 검사.
- [x] AccidentState.cs, CampaignAccount.cs, EconomyLedger.cs에 ChargeAccident/CompleteAccident/AdvanceAccidentProcessing 구현 및 GREEN.

## Task 2: 공업사
- [x] Workshop Editor 검사를 먼저 작성해 시설수/칸/접근/예약 계약 실패 확인.
- [x] Workshop layout/controller와 ServiceStops·VehicleDamageController·지도·봇 시설 연결. 기존 유료 bay 유지.
- [x] Editor 배치/기하 검사 및 Player 실제 입출차/4칸 점유 검증.

## Task 3: 사건·운송·복제 통합
- [x] AccidentIntegrationChecks/Player probe로 보행자·대기승객·운송 중단/확정운임·단계별 저장 RED.
- [x] AccidentController, DriveSession/TaxiController, AmbientPedestrianSystem/PassengerJobs, BusinessProgress Live, PvpSimulationMatch/NativeLan을 연결. root가 소유.
- [x] PVP45초는 기존 operation 대신 ledger service 경과로 적용, 호스트 권한·replica 상태/입력 잠금·봇 승계 검증.

## Task 4: 검증·배포
- [x] Editor/독립 모델/기존 저장·P15·LAN 회귀, BuildHybrid 후보 빌드.
- [x] 격리 저장 Player 싱글/PVP/새 프로세스 재개·두 Player LAN·실제 화면 및 물리/시설 검사. 사용자 저장 해시 비교.
- [x] 별도 읽기전용 리뷰 후 필요한 수정·검증, 소스/기획/체크리스트/이력 커밋.
- [ ] 사용자 실행 프로세스/동시 빌드 확인 후 기본 exe에 후보 반영, 배포 manifest 재생성·VerifyOnly·Strict, 실제 결과 기록.

원래 C:/git/project-b의 HUD 작업은 진행 중이므로 덮어쓰지 않는다. 원래 실행본 갱신 전 소스 통합/동시 변경을 다시 확인한다. 신규 지도 입력을 변경하지 않는 런타임 시설 생성이면 지도 hash 유지.

수용 한계: 사고 UI ScreenCapture 검은 출력으로 시각 수용 미완료. 공업사 RenderTexture 캡처 확인. 수동/다른PC/전체 캠페인은 별도.
