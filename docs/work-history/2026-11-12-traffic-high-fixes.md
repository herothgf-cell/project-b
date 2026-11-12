# 2026-11-12 신호·법규·교통 점검 '높음' 항목 수정

## 요구사항

사용자: [점검 기록](2026-11-12-traffic-audit-room-items.md)의 높음 항목을 수정하고 main에 push한다.

## 실제 구현

| 점검 항목 | 수정 | 파일 |
|---|---|---|
| 신호 H1: 황색 매너 면제를 방향과 상관없이 적용 | 직진·좌회전(유턴 포함)·우회전 각자의 신호가 황색일 때만 면제한다. 우회전은 전용 신호가 있으면 그 신호를, 없으면 직진 신호를 본다. 보호 좌회전 황색에 직진·무정지 우회전을 하면 이제 감점된다. 저장된 진행 상태(구 스냅샷)는 기존 단일 플래그로 세 방향을 채워 그대로 복원한다 | `DrivingManners.cs` |
| 법규 H2: 진입선 침범 후 유턴 허용을 다시 계산하지 않음 | 진입선을 넘은 뒤 교차로에 들어가기 전까지 신호를 다시 읽을 때 `uturnAllowed`도 다시 계산한다(진입선 통과 때 차로 ID를 보관). 좌회전·보행 녹색에 하는 정상 유턴이 L01로 남지 않는다. 진입선 L09(-1)는 그대로 남는다 | `DrivingLaw.cs` |
| 법규 H1: 보행자 보호 판정 범위 | 지나는 횡단보도만 본다. 가까운 횡단보도는 항상, 직진이면 먼 쪽, 우회전이면 우측 출구 횡단보도를 본다. 우회전 의도는 **우측 깜빡이 또는 차체가 15° 넘게 우측으로 돈 상태**로 판단하므로 깜빡이 없이 우회전해도 검사한다. 좌회전은 먼 쪽 횡단보도를 보지 않는다. 선분 표본 간격을 1.5m 이하로 줄여 놓침을 없앴다 | `AmbientPedestrianSystem.cs` |
| 교통 H2: NPC 진입 판단이 거리 보행자를 보지 않음 | 회전 NPC는 `CanEnter`에서 가까운 횡단보도와 출구 횡단보도(좌·우)에 건너는 보행자가 있으면 진입하지 않는다. 편집기 신호 교차로에도 적용된다. 교차로 안에서 예약을 쥔 채 멈추는 일이 줄어든다 | `TrafficSystem.cs`, `AmbientPedestrianSystem.cs` |
| 교통 H3: 은계 NPC가 차로 변경 시 옆·뒤를 확인하지 않음 | PvP(작성 합류 구역 사용)를 뺀 지도에서, 차로 변경 간선 시작 12m 이내(또는 속도×3초)에 목표 차로 합류점 뒤쪽 `max(8, v·3+8)`m부터 앞 3m까지를 검사한다. 차가 나란히 있거나(뒤 6m 이내) 뒤에서 1m/s 넘게 다가오면(플레이어 포함) 간선 시작점에서 기다린다. 서로 차로를 바꾸려는 두 NPC는 인스턴스 ID가 작은 쪽이 먼저 간다. 대기는 간선마다 6초까지만 하고, 그 뒤에는 기존 전방 감지가 접촉을 막는다 | `TrafficCar.cs`, `TrafficSystem.cs` |
| 교통 H1(추정): 비신호 교차로 요청 순서 교착 | 먼저 요청한 차라도, 이 차의 차체가 이미 그 차의 회전 통로를 점유하고 있으면 우선권을 넘긴다. 서로 기다리는 교착을 끊는다 | `TrafficSystem.cs` (`BodyBlocks`) |

**수정하지 않은 높음 항목:** 법규 H3('좌회전·보행 신호시' 유턴과 보행자)는 점검에서 불확실로 분류했다. 유턴 곡선이 실제로 횡단보도를 지나는지 지도 기하로 확인해야 해서 이번에는 보류했다. 위 H1 수정으로 유턴 의도(좌측)일 때는 먼 쪽 횡단보도를 보지 않게 되어 범위는 일부 좁아졌다.

## 검증

| 검사 | 종류 | 결과 |
|---|---|---|
| Editor 회귀 30개 묶음(유턴·신호·보행 신호·예약·통로·우회전 대기·교통 흐름·PvP 합류/교차로 등, 임시 러너로 각각 호출) | Editor 정적·위치 제어 | 수정 후 18 PASS / 12 FAIL. **수정 전 코드(HEAD 5개 파일로 잠시 되돌림)도 같은 12개가 같은 메시지로 실패**(로그 diff 동일). 새 실패 없음 |
| 위 12개 기존 실패 | — | AdditionalUturnObserver J7254-SW, UturnTests/UturnIntegration 보행 단계 유턴, UturnMapObserver T1-SE, ReviewedJunctionObserver J5057-N 좌회전, District/JunctionFacility NullReference, HybridNetwork(구 지도 JSON 없음), PvpJunctionFlow·PvpNpcMerge DriverIntent·PvpFlowUnified 1·PvpJunctionFollowup 3(인계된 기존 실패) |
| 검증용 빌드 `artifacts/traffic-verify/EungyeTaxi.exe` | 빌드 | PROTOTYPE_BUILD_OK, exit 0 (출하 폴더 아님) |
| `-oneDayTrafficTest` 연속 물리 하루(실제 NPC 교통) | 자동 물리 주행 | 출하 실행본(수정 전) 3회·검증용 빌드(수정 후) 3회 모두 exit 1. 아래 '하루 주행 비교' |

로그: `artifacts/traffic-audit-fix/editor-before.log`, `editor-after.log`, `build.log`(Git 제외).

### 하루 주행 비교

호출이 무작위라 경로가 달라진다. 같은 경로끼리 짝지어 비교했다.

| 짝 | 수정 전 | 수정 후 |
|---|---|---|
| 경로 A (J866 유턴→…→J6858 좌회전) | 승차 301s(최대 정지 21s) · 하차 357s(70s) · J6858 좌회전 -20 | 승차 326s(35s) · 하차 418s(96s) · **같은 법규 기록, 같은 -20** |
| 경로 B (식사·주유까지 진행) | 하차 141s · 주유 접근 228s(57s) · 주유 접근 안전 검사 FAIL | 하차 117s · 주유 접근 239s(78s) · **같은 FAIL** |
| 경로 C | 하차 245s(76s), J6858 좌회전 -20 | 하차 경로가 달라짐(J1927 좌회전 -20, J5057 유턴 -20), 395s(94s) |

- 6회 모두 예외 0건, 택시 충돌 0건이다. 실패 항목은 수정 전과 같다: 자동 운전기가 직진 녹색에 좌회전해 -20을 받고(기존), 구간 안전 검사가 실패한다.
- 경로 C의 J5057 유턴 -20은 수정 전 실행에서 같은 지점을 지나지 않아 비교하지 못했다. 보행 신호 중 보행자가 있으면 유턴을 불허하는 기존 규칙(법규 H3, 보류)과 관련된 것으로 보인다.
- **수정 후 같은 경로의 최대 정지와 구간 시간이 늘었다**(경로 A 하차 +61초). NPC가 회전 전 보행자, 차로 변경 전 옆·뒤 차량을 기다리게 된 영향으로 보인다. 원인은 따로 측정하지 않았다. 사람 플레이에서 정체가 체감되는지 확인이 필요하다.

## 미검증·남은 항목

- 하루 주행에서 NPC 대기 증가로 구간 시간이 늘었다(위 비교). 체감 정체가 크면 차로 변경 대기 상한(6초)이나 회전 전 보행자 대기 범위를 줄인다.
- 수정한 동작을 직접 겨누는 새 자동 검사는 만들지 않았다. 기존 회귀 묶음과 하루 주행으로 부작용만 확인했다. 사람 플레이 확인이 필요하다: 보호 좌회전 황색 직진 감점, 진입선 침범 후 유턴, 깜빡이 없는 우회전 보행자 보호, NPC 차로 변경 양보.
- 출하 실행본 `builds/EungyeTaxi`와 manifest는 다시 빌드하지 않았다. 다른 PC에 반영하려면 BuildHybrid 출하가 필요하다.
- 중간·낮음 항목(황색 3초, 편집기 신호 우회전 전용 판정 등)은 미적용이다.

## 재현

검증용 빌드는 임시 Editor 스크립트에서 `PrototypeBuild.BuildAt("../../artifacts/traffic-verify/EungyeTaxi.exe","eungye")`를 reflection으로 호출했다(스크립트는 삭제). 하루 주행:
```
EungyeTaxi.exe -batchmode -nographics -oneDayMode -hybridV2 -oneDayTrafficTest   # 결과: <작업 폴더>/artifacts/one-day-traffic-drive.txt
```

## 후속: BuildHybrid 출하 (사용자 요청 "출하 빌드")

- 첫 출하 빌드에서 배포 크기가 127.5MB → 194.6MB로 늘었다. 원인은 방 조명/러그 그림 8장이다. 배경과 같은 1672×941 전체 크기이고 높이 941이 4의 배수가 아니라서 Unity가 DXT 압축을 하지 못하고 비압축 RGBA(밉맵 포함)로 저장했다(`resources.assets.resS` 74.8MB).
- 수정(소스 ac04a11): 그림마다 물건이 있는 영역만 잘라 저장한다. 크기는 4의 배수다(러그 676×176, 조명 132×136, 빛 568×388). `PersonalRoomHUD`가 배경 좌표의 같은 영역에 그린다. 생성 도구는 자른 영역 밖에 그린 픽셀이 있으면 실패한다. 다시 빌드한 결과 `resources.assets.resS`는 11.7MB(이전 출하 7.7MB)다.

| 검사 | 결과 |
|---|---|
| `pwsh -File tools/check_workspace.ps1 -Strict` | Input issues 0 |
| `PrototypeBuild.BuildHybrid` (`artifacts/ship-2026-11-12-build.log`) | exit 0, PROTOTYPE_BUILD_OK |
| 출하 실행본 `tools/test_personal_room_player.ps1 -Visible` | PERSONAL_ROOM_PLAYER_OK 36, RELOAD_OK 7. 캡처 02·03을 열어 자르기 전과 위치가 같은 것을 확인 |
| `update_distribution_manifest.ps1 -SourceCommit ac04a11… -ValidatedOn 2026-11-12` → `-VerifyOnly`, `update_pvp_distribution_manifest.ps1 -VerifyOnly` | DISTRIBUTION_OK 143파일 131,475,770바이트 |

- 출하 실행본의 교통 코드는 위 검증용 빌드(eb45976)와 같다. 출하 실행본으로 하루 주행을 다시 돌리지는 않았다.
- 지도와 LAN 프로토콜은 바꾸지 않았다. 다만 NPC 동작이 바뀌었으므로 모든 PC를 이 실행본으로 맞추는 것을 권장한다.
