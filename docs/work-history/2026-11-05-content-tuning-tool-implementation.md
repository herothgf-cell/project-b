# 승객·대화·효과·법규 관리 도구 구현

## 요구·승인·완료 범위

사용자가 2026-11-05 설계안대로 구현하고 직접 실행해 설정을 바꿀 수 있게 해 달라고 승인했다. 별도 로컬 관리창·설정 코어·기존 게임 소비자·저장/LAN·실제 계산 미리보기·테스트 실행을 구현했다. 사용자 별도 사고 채팅의 **두 기능 공동 배포·조율 승인**도 해당 채팅의 실제 사용자 답변을 읽어 확인했다. 설정 단독 후보를 검증하고 소스를 고정한 뒤 사고 작업이 최종 공동 빌드/기본 실행본/manifest를 맡는다. 단독 검증을 공동본 검증으로 표시하지 않는다.

## 실제 구현

- `설정 관리.cmd` → Windows 기본 PowerShell 실행기 → Node 표준 라이브러리의127.0.0.1 서버 → 브라우저 관리창. 이 PC의 기존 Node 사용, 추가 패키지/공개 서비스 없음.
- 요청/대화/효과/법규/공통 영향5분류, 한국어 대사·ID·영향 검색, 구현/모드/사용/변경 필터, 현재/기본/저장값·연관 항목·효과 시점, 프로필 저장/원복/가져오기/내보내기/게임 적용. 브라우저 기본 prompt/confirm 미지원 문제를 실제 IAB에서 재현하고 화면 내 대화상자로 교체했다.
- 카탈로그327항목: **편집289, 고정 참조38**, 별도 법규 자료76개 색인. 실제 도달하지 않는 요청 실패/중립 점수를 편집 가능으로 꾸미지 않는다. 일반/취객의 기존 예약·보장 정책 등 고정 조건도 이유를 표시한다.
- 순수 C# `ContentTuning`/`ContentJson`: 엄격한 전체 프로필·버전·ID·중복/누락·관계식·UTF8 SHA256 검증. 문구4000자·프로필240KiB, ASCII 프로필 ID64자·Int32 revision. 코드 소비자와 효과 설명에 실제 값을 연결하고 선택 대사/실제 답변/저장을 함께 반영한다.
- 기본값은 기존 정책을 보존. 새 캠페인/새 PVP 방 시작 때 고정, checkpoint의 전체 snapshot으로 이어하기. 구형 snapshot 없는 저장은 구형 기본값·운임 정책2/3/4를 유지한다. 검증 실패한 주 저장이 백업 검증의 전역 설정을 오염시키지 않도록 되돌린다.
- 호스트가 설정 snapshot을 방 시작 전에 전달한다. 설정 단독 LAN은 v4, 공동 사고본은 별도v5 통합 필요. 봇/참가자도 호스트 설정 소비. 실제 계산 미리보기는 고정 실행본의 C# 코어를 호출한다.
- `분리 테스트 실행`은 실행별 임시 캠페인/주행 기록/연습 입력·결과/로그를 사용한다. 기본 캠페인·제품/회사명·지도 입력을 바꾸지 않는다.
- 저장은 원자 교체·백업·revision/hash 충돌 검사. 다른 서버 인스턴스도 같은 파일 잠금을 사용한다. 중단된 잠금을 임의 삭제하지 않으며 복구 안내를 표시한다. Origin/token·경로·크기 제한, 입력 대사는 textContent로 표시한다.
- 빌드 전 config/Resources의 카탈로그·기본 프로필이 바이트 단위로 같은지 검사. 배포 검증은 정확히 active-profile만 사용자 변경으로 별도 검증하고 나머지 출하 파일 크기/SHA256을 유지한다.

## 변경 파일 묶음

`config/content/{catalog,default-profile}.json`, `unity/EungyeTaxi/Assets/Resources/Content/` 사본, `ContentJson/ContentTuning/ContentTuningRuntime/ContentTuningProbe/ContentTuningPlayerCheck` 및 Editor 체크·빌드 가드, 기존 Passenger/VehicleCare/Manner/Law/OneDayOffer 소비자와 checkpoint/PVP/연습 연결, Node 관리창·검증기·실행기, C#/Node/Player/LAN 검사와 모델 스크립트 의존성, 사용자 안내/기획/체크리스트/범위/이력.

병렬 파일 범위를 나누고 분리 작업 공간에서 개발했다. 원래 폴더의 HUD3338612/배포e29a0cb를 보존해 소스만 통합했다. BusinessProgress의 겹친 저장 경로는 HUD 테스트 보호와 ContentTest 보호를 모두 유지하도록 병합했다. 미추적 루트 Unity 자료·다른 지역 프로젝트·기존 사용자 문서는 포함하지 않았다.

## 수행 검증과 실제 결과

- `pwsh -File tools/test_content_tuning.ps1` **exit0/636**: 기본 고정seed·수치·스케줄 동등성, 변경 전달, 실제 저장/답변, 범위·엄격JSON·원자 교체·복원·구형 정책·0점 위반 의미. 독립 Node/C# UTF8 hash `c441bcfdb6223365d04e8fa51453096eefa06fac1456df25df8bee6862033d0b` 일치.
- 영향 모델: care13,301/rest40/서행21/안정 회복16/운영 계획49, 법규·요청·연습 feedback 별도 회귀 exit0. 전체 프로토 모델28묶음을 실패 후에도 끝까지 실행한 수집기 **27통과/1기존실패**, 아래 제한을 따른다.
- Node 관리창/manifest 최종 **18테스트 exit0**. 실제 두 서버의 동시 저장200/409와 보존된 revision/hash, 취소/비동기 편집 보존, 대사 검색, 모달Enter/Escape, 토큰 새로고침, 프로필 경계/해시·출하 DLL 변조 거부를 확인했다. 샌드박스 루프백 EACCES는 권한 있는 동일 테스트 재실행으로 통과했다.
- Unity `ContentTuningBuild.Build` **exit0**: runtime26, 빌드 사본3, 기존 HUD/준비/내비·캠페인 회귀·BuildHybrid 성공. 최종 단독 로그 `artifacts/content-tuning/final-build-with-lan-probe.log`. 최초 Unity compile에서 유턴 설정 치환 오타를 발견해 수정했고, 빌드 가드 누락 RED→GREEN도 구분 기록했다.
- 실제 기본 exe `-contentTest -contentProfile ... -contentTuningCheck` **exit0/17**: 법규37/요청11/음식9 적용, 실제 C# 평가, 임시 경로/전체 snapshot 저장, 디스크 수정 중 live 불변, 이어하기는 이전 값, 새 캠페인만 새 값 적용. 위치 제어·물리 주행 검사가 아니다.
- 실제두 Player LAN **host41/client56·재접속 exit0**. 호스트는 별도 수정 프로필, 참가자는 기본 프로필로 시작했고 world-ready 뒤 실제 활성hash가 호스트 것임을 양쪽에서 확인했다. 기존 실제 WheelCollider 약19m/선점·복제·재접속 회귀 포함. 로그 `artifacts/content-tuning/final-lan.log`, 원본 증거 `artifacts/pvp/native-lan-20261105-183905-58eebe49/`.
- Windows 기본 `powershell.exe`로 실제 실행기 **exit0**, 관리창 열림 확인. 실제 IAB에서 프로필 생성→요청8→11 수정→저장→적용→C# 미리보기 requestDelta11 확인. 브라우저 화면과 좁은 창 검증 결과는 아래 후속 영수증에 보강한다.
- Strict **exit0/input issues0**, 프로젝트 지도 해시 유지. 출발 전 사용자 종료 저장4파일은 기존 HUD 기준 SHA256과 일치했다. 이번 종료 시 재대조는 후속 영수증에 기록한다.

## 기존 실패·수정·미검증

`ManualHandlingTests`는 기존 제동 계수0.30을 이전0.18 기준 범위로 검사해 실제8.823529로 실패한다. 수정 전 분리 HEAD에서도 동일했다. `PassengerBehaviorTests`의 accepted 대표 답변 문자열, `PassengerSituationTests`의 깬 승객 문구 기대도 정확한 HEAD 소스만 별도 컴파일해 같은 실패를 재현했다. `OperationPlanningAdapters`의 기존 stub에는 실제 서비스가 참조하는 HasTimeViableOffer가 없어 컴파일 실패한다. 이를 이번 설정 변경의 신규 회귀나 전체 테스트 통과로 보고하지 않는다. 근거는 `artifacts/content-tuning/task1-report.md`, `baseline-models.log`, `final-prototype-suites.log`다.

독립 리뷰에서 UI 목록/범위·수정 보존·새로고침·실제 미리보기 코드와 runtime 소수 진행률 복원/실제 답변/도달 불가 설정/0점 위반 의미를 수정했다. 마지막 전체 리뷰의 다른 서버 동시 저장·대사 검색·미저장 교체도 RED→GREEN으로 반영했다. 사람 연속 운전/전체3일 캠페인/다른PC LAN·패드·현행 법률 감수는 미수행이다. 기본값 동등성과 이 제한을 분리한다.

## 실행·배포 인계

사용 방법: `docs/content-tuning-tool-guide.md`. 루트 `설정 관리.cmd`를 더블클릭한다. 기본 게임은 `builds/EungyeTaxi/EungyeTaxi.exe`, 빌드는 `PrototypeBuild.BuildHybrid`, 은계 싱글/A2 PVP다. 관리창 예시 프로필은 사용자 로컬 영역에 보관하며 정식 active는 기본값으로 원복해 공동 통합에 전달한다.

사고 작업이 이 소스 커밋과 기획/검증 기록을 가져가 LAN v5·사고 상태·Content snapshot·저장·모델 의존성을 병합하고 공동 빌드/최종 manifest·실행 폴더를 검증·갱신한다. 현재 작업의 단독 후보 실행본을 최종 공동 배포로 커밋하지 않는다. 실제 이력 커밋 확인: `git log -1 --oneline -- docs/work-history/2026-11-05-content-tuning-tool-implementation.md`.

## 최종 브라우저·인계 영수증

소스 커밋은 실제 `304faeef4e0c96f976c0f66f9eea060bd85024b1`이며 사고 작업에 전달했고, 해당 작업이 별도 폴더에서 병합·공동 v5 빌드를 담당한다고 회신했다. 이후 게임 C#·실행본·manifest는 이 작업에서 추가로 덮어쓰지 않는다.

최종 Node 관리창/manifest 전체18테스트 **exit0**, 단일 새로고침 뒤 토큰 유지와 화면 내 프로필 생성/저장 확인창, 실제 대사 검색·관련 항목과 입력 확인. 실제 IAB에서 `sample-request-11`을 생성하고 이행8→11 저장/적용, 빌드된 C# 미리보기 **requestDelta11**을 확인했다. 결과는 JSON 내부 필드 대신 한글 만족도·지급·요청 점수·법규 감점으로 표시한다. `preview.jpg`는 실제 출력이다.

실제 브라우저1280×720과800×720에서 렌더·편집 필드 접근을 확인했다. 800창의 documentWidth785≤innerWidth800, 편집값11·저장 버튼 존재 확인. 별도 전체 페이지 캡처는 IAB에서 지원되지 않아 실패했으며 정상 viewport 캡처로 대체했다. 이미지: `docs/ui/2026-11-05-content-tuning/editor-1280.jpg`, `editor-800.jpg`, `preview.jpg`. 마지막에 임시 viewport를 해제하고 사용자 편집 창을 유지했다.

적용 파일을 UI에서 **default / 1d7fc834d91295e14f15ef90a29f1cd4a5d992ad5f8952c2d8d79d354785ac1c**로 복구하고 디스크 기본값과 hash 동일 확인. 예시 프로필은 Git 제외 로컬 영역에 보관했다. 사용자 원래 저장4파일의 최종 SHA256 재대조도 **변경0**. 전체 프로토 모델 수집기는 출력 줄 수 대신 묶음 수로 다시 집계해 **27통과/1기존실패**를 확인했다.

소스/기획 경로는 저장소 상대 경로, 임시 테스트 폴더와 Unity/Node 설치는 로컬 환경 정보다. 최종 공동 실행본·manifest 검증은 사고/공업사 공동 배포 이력에서 확인하며, 위 단독 결과와 사람 수동/전체 캠페인 검증을 혼동하지 않는다.

최종 문서6개·저장소 로컬 링크164개 존재 검사 exit0, `git diff --check` exit0. 미검증인 법률 감수·사람 전체 캠페인 등을 문서 통과로 대체하지 않는다.
