# 2026-11-25 지도 검토 2단계: 확인 답 반영과 노면 1차(황색 횡단보도·자전거횡단도·대각선 횡단, 지도 r55)

## 요구

- 사용자가 확인 문서([1단계 기록](2026-11-25-map-review-poles.md))의 답 10개를 돌려줬다(`map-review-answers-2026-11-25.json`).
- 이전 약속대로 답을 판정에 합치고, 노면(횡단보도·보호구역·자전거 횡단도)부터 고친다. 고치기 전에 해당 위치를 항공사진·로드뷰로 확인한다.

## 확인 답

| 질문 | 답 | 반영 |
|---|---|---|
| Q1 노란 테두리 사각형 4곳 | 모두 과속방지턱 | J029-2·J031-1·J034-1·J039-3 반려(게임 그대로) |
| Q2 B4 지점 | 둘 다 반려 | J041-1·J045-3 반려 |
| Q3 주도로 노란 표시 | 둘 다 과속 주의 표시, 횡단보도 추가 안 함 | J045-2·J047-3 반려. "과속 주의 구간" 노면 표시를 새로 그리는 것은 답하지 않아 하지 않음 |
| Q4 적색 고원식 횡단보도 | 넣지 않는다 | J066-4·J069-1 반려 |
| Q5 아래쪽 노란 표시 | 방지턱, 옮기지 않음 | J066-5·J069-2 반려 |
| Q6 아파트 입구 | 공통 규칙: 중앙선 없는 짧은 진입로로만 정리, 넓히거나 횡단보도·연장 안 함 | J024-4·J025-4·J044-4·J075-2·3을 이 규칙으로 처리(도로망 단계) |
| Q7 J064-1 안의 흰 횡단보도 | 넣지 않는다 | J064-1 전부 반려 |
| Q8 J022-1 | 승인 | 왼쪽 접근로 폭 축소(폭 단계) |
| Q9 "3번" 메모 | 로드뷰로 확인하고 고친다 | J004-3·J007-3 로드뷰 확인 항목 |
| Q10 "4군데" | 교통섬 4개를 넣는다 | J008-4·J020-4 승인(도로망 단계) |

모든 판정을 항목 하나씩 합친 작업 목록: `config/maps/review/2026-11-25-map-review-decisions.json`(생성: `tools/map_review_decisions.py <검토 JSON> <답 JSON>`). 항목마다 최종 판정, 판정 출처(항목/카드/질문 답), 처리 결과(`done`)가 있다.

| 최종 판정 | 건수 |
|---|---|
| 승인 | 247 |
| Q6 규칙 적용 | 5 |
| 로드뷰 확인 후 수정(Q9) | 2 |
| 다시 검토(답 없음, 보류) | 5 |
| 반려 | 18 |
| **합계** | **277** |

처리 완료 30건(기둥 16건 + 이번 노면 14건). 남은 승인·규칙·로드뷰 항목은 노면 75, 연결·교차로 모양 56, 폭·인도·횡단보도 37, 선형 20, 선 종류 18, 차로 수 11, 기타 7건이다.

## 이번에 고친 것(지도 r54 → r55)

대상은 높은 확신·로드뷰 불필요로 분류된 노면 항목 중 지금 게임이 그릴 수 있는 것이다. 고치기 전에 교차로마다 카카오 항공사진에 게임 횡단보도 위치를 겹쳐 보았다(`artifacts/map-review-20261124/r55-check/*-air.jpg`, Git 제외).

| 교차로 | 항목 | 고친 것 |
|---|---|---|
| J2145_15970 | J004-1 | 횡단보도 4곳 황색 + 바깥쪽 자전거횡단도 |
| J4396_13458 | J007-2 | 자전거횡단도 4곳 |
| J5057_4449 | J008-3 | 자전거횡단도 4곳(북쪽은 기존 검토 횡단보도) |
| J5692_14220 | J011-2 | 큰길 횡단보도 2곳 자전거횡단도(기존 검토 횡단보도) |
| J6199_11987 | J013-1 | 자전거횡단도 3곳(큰길 2 + 북동) |
| J7254_14780 | J016-1 | 자전거횡단도 4곳 |
| J7801_9629 | J017-2 | 자전거횡단도 4곳 |
| J7973_3744 | J020-3 | 자전거횡단도 4곳 |
| J7841_867 | J018-1·4, J065-1 | 횡단보도 4곳 황색 + 교차로 안 황색 대각선 횡단 2개 |
| J7917_2565 | J019-3, J067-2 | 서·동·남 횡단보도 황색 |
| J8066_6364 | J021-1 | 횡단보도 4곳 황색 |

항공사진 확인에서 시트와 다르게 처리한 것:

- **자전거횡단도는 모두 바깥쪽이다.** 시트에서 J007은 "교차로 쪽", J011은 "서쪽 면"으로 적었지만, 항공사진에서는 8곳 모두 횡단보도의 교차로 반대쪽에 붉은 띠가 있다.
- **J016은 남쪽에도 붉은 띠가 보여 4곳 모두 넣었다**(시트는 3곳).
- **J065·J067 카드의 화면 중심은 옆 작은 교차로였다.** 지적 대상은 J7841(J018)·J7917(J019) 교차로라서 그쪽에 반영했다.
- **J015(H3·H4 황색)는 미뤘다.** 항공사진으로 색을 판단할 수 없어 로드뷰 확인 항목으로 남겼다.

### 구현

- **횡단보도 위치**: 생성 횡단보도에 이름표 `Crosswalk <교차로> a<축> s<방향>`을 붙였다(`TrafficSystem.AddApproach`). 덤프에서 위치·폭·방향을 뽑아(`tools/map_crosswalk_approaches.py`) 같은 교차로·축·방향의 문서 횡단보도 시설로 바꿨다. 같은 축·방향의 문서 시설이 있으면 생성 줄무늬는 그려지지 않는다(기존 구조).
- **자전거횡단도**: 새 필드를 넣지 않고 횡단보도 시설의 `text`(`자전거횡단도:바깥쪽`)로 표시했다(`CrosswalkBikeCrossing.cs`). 지도 내용 해시가 직렬화된 문서 전체에 걸려 있어, 필드를 더하면 이전 모든 판의 저장 해시가 바뀌기 때문이다. 짙은 적갈색 2 m 띠와 양쪽 흰 사각 점선으로 그린다. 그림만 바뀌고 보행자·신호·차로 판정은 이 값을 읽지 않는다.
- **위치 조정**: 생성 정지선이 횡단보도 바깥 0.9 m에 있어 바깥쪽 띠가 정지선을 덮는다. 그래서 띠가 붙는 횡단보도를 교차로 쪽으로 2.3 m 옮기고, 비운 자리에 띠를 그렸다(정지선 → 자전거횡단도 → 횡단보도 순서는 실제와 같다). 항공사진에서도 해당 게임 횡단보도들은 실제보다 바깥에 있었다. 생성 스크립트는 띠가 정지선(생성·문서)과 겹치면 멈춘다.
- **모서리**: 옮긴 횡단보도 끝이 옆 횡단보도와 겹쳐서, 좁은 것부터 0.3 m 간격을 두고 끝을 잘랐다(13곳, 0.1~8.4 m). 실행 시에는 도로 폭 맞춤(`GeneratedCrosswalkGeometry.Effective`)과 잘린 폭 중 겹치는 부분만 쓴다. 그래서 연석도 넘지 않고 모서리도 겹치지 않는다.
- **대각선 횡단(J7841_867)**: 네 횡단보도 안쪽 모서리를 잇는 황색 횡단 2개(폭 4 m, 대각선 길이의 80%)다. 교차로 번호 없는 도색 시설이라 보행 신호·보행자 경로와는 연결하지 않았다.
- **편집**: `config/maps/review/r55-crosswalk-edits.json`(생성 `tools/map_review_r55_crosswalks.py`)을 `MapReviewCrosswalkEdits.Apply`가 r54에서만 적용한다. 바꾼 시설 3, 추가 39. 차로·정지선·신호 프로그램은 그대로다. 저장 이전은 r55 해시만 더했다(`BusinessProgress.cs`). contentHash `78fa0713…e816`.
- 검사기 제한(폭 40 m 이하)에 걸린 5곳은 40 m로 넣었다. 실행 시 도로 폭으로 다시 맞춰진다.

전후 화면(게임 위 시점, 왼쪽 r53 11/24 화면 · 오른쪽 r55): [before-after-top.jpg](../ui/2026-11-25-map-review-markings-1/before-after-top.jpg)

### 이번에 하지 않은 노면(다음 묶음)

- **J067-1 북쪽 횡단보도 삭제**: 항공사진에서도 없다. 생성 횡단보도를 지우면 보행 신호·보행자 횡단도 같이 정리해야 해서 따로 한다.
- **새 그림이 필요한 것**: 적색 미끄럼방지·보호구역 포장(J018-3, J019-4, J021-1, J042-4, J052-3, J063-4, J065-2, J066-3), '30'·'어린이보호구역' 글자, 횡단보도 예고 마름모(J023-3)
- **크기 변경**: 남북 횡단보도 폭 8 m(J065-1), 남쪽 폭 확대(J067-2)
- **새 횡단보도 추가**(J010-1, J027, J042-3, J049-3, J050-2, J051-4, J052-2, J055-1, J056-4, J060-1, J061-1, J062-1, J068-1, J072-1): 보행 신호·정지선과 함께 놓아야 한다.
- **로드뷰 확인 후**: J015-1, 그 밖의 로드뷰 필요 노면 항목

## 검증

| 검사 | 종류 | 결과 |
|---|---|---|
| `PrototypeBuild.BuildHybrid`(편집기 검사 포함) | 빌드 | 성공(`PROTOTYPE_BUILD_OK`), `builds/EungyeTaxi/` 갱신 |
| `MapReviewCrosswalkEdits.Apply` | 지도 편집 | 바꿈 3·추가 39, 저장 후 해시 재확인 |
| 교차로 11곳 위 시점 화면 전후 비교 | 위치 제어(화면) | 황색·자전거횡단도·대각선 표시, 모서리 겹침 없음, 연석 넘침 없음(1차에서 겹침·넘침을 보고 고친 뒤 다시 확인) |
| `-mapAudit -mapAuditSettle 1 -mapAuditObserve 1`(77곳) | 위치 제어 | 적신호 진입 0, 멈춤 0, 겹침 0, 보행자 근접 0 |
| `-mapAuditDump` + `tools/map_poles_in_lanes.py` | 위치 제어 | 바닥 기둥 370개, 차로 안 0 |
| `-districtTrafficTest` 2회 | 자동 물리 주행 | **실패(2회 같음)**. 아래 참고 |

`-districtTrafficTest` 비교:

| 실행본 | 멈춘 차량 | 위치 | 녹색 대기 줄 통과 |
|---|---|---|---|
| r54(1단계) | 5 | J7801 유턴 2 + J3516_12881 3 | 실패 3 |
| 이번 r55 | 6 | J7801 유턴 2 + J3516_12881 3 + **J6199_11987 유턴 1** | 실패 3 |

J6199_11987은 이번에 횡단보도를 옮긴 교차로다. 멈춘 차는 "유턴(좌회전·보행 신호 시)" 차로에서 적신호를 기다리고 있었고, 허용 시간은 240초 중 40초다. J7801과 같은 종류의 긴 유턴 대기다. 유턴 허용은 신호 프로그램과 좌회전·보행 신호 색만 읽고 횡단보도 시설은 읽지 않는다(`TrafficSystem.UturnPermitted`). 그래서 횡단보도 위치가 보행자 횡단 시점을 바꿔 차량 흐름이 달라진 결과로 보이지만, 원인은 확인하지 않았다.

사람 플레이와 전체 캠페인 검사는 하지 않았다.

## 변경 파일

- 지도: `config/maps/project/eungye.project.json`(r55), `config/maps/local-input-manifest.json`(크기·SHA256), `config/maps/review/r55-crosswalk-edits.json`, `config/maps/review/2026-11-25-map-review-decisions.json`
- 코드: `Assets/Scripts/CrosswalkBikeCrossing.cs`(새로), `RoadMapFacilities.cs`(띠 그리기), `GeneratedCrosswalkGeometry.cs`(띠 있는 횡단보도의 폭), `TrafficSystem.cs`(생성 횡단보도 이름표), `BusinessProgress.cs`(r55 이전), `Assets/Editor/MapReviewCrosswalkEdits.cs`(새로), `Assets/Editor/JunctionRouteFeedbackTests.cs`(r25~r55)
- 도구: `tools/map_crosswalk_approaches.py`, `tools/map_review_r55_crosswalks.py`, `tools/map_review_decisions.py`, `tools/map_review_before_after.py`(모두 새로)
- 문서: 이 기록, `LATEST.md`, `CURRENT-STATE.md`, `prototype-development-checklist.md`, `docs/ui/2026-11-25-map-review-markings-1/before-after-top.jpg`
- 배포: `builds/EungyeTaxi/`, `distribution/EungyeTaxi-build-manifest.json`(다음 커밋)

## 재현

```
python tools/map_review_decisions.py <map-review-feedback-2026-11-25.json> <map-review-answers-2026-11-25.json>
artifacts/hybrid-trial/EungyeTaxi.exe -singlePlayer -contentTest -dayLightDayProbe -mapAudit -mapAuditLimit 0 -mapAuditDump "1050,875,2000" -mapAuditOutput <dir> -batchmode   (이름표 붙은 r54 빌드)
python tools/map_review_r55_crosswalks.py <dir>/map-audit.txt
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod MapReviewCrosswalkEdits.Apply   (r54 지도에서만 동작)
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod PrototypeBuild.BuildHybrid
builds/EungyeTaxi/EungyeTaxi.exe -singlePlayer -contentTest -dayLightDayProbe -mapAudit -mapAuditStart 4 -mapAuditLimit 21 -mapAuditOutput <dir>   (-batchmode 없이: 화면 캡처)
python tools/map_review_before_after.py artifacts/map-review-20261124/game <dir> <out> 004-J2145_15970 ...
```

## 다음 작업

1. 노면 2차: J067-1 생성 횡단보도 삭제(보행 신호·보행자 포함), 보호구역 적색 포장·글자·마름모 그리기 추가, 새 횡단보도(보행 신호·정지선과 함께).
2. 로드뷰 확인 항목(J015-1, Q9 J004-3·J007-3 등) 확인 후 수정.
3. 폭·차로 수 → 도로망(Q6 아파트 입구 규칙, Q10 교통섬 4개) 순서.
4. `-districtTrafficTest` 기존 실패(유턴 장기 대기·J3516 교착) 원인.
