# 2026-11-06 감점 음수 입력과 변화율 설명

## 요구사항·구현
법규 위반 및 다른 패널티를 `-2`처럼 음수로 입력하고 기존의 실제 감점을 보존했다. 법규10개·운전 M8개(구 Comfort 참조 포함)·음식/오염 짐 청결3개·기존 Quiet 미응답 및71선택지의 급가감속 추가 효과에 같은 표시 변환을 사용한다. 단순 감점은 -100~0, 선택지 M 추가 변화는 -100~100이다. 추가 변화 -2는 기본 감점에2점 추가 감점, +2는2점 완화이며 최종 감점은0이상이다. 이미 부호 있는 S·피로 증감, 회복량·확률·시간·상한은 바꾸지 않는다.

관리창 표시/입력 경계만 변환해 JSON의 기존 양수 감점량·실제 게임 효과·프로필 schema/hash·이관 의미를 유지한다. 기본값·저장값 복원·연결 요약·읽기 전용 구 설정·일반 상세와 계산 미리보기도 부호를 맞췄다. 사용자 프로필은 저장하거나 적용하지 않는다.

변화율 입력 아래에 현재값 기준 1회 적용과 활성화 고정 기준값의 분당 적용 예시를 추가했다. 선택지가 있는 화면의 `변화율 계산법과 기본 자연 피로`에서 계산 예시와 포만감별 싱글/PVP 기준 표를 펼친다. 싱글4/6/8점/분, PVP5/7.5/10점/분, 포만감5점/분 감소·경계 통과·정차/수면·합산 제한·0의 의미를 설명한다. 현재 게임 기본 규칙이며 실시간 기사 상태 조회가 아님을 표시한다. 자연 피로 추가율 +50%는 기본4→6점/분, -50%는2, -100%는0, 0은기본4 유지다.

## 변경 파일
- `tools/content-editor/public/app.js`: 감점 표시·입력·범위·복원·요약·미리보기 변환 및 변화율 설명 표.
- `tools/content-editor/public/content-model.js`: 적용 시점·영향 대상·음수 예시·단계별 % 계산 안내.
- `tools/content-editor/public/style.css`: 설명 표 레이아웃.
- `tests/content-editor/context-ui.test.mjs`: 실제 카탈로그 감점 종류별 입력·원복·기존 부호 유지 및 설명/표 검사.
- `tests/ContentTuningTests.cs`: 실제 DriverCondition/PVP 기본 증가율6건·추가율4예시·경계/수면·실제 법규/M2점 감점 보존.
- 사용 안내·체크리스트·LATEST·이번 기록.

## 검증·결과
- 새 화면 회귀2개가 기존 양수 표시/안내 부재로 실패하는 것을 확인한 뒤 구현했다.
- Node 관리창/프로필/서버/배포 전체60개 통과·실패0·exit0.
- 실제 C# 콘텐츠/요청 모델 및 자연 피로/감점 검사663개 통과·exit0. Unicode와 이관 C#·Node hash 일치.
- 실제 브라우저 법규 -20 기본 표시·-100~0 범위·-2 입력/API 검증 통과, 시험 입력은 저장값 복원했다. 새 탭을 사용하여 기존 탭의 편집 내용을 보존했다. 설명을 펼쳐 계산 예시·자연 피로 표를 확인했다. 화면 `artifacts/signed-law-penalty.png`, `artifacts/fatigue-rate-guidance.png`.
- 최종 배포 manifest VerifyOnly와 사용자 active/sample·실행 Assembly/manifest 보존 hash 검사는 아래 최종 영수증에 기록한다.

## 실행·남은 항목
이 PC의 실제 실행 루트는 `C:/git/project-b`이며 루트 `설정 관리.cmd`를 실행한다. 법규 → 원하는 상황에서 음수 감점을 입력하고, 승객 요청/대화 선택지/효과에서 변화율 설명을 확인한다. 저장·게임 적용 후 다음 새 세션에 사용하는 기존 절차를 유지한다.

이번은 관리창 표시·설명 변경이며 Unity 재빌드·새 Player/물리 주행·수동 전체 캠페인/다른PC LAN 검사는 하지 않았다. 기존 은계 싱글/A2 PVP·LANv9·BuildHybrid `builds/EungyeTaxi/EungyeTaxi.exe`와 지도 입력/ZIP을 유지한다. 외부 관리 worktree 및 동시 기획 작업을 보존한다.

최종 영수증: Node60/실패0·C#663/exit0·Unicode/이관 hash 일치, 통합 manifest VerifyOnly exit0(사용자 active 포함141파일/출하140파일116,826,789바이트). 시작/종료 사용자 active/sample·Assembly/manifest4파일 SHA256 동일. 로그는 `artifacts/signed-penalties-node.log`, `artifacts/signed-penalties-runtime.log`, 보존 목록은 `artifacts/signed-penalties-preserve.json`이다. 지도 입력/ZIP은 미변경·이번 Strict 재실행 없음.
