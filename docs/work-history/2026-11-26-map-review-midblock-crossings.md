# 2026-11-26 지도 검토 10단계: 항공사진에 분명한 중간 횡단보도 (지도 r64)

## 요구

- 사용자: "계속 진행해 줘"(묶음 사이에 묻지 않고 이어서). 남은 노면 항목 중 항공사진만으로 자리가 분명한 횡단보도를 넣는다.

## 바꾼 것(지도 r63 → r64, 추가 5)

| 항목 | 자리 | 내용 |
|---|---|---|
| J002-2 | J1919 동쪽 약 45 m(237, 88) | 적색 포장(깊이 6 m) 위 노란 횡단보도 |
| J009-2 | J9 회전교차로 서쪽(452, 88)·동쪽(559, 87) 진입로 | 노란 횡단보도(항공사진의 황토색 횡단보도) |
| J046-4 | J046 옆 교차로의 남북 길(283, 1174) | 흰 횡단보도, 남북 길 방향으로 가로지름 |

- 게임에는 이 자리 25 m 안에 문서·생성 횡단보도가 없었다(r62 덤프로 확인).
- 계획 `PYTHONPATH=artifacts/py314-deps python tools/map_review_r64_plan.py --check <폴더>` → `config/maps/review/r64-midblock-crossings.json`. 항공사진 위 계획 그림으로 J046을 실제 자리로 옮기고, 가까운 차로가 옆길이 되지 않게 방향을 남북 길로 정했다.
- 로드뷰: J009-2는 회전교차로 안 시점이라 50 m 밖 횡단보도가 보이지 않았다(판정 "일부"). J002-2·J046-4는 항공사진으로만 정했다(판정 "항공").
- 적용 `MapReviewCrosswalkEdits.ApplyR64`(r63에서만). r64 해시 `c37a1288…ec32`.

## 검증

| 검사 | 종류 | 결과 |
|---|---|---|
| `MapReviewCrosswalkEdits.ApplyR64` | 지도 편집 | 추가 5, 바꿈 0. 저장 후 해시 재확인 |
| `PrototypeBuild.BuildHybrid` | 빌드 | 성공 |
| `-mapAudit -mapAuditSettle 1 -mapAuditObserve 1`(77곳) | 위치 제어 | 적신호 진입 0, 멈춤 0, 겹침 0 |
| `-mapAuditDump` + `tools/map_poles_in_lanes.py` | 위치 제어 | 바닥 기둥 352개, 차로 안 0 |
| `-callBoardCheck` | 위치 제어(승객 호출판) | 통과 116, 실패 0 |
| `-districtTrafficTest` | 자동 물리 주행 | 실패. 멈춘 차량 3(J7801 유턴 2·J6199 유턴 1, 이전과 같음), 녹색 대기 줄 3 |

게임 화면 캡처, 사람 플레이, 전체 캠페인 검사는 하지 않았다.

## 같이 닫은 항목(변경 없음)

| 항목 | 근거 |
|---|---|
| J003-3 | 격자 항공사진에서 동쪽 진입부 중앙의 황색 빗금이 실제로 보여 게임 것을 유지 |
| J072-3 | 로드뷰: 북쪽 진입로는 건물 사이 주차장 출입 통로 |
| J037-4 | 로드뷰: 동쪽 포장 통로는 은행동 행정복지센터 주차장 입구 |
| J046-3 | 로드뷰(2024-01): 은행로55번길은 중앙선 있는 양방향 차도(약 9 m) |

로드뷰로 확인만 하고 차로 형상 작업에 넘긴 것: J048-2(볼라드 있는 약 6 m 상가 골목), J050-3(차도 약 9~10 m), J025-3·J004-3(적색 포장, 다음 r65). 로드뷰 판정 60건.

## 변경 파일

- 지도: `config/maps/project/eungye.project.json`(r64), `config/maps/local-input-manifest.json`, `config/maps/review/r64-midblock-crossings.json`, `config/maps/review/2026-11-25-map-review-decisions.json`, `config/maps/review/2026-11-26-roadview-verdicts.json`
- 코드: `Assets/Editor/MapReviewCrosswalkEdits.cs`(ApplyR64), `Assets/Scripts/BusinessProgress.cs`(r64 해시), `Assets/Editor/JunctionRouteFeedbackTests.cs`
- 도구: `tools/map_review_r64_plan.py`(새로), `tools/map_review_decisions.py`
- 문서: 이 기록, `LATEST.md`, `CURRENT-STATE.md`

## 재현

```
PYTHONPATH=artifacts/py314-deps python tools/map_review_r64_plan.py --check <폴더>
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod MapReviewCrosswalkEdits.ApplyR64   (r63에서만)
python tools/map_review_bump.py 64 "<설명>"
Unity.exe ... -executeMethod PrototypeBuild.BuildHybrid
bash tools/map_review_regress.sh builds/EungyeTaxi/EungyeTaxi.exe <out>
builds/EungyeTaxi/EungyeTaxi.exe -contentTest -callBoardCheck -callBoardOutput <dir>
```
