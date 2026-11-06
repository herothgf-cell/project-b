# Merge Driving Fixes with Published Main UI

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to execute this integration in place; user explicitly selected merge and push to main.

**Goal:** 최신 main UI를 보존한 채 ecd6a65까지의 운전 수정을 main에 병합·재빌드·검증·게시한다.
**Architecture:** df3e693에서 통합 브랜치를 만들어 실제 merge 부모를 보존한다. UI 충돌은 main 스타일과 새 동작을 결합하고 실행 폴더는 통합 소스로 재생성한다.
**Tech Stack:** Git, Unity2022.3.62f2, 기존 FullUIBuild/Player/LAN 검증.
**Spec:** ../../work-history/2026-11-06-merge-driving-with-main-ui.md

## Global Constraints
- main 카드/초상/헤더/행 테마·기존 콘텐츠/대사/가격/저장·지도 입력/ZIP을 보존한다.
- 브레이크5항목·실제 NGII 좌회전·하단 공업사·LANv11 유지. 원격/로컬 main에 강제 푸시/리셋 없음.
- 미추적 사용자 캐시·복사 기획·active 프로필은 source/배포에 섞지 않는다.
- main 기존 바이너리/manifest는 임시 충돌 기준일 뿐, 재빌드 없이 게시하지 않는다.

## Review Focus
- Warm 속도 카드/초상/본문 구분선이 충돌 해결에서 사라지지 않는다.
- 5번째 슬라이더가605높이 테마 카드 안에 들어간다.
- 일시정지/경기 종료·입력 안전 차단과 LAN 계수 검증이 함께 유지된다.
- 사용자 저장/개인 preference를 실제 검사에서 덮지 않는다.
- push가 기존 원격 main의 새 커밋을 덮지 않는다.

### Task 1: 소스 충돌 통합
- [x] 실제 main/원격/ecd6a65/공통 조상·기존 변경 확인, origin fetch.
- [x] codex/merge-driving-main에서 no-ff/no-commit 병합, main UI+새 동작 명시적 결합.
- [x] 신선한 읽기 전용 소스 검토, 미해결 충돌0·main 테마/콘텐츠/초상 원본 차이0 확인.

### Task 2: 통합 실행 검증
- [x] FullUIBuild.Build에 BrakingSettingsChecks.All 연결·통합 아트/테마/조향/감속/지도/HUD/콘텐츠·BuildHybrid exit0.
- [x] 실제 전체 UI112·메뉴4·새 운전 싱글17/PVP15·LAN51+64/재접속.
- [x] 보호17파일 hash·Strict/문서 링크/공백 검사.

### Task 3: source/배포/main 게시
- [x] 실제 merge source 커밋, 두 부모·실제 hash 기록.
- [x] 통합 runtime의 manifest 크기/SHA256 재생성·통합/PVP VerifyOnly·배포 커밋.
- [x] 원격 main 최신 여부 확인·로컬 main fast-forward·일반 push·원격 hash/운전 커밋 조상 여부 확인.
- [x] 문서 게시 영수증도 push하고 최종 로컬/원격 hash 일치 확인.
