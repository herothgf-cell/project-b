# Passenger action popups Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 승인된 행동 전용 팝업을 기존 승객 처리에 연결하고 main 출하 후 브랜치를 안전하게 정리한다.
**Architecture:** PassengerActionUI는 실제 모델을 읽는 표시/접기 상태만 소유한다. PassengerActionPresentation은 종류·문구·숫자행동·자동열기 상태를 분리한다. 기존 Interact/Respond API와 호스트 권한을 유지한다.
**Tech Stack:** Unity2022.3 C# IMGUI, existing numeric input and bright UI.
**Spec:** docs/superpowers/specs/2026-11-07-passenger-actions-design.md (2026-11-07 승인)

## Global Constraints
기존15초 깨우기/30초 자동하차·정차조건·자동 적재/하역·저장·LAN 유지. 시안의 새 배경/초상은 적용하지 않는다. main push/브랜치 삭제는 사용자 승인 범위이며 미커밋 파일/작업폴더를 삭제하지 않는다.

## Review Focus
타이머 갱신마다 재열림 금지, 다음 운송의 같은 상태 정상 열림, 숫자/F 중복 실행 방지, 다른 모달 중 입력 금지, LAN 복제 객체 교체시 UI 상태 유지.

## Tasks
- [ ] Editor 검사로 표시 모델/접기 계약의 누락 실패 확인. PassengerActionPresentation.cs와 PassengerActionUI.cs 구현, 메타 생성. 상태종류/안전조건/기존 Interact와 숫자 입력 연결.
- [ ] DriveSession 부착·CockpitUI 요약/행동 보기·PassengerBehaviorUI의 일반 발언을 카드에 연결, 작업팝업 중 중복 숨김. 조건/기존 요청은 그대로 유지. Editor 상태/배치검사 통과.
- [ ] BrightUIProbe에 실제 깨우기/작업 상태 캡처와 접기·자리이탈/입력/정산 검사. BuildHybrid 및 실제1280/1920, 모델·LAN 회귀, 사용자저장/manifest 검증. 신선한 독립 코드 검토 후 확정.
- [ ] 이력/기획05·07/체크리스트 갱신, main의 새 완료 변경 보존 통합·소스/배포 커밋·push.
- [ ] 최종 fetch와 모든 브랜치 포함 관계·worktree dirty 검사. 커밋 포함된 worktree는 detach로 파일 보존, main 외 로컬/원격 브랜치 제거, 결과 이력 main push. 미반영 작업은 조사/보존 통합 없이 삭제 금지.

## Ledger
승인된 기존 자동승하차 보존이 설계보다 우선한다. 구현은 부모가 순차 수행, 최종 전체 검토만 독립 agent 사용(executing-plans 규칙).
