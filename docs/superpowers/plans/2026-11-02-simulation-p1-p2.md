# P1/P2 Simulation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans. Work in this isolated checkout; do not edit the user's main checkout or launch concurrent Unity builds.

**Goal:** Implement the requested P1/P2 gameplay additions, deferring actual music/air-conditioning/window controls.
**Architecture:** Small pure C# policies with serialized per-trip/session state and narrow Unity adapters. Reuse native fares, conditions, traffic, routes, facilities, and checkpoint boundaries; preserve observed event and transaction identities.
**Tech Stack:** Unity2022.3.62f2/C#5-compatible model tests/PowerShell7/Python map tooling.
**Spec:** `docs/future-design/2026-11-02-simulation-priorities.md`, `docs/content-design/01-driving-and-traffic.md`, `02-passengers-and-fares.md`, `03-life-and-economy.md`, `pvp/27-flow-unified-mode.md`. User approved P1/P2 development; music/AC/windows are on hold.

## Global Constraints

- Single `builds/EungyeTaxi/EungyeTaxi.exe`, `PrototypeBuild.BuildHybrid`, existing company/product/save path. Shared model semantics for player and bots; separate campaign/PVP session clocks.
- Never write user AppData in tests. Old saves use neutral defaults; no rerolled outcomes or repeated charges after restore.
- No arbitrary scene scans per frame or population increase beyond existing72 NPC pool. Cache route/forecast queries and rate-limit reactions.
- Avoid duplicate manners/satisfaction penalties, unsafe answering, or changing agreed fare after a settled trip. Necessary emergency braking must not become a second driver-fault penalty.
- Preserve the original checkout's unrelated UI source/build/docs/WIP mindmap changes. Integrate the completed UI commit later; do not revert it.
- Verified road-sign evidence is required for real permissive-left placement. No invented permissive locations. Safety-map prerequisites must be explicitly recorded if evidence remains unavailable.
- Deferred: actual music/AC/window controls; P3/P4 features and actual remote multiplayer.

## Review Focus

- Save while a reaction, errand wait, alternative stop, queue, or service is active: restore without new money/time/effects.
- Trip acceptance to day/overtime or match-end boundary: honest forecasts; preserve delivered fares and accepted-trip identity.
- Necessary braking, police/law warnings, passenger requests, motion resumption: no false blame, modal overlap, or lost required answers.
- Facility busy/unreachable, no alternative bay, no ETA: explicit unavailable/wait fallback; no fabricated zero cost/teleport/despawn recovery.
- Shared PVP service queues/condition rules and campaign data isolation: no free bot condition recovery or output contamination.

## Task A — Passenger reactions and errand waits

**Owned:** `PassengerBehavior.cs`, `PassengerJobs.cs` and passenger-owned partials/snapshots, new passenger reaction/errand policy + adapter/tests. Do not edit service/traffic/forecast files or common test runners.
- [ ] Add focused RED models for event-gated signal wait/real jolt reactions, conversation budget/cooldown/quiet choice, no duplicate dirt/satisfaction, emergency-brake neutrality, save restore.
- [ ] Implement actual-event reactions: signal waiting18s; jolt/brake event only from observed driving input/motion; at most2 extra situation reactions and existing total3-question budget,>=20s cooldown. Stronger warning never auto-selects an answer.
- [ ] Add physical errand wait at an accepted existing ViaStop: agree to20s free waiting before changing route, safely stop, advance only active gameplay, preserve first destination and trip ID, return to original destination after work; no waiting surcharge. Late/closed/damaged state uses existing trip protections and explicit cancellation policy, not free fare.
- [ ] Snapshot reaction/errand state at existing live checkpoint boundaries; neutral old-save migration. Actual driver/UI wiring is required, not model-only.
- [ ] GREEN model and focused Editor/native probe; send exact commands/results and ownership to parent.

## Task B — Forecasts, facility queues and bot service decisions

**Owned:** `ServiceStops.cs`, `PvpSimulationBotServices.cs`, new forecast/queue/condition-operation policies + adapters/probes. Do not edit passenger/traffic models, `BusinessProgress.cs`, common test runners or full-map UI; request narrow parent integration if needed.
- [ ] RED side-effect-free forecast models using current condition/account snapshots; missing ETA, overtime, PVP/campaign clock, service detour and quote/fee boundaries.
- [ ] Forecast current selection/active trip at1Hz and route/selection changes. Show end fuel/satiety/fatigue and service travel+use time/price separately; no data means unavailable. Expose parent-friendly display API.
- [ ] Shared physical facility queue: identify authored site; visible player/bot wait order, cancel/depart/finish/disconnect release, no overlapping service grant. No fee/service time before actual admission. Show alternate reachable facilities and waits; do not create permanent blockers.
- [ ] Add bot independent hunger/meal choice and remaining-time/detour net-benefit decision. Same current service cost/time/effect as player; no new automatic fatigue fare penalty. Existing severe-condition/tow/wash protections take precedence.
- [ ] GREEN focused tests and Editor/native probe including shared4-taxi facility, restore/idempotence, no mutation from forecasts. Send integration API/results.

## Task C — Traffic/demand profiles and manual indicator cancellation

**Owned:** `TrafficSystem.cs`, `TrafficCar.cs`, `PassengerDemandPolicy.cs`, `TaxiSignals.cs`, new profile/time-of-day/indicator model tests/adapters. No PassengerJobs/ServiceStops/BusinessProgress edits or shared runners.
- [ ] RED deterministic time-of-day origin+destination weights, stable accepted offers, even overtime supply; no fare multipliers. Expose bounded weights without changing existing accepted jobs.
- [ ] Map existing campaign clock to morning/day/evening origin/destination direction; PVP uses relative match phases. Reuse72 NPCs; bounded offline redistribution and route preference only.
- [ ] Add observable calm/normal/impatient general-car profiles with safe speeds/gaps/acceleration/lane-change cooldown. Keep red signals, crossings, collision/queue occupancy, turn legality mandatory. No aggressive ramming, fake unblocking, or exempt penalties.
- [ ] Manual indicator: record movement heading on activation, require observed turn>=35deg and return to straight steering for1s before cancel; no timeout cancelling while waiting, hazards override, lane-change completion supported, assisted control retains authority. Heading wrap, reverse, reset/save cases covered.
- [ ] GREEN focused models + Editor/native probes; communicate demand API so parent can integrate offer generation.

## Task D — Parent map/law integration and total acceptance

- [ ] Check isolated baseline models and input Strict before production changes.
- [ ] Author/validate game-only alternative stop pairs with physical lane/bay/sidewalk and pedestrian routes; reject occupied/unsafe/unreachable alternatives. Integrate offer/route selection, same trip/settlement, once-only deadline/request protection and save restore.
- [ ] Observe actual road-view evidence for permitted-left approaches from more than one point/direction; only positive readable evidence can change map permissions. Record unresolved sites; do not fabricate completion.
- [ ] Game-specific slow-turn rule: evaluate only explicitly authored observable zones; show game speed limit, protect emergency avoidance and same-event max deduction. Never claim a universal legal speed threshold. Add wrong-approach/no-zone/stop/duplicate-event tests.
- [ ] Wire A/B/C adapters into existing session/save/UI and demand generation, respecting ownership. Complete focused test review per task; address review findings before whole build.
- [ ] Update content specs and mindmap50/25/0 only for actually implemented/validated scope; controls held item remains on hold.
- [ ] Integrate completed bright UI changes without publishing unrelated unfinished code. Full model/Editor/BuildHybrid, isolated native functional/restore/physical/visual/performance checks at final integration.
- [ ] Refresh full execution folder/manifest with actual source SHA, history/LATEST, and preserve user files. Report unresolved evidence/manual/otherPC limits honestly.

## Decisions Ledger

- Ruling: retain current campaign fatigue/food costs and avoid new direct condition fare deductions; improve forecasts and service choices first — preserves the approved satisfaction economy; if weak in short matches, later profile tuning remains separate.
- Ruling: errand waiting starts as explicitly agreed20s at existing ViaStop, free and safely stopped — avoids inventing waiting-meter pricing while making time a real operating cost.
- Ruling: new alternatives may be authored as game-only safe bays, never represented as real approved roadside stopping — validated pair EG-GAME-02→EG-ALT-02-A; invalid sites fail closed.
- Task A: fix rounds1–3/5 addressed typed physical reactions, autonomous calm rest and unavailable initial-choice hold; final scoped review6332729 has no material residual. Covering actual adapter40/situation82/behavior537; final native still pending.
- Task B: fix rounds1–2/5 addressed physical admission succession, cancel suppression, single planned purchase/travel budget and critical departure control; scoped review b8521a4 no material residual. UI acc5fad/adbed16; actual Editor reachability fixture RED→GREEN c2d4806 (off-road origin rejected, actual lane reaches3 sites,22checks).
- Integration: Editor2 exit0 includes A26/C/B22/checkpoint-law15/alternative runtime. Replace nonexecuting Editor SendMessage Tick with direct method invocation; no behaviour assertion remains.
- Ruling: delayed food spill applies only to new timing-version1 trips; legacy0 timing remains. Existing seeded spill is brought forward by physical jolt, never an extra dirt/penalty event.
- Remaining acceptance: completed bright UI merge; final native/physical/visual/performance; full executable manifest/source SHA/main publication. Actual positive permissive-road sign evidence remains pending, no invented site.
