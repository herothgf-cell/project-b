# Handling, traffic and receipt Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans. Use checkbox steps.

**Goal:** Responsive steering, distributed visible traffic, simple receipt, additional privately marked U-turn observations and a pedestrian proposal for approval.
**Architecture:** Existing Unity graph, traffic and physics; presentation derives from settled results. Research artifacts stay outside game resources.
**Tech Stack:** Unity2022.3.62f2 C#, PowerShell models, Naver Street View, Python map rendering.
**Spec:** docs/superpowers/specs/2026-10-26-handling-traffic-receipt-design.md

## Global Constraints
- Preserve user changes, campaign saves, fare calculations, canonical map/input hashes.
- Default executable builds/EungyeTaxi/EungyeTaxi.exe via PrototypeBuild.BuildHybrid.
- U-turn additions are private guide annotations only; no game-map movement edits.
- Pedestrian runtime implementation requires explicit human approval after proposal.
- Serialize Unity use and product implementation tasks; no worker-spawned subagents.
- Record actual command exit results, coverage and unverified human/field limits.

## Review Focus
- Negative/zero/positive net receipt bonus including legacy records and timeout must reconcile to received money.
- Small steering at speed must not trigger automatic brakes; reverse/brakes/H and physical stability remain valid.
- Allocated NPC count must not be confused with visible count; route continuation and broad distribution must be real.
- No spawn overlap or forced unsafe spawn to meet count; report impossible areas honestly.
- Research marks require observed signs/markings and approach direction, not a geometric U-turn guess.

### Task 1: Compact completed receipt
**Files:** unity/EungyeTaxi/Assets/Scripts/RideReceiptSummary.cs, RideCompletionPanel.cs, tests/PassengerCareTests.cs, focused docs as needed.
**Interfaces:** RideReceiptSummary.Money(PassengerResult) always renders three lines: initial expected fare; `보너스  +/-net원` where net=paid-InitialFare; actual received fare. Zero has no plus sign. Remove completed-panel Rule display; keep detailed saved economic fields and F8 history.
- [ ] Add focused RED cases modern and legacy: positive/zero/negative/timeout. No 약정/감액/정산 증감 or Rule explanation on panel. Preserve same amounts.
- [ ] Implement minimal presentation-only change, remove empty rule gap if practical without broad layout redesign.
- [ ] Run focused model GREEN, update affected existing presentation assertions rather than delete meaningful checks. Record command/output and self-review.
- [ ] Commit only owned source/tests/docs, full report scratch. No Unity use until parent grants slot; parent handles final build/capture.

### Task 2: Responsive manual steering
**Files:** unity/EungyeTaxi/Assets/Scripts/DriveMath.cs, TaxiController.cs; existing tests/DriveMathTests.cs or precise equivalent; focused runtime probe if needed.
**Interfaces:** Preserve automated validation/H paths. Diagnose CornerAssist call using normalized steer against ParkingSteerAngle even though actual ManualSteerAngle varies by speed. Fix actual-angle interpretation rather than remove all stability protection. Explain any input ramp tuning with measurement.
- [ ] Inspect actual demand/steering math and write a regression showing small 30/50/70kph corrections cut motor/add brake before fix.
- [ ] Correct physical-angle corner cap for manual control; ensure small corrections retain requested acceleration and normal brake/reverse works. Improve manual input rise/return only if concrete sluggishness remains, no new speed/traction hacks.
- [ ] Focused model GREEN, add real Rigidbody automated left/right correction run measuring speed retention, lateral movement, slip/roll and recovery. Low-speed90-degree and H path regression remain parent integration gates if code touches them.
- [ ] Coordinate Unity slot, self-review, commit owned files and report RED/GREEN, behavior, measurement limits.

### Task 3: Visible district traffic distribution
**Files:** unity/EungyeTaxi/Assets/Scripts/DistrictTraffic.cs, relevant TrafficSystem/TrafficCar if required; focused editor/runtime distribution tests.
**Interfaces:** Keep existing graph and legal/safe route admission. Measure actual Visible car count and geographical spread, allocated max48 and initial12service stop seeding. Distribute admitted cars along verified clear routes or valid connected ordinary lane starts with safe occupancy; do not just increase max cars or bypass static clearance. Existing stop goals/continuation can be reused.
- [ ] Reproduce clustered or recycled initial population using the actual canonical map; record initial active count, distinct250m cells and routes.
- [ ] Seed a deterministic spatially distributed population, prefer improving currently48 allocated cars; target at least24 simultaneously visible across at least8 feasible250m cells if map capacity permits. Keep minimum10m vehicle spacing, avoid player/static overlap, parking/service/turn/change-only starts, and require onward continuation.
- [ ] Verify actual canonical map distribution and safe initial placements, forward movement/continuation and signals/queues over simulated time. Report fallback constraints instead of forcing unsafe targets.
- [ ] Self-review, commit owned files/report. Do not add pedestrians or modify map JSON.

### Task 4: Pedestrian planning review (read-only)
**Files:** existing docs/content-design/06-world-map-and-driving.md, scope/checklist, RoadMapWalker/RoadMapFacilities/scenario pedestrians/passenger actors. Report scratch first; parent writes approval proposal.
- [ ] Identify existing planning and actual runtime creation, signal intent/phase/gaps, ground safety/collider/AI reaction and passenger separation.
- [ ] Propose concrete minimal scope: where/count/visible density, sidewalk and crossing logic, separate roles/appearance/pins, performance cap, offscreen spawning, vehicle yielding and failure handling.
- [ ] Compare two realistic alternatives, recommend one, record unresolved choices and acceptance tests. No production edits/commits/builds. End deliverable is proposal for human approval.

### Task 5: Research, map, integration and release (parent)
- [ ] Naver-review additional other junction approaches, save evidence log and update existing private M-style map; mark newly observed vs currently implemented.
- [ ] Run fresh task reviews and final whole-branch review, resolve actionable findings.
- [ ] Run relevant integrated models/BuildHybrid/receipt screens/real steering physics and traffic tests, verify manifest/Strict.
- [ ] Record history/LATEST/checklist and integrate completed authorized fixes to default local executable, preserve primary changes. No pedestrian implementation; final request approval of concrete proposal.

## User testing constraint (supersedes broader gates)
Run only essential focused checks for changed behavior. Baseline23model suites already passed; do not rerun all models or full stability/campaign scenarios. Task1 focused receipt models already passed. Use minimal actual steering correction/brake checks, traffic active distribution/clear spawn/short movement checks, and one final BuildHybrid plus receipt capture. Broaden only for a concrete failure caused by these edits. Human manual/campaign remains unverified.
