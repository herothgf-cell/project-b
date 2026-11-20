# 차속에 따른 주행음과 방향지시등 릴레이

2026-11-20 · 은계 r49 / 자체 도시 PvP A2. 운전 중 엔진 소리가 잘 들리지 않고 브레이크는 공기가 빠지는 느낌이라는 피드백을 받았다. 사용자의 후속 지시에 따라 독립 제동음을 제거하고 감속은 주행음으로 표현했다. 방향지시등과 비상등에는 점멸에 맞춘 소리를 추가했다.

## 변경 내용

- `unity/EungyeTaxi/Assets/Scripts/VehicleAudio.cs`에서 주행음에 엔진 회전의 중저역 배음을 더했다. 차속이 올라갈수록 음높이와 음량이 오르고 속도가 줄면 공회전 쪽으로 내려간다. 가속 페달의 부하는 작은 가산값으로 처리하고, 프레임 간 음량·피치 변화는 65ms 시정수로 완화한다. 브레이크 페달용 클립과 재생 소스를 제거했다.
- Q/E 방향지시등, G 비상등 및 승하차 자동 비상등은 기존 `TaxiSignals.Lit`의 실제 점멸 상태를 공유한다. 켜짐·꺼짐에 서로 다른 짧은 릴레이 클립을 로컬 차량 안에서 한 번씩 재생한다. 메뉴·일시정지·깜빡이 음소거에서는 재생을 멈춘다.
- DrivingSettings·DrivingSettingsPanel과 `설정 관리.cmd`의 차량 효과음을 주행음·깜빡이·경적·빗소리로 맞췄다. 기존 ‘브레이크음 끔’은 깜빡이의 기본 켜짐과 독립이다. 기존 프로필은 원본 해시 검증을 거쳐 이관하며 다른 소리 선호는 보존한다. 관리 도구와 C# 프로필 검증기, 양쪽 카탈로그와 기본 프로필을 갱신했다. 호스트 스냅샷 키가 변경되어 LAN 버전은 v23이다.
- 검사는 `CabinSoundChecks.cs`, `ShiftDevEndProbe.cs`와 `tests/content-editor/driving-settings.test.mjs`에 추가했다.

## 소리 비교

[8초 미리듣기](../content-design/audio/taxi-drive-indicators.wav). 공회전에서 45km/h까지 가속하고 약 3.2초에 깜빡이를 두 번 울린 뒤 5.25초부터 감속해 멈춘다. Unity에서 생성한 전후 클립의 PCM과 속도별 레벨·피치를 같은 시간표로 렌더링한 예시다. 실제 장치의 녹음과 별도로 측정했다.

| 45km/h 정속, 부하 25% | 이전 | 변경 후 |
|---|---:|---:|
| 주행음 RMS | −24.80 dBFS | −17.31 dBFS |
| 120~700Hz 대역 RMS | −36.22 dBFS | −18.29 dBFS |

같은 구간의 주행음은 약 7.5dB 상승했다. 변경 후 예시의 최대 피크는 −3.12dBFS. 경적·비의 변경 전후 클립은 PCM 바이트가 일치했다.

## 검증과 재현

- `node --test tests/content-editor/*.test.mjs tests/content-manifest.test.mjs` 112/112 통과. `node tools/content-editor/choice-catalog.mjs . --verify` 출력은 설정 1,666개와 기본 프로필 186,706바이트. `tools/test_content_tuning.ps1` 누적 1,471건과 C#/Node 이관 해시 대조를 통과했다.
- 분리한 프로젝트 사본에서 Unity 2022.3.62f2의 `CabinSoundChecks.Run`과 `PrototypeBuild.BuildHybrid`를 실행했다. 지점 39개, 클립 5개, 이전 브레이크 음소거 프로필의 이관을 검사했다. 필수 입력 `tools/check_workspace.ps1 -Strict` 문제 0.
- 게임에서 은계 영업을 시작해 직선에서 가속 후 페달을 떼고 감속한다. Q와 E로 좌우 깜빡이를 각각 켜고 G로 비상등을 켠다. Esc → 설정 → 소리에서 ‘깜빡이’를 끄면 등은 계속 깜박인다. 콘텐츠 프로필은 차량 → 차량 효과음 → 저장 → 게임에 적용 후 새 캠페인/새 방에서 확인한다.
- 측정 파일과 검사 로그는 `artifacts/taxi-audio-revision/`에 있다. 스피커·헤드폰 실청, 사람의 수동 운전, 전체 캠페인과 두 PC LAN 실기는 미검증이다.

분리 빌드의 Player에서 임시 경력으로 `-cabinControlsCheck -taxiAudioCheck`를 실행해 `CABIN_PLAYER_OK checks=33`, 종료 0을 확인했다. 제동 소스 제거, 45km/h에서 정지 시 음량·피치 감소, 좌·우·비상등의 밝고 어두운 위상에 맞는 서로 다른 클립, 음소거를 검사했다. 차량 속도와 위치를 지정한 검사이며 실제 페달 물리 주행 측정과 구분한다.

## 기본 실행본 확인

첫 출발 차고 수정과 소리 변경을 함께 포함한 배포 소스는 `491a7e8`이다. `builds/EungyeTaxi/EungyeTaxi.exe -cabinControlsCheck -taxiAudioCheck -shiftCaptureOutput <출력 폴더> -logFile <로그 파일>`로 실제 기본 실행 파일을 재검사했다. 좌·우·비상등 세 모드, 음소거, 감속 시 소리 변화 등 총 33건 통과, 종료 0. 소리 소스 커밋은 `186a256`이다. PvP 별도 위치 제어 검사 `tools/test_pvp_condition.ps1`는 24건 및 종료 0이었다. 기본 실행본의 출하 파일 143개(145,396,367바이트)를 검증 사본과 일치시켰다.

배포 커밋 `f17e8a8`의 기본 통합 및 PvP 배포 매니페스트 `-VerifyOnly` 모두 종료 0. 출하 143개와 선택적 로컬 사용자 프로필 1개를 검증했다. [통합 빌드 기록](2026-11-20-first-departure-taxi.md).
