# 다른 PC에서 바로 실행하기

Windows 64비트 PC에서 main을 clone하거나 로컬 변경을 보존한 뒤 `git pull --ff-only`한다. **Unity 설치·지도 ZIP 해제 없이 `builds/EungyeTaxi/EungyeTaxi.exe`를 바로 실행**한다. 제품명은 택시 시뮬레이션이다.

실행 폴더의 `EungyeTaxi_Data`, `MonoBleedingEdge`, `UnityPlayer.dll` 등을 함께 유지한다. exe만 이동하면 실행되지 않는다. GitHub Download ZIP을 받았다면 저장소 전체를 압축 해제한다. Git LFS는 사용하지 않는다.

## 현재 모드와 조작

| 항목 | 동작 |
|---|---|
| 은계 영업 | 3일×예정20분 캠페인, 초과 영업 가능. 고정3만원 목표가 아니라 일별/누적 순수익 비교 |
| PvP | 현재 A2 도시, 봇3 또는 LAN 최대4명. 닉네임·경기시간 입력. 양쪽 PC 모두 최신 실행본 사용 |
| 운전 | ↑ 가속, ↓ 감속/정차 후 후진, ←/→ 조향, Space 제동. WASD도 유지 |
| 호출/승하차 | M 지도에서 선택·수락, 승차지에 정차 후 F. 탑승 후 하차 목표로 전환 |
| 보조/시점 | H 조향만 보조, Q/E 방향지시등, G 비상등, C 시점/B 후방/Z·X 좌우 |
| 시설/기록 | 현장 안전 정차 후 F7, F8 기록, 도움말 버튼 |
| 승객 대화 | 클릭 또는 Y/I/O/U, 운행 중 답변 가능. 적재/정리/재고정·용무는 안전 정차 중 진행 |
| 지도/메뉴 | M·+/−·휠/드래그. 싱글은 일시정지 조건을 따르며 PvP는 지도를 열어도 공통 경기가 계속 진행 |

신호 요약은 변경까지 남은 시간, 네비는 승차/경유/하차까지 잔여 거리와 다음 회전을 구분한다. 현재 대사·효과는 [대화 스크립트](../docs/content-design/07-passenger-dialogue.md)를 따른다. 실제 음악·에어컨·창문 조작은 보류다.

## 파일 검사·개발·저장 이전

저장소 루트에서 `pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly`로 실행 폴더 전체를 검사한다. 파일 수·크기·SHA256과 실제 빌드 소스는 `EungyeTaxi-build-manifest.json`이 기준이다. 임의 실행 ZIP을 실행 폴더 안에 더 넣으면 정식 파일 수 검사에 실패할 수 있다.

현재 구현/검증 범위, 새 PC 개발 준비와 개인 캠페인·편집 지도 이전은 [현재 구현·PC 인계](../docs/CURRENT-STATE.md)를 따른다. 개발 프로젝트는 `unity/EungyeTaxi`, Unity2022.3.62f2, 빌드는 `PrototypeBuild.BuildHybrid`다. 원본 지도 재수입 때만 `maps/`의 입력 ZIP이 필요하다.

캠페인 저장과 PvP 닉네임은 Git으로 동기화되지 않는다. 기존 저장 폴더명은 제품명 변경 후에도 호환을 위해 유지한다. 새 PC 하드웨어/다른PC LAN·장시간 플레이는 해당 PC에서 확인해야 한다. 과거 배포·시험 수치는 [작업 이력](../docs/work-history/LATEST.md)에서 날짜별로 확인한다.
