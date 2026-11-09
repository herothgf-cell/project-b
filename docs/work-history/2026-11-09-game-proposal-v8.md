# 제안서 V8: 재미·시스템·최신 화면

## 요청·실제 반영

ETS2 의미·비교작 중복, USP와 재미 연결, 조작/콘텐츠/아트 및 공통 이미지 최신화, 시스템별 간단 설명 요청. V7 가져오기 후 셀링/재미 통합, 경쟁작21장으로 집중·ETS2 성장참고 구분, 시스템2장/싱글·PvP 각1장으로26장 작성. 주요 시스템6영역과 현재/후속 구분. 실제 11.09 검증 캡처를 복사·교체, 둥근 택시 구매/이야기 시안2장 생성. 아트 페이지 실제 UI와 목표 시안 병렬. 사용자 편집 V6 및 V7 보존.

근거: LATEST/scope/checklist, 마인드맵 목차, content-design/01~05·10~14·18,11.09 실제 배포 이력. 운전 점수는 누적 법규/다음 승객 운임, 이번 승객 주행 만족도와 구분. ETS2 공식 about/press 확인·노트 기록.

## 파일·재현

docs/proposals/project-b-game-proposal-v8.pptx, proposal-v8-review.md, project-b-prototype-manual-v8.md, assets/v8, README 및 본 이력/LATEST. PPT3장 재미,6장 조작,7~8시스템,17~18모드,20아트,21비교작을 확인한다. 생성기/검사 영수증은 Git 제외 artifacts/proposal-v8-20261109/edit.mjs,finalize.mjs,validation-final.json. 내장 Artifact Session 편집·export 후 bundled Node finalizer.

## 실제 검증·결과

26장 PNG 전수 시각 확인, native tables5개(4/9/21/23/24), 구조pass/레이아웃0/재가져오기26장/맑은고딕 정책 통과. 최종18,210,098bytes, SHA256 `e68cc1982ed2a0b7dc998d46157d891cd98d08aa9121dca62f266fab2df05bd6`.

초기 편집 API 삭제/셀text 대입 오류는 세션 롤백 후 public aid 삭제와cell.value로 수정. 전체렌더 시간초과 후 배치렌더로 완료. 비율 조정 뒤 finalizer가 기존 최종본/receipt 덮어쓰기를 거부해 중간본 보존 및 새 receipt명으로 최종 exit0. ZIP 슬라이드/노트52파트 제외어 포함0건, git diff --check exit0.

이번 게임/빌드/지도/ZIP/저장 미변경·새게임 실행/자동물리/사람/전체캠페인/Strict/manifest 검사 없음. 현재 은계/A2·LANv16·BuildHybrid 및 필수 입력 상태는 최신 게임 배포 이력 인용. 동시 승객 반응 코드/검사와 사용자 V6수정·미추적 자료는 이번 커밋 제외. 사람 장기·실물패드·다른PC는 기존 후속. 다음은 실제 참여자·회사 승인 범위/예산 확정. 실제 커밋 확인: git log -1 --oneline -- docs/work-history/2026-11-09-game-proposal-v8.md.
