# 실차 녹음으로 교체한 택시 효과음

2026-11-20 · 은계 r50 · 자체 도시 PvP A2/LAN v23

주행음을 더 잘 들을 수 있게 하고, 제동할 때는 주행음이 낮아지며, 깜빡이가 점멸과 함께 들리도록 해 달라는 요청이다. 유튜브에서 승용차 실내 정속·저속 주행, 방향지시등, 길이가 다른 경적의 영상 설명을 확인했다. [실내 주행](https://www.youtube.com/watch?v=KpxLJO1IBbI) · [저속 주행](https://www.youtube.com/watch?v=-g6kXOYdo2U) · [방향지시등](https://www.youtube.com/watch?v=EGmmEhMMWUc) · [경적](https://www.youtube.com/watch?v=WwlKIpjJf80).

## 게임에 반영한 소리

- Freesound에서 CC0로 공개한 실차 녹음 5개에서 게임용 모노 PCM 6개를 편집했다. 가속 녹음의 회전수가 비교적 일정한 구간을 골라 2.75초 엔진 루프로 만들었다. 실내 주행의 노면음은 10초, 실내 빗소리는 13초로 편집했고, 세 루프의 경계에 크로스페이드를 넣었다. 음원은 `unity/EungyeTaxi/Assets/Resources/Audio/Taxi/`에 있다. 제작자와 원본 링크는 실행본의 `ThirdPartyNotices/vehicle-recordings.txt`에도 들어간다. [원본과 이용 조건](../../licenses/vehicle-recordings.txt).
- `VehicleAudio.cs`에서 엔진 음높이와 음량은 차속·페달 부하를 반영한다. 노면음은 회전음을 따라 조옮김하지 않고 차속에 비례해 더해진다. 감속해서 멈추면 공회전만 남는다. 엔진 단독 RMS는 정차 −23.78 dBFS, 시속 45km 부하 25%에서 −17.62 dBFS다.
- 깜빡이 점등과 소등은 같은 녹음의 연속된 서로 다른 클릭을 74ms씩 잘라 쓴다. 좌우와 비상등은 기존 램프 위상을 공유한다. 경적은 한 번 누르면 길이가 다른 두 번의 경적이 나고, 빗소리 크기는 기존 강수 강도를 따른다. 주행음 스위치 하나로 엔진과 노면음을 함께 제어하고, 깜빡이·경적·비는 개별 제어한다.
- [12초 미리듣기](../content-design/audio/taxi-cabin-recordings.wav)는 배포 PCM과 게임의 음량·음높이 계산으로 렌더링했다. 출발과 가속, 4.55초부터 깜빡이, 6~9초 감속, 9.3초 경적, 10초부터 비가 차례로 들린다. 기기별 출력 차이는 포함하지 않는다.

## 재현

은계 싱글 운행에서 가속 후 페달을 떼고 정차한다. Q/E로 좌우 방향지시등, G로 비상등, Y로 경적을 조작한다. Esc → 설정 → 소리에서 각 효과를 바꿀 수 있고, `설정 관리.cmd`의 차량 효과음에서도 기본 동작을 지정할 수 있다.

## 검증

- 소스 `c2668c4`에 상점·부품과 벽 메모·학생 재회를 포함한 당시 main을 통합했다. Unity 2022.3.62f2에서 `CabinSoundChecks.VerifyAndBuild`로 클립 6개·지점 39개, 구형 설정 이관과 주행음 RMS를 확인하고 `PrototypeBuild.BuildHybrid`로 Windows 빌드에 성공했다. 종료 0. `tools/check_workspace.ps1 -Strict` 필수 지도 입력 문제 0.
- `node --test tests/content-editor/*.test.mjs tests/content-manifest.test.mjs` 112/112 통과. 샌드박스에서 막힌 로컬 서버·임시 Git 저장소 검사는 해당 접근을 허용한 환경에서 재실행했다.
- Windows 배포본에 `-cabinControlsCheck -taxiAudioCheck -shiftCaptureOutput <폴더> -logFile <로그>`를 전달해 싱글 위치·속도 제어 35건 통과. 엔진과 노면음 공유 음소거, 좌우·비상등 점멸, 감속을 확인했다. 시속 45km에서 정지까지 노면 소스 실측값은 `cruise=0.2607954 rest=0 pitch=1 speed=0`였다. 검사에서는 주차 위치의 차체 움직임이 측정을 흐리지 않도록 정지 구간의 물리 위치를 고정했다. 실제 수동 주행, 스피커·헤드폰 실청, 전체 캠페인 실기는 미검증이다.
- 같은 실행본에서 `tools/test_pvp_condition.ps1`의 별도 PvP 위치 제어 24건 통과. 검사 로그와 미리듣기 중간 자료는 `artifacts/cabin-recordings/`에 있다.
- 통합 및 PvP 배포 매니페스트의 `-VerifyOnly`가 각각 출하 파일 145개·150,793,717바이트의 SHA256 검증을 통과했다.
