# 보호좌회전 대열과 일반 차량의 주행 반응

일반 차량이 좌회전 화살표를 받고도 줄지어 출발하지 못하고, 주행과 합류가 지나치게 느리다는 피드백을 반영했다.

## 원인과 변경

- 수인로 J7973_3744의 좌회전 연결 차로 `A2218G611452:0`은 원본 규칙 ID가 `NGII:A2218G611452`다. 두 NPC의 여정이 다르면 기존 코드는 이 값이 교차로 이름과 일치해야 같은 대열로 인정했다. 그 결과 12m 앞에서 초속 4m로 회전 중인 선두차가 후속차의 진입을 막았다. 이제 경로 생성 시 정지 지점에 실제 회전 차로 인덱스를 보관하고, 후속차가 앞차와 같은 방향·경로를 달리는지 확인한다.
- 앞차의 출발 속도를 추종 목표에 반영하고, 운전자 성향별 출발 가속과 차간 간격을 조정했다. 종전에는 12° 이상 굽거나 방향지시등이 켜진 회전 접근부에서 바로 4.5m/s로 제한했다. 이제 최대 14m 앞의 굽은 정도와 교차로까지 남은 거리에 맞춰 감속한다. 좌회전 기준 상한은 6.5m/s, 우회전은 5.2m/s이며 실제 속도에는 곡률·운전자 성향·앞차·비·도로 제한속도가 함께 적용된다.
- 은계 차로 변경 시 뒤차가 움직인다는 이유로 계속 기다리던 판정을 진입 지점의 거리와 상대 접근 속도로 바꿨다. 뒤차가 17m 뒤에서 5m/s 빠르게 접근하는 조건에서는 성급형은 들어가고 여유형·보통형은 기다린다. 나란히 있는 차량이나 충분히 가까운 고속 접근 차량은 세 유형 모두 양보한다. 실제 충돌을 강제로 발생시키는 연출은 추가하지 않았다.

## 재현과 검증

- Unity 2022.3.62f2 `-batchmode -nographics -quit -executeMethod ProtectedLeftQueueChecks.Run`. 실제 은계 차로에 서로 다른 경로의 차량 다섯 대를 6.8m 간격으로 세웠다. 적색 대기 뒤 기존 기본 녹색 8초 동안 동일한 고정 물리 간격으로 각 차량의 경로·예약·차체 이동을 실행했다. 정지선 **진입** 시각을 기록했다.

| 조건 | 진입 차량 | 녹색 시작 후 진입 시각 | 최소 차체 중심 간 거리 |
|---|---:|---|---:|
| 종전 | 1대 | 0.30초 | 6.35m |
| 개선 | 4대 | 0.26, 2.86, 4.84, 7.10초 | 6.71m |

- 개선 후 본 검사 종료 0. 차체 겹침과 적색 진입 0건, 선행차 정지·출구 점유·횡단 보행자 대기 통과. 변경 전후 로그는 `artifacts/traffic-left-nov20/baseline-queue.log`와 `queue-final.log`다. 테스트는 충돌체를 갖춘 일반 NPC의 실제 경로 추종 로직을 실행하며 물리 속도를 직접 고정하는 플레이어 주행 시험과 구분한다.
- `pwsh -File tools/test_traffic_demand_indicator.ps1` 성향·수요 검사 47건, 실패 0. `pwsh -File tools/test_prototype.ps1`의 모델 회귀도 통과했다. 관련 런타임은 `unity/EungyeTaxi/Assets/Scripts/`의 `DistrictTraffic.cs`, `TrafficSystem.cs`, `TrafficCar.cs`, `TrafficDriverProfile.cs`. 검증 코드는 `unity/EungyeTaxi/Assets/Editor/ProtectedLeftQueueChecks.cs` 및 `tests/TrafficDemandIndicatorTests.cs`.

## 통합 실행본

- 교통 소스 `cdb1900`과 계수로·신호 수정이 합쳐진 `172138d`에서 은계 r50 통합 Player를 빌드했다. 같은 소스에서 8초 좌회전 결과를 다시 확인했고, 교차 진로 점유·적색 대기 예약 해제·PvP 대열 회귀 및 `PrototypeBuild.BuildHybrid`가 모두 종료 0이었다. PvP 대열 시험에서 최소 중심 간격 10.00m, 후속차 정차 프레임 0이었다. 로그는 `artifacts/traffic-left-nov20/release.log`다.
- 격리 빌드에서 `-batchmode -nographics -junctionGridlockTest`로 보행자와 자연 차량 72대를 유지했다. 출발지 교차로 반경 65m에서 서로 다른 17대를 관찰했고 게임 시간 201.18초 동안 5초 이상 지속된 상호 대기와 90초 이상 고정된 차량이 없었다. 위치 제어 대열 시험과 별도의 자동 관찰이며 사람이 운전한 캠페인이나 전체 도로의 사고율 측정은 수행하지 않았다.
- 기본 실행 경로는 `builds/EungyeTaxi/EungyeTaxi.exe`다. 기본 폴더의 Burst 네이티브 DLL은 사용 중이어서 기존 파일과 재빌드 파일의 바이트를 비교했다. PE/COFF 타임스탬프 4바이트 필드를 제외하면 두 파일 전체가 동일해 설치된 DLL을 유지했다. 통합 manifest를 설치된 파일에서 생성했다. 일반·PvP `-VerifyOnly` 종료 0, 배포 143파일 145,403,446바이트와 별도 사용자 프로필 검증. `tools/check_workspace.ps1 -Strict` 필수 입력 문제 0.
- 설치된 기본 Player에서도 같은 자연 교통 검사를 반복해 종료 0을 확인했다. 출발지 교차로 반경의 차량 17대를 게임 시간 202.51초간 관찰했고 두 정체 기준에 걸린 차량은 0대였다. 재현 명령은 `builds/EungyeTaxi/EungyeTaxi.exe -batchmode -nographics -junctionGridlockTest`, 결과는 `artifacts/traffic-left-nov20/shipped-traffic.txt`다.
