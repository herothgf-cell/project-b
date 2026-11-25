# 2026-11-25 지도 검토 1단계: 차로 위 기둥(P01~P16) 확인·수정 (지도 r54)

## 요구

- 사용자가 [11/24 검토 시트](2026-11-24-map-review-sheet.md)의 판정 JSON(`map-review-feedback-2026-11-25.json`, 승인 87·다시 검토 5·반려 1, Q1~Q5 답)을 돌려줬다. 사용자 요청: "혹시라도 내가 잘못한 거나 같은 위치인데 응답이 이상하면 확인 받고 진행해."
- 같은 위치끼리 판정이 엇갈린 10곳을 질문으로 남겼다(아래 "확인 대기"). 사용자는 "기둥부터 시작"으로 답했다.

## 확인 결과: P02~P16은 차로를 막지 않는다 (시트 오판)

- 시트의 "차도 위 기둥 16개" 중 유턴 조건 표지 "받침대" 15개(P02~P16)는 높이 4.45~6.95 m에 떠 있는 짧은 막대다. 신호등 함 위에 표지판을 다는 막대(`RoadMapFacilities.cs`의 `표지 받침대`)라 바닥에 서 있지 않고, 차를 막지 않는다. 덤프 좌표: 중심 y 5.7 m, 높이 2.5 m.
- 11/24 계산은 높이를 보지 않고 굵기와 길이만 봐서 15개를 잘못 셌다. 시트 시안("바닥 기둥 삭제 → 신호등 가로대에 표지")은 이미 게임에 들어가 있는 모습이다. 그래서 P02~P16은 고칠 것이 없다.
- 검사를 `tools/map_poles_in_lanes.py`로 옮기고, 바닥에서 0.5 m 안에서 시작하는 기둥만 세도록 고쳤다. 11/24 덤프(r53)를 다시 계산하면 바닥 기둥 364개 중 차로 안은 **P01 하나**다.

## 고친 것: P01 신호등 기둥 (J5692_14220, 지도 r53 → r54)

| 항목 | 전 | 후 |
|---|---|---|
| 시설 | `reviewed:Y05:44:0:-1:signal`(서행 먼쪽 신호) | 같음 |
| 기둥 위치 | (569.27, 1438.92): 아파트 정문 도로와 큰길이 만나는 아스팔트. 회전 경로 `join:local:7003:14854:5837:14446:0` 중심에서 0.1 m(차로 안 1.74 m) | (568.35, 1444.22): 정문 도로 동쪽 모서리 인도. 가장 가까운 차로까지 1.62 m |
| 가로대 | 3.1 m | 8.1 m(가로대 쪽으로 5 m, 도로 방향 서쪽으로 2 m) |
| 신호등 함 | 서행 차로 위 | 같은 차로 위, 도로 방향으로 2.0 m 이동 |

- 바꾼 것은 이 시설 1개뿐이다. 차로·정지선·신호 그룹·신호 시간은 그대로라 경로와 법규 판정은 달라지지 않는다. 저장 이전(migration)은 r54를 받도록만 고쳤다(`BusinessProgress.cs`, 시설만 바뀌었으므로 경로 재계산 없음).
- 편집 스크립트: `unity/EungyeTaxi/Assets/Editor/P01SignalPoleMove.cs`(r53이 아니면 멈추고, 옮긴 뒤 0.8 m 이상 떨어졌는지 확인한다). 저장된 contentHash: `9f37adc6…5b8d`.
- 위치 선정: 차로 중심선 거리만 보면 교차로 안 빈 아스팔트도 "차로 밖"으로 나온다. 그래서 게임 위 시점 화면에서 반경 1.2 m가 모두 밝은 바닥(인도)인 가장 가까운 자리를 골랐다. 전후 화면: [before-after-top.jpg](../ui/2026-11-25-map-review-p01/before-after-top.jpg)

## 검증

| 검사 | 종류 | 결과 |
|---|---|---|
| `PrototypeBuild.BuildHybrid`(편집기 검사 포함) | 빌드 | 성공, `builds/EungyeTaxi/` 갱신 |
| `-mapAuditDump` + `tools/map_poles_in_lanes.py` | 위치 제어 | 바닥 기둥 368개, 차로 안 **0** |
| `-mapAudit -mapAuditSettle 1 -mapAuditObserve 1`(77곳) | 위치 제어 | 적신호 진입 0, 멈춤 0, 겹침 0 |
| J5692_14220 위 시점 화면 | 위치 제어 | 기둥이 모서리 인도 위 |
| `-districtTrafficTest` | 자동 물리 주행 | **실패(기존과 같은 실패)**. 아래 참고 |

`-districtTrafficTest` 비교(모두 `-contentTest`, 결정적이며 같은 실행본은 두 번 모두 같았다):

| 실행본 | 멈춘 차량 | 위치 | 녹색 대기 줄 통과 |
|---|---|---|---|
| 11/24 22:35 검사용 빌드 | 2 | J7801_9629 유턴 2 | 실패 3(기존) |
| HEAD 53566228의 배포 실행본(r53, Git에서 꺼냄) | 4 | J7801 유턴 2 + J1927_5181 은행고길 2(11/24 "1134.566 m" 쌍) | 실패 3 |
| 이번 r54 | 5 | J7801 유턴 2 + J3516_12881 대은로 3(교차로 안 2대가 녹색인데 정지, 1대 적색 대기) | 실패 3 |

교차로 J3516은 옮긴 신호(J5692)에서 약 260 m 떨어져 있다. 이번 변경은 화면용 시설뿐이다. 실행본마다 멈춘 위치가 J1927 → J3516으로 옮겨 다니므로, 실행 시점에 따라 드러나는 기존 교차로 교착으로 본다. 원인은 확인하지 않았다.

사람 플레이와 전체 캠페인 검사는 하지 않았다.

## 확인 대기(사용자 답 필요, 지도 미변경)

검토 JSON에서 같은 위치인데 답이 엇갈리거나 판정과 메모가 맞지 않는 곳이다.

1. 은행로 블록의 노란 테두리 사각형(J029-2·J031-1 반려, J034-1 다시 검토, J039-3 승인, 서로 3~11 m): 모두 과속방지턱으로 둘지
2. J041-1(다시 검토)과 J045-3(반려, 방지턱): 약 4 m 거리, 같은 판정으로 볼지
3. J045-2(반려, "과속 주의 구간 표시")와 J047-3(승인, 황색 횡단보도·고원식): 약 11 m 거리
4. J066-4(승인)와 J069-1(반려): 같은 골목 입구의 적색 고원식 횡단보도
5. J066-5(승인, 노란 횡단보도 8 m 이동)와 J069-2(다시 검토, "방지턱"이라는 메모)
6. 아파트 입구 처리 규칙(J024-4 승인, J025-4·J044-4·J075-2·3 반려, J025 메모 "진입로처럼은 OK")
7. J064-1 반려 항목 안의 흰 횡단보도 추가
8. J022-1: "빨간 왼쪽 박스면 승인"이라는 메모. 맞으니 승인으로 처리한다.
9. J004·J007의 "3번" 한 단어 메모 뜻
10. J008·J020의 "4군데" = 모서리 교통섬 4개인지

## 변경 파일

- 지도: `config/maps/project/eungye.project.json`(r54), `config/maps/local-input-manifest.json`(크기·SHA256)
- 코드: `unity/EungyeTaxi/Assets/Editor/P01SignalPoleMove.cs`(새로), `Assets/Scripts/BusinessProgress.cs`(r54 이전), `Assets/Editor/JunctionRouteFeedbackTests.cs`(r25~r54)
- 도구: `tools/map_poles_in_lanes.py`(새로)
- 문서: 이 기록, `LATEST.md`, `CURRENT-STATE.md`, `prototype-development-checklist.md`, `docs/ui/2026-11-25-map-review-p01/before-after-top.jpg`
- 배포: `builds/EungyeTaxi/`, `distribution/EungyeTaxi-build-manifest.json`(다음 커밋)

## 재현

```
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod P01SignalPoleMove.Apply   (r53 지도에서만 동작)
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod PrototypeBuild.BuildHybrid
builds/EungyeTaxi/EungyeTaxi.exe -singlePlayer -contentTest -dayLightDayProbe -mapAudit -mapAuditDump "1050,875,2000" -mapAuditOutput <dir> -batchmode
python tools/map_poles_in_lanes.py <dir>/map-audit.txt
builds/EungyeTaxi/EungyeTaxi.exe -singlePlayer -contentTest -districtTrafficTest -batchmode -logFile <log>   (결과: artifacts/district-traffic-test.txt)
```

## 다음 작업

1. 위 10개 확인 답을 받으면 노면(횡단보도·보호구역·자전거 횡단도) → 폭·차로 수 → 도로망 순서로 고친다. Q5 답에 따라 고치기 전에 해당 위치를 로드뷰로 확인한다.
2. Q4: 도로 번호(`config/maps/eungye-road-zones.json`)를 개발용 지도 표시로 켜고 끄게 한다.
3. `-districtTrafficTest`의 J3516_12881 교차로 안 교착(녹색인데 2대 정지) 원인을 확인한다.
