# 2026-11-26 지도 검토 6단계: 물결치는 중앙선을 곧게 (지도 r59)

## 요구

- 사용자: "계속 진행해 줘"(묶음 사이에 묻지 않고 이어서).
- 대상: 선형(L) 중 노란 중앙선이 물결치는 항목(J002-1, J010-2, J012-1, J023-2, J024-2, J025-2, J063-2, J066-2, J077-2).

## 원인과 방법

- 이 구간들의 노란 중앙선은 차로 배치로 만들어진다(`RoadPaintGeometry`). 한쪽에서 좌회전 포켓이 시작되고 반대쪽에서 안쪽 차로가 시작되면, 두 방향 차로 사이 빈 폭의 가운데가 옆으로 옮겨 가 선이 물결친다.
- 중앙선 침범 판정은 실제로 그려진 노란 선분을 쓴다(`TrafficSystem` 그리기 콜백 → `CentreLineCrossing`). 그래서 그리기 단계에서 선을 옮기면 판정도 같이 옮겨진다.
- **새 시설 `검토 중앙선 직선`(`ReviewedCentreLine.cs`):** `lanePresentation` 띠(중심·방향·길이·폭)다. 띠 안의 노란 중앙선 조각을 띠 축 위로 옮겨 그린다. 차로 기반 도색 함수(`RoadPaintGeometry.Paint`) 입구에서 그리기 콜백을 감싸므로, 차로와 경로는 그대로다. `ReviewedNativeLanePresentation` 검사(신호 교차로 지정 요구)에서는 이 종류를 뺐다.
- **띠를 놓는 조건(`tools/map_review_r59_plan.py`):** r58 덤프의 실제 중앙선 조각에서 구간 양 끝(첫·마지막 4 m)의 평균 위치를 잇는 직선을 쓴다. 그래서 끝에서 기존 선과 이어진다. 그 직선이 양방향 모든 차로(연결 차로 제외)의 가장자리에서 0.15 m 이상 떨어지고, 물결이 0.4 m보다 클 때만 놓는다. 정상 주행은 새 선을 넘지 않는다. 검사기 길이 제한(40 m) 때문에 긴 구간은 같은 축으로 나눴다.

## 결과(지도 r58 → r59, 띠 6개)

| 항목 | 구간 | 물결 | 차로까지 여유 | 처리 |
|---|---|---|---|---|
| J002-1 | J1919 동쪽 팔 54 m | 1.59 m | 0.25 m | 직선 |
| J010-2, J012-1 | J5076 동쪽 = J5750 서쪽 37 m | 1.53 m | 0.94 m | 직선 |
| J025-2 | r47 P51 서쪽 44 m, 동쪽 37 m | 0.50 m, 1.09 m | 1.34 m, 0.47 m | 직선 |
| J012-1 동쪽 / J023-2 | J5750 동쪽 = J6480 서쪽 | 1.5~1.8 m | −1.4~−1.6 m(직선이 r47 D 서행 안쪽 차로에 걸림) | 차로 형상 작업으로 남김 |
| J063-2 | J7876 남북 | 0.96 m | −0.01 m | 남김 |
| J066-2 | J7841~J7917 사이 | 차로 기반 선이 아님 | — | r47 검토 중앙선 획(30 m에 2.4 m 옮겨 가는 의도된 점진 구간)이다. 같은 구간의 남행 4차로 추가(J063-1·J066-1)와 함께 차로를 다시 배치할 때 고친다 |
| J024-2 | P22:56 남북 | 0.31 m | — | 물결이 작아 그대로 |
| J077-2 | J866 서쪽 | 정밀도로지도 노면표시(501) 구간 | — | 차로 기반 선이 아니어서 이 방법이 안 맞음. 남김 |

전후 화면(게임 위 시점, 왼쪽 11/24 · 오른쪽 r59): [before-after-top.jpg](../ui/2026-11-26-map-review-centre-lines/before-after-top.jpg)

## 검증

| 검사 | 종류 | 결과 |
|---|---|---|
| `MapReviewCrosswalkEdits.ApplyR59` | 지도 편집 | 추가 6, 저장 후 해시 재확인 |
| `PrototypeBuild.BuildHybrid`(편집기 검사 포함) | 빌드 | 성공 |
| J002·J010·J025 위 시점 화면 전후 | 위치 제어(화면) | 중앙선이 곧게 그려짐 |
| `-mapAudit -mapAuditSettle 1 -mapAuditObserve 1`(77곳) | 위치 제어 | 적신호 진입 0, 멈춤 0, 겹침 0 |
| `-mapAuditDump` + `tools/map_poles_in_lanes.py` | 위치 제어 | 바닥 기둥 356개, 차로 안 0 |
| `-districtTrafficTest` | 자동 물리 주행 | 실패. 기존과 같은 6대, 녹색 대기 줄 3 |

중앙선 침범 판정은 차로까지의 여유 계산으로만 확인했다. 직선 구간을 사람이 직접 주행해 침범 경고가 나지 않는지는 보지 않았다.

## 변경 파일

- 지도: `config/maps/project/eungye.project.json`(r59), `config/maps/local-input-manifest.json`, `config/maps/review/r59-centre-lines.json`, `config/maps/review/2026-11-25-map-review-decisions.json`(처리 93건)
- 코드: `Assets/Scripts/ReviewedCentreLine.cs`(새로), `RoadPaintGeometry.cs`(콜백 감싸기), `ReviewedNativeLanePresentation.cs`(검사 예외), `BusinessProgress.cs`(r59 해시), `Assets/Editor/MapReviewCrosswalkEdits.cs`(ApplyR59), `JunctionRouteFeedbackTests.cs`
- 도구: `tools/map_review_r59_plan.py`(새로), `tools/map_review_decisions.py`
- 문서: 이 기록, `LATEST.md`, `CURRENT-STATE.md`, `prototype-development-checklist.md`, `docs/ui/2026-11-26-map-review-centre-lines/before-after-top.jpg`

## 재현

```
python tools/map_review_r59_plan.py <r58 덤프 map-audit.txt>
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod MapReviewCrosswalkEdits.ApplyR59
python tools/map_review_bump.py 59 "<설명>"
Unity.exe ... -executeMethod PrototypeBuild.BuildHybrid
bash tools/map_review_regress.sh builds/EungyeTaxi/EungyeTaxi.exe <out>
```
