# Route 42 Lane Network Implementation Plan

> Execute inline using superpowers:executing-plans. User approved the written design and explicitly requested implementation and a runnable build.

**Goal:** Drive real NGII lane geometry through Route 42 test junctions with navigation and steering-only assistance.
**Spec:** ../specs/2026-10-15-route42-lane-network-design.md
**Architecture:** Dedicated HD graph and trial map, selected by launcher. Navigation and assistance share a graph and route. Preserve legacy regions.
**Tech stack:** Python/pyshp/pyproj, Unity 2022.3 C#.

## Constraints and review focus

- Keep source ZIPs unchanged; convert 5179→5186; no invented endpoint connections.
- Check disconnected right-turn endpoints, reversed headings, partial segments, lane changes across solid markings, and control input preservation.
- Preserve user's existing uncommitted work; use current feature branch and do not commit unrelated changes.
- Build trial separately so existing executable remains available.

## Tasks

- [x] 1. Importer: tests for ID-only directional joins and solid-line exclusion; export full original lane paths, markings, verified adjacency windows, four trial routes with source IDs and provenance. Investigate right-turn topology and record missing coverage.
- [x] 2. Shared graph: directed shortest routes, position projection, source curve following, validated lane-change curves, unreachable handling. Add Unity tests for route edges, wrong directions, missing connections and transition continuity.
- [x] 3. Trial integration: separate launcher/map, actual marking geometry, purpose-built trial selection, same graph for RoadNavigation and H control. No campaign persistence, no legacy traffic on fabricated routes. Preserve acceleration/braking.
- [x] 4. Build and physical tests: four turn cases, lane change, no longitudinal override, missed-turn routing, legacy regression. Independent review and repair material findings. Deliver launcher and limitations.

## Ledger

- Setup: existing feature/assisted-route-driving contains the active, uncommitted project and local raw data; retain it rather than create a checkout missing required changes.
- Ruling: execute the plan in this session as expressly requested; avoid another approval loop for an already approved scope.
- Investigation: right-turn geometry is present. Some terminate at the limit of minor-road capture, or start there, unlike left-turn through-link samples. Validate usable main-road→right-turn paths without inventing missing minor-road continuation.
- Importer RED: test_hdmap.py failed before module existed; GREEN: all 5 HD tests, all 34 Python repository tests pass.
- Graph RED: Unity missing HdRoadGraph; GREEN: HdNetworkTests checks five cases, 444 edges, shared source connectivity, change route, reversed heading, off-map and terminal reversal rejection.
- Ruling: trial signal operation and legacy AI are disabled instead of using an unverified axis-based signal configuration over actual curved lanes. UI and README explicitly disclose this; no live traffic timing was acquired.
- Independent reviewer found terminal via-goal rejection and missing HD indicators. Added failing terminal-via regression (`hd-via-red.log`), fixed Via and indicator synchronization, added physical request/indicator/missed-turn tests.
- Final build: `PrototypeBuild.BuildHd` succeeded, `artifacts/hd-final-build.log`. Executable `builds/Route42Taxi/Route42Taxi.exe`, launcher `Play-Route42.cmd`.
- Final physical tests: `artifacts/hd-drive-test.txt` success=True, all five routes physically completed; maximum lateral error 0.8404m, no static body overlaps. Test driver controls speed; H longitudinal demand unchanged. Both right-turn requests accepted, unsupported opposite request leaves route unchanged, actual missed-turn adjacent lane yields replan/no-route, off-map arming rejected, reverse disengages.
- Legacy regression: `AssistedNetworkTests.Run` PASS, 180 lanes (`hd-legacy-regression.log`).
- Independent reviewer rechecked fixes and final tests: no new material issues. Raw/derived map data and binaries remain ignored; no push requested or performed.
