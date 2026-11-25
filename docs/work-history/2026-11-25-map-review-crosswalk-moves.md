# 2026-11-25 지도 검토 4단계: 횡단보도 위치 이동·정지선·2열 깊이 (지도 r57)

## 요구

- 사용자: "네, 이어서 진행"(다음 묶음으로 횡단보도 위치 이동). 이전 지시: 수정 요청한 것을 모두 진행하고 푸시.
- 대상: 노면 항목 중 위치 이동(J001·J003·J005·J006·J011·J022·J024·J077), 폭·인도 항목 중 횡단보도 위치·깊이(J004-4, J008-1·2, J018-2, J019-1, J020-1·2, J021-2, J022-4).

## 확인과 판단

- 100 m 대조 화면(카카오 항공사진 옆에 게임 화면, 10 px = 1 m)에 현재 횡단보도·정지선을 그려 실제 위치를 읽었다(`artifacts/map-review-20261124/r57-check/`, Git 제외).
- **이동하면 정지선도 같이 옮긴다.** 횡단보도만 바깥으로 옮기면 차가 횡단보도를 지나서 선다. 정지선은 이동 거리만큼 함께 옮긴다(자전거횡단도가 붙으면 2.3 m 더). 생성 정지선은 같은 교차로·축·방향의 문서 정지선이 되고, 교통 시스템은 문서 정지선을 정지 거리로 쓴다(`TrafficSystem`의 `editorStops`).
- **J001·J005:** 교차로 바로 앞 큰길 횡단보도는 실제에 없다. J001의 실제 넓은 횡단보도는 북동쪽 20 m에 이미 게임에 있다. J005는 북동쪽에 하나를 새로 넣었고, 남서쪽은 이미 있다.
- **J077-4는 J866(J022) 교차로를 가리킨다.** 카드 화면 중심이 옆 교차로였다. J022-3과 합쳐 북쪽은 수평 2열(28 m), 남쪽은 수평(13 m)으로 바꾸고 동쪽은 8 m 옮겼다.
- **J004-4:** 항공사진에서는 약 18 m 바깥이지만 승인 문구(약 10 m)대로 10 m만 옮겼다.
- **J011:** 처음 계산(11 m)은 3 m 더 들어가 8 m로 고쳤다. 남은 차이는 길 가로 방향(게임 도로와 실제 도로의 위치 차이)이다.
- **J007-4:** r55에서 옮긴 위치가 이미 실제와 맞아 고치지 않았다.

## 고친 것(지도 r56 → r57, 시설 바꿈 19·추가 17)

| 항목 | 교차로 | 내용 |
|---|---|---|
| J001-1·2 | J1882_11791 | 교차로 앞 큰길 횡단보도 2곳 삭제 |
| J003-1·2 | J1927_5181 | 서쪽 13 m 바깥, 동쪽 12 m 바깥 + 바깥쪽 자전거횡단도, 정지선 2곳 함께 |
| J005-1 | J2564_12237 | 교차로 앞 2곳 삭제, 북동쪽 2열(깊이 6 m) 횡단보도 추가 |
| J006-1 | J3516_12881 | 남서 횡단보도 삭제, 남서쪽 실제 위치에 추가 |
| J011-1 | J5692_14220 | 서쪽(자전거횡단도 포함) 8 m 교차로 쪽 |
| J022-3·4, J077-4 | J866_5285 | 북쪽 수평 2열 + 정지선, 남쪽 수평, 동쪽 8 m·서쪽 4 m 바깥 + 정지선 |
| J024-1 | reviewed:P22:56 | 북쪽 횡단보도·정지선 4.8 m 북쪽 |
| J004-4 | J2145_15970 | 북서 10 m 바깥 + 정지선 |
| J008-1·2 | J5057_4449 | 북쪽 12 m, 동쪽 5 m 교차로 쪽. 서쪽은 그대로 둠(아래 교통 검사) |
| J020-1·2 | J7973_3744 | 남쪽 6 m, 서쪽 3 m, 동쪽 3.5 m 교차로 쪽 |
| J018-2, J019-1, J021-2 | J7841_867, J7917_2565, J8066_6364 | 황색 횡단보도 2열 깊이 5~6 m(교차로 쪽으로 넓혀 정지선 간격 유지) |

- 정지선: 바깥으로 옮긴 횡단보도는 정지선도 같이 옮겼다. 교차로 쪽으로 옮긴 J008·J011·J020은 정지선을 그대로 두었다(교통 검사 비교 후 결정).
- 계획: `tools/map_review_r57_plan.py` → `config/maps/review/r57-crosswalk-moves.json`. 적용: `MapReviewCrosswalkEdits.ApplyR57`(r56에서만). 기존 시설 수정에 정지선도 허용하도록 일반화했다.
- 교차로 중심은 교차로 카드에서만 읽는다(기둥 카드 P01~P16이 같은 교차로 이름을 써서 중심이 덮이던 오류를 고침).
- 저장 이전 r57 해시 `b0a1e15f…012d`(`BusinessProgress.cs`), 검사 범위 r25~r57.

전후 화면(게임 위 시점, 왼쪽 r56 · 오른쪽 r57): [before-after-top.jpg](../ui/2026-11-25-map-review-crosswalk-moves/before-after-top.jpg)

## 검증

| 검사 | 종류 | 결과 |
|---|---|---|
| `MapReviewCrosswalkEdits.ApplyR57` | 지도 편집 | 바꿈 19·추가 17(최종), 저장 후 해시 재확인 |
| `PrototypeBuild.BuildHybridTrial`(편집기 검사 포함) | 검사용 빌드 | 성공. 다른 세션의 미커밋 코드 포함 |
| 계획 겹쳐 보기(항공사진) | 계획 확인 | J003·J005·J011·J022 확인 후 J011 거리 수정 |
| 교차로 13곳 위 시점 화면 전후(r56·r57) | 위치 제어(화면) | 이동·삭제·깊이·정지선 표시 확인 |
| `-mapAudit -mapAuditSettle 1 -mapAuditObserve 1`(77곳) | 위치 제어 | 적신호 진입 0, 멈춤 0, 겹침 0, 보행자 근접 0 |
| `-mapAuditDump` + `tools/map_poles_in_lanes.py` | 위치 제어 | 바닥 기둥 357개, 차로 안 0 |
| `-districtTrafficTest` 2회 | 자동 물리 주행 | 실패. 아래 참고 |

`-districtTrafficTest`:

| 실행 | 멈춘 차량 | 그 밖 |
|---|---|---|
| r56 배포본(다른 세션 코드 없음) | 6(J3516 3, J7801 유턴 2, J6199 유턴 1) | 녹색 대기 줄 실패 3 |
| r57 검사용 빌드, 안쪽 이동에도 정지선 이동 | 9(위 6 + J5057 서쪽 유턴 대기 3) | 녹색 대기 줄 실패 3 |
| r57 검사용 빌드, 안쪽 이동은 정지선 유지(최종) | 9(같음) | 경로 이탈 1, 녹색 대기 줄 실패 3 |

J5057 대기 줄 3대는 "유턴(좌회전·보행 신호 시)" 차로에서 240초 중 40초 허용을 기다린다(기존 J7801·J6199와 같은 종류). 그 줄에 다른 세션의 NPC 택시("택시")가 끼어 있다. 정지선을 원래 자리에 둬도 결과가 같아서 정지선 이동이 원인은 아니다. 그래서 최종본은 교차로 쪽으로 옮긴 횡단보도의 정지선을 원래 자리에 두었다(정지선은 그대로 줄무늬 바깥에 있다). 경로 이탈 1과 J5057 증가는 검사용 빌드에 섞인 미커밋 NPC 택시 코드 때문일 수 있으나 확인하지 않았다. 다른 세션에 결과를 공유했다. 배포 빌드에서 다시 확인해야 한다.

### 2026-11-26 수정: J5057 서쪽 횡단보도는 옮기지 않음

다른 세션(택시 승강장·NPC 택시)이 같은 검사용 실행본으로 승강장·NPC 택시를 켜고 끄며 각 2회씩 비교했다. 네 번 모두 9대로 같아, 증가 원인이 그 기능이 아님을 확인했다. 그래서 J5057 서쪽(a1 s1) 횡단보도의 5 m 안쪽 이동만 되돌린 지도로 다시 빌드해 돌렸더니, 멈춘 차량이 6대(r56과 같은 위치)로 돌아왔다. 그 접근로는 "유턴(좌회전·보행 신호 시)" 차로가 있다. 횡단보도를 교차로 쪽으로 당기면 유턴 차가 허용 시간(240초 중 40초)을 놓쳐 줄이 길게 남는 것으로 보인다. 그래서 그 횡단보도는 r56 위치에 두었다. r57은 배포하지 않은 판이라 같은 판 번호로 내용과 해시만 바꿨다(`b0a1e15f…012d`). 경로 이탈 1은 다른 세션의 네 번 실행에서 모두 0이라 한 번만 나온 흔들림으로 본다.

| 실행 | 멈춘 차량 |
|---|---|
| r57 최종(J5057 서쪽 유지), 검사용 빌드 | 6(J3516 3, J7801 유턴 2, J6199 유턴 1), 녹색 대기 줄 실패 3 |

사람 플레이와 전체 캠페인 검사는 하지 않았다.

## 배포

같은 작업 폴더에서 다른 세션(택시 승강장·NPC 택시·분실물)이 미커밋 코드로 작업 중이다. `PrototypeBuild.BuildHybrid`는 폴더의 미커밋 코드까지 컴파일하므로, 배포 실행본(`builds/EungyeTaxi/`)은 그 작업이 커밋된 뒤 다시 빌드한다(세션 간 합의). 그래서 이번 커밋은 소스·지도만 담는다. 저장소의 배포 실행본은 r56이다. 검사는 검사용 빌드(`BuildHybridTrial`, `artifacts/hybrid-trial`)로 했고, 그 빌드에는 다른 세션의 미완성 코드도 들어 있었다.

## 남은 것

- 폭·인도 W 28건(도로 폭·연석·차로 폭 축소 등, 차로 형상 편집 필요), 차로 수 N 11, 선 종류 M 18, 선형 L 20, 연결·교차로 모양 C 56, 기타 X 7
- 노면 G 33건: 대부분 로드뷰 확인 항목과 정지선 추가(J027-1, J029-1, J056-4), 크기 변경(J065-1, J067-2)
- 처리 81/277건(J008-2는 동쪽만 옮김)

## 변경 파일

- 지도: `config/maps/project/eungye.project.json`(r57), `config/maps/local-input-manifest.json`, `config/maps/review/r57-crosswalk-moves.json`, `config/maps/review/2026-11-25-map-review-decisions.json`
- 코드: `Assets/Editor/MapReviewCrosswalkEdits.cs`(ApplyR57, 기존 정지선 수정), `Assets/Editor/JunctionRouteFeedbackTests.cs`, `Assets/Scripts/BusinessProgress.cs`(r57 해시만)
- 도구: `tools/map_review_r57_plan.py`(새로), `tools/map_crosswalk_approaches.py`(공백 있는 교차로 이름), `tools/map_review_decisions.py`
- 문서: 이 기록, `LATEST.md`, `CURRENT-STATE.md`, `prototype-development-checklist.md`, `docs/ui/2026-11-25-map-review-crosswalk-moves/before-after-top.jpg`

## 재현

```
python tools/map_review_r57_plan.py <r56 덤프 map-audit.txt>      (r56 지도에서 실행)
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod MapReviewCrosswalkEdits.ApplyR57
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod PrototypeBuild.BuildHybridTrial
artifacts/hybrid-trial/EungyeTaxi.exe -singlePlayer -contentTest -dayLightDayProbe -mapAudit -mapAuditStart 1 -mapAuditLimit 24 -mapAuditOutput <dir>
python tools/map_review_before_after.py <r56 캡처> <dir> <out> 001-J1882_11791 ...
```
