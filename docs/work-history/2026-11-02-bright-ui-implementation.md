# 2026-11-02 밝은 캐주얼 UI 적용

## 요구사항과 범위

사용자는 밝은 캐주얼 UI 재시안을 기준으로 실제 작업하고 main에 푸시하도록 요청했다. 은계 싱글과 통합 PVP의 UI를 함께 적용하며 맵·도로·건물·차량 모델·주행 물리·경제/승객 규칙은 유지한다. 새 Canvas나 신규 게임 시스템 없이 기존 IMGUI의 패널·버튼·색상·아이콘을 조정한다.

## 실제 변경

- `CasualUI.cs`: 공통 색상, 흰색 둥근 카드·작은 그림자, 파란 보조 버튼과 초록 주요 버튼. 작은 9분할 텍스처를 캐시한다.
- `KoreanText.cs`: 공유 스킨의 진한 글자·밝은 카드·입력 필드·버튼 상태를 통일한다.
- 주행 HUD·미니맵/전체지도·호출 선택/수락·승객 대화·시설·내구도 경고·운송 정산·영업 준비/하루 마감·모드 메뉴/PVP 설정·결과에 연결한다.
- 미니맵은 기존 `LightTile` 표시를 사용한다. 원본 지도 데이터, 경로 계산, 승차 접근/운송 선 구분은 유지한다.
- 공유 스킨을 쓰는 도움말·연습·지도 편집 화면의 대비도 함께 조정한다. 편집 기능과 지도 저장 로직은 변경하지 않는다.
- `BrightUIThemeTests.cs`는 실제 카드 픽셀·캐시·글자 대비를 검사한다. `BrightUIProbe.cs`는 명시적 검사 인자로만 실행하며 기존 `UnifiedModeProbe` 임시 저장 경로를 사용한다.

## 수행한 검증과 실제 결과

- 시작 `git status --short`로 기존 미추적 Unity 폴더·문서·지도 편집 설정과 다른 작업의 마인드맵 변경을 확인하고 보존했다. 별도 작업트리 없이 사용자가 지정한 main 작업/배포를 진행한다.
- `pwsh -File tools/check_workspace.ps1 -Strict`: 종료0, 필수 입력 issues0, 프로젝트 지도 해시 `35d1bc43d92c996d431acd0464bfc7a175b926cd1e35adb995e99dd7ab91cbac`. 선택 가져오기2맵 미배치 유지.
- `pwsh -File tools/test_prototype.ps1`: 종료0, 28개 모델 검사 묶음 통과.
- 최초 샌드박스 Unity는 라이선스 IPC 연결 시간 초과·종료199. 정상 사용자 환경의 첫 실행은 진행했지만 GUI EXE 직접 호출이 즉시 반환해 중복 실행을 시도했고, 두 번째 프로세스는 프로젝트 점유 오류·종료1073741845. 두 실패는 검증 완료 근거에서 제외했다. 이후 한 번씩 실행·종료를 확인한다.
- 변경 전 `BrightUIThemeTests.Run`: 어두운 카드 중심색으로 의도한 실패·종료1. 변경 후 `theme-green.log`: 종료0, 밝은 본문·투명 모서리·동일 캐시·본문/상태/주요버튼 4.5:1 대비 검사 통과.
- 빌드·실제 Player·최종 배포/푸시 결과는 아래 후속 항목에 기록한다. 진행 중 결과를 완료로 표시하지 않는다.

## 실행·재현

Unity 로컬 환경은 `C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe`다. 저장소 명령은 `-batchmode -nographics -quit -buildTarget Win64 -projectPath unity/EungyeTaxi -executeMethod BrightUIThemeTests.Build`이며 내부에서 테마/프로젝트 지도/통합PVP 검사 후 `PrototypeBuild.BuildHybrid`를 호출한다.

실제 UI 확인은 `builds/EungyeTaxi/EungyeTaxi.exe -brightUiCheck -brightUiOutput <출력폴더> -screen-fullscreen 0 -screen-width 1600 -screen-height 900 -logFile <로그>`를 사용한다. 프로그램이 메뉴→싱글 준비/주행/지도/승객/정산/마감→PVP 설정/4인 주행/지도/결과를 검사한다. 승하차는 위치를 제어한 fixture로서 자동 물리 주행·수동 플레이·전체 캠페인 검증과 다르다.

## 남은 검증과 한계

사람의 장시간 운전·20분/3일 경제 체감·다른PC 검증을 이번 UI 자동 검사로 대체하지 않는다. 생성 시안의 도시 일러스트와 달리 실제 월드 아트는 기존 것을 유지한다. 변경 파일은 최종 커밋의 실제 목록과 함께 확인한다.

## 실제 화면 검토와 문구 후속

- 첫 전체 검사/빌드 `build.log` 종료0: 프로젝트 지도/편집/저장 검사와 `PvpFlowUnifiedChecks.Run`의 모드·교통·도로/화살표·4인 호출·실시간 지도·핀 검사가 통과했다.
- 리뷰에서 미러 제목과 편집기 어두운 하위 패널의 대비 저하를 확인해 수정했다. 실제 캡처에서 추가 확인한 어두운 편집 지도 위 글자도 흰색으로 분리했다. 체크박스 상태·경고 텍스트 대비와 PVP 재경기 강조를 보완했다.
- `build-final.log` 및 `build-release.log` 종료0. 첫 실제 렌더 Player `player.log` 종료0 / `BRIGHT_UI_RESULT PASS checks=35`. 싱글/PVP의 1280·1920 HUD/지도, 운전석/편집기/승객 요청/정산/마감/모드/결과를 확인했다. 당시 사용자 저장 JSON/백업14파일 크기 아닌 SHA256 및 파일 목록 불변 확인.
- 사용자 후속: UI 틀뿐 아니라 시안처럼 간단명료한 문구로 수정하도록 요청했다. HUD·지도·호출·메뉴·PVP·시설 문구를 짧게 정리하고 영업 준비/마감의 정보 계층을 재구성했다. 상세 장부/공차·탑승 지표/비용은 접기·펼치기로 유지한다. 승객 효과·결제 조건·저장 복구의 핵심 안내는 유지한다.
- 문구 수정 후 `build-copy.log`와 새 실제 Player 검증/화면을 최종 결과로 사용한다. 위35검사를 최종 문구의 검증 결과로 재사용하지 않는다.

## 문구 포함 최종 검증

- `BrightUIThemeTests.Rebuild` / `artifacts/ui-bright/build-copy.log`: 종료0, 테마 검사와 `PrototypeBuild.BuildHybrid` 성공.
- 실제 렌더 `-brightUiCheck` / `artifacts/ui-bright/player-copy.log`: 종료0, `BRIGHT_UI_RESULT PASS checks=38`, PNG18개. 싱글/PVP 각1280×720·1920×1080의 HUD/지도와 메뉴·영업 준비·승객 요청·운송 정산·하루 마감/상세·PVP 설정/4인 결과를 검사했다. 메뉴의 커진 제목 뒤 종료 버튼도 화면 안에 보인다.
- 마감 상세 접기/펼치기의 잔액·누적 순수익 불변, 정산창 닫기의 재입금 없음, 싱글 지도 시간 정지와 PVP 지도 중 경기 진행, PVP 캠페인 저장 금지 확인.
- `pwsh -File tools/test_prototype.ps1`: 최종 재실행 종료0, 모델28묶음. `pwsh -File tools/check_workspace.ps1 -Strict`: 종료0, issues0. `git diff --check`: 종료0.
- 기존 사용자 저장 JSON/백업14파일 목록·SHA256 불변 재확인. 지도 원본과 차량/월드 생성 소스 diff 없음. `FirstDrive.unity`는 BuildHybrid 재생성에 따른 객체 ID만 변경됐다.
- 실제 검토 화면8개를 `docs/ui/2026-11-02-bright-ui/`에 보관했다. [싱글 HUD](../ui/2026-11-02-bright-ui/03-single-hud-1280.png), [PVP HUD](../ui/2026-11-02-bright-ui/09-pvp-hud-1280.png), [영업 준비](../ui/2026-11-02-bright-ui/02-single-preparation.png), [하루 마감](../ui/2026-11-02-bright-ui/07-single-day-summary.png).
- Unity가 생성한 `*_BurstDebugInformation_DoNotShip`은 경로 범위를 확인한 뒤 `artifacts/ui-bright/burst-debug`로 이동했다. 현재 실행 폴더는140파일이며 배포 manifest는 실제 소스 커밋 후 재생성한다.
- 다른 작업의 마인드맵/기획/스크립트 변경과 미추적 Unity 설정은 이번 커밋에서 제외한다. 공유 체크리스트는 이번 UI 항목만 분리해 스테이징한다.

## 후속 요청: 회전 안내 카드

- 사용자가 큰 화살표/38m/도로명·작은 후속 회전 예시를 제공해 이번 UI에 추가하도록 요청했다.
- `NavigationCue.cs`는 기존 HD 경로 링크와 누적거리를 읽어 방향/회전까지 거리/진출 도로명/후속 회전을 구성한다. `NavigationArrow.cs`는 글꼴 의존 없는 캐시 아이콘이다. 경로 선택·탐색·지도·차량·규칙·저장은 변경하지 않는다.
- 후속 거리의 기준은 ‘첫 회전 종료 후 다음 회전 시작까지’이며 화면에 ‘이후’를 표시한다. 유턴을 좌회전으로, 차로 변경을 교차로 회전으로 표시하지 않는다. 도로명/후속 정보 없는 경우 사실인 항목만 보인다. 도착 근처에는 정차 후 F를 안내한다.
- `NavigationCueTests.Run`의 변경 전 실행은 구조화 안내 부재로 의도한 실패·종료1(`nav-red.log`). 새 구현의 거리/방향/이름/부분 경로/읽기 전용 검사와 `build-navigation.log` 및 후속 실제 Player 결과를 최종 결과로 기록한다. 앞선38검사를 내비 카드 검증으로 재사용하지 않는다.

### 내비 카드 포함 최종 결과

- `build-navigation-final.log`: 종료0 / `NAVIGATION_CUE_OK checks=15` / 테마 검사 / `PROTOTYPE_BUILD_OK`. 리뷰에서 확인한 비하이브리드 HD 연습 도착은 F 대신 감속·정차 안내를 유지하도록 보완했다.
- 실제 렌더 `player-navigation.log`: 종료0 / `BRIGHT_UI_RESULT PASS checks=44`, PNG20개. 기존 UI38검사에 실제 싱글 경로 거리·경로 객체 불변과 PVP 공용 호출 수락 후 승차 경로 카드·2화면을 추가했다. 위치 제어 UI 검사이며 전 구간 자동 물리 주행이 아니다.
- 최종 지도 Strict 종료0/issues0, 지도/차량/월드 생성 소스 diff 없음, 사용자 저장14파일 목록/SHA256 불변. 배포용 실행폴더140파일, 디버그 정보는 범위 확인 후 artifacts로 이동했다.
- 실제 최종 화면10개를 `docs/ui/2026-11-02-bright-ui/`에 보관했다. [싱글 회전 카드](../ui/2026-11-02-bright-ui/05a-single-navigation.png), [PVP 회전 카드](../ui/2026-11-02-bright-ui/10a-pvp-navigation.png).

## 소스·통합 배포 준비

- 소스 커밋 `3006e814a871171f913513cac5b3b48332de0131` (`feat: apply bright UI, concise copy and turn navigation`). UI/화면/기획/이번 체크리스트·LATEST 항목만 분리했다. 신규 Unity meta의 빈 필드 끝 공백은 GUID/설정을 바꾸지 않고 정리해 staged diff 검사를 통과했다.
- `tools/update_distribution_manifest.ps1 -SourceCommit 3006e814a871171f913513cac5b3b48332de0131 -ValidatedOn 2026-11-02 -ValidationReport artifacts/ui-bright/distribution-validation.json` 종료0.
- 통합 manifest와 PVP 위임 `-VerifyOnly` 모두 종료0: **140파일 / 115,200,835바이트**, 실제 크기·SHA256 전수 일치. 검증 보고서는 이번 테마/내비15·모델28·실제UI44와 수동/전체 캠페인 미검증을 구분했다.
- 실행: `builds/EungyeTaxi/EungyeTaxi.exe` → 은계 영업 또는 PVP 대전. 회사/제품명·기존 저장 위치·지도 원본/ZIP 유지. 게시 결과는 아래 영수증에 기록한다.

## 최신 원격 main 통합

- 게시 직전 fetch에서 원격 main `7597eaa66586302eb5ea172bd387f1fac794590a`까지 PVP 피로/식사/휴식·승객 동등성·교차로 수정과 법규 문서가 추가된 것을 확인했다. UI만 게시하면서 이 기능을 되돌리지 않도록 네이티브 관리 작업트리 `bright-ui-main/project-b` / `codex/bright-ui-main`에서 3-way 통합했다. 원래 `C:/git/project-b`의 사용자 미커밋을 stash/reset하지 않았다.
- 충돌은 체크리스트·LATEST·빌드 생성 Scene·지도 호출 예측·시설 UI에서 해결했다. 최신 PVP의 피로 예측·공용 선점·실시간 이용/중단·완료 시 결제/회복을 보존하며 짧은 문구·밝은 카드로 표현했다. 새 피로 안내는 대화/응답 화면과 겹치지 않게 숨긴다. 원격 변경 대비 지도/차량/월드 생성 소스 diff 없음.
- `BrightUIThemeTests.IntegratedBuild` / `integrated-build.log` 종료0: 테마·내비15·승객36,041·PVP 피로/서비스/봇·도시90,645·모드/저장 검사와 BuildHybrid 통과. 통합 모델28묶음 종료0.
- 실제 통합 UI `integrated-ui.log` 종료0 /44검사/20캡처. 여기부터가 최신 PVP를 포함한 최종 실행본이다. 추가 실제 PVP 서비스·승객 검사 결과와 최종 배포 해시는 아래에 기록한다.
- 실제 서비스 `test_pvp_condition.ps1 -Capture` 종료0 /24검사: 식사/휴식 소요 시간·완료 시 결제·회복·중단/종료 시 미완료 보상 없음, 새 밝은 UI3캡처 확인. 실제 승객 `test_pvp_passengers.ps1` 종료0 /38검사: 공유8+길가3/선점·요청 응답·음식 효과·짐·점유 대기/해소·72NPC 진행 확인. 두 검사는 위치/유형/상태 fixture이며 수동 캠페인이나 전 구간 자동 물리 주행이 아니다.
- 원격에서 이미 존재하던 meta 끝 공백/과거 문서 빈 줄은 변경하지 않았으며, 원격 main 대비 UI 차이의 staged 공백 검사는 종료0이다.