# 2026-11-10 J5057 유턴 표시↔판정 일치 (A안)

## 요구

[거리뷰 대조](2026-11-10-roadview-lane-audit.md)에서 찾은 문제를 사용자가 "A안 유턴 허용"으로 골랐다. J866·J5057의 차선 화살표는 유턴+좌회전을 그리지만, 해당 원본 회차 곡선은 NGII `turn=0`이고 유턴 조건 표지가 없었다. 그래서 플레이어가 유턴하면 항상 L01 위반으로 기록됐고, 내비에는 유턴 안내가 나오지 않았으며, NPC는 직진 신호에 맞춰 돌았다.

## 실제 구현

- **J5057 동향·서향**: 기존 검토 유턴과 같은 방식(`AdditionalReviewedUturnMap`)으로 정식 유턴 `join:reviewed:J5057-E:uturn`, `join:reviewed:J5057-W:uturn`(`turn=-2`)을 추가했다. 진입 차로(`A222BC004208:0`, `A2218G611401:0`)에 `유턴 (좌회전·보행 신호시)` 표지를 붙였다. 진입·출구 차로는 원본 회차 곡선과 같다.
- 원본 회차 곡선 `A2218G611358:0`, `A2218G611107:0`은 비활성으로 바꾸고 앞 차로의 연결 목록에서 뺐다. 지도 문서에서 지우지는 않았다.
- **J7973 서향 급회차** `A222BC004191:0`(약 11m, 거리뷰 근거 없음, 조건 표지 없음)도 같은 방식으로 비활성했다. 같은 교차로의 검토 유턴 `J7973-E`는 그대로다.
- **J866 서향**: 처음에는 보조표지를 읽지 못해 보류했고, 아래 [후속](#후속-j866-서향-사용자-확인)에서 사용자 확인에 따라 적용했다.
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

1. J866 서향 보조표지 문구(현재 조건은 게임 가정).
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

## 후속: J866 서향 (사용자 확인)

사용자가 보낸 2025-10 거리뷰 캡처 2장은 도로를 옆에서 본 구도라 서향 신호등 팔의 표지가 보이지 않았다. 사진에는 횡단보도 앞 중앙선이 노란 실선으로 보였고, 2026-05 파노라마에서는 직진·좌회전 표지만 읽혔다. 이후 사용자가 "유턴하는 곳 맞아"로 확인했다(2026-10-27에도 서쪽 유턴을 보고한 기록이 `EungyeConnectivityRepair` 주석에 있다).

- `join:reviewed:J866-W:uturn`(A2218G611780:0 → A222BC004223:0)과 `유턴 (좌회전·보행 신호시)` 표지를 추가하고, 원본 회차 곡선 A2218G611778:0을 비활성했다. 보조표지 문구를 읽지 못했으므로 조건은 J5057과 같은 **게임 가정**이다. 근거는 `state=user`, `ruleConfirmed=false`로 기록했다(`AdditionalReviewedUturnMap.ApplyJ866`).
- 실행 시 연결 보완 `west-inner-straight`(A2218G611364:2 → A2218G611780)가 이 진입 차로의 규칙 표지 때문에 빠져 added가 25→24로 줄어드는 것을 검사에서 찾았다. 보완 대상 차로를 쪼개지 않는 이 경로에서만 유턴 조건 표지를 허용하고, 비활성된 원본 회차와 정식 유턴의 교체를 같은 연결로 보도록 `EungyeConnectivityRepair.Unedited`에 선택 인자를 추가했다. 그 결과 added=25로 돌아왔다.
- 검사 기대값을 고쳤다: 정식 유턴 8→9, 검토 유턴 5→6.

지도 대조(revision 13 백업과 비교): baseline·scene·signals 동일. 차로 1개 추가, A2218G611780 ruleId·next 변경, A2218G611778 비활성. 시설은 조건 표지 1개(user) 추가. revision 14, contentHash `9625b6c6…`, 21,670,000바이트, SHA256 `c9aa1e76…`.

| 검사 | 결과 |
|---|---|
| `AdditionalReviewedUturnMap.Validate` | 통과 `movements=6 replacedNative=4` |
| Editor 묶음(차로 역할·표시·노면·내비·코리도·연결 보완·좌회전 수요·상류 유턴·깜빡이·서비스 앵커) | 모두 통과. `EUNGYE_CONNECTIVITY_REPAIR_TESTS_OK checks=2550 added=25` |
| `AdditionalUturnObserverTests` J866-W | 기존과 같은 이유로 단계 검사 실패(`legal=45 illegal=0 left=False walk=True`) |
| 출하 실행본 `-additionalUturnTest` | J866-W 경로·허용 단계·완주 통과. 23 PASS / 1 FAIL(기존 J7973-E) |
| 출하 실행본 `-laneMovementReview` | `PASS checks=31`, 실행 로그 `added=25`. 위에서 본 렌더에서는 J866 유턴 글리프 위에 NPC 차량이 있어 가려졌다 |
| `check_workspace.ps1 -Strict` / BuildHybrid / 통합·PvP VerifyOnly | issues 0 / Success / `DISTRIBUTION_OK files=143 bytes=127445792` |

커밋: 소스 `0c4ca865bbb51936f3e2701f84f956ae3f81d3da`, 출하 `b43670648447d277774906098bee6d0374a62408`(push 안 함). 사람이 직접 J866에서 유턴하는 플레이는 하지 않았다.
