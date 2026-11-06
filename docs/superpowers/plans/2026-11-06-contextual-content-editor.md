# 콘텐츠 설정: 게임 상황별 연결 편집

## 사용자 요구와 설계
승객 요청/대사/선택지/효과를 한 상황 안에서 이해하고 수정한다. 참/거짓 표현과 숫자 범위의 오해를 없앤다. 법규는 상황·조건·법규 점수와 운전 만족도 결과를 함께 보여준다. 기존 프로필 ID, 저장, 검증, 적용 및 게임 동작은 유지한다.

요청 하나당 목록 한 줄. 상세는 등장 설정 → 승객 말풍선 → 선택지별 문구/실제 효과 → 공유 설정 순서다. 한국어 문구를 직접 편집하며 다른 언어 로케일 추가는 현재 게임에 없으므로 제공하지 않는다. 기본 후보군 추첨 비중은 실제 전체 등장 확률과 구분한다. 분실물은 별도 사건이다. 공통 효과가 여러 선택지에서 쓰이면 같은 프로필 값을 편집한다.

## 고정 인터페이스
`public/content-model.js`: classic script `globalThis.ContentModel` exposes `build(catalog)` and `weightInfo(entity, profile, catalog)`.

build returns array of `{id, group, title, summary, kind, fieldIds, sections, notes, links}`. sections are `{title, description?, kind?, fieldIds, cards?}`. Cards are `{title, description?, fieldIds, notes?}`. All text plain strings. kind request/dialogue/effect/law/common/reference/field. request has `requestKind`; optional `enabledId`, `weightId`, `promptId`. links array of entity IDs. Every catalog entry covered by an entity. References remain individually browseable. FieldIds include all linked settings for searching/filtering; duplicates reference the same profile value.

weightInfo returns `{text, explanation}`. Equal weights are equal only within one eligible draw, not actual overall spawn rate. LostItem separate fixed 10% conditional postride event. Other constraints and representative passenger binding explained.

Renderer preserves fallback flat field rendering if ContentModel absent (existing minimal regression harness). Real app loads model before app.js. Grouped renderer uses shared existing profile object, existing management controls unchanged. Fields use textContent; booleans use 사용/사용 안함; numeric validation label 입력 가능한 값; no numeric range for bool/text. Optional technical details. Input updates shared sibling editors and computed weight summary without losing caret.

## 실행 순서
1. Pure semantic model + behavior mapping, tests before implementation. Own files: content-model.js/model.test.mjs.
2. Contextual list/detail renderer + CSS + index route. Own files: app.js/style.css/index.html/server.mjs, contextual UI test.
3. Integrate, run all Node tests, actual browser review (Quiet/Food/Law/shared settings, filtering/input/scroll), fix review findings.
4. Copy only intended source files into current C:/git/project-b, preserve user profiles/builds/untracked experiments. Record guide/checklist/history and actual verification. Commit only task files; no push.

## 작업 영역과 판단
Current worktree contains stale uncommitted previous implementation. Canonical source C:/git/project-b clean tracked at 90fadf7. Develop in ignored artifacts/2026-11-06-editor-context copied from canonical; integration explicitly scoped with elevated writes. No Unity/gameplay/default/catalog/build changes. No new dependencies.

## 검증
Node existing codec/server/security/async edit tests; new semantic coverage and weighted share tests; DOM linked editors and type-specific labels; browser 1280×720 and narrow viewport. Distribution manifest verify confirms executable untouched; automated driving/manual gameplay/full campaign not rerun for this UI-only change.
