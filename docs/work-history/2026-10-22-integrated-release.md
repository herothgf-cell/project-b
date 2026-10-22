# 2026-10-22 은계 통합 빌드 배포

## 요청·반영 범위

사용자 요청으로 대기 중이던 휴식 주차구역·하루20분 호출 접근 보완과 차로별 회전 설정 기반을 통합 빌드한다. 런타임 소스 기준은 14fd920d7a92cbd559f866eae16336092292c515. 기본 실행은 builds/EungyeTaxi/EungyeTaxi.exe, PrototypeBuild.BuildHybrid, 은계 일반 영업이다. 별도 승객 효과 기획/라이선스 변경은 커밋에 섞지 않는다.

## 실제 검증

- tools/test_prototype.ps1: 모델21묶음 종료0.
- tools/check_workspace.ps1 -Strict: 지도 입력3개 오류0. 입력/ZIP 변경 없음.
- 통합 BuildHybrid: artifacts/integrated-release-final-build.log, 종료0. 공유 도로288쌍·하이브리드1765 edges/132운송쌍·휴식 시설 검사 포함.
- 기본 exe -batchmode -nographics -mapIdentityCheck: artifacts/integrated-map-final-check.log, 종료0. 지역 옵션 없이 은계2100×1750, 노면1669개/승하차12/주유소3/휴식 주차1 확인.
- 최초 지도 검사는 추가 노면768개를 집계하지 않은 옛 기준으로 실패했다. MapIdentityProbe에서 CorridorPolish.ribbons와 휴식 구역 존재를 검증하도록 보완하고 재빌드 후 통과. 게임 지도/운전 동작은 변경하지 않았다.
- 배포 실행 폴더128파일 크기·SHA256을 다시 생성하고 전부 대조했다. manifest의 로컬 대기/미커밋 소스 표시 제거, 실제 소스 커밋 기록.
- 직전 연속 자동 물리 하루(48대/운송·식사·주유·수면/위반0)는 2026-10-22-day-access-balance.md 참조. 이번 배포에서는 모델·통합 빌드·기본 실행 회귀를 수행했으며 하루 전체/수동 주행은 반복하지 않았다.

## 변경 파일·남은 작업

MapIdentityProbe.cs, 빌드 생성 FirstDrive.unity, builds/EungyeTaxi 전체, distribution/EungyeTaxi-build-manifest.json, 체크리스트·LATEST·이력. 전체 교차로 실제 차로 방향 확인·다건 운송 수익·수동 하루 P7-06은 미완료. laneTurns는 아직 빈 배열이므로 현장 규제 확정으로 표시하지 않는다.

## 실행·PC 이동

main pull 후 builds/EungyeTaxi/EungyeTaxi.exe를 실행한다. 새 휴식 구역은 동부 주유소 약80m 앞, 파란 주차면에 정차 후 F7 수면. 개발 시 기존 지도 ZIP을 루트에 해제하고 Strict 검사를 실행한다. 배포 커밋은 git log -1 --oneline -- docs/work-history/2026-10-22-integrated-release.md로 확인한다.
