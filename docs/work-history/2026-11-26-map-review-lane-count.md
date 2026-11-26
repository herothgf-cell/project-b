# 2026-11-26 지도 검토 13단계: 차로 추가·삭제 도구와 첫 차로 수 조정 (지도 r68)

## 요구사항

- 사용자: "도구 만들기". 남은 폭·차로 수 항목 대부분은 차로를 늘리거나 줄이고 교차로 연결을 다시 맞춰야 하므로, r66 단면 변형 도구 위에 차로 추가·삭제 도구를 만든다.
- 만든 도구로 첫 항목을 처리하고, 이전 단계와 같은 검사·배포 절차를 따른다.

## 구현

### 도구

- `tools/map_lane_edit.py`(새 파일): `LanePatch`는 r66 단면 변형(`map_geometry_edit.Edit` 목록)을 먼저 적용하고, 변형된 형상 위에서 차로 작업을 한다.
  - `add`: 기존 차로를 복사해 새 차로를 만든다. 원본의 계열, 교차로 규칙, 도로명, 신호 그룹을 따른다.
  - `offset`: 차로 중심선을 옆으로 평행 이동한다.
  - `blend`: 차로 시작이나 끝을 한 점으로 옮기고, 이동량을 지정한 길이에 걸쳐 줄인다.
  - `connector`: 교차로 연결 차로를 만든다(편집기 `Connect`와 같은 3차 곡선).
  - `exits`: 다음 차로 목록을 바꾼다.
  - `disable`: 차로를 끈다. 그 차로에서 시작하거나 끝나는 연결 차로도 함께 끈다.
  - `copy_mark` / `remove_mark`: 도색 시설을 옆으로 복사하거나 지운다.
  - 차로를 옮기거나 새로 만들면 `road:polish:<id>` 띠도 함께 옮기거나 복사한다.
  - `check`: 항공사진 위에 결과를 그린다. 새 차로는 하늘색, 끈 차로는 빨간색, 바뀐 도색은 노란 사각형이다.
- `tools/map_geometry_edit.py`: `Edit(skip=...)`을 추가했다. 지정한 차로·도로 띠·시설은 그 변형을 받지 않는다.
- `MapReviewGeometryEdit.cs`(같은 패치 파일로 적용): 기존 형상 설정 다음에 아래 작업을 처리한다.
  - `disableLanes`: 편집기의 `DeleteLane`을 쓴다. 그 차로에 묶인 시설과 신호 그룹도 함께 정리된다.
  - `nexts`: 다음 차로 목록을 교체한다.
  - `addLanes`: 원본 차로를 복사하고, 원본이 속한 신호 그룹에 함께 넣는다.
  - `addFacilities` / `removeFacilities`: 시설을 복사하거나 지운다.
  - 차로 점 개수가 바뀌는 것을 허용한다. 높이 값은 보간한다.
  - 노드 연결 규칙: 다음 차로에 다른 진입 차로가 없으면, 그 차로의 시작 노드가 이 차로의 끝 노드를 따른다. 다른 진입이 있으면 이 차로가 그 노드에 맞춘다. 검사기가 끝점 단절을 노드 단위로 본다.
  - 적용 후 확인:
    - 활성 차로의 다음 차로가 모두 존재하고 켜져 있는지.
    - 원래 출구가 있던 차로가 출구를 모두 잃지 않았는지.
    - 그다음 `ProjectMapPackage.Create` 검사기를 그대로 통과하는지.

### 첫 적용: J063-1·J066-1 남북 간선 남행 4차로 (`tools/map_review_r68_plan.py` → `config/maps/review/r68-lanes.json`)

- **대상:** J7917(z 238.5)과 G04 구간 시작(z 184) 사이 간선이다.
  - 게임: 남행 3차로, 북행 4차로, 차로 간격 3.8 m.
  - 항공사진: 같은 연석 사이(x 776 / 803)에 남행 4차로, 북행 4차로가 약 3.25 m 간격으로 있다.
  - 연석은 차로와 평행하다(기준선에서 −9.52 m). 항공사진의 연석과 맞으므로 옮기지 않았다.
- **단면 재배치(단면 변형):**
  - 남행 3차로와 중앙선을 동쪽으로 옮겼다.
  - 북행 4차로 간격을 좁혔다. 바깥 차로 위치는 그대로 두었다.
  - 차로 폭은 3.0 m다.
  - 북쪽은 교차로 안쪽으로 10 m에 걸쳐 원래 위치로 돌아간다. 남쪽은 북행 차로와 도색만 15 m에 걸쳐 돌아가고, G04 남행 차로는 변형에서 뺐다.
- **새 차로 `review:r68:J7917:SB:kerb`:**
  - 옮긴 바깥 차로에서 3.25 m 서쪽에 두었다. 연석까지 0.2 m 여유가 있다.
  - J7917 서쪽 팔 우회전이 새 차로로도 들어온다(`review:r68:J7917:right-into-kerb`, 기존 우회전 연결의 복사).
- **G04 구간과 1:1 연결:**
  - 남행 1~4차로가 각각 좌회전, 직진 1, 직진 2, 우회전 차로로 이어진다. 이전에는 안쪽 차로 하나가 좌회전과 직진 1로 함께 이어졌다.
  - G04 차로들의 시작점을 새 차로 끝으로 옮기고, 15 m에 걸쳐 원래 위치로 돌아가게 했다.
- **도색:**
  - 새 차로에 보호구역 적색 포장 4조각을 복사했다(z 216~236).
  - r47 검토 점선(z 184~207, 도색 교체 영역 안)에 바깥 차로 경계 점선 4조각을 추가했다.
  - z 169~184의 r47 점선 6조각은 지우고, 옮긴 G04 차로 사이에 6조각을 다시 그렸다.
  - z 207 북쪽은 차로에서 자동으로 칠해지므로 새 차로 점선이 자동으로 생긴다.

## 변경 파일

- 도구:
  - `tools/map_lane_edit.py`(새 파일)
  - `tools/map_review_r68_plan.py`(새 파일)
  - `tools/map_geometry_edit.py`(skip)
  - `tools/map_review_decisions.py`(J063-1·J066-1 처리)
- Unity:
  - `unity/EungyeTaxi/Assets/Editor/MapReviewGeometryEdit.cs`
  - `BusinessProgress.cs`·`JunctionRouteFeedbackTests.cs`(r68 해시, `map_review_bump.py`)
- 지도:
  - `config/maps/review/r68-lanes.json`
  - `config/maps/project/eungye.project.json`
  - `config/maps/local-input-manifest.json`
  - `config/maps/review/2026-11-25-map-review-decisions.json`
- 화면: `docs/ui/2026-11-26-map-review-lane-count/` (019 J7917, 063 J7876 전후)
- 배포: `builds/EungyeTaxi/`, `distribution/EungyeTaxi-build-manifest.json`

## 검증

- **Unity 적용:** `MAP_REVIEW_GEOMETRY ... addedLanes=2 saved=512f42e0…de801c`.
  - 첫 시도에서는 연석을 기준선에서 −9.8 m로 잘못 잡아, 인도 5조각이 새 차로를 침범했다. −9.52 m로 고친 뒤에는 G04 차로 노드 단절이 나와서 노드 연결 규칙을 추가하고 통과했다.
- **`PrototypeBuild.BuildHybrid`:** 성공.
- **`tools/map_review_regress.sh`(단독 실행):**
  - 1회차: 늦은 적신호 1(J1927, 속도 0·redFor −1. r66 때와 같은 형태이고, 이번 수정과 먼 곳이다).
  - 2회차: 적신호 진입·늦은 진입·멈춤·겹침 모두 0, 차로 안 바닥 기둥 0(348개).
  - 교통 검사: 이전과 같이 실패한다(멈춤 3 — J7801 2건, J6199 1건 — 와 녹색 대기열 3건).
- **`-contentTest -callBoardCheck`:** 통과 113, 실패 0.
- **게임 위에서 본 화면(J7917·J7876 전후):** 남행 점선 3줄(4차로)과 북행 점선 3줄(4차로)이 보인다. 새 차로에도 적색 포장과 "30" 표시가 이어진다. 중앙선은 z 170~185에서 이전처럼 완만하게 굽는다.
- **확인하지 못한 것:** 사람이 직접 주행해 보지 않았다. AI 차량이 새 차로를 실제로 쓰는지는 따로 관찰하지 않았다(그래프 연결과 검사기만 확인).

## 남은 일

- 처리 153/277. 할 일 101건(C 35, W 19, L 16, G 10, M 9, N 7, X 5).
- 같은 도구로 할 차로 수 항목:
  - J075-1(서행 5~6→4), J077-1·J022-5(수인로 서행 5→3, 지도 경계 근처), J017-1(바깥 차로 줄이기).
  - J021-3·J022-1(차로 폭 재배치), J008-5, J056-1(차로 하나 삭제).
- 차로를 끄는 경우(`disable`)는 아직 실제 지도에 적용해 보지 않았다.
- NGII 원본 차로(status `ngii`)는 도색이 원본 노면표시라서, 차로를 바꾸면 도색 교체 영역(mask)과 새 선을 함께 넣어야 한다.

## 실행·재현

```
PYTHONPATH=artifacts/py314-deps python tools/map_review_r68_plan.py --check <dir>
Unity -batchmode -quit -projectPath unity/EungyeTaxi -executeMethod MapReviewGeometryEdit.ApplyFromCommandLine -geometryPatch config/maps/review/r68-lanes.json
python tools/map_review_bump.py 68 "<설명>"
```
