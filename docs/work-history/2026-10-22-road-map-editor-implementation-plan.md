# 도로 편집기 구현 계획 준비

## 요구사항과 현재 결과

사용자가 `docs/content-design/12-road-map-editor.md`를 기준으로 편집기 구현과 사용법 정리를 요청했다. 기존 기획을 구현 기준으로 읽고 현재 코드와 연결 지점을 대조해 `docs/superpowers/plans/2026-10-22-road-map-editor.md`에 E1~E4 구현·검증·사용 안내 계획을 작성했다. **제품 편집기는 아직 미구현이며 이번 작업은 계획 검토 단계다.**

## 확인한 코드와 결정

- `MapCatalog.Load`는 보조자료→ReviewedLaneLayout→CorridorPolish 순서다. 사용자 수정은 기존 검토 차로를 포함한 기준 뒤, 노면/충돌/공유 경로 구성 전에 적용한다.
- `HdEdge.next`와 서비스 접근은 정수 인덱스다. 수정본은 안정적 ID로 저장하고 후보 지도 완성 시 인덱스로 변환한다.
- `TrafficSystem`은 축·진입방향 기반 신호다. 임의 차로 그룹은 점등 외형 외에 NPC·법규·보행 공통 조회를 확장해야 한다.
- 기존 `PracticeSession`은 별도 프로세스로 영업을 격리한다. 지도 시험도 이 방식을 따르되 캠페인/저널 저장을 명시적으로 차단한다.
- 시작 시 README, 체크리스트, 범위, LATEST 및 기획/라이선스 관련 미커밋 변경이 있었다. 사용자 변경을 보존했다.

## 변경 파일

- 새 구현 계획과 이 작업 기록.
- `docs/work-history/LATEST.md` 상단에 현재 계획 상태 추가.

## 검증과 미완료

- 실제 실행: `git status --short` 종료0. 기존 사용자 변경 확인.
- 관련 이력·기획·체크리스트·범위·MapData/MapCatalog/DriveSession/PracticeSession/TrafficSystem/RoadNavigation/PrototypeBuild 코드 읽기 수행.
- 제품 코드·빌드·필수 지도 입력3개·지도 ZIP·배포 manifest 변경 없음. 입력 Strict/테스트/빌드/자동 물리/수동/캠페인 검사를 이번에는 수행하지 않았다.
- 계획 문서의 작업 체크는 모두 미완료다. 아직 실행 가능한 편집기 사용법으로 안내하지 않는다.

## 다음 작업과 실행

계획 검토 후 E1→E2→E3→E4 및 사용 안내를 순차 구현한다. 기본 실행은 기존 `builds/EungyeTaxi/EungyeTaxi.exe`, 은계 BuildHybrid다. 로컬 입력 상태는 기존 인계 기록을 따르며 이번에 재검증하지 않았다.

적용한 writing-plans 스킬은 구현 전에 작성된 계획 검토와 실행 방식 선택을 요구하므로 그 단계에서 대기한다. 제품 구현 완료·테스트 통과·배포 완료로 표시하지 않는다.
