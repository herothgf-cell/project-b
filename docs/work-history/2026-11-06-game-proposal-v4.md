# 시안 중심 PPT 4차 개편

## 요청·실제 작업

작은 방 꾸미기·차량 구매·이사를 기존 밝은 카툰 콘셉트로 이미지화하고, 텍스트 위주 PPT를 간결하게 바꾸라는 요청. 내장 image_gen으로 방/차량 비교/이사 전후/재출발3컷 총4장을 생성해 프로젝트에 복사했다. 기존32장 V3를20장 V4로 재구성, 필수 항목 유지·상세 규칙/수치/근거는 노트로 옮겼다. 원본 V3와 기존 시안은 보존했다.

변경: `docs/proposals/project-b-game-proposal-v4.pptx`, `assets/v4/`의PNG4개와`prompts.md`, `proposal-v4-review.md`, `README.md`, 본 기록·LATEST. 별도 조작 문서는 기존V3 매뉴얼을 사용한다. 비공개 생성기/렌더/검증 영수증은 `artifacts/proposal-v4-20261106/`에 있다.

## 검증

- LATEST·prototype-scope·checklist·생활기획13/14·V3의기획10~12 근거와 현행코드/배포 경계를 확인. 게임 작업의 동시 미커밋 소스는 출시된 기능으로 간주하지 않음.
- 번들 Node `build.mjs` 종료0. 로컬 하드링크 제한은 자동 승인된 권한 확장으로 최종화.
- PPT20장·편집 가능한 표5개, 구조/레이아웃 finding0, Artifact Tool 재가져오기 통과, 폰트Malgun Gothic 정책 통과.
- 생성 이미지4개 및 전체20장PNG를 개별 열람. 겹침/잘림 없음. PowerPoint 앱 검사는 수행하지 않음.
- Python zipfile/XML 비교 종료0: 편집 가능한 본문 V3 7,560자 → V4 3,005자, 약60% 감소(노트·이미지 내부 문자 제외). 단골/예약 본문0건.
- 최종19,531,626바이트, SHA256 `469670c5f5f46de5c7de4b2fdd10354c7a8222fb56932700a4c477fcc69503d7`.
- `git diff --check` 종료0. 동시 게임 개발/빌드/기존 staged 변경을 이번 커밋에 섞지 않는다.

## 미검증·실행·인계

게임 기능·지도·빌드·저장·프로필·manifest는 이번 작업에서 변경하지 않았다. 게임 실행/위치제어/물리주행/수동플레이/전체캠페인·Strict/manifest 검사는 없음. 동시 방·조작설정 구현 작업은 해당 이력이 기준이다. 기존 기본 실행 `builds/EungyeTaxi/EungyeTaxi.exe`, 은계싱글·A2PVP·LANv9 / `PrototypeBuild.BuildHybrid`, 필수 지도입력/ZIP은 기존 이력 상태를 인용한다.

`docs/proposals/project-b-game-proposal-v4.pptx`를 연다. 이미지별 완전한 프롬프트와 경로는`assets/v4/prompts.md`에 기록. 재생성은 로컬 번들 Node와`artifacts/proposal-v4-20261106/build.mjs`, 새 출력명을 사용한다. 회사 제출 전 실제 참여자·범위·인력/예산/일정을 확인하고 시안은 게임의 실제 화면과 구분한다.
