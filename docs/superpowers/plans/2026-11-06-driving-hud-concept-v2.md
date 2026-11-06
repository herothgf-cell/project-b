# Driving HUD concept V2 implementation plan

> Execute inline using superpowers:executing-plans; review the completed change once.

Goal: apply the user's approved follow-up direction from the original concept gap review, producing a working second design and real captures.

Spec: `docs/work-history/2026-11-06-original-concept-gap-review.md`, user request to develop the second design and proceed.

Architecture: preserve Unity IMGUI and all data/control paths. Add a presentation-only dark glass/dial helper; relocate the existing HUD groups and navigation; keep the shared dialogue scroll/effects. Existing portraits are enlarged, retaining all four identities. New expressive portrait art and world art remain separate.

Constraints: no gameplay values, timing, signal authority, map inputs, saves, platform controls or fictitious rewards change. No decorative touch pedals. Keep 5 condition gauges, money, score, deadlines, route/next turn, 3/4 lamp evidence captions and all menu actions. Use 1600x900 reference coordinates with the existing responsive scale.

Review focus: active long dialogue must leave speed/turn indicators visible; long destinations wrap; PVP 4-player standings must fit between trip and actions; warning states must remain legible on dark surfaces; navigation and service/situation guidance must not overlap.

- [ ] Add layout regression for persistent speedometer vs long dialogue at 1280/1920 and ultrawide. Observe failure before applying layout.
- [ ] Add `DrivingHudArt.cs` (dark glass surfaces, semicircle gauge); update `CockpitUI.cs` (clock/cash/conditions left, passenger bottom-left, dial bottom-center, actions bottom-right, job below map).
- [ ] Update `DrivingReadability.cs` for centered top navigation and `PassengerDialogueUI.cs` for larger portrait and dialogue above dial. Keep next-cue geometry and signal resolver unchanged.
- [ ] Run HUD/layout, effect, navigation, signal regression and BuildHybrid; render actual single/PVP, dialogue/long-scroll and signal captures at both sizes. Inspect images and fix visual defects.
- [ ] Fresh code review; record limits/results, update content05/checklist/LATEST, commit source then regenerate and verify distribution manifest, commit build. No remote push.

Design decision: use actual game captures as the second design deliverable, so the approved visual direction is directly reviewable in the playable build. Current passenger art is enlarged rather than replacing passenger identities with a single concept character.
