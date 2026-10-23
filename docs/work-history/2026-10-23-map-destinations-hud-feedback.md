# HUD 피드백·지도 시설/목적지 선택

## 요구사항과 구현
- 도움말 중앙 배치(속도계와 분리), 상단 정보→F8 운행 기록, 우하단 기록 버튼 제거. 만족도 얼굴/운행 점수 방패 아이콘+게이지+숫자.
- 위치 정보의 중첩 ModalWindow를 단일 지도 창 내부 패널로 변경. 좌표·방향·교차로/복사/닫기 유지, 실제 렌더에서 텍스트 확인.
- 미니맵/M 시설 아이콘 클릭→우측 상세(식당 메뉴·주유·휴식·주차 조건)→입구까지 길안내. 일반 지도 클릭도 인근 연결 차로를 선택. M 드래그는 위치선택과 구분.
- 호출은 승차→하차 경로 미리보기. 수락 전 배차/잔액/현재 내비 변경 없음. 승객 운송 중 시설 길안내는 차단.
- 도로상황/법규 기본OFF. 시설ON. 두 지도 공유. 경로 혼잡색은 필터와 별개로 같은 방향의 RoadConditions 추가 지연에 따라 초록(<2초), 주황(2~6초 미만), 빨강(6초 이상). 외부 실시간 정보 아님.
- 시설 목록/경로 색 1초 캐시. 목적지 선택시 인근 35m/최대24개 차로에서 연결가능 경로 탐색. 선택한 차로 방향을 실제 길안내에도 보존.
- 문제 수정: 촘촘한 경로의 외곽선이 색을 덮던 현상은 전체 외곽→전체 색상 순서로 변경. 가장 가까운 연결불가 차로로 인한 클릭 실패는 인근 도달가능 차로 탐색으로 보완.

## 변경 파일
- CockpitUI, DriveHUD, HudGlyph, DrivingReadability, FullMapOverlay, MapDestinationUI(신규), RoadNavigation, MapInteractionProbe, 빌드 생성 FirstDrive 장면.
- content-design/13, 04, prototype-scope, 체크리스트, 이력/LATEST, 기본 은계 실행폴더와 배포 manifest.
- 기존 README 및 별도 문서/마인드맵 미커밋 변경 보존. 지도 입력/ZIP/프로젝트 지도 설정 변경 없음.

## 실제 검증
- PrototypeBuild.BuildHybrid 종료0: artifacts/map-destination-final-build.log (PROTOTYPE_BUILD_OK).
- 실행파일 -hybridV2 -mapInteractionTest -uiCapture 종료0: artifacts/map-destination-final-player.log, 32 checks ok=True. UI 메서드 직접호출 검사이며 OS 마우스 클릭 검사가 아님.
- 1280x720,1600x900,1920x1080 주행/도움말/지도/위치/시설 패널 렌더. artifacts/*-live-*.png. 1280 도움말/주행/위치/시설 직접 이미지 확인. 도움말 속도계 겹침0 자동좌표 검사.
- tools/test_prototype.ps1 종료0: artifacts/map-destination-model-tests.log.
- -presentationHudTest -hybridV2 -batchmode -nographics 종료0: artifacts/map-destination-hud-regression.log.
- 초기 지도검사에서 임의지점 연결 실패를 발견했고 보완 후 최종32개 모두 통과. 최종 지도/HUD 로그 FAIL/Exception 없음.

## 미검증/다음 확인
- 직접 마우스 클릭/휠/드래그/복사 버튼 사용감, 모든 시설까지 수동운전, 전체3일 캠페인, 새PC 성능. 이번 작업은 자동 물리 주행 검사가 아님.
- 혼잡은 게임 차량/사건 관측치이며 실제 도로 상황을 보장하지 않음. 호출 미리보기는 기본 경로이고 실제 안내는 선호/교통으로 달라질 수 있음.

## 실행
builds/EungyeTaxi/EungyeTaxi.exe → 미니맵 시설 아이콘 또는 M 지도 시설 클릭 → 우측 정보 → 길안내 시작. 호출번호 선택은 승차~하차 미리보기 후 별도 수락. 일반 빈 지도 클릭은 인근 차로 선택. 상단 정보는 운행 기록, 도움말은 중앙 창. M 위치정보는 좌표 패널.

## 배포 확인
실제 소스 커밋235e086(git log 확인). 배포128파일/106,329,682바이트 SHA256 검증 종료0. distribution/EungyeTaxi-build-manifest.json은 해당 실제 소스 및 이번 검증 범위를 기록한다. 이번 요청의 변경은 로컬 main에 커밋했고 원격 푸시는 실행하지 않았다.

## 사용자 요청 후 원격 배포
2026-10-23 git fetch 후 2 ahead / 0 behind. git push origin main 종료0(3bcac76..e9abf5c). 소스235e086과 실행파일e9abf5c 원격 배포 완료. 배포128파일/106,329,682바이트 SHA256 재검증 종료0. 기존 README/LATEST의 별도 변경 및 미추적 문서는 포함하지 않았다. 재빌드/새 수동검사는 수행하지 않았고 위 검증 범위가 유지된다.
