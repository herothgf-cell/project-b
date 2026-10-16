# Eungye Hybrid Implementation Plan

Execute inline using executing-plans; user explicitly requested implementation of the preceding combined-map design. Spec: ../specs/2026-10-15-eungye-hybrid-design.md.

- [x] Compiler and tests: generate provisional lanes/turns, preserve HD edges, identify covered lanes, explicit provisional portals; editable rules and source provenance.
- [x] Shared graph and stops: district connectivity, right-side service stops, navigable approaches, route costs and performance.
- [x] Gameplay integration: campaign and jobs remain active in hybrid; HUD status/setting IDs, signals from provisional settings; executable launcher.
- [x] Verify: all service pairs, raw-HD integrity, override restrictions, region/HD physics, full ride lifecycle, independent review, build.

## Ledger

- Work in existing feature branch with prior user changes intact. No raw data publication or Git push.
- Ruling: user approved the preceding combined-map proposal and explicitly requested proceeding; implement within that scope without another authorization loop.

## Verification record — 2026-10-15

- Unity BuildHybrid exit 0: 1,765 directed edges, 24 provisional portals, 12 stops / 132 directed service pairs. Largest service component 922 edges; 84 source HD edges in it. Other disconnected or terminal roads are not promised as return-trip service destinations.
- Original 444 HD graph edges retain IDs, coordinates and existing adjacency. All stop overshoot circuits, junction turn overrides, weighted detour and 80 rotated-signal time samples pass. Existing HDNetworkTests: five cases pass.
- Full player test exit 0: source→local and local→source physical passenger trips, actual pickup/dropoff/payment, driver-only throttle/brake, no sampled static-body overlap. Max lateral error 0.911m / 1.065m and tilt 0.570° / 0.477°. Traffic is disabled for this physical path test; this is not a whole-map traffic soak test.
- Final player targeted test exit 0: all stop routes, campaign active, H with no call, manual deviation rebuilding, H rearm, roadside pickup replacing free guidance. Final localized free-guidance/test-mode edits were checked with -hybridTest -hybridQuickTest; the full physical ride report is retained separately.
- Independent review findings addressed: no-call H regression, overshot same-edge destinations, per-junction NPC signal orientation and free-guidance recovery. Source geodata remains ignored; no Git push.
- Evidence: artifacts/hybrid-build.log, artifacts/hybrid-drive-test.txt, artifacts/hybrid-quick-test.txt. Launcher: Play-Eungye.cmd.

## District traffic extension — 2026-10-15

Implemented user-requested whole-map NPC traffic inline, reusing TrafficCar collision/following and HdRoadGraph navigation connections. DistrictTraffic distributes 24 NPCs across ten physically clear service endpoints, continues trips without end-of-route teleporting, and controls 72 mapped/provisional intersection areas. Configured signal junctions retain their lamps/rules; inferred areas remain explicitly unsignalized. Static-obstacle routes and endpoints without onward clear journeys are excluded.

Each route stores separate controls for each junction traversal, including revisits. Signal stop alignment is local to that traversal. Expired visits release once per car; distant future red visits cannot release a current traversal reservation. Shared-stream exemptions require straight movement and matching geometry. Independent review identified repeated-visit and future-release issues; both were repaired with regression assertions.

Final validation (all exit 0):
- BuildHybrid: original hybrid/HD tests plus DistrictTrafficTests, 132 NPC service-pair routes, both turns and configured signals, repeated-junction control and reservation protection.
- District player: 24 cars / 180 simulated seconds, total 27,906m, nine trip continuations, both source areas and turn directions, zero sampled overlaps/offpath and zero cars stalled over the last minute. The player taxi and automatic incidents are excluded to isolate NPC traffic. Three-car real-map red stop / green queue-clear test passes.
- Existing GreenFlowSmokeTest: clear green, following green and three-car convoy retain 9m/s without junction waits; red/yellow, blocked exit, occupied conflict, one-way road checks pass.
- Hybrid quick player regression: all service routes, campaign, free H, manual deviation recovery and roadside pickup pass.
- Evidence: artifacts/district-build.log, artifacts/district-traffic-test.txt, artifacts/green-flow-smoke.txt, artifacts/district-hybrid-regression.log. Final executable: builds/EungyeTaxi/EungyeTaxi.exe; launcher unchanged.
