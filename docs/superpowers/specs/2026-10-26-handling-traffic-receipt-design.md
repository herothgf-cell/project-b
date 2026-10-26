# Handling, traffic and receipt design — 2026-10-26

User intent: make lateral driving responsive, see ordinary cars across more of the existing district, simplify the completed-trip receipt, and mark additional Naver-confirmed U-turns on the private M-style guide. Review pedestrian planning and request human approval at the end before implementing it.

## Binding scope
- Receipt: retain the initial expected fare and actual received fare. The middle line is only `보너스` with signed net difference `paid - InitialFare(result)`: positive `+N`, zero `0`, negative `-N`, currency won. Remove 약정/감액 components and base-fare/bonus rule explanation from the completed-trip panel. Preserve actual settlement calculations, records and save compatibility.
- Handling: diagnose the manual steering automatic slowdown using actual wheel angle, not just normalized input. Small steering corrections must retain throttle and add no automatic braking at normal road speeds. Make input/return responsive without increasing the tire lateral acceleration cap or breaking H steering, service brakes/reverse or low-speed turns. Verify real Rigidbody driving as well as focused math checks.
- Traffic: improve spatial distribution of ordinary moving vehicles over valid drivable routes. Diagnose visible vs allocated cars and stop-only seeding first; prioritize distribution over merely raising a global count. Retain safe occupancy/static-route validation, signal/queue behavior and valid continuation. No visible teleporting/repopulation on top of the player.
- U-turn research: inspect other mapped junctions in Naver Street View, record precise approach/sign condition/month/source and distinguish confirmed, unchecked and unconfirmed. Update only the explanatory map outside game assets; do not add unapproved U-turn movements to the game.
- Pedestrians: assess existing scenario walkers, editor crosswalk walkers and passenger actors. Propose a small signal-following sidewalk/crosswalk population with visibly distinct passengers. No pedestrian runtime implementation before explicit human approval.

## Choices
Keep the existing Unity shared map/physics/traffic and compact receipt. Avoid changing the economic rules or adding a traffic simulation framework. Dimensions/tuning remain game parameters. Existing unrelated recovery-easing plan stays unimplemented in this task. Existing primary user files stay untouched.

## Acceptance
Focused RED/GREEN tests for bugs, relevant existing models, one integrated BuildHybrid, packaged receipt captures, physical steering checks and traffic distribution/continuation checks. Build manifest SHA256 and Strict input verification. Distinguish controlled poses, NPC kinematic driving, real Rigidbody and human manual play. Pedestrian proposal must be concrete and reviewable before the final approval question.
