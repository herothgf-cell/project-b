# 2026-11-26 지도 검토 14단계: 항공사진 횡단보도 위치 (지도 r73)

## 요구사항

- 사용자: "계속 진행". 남은 폭 항목 가운데 시설 편집으로 끝나는 횡단보도 항목을 처리한다(J007-4, J032-2, J040-1).

## 구현 (`tools/map_review_r73_plan.py` → `config/maps/review/r73-crosswalks.json`)

- **위치 읽기:** 게임 횡단보도를 격자 항공사진 위에 그려 놓고 위치를 읽었다(`$TEMP/lt/cwdraw.py` 방식).
- **J007-4 (J4396 북서):**
  - 게임 횡단보도가 줄무늬보다 약 3.6 m 바깥에 있었고, 길이 38.6 m로 북쪽 끝이 약 7 m 더 나가 있었다.
  - 항공사진 줄무늬 위치로 옮겼다(중심 x 430.4 z 1356.6, 길이 23 m).
  - 같은 교차로의 나머지 3개도 똑같이 바깥에 있지만, 시트에 없어서 그대로 두었다.
- **J040-1 (J2225 대은로, r60 보행 거리 남쪽 끝):**
  - 항공사진에는 차도 전체를 가로지르는 넓은 횡단보도 하나가 있다. 차도 방향 22.5 m, 도로 방향 깊이 11.5 m, 중심 x 220.7 z 1201.85, 도로 방위 50°.
  - r64 중간 횡단보도와 같은 방식(교차로 지정 없음, 흰색)으로 추가했다.
  - 바로 서쪽에 OSM 횡단보도 노드 12279718021의 좁은 줄무늬가 남았다. 11월 20일 작업에서 대은로 연석 사이로 옮겨 둔 것이고, 항공사진에서는 자전거 횡단 띠 자리다.
  - 이 줄무늬를 가리려고 그 띠 위에 "없음" 유형 횡단보도(`review:r73:J040:crosswalk:002`)를 두었다.
- **J032-2:** 시트 좌표를 게임 위치로 확실히 풀 수 없었다(같은 넓은 횡단보도로 보이지만 확인 못 함). 열어 두었다.
- **도구·코드:**
  - `MapReviewGeometryEdit.FacilityAdd.type`: 복사한 시설의 유형을 바꿀 수 있다.
  - `MapSupplements.DrawCrossings`: 지정 횡단보도(숨김 유형 포함) 영역 안의 OSM 횡단보도는 그리지 않는다. 영향을 받는 곳은 위 J2225와 r64 J046 중간 횡단보도뿐이다(겹쳐 그려지던 OSM 줄무늬).

## 변경 파일

- 도구: `tools/map_review_r73_plan.py`(새 파일), `tools/map_lane_edit.py`(설명), `tools/map_review_decisions.py`
- Unity: `unity/EungyeTaxi/Assets/Editor/MapReviewGeometryEdit.cs`, `unity/EungyeTaxi/Assets/Scripts/MapSupplements.cs`, `BusinessProgress.cs`·`JunctionRouteFeedbackTests.cs`(r73 해시)
- 지도: `config/maps/review/r73-crosswalks.json`, `config/maps/project/eungye.project.json`, `config/maps/local-input-manifest.json`, 결정 JSON
- 화면: `docs/ui/2026-11-26-map-review-crosswalks/`
- 배포: 이번에는 없음(아래 검증 참고)

## 검증

- **적용:** `MAP_REVIEW_GEOMETRY ... facilities=1`, 시설 2개 추가, `saved=98a69914…59da`.
- **시험 빌드 `artifacts/r73-trial`(OSM 규칙 포함):**
  - 77곳 감사: 적신호 진입·늦은 진입·멈춤·겹침 0, 차로 안 바닥 기둥 0(354개).
  - 교통 검사는 이전과 같이 실패한다(멈춤 3).
  - 호출판: 통과 115, 실패 0.
  - OSM 횡단보도 줄무늬는 297개에서 257개로 줄었다(두 곳).
- **전후 화면:** [007-J4396_13458.jpg](../ui/2026-11-26-map-review-crosswalks/007-J4396_13458.jpg), [040-J2225_12001.jpg](../ui/2026-11-26-map-review-crosswalks/040-J2225_12001.jpg).
  - J4396 북서 횡단보도가 안쪽으로 짧아졌다. 정지선과 붉은 포장은 원래 자리에 남아 횡단보도 바깥에 있다.
  - J2225에는 넓은 횡단보도 하나만 보인다.
- **배포 실행본:** `PrototypeBuild.BuildHybrid`(14:43:56 시작)는 성공했고 실행본 호출판도 통과 115였다. 그런데 빌드 도중(14:44:04~14:45:16) 다른 세션이 작업 중인 스크립트(CockpitUI, PassengerDialogueUI 등 7개)를 저장했고, 실행본용 컴파일은 14:44:44에 끝났다. 미커밋 코드가 섞였을 수 있어 이 빌드는 버리고 `builds/`를 r72 배포본으로 되돌렸다. r73 배포 실행본은 그 세션이 커밋한 뒤에 만든다.
- **확인하지 못한 것:**
  - 사람이 직접 주행하지 않았다.
  - J2225는 신호 없는 교차로이고, 새 횡단보도에서 차가 멈추는지는 따로 관찰하지 않았다(r64 중간 횡단보도와 같은 방식).

## 남은 일

- 처리 163/277.
- J032-2: 위치 확인.
- J4396의 나머지 횡단보도 3개도 바깥에 있다(시트 밖, 사용자 확인 필요).

## 실행·재현

```
PYTHONPATH=artifacts/py314-deps python tools/map_review_r73_plan.py
Unity -batchmode -quit -projectPath unity/EungyeTaxi -executeMethod MapReviewGeometryEdit.ApplyFromCommandLine -geometryPatch config/maps/review/r73-crosswalks.json
```
