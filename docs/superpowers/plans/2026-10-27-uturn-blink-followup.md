# U-turn signal mounting and staged drowsy eyelids

> Execute inline with executing-plans; user delegated detailed values and explicitly asks main merge/push. Preserve existing in-place branch and user's mindmap. Minimal RED/GREEN then final grouped checks.

**Goal/spec:** Latest screenshot and exact pose(807.6996,607.1715),yaw3.280868,J8066_6364: move the actual U-turn sign above matching signal. Early drowsiness briefly closes/reopens repeatedly, then smoothly closes completely.
**Architecture:** RoadMapFacilities is the real U-turn renderer, independent of RoadSignage. Mount only conditional U-turn signs on exact junction/axis/sign native lamp housing, with no extra roadside pole. Keep document/rules/map hashes unchanged. Drowsiness.VisionLoss becomes deterministic function of persisted active exposure; preserve120s cancellation and180s blackout/day-close policy.
**Tech:** Unity2022.3/C#/PowerShell.

- [x] 1. RED exact authored U-turn rendering fixture and early close/reopen curve. Inspect actual native signal pose, no nearest unrelated junction fallback.
- [x] 2. Mount conditional U-turn signs above exact matching vehicle signal; keep unmapped/editor-preview fallback explicit and nonconditional signs unchanged. Early blink cycles at30,50,70,90,110seconds; duration grows.120~180seconds smooth permanent closure. Existing toast/Zzzz/day close and paused/saved exposure remain.
- [x] 3. Focused GREEN + full model27, final grouped Editor/persistence/signs/previous direct-turn/exit checks, one independent review, BuildHybrid, visible Player capture at exact user pose + sampled blink frames and complete blackout cancellation. Avoid repeating full suites per edit.
- [ ] 4. Update design06/10/checklist/history/LATEST, commit source; build/manifest all130files; preserve user changes; fetch main, merge locally and push, verify remote SHA and record actual result.

## Review focus

- A sign must match junction AND approach axis/sign; no wrong-side or roadside duplicate.
- Different native permissive/shared housings or existing upper signs must not overlap the U-turn panel.
- Unmapped sign/editor preview must not disappear silently; no fake field-rule changes.
- Blink phase must not depend on frame count, restart at reload, or progress during paused menus.
-180s blackout must remain complete and irreversible until actual recovery;120s cancellation unchanged.

## Ledger

- Base83ddd28 on codex/drowsy-map-handling-20261027; user .mmst/untracked files pre-existing. No map/input/ZIP changes intended.
- Prior RoadSignage fix never touched RoadMapFacilities U-turn signs: trace confirmed, previous repeated feedback is not a new permissive-sign issue.
- Ruling: times/curve are delegated game balance, not a medical/physiological model. Short repeated early closures followed by smooth final shutdown, no new fatigue thresholds.
