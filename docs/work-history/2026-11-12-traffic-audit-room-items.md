# 2026-11-12 신호·법규·교통 체계 점검(조사만) · 방 꾸미기 조명/러그 그림 교체

## 요구사항

1. 신호등 체계 점검 → 이상한 부분과 수정할 부분 보고
2. 교통 법규 체계 점검 → 이상한 부분과 수정할 부분 보고
3. 전반 교통 체계 점검(싱글 모드는 신호를 오래 기다리지 않게 설정한 것이 의도임)
4. 방 꾸미기의 임시 전등·카펫(러그)을 현재 방 디자인에 어울리게 설치 가능하도록 수정

## 1–3. 점검 결과 (코드 읽기 전용, 수정 안 함)

에이전트 3개가 코드를 병렬로 읽었고, 주요 항목은 직접 다시 확인했다(✔ 표시). 실행으로 재현한 항목은 없다. 아래는 사용자 확인 전 **수정 후보**다.

### 신호

| 우선 | 항목 | 위치 |
|---|---|---|
| 높음 ✔ | 황색 '불확실' 판정을 직진 황색 또는 좌회전 황색 하나로 모든 방향에 적용한다. 보호 좌회전이 황색이면 직진은 적색인데, 이때 직진하거나 일시정지 없이 우회전해도 매너 감점이 면제된다. 반대 경우도 같다 | `DrivingManners.cs` 73·94행(`uncertain`) |
| 중간 ✔ | 수요 감응 신호(`DriveFlowPolicy.Signals`)의 황색은 2초이고, 다른 신호(`KoreanSignalRules`·`RoadMapSignalProgram.Import`)는 3초다. 50–60km/h에서는 약 3초 이상이 필요하다. 대기 시간은 녹색 길이와 수요 감응으로 이미 짧으므로 **황색만 3초로 늘려도 싱글 대기 의도는 유지된다** | `DriveFlowPolicy.cs` 20–21행 |
| 중간 | 우회전 양보(`YieldClear`)가 상대 차량을 직진 신호로만 걸러서, 맞은편 보호 좌회전 차량은 교차로에 들어오기 전까지 보지 않는다 | `TrafficSystem.cs` 약 162–163행 |
| 중간 | 실제 신호기는 좌회전 황색 동안 화살표를 끄고 적색만 보이는데, HUD와 판정은 황색으로 처리한다 | `TrafficSystem.cs` 약 553–558행 |
| 중간(불확실) | NPC 유턴(Turn=-2)이 신호 수요 그룹과 맞지 않아 수요가 0이 된다. 다른 좌회전 수요가 없으면 무한 대기할 수 있다 | `TrafficSignalDemand.cs` 약 85행 |
| 낮음 | 고정 주기 보행 녹색 10초는 20m 횡단에 짧다(수요 감응 교차로는 전적색을 유지하므로 안전). 보행자가 붙잡히면 전적색이 계속되고, 3600초를 넘으면 Restore 예외가 난다. `KoreanSignalRules.MayEnter`는 쓰이지 않는 코드다 | `TrafficSystem.cs:94`, `DemandSignalClock.cs:24` |

**정상으로 확인한 것:** 상충 녹색이 없고, 모든 단계에 황색과 전적색이 있다. NPC·플레이어 판정·신호기·HUD가 같은 시계를 쓴다. 대기 방향이 굶지 않는다(녹색 연장 상한은 green+4). PvP recall은 직진·보행 단계만 넣는다. 비보호 좌회전 표지도 함께 생성된다.

### 법규

| 우선 | 항목 | 위치 |
|---|---|---|
| 높음 ✔ | 적색에서 진입선을 0.25m 넘겨 정지하면, 그 뒤 신호를 다시 읽을 때 `uturnAllowed`를 갱신하지 않는다. 그래서 좌회전 녹색에 정상 유턴해도 L01(-20)이 된다 | `DrivingLaw.cs` 약 72·93행 |
| 높음 | 보행자 보호(L04) 판정이 교차로 중심 기준 ±28m 선과 횡단보도 선분이 가까운지만 본다. 우회전 차량이 지나지 않는 먼 쪽 횡단보도의 보행자에도 감점된다(오탐). 나가는 쪽 횡단보도는 우측 깜빡이를 켰을 때만 검사하므로, 깜빡이 없이 우회전하면 L04를 피한다(누락) | `AmbientPedestrianSystem.cs` 41–43행 |
| 높음(불확실) | '좌회전·보행 신호시' 유턴인데, 보행 녹색에 사람이 건너고 있으면 유턴이 위반이 된다. 유턴 곡선이 실제로 지나는 횡단보도만 봐야 한다 | `DrivingLaw.cs:93`, `UpstreamUturnObserver.cs` |
| 중간 ✔ | 편집기 신호 프로그램이 있으면 우회전을 항상 '전용 신호'(`dedicated=true`)로 본다. 우회전 그룹이 없는 교차로(예: J6480_2573)에서는 녹색에 우회전해도 L03이 된다 | `TrafficSystem.cs` 103행 |
| 중간 | 적색 우회전을 위해 일시정지한 뒤 조금 나가 횡단보도 앞에 다시 서면 L09(-1)가 붙고 L02 준수 회복(+3)이 사라진다 | `DrivingLaw.cs` 약 67행 |
| 중간 | 과속: 은계와 A2 모두 제한속도 표지가 없어 판정이 0건이다. 표지를 추가할 경우 `CheckEditorSpeed`에 히스테리시스가 없어 2초마다 -5가 반복된다. 꼬리물기는 연습 구역에서만 판정한다 | `DrivingLaw.cs:25` |
| 낮음 | 유턴 위반이 L01 '신호' 계열로 표기된다. L08 교차 차량 양보와 비보호 좌회전은 항상 보류다. 3m 후진하면 진입선 사건이 사라진다. 적색 우회전 무정지 감점 비율이 현실(신호위반과 동급)과 다르다 | — |

**정상으로 확인한 것:** 신호를 정지선 통과 순간 기준으로 판정한다. 황색 진입은 보류로 처리한다. 교차로 안에서 적색으로 바뀌어도 위반이 아니다. 감점은 사건당 최대 1개이고, 역주행은 래치로 1회만 기록한다. NPC가 앞을 막아서 감점되는 경로는 없다.

### 전반 교통

| 우선 | 항목 | 위치 |
|---|---|---|
| 높음(추정) | 은계 비신호 교차로에서 먼저 요청한 차 A가, 정지선에서 A의 회전 통로를 막고 있는 B 때문에 진입하지 못한다. B는 A의 요청이 우선이라 거부된다. 그 결과 영구 교착이 될 수 있다 | `TrafficSystem.cs` 약 292·305행 |
| 높음 ✔ | NPC 진입 판단(`CanEnter`)이 거리 보행자를 보지 않는다(`pedestrian=None`). 그래서 회전한 뒤 출구 횡단보도에서 급정지하고, 교차로 안에서 예약을 쥔 채 막는다. PvP 봇은 `Observe`로 보행자를 본다 | `TrafficSystem.cs` 111–114행 |
| 높음 | 은계 NPC가 차로를 바꿀 때 목표 차로의 뒤·옆 차량(플레이어 포함)을 확인하지 않는다. 합류 정책은 PvP만 사용한다 | `TrafficCar.cs` 11–20·121–125행, `NativeTrafficMergePolicy.cs:51` |
| 중간 | 황색에 NPC가 감속 제한 없이 정지선에 순간 정지한다(뒤차 추돌 위험). PvP 봇은 NPC의 예약·출구 확인 체계 밖에 있고 정지 기준도 다르다. 붙어 있는 교차로에서 회전 간선이 엉뚱한 교차로에 배정된다. 우회전 판정 핫패스가 O(n²×베지어) | `TrafficCar.cs:111-139`, `PvpSimulationBot.cs:331-336`, `DistrictTraffic.cs:24`, `TrafficSystem.cs:137-186` |
| 낮음 | `ClearPlayerOverlap` 재배치가 화면 안에서 차를 갑자기 나타나게 한다. `ExitClear`가 Default 트리거까지 감지한다. `CanEnter` 경로의 람다가 GC 할당을 만든다. 플레이어 뒤 NPC의 추종 간격이 좁다 | `DistrictTraffic.cs:111` 등 |

**정상으로 확인한 것:** NPC끼리 교차로를 막는 것은 방지된다(ExitClear·CancelApproach). NPC가 신호를 위반해 주행하는 경로는 없다. 재배치는 화면 밖·원거리에서만 한다. 싱글의 짧은 신호 대기는 의도된 설정이라 문제로 보지 않았다.

**알려진 기존 실패(Editor DriverIntent, turn sensing, 우회전 행렬 3건, exe test_pvp_simulation):** 정적 분석으로는 원인을 단정하지 못했다. 관련 후보는 위의 'NPC가 보행자를 무시'와 '봇 정지 기준 차이'다.

## 4. 방 꾸미기 조명·러그 (구현)

- 이전 상태: `PersonalRoomHUD`가 배경 위에 단색 사각형으로 조명과 러그를 그렸다.
- 변경: 배경 `personal-room-base.png`(1672×941)와 같은 크기의 투명 PNG 레이어로 바꿨다. 배경과 같은 사각형에 겹쳐 그리므로 좌표 정렬이 필요 없다.
  - 생성 도구: `tools/generate_room_items.py`(절차적 생성, 외부 API 없음, 재실행으로 재현 가능)
  - 러그: 방의 소실점(840,420)에 맞춘 원근 호모그래피로 바닥에 놓았다. 직조 질감과 접지 그림자를 넣었다. 침대와 옷장을 피해 책상 앞 바닥에 둔다.
    - 스타일 0: 회청색 테두리(커튼·침구 색)
    - 스타일 1: 테라코타 줄무늬에 양끝 술 장식
  - 조명: 책상 상판 왼쪽(의자 옆)에 놓았다. 켜짐 상태는 벽 후광과 책상 위 빛 웅덩이 레이어를 따로 그린다. 꺼짐 상태는 갓 색을 어둡게 한다.
    - 스타일 0: 나무 받침과 리넨 갓
    - 스타일 1: 짙은 철제 스탠드
  - 카드: 보유 중이면 현재 스타일 이름을 보여 준다. 미리보기 문구에 스타일 이름과 '다시 누르면 전환' 안내를 넣었다.
- 저장 구조·가격·구매/보관/스타일/조명 조작 흐름은 그대로다. 그림만 바꿨다.

### 변경 파일

- `unity/EungyeTaxi/Assets/Scripts/PersonalRoomHUD.cs`
- `unity/EungyeTaxi/Assets/Resources/room-rug-{0,1}.png`, `room-lamp-{0,1}-{on,off,glow}.png` (+ meta: NPOT 유지, alphaIsTransparency)
- `tools/generate_room_items.py`
- `docs/ui/2026-11-12-room-items/` (실제 게임 캡처 2장, 합성 미리보기 2장)
- `docs/prototype-development-checklist.md`, `docs/work-history/LATEST.md`

### 검증

| 검증 | 결과 |
|---|---|
| 검증용 빌드(출하 폴더 아님) | `artifacts/room-verify/EungyeTaxi.exe`. `PrototypeBuild.BuildAt`를 임시 Editor 스크립트로 호출했고 PROTOTYPE_BUILD_OK, exit0. 임시 스크립트는 삭제했다 |
| `pwsh -File tools/test_personal_room_player.ps1 -Executable artifacts/room-verify/EungyeTaxi.exe -Visible` | `PERSONAL_ROOM_PLAYER_OK checks=36`, `PERSONAL_ROOM_RELOAD_OK checks=7` PASS. 위치 제어 fixture이며 물리 주행이 아니다 |
| 캡처 확인 | `03-second-day-room.png`를 직접 열어 철제 스탠드(꺼짐)와 회청색 러그가 방에 자연스럽게 놓인 것과 스타일 문구를 확인했다 |

### 미검증·남은 항목

- 출하 실행본 `builds/EungyeTaxi`와 manifest는 **다시 빌드하지 않았다**. 다른 PC에 반영하려면 BuildHybrid 출하가 필요하다.
- 사람의 직접 마우스 조작과 켜짐 상태 실제 게임 캡처(합성 미리보기로만 확인)는 하지 않았다.
- 빌드가 `Assets/Scenes/FirstDrive.unity`를 다시 생성해 8줄이 바뀌었다. 이번 커밋에서는 제외했다(되돌리기 명령은 자동 권한에서 거부되어 작업 폴더에 남아 있다).
- 1–3 점검 항목은 사용자 확인 전이라 수정하지 않았다.

## 재현

```
python tools/generate_room_items.py --preview <출력폴더>
pwsh -File tools/test_personal_room_player.ps1 -Visible   # 기본 exe 대상, 출하 재빌드 후
```
사용자 경로: exe → 싱글 → 영업 마감 → 내 공간 → 조명/러그 미리보기(다시 누르면 스타일 전환) → 구매 → 보관/놓기·스타일 변경·조명 켜기/끄기.
