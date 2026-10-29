# 2026-10-29 PVP 실제 입력 운송·공유 NPC·법규

## 요구·보존·실행 범위

위치 제어 없이 실제 주행으로 운송을 검증하고 공유 NPC·법규를 구현, 다른PC/사람 검증 분리, 다른 채팅 보존·이력·커밋·푸시. 기존 feature/pvp-prototype worktree에서 진행했다. [계획18](../content-design/pvp/18-driving-npc-rules-plan.md)과 [수용/미완료19](../content-design/pvp/19-driving-npc-rules-acceptance.md)를 따른다.

처음 최신 main37b6adb의 완료 내구도/수리·배포를9489e6e로 병합했다. 작업 중 다른 채팅이 b8ad18a의 PVP 전체 실행 폴더 배포와16aa387의 main HUD/싱글 배포 병합을 이 worktree에 추가했다. 이 커밋과 미커밋 PVP 소스를 모두 보존했다. 마지막 main 게시 문서 c62a959도 cc1d18b로 병합했다. 현재 싱글은130파일112,194,710바이트이며 본 작업에서 다시 빌드하거나 편집하지 않았다. 다른 checkout의 사용자/진행 중 변경도 복구·스테이징·종료하지 않았다.

이번 자동 실주행은 현재 PVP 평면 모터의 가속·조향·제동 입력 시뮬레이션이다. 경기 중 위치 fixture 호출은 예외 처리하고 accepted 이동거리/경계 차단/입력 프레임을 서버에서 기록한다. 정상 경기 시작 배치와 client 권한 보정은 기존 경로를 유지한다. 사람 운전·WheelCollider 접지 검증으로 확대하지 않는다. PC 화면/키보드/마우스 조작은 하지 않았다.

## 실제 구현

- Driving/PvpDrivingPilot: 합법 경로 추종·코너 감속·지정 방향 정차·신호/우회전 정차·NPC/보행 양보 입력만 생성. 활성 차량 상태를 쓰지 않는다. 제품 기본 자동 운전은 추가하지 않았다.
- Traffic/PvpNpcAuthority: 서버 차량2대·보행1명. 신호/앞차/선수·점유 횡단에 대기하고 동일 ID/상태를 전송한다. circuit01 고정 기하를 검증하며 client는 AI를 생성하지 않고 표시/제한된 외삽만 한다.
- Rules/PvpRuleAuthority: 실제 이동의 swept 정지선/진입, 우회전 정차/전용 신호, 보행 보호, 지속 과속/역주행, 관측된 NPC 후방 접촉. 같은 사건 상향 차액/재전송/격리와 정상 진입 후 이탈을 구분한다. 책임·관측이 부족한 접촉/차단 이동은 보류한다.
- Motor/Session/Bridge/Probe: 위치 fixture 금지·거리/입력 audit, 실제 입력3프로필·역할/출발점 교환·진행 로그, 서버 사건 원장/표시. 신호·정지선·제한속도·우회전/보행 우선 안내. wire pvp-drive-4/규칙v4.
- tools/test_pvp_driving.ps1: 기본/지연 역할 교환/위반 실제 두 Player 검사. 동일 NPC/보행 sample·결과·fixture0·도로 경계0·최소 이동거리/입력을 요구한다.
- PVP Git 배포가 추가된 규칙에 맞춰 manifest 도구를 검증 보고서/실제 싱글 manifest 기반으로 갱신했다. 기존 배포라고 하드코딩된 완료 표기를 새 빌드에 재사용하지 않는다.

## 실패 재현·수정과 리뷰

| 관측 | 수정·증거 |
|---|---|
| input-only pilot 부재 | driving-red.log Editor1 후 구현 |
| 교통 없는 검사에서 기본 장애물로 무한 대기 | 미수신 교통을 장애물로 가정하지 않음. continuous1 로그 |
| 다른 시작 지점과 NPC 초기 겹침 | 모든 스폰에서 분리된150/300m 초기 배치. continuous4 실패→5 통과 |
| 정차 완료가 재출발 때 초기화 | rule-model-red.log1. 완료 정차 이력 유지 |
| 저속 우회전이 녹색 중 정지선을 넘고 적색 진입 | 검사 운전기의 진입 소요시간 여유3초. integrated1 실패→2 통과 |
| 끊긴 부분 정차/보행/NPC 관측 합산, 고정 지도 가정 | 독립 리뷰, rule-review-red.log1. 부분 증거 무효화·완료 사실/사건ID 보존·기하 검증 |
| 보행자가 점유 차량을 통과 | pedestrian-safety-red.log1. 시작/다음 단계 점유 대기 |
| 보행 중 접근 차량의 contact 띠 갇힘 | 독립 리뷰P1, pedestrian-escape-red.log1.3.3m 여유와 기존 겹침의 분리 이동 허용, 실제 motor 탈출 회귀 |
| 첫 Player 운송은 성공했지만 러너 경로 판정 실패 | driving-first-player.log: Select-String 위치 인자를 LiteralPath/Pattern으로 수정 |

초기 회전 API 인자/지역 변수 중복 컴파일 오류도 수정했고 이후 통합 컴파일/모델은 통과했다. 독립 최종 읽기 전용 확인에서 추가 중요 결함 없음. 실패 로그도 artifacts/pvp에 남겼다.

## 실제 검사 결과

모든 로그는 artifacts/pvp/ 아래이며 Git 제외다.

| 검사 | 실제 결과 | 근거 |
|---|---|---|
| PvpDrivingTrafficTests.RunAll | Editor0 | driving-release-model.log: 기존 isolation/Task2/network/reconnect/competition33 + 주행/법규 |
| 입력 경로 | 스폰 접근16개·연속 운송12개 도착 | 각 경계 차단0, 연속 최대78.98초; 실제 조향/가감속 |
| NPC/법규 | 25,628단언0 | 주로5,000tick NPC 연속 관측 및600tick 점유 안전, 유형별 경계·분리 탈출 |
| clean Windows build | 종료0 | driving-release-build.log |
| 실제 기본 운송 | host/client0,199.541/319.436m | fixture/경계0, 각1회 완료·위반0·33/44점 |
| RTT150ms·jitter±15ms·손실3%·출발점 교환 | host/client0,319.414/199.335m | fixture/경계0, 각1회·위반0·44/33점, 공통 NPC/보행 exact144sample |
| 지연 proxy 계측 | received26560/drop840/forward25720 | driving-proxy.log |
| 실제 위반 입력 | host/client0,199.609/319.414m | host18점/client44점, 공통 exact145sample |
| 실제 위반 내역 | 과속3건×3 + 같은 접근 정지선2→우회전정차3(추가1)=12 | driving-violation-host.log, 원장9·안전 보너스0. 적색진입7/보행9/접촉5 양성은 모델 검사 |
| 주행 전체 러너 | 종료0 | driving-release-players.log: PVP_INPUT_DRIVING_PLAYERS_OK |
| 기존 competition3 | 종료0 | driving-competition-regression.log; 이 검사는 기존 위치 fixture로 분리 |
| 기존 reconnect6 | 종료0 | driving-reconnect-regression.log; blackout·위조·만료·퇴장·호스트 종료 포함 |
| 기존 network/core | 각각0 | driving-network-regression.log,driving-core.log |
| 내구도/승객/캠페인 모델 | 각각0 | driving-durability-model.log191, passenger430, campaign suite |
| 지도 Strict | 종료0/Input issues0 | driving-strict.log |
| 싱글 manifest | 종료0 | 최신 main 병합 후130파일112,194,710바이트 동일 |

운송 검사의 accepted 거리/fixture0은 서버 audit다. client 보정/replay 거리나 단순 위치 차이를 실제 주행거리로 사용하지 않는다. maxCorrection에는 시작 배치/보정이 포함되므로 사람 조작감/주행 중 보정 품질 합격으로 해석하지 않는다.

## 실행·재현

로컬 Unity2022.3.62f2에서 Pvp.Tests.EditMode.PvpDrivingTrafficTests.RunAll → Pvp.Editor.PvpBuild.BuildClient 순서로 실행한다. 이 PC Editor는 C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe. -batchmode -nographics -quit -projectPath <저장소>/unity/EungyeTaxi -executeMethod <위 메서드> -logFile <절대 로그 경로>를 사용하고 Start-Process -WindowStyle Hidden -PassThru/WaitForExit로 종료 코드를 확인했다.

```powershell
pwsh -File tools/test_pvp_driving.ps1
pwsh -File tools/test_pvp_driving.ps1 -Case lag-swap -Port 18311
pwsh -File tools/test_pvp_competition.ps1
pwsh -File tools/test_pvp_reconnect.ps1 -Port 18401
pwsh -File tools/test_pvp_network.ps1 -Rounds 1
pwsh -File tools/test_pvp.ps1
pwsh -File tools/test_vehicle_damage.ps1
pwsh -File tools/test_passenger_behavior.ps1
pwsh -File tools/test_prototype.ps1
pwsh -File tools/check_workspace.ps1 -Strict
pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly
pwsh -File tools/update_pvp_distribution_manifest.ps1 -VerifyOnly
```

driving 러너는 -Case baseline/lag-swap/violation, -Executable <복사본 exe>를 지원하고 자신이 만든 프로세스만 정리한다. 제품 실행은 builds/PvpPrototype/PvpPrototype.exe→직접 IP Host/Client→게스트 준비→호스트 시간 입력. 수동 입력 W/S/Space/A/D/Q/E/M은 기존 안내를 따른다.

## 잔여 수용·배포

다른 물리PC·LAN/인터넷/방화벽, 사람 키보드/패드·화면 시야/고지·장시간 조작감, WheelCollider/다른 지도/복수 신호군·금지차로/합류·복잡한 측면 책임·간접 봉쇄·전체 교통/공급/수익 밸런스는 [19](../content-design/pvp/19-driving-npc-rules-acceptance.md)에 분리했다. P2 전체나 공개 랭킹 운영 완료로 표시하지 않는다. 유형별 수면/준비/응답/오염과 PVP 내구도/수리는 후속이다.

동시 작업의 PVP Git 배포 규칙을 보존한다. 소스 커밋을 먼저 확정한 뒤 최신 main HUD를 포함해 재빌드·복사본 운송/격리·전체 해시 검증 후 실행 폴더/manifest를 별도 커밋한다. 실제 소스 해시·복사본 결과·게시 결과는 확인 뒤 아래에 추가한다. 자기 자신을 포함할 커밋 해시는 추측하지 않는다.

## 최종 소스·실행 폴더 검증

- 실제 소스 커밋: `32b23317c8781a99fb4f72a1a125b9eb75bd2c9e`. 최신 main HUD 병합 뒤 이 커밋의 Unity 소스에 미커밋 변경이 없는 상태에서 다시 검사·빌드했다.
- `driving-packaged-model.log`: Editor 종료0, 16개 접근/12개 연속 운송 경로와 법규/NPC25,628개 관측 검사 통과. `driving-packaged-build.log`: BuildClient 종료0/`PVP_BUILD_OK`.
- 프로젝트·Library 밖 `artifacts/pvp/runtime-copy-<고유값>/`에 전체 실행 폴더를 복사하고139개 파일의 크기·SHA256 동일성을 확인했다. 복사본 자체를 작업 디렉터리로 실행했다.
- `driving-copied-isolation.log`: 복사본 종료0/`PVP_RUNTIME_ISOLATION_OK`.
- `driving-packaged-baseline.log`: 복사본 실제 두 Player 종료0. host199.541m/client319.404m, 각1회 운송·33/44점, fixture0/도로 경계 차단0, 동일 최종 원장/결과, 공유 NPC/보행 상태145개 표본 일치. 앞서 수행한3프로필 전체 결과와 별도로 기록한다.
- `driving-package-validation.json`을 근거로 `update_pvp_distribution_manifest.ps1 -SourceCommit <위 소스40자리> -ValidatedOn 2026-10-29 -Rebuilt -ValidationReport artifacts/pvp/driving-package-validation.json` 실행0. manifest 검증0: **139파일93,199,109바이트**. 로그/복사본은 Git 제외 로컬 증거이고 전체 원본 실행 폴더·manifest는 Git 배포한다.
- 싱글 manifest 재검증0: **130파일112,194,710바이트**. 싱글 실행 폴더/지도 입력/Unity 소스 추가 변경 없음. 화면·사람 입력·다른 물리PC 검증은 수행하지 않았다.

배포 커밋·원격 반영은 실제 결과를 확인한 뒤 아래 게시 확인에 기록한다.
