# 2026-11-13 마인드맵 개요화·세부 기획 이관

## 요구사항과 실제 변경

승객 유형·개별 법규·너무 상세한 항목으로 읽기 어려운 마인드맵을 핵심 구조로 정리하고, 세부 내용은 기획문서 링크로 제공한다.

- `docs/content-design/택시 시뮬레이션.mmst`: 378→37노드, 루트 아래 최대 3단계. 현재 플레이 / 성장·후속 구상 / 기획·검증 문서. 개요에 완료율을 집계하지 않는다.
- 같은 폴더의 `택시 시뮬레이션-목차.md`: 마인드맵과 동일 구조, 로컬 상대 링크와 앱의 Detail → Links 이용 안내.
- `24-passenger-detail-index.md`, `25-driving-rule-detail-index.md`, `26-system-detail-index.md`, `27-future-detail-index.md`: 기존 루트 외 377항목을 분야별로 모두 이관. 담당 기획 링크를 우선 배치하고 옛 항목명·진척도는 당시 기록임을 명시. 최신 승객 표와 기획22·18을 연결해 과거 숫자/후속 분류와 구분.
- `docs/design-history/content-design/2026-11-13-mindmap-before-simplify.mmst`: 변경 전 원본 바이트 그대로. 노드 ID·메모·작업 속성·접힘·태그·사용자 링크 보존.
- `docs/content-design/README.md`, `06-mindmap-review.md`, `docs/prototype-development-checklist.md`, `docs/work-history/LATEST.md`: 탐색 경로·앞으로 세부 가지를 늘리지 않는 기준·이번 검증 범위.
- `tools/simplify_content_mindmap.cjs`: 일회성 이관과 `--check` 검사. 보존본이 있으면 재이관을 거부해 원본 덮어쓰기 방지. 과거 마인드맵 업데이트 스크립트는 구 구조 전용이다.

## 수행한 검증과 결과

- 최신 이력·체크리스트·프로토 범위·기획01/22·승객 현황과 코드의 `ContentTuning.cs`, `DrivingLaw.cs` 관련 정의를 대조했다. 게임 규칙이나 배점 변경은 없다.
- `node tools/simplify_content_mindmap.cjs`: exit 0. 378→37노드, 최대3단계, 개요 링크50개 대상 파일 존재, 기존377항목 각각 한 번 이관, 목차 항목 일치.
- `node tools/simplify_content_mindmap.cjs --check`: exit 0, 동일 검사와 목차/세부 문서의 상대 링크 675개 대상 파일 존재 확인. 이관 중 발견한 기존 상대 경로 `work-history/2026-11-06-drunk-settings.md`는 `../work-history/…`로 교정했다.
- `git diff --check`: exit 0. 체크리스트 끝의 추가 빈 줄을 정리한 뒤 통과.
- 원본 SHA256: `85e4e67a8574570bc028bd12748ca8667adf0d55ebb97ba598e88a4650a61a3a`.
- Python 실행 별칭은 접근 오류(exit 1)로 사용할 수 없어 설치된 Node.js로 처리했다. 환경 설치/변경 없음.

## 미검증·남은 항목

- 네이티브 마인드맵 앱의 실제 배치와 링크 클릭은 미검증. 구조 검사와 구분한다.
- 앱은 기존 GitHub main 링크 방식을 유지한다. 새 문서의 원격 링크는 push 후 반영되며, 로컬에서는 목차의 상대 링크로 읽는다. 이번 작업은 로컬 커밋까지이며 push는 수행하지 않는다.
- 위치 제어·자동 물리 주행·사람 수동 플레이·전체 캠페인·Unity 빌드·Strict 지도 입력/배포 manifest 검사는 수행하지 않았다. 문서만 변경했다.
- 기존 Unity 씬·설정, 빌드/제안서/도로 조사 파일과 동시 작업의 체크리스트·LATEST 도로 조사 문구를 보존하고 이 커밋에서 제외한다.

## 실행·재현과 현재 기준

저장소 루트에서 `node tools/simplify_content_mindmap.cjs --check`. 마인드맵 또는 목차를 열고 승객·법규 노드의 Detail → Links에서 세부 문서를 확인한다. 원본 상세를 비교할 때는 보존본을 연다.

배포 기준은 은계 r19 / A2·LAN v19, `builds/EungyeTaxi/EungyeTaxi.exe`, `PrototypeBuild.BuildHybrid`. 필수 입력은 직전 이력의 정식 지도·Strict 문제0 상태를 인용하며 이번 작업에서는 재검사하지 않았다. 실제 커밋은 `git log -1 --oneline -- docs/work-history/2026-11-13-mindmap-simplification.md`로 확인한다.
