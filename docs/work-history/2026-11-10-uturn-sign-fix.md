# 2026-11-10 J5057 유턴 표시↔판정 일치 (A안)

## 요구

[거리뷰 대조](2026-11-10-roadview-lane-audit.md)에서 찾은 문제를 사용자가 "A안 유턴 허용"으로 골랐다. J866·J5057의 차선 화살표는 유턴+좌회전을 그리지만, 해당 원본 회차 곡선은 NGII `turn=0`이고 유턴 조건 표지가 없었다. 그래서 플레이어가 유턴하면 항상 L01 위반으로 기록됐고, 내비에는 유턴 안내가 나오지 않았으며, NPC는 직진 신호에 맞춰 돌았다.

## 실제 구현

- **J5057 동향·서향**: 기존 검토 유턴과 같은 방식(`AdditionalReviewedUturnMap`)으로 정식 유턴 `join:reviewed:J5057-E:uturn`, `join:reviewed:J5057-W:uturn`(`turn=-2`)을 추가했다. 진입 차로(`A222BC004208:0`, `A2218G611401:0`)에 `유턴 (좌회전·보행 신호시)` 표지를 붙였다. 진입·출구 차로는 원본 회차 곡선과 같다.
- 원본 회차 곡선 `A2218G611358:0`, `A2218G611107:0`은 비활성으로 바꾸고 앞 차로의 연결 목록에서 뺐다. 지도 문서에서 지우지는 않았다.
- **J7973 서향 급회차** `A222BC004191:0`(약 11m, 거리뷰 근거 없음, 조건 표지 없음)도 같은 방식으로 비활성했다. 같은 교차로의 검토 유턴 `J7973-E`는 그대로다.
- **J866 서향은 보류**: 이번 파노라마 해상도로는 보조표지 글자를 읽지 못했다. 원본 회차 곡선과 기존 문제(유턴하면 위반)가 그대로 남아 있다.
- **유턴 최소 간격 10m → 9.9m** (`UturnMovement.MinimumAcross`): J5057 동향 포켓에서 서향 출구 차로까지 원본 간격이 9.99m였다.
- **차선 화살표** (`ReviewedLaneMovementTrace`): 원본 차로에서 바로 이어지는 정식 유턴 연결도 유턴 역할로 인정한다. J5057 포켓의 유턴+좌회전 표시가 유지된다. 유턴 노면 표시(중앙 개구부, 정지선 가까이의 유턴 화살표)는 기존 정식 유턴 규칙으로 추가된다.
- 지도 검토 기록: 기존 3개 유턴은 이후 작업(`StopUturnMapEdits`)에서 수정됐다. `Apply`를 다시 실행하면 그 수정을 덮어쓰므로 새 항목만 `ApplyReplacedNative`로 적용했다.
- 검사 기대값을 실제 지도에 맞췄다. 정식 유턴 6→8, 검토 유턴 3→5, 노면 개구부 폭 하한 `MinimumAcross`. 관찰 테스트에는 실패할 때 단계 값을 출력하도록 했고, 프로브는 유턴 단계가 없을 때 예외로 멈추는 대신 실패로 기록한다.

## 변경 파일

`config/maps/project/eungye.project.json`(revision 12→13, contentHash `424374e5…`, 21,665,104바이트, SHA256 `fe00e8e1…`), `config/maps/local-input-manifest.json`, `Scripts/UturnMovement.cs`, `Scripts/ReviewedLaneMovementTrace.cs`, `Editor/AdditionalReviewedUturnMap.cs`, `Editor/AdditionalUturnObserverTests.cs`, `Editor/ImportedMarkingRendererTests.cs`, `Editor/RoadPresentationRevisionChecks.cs`, `Editor/RoadviewCorridorTests.cs`, `Scripts/AdditionalUturnIntegrationProbe.cs`, `Scripts/UturnIntegrationProbe.cs`. 출하 `builds/EungyeTaxi/`, `distribution/EungyeTaxi-build-manifest.json`.

지도 대조(원본 백업과 비교): baseline·scene·signals 동일. 차로는 2개 추가, 5개 변경(진입 2개의 ruleId·next, J7973 포켓의 next, 원본 곡선 3개 disabled). 시설은 유턴 조건 표지 2개 추가. 그 밖의 변경은 없다.

## 검증 (실제 결과)

로그는 Git 제외 `artifacts/uturn-sign-fix/`.

| 검사 | 종류 | 결과 |
|---|---|---|
| `AdditionalReviewedUturnMap.Validate` | Editor, 지도 | 통과: `movements=5 replacedNative=3`, 경로·기하·조건 표지 |
| 차로 역할·표시(`ReviewedLaneMovementTraceTests`, `ReviewedNativeLanePresentationTests`, `LaneDirectionPaintTests`) | Editor | 통과: J5057 `A2218G611350:2`·`A2218G611401:0` 역할 9(유턴+좌회전) 유지 |
| `ImportedMarkingRendererTests`, `RoadPresentationRevisionChecks.CompiledUturn/AuthorizedUturnVisibility/Navigation`, `RoadviewCorridorTests`, `EungyeConnectivityRepairTests`, `TrafficSignalLeftDemandRegressionTests`, `UpstreamUturnObserverTests`, `UturnBlinkFeedbackTests` | Editor | 통과 |
| `AdditionalUturnIntegrationProbe`(출하 실행본 `-singlePlayer -contentTest -batchmode -nographics -additionalUturnTest`) | 실행본, NPC 위치 제어 | **J5057-E/W 모두 통과**: NPC 경로가 정식 유턴(J5057:-2)을 지나고, 허용 신호 단계를 찾고, 정적 충돌 없이 완주했다. 전체 19 PASS / 1 FAIL(J7973-E, 아래 기존 실패) |
| 차로 렌더(`-brightUiCheck -laneMovementReview`) | 실행본, 위치 제어 렌더 | 테스트 빌드와 출하본 모두 `PASS checks=31` |
| `check_workspace.ps1 -Strict` | 입력 | exit 0, issues 0 |
| `PrototypeBuild.BuildHybrid` | 출하 | exit 0, Build Finished Success |
| 통합/PvP manifest VerifyOnly | 배포 | `DISTRIBUTION_OK files=143 bytes=127440384` 두 번 |

**이번 변경 전부터 있던 실패**(이번에 확인, 수정 안 함):
- `AdditionalUturnObserverTests` 단계 검사: 원래 지도(백업)로 기존 3개만 돌려도 J7254-SW가 같은 메시지로 실패한다. 값을 찍어 보면 모든 유턴(J8066·J7973·J5057 포함)이 `left=False walk=True`다. 축소 테스트 지도에서는 좌회전 녹색 단계가 오지 않는다. 보행 신호 때 허용, 그 외 금지는 J5057도 기존 유턴과 같다. 이 테스트의 플레이어 주행 판정 부분은 이 검사에 막혀 실행되지 않았다.
- `RoadPresentationRevisionChecks.Signals`: J1927_5181 신호등을 4구로 기대하지만, 기획20에서 3구로 바꿨다(`actual=3`).
- `AdditionalUturnIntegrationProbe` J7973-E: NPC 경로에 교차로 정지 단계가 없다(`stops=`). 이번에 바꾸지 않은 차로(`A222BC004180`→`J7973-E`→`editor:segment…`)다. 이전 실행본에서 재현하지는 않았다.

**하지 않은 검사**: 플레이어가 J5057에서 직접 유턴해 보는 수동·물리 주행, 전체 3일 캠페인, 다른 PC LAN. 위반/준수 판정은 코드 경로(`DrivingLaw`의 `uturnSigned`·`UturnPermitted`)로만 확인했다.

## 커밋

소스 `1d85fa37425d6893787d22d5578e03b18f0d7b06`, 출하 `f318d84ad8fcbaa4cde0483e5b1134079384914f`(브랜치 `feature/vehicle-purchase-3day`, push 안 함). 이 기록의 커밋은 `git log -1 --oneline -- docs/work-history/2026-11-10-uturn-sign-fix.md`로 확인한다.

같은 시간에 다른 세션이 같은 폴더에서 차량 UI 작업을 했다. 그 세션은 별도 worktree에서 빌드해 출하 30bce76을 냈다. 이번 출하는 그 위의 소스(차량 UI 포함)에서 다시 빌드한 것이다. `unity/EungyeTaxi/Assets/Scenes/FirstDrive.unity`의 빌드 부산물 ID 변경과 미추적 `PackageManagerSettings.asset`은 커밋하지 않았다.

## 남은 항목

1. J866 서향: 보조표지를 다시 확인한 뒤 같은 방식으로 적용. 그 전까지는 유턴하면 위반이다.
2. 기존 실패 3건(관찰 테스트의 좌회전 단계, J1927 신호등 구수 기대값, J7973-E NPC 정지 단계) 정리.
3. J5057에서 사람이 직접 유턴하는 플레이 확인.

## 재현

```powershell
# Editor(Unity 2022.3.62f2, Editor 닫은 상태)
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod AdditionalReviewedUturnMap.ApplyReplacedNative
# 실행본 검사
builds/EungyeTaxi/EungyeTaxi.exe -singlePlayer -contentTest -batchmode -nographics -additionalUturnTest   # artifacts/additional-uturn-probe.txt
builds/EungyeTaxi/EungyeTaxi.exe -singlePlayer -brightUiCheck -laneMovementReview -brightUiOutput <폴더>
```
