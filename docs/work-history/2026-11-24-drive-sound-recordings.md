# 사용자가 고른 녹음으로 바꾼 실내 주행음·창문 바람·급제동음

2026-11-24 · 은계 싱글 r53 · 자체 도시 PvP A2 / LAN v25(지도·LAN 프로토콜 변경 없음)

## 요구사항 (사용자 2026-11-24)

[음원 후보 9건](2026-11-24-test-feedback-round2.md) 중에서 다음을 골랐다.

- 기본 주행음: "Highway drive interior"와 "Interior of a car driving a highway".
- 창문 내린 주행: 위 두 음원을 편집해서 만든다.
- 브레이크: 급브레이크가 아니면 따로 소리를 내지 않고 기본음이 속도를 따라 변하는 것으로만 표현한다. 급브레이크 때만 "R04-12 Car Comes to a Stop"을 쓴다. 이 음원은 실외에서 듣는 느낌이라 실내에서 듣는 소리로 다듬는다.
- 스키드는 제외한다.

## 구현

### 음원과 편집 (`Assets/Editor/DriveSoundAuthoring.cs`, `Assets/Resources/Audio/Taxi/`)

- 원본 파일은 Freesound 로그인이 있어야 받을 수 있어, 로그인 없이 제공되는 고음질 MP3 미리듣기 파일을 받았다(세 개, CC0). 원본(WAV·FLAC)보다 음질이 낮다.
- Unity 에디터에서 디코딩해 구간별 음량·밝기를 재고 구간을 골랐다. 편집 도구와 구간 값은 `DriveSoundAuthoring.cs`에 남겼다. 원본 MP3는 저장소에 넣지 않았다(`artifacts/audio-src/`, Git 제외).

| 파일 | 출처 구간 | 처리 |
|---|---|---|
| `Road.wav` (고속 실내음, 12초 루프) | Highway drive interior 8~22초 | 35Hz 아래 제거, 느린 음량 고르기, 2초 크로스페이드 |
| `RoadSlow.wav` (시내 실내음, 12초 루프) | Interior of a car driving a highway 510~524초(낮은 속도로 일정하게 달리는 구간) | 위와 같음 |
| `WindowWind.wav` (창문 바람, 10초 루프) | 두 녹음의 빠른 구간(24초~, 664초~)을 섞음 | 260Hz 아래 제거, 2.2kHz 위 +9dB, 느린 출렁임, 2초 크로스페이드 |
| `HardBrake.wav` (급제동, 2초 단발) | R04-12의 8.7~10.7초(끽 소리 구간) | 500Hz 아래 제거, 3.2kHz·4.2kHz 저역 통과 두 번(실내에서 듣는 둔한 소리), 0.5초 페이드아웃 |

- 기존 `Road.wav`의 출처(Car Driving Interior by squareal)는 더 쓰지 않는다. `licenses/vehicle-recordings.txt`를 고쳤다.

### 재생 (`VehicleAudio.cs`, `DriveFeelAudio.cs`, `TaxiController.cs`)

- 실내음 전체 크기는 예전과 같은 식(0.62·v²/(v²+90))으로 차속을 따른다. 25km/h 아래는 시내 녹음, 70km/h 위는 고속도로 녹음이고 그 사이는 섞는다. 음높이는 바꾸지 않는다.
- 창문(K)을 내리면 바람 소리가 차속에 따라 더해진다(정지 0, 80km/h에서 약 0.38).
- 급제동음: Space 긴급제동 또는 감속 6.5m/s² 이상이고 속도가 14km/h(4m/s)를 넘을 때 한 번 난다. 다시 나려면 2.5초가 지나야 한다. 멈추면 빠르게 잦아든다. 감각 탭의 "추가 주행음" 세기를 따르고 은계 싱글에서만 난다.
- 타이어 미끄러짐 소리와 고속 바람 층(기존 녹음의 재생 속도를 바꿔 만든 것)을 없앴다. 엔진음(6단 변속 모델)은 그대로다.
- 설정 문구: "추가 주행음 · 가속 부하·급제동음(싱글)".

미리듣기(게임과 같은 계산식으로 렌더링): [32초](../content-design/audio/taxi-drive-cabin-2026-11-24.wav). 0~14초 출발·가속(80km/h까지), 14~18초 정속, 18~23초 창문 내림, 23~26초 창문 올림, 26초부터 급제동.

## 변경 파일

- `unity/EungyeTaxi/Assets/Scripts/`: `VehicleAudio.cs`, `DriveFeelAudio.cs`, `TaxiController.cs`(긴급제동 상태 읽기 전용 속성), `DrivingSettingsPanel.cs`(문구)
- `unity/EungyeTaxi/Assets/Editor/`: `DriveSoundAuthoring.cs`(신규), `CabinSoundChecks.cs`, `FeelSoundChecks.cs`
- `unity/EungyeTaxi/Assets/Resources/Audio/Taxi/`: `Road.wav`(교체), `RoadSlow.wav`, `WindowWind.wav`, `HardBrake.wav`(신규)
- `licenses/vehicle-recordings.txt`, `docs/content-design/01-driving-and-traffic.md`, `docs/content-design/audio/taxi-drive-cabin-2026-11-24.wav`, `docs/work-history/LATEST.md`, 이 기록

## 검증 (Unity 2022.3.62f2, 이 PC)

| 구분 | 명령 | 결과 |
|---|---|---|
| Editor | `CabinSoundChecks.Run` | `CABIN_SOUND_CHECKS_OK places=39 clips=9`(새 클립 3개 포함, 모노·진폭 확인) |
| Editor | `FeelSoundChecks.Run` | `FEEL_SOUND_OK checks=13`(실내음 혼합, 창문 바람, 급제동 조건) |
| Editor | `DriveSoundAuthoring.Build` / `Preview` | 클립 4개와 미리듣기 작성(로그의 길이·RMS·피크 확인) |
| 실행본(위치 제어) | `-cabinControlsCheck -taxiAudioCheck` | `CABIN_PLAYER_OK checks=35`. `TAXI_ROAD_MIX cruise=0.214 rest=0 pitch=1`(45km/h에서 고속 실내음 층의 크기, 정지하면 멎음), `TAXI_ENGINE_MIX fastPitch=1.032 fastLevel=0.166 restPitch=0.899 restLevel=0.126` |
| 빌드 | `PrototypeBuild.BuildHybrid` | `PROTOTYPE_BUILD_OK`, 종료 0(아래 "배포 실행본" 참고) |

미검증:

- **소리를 사람 귀로 듣지 못했다.** 구간 선택은 음량·밝기 수치로만 했다. 루프 이음, 창문 바람의 느낌, 급제동음이 실내 소리로 들리는지는 미리듣기 파일과 실제 플레이로 확인이 필요하다.
- 급제동 기준(6.5m/s²)이 보통 제동에서 걸리지 않는지는 실제 주행으로 재지 않았다.
- 수동 플레이, 전체 캠페인, PvP.

## 배포 실행본

**이 작업에서는 `builds/EungyeTaxi/`와 배포 manifest를 커밋하지 않았다.** 같은 작업 폴더에서 다른 작업(제한속도·사고 과실·생활시설 카드, 콘텐츠 설정)의 미커밋 변경이 진행 중이었고, 검사용으로 만든 실행본에는 그 변경이 함께 컴파일됐다. 저장소의 배포 실행본은 텍스트 줄이기 2차(`91b22fbf`) 상태다. 그 작업이 끝난 뒤 한 번 재빌드하고 manifest를 갱신하면 이 주행음이 배포본에 들어간다. 이 PC의 `builds/EungyeTaxi/EungyeTaxi.exe`는 지금 새 주행음이 들어 있는 상태다.

## 재현

```
# 음원을 다시 자를 때: MP3 세 개를 unity/EungyeTaxi/Assets/TempAudioSrc/에 두고
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod DriveSoundAuthoring.Build -logFile <로그>
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod DriveSoundAuthoring.Preview -logFile <로그>
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod CabinSoundChecks.Run -logFile <로그>
```

구간은 `-highwayFrom`, `-slowFrom`, `-windFromA`, `-windFromB`, `-brakeFrom`, `-brakeSeconds`(초)로 바꿀 수 있다. 작업 뒤 `TempAudioSrc` 폴더는 지운다.
