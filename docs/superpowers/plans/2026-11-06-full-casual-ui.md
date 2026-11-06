# 전체 캐주얼 UI 적용 계획

> 직접 통합과 독립 화면 그룹의 병렬 구현. 실행은 executing-plans 및 dispatching-parallel-agents의 파일 소유 범위를 따른다.

**Goal:** 승인된 최초 시안을 기존 전체 게임 화면에 일관되게 적용한다.
**Architecture:** IMGUI와 게임 모델을 유지하고 공통 CasualUI에 헤더/초상/배경/강조 행동을 추가한다. 독립 화면을 그룹별 변경하고 마지막에 실제 통합 실행본으로 확인한다.
**Tech Stack:** Unity2022.3/C#/IMGUI, Resources PNG 초상 atlas.
**Spec:** docs/ui/2026-11-06-full-ui-direction.md.

## 공통 API/파일 소유

- 루트: CasualUI/KoreanText/신규 UI art helper, 모드/PVP 진입, Cockpit/네비/지도/도움말·일시정지·기록/연습, 전체 문서/빌드/검증.
- 대화/운영 그룹: PassengerDialogueUI, PassengerBehaviorUI, PassengerRequestObservation, ScenarioChoiceSituation, ServiceStops(+PlannerUI), DriveNotifications, AccidentController/VehicleDamageController의 표시 부분.
- 결과/생활 그룹: OneDayHUD, RideCompletionPanel, DrivingSettingsPanel, PersonalRoomHUD, PvpSimulationMatch의 결과 표시 부분.
- 공통 새 API: `CasualUI.Header(Rect,string,int glyph=1,Color? accent=null)`, `CasualUI.Portrait(Rect,int variant=0)`, `CasualUI.Backdrop()`, `CasualUI.Divider(Rect)`. 기존 Panel/Action/Bar 및 KoreanText.Button은 유지한다. 새 초상은 순수 장식이며 모델/판정 입력이 아니다.

## 작업과 검증

- [x] 화면 목록/기존 기능 대조·최신 main 동기화, UI용 초상 생성/저장/검수.
- [x] 공통 토큰/스킨/헤더/초상과 메뉴·주행·지도/연습 통일.
- [x] 대화/운영 그룹 구현·원래 조건/긴 효과·클릭/키보드 보존 검토.
- [x] 결과/생활 그룹 구현·지급/구매/설정 적용/취소·경기 시계 보존 검토.
- [x] 전체 소스 diff/독립 검토, 기존 Editor/테마·네비 및 관련 계약, BuildHybrid.
- [x] 임시 저장/설정 키를 쓰는 실제 화면·주요 입력/마감/모드 전환 검증. 실패가 있으면 해당 부분만 수정/재검증.
- [x] 실제 스크린샷/변경·검증/남은 항목 기록·필수 입력/manifest 전수 검증, 최신 main 보존 통합 후 게시.

맵/차량 아트와 새 시스템 개발은 보류한다. 사용자 승인된 진행/게시 범위 안에서 실행하며 추가 설계 승인 단계는 만들지 않는다.
