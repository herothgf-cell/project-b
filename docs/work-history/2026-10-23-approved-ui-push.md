# 승인 UI와 실행파일 메인 배포

- 요청: 승인 UI 변경과 은계 실행 폴더를 main에 푸시.
- 구현 소스: 50e1bd6, 배포 커밋: c8c2eda. 운전 편의/식당 메뉴 선행 커밋도 포함.
- 원격 fetch 후 main은 4 ahead / 0 behind. git push origin main 종료0: 037b066..c8c2eda.
- 배포 manifest VerifyOnly 종료0: 128파일 / 84,903,514바이트 SHA256 일치. 빌드/자동 검사/렌더 검증은 [UI 구현 이력](2026-10-23-approved-cockpit-map-ui.md) 참조. 이번 작업에서는 재빌드하지 않음.
- 실행: builds/EungyeTaxi/EungyeTaxi.exe. 다른 PC에서는 main을 pull한 뒤 실행 폴더 전체를 유지하여 실행.
- 지도 입력3개/지도 ZIP 변경 없음. 기존 README, LATEST의 별도 문서 편집과 미추적 파일은 보존하고 배포 커밋에서 제외.
- 남은 확인: 직접 마우스 조작, 모든 승객 대화 화면, 전체3일 캠페인 수동 플레이.
