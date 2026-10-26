# 2026-10-26 은계 일반 차량 가시 분포

## 요구와 실제 구현

최대 48대가 할당되어도 실제 가시 차량은 승하차 지점 12곳에 한 대씩만 남는 문제를 수정했다. `DistrictTraffic`은 기존 공통 차로 그래프에서 물리적으로 통과 가능하고 계속 운행할 수 있는 경로를 사용한다. 일반 직진 구간의 안전한 시작점을 공간적으로 골라 48대를 배치한다. 정적 장애물·플레이어와의 겹침, 차량 간 10m 미만 간격, 회전/차로 변경/주차/서비스 차로 시작을 허용하지 않는다. 지도·요금·UI는 변경하지 않았다.

## 검증과 결과

- 정식 은계 지도 RED: Unity 편집기 `TrafficDistributionTests.Run`, 종료 1. 할당 48, 가시 12, 250m 셀 12, 경로 12, 최소 간격 325.56m. `artifacts/traffic-distribution-red.log`.
- GREEN: 동일 편집기 검사, 종료 0. 가시 48/할당 48, 250m 셀 28, 경로 30, 차로 39, 최소 간격 68.44m, 정적/플레이어 겹침과 비일반 시작 0. 기존 12개 셀 전부 유지, 새 셀 16개. `artifacts/traffic-distribution-green.log`, `artifacts/traffic-distribution-editor.txt`.
- `git diff --check` 종료 0. 이는 배치 및 위치 검사다. 자동 물리 주행과 수동 플레이 성공으로 확대 해석하지 않는다.

## 실행·재현

Unity 2022.3.62f2에서 `unity/EungyeTaxi`를 대상으로 `-batchmode -nographics -quit -executeMethod TrafficDistributionTests.Run`을 실행한다. 실행파일 물리 검사는 최종 `PrototypeBuild.BuildHybrid` 이후 기본 은계 exe에 `-trafficDistributionTest` 인수를 전달하며, `artifacts/traffic-distribution-runtime.txt`와 종료 코드를 확인한다. 이 검사는 별도 시험 저장 경로를 사용한다.

## 미검증·다음 작업

물리 20초 이동, 목적지 경로 연장, 신호 3대 대기열은 시험 코드가 있으나 아직 실행되지 않았다. 최종 빌드에서 한 번 검증한다. 일반 교통 포함 수동 1인칭 주행, 20분 영업, 전체 캠페인, 다른 PC 실행도 남는다. 기본 `builds/EungyeTaxi/EungyeTaxi.exe`, 지도 입력과 배포 ZIP/manifest는 이 소스 작업에서 변경하지 않았다. 통합 시 `docs/work-history/LATEST.md`를 이 기록과 실제 빌드 결과로 갱신한다.

## 변경 파일

`unity/EungyeTaxi/Assets/Scripts/DistrictTraffic.cs`, `TrafficDistributionSnapshot.cs`/`.meta`, `TrafficDistributionTest.cs`/`.meta`, `unity/EungyeTaxi/Assets/Editor/TrafficDistributionTests.cs`/`.meta`, 이 기록, `.superpowers/sdd/2026-10-26-handling-traffic-receipt/task-3-report.md`.

## 최종 통합 결과
최종 BuildHybrid 및 해당 실행 검사 종료0. [통합 기록](2026-10-26-handling-traffic-receipt.md)의 실제 측정값·실행 범위·남은 확인을 따른다.
