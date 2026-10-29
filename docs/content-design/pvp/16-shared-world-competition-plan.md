# 공유 교통·승객 경쟁·정산 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans. 구현은 현재 worktree에서 직접 실행하고 마지막에 독립 리뷰1회.

**Goal:** 공유 신호/이동 장애물 검증 후 두 Player가 실제 서버 승객 소유권·운송·원장·결과를 공유한다.
**Architecture:** NGO 입력 경로 유지. 서버 tick에 교통→차량→시간/이탈 경계→사건→같은 tick 승하차 중재→원장→스냅샷. 클라이언트는 위치/운임/소유자를 제출하지 않음. 원장과 종결 상태는 서버 메모리에서 한 번 확정한다.
**Tech Stack:** Unity2022.3.62f2, NGO1.12.0/Transport1.5.0, 정적 Editor Run 검사와 독립 Windows Player.
**Spec:** pvp/01,02,03,04,05,07,08,09 및 사용자 이번 후속 지시.

## 판단과 제약

- 사용자 요청으로 다른PC·사람 조작 수용은 별도 미완료 목록으로 유지하면서 Task4/5까지 진행. P2 전체 수용으로 표시하지 않는다.
- 공유 신호/장애물은 시험장 중앙 연결로의 최소 권한 장면. 일반 NPC/교차로 법규 전체 구현은 Task6 잔여다.
- 게임 수치는 시험 설정으로09에 기록: 50Hz, 2인, 차량A/무접촉, 신호5초 녹색/5초 적색, 이동 장애물8초 왕복, 운임1000원+100m단계1000원/포함0,100원/점, 안전1/10, 정차3m/25도/0.35m/s/0.2초. 경기 시간은 호스트 필수 입력, 제품 기본값 없음.
- 공유 공급은 유효4정차점의 방향/최단 합법 경로로 생성, waiting 목표3. 종료 공급은 새ID, 기존 종결 객체/응답은 재개하지 않음. 원장·요청 캐시는 제한을 두고 상한 시 명시 거절한다.
- 서버 전체 재시작 복구/호스트 이전은 기존 미지원 유지. 같은 프로세스 복귀에 소유권·요청 결과·확정 점수/종료 스냅샷 포함.
- 네트워크 경기 검사는 서버 전용 위치 제어 fixture를 사용한다. 실제 메시지/중재/정산 검사이며 물리 전체 코스 주행·사람 조작과 구분한다. 클라이언트 위치 이동 API를 추가하지 않는다.

## 작업

- [x] 1. Runtime/Traffic/PvpTrafficAuthority.cs + Editor/PvpCompetitionTests.cs. 신호·다음 전환tick·장애물 상태/접촉 차단의 red→green. 권한 상태만 화면에 표시, 예측 재실행은 사건을 생성하지 않음.
- [x] 2. Runtime/Match/PvpRoundController.cs, Runtime/Passengers/PvpPassengerAuthority.cs/Commands.cs. 공통 시작/마감·정차 기하·정차 유지·빈차·revision·시드 중재·중복 요청·종결 상태 검사. 같은 tick 요청 순서 역전에도 같은 승자.
- [x] 3. Runtime/Scoring/PvpScoreLedger.cs/SettlementService.cs. 완료 상태와 운임/안전 보너스 원자 확정, 사건 상향 차액·옛 안전 구간·checked 정수·음수/동점·무운송 제외·최종 자격 동결 검사.
- [x] 4. Runtime/Network/PvpCompetitionBridge.cs + 기존 session/probe. 준비·호스트 필수 시간·카운트다운·목표/승하차·거절·원장 구성/결과·길안내·서버 상태 복귀. 최신 상태 전체 집계 snapshot과 최근 내역으로 누락을 복구.
- [x] 5. tools/test_pvp_competition.ps1. 실제 두 Player 공유 교통 checksum/동시 요청/중복 완료/공통 종료·결과 checksum, 운송 중 복귀와 지연손실 검사. 기존 재접속·네트워크·모델·Strict/배포 회귀.
- [x] 6. 독립 리뷰·수정 후 이력/LATEST/로드맵·수치 후보 갱신, 커밋·feature/pvp-prototype push. 다른 checkout/main/배포는 보존.

## 리뷰 초점

동일 tick 중재의 수신 순서 편향, 마감/유예 만료 동시 경계, 반대 차로/복구 상태 승하차, 완료 실패 중간 상태, 오래된 사건 상향의 다음 안전 구간 오염, 재접속 중복 지급/클라이언트 임의 위치·시각·운임 제출, 스냅샷 크기/역순/누락, 준비 취소·명시 이탈 자격 동결.
## 실행 판단·결과

핵심 모델33검사·최종 clean build·Player3시나리오·이전 network/reconnect 회귀 통과. 상세 로그/실패 재현과 미검증은 날짜별 이력을 따른다. 후기 판단: null 경기 JSON은 명시 presence로 분리, 첫 양수 감점의 안전 구간 귀속, MatchId로 이전 경기 요청 차단, 소유권 변경 목표의 길안내 해제. 실제 다른PC/사람 게이트는 사용자 지시대로 별도 유지.
