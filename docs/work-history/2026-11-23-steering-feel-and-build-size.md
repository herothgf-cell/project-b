# 2026-11-23 조향 프리셋(캐주얼/시뮬)·감각 연출과 빌드 용량 최적화 (LAN v24)

## 요구

1. [08 재미 보강](../content-design/08-fun-improvements.md) 1장(조향 프리셋, 감각 연출)만 진행한다.
   - 8장 "구현 전에 코드에서 확인할 것" 중 조향·카메라 항목을 코드와 대조한다.
   - 계획을 보여 주고 확인을 받은 뒤 구현한다.
   - 캐주얼(새 경력 기본)과 시뮬(현재 값)을 둔다. 캐주얼은 40~70km/h 횡가속 상한 7~8m/s², 회전 보조 80km/h까지다.
   - 목표는 50~60km/h에서 한 차로(3.5m) 이동 1.2초 안과 직진 복귀다.
   - 감각 연출 4종을 넣고, 각각 강도 조절·끄기가 가능해야 한다.
   - `CurveSteeringChecks` 방식으로 변경 전후, 기울기·전복을 비교한다.
   - 빌드하고 키보드·패드 비교 재현법을 적는다. LAN 버전, 01 문서, 작업 이력을 갱신한다.
2. 같은 요청에서 "용량 최적화도 같이"를 받았다. r52 빌드에서 `resources.assets`가 54MB로 커진 문제다.

계획(코드 대조·용량 원인·구현안)은 사용자가 "계획대로 진행"으로 승인했다.

## 코드 대조에서 문서와 달랐던 점

- 횡가속 5m/s² 상한은 두 군데에 있다.
  - 회전 보조의 목표가 **40km/h부터** 5m/s²로 묶인다.
  - 60km/h 이상에서는 조향각 공식(`SteerAngle = atan(5·2.6/v²)`) 자체가 5m/s²다. 50km/h 바퀴각 6.48°는 이미 약 8.4m/s²를 낼 수 있는 각도다.
- 설정에는 이미 "조향: 부드럽게/표준/빠르게"가 있었다. 입력 감도(민감도·반응·복원)만 바꾸는 버튼이다. 이번에 "입력: …"으로 이름을 바꿨다.
- 운전 설정은 경력 저장과 무관한 PC별 설정(PlayerPrefs)이다. 그래서 "새 경력 기본"은 "설정이 없는 새 기기 기본"으로 구현했다. 이미 설정을 저장한 기기는 운전감이 갑자기 바뀌지 않도록 시뮬로 둔다.
- 1인칭 핸들 회전과 조향 방향 선행 시선(V, 6°, 기본 꺼짐)은 이미 있었다. 시야각은 1인칭 82°·3인칭 62° 고정이었다. 1인칭 카메라는 차체 기울기를 일부러 따라가지 않는다.
- **1.2초 목표는 7~8m/s² 상한과 동시에 맞출 수 없다.**
  - 3.5m를 옮겼다가 직진으로 끝나는 차선 변경의 최소 시간은 약 √(4×3.5/a)다. 8m/s²에서 1.32초, 5m/s²에서 1.67초다.
  - 문서의 "이론상 약 1.7초"도 같은 식이다.
  - 그래서 상한은 범위 위쪽 끝인 8m/s²로 정했다. 1.2초는 검사에서 목표 대비 측정값으로만 보고한다.

## 실제 구현

### 조향 프리셋

- `DrivingSettings.steeringFeel`: 캐주얼 0, 시뮬 1이다.
  - 저장값이 없는 기기는 캐주얼, 이 필드 이전에 저장된 설정은 시뮬, 알 수 없는 값은 캐주얼로 처리한다.
  - 연출 강도 `speedFov`, `steerLook`, `brakeDip`, `bodyLean`을 추가했다. 범위는 0~1, 기본은 1이다.
- `DriveMath`:
  - 시뮬 경로(`ManualSteerAngle(kph)`, `ManualYawAcceleration(…)`)는 그대로다.
  - 캐주얼 횡가속 예산은 70km/h까지 8m/s²이고, 70~85km/h에서 시뮬의 5m/s²로 부드럽게 이어진다.
  - 캐주얼 바퀴각 = max(시뮬 바퀴각, atan(예산·2.6/v²))이다.
  - 캐주얼 회전 보조는 목표 횡가속 상한도 같은 예산을 쓴다. 보조 강도는 50~70km/h에서 0.8을 유지하고 70~80km/h에서 0으로 줄어든다(시뮬은 60km/h에 0).
  - 바퀴각 비교(km/h): 60에서 시뮬 2.68° → 캐주얼 4.28°, 70에서 1.97° → 3.16°. 50km/h 이하는 같다.
- `TaxiController.CasualSteering`:
  - PvP(로컬 봇 대전, LAN 자기 차 예측, 복제 차)는 항상 시뮬이다.
  - 싱글에서 사람이 운전할 때만 기기 설정을 따른다. 자동 검증과 봇은 시뮬이며, 검사는 `SteeringFeelOverride`로 지정한다.
- LAN 호환 계약 `native-city-lan-v23` → **`v24`**. PvP 물리는 바뀌지 않았지만, 조향 코드가 바뀐 실행본끼리 섞이지 않게 올렸다.

### 감각 연출

`DrivingCamera`에 넣었다. 화면만 바뀌고 물리·승차감 판정·저장·PvP 상태는 그대로다.

- **속도감 시야**: 20~80km/h에서 1인칭 +6°, 3인칭 +8°까지 넓어진다. 검사·컷신이 직접 정한 시야각은 건드리지 않는다.
- **조향 방향 미리 보기**: 1인칭 6°(기존 V 토글을 이제 기본 켜짐), 3인칭 8°까지 꺾는 쪽을 먼저 본다.
- **제동 쏠림**: 감속에 비례해 시선이 최대 2.5° 숙여진다.
- **차체 기울기**: 바깥 차체 그래픽(`Taxi exterior`)만 기운다. 선회 롤 최대 2°, 제동·가속 피치 최대 1.2°다. 1인칭은 롤의 절반만 기운다.
- 설정 → 새 **감각** 탭: 캐주얼/시뮬 버튼과 강도 막대 4개(0%=끔)가 있다. 기본값 버튼은 캐주얼·100%로 되돌린다. 운전 탭의 기본값 버튼은 감각 설정을 바꾸지 않는다.

### 빌드 용량

- **A. 원본 지도 제외**:
  - r52 빌드에는 `Assets/Resources`의 `eungye-map.json`(4.5MB)과 `route42-map.json`(2.9MB)이 들어갔다. 이 PC에만 있는 로컬 입력이고, 이전 출하 빌드 8번에는 없었다.
  - 실행 중에는 프로젝트 지도만 읽는다. 그래서 `PrototypeBuild.BuildHybrid`가 빌드 동안 두 파일을 `Assets/BuildExcludedResources~`(Unity가 가져오지 않는 폴더)로 옮겼다가 항상 되돌린다.
- **B. 프로젝트 지도 gzip**:
  - `ProjectMapBuild.Prepare`가 `eungye-project-map.bytes`(gzip, 4,565,203바이트)를 만든다. 원래 JSON은 24.9MB다.
  - 압축을 풀어 원본과 같은지 검증한다. 예전 `.json` 리소스가 있으면 지운다.
  - 실행 중에는 `ProjectMapStore.ReadResource`가 gzip 여부를 보고 읽는다. 저장소의 `config/maps/project/eungye.project.json`은 그대로다.
- **C. UI 이미지 BC7**:
  - `UiTextureCompression.Apply`는 Windows용 BC7(최고 품질)을 지정한다. BC7은 모든 밉 단계가 4의 배수여야 하고, 아니면 Unity가 말없이 RGBA32로 둔다.
  - 시설 사진 8장은 `ScaleAndCrop`으로 그려서 비율이 중요하다. 그래서 원본을 834×468/469에서 가운데 832×468로 한 번 잘랐다(JPEG 품질 95).
  - 승객 초상화(0.5 UV 칸)와 방 조명 빛 2장(정해진 사각형에 늘려 그림)은 가장 가까운 2의 거듭제곱으로 저장한다(1254→1024, 568×388→512×512).
  - 부품 이미지 4장은 그대로 BC7이다. 작은 이미지와 방 배경(기존 DXT1)은 기본 설정을 유지한다.
  - Resources 이미지 저장 크기: 24.72MB → 9.17MB.

## 검증

### 조향 수치 (`CurveSteeringChecks.Run`, Editor 평지 물리, 속도 고정)

차선 변경은 두 프리셋에 같은 "운전자"를 쓴다. 다음 차선을 향한 방향(1m당 4°, 최대 20°)으로 조향하며, 입력이 대부분 ±1에 붙어 키보드와 비슷하다. 지표는 3.25m 도달 시간과 정착 시간(목표 ±0.25m, 방향 오차 1° 이내가 0.5초 유지)이다.

| 프리셋 | km/h | 3.25m 도달 | 정착 | 정착 후 방향·회전 | 최대 기울기 | 바퀴 접지 |
|---|---:|---:|---:|---|---:|---:|
| 시뮬 | 50 | 2.22초 | 2.72초 | 0.18° · 1.09°/s | 0.79° | 4 |
| 캐주얼 | 50 | 2.20초 | 2.72초 | 0.18° · 1.09°/s | 0.78° | 4 |
| 시뮬 | 60 | 2.04초 | 3.46초 | 0.71° · 0.65°/s | 0.73° | 4 |
| 캐주얼 | 60 | **1.88초** | **2.38초** | 0.58° · 1.36°/s | 0.82° | 4 |

- 50km/h는 시뮬 바퀴각(6.48°)이 이미 캐주얼 8m/s² 각도(6.15°)보다 커서 같다.
- 1.2초 목표는 두 프리셋 모두 미달이다(위 물리 한계 설명 참고).

최대 조향 유지(3초)에서 측정한 횡가속·기울기:

| 프리셋 | 60km/h | 70km/h | 80km/h |
|---|---|---|---|
| 시뮬 | 3.74m/s² · 0.81° | 3.65 · 0.80° | 3.57 · 0.80° |
| 캐주얼 | 6.21m/s² · 1.20° | 6.11 · 1.19° | 4.15 · 0.89° |

- 모든 경우에 바퀴 4개가 접지했고 차체가 똑바로 섰다(upright 1.000). 전복과 들림은 없다.
- 시뮬의 기존 `CURVE_PHYSICS` 17줄은 변경 전 기준 실행과 문자 그대로 같다.

### Editor 검사

| 검사 | 결과 |
|---|---|
| `CurveSteeringChecks.Run` | `CURVE_STEERING_OK`. 안에서 `DRIVING_SETTINGS_OK checks=181`, `DRIVING_SETTINGS_MENUS_OK checks=193`, `CASUAL_DRIVE_OK checks=27`. 새 설정 규칙(새 기기 캐주얼, 기존 저장 시뮬, 0~1 범위, LAN 예측·복제 시뮬)을 포함한다. |
| `CurveSteeringChecks.RunCornerPedals` | `CORNER_PEDALS_OK scenarios=12` |
| `BrakingSettingsChecks`·`ContentTuningChecks`·`MenuConditionFollowupTests` | 22·32·19건 통과(v24 계약 포함) |
| `StartupPathTests.RunValidationBroadPhase` | 통과(gzip 리소스 읽기) |
| `ProjectMapBuild.RunTests` | `PROJECT_MAP_PREPARED hash=3efe1faf…`, `PROJECT_MAP_TESTS_OK` |
| `UiTextureCompression.Apply` | 대상 15장 모두 BC7, 합계 9.17MB |

### 빌드

- `PrototypeBuild.BuildHybrid` 종료 0, `BUILD_RAW_MAPS_EXCLUDED 4`(지도 2개와 .meta), 빌드 뒤 원본 지도 복원을 확인했다.
- Unity 보고: 전체 108.7MB, Textures 11.8MB, Other Assets 23.6MB.

| 파일 | r52 출하 | 이번 |
|---|---:|---:|
| 실행 폴더 | 약 152MB | 약 110MB |
| `resources.assets` | 54,382,288 | 26,571,336 |
| `resources.assets.resS` | 약 24.7MB | 9,616,672 |

### 실행본 검사 (통합 실행본, 1600×900 창)

| 검사 | 결과 |
|---|---|
| `-drivingSettingsCheck` (싱글) | `DRIVING_SETTINGS_PLAYER_OK checks=23`. 감각 탭 저장(시뮬·40%·0%), 기본값(캐주얼·100%), 취소 유지를 포함한다. |
| `-drivingSettingsCheck -drivingSettingsPvp` | `DRIVING_SETTINGS_PLAYER_OK checks=22`. PvP 택시는 캐주얼 지정을 무시한다. |
| `-brightUiCheck` | `BRIGHT_UI_RESULT PASS checks=102`(세 해상도·싱글/PvP 화면 배치). |
| `-mapAudit -mapAuditLayers reviewed:r47:P51` | gzip 지도로 정상 시작. P51 층 구성 source 3·generated 2·facility 706으로 r52와 같다. |
| `-drivingSettingsCheck -drivingSettingsCapture` | 화면 4장 저장([감각 탭](../ui/2026-11-23-steering-feel/settings-feel-tab.png)). 다만 21건 뒤 기존 검사 "healthy durability51 keeps repair dot hidden"에서 멈췄다. 이 검사는 내구도 100에서 시작한다고 가정한다. 앞선 통과 실행이 싱글 저장의 내구도를 23까지 낮춘 채 남겨서(캡처 화면의 "차량 내구도 23"), 이 PC의 저장 상태 때문에 실패한 것이다. 이번 변경과 무관하다. |
| 화질 | [승객 초상화 BC7 1920px](../ui/2026-11-23-steering-feel/bc7-portrait-1920.png), [시설 지도 1920px](../ui/2026-11-23-steering-feel/bc7-facilities-1920.png). 시설 팝업의 배경 사진은 이번 자동 화면에 없어 직접 확인이 필요하다. |

주의: 위 실행본 검사는 이 PC의 실제 싱글 저장·설정 슬롯(`AppData/LocalLow/Taxi Prototype/…`)에서 돌았다. 이전 작업들과 같은 방식이며, 검사 중 운행·손상이 저장에 남는다.

## 남은 것 · 수동 확인

- **사람 체감은 확인하지 않았다.** 위 수치는 Editor 평지에서 속도를 고정한 자동 물리다. 키보드·패드로 캐주얼과 시뮬을 비교하고, 연출 4종이 멀미나 거슬림 없이 속도감을 주는지 봐야 한다.
- 50km/h에서는 캐주얼과 시뮬의 차이가 없다. 체감상 50km/h 차선 변경이 느리다면, 바퀴각보다 입력 반응(`response`)이나 타이어 횡마찰 쪽을 다시 봐야 한다.
- 시설 사진은 가로 2px, 일부는 세로 1px를 잘랐다. BC7 화질은 게임 화면에서 확인해야 한다.
- 다른 PC에서 LAN v24 두 실행본으로 대전하는지 확인해야 한다.
- 글꼴 3개(각 5.9MB)는 자르지 않았다. 한글 누락 위험 때문이다.

## 재현

```powershell
# Editor 검사 (빌드 전에도 가능)
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod CurveSteeringChecks.Run      # STEERING_FEEL 줄이 위 표
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod UiTextureCompression.Report  # 이미지별 저장 형식·크기
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod PrototypeBuild.BuildHybrid
# 실행본
.\builds\EungyeTaxi\EungyeTaxi.exe -drivingSettingsCheck -drivingSettingsCapture -drivingSettingsOutput artifacts/steering/ds-single
.\builds\EungyeTaxi\EungyeTaxi.exe -drivingSettingsCheck -drivingSettingsPvp -drivingSettingsOutput artifacts/steering/ds-pvp
```

**키보드·패드 비교(사람 확인용)**

1. `builds/EungyeTaxi/EungyeTaxi.exe` → 은계 싱글로 영업을 시작한다.
2. Esc → 설정 → **감각** 탭에서 "시뮬"을 고르고 적용한다.
3. 은계남로 같은 직선에서 50km/h와 60km/h로 각각 한 차로를 옮긴다(A/D 또는 패드 왼쪽 스틱). 같은 구간을 3번씩 한다.
4. 감각 탭에서 "캐주얼"로 바꾸고 같은 구간을 다시 3번씩 한다. 60km/h에서 덜 둔한지, 옮긴 뒤 직진이 흔들리지 않는지 본다.
5. 패드가 있으면 3~4를 패드로 반복한다. 키보드(입력 0/100)와 패드(아날로그)의 차이가 조향 수치 문제인지 입력 방식 문제인지 가른다.
6. 감각 탭 막대를 하나씩 0%로 내려 연출 4종의 효과를 따로 본다. 1인칭(C)과 3인칭 모두 본다. V는 조향 방향 미리 보기를 켜고 끈다.
7. PvP(봇 3명)에서는 감각 탭 선택과 관계없이 시뮬 조향인지 본다.

기록할 것: 프리셋별 "둔하다/적당/과하다", 멀미 여부, 연출별로 남길 강도.
