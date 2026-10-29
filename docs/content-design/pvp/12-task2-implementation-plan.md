# Task 2 자체 시험장·차량 후보 Implementation Plan

> REQUIRED SUB-SKILL: superpowers:subagent-driven-development. 지도/차량은 독립 파일로 구현하고 통합은 주 에이전트가 수행한다.

Goal: Task 1 검증된 별도 Player에서 자체 회색 상자 경기장을 실제 조작하고 방향 경로·후방 복구·빠른 조작 후보를 검증한다.
Architecture: Unity 2022.3 C#, 별도 Assets/Pvp. 지도는 ScriptableObject의 방향 노드/연결/정차점 정의를 사용하고 렌더와 경로가 같은 데이터를 읽는다. 차량은 상태 캡처/복원 가능한 고정 tick 평면 운동 후보를 사용한다. 네트워크 물리 채택은 P2까지 미정이다.
Spec: docs/content-design/pvp/02-driving-and-traffic.md, 03-map-and-content.md, 08-development-roadmap.md Task 2.

## 제약과 선택 근거

- 기존 은계·캠페인 코드/자산/입력/패키지 수정 없음. 외부 SDK 도입 없음.
- 시간·최종 인원·배점·충돌을 확정하지 않는다. 시험장은 오프라인이며 승객/점수/온라인 없음을 표시한다.
- 자체 맵은 외곽 순환과 내부 연결, 4구역의 정차 접근·출구, 합법 회전, 후방 복구를 포함한다.
- 후보 A/B의 가속·조향·제동을 비교하며 최종 튜닝 승인으로 표현하지 않는다.
- 자동 위치·상태 검사, 자동 주행, 에이전트 UI 조작, 사람 체감을 구분한다.

## 작업 분담과 인터페이스

| 작업 | 생산·검사 파일 | 계약 | 검증 |
|---|---|---|---|
| A 지도·경로 | Runtime/Map/*, Content/Maps/PvpArena01.asset, Editor/PvpMapTests.cs | PvpMapDefinition, PvpMapLoader.Build, PvpRoutePlanner.FindRoute, readonly runtime queries | 출발→모든 정차→출구 도달, 방향·금지 연결 거절, 경로 없음, 복구 비전진 |
| B 차량 | Runtime/Driving/PvpVehicleInput.cs, PvpVehicleMotor.cs, Editor/PvpVehicleTests.cs | Simulate(in input,float dt), Capture/Restore state, 2 tuning candidates | 입력 clamp/NaN 거절, brake priority, explicit reverse, fixed tick replay, acceleration/braking/turn metrics |
| C 통합 | Runtime/PvpPracticeSession.cs, UI/PvpPrototypePanel.cs, PvpBootstrap.cs, Editor/PvpTask2Tests.cs | 초기 설정과 별도의 오프라인 시험장 버튼, 카메라/HUD/목표 경로/복구 | Unity 검사·빌드·Player 주행 probe·UI·격리 회귀 |

A와 B는 서로 타입을 참조하지 않는다. C가 지도 질의와 차량 상태를 연결하며 두 입력이 완성된 뒤 컴파일한다. Unity 프로세스는 주 에이전트만 실행한다.

## 실행 순서

- [ ] A/B 각 행동 테스트를 먼저 작성하고 실제 실패를 기록한 뒤 최소 구현.
- [ ] C 연결 전 계약과 파일 차이 검토. 기획 기준 대비 누락/예외 확인.
- [ ] Task 2 Editor 검사를 실행하고 실제 Unity 빌드 및 Player probe 확인.
- [ ] 실제 화면에서 시험장 진입·운전·목표 변경·복구·종료 확인.
- [ ] 기획표/로드맵/09 후보/이력/LATEST에 실제 통과와 미검증을 구분해 기록.
- [ ] 전체 변경 검토 후 feature/pvp-prototype 커밋·push, 원격 SHA 확인.

## 검토 초점

잘못된 방향의 정차, 연결되지 않은 도로의 시각 교차, 복구로 목표 전진, NaN·잘못된 tick 입력, 복원 후 조향 상태 누락, UI 입력의 차량 중복 처리, 캠페인 저장/배포 덮어쓰기.
