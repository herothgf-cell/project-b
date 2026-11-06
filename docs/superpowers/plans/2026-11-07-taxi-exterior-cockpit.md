# Taxi exterior and cockpit Implementation Plan

> Use superpowers:executing-plans for inline implementation, then one independent final review.

**Goal:** 곡면 택시 외형과 전방을 덜 가리는 1인칭 운전 표시.
**Architecture:** TaxiArt는 충돌 없는 렌더 메시만 생성. TaxiController의 물리 유지. DrivingCamera는 기존 시점에 새 cockpit 시각물 부착. CockpitUI는 시점에 따른 계기판 분기만 추가.
**Tech Stack:** Unity2022.3 Mesh, Standard materials, existing IMGUI.
**Spec:** docs/superpowers/specs/2026-11-07-taxi-exterior-cockpit.md

## Constraints / review
물리/신호/경제/LAN 유지. 모든 외형 layer8, 운전석 layer10; 1인칭 외부 차체 숨김/후방 운전석 숨김. 추가 충돌 금지. 곡면 normals/삼각형/유효 bounds, 실제 타이어 회전과 시점전환, 1280/1920 대화·속도 가림 확인.

- [ ] TaxiArt 곡면 메시/차체/등/휠 생성, 기존 외형 교체. 물리 값/추가 collider 없는지 검사.
- [ ] cockpit 둥근 대시보드·연속 핸들, 조향 시각 연동, 낮은 디지털 계기판. 3인칭 기존HUD 보존.
- [ ] 실제 exterior/cockpit/좌우/후방1280/1920 캡처·물리/카메라 회귀, independent review, 통합 빌드/manifest 검증.
- [ ] 기획/체크리스트/이력 갱신 및 출하. 기존 사용자 파일 보존. 필요없는 새 브랜치 생성하지 않음(이미 격리 worktree).
