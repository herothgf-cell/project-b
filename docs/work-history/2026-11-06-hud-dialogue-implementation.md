# 승인 HUD·승객 대화창 가독성 적용

## 요구·구현

사용자가 검토한 시안을 게임에 적용하되 기존 기능/수치를 유지하고 내비 신호3구/4구와 실제 도로 시설을 구분하도록 요청했다. [계획](../superpowers/plans/2026-11-06-hud-dialogue-readability.md)에 따라 별도 작업공간에서 구현했다.

- 상단 도구막대 축소, 내비/운송 카드를 위쪽으로 모음, 지도 아래 잔액/순수익 또는 PVP4인 현황, 작은 왼쪽 아래 상태계. 기존 메뉴/레드닷·비활성 조건·5종 상태/점수/기한/운임 계산 유지.
- 큰 승객 대사와 초상, 답변/일반 효과/성공·실패 종료 배지 분리. 활성 프로필의 원문 절과 수치를 읽으며 새 밸런스 값 없음. 하단 두 모서리 카드 사이에 배치하고 긴 대화는 높이48%/최대420논리px 안에서 스크롤. 기존 단축키·응답/작업 잠금·LAN 경로 보존.
- 신호 고정4구 그림 대신 방향별 실제 생성 신호기 또는 작성 시설 형식을 조회한다. `RoadMapFacility.type`의 명시 `3구`/`4구`를 월드/내비가 함께 사용하며 기존 미지정4구 보존. 꺼진 좌회전 슬롯과 좌회전 권한은 별개다. 좌회전 연결이 없다고 무조건3구로 만들지 않는다. 새 지도 필드/입력/주기 변경 없음.
- 작성 시설의 명시 형식+appearanceConfirmed+자료URL이 함께 있으면 `현장 확인`, 그 외 `게임 배치`. 방향 불일치/형식 상충은 `형식 미확인`. 현장3구 위치를 새로 조사하거나 미확인 교차로를 실제와 일치한다고 인증한 작업은 아니다.

## 검증과 수정 과정

- `red.log`: 신호 형식 모델 부재로 의도한 RED, Unity exit1.
- `build.log`: HUD16전 단계15·테마·기존 HUD/메뉴/날짜/저장·내비86·회전15·도착30·신호/실제 지도 접근·NGII좌회전·효과72와 BuildHybrid 통과, exit0.
- 첫 Player는174검사 통과했지만 숨긴 창의 초기1280 캡처가 검은 화면이었다. 그 이미지는 화면 검증 근거에서 제외했다. 후속 Capture는 실제 픽셀을 확인하고 재시도하며, 정상 창으로 재검사했다.
- 첫 실제 대화 화면에서 원문이 쉼표 구분이라 배지 분리가 안 되는 것을 발견했다. `effects-red.log`로 재현 실패 확인 후 생성 효과의 알려진 접두사 앞 쉼표도 분리하도록 수정. 임의 사용자 문구의 쉼표는 그대로 둔다.
- 독립 코드 리뷰의 신호 출처 툴팁 미표시 지적을 확인하고 화면 캡션으로 직접 표시하도록 수정했다.
- `final-build.log`: HUD/3구·4구/효과16, 선택 효과72, 기존 내비/HUD/저장/신호·실제 지도 회귀와 통합빌드 exit0. `final-player.log`: 정상 렌더 검증 포함178검사 PASS, 싱글/PVP/메뉴·시설·정산 및1280/1920 긴 대화 캡처.
- 마지막 검증용 probe는 활성 대화 캡처에서 시간정지를 제거하고 긴 내용의 스크롤 끝을 확인하도록 보완했다. 마지막 빌드/실행 결과와 배포 영수증은 아래에 추가한다.

## 범위·실행·파일

로그/전체 캡처는 로컬 `artifacts/hud-dialogue/`, 대표 캡처는 `docs/ui/2026-11-06-hud-dialogue-applied/`에 둔다. 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`, 은계 싱글/A2 PVP·LANv11, 빌드는 `PrototypeBuild.BuildHybrid`다. 로컬 Unity 설치는 `D:/Program Files/Unity Hub/Editor/2022.3.62f2/Editor/Unity.exe`다.

재현: Unity에 `-batchmode -nographics -quit -projectPath <root>/unity/EungyeTaxi -executeMethod HudDialogueReadabilityChecks.Build -logFile <log>`; Player에 `-brightUiCheck -fullUiReview -lifeHubUiReview -hudDialogueReview -brightUiOutput <PNG폴더> -screen-fullscreen 0 -logFile <log>`. Player는 격리 저장·위치/상태 제어 검증이며 수동 플레이/자동 물리 주행/전체3일 수용이 아니다. 별도 신호 캡처는 `-brightUiCheck -signalCountdownOnly`를 사용한다.

변경 코드: `CockpitUI.cs`, `DrivingReadability.cs`, `PassengerDialogueUI.cs`, `SignalHeadPresentation.cs`, `RoadMapFacilities.cs`; 검증: `BrightUIProbe.cs`, `HudDialogueReadabilityChecks.cs`, `ChoiceEffectsRuntimeChecks.cs`. 기획05·체크리스트·계획·이력/LATEST·대표 캡처·실행폴더/manifest를 함께 보존한다. 동시 main의 제안서/기획 정리 커밋은 ff로 받아 보존했다.

지도 Strict exit0/issues0, 지도 contentHash `35d1bc43d92c996d431acd0464bfc7a175b926cd1e35adb995e99dd7ab91cbac`, 지도/ZIP/밸런스 입력 미변경. 사용자 저장16개 해시 기준을 보관했으며 최종 비교는 영수증에 남긴다. 수동 장시간/다른PC·물리 패드·전체 캠페인 및 실제 도로3구 현장 배치 조사는 미검증/후속이다.

## 최종 실행 검증 영수증

- `release-build.log`: 정식 `PrototypeBuild.BuildHybrid` exit0.
- `release-player.log`:90검사 PASS/exit0. 활성 대화1280/1920, 긴 대화의 실제 스크롤 끝·4번째 선택지 노출과 clamp 양쪽 해상도 확인. `signals.log`:8검사 PASS/exit0, 실제 접근 신호를 읽는4구·게임배치·다음 등화/초 화면 확인.3구 월드와 내비의 공유 계약은 Editor fixture로 검증했으며 실제 은계 현장3구 배치 확인으로 표시하지 않는다.
- 보관 사용자 저장/백업16파일 SHA256 변화0. 원래 작업폴더와 비교한 catalog/default-profile/프로젝트 지도/로컬 입력 manifest 해시 모두 동일. 새 지도·ZIP·게임 수치 변경 없음.
- 대표 실제 화면: [주행](../ui/2026-11-06-hud-dialogue-applied/05a-single-navigation.png), [대화1280](../ui/2026-11-06-hud-dialogue-applied/dialogue-single-1280.png), [대화1920](../ui/2026-11-06-hud-dialogue-applied/dialogue-single-1920.png), [긴 대화 스크롤 끝](../ui/2026-11-06-hud-dialogue-applied/dialogue-long-bottom-1280.png), [PVP](../ui/2026-11-06-hud-dialogue-applied/09-pvp-hud-1280.png), [신호](../ui/2026-11-06-hud-dialogue-applied/signal-countdown-1280.png).
- 코드 리뷰 후속 확인에서 출처 표시 수정과 쉼표 효과 분리의 추가 중요 지적 없음. `git diff --check` 통과.

소스 커밋 이후 실제 해시를 읽어 배포 manifest를 생성하고 기본 폴더 반영 결과를 아래에 덧붙인다. 원격 push는 요청되지 않아 수행하지 않는다.

실제 소스 커밋 `c8f5d89`를 `git log -1 --oneline -- <본 기록>`으로 확인했다. 해당 전체40자리 해시로 통합 manifest 생성, 통합/PVP위임 VerifyOnly 모두 exit0:141파일/124,579,862바이트 전수 SHA256/크기 일치. 이 빌드/manifest를 로컬 main 기본 실행폴더에 통합한다.

배포 커밋 `7dc84b6`, 동시 승객 기획 `854f397`을 보존한 병합 `63efe2a`를 실제 Git 결과로 확인했다. LATEST의 충돌은 두 작업 항목을 모두 남겨 해소했고 게임 코드 충돌은 없었다. 로컬 main을63efe2a로 fast-forward하여 **원래 `D:/Git/project-b/builds/EungyeTaxi/EungyeTaxi.exe` 실행폴더에 반영 완료**. 원래 폴더에서 통합/PVP VerifyOnly 모두 exit0,141파일124,579,862바이트. 원격 게시와 사용자 수동 플레이는 수행하지 않았다. 이 영수증의 최종 커밋은 `git log -1 --oneline -- <본 기록>`으로 확인한다.
