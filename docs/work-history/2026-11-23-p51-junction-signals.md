# 2026-11-23 P51 교차로 실제 위치로 이동, 도로 중간 신호 표시 정리, 차량 신호등 원형 등화 (지도 r51)

## 요구

1. 사용자 화면(은계남로 `reviewed:r47:P51`, 한양수자인·호반써밋 정문): "여기 신호등이랑 차선 표시 위치가 다 이상함 · 로드뷰/항공뷰 참고해서 수정해줘". 범위 질문에서 사용자는 **"교차로 위치까지 옮기기 (추천)"**를 골랐다.
2. 진행 중 추가 요청(`reviewed:P22:56`, x=508.4, z=171.3): "여기도 신호등 위치가 이상함 · 신호등 불이 원이 있고 네모가 있는데 원 형태로 통일해줘".

## 원인

- **P51**: 카카오 항공사진(L0)·로드뷰(2026-06)와 대조했다. 실제 교차로는 r47 배치보다 약 8m 동쪽에 있다. 서쪽 횡단보도는 x≈653, 동쪽은 x≈683, 북쪽 호반 정문 도로는 x≈670, 남쪽 한양 정문 도로는 x≈664다. 차량 신호등이 정지선 옆 차도 위에 서 있었다. 실제로는 건너편 오른쪽 모서리 기둥에 팔을 달아 차로 위로 내민다.
- **P22:56(공통 원인)**: 도로 중간 신호 교차로의 **제어 표식**(`midblockControl`, 편집용 자료)을 실행 화면이 일반 차량 신호처럼 그리고 있었다. 그래서 도로 한가운데에 기둥이 서고, 표식 폭만큼(12.6m) 긴 팔이 보도 밖까지 뻗었다. P51 표식(`reviewed:r47:B:gate`)도 같은 문제였다.
- **등화 모양**: 자동 생성 신호(`TrafficSystem`)는 둥근 등화를, 지도 편집 신호(`RoadMapFacilities`)는 네모 상자 등화를 쓰고 있었다.

## 실제 구현

- **지도 r50 → r51** (`Assets/Editor/P51Relocate.cs`, `P51Relocate.Apply`; r50에서만 동작):
  - 정문 도로: 한양 (664.2,74)→(664.3,63), 호반 (670,101)→(670,112). 동행 진입 차로와 좌회전 대기 차로는 서쪽 횡단보도 앞 x=650.3까지 늘렸다. 회전 연결 5개(직진, 양 정문 진입, 서행 좌·우회전)는 편집기 연결과 같은 3차 곡선으로 다시 그렸고, 노면 장면(`road:polish:`)도 맞췄다.
  - 횡단보도 6개: 서 x=653.2, 동 x=683, 북(호반) 670±4.15, 남(한양) 664.2±4.8. 대각선 2개는 (658.5/681, 78.3↔96.2)다. 보행 대기 공간과 보행 신호도 각 끝으로 옮겼다. 대각선 a의 남서 끝은 한양 우회전 차로를 피하려고 658.5로 옮겼다.
  - 정지선은 각 횡단보도 바로 앞에 둔다. r42의 중복 동행 정지선은 지웠다.
  - 차량 신호 4개: 건너편 오른쪽 모서리 기둥에 높이 5.2m로 단다. 은계남로 방향은 5.6m 팔, 정문 방향은 3.3m 팔이다.
  - 보도: 정문 출입구 개구부를 새 회전 경로에 맞췄다. 자동 생성 보도 조각 중 주행 차로 폭과 겹치는 조각만 끄고, 나머지는 다시 켰다(켬 19, 끔 17).
  - 교차로 표식은 x=668.1로 옮겼다. 확인된 신호 프로그램 근거는 그대로 둔다. 시설 근거에는 카카오 로드뷰 링크(2026-06)를 남긴다. 치수는 게임 추정치다.
  - contentHash `46bf8e43…`, 24,929,686바이트, SHA256 `444e48ca…`. `config/maps/local-input-manifest.json`을 갱신했다.
- **도로 중간 신호 표식 숨김**: 실행 화면에서는 `midblockControl`을 그리지 않는다. 편집기 화면에서는 그대로 보인다(`RoadMapFacilities.Build`). 대상은 P22:56과 P51 2곳이다.
- **둥근 차량 등화**: 지도 편집 차량 신호의 적·황·녹·좌회전 등화를 납작한 구로 바꿨다. 자동 생성 신호와 같은 모양이다. 보행 신호는 실제 서 있는/걷는 사람 그림이 네모 등화라서 네모로 둔다.
- **저장 이관**: `BusinessProgress`에 r50·r51 해시를 추가했다. r51로 넘어오는 저장은 P51의 진입 관측과 횡단 상태를 비운다. 점수는 유지한다. `JunctionRouteFeedbackTests`의 대상 범위를 r51까지 넓혔다.
- **점검 도구**: `MapAuditProbe`에 `-mapAuditDump "x,z,r"`(반경 안 렌더러 계층 목록)과 층 촬영의 운전자 시점 2장을 추가했다.

## 검증

| 검사 | 결과 |
|---|---|
| `P51Relocate.Apply` | `P51_RELOCATED 46bf8e43…`, 지도 검사 통과(보도 침범·도로 중간 신호 근거 포함) |
| `JunctionRouteFeedbackTests.VerifyMigration` | `JUNCTION_MIGRATION_OK target=r51 versions=37 checks=1228` |
| Editor 묶음 | 통과: `ImportedMarkingRendererTests`, `ReviewedMarkSuppressionTests`(90), `ReviewedNativeLanePresentationTests`, `ReviewedLaneMovementTraceTests`(37), `AdditionalReviewedUturnMap.Validate`, `UturnBlinkFeedbackTests`, `RuntimeServiceAnchorRegressionTests` |
| 승객 경로 | `NavigationStopRegressionTests.AuditRoutes`: routes=1676 unreachable=0 failures=0, 독립 최소비용 검산과 일치 |
| 이전부터 실패 | 원래 r50 코드·지도로 되돌려도 같은 메시지로 실패한다: `PedestrianSignalConsistencyTests`("unknown countdown"), `NavigationStopRegressionTests.Run`("S09 … body/walking continuity") |
| 화면(시험 빌드, 위에서 본 층 촬영) | P51: 횡단보도 6개, 정문 도로, 모서리 기둥 신호 4개가 새 위치에 있다. P22:56: 도로 가운데 기둥과 긴 팔이 사라졌다. 물체 목록에서 `control/신호함·지지 가로대·지주`가 없어진 것도 확인했다. [이미지](../ui/2026-11-23-p51-signal-fix/) |

| 출하 빌드 | 소스 커밋 `93ea285`에서 `PrototypeBuild.BuildHybrid`(종료 0)를 실행해 `builds/EungyeTaxi/`에 반영했다. 같은 실행본으로 P51·P22:56을 위·운전자 시점에서 촬영했다. 차량 등화는 원형, 보행 등화는 네모다. |
| 배포 매니페스트 | `update_distribution_manifest.ps1 -SourceCommit` 후 `-VerifyOnly`: `DISTRIBUTION_OK files=145 bytes=150794516` |
| `check_workspace.ps1 -Strict` | Input issues 0 |

## 남은 것

- 직접 운전하며 본 확인은 하지 않았다(회전 연결 체감, 신호 가독성).
- P51 남쪽 개구부는 x 652~669로, 실제 출입구 폭보다 넓다. 회전 곡선이 모서리를 크게 돌기 때문이다. 필요하면 회전 반경을 따로 조정한다.
- P51 북서쪽의 가는 흰 선 하나(z≈99, x 652~665)는 출처를 아직 확인하지 않았다.
- 보행 신호 등화는 네모 그대로다. 원형으로 바꾸길 원하면 같은 함수에서 바꿀 수 있다.

## 재현

```powershell
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod P51Relocate.Apply   # r50 지도에서만 동작
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod JunctionRouteFeedbackTests.VerifyMigration
EungyeTaxi.exe -singlePlayer -contentTest -dayLightDayProbe -mapAudit -mapAuditLayers "reviewed:r47:P51|reviewed:P22:56" -mapAuditOutput <폴더>
EungyeTaxi.exe -singlePlayer -contentTest -dayLightDayProbe -mapAudit -mapAuditDump "500,172,10" -mapAuditOutput <폴더>
```
