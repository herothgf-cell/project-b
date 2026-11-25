# 2026-11-26 지도 검토 5단계: 좌회전 유도선·지그재그 서행선 (지도 r58)

## 요구

- 사용자: "순서대로 쭈욱 진행해 줘, 중간중간 묻지 말고." 남은 승인 항목을 묶음 단위로 이어서 고친다.
- 이번 묶음: 선 종류(M) 중 그림만 바꾸면 되는 항목.

## 고친 것(지도 r57 → r58, 시설 641개 추가)

| 항목 | 교차로 | 내용 |
|---|---|---|
| J004-2, J007-1, J011-3, J013-2, J016-2, J017-3, J021-4 | J2145, J4396, J5692, J6199, J7254, J7801, J8066 | 교차로 안 좌회전 유도선. 좌회전 연결 차로(turn −1) 양쪽 가장자리를 따라 흰 점선(1 m 칠·1 m 띄움, 폭 0.15 m). 이웃한 두 좌회전 차로가 같이 쓰는 가장자리는 한 번만 그리고, 횡단보도·자전거횡단도·정지선 위 점선은 뺐다 |
| J007-3 | J4396_13458 북서 진입 | 진입 차로 가장자리 지그재그 흰 실선(2.5 m 간격, 좌우 0.3 m)과 적색 미끄럼방지 포장(교차로에서 20~50 m). Q9 답(로드뷰로 확인)은 r55 때 본 항공사진의 붉은 포장·지그재그로 대신했다 |

- 유도선이 게임 연결 차로를 따라가므로 실제 선과 모양은 조금 다를 수 있다. 차가 실제로 도는 길을 표시한다. J017-3의 "직진 연장 점선"은 넣지 않았다.
- 계획: `tools/map_review_r58_plan.py <r57 배포 덤프>` → `config/maps/review/r58-line-marks.json`. 적용: `MapReviewCrosswalkEdits.ApplyR58`.
- 판 등록 도구 `tools/map_review_bump.py <판> "<설명>"`를 새로 만들었다. 저장된 지도 파일에서 contentHash를 직접 읽어 저장 이전 상수·검사 범위·입력 manifest를 갱신하므로, r57처럼 해시가 잘릴 수 없다. r58 해시 `7cf4928a…2a20`.
- 회귀 스크립트 `tools/map_review_regress.sh <exe> <out>`를 만들었다(77곳·기둥·교통 검사).

전후 화면(게임 위 시점, 왼쪽 r57 · 오른쪽 r58): [before-after-top.jpg](../ui/2026-11-26-map-review-guide-lines/before-after-top.jpg)

## 이번 묶음에서 뺀 선 종류 항목과 이유

- **황색 빗금 안전지대(J009-1, J056-2, J059-1, J060-2, J075-4):** 실제는 양방향 차로 사이가 약 3 m 벌어져 있다. 게임은 두 방향 차로가 붙어 있어서, 빗금만 그리면 차로 위에 얹힌다. 차로 간격을 바꾸는 차로 형상 작업과 함께 한다.
- **중앙선 물결 정리(선형 L: J002-1, J010-2, J012-1, J023-2, J024-2, J025-2, J063-2, J066-2, J077-2):** 노란 중앙선은 양방향 차로 사이에 생긴다. 좌회전 포켓이 시작되는 곳에서 차로가 옆으로 옮겨 가며 물결친다. 검토 중앙선(`검토 중앙선` 획)으로 덧그리면 중앙선 침범 판정도 그 획을 쓴다(`ReviewedCorridorPaint`). 그래서 차로는 그대로 두고 선만 곧게 그리면 정상 주행이 침범으로 잡힌다. 차로 점을 옮기는 작업으로 한다.
- 로드뷰 확인 항목(J003-3, J012-2, J030-3, J056-5, J063-3)

## 검증

| 검사 | 종류 | 결과 |
|---|---|---|
| `MapReviewCrosswalkEdits.ApplyR58` | 지도 편집 | 추가 641, 저장 후 해시 재확인 |
| `PrototypeBuild.BuildHybrid`(편집기 검사 포함) | 빌드 | 성공, `builds/EungyeTaxi/` |
| 교차로 7곳 위 시점 화면 전후 | 위치 제어(화면) | 유도선·지그재그·포장 표시, 횡단보도 위 점선 없음 |
| `-mapAudit -mapAuditSettle 1 -mapAuditObserve 1`(77곳) | 위치 제어 | 적신호 진입 0, 멈춤 0, 겹침 0 |
| `-mapAuditDump` + `tools/map_poles_in_lanes.py` | 위치 제어 | 바닥 기둥 353개, 차로 안 0 |
| `-districtTrafficTest` | 자동 물리 주행 | 실패. 기존과 같은 6대, 녹색 대기 줄 3 |

사람 플레이와 전체 캠페인 검사는 하지 않았다.

## 남은 것(처리 89/277)

- 차로 형상이 필요한 것: 폭·인도 W 28, 차로 수 N 11, 선형 L 20, 연결·교차로 모양 C 56, 빗금 안전지대 5. 차로 점·폭·연석(장면 `road`·`sidewalk` 객체)을 함께 옮겨야 하고, 항목마다 경로·신호·정차점·주유소 계보 회귀가 필요하다(예: 2026-11-12 J7973, 2026-11-13 J8066 작업).
- 로드뷰 확인 항목과 정지선 추가(노면 G 33)

## 변경 파일

- 지도: `config/maps/project/eungye.project.json`(r58), `config/maps/local-input-manifest.json`, `config/maps/review/r58-line-marks.json`, `config/maps/review/2026-11-25-map-review-decisions.json`
- 코드: `Assets/Editor/MapReviewCrosswalkEdits.cs`(ApplyR58), `Assets/Editor/JunctionRouteFeedbackTests.cs`, `Assets/Scripts/BusinessProgress.cs`(r58 해시)
- 도구: `tools/map_review_r58_plan.py`, `tools/map_review_bump.py`, `tools/map_review_regress.sh`(새로), `tools/map_review_decisions.py`
- 문서: 이 기록, `LATEST.md`, `CURRENT-STATE.md`, `prototype-development-checklist.md`, `docs/ui/2026-11-26-map-review-guide-lines/before-after-top.jpg`
- 배포: `builds/EungyeTaxi/`, `distribution/EungyeTaxi-build-manifest.json`(다음 커밋)

## 재현

```
python tools/map_review_r58_plan.py artifacts/map-review-20261124/deploy-r57/dump/map-audit.txt
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod MapReviewCrosswalkEdits.ApplyR58   (r57에서만)
python tools/map_review_bump.py 58 "<설명>"
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod PrototypeBuild.BuildHybrid
bash tools/map_review_regress.sh builds/EungyeTaxi/EungyeTaxi.exe <out>
```
