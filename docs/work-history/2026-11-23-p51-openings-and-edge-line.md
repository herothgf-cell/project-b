# 2026-11-23 P51 정문 개구부 축소와 보도 위 흰 선 제거 (지도 r52)

## 요구

사용자 요청: "P51 개구부랑 흰 선 수정해줘". [r51 기록](2026-11-23-p51-junction-signals.md)의 남은 항목 두 가지다.

1. 남쪽(한양수자인) 정문 앞 보도 개구부가 x 652~669로, 정문 횡단보도(x 659.4~669)보다 넓다.
2. 북서쪽 보도 위에 가는 흰 선이 있다(z≈99, x 652~665). 어디서 그려지는지 확인되지 않았다.

## 원인

- **개구부**: r51은 정문 우회전 연결을 편집기 기본 3차 곡선으로 그렸다. 이 곡선은 핸들 길이가 두 끝점 거리의 40%다. 그래서 정지선(x 650.3)부터 크게 돌아 들어간다. 보도 조각은 주행 차로가 지나가면 꺼지므로 개구부도 그만큼 넓어졌다. 같은 원인으로 북쪽(호반써밋) 개구부도 x 664~682로, 정문 횡단보도(x 665.9~674.2)보다 동쪽으로 넓었다.
- **흰 선**: 이번에 노면선 출처 추적 기능을 추가했다(아래 점검 도구). 추적해 보니 이 선은 `RoadPaintGeometry.PaintCompiled`가 그리는 **도로 가장자리선("Road edge")**이었다. 경위는 다음과 같다.
  - 은계남로의 동행·서행 차로는 같은 원본 도로를 공유한다.
  - r51 이동 뒤 서행 출발 차로는 교차로 안 x=675.6에서 시작했다. 반면 x 650~675 구간의 동행 쪽은 모두 회전 연결(`join:`)이라 가장자리선 계산에서 빠진다.
  - 그래서 그 구간에서는 차로가 하나만 잡혔다. 차로 폭 대신 도로 폭의 절반이 가장자리 거리로 쓰이면서 선이 북쪽 보도 위(z 98.9~99.0)로 밀렸다.
  - 동행 출발 차로(x 675 시작)도 같은 이유로 남동쪽 보도 가장자리에 짧은 선을 남겼다(r51 위 화면 x 677~682).

## 실제 구현

- **지도 r51 → r52** (`Assets/Editor/P51Openings.cs`, `P51Openings.Apply`; r51에서만 동작):
  - 정문 우회전 두 곳(동행→한양 `join:service:reviewed:r42:P51:hanyang-gate`, 서행→호반 `join:reviewed:P15:WB:hoban`)을 직선 + 중심선 반경 6.5m 원호 + 직선으로 바꿨다. 노면 장면(`road:polish:`)도 맞췄다.
  - 보도 조각은 r51과 같은 규칙으로 다시 켰다. 주행 차로가 지나는 조각만 끄며, 이번에 10개를 다시 켰다. 결과 개구부는 다음과 같다.
    - 남쪽: x 652.2~669.1 → **658.2~669.1**
    - 북쪽: x 664.6~682.1 → **664.6~676.5**
  - 직진 연결 두 개가 교차로를 끝까지 가로지르게 했다.
    - 서행 출발 차로는 서쪽 횡단보도 바로 뒤 x=651.5에서 시작한다(24.10m 잘라냄).
    - 동행 출발 차로는 동쪽 횡단보도 바로 뒤 x=684.5에서 시작한다(9.50m 잘라냄).
    - 잘라낸 길이만큼 `sourceOffset`을 늘려, 그 뒤 원본 도로 기준 거리(승객 정류장 계보)는 바뀌지 않는다.
  - 교차로 안 중앙선 숨김 띠(`reviewed:r42:P51:yellow-gap`)의 시작을 원본 거리 91.95 → 114.45로 옮겼다(새 서행 출발 시작점). 잘린 앞부분은 이제 회전 연결이라 원래 노면선을 받지 않는다.
  - 정문 도로, 횡단보도, 신호, 정지선, 신호 프로그램은 그대로다.
  - contentHash `3efe1faf…`, 24,925,022바이트, SHA256 `573391c5…`. `config/maps/local-input-manifest.json`을 갱신했다.
- **저장 이관**: `BusinessProgress`에 r52 해시를 추가했다. r52로 넘어오는 저장은 P51의 진입 관측·횡단 상태를 비우고 점수는 유지한다(r51 규칙을 `from<52`로 확장). `JunctionRouteFeedbackTests` 대상 범위를 r52까지 넓혔다.
- **점검 도구**: `-mapAuditDump "x,z,r"`이 렌더러 목록에 더해, 반경 안의 **생성 노면선 획**마다 이름·위치·방향·호출 경로를 적는다(`TrafficSystem.PaintTrace`). 합쳐진 노면 메쉬 안의 선도 출처를 찾을 수 있다. 이 인수가 없으면 기록하지 않는다.

## 검증

| 검사 | 결과 |
|---|---|
| `P51Openings.Apply` | `P51_OPENINGS sidewalk enabled+10 south=658.2..669.1 north=664.6..676.5`, `P51_OPENINGS_SAVED 3efe1faf…`, 지도 검사 통과. 첫 시도는 숨김 띠 범위 검사(`표시 범위에 비활성/누락 차로 구간`)에 막혀, 띠 시작 보정을 넣고 다시 적용했다. |
| 생성 노면선 추적(r51 → r52 실행본) | r51: (658,100) 반경 7m 안에 "Road edge" 14획(x 652.1~665.1, z 98.9~99.0). r52: (668,88) 반경 22m 안의 가장자리선·차선은 x 685(z 75)의 1획과 서행 진입 끝 차선 1획뿐. 보도 위 선은 없다. |
| `JunctionRouteFeedbackTests.VerifyMigration` | `JUNCTION_MIGRATION_OK target=r52 versions=38 checks=1261` |
| `NavigationStopRegressionTests.AuditRoutes` | routes=1676 unreachable=0 failures=0, 독립 최소비용 검산과 일치 |
| Editor 묶음 | 통과: `ImportedMarkingRendererTests`, `ReviewedMarkSuppressionTests`(90), `ReviewedNativeLanePresentationTests`, `ReviewedLaneMovementTraceTests`(37), `AdditionalReviewedUturnMap.Validate`, `UturnBlinkFeedbackTests`, `RuntimeServiceAnchorRegressionTests` |
| 이전부터 실패 | r51 기록과 같은 메시지: `PedestrianSignalConsistencyTests`("unknown countdown"), `NavigationStopRegressionTests.Run`("S09 … body/walking continuity") |
| 출하 빌드 | `PrototypeBuild.BuildHybrid` 종료 0, `project-map-build.json` contentHash `3efe1faf…` |
| 화면(출하 실행본, 위·운전자 시점) | [r51 위](../ui/2026-11-23-p51-openings/r51-before-top.jpg) · [r52 위](../ui/2026-11-23-p51-openings/r52-after-top.jpg) · [r51 생성 노면선](../ui/2026-11-23-p51-openings/r51-before-generated-paint.jpg) · [r52 생성 노면선](../ui/2026-11-23-p51-openings/r52-after-generated-paint.jpg) · [운전자 A](../ui/2026-11-23-p51-openings/r52-after-driver-a.jpg) · [운전자 B](../ui/2026-11-23-p51-openings/r52-after-driver-b.jpg). 북쪽 보도 위 흰 선과 남동쪽 짧은 선이 없어졌다. 남쪽 보도는 x≈659까지, 북쪽 보도는 x≈675부터 이어진다. |

| 배포 manifest | 소스 커밋 `e3de6615`으로 `distribution/EungyeTaxi-build-manifest.json`을 갱신했다(145파일, 158,242,110바이트, 바뀐 파일 7개). Python으로 크기·SHA256 전수 대조: 146항목(지도 포함) 불일치 0·누락 0, 필수 지도 입력 6항목 불일치 0. PowerShell 7이 없어 `update_distribution_manifest.ps1`은 실행하지 못했다. |

**빌드 크기 차이(원인 미확인)**: `resources.assets`가 46,935,688 → 54,382,288바이트로 커졌다. 다른 PC에서 만든 이전 출하 빌드 8번은 모두 약 46.9MB였다. `Assets/Resources` 원본과 .meta는 r51 출하(`2d68c664`) 이후 바뀌지 않았다. 따라서 이 PC의 `Library` 가져오기 결과 차이로 추정한다. 기능 검사와 화면에는 차이가 없었다. 다른 PC에서 다시 빌드하면 크기가 돌아오는지 확인할 필요가 있다.

## 남은 것

- 직접 운전 확인은 하지 않았다. 반경 6.5m 우회전의 체감, 일반 차량이 정문으로 들어갈 때 보도 턱에 닿는지, 교차로를 지나는 긴 직진 연결에서 신호 대기와 진행이 자연스러운지를 봐야 한다.
- x 685, z 75에 가장자리선 한 획(약 1m)이 남는다. 동행 출발 차로 첫 샘플이다. 위 화면에서는 보이지 않지만 가까이서 보이는지 수동으로 확인한다.
- 보행 신호 등화는 네모 그대로다(r51 남은 항목).
- `resources.assets` 7.4MB 증가 원인 확인(위 표 아래 설명).
- 이 PC에는 PowerShell 7이 없다. `check_workspace.ps1`·`update_distribution_manifest.ps1`은 Windows PowerShell 5.1에서 문법 차이로 실패한다. 그래서 manifest 값은 Python으로 같은 규칙(경로·바이트·SHA256)에 따라 계산·대조했다.

## 재현

```powershell
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod P51Openings.Apply   # r51 지도에서만 동작
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod PrototypeBuild.BuildHybrid   # 리소스 지도를 r52로 갱신
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod JunctionRouteFeedbackTests.VerifyMigration
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod NavigationStopRegressionTests.AuditRoutes
EungyeTaxi.exe -singlePlayer -contentTest -dayLightDayProbe -mapAudit -mapAuditDump "658,100,7" -mapAuditOutput <폴더>    # PAINT 줄: 생성 노면선 출처
EungyeTaxi.exe -singlePlayer -contentTest -dayLightDayProbe -mapAudit -mapAuditLayers "reviewed:r47:P51" -mapAuditOutput <폴더>
```

Editor 검사는 빌드 뒤에 실행한다. 빌드 전에는 `Assets/Resources`의 예전 지도를 읽는다(이번에 r47로 잘못 잡힌 첫 실행을 버렸다).

게임에서 확인: `builds/EungyeTaxi/EungyeTaxi.exe` → 은계 싱글 → M 지도에서 은계남로 한양수자인·호반써밋 정문 교차로로 경로를 잡아 동행·서행 직진과 양쪽 정문 우회전을 지나 본다.
