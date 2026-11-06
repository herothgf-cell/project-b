# 2026-11-07 승객 행동 팝업 시안·브랜치 사전 조사

## 요구사항
깨우기 등 작은 승객 말풍선에 섞인 행동을 유형별로 정리하고 시안 후 구현. 완료 후 main push와 main 외 브랜치 정리, 미반영 작업 검사.

## 현재 수행
사용자가 ‘행동이 필요한 상황만 팝업, 일반 대사는 카드 유지’를 선택했다. PassengerJobs/Behavior/Activities와 기획07/05·체크리스트·범위를 대조하고 깨우기/적재·보호/하역/음식정리/짐재고정/용무대기 및 기존 반응·요청 팝업을 구분했다.
[설계](../superpowers/specs/2026-11-07-passenger-actions-design.md) · [시안](../ui/2026-11-07-passenger-actions/concept-v1.png). 시안은 검토용이며 기존 초상/아이콘과 모델 규칙으로 구현 예정. 도로 배경/캐릭터 그림 신규 적용은 범위 아님.

## 브랜치 조사
실제 git fetch origin --prune 후 로컬 작업13개와 남아 있는 원격 작업2개의 origin/main 대비 독자커밋0. main은 HUD 출하30d5587 포함. D:/Git/project-b의 LATEST 수정·기존 미추적 문서/자료는 보존 필요. easy-road-editor worktree는 clean. 삭제는 구현/출하 완료 후 최신 상태 재검사하고 수행한다. 현재 삭제0.

## 검증/미검증
읽기·시안 생성·Git 커밋 포함 관계 조사만. 게임 코드·실행본·지도·manifest·저장 변경 없음. Unity/Player 새 검사 없음. 기존 실행 기준 은계/A2·LANv14·BuildHybrid·출하30d5587, 직전 Strict issues0 유지. 이번 시안 승인 후 구현/검증/출하/브랜치 정리가 남아 있다.

독립 설계 검토: 적재/하역 완료 후 수동 F가 필요하다는 초안의 잘못된 가정을 발견했다. PassengerJobs.AdvanceBehavior의 기존 자동 Board/CompleteDelivery를 유지하도록 설계 정정. 기존 동작 보존 원칙으로 해결.
