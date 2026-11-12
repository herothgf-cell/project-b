# PvP 차량 안정성·정차 끊김 진단과 최적화

## 요구와 범위

- 사용자 제공 3분 18초 영상에서 차가 튀거나 이상하다는 제보. 호스트도 심하게 버벅이며 호스트1·참가자1·봇2, 정차 중에도 끊김.
- 외부 서버·중계 없이 기존 LAN 방 목록과 직접 연결을 유지한다.
- 40~50km/h 코너 개선은 **의견 요청**이다. 완만한 커브의 반응을 개선하되 좁은 90도 교차로는 감속하도록 권고했으며, 이번 조향·타이어·최고속도 수치를 변경하지 않는다.
- 시작 시 다른 교차로/승객 작업의 변경이 있었고 작업 중 `ec7aad9`로 커밋·별도 배포되었다. 사용자 FirstDrive 씬·기획 문서·설정과 그 배포 작업을 보존한다. 이번 진단 빌드는 `artifacts/pvp-stability/`에 따로 생성한다.

## 확인한 원인

- 영상의 도로 이탈/재진입과 직선 고속 구간을 프레임으로 추출·확인했다. 영상만으로 물리 튐과 화면 지연을 동일 원인으로 확정하지 않았다.
- 평지 실제 PhysX 시뮬레이션: 4초 안정화 후 20m/s에서 가속, 10초 검사. 2초 과도 구간 이후 차체 높이 범위 0.007571101m, 수직 속도 최대 0.002370352m/s, 기울기 최대 0.1545051도, 마지막 속도 24.8397m/s. **평지 기본 서스펜션에서는 큰 튐 미재현**.
- 기존 배포본 정차 월드 검사: 72 일반차·4택시, 30.00993초/1568프레임, 평균52.25FPS, p95 31.89ms, 최대104.52ms, 50ms 초과8프레임, GC0 40회.
- 숨겨진 창의 ScreenCapture가 검정으로 확인되어 위 결과는 **월드 CPU 처리/프레임 진행 측정**이며 실제 HUD·GPU 렌더 수용 결과로 사용하지 않는다. 진단 빌드 측정 중 편집기 검사와 겹친 숫자도 최종 전후 비교에 사용하지 않는다.
- 개발 빌드 Unity Profiler 300프레임(약5.43초): DrivingLaw.FixedUpdate 1281.876ms/최대13.351ms·할당48,716,288바이트, DrivingManners.FixedUpdate 482.285ms·할당48,792,948바이트. 전체 GC.Alloc 130,522,036바이트.
- 전체 교차로를 4차량×50Hz 검사하면서 반복 `new[]{-1,1}`, 문자열 키·정수 boxing, 컴포넌트 조회와 StopPosition/StopNormal의 캡처 람다를 만들었다. 정지선 정차/통과 후보가 아닌 접근도 추가 차로 확인을 했다.
- 기준 법규 tick 최대5ms 회귀 목표는 기존13.351ms로 종료1(RED). `GC.GetAllocatedBytesForCurrentThread`는 해당 플레이어에서 모두0을 반환하여 계측에서 제거하고 실제 Unity GC.Alloc 메타데이터를 사용했다.

## 구현

- TrafficJunction의 접근별 키 재사용(이름 변경 시 무효화), 부호 배열 대신 정수 순회.
- DrivingLaw/DrivingManners의 컴포넌트 조회를 접근별 루프 밖으로 이동. 기존 정차/정지선 통과 조건을 먼저 확인하되 법규 정차 기록의 만료 처리는 유지.
- StopFacility의 캡처 람다를 직접 순회로 교체. 첫 일치 항목·실시간 편집 자료 읽기를 유지하며 시설 위치를 영구 캐시하지 않는다.
- 진단 전용 프로파일 기록/집계, 평지 물리 검사, LAN 사람2·봇2 정차/높이 안정성 검사 추가.
- 기존 motion-only 검사에서 1200초 방에 300초 방의 가까운 목적지 규칙을 적용하던 검사 조건을 분리했다. 일반 LAN 수용 검사 규칙은 유지한다.

## 검증과 한계

- 평지 물리 검사 통과. 예측17·보간11 검사 통과.
- 기존 관측기 검사에 현재 지도와 맞지 않는 실패가 있어, 이번 DrivingLaw/DrivingManners 변경 전 원본으로 별도 재실행했다. J5057 좌회전 red/green 관측값2, T1-SE 유턴 이벤트0, J7254-SW 조건 단계 기대값은 **원본에서도 실패**. NativeLeftSignalRegressionTests와 TrafficSignalLeftDemandRegressionTests는 원본 통과. 이 결과를 전체 회귀 통과로 표시하지 않는다.
- 이후 최적화 전후 프로파일·LAN 정차/주행·실제 화면 확인 결과는 아래에 갱신한다.
- 실제 제보한 호스트 PC·별도 Wi-Fi PC에서의 개선 여부는 아직 미검증. CPU/GPU 기종은 제공되지 않았다.

## 재현

- Unity 2022.3.62f2 로컬 환경. `PvpStabilityTests.PhysicsBaseline`: 평지 물리 검사.
- `PvpStabilityTests.Observers`: 기존 관측기 결과를 파일로 기록(종료0은 기록 완료이며 모든 검사 통과라는 뜻이 아님).
- `PvpStabilityBuild.Build`: 사용자 FirstDrive 씬 대신 임시 진단 씬으로 별도 빌드. `-compareObservers`는 로컬 before/after 보고서를 비교. `-stabilityRelease`는 `artifacts/pvp-stability/release/` 비개발 빌드.
- 플레이어: `-pvpCityPerformanceTest -trafficPerfProfiler -trafficPerfSeconds 10 -trafficPerfOutput <출력폴더>`. `PvpProfileReport.Run -profileInput <frames.raw>`로 실제 CPU/GC.Alloc 집계.
- `pwsh -File tools/test_pvp_native_lan.ps1 -Executable <후보exe> -ParkedSeconds 30`: 사람2·봇2 정차 검사.
- 로컬 증거: `artifacts/pvp-stability/`, 영상 원본은 사용자 제공 로컬 녹화 파일이며 외부 업로드하지 않았다.
- 프로파일 API 참고: [Unity RawFrameDataView](https://docs.unity.cn/2021.1/Documentation/ScriptReference/Profiling.RawFrameDataView.html).

## 2차 최적화: PvP 호스트 렉 (같은 날 후속)

### 요구

- 사용자: "택시 프로젝트 최적화, 특히 PVP 호스트가 렉이 심하다". 호스트는 4명(사람·봇) 세션의 내비·법규·사고·승객 판정을 모두 실행하므로 참가자보다 무겁다.

### 확인한 원인 (1차 후 남은 비용, 개발 프로파일 300프레임)

- `RoadNavigation.Update` 최대 42.08ms·할당 14,325,024바이트: 운전자마다 6초마다 전체 HD 차선×점에 대해 혼잡 지연(`RoadConditions.ExtraSeconds`)과 제한속도(`RoadMapRules.TravelSeconds`, 차선마다 시설 전체 순회)를 다시 계산. 매초 대안 경로 Dijkstra도 실행하며 탐색마다 배열 3개와 `SortedSet<Tuple>` 노드를 할당.
- `DrivingManners` 5,336,688바이트: 감속 중 매 tick `Physics.SphereCastAll` 배열과 교차로마다 `new[]{-1,1}`.
- `AccidentController` 3,919,104바이트·`PassengerJobs` 1,846,800바이트: `IReadOnlyList<DriveSession> Drivers` foreach의 열거자 박싱, 정류장마다 `"waiting:"+i` 문자열, `RoadsideAt` 클로저.

### 구현 (판정·경로 결과 불변 목표)

- `HdRoadGraph`: Dijkstra 작업 배열 재사용, `(비용, 차선번호)` 사전순 이진 힙으로 교체. 이전 `SortedSet<Tuple<float,int>>`와 꺼내는 순서·동률 처리가 같다. `SearchDistances`의 반환 배열은 그대로 새로 만든다. `PathBounds` 공개.
- `RoadNavigation`: 혼잡 비용 갱신 시 사고·정체 표본 위치가 차선 경계(+1.95m) 안에 없는 차선은 지연 0으로 건너뜀(`OnSegment` 조건상 정확히 0). 제한속도 표지는 갱신마다 실시간 문서에서 차선별 색인을 다시 만들어 같은 순서로 합산(`RoadMapRules.SpeedSignIndex`). 봇은 표시하지 않는 대안 경로 탐색을 생략.
- `RoadConditions.CollectPoints` 추가.
- `PvpSimulationMatch.Drivers/Bots`: 구조체 뷰(`DriverView`, `IReadOnlyList` 구현)로 foreach 무할당.
- `DrivingManners`: `SphereCastNonAlloc`(64칸 공유 버퍼), 부호 정수 순회, 모서리 배열 정적화.
- `AccidentController`: 접촉 목록 재사용, `WaitingId` 문자열 캐시(`PassengerJobs`도 사용). `PassengerJobs.WaitingSeed` 캐시. `PvpSimulationCallBoard.RoadsideAt` 클로저 제거(첫 일치·null 예외 동일).
- 통신 형식·지도 지문·LAN v17·차량 물리 수치 변경 없음. 기존 실행본과 접속 호환 대상이다.

### 검증 (실제 명령·결과)

| 검사 | 종류 | 이전 | 이후 |
|---|---|---|---|
| 정차 PvP 월드 10초 개발 프로파일 `-pvpCityPerformanceTest` (`profile-final` → `profile-opt2`) | 위치 제어·단일 프로세스 | p99 23.29ms, 최대 152.36ms, 50ms초과3, GC0 14회 | p99 16.90ms, 최대 88.12ms, 50ms초과1, GC0 10회 |
| 같은 프로파일 `RoadNavigation.Update` | 프로파일러 | 123.6ms·최대42.08ms·14.3MB | 14.5ms·최대2.93ms·0.32MB |
| `DrivingManners.FixedUpdate` | 프로파일러 | 214.7ms·5.34MB | 105.8ms·0.55MB |
| `AccidentController.FixedUpdate` | 프로파일러 | 54.6ms·3.92MB | 39.1ms·1.04MB |
| `ScriptRunBehaviourUpdate` 최대 | 프로파일러 | 43.04ms | 3.61ms |
| LAN 두 프로세스(루프백, 사람2·봇2, 자동 주행 검사) 1회차 | 자동 물리 주행 | 호스트 frame p95 33.63ms·최대123.34 / 참가 p95 18.51 | 호스트 p95 21.78·최대56.41 / 참가 p95 18.42 |
| 같은 검사 2회차 | 자동 물리 주행 | 호스트 p95 35.7·최대271.3 / 참가 p95 18.8·최대202.5 | 호스트 p95 22.2·최대57.5 / 참가 p95 18.8·최대33.9 |
| LAN 정차 30초 `-ParkedSeconds 30` (후보만) | 자동 정차 | — | 양쪽 통과, 높이 범위 3e-8m, 호스트 p95 22.28·최대77.22 / 참가 p95 18.61 |

- 이전 = 현재 배포 `builds/EungyeTaxi/EungyeTaxi.exe`, 이후 = `artifacts/pvp-stability/release/` 비개발 후보(`PvpStabilityBuild.Build -stabilityRelease`, 컴파일 오류0, `PVP_STABILITY_CORE_OK`). 모든 LAN 검사 `PVP_NATIVE_LAN_TWO_PROCESS_OK`, 실패0.
- LAN 검사는 `-batchmode -nographics` 루프백 두 프로세스다. GPU 렌더·실제 다른 PC·Wi-Fi 지연은 포함하지 않는다.
- Dijkstra 교체는 순서 동등성을 코드로 대조했고 별도 경로 전수 비교 검사는 실행하지 않았다(LAN 주행·봇 경로 검사 통과로 간접 확인).

### 남은 항목

- **배포 실행본(`builds/EungyeTaxi`) 재빌드·manifest 갱신 안 함.** 사용자 FirstDrive 씬 변경이 작업 트리에 있어 `BuildHybrid` 반영 여부는 사용자 확인 필요.
- 남은 할당 대부분은 IMGUI(`DriveHUD.OnGUI` 5.9MB, `DrivingReadability.OnGUI` 3.6MB/300프레임). 호스트 전용 비용은 아니며 다음 후보.
- `TrafficCar.FixedUpdate`(72대, tick당 약2.6ms)와 호스트 10Hz JSON 스냅샷(참가자마다 직렬화)은 이번에 변경하지 않음.
- 실제 제보한 호스트 PC·Wi-Fi 참가 PC의 체감 재확인.

### 재현

- 개발 후보: `Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod PvpStabilityBuild.Build -logFile <log>` → `artifacts/pvp-stability/candidate/`. `-stabilityRelease` 추가 시 `release/`.
- 프로파일: `candidate/EungyeTaxi.exe -pvpCityPerformanceTest -trafficPerfProfiler -trafficPerfSeconds 10 -trafficPerfOutput <폴더> -screen-fullscreen 0 -screen-width 1280 -screen-height 720`, 집계 `Unity.exe -batchmode -quit -projectPath unity/EungyeTaxi -executeMethod PvpProfileReport.Run -profileInput <폴더>/frames.raw`.
- LAN: `pwsh -File tools/test_pvp_native_lan.ps1 -Executable <exe> [-ParkedSeconds 30]`. 증거 `artifacts/pvp/native-lan-20261112-180943-544cd18f`(이전1), `-181143-b4460770`(이후1), `-181336-a533a8d2`(이전2), `-181523-c1d57539`(이후2), `-181715-b42bb7dd`(이후 정차).
