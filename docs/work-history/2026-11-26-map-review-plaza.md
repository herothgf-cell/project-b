# 2026-11-26 지도 검토 7단계: 은행동 남쪽 보행자 거리를 보행 광장으로 (지도 r60)

## 요구

- 사용자: "계속 진행해 줘". 로드뷰로 확인한 은행동 보행자 거리([로드뷰 확인 기록](2026-11-26-map-review-roadview-survey.md))를 게임에 반영한다.

## 바꾼 것(지도 r59 → r60)

| 대상 | 내용 |
|---|---|
| 차로 38개 끔 | 대은로(J2225)–J2468–J2640 남북 보행 거리의 양방향 차로 4개, J2468에서 갈라지는 동서 골목 차로 4개(J2111·J2830 쪽 끝까지), 그 차로로 들어가거나 나오는 연결 차로 30개 |
| 도로 포장 8개 끔 | 장면 `road` 1107·1132·2501·3693(거리·골목)과 J2468 교차부 조각 1084·3830·1141·4129 |
| 보행 광장 8개 추가 | 같은 자리에 `sidewalk` 다각형 "검토 보행 공간"(높이 0.14 m). 끝은 남는 차도(대은로·J2640·J2111·J2830)에서 0.3 m 떨어지도록 잘랐다 |

- 해당 항목: J001-3, J038-2·3, J040-2(대은로 남쪽), J043-1·2, J045-1, J046-2, J047-2(남쪽 거리). 북쪽 보행 십자(J1897: J005-3, J032-1, J036-1·2, J039-1·2)는 북서 가지를 아직 보지 못해 다음에 한다.
- 계획 `tools/map_review_plaza_plan.py south` → `config/maps/review/r60-plaza-lanes.json`. 씨앗 차로에서 시작해 그 차로에서 시작하거나 끝나는 연결 차로를 반복해서 함께 끈다. 남는 차로 가운데 출구가 모두 꺼지는 차로(막다른 길)가 하나라도 생기면 멈춘다. 이번에는 0개였다.
- 적용 `MapReviewPlazaEdit.Apply`(r59에서만). 남는 차로의 `next`에서 꺼진 차로를 빼고, 출구가 0이 되면 멈춘다.
- 신호 그룹·시설·승하차 지점 중 꺼진 차로를 쓰는 것은 없었다.
- 저장 이전: r33 이후 판으로 옮길 때는 길찾기 색인을 다시 잡으므로(`ApplyReviewedEungyeCheckpointMigration`) 해시만 등록했다. r60 해시 `7adfe9e5…09b9`.
- 장면 포장 조각 3374·1144(골목 끝)와 1115·1129(대은로·J2640 연결)는 남는 교차로·차도 안에 있어 그대로 두었다.

전후 화면(게임 위 시점, 왼쪽 11/24 · 오른쪽 r60): [before-after-top.jpg](../ui/2026-11-26-map-review-plaza/before-after-top.jpg). 교차로 모서리의 둥근 포장 조각(남는 교차로 다듬기)이 광장 끝에 조금 남는다.

## 검증

| 검사 | 종류 | 결과 |
|---|---|---|
| `MapReviewPlazaEdit.Apply` | 지도 편집 | 차로 38, 포장 8, 광장 8. 검사기 통과(광장 폭 0은 거부돼 기본 폭 4로 둠), 저장 후 해시 재확인 |
| `PrototypeBuild.BuildHybrid`(편집기 검사, 경로 회귀 포함) | 빌드 | 성공 |
| J2225·J2468·J2640 위 시점 화면 전후 | 위치 제어(화면) | 거리·골목이 광장 포장으로 바뀜 |
| `-mapAudit -mapAuditSettle 1 -mapAuditObserve 1`(77곳) | 위치 제어 | 적신호 진입 0, 멈춤 0, 겹침 0 |
| `-mapAuditDump` + `tools/map_poles_in_lanes.py` | 위치 제어 | 바닥 기둥 358개, 차로 안 0 |
| `-callBoardCheck` | 위치 제어(승객 호출판) | 통과 115, 실패 0 |
| `-districtTrafficTest` | 자동 물리 주행 | 실패. 멈춘 차량 6 → **3**(J3516 3대가 사라지고 J7801 유턴 2·J6199 유턴 1만 남음), 녹색 대기 줄 3 |

사람 플레이와 전체 캠페인 검사는 하지 않았다.

## 변경 파일

- 지도: `config/maps/project/eungye.project.json`(r60), `config/maps/local-input-manifest.json`, `config/maps/review/r60-plaza-lanes.json`
- 코드: `Assets/Editor/MapReviewPlazaEdit.cs`(새로), `Assets/Scripts/BusinessProgress.cs`(r60 해시), `Assets/Editor/JunctionRouteFeedbackTests.cs`
- 도구: `tools/map_review_plaza_plan.py`(새로)
- 문서: 이 기록, `LATEST.md`, `CURRENT-STATE.md`, `prototype-development-checklist.md`, `docs/ui/2026-11-26-map-review-plaza/before-after-top.jpg`

## 재현

```
python tools/map_review_plaza_plan.py south
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod MapReviewPlazaEdit.Apply   (r59에서만)
python tools/map_review_bump.py 60 "<설명>"
Unity.exe ... -executeMethod PrototypeBuild.BuildHybrid
bash tools/map_review_regress.sh builds/EungyeTaxi/EungyeTaxi.exe <out>
builds/EungyeTaxi/EungyeTaxi.exe -contentTest -callBoardCheck -callBoardOutput <dir>
```
