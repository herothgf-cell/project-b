# 전체 캐주얼 UI 통일

## 요구와 범위

최초 밝은 캐주얼3D 시안의 전체 UI 적용을 승인받았다. 싱글/PVP 공통으로 둥근 카드, 아이콘, 짧은 제목/행동 문구, 초록 주요 행동과 파란 안내를 통일한다. 맵·건물·차량 외형은 사용자가 가장 마지막에 고민하기로 했으므로 유지한다. 현재 기능만 표시하며 시안의 미구현 보상/성장 시스템은 추가하지 않는다.

직전 작업 트리는 5a5df15였고 원격 main 93개 커밋을 fast-forward하여 d292508 위에 적용했다. 승인 밸런스50개·운전 설정·작은 방·LANv10을 보존한다. [화면별 기준](../ui/2026-11-06-full-ui-direction.md), [계획](../superpowers/plans/2026-11-06-full-casual-ui.md), [초상 생성 프롬프트/출처](../ui/2026-11-06-full-ui-art.md).

## 실제 구현과 변경 파일

- `CasualUI`, `KoreanText`, `HudGlyph`: 공통 그림자/테두리/헤더·초상·배경·기어 아이콘·입력/스크롤/슬라이더. 신규 `CasualMenuUI`에서 모드 선택 구성, 기존 `UnifiedModeBootstrap` 전환 호출 유지.
- `CockpitUI`, `DrivingReadability`, `FullMapOverlay`, `MapDestinationUI`: HUD 계층과 지도 제목/선택 카드. 기존 경로·지도 형상·신호 다음 등화/잔여 초·선택/수락 계약 유지.
- `PassengerDialogueUI`, `ScenarioChoiceSituation`, `DriveNotifications`, `ServiceStops`/`ServiceStopsPlannerUI`, `AccidentController`/`AccidentPresentation`, `VehicleDamageController`/`WorkshopPresentation`: 초상/말풍선·응답/효과 분리, 비용/시간/경고/취소의 카드화. 사용자 작성 대사/선택 효과 원본은 변경하지 않는다.
- `DriveHUD`, `DrivingLaw`, `PracticeSession`, `PracticeCoach`, `DrivingSettingsPanel`: 메뉴/도움말/기록/연습/운전 설정의 공통 헤더·행동. 법규 기록은 실제 본문 폭으로 높이를 측정한다.
- `RideCompletionPanel`, `OneDayHUD`, `PersonalRoomHUD`: 실수령/순수익 강조·정산3줄/상세 유지·내 공간 상품과 구매 확인 정리. 방 그림/구매 가격/소유 저장 유지.
- `PvpPrototypePanel`, `PvpNativeLan` 메뉴, `PvpSimulationMatch` 결과: 설정/LAN대기실·경기 종료 카드. 긴 닉네임은 한 줄 말줄임+툴팁. 서버 권한/경기 시계/금액 계산은 변경하지 않는다.
- `Resources/UI/passenger-portraits.png`: 내장 image_gen으로 생성한 투명 캐주얼3D 성인 초상4종 atlas(1254×1254), 표시 전용. `FullUIBuild`는 importer/기존 계약/통합 빌드를 실행한다. `BrightUIProbe`에 명시적 전체 UI/메뉴 전용 검사 옵션을 추가했다. `FirstDrive.unity`는 기존 빌드 생성 절차의 재생성 결과다.

## 수행 검증

- `pwsh -File tools/check_workspace.ps1 -Strict`: exit0, issues0. 입력 지도/ZIP 미변경, contentHash `35d1bc43d92c996d431acd0464bfc7a175b926cd1e35adb995e99dd7ab91cbac` 유지.
- Unity2022.3.62f2 `FullUIBuild.Build`: exit0. `FULL_UI_ART_OK 1254x1254`, 테마, 운전 설정171/메뉴183/모달, 콘텐츠 runtime32/build3·복사본, HUD focused/integration 계약, `PROTOTYPE_BUILD_OK` 확인. C#760/Node63 전체 묶음은 직전 main의 기록이며 이번 재실행으로 세지 않는다.
- 실제 통합 Player `-brightUiCheck -fullUiReview`: exit0, `BRIGHT_UI_RESULT PASS checks=112`, PNG46개. 1280×720/1920×1080에서 메뉴/설정 취소/도움말/기록/연습/생활거점/지도/시설/운행/운송 완료/하루 마감/방/재시작 취소/PVP 결과를 검사했다. 싱글 지도 시계 정지·PVP 지도 시계 진행, 호출 미리보기/수락 분리, 정산 한 번·탐색 중 지출/시간 불변을 확인했다.
- 테스트는 `UnifiedModeProbe.CampaignPath` 임시 저장과 별도 임시 설정 키/취소만 사용했다. 사전 백업한 사용자 JSON/backup16개 SHA256 사후 차이0, 사용자 설정 Apply 없음.
- 독립 소스 검토에서 게임/저장/시계/LAN 권한 계산 변경 없음. 법규 본문 폭 불일치와 PVP 긴 닉네임 줄바꿈2건을 수정 후 위 빌드를 수행했다.
- 첫 `01-mode-menu.png`는 검게 캡처되어 시각 확인 근거로 사용하지 않았다. 메뉴 전용 후속 확인/최종 배포 결과는 아래 영수증에 기재한다. 유효한 실제 화면은 `docs/ui/2026-11-06-full-ui/`에 보관한다.

## 실행·재현

기본 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`. 모드에서 싱글/PVP를 선택한다. 기존 저장/개인 설정/밸런스 관리창 사용법은 유지한다.

로컬 개발 환경의 Unity 경로는 `C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe`다. `-batchmode -nographics -quit -buildTarget Win64 -projectPath <저장소>/unity/EungyeTaxi -executeMethod FullUIBuild.Build -logFile <로그>`로 빌드한다. 실제 화면 검사는 실행 파일에 `-brightUiCheck -fullUiReview -brightUiOutput <출력> -screen-fullscreen 0 -logFile <로그>`를 지정한다. 메뉴만 검사할 때는 `-fullUiReview` 대신 `-fullUiMenuOnly`를 사용한다.

## 미검증·다음 작업

위 Player 검사는 통제 위치에서 네이티브 호출/승하차 메서드를 쓰는 화면/상태 통합 검사이며 자동 물리 주행·사람 장시간·전체3일 캠페인 검사가 아니다. 다른 PC/DPI·실물패드·2인 LAN 장시간은 새로 검사하지 않았다. 긴 모든 사용자 작성 대사와 모든 사고/서비스 상태의 수동 전수 검사는 남는다. 현재 월드/차량 아트를 유지한 실제 화면이므로 시안의 도시/택시 렌더와 동일하다고 주장하지 않는다. 다음은 사용자 실제 플레이에서 정보 밀도·문구 수용 확인, 이후 맵/차량 외형 별도 검토다.

## 최종 게시 영수증

- 최종 재빌드 `artifacts/full-ui/build-final.log`: exit0, 위 계약 모두 재통과. 프로덕션 소스는 첫112검사와 동일하며 메뉴 전용 probe 분기만 추가했다.
- `-brightUiCheck -fullUiMenuOnly`: exit0, `FULL_UI_MENU_RESULT PASS checks=4`. 두 해상도 모두 정상 메뉴/초상/버튼을 실제 PNG로 확인했다. 캠페인 DriveSession0, 저장/설정 미접근. 첫 검은 캡처 대신 유효한2장을 보관했다.
- 소스/이력 최초 커밋은 `686d90fce0053e37e09d79284353a284123b9cd5`다. `git log -1 --oneline -- docs/work-history/2026-11-06-full-casual-ui.md`로 확인했다.
- 출하 실행 폴더141파일·124,544,801바이트, 통합/PVP VerifyOnly 모두 exit0. manifest는 위 실제 소스 커밋과 이번 검증 보고서를 참조한다. 기존 추적 Burst 디버그txt도 그대로 포함한다. 사용자 active-profile·지도 ZIP/입력은 미변경이다.
- 원본 `C:/git/project-b`는 현재 `codex/driving-settings`이고 기존 기획2파일 변경과 untracked 자료가 있으므로 브랜치/작업 폴더는 건드리지 않는다. 이 관리 작업 트리의 실행본과 원격 main으로 배포한다.
- 원격 게시 해시는 실제 push 확인 후 별도 영수증으로 추가한다.
