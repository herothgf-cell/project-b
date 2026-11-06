# Passenger Override Implementation Plan

> **For agentic workers:** Use superpowers:subagent-driven-development or executing-plans. Continue authorized work without repeated permission gates.

**Goal:** 프리셋별 대사·사건 확률·효과 수치를 툴과 실제 운송에서 안전하게 분리한다.
**Architecture:** version2 sparse override rows + catalog validation + per-instance resolver; shared values inherited, snapshots deep copied.
**Tech Stack:** existing vanilla JS/Node, Unity C#, PowerShell test/build.
**Spec:** docs/superpowers/specs/2026-11-07-passenger-overrides-design.md

## Global Constraints
No global profile mutation; old profile/hash/save semantics preserved; no unrelated user file staging. version1 strict old fields, version2 adds required overrides array;32 presets,128 overrides,32000 chars,240KiB limits. Reuse existing codex/passenger-composition workspace and Unity inputs.

## Review Focus
Explicit zero vs inheritance; removed trait invalid overrides; interleaved passengers/no leaks; saved ChoiceEffectDefinition validation; preview vs actual runtime consistency.

## Tasks
- [x] 1 Runtime: failing pure tests, version2 parser/allowlist/resolver, selected preset copy/restore, behavior/drunk/request/choice effect consumers and actual Player fixture. Own C# and pure tests.
- [x] 2 Tool: failing Node tests, matching validator, inherited/effective/override panel, migration/reset/duplicate/invalid trait feedback, fixtures/examples. Own JS/Node/config.
- [x] 3 Review: independent design and final diff review, meaningful regressions and UI capture.
- [x] 4 Delivery: detailed design/effect examples/screenshot, checklist/scope/history, unified build + manifests and local commits. Preserve user profiles/saves.

## Progress
2026-11-07 baseline df7ef19; existing untracked license/mindmap/history files preserved. Existing implementation branch continued because this is its direct extension and Unity local inputs/build paths are already prepared. No second Unity project/worktree needed.

2026-11-07: runtime42/Node80/Player57/첫LAN41+52. P2경유문구·resolver캐시 수정 재검토 승인, 최종BuildHybrid exit0. 상세 범위/baseline4실패는 작업이력 참조. 출하 manifest·로컬커밋 진행.

출하: 소스c1ffc29, 통합/PVP141파일132,060,084바이트 전수검증 exit0. 실제 자기포함 배포 커밋은 작업이력 git log로 조회.
