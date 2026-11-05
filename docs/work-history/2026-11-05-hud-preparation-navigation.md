# 2026-11-05 준비 결산·운행 HUD·내비 정리

## 요구·결정

사용자 세 이미지의 번호별 마크업을 기준으로 준비 팝업/기본 UI/운송 내비를 정리한다. 사용자는 후속 질문에서 **2일차부터 결산 팝업**, **기어/조향 상태 배지·지시등 화살표 변경**을 확정했다. 1일차 최초 한 번의 시작은 유지한다.

## 실제 구현

- `OneDayHUD`/새 `DayPreparationPresentation`: 2일차부터 전날 실제 이용 비용,4아이콘/현재값/전날 종료 대비 증감. 고정 가격표·조작/캠페인 설명·연습/새 캠페인·회복 문구 제거. 현금/연료는 NextDay가 그대로 이월하므로 증감0, 포만감/피로는 Last.driver와 비교한다. 옛 운전자 기록 부재는 미확인 표시이며 저장 스키마를 추가하지 않는다.
- `UnifiedModeBootstrap`: `싱글 (체험판)`→`싱글`.
- `CockpitUI`/새 `CockpitPresentation`: `호출 보기 (M)`, 기어 D/R·수동/조향 보조 배지와 실제 점멸 지시등. 운송 카드는 `목적지 (...)`·빈차/예약/탑승중 무표시, 실제 도착 제한 카운트다운과 별도 예상 운임 비용. 요청한 큰 도착 행동/조건 문구만 이 카드에서 제거하며 F/현장 표시/운임은 유지한다.
- `ServiceStops`: 기록 옆 생활거점 버튼으로 기존 팝업 접근·제목 `생활 거점`. 기존 운송 종료 추천도 유지한다. LAN 참가자의 로컬 팝업 상태를 호스트 권한 조건으로 오인하지 않도록 GuideAfterTrip의 UI 가시성 조건만 제거했다. kind/운송 중/정지·영업 상태 검증과 단순 내비 목적지 선택은 유지하며 새 프로토콜을 추가하지 않는다.
- `DrivingReadability`: 작은 다음 회전을 주 내비 오른쪽 위, 미니맵 범례/중복 호출 버튼/고정 출처 행 제거·`지도 (M)`만 표시. 생활 거점이 열려 있는 동안 작은 다음 회전은 겹치지 않도록 숨긴다. 지도 출처는 도움말의 자료/라이선스 링크로 옮겼다.
- 새 `SignalPhasePreview`/`TrafficSignalPreview`: 다음 등화 그림+초만 표시·직진까지 예상 대기 문장 숨김. 실제 권한 시계를 복제해 고정된 현재 수요로 다음 단계 경계를 확인한다. 빈 수요/횡단 점유로 다음 변경을 알 수 없으면 임의 값 없이 중립 표시한다. 실제 신호 프로그램·수요·통신·법규 권한은 변경하지 않는다. 기존 SignalCountdownText 계산 API는 회귀 호환을 위해 유지하며 화면에서는 새 표시를 사용한다.
- `HudRevisionProbe`/BusinessProgress의 진단 저장 경로: 임시 GUID 캠페인으로 실제 시작·생활거점·예약/탑승·2일차·싱글/PVP 화면을 검사한다. 일반 사용자 저장 경로/마이그레이션에는 영향을 주지 않는다.

## 빠른길/짧은길·출처 대조

RoadNavigation은 빠른길에 시간/교통 비용, 짧은길에 실제 거리 가중치를 사용한다. 같은 출발/도착에서 직선 경로에90초 교통 비용을 추가한 실제 RoadNavigation fixture가 다른 경로를 선택하고 토글/승객 경로 선호를 확인했다. 기능 차이가 있으므로 관련 승객 요청은 비활성화하지 않는다. 같은 길밖에 없는 경우에도 선택 기능을 거짓으로 비활성화하지 않는다.

실제 택시 상태 용어는 [서울시 안내](https://english.seoul.go.kr/service/movement/public-transportation/taxi/)를 대조했다. 미니맵 출처 행 제거 시 게임의 별도 출처 위치 허용은 [OSMF 게임/시뮬레이션 지침](https://osmfoundation.org/wiki/Attribution_Guidelines#Computer_games_and_simulations)을 확인했고 도움말에 `© OpenStreetMap contributors · ODbL` 및 공식 copyright 링크를 유지한다.

## 검증·수정 이력

- 세 영역을 나누어 읽기 전용 데이터/표시를 구현하고 Unity/Git/빌드는 root에서 직렬 실행했다. 기존 미추적7항목·사용자 자료를 보존해 `codex/hud-preparation-navigation`에서 작업한다.
- `HudRevisionBuild.Focused`, `artifacts/hud-revision-red.log` **exit1**. Cockpit/준비/다음 신호 표시가 없어3그룹 예상 실패. 실제 빠른길/짧은길 차이 fixture는 기존 구현에서 통과했다.
- 첫 GREEN `artifacts/hud-revision-green.log` **exit1**. 준비 테스트가 실제 지출12,300원과 상환 후 미납11,300원을 혼동했다. 실제 컴파일된 계정/표시 객체를 대조해 테스트 산술을 고치고 미납 별도 검사를 유지했다. 제품 지출 조건은 완화하지 않았다.
- 같은 실행의 적응형 녹색 예측 실패는 Unity Mono의 경계 부동소수점 비교로 복제 시계가 연장을 건너뛴 원인이었다. 번들 Mono에서6.997초→수정 후11초/황색을 재현했다. 예측 복제본만 경계를0.0001초 넘겨 확인하고 밀리초 단위 표시를 정규화했다. 실제 DemandSignalClock은 수정하지 않았다.
- `HudRevisionBuild.Run`, `artifacts/hud-revision-green-final.log` **exit0**. Cockpit9/준비150/내비86, 기존 UnifiedMode/SignalCountdown/NavigationCue/ArrivalReadability·캠페인 시작/체크포인트/LiveCampaign 회귀·HUD_REVISION_INTEGRATION_OK 확인.
- 별도 읽기 전용 리뷰에서 중요/치명적 발견0. 작은 다음 회전과 생활 거점 팝업이 겹친다는 경미한 의견은 팝업 동안 해당 작은 안내를 숨기는 표시 수정으로 반영했다.

## 사용자 저장·빌드

시작 시 사용자 게임 PID31716을 확인했다. 사용자에게 저장·종료 요청 후 종료 답변을 받았고, 권한 있는 프로세스 조회에서 종료를 확인해4파일을 `artifacts/hud-revision-user-saves-after-user-exit.json`에 새 보존 기준으로 기록했다. 사용자 게임을 강제 종료하거나 플레이/종료 저장을 이전 값으로 되돌리지 않는다. 실제 최종 BuildHybrid/Player/시각/manifest 결과는 실행 종료 후 아래에 추가한다.

## 최종 실행·시각 확인

- `HudRevisionBuild.Build`의 최종 `artifacts/hud-revision-build-rendered.log`: Unity **exit0**, 관련 통합 검사와 `PROTOTYPE_BUILD_OK` 확인. 실행중인 사용자 게임 종료 이후 기본 실행 파일을 빌드했으며 이후 제품 코드를 수정하지 않았다.
- 첫 Player `artifacts/hud-revision-player.log`는 exit1: 캡처를 위해 Time.timeScale=0으로 멈춘 상태에서 호출 수락을 시도해 기존 조건이 정상 거부했다. 실제 수락 순간만 운행 상태로 전환하도록 테스트를 고쳤으며 수락 조건을 완화하지 않았다.
- 수정 후 `artifacts/hud-revision-player-final.log`는 **20개/exit0**. 단, 그 위치 fixture는 도로 밖이라 연결 내비의 후속 회전 그림을 직접 확인할 수 없었다. 실제 선택 경로의 연결 차로 위에서 다음 회전/신호가 함께 보이는 지점을 찾도록 캡처 fixture만 보완했다.
- 최종 `artifacts/hud-revision-player-rendered.log`는 **21개/실패0/exit0**, `HUD_PLAYER_OK`. 최초 한 번의 시작·생활거점 열기/닫기(결제 없음), 실제 호출 수락·예약/탑승 상태 표시, 연결 차로의 현재/후속 경로·다음 신호 권한 불변, 실제 장부 정산/다음날 준비·출발, 모드 복귀·PVP4대를 확인했다. 탑승은 FareTrip 상태 전이 fixture이며 도로를 자동/수동으로 주행해 승하차한 검사가 아니다.
- 준비 fixture는 실제 장부로 식사3,000원+마감 주차500원=3,500원을 정산하고 1일차 종료 피로80→2일차20, 현금6,500원/연료60%/포만50의 실제 이월 증감을 확인한다. 20분/3일 사람 플레이를 했다는 의미가 아니다.
- 선별 PNG8개는 `docs/ui/2026-11-05-hud-preparation-navigation/`에 보존했다. [모드](../ui/2026-11-05-hud-preparation-navigation/00-mode.png)·[빈차](../ui/2026-11-05-hud-preparation-navigation/01-vacant.png)·[생활거점](../ui/2026-11-05-hud-preparation-navigation/02-life-hubs.png)·[예약/후속 경로/다음 신호](../ui/2026-11-05-hud-preparation-navigation/03-reserved-navigation.png)·[탑승](../ui/2026-11-05-hud-preparation-navigation/04-occupied.png)·[2일차1280](../ui/2026-11-05-hud-preparation-navigation/05-day-two-1280.png)·[2일차1920](../ui/2026-11-05-hud-preparation-navigation/06-day-two-1920.png)·[PVP](../ui/2026-11-05-hud-preparation-navigation/08-pvp-cockpit.png). 실제 캡처를 열어 주요 배치/글자를 확인했으며 PNG 헤더로1280×720·1920×1080을 검증했다. 물리 마우스 클릭 검사는 아니다.
- Strict **exit0/Input issues0**, `git diff --check` 통과. 사용자 종료 기준4파일 SHA256을 Player 종료 후 전수 비교해 **변경0**. 생성한 DoNotShip 폴더는 실행 폴더 안 절대 경로를 확인한 뒤 각 빌드별 `artifacts/hud-revision-burst-debug*`로 보존했다.
- 작업 중 다른 작업의 문서 커밋 ad19d7d/b0b5245/e25fd5d가 같은 브랜치에 추가된 것을 확인해 보존했다. 그 기획 제안을 이번 런타임에 구현하거나 사용자 미추적 자료를 포함하지 않았다. 최종 소스/배포 manifest 실제 커밋·전수 검사 결과는 다음 절에 기록한다.

## 배포 확정

- 실제 소스 커밋은 `3338612f589892983306881e3413d453a169cbf8`이다. 소스 커밋 후 `git log -1 --oneline -- docs/work-history/2026-11-05-hud-preparation-navigation.md`로3338612를 확인했다. 빌드 이후 제품 소스 변경은 없다.
- `artifacts/hud-revision-validation.json`과 위 실제 소스로 `distribution/EungyeTaxi-build-manifest.json`을 갱신했다. `pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly`와 `pwsh -File tools/update_pvp_distribution_manifest.ps1 -VerifyOnly` 각각 **exit0/DISTRIBUTION_OK files=140 bytes=115519571**, 크기/SHA256 전수 일치.
- 기본 배포 `builds/EungyeTaxi/EungyeTaxi.exe` / 은계 싱글·A2 PVP/LAN v3. 지도/ZIP·물리·운임/경제/신호 주기·회사/제품/저장 위치와 기존7개 미추적 항목을 보존했다.
- 실행 폴더·manifest·선별 화면·최종 기획/체크리스트/이력은 로컬 `codex/hud-preparation-navigation`에 커밋한다. 원격 push/통합은 이번 요청에 포함하지 않았다. 이 기록의 최신 실제 커밋은 위 `git log` 명령으로 확인한다.
- 최종 관련5문서 로컬 링크153개 누락0·`git diff --check` 통과, 사용자 종료 저장4파일 SHA256 재대조 변경0.

## 재현·미검증 범위

기본 실행은 `builds/EungyeTaxi/EungyeTaxi.exe` / 은계 싱글·A2 PVP/LAN v3, 개발 빌드는 `PrototypeBuild.BuildHybrid`다. `HudRevisionBuild.Build`는 관련 통합 검사 후 기본 빌드를 호출한다. 새 exe의 `-hudRevisionCheck -hudCaptureOutput <캡처 폴더>`는 사용자 저장과 다른 임시 캠페인을 사용한다. GUI 실행은 실제 프로세스 종료 코드를 기다려 판정한다.

모델·상태/위치 fixture·렌더와 실제 마우스/사람 연속 운전·다른PC LAN·전체 캠페인을 구분한다. 장시간/전체3일 캠페인·실물 패드·경제 체감은 이번 검증에 포함하지 않는다. 지도/ZIP·조향/제동/신호 주기/운임/저장 경로·LAN 프로토콜은 유지한다.
