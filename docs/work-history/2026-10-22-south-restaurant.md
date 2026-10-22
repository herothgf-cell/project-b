# 남부 식당 독립 배치 (2026-10-22)

## 요구와 구현

사용자가 전체지도에 표시한 남부 블록 안쪽 도로 부근으로 식당을 옮기고 주차·휴식 거점/승객 지점과 분리한다. 전체 지도 투영과 원본 은계 공유 도로를 대조해 목표(688,326) 근처의 진행 차로에 독립 식당 정차점을 만들었다. 실제 생성 위치는 로컬 x688.61/z328.63,공유 edge781/at83.12617이다. 사용자 첨부 범위 안이며 실제 영업 식당의 현장 확인 위치가 아닌 게임시설이다.

| 변경 파일 | 적용 |
|---|---|
| ServiceStops.cs | MealStop/StopFor,남부 식당 생성·표지,식사 가능 판정·F7/하차 후 안내·추천 거리·메뉴를 한 지점에 연결 |
| FullMapOverlay.cs / DrivingReadability.cs | 전체지도/미니맵 식당 마커를 독립 위치로 이동 |
| PassengerJobs.cs | 호출 추천의 하차 후 식당 접근 거리도 새 식당 사용 |
| OneDayCampaignProbe.cs / CompletionSmokeTest.cs / OneDayTrafficDriveTest.cs | 자동 검사도 승객 인덱스6 대신 독립 식당 사용 |
| FuelStationLayoutTests.cs | 첨부 범위·기존 지점/주차장 분리·공유 차로 위 정차·모든12승객 지점 왕복 연결 검사 |
| 기획05 / 체크리스트 / LATEST | 배치와 실제 검증/남은 확인 기록 |

승객12지점·기존 주차·수면·퇴근 위치·원본 도로/차로 연결/NPC 경로는 유지한다. 식당은 공유 차로로 안내하며 같은 방향으로 정차 후 F7로 식사한다.3,000원/50초·포만감 회복·취소 무차감 규칙 유지. 식당 안내를 자동 필수 경유지로 만들지 않는다. 식당 표지는 도로 바깥 방향에 비충돌 TextMesh로 배치하고 새 흰색 건물 박스를 만들지 않았다.

## 실제 검증

- 먼저 위치/연결 회귀를 추가해 `FuelStationLayoutTests.Run` 실행:CreateMealStop 미구현 컴파일 오류,종료1 확인.
- 구현 후 Unity2022.3.62f2 `-batchmode -nographics -quit -projectPath unity/YeongtongTaxi -executeMethod PrototypeBuild.BuildHybrid`:종료0. FuelStationLayoutTests에 새 식당 범위/분리/공유차로/12곳 왕복 검사가 포함됨. `artifacts/restaurant-build.log`의 RESTAURANT_LAYOUT 위치 확인.
- 기본 exe `-batchmode -nographics -oneDayTest -hybridV2`:종료0. 독립 식당에서 식사 확인·취소 무차감·3,000원/50초·연료 무소모·정산 저장 확인. `artifacts/one-day-runtime.txt`, `artifacts/restaurant-day.log`.
- 기본 exe `-batchmode -nographics -passengerExpansionTest -oneDayMode -hybridV2`:종료0,525 PASS/FAIL0. 기존17종/42선택·정산/저장·운송 후 식사 선택이 새 내비 목적지를 사용하는 회귀 포함.
- `pwsh -File tools/check_workspace.ps1 -Strict`:종료0/Input issues0. 필수 지도3개/ZIP/OSM 입력 변경 없음.
- scoped `git diff --check`:종료0. 기존 README/라이선스/XMind/편집기 계획의 미커밋 변경은 제외/보존.

자동 검증은 생성 그래프와 위치 제어 검사다. 새 구간의 사람 수동 주행·일반 차량 포함 자동 물리 진입·표지 실제 화면 가독성·3일 수익 밸런스는 미검증이며 별도 확인한다. 이번 작업에서 현장 지도 조사/실제 상호 확인은 재개하지 않았다.

## 실행과 다음 확인

`builds/EungyeTaxi/EungyeTaxi.exe` → M 전체지도 식당(남쪽 작은 블록) 확인 → 빈차 안전 정차 후 F7 식당 길 안내 → 새 식당의 도로변 같은 방향으로 정차 → F7 식사. 기존 승하차7에서는 더 이상 식사를 제공하지 않는다. 주차·휴식 기능은 종전 지점에 유지한다. 실행폴더 전수 해시와 실제 소스 커밋은 배포 manifest 기준이다.

## 배포 검증

실제 기록/소스 커밋은 git log로 확인한 f5e6fe5이다. 실행폴더128파일/84,883,759바이트 SHA256 전수 검사 종료0. manifest에 실제 소스 전체 해시를 기록했다.
