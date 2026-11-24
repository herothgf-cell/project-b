# 2026-11-24 직접 플레이 테스트 피드백 반영 (은계 싱글, 지도 r53)

## 요구

사용자가 직접 플레이하며 화면 3장과 함께 준 피드백이다.

1. 주행: 3인칭 캐주얼에서 좌우로 너무 흔들린다. 1인칭은 차체가 낮고 시트 위치·시야가 낮게 느껴진다. Taxi Life를 참고하면 어떤가.
2. 효과음: 주행음에 이상한 다른 효과음이 섞여 들린다(어떻게 만든 것인지 확인 필요). 브레이크음이 이상하다. Taxi Life 참고.
3. 설정 화면: 막대(게이지)와 수치를 무엇으로 조절하는지 색이 같아 잘 안 보인다.
4. 지도 화면 오른쪽: 글자가 너무 많아 읽기 어렵다. 아이콘을 쓰고 문구를 줄인다.
5. 은계남로 `reviewed:r47:P51`(x=662, z=90)에서 서쪽으로 갈 때 신호 대기가 80초쯤이라 너무 길다.

## 원인과 실제 구현

### 1. 3인칭 흔들림 (`DrivingCamera.cs`)

코드에서 흔들림을 키우는 요소가 셋이었다. 물리(캐주얼 횡가속 예산 8m/s²)는 건드리지 않았다.

| 요소 | 이전 | 이후 |
|---|---|---|
| 카메라 방향 | 차 방향을 그대로 따라감(위치만 초당 7 비율로 완만) | 차 방향을 초당 4.5 비율로 완만하게 따라간 뒤 위치를 맞춤. 짧게 좌우로 트는 동안 화면 전체가 같이 돌지 않는다 |
| 조향 방향 미리 보기 | 조향 입력 × 8°, 즉시 | 최대 4°, 초당 3 비율로 완만, 40→80km/h에서 30%까지 줄어듦 |
| 차체 좌우 기울기(그래픽) | 횡가속 × 0.25, 최대 2° | 횡가속 × 0.2, 최대 1.4°, 초당 3.5 비율로 완만(1인칭은 이 값의 절반) |

감각 탭의 "조향 방향 미리 보기", "차체 기울기" 막대는 그대로 이 값들에 곱해진다.

### 2. 1인칭 시점 (`DrivingCamera.cs`)

수정 전 화면에서 운전대 윗부분과 계기판이 화면 아래 약 30%를 가리고 있었다([수정 전](../ui/2026-11-24-test-feedback/first-person-before.png)). 눈 위치는 지면에서 약 1.4m로 낮지 않았지만, 운전대가 눈에서 40cm 거리에 크게 보였고 세로 시야각이 82°로 넓어 가까운 노면이 화면 아래로 몰렸다.

- 눈높이를 8cm 올렸다(차 기준 1.07 → 1.15m). 운전석 모형은 그대로 두어 운전대·계기판이 화면에서 내려간다.
- 세로 시야각 82° → 70°. 속도감 시야(+6°)는 그대로 얹힌다.
- [수정 후](../ui/2026-11-24-test-feedback/first-person-after.png): 운전대·계기판은 화면 맨 아래에만 걸리고 도로가 더 보인다.

Taxi Life는 이 작업 환경에서 실행·시청하지 못했다. 위 값은 화면 비교로 정했다.

### 3. 주행음 (`VehicleAudio.cs`, `DriveFeelAudio.cs`)

**어떻게 만든 소리였나**: 녹음 파일은 엔진·노면·비·경적·깜빡이 6개뿐이다. 11월 24일 "추가 주행음"은 새 녹음 없이 기존 녹음의 재생 속도만 바꿔 세 층을 얹은 것이다.

| 층 | 이전 방식 | 문제 | 이후 |
|---|---|---|---|
| 엔진 부하 | 엔진 녹음을 0.62~0.87배로 낮춰 **한 벌 더** 재생 | 같은 엔진이 낮은 음으로 하나 더 깔려 다른 소리처럼 들림 | 별도 음원을 없애고 엔진 한 벌의 음량에만 더함 |
| 타이어 | 노면 녹음을 1.9배로 높여 재생, 바퀴 미끄러짐 0.12부터 | 타이어 접지 한계(옆 0.2, 앞뒤 0.4)보다 낮은 값이라 **보통 제동·코너에서도 "쉬익"** 소리가 남. 브레이크음이 이상하게 들린 원인으로 본다 | 접지 한계를 넘은 만큼만 잰다(실제 미끄러짐·바퀴 잠김). 시속 18km 미만은 무음 |
| 바람 | 노면 녹음을 0.5~0.62배로 낮춰 재생, 시속 35km부터 | 바람이 아니라 낮은 웅웅거림 | 1.3~1.5배로 높여 재생. 음량 곡선을 제곱해 시내 속도에서는 거의 안 들리고 고속에서 열린다(60km/h에서 최대의 약 18%) |

설정 수치(`feel.*`)와 콘텐츠 파일은 바꾸지 않았다. 감각 탭 문구는 "추가 주행음 · 가속 부하·미끄러짐·고속 바람(싱글)"으로 고쳤다.

**전용 브레이크음은 없다.** 제동 중에는 엔진음이 내려가고 노면음이 줄어드는 것만 들린다. 브레이크 마찰음·실제 타이어 스키드음·바람음은 녹음이 있어야 넣을 수 있다(부족 목록은 [이전 기록](2026-11-24-drive-sound-rumble.md)).

### 4. 설정 막대 (`CasualUI.Slider`, `DrivingSettingsPanel.cs`)

기본 막대가 행 배경과 같은 연한 하늘색이라 보이지 않았다. 진한 회청색 트랙, 파란 채움, 흰 중심의 파란 손잡이로 바꿨다. 운전·감각·날씨 탭의 막대 전부에 적용된다. [감각 탭](../ui/2026-11-24-test-feedback/settings-feel.png)

### 5. 지도 호출 패널 (`MapDestinationUI.cs`, `PassengerJobsCallBoard.cs`, `PassengerDemandPolicy.cs`)

| 위치 | 이전 | 이후 |
|---|---|---|
| 수요 안내 | 두 줄 문장 | 아이콘 + "낮 · 업무 → 상업 손님 많음" 한 줄(전체 문장은 마우스를 올리면 표시) |
| 시설 버튼 | "생활시설 · 이동·대기·견적 비교" | 아이콘 + "생활시설 비교" |
| 운송 중 | 문장, "호출 목록" 제목, 승차/하차 버튼, "운송 완료 후 새 호출 가능" | "현재 운송" 제목, 색 점 + 승차/하차 한 줄씩. 중복 문구 삭제 |
| 호출 카드 | 5~7줄 문장 | 번호 원 + 승차/하차 2줄, 아이콘 줄(운임·기한·승차 거리와 예상 시간), 작은 글씨 태그(호출 성격·수요·미션) |
| 다음 호출 예약 | 문장 2줄(잘림) | 구간 한 줄 + 아이콘(거리·운임) |
| 아래 상태 칸 | 높이 94 | 높이 62, 막힌 사유에는 경고 아이콘 |

호출 카드에 들어가는 정보 자체는 줄이지 않았다. "하차지 수요 보통"만 생략한다. [운송 중](../ui/2026-11-24-test-feedback/map-active.png) · [호출 목록](../ui/2026-11-24-test-feedback/map-calls.png)

### 6. P51 신호 주기 (지도 r52 → r53, `Assets/Editor/P51SignalTiming.cs`)

P51은 지도에 직접 넣은 고정 주기 신호다. 네 방향이 한 방향씩 차례로 켜지고(각 녹색 12 + 황색 3 + 전적색 2초) 마지막에 대각선 포함 동시 보행 30초가 온다. 합계 98초라 한 방향은 최대 86초를 기다렸다.

- 단계 순서와 구성은 그대로 두고 시간만 바꿨다.
  - 은계남로 동행·서행: 12 + 3 + 1초
  - 아파트 정문 두 방향: 5 + 3 + 1초
  - 동시 보행: 17 + 3초(가장 긴 대각선 28.8m에 필요한 14.1초 이상)
- 합계 70초. 은계남로 최대 대기 86초 → 58초, 정문 방향 86초 → 65초.
- contentHash `bcc21050…`, 24,925,020바이트, SHA256 `6d9c15ae…`. `config/maps/local-input-manifest.json` 갱신.
- 저장 이관: `BusinessProgress`에 r53 해시 추가. r53으로 넘어오는 저장은 P51의 진입 관측·횡단 상태를 비운다(신호 시각이 달라지므로). `JunctionRouteFeedbackTests` 범위를 r53까지 넓혔다.

같은 방식의 고정 주기 신호가 3곳 더 있다(이번에 바꾸지 않음): `J5692_14220` 100초, `reviewed:P22:56` 63초, `J6480_2573` 54초.

## 변경 파일

- `unity/EungyeTaxi/Assets/Scripts/`: `DrivingCamera.cs`, `VehicleAudio.cs`, `DriveFeelAudio.cs`, `CasualUI.cs`, `DrivingSettingsPanel.cs`, `MapDestinationUI.cs`, `PassengerJobsCallBoard.cs`, `PassengerDemandPolicy.cs`, `BusinessProgress.cs`, `TrafficSmokeTest.cs`
- `unity/EungyeTaxi/Assets/Editor/`: `P51SignalTiming.cs`(신규), `JunctionRouteFeedbackTests.cs`
- `config/maps/project/eungye.project.json`, `config/maps/local-input-manifest.json`
- `docs/content-design/01-driving-and-traffic.md`, `docs/ui/2026-11-24-test-feedback/`, 이 기록, `LATEST.md`, `CURRENT-STATE.md`

## 검증

| 구분 | 검사 | 결과 |
|---|---|---|
| Editor | `P51SignalTiming.Apply` | `P51_SIGNAL_SAVED bcc21050… cycle=70`, 최대 대기 58.0 / 65.0초 |
| Editor | `JunctionRouteFeedbackTests.VerifyMigration` | `JUNCTION_MIGRATION_OK target=r53 versions=39 checks=1294` |
| Editor | `ProjectMapBuild.RunTests` | `PROJECT_MAP_PREPARED hash=bcc21050…`, `PROJECT_MAP_TESTS_OK` |
| Editor | `FeelSoundChecks.Run` · `CabinSoundChecks.Run` | `FEEL_SOUND_OK checks=11` · `CABIN_SOUND_CHECKS_OK places=39 clips=6` |
| Editor | `CurveSteeringChecks.Run` | `CURVE_STEERING_OK`(설정 181·193건, 캐주얼 27건 포함) |
| Editor | `DemandSignalChecks.Run` | `DEMAND_SIGNAL_OK checks=1810` |
| 실행본(위치 제어) | `-brightUiCheck` | `BRIGHT_UI_RESULT PASS checks=102`(세 해상도·싱글/PvP 화면 배치) |
| 실행본(위치 제어) | `-cabinControlsCheck -taxiAudioCheck` | `CABIN_PLAYER_OK checks=35`, 엔진 측정값은 이전과 같음(`fastLevel=0.166 restLevel=0.124`) |
| 실행본(위치 제어) | `-homeBaseCheck -chatNoticeCapture` | `CHAT_NOTICE_CAPTURE_OK checks=9`, 1인칭 화면 전후 비교 |
| 실행본(위치 제어) | `-drivingSettingsCheck -drivingSettingsCapture` | 21건 통과 뒤 기존 항목 "healthy durability51 keeps repair dot hidden"에서 멈춤. 11월 23일 기록과 같은 실패로, 이 PC 저장의 내구도 23 때문이다. 감각 탭 화면은 저장됨 |

실행본 검사 전에 이 PC의 저장 폴더와 설정(레지스트리)을 백업했고 검사 뒤 되돌렸다.

**하지 않은 확인**: 사람이 운전하며 보는 3인칭 흔들림과 1인칭 높이 체감, 스피커·헤드폰 실청, P51에서의 실제 대기 체감, 자동 물리 주행, 전체 캠페인.

## 같은 작업 폴더의 동시 작업

작업 중 다른 세션이 같은 작업 폴더(`main`)에서 제한속도 기능을 만들며 `PrototypeBuild.BuildHybrid`를 번갈아 실행했다(`artifacts/probes/build-speedlimit.log`). 그 세션의 미커밋 파일(`CockpitUI`, `DrivingHudArt`, `DrivingLaw`, `TrafficCar`, `TrafficSystem`, `RoadSpeedLimits`, 콘텐츠 catalog·profile 등)은 이 작업과 겹치지 않으며 이 커밋에 넣지 않았다.

- `PrototypeBuild.BuildHybrid`는 종료 0(`PROTOTYPE_BUILD_OK`)이고 위 실행본 검사는 그 실행본으로 했다. 다만 `builds/EungyeTaxi/`의 현재 실행본에는 **그 세션의 미완성 변경도 함께 들어 있다.**
- 그래서 `builds/EungyeTaxi/`와 `distribution/EungyeTaxi-build-manifest.json`은 이 커밋에 넣지 않았다. 저장소의 배포 실행본은 여전히 r52(피드백 반영 전)다. 제한속도 작업이 끝난 뒤 한 번만 다시 빌드하고 manifest를 갱신해야 한다.
- 이 PC에서는 `builds/EungyeTaxi/EungyeTaxi.exe`를 바로 실행해 이번 수정을 확인할 수 있다.

## 남은 것

- 3인칭이 여전히 흔들리면 다음 후보는 캐주얼 조향 자체다(40~70km/h 횡가속 예산 8m/s²를 6~7로 낮추기, 고속에서 키보드 입력이 차오르는 속도 줄이기).
- 브레이크·스키드·바람 전용 녹음 확보(무료 공개 음원을 받으려면 사용자 승인 필요).
- 다른 고정 주기 신호 3곳의 대기 시간 검토.
- 지도 호출 카드의 태그 줄은 미션이 붙으면 2줄이 된다. 더 줄일지 확인.

## 재현

```powershell
$unity = "<Unity 2022.3.62f2>\Editor\Unity.exe"   # PC마다 다름
& $unity -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod P51SignalTiming.Apply   # r52 지도에서만 동작
& $unity -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod JunctionRouteFeedbackTests.VerifyMigration
& $unity -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod FeelSoundChecks.Run
& $unity -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod PrototypeBuild.BuildHybrid
.\builds\EungyeTaxi\EungyeTaxi.exe -brightUiCheck -brightUiOutput artifacts/test-feedback/ui
.\builds\EungyeTaxi\EungyeTaxi.exe -homeBaseCheck -chatNoticeCapture -shiftCaptureOutput artifacts/test-feedback/home
```

게임에서: 은계 싱글 → C로 1인칭/3인칭 전환, 50~60km/h에서 좌우로 짧게 틀어 보기 → 급제동과 보통 제동의 소리 비교 → Esc → 설정 → 감각 탭 막대 → M 지도 → 은계남로 P51에서 신호 대기.
