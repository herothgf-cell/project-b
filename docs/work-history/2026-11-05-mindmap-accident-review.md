# 마인드맵 정리·미구현·사고 검토

## 요구와 실제 작업

마인드맵과 미구현 사항을 정리하고 사고 기획의 프로토 도입 방향을 간단히 안내해 달라는 요청. 최신 이력·체크리스트·범위·담당 기획과 충돌/보행자/승객 코드를 대조했다. 기존 내구도·수리·견인을 구현으로, 대인 사건·보험·과실을 미구현으로 구분했다. 사고 결과 전달/간단한 처리/전체 확장 중 간단한 처리를 제안했으며 상세 기획 승인·런타임 적용은 아니다.

## 변경 파일

- `docs/content-design/택시 시뮬레이션.mmst`, `택시 시뮬레이션-목차.md`: 신호·조향/제동·내비·사고 검토 제목4개 갱신. 노드 구조/단계/접힘/메모 유지.
- `docs/content-design/06-mindmap-review.md`: 최신 요약 연결·과거 메모의 날짜 경계.
- `docs/future-design/2026-11-05-mindmap-and-accidents.md`: 가지별 현황, 미구현/검증 잔여, 사고 코드 근거/제안/결정사항.
- `docs/future-design/accidents-and-repairs.md`, `docs/prototype-development-checklist.md`, `docs/work-history/LATEST.md`: 제안 연결·이력 인계.

## 검증·한계

PowerShell JSON 파싱으로 HEAD 대비 노드ID/순서/접힘/task/메모/링크 불변과 제목4개만 변경한 것을 검사한다. `git diff --check`로 공백 검사. 실제 결과는 아래 영수증에 남긴다.

기본 Python/py 실행은 WindowsApps 별칭 접근 실패로 종료1이므로 기존 Python 검증기를 실행했다고 표시하지 않는다. PowerShell 구조 비교로 대체했다. 원본 앱 화면 검사·빌드·위치 제어·자동 물리 주행·수동 플레이·전체 캠페인 검사 없음. 기존 미추적8항목 보존.

## 실행·다음 작업·입력

마인드맵은 `docs/content-design/택시 시뮬레이션.mmst`, 설명은 위 요약 문서에서 확인한다. 기본 실행 `builds/EungyeTaxi/EungyeTaxi.exe`, `PrototypeBuild.BuildHybrid`, 은계 싱글/A2 PVP/LAN v3 유지. 실행본·manifest·지도·ZIP·저장 변경 없음. 필수 로컬 입력은 직전 시작 메뉴 이력 Strict issues0 인용이며 이번 재검사 없음.

다음은 사고 방향·보행자 접촉 시 운송 중단 여부 확정과 상세 기획이다. 커밋 확인: `git log -1 --oneline -- docs/work-history/2026-11-05-mindmap-accident-review.md`.

## 검증 영수증

PowerShell 구조 비교 exit0: nodes=340, topicChanges=4, allOtherFieldsPreserved=True. 구현50%178/기획완료25%0/기획필요0%53. 첫 비교에서 메모 안의 동일 문구까지 치환된 것을 발견해 제목 필드만 변경하도록 수정 후 재검사 통과. git diff --check exit0. 런타임 검증은 미수행.

