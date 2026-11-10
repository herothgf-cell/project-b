# 2026-11-10 PVP 신호 대기·신호 교차로·뒷골목, 승객 요청 빈도 상향

## 요구 (사용자 원문)

- PVP 신호 대기가 너무 없는 것 같은데...
- PVP 맵에 신호등이 너무 없기도 함
- PVP 맵에 골목 골목 길이 조금 더 있었으면 하고
- PVP 및 싱글 모드 둘 다 승객 요청 빈도가 높았으면 좋겠어. 가중치를 높이고 해당 수치를 툴에서도 확인할 수 있게 조정해줘

## 원인

- **신호 대기가 거의 없음**: PVP A2의 보호 좌회전 신호 교차로는 수요 감응 신호(`DemandSignalClock`)로 돈다. 다른 방향에 대기 차량이 없으면 현재 녹색을 그대로 유지하고, 수요가 있는 단계만 짧게 돌린다. A2는 NPC·봇이 적어서 내가 도착하면 대부분 바로 녹색이었다.
- **신호등이 적음**: 49개 격자 교차로 중 19곳만 신호였다. 2차로끼리 만나는 안쪽 6곳과 외곽 순환로 교차로는 모두 무신호였다.
- **골목**: 9구역에 골목 고리가 하나씩만 있었다(10-30 추가분 포함).
- **요청 빈도**: 운송마다 요청 0·1·2건 비중이 15/60/25%(평균 1.10건)였다. 운행 중 요청은 진행률 25% 이후, 응답 뒤 실제 주행 20초가 지나야 나왔다. 값은 관리창의 `공통 영향 요소 > 요청 개수와 등장 시점`과 각 요청 화면 맨 아래 접힌 칸에만 있어서 찾기 어려웠다.

## 실제 구현

### PVP 지도 (`PvpCityRoadBuilder.cs`)

- 신호 교차로 19→37. 안쪽 격자 교차로 25곳을 모두 신호로 바꾸고, 외곽 순환로가 4차로 이상 간선과 만나는 T 12곳을 추가했다(`Signalized(x,z)`). 모서리 4곳과 골목 접점은 무신호로 둔다. 비보호 좌회전 2곳(2:1, 4:5)과 조건부 유턴 4경로는 그대로다.
- 뒷골목: 구역마다 기존 골목 고리 동쪽 블록에 ㅓ자 골목 5구간을 추가했다. 고리의 동쪽 T(`alley:x:z:east`)에서 동쪽으로 190~210m 간 뒤 남·북으로 갈라져, 다음 간선의 골목 분기점(`entry`/`exit`)에 합류한다. 동쪽 끝 구역은 외곽 4차로(x=6)를 같은 25%·75% 지점에서 나눈 `edge:z:entry/exit`에 붙인다. 모든 교차는 그래프 노드이며 겉모습만 겹친 길이 아니다.
- 결과(Editor 실측): 도로 252구간, 차로 링크 3,216, 차로 변경 408(외곽 4차로 분할로 변경 창 12개 감소), 노면 방향 표시 720, 건물 832(가로변 625). 18거점 306방향 연결, 주유3·정비2·세차2·출발 주차4 접근 유지.

### PVP 신호 운영 (`TrafficSignalDemand.cs`, `TrafficSystem.cs`)

- `TrafficSystem.signalRecall`을 PVP 지도(`pvp-city-a`)에서만 켠다. 수요 집계 때 직진 단계 2개와 보행 단계에 기본 수요 1을 넣어 매 주기 돌게 한다. 보호 좌회전 단계는 기존처럼 실제 수요가 있을 때만 나온다. 단계 구성·황색·전적색·보행 보호는 바꾸지 않았다.
- 결과: 한 주기 약 48초(직진 14초+전환 4초 ×2, 보행 10초+2초) + 좌회전 수요 시 추가. 은계 싱글 지도의 신호는 변경 없음(편집 신호 프로그램 또는 기존 수요 감응 유지).
- LAN: 호스트만 신호 시계를 계산하고 참가자는 받은 상태를 쓰는 기존 구조 그대로다.

### 승객 요청 빈도 (싱글·PVP 공통, `config/content/default-overrides.json`)

| 값 | 이전 | 현재 |
|---|---|---|
| `schedule.count0/1/2` (요청 0·1·2건 비중) | 15/60/25 (평균 1.10건) | **5/40/55 (평균 1.50건)** |
| `schedule.progressMin` (요청 시작 진행률) | 0.25 | **0.15** |
| `schedule.movingSeconds` (응답 사이 최소 주행) | 20초 | **12초** |
| `schedule.progressMax` / `schedule.cutoff` | 0.65 / 0.85 | 유지 |
| `request.*.weight` (요청 종류별 상대 비중) | — | 유지 |

- 한 운송의 요청은 코드상 최대 2건이다(저장·LAN 스냅샷 검사가 2건까지). 그래서 "빈도"는 2건 비중을 올리고 0건을 줄이는 방식으로 높였다. 요청 종류별 가중치는 서로의 비율만 바꾸므로 모두 올려도 횟수는 늘지 않는다. 그대로 두고 관리창에서 함께 보이게 했다.
- `progressMax`는 낮추지 않았다. 기존 저장의 요청 시점이 최대 0.65까지 있어 복원 검사에 걸리기 때문이다.
- `node tools/content-editor/choice-catalog.mjs .`로 `config/content/`와 `Assets/Resources/Content/`의 catalog·default-profile을 다시 만들고 `--verify`로 확인했다.

### 관리창 (`tools/content-editor/public/content-model.js`, `app.js`)

- `승객 요청` 탭 맨 앞에 **요청 빈도 한눈에 보기**를 추가했다. 요청 개수 비중, 등장 시점, 요청 16종의 사용 여부·가중치를 한 화면에서 고친다. 같은 프로필 ID를 모아 보여줄 뿐 값을 복사하지 않는다. 다른 화면에서 고쳐도 같은 값이다.
- 맨 위 요약 줄: `운송당 평균 요청 1.5건 · 0건 5% / 1건 40% / 2건 55%`. 값을 바꾸면 바로 다시 계산한다. 합계가 100이 아니면 경고한다. 펼치면 진행률 범위, 응답 사이 주행 시간, 요청별 비중(%)이 나온다.
- 캡처: [관리창 요청 빈도 화면](../ui/2026-11-10-pvp-signals-alleys/content-editor-request-frequency.jpg)

### 이전 사용자 프로필 호환

- 프로필 형식·ID는 바뀌지 않았다. 이전 프로필은 그대로 불러오고 검증된다.
- **이미 저장한 사용자 프로필은 저장 당시 값(예: 15/60/25)을 유지한다.** 새 기본값을 쓰려면 관리창에서 `기본값으로`를 누르거나 새 화면에서 값을 바꾼 뒤 저장·`게임에 적용`한다.
- 진행 중 캠페인·PVP 방은 시작 때의 설정을 유지한다. 새 값은 다음 새 운송부터 쓴다.

### 검사·캡처 도구

- `Editor/PvpCityA2Tests.SignalWaits`: 신호 37곳, 외곽 T 신호, 골목 접점 무신호, 뒷골목 45구간, recall 켜짐을 확인한다. 실제 신호 프로그램으로 recall 없을 때는 녹색이 유지되고 recall 있을 때는 단계가 돌며 적색이 생기는지도 확인한다. `PvpFlowUnifiedChecks.Run`에 포함했다.
- `PvpSimulationCapture`: `-pvpSimulationMenuCapture -pvpSignalAlleyCapture -pvpSimulationOutput <폴더>`. 출하 실행본에서 내 차를 1:1 교차로 정지선 앞에 **위치 제어로** 세우고, 녹색→적색→녹색 한 번의 실제 적색 시간을 기록한다. 이어서 뒷골목에서 화면과 지도를 찍는다.
- 테스트 기대값 갱신: 관리창 목록 17→18행(요약 화면), 출시 기본값 5/40/55·0.15·12초, 승인된 효과 균형 비교에서 `schedule.*`를 등장 설정으로 분류.

## 변경 파일

- 게임: `unity/EungyeTaxi/Assets/Scripts/PvpCityRoadBuilder.cs`, `TrafficSignalDemand.cs`, `TrafficSystem.cs`, `PvpSimulationCapture.cs`, `Editor/PvpCityA2Tests.cs`, `Editor/PvpFlowUnifiedChecks.cs`, `Assets/Resources/Content/catalog.json`, `default-profile.json`, `Assets/Scenes/FirstDrive.unity`(빌드 때 다시 저장된 객체 ID만 바뀜)
- 설정·도구: `config/content/default-overrides.json`, `catalog.json`, `default-profile.json`, `tools/content-editor/public/content-model.js`, `app.js`
- 테스트: `tests/PassengerRosterTests.cs`, `tests/content-editor/choice-schema.test.mjs`, `context-ui.test.mjs`, `model.test.mjs`, `passenger-roster.test.mjs`
- 출하: `builds/EungyeTaxi/`, `distribution/EungyeTaxi-build-manifest.json` (소스 575ca46 · 출하 9ae2297)
- 문서: 이 파일, `LATEST.md`, `docs/CURRENT-STATE.md`, `docs/content-design/02-passengers-and-fares.md`, `pvp/README.md`, `pvp/26-city-a2-four-player.md`, 캡처 `docs/ui/2026-11-10-pvp-signals-alleys/`

## 검증 (실제 결과)

로그는 Git 제외 `artifacts/pvp-signals-alleys/`.

| 검사 | 종류 | 결과 |
|---|---|---|
| `node tools/content-editor/choice-catalog.mjs . --verify` | 생성물 | 통과 (entries 1436) |
| `node --test tests/content-editor/` | Node | 101/101 통과(새 요약 화면 검사 1건 추가) |
| `tools/test_content_tuning.ps1` | C# 모델 | exit 0, C#/Node 해시 일치 |
| `tools/test_passenger_behavior.ps1` | C# 모델 | exit 0, 537 |
| `tools/test_passenger_roster.ps1` | C# 모델 | 기대값 갱신 후 exit 0, 2858 (갱신 전 15/60/25 기대로 실패) |
| `tools/test_offer_variety.ps1` | C# 모델 | exit 0, 593 |
| `PvpCityA2Tests.Run`·`LaneChoices`·`SignalWaits`, `PvpCityMapTests.Run`, 4인·호출·실시간 지도·호출 핀 (`PvpFlowUnifiedChecks.Run` 안) | Editor | 모두 OK. signals=37, backLanes=45, recall 시 적색 78.25초/120초, directedPairs=306 |
| `PvpJunctionFollowupChecks.Run` | Editor | 회전 궤적·예약 수명·곡선 점유 OK, 나머지 3건은 아래 기존 실패 |
| `PrototypeBuild.BuildHybrid` | 출하 | exit 0 (최종 빌드 83초 내외) |
| 출하 실행본 `-pvpSimulationMenuCapture -pvpSignalAlleyCapture` | 위치 제어 캡처 | exit 0, `PVP_SIGNAL_WAIT junction=city-junction:1:1 redSeconds=32.0 signals=37`, 캡처 5장 |
| `tools/test_pvp_passengers.ps1` | 출하 실행본 | exit 0, `PVP_PASSENGER_PLAYER_OK` |
| `tools/test_pvp_native_lan.ps1` | 출하 실행본 2개(같은 PC) | exit 0, `PVP_NATIVE_LAN_TWO_PROCESS_OK` |
| `tools/test_pvp_simulation.ps1` (은계·`-Expanded` A2) | 출하 실행본 | exit 1, 아래 기존 실패와 같은 항목 |
| `update_distribution_manifest.ps1` 생성 → `-VerifyOnly`, `update_pvp_distribution_manifest.ps1 -VerifyOnly` | 배포 | exit 0, 143파일 127,469,810바이트 |
| `check_workspace.ps1 -Strict` | 필수 입력 | exit 0, Input issues 0 |

### 기존부터 실패하던 검사 (이번 변경과 무관)

Editor 검사는 이번 변경 전 HEAD 스크립트(`PvpCityRoadBuilder`·`TrafficSignalDemand`·`TrafficSystem`·관련 Editor 파일)로 잠시 되돌려 다시 돌렸고, 실행본 검사는 이전 출하 99fd717을 별도 worktree에서 돌려 비교했다.

| 검사 | 결과 | HEAD에서 |
|---|---|---|
| `PvpNpcMergeTests.DriverIntent` ("unentered registered AI grant must release after native nav.Clear…") | 실패 | 같은 실패 |
| `PvpJunctionFollowupChecks.Run`의 turn sensing and yield ("actual native Tick physically clears selected turn") | 실패 | 같은 실패 |
| 같은 묶음의 campaign right-turn convoy ("Moving right-turn convoy blocked 11/26", 은계 지도) | 실패 | 같은 실패 |
| `test_pvp_simulation.ps1` 은계: 봇 첫 호출 경로, 실제 승차·유료 운송, 둘 다 운임, 종료 고정 호출 갱신 Complete/Failed, 하루 호출 단계 | 6건 실패 | 이전 출하 99fd717 실행본에서 같은 6건 |
| `test_pvp_simulation.ps1 -Expanded` A2: 실제 승차·유료 운송, 둘 다 운임, 종료 고정 Complete/Failed, 하루 호출 단계 | 5건 실패 | 99fd717에서 같은 5건. 새 빌드 첫 실행에서는 봇 첫 호출 경로·서비스 시간 잠금 2건이 더 실패했으나 재실행에서 통과(그때 차량 상태에 따라 흔들리는 검사) |

이 때문에 `PvpFlowUnifiedChecks.Run`과 `PvpJunctionFollowupChecks.Run` 전체는 exit 1이다. 나머지 항목은 통과했다. 인계에 적힌 기존 실패(test_prototype ManualHandlingTests 등)는 이번에 다시 돌리지 않았다.

## 미검증·남은 항목

- 사람이 직접 운전한 PVP 체감(신호 대기 길이, 뒷골목 사용감, 봇 흐름, 장시간 혼잡)은 확인하지 않았다. 캡처는 위치 제어다.
- 다른 PC 간 LAN 접속은 하지 않았다. 지도 지문이 바뀌었으므로 **양쪽 PC 모두 이번 실행본으로 갱신해야 한다.** 이전 실행본과는 "PVP 지도가 다릅니다"로 접속이 거부된다.
- 요청 평균 1.5건은 배정 기준값이다. 지도 경로·안전 대안·조합 제외 때문에 실제 표시 횟수는 조금 적을 수 있다. 사람 플레이에서 너무 잦은지 확인이 필요하다.
- C# 코드의 비상용 기본값(`PassengerRequestSchedule`의 20/60·0.25·20초)은 기본 프로필이 없을 때만 쓰이며 바꾸지 않았다.
- 위 기존 실패 3건.

## 실행·재현

```powershell
node tools/content-editor/choice-catalog.mjs . --verify
node --test tests/content-editor/
pwsh -File tools/test_content_tuning.ps1
pwsh -File tools/test_passenger_roster.ps1
# Unity Editor (실행 중인 Unity가 없을 때)
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe' -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod PvpFlowUnifiedChecks.Run -logFile artifacts/pvp-signals-alleys/flow-unified.log
# 출하 실행본 신호 대기·뒷골목 캡처
builds/EungyeTaxi/EungyeTaxi.exe -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -pvpSimulationMenuCapture -pvpSignalAlleyCapture -pvpSimulationOutput <폴더> -logFile <로그>
```

관리창은 루트 `설정 관리.cmd` → `승객 요청` 탭 → `요청 빈도 한눈에 보기`.
