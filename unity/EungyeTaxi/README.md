# 은계 택시 시뮬레이터 · Unity 프로젝트

Unity Hub에서 **이 폴더(`unity/EungyeTaxi`)**를 Unity **2022.3.62f2**로 엽니다. 이전 경로로 등록된 프로젝트는 Hub 목록에서 새 폴더를 다시 추가합니다.

- **지도 편집:** Play 모드를 끄고 **Taxi → 프로젝트 지도 3D 편집 → 프로젝트 지도 열기**. Scene에서 선택 후 W 이동/E 회전/R 크기/F 초점을 사용합니다. [편집·저장·다른 PC 사용법](../../docs/unity-map-editor-guide.md).
- **게임 빌드:** **Taxi → Build Eungye Hybrid**. `builds/EungyeTaxi/EungyeTaxi.exe`가 생성됩니다. 일반 Build Windows Prototype 메뉴도 같은 은계 빌드를 사용합니다.
- **게임 실행:** 저장소 루트 `Play-Eungye.cmd` 또는 `Play-Taxi.cmd`. Unity Play는 `Assets/Scenes/FirstDrive.unity`를 열어 실행합니다.
- **운전:** W/S 가감속, A/D 조향, Space 제동, C 시점, H 조향 보조, F 승하차, F7 시설 이용, M 전체지도, Esc 메뉴, F11 전체화면. 자세한 주행/HUD 규칙은 [UI 기획](../../docs/content-design/05-interface-and-navigation.md)을 참고합니다.
- **지도 저장:** `config/maps/project/eungye.project.json`. 커밋·푸시→다른 PC pull→Build Eungye Hybrid로 반영합니다.
- **캠페인 저장:** `%USERPROFILE%/AppData/LocalLow/Taxi Prototype/은계 택시 시뮬레이터/`. 이전 제품 폴더의 같은 캠페인 파일과 백업은 새 폴더에 저장이 없을 때 1회 복사합니다. 원본 저장을 지우거나 새 은계 저장을 덮어쓰지 않습니다.

은계 국도42 차로 시험과 가상 연습장은 개발 검사용이며 기본 은계 영업과 구분합니다. 현재 범위·검증·남은 작업은 [최신 인계](../../docs/work-history/LATEST.md)를 따릅니다.

## 한국 도로 규칙 시험장

별도 가상 시험은 Play-RoadRulesTrial.cmd와 PrototypeBuild.BuildAssisted를 사용합니다. 실제 은계 영업은 Play-Eungye.cmd로 실행합니다.

## 기간 목표와 하루 정산

기본 은계 영업은 3일 캠페인입니다. 하루 정산을 저장하고 다음 날로 이어갑니다. 현재 목표·시간·정산 규칙은 통합 기획과 최신 검증 기록을 따릅니다.
