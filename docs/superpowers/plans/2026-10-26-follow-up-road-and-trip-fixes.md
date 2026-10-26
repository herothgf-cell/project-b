# Follow-up requested 2026-10-26 (after U-turn/median work)

User explicitly requests completing current U-turn/centreline/median work first, then:
- (504.753723,475.289825): Naver cross-check whole junction marking layout, separate right-turn lane, misplaced pedestrian signals.
- Survey and implement observed pedestrian refuge/traffic islands between sidewalks, signal poles and slip right turns (user second panorama attachment).
- (505.3904,273.581268): verify the incoming two-lane approach and inner left-turn lane via Naver.
- Fare settlement: satisfaction60 required70 still pays base9200; determine intended rules, clarify UI or fix discrepancy.
- Satisfaction: inspect positive live changes, implement live gauge/feedback.
- Navigation briefly loses route on lane change; fix.
- Full map clicked destination overrides trip route and hides ride destination; provide restore-trip-route control and preserve markers.
- Eungye stop9 drop-off fails; reproduce/fix.

Attachments are in user chat, filenames 89c91fcc, baea1d5b, 918070ab, b528d509. Do not lose this queue after context compaction. Tasks A/B/C are now in implementation; final integration status is recorded below.

## Execution plan (approved continuation; no additional design gate)
Keep the U-turn/median changes and finish their focused review before applying the following fixes. Use existing design documents as binding economy/score policy; do not silently change fares to compare the wrong score.

### Task A — Satisfaction and fare explanation
Owner: fresh implementer; DrivingManners/MannerLedger/FareTrip/PassengerRequestUI plus score/receipt presentation and focused tests. Coordinate any PassengerJobs change with Task B.
- Reproduce missing live positive change; inspect existing request satisfaction event/HUD binding.
- Implement P8-07 stable driving recovery as specified in content-design/04: only abrupt acceleration/braking losses recover, +2 per 500m or 60s genuinely moving, cap+6/trip and score100, no stopped/repeated-path farming or legal/collision loss restoration; preserve progress in save/restore.
- Ensure confirmed positive changes update the existing live S gauge and immediate notification. No arrival bonus prediction or fake startup score notification.
- Explain base fare (M threshold) and bonus (S threshold) in result UI while preserving base100/70/timeout0 policy and recent compact receipt semantics. Verify 60S/70R example.
- Focused model/Unity/runtime tests for recovery rules, save, UI score change and settlement. Report limits. Commit owned code/docs only.

### Task B — Navigation and stop9
Owner: fresh implementer; RoadNavigation/HdRoadGraph if needed, FullMapOverlay partials, PassengerJobs/PassengerStopCatalog/ServiceStops and focused tests. Coordinate changes to scores with A.
- Reproduce route dropout during valid lane changes, trace nearest edge/repath/progress; fix continuity without guiding through disconnected/wrong-way lanes.
- Preserve active pickup/drop markers during arbitrary map selection; clear selected preview and restore passenger route through obvious M control. Do not overwrite an onboard passenger destination.
- Reproduce Eungye legacy stop9 drop with real package/compiled lane geometry and position/heading checks; fix cause rather than globally enlarging every stop. Test near miss, wrong direction, actual valid stop and one-time settlement. Respect drunk arrival waiting and existing save states.
- Add focused tests/probe hooks as needed. Commit owned files only.

### Task C — Junction map and islands (parent)
- Naver cross-check actual x504.753723,z475.289825 (J5057_4449 north approach) and x505.3904,z273.581268 (J5076_2580 north approach). Record panorama, month, approach, observed lane arrows/count, and unknowns.
- Fix misleading yellow/white markings using source roles and actual connections; place pedestrian signals on sidewalk/island endpoints instead of drivable lanes.
- Add observed traffic/refuge islands and right-turn slip road separation. Geometry dimensions are game estimates; keep walk/crossing paths, drivable turning swept area and colliders consistent. Do not fabricate an island at every intersection.
- Save project package through supported creation/store path preserving baseline hash; update relevant observations/catalogs. Focused geometry/graph/collider tests and camera captures.

### Task D — Integration
Serialize Unity project use. Run relevant regressions, BuildHybrid, executable probes and camera review. Separate controlled-position checks, actual physics and human play; never claim manual/campaign coverage. Update user docs, checklist, scope, work history/LATEST and distribution hashes from actual committed source. Preserve initial primary checkout changes; integrate completed work there so the default executable contains the fixes. Fresh whole-branch review and one fix wave before final delivery. No remote push without explicit authorization.

## Preliminary field read (during final checks of previous task; no follow-up map edits yet)
- J5057 north-side island panorama dcuIEvnfmTeYM4mY2DWaXA, capture2026-05: a raised triangular refuge island with pedestrian/cycle crossing; curved slip road separated from main approach, curb edge and yellow speed table. It is on the north-west corner (looking north, main approach is to camera right; west-side apartment/rail on left).
- Heading30,-8,60 and16,-3,30 show three southbound approach arrow positions: curb right turn, middle straight, inner left-only; heading35,-8,50 subsequently confirmed all three arrows and one opposing northbound lane.
- URL: https://map.naver.com/p/entry/coordinates/3zafbw,2AGEMu?c=16,0,0,0,adh&p=dcuIEvnfmTeYM4mY2DWaXA,16,-3,30,Float . Heading190,-12,70 shows island and main crossing.
- J5076_2580 coordinate lookup37.4363461,126.8012172 resolved entry3zaffK,2AGAoR and panoramaPo3cjZOgY9v4qPfVBPGTYg (2026-05). View0,-12,65 from junction lookingnorth clearly shows southbound2lanes: inner LEFT-only, outer STRAIGHT; opposing northbound1lane behind median railing. Black/yellow post rail on yellow double line before crossing. View180,-8,70 shows crossing/east-side 은계번영길 and south road continuing with median. The east arm is already in the graph, so the left movement uses that existing road.
- Sources: https://map.naver.com/p/entry/coordinates/3zaffK,2AGAoR?c=17,0,0,0,adh&p=Po3cjZOgY9v4qPfVBPGTYg,0,-12,65,Float ; samepano180,-8,70.
