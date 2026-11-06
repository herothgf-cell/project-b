# 프로젝트 전체 점검 기록

## 요구사항과 결과

- TAXI LIFE/유로 트럭 비교, 스펙상 차별화와 부족한 요소, 개발/마인드맵 공백, 추가 진행 순서를 요청받았다.
- [전체 점검](../future-design/2026-11-06-project-wide-review.md)에 공식 경쟁작 자료·현행 코드/스펙 대조, 구현/검증 잔여/후속/보류, 기존 실패2건, 문서 정합성과 우선순위를 기록했다.
- 게임 기능·수치·지도·배포·설정/저장·마인드맵 원본 변경 없음. 신규 구현 승인이나 상세 구현 계획이 아니다.

## 변경 파일

- `docs/future-design/2026-11-06-project-wide-review.md`: 점검 보고서.
- `docs/work-history/2026-11-06-project-wide-review.md`: 요구사항·근거·검증/인계.
- `docs/work-history/LATEST.md`: 점검 입구/실행·다음 작업.
- `docs/prototype-development-checklist.md`: 점검 범위와 후속 구분.

## 확인한 자료와 검증

- 시작 `git status --short`: 기존 미추적9항목 보존. 루트 실험 Unity 폴더·사용자 active 설정·잠금 PPTX·기존 이력·별도 Unity 프로젝트 등을 이번 커밋에 넣지 않는다.
- 시작 브랜치 `codex/accident-severity-ui`, HEAD `6656e96`. 최신 사고 소스/배포 기록 기준.
- LATEST·scope·체크리스트·content-design·CURRENT-STATE·원본 `.mmst`와 승객 반응·사고/운송·예측·시설·수요·캠페인·LAN 코드를 대조했다. 전체 소스 결함 검사는 아니다.
- 원본 PowerShell JSON 순회:340노드, 단계50%178/25%0/0%53. 중복/설명이 있어 구현률이 아니며 오래된 사고 미구현·상황 반응 표기를 확인했다.
- MindMap AI `list_projects(query="택시")`:0건. 저장소 원본 기준이며 연결 서비스 편집 없음.
- `pwsh -File tools/check_workspace.ps1 -Strict`:exit0/input issues0. 정식 지도 정상, 선택 재수입 JSON2개 없음. 필수 입력/ZIP/manifest 변경 없음.
- `pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly`:exit0. 사용자 active 포함141파일/115,991,366바이트·출하115,966,642바이트.
- 초기 `python`은 WindowsApps 별칭 실행 불가로 실패해 PowerShell로 대체했다. rg 경로 wildcard 실패는 디렉터리+glob으로 대체했다. 기능 검사 실패가 아니다.
- Nacon/공식 Steam·ETS2 공식 소개/SCS Convoy FAQ를 열어 확인했다. 가격/리뷰·차량 수 등은 비교에 사용하지 않았다.
- 문서 링크·diff/기존 변경 보존은 아래 최종 검증에 기록한다.
- 점검 도중 다른 작업이 콘텐츠 관리창 소스/테스트·공유 체크리스트/LATEST를 스테이징했다. 해당 변경을 보존하며 이번 점검 범위와 분리한다. 새 선택지 구조 조사와 실제 `PassengerRequest`도 확인해 요청 종류별 공유 점수/선택지 독립 효과 미구현을 보고서에 추가했다.

## 미검증과 다음 작업

- 새 Unity 빌드·게임 기능 검사·위치 제어·자동 물리 주행·수동20분·전체3일·다른PC/실물패드 검사는 하지 않았다.
- 기존 유턴 observer/도착 확정 수하물 견인 fixture 실패는 이력을 인용했고 재현/원인 확정은 하지 않았다.
- 먼저 실패 원인/문서 정합성, 이어 사람 싱글·다른PC PVP 수용. 단골/예약·지역 콘텐츠·장기 운영은 후속 후보이며 실제 차내 장치 보류 유지.

## 실행과 재현

기본 `builds/EungyeTaxi/EungyeTaxi.exe`, 은계 싱글3일/A2 PVP·LAN v6, `PrototypeBuild.BuildHybrid` 유지. 두 PC는 같은 최신 실행본을 사용한다. 저장소 루트에서 위 Strict/VerifyOnly 명령으로 확인한다. 기존 실패 도구/명령은 [사고 등급 이력](2026-11-06-accident-severity-ui.md)을 따른다.

완료 후 `git log -1 --oneline -- docs/work-history/2026-11-06-project-wide-review.md`로 실제 이력 커밋을 확인한다. 자신의 커밋 해시는 미리 적지 않는다.

## 최종 문서 검증

- 보고서/이력의 로컬 링크23개 모두 존재, 검사 exit0. `git diff --check` exit0.
- 동시 작업이 `2c13dfb`로 커밋된 뒤 공유 문서의 남은 diff가 이번 점검 추가 부분만임을 확인했다. 동시 관리창 코드·테스트·문서 커밋은 보존한다.
- 이번 선택 커밋 대상은 위4개 문서뿐이다. 시작 시 미추적9항목은 그대로 남기고 원격 push는 하지 않는다.
