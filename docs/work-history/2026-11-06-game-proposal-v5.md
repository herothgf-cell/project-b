# 제안서 5차: 필수 양식 검토와 승객 반응

## 요청과 반영

필수 양식 포함 여부 검토 및 취객·잠자는 승객 반응 표현 요청. V4 20장에 요청/대응/결과1장과 취객·수면2장을 추가해23장 V5를 작성했다. image_gen으로 기존 따뜻한3D카툰 컨셉에 맞춘 시안2장을 생성했다. 세계관/캐릭터·아트 스타일·경쟁작별 한계를 본문에 보강했다. 단골·예약 제외 유지, 참여자 실제 이름/인원은 사용자 승인대로 회사 제출 전 확인 필요다.

관련 scope/checklist, content-design02·10~14와 요청 판정 문서, PassengerJobs.cs, 작은 방 구현 이력을 대조했다. 유형 응답과 개별 요청 수락, 조건부 구토와 세차, 수면 중 무응답 중립·도착 후 운임 확정 정책을 구분했다. 작은 방 핵심 구현과 후속 비교화면/장기 차량·이사를 구분해 매뉴얼도 갱신했다.

## 변경 파일

- docs/proposals/project-b-game-proposal-v5.pptx
- docs/proposals/assets/v5/ 이미지2개 및 prompts.md
- docs/proposals/proposal-v5-review.md, README.md, project-b-prototype-manual-v3.md
- 본 이력 및 LATEST.md의 제안서5차 항목

기존 V4 보존. 다른 작업의 게임/빌드/설정 및 LATEST 운전설정 항목은 이번 커밋에서 제외한다.

## 검증과 결과

- bundled Python으로 artifacts/proposal-v5-20261106/revise.py 실행 exit0, bundled Node build.mjs 실행 완료.
- 최종화 영수증 validation.json: 23슬라이드,6 native tables, 구조 findings0, 레이아웃 findings0, 재가져오기23장 통과, Malgun Gothic 정책 통과.
- PNG23장을 모두 시각 확인: 제목/본문 잘림 없이 승객 시안과 오른쪽 요청/대응/결과 배치 확인.
- 최종 PPT 23,506,346bytes, SHA256 `5068675ee5476b2b5ba73c3b781f0097bf562b57cada43a73321d19cb21676a9`.
- ZIP의 슬라이드/노트46개 XML 검사: 단골/예약 포함 파트0. `git diff --check` 대상 문서 검사 exit0.

실제 PowerPoint 앱 폰트 렌더는 미검증. 새 게임 실행·빌드·수동 플레이·자동 물리·전체 캠페인·Strict·manifest 검사는 수행하지 않았다. 게임/지도/ZIP/사용자 저장 미변경. 현재 실행은 builds/EungyeTaxi/EungyeTaxi.exe / BuildHybrid / 은계싱글·A2PVP이며 LAN 버전과 필수 입력의 최신 검증은 동시 게임 작업 이력을 따른다.

## 재현·다음 작업

PPT를 열어5~7장 승객 반응,17장 비교,21장 참여자를 검토한다. 별도 조작은 docs/proposals/project-b-prototype-manual-v3.md. 생성 이미지 원본과 프롬프트는 assets/v5/prompts.md. 로컬 생성기는 Git 제외 artifacts/proposal-v5-20261106/build.mjs이며 bundled Node/Artifact Tool로 최종화했다. 회사 제출 전 참여자 실정보와 개발 인력·예산·일정을 확정한다. 커밋 확인: `git log -1 --oneline -- docs/work-history/2026-11-06-game-proposal-v5.md`.
