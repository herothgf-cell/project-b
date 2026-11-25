# 2026-11-26 지도 검토 8단계: 로드뷰로 확인한 노란 횡단보도 (지도 r62)

## 요구

- 사용자: "계속 진행해 줘"(묶음 사이에 묻지 않고 이어서). 로드뷰로 확인한 항목 중 노면만 바꾸면 되는 것을 반영한다([로드뷰 확인 기록](2026-11-26-map-review-roadview-survey.md)).

## 바꾼 것(지도 r61 → r62)

| 항목 | 대상 | 내용 |
|---|---|---|
| J044-2, J044-3 | J2608_13908(신호 없는 세 갈래) | 게임에 횡단보도가 없었다. 로드뷰에서 본 노란 횡단보도를 세 가지 모두에 놓음(폭 10.6·9.6·10.6 m, 깊이 4 m). 놓는 방법은 r56 교차로 횡단보도와 같음(`map_review_marks.arms`/`arm_crossing`) |
| J005-2 | J2564_12237 | 생성 횡단보도 a1 s+1(북서)·a1 s-1(남동)을 같은 자리 황색 문서 횡단보도로 바꿈 |

- 계획 `python tools/map_review_r62_plan.py <r61 덤프>` → `config/maps/review/r62-yellow-crossings.json`(추가 5).
- 적용 `MapReviewCrosswalkEdits.ApplyR62`(r61에서만). r62 해시 `54c6f40a…ecb5`.
- J044 교차로는 신호 없는 곳이라 횡단보도는 노면 도색만이다(보행 신호·정지선 없음).

## 검증

| 검사 | 종류 | 결과 |
|---|---|---|
| `MapReviewCrosswalkEdits.ApplyR62` | 지도 편집 | 추가 5, 바꿈 0. 저장 후 해시 재확인 |
| `PrototypeBuild.BuildHybrid` | 빌드 | 성공 |
| `-mapAudit -mapAuditSettle 1 -mapAuditObserve 1`(77곳) | 위치 제어 | 적신호 진입 0, 멈춤 0, 겹침 0 |
| `-mapAuditDump` + `tools/map_poles_in_lanes.py` | 위치 제어 | 바닥 기둥 356개, 차로 안 0 |
| `-callBoardCheck` | 위치 제어(승객 호출판) | 통과 111, 실패 0(r61의 114와 차이는 뽑힌 호출 종류 구성) |
| `-districtTrafficTest` | 자동 물리 주행 | 실패. 멈춘 차량 3(J7801 유턴 2·J6199 유턴 1, r60·r61과 같음), 녹색 대기 줄 3 |

게임 화면 전후 캡처는 이번에 하지 않았다. 위치는 계획 단계에서 항공사진·게임 차로와 겹쳐 확인했다. 사람 플레이와 전체 캠페인 검사도 하지 않았다.

## 로드뷰 확인 추가(이번 묶음 중)

어린이보호구역 노면 항목 11건을 더 봤다(J029-3, J033-1, J035-1, J037-2, J041-2(일부), J048-3, J065-4, J067-4, J070-2, J071-2, J073-1). 확인 결과는 `config/maps/review/2026-11-26-roadview-verdicts.json`(40건)에 있다. 반영은 다음 묶음(r63)에서 한다.

## 변경 파일

- 지도: `config/maps/project/eungye.project.json`(r62), `config/maps/local-input-manifest.json`, `config/maps/review/r62-yellow-crossings.json`, `config/maps/review/2026-11-25-map-review-decisions.json`(처리 119건), `config/maps/review/2026-11-26-roadview-verdicts.json`
- 코드: `Assets/Editor/MapReviewCrosswalkEdits.cs`(ApplyR62), `Assets/Scripts/BusinessProgress.cs`(r62 해시), `Assets/Editor/JunctionRouteFeedbackTests.cs`
- 도구: `tools/map_review_r62_plan.py`(새로), `tools/map_review_decisions.py`
- 문서: 이 기록, `LATEST.md`, `CURRENT-STATE.md`

## 재현

```
python tools/map_review_r62_plan.py <r61 덤프 map-audit.txt>
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod MapReviewCrosswalkEdits.ApplyR62   (r61에서만)
python tools/map_review_bump.py 62 "<설명>"
Unity.exe ... -executeMethod PrototypeBuild.BuildHybrid
bash tools/map_review_regress.sh builds/EungyeTaxi/EungyeTaxi.exe <out>
builds/EungyeTaxi/EungyeTaxi.exe -contentTest -callBoardCheck -callBoardOutput <dir>
```
