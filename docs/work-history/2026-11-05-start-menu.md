# 2026-11-05 모드·싱글 시작 메뉴 간소화

## 요구·실제 구현

사용자 첫 이미지의 메뉴를 `싱글 (체험판)`, `PVP`, `게임 종료`로 정리하고 두 설명 행을 제거했다. 후속 마크업을 두 번째 화면 기준으로 삼아 현재 지역·다음 실행 지역 선택·전용 빌드/정산 설명 영역을 제거하고 지도 편집 버튼을 숨겼다.

사용자는 추가 질문에 **버튼 한 번으로 바로 운행**을 선택했다. 별도 1일차 상태/비용 준비 패널 대신 공통 싱글 메뉴를 사용한다. 새 캠페인은 `영업 시작`만 표시하고 기존 복원/현재 운행은 `영업 계속하기`와 `처음부터 시작`을 표시한다. 초기화 확인·취소 및 복원 오류 원본 보존은 유지한다. 운전 연습·종료·앞차 추종 설정과 PVP 흐름은 보존한다.

새 저장 스키마·PlayerPrefs를 만들지 않고 BusinessProgress의 복원 checkpoint/준비 상태/날짜로 분기한다. 준비 상태의 계속하기는 Depart, 운행 일시정지의 계속하기는 같은 세션의 시간 재개로 연결한다.

## 파일

- `unity/EungyeTaxi/Assets/Scripts/UnifiedModeBootstrap.cs`: 모드 문구/설명·메뉴 높이.
- `BusinessProgress.cs`, `DriveHUD.cs`, `OneDayHUD.cs`(같은 Scripts 폴더): 공통 시작/계속 메뉴·준비 화면 통합.
- `UnifiedModeProbe.cs`, `unity/EungyeTaxi/Assets/Editor/UnifiedModeTests.cs`: 상태 분기와 임시 저장/실제 실행 회귀.
- 기획05·체크리스트·실행 계획·이 기록/LATEST·최종 배포 manifest.

## 검증 진행

- `UnifiedModeTests.MenuFlow`: `artifacts/start-menu-red.log` exit1, 새 캠페인 메뉴 분기가 없어 예상 실패.
- `UnifiedModeTests.Run`: `artifacts/start-menu-green.log` exit0, START_MENU_STATE_OK 4개 + 기존 UNIFIED_MODE_EDITOR_OK 9개.
- `UnifiedModeTests.Build`: `artifacts/start-menu-build.log` Unity exit0. 메뉴4/기존 모드9·기존 캠페인 시작10·checkpoint500회/잠금/복구·live campaign22 검사 후 `PROTOTYPE_BUILD_OK` 확인. 기본 `PrototypeBuild.BuildHybrid` 실행 폴더를 갱신했다.
- 별도 읽기 전용 리뷰에서 중요/치명적 발견0. 실제 운송 중 저장/정산 재진입을 이번 Probe에서 추가 재현하지 않은 한계는 기존 live 모델/저장 회귀와 구분한다.
- Strict exit0/input issues0, `git diff --check` 통과. 이후 실제 실행·manifest·저장 보존 결과는 아래에 실제 명령 종료 후 기록한다.
- 첫 Player 검사 `artifacts/start-menu-player.log`는 exit1. 새 시작/실제 출발은 통과했으나 테스트가 출발 후 시간을 진행시킨 캠페인을 인위적으로 `preparing=true`로 되돌려 저장했다. 기존 저장 계약은 경과 시간이 있는 운행에 live snapshot을 요구하므로 `Missing active session`으로 정상 거부됐다. 제품 저장 조건은 완화하지 않고 테스트의 인위적 준비 복귀를 제거해 실제 운행 저장→정지 상태 복원→계속하기를 검사하도록 수정했다. 이 실행의 사용자4파일 비교 실패는 동시 실행 중 사용자 게임의 정상 저장 변경이며 보존 통과로 계산하지 않는다. 초기 오류 캡처는 합격 증거에서 제외한다.
- fixture 수정 후 `UnifiedModeTests.Build`를 다시 실행한 `artifacts/start-menu-build-final.log`도 Unity exit0. 메뉴4/기존 모드9·시작10·checkpoint500회/잠금/복구·live22와 기본 `PROTOTYPE_BUILD_OK` 재확인. 최종 빌드 이후 제품 코드는 변경하지 않았다.

## 환경·남은 범위

tracked 변경 없이 시작해 기존 Unity 캐시와 미추적 자료를 보존하고 `codex/simplify-start-menu`에서 작업했다. 시작 시 있던 `builds/EungyeTaxi/EungyeTaxi.zip`은 빌드 시작 전 재조회에서 이미 부재였다. 이번 작업에서 ZIP을 이동/삭제하지 않았다. 다른 미추적 자료를 커밋에 포함하지 않는다.

일반 권한 프로세스 조회에는 게임이 보이지 않았으나, 빌드 후 사용자 저장 변경을 감지하고 권한 있는 조회에서 기존 `EungyeTaxi.exe` PID26832를 확인했다. 게임을 강제 종료하거나 변경된 사용자 저장을 과거 값으로 되돌리지 않는다. 사용자에게 저장·종료 후 알림을 요청했으며 종료 후의 저장을 테스트 보존 기준으로 다시 잡는다. 이 초기 해시 차이는 저장 불변 통과로 계산하지 않는다.

사용자가 종료를 알린 뒤 권한 있는 조회에서 게임 프로세스 부재를 확인했다. 사용자 종료 저장4파일의 SHA256을 `artifacts/start-menu-user-saves-after-user-exit.json`에 새로 기록했다. 이 기준으로 최종 임시 저장 실행 전후를 비교한다.

새 빌드의 `택시 시뮬레이션_BurstDebugInformation_DoNotShip` 폴더는 실행 폴더 내 절대 경로를 확인한 뒤 `artifacts/start-menu-burst-debug`로 보존해 정식 배포에서 제외했다. NotoSansKR 공지 파일은 BuildHybrid가 저장소 라이선스 원본에서 복사한 결과다.

코드·자동 메뉴/위치 검사와 사람 수동 주행·다른PC LAN·전체3일 캠페인을 구분한다. 이번 변경의 수동 장시간 운전·패드·경제/전체 캠페인 검사는 수행하지 않는다. 지도 입력·정식 프로젝트 지도·지도 ZIP·운송/법규/조향·저장 위치는 유지한다.

## 재현

기본 실행은 `builds/EungyeTaxi/EungyeTaxi.exe` / 은계 싱글·A2 PVP/LAN v3. 새 저장과 기존 저장의 화면은 임시 저장을 사용하는 `-unifiedModeCheck -unifiedModeCaptureOutput <캡처 폴더>`로 검사한다. 개발 Unity 2022.3.62f2에서 `-executeMethod UnifiedModeTests.Build`는 집중 메뉴/저장 회귀 후 `PrototypeBuild.BuildHybrid`를 호출한다. 일반 실행은 사용자의 기존 캠페인을 복원하므로 최초 화면 확인을 위해 실제 사용자 저장을 삭제하지 않는다.

## 최종 실행·배포 확정

- 실제 소스 `618c504e2e7e9458063d59032c286de3ef985d8a`. `git log -1 --oneline -- docs/work-history/2026-11-05-start-menu.md`에서 실제 커밋을 확인했다. 제품 소스 구현은3230317, 후속618c504는 검증 fixture 수정과 빌드 생성 scene/기록이다.
- 최종 명령: `builds/EungyeTaxi/EungyeTaxi.exe -unifiedModeCheck -unifiedModeCaptureOutput C:/git/project-b/artifacts/start-menu-captures-final -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -force-d3d11 -logFile C:/git/project-b/artifacts/start-menu-player-final.log`. 각 PC의 절대 경로는 로컬 환경 정보다.
- 실제 Player exit0·UNIFIED_MODE_RESULT PASS, **38 assertions/실패0**. 최초 시작 한 번으로 운행, 실제 운행 저장/정지 상태 복원·계속하기, 초기화 취소·같은 캠페인 유지, 잘못된 원본/백업 보존, PVP4대·재시작/모드 복귀를 확인했다. 준비 상태 복원 분기는 집중 Editor에서 별도 확인했으며 최종 Player는 실제 운행 저장 복원이다.
- [모드 선택](../ui/2026-11-05-start-menu/01-mode-selection.png), [최초 시작](../ui/2026-11-05-start-menu/04-single-preparation.png), [복원 메뉴](../ui/2026-11-05-start-menu/05-single-restored-menu.png), [일시정지](../ui/2026-11-05-start-menu/06-single-paused-menu.png)의 실제1280×720 PNG를 직접 열어 확인했다. 앞선 실패 캡처와 다른 최종 폴더다. 자동 호출은 실제 버튼에 연결된 production 메서드이며 물리 마우스 클릭/수동 도로 주행 검사는 아니다.
- 사용자 종료 후 기준4파일 SHA256과 최종 전수 비교 변경0·새 사용자 저장 생성0. 사용자 플레이 중 저장과 종료 저장을 되돌리지 않았다.
- `artifacts/start-menu-validation.json`과 실제 소스로 manifest 갱신. `pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly` 및 `pwsh -File tools/update_pvp_distribution_manifest.ps1 -VerifyOnly` 각각 **exit0·140파일/115,500,849바이트**, 크기/SHA256 전수 일치. 최종 빌드가 다시 생성한 DoNotShip 폴더도 절대 경로 확인 후 `artifacts/start-menu-burst-debug-final`로 보존했다.
- 별도 LAN 프로세스·다른PC/실물 패드·장시간/전체 캠페인 검사는 이번에 반복하지 않았다. 지도·조향·운송·프로토콜·저장 경로의 변경은 없다.
- 최종 관련5문서 로컬 링크135개 누락0, `git diff --check` 통과·사용자 종료 저장4파일 최종 SHA256 재대조 변경0.
- 소스·기획·체크리스트·이력과 실행 폴더/manifest·선별 화면은 로컬 `codex/simplify-start-menu`에 커밋한다. 원격 push/통합은 이번 요청에 포함하지 않았다. 이 기록의 최신 실제 커밋은 위 `git log` 명령으로 확인한다.
