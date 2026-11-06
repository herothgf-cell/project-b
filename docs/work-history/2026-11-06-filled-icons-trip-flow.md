# 2026-11-06 채움 아이콘·운송 안내 연결

## 요청과 실제 구현

시안과의 아이콘 차이 개선 및 호출→승차→요청→하차→정산 안내 점검의 다음 구현을 승인받아 진행했다. 맵/차량 외형은 후순위로 유지한다.

- 공통 아이콘20종을 굵은 채움 실루엣으로 교체했다. 금색 원화 동전, 초록 미소/안전 방패, 파랑 타이머와 상태별 색 배지를 HUD·상태계·정산에 적용했다. 파란 헤더의 흰 아이콘과 기존 의미/경고 색을 유지한다. 시안의 개별3D 아이콘 이미지를 그대로 복제한 것은 아니며, 작은 크기에서 읽히는 공통 도형이다.
- 픽업은 `승차 장소`, 탑승 후에는 `도착지`로 구분한다. 예약/기한·운임 조건은 유지한다.
- 요청/유형/상황 대화와 정산 초상이 기존 offerId→tripId 기준을 공유한다. 대화 제목이나 출발/도착 문자열에 따라 같은 승객의 얼굴이 바뀌지 않는다. 저장/LAN 필드 추가 없음.
- 정산 `시설 찾기`는 기존 생활거점을 연다. 이미 열린 거점을 닫지 않고 유지하며, 시설 선택/길안내/현장 결제는 기존 별도 행동이다. 영업마감·경기종료·고장 상태에서는 전환하지 않는다. 실패하면 정산을 보존한다.

## 변경 파일

`HudGlyph.cs`, `CasualUI.cs`, `DriverStatusGauges.cs`, `CockpitPresentation.cs`, `CockpitUI.cs`, `PassengerDialogueUI.cs`, `PassengerBehaviorUI.cs`, `PassengerRequestObservation.cs`, `RideCompletionPanel.cs`, `BrightUIProbe.cs` 및 Editor `CockpitPresentationChecks.cs`. 모두 `unity/EungyeTaxi/Assets/` 아래다. 통합 빌드 생성 장면과 실행본, 배포 manifest 및 본 문서/체크리스트/UI 기획을 함께 게시한다.

## 수행한 검증

- 최초 FullUIBuild는 문구 테스트의 Reflection `GetMethod`가 새 overload를 구분하지 못해 exit1. 호출 타입을 명시하고 승차/도착2검사를 추가한 후 다시 실행했다. 실패 로그 `artifacts/filled-icons-trip-flow/build-initial-reflection-failure.log` 보존.
- 최종 `FullUIBuild.Build`→`PrototypeBuild.BuildHybrid` exit0. 테마·설정171/메뉴183/모달·제동22/실제PhysX8·NGII좌회전 회귀·콘텐츠32/빌드3/복사·HUD focused/integration 통과. 단계문구11, 기존 ArrivalReadability30도 통과.
- 실제 통합 Player `-brightUiCheck -fullUiReview -lifeHubUiReview`: exit0, **174검사 PASS / 60PNG**. 싱글/PVP 공통 화면1280×720/1920×1080 확인. 호출 수락→승차→요청 표시→하차→정산은 실제 게임 메서드를 호출하되 위치를 제어한 검사다. 요청은 Comfort 표시 후 안전 유보로 진행했다. 자동 물리 완주나 사람 수동 주행이 아니다.
- 승차 단계 문구·초상 동일성·정산 이후 생활거점 이미열림/닫힘·무중복지급·무경로변경·고장상태 정산유지 검사 통과. 독립 코드 검토의 고장상태 지적을 수정 후 재검토했다.
- 사용자 저장/백업 JSON10파일을 `artifacts/filled-icons-trip-flow/user-save-snapshot/`에 별도 보관, 실행 후10파일 SHA256 변화0. 일반 저장을 검사에 사용하지 않았다.
- `pwsh -File tools/check_workspace.ps1 -Strict`: exit0, Input issues0. 지도 입력/배포ZIP·맵/차량외형·밸런스·제동/신호/LANv11 코드 변경 없음. 필수 지도 contentHash `35d1bc43d92c996d431acd0464bfc7a175b926cd1e35adb995e99dd7ab91cbac` 유지.

## 실제 실행 화면

[싱글HUD](../ui/2026-11-06-filled-icons-trip-flow/attention-single-1920.png) · [PVP HUD](../ui/2026-11-06-filled-icons-trip-flow/attention-pvp-1280.png) · [승차](../ui/2026-11-06-filled-icons-trip-flow/05c-single-pickup-ready.png) · [요청](../ui/2026-11-06-filled-icons-trip-flow/05-single-passenger-request.png) · [정산](../ui/2026-11-06-filled-icons-trip-flow/06-single-receipt.png) · [정산 후 생활거점](../ui/2026-11-06-filled-icons-trip-flow/06a-receipt-lifehub.png).

## 재현·범위·다음 작업

기본 실행은 `builds/EungyeTaxi/EungyeTaxi.exe` 메뉴에서 은계 싱글/자체도시PVP를 선택한다. 개발 검사는 해당 exe에 `-brightUiCheck -fullUiReview -lifeHubUiReview -brightUiOutput <PNG폴더> -screen-fullscreen 0 -logFile <로그>`를 함께 지정한다. Unity2022.3.62f2 `-batchmode -nographics -quit -buildTarget Win64 -projectPath <저장소>/unity/EungyeTaxi -executeMethod FullUIBuild.Build`로 빌드한다.

로컬 환경: Unity `C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe`, Python `C:/Users/DRI/AppData/Local/Python/pythoncore-3.14-64/python.exe`.

미검증: 사람 장시간 주행·전체3일·다른PC/2인LAN·실물패드. 취객 기상/경유/준비 대기 분기는 기존 모델검사/소스 연결을 확인했으나 이번 Player에서 전부 연속 재현하지 않았다. 다음은 이러한 특수 승객 분기와 도착 직전 안내의 실제 주행 수용 점검이다. 맵·차량 아트는 마지막 별도 검토다.

## 게시

소스 커밋 후 실제 hash로 배포 manifest를 생성하고 전수 검증한 뒤 실행본과 함께 main에 게시한다. 실제 hash/게시 결과는 아래 영수증에 덧붙인다.

### 배포 검증

실제 소스 커밋 `04e2e878c3c9505fffbf12c39eb475f2a47e8f67`을 `git log -1 --format=%H -- <본 기록>`으로 확인했다. 이 hash로 manifest를 재생성했으며 통합/PVP `VerifyOnly` 모두 exit0: **141파일 / 124,559,137바이트** SHA256/크기 전수 일치. 실행본은 위 최종 빌드 결과다.
