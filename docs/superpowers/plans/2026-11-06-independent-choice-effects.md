# Independent choice effects implementation plan

> **For agentic workers:** Use superpowers:subagent-driven-development or superpowers:executing-plans to implement task-by-task.

**Goal:** 선택지별 즉시/운행중/종료 만족도·기사 피로 효과를 실제게임·관리창·저장/LAN과 함께 구현한다.
**Architecture:** 순수 효과 ledger, 엄격한schema2/구프로필이관, PassengerJobs의호스트권한수명주기, 관리창단계편집을분리한다.
**Tech:** 기존Unity2022.3.62f2 C#/.NET, Node표준라이브러리, 기존HTML/CSS/JS.
**Spec:** ../specs/2026-11-06-independent-choice-effects.md

## Global constraints
0=추가효과없음; 기존기본동작/구저장/사용자profiles보존. 임의스크립트/새감지기없음. profile240KiB/로컬인증유지. 별도작업미커밋보존. 최종BuildHybrid/기본exe/manifest확인.

## Review focus
- 반복 응답/저장/재접속으로 즉시/종료 효과 중복 지급 금지.
- 모든0, 음수, %/분,경계0/100,프레임 분할과일시정지/서비스시간.
- schema1 검증/hash/수정한legacy값 이관, schema2누락/임의키거부,원본미변경.
- 사고/견인/픽업취소/도착확정/구캠페인·호스트vs참가자/봇.
- 실제게임 소비와관리창표시 일치, ETA각답변/공감독립값/Food·Comfort중복없음.

## Tasks
- [x] 순수코어: ChoiceEffects.cs + 모델테스트. 0/경계/독립/단계/활성화/멱등성/복사복원·취소검사.
- [x] 카탈로그/codec: choice-catalog.mjs, profile.mjs/server, ContentTuning.cs, config+Resources, 이관/보안/크기/동등성검사.
- [x] 관리창: content-model/app/CSS, 각선택지3단계+유효결과+명시단위+0안내, livepreview/프로필기능/DOM검사.
- [x] 런타임: PassengerJobsChoiceEffects.cs, 선택성공hook, 실제판정/DriverCondition/M/snapshot검증/구운송/LANv9/실제코어미리보기.
- [x] 전체모델·Node·UnityEditor·Player·LAN/이어하기/독립설정상충 검사, BuildHybrid/기본사용자파일보존·manifest갱신/검증, 브라우저QA/문서·이력/선택커밋/최종실행.

## Workspace ledger
canonicalC:/git/project-b e34f60e에는미추적로컬자료만있음. 현재외부관리worktree는이전미커밋구현이많아건드리지않는다. 승인기능은artifacts/choice-effects-system에git archive한최신source로개발후지정파일만canonical에통합한다. UnityLibrary/map검증은현재canonical에서최종수행한다. 프로필sample/active시작SHA는이전과같음. 작업중예외/검증은여기에계속추가한다.

Integration progress: 최신 보행자 소스9275f2a/출하6e73f0f를 보존해 canonical에80개 지정 소스/문서를 통합했다. 후보 Editor72/32/3·BuildHybrid0·Player54·LAN60+76/재접속·Node56·Strict issues0 통과. Canonical 최종 빌드/실행/배포 영수증은 아래 후속으로 남긴다.

Final verification: canonical source c116ce4, BuildHybrid exit0, Editor72/32/3, Player54, LAN60+76/재접속/상충프로필 exit0, Node56, Strict issues0, 사용자16파일 변경0. 출하140파일116826789바이트·통합/PVP alias VerifyOnly 각각exit0. 숨김 대화 fixture는 HWND 미반환으로 실제 마우스/렌더 검증 미완료. 사람/다른PC 수용 및 기준 소스에서도 재현한 기존 회귀3건은 작업 이력에 구분했다.
