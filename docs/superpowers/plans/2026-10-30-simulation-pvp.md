# Existing simulation PVP and expanded A map Implementation Plan

> **For agentic workers:** Use superpowers:subagent-driven-development for independently owned tasks and whole-branch review. Preserve unrelated work.

**Goal:** Existing DriveSession gameplay with an offline competing taxi bot on Eungye first, then the approved 3.6×2.8 km map.
**Architecture:** Reuse DriveSession/TaxiController/PassengerJobs/DrivingLaw/VehicleDamageController/WorldBuilder and original HUD. A mode context isolates persistence and adds match lifecycle/competition. Approved custom MapData feeds existing world and lane generation, not legacy PvpMapRuntime.
**Tech Stack:** Unity 2022.3.62f2, C#, Windows, existing project dependencies.
**Spec:** docs/work-history/2026-10-30-pvp-simulation-realignment.md plus user approval of expanded A map (2026-10-30).

## Constraints and review focus
- Preserve original campaign saves, settings, scene, executable, inputs and other chat work. Latest main 2445f27 merged before edits.
- Existing camera/HUD/input/physical taxi/passenger behavior/damage remain the source of behavior.
- Bot must drive physically and earn from actual transport, not elapsed-time fabricated earnings. Shared offer ownership and idempotent scoring required.
- Match result freezes earnings; cancellation/restart must not leak state. No fixed ten-minute product rule.
- Tests distinguish model/position fixture/actual driving/visual/manual/network. Offline bot is requested here; old network mode must not be advertised as migrated simulation networking.

## Task 1 — Simulation match
Files: new Assets/Scripts/PvpSimulation*.cs, scoped existing DriveSession/BusinessProgress/PassengerJobs/HUD hooks.
Interfaces: PvpSimulationMode.Active, Expanded, DurationSeconds; PvpSimulationMode.Configure(bool expanded,int seconds); PvpSimulationMatch attached to human DriveSession. Map factory called by root MapCatalog seam only when Active && Expanded.
- [ ] Tests first for mode isolation/ownership/once-only settlement and actual components.
- [ ] Implement existing simulation human/bot sessions sharing world/traffic, distinct vehicle/jobs/law/damage state; no duplicate global cameras or world.
- [ ] Preserve UI by original components and small competition overlay; isolate campaign persistence before initialization.
- [ ] Verify physical driving, transport, damage and settlement.

## Task 2 — Expanded A data
Files: new Assets/Scripts/PvpCityMap.cs and Editor/PvpCityMapTests.cs only.
Interface: public static MapData PvpCityMap.Create(). Native MapData and existing HybridRoadNetwork pipeline; 3600×2800, 9 districts/18 passenger hubs, 3 fuel/2 repair/2 wash, connected ring/grid/local roads, original world styling.
- [ ] Test dimensions/counts/lane graph connectivity and safe service/passenger approaches.
- [ ] Implement deterministic authored map factory; actual existing metadata/scene format used for facilities.
- [ ] Verify all stop pairs and road/rules consistency, buildings clear of drivable roads.

## Task 3 — Launch/build and integration
Files: PvpBootstrap/PvpPrototypePanel/PvpBuild, MapCatalog, new runtime probe/editor runner, tools test runner.
- [ ] Connect existing PVP executable launch to original simulation match with Eungye default and expanded A selection. Legacy diagnostics remain explicit.
- [ ] Prepare current committed Eungye map inputs without overwriting campaign scene/build settings.
- [ ] Run Eungye integration before custom-map runtime acceptance.
- [ ] Build isolated Windows client, test copied distribution, capture actual original gameplay HUD, verify campaign/PVP hashes.

## Task 4 — Review and publication
- [ ] Independent review, fix material findings, run relevant regressions.
- [ ] Update roadmap/content/history/LATEST with actual results and limitations; commit source and rebuilt distribution; push feature/pvp-prototype.
