# 호출·시설 함께 보기와 접근/운송 경로 구분

- 요청: 미니맵 호출을 시설과 함께 표시, 승차 접근과 승객 운송 경로 구분, 식당 터치 즉시 경로 표시.
- 구현: 미니맵 호출 보기 버튼/외곽 호출번호, 시설 선택 중 M 호출핀 유지. 차량→승차 실선(검정 외곽), 승차→하차 점선(분홍 외곽), 혼잡색 유지. 점선은 세계거리16m 표시/10m 공백으로 분할해 작은 도로 조각에서도 구분. ToPickup 두 구간/OnTrip 운송 구간만 표시.
- 식당/시설 선택 즉시 기존 PreviewTo로 경로를 계산하고 전체 경로에 맞춰 줌/중심을 조정. 실제 길안내 시작은 명시적 버튼, 운송 중 목적지 보호 유지. 호출도 두 경로를 포함해 자동 맞춤. 비용/배차/식사 조건 변경 없음.
- 파일: DrivingReadability, FullMapOverlay, MapDestinationUI, MapInteractionProbe, 생성 FirstDrive 장면, 콘텐츠04/13, 체크리스트/이력/LATEST, 실행폴더/manifest.
- 검증: PrototypeBuild.BuildHybrid 종료0(artifacts/two-leg-final-build.log). -hybridV2 -mapInteractionTest -uiCapture 종료0, MAP_INTERACTION_CHECKS 32 ok=True(artifacts/two-leg-final-player.log), FAIL/Exception 없음. 두 경로가 모두 존재하며 선택만으로 내비/요금/운송 상태를 바꾸지 않는지 검사. 세 해상도 렌더; 1280 M 접근/운송 화면과 1920 호출 선택 화면 직접 이미지 확인. artifacts/two-leg-call-preview.png.
- 자동 메서드/화면 렌더 검사이며 실제 OS 클릭·터치 검사/자동 물리 주행/전체3일 검사는 아님. 직접 클릭감/외곽 번호 밀집과 시설까지 수동 주행은 다음 확인.
- 지도 입력3개/ZIP/프로젝트 지도 설정 변경 없음. README/기존 LATEST 별도 수정 및 미추적 문서 보존.
- 실행: builds/EungyeTaxi/EungyeTaxi.exe → 미니맵 호출 보기 또는 번호 선택 → 실선/점선 확인. 식당 아이콘 클릭 → 정보와 입구까지 경로 즉시 표시 → 길안내 시작.

배포: 실제 소스 커밋 d635445. manifest 128파일/106,331,730바이트 SHA256 검증 종료0. 로컬 커밋/빌드까지 수행, 원격 푸시는 미실행.

## 후속 요청에 따른 배포 완료
2026-10-23 사용자 푸시/배포 요청: fetch 후 2 ahead/0 behind. git push origin main 종료0(2c71001..c856c32). 코드d635445/실행폴더c856c32 원격 배포 완료. 배포128파일/106,331,730바이트 SHA256 재검증 종료0. 다른 PC는 main pull 후 builds/EungyeTaxi/EungyeTaxi.exe 실행. 입력ZIP 변경 없음. 새 수동 검사는 없으며 위 미검증 범위 유지. 관련 없는 작업 변경 보존.
