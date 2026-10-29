# 최신 작업 인계

## PVP 최신: 2026-10-29 직접 IP 주행 시험

[작업·실제 검증 기록](2026-10-29-pvp-direct-ip-network.md). 시험장 실제 표시 화면 확인, Fusion 미준비에 따른 NGO1.12.0 직접 IP 대안 구현. 서버50Hz·클라이언트 입력 예측/보정·입장 호환성·검사 러너 추가. Editor/clean빌드/두Player·입장거절·방장종료/RTT100·150ms+손실1·3% 검사 통과. 정지 후 수렴0,150ms조건 최대보정0.839m. 정확한 최신 실행 결과는 링크 이력의 검증 표를 따른다. 사람 조작감/다른PC/인터넷/재접속/원격차량 보간·P2 전체 수용은 미완료. 다음은 해당 잔여 검증이며 승객·내구도는 상대 완료 커밋 후 연동. 다른 checkout은 보존했다. 기본 은계 exe/BuildHybrid 유지, PVP exe는 로컬 빌드·Git 제외. 필수 지도 Strict0 확인. 아래는 이전 시점 기록이다.


## PVP 후속 요구 갱신: 2026-10-29

[승객·내구도 적용 요구](2026-10-29-pvp-followup-requirements.md)를 콘텐츠 검토표에 기록. 다음은 Task2 잔여 수용→Task3 네트워크→Task4/5 운송·정산이며 승객/내구도 확장은 별도 후보 검토. 완료 후 Unity 화면 확인 허용으로 기존 UI 전면 중단 지시 갱신; 사람 조작감 미검증 상태 유지. 이번 변경은 문서만이며 게임 검사·화면 조작 없음.

## 통합 최신: 2026-10-29 PVP·캠페인 기획 병합
## 최신 구현·배포: 2026-10-29 승객11유형·34변주·준비/청결 사건

[작업·검증 기록](2026-10-29-passenger-types-implementation.md). 기본 배포5ed29ce의 main 푸시·원격 동일 해시를 확인했다. 일반 승객과11유형,34변주·요청 응답 기억·적재/하역·수면·음식/짐 오염·취객 빈도 보완을 구현했다. 모델430/기존care13,301·전체prototype·Editor1,526+care14/live22/P11·BuildHybrid·Player174/새 프로세스8 종료0. 위치 제어 검증이며 실제 물리 주행·수동 전체 캠페인과 구분한다. 숨긴 창 캡처는 폐기하고 사용자 승인 후 실제 창1280×720의 적재·음식 선택·수면·정산4화면을 확인했다. 기존 care Player201검사도 종료0.

현재 기본 실행 builds/EungyeTaxi/EungyeTaxi.exe / 은계 / BuildHybrid. 지도입력/ZIP은 기존 해시 유지, Strict0. 최신 main73e5962의 별도 PVP 작업을 보존해 병합했으며 통합 release Editor(PVP 분리 포함)·BuildHybrid 종료0. 통합 소스7dec39f 기준 실행폴더/manifest130파일112,155,810바이트 크기·SHA256 검증 종료0. 통합 Player174/재개8/기존care201과 실제4화면 재확인 완료. 다음은 사람20분/3일 체감과 내구도·수리 구현이다. 내구도·수리는1차 확인 완료·미구현 상태다. 사용자 .mmst/기존 untracked는 보존했다.

## 이전 통합: 2026-10-29 PVP·캠페인 기획 병합

[통합 검증 이력](2026-10-29-pvp-arena-integration.md): PVP 구현66c5368 + main3293e3b를2507300에서 통합. 두 공용 문서 충돌은 양쪽 내용 보존으로 해결. 통합 Editor·BuildClient·headless Player3개·캠페인/PVP 모델·Strict·manifest130파일 검사 모두 종료0. 캠페인 소스/배포 변경 없음. 다른 채팅의 passenger-type-variants와 미커밋 파일은 그대로이며 후속 통합 때 최신 main을 반영해야 한다. PVP 화면/사람 주행 검증은 미완료, PC UI 자동 조작은 사용자 요청으로 중단.

## PVP 최신: 2026-10-29 자체 시험장·차량 후보

[구현·검증·통합 이력](2026-10-29-pvp-arena-integration.md). 자체 맵·방향 경로·복구·차량A/B·오프라인 실행 연결 구현, Unity 자동 검사 통과. Task2 화면/실물 입력/사람 체감은 미검증이며 사용자 요청으로 PC 조작 중단. 다음은 해당 수용 검증이며 P2는 미착수. PVP 로컬 builds/PvpPrototype/PvpPrototype.exe는 Git 제외. 기본 은계 실행/BuildHybrid/배포/manifest 유지, 필수 입력 Strict0. 아래 항목은 이전 시점 기록.


## PVP 후속: 2026-10-29 기획 요소 검토·Task 1 실제 Unity 검증

[검토·검증 이력](2026-10-29-pvp-review-validation.md) · [콘텐츠 요소 표](../content-design/pvp/11-content-elements-review.md). feature/pvp-prototype 별도 worktree. Unity 2022.3.62f2 Editor·Windows 빌드·두 Player 진단·초기 UI 입력/종료 통과. 캠페인 배포·저장·설정298파일 변경0, Strict0. 기본 은계 exe/BuildHybrid 유지, PVP는 로컬 builds/PvpPrototype/PvpPrototype.exe(Git 제외). 다음은 승인된 Task 2 자체 시험장·차량. 다른 PC 배포·온라인·사람 주행 체감 미검증. main과 LATEST.md 병합 충돌은 양쪽 이력 보존 필요. 아래 기존 캠페인·PVP 인계는 과거 시점 기록.


## PVP 작업 브랜치: 2026-10-29 시작·저장·빌드 기반

`feature/pvp-prototype`에서 로드맵 Task 1의 전용 시작 씬·한글 설정 확인 화면·불변 설정·전용 저장·프로세스별 진단·Windows 빌드 소스를 추가했다. [요구/변경/검증 기록](2026-10-29-pvp-startup-isolation.md) · [현재 로드맵과 실행 명령](../content-design/pvp/08-development-roadmap.md).

순수 PVP 검사 8개 그룹·실제 두 .NET 프로세스 진단 분리·러너 실패 기록 회귀 1개, 기존 캠페인 모델 28개 클래스, 실제 Unity 참조 DLL을 이용한 PVP 런타임 컴파일을 통과했다. 소스 검토의 세 보완점도 수정·재검토했다. **Unity Editor/Player 실행과 EXE 제작은 미수행이며 Task 1/P1 엔진 게이트는 미완료**다. 다음은 이미 추가된 기반 코드를 재작성하는 것이 아니라 Unity import·Editor 검사·독립 빌드·두 Player·화면 확인이다. 이후 자체 경기장·주행 Task 2로 진행한다.

기존 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`, 기존 빌드는 `PrototypeBuild.BuildHybrid`다. 기존 캠페인 소스·씬·지도 입력·배포 manifest·마인드맵은 변경하지 않았고, 이 환경에서 지도 ZIP/Strict 점검을 수행한 것으로 표시하지 않는다. 기존 캠페인 개발은 AGENTS.md의 입력 준비 절차를 따른다. 아래 기록은 이전 기획과 분기 기준의 캠페인 이력으로 보존한다.

## PVP 작업 브랜치: 2026-10-27 기획·개발 계획

브랜치 feature/pvp-prototype은 main 60b15edd6094d9a6383de4d0c75c4a40d49eda40에서 분기했다. [PVP 기획 입구](../content-design/pvp/README.md) · [개발 로드맵](../content-design/pvp/08-development-roadmap.md) · [이번 작업 이력](2026-10-27-pvp-planning.md).

이번 변경은 PVP 시스템/콘텐츠별 기획과 개발 계획 문서다. PVP 코드·전용 맵·별도 실행파일·네트워크는 미구현이며, 경기 시간·인원·배점·충돌·SDK는 [결정 상태](../content-design/pvp/09-decisions-and-balance.md)를 따른다. 다음은 독립 실행 구조·자체 제작 시험장·민첩한 조작 후보 검증이다.

현재 실행 가능한 게임은 기존 builds/EungyeTaxi/EungyeTaxi.exe이고 빌드는 PrototypeBuild.BuildHybrid다. 기존 배포·지도 입력·manifest·사용자 마인드맵은 변경하지 않았다. 아래 캠페인 인계는 분기 기준의 기존 작업 기록으로 보존한다. PVP 문서 검증과 미수행 제품 검증은 위 작업 이력에 구분한다.


## 최신 기획: 2026-10-29 승객 유형 통합·아이데이션

[작업 기록](2026-10-29-passenger-types-ideation.md). 취객을 공통 승객 유형으로 정리하고 운행/상태 영향형 후보11종(취객 포함)을 비교했다. 첫 확장 묶음·후속 후보는 제안이며 채택 대기. 내구도·수리 기획은 사용자 1차 확인 완료·미구현으로 상태 갱신했다. 다음은 승객 유형 채택/상세 검토와 구현 계획이다.

기본 실행 builds/EungyeTaxi/EungyeTaxi.exe / 은계 / BuildHybrid 유지. 코드·빌드·지도·manifest·.mmst 변경 없음. 필수 로컬 입력은 기존 인계 유지, Strict와 게임 검사 재실행 없음.

## 최신 기획: 2026-10-29 취객·지속 응대·충돌 내구도

[작업 기록](2026-10-29-passenger-interaction-durability-plan.md). 취객 미등장 보완·지속 응대3유형과 충돌 강도별 내구도/수리·견인안을 기존 담당 문서에 통합했다. 사용자 확정 방향은 Proto 수리비·시간 중심, 내구도0 운행 불가. 세부 수치는 검토안이며 코드/빌드에는 미반영이다. 다음은 세부안 검토·구현 계획 확정이다.

현재 실행은 기존 builds/EungyeTaxi/EungyeTaxi.exe / 은계 / BuildHybrid. 지도 입력·실행 파일·manifest 유지, Strict/게임 검증 재실행 없음. .mmst와 기존 untracked는 변경하지 않았다. 현재 실행·배포 근거는 아래 기존 인계를 따른다.

## 최신 문서 작업: 2026-10-27 제안서 2차·마인드맵 제품 비전

[32장 제안서](../proposals/project-b-game-proposal-v2.pptx) · [양식/마인드맵 검토표](../proposals/proposal-v2-review.md) · [작업 기록](2026-10-27-game-proposal-v2.md). 필수19항목 중18개 내용 포함, 실제 제안 참여자 정보는 기존 사용자 지시대로 제출 전 확인 필요. 현재 구현과 차량 성장·단골/예약·환경·경제의 목표/후속 구상을 분리하고 시안2장을 생성·내장했다.

PPT32장 패키지/레이아웃/글꼴/재가져오기 통과·전 슬라이드 렌더 확인. 게임 코드·빌드·지도·manifest·마인드맵 원본은 변경 없음. 기본 실행은 은계 `builds/EungyeTaxi/EungyeTaxi.exe`, 빌드는 BuildHybrid. 필수 로컬 입력은 직전 Strict 통과 인계 유지, 이번 문서 작업 재검사 없음. 다음은 회사 참여자·채택 기능·인력/예산/일정 확정, 게임은 기존 수동20분/3일 체감 검증. 기존 사용자 변경 보존.

## 최신 저장소 정리: 2026-10-27

[브랜치 삭제·보존 근거](2026-10-27-branch-cleanup.md). main 포함 확인 후 로컬8개·원격5개 삭제. 원격은 main만 남았고, 로컬은 main과 codex/satisfaction-fare-recovery만 남았다. 후자는 고유3커밋의 초기 회복/운임 구현·옛 빌드/이력이라 사용자 내용 검토 후 삭제 여부 결정 대기다. StableDriveRecovery.cs가 main에 없지만 회복 기능은 StableDrivingProgress라는 별도 구현으로 존재한다. 별도 worktree는 같은817c5dc detached로 보존했다.

기존 미푸시 제안서 커밋·사용자 .mmst 변경·untracked를 보존했으며 main 푸시는 하지 않았다. 빌드/지도/필수 입력/manifest와 게임 검증 상태는 아래 기존 배포 인계 그대로다.

## 최신 문서 작업: 2026-10-27 회사 개발 착수 제안서 1차

[제작·검증 기록](2026-10-27-game-proposal.md) · [제안 자료](../proposals/README.md) · [24장 PPTX](../proposals/project-b-game-proposal-v1.pptx) · [별도 조작 매뉴얼](../proposals/project-b-prototype-manual-v1.md).

회사 내부 개발 착수 승인용 1차본. 참여자 명단은 사용자 지시에 따라 회사 제출 전 확인 필요. 핵심6명·8주 검증·12개월 로드맵과 KPI는 제안 가정이며 승인된 개발 범위가 아니다. PPT 패키지/레이아웃/글꼴/24장 재가져오기 검사 통과 및 전 슬라이드 렌더 확인. PowerPoint 네이티브 재생과 게임 재검사는 수행하지 않았다.

게임 현재 빌드/지역은 아래 기존 배포 인계 그대로 `builds/EungyeTaxi/EungyeTaxi.exe` / 은계, BuildHybrid. 필수 지도 입력·배포 파일·manifest 변경 없음, 직전 Strict 통과 상태를 유지하며 이번 문서 작업에서는 재실행하지 않음. 다음 문서 작업은 참여자·회사 단가·책임자/일정 확정이며, 게임 후속은 수동20분/3일 체감 검증. 기존 사용자 `.mmst`와 untracked 보존.

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

