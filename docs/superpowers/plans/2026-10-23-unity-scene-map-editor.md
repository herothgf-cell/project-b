# Unity Scene Map Editor Implementation Plan

> Use superpowers:subagent-driven-development for independent session and renderer tasks; parent owns integration/UI and verification.

**Goal:** 실행게임과 같은 정적지도를 Unity에서 직접 조작하고 정식JSON으로 저장한다.
**Architecture:** 객체단위 편집세션 + 실게임 공통기하 렌더러 + Scene뷰 도구/실행카메라 프리뷰. 전체패키지는 열기/정식저장에만 처리.
**Tech Stack:** Unity2022.3.62f2/C#/Editor APIs/PowerShell.
**Spec:** docs/superpowers/specs/2026-10-23-unity-scene-map-editor.md

## Global Constraints

기존지도·사용자변경 유지, 정식JSON/Git재현 유지, runtime렌더와 동일형상/재질, 동기전체복사/저장 드래그금지. 별도Scene 작업공간. 새패키지 의존성 없음.

## Review Focus

1. 실제노면/건물/서비스와 편집화면 불일치.
2. 드래그/Undo에서 전체JSON복사 또는 전체월드 재생성.
3. 저장 충돌, 외부수정, 도메인리로드/창닫기 초안손실.
4. 음수배율/회전/정차점/꼭짓점 변경 후 유효하지 않은 지도 저장.
5. 미저장 사용자Scene·FirstDrive·다른작업 변경 파괴.

## Tasks

- [ ] 1. ProjectMapEditSession/object handles + tests: sparse object Undo, transform/height/vertex/service operations, dirty state, Store save/load/conflict/draft lifecycle. Renderer와 파일공유 없음.
- [ ] 2. ProjectMapAuthoringRenderer + WorldBuilder shared geometry + tests: runtime동일자료, 배치배경/변경청크 갱신, ray pick/bounds, resource cleanup. Session과 파일공유 없음.
- [ ] 3. Unity EditorWindow/SceneView tools: 실제지도 렌더 + camera preview, click/F/W/E/R, 수치/꼭짓점/서비스, 저장·시험·복구. 1/2 interface사용.
- [ ] 4. 독립 리뷰/수정, 자동검사/성능측정/렌더확인, 최신main통합, BuildHybrid/배포manifest, 사용법/체크리스트/이력/커밋·push.

## Ledger

Ruling: 직전 검토 제안을 사용자가 명시적으로 승인하고 실행화면 일치를 강조했으므로 추가 조작안 승인을 요청하지 않고 진행한다.
Ruling: 기존 격리 worktree를 최신main ea5fb1c에서 codex/unity-scene-map-editor로 사용한다. 주 작업폴더의 문서변경은 보존한다.
