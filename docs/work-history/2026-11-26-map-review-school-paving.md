# 2026-11-26 지도 검토 11단계: 로드뷰로 확인한 보호구역 진입부 적색 포장 (지도 r65)

## 요구

- 사용자: "계속 진행해 줘"(묶음 사이에 묻지 않고 이어서). 로드뷰로 확인한 J004-3·J025-3 적색 포장을 반영한다.

## 확인(로드뷰 2026-06)

| 항목 | 로드뷰에서 본 것 |
|---|---|
| J004-3 | 은계초 앞 J2145_15970 남동쪽 진입부는 양방향 차로 전체가 적색 어린이보호구역 포장. 버스차로가 아님. 노면 글자는 보이지 않음 |
| J025-3 | 적색 차로는 버스·자전거 전용차로가 아니라 어린이보호구역 포장이며 차로에 '어린이보호구역'과 '30'이 있음. 교차로로 들어오는 차로 쪽(서쪽 가지 남측, 동쪽 가지 북측) |

## 바꾼 것(지도 r64 → r65, 추가 82)

| 항목 | 내용 |
|---|---|
| J004-3 | 남동쪽 가지(방위 133°) 16~50 m, 양방향 모든 차로에 적색 포장 43조각(차로를 따라 약 6 m씩) |
| J025-3 | 서쪽 가지 17~58 m·동쪽 가지 18~55 m의 교차로로 들어오는 차로에 적색 포장 31조각, 들어오는 차로마다 '30'(24 m)·'어린이보호구역'(29 m) 글자 8 |

- 계획 `PYTHONPATH=artifacts/py314-deps python tools/map_review_r65_plan.py --check <폴더>` → `config/maps/review/r65-school-paving.json`. 항공사진 위 계획 그림으로 범위를 맞췄다. J004 글자는 로드뷰에서 보이지 않아 넣지 않았다.
- 적용 `MapReviewCrosswalkEdits.ApplyR65`(r64에서만). r65 해시 `33cfa49c…a8ad8`.

## 검증

| 검사 | 종류 | 결과 |
|---|---|---|
| `MapReviewCrosswalkEdits.ApplyR65` | 지도 편집 | 추가 82, 바꿈 0. 저장 후 해시 재확인 |
| `PrototypeBuild.BuildHybrid` | 빌드 | 성공 |
| `-mapAudit -mapAuditSettle 1 -mapAuditObserve 1`(77곳) | 위치 제어 | 적신호 진입 0, 멈춤 0, 겹침 0 |
| `-mapAuditDump` + `tools/map_poles_in_lanes.py` | 위치 제어 | 바닥 기둥 357개, 차로 안 0 |
| `-callBoardCheck` | 위치 제어(승객 호출판) | 통과 114, 실패 0 |
| `-districtTrafficTest` | 자동 물리 주행 | 실패. 멈춘 차량 3(J7801 유턴 2·J6199 유턴 1, 이전과 같음), 녹색 대기 줄 3 |

게임 화면 캡처, 사람 플레이, 전체 캠페인 검사는 하지 않았다. 처리 142/277.

## 남은 일(135건)

남은 항목은 대부분 노면 도색으로 끝나지 않는다. 차로 점·폭과 장면 포장·보도를 함께 옮기는 차로 형상 작업이나 새 도로·연결을 만드는 도로망 작업이다(폭·연석, 선형, 차로 수, 도로 추가). 로드뷰로 판정만 해 둔 것은 `config/maps/review/2026-11-26-roadview-verdicts.json`에 있다. 예: J062-2 진입부 편도 2차로, J048-2 약 6 m 골목, J050-3 차도 약 9~10 m, J040-3 대은로 편도 3차로, J059-3 수인로 서행 4차로.

## 변경 파일

- 지도: `config/maps/project/eungye.project.json`(r65), `config/maps/local-input-manifest.json`, `config/maps/review/r65-school-paving.json`, `config/maps/review/2026-11-25-map-review-decisions.json`
- 코드: `Assets/Editor/MapReviewCrosswalkEdits.cs`(ApplyR65), `Assets/Scripts/BusinessProgress.cs`(r65 해시), `Assets/Editor/JunctionRouteFeedbackTests.cs`
- 도구: `tools/map_review_r65_plan.py`(새로), `tools/map_review_decisions.py`
- 문서: 이 기록, `LATEST.md`, `CURRENT-STATE.md`, `prototype-development-checklist.md`

## 재현

```
PYTHONPATH=artifacts/py314-deps python tools/map_review_r65_plan.py --check <폴더>
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod MapReviewCrosswalkEdits.ApplyR65   (r64에서만)
python tools/map_review_bump.py 65 "<설명>"
Unity.exe ... -executeMethod PrototypeBuild.BuildHybrid
bash tools/map_review_regress.sh builds/EungyeTaxi/EungyeTaxi.exe <out>
builds/EungyeTaxi/EungyeTaxi.exe -contentTest -callBoardCheck -callBoardOutput <dir>
```
