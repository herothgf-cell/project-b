# 2026-11-13 은계 통합 배포 병합

- 사용자 요청 범위는 `herothgf-cell/project-b`의 `main`에 은계 영업 수정, 통합 실행본, 승객 현황 문서를 포함하는 것이다. 원격 관리 툴 변경 `541f27f`에 로컬 승객 현황 `e86b253`, 은계 소스 `2f91166`, 통합 실행본 `d623cd0`이 포함된 이력을 병합했다.
- `docs/work-history/LATEST.md`와 `docs/prototype-development-checklist.md`의 동시 편집을 합쳤다. 기존 사용자 씬과 로컬 프로필, 미추적 문서는 작업 트리에 보존했다.
- `tools/update_distribution_manifest.ps1 -VerifyOnly` 및 `tools/update_pvp_distribution_manifest.ps1 -VerifyOnly` 각각 종료 0. 배포 143파일, 131,600,169바이트. `tools/check_workspace.ps1 -Strict` 종료 0, 필수 지도 입력 문제 0. `node tools/content-editor/choice-catalog.mjs . --verify` 종료 0.
- 은계 r18 / A2 PvP LAN v19. 기본 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`. [은계 변경과 기존 자동 검증](2026-11-13-eungye-drive-feedback.md) 및 [승객 현황 조사](2026-11-13-passenger-inventory.md)를 참고한다. 이번 병합에서는 재빌드, 추가 주행, 전체 캠페인, 다른 PC LAN 검사를 수행하지 않았다.
