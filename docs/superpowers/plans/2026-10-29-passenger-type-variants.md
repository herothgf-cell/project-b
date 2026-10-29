# Passenger Type Variants Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement passenger types with persistent dialogue/behaviour variants, including preparation work and passenger-caused cleanliness events.
**Architecture:** A pure serializable passenger behaviour model owns type/variant, responses, timers and effects. PassengerJobs owns boarding/arrival/settlement and saves the model; PassengerRequestUI shares safe-stop presentation and existing request evaluation. VehicleCare owns replay-safe vehicle dirt and encounter frequency.
**Tech Stack:** Unity2022.3/C#, existing IMGUI, .NET Framework standalone model checks, Unity editor/player probes.
**Spec:** docs/content-design/02-passengers-and-fares.md, 03-life-and-economy.md. Latest user explicitly includes preparation and food/drink consumption/spills.

## Global Constraints

- No additional user approvals needed for the requested implementation; prior main push authorization persists. Work on codex/passenger-type-variants in existing checkout to preserve local Unity cache/map inputs; do not stage existing untracked files.
- One primary type, stable variant/choices across save/reload. Same event cannot double-charge time/dirt/score. General passenger remains possible.
- Existing request0~2, route-changing request maximum1, request delta clamp[-10,+10]. Type-only dialogue no extra score. Safe responding only stopped2seconds outside crossing/junction conflict.
- All11 types receive documented variants. Precision-stop type uses only existing validated SafeStop alternatives; unavailable candidates remain ordinary service and neutral, no invented P13 safe alternative.
- Food choice: request sealed storage (no extra dirt), allow consumption (crumbs2, preseeded spill risk25% extra6), or safe stopped cleanup10seconds (no extra dirt). Explain risk before choosing; no response/no safe opportunity means no permission to eat and no dirt. No driving-related spill multiplier in first release. Dirt is distinct from normal exit-2, and not vomit lock unless cleanliness threshold independently requires wash.
- Preparation: luggage load10seconds/unload10; muddy luggage protection10seconds vs extra dirt5 on exit; safe deferral neutral. Time runs only safely stopped and not paused. Existing deadlines protect system-required work and first arrival fare lock.
- Sleeping has4variants; other types at least3coherent variants with prior answer/observed-driving responses. No re-roll on load; short trip may skip events neutrally.
- Vehicle durability/repair remains first-review-complete, not part of this passenger implementation request.

## Review Focus

1. Old JsonUtility saves synthesize absent nested objects: explicit version/presence validation and migration, preserve active legacy ride.
2. Food/preparation during save/reload/cancel/day transition must not duplicate dirt or consume frequency for refused boarding.
3. Existing request and new type panel must not overlap or receive same input; moving resumes defer responses without penalties.
4. Sleeping and preparation around arrival/deadline/fatigue must preserve first-arrival fare and allow eventual safe exit.
5. Type eligibility and forced existing test fixtures must not silently mutate route or make old probes nondeterministic.

## Task1: Pure behaviour/frequency/effect model

Files: new Assets/Scripts/PassengerBehavior.cs, VehicleCare.cs, tests/PassengerBehaviorTests.cs, tools/test_passenger_behavior.ps1, affected model compiler source lists.
- [x] Write failing model tests for stable variant, each type/action, safe timer, sleeping, food risk/crumbs, dirt idempotency, frequency cap/rest/missed guarantee and malformed/legacy snapshots; run failure.
- [x] Implement pure model and publish exact integration API in artifacts/passenger-behavior-api.md before integration starts. APIs must expose type/variant, dialogue, choices/respond, safe advancement, boarding/unload readiness, sleep/arrival hooks, dirt events and snapshot validation.
- [x] Run model and existing passenger care/prototype checks; fix regressions. Do not alter legacy forced DrunkRideCase behaviour.

## Task2: Live integration and regressions

Files: PassengerJobs.cs, PassengerRequestUI.cs, PassengerRequestSchedule.cs, PassengerRequestObservation.cs, BusinessProgress.cs, new PassengerBehaviorUI.cs and Editor/Player probes as needed.
- [x] Test first integration gaps in Editor fixtures: type-bound request compatibility, safe response exclusivity, preparation boarding and timing, food effects once, old/new JSON and asleep arrival.
- [x] Bind model to candidate boarding, durable encounter policy, per-frame observation, visible choice UI, request schedule and safe waiting. Use current validated route proposals only.
- [x] Expose type+variant in detailed trip records; completion line includes passenger reaction. Queue type dialogue behind warnings/requests. Save on semantic changes and existing cadence.
- [x] Run Editor integrated tests and isolated Player tests with forced cases. No test modifies user save. Verify new-process reload and all action branches.

## Task3: Content and deployment

Files: docs/content-design/02-passengers-and-fares.md, relevant scope/checklist, dated history/LATEST; default build and manifest.
- [x] Expand every type to3+variants with stage/choice/action/state outcomes, reflect user food request and final behaviour. Label map-dependent exclusions honestly.
- [x] Fresh code review of combined source; resolve material findings.
- [x] Unity editor validation, BuildHybrid, isolated Player regression/UI screenshot, existing model regression. User running game must be closed before replacing binary; check process first.
- [x] Strict input/manifest verification, source+docs/history commit, build manifest and distribution commit, merge main and push. Record exact commands/results and limits.
