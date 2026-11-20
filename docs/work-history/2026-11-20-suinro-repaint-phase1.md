# 2026-11-20 지도 수정 1단계: 수인로 차선 다시 그리기·좌회전 유도선·유턴 표지 받침대 (지도 r48)

## 요구

사용자: "1단계 진행"([계획](2026-11-20-map-aerial-comparison-plan.md)의 A·B·I). 진행 중 사용자 정정: G1(J2338) 2번 "서쪽 도로 없음"은 공용 도로가 아니라 아파트 입구 뒤 단지 안 도로다. 2단계 목록에서 "서쪽 도로 추가"를 뺀다.

## 원인 다시 확인

계획에서는 "게임이 자동 생성한 선이 원본 위에 겹친다"고 봤다. 층별로 하나씩 켜서 찍어 보니 실제 원인은 달랐다(점검 도구 `-mapAuditLayers`). 엉킨 노란 선과 흩어진 점선은 거의 다 **원본 정밀도로지도 노면선 층**에서 나왔다. 원본 자료를 게임 화면 위에 겹치면 정확히 일치한다. 즉 게임은 원본(2020 측량)을 그대로 그리고 있고, 원본의 중앙선·가장자리선·안전지대 테두리가 지금 차선(카카오 항공사진·로드뷰 2026-06)과 맞지 않는다. 그래서 "자동 생성 선 숨기기"가 아니라 **해당 구간을 다시 그리는 방식**으로 바꿨다.

## 실제 구현

- **좌회전 유도선(전체)**: 원본 유도선(종류 525)을 일정한 0.75m 간격의 흰 점선 곡선으로 그린다. 원본이 황색 실선(J866·임시 385)이어도 흰색으로 그린다(J866 카카오 로드뷰로 흰 점선 확인). `HdRouteTrial.DrawMarkings`.
- **유턴 표지판 받침대(전체)**: 신호등 위에 붙는 유턴 조건 표지에 신호등 몸체까지 이어지는 받침대를 그린다. `RoadMapFacilities.Build`.
- **수인로 다시 그리기(지도 r48)**: `tools/suinro_lane_repaint.py`가 원본 주행 차로(실제로 차가 다니는 선)의 위치에서 차선 점선(3m/3m), 황색 복선 중앙선, 흰 가장자리선과 교체 영역을 계산한다. 결과는 `config/maps/edits/2026-11-20-suinro-lane-repaint.json`(시설 1,618개)이고, 편집기 메서드 `SuinroLaneRepaint.Apply`가 정식 지도에 붙인다.
  - 교체 영역 안의 원본 중앙선·차선·가장자리선과 안전지대 테두리(531)는 숨긴다. 이번 영역(`r48:suinro:`)에서만 노란 원본선을 방향과 관계없이 숨긴다. 예전 교체 영역은 기존 규칙 그대로라, 확인된 노란 경계석 선이 유지된다.
  - 교차로 상자(정지선·화살표·유도선·횡단보도)는 건드리지 않는다. J5057은 11/09 로드뷰로 검토한 진입부라 중심에서 36m까지 원본을 유지한다.
  - 유턴 연결 시작점 12m 안은 중앙선을 비운다.
  - 주행 차로·신호·유턴 연결·승하차 지점은 바뀌지 않는다(적용 시 차로 JSON이 같은지 확인).
- **저장 이관**: 지도 r48 해시를 `BusinessProgress` 이관 표에 추가해 r14~r47 저장이 r48로 이어진다. 이관 회귀 검사는 r48 대상으로 넓혔다. 직전 변경 교차로 확인은 r46→r48로 한다(r48은 도색만 바뀜).
- **점검 도구**: `MapAuditProbe`에 층별 촬영(`-mapAuditLayers "J..|J.."`)과 범위 지정(`-mapAuditStart`)을 추가했다.

지도: r47 → r48, contentHash `ac84fe3e…`, 24,934,620바이트, SHA256 `82a8291b…`. 원본(baseline)·장면·신호·차로는 같다.

## 검증

| 검사 | 결과 |
|---|---|
| `SuinroLaneRepaint.Apply` | `SUINRO_REPAINT_APPLIED ac84fe3e… facilities=1618`, 주행 차로 동일 |
| `JunctionRouteFeedbackTests.VerifyMigration` | `JUNCTION_MIGRATION_OK target=r48 versions=34 checks=1095` |
| Editor 묶음 | 통과: `ImportedMarkingRendererTests`, `ReviewedMarkSuppressionTests`, `ReviewedNativeLanePresentationTests`, `ReviewedLaneMovementTraceTests`, `RoadPresentation Navigation`, `AdditionalReviewedUturnMap.Validate`, `UturnBlinkFeedbackTests`, `RuntimeServiceAnchorRegressionTests` |
| 이전부터 실패하던 Editor 검사 | 원래 r47 상태(변경 되돌림)에서도 같은 메시지로 실패한다: `LaneDirectionPaintTests`(P29 진입), `CompiledUturn`, `AuthorizedUturnVisibility`(유턴 9개 기대, 현재 15개), `RoadviewCorridorTests`, `EungyeConnectivityRepairTests` |
| 실행본 차로 렌더 `-laneMovementReview` | `PASS checks=31` |
| 실행본 유턴 `-additionalUturnTest` | 23 PASS / 1 FAIL(이전부터 있는 J7973-E NPC 정지 단계) |
| 화면 | 수인로 7곳 층별 촬영. 차로를 가로지르던 노란 선이 사라지고 차선·중앙선이 차로를 따름. 유도선은 이어진 흰 점선 곡선, 유턴 표지판은 받침대로 신호등과 이어짐. [전후 비교](../ui/2026-11-20-suinro-repaint/) |
| `check_workspace.ps1 -Strict` | Input issues 0 |

출하 빌드·배포 검증 결과와 커밋은 LATEST에 적는다.

## 남은 것

- 교차로 바로 앞(정지선~25m) 원본 노란 조각 몇 개가 J866 서쪽·동쪽 등에 남아 있다.
- 은계로·대은로 등 다른 대로는 이번에 다시 그리지 않았다. 같은 방식(`tools/suinro_lane_repaint.py`를 도로 이름별로 일반화)으로 넓힐 수 있다.
- 2단계(막다른 길·차로 수·덩어리 교차로·고가), 3단계(차도 제외), 4단계(보호구역 노면)는 미착수다. G1 J2338의 "서쪽 도로 추가"는 사용자 정정으로 뺐다.
- 사람이 직접 운전하며 보는 확인은 하지 않았다.

## 재현

```powershell
python tools/suinro_lane_repaint.py config/maps/edits/2026-11-20-suinro-lane-repaint.json
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod SuinroLaneRepaint.Apply   # r47 지도에서만 동작
EungyeTaxi.exe -singlePlayer -contentTest -dayLightDayProbe -mapAudit -mapAuditLayers "J7973_3744|J866_5285" -mapAuditOutput <폴더>
```
