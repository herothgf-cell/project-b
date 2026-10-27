# 현재 기획 최신화·행동 영향표·시스템 구조 정리

## 요구사항과 범위

사용자 요청: 현재 구현 기준으로 기획서 최신화, 히스토리 별도 분리, 시뮬레이션 요소별 행동 결과/영향을 표로 정리, 콘텐츠/시스템 구성 설명, main 커밋·푸시.

문서만 수정한다. 게임 소스·수치·지도·실행 폴더·배포 manifest·마인드맵 유지. 시작 main e46ba9a, 현재 게임 소스 e9c6fae/배포57eb4c4를 기준으로 조사했다.

## 실제 변경

| 영역 | 변경 |
|---|---|
| 기획 입구 | README/00에 P11/P12 구현·P13 대기 제공/대체 미구현 구분, 과거 신규 개발 잔여 문구 제거 |
| 행동·영향 통합 | 18에 점수 지표·호출/운송·법규/M·회복·요청·거점·공복/피로·취객/청결·도시/점유·시간/저장 표 |
| 시스템 구성 | 19에 단계/데이터 흐름·카탈로그·코드 소유권·상태 생명주기·지도/저장/배포·문서 소유권 |
| 상세 현행화 | 01 PS08 영업 팝업 제외/연습 유지,04 요청/기한/UI/피로,06 조향52도·40%,07 안전 취소/당일 마감,09 코스/선택/요청,10 눈감김/최종 차단,11/13/16/17 최신 범위 |
| 기획 이력 | design-decisions에 결정 흐름·미구현 제안·마인드맵 검토안. before-refresh에 직전12문서 보존·원래 위치 기준 링크 재연결 |
| 인계/추적 | scope/checklist의 중복 날짜 덧붙임·PS08 설명 정리. LATEST는 현재 상태/다음 작업, 직전 전체 인계는 별도 파일 보존 |

## 코드 대조

PassengerRequest/Schedule/UI/Observation, PassengerDemandPolicy, OperationMetrics, OneDayOfferPolicy, StopOccupancyPolicy/System, DriverCondition/Drowsiness, EconomyLedger, CampaignAccount, ScenarioChoiceSituation 등을 읽어 수치·예외·미제공 범위를 대조했다. 새 법규 해석·의학 기준·현장 지도 조사는 하지 않았다.

## 검증·결과

- 임시 `artifacts/check_design_docs.ps1`: Markdown 파일 경로·현재 문서 앵커·신규18/19 표 열 수 검사. 첫 검사39파일/637링크/25앵커 중4건 발견: 개편01의 기존 앵커2개와 생성 전 본 이력2참조. 호환 앵커/본 이력 생성으로 보완.
- 최종 `pwsh -File artifacts/check_design_docs.ps1` 종료0: 40파일/641로컬 링크/25앵커, 신규18/19 표 열 수 오류0. 과거 원문 스냅샷은 파일 링크만 검사하며 원래 버전의 앵커 완전성까지 현재 문서 기준으로 판정하지 않는다.
- `git diff --check` 종료0, `git diff --quiet -- unity config builds distribution` 종료0으로 런타임·지도·배포 변경 없음 확인. `update_distribution_manifest.ps1 -VerifyOnly` 종료0: 130파일/105,870,100바이트 전체해시 일치.
- 마인드맵 SHA256 `F1951492F9C5A13941E60A17D92EB31B1A7471FB8ABA7996D47A0C2E02F00937` 유지. 같은 후보의 요청·경로형 요청 최대1개·P13 실제 대기/해소/보정·졸음/마감·청결 정책을 코드와 대조했다.
- 기획 변경 커밋 `5a70eb47df18499959376325629132d032e20ddc`를 main에 커밋, `git push origin main` 종료0. `git ls-remote origin refs/heads/main`과 로컬 HEAD 일치 확인. 본 결과 기록은 후속 문서 커밋으로 push한다.
- 문서만 변경하므로 Unity/모델/물리/수동 캠페인 재검사·빌드는 실행하지 않는다. 직전 게임 검증은 이전 배포 이력의 범위만 유효하다.
- 사용자 .mmst·개인 메모·기존 untracked 보존/stage 제외. 기본 배포130파일/105,870,100바이트는 이전 manifest 기준 유지.

## 읽는 위치·남은 범위

[현재 기획 입구](../content-design/README.md) → [행동 결과/영향](../content-design/18-simulation-action-results.md) → [콘텐츠·시스템](../content-design/19-content-system-structure.md). 과거 정책은 [기획 변경 이력](../design-history/content-design/2026-10-27-design-decisions.md).

사람의20분/3일 경제·조작/빈도 체감, 다른PC/DPI·패드·장시간 성능, 근거 있는 지도 제보 보완, 안전 후보 확보 후 P13 대체 이동은 별도다. .mmst는 사용자 검토 대상. 실제 본 이력 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-27-design-refresh.md`로 확인한다.
