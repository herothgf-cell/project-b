# 2026-11-02 지도 스타일·네비/운행 요약 두 카드

## 요청·적용

사용자 요청: 미니맵/M지도도 승인한 밝은 시안처럼 수정. 왼쪽 신호/네비/운행 정보3개를 네비+운행 요약2개로 합친 간단 시안을 확인하고 ‘이어서 진행, 네비 이견 없음’ 승인. 기존 main 푸시 지시를 이어서 수행한다.

- MapPresentation: 실제 MapData의 건물·수변·녹지·보도·도로 면과 도로 폭을 2048 지도 캐시로 표현한다. 미니맵/M지도 공용. 없는 도로나 건물을 만들지 않는다.
- 지도 경로는 승차 파란 실선·운송 분홍 점선, 교통 필터 활성화 시 혼잡색 유지. 위치 핀 끝점/클릭 영역을 실제 투영 좌표에 맞추고 노란 차량 화살표는 회전된 GUI 행렬 없이 캐시로 표시한다.
- 네비+운행 요약 두 카드. 신호 없을 때 불필요한 안내/빈 영역 축소, 실제 신호가 있는 접근만 네비 안에 짧게 표시. 교육용 점멸/비보호 ‘녹색에서’/현장 규제 아님은 보존한다. 도착 시 현재 승하차 목적지와 일치할 때만 운행 요약에 정차/F 안내.
- 지도 데이터·월드/차량·길찾기·서비스·수락/정산·저장·PVP 경기 규칙은 바꾸지 않는다. 싱글 지도 시간 정지/PVP 지도 시간 진행 유지.

## 변경 파일

`unity/EungyeTaxi/Assets/Scripts/{MapPresentation,DriveHUD,DrivingReadability,FullMapOverlay,MapDestinationUI,CockpitUI,BrightUIProbe}.cs`, `Assets/Editor/MapPresentationTests.cs` 및 해당 meta, 재생성 FirstDrive 씬, 통합 배포 폴더·manifest. 관련 UI 계획/인터페이스 기획/체크리스트/LATEST도 함께 갱신한다.

## 검증·리뷰

- 사전 git status clean. 현재 관리 작업트리에서만 구현. 원격 fetch 결과 시작 HEAD d818c33와 main 동일(0/0).
- `tools/check_workspace.ps1 -Strict`: exit0 / issues0. 기본 프로젝트 지도 contentHash35d1bc43… 유지. 선택 입력 eungye-map/route42-map 부재는 기존 프로젝트 지도 모드의 허용 상태.
- `tools/test_prototype.ps1`: exit0 / 모델28묶음.
- `MapPresentationTests.Run` 초기 RED: exit1 ‘renderer is not implemented’. 최초 Build exit0 / 지도 면·실제 폭·클리핑·입력 불변·빈 지도, 내비15, 기존 MapReadability/법규 라벨 배치 검사 통과.
- 독립 코드 리뷰에서 직사각형 지도 폭·핀 앵커/클릭·교육 비보호 문구를 지적. 실제 200×100 지도 회귀 RED(exit1)를 확인하고 월드 좌표 사각형으로 도로를 그리도록 수정. 핀은 실제 위치가 끝점이며 패딩한 클릭 영역이 끝점/전체 그림을 포함한다. 미니맵 라벨 배치도 최종 앵커/핀 크기를 공유한다.
- 중간 빌드의 실제 Player `-brightUiCheck`: exit0 /48체크·22PNG. 최종 클릭/핀 보정 후 다시 빌드·실행한다. 아래 최종 검증 항목에서 최종 결과를 따로 기록한다.

## 실행·재현

로컬 Unity Editor: `C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe` (PC 환경 정보).

`Unity.exe -batchmode -nographics -quit -buildTarget Win64 -projectPath <repo>/unity/EungyeTaxi -executeMethod MapPresentationTests.Build -logFile <repo>/artifacts/map-ui/publication-build.log`

`builds/EungyeTaxi/EungyeTaxi.exe -brightUiCheck -brightUiOutput <repo>/artifacts/map-ui/publication -screen-fullscreen 0 -screen-width 1600 -screen-height 900 -logFile <repo>/artifacts/map-ui/publication.log`

로그·중간 PNG는 ignored artifacts/map-ui, 최종 확인 화면은 docs/ui/2026-11-02-map-style/에 둔다. 실제 실행 대상은 통합 은계 싱글/자체 도시PVP·BuildHybrid이다.

## 남은 범위

위치 제어 fixture와 UI 상태/표시 검증이며 자동 물리 완주·사람의 직접 운전·전체20분/3일 캠페인 검증을 수행했다는 뜻이 아니다. 원격4인 PVP나 새로운 지도/차량 아트 작업도 이번 범위가 아니다.

## 최종 검증·배포

`MapPresentationTests.Build` 최종 verified-build.log: Unity exit0, 지도 면/도로 폭/직사각형/핀 끝점/핀 클릭·입력 불변·빈 지도, 내비15, MapReadability, 법규 라벨 배치 통과·BuildHybrid 성공.

직전 release.log의52검사 중 정산1건은 무작위 졸린 취객이 정상 하차 대기에 들어가서 실패했다. 게임의 취객 규칙은 바꾸지 않고 BrightUIProbe만 명시적 일반 승객/Normal case로 고정했다. verified.log에서 전체52검사·22PNG 통과/exit0, 사용자 저장14파일 경로/SHA256 불변을 확인했다.

1280 PVP 지도 캡처에서 선택 경로 끝의 핀이 위쪽 경계에서 일부 잘리는 것을 발견해, FitRoutes의 고정160m 여유를 화면100px 총 여유로 변경했다. 확대 최소값1은 기존대로 유지한다. 핀 라벨은 흰 카드/진한 글자로 대비를 보정했다. 독립 재리뷰에서 추가 결함 없음. 이 최종 표시 보정의 빌드/실행과 manifest/게시 결과는 완료 후 덧붙인다.

게시용 최종 publication-build.log Unity exit0, publication.log Player exit0 /52체크·22캡처. 1280 PVP 지도 끝점 핀 여백과 1920 은계 지도/싱글·PVP 두 카드를 실제 캡처로 확인했다. 저장14파일 불변. 최종 확인8화면을 docs/ui/2026-11-02-map-style에 보관했다. 모델28묶음·Strict issues0 결과와 함께 이번 검증 범위다. 배포 실행 폴더139파일115,118,797바이트.

소스/기획/실제 화면 커밋은 `git log -1 --oneline -- docs/work-history/2026-11-02-map-style-two-cards.md`로 확인한8aa382c이다. 실제40자리8aa382c73b39951956b839e29a70ceb8301b0d44를 SourceCommit으로 manifest를 생성했다. 통합/PVP VerifyOnly 모두 exit0: DISTRIBUTION_OK files139 bytes115118797. 공유 main 게시 뒤 실제 게시 결과를 덧붙인다.

## 게시 직전 최신 LAN PVP 보존 통합

UI 단독 후보8aa382c/배포 후보2a2293f는 원격에 게시하지 않았다. 마지막 fetch에서 원격 main에7개 새 커밋(끝6ab68d9663edb4d6162cea089751500e0c4eda8e)이 추가됐음을 확인해 푸시를 멈추고 통합했다. 위115,118,797바이트는 단독 후보이며 최종 배포는 이 후속 통합 결과를 기준으로 한다.

충돌은 생성 빌드6파일·manifest·FirstDrive 씬·LATEST였다. 빌드/manifest/씬은 원격 것을 일단 유지한 뒤 현재 소스로 재생성하고 LATEST는 두 이력을 모두 유지했다. CockpitUI의 LAN 호스트/참가자/오프라인 표시는 자동 병합 후 보존 여부를 확인했다. 원격 대비 게임플레이/LAN 소스 변경 없음, UI8파일과 지도 검사 진입점만 차이임을 독립 리뷰로 확인했다.

- `MapPresentationTests.Build` / lan-integrated-build.log: Unity exit0. 테마·내비15·지도/경계/라벨, 승객36,041·LAN JSON17·BuildHybrid 통과.
- 최신 통합 상태에서 `tools/test_prototype.ps1` 모델28묶음 exit0, `tools/check_workspace.ps1 -Strict` exit0/issues0.
- `tools/test_pvp_native_lan.ps1 -Port 18983 -Reconnect -TimeoutSeconds 300`: exit0, host26/client28(총54), artifacts/pvp/native-lan-20261102-163023-75390201. 실제 호스트 WheelCollider13.921978m·최고4.051656m/s, 메뉴 중 공유시계·자기차 제동·승객 선점/복제·봇 승계·동일 좌석 재접속/언로드 통과. 같은 PC loopback 두 프로세스이며 다른PC/4인 전체 운송 검사가 아니다.
- 최종 통합 실행 폴더139파일115,172,656바이트. 맵은 기존 은계/A2이며 원격 A3는 시안 문서만 보존한다. UI가 지도 원본/차량/GIS/ZIP을 바꾼 것이 아니다.

통합 후 UI52검사·manifest/실제 커밋/게시 결과를 확인해 아래에 덧붙인다.

통합 UI 최종 lan-integrated.log: Player exit0 /52검사·22캡처·사용자 저장14파일 경로/SHA256 불변. 최종8캡처도 이 실행본 것으로 교체했다. 원격 기준 git diff origin/main --check 통과(병합으로 들어온 원격 meta의 기존 공백은 수정하지 않음). 최종 소스 병합 커밋 이후 manifest를 재생성한다.

실제 소스 병합 커밋은 `git log -1 --oneline -- <이력 파일>`로 확인한 **f27ffbe692fed9ce3af0916be46fea18c3e62f1b**. 이를 SourceCommit으로 lan-integrated-validation.json의 실제 결과를 사용해 통합 manifest를 다시 생성했다. 통합/PVP VerifyOnly 각각 exit0, **139파일115,172,656바이트 전수 크기·SHA256 일치**. 게시 영수증은 후속 절을 따른다.
