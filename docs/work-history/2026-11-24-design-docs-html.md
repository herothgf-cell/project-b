# 2026-11-24 기획서 HTML 보기 (문서만)

## 요구

기존 기획서 md도 [기획서 목록](../content-design/INDEX.md)처럼 HTML 문서로 볼 수 있게 한다(사용자 요청).

## 판단

md 파일을 HTML로 바꿔 없애지는 않았다. 근거는 세 가지다.

- AGENTS.md와 작업 이력 수백 곳이 md 경로로 링크한다.
- 같은 폴더의 다른 작업이 지금도 md를 고친다.
- 작업 규칙이 기획을 md로 관리한다.

그래서 md를 원본으로 두고, 같은 폴더·같은 이름의 `.html`을 생성한다. 대상은 `docs/content-design/`의 md 13개(00~09, README, INDEX→`index.html`, 마인드맵 목차)와 `pvp/` 31개, 모두 44개다. `archive/`는 보관본이라 뺐다.

## 실제 구현

- `tools/render_design_docs.py`(새 파일)는 표준 라이브러리만 쓰는 md→HTML 변환기다(이 PC에 `markdown` 패키지가 없고, 다른 PC에서도 설치 없이 돌게 함).
  - 지원 문법: 제목(GitHub와 같은 앵커·중복 번호), 표(정렬 포함), 중첩 목록·번호 목록·체크 목록, 코드 블록, 인용, 가로줄, 이미지, `<a id>` 같은 HTML 줄, 굵게·기울임·취소선·코드·링크.
  - 다른 기획서 md로 가는 링크는 `.html`로 바꾼다. 작업 이력 등 HTML이 없는 md 링크는 그대로 둔다.
  - 페이지 구성: 위 경로(기획서 목록 · 기획 README · PvP 목록 · 원본 md), 머리말 상자, 목차(h2·h3), 본문, 번호 문서의 이전/다음 이동. 밝은/어두운 화면을 따른다.
  - INDEX는 기존처럼 상태 배지와 상태별 필터를 단다.
  - `--rev HEAD`는 커밋된 md로 만든다. 이때 커밋된 HTML에 미커밋 변경이 있으면 건너뛰고, `--force`면 덮어쓴다.
- `tools/render_design_index.py`는 새 생성기를 부르는 얇은 래퍼로 바꿨다(이전 명령 호환).
- `docs/content-design/README.md`에 "HTML로 읽기" 안내를 넣었다.

## 검증

- 44개 생성. 생성된 HTML 전체 링크·이미지 2,570개 중 없는 파일 0건, 없는 앵커 0건(스크립트 대조).
- 본문에 md 기호(`](`, `**`, 표 줄, 목록 기호)가 남은 곳을 찾는 검사에서 생성기 문제 두 가지를 찾아 고쳤다. 하나는 문단 바로 다음 줄의 목록이었고, 다른 하나는 링크 안의 링크(GitHub처럼 바깥 괄호를 글자로 둠)였다. 남은 한 곳은 원본 md 쪽 문제다: `pvp/30-native-lan.md` 80행이 `ative-city-lan-v17**`로 시작해 굵게 표시가 깨져 있다.
- 앱 내 브라우저로 02·08·index를 열어 보았다. 표 39개·목차가 정상이었고, INDEX의 "미결" 필터는 31행을 보였다.

## 미검증 / 남은 것

- 44개 전부를 눈으로 보지는 않았다. 위 자동 검사와 세 문서 화면만 확인했다.
- md를 고친 작업이 HTML을 다시 만들지 않으면 둘이 어긋난다. 지금은 README 안내와 각 페이지 아래 문구로만 알린다. 커밋 전 자동 생성(훅 등)은 넣지 않았다.
- `archive/`, `road-survey/`, `docs/ui/`, `proposals/`의 md는 대상이 아니다.

## 실행·재현

- 열람: `docs/content-design/index.html`에서 시작한다.
- 생성: `python tools/render_design_docs.py` (작업 폴더 md), `python tools/render_design_docs.py --rev HEAD` (커밋된 md).
