# Additional reviewed U-turns implementation plan

> Execute inline with superpowers:executing-plans. One final focused review before integration.

**Goal:** Implement observed approaches ④ J7254 southwest, ⑤ J8066 south, ⑥ J7973 east in the game.
**Architecture:** Extend canonical authored lane graph and lane-bound conditional signs using existing UturnMovement. Preserve original baseline and all other approaches. Curves/exit lanes and signal timing are game estimates.
**Tech Stack:** Unity C#, canonical project JSON.
**Spec:** docs/map-observations/2026-10-26-additional-uturn-observations.md; user now explicitly requests game implementation.

## Integrated task
- [x] Add reproducible AdditionalReviewedUturnMap authoring and focused three-approach validation (routes, signal conditions, opposite-direction rejection).
- [x] Apply via ProjectMapStore; verify vehicle clearance and stop-line observer at these three approaches only. Correct actual geometry failures.
- [x] Review complete diff, BuildHybrid, minimal execution check, regenerate distribution manifest.
- [x] Update observation status, scope/checklist and work history with actual results; merge main and push preserving user changes.

## Constraints / review focus
Only observed incoming directions; left signal OR pedestrian signal permission with existing pedestrian/traffic clearance. No speculative other U-turns. Minimal tests, no full campaign. Check native lane rule binding at ⑥, continued straight links, geometric radius, existing T1 preservation, map hash/save compatibility. Keep private location map as historical artifact.
