# 2026-11-25 기획서 목록 HTML에서 기획서 HTML로 이동 (문서만)

## 요구

`docs/content-design/index.html`에서 기획서를 열면 md가 열린다. 각 기획서의 HTML로 이동하게 한다(사용자 요청).

## 확인한 원인

- 00~09·pvp 문서로 가는 링크는 생성된 index.html에서 이미 `.html`이었다.
- 기획 위치 칸 가운데 기획서 폴더 밖의 문서는 HTML이 없어 md로 열렸다. 작업 이력의 [후보 4종 상세 기획](2026-11-24-next-candidates-detail-design.md), 보관 37, 제안서·후속 아이디어·이전 기획 README, 기획서 목록 작업 기록이 여기에 해당한다.
- 앱 내 브라우저는 `file:///` HTML을 정적 사본(data:)으로 띄워 상대 링크가 깨진다. 같은 탭에서 예전 index.html이 캐시돼 새 링크 대신 옛 md 링크가 열리는 것도 확인했다.

## 실제 구현

- `tools/render_design_docs.py`가 INDEX의 "기획 위치" 칸이 가리키는 md 중 기획서 폴더 밖의 문서 6개도 같은 이름의 `.html`로 만든다. 이로써 index.html의 기획 위치 링크는 모두 HTML로 간다. 근거 열의 작업 이력 링크는 그대로 md다.
- 개선 필요 표시도 이 문서들에 붙는다(상세 기획 6건, 앵커가 없어 문서 전체 상자).
- 모든 페이지에 `Cache-Control: no-cache` meta를 넣어 다시 만든 뒤 옛 페이지가 보이는 일을 줄였다.
- `.claude/launch.json`에 `design-docs`(`python -m http.server 8765 --directory docs`)를 추가했다. 앱 내 브라우저에서는 이 서버로 `http://localhost:8765/content-design/index.html`을 연다.

## 검증

- `python tools/render_design_docs.py`: 50개 생성.
- 생성 HTML 51개(검사 대상 목록 기준)의 링크 2,867개 중 없는 파일 1건. `archive/37-driver-life-and-business.md`의 `../../08-fun-improvements.md` 링크가 원본 md에서 이미 잘못돼 있다(고치지 않음). 없는 앵커 0건.
- 앱 내 브라우저(로컬 서버)에서 index.html → 08 링크 클릭 → `08-fun-improvements.html`, index.html → 상세 기획 링크 클릭 → `work-history/2026-11-24-next-candidates-detail-design.html`로 이동하는 것을 확인했다.

## 미검증 / 남은 것

- 사용자 PC의 일반 브라우저(file://)에서는 직접 확인하지 않았다. 링크는 상대 경로라 같은 폴더 구조면 동작한다.
- 보관 37 md의 잘못된 상대 링크.

## 실행·재현

- 생성: `python tools/render_design_docs.py`
- 일반 브라우저: `docs/content-design/index.html`을 연다. 앱 내 브라우저: launch 설정 `design-docs`를 시작한 뒤 위 주소를 연다.
