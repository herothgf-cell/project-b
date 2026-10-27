# Drowsy closing, map guidance and manual handling

> Execute inline using superpowers:executing-plans. User previously delegated values and direct execution; no repeated design gate. Minimal focused RED/GREEN, final integrated verification.

**Goal:** Apply latest screenshot feedback without changing unrelated map permissions or user's mindmap.
**Architecture:** Reuse day closing/settlement after emergency sleep; persist exposure and one-time warning state. Keep genuine directed graph routing, fix facility target/current-lane failure at its source. Simplify map presentation and increase manual low-speed steering only.
**Tech stack:** Unity2022.3/C#/PowerShell.
**Spec:** User's latest seven screenshots plus appended navigation-loop screenshot; existing content-design10/13/06, latest handoff.

## Tasks

- [x] 1. Condition: test hunger integration across25/10 and emergency sleep→Closing→Summary once. Implement fatigue recovery-20/100s retained but day ended; toast once per sleep cycle then taxi Zzzz; correct simulator spelling.
- [x] 2. Map: reproduce all station route failures at screenshot-like positions and current parking; fix only actual connectivity/arrival selection. Remove facility detailed paragraphs, keep name/capability/distance/action; fit Korean tooltip; offer rows retain pickup/drop and estimated fare only. Exit continuity focused RED sample40→GREEN including rest/restaurant.
- [x] 3. Navigation: identify southbound source of screenshot, pin unwanted south-block loop with exact source/target route. Correct evidence-backed lane changes/turn links only; retain footprint/validator. If no evidence supports proposed turn, document limitation rather than invent permission.
- [x] 4. Manual handling: add low-speed tighter-turn regression, preserve high-speed and H automation calibration; adjust steering curve/input response modestly.
- [ ] 5. Final integrated Editor/model/Player/UI/save checks, single independent review, BuildHybrid and130-file manifest verification. Update content docs/checklist/history/LATEST; commit own files. No new main push requested in this message; keep feature branch unless explicitly requested.

## Review focus

- Blackout close must not continue trading or charge normal40 recovery in addition to emergency20; repeat/reload no duplicate.
- Warning once must survive map open/pause and restored exposure; recovery enables next cycle.
- Facility route must originate on actual directed lane and not cross divider/teleport; pickup fuel diversion/occupied rejection unchanged.
- Narrow1280 sidebar and long Korean names must remain inside viewport; simplified fare is estimate not promise.
- Manual low-speed change must not alter automated H curve or high-speed grip.

## Ledger

- Base79e3d22; only pre-existing mindmap/untracked directories dirty. In-place new branch honors existing user preference and caches.
- Ruling: keep explicit blackout button as safe confirmation, label day-end; no automatic moving teleport. Cost: one click still required before daily results.
- Ruling: ‘fare only’ means remove fare breakdown/demand/route/deadline text from offer rows, preserve pickup/drop identification. Fare remains estimated.
- Tasks1/4 model focused RED→GREEN, full26 suites0/care13301. Task2 parking routes already passed; pump exit RED→GREEN. Task3 real south-loop1752→709m via staggered164/194m windows, validator/footprint/direct turns/rails passed.
- Final review Critical0/Important1/Minor1. Important exit-gap and review's rest-departure limitation are both fixed-scope requirements; full authored exit sampling added. Minor nonfuel capability subtitle deferred.
- Ruling: independent field permissions and full manual physics are not inferred from graph tests; provisional geometry stays labeled. Cost: user's desired street-exact route still needs targeted evidence/manual feel.
