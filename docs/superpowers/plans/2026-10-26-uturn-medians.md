# Eungye U-turn and medians implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Evidence-backed U-turn movements and median facilities, with a whole-map centreline audit.
**Architecture:** Keep the existing shared Unity map/graph. Represent U-turn as explicit turn=-2 (existing L=-1/S=0/R=1 unchanged); put observed sign conditions in existing facility types to avoid serializing new fields into hashed baseline/document classes. Add a separate surveyed-facility catalog for evidence/geometry and apply it through the existing map composition/build path.
**Tech Stack:** Unity 2022.3.62f2, C#, JSON, PowerShell/Python audit tools.
**Spec:** docs/superpowers/specs/2026-10-26-uturn-medians-design.md

## Global Constraints
- Preserve user edits, campaign save, raw GIS inputs and checksum validation.
- Confirmed observations only; geometric dimensions remain game estimates.
- Default executable builds/EungyeTaxi/EungyeTaxi.exe via PrototypeBuild.BuildHybrid.
- Test record must distinguish controlled positions, physical driving and human play.

## Review Focus
- Opposing or disabled target/short-radius U-turn must fail validation rather than produce discontinuous paths.
- Signal phase and path conflicts must not treat U-turn as ordinary protected left without checking.
- Existing maps load with original hashes and save/reopen without silently dropping edits.
- Median endpoints must keep crossings, U-turn openings and driveway/service access open.
- Whole-map yellow-line duplication/spacing reports must distinguish imported road edges and safety areas.

### Task 1: Shared U-turn support
**Files:** Scripts/RoadMapAuthoringOperations.cs, RoadMapValidator.cs, RoadMapRules.cs, RoadMapSignalProgram.cs, TrafficSystem.cs, HdRoadGraph.cs, RoadNavigation.cs, AssistedDriveController.cs, LaneDirectionPaint.cs, RoadMapEditor*.cs, Editor/ProjectMapTrafficTools.cs, focused new Editor/UturnTests.cs; all under unity/EungyeTaxi/Assets.
**Interfaces:** turn=-2. A new UturnMovement helper owns label, condition recognition, turn geometry/validation as needed. Existing RoadMapFacility sign type choices represent U-turn conditions (unconditional yield, left green, pedestrian green, left-or-pedestrian green); those signs bind laneId and have rule=true. Do not add serialized fields to MapData/HdEdge/RoadMapLane/RoadMapDocument. Parent author supplies observed map placements after this task.
- [x] Write a regression through existing Connect(...,-2), proving currently rejected U-turn then run Unity batch focused test and record RED.
- [x] Implement continuous U-turn connector with opposite-direction target, explicit semantics throughout navigation/H/NPC, condition-aware admission and conflicts. Reject unsupported target/geometry/condition combinations and disallow automatic unobserved U-turn generation.
- [x] Add editor U-turn control/target click, condition selection, understandable glyph/sign and navigation label. Existing L/S/R must preserve behavior.
- [x] Verify round-trip/old-package hashes, positive and negative connection cases, signal conditions, routing and glyph. Run focused Unity checks and tools/test_prototype.ps1. Coordinate Unity process with parent before running.
- [x] Self-review, report exact tests and changes, commit only owned files. Do not modify map data/build, WorldBuilder, HdRouteTrial or RoadPaintGeometry; tell parent any needed integration.

### Task 2: Map audit and median facilities
**Files:** new Scripts/ReviewedRoadFacilities.cs, Editor/ReviewedRoadFacilitiesTests.cs, tools/audit_road_markings.py, config/maps/eungye-road-facilities.json; integration WorldBuilder.cs/HdRouteTrial.cs/RoadPaintGeometry.cs and map load/build code as needed.
**Interfaces:** catalog includes source URL, capture month, checked date, observed vs provisional dimensions, explicit segments/openings. Its renderer and collider derive from the same line/width layout. Parent maintains reviewed site evidence.
- [x] Add failing geometry test: segmented median leaves declared gaps clear and does not overlap tested live lane corridors.
- [x] Implement median barriers/curbs and safety hatching only for observed geometries, preserving open ends and facilities. Audit all imported and generated yellow lines for coincident or spaced parallels and report roles.
- [x] Add evidence-backed U-turn placements to the project document using Task 1 operations and validated package save. Confirm each approach sign condition in Naver panorama.
- [x] Verify whole-map audit, tests and camera captures of attachment area and added U-turns.

### Task 3: Integration, documentation and distribution
**Files:** relevant checklist/scope/design/editor guide, docs/work-history/2026-10-26-uturn-medians.md, LATEST, distribution manifest and tracked build folder.
- [x] Run integrated Unity map checks, BuildHybrid, runtime U-turn/routing/median collision checks and visual capture. Run model suite once after integration.
- [x] Review full diff with a fresh reviewer and fix actionable findings. Record unreviewed field locations honestly.
- [ ] Update docs/history and build manifest; verify manifest and strict workspace. Commit only task changes. Integrate into user's primary checkout without overwriting preexisting work, according to user authorization.
