# 회사 제안서 V6 검토·보완

## 요구사항과 결정

2026-11-06. 회사 필수 양식과 최신 V5 PPT를 검토해 필요한 부분을 수정해 달라는 요청. 사용자 답변에 따라 제안 참여자는 미정으로 표시했다. 실제 인원을 임의 작성하지 않고 직군/기여는 배정안으로 분리했다. 기존 23장 구성·디자인과 목표 시안을 유지한 V6를 별도 작성했다.

## 실제 반영

- 최신 실제 작은 방·싱글·PVP·승객 요청 화면4개 반영, 가제/버전 명시.
- 조향·일반 감속 설정과 Space 긴급 제동 구분, 현재 은계3일/A2PVP/LANv11과 장기 미구현 차량교체/이사를 구분.
- 로드맵 단계 통과 조건, 핵심6명 인월 가정과 공유/외주 분리, 기존 노트의 검증KPI를 본문에 표시.
- 사용자 미정 참여자, 미래 역할 배정안 명시. 별도 V6 매뉴얼과 필수항목 검토표, 제안서 README 갱신.
- 공식 경쟁작 제품 설명 재확인 및 노트 출처 유지. 판매 실적/시장규모/수익 예상은 만들지 않음.

## 변경 파일

- docs/proposals/project-b-game-proposal-v6.pptx
- docs/proposals/project-b-prototype-manual-v6.md
- docs/proposals/proposal-v6-review.md
- docs/proposals/README.md
- 본 이력과 docs/work-history/LATEST.md의 이번 요약

관련 prototype-scope/checklist 및 content-design02·10~14와 최신 UI·방·제동 구현 이력, TaxiController/DrivingCamera/DrivingSettings/DriverPedalIntent 코드를 대조했다. 게임 범위는 변경하지 않아 해당 게임 체크리스트는 수정하지 않았으며 제안서 검토표를 갱신했다. 다른 채팅의 HUD 대화 시안/LATEST 변경과 시작 시 미추적 자료는 보존한다.

## 검증 결과

- bundled Node Artifact Tool로 사본 가져오기·본문/표/이미지 편집·PPTX 생성. 최종 실행 성공.
- 검증 영수증 artifacts/proposal-v6-20261106/validation.json:23장, native 표6개, 구조 findings0, 레이아웃 findings0/warnings0, 원본 Malgun Gothic 정책 통과, 최종23장 재가져오기 통과.
- 23장 PNG를 개별 시각 검토, 수정된 장은 원본 렌더와 비교. 편집 가능한 본문·표와 시안 구성 보존, 넘침 없음.
- 최종 5,854,404 bytes, SHA256 `65a16476e806234e4aae4ec7956361597a887d6db5ca5526404eb5c6950301ef`. 원본 V5 SHA256 불변 확인.
- 최종 SHA256/원본 불변/23 XML/필수 문구 확인 스크립트 exit0. 문서 git diff --check 결과는 아래 기록.

초기 원본23.5MB 가져오기는 새 런타임 construction limit 오류. 원본 보존 후 이미지1400×1000 이내/고품질JPEG 작업용 사본으로 재가져오기 성공했다. 텍스트/표는 래스터화하지 않았다. 도구 폰트정책 SHA와 native table검사 인자를 보완한 뒤 검증 통과했다. Windows hardlink EPERM은 저장소 내 검증 완료 파일의 배타적 copyFile로 대체하여 최종화 완료했다. 설치된 런타임/스킬 자체는 수정하지 않았다.

## 미검증·유지 상태

PowerPoint 앱의 실제 폰트 렌더는 미검증이다. 게임 실행/Unity 빌드/위치 제어/자동 물리/수동 플레이/전체 캠페인/Strict/배포manifest 재검사는 이번에 수행하지 않았다. UI·게임 구현 검증은 기존 이력 인용이며 새 검증으로 주장하지 않는다.

기본 실행 builds/EungyeTaxi/EungyeTaxi.exe, BuildHybrid, 은계/A2·LANv11·실행본0bba941 유지. 게임/지도입력/배포ZIP/manifest/저장 미변경. 필수 입력 Strict issues0와 배포141파일 검증 상태는 직전2026-11-06-work-history-review.md 기록 기준이다.

## 실행·재현·다음 작업

PPT V6를 열어10·14·15·19~23장을 확인한다. 시연은 별도 V6 매뉴얼에 따른다. 로컬 제작 파일은 artifacts/proposal-v6-20261106/build.mjs, 작업용 입력 source-compact.pptx, 렌더 renders/ 및 validation.json이다. Git 제외 중간 파일이다. 로컬 Node/Python은 C:/Users/USER/.cache/codex-runtimes/codex-primary-runtime/dependencies 아래 제공 런타임을 사용했다. 다른 PC는 최종 PPT/매뉴얼/검토표로 열람 가능하다.

회사 제출 전 참여자 이름·총인원·실제 기여, 범위·인력·예산·일정을 확정한다. 게임의 다음 작업은 특수승객/경유/기상·도착 안내의 실제 주행 수용, 사람3일·다른PC 점검을 유지한다. 커밋은 `git log -1 --oneline -- docs/work-history/2026-11-06-game-proposal-v6.md`로 확인한다.

문서 검증: 대상 파일의 git diff --check exit0. 다른 채팅의 기획 정리 변경은 이번 커밋에서 제외한다.
