# 호출 유지·접근 경로·가독성·UI 겹침 수정

## 요청과 실제 구현
- 시설 선택 시 M 호출 목록/핀 소실: 기존 우측 패널 교체와 지도 범위 밖 핀 누락이 원인. 시설 상세 위/호출 목록 아래로 동시 구성. 빈차 호출은 화면 밖도 가장자리 번호(실제 위치와 구별 필요)로 유지. 운송 중은 진행 중 운송 안내.
- 접근 실선: ShowCalls가 previewPath만 지우고 선택을 남기던 상태를 수정해 호출 재선택/경로 복구. 하늘색 넓은 외곽선으로 공유 경로에서도 접근 실선 식별. 분홍 점선/혼잡색 유지. 우측 승차 접근 거리 표시.
- 주행·미니맵·M 문구 ‘주변 호출 보기 (M)’ 통일.
- Noto Sans KR Medium/Bold를 Resources에 포함해 다른 PC에서도 일관된 글꼴. 기본 스타일의 패딩/정렬·굵기와 버튼 대비를 조정, 제목/속도/위치정보 높이를 보정. 호출 카드 실제 줄 높이 계산, 긴 시설 설명은 스크롤, 시설 거리와 길안내 버튼은 고정.
- 도움말 및 기본 GUI.Box/Window, 거점/준비/정산/재시작/운송완료/연습결과/승객 말풍선 본문을 불투명하게. 외부 화면을 어둡게 하는 덮개와 비활성 버튼은 구분.
- 운송 중 상황/결과 알림은 CockpitUI 공통 배치 x365/y405, x365/y318 영역. 속도계와 겹치지 않음. 긴 상황 텍스트 스크롤.
- F7 안내는 x365/y200 불투명 패널/밝은 굵은 글자로 이동. 운송 후 거점 선택도 상단 별도 영역. 지도·대화·도움말·재시작·정지 중에는 안내 숨김.

## 글꼴 출처/배포
- 로컬 설치된 원본 NotoSansKR-Medium.ttf / NotoSansKR-Bold.ttf 메타데이터를 읽어 Adobe 2014-2021 및 SIL OFL1.1 확인. 원본을 수정하지 않고 포함.
- 공식 라이선스: https://raw.githubusercontent.com/google/fonts/main/ofl/notosanskr/OFL.txt
- licenses/NotoSansKR-OFL.txt / Resources/Fonts/OFL.txt. PrototypeBuild가 ThirdPartyNotices/NotoSansKR-OFL.txt를 실행 폴더에 복사.
- 다른 진행 중인 라이선스 감사 문서/README/지도 설정은 편집하지 않음.

## 변경 파일
KoreanText, ReadableUITheme(신규), 폰트2종/메타/라이선스, PrototypeBuild, CockpitUI, DriveHUD, DrivingReadability, FullMapOverlay, MapDestinationUI, MapInteractionProbe, ServiceLawZones, ServiceStops, BusinessHUD, OneDayHUD, RideCompletionPanel, PassengerRequestUI, PracticeCoach, DriveNotifications, 생성 FirstDrive 장면. 콘텐츠04/13·범위·체크리스트·이력, 기본 실행폴더/manifest.

## 검증
- PrototypeBuild.BuildHybrid 종료0, PROTOTYPE_BUILD_OK: artifacts/readable-release-build.log.
- 지도/표현 회귀 -hybridV2 -mapInteractionTest -uiCapture: 43검사. 호출 유지·화면 안 핀 위치·목록 복귀 후 실선/운송 경로 보존·글꼴 포함·불투명 표면·3해상도 상황/F7/속도계 사각형 비중첩·기존 지도/목적지/거래 보호 검사.
- tools/test_prototype.ps1 종료0: artifacts/readability-model-tests.log.
- 1280x720 / 1600x900 / 1920x1080 주행/도움말/지도/좌표/시설+호출/상황+F7 렌더. 도움말/시설/두 경로/좌표/거점 및 재시작 팝업 이미지 점검. artifacts/help-live-*.png, facility-calls-*.png, two-leg-call-preview.png, situation-f7-*.png, service-popup.png, restart-popup.png.
- 상황+F7 화면의 상황 문구는 실제 ServiceLawZones.DrawSituationCard를 호출하는 대표 UI 표본이며, 실제 시나리오를 운전해 발생시킨 결과가 아님. 재시작 화면도 표본 상태로 열어 렌더를 확인. 지도/거래 검사는 공개 메서드 호출로 OS 마우스 검사가 아님.

## 남은 확인
- 사람이 직접 클릭/터치·긴 설명 스크롤, 모든 연습/승객 요청/평가 조합, 실제 상황 운전·교통 혼잡·전체3일 캠페인. UI 렌더/사각형 검증을 자동 물리주행이나 수동 플레이로 취급하지 않음.
- 작은 화면에서 번호 핀이 밀집하면 호출 목록으로 선택. 가장자리 번호는 화면 밖의 실제 위치까지 그대로 나타내지는 않음.
- 필수 지도 입력3개/지도ZIP/프로젝트 지도 설정 변경 없음. 다른 미커밋 문서/미추적 파일 보존.

## 실행
builds/EungyeTaxi/EungyeTaxi.exe → M → 식당 등 시설 선택 → 우측 시설 정보 아래 호출 목록 확인 → 호출 선택/주변 호출 보기 (M) 재진입 시 하늘색 접근 실선 확인. 도움말/F7 팝업은 불투명. 운송 중 상황과 거점 안내는 속도계와 별도 공간 사용.

최종 확인: artifacts/readable-release-player.log MAP_INTERACTION_CHECKS 43 ok=True / 프로세스 종료0, FAIL/Exception 없음. 기존 -presentationHudTest -hybridV2 -batchmode -nographics 20 PASS/종료0(artifacts/readable-release-hud.log). 새 글꼴/불투명 패널 적용 후 재시작 팝업 뒤 F7 미표시도 이미지 확인.
