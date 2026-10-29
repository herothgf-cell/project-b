# 2026-10-29 교통 정체·시설 안내·점수·분산·도로 표시 후속

## 사용자 요구와 실행 계획

녹색인데 정체된 차량, 식사 후 남은 식당 안내,84점으로 보이는 운전 점수, 초과 영업 호출 밀집을 수정한다. 차량/보행자 밀도와 공간 분산을 높이고 풀·검색/갱신 예산으로 비용을 제한한다. 첨부 지도6교차로의 선/방향 화살표 중복을 정리하고 네이버 거리뷰와 비교한다. 상가 파란 두 지점은 무신호 횡단보도, 가운데 빨간 지점은 신호 횡단보도로 정정한다.

사용자 지시에 따라 모든 수정이 끝날 때까지 테스트·Unity·빌드·자동 검증을 실행하지 않는다. 먼저 소스/저장 기록/지도 자료를 읽고 코드를 수정한 뒤, 마지막 통합 단계에서 검증·수정·필요 재검사·실행 폴더 갱신·main 게시를 수행한다. 개인 저장과 다른 PvP 작업은 보존한다.

- [x] 교통: 잘못된 넓은 점유 판정 수정,72차량/36보행자 상한과 공간·시간 예산.
- [x] 시설: 완료한 식당 등 안내 해제/승객 경로 복원. 점수: 기존 좌회전 Deferred/저널 사건 합치기 원인 수정.
- [x] 호출: 초과 영업 일반 후보 유지, 동일/인접 픽업 대신 지리적 층화 선택.
- [x] 지도:6교차로 실제 폭 기반 도색 절단·중복 제거, 유턴/다른 방향 화살표 별도 배치. 상가 양끝 무신호/중앙 단일 신호 횡단보도.
- [x] 모든 수정 후 최종 회귀·실제 Player·저장 복원·밀도/CPU·화면 검사, 지도/배포 해시 및 이력 갱신.

## 위치 대응·자료 조사

사용자 지도2의 동서 주도로/식당/호수와 기존 도로 좌표로 대응한 대상은 J993_5620,J1927_5181,J5057_4449,J7973_3744,J8066_6364,J7801_9629다. 지도3 파란 점은 J5750_2576/J7204_2569, 빨간 보행 횡단은 약(648,257.3)이다. 지도 캡처 대응 위치이며 실측 신설 좌표로 표시하지 않는다.

네이버 2026년05월 거리뷰: 서쪽 상가 sQk5S2CC6NhnDTAjQO2Jbw, 동쪽 PI3bjE_fvJJ6FJjeHXaVMw는 무신호 횡단보도/상가 골목. 가운데 i7qori-dtj0W3l_zrXdqrg에서 보행 통로 앞 횡단보도와 가로형 차량 신호를 관찰했다. 모두 map.naver.com/p/entry/coordinates/3zao1h,2AGJhI의 거리뷰에서 직접 이동했다. 기존 주도로 관찰 링크는 해당 지도 차로의 evidence에 유지한다. 현재 도로상태를 추측해 보상/법규를 추가하지 않고 관찰과 게임 치수/시간 추정을 분리한다.


## 실제 변경

| 영역 | 구현 |
|---|---|
| 교통 진입 | 넓은 교차로 상자 대신 실제 이동 경로의 좁은 점유 범위 검사. 적색 대기 접근이 비충돌 녹색 흐름을 잠그지 않게 함. 비활성 차량은 예약/요청/소유권 해제 |
| 차량 분산 |72대 풀, 초기250m 셀 분산.5초마다128후보/2대 한도로 화면 밖 일반 주행 차량 보완. 주변500m 목표18대, 공여 지역 최소1대·10m 간격·플레이어/정적 충돌 보호 |
| 보행자 | 최대8허브/36명, 실제 지도 안전 경로에서23명(보도16/횡단7) 생성. 기존 출하6명 ID 계약으로 구형 저장 이관, 새 슬롯은 안전 초기화 |
| 호출 | 초과 영업에도 일반 후보 유지. 최대12승차×4목적지,3×3 층화·픽업 중복 배제·120m 간격 우선. 경로/연료/점유/기한 필터 유지 |
| 내비 | 식사/수면/주유/세차/수리 완료 시설 해제, 수락한 승차 안내 복원·운송 경로 보존 |
| 운전 기록 | 기존 보호 좌회전의 항상 Deferred 판정을 실제 좌회전 신호 판정으로 수정. 동일 사건은 최대 패널티로 합치고 별도 사건은 추가 차감. 저장된 ID에서 시퀀스 복구 |
| 도색 | 실제 진입 폭·정지선·횡단보도 기반 선 절단, 근접 중복 선 제거, 화살표 전체 크기/위치 검사. 유턴/다른 방향은 별도 간격으로 표현하고 법적 허용 집합은 유지 |
| 가져온 선 |501/502/503/506 종방향은 교차로 개방부에서 절단.505/515 도로 경계와525 유도선은 실제 횡단보도 띠에서 절단.525 회전 유도선의 교차로 내부 점선은 보존. 공간 인덱스로 검색 제한 |
| 주행 경로 |6교차로 일반 연결45개를 끝점·접선 연속인33점 곡선으로 정돈. 기존 검토/유턴 연결과 보도·건물에 간섭하는 후보3개는 유지. 지도 검사/왕복 접근 연결성 유지 |
| 상가 |J5750_2576/J7204_2569 무신호 횡단보도, J6480_2573 단일 중앙 신호 횡단보도. 양방향 차량/보행 그룹과 배타적54초 게임 주기. 신호 기둥은 도로 가장자리·머리는 차로 위 |

사용자 저장은 읽기 진단만 했다. 저장의84점은 당시 사건 누적값으로 상한이 아니며, 후속 적색 직진 두 사건은44점까지 반영되어 있었다. 좌회전 사건들이 보류된 코드 경로와 저널 사건 병합/시퀀스 문제를 별도로 수정했다.

## 최종 통합에서 발견한 문제와 처리

- 신호 규칙 변경이 `PrototypeScenario`의 도로 선택을 바꾸고, `CorridorPolish`가 다른 지역 도로/건물/보도를 다시 생성했다. 원본 검증 지도에서 물리 보정244개 소스를 고정하고 `includeScenarioRoutes=false`로 분리했다. 원본 물리 장면 SHA256을 편집 전후 비교해 동일함을 강제했으며, 해시/저장 재파싱 검사를 우회하지 않았다.
- 검토된 J5057 우회전 곡선을 일반 곡선으로 치환하면 보도 간섭이 발생했다. 검토된 곡선은 유지하고 일반 곡선도 폭 전체를0.5m 간격으로 검사한 후보만 적용했다.
- EditMode에서 일반 MonoBehaviour 비활성 이벤트가 자동 실행되지 않아 해당 단위 fixture는 핸들러를 명시 호출한다. Player에서는 실제 `.enabled=false` 이벤트로 예약/요청/소유권 해제를 별도 확인한다.
- 오래된 `TrafficDistributionTest`가 분할 시작 화면보다 먼저 business에 접근해 NullReference 후 대기했다. `StartupReady` 대기 후 검사를 시작하도록 fixture를 보완했다. 실제 사용자 게임에는 이 opt-in 코드가 실행되지 않는다.
- 화면 검사에서 생성 횡단보도에 마스크가 없는505/515 노란 경계선을 찾았다. 생성/명시 횡단보도 모두 띠 마스크를 만들고, 리뷰에서 지적한 중심 일치(reach0) 신설 횡단도 별도 회귀 검사로 보완했다.

## 변경 위치·재현

주요 코드는 `Assets/Scripts/TrafficSystem.cs`, `DistrictTraffic.cs`, `AmbientPedestrian*`, `PassengerJobs.cs`, `OneDayOfferPolicy.cs`, `ServiceStops.cs`, `DrivingLaw.cs`, `DrivingScoreJournal.cs`, `PrototypeState.cs`, `RoadPaintGeometry.cs`, `RoadJunctionGeometry.cs`, `LaneDirectionPaint.cs`, `HdRouteTrial.cs`, `RoadMapFacilities.cs`다. 경로 기준은 `unity/EungyeTaxi/`다. 대응 Editor/Player 회귀 및 `WorldFollowupChecks`/`WorldFollowupProbe`를 추가했다.

지도는 `config/maps/project/eungye.project.json`, 신호 override/교차로 검토/물리 보정 설정을 갱신했다. 필수 manifest에 실제 프로젝트 원본·신호 override·보정 설정의 바이트/SHA256을 추가했다. 원시 지도2개 및 지도 ZIP은 변경하지 않았다.

로컬 환경: Unity2022.3.62f2(`C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe`), Python은 Codex 런타임 경로. 다른 PC는 자체 설치 경로를 사용한다. 최종 재현은 `pwsh -File tools/test_prototype.ps1`, Unity `-batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod WorldFollowupChecks.Build`, 기본 `builds/EungyeTaxi/EungyeTaxi.exe`의 개별 `-worldFollowupTest`, `-navigationTripTest`, `-driveFeedbackTest`, `-trafficDistributionTest`, `-ambientPedestrianTest`→별도 프로세스 `-ambientPedestrianReloadTest`, `-trafficPerformanceTest -trafficPerfSeconds 60`이다. 모든 Player에 `-hybridV2`를 사용하고 테스트 저장은 `artifacts/`로 격리한다.


## 성능 진단·화면 표시 보완

기능 회귀가 통과해도 초기72대/23명 실제 Direct3D11/1280×720/Ultra60초에서 평균16.30fps·p95 117.94ms가 나와 배포를 보류하고 추가 진단했다. 그래픽 없는10초는53.15fps로 실제 화면 성능을 대신하지 못한다. 동일 위치/화면의10초 비교는 주행 안내 포함14.93fps, 해당 안내 컴포넌트만 제외45.28fps, 그림자만 제외13.48fps였다. 따라서 인구/안전을 줄이거나 그림자를 제거하지 않고 안내 렌더 호출을 줄였다. 신호 관찰은15ms/10초로 주된 비용이 아니었다.

미니맵의 기존 경로선은 픽셀마다 많은 GUI 사각형을 제출했다. `MapRouteRaster`의280×280 재사용 RGBA 버퍼/텍스처에 기존2겹 색/폭/끝점을 그려 최대10Hz로 갱신하고 한 번 합성한다. 경계/줌/실제 RoutePaint 객체 변경에 캐시를 무효화한다. IMGUI 그룹 안에서 회전 사각형을 쓰는 대안은 실제 화면 정렬 문제로 폐기했고, 일반 글리프/전체 지도는 기존 안전한 도장 렌더를 유지한다. 관련 경계·Y방향·색·폭 회귀를 `MapReadabilityTests`에 추가했다. 테스트 모드만 사용하는 비교 플래그/실제 전체 UI 캡처를 성능 probe에 넣었다.

보행자는 전체 횡단선5.5m 근접 보호를 먼저 유지한 뒤, 기존22m 중점 거리 조건으로 먼 차량의 경로 예측을 먼저 생략한다. 동일 조건 순서만 바꾸며 밀도/충돌 안전을 완화하지 않는다. 관련 opt-in 시간 계측을 추가했다. 기본 화질/그림자/72대 풀/안전 경로23명은 유지한다.


## 최종 검증 결과

| 실제 실행 | 결과 | 범위 |
|---|---|---|
| `pwsh -File tools/test_prototype.ps1` | 종료0 | 운행·요청·운임·경제·법규·점수/사건 모델 회귀 |
| `WorldFollowupChecks.Build` | 종료0 |11개 Editor 그룹, 지도 검증·물리 장면 동일성·접근성·회귀. Startup11672검사,72대/28셀/정적·플레이어 초기 겹침0 |
| `WorldFollowupChecks.HudAndBuild` | 종료0 | 지도 선 클리핑·래스터 회귀 + BuildHybrid. 소스 리뷰 지적 중심 횡단 마스크 누락 해소 |
| `PrototypeBuild.BuildHybrid` 최종 | 종료0 | 기본 은계 실행 폴더 갱신 |
| `-worldFollowupTest -hybridV2` | 종료0,325검사 | 실제 중앙 신호/구형 접근 좌회전·별도 위반·초과 영업 호출/저장·비활성 이벤트·13화면. 위치 제어 fixture이며 수동 주행 아님 |
| `-navigationTripTest`, `-driveFeedbackTest` | 각각 종료0 | 실제 시설 이용 완료 안내/승차 경로 복원, 독립 사건/중복·저널 재개/알림 |
| `-trafficDistributionTest` | 종료0 | 실제 물리 시뮬레이션·초기 겹침·이동/후속 연결·적색 대기3대→녹색 출발 |
| `-ambientPedestrianTest`→별도 `-ambientPedestrianReloadTest` | 각각 종료0 |23명/구형6명 계약·신호·택시/차량 접촉 방지·실제 저장 재개 |
| `-trafficPerformanceTest -trafficPerfSeconds 60` | 종료0 | 실제 Direct3D11/Ultra/1280×720,72대·23명 그대로:60.021초2586프레임, 평균43.08fps·p50 19.37ms·p95 38.82ms·p99 68.08ms. 동일 밀도 초기16.30fps/p95 117.94ms보다 개선 |
| `pwsh -File tools/check_workspace.ps1 -Strict` | 종료0, issues0 | Git 프로젝트 지도/필수 source 바이트·SHA256. 원시 지도2개는 현재 선택 모드에서 optional/미변경 |
| `git diff --check` | 종료0 | 공백 검사 |

최종 Player7개 프로세스 결과는 `artifacts/world-followup-native-results.json`, 성능은 `world-followup-release-performance.txt`, 교차로13화면은 `world-followup-J*-overhead/driver.png`와 commercial overview다. 최종 미니맵1280/1920 정렬·플레이어 화살표·경계와 전체 지도1280의 경로/호출 위치를 실제 GUI 캡처로 확인했다. 전체 지도 캡처는 열린 뒤2프레임 대기한다. 최종1920×1080 추가 프로세스도 종료0이며 미니맵/전체 지도 경로·플레이어 위치·경계를 확인했다. 해당10초 측정32.84fps는60초1280측정과 별도다. 배포 해시/게시 결과는 후속 기록에 남긴다.

이번 성능 수치는 로컬 RTX4070 Laptop/Ultra 고정 지점의1분 측정으로 모든 PC/해상도/도로60fps 보장이 아니다. 사람이 직접20분/3일 운행하는 체감, 모든 도로의 신호 정체 장시간, 차량 이동 중 UI100ms 갱신 체감은 후속 수동 항목이다. 기존 PvP/사용자 untracked8항목을 보존했다. 원시 지도 ZIP은 변경하지 않았다.


## 배포 준비 검증

이력의 실제 소스 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-29-world-traffic-followups.md`로 확인한 `7c66186b2c36f86e6f43eca5a82df5a843a9074c`다. 이 소스 기준 기본 은계 실행 폴더130파일/112,252,826바이트를 갱신했다. `tools/update_distribution_manifest.ps1 -SourceCommit 7c66186b2c36f86e6f43eca5a82df5a843a9074c -ValidatedOn 2026-10-29 -ValidationReport artifacts/world-followup-validation.json` 종료0, `-VerifyOnly` 종료0로 전체 크기/SHA256을 확인했다. 실제 프로젝트 지도contentHash는 `81091210c8826a1030a9959978e4bf4fdbbe490d76017404428136f3fe1e296b`다. main 게시·원격 동일 해시 확인은 다음 기록으로 남긴다.
