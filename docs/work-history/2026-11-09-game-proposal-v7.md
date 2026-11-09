# 제안서7차: 순서·타이틀·표지 개편

요청: 최신 사용자 편집 V6를 기준으로 개요를2페이지부터 배치하고 게임성/세계관/시장/로드맵 순서로 개편. 제목을 페이지 타이틀답게 꾸미고 표지를 현재 구현과 시안에 맞춰 조정.

## 반영

원본23장 가져오기, USP·핵심재미2장 추가해25장. 원본 V6 미변경 보존. 섹션명·큰 제목·노란 강조선, 표지는 현재 둥근 노란택시/한국 동네와 기존따뜻한3D 방향으로 내장 image_gen 신규 생성. 개요2~5/게임성6~17/세계관18~19/시장20~21/로드맵22~25. 참여자 미정, 핵심6명·12개월은 제안 가정. V7 별도 매뉴얼, 구성 대조표 및 README/LATEST 갱신. 승객·방·차량·이사 이미지 유지.

## 파일·재현

docs/proposals/project-b-game-proposal-v7.pptx, proposal-v7-review.md, project-b-prototype-manual-v7.md, assets/v7 이미지/프롬프트, README 및 본 이력/LATEST. PPT를 열어2~5 개요와22~25 로드맵/스펙을 확인한다. 로컬 재현 원본 및 중간 자료는 Git 제외 artifacts/proposal-v7-20261109/edit.mjs, finalize.mjs, validation.json. 내장 Artifact Session으로 V6 가져오기/수정/내보내기 후 bundled Node finalizer 실행.

## 실제 검증

25장 PNG 전수 확인, 구조pass/레이아웃findings0/재가져오기25장 통과·native table6개/맑은고딕 정책 통과. finalizer 첫 실행은 필수표 정책인자 누락으로 종료1, 인자 보완 후 종료0. ZIP 슬라이드/노트50파트 제외어 포함0건. 최종 7619652bytes, SHA256 `f2f8c850c463b8488edff965381d32a4f801e4a528c4f52e922b33bc493d5147`. git diff --check는 커밋 전 실제 결과를 확인한다.

게임/빌드/지도입력/ZIP/사용자 저장 미변경. 게임 재실행·물리/사람/전체캠페인·Strict/manifest 새 검사 없음. 현재 은계/A2·LANv16·BuildHybrid, 필수 로컬 입력 상태는 최신 실제 게임 작업 이력을 인용한다. 사람 장기·패드·다른PC 검증은 기존 후속. 기존 V6 사용자수정·미추적 자료는 커밋에서 제외. 다음은 회사 제출용 실제 참여자·범위/예산/일정 확정. 커밋은 git log -1 --oneline -- docs/work-history/2026-11-09-game-proposal-v7.md로 확인.
