# Task 3 — 은계 일반 차량 분산

## 요구사항과 구현

- 기존 최대 48대의 할당 객체가 실제로는 승하차 지점 12곳에서 가시 차량 12대로 재활용 대기하는 현상을 정식 은계 지도에서 재현했다.
- 정적 장애물까지 검증된 연결 경로의 일반 직진 차로 구간을 20m 간격으로 채집하고, 비어 있는 250m 셀을 우선한 뒤 기존 차량과의 거리를 고려해 배치한다. 최대 할당 48대, 기존 경로의 계속 주행, 신호·추종 규칙은 유지한다.
- 차로 끝, 교차로/신호 정지 구간, 승하차 지점 부근, 회전·차로 변경·주차·서비스 차로, 물리 장애물과 플레이어 겹침을 시작 후보에서 제외한다. 최소 중심 간격 10m를 강제한다.
- 정식 지도 에디터 검사는 가시 차량 수와 기존 12개 지역 유지 및 새 지역 확장, 간격과 배치 안전을 측정한다. 실행 시 검사는 시작 상태, 자연 주행 20초, 경로 연장, 3대 적색/녹색 대기열을 검사하도록 추가했다.

## 실제 검증

| 검사 | 명령/결과 |
| --- | --- |
| 기존 상태 RED | Unity `-batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod TrafficDistributionTests.Run`; `artifacts/traffic-distribution-red.log` 종료 1. 할당 48, 가시 12, 250m 셀 12, 경로 12, 최소 간격 325.56m, 정적/플레이어 겹침 0. |
| 수정 후 에디터 GREEN | 같은 명령, `artifacts/traffic-distribution-green.log` 종료 0. 할당 48, 가시 48, 250m 셀 28, 차로 39, 경로 30, 최소 간격 68.44m. 이전 12개 셀 모두 유지, 새 셀 16개. 정적/플레이어 겹침·비일반 시작 차로 0. |
| 자체 검토 | `git diff --check` 종료 0. 기존 `TrafficSystem.Spawn`의 전체 경로 정적 장애물 검사 및 `DistrictTraffic.Clear`의 계속 운행 가능 목표 검사를 유지한다. |

## 미검증·인계

- 실행파일의 실제 물리 시간 20초 이동, 경로 연장, 적색/녹색 대기열은 실행 시 검사 코드만 작성했고 **아직 실행하지 않았다**. 최종 BuildHybrid 후 `-trafficDistributionTest`를 한 번 실행하여 `artifacts/traffic-distribution-runtime.txt`와 프로세스 종료 코드를 확인한다.
- 사람의 1인칭 수동 주행, 20분 영업, 전체 캠페인 및 다른 PC 배포는 수행하지 않았다.
- 지도 JSON, 운임, 보행자, UI, 기본 exe와 배포 manifest는 변경하지 않았다.

## 변경 파일

- `unity/EungyeTaxi/Assets/Scripts/DistrictTraffic.cs`
- `unity/EungyeTaxi/Assets/Scripts/TrafficDistributionSnapshot.cs` 및 `.meta`
- `unity/EungyeTaxi/Assets/Scripts/TrafficDistributionTest.cs` 및 `.meta`
- `unity/EungyeTaxi/Assets/Editor/TrafficDistributionTests.cs` 및 `.meta`
- `docs/work-history/2026-10-26-traffic-distribution.md`
