# 2026-11-26 지도 검토 12단계: 차로 형상 편집 도구와 첫 폭 조정 (지도 r66)

## 요구

- 사용자: "도구부터 진행". 남은 지도 검토 항목은 대부분 차도 폭·연석·차로 위치를 고치는 일이라, 차로·도로면·인도·노면 표시를 함께 옮기는 편집 도구를 먼저 만든다.

## 만든 도구

| 파일 | 역할 |
|---|---|
| `tools/map_geometry_edit.py`(새로) | 도로 한 구간의 단면을 바꾸는 계획 도구. 기준선(도로를 따라가는 점들), 구간(s0~s1), 옆 방향 이동표(옛 위치 → 새 위치)로 평면 변형을 정의하고, 같은 변형을 모든 차로 점, 장면 도로 띠(중심·폭, 폭이 달라지면 구간별로 나눔), 인도 다각형 꼭짓점(2 m마다 점을 넣은 뒤), 노면 표시·횡단보도(중심·폭·길이)에 적용한다. 구간 양 끝은 부드럽게(smoothstep) 줄이며, 끝마다 길이를 따로 줄 수 있다. 가장 바깥 이동점보다 `fade`(기본 1.5 m) 넘게 떨어진 점은 움직이지 않으므로 인도 바깥 끝은 그대로 있고 인도가 넓어지거나 좁아진다. 차로 폭은 옮겨진 점 가운데 가장 좁아지는 곳의 비율을 따르되 최소 3.0 m. 가는 선(폭 0.5 m 미만)만 차로를 따라 방향을 돌리고, 횡단보도·포장·글자는 방향을 유지한다. 차로에 묶인 표시(`lanePresentation`)는 차로 길이가 바뀐 비율만큼 범위를 줄인다. `check()`는 카카오 항공사진 위에 전(얇게)·후(굵게)를 그린다(Git 제외 확인 그림). |
| `Assets/Editor/MapReviewGeometryEdit.cs`(새로) | 범용 적용기. `-executeMethod MapReviewGeometryEdit.ApplyFromCommandLine -geometryPatch <json>`. 계획 JSON의 차로 점·폭, 도로 띠·인도 점, 나눈 도로 조각(원래 객체 복사), 노면 표시 좌표를 그대로 넣고 지도 패키지 검사기(인도의 차로 침범, 건물 침범 등)를 통과해야 저장한다. |

검사기 메모: 장면 오류(예: 인도가 차로를 침범)가 하나라도 있으면 검사기는 장면을 빼고 원본 지도로 차로를 다시 검사하므로, 편집 범위 밖의 차로 오류(건물 침범 등)가 함께 나온다. 먼저 장면 오류만 고치면 된다.

## 바꾼 것(지도 r65 → r66, `tools/map_review_r66_plan.py` → `config/maps/review/r66-cross-sections.json`)

연석 위치는 2 m 격자를 얹은 카카오 항공사진으로 읽었고, 시트의 m 추정은 방향과 대략의 크기만 참고했다.

| 항목 | 구간 | 내용 |
|---|---|---|
| J033-2, J037-3(남쪽) | 도서관 길 J1832 북쪽(z 207~221) | 연석 사이 13 m → 약 10.4 m, 양쪽 연석 1.3 m 안으로. 차로는 그대로 |
| J037-3(북쪽) | J1906 북쪽 가지(z 251~278, 북쪽 끝 16 m에 걸쳐 원래대로) | 서쪽 연석 2.3 m 동쪽(볼라드 있는 보도), 두 차로 1.4~2.6 m 동쪽(항공사진의 횡단보도 예고 마름모가 남행 차로 중심). 동쪽 연석은 길이 굽는 곳이라 북행 차로 여유를 위해 유지 |
| J067-3, J071-1 | J8385 동쪽 이면도로(x 840~866, x 883~903) | 연석 사이 9.8~11.9 m → 약 8 m(북측 주차 1열 포함). 교차로 안 모서리는 건드리지 않도록 구간을 교차로 입구 앞에서 끝냈다 |
| J070-1 | 은빛초 앞길(x 838~905) | 남쪽 연석 2.4 m 북쪽(넓은 보도), 연석 사이 9.5 m → 약 7.1 m. 중앙선 유지(로드뷰 미확인) |

- 차로 30개, 장면 객체 195개(도로 띠·인도), 나눈 도로 조각 88개, 노면 표시 15개(차로 표시 범위 1개 포함).
- 저장 이전: r66 해시 `0e4e211b…88e9`(`BusinessProgress`), r25~r66 회귀 범위.
- 처리 147/277. 남은 130건 가운데 할 일 107건: 연결·교차로 모양 35, 폭 23, 선형 16, 노면 10, 선 종류 9, 차로 수 9, 기타 5(나머지는 반려·보류).

전후 화면(게임 위 시점, 왼쪽 r65 · 오른쪽 r66): [docs/ui/2026-11-26-map-review-cross-sections/](../ui/2026-11-26-map-review-cross-sections/). J067·J070·J071의 "전"은 J033 시험 적용만 들어간 검사용 빌드(그 구간은 r65와 같음)로 찍었다.

## 검증

| 검사 | 종류 | 결과 |
|---|---|---|
| `MapReviewGeometryEdit.ApplyFromCommandLine`(r65에서) | 지도 편집 | 검사기 통과, 저장 후 해시 재확인. 중간에 두 번 거부됨: J037 북행 차로가 동쪽 인도 침범(동쪽 연석 유지로 해결), J071 교차로 모서리 인도가 우회전 연결 차로 침범·차로 표시 범위 초과(구간을 교차로 앞에서 끝내고 범위를 비례 조정해 해결) |
| `PrototypeBuild.BuildHybrid` | 빌드 | 성공 |
| J1832·J1906·J8385·J8761·J8762 위 시점 화면 전후 | 위치 제어(화면) | 차도가 좁아지고 인도·연석·차선·횡단보도가 따라옴. 첫 판에서 테이퍼 구간의 적색 포장 조각이 돌아가 고침 |
| `-mapAudit -mapAuditSettle 1 -mapAuditObserve 1`(77곳) | 위치 제어 | 적신호 진입 0, 늦은 진입 0, 멈춤 0, 겹침 0. (첫 판 회귀는 창 캡처와 동시에 돌려 J1927에서 늦은 진입 1이 잡혔고, 단독 재실행에서 0) |
| `-mapAuditDump` + `tools/map_poles_in_lanes.py` | 위치 제어 | 바닥 기둥 354개, 차로 안 0 |
| `-callBoardCheck` | 위치 제어(승객 호출판) | 통과 113, 실패 0 |
| `-districtTrafficTest` | 자동 물리 주행 | 실패. 멈춘 차량 3(J7801 유턴 2·J6199 유턴 1, r60부터 같음), 녹색 대기 줄 3 |

사람 플레이와 전체 캠페인 검사는 하지 않았다.

## 남은 일

- 테이퍼 구간의 흰 가장자리 선(점선 조각)이 조금 들쭉날쭉하다. 선 조각을 다시 잇는 정리는 아직 없다.
- 차로 수 바꾸기(차로 추가·삭제와 교차로 연결)와 새 도로 잇기는 이 도구로 하지 않는다. 다음 도구 작업이다.
- 남은 폭 항목(J023-1, J025-1, J042-1, J054-1, J056-1, J065-3·5, J071-3 등)은 같은 도구로 이어서 측정해 반영한다. 넓은 간선(J008-5, J017-1, J021-3, J022-1)은 차로 수 변경과 함께 해야 한다.

## 변경 파일

- 지도: `config/maps/project/eungye.project.json`(r66), `config/maps/local-input-manifest.json`, `config/maps/review/r66-cross-sections.json`, `config/maps/review/2026-11-25-map-review-decisions.json`
- 코드: `Assets/Editor/MapReviewGeometryEdit.cs`(새로), `Assets/Scripts/BusinessProgress.cs`(r66 해시), `Assets/Editor/JunctionRouteFeedbackTests.cs`
- 도구: `tools/map_geometry_edit.py`(새로), `tools/map_review_r66_plan.py`(새로), `tools/map_review_decisions.py`
- 문서: 이 기록, `LATEST.md`, `CURRENT-STATE.md`, `prototype-development-checklist.md`, `docs/ui/2026-11-26-map-review-cross-sections/`

## 재현

```
PYTHONPATH=artifacts/py314-deps python tools/map_review_r66_plan.py --check <dir>
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod MapReviewGeometryEdit.ApplyFromCommandLine -geometryPatch config/maps/review/r66-cross-sections.json   (r65에서만)
python tools/map_review_bump.py 66 "<설명>"
Unity.exe ... -executeMethod PrototypeBuild.BuildHybrid
bash tools/map_review_regress.sh builds/EungyeTaxi/EungyeTaxi.exe <out>
builds/EungyeTaxi/EungyeTaxi.exe -contentTest -callBoardCheck -callBoardOutput <dir>
위 시점 화면: EungyeTaxi.exe -singlePlayer -contentTest -dayLightDayProbe -mapAudit -mapAuditStart 33 -mapAuditLimit 37 -mapAuditSettle 1 -mapAuditObserve 1 -mapAuditOutput <dir>   (-batchmode 없이 실행해야 그림이 찍힘)
```

## 2026-11-26 추가: 폭 항목 2차 (지도 r67)

- 사용자: "계속 진행". 남은 폭 항목을 같은 도구로 측정했다.
- **반영(r67)** `tools/map_review_r67_plan.py` → `config/maps/review/r67-cross-sections.json`: J065-3 은빛초 앞길 서쪽 구간(x 803~826). r66 J070 구간과 같은 단면으로 남쪽 연석 z 81.35 → 83.0(넓은 보도·자전거길), 두 차로 1.6 m·0.2 m 안쪽, 북쪽 연석 유지. 차로 6, 장면 객체 24, 나눈 조각 13. r67 해시 `abc169d7…70fc`.
- **항공사진으로 보고 바꾸지 않고 닫음**: J025-1(연석 사이 약 14.5 m, 게임 약 15 m), J042-1(동쪽 절반은 건물 그림자 속 차도로 차량이 서 있음, 시트의 "녹지"는 그림자), J071-3(간선 남서쪽 연석 차이 1~3 m).
- **시도했다가 뺀 것**: J054-1(J495 남쪽 가지, 항공사진상 서쪽 보도가 약 2 m 동쪽). 남행 차로가 지도 경계 밖(x < 0)까지 이어져 검사기가 그 차로의 변경을 막고(경계 밖 제어점), 차로가 비스듬해 연석만 1 m 옮겨도 인도가 차로에 닿았다. 지도 경계 밖 차로를 다루는 방법이 정해질 때까지 남긴다.
- **다음 작업으로 넘김**: J023-1(북쪽 띠가 주차 공간인지 차로인지 로드뷰 필요), J056-1(남쪽 가장자리를 올리면 그 띠의 차로 하나를 없애야 해 차로 수 작업), J013-3·4(대형 교차로 가지 위치 재구성).
- 도구 수정: 이전 판에서 나눈 도로 조각 이름(`:geo1`)과 겹치지 않게 빈 번호를 찾는다.
- 처리 151/277(할 일 103: 연결·교차로 모양 35, 폭 19, 선형 16, 노면 10, 선 종류 9, 차로 수 9, 기타 5).

| 검사 | 종류 | 결과 |
|---|---|---|
| `MapReviewGeometryEdit.ApplyFromCommandLine`(r66에서) | 지도 편집 | 검사기 통과(J054 포함안은 경계 밖 제어점·인도 침범으로 거부) |
| `PrototypeBuild.BuildHybrid` | 빌드 | 성공 |
| J8314 위 시점 화면 전후 | 위치 제어(화면) | 서쪽 구간이 r66 동쪽 구간과 같은 폭으로 이어짐. "전"은 r65 기반 검사용 빌드라 동쪽 구간도 넓게 보인다 |
| `-mapAudit`(77곳) | 위치 제어 | 적신호 진입 0, 늦은 진입 0, 멈춤 0, 겹침 0 |
| 바닥 기둥 | 위치 제어 | 349개, 차로 안 0 |
| `-callBoardCheck` | 위치 제어 | 통과 115, 실패 0 |
| `-districtTrafficTest` | 자동 물리 주행 | 실패. 멈춘 차량 3(같음), 녹색 대기 줄 3 |

화면: [065-J8314_858.jpg](../ui/2026-11-26-map-review-cross-sections/065-J8314_858.jpg). 사람 플레이는 하지 않았다.
