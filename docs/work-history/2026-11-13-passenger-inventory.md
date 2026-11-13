# 승객 다양화 기획 준비: 현재 유형·선택·효과

- 요청: `docs/content-design/`를 기준으로 현재 등장할 수 있는 승객 유형, 요청, 대화 선택과 효과를 표로 정리.
- 실제 작업: [현재 대조표](../content-design/2026-11-13-passenger-current-inventory.md) 작성. 기본11종+개별18종, 행동별 선택·시간·오염, 요청14종의 즉시/종료 효과, 비활성·텍스트 전용 항목, 기본 요청 선택지34개의 원문을 정리했다.
- 확인 기준: `config/content/default-profile.json` 및 내장 `passenger.presets`, 기획02/07/15~17/19/20·밸런스, 요청/행동 소스. 과거 문서의 요청 빈도와 Quiet 실패 규칙은 현재값으로 구분했다.
- 검증: PowerShell JSON 파싱 성공; 총30/활성29 프리셋, 활성 요청 원문 선택34행 확인. 요청0/1/2=5/40/55, 이동 간격12초·진행15~65%를 기본값에서 확인. `git diff --check` 종료0(타 작업 C# 파일 CRLF 안내만).
- 변경 파일: 위 기획 대조표, 이 작업 이력, `docs/work-history/LATEST.md`.
- 미검증: 실행본 실행, 위치 제어, 자동 물리 주행, 수동 플레이, 전체 캠페인, 빌드/지도/manifest/Strict 재검사 없음. 기존 캠페인·로컬 active-profile 수치와의 일치 보장 없음.
- 기존 변경 보존: 시작 시 FirstDrive 씬·미추적 제안서/설정/이력이 있었으며 작업 중 다른 C#·테스트 수정도 관찰됨. 이 작업은 게임 코드·사용자 설정·지도·빌드를 수정하지 않았다.
- 재현: 기본 프로필 JSON의 `values`를 id로 조회하고 `passenger.presets` 문자열을 다시 JSON 파싱한다. schema2 효과는 구 `request.*.success`가 아니라 `choice.*`와 승객별 overrides를 사용한다.
- 다음: 사용자가 현재 표를 검토한 후 다양화 목표와 우선 요청을 논의한다. 기존 실제 두 PC 수용 및 지도/배포 후속은 유지한다.
- 현재 빌드/지역·필수 로컬 입력 상태: 직전 LATEST의 은계r18/A2·BuildHybrid·LANv19와 Strict 문제0 기록을 인용하며 이번 재검증은 하지 않았다.
