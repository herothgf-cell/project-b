# Ambient pedestrians implementation plan

> Required execution skill: superpowers:subagent-driven-development. User approved A on2026-10-26 and requested minimal essential tests.

Goal: safe visible pedestrians at four approved candidate hubs, distinct from taxi passengers.
Architecture: separate ambient pedestrian placement, actor/state, and district manager. Integrate geometry-aware vehicle yielding and live checkpoint data using existing traffic/signals. Never infer safe paths from marker positions alone.
Tech: Unity2022.3 C#, existing MapData/project geometry and BusinessProgress save.
Spec: docs/content-design/16-ambient-pedestrians-proposal.md (A explicitly approved).

## Global constraints
- J5057/J5076/T1/T2 candidates, max12 including scenario actors:8 sidewalk+4 crossing. Each supported hub2sidewalk1crossing. Unsafe/unverified slots empty, report why.
- Sidewalk20–40m,1.2–1.4m/s, full body clearance and actual height; walking anim and grey/brown/purple clothing, passenger teal/F/pins unchanged.
- Use existing crossing geometry and same pedestrian signal as visible lamps. Wait on red/last2s green, only enter vehicle-clear crossing; finish on red, no same-green teleport/re-cross.
- Yield only intersecting vehicle paths, including cars past stop line. Existing manual/H acceleration-brake ownership stays with driver; NPCs yield automatically and taxi observation/contact uses crossing occupancy. Waiting sidewalk actors do not freeze unrelated traffic.
- R5/PS09 share slots; no overlap/extra actors. First spawn before presentation; later replenishment offscreen and>80m taxi; never despawn crossing actors.
- Save movement and crossing occupancy, restore before simulation; old saves start safe. Trigger contact uses existing collision event once, no physical taxi bouncing/new fees.
- No canonical map JSON/input/ZIP changes. No other economy/handling changes. Preserve main6fb97e8.
- Minimal targeted model ifneeded and one focused editor/player probe covering3approved gates; no full model/campaign/performance repeats. One final BuildHybrid unless concretefailure requires repair.

## Task1 — Integrated pedestrian runtime and essential verification probe
Files: new AmbientPedestrianPlacement.cs/AmbientPedestrian.cs/AmbientPedestrianSystem.cs (or similarly clear focused names), existing DriveSession.cs/TrafficSystem.cs/TrafficCar.cs/AssistedDriveController path, BusinessProgress.cs save, scenario owners and collision integration as necessary; focused AmbientPedestrianProbe.cs +meta. Optional small pure state file/test only if valuable.
- [ ] Read existing signal placement, map geometry, scenarios/save/collision and implement safe route selection for actual four candidate hubs. Cache geometry, avoid18k polygon scans everyframe. Recordactualpositions/slots/unavailable reasons.
- [ ] Implement actors/state and walking animation; signal entry/vehicle-clear/finish, scenario budget ownership and route-local yield. Keep passenger arrays/F/UI untouched.
- [ ] Integrate checkpoint capture/validation/restore before motion, old/malformed snapshot handling and one-contact collision event.
- [ ] Add narrowly scoped runtime probe for safe placement+red/green/finish/yield; passenger separation; actual live checkpoint save whilecrossing plus one freshprocess reload via -ambientPedestrianReloadTest and isolatedartifactpath. One visual capture actualactors at representative hub. Test flag early-isolates realcampaignsave. Essential model tests only ifchangedstatebenefits. Runonefocused editorcompile/geometry check ifneeded, no BuildHybrid (parent ownsfinalbuild).
- [ ] Self-review, history report actualcommands/results, commit allownedsource/meta, release Unityslot. Do notspawnagents.

## Task2 — Review, default build and handoff (parent)
- [ ] Fresh spec/quality whole-branch review; one combinedfixwave ifneeded.
- [ ] BuildHybrid once, focused player3gates and image; fix only concrete failures.
- [ ] Update design/scope/checklist/history/LATEST with actual counts and limitations. Regenerate/verify deploymentmanifest, preserve artifacts and integrate defaultlocalmain. No remote push requested.

## Review focus
Restored crossing while green changes; caralready beyondstopline; nonintersectinglanes notfrozen; scenario activation whileambientcrossing; unsupported safegeometry doesn'tspawn onroad oroverpass; usercampaign save untouched byprobe.
