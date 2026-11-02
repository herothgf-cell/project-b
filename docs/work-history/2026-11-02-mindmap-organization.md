# 기존 마인드맵 원본 정리

## 요청·실제 변경

기존 마인드맵을 한 번 정리해 달라는 요청에 따라 `docs/content-design/택시 시뮬레이션.mmst`를 직접 수정했다. 게임 기능·수치·실행파일 변경은 없다.

| 구분 | 결과 |
|---|---|
| 현재 플레이 기획 | 기존 도메인을 모으고 통합 exe/은계 싱글/자체 도시 나1+봇3, 구현된 내구도·수리·견인을 반영 |
| 승객 | 취객을 진상 아래에서 분리. 일반+특수11유형/34변주·현재 대화 효과와 배정 요약 연결 |
| 기존 아이디어·후속 | 차량/용품, 생활/보험, 환경/교통/사고/법규, 승객/예약, 원격 모드로 묶고 원안 보존 |
| 추가 제안 | 앞선 검토안9개에 기존/확장/신규 출처와 추천 순서 표시. 채택·구현 지시 아님 |
| 제작·검토 참고 | 개발 검사·Unity 작업·조작감 메모 분리 |

현재 봇 모드와 기존 원격 사용자 PVP 원안을 구분했다. 보험/현장 구조/원격4유저 수용과 생활 수치 후보를 구현 완료로 바꾸지 않았다. 옛 수리비 노드는 현재 차량 관리로 이동하고 기타 사고 아이디어는 후속에 보존했다. 규칙 상세는 담당 기획 Detail-Link로 연결하고 기존 작업 진척도를 임의 완료 처리하지 않았다.

## 보관·파일

- 이번 수정 전 파일을 바이트 그대로 `docs/design-history/content-design/2026-11-02-mindmap-before-organize.mmst`에 보관. SHA256 `7c4e5d0c86970d633040a4cf77b4d9511af2c3b5aabe6a636ba3622e850b6666`.
- 기존 2026-10-27 보관본도 유지한다.
- `docs/content-design/택시 시뮬레이션-목차.md`: 전용 도구 없이 읽는 가지 목차.
- `docs/content-design/06-mindmap-review.md`, README, 체크리스트, LATEST: 현재 배치·모드·정리 범위·파일 접근 안내.
- `tools/organize_content_mindmap.py`: 현재 정리/보존 검사. 구 PowerShell 변환은 정리된 구조를 덮지 않도록 차단하고 `-ValidateOnly`는 유지.

시작 시 기존 LATEST 미커밋 수정과 UI 시안 이력/기타 untracked를 확인했다. LATEST의 해당 작업 내용은 보존하고 이번 항목만 분리해 커밋한다.

## 수행 검증·실행 방법

로컬 Python 환경: `C:/Users/DRI/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`.

1. `python tools/organize_content_mindmap.py`: 종료0. JSON 파싱·중복 ID·원래 ID/작업 체크/사용자 메모/외부 링크 보존,331노드/331개 Detail-Link 파일 존재 확인.
2. `python tools/organize_content_mindmap.py --validate-only`: 종료0, `MINDMAP_ORGANIZED_OK`.
3. `pwsh -File tools/update_content_mindmap.ps1 -ValidateOnly`: 종료0, `MINDMAP_OK nodes=331 links=331 original IDs preserved`.
4. 정리 재실행 종료0, `IDEMPOTENCE_OK map+outline unchanged`. 원본/목차 해시 불변과 정리 전 보관본 SHA256 불변 확인.
5. 변경 Markdown5파일의 상대 링크76개 존재 검사 `DOC_LINKS_OK files=5 links=76`와 `git diff --check` 종료0.

제출 전 cached 검사에서 새 목차의 기존 두 가지 이름에 따라붙은 말미 공백을 발견해 목차 출력에서만 제거했다. 원본 가지 이름·ID는 유지하고 최종 cached 검사를 다시 수행했다. LATEST의 기존 UI 작업 내용은 이번 추가 블록을 제외하면 바이트 그대로임을 `LATEST_ISOLATION_OK`로 확인했다.

네이티브 마인드맵 앱에서 열기/화면 배치 검사는 수행하지 않았다. 처음에는 큰 가지와 현재 도메인을 중심으로 접고 펴서 읽도록 했다. JSON 구조·ID·링크/보관·목차 검사를 네이티브 앱 렌더링 검사로 표시하지 않는다. 게임 빌드·자동 물리 주행·수동 캠페인 검사는 문서 정리 범위가 아니므로 미수행이다.

현재 배포는 통합 `builds/EungyeTaxi/EungyeTaxi.exe`/은계 싱글·자체 도시 PVP/`PrototypeBuild.BuildHybrid` 유지. manifest 소스61c9e98·140파일115,186,267바이트·지도/ZIP 미변경, 필수 입력은 직전 Strict issues0 인계 유지다. 다음은 사용자가 맵을 열어 가지를 살펴본 후 기존 후속/추가 제안의 채택 범위를 정하는 단계다.
