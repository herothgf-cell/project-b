# 한 창 1인 봇 경기 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 한 실행 창에서 사람이 직접 운전하며 승객·NPC·법규·정산을 시험한다.
**Architecture:** 기존 PvpNetworkSession의 권한 tick/모터와 PvpCompetitionBridge/승객 원장을 재사용한다. 로컬 모드는 transport를 생성하지 않고 두 번째 참가자를 입력 기반 봇으로 채운다. 봇은 공개 승객 snapshot으로 목적지를 고르고 같은 승하차 명령/정차 검사/법규/정산을 거친다.
**Tech Stack:** Unity2022.3.62f2, 기존 평면 차량 모터/C# 권한 모델.
**Spec:** 사용자의 ‘한 창 1인 모드 또는 봇 버전’ 요청과 기존 [19번 수용](19-driving-npc-rules-acceptance.md).

## 선택과 제약

별도1인 규칙/원장을 만드는 방법은 온라인과 판정이 달라지므로 피한다. 두 실행 창의 자동 게스트는 한 창이라는 요구에 맞지 않는다. 구현 판단으로 기존2참가자 모델의 두 번째 자리를 로컬 봇으로 채운다. 사용자가 봇 난도/최적 전략을 확정한 것은 아니다.

- 초기 화면에서 시간·유예 입력→‘혼자 경기 시작 · 봇 상대’→3초 준비→직접 운전/승객 선택/E 승하차→시간 종료 정산→나가기/다시 시작.
- 참가 인원/IP/포트/Photon/별도 실행 창/방화벽 설정 없이 실행한다. 오프라인 단순 주행·직접IP2인 진입도 보존한다.
- 봇은 합법 경로 거리로 대기 승객을 고르고, 소유 승객 목적지를 우선한다. 위치 변경·즉시 정산·미정차 승하차·특별 점수/면책을 허용하지 않는다. 택시 간 무접촉과 NPC/법규 시험 규칙은 동일하다.
- 시간/유예는 기존 범위를 검증하며10분 고정이나 새로운 영구 기본값을 만들지 않는다. 봇 경기 성적은 로컬 시험용이며 사람 상대 온라인 실력/밸런스 수용으로 취급하지 않는다.
- 캠페인/저장/지도/다른 채팅 변경을 보존한다. PC 화면/키 입력은 사용하지 않는다.

## 파일과 검사

| 파일 | 책임 |
|---|---|
| Runtime/Competition/PvpTaxiBot.cs | 공개 상태→입력·승하차 명령, 연속 운송 |
| Runtime/Network/PvpNetworkSession.cs | transport 없는 로컬2참가자/기존50Hz tick·종료 |
| Runtime/Network/PvpCompetitionBridge.cs | 봇 명령 연결·나/봇 표시·동일 원장 |
| Runtime/PvpBootstrap.cs, Runtime/UI/PvpPrototypePanel.cs | 한 창 진입/시간 검증 |
| Editor/PvpSoloBotTests.cs | 선택·소유권·마감·복수 연속 실제 모터 운송 |
| Runtime/PvpSoloBotProbe.cs, tools/test_pvp_solo_bot.ps1 | 실제 단일 Player의 운송·법규·정산·나가기/재시작·무transport 검사 |

- [x] 1. 새 봇 계약/연속 운송 검사를 작성하고 미구현 실패를 확인한다.
- [x] 2. 봇·로컬 권한 경로·한글 메뉴/HUD를 구현해 모델 검사를 통과한다.
- [x] 3. 단일 Player 운송/결과/재시작과 기존 두 Player·격리 회귀를 실행한다. 정상/위반 각각2회 운송, fixture/경계0, 위반3감점, exit/restart·무transport 확인. 독립 리뷰 중대 발견 없음.
- [ ] 4. 소스 커밋 기준 재빌드·전체 실행 폴더/manifest 검증, 이력/LATEST/로드맵 갱신·커밋·push.

수용 잔여: 사람 키보드/패드·화면 시야·봇 난도/수익 경쟁 체감·다른PC통신/장기 부하. 독립 리뷰는 전체 기능 검사 뒤1회 수행한다.
