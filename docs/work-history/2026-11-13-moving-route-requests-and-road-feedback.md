# 2026-11-13 운행 중 경로 변경과 은계 법규·정차 공간

## 요구사항과 구현

- 가까운 안전 하차·목적지 변경·경유 승객 요청을 주행 중에도 선택할 수 있다. 저속으로 제한하던 경로 미리보기 조건을 제거했다. 현재 방향에서 연결된 빈 정차 지점을 제안하며, 수락할 때 해당 지점의 연결과 점유 여부를 다시 검사하고 현재 위치 기준 운임·기한을 반영한다. 수락은 같은 운송에 한 번만 적용된다. 실제 하차와 경유 용무는 해당 지점에 방향을 맞춰 정차한 뒤 F로 진행한다.
- 이전에는 역주행이 2초·8m 이어졌거나 교차로의 유턴 조건을 위반해야 감점이 발생했다. 은계 지도에서 황색 실선 중앙선을 차체 중심이 전진해 넘으면 기본 운전 점수 −10, 운전 만족도 −10을 기록한다. 실선을 넘으며 반대 방향으로 크게 회전하면 비허용 유턴으로 운전 점수 −20, 운전 만족도 −12를 기록한다. 인접한 실선과 역주행 조건이 같은 사건에서 겹치는 경우 한 건만 기록한다. 표시된 유턴 개구부는 실선 검사의 대상에서 빠진다. 기존 교차로 유턴 판정도 유턴 조건 위반의 운전 만족도를 기록한다.
- 콘텐츠 관리 툴의 법규에서 중앙선 침범은 역주행의 감점 설정을 공유한다. 비허용 유턴은 진행 방향 적색 위반의 법규 감점과 함께 기록되는 운전 만족도 설정을 공유한다. 승객 탑승 중 운전 만족도는 하차 시 승객 만족도에 반영된다.
- 기존 정차 지점 31곳에 S01-B·S02-B·S03-B·S06-B·S07-B·S09-B·G09-B·G10-B를 추가했다. S03-B는 가까운 맞은편의 건물·횡단보도 간섭을 피해 약 168m 떨어진 반대 방향 접근로에 배치했다. 넓은 정차 공간은 주변 차로의 일반 검색이 끊겨도 포켓에서 출발할 때 지정 차로를 찾는다. [39곳 위치 지도](../content-design/images/eungye-pickup-map-2026-11-13.png).

## 변경 파일

- 승객 대화와 변경 약정은 `PassengerRequestObservation.cs`, `PassengerJobs.cs`, `PassengerActivitiesProbe.cs`.
- 중앙선과 유턴은 `CentreLineCrossing.cs`, `DrivingLaw.cs`, `WorldBuilder.cs`, `HdRouteTrial.cs`, `TrafficSystem.cs`, `DriveSession.cs`, `ScoreFeedback.cs`, `PlaytestBalanceTests.cs`.
- 정차 지점은 `eungye-passenger-stops.json`, `PassengerStopCatalog.cs`, `HdRoadGraph.cs`, `PassengerStopCatalogTests.cs` 및 위치 지도. 정식 지도 r18은 그대로 사용한다.

## 검증 및 재현

- 이전 동작의 실행본에 주행 중 4m/s인 안전 하차 요청을 재현했다. 요청이 표시됐지만 수락 불가로 위치 제어 검사 종료 1.
- `pwsh -NoProfile -File tools/check_workspace.ps1 -Strict` 종료 0. 필수 지도 입력 문제 0. 선택 재수입 JSON 두 개 부재.
- `pwsh -NoProfile -File tools/test_content_tuning.ps1` 종료 0. 846 검사 및 Node/C# 설정 해시 일치.
- Unity Editor `PlaytestBalanceTests.Run` 통과. 실제 점수 장부에서 중앙선 침범 −10/−10, 비허용 유턴 −20/−12 한 번씩 확인. `PassengerStopCatalogTests.Run` 2,755 검사 및 방향별 경로 2,052건 통과, 후보 거절 0. `AlternativePassengerStopTests.Run` 402 검사 통과. 도로 배치와 점수 판정은 위치 제어 검사 범위다.
- Unity 2022.3.62f2 `PrototypeBuild.BuildHybrid` 종료 0. `tools/test_passenger_activities_player.ps1`의 Player 인수를 격리된 배포본과 테스트 저장 위치에 적용했다. `-passengerActivitiesTest` 92 검사와 `-passengerActivitiesReloadTest` 6 검사 종료 0. 4m/s 주행 중 제시·수락, 중복 차단, 실제 정차 후 F, 새 프로세스에서 같은 운송·남은 경유 시간 복원까지 포함한다.
- `tools/test_pvp_native_lan.ps1` 종료 0. 같은 PC에서 통합 실행본으로 호스트 48 검사·클라이언트 60 검사 통과. LAN v19.
- 소스 커밋 `fccff24` 확인 뒤 통합 실행 폴더와 SHA256 manifest를 갱신했다. 통합/PvP `VerifyOnly` 각각 종료 0. 배포 143파일·131,612,745바이트. 선택 로컬 프로필은 별도 검증됐고 사용자 씬과 함께 작업 전 SHA256을 유지했다.
- 은계 통합본에서 일반 운송 중 경로 변경 요청이 나타나면 마우스나 Y 등 카드에 표시된 키로 수락하고, 새 내비 목적지에 정차해 F를 누른다. 지도(M)의 정차 방향 화살표를 참고한다. 황색 실선이 있는 구간에서 중앙선 통과와 방향 전환을 각각 하면 법규 이력과 운전 만족도에 감점이 표시된다.
- 사람 직접 주행, 전체 3일 캠페인, 다른 PC LAN과 실제 도로의 정차 적합성은 미검증.
