# 내비·승객 잡담 수정 계획

> 실행: superpowers:executing-plans. 사용자 지정 대사별 중복 방지와 카드 표시를 인라인으로 구현한다.

**Goal:** 작은 상단 내비와 반복 없는 선택적 잡담.
**Architecture:** 내비 내부 GUI 변환만 축소한다. 순수 대사 기록 모델과 리소스 문구를 기존 PassengerRequestUI snapshot 및 CockpitUI 승객 카드에 연결한다.
**Tech Stack:** Unity2022.3.62f2, C#/IMGUI, PowerShell.
**Spec:** ../specs/2026-11-09-compact-nav-passenger-chat-design.md

## 제약·검토

기존 main/사용자 자료·지도·저장 식별자를 보존한다. Quiet 위반, 긴 사용자 대사, 진행 중 새 요청 우선순위, 구저장, LAN의 권한 있는 명령을 확인한다.

- [x] 순수 모델 회귀 검사에서 같은 대사·구저장·중복 문구·소진 후 재표시를 실패/통과로 확인한다.
- [x] PassengerChatHistory.cs와 Content/passenger-chat-lines.json을 추가하고 PassengerRequestUI.cs의 snapshot/선택/카드 표시에 연결한다. 대사 7개를 추가한다.
- [x] CockpitUI.cs의 기존 승객 카드에 작은 말 걸기 입력을 두고 DrivingReadability.cs 내비를 85%로 축소한다.
- [x] 실제 Player 회귀와 화면 검사, BuildHybrid·manifest/Strict·사용자 저장 보존을 확인했다. (후속 검증/배포 이력 참조)
- [x] 독립 검토를 반영하고 기획/체크리스트/작업 이력/LATEST와 실행본을 함께 기록한다.

후속 사용자 요청으로 말 걸기 UI/답변 표시는 숨김으로 대체했다. 최종 배포/검증은 2026-11-09-passenger-cards-call-variety.md 계획·이력을 따른다.
