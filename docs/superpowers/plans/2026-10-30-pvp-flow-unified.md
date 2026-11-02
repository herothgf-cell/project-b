# PVP flow and unified mode Implementation Plan

> For agentic workers: Use superpowers:subagent-driven-development for independent owned subsystems. Root alone runs Unity. User explicitly overrides per-step reviews/tests: implement together, then essential final verification.

**Goal:** Natural parallel traffic, varied readable A2 roads and one executable with isolated single/PVP modes.
**Architecture:** Extend existing native physics, route graph, map rendering and startup. Preserve campaign saves; native PVP map only. No new vehicle engine.
**Tech Stack:** Unity2022.3.62f2/C#/Windows.
**Spec:** docs/content-design/pvp/27-flow-unified-mode.md

- [x] Traffic agent: NativeTrafficMergePolicy/layout, necessary traffic/bot hooks and final flow/performance fixtures. Parallel compatible movements, conflicting movement exclusion, prior queue/continuation safety. Do not run Editor until final checkpoint.
- [x] Map agent: PvpCityMap/RoadBuilder, road direction markings,6lane connectors, additional alleys, final geometry/paint tests. Coordinate metadata geometry with traffic owner.
- [x] Mode agent: unified startup and build entry, campaign/PVP session isolation, PVP city-only choice, final startup/save transition tests. Preserve prior standalone build compatibility until migration verification.
- [x] Root: source/remote preservation, specified-coordinate profiling orchestration and red-car evidence,20minute life design proposal, final test coordinator and distribution/docs.
- [x] Final gate only: compile, essential Editor contract/geometry/regression, source build, copied Player traffic/4driver/services/mode/save/performance, brief visuals only if needed. Fix observed failures; do not repeat unrelated checks.
- [x] Update roadmap/checklist/history/LATEST and manifests, commit/push with latest main integrated, preserve other checkout/untracked work.

Ruling: integrate into existing EungyeTaxi.exe/product identity to preserve campaign save location; PVP must retain separate save policy and process diagnostics. No deletion of historical standalone binaries until unified acceptance is complete.

Final evidence: docs/work-history/2026-11-02-pvp-flow-release.md. Human/different-PC/native remote4 acceptance remains separate. Release e6ab18c was atomically pushed to feature/pvp-prototype and main; both remote refs verified.
