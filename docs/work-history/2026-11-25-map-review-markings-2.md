# 2026-11-25 지도 검토 3단계: 노면 2차(새 횡단보도·보호구역 포장·노면 글자·예고 마름모·횡단보도 삭제, 지도 r56)

## 요구

- 사용자: "노면 2차로 진행"에 이어 "한글로 하고, 수정 요청했던 거 모두 진행해 줘. 그런 다음 푸시해 줘."
- 대상: [노면 1차](2026-11-25-map-review-markings-1.md) 다음으로 남은 승인 노면 항목. 작업 목록은 `config/maps/review/2026-11-25-map-review-decisions.json`.

## 미리 확인한 것

- **빨간 포장 이전 결정과의 관계.** 11/24 5차 피드백에서 "바닥의 과속 구간 빨간 포장을 없앤다"는 요청으로 보호구역 구간 전체의 빨간 포장을 뺐다. 이번 검토에서는 실제 사진에 있는 특정 진입로의 적색 미끄럼방지 포장을 항목별로 승인했다. 그래서 승인된 위치에만 짧게(교차로 진입 약 20~40 m) 넣었다. 구간 전체 포장(`SpeedLimitSections`)은 되살리지 않았다.
- **시트의 픽셀 좌표는 쓸 수 없었다.** 검토 시트 문구의 "x≈…px" 값을 그대로 옮기면 실제 횡단보도와 10~20 m씩 어긋났다(J037 등). 그래서 위치는 게임 차로에서 다시 구했다.
  - 교차로 횡단보도: 교차로 둘레 차로 방향으로 가지를 찾았다(`map_review_marks.arms`). 생성 횡단보도처럼 "다른 도로 폭의 절반 + 3 m" 거리에 놓고, 가로지르는 차로 전체 폭으로 맞췄다.
  - 도로 중간 횡단보도: 시트 카카오 이미지(게임 화면과 같은 틀)에서 중심을 읽고, 방향과 폭은 게임 차로에서 구했다.
  - 모든 계획을 항공사진과 게임 화면에 겹쳐 보고 고쳤다(J052 동쪽 폭, J060 방향).
- **신호 없는 교차로는 교통 시스템 교차로가 아니다.** 접근로 자료가 비어 있어 생성 횡단보도 자리를 계산하지 않는다(덤프 후보 기록으로 확인: 후보는 신호 교차로 2곳뿐). 그래서 새 횡단보도는 교차로 번호 없는 도색 시설이다. 보행 신호·보행자 횡단 경로와는 연결하지 않았다.

## 고친 것(지도 r55 → r56, 시설 338개 추가)

| 종류 | 항목 | 내용 |
|---|---|---|
| 새 횡단보도 40 | J027-1·2, J029-1, J037-1, J042-3, J049-3, J050-2, J051-4, J052-2, J054-4, J055-1, J056-4, J060-1, J061-1, J062-1, J068-1, J072-1, J010-1 | 황색 12(보호구역)·흰색 28. 자전거횡단도 7곳(J061·J062·J068·J072, 바깥쪽). J010 교차로 안 대각선 2개 |
| 보호구역 적색 포장 235조각 | J018-3, J019-4, J021-1, J042-4, J051-4, J052-3, J063-4·J066-3, J065-2 | 횡단보도 바깥 2.5 m부터 진입 차로(또는 양방향) 20~40 m. 생성 차선·화살표(높이 0.071 m) 아래(0.064 m)에 깔아 표시가 가려지지 않음 |
| 노면 글자 53 | J018-3, J019-4, J021-1, J052-3, J063-4·J066-3 | 진입 차로마다 "30", 일부 "어린이 보호구역" |
| 예고 마름모 9 | J023-3, J041-4, J043-4, J049-4, J055-3 | 진입 차로마다 흰 마름모(폭 1.6 m, 길이 5 m) |
| 횡단보도 삭제 1 | J067-1·J019-2 | J7917_2565 북쪽 생성 횡단보도. 항공사진에도 없다. 그 자리의 보행 신호등·보행자 횡단·교차로 정리 구역도 함께 뺐다 |

### 구현

- `RoadSurfaceMarks.cs`(새로): "line" 시설에 종류 3개를 더했다(`보호구역 적색 포장`, `노면 글자`, `횡단보도 예고`). 횡단보도 종류 `없음`도 더했다. 새 필드는 쓰지 않았다(지도 해시 보존).
- `없음` 횡단보도를 무시하게 한 곳: `TrafficSystem`(보행 신호등), `AmbientPedestrianPlacement`(보행자 횡단), `RoadJunctionGeometry`(정리 구역), `LaneMergePaint`, 승객 정차 후보 2곳.
- `TrafficSystem.AddApproach`: 횡단보도 없는 접근로의 "생성했다면 놓였을 자리"를 덤프에 `CANDIDATE`로 남긴다(검사용, 그림 없음).
- 계획: `tools/map_review_marks.py`(가지·폭·포장·차로 위 점), `tools/map_review_r56_plan.py` → `config/maps/review/r56-mark-edits.json`. 적용: `MapReviewCrosswalkEdits.ApplyR56`(r55에서만). contentHash `496208f5…efc3`.
- 저장 이전 r56 해시 추가(`BusinessProgress.cs`), 검사 범위 r25~r56.

전후 화면(게임 위 시점, 왼쪽 r53 11/24 · 오른쪽 r56): [before-after-top.jpg](../ui/2026-11-25-map-review-markings-2/before-after-top.jpg)

### 이번에 하지 않은 노면

- **정지선 추가(J027-1, J029-1, J056-4).** 도색만 하면 차가 서지 않는 정지선이 된다. 교차로 정지선 판정과 함께 해야 해서 뺐다.
- **일부 위치:** J037 북측 두 번째와 먼 서쪽 횡단보도, J049 먼 B3·A1, J050 C3·B2 황색, J052 G2~H1. 시트 위치를 항공사진에서 확인하지 못했다.
- **크기 변경:** J065-1 남북 8 m, J067-2 남쪽 폭.
- **횡단보도 위치 이동:** J001-1, J003-1·2, J005-1, J006-1, J011-1, J022-3, J024-1, J077-4. 정지선을 함께 옮겨야 하는 경로 변경이다.
- **로드뷰 확인 항목:** J002-2, J004-3, J005-2, J009-2, J015-1·2, J024-3, J025-3, J029-3, J030-4, J033-1, J035-1, J037-2, J041-2, J044-2·3·4, J046-4, J048-3, J059-4, J061-2, J065-4, J067-4, J070-2, J071-2, J073-1·2, J074-2, J076-1. J036-3은 위치를 찾지 못했다.

## 검증

| 검사 | 종류 | 결과 |
|---|---|---|
| `PrototypeBuild.BuildHybrid`(편집기 검사 포함) | 빌드 | 성공(`PROTOTYPE_BUILD_OK`) |
| `MapReviewCrosswalkEdits.ApplyR56` | 지도 편집 | 추가 338, 저장 후 해시 재확인 |
| 계획 겹쳐 보기(항공사진·게임) | 계획 확인 | 교차로 15곳 확인 후 J052·J060 수정 |
| 교차로 27곳 위 시점 화면 전후 | 위치 제어(화면) | 횡단보도·포장·글자·마름모·삭제가 표시됨. J043 마름모는 70 m 비교 범위 밖이라 화면으로 보지 못함 |
| `-mapAudit -mapAuditSettle 1 -mapAuditObserve 1`(77곳) | 위치 제어 | 적신호 진입 0, 멈춤 0, 겹침 0, 보행자 근접 0 |
| `-mapAuditDump` + `tools/map_poles_in_lanes.py` | 위치 제어 | 바닥 기둥 368개, 차로 안 0 |
| `-districtTrafficTest` | 자동 물리 주행 | 실패. r55와 같은 6대(J3516 3, J7801 유턴 2, J6199 유턴 1), 녹색 대기 줄 3 실패 |

사람 플레이와 전체 캠페인 검사는 하지 않았다.

## 변경 파일

- 지도: `config/maps/project/eungye.project.json`(r56), `config/maps/local-input-manifest.json`, `config/maps/review/r56-mark-edits.json`, `config/maps/review/2026-11-25-map-review-decisions.json`(처리 62건)
- 코드: `Assets/Scripts/RoadSurfaceMarks.cs`(새로), `RoadMapFacilities.cs`, `TrafficSystem.cs`, `AmbientPedestrianPlacement.cs`, `RoadJunctionGeometry.cs`, `LaneMergePaint.cs`, `AlternativePassengerStops.cs`, `PassengerStopCatalog.cs`, `BusinessProgress.cs`, `Assets/Editor/MapReviewCrosswalkEdits.cs`, `Assets/Editor/JunctionRouteFeedbackTests.cs`
- 도구: `tools/map_review_marks.py`, `tools/map_review_r56_plan.py`(새로), `tools/map_review_decisions.py`
- 문서: 이 기록, `LATEST.md`, `CURRENT-STATE.md`, `prototype-development-checklist.md`, `docs/ui/2026-11-25-map-review-markings-2/before-after-top.jpg`

## 재현

```
PYTHONPATH=artifacts/py314-deps python tools/map_review_r56_plan.py <r55 덤프 map-audit.txt> --check <dir>
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod MapReviewCrosswalkEdits.ApplyR56   (r55 지도에서만)
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod PrototypeBuild.BuildHybrid
builds/EungyeTaxi/EungyeTaxi.exe -singlePlayer -contentTest -dayLightDayProbe -mapAudit -mapAuditOutput <dir>   (화면 캡처, -batchmode 없이)
python tools/map_review_before_after.py artifacts/map-review-20261124/game <dir> <out> 018-J7841_867 ...
```

## 다음 작업

1. 횡단보도 위치 이동 + 정지선(위 9건)
2. 폭·인도(W 37), 차로 수(N 11), 선 종류(M 18), 선형(L 20), 연결·교차로 모양(C 56, Q6 아파트 입구 규칙·Q10 교통섬 포함)
3. 로드뷰 확인 항목
