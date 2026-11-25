# 2026-11-26 지도 검토 9단계: 로드뷰로 확인한 어린이보호구역 노면 (지도 r63)

## 요구

- 사용자: "계속 진행해 줘"(묶음 사이에 묻지 않고 이어서). r62 작업 중 로드뷰로 확인한 어린이보호구역 노면 11건을 게임에 반영한다.

## 확인(로드뷰 2026-06, 일부 2024-01)

| 항목 | 로드뷰에서 본 것 |
|---|---|
| J029-3 | 은행로 남쪽 좁은 길에 노란 횡단보도와 적색 보호구역 포장 |
| J033-1, J035-1 | 은계도서관 앞 길의 황색 줄무늬는 과속방지턱이 아니라 노란 횡단보도(대기 공간 노란 포장 포함), 옆 골목 입구에도 노란 횡단보도 |
| J037-2 | 교차부 둘레 적색 보호구역 포장, 서쪽 길 노란 횡단보도 |
| J041-2 | 마름모 예고표시는 있음. 횡단보도 색은 이 사진(2024-01)에서 흰색 → 이번에 바꾸지 않음 |
| J048-3 | 남동 길을 가로지르는 노랑·흰 줄 횡단보도, 그 뒤 '어린이보호구역' 글자와 '30' |
| J065-4, J070-2 | 은빛초 옆 동서 길에 적색 포장과 노란 횡단보도 |
| J067-4, J071-2, J073-1 | 골목 교차로 입구마다 적색 고원식 포장(흰 삼각형) |

## 바꾼 것(지도 r62 → r63, 추가 24)

위치는 로드뷰로 있음을 확인한 뒤, 격자를 그린 카카오 항공사진에서 세계 좌표로 읽었다(시트의 픽셀 숫자는 믿을 수 없음). 폭은 지나가는 게임 차로를 덮도록 맞췄다(`map_review_marks.crossing`).

| 종류 | 개수 | 자리 |
|---|---|---|
| 노란 횡단보도 | 8 | 은빛초 길 2·J8761 골목 입구 1, 도서관 길 본선 2·서쪽 옆길 2, J3063 남동 가지 1(교차로 입구) |
| 적색 고원식 포장(깊이 3 m) | 9 | 골목 교차로 J8385·J8762·J9150 둘레 8, J8314 북쪽 골목 입구 1. 흰 삼각형은 그리지 않음 |
| 적색 포장(J037) | 4 | 서쪽·남쪽 횡단보도 바로 바깥(항공사진에서 적색이 있는 쪽. 북쪽은 흰 횡단보도라 넣지 않음) |
| 노면 글자 | 3 | J048 '어린이보호구역'·'30', J1310 서쪽 '어린이보호구역' |

- 계획 `PYTHONPATH=artifacts/py314-deps python tools/map_review_r63_plan.py --check <폴더>` → `config/maps/review/r63-school-marks.json`. `--check`는 항공사진 위에 계획을 그린다. 이 그림으로 J048 횡단보도(교차로 입구로 당김), J8761 골목 입구(2 m 남쪽), J037 포장(북쪽 → 서·남쪽)을 고쳤다.
- 적용 `MapReviewCrosswalkEdits.ApplyR63`(r62에서만). r63 해시 `40c0ad75…d4d1`.
- 모두 노면 도색이다. 신호·정지선·차로는 바꾸지 않았다.

## 검증

| 검사 | 종류 | 결과 |
|---|---|---|
| `MapReviewCrosswalkEdits.ApplyR63` | 지도 편집 | 추가 24, 바꿈 0. 저장 후 해시 재확인 |
| `PrototypeBuild.BuildHybrid` | 빌드 | 성공 |
| 계획 `--check`(항공사진 위 계획) | 위치 제어(그림) | 10장 확인, 3곳 고친 뒤 적용 |
| `-mapAudit -mapAuditSettle 1 -mapAuditObserve 1`(77곳) | 위치 제어 | 적신호 진입 0, 멈춤 0, 겹침 0 |
| `-mapAuditDump` + `tools/map_poles_in_lanes.py` | 위치 제어 | 바닥 기둥 358개, 차로 안 0 |
| `-callBoardCheck` | 위치 제어(승객 호출판) | 통과 114, 실패 0 |
| `-districtTrafficTest` | 자동 물리 주행 | 실패. 멈춘 차량 3(J7801 유턴 2·J6199 유턴 1, 이전과 같음), 녹색 대기 줄 3 |

게임 화면 캡처는 하지 않았다. 같은 시설 종류(노란 횡단보도·적색 포장·노면 글자)는 r55·r56에서 화면으로 확인했다. 사람 플레이와 전체 캠페인 검사도 하지 않았다.

## 로드뷰로 닫은 항목(변경 없음)

| 항목 | 근거 |
|---|---|
| J068-2, J072-2 | 은계호수로는 노란 중앙선에 편도 1차로(넓은 차로)다. 2차로로 나누지 않음 |
| J073-2 | J9150 남쪽 골목에 노란 횡단보도가 실제로 있어 게임 것을 유지 |
| J024-3 | 사용자 답("횡단보도 그쯤에 있을듯")대로 게임 E4 횡단보도 유지 |

차로 형상 작업으로 넘긴 확인: J062-2(진입부 편도 2차로 확인), J040-3(대은로 편도 3차로로 보임), J059-3(수인로 서행 4차로로 보임). 판단 보류: J061-3, J063-5. 처리 133/277.

## 변경 파일

- 지도: `config/maps/project/eungye.project.json`(r63), `config/maps/local-input-manifest.json`, `config/maps/review/r63-school-marks.json`, `config/maps/review/2026-11-25-map-review-decisions.json`, `config/maps/review/2026-11-26-roadview-verdicts.json`(50건)
- 코드: `Assets/Editor/MapReviewCrosswalkEdits.cs`(ApplyR63), `Assets/Scripts/BusinessProgress.cs`(r63 해시), `Assets/Editor/JunctionRouteFeedbackTests.cs`
- 도구: `tools/map_review_r63_plan.py`(새로), `tools/map_review_decisions.py`
- 문서: 이 기록, `LATEST.md`, `CURRENT-STATE.md`

## 재현

```
PYTHONPATH=artifacts/py314-deps python tools/map_review_r63_plan.py --check <폴더>
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod MapReviewCrosswalkEdits.ApplyR63   (r62에서만)
python tools/map_review_bump.py 63 "<설명>"
Unity.exe ... -executeMethod PrototypeBuild.BuildHybrid
bash tools/map_review_regress.sh builds/EungyeTaxi/EungyeTaxi.exe <out>
builds/EungyeTaxi/EungyeTaxi.exe -contentTest -callBoardCheck -callBoardOutput <dir>
```
