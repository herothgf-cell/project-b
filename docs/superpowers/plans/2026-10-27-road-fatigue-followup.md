# Road and fatigue follow-up implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans or superpowers:subagent-driven-development. Independent navigation/condition domains use dispatching-parallel-agents; parent owns road evidence and integration.

**Goal:** Resolve reported turn detours, review the two pictured junctions against Naver street view, and implement sustained fatigue/cleanliness consequences before main integration.
**Architecture:** Preserve directed graph legality and source evidence. Persist condition progression with the existing campaign snapshot; keep emergency recovery distinct from normal rest.
**Tech Stack:** Unity 2022.3/C#, project map JSON, PowerShell validation/build.
**Spec:** User screenshots/request of 2026-10-27; `docs/content-design/06-world-map-and-driving.md`, `10-driver-condition.md`, `15-drunk-passengers-and-cleanliness.md`.

## Global constraints

- Current feature branch/current directory, no mindmap changes.
- Hunger: ideas only, no new debuff implementation.
- Minimal focused RED/GREEN during development; whole regression/build at end.
- No invented real-road facilities or permission changes without observed evidence.
- Preserve existing saves and user changes; main merge/push explicitly requested.

## Review focus

- Save/reload during progressive blackout must not restore clear vision.
- Passenger cancellation pays no fare and happens once, without unsafe moving teleport.
- Menus pause condition/cleanliness; confirmed services account time once.
- Legal same-direction lane changes must not cross solid lines/medians or miss downstream exits.
- Map revision/input hashes, shipped executable and ZIP must remain consistent.

### Task 1: directed turn routes

Files: navigation graph repair/generator, focused Editor tests; project map only if graph source requires it.
Interface: existing directed route search and partial edge distances.
- [x] Reproduce reported J7973 west-to-north / J8066 south-to-east failures in focused test (RED). Initial one-block-north fixture was rejected during location matching.
- [x] Correct supported lane-change connectivity without nearest-lane snapping; verify source/target positions (GREEN).
- [x] Recheck prior two detours and graph integrity (old diagnostic audit still passes; other areas unchanged).

### Task 2: observed junction presentation

Files: applicable map configuration/renderer; `docs/map-observations/2026-10-27-junction-followup.md`.
Interface: source junction IDs/coordinates from Task 1; observed Naver roadview, not screenshots alone.
- [x] Locate pictured junctions, inspect Naver street view and record observed vs estimated/unverified. J5750 exact approach remains user-corrected, not independently surveyed.
- [x] Pin missing median behavior with focused regression before implementation; no confirmed island invented.
- [x] Apply corrections; canonical source changed, required generated input2/ZIP unchanged.

### Task 3: persistent fatigue and cleanliness

Files: DriverCondition/DrowsinessHUD/VehicleCare and existing trip/rest/save integration; design10/15; focused tests.
Interface: campaign snapshots, active elapsed time, cancellation, existing parking rest.
- [x] RED tests for sustained non-reopening vision, once-only passenger cancellation, blackout home confirmation/recovery, persistence, paused/active cleanliness.
- [x] Implement fatigue100 warning0~60s then narrowing, passenger request120s, blackout180s; normal rest100s/-40, confirmed emergency return100s/-20 at existing parking/home surrogate.
- [x] GREEN focused tests; retain normal food/fatigue economics and hunger behavior.

### Task 4: final verification and integration

Files: work history/LATEST/checklist, distribution manifest, full tracked build folder.
- [x] Single integrated Editor/model/Player/save/visual verification; fix discovered important errors. Editor/model25/care13301, Player navigation30/fatigue24/rendered30/passenger201/scenario99/save9/reload23/UI14.
- [x] Fresh whole-change review; record unverified manual/field cases. Important system-brake grading fixed, cancelled-receipt history minor deferred.
- [x] Commit source/docs, build BuildHybrid, verify actual shipped execution and all130 distribution hashes. Source251c219, BuildHybrid0;130files/105789674bytes.
- [x] Fetch/check remote main, merge feature into local main preserving user's dirty mindmap, normal push and verify remote SHA. Fast-forward24c8ce3; merged model25/130file hashes0; push0/remote SHA equal. Result documentation committed afterward.

## Execution ledger

- Initial HEAD ae9b045; only pre-existing user mindmap/untracked directories dirty. Previous feedback commits remain included.
- User delegated detailed values/direct execution; no additional design gate. Hunger proposals remain non-implemented.
- Ruling: corrected image locations are J7973/J8066; prior J8066/J7801 assumption was one block north and was not persisted.
- Ruling: J5750 removes prototype signals on explicit user report; nearby Naver view is not exact field verification. Cost: precise access/lane dimensions still need targeted evidence.
- Ruling: emergency home uses existing parking and same-day100s/-20, no new house/day transition. Cost if intent differs: flow adjustment later.
- Final review: one read-only reviewer, Critical0/Important1/Minor1. Important system-brake grading RED reproduced, fix uses short grace only after actual system deceleration; integrated suite and actual grounded Player pass (law100/M100 retained).
- Final minor (deferred): cancelled receipt can lose completed request history (UI Clear); no fare/cancellation duplication. Recorded in integration history for follow-up.
