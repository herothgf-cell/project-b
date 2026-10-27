# 최신 작업 인계

## PVP 작업 브랜치: 2026-10-27 기획·개발 계획

브랜치 feature/pvp-prototype은 main 60b15edd6094d9a6383de4d0c75c4a40d49eda40에서 분기했다. [PVP 기획 입구](../content-design/pvp/README.md) · [개발 로드맵](../content-design/pvp/08-development-roadmap.md) · [이번 작업 이력](2026-10-27-pvp-planning.md).

이번 변경은 PVP 시스템/콘텐츠별 기획과 개발 계획 문서다. PVP 코드·전용 맵·별도 실행파일·네트워크는 미구현이며, 경기 시간·인원·배점·충돌·SDK는 [결정 상태](../content-design/pvp/09-decisions-and-balance.md)를 따른다. 다음은 독립 실행 구조·자체 제작 시험장·민첩한 조작 후보 검증이다.

현재 실행 가능한 게임은 기존 builds/EungyeTaxi/EungyeTaxi.exe이고 빌드는 PrototypeBuild.BuildHybrid다. 기존 배포·지도 입력·manifest·사용자 마인드맵은 변경하지 않았다. 아래 캠페인 인계는 분기 기준의 기존 작업 기록으로 보존한다. PVP 문서 검증과 미수행 제품 검증은 위 작업 이력에 구분한다.

## 최신 완료: 2026-10-27 내비·TIP·표지·졸음·마인드맵

[최신 요구/변경/검증 기록](2026-10-27-nav-tip-drowsy-mindmap.md). 제보 경로2504→703m, 점유 안내 TIP 슬라이드 통합, 표지 가독성, 짧은 눈감김 입력 불응(제동 가능), 사용자 승인 마인드맵 직접 반영/Detail-Links290개 갱신. 모델28·그래프2549·전체 Editor 종료0·마인드맵/Strict 통과. 게임 종료 후 글꼴 렌더 회귀도 수정하고 기본 빌드·최종 Player9/38검사·실제 화면 확인을 마쳤다. 배포 d462660 main 통합·push0·원격 SHA 일치 확인.

현재 main. 작업 브랜치 codex/nav-tip-drowsy-feedback-20261027은 병합 완료했다. 원본 사용자 .mmst는 별도 기획 이력 보존본에 시작 해시 그대로 보관했다. 아래 과거 섹션은 직전 배포 인계다.

최신 실행은 builds/EungyeTaxi/EungyeTaxi.exe, 소스 b11ac5f1c9b79c8e694367cda72c9b386a35085e, BuildHybrid 종료0. 배포 manifest130파일/112,072,682바이트 전수 SHA256 일치. 정식 지도1888차로/contentHash76ebb91c8e5ae202f81c15a5df399375db2273157406eccc4b07ef258ebfe758와 필수 입력/ZIP 유지. 다음 작업은 사람의 유턴·표지 가독거리·졸음 빈도/조작감·전체20분/3일 체감이며 자동 검사 완료와 구분한다.

## 직전 작업: 2026-10-27 플레이 기획 재통합·마인드맵 검토

통합 문서 f91c0de main push0·원격 SHA 일치. 관련83문서/링크1,430/현재 앵커14/표45 검사0, 기존20본문 보존·원본 .mmst 해시 유지 확인. 문서만 변경했으며 모델/Unity 재검사·재빌드하지 않았다.

직전 기획 정리501e548 이후 사용자 피드백에 따라20개 본문을6개 플레이 기획+가지별 검토표로 재통합했다. 현재 규칙은 각 주제 한 곳에만 둔다. 개발/제작 참고·후속·정리 전 원문은 content-design 밖으로 분리했다. 원본 .mmst는 읽기만 했으며 검토 전 변경 금지다. 이번 작업 결과는 [재통합 이력](2026-10-27-planning-only-mindmap-review.md)에 기록한다.

[기획 입구](../content-design/README.md) · [마인드맵 가지별 추가/수정/삭제 검토](../content-design/06-mindmap-review.md) · [별도 개발 참고](../technical-reference/README.md) · [이전 정리 이력](2026-10-27-design-refresh.md). 게임 코드·지도·실행 파일·배포 manifest·사용자 마인드맵은 유지한다.

## 현재 실행·배포 기준

| 항목 | 현재 상태 |
|---|---|
| 기본 실행/프로젝트 | builds/EungyeTaxi/EungyeTaxi.exe / unity/EungyeTaxi |
| 빌드 | PrototypeBuild.BuildHybrid, 마지막 게임 소스 e9c6fae, 배포57eb4c4 |
| main 게임 통합 | [표지·졸음 후속](2026-10-27-uturn-blink-followup.md), 기록e46ba9a까지 push·원격 SHA 일치 |
| 주요 기능 | P11/P12·P13 대기·시설 출구 내비·저속 조향·졸음 당일 마감·반복 눈감김·신호등 상부 유턴 표지 |
| 최신 자동 검증 | 모델27·전체 Editor·표지/눈감김 실제 렌더24·피로/마감34 종료0 |
| 배포 전수검사 | 130파일 /105,870,100바이트 SHA256·크기·개수 일치 |
| 정식 지도 | eungye·1888차로, contentHash 76ebb91c8e5ae202f81c15a5df399375db2273157406eccc4b07ef258ebfe758 |
| 필수 로컬 입력 | 직전 Strict 검사0, 필수 입력/ZIP 유지. 선택 import2개 부재는 허용 |
| PC 이동 | 실행은 pull 후 기본 exe. 개발은 distribution/maps ZIP을 저장소 루트에 풀고 tools/check_workspace.ps1 -Strict |
| 사용자 변경 | .mmst·개인 메모/기존 untracked 보존, 자동 stage 금지 |

## 다음 작업과 미검증 범위

| 구분 | 남은 것 |
|---|---|
| 사람 플레이 | 실제 저속 조향·시설 진출입·20분/초과 영업/3일 경제·졸음 강도/빈도·요청 답변 여유 체감 |
| 환경/성능 | 다른 물리PC·DPI·실물 패드·장시간 교통/프레임 |
| 제한 구현 | P13 안전 대체 쌍0개 → 대기만 제공. 검증 후보 확보 전 대체 이동 개발/완료 표시 금지 |
| 조건부 지도 | 제보 좌표/방향·관찰 근거 있는 접근만 보완, 전체 현장 규제 인증 아님 |
| 경미한 후속 | 비주유 시설 기능 부제, 이전 피로 취소 영수증 요청 이력 표시 |

## 이전 기록

[직전까지의 인계 모음](2026-10-27-handoff-before-design-refresh.md) · [P11~P13 안정화](2026-10-27-proto-stabilization.md) · [도로/피로](2026-10-27-road-fatigue-integration.md) · [시설·지도·조향](2026-10-27-drowsy-map-handling.md) · [표지·눈감김 배포](2026-10-27-uturn-blink-followup.md).

위 과거 인계의 ‘미구현/병합 없음/같은 날 계속 영업’은 당시 상태다. 최신 기획·main 코드와 실제 날짜별 증거를 우선한다. 이 문서의 커밋은 git log -1 --oneline -- docs/work-history/LATEST.md로 확인한다.

