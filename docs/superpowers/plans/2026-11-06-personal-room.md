# Small room implementation plan

Goal: ship the approved13 spec in current three-day prototype.
Spec: docs/content-design/13-three-day-room-and-first-purchase.md

- [x] Account/ownership: tests first for purchase, insufficient cash/debt, duplicate, operating net invariant and snapshot restore. Room state lives in account snapshot and resets with new campaign.
- [x] Integration/UI: summary optional entry, fixed-view furnished room, lamp/rug two styles, preview/confirm/purchase, placements persistent, completed room read-only after explicit finish. Transaction save failure rollback.
- [ ] Validate pure regression suites, Unity compile/build, isolated player room probe/screenshots, distribution manifest. Update scope/checklist/history and commit selected changes.

Architecture: CampaignAccount owns room state and personal transactions; BusinessProgress exposes saved mutations; dedicated PersonalRoomHUD renders and OneDayHUD links. Preserve original saves and PVP. Price5000/8000 trial values. No performance buffs.
