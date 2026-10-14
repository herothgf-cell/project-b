# Assisted Route Driving Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 조향 부담을 줄이고 차로·경로 선택에 집중할 수 있는 비교 가능한 시험 모드를 만든다.
**Architecture:** 지도 중심선에서 방향별 차로 연결망을 만들고 보조 제어기는 플레이어가 선택한 연결만 추종한다. 기존 WheelCollider와 수동 모드는 유지한다.
**Tech Stack:** Unity 2022.3.62f2, C#, Windows.
**Spec:** ../specs/2026-10-14-assisted-route-driving-design.md

## Global Constraints

- 기존 수동 주행·자동 검사 입력 우선순위를 보존한다.
- 가감속과 신호 판단은 플레이어가 수행한다.
- 차선 변경 요청을 대기열에 넣지 않는다. 내비 목적지가 조향 방향을 정하지 않는다.
- 시험은 사용자 캠페인 저장을 읽거나 쓰지 않는다.

## Review Focus

- 교차로에서 인접 반대 차로에 부착되거나 회전 예약이 뒤늦게 바뀌는 경우.
- 정지·후진·모드 변경·복귀·일시정지 시 잔여 입력과 조향 상태.
- 점 간격이 짧은 원본 도로에서 이동 궤적 불연속.
- 지원되지 않는 도로에서 자동으로 다른 도로에 붙는 경우.
- 시험 재시작이 승객·경제·교통 상태를 오염시키는 경우.

## Task 1: 차로망과 입력 정책

Files: `AssistedDrivingPolicy.cs`, `AssistedRoadNetwork.cs`, `tests/AssistedDrivingPolicyTests.cs`, `Assets/Editor/AssistedNetworkTests.cs`.
Interfaces: `AssistedDrivingPolicy.Pulse(float)`, `CanChange(int,int,int,bool,float)`, `AssistedRoadNetwork.Build(MapData)`, `FindLane(Vector3,Vector3,out float)`, `Connection(Lane,int)`.
- [x] 입력 유지/중립·차로 경계·이동 중 중복·저속 거절 테스트를 먼저 실패시킨다.
- [x] 같은 방향 차로와 노드 연결을 구성한다. 중간 degree-2 형상점을 합쳐 짧은 구간마다 교차로로 판정하지 않는다.
- [x] 실제 지도와 단순 교차로 fixture에서 역방향 제외·허용 회전·곡선 연속을 검증한다.

## Task 2: 물리 보조 주행과 UI

Files: `AssistedDriveController.cs`, `TaxiController.cs`, `TaxiSignals.cs`, `DriveSession.cs`, `DriveHUD.cs`, `AssistedDriveSmokeTest.cs`.
Interfaces: `SetActive(bool)`, `RequestLaneChange(int)`, `RequestTurn(int)`, `Apply(ref float,ref DriveMath.Demand)`.
- [x] Player 시험으로 직선 유지·양방향 차선 변경·회전·점유 거절·reset을 검증한다.
- [x] 순수 추종 조향, 변경 상태, 회전 선택 잠금, 끝점 감속을 기존 물리에 연결한다.
- [x] H 모드 전환, A/D 차로, Q/E 회전 예약과 한글 상태 표시를 연결한다.
- [x] Unity 빌드와 기존 주행/승객 회귀 검사를 수행한다.

## Task 3: 경로 시험과 전달

Files: `AssistedRouteTrial.cs`, README, `docs/assisted-driving-validation.md`.
- [x] 사용자 저장과 분리된 시험 실행 인자, 시작·종료와 경과시간, 정상/정체 선택을 추가한다.
- [x] 같은 출발/도착에 대안 경로가 실제로 연결되는지 검증한다. 경로별 시간 결과와 한계를 기록한다.
- [x] 전체 독립 테스트, 물리 smoke, 빌드를 실행하고 별도 리뷰를 받는다.

## Execution record

- 사용자 지시: 새 브랜치에서 진행, 설계 제시 후 '일단 진행해줘'. 현재 세션에서 직접 구현한다.
- 현재 코드에는 경제·매너가 구현돼 있으므로 시험 모드에서만 캠페인을 비활성화한다.
- Task 1: complete — 독립 보조 정책 15개, Unity 차로망/실제 지도 연결 검사 통과.
- Task 2: complete — 실제 Player 주행·차선 변경·회전·점유/정지/후진/일시정지/복귀 검사 통과.
- Task 3: complete — 실제 네 경로 주행 성공, 정상 본선 우세/정체 우회 우세 확인. 기존 수동·승객 검사 통과. Windows 최종 빌드 성공.
- Ruling: 기존 좌표에서 T2 직진 출구는 폭 8m로 지원 범위 밖이므로 T2 북서쪽 지원 도로를 공통 도착점으로 선택했다. 다른 도착 구간을 원하면 시험 설정을 바꿔야 한다.
- Ruling: 정체 해제는 초기 75초에서 120초로 조정했다. 75초에서는 신호 대기로 두 경로가 사실상 동률이었다. 현재 우회 이득은 5.6초이며 체감 균형은 사용자 검증 후 다시 조정한다.
- Final review: 독립 리뷰의 중요 문제 2개(재시작 차량 겹침, 직진 연결 감속)를 재현 검사 후 수정했다. 보류한 경미 항목은 없다. 좁은 회전곡선 우려는 실제 양 경로 완주 및 무감점 기록으로 시험 범위에서 확인했다.
