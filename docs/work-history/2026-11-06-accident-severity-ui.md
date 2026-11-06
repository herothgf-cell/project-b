# 2026-11-06 지도·신호·사고 등급과 결과 UI

## 요구사항과 승인

사용자 스크린샷의 시설 이름 잘림/아이콘 겹침, 내비 내부의 작은 후속 화살표·거리, 실제 차량 신호4구 일치, 기존 허용 U턴6곳 표시 보강, 콘텐츠 적용 상태 배너 제거를 반영한다. 사고/고장 안내는 아이콘·짧은 제목·비용/시간/결과·행동 카드로 정리한다. 새 U턴 구역은 추가하지 않는다는 사용자 답변을 받았다.

사용자 승인 초기값: 보행자 경미/중간/중대 3,000/8,000/15,000원·45/120/240초·운전 점수−10/−25/−40. 차량·시설은1,000/3,000/6,000원·15/45/90초·−3/−8/−15. 중대는 현장 운행 재개 없이 공업사로 이동한다. 초기 무작위 가중치는 사람60/30/10%, 차량·시설70/25/5%로 구현했다. 상세 [사고 기획](../content-design/08-accident-and-workshop-proposal.md), [실행 계획](../superpowers/plans/2026-11-06-accident-severity-ui.md).

## 실제 구현

- `FullMapOverlay.cs`, `MapFacilityLabelLayout.cs`: 고정84×22 대신 실제 한글 줄바꿈 크기·여백을 측정한다. 시설 아이콘·호출/승하차 핀·내 위치·조작부와 겹치지 않는 위치를 찾고, 공간이 없으면 이름만 숨기고 아이콘/툴팁은 유지한다.
- `TrafficSystem.cs`, `RoadMapFacilities.cs`: 적색/황색/좌회전/녹색4구. 기존 RGB 배열·신호 권한/주기·보행2구 유지, 좌회전이 없는 접근의 좌회전 렌즈는 소등한다.
- `DrivingReadability.cs`: 작은 후속 화살표/거리를 파란 카드 안 우측에 배치, 차로 한글 설명·후속 행동·회전 후 유지 문장 제거. 실제 경로 시간/거리 선택은 유지한다.
- `UturnRoadMarkings.cs`, `LaneDirectionPaint.cs`: 활성 compiled 유턴 지원, 실제 허용6곳 흰 점선 .28m/유턴 화살표 .22m 보강. 지도·허용 권한·연결·U턴 구역 범위는 변경하지 않았다.
- `AccidentSeverityPolicy.cs`, `AccidentState.cs`, `CampaignAccount.cs`, `EconomyLedger.cs`: 선택 결과와 처리 플래그 저장/복제, 선택 비용·시간1회 적용, 기존 진행45초 사건은 재추첨/소급 감점 없음.
- `AccidentController.cs`, `AccidentScore.cs`, `PassengerJobsAccident.cs`: 종류별 운송 취소/확정 운임 보호·전역 운전 점수 사건1회, 벽과 사람의 동시 접촉을 비용 확정 전에 재검사. 승객 M 점수와 전역 운전 점수는 별도다.
- `VehicleDamageController.cs`, `PvpSimulationBotServices.cs`: 실제 손상 유효 충돌에서 차량/시설 사고 접수. 중대는 빈 공업사 견인칸으로 이동해야 복귀, 사고비 포함으로 별도 일반 견인비/시간 없음. 내구도/유료 수리는 유지, 건강한 봇도 실제 출차 경로로 복귀한다.
- `AccidentPresentation.cs`, `WorkshopPresentation.cs`: 사고·고장/정비 결과 카드. `ContentTuningProbe.cs`는 배너/설명 OnGUI만 제거하고 설정 소비·snapshot·별도 관리창 유지.
- `PvpNativeLan.cs`: 선택된 사고 상태를 포함한 LAN v6, 양쪽 PC 실행본 갱신 필요. `PvpNativeLanProbe.cs`는 실제90초 중대 시설 사고/견인/감점 복제를 검사한다.

## 검증 진행과 결과

기준 소스90fadf7, 작업 브랜치 `codex/accident-severity-ui`. 동시 관리창 작업 de32883과 기존 미추적 자료·사용자 프로필을 보존한다. 사용자 종료 확인 후 저장4개/active 프로필1개, 총5파일의 바이트/SHA256을 `artifacts/accident-severity-user-files-closed.json`에 기록했다. 시작 전 값과 사용자 종료 후 값은 별도 기준이다.

- `pwsh -File tools/check_workspace.ps1 -Strict`: exit0, 필수 입력 issues0. 지도 JSON/ZIP 변경 없음.
- 최초 RED: 지도252검사 중54실패(필요55px/실제22px), 기본/편집4구·내부 다음경로·compiled 유턴·정책·팝업 7그룹 요구 실패. 별도 실제 유턴 .16m 획 RED exit1.
- `AccidentSeverityUiBuild.Run`: exit0. 지도252·실제 신호71접근/좌회전없음12·편집4구/후속경로/compiled 유턴·실제6구역/20획, 사고 정책3,158·팝업/감점/재생/구 저장, 기존 사고7/공업사51·HUD9/150/86·시작/체크포인트500회/Live22 통과.
- `tools/test_accidents.ps1`: 77검사 PASS exit0. `tools/test_vehicle_damage.ps1`: 191검사 PASS exit0. 기본45초 overload의 불필요한 모델 간 의존을 제거한 후 재확인했다.
- 최초 Unity sandbox 실행은 라이선스 IPC 실패 exit199, 정상 권한 재실행으로 진행했다. 최초 팝업 `.meta` GUID 길이 오류를 수정해 컴파일 통과했다. 추가 기존 신호 회귀는 과거17초 주기 가정으로 실패했고, 실제 프로그램의 녹/황/적 단계와 재질·좌회전 소등을 함께 검증하도록 수정했다.

## 최종 실행 검증

- 최종 전체 관련 Editor 검사와 `AccidentSeverityUiBuild.Build` exit0(`artifacts/accident-severity-final-build.log`). 지도288·신호/노면/차로49방향×조합·300차로/73교차로, 정책3,158·HUD/저장·콘텐츠26/사본3 통과. 이후 UI 표시 순서와 캡처 fixture만 수정했고 `PrototypeBuild.BuildHybrid` 최종 exit0(`artifacts/accident-severity-presentation-build.log`). 게임 회사/제품·저장 경로·지도/ZIP·신호 주기/권한은 유지했다.
- 최종 실제 창 싱글171검사 exit0(`artifacts/accident-severity-ui-final-render/result.json`):9종 선택 결과/전역 감점/비용·시간1회, 실제 checkpoint JSON/상태 복원, 중대 공업사 배치, 정지 직후 마지막 스윕의 보행자 우선 분류, 지도3해상도/내비/사고3등급/고장·수리 실제 렌더, 실제 유료 견인·수리 버튼 확인. 지도/내비1280/1600/1920·사고/정비1280 이미지를 직접 확인했다. 초기 캡처는 검사 도구가 accident 컴포넌트를 꺼 팝업이 없었고, 다음 캡처는 HUD/점수 알림이 카드 앞에 나와 수정했다. 기능 PASS와 시각 PASS를 구분했다.
- PVP155검사 exit0(`artifacts/accident-severity-ui-pvp/result.json`):9종 명시적 호스트 tick/복제 상태 JSON/비용·시간·점수 멱등, 건강한 중대 봇이 처리 후 실제 바퀴·조향·페달로 공업사 출구를 나가는 별도 물리 주행 확인. 위치를 옮겨 출차 완료로 판정하지 않았다.
- 실제 Rigidbody/PhysX 충돌31검사 exit0(`artifacts/accident-physical-collision-player/result.json`): 지면 제외, 정상 sampler의 중대 시설/경미 차량 사고, 자동 VDC 접수/제동/장부/운전 점수/공업사/확인 시간·지속 접촉 재청구 없음. 벽까지11.50m/차량까지10.01m 실제 이동, 접촉 접근 속도6.54/6.60m/s, 각각 내구도85. 초기 지원면 fixture의 사고 미발생 exit1은 `artifacts/accident-physical-collision-red`에 보존하고, 지원면 마찰0으로 충돌까지의 이동을 보장한 뒤 재검사했다. 사람 운전/WheelCollider 주행으로 확대하지 않는다.
- 기존 사고 싱글51·새 프로세스 재개8·PVP55 exit0(`artifacts/accident-revision-{single,reload,pvp}`): 운송 중 취소·도착 확정 운임 보호·실제45초 처리·수리25초/4대견인30초·경기 종료 직전 정산 멱등 통과.
- 두 실제 프로세스 LAN/재접속 호스트48·참가자63 exit0(`artifacts/pvp/native-lan-20261106-114735-d046c9e1`): 실제90초 중대 시설 사고, 선택 비용6,000원/점수−15/공업사 이동 완료 상태 복제, 제어 잠금·중복 청구/감점 없음. 이후 생산 변경은 지도 핀/카메라 전환/팝업 표시 순서뿐이며 동일 회계·전송 검사를 중복 실행하지 않았다. 다른PC LAN 검사는 아니다.
- 사용자 종료 후 대상 은계 저장4+active 프로필1 기준과, 회사 폴더 전체 JSON/백업14+active1 총15파일 기준을 따로 만들었다. 실행 검증 후15파일 모두 바이트/SHA256 변경0. 단독 mutable active 프로필은 배포 소스/기본값으로 커밋하지 않는다. 생성 Burst 디버그 폴더는 `artifacts/accident-severity-burst-debug`로 보관하고 실행 폴더에서 제외했다.
- 변경 문서5개 로컬 링크86개/`git diff --check` 통과. 독립 리뷰에서 발견한 건강한 봇 출차 컴포넌트 위치 및 동시 보행자 스윕 판정 순서를 수정했고, 해당 동작을 실제 출차/제어기 검사로 확인했다.

### 별도 실패·미검증

- 추가 실행한 기존 `UturnTests.Run`의 보행 신호 유턴 관찰 fixture는 사건 목록이 비어 실패했다. 최초 실패 시 UturnTests/DrivingLaw/DrivingManners는 HEAD와 동일한 소스였지만 별도 깨끗한 HEAD 실행본 검사는 하지 않았으므로 원인을 단정하지 않는다. 시도한 observer fixture 변경은 원래대로 복원했고 이번 표시 검사에서 분리했다. 현재 허용6곳의 표식/활성 연결 검사 통과를 법규 관찰 전체 통과로 확대하지 않는다.
- `test_vehicle_durability_player.ps1 -Capture -Visible`는 기존 `arrival locked luggage can tow from unsafe crossing`에서 첫 프로세스 exit1(34항목)로 실패했다. 새 사고 채널과 분리하도록 기계 손상 fixture의 Fresh에서 사고 상태/유예를 초기화한 후에도 동일 실패였다. 뒤4개 재시작 검사는 이 runner로 실행되지 않았다. 실제 유효 충돌31·유료 견인/수리 전용 화면/동작3·기존 사고 PVP55 통과와 구분한다. 이 실패의 상세 원인은 남은 조사다.
- 사람 장시간·전체3일·다른PC/실물 패드는 미검증이다. 아래 재현 명령과 로그로 이어간다.

실제 sourceCommit으로 manifest를 생성한 배포 영수증은 아래 후속 기록을 따른다.

## 배포 영수증

- 실제 소스 커밋 `cab0924413f0d532c12e257a0a1777f1565c0ce5`(기획/체크리스트/이력 포함)를 `git log -1 --oneline -- docs/work-history/2026-11-06-accident-severity-ui.md`로 확인했다. 동시 관리창 de32883은 조상으로 보존했다.
- 현재 기본 실행본 `builds/EungyeTaxi/EungyeTaxi.exe`, 은계 싱글/A2 PVP·LAN v6. 최종 빌드는 `PrototypeBuild.BuildHybrid` exit0, 실제 최종 화면/동작171검사도 exit0이다.
- 사용자 active 프로필을 옮기거나 출하하지 않고, 별도 `artifacts/accident-severity-shipped-runtime`에 출하140파일만 복사해 원본/사본 SHA256 전수 일치를 확인한 뒤 공식 manifest 생성 도구를 실행했다. 출하140파일 **115,966,642바이트**, 실제 원래 폴더는 유효한 active 프로필 포함141파일 **115,991,366바이트**다.
- 통합/PVP alias `-VerifyOnly` 각각 exit0, 파일 크기/SHA256 전수 일치. mutable active 프로필은 별도 contentHash 검증을 통과했고 원래 사용자 파일 그대로다.
- 최종 Strict 필수 입력 issues0. 생성 지도 JSON·필수 입력 manifest·OSM 대조 설정·지도 ZIP은 변경하지 않았다. 사용자 저장/백업14+active1 모두 종료 후 기준과 바이트/SHA256 변경0이다.
- 실행 검증용 게임/Unity는 종료했다. 기존 미추적 자료와 관리창은 보존했다. 로컬 소스/배포 커밋이며 원격 push는 하지 않았다. 배포 커밋의 실제 값은 `git log -1 --oneline -- distribution/EungyeTaxi-build-manifest.json`으로 확인한다.

## 실행·재현과 수용 범위

Unity는 로컬 Unity2022.3.62f2 Editor로 `-batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod AccidentSeverityUiBuild.Build -logFile <별도 로그>`를 실행한다. 기본 빌드는 내부 `PrototypeBuild.BuildHybrid`, 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`다.

새 실행 검사: `pwsh -File tools/test_accident_severity_ui_player.ps1`, 같은 명령의 `-Pvp`, 실제 창 렌더는 `-Capture -Visible -View all`. 전용 `-contentTest` 저장으로 사용자 캠페인을 사용하지 않는다. 9가지 종류/등급의 위치 제어·명시적 호스트 tick을 실제 물리 충돌/실시간 타이머로 부르지 않는다. PVP 건강한 중대 봇 출차는 별도로 실제 바퀴/조향/페달 주행을 확인한다. 기존 `test_accident_workshop_player.ps1`의 싱글/재시작/PVP와 `test_pvp_native_lan.ps1 -Reconnect`도 구분한다.

사람의 장시간 플레이·전체3일 캠페인·다른PC LAN/실물 패드 수용은 이번 자동 검사와 별도다. 원격 게시 없이 로컬 커밋/배포 파일을 인계한다.
