# 2026-11-12 주행 HUD 기록·도움말 단축키

## 요청과 구현

- `unity/EungyeTaxi/Assets/Scripts/CockpitUI.cs`에서 공통 주행 HUD 버튼을 `기록 (F8)`, `도움말 (F1)`로 표시한다.
- `DriveHUD.cs`에서 F1과 도움말 버튼이 같은 패널을 열고 닫는다. 지도·현장 시설·기록 등 주행 HUD를 가리는 화면에서는 F1 입력으로 도움말 상태가 바뀌지 않는다. `BusinessHUD.cs`의 조작 안내도 F1로 맞췄다.
- 조작 기획은 `docs/content-design/05-interface-and-navigation.md`, 작업 상태는 체크리스트와 `LATEST.md`에 반영했다.

## 생활거점 F 통합 검토

- `PassengerJobs.Update`는 F로 승차·경유·하차·깨우기를 처리한다. `ServiceStops.Update`는 F7로 현장 이용 창 열기·닫기와 대기 취소를 처리한다. HUD의 생활거점 버튼은 시설을 찾아 길안내를 받는 별도 진입점이다.
- 싱글에서는 승차 장소로 이동하는 중에도 지정 위치에서 주유·세차를 할 수 있다. 두 처리기에 F를 각각 연결하면 입력 한 번으로 두 처리가 시도될 수 있으며, PvP LAN에서는 별도 요청 두 개가 호스트로 전달될 수 있다.
- 통합 시 현재 실행 가능한 승객 동작에 우선권을 주고, 현장 서비스 안내가 표시된 정차 상태에서만 시설 메뉴를 여는 단일 분기를 권장한다. 이용 확정은 메뉴에서 한다. 이번 작업의 시설 조작은 F7이다.

## 검증과 재현

| 검사 | 결과 |
|---|---|
| 주행 HUD 소스 대조 | 변경 전 예상 항목 4개 실패, 변경 후 4개 통과. 두 버튼 표기, F1 입력 경로와 조작 안내 확인. |
| `pwsh -NoProfile -File tools/check_workspace.ps1 -Strict` | 종료 0, 필수 입력 문제 0. 정식 은계 r16, 선택 재수입 입력 2개 부재. |
| Unity 2022.3.62f2 `PrototypeBuild.BuildHybrid` | 종료 0, `Build Finished, Result: Success.`. 로그 `artifacts/hud-shortcuts-build-native.log`. 최초 제한 환경 실행은 라이선스 IPC 연결 시간 초과로 끝나 로컬 권한 실행에서 재시도했다. |
| Windows Player 실제 키 입력 | 새 통합 실행본의 복사본에 `-singlePlayer -contentTest -screen-fullscreen 0 -screen-width 1280 -screen-height 720`을 전달했다. 영업 시작 후 정차 상태에서 두 버튼 표기, F1 도움말 열기·닫기, F8 기록 열기·닫기, F7 시설 창 열기·닫기를 확인했다. 시설 창에서 F1을 누른 뒤 닫아도 도움말이 열리지 않았다. |
| 배포 파일 검증 | 소스 `dca65c7`에서 만든 `builds/EungyeTaxi/`의 `Assembly-CSharp.dll`, `boot.config`, `distribution/EungyeTaxi-build-manifest.json`을 갱신했다. `tools/update_distribution_manifest.ps1 -VerifyOnly` 및 `tools/update_pvp_distribution_manifest.ps1 -VerifyOnly` 각각 종료 0, 143파일·131,489,978바이트. 테스트 복사본의 실행 파일과 변경된 배포 파일 2개의 해시도 일치했다. |

검증은 은계 1일차 시작 위치의 UI와 실제 키 입력 범위다. 수동 운전, 자동 물리 주행, 전체 캠페인 및 PvP/LAN 재검증은 이번에 실행하지 않았다.
