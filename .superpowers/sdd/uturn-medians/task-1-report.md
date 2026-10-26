# Task 1: shared U-turn movement

## Scope and implementation

- Added explicit `turn=-2` movement geometry, validation, lane targeting and one lane-bound U-turn condition sign. No serialized package fields were added.
- Exposed the movement in both map editors, direction glyphs, navigation labels, route steering, traffic admission and conflicts. Default signal generation remains L/S/R; observed U-turn conditions use the existing left or pedestrian phase.
- NPC admission checks the condition, live pedestrian observation, actual crossing walkers and occupied route corridor. Manual law and manners observers snapshot the U-turn condition on stop-line entry and classify a completed reverse exit as U-turn. Manual yielding no longer exempts opposing left-turning or perpendicular traffic for `turn=-2`.
- Only Task 1 C# sources and their new `.meta` files are in my commit. Parent owns project map placements, integration probes and documentation; median agent owns facility rendering and audit.

## Verification

- Initial focused RED: `RoadMapAuthoringOperations.Connect(...,-2)` rejected the explicit U-turn (previous agent; `.superpowers/sdd/uturn-medians/uturn-red.log`).
- NPC crossing pedestrian observation RED: `.superpowers/sdd/uturn-medians/uturn-green.log`, failure `U-turn yields to crossing pedestrian observation`; fixed by checking `Observe(...).PedestrianClear`.
- Manual opposing-left yield RED: `.superpowers/sdd/uturn-medians/uturn-opposing-red.log`, `UturnTests.Run` exit 1 at `manual U-turn yields to opposing left-turn traffic`; fixed filter.
- Final focused Unity `UturnTests.Run`: `.superpowers/sdd/uturn-medians/uturn-final-green2.log`, exit 0 and `UTURN_TESTS_OK`. Covers geometry/invalid targets, sign conditions, validator, signal phases, NPC conflicts and pedestrians, manual law/manners on pedestrian phase, glyph, routing and document roundtrip.
- `pwsh -File tools/test_prototype.ps1`: exit 0, all 23 model test executables PASS; `.superpowers/sdd/uturn-medians/model-tests-final.log`.
- `ProjectMapBuild.Prepare`: exit 0 before focused map-backed test (initial missing local Unity Resource; tracked project JSON was present). Parent later updated the project map and reran Prepare.
- `git diff --check` on Task 1 tracked files: exit 0.

## Limits and handoff

- I did not run manual physical driving or a full campaign. Parent is running aggregate integration/editor checks and the final build.
- The first new manual-observer RED used a synthetic lane 6m from centre with default junction half-width 5m; that fixture did not enter the law observer. The fixture now sets half-width 8m and clears a pedestrian-crossing observation intentionally injected by the preceding case. The final focused run passes with this corrected fixture.
- Test project map Resource is local generated input and remains ignored; no map or package schema changes are in this Task 1 commit.

## Review round 1 fixes

- A stationary follower 6 m behind the approaching U-turn car previously blocked admission through the projected path check. The check now ignores only a same-stream vehicle that projects at least 4.5 m behind the subject with aligned heading; occupied turn paths still block.
- The simple map editor previously announced success and ended target selection after an invalid U-turn connection. Connection geometry is validated on cloned endpoint lanes before Edit records an undo/revision; failure keeps target selection and shows the geometry error.
- Focused RED evidence: review-red.log fails invalid editor connection; review-follow-red.log fails stationary follower after the editor fix.
- Focused GREEN evidence: review-green.log, Unity UturnTests.Run process exit 0 and UTURN_TESTS_OK. The existing occupied-path assertion continues to pass.
- The model suite was not repeated in this review round, per task direction.
