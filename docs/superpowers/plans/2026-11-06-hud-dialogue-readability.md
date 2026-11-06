# HUD·승객 대화 가독성 적용 계획

> 실행: superpowers:executing-plans로 이 세션에서 순차 구현한다.

**Goal:** 승인된 시안의 정보 계층을 싱글/PVP에 적용하고 3구/4구 신호기 형식과 실제 접근 방향 표시를 일치시킨다.
**Architecture:** 기존 IMGUI/공통 CockpitUI와 PassengerDialogueUI의 표시만 변경한다. 신호 형식은 명시 지도 자료/렌더링이 공유하며 좌회전 권한·신호 주기/법규·수치와 독립이다.
**Tech Stack:** Unity2022.3.62f2 C#, 기존 Editor 검사·격리 Player probe.
**Spec:** docs/work-history/2026-11-06-hud-dialogue-preview.md 및 사용자 적용 승인/3구·4구 구분 요청.

## 보존과 검토

기존 단축키/응답/71선택 효과·기한·운임·5종 상태·요구치·운전 점수·LANv11/저장·지도 입력 유지. 실제 도로 근거 없이 신호 형식을 추정하지 않는다. 시안 화살표/수치는 적용 데이터가 아니다. 긴 대사, 좁은 창, 비활성 선택지, 무신호/방향 불일치, 좌회전 없는4구/명시3구를 검증한다. 기존 기획 문서 변경은 별도 작업이므로 보존한다.

## 작업

- [x] 1. 신호 형식 회귀검사 RED→구현→GREEN. `SignalHeadPresentation` 공통 표시 모델, 기존 TrafficSystem/RoadMapFacilities의 기본4구를 보존하되 명시3구 지원, DrivingReadability 연결. 형식 미상/출처/방향별 매칭 검사.
- [x] 2. CockpitUI/DrivingReadability/PassengerDialogueUI 정보 계층 정리. 내비와 운송 묶음/상단 압축/지도 아래 돈·PVP정보, 아래 모서리 상태. 대사/답변/효과를 분리하고 긴 내용 스크롤·기존 값 유지. 기존 HUD계약 검사 및 실제1280/1920 UI 확인.
- [ ] 3. 통합 BuildHybrid·싱글/PVP Player probe·기존 신호/내용/레이아웃 검사, 사용자 저장 불변과 Strict/manifest 확인. 작업 이력·체크리스트·전용 적용 문서와 선택 소스 커밋 후 기본 실행폴더 배포. 실제 수동/전체 캠페인 검증과 구분한다.

## 진행 기록

2026-11-06 시작: 원래 폴더의 콘텐츠 문서 수정과 미추적 파일을 보존하기 위해 독립 worktree를 만들었다. 현재 신호는 두 월드 생성 경로와 HUD 모두4구 고정이다. 기본을 무근거3구로 바꾸지 않고 명시 자료를 우선하는 계약으로 구현한다.

구현/검사 완료: HUD/형식16·효과72, 기존 HUD/저장/신호 회귀, 정상창 전체UI178, 최종Player90·스크롤 끝/신호8, BuildHybrid exit0. 사용자16파일 불변·Strict issues0. 소스 커밋 후 manifest/기본폴더 통합을 진행한다. 상세 증거는 작업 이력 참조.
