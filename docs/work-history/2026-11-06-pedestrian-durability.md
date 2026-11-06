# 2026-11-06 보행자 사고 내구도 손상

## 요구와 구현

사용자는 보행자 사고에도0~N 차량 손상을 요청했고, 경미0~2/중간0~5/중대0~10점(내구도100 기준)을 승인했다. 차량·구조물의 기존 최대50점 물리 손상보다 작은 상한이다. [설계](../superpowers/specs/2026-11-06-pedestrian-durability-design.md), [실행 계획](../superpowers/plans/2026-11-06-pedestrian-durability.md), [사고 기획](../content-design/08-accident-and-workshop-proposal.md).

- `AccidentSeverityPolicy.cs`: 사고 등급별 상한·균등 정수 sampler. 기존 비용/시간/감점은 유지.
- `AccidentState.cs`, `VehicleCare.cs`: 호스트의 선택/실제 손상/적용 버전·상태, 차량 snapshot의 적용 사건 ID.0점도 적용 사건으로 기록한다. 구 저장은 필드 부재를0버전으로 처리해 소급하지 않는다. 수리 후에도 사건 ID를 남겨 재적용을 막는다.
- `AccidentController.cs`: 신규 사람 사고와 처리비 확정 전 사람 우선 분류에 선택 연결, 안전 정차 정산 때 한 번 적용. 중대D0은 기존 공업사 예약, 나머지D0은 기존 견인·수리.
- `AccidentPresentation.cs`: 신규 사람 사고의 실제 내구도 손상 행, 감소0은 `변동 없음`. 다섯 행 카드 배율/범위를 별도 계산.
- `PvpNativeLan.cs`/검사: v8, 선택한 손상과 실제 내구도·적용 상태 호스트 복제. 기존 독립 물리 손상은 유지.
- 새 순수/Editor 검사, 기존 실제 제어기 probe의 손상 기대값·D0 견인/수리/수리 후 중복 방지, LAN 재접속 후 실제120초 보행자 사고 검사.

## 진행·검증 근거

시작 소스e34f60e, 브랜치 `codex/pedestrian-durability`. 시작 시 추적 파일 변경 없음, 기존 미추적9항목·관리창 후속2c13dfb/전체 조사e34f60e 보존. 사용자 게임/Unity 미실행 확인 후 저장/백업14+active1 총15파일의 크기/SHA256을 `artifacts/pedestrian-damage-user-files-before.json`에 기록했다.

- 순수 RED: `tools/test_pedestrian_damage.ps1`가 누락된 `SamplePedestrianDamage`로 예상 실패 exit1. 컴파일 오류를 RED로 세지 않았다.
- GREEN: 새 순수3073·기존 차량 손상191·기존 사고77 PASS/exit0. 손상0/상한/균등 분포·재선택 금지·차량 snapshot 중복 ID·수리 후 재생·D0 하한·기존 진행 사고 호환을 검사했다.

독립 리뷰에서 기존 차량/시설 사고의 보행자 우선 분류 시 소급 손상 가능성을 발견했다. 새 사건 자격 표식의 RED를 확인하고, marker가 없는 구 사건은 분류가 바뀌어도 면제하도록 수정했다. 재검토에서 추가 중요 결함 없음. 위 순수 검사를 실제 운전/전체 캠페인 검사로 확대하지 않는다.

## 실제 실행 검증

- `PedestrianDamageChecks.Build` exit0(`artifacts/pedestrian-damage-final-build.log`): 새 Editor13(사고/차량 JSON/구 필드 부재·손상 표시/3해상도 카드범위)·기존 등급3158/UI·콘텐츠26/사본3·HUD9/150/86·메뉴/저장/체크포인트500회/Live22 및 BuildHybrid 통과.
- 실제 싱글201검사 exit0(`artifacts/pedestrian-damage-single/result.json`):9종 선택/실제 손상·중복 없음·JSON/상태 복원, 새 물리→사람 분류의 손상과 구 물리→사람 분류의 면제, 경미/중대D0의 실제 유료 견인·수리·수리 후 재생 차단.5장 실제1280 팝업에서 손상 행·D0 행동을 직접 확인했다(중대8점·잔여1점 실제 감소 등).
- PVP182검사 exit0(`artifacts/pedestrian-damage-pvp-final/result.json`): 같은9종·D0 예약/일반 견인/수리·중복 방지 및 건강한 봇의 실제 물리 출차. D0 서비스는 실제 BusinessProgress와 같은 .02초 진행/시계 관찰 순서로 검사했다. 초기 단일30초 점프는 완료 checkpoint에서 미관찰 시간을 추가 잠금으로 간주해 수리를 거절했다(`pedestrian-damage-pvp-diagnostic`: pending0/atBay=true/matchLock30). 게임 코드의 기존 서비스 흐름은 변경하지 않고 검사 진행 순서를 맞춘 뒤 matchLock0/수리 가능을 확인했다.
- 기존 사고 싱글51·새 프로세스 저장 재개8 exit0(`artifacts/pedestrian-damage-legacy-single`, `artifacts/pedestrian-damage-reload`). 처리 중 연료·청결·내구도 유지 기대값을 새 충돌 직후 내구도를 기준으로 수정했다. 손상 시점과 처리 시간 경과를 구분한다.
- 두 실제 프로세스 LAN 재접속 호스트48/참가자63 exit0(`artifacts/pvp/native-lan-20261106-124529-fe39d4b2`): 실제120초 중간 보행자 사고, 손상3점/비용8000원·점수25감점의 호스트 결정/복제, 반복 snapshot 후 내구도/금액/점수 불변. 이는 v7 중간 검사본이며 이후 전송 필드를 바꾸지 않고 계약 번호만v8로 구분했다.
- 별도 선택지 효과 schema2 작업도v7을 사용할 예정이라는 인계를 받아, 독립 중간 실행본이 같은 계약 번호로 오인되지 않도록 이번 배포는v8로 구분했다. **이 실행본에는 선택지별 효과 엔진을 통합하지 않았다.** 후속 통합은 양쪽 기능을 보존하고 새 계약 번호로 갱신해야 한다. 최종v8 검사/manifest 결과는 배포 영수증에서 확인한다.
- 최종v8 Editor13/정책3158·HUD/저장 검사 통과 후, 빌드가 일시적 `resources.assets` 파일 매핑 점유로 exit1했다. 사용자 저장15파일 불변·게임/Unity 프로세스 없음 확인 후 재시도한 `PrototypeBuild.BuildHybrid`는 exit0(`artifacts/pedestrian-damage-v8-final-build.log`). 최종v8 싱글201/실제 팝업 재검사도 exit0(`artifacts/pedestrian-damage-v8-single`).
- 빠른 `MotionOnly/10초` LAN 검사는 v8 접속/월드 ready를 통과했지만 기존 짧은 경기 호출 기대값을1200초 MotionOnly 모드에도 적용하는12번째 fixture에서 실패했다(`artifacts/pvp/native-lan-20261106-130247-8f45b9d0`). 게임 호출 로직/기존 fixture는 변경하지 않고 정식 재접속 모드로 최종v8 검증을 다시 실행했다. 이를 내구도/전송 성공으로 포괄 표시하지 않는다.

## 실행과 남은 수용

### 최종 배포 영수증

- 실제 소스 `9275f2afbf8601bee32c36ff43c33f8918281db3`(코드/기획/체크리스트/이력 포함), `git log -1 --oneline -- docs/work-history/2026-11-06-pedestrian-durability.md`로 확인했다. 로컬 브랜치 `codex/pedestrian-durability`이며 원격 게시하지 않았다.
- 최종v8 정식 재접속 LAN도 호스트48/참가자63 exit0(`artifacts/pvp/native-lan-20261106-130516-610fb496`). 실제120초·손상3점/반복 snapshot 불변을 최종 계약으로 확인했다. 앞의 MotionOnly 실패와 구분한다.
- 현재 기본 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`, 은계 싱글/A2 PVP·LANv8(양쪽 갱신). BuildHybrid 최종 exit0·최종v8 싱글201 및 실제 팝업 PASS, 게임/Unity 검사 프로세스 종료. 별도 선택지 효과 작업과 통합할 때는 이 소스를 보존하고 **v9 이상 새 계약**으로 갱신한다.
- 사용자 active 파일을 이동/포함하지 않고 출하140파일만 별도 `artifacts/pedestrian-damage-shipped-runtime`으로 복사해 원본/사본 SHA256 전수 일치 후 공식 도구로 manifest를 생성했다. 출하140파일 **115,975,346바이트**, active 포함 실제141파일 **116,000,070바이트**. 통합/PVP alias VerifyOnly 각각exit0, 전수 크기/SHA256 일치·optional 사용자 프로필 contentHash 정상.
- 사용자 저장/백업14+active1은 시작 기준15파일 모두 바이트/SHA256 불변. Burst 디버그 출력은 `artifacts/pedestrian-damage-burst-debug`로 보관하고 출하에서 제외했다. 필수 지도 입력 Strict issues0, 지도/ZIP/OSM 설정·사용자 콘텐츠 설정 미변경. 기존 미추적 자료와 동시 관리창 작업은 보존했다.
- 배포 커밋의 실제 해시는 `git log -1 --oneline -- distribution/EungyeTaxi-build-manifest.json`으로 확인한다. 수동 장시간/전체 캠페인/다른PC 수용은 이 영수증의 자동·위치 제어·loopback 검사와 별도다.

기본 `builds/EungyeTaxi/EungyeTaxi.exe`, 은계 싱글/A2 PVP, `PrototypeBuild.BuildHybrid`. 로컬 Unity2022.3.62f2에서 `-executeMethod PedestrianDamageChecks.Build`는 새 Editor/기존 정책/UI/콘텐츠·HUD 검사 후 기본 통합 빌드를 수행한다.

재현: `pwsh -File tools/test_pedestrian_damage.ps1`, `pwsh -File tools/test_accident_severity_ui_player.ps1 -Capture -Visible -View popup -OutputDirectory artifacts/pedestrian-damage-single`, 동일 명령의 `-Pvp`, `tools/test_pvp_native_lan.ps1 -Reconnect`. 실제 제어기 probe는 위치 제어/명시적 tick이며, 건강한 봇 출차만 별도 실제 바퀴/조향·페달 검사다. LAN은 두 loopback 프로세스이고 다른PC는 아니다. 임시 contentTest/기존 명시적 artifact 저장을 사용한다.

이전 작업의 유턴 observer·수하물 도착 후 견인 fixture 실패는 미해결 항목으로 유지한다. 사람 장시간·전체3일·다른PC·실물 패드 수용은 별도다. 이번에는 지도/ZIP·회사/제품명/저장 위치·콘텐츠 설정과 다른 작업 자료를 변경하지 않는다.
