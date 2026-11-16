# 은계 색상별 도로 실경 조사 — 2026-11-13

## 사용자 기준과 진행 방식

색은 작업 설명·질문을 위한 구역 구분이다. 하늘색(호수 동·남쪽 은계중앙로), 초록색(주간선), 노란색(북쪽 횡단도로와 연결로), 주황색(J8066 서쪽 곡선), 분홍색(남쪽 생활도로)을 모두 점검한다. 각 구간을 양방향으로 조사하며 차로 수·역할·중앙선/안전지대·분리봉/분리대·보도 난간·시작/끝 전환을 별도 기록한다. 최신 촬영 우선, 가림은 과거 자료 대조. 불명확한 값은 사용자에게 지도 번호로 질문한다. 서로 다른 방향의 차로 수를 강제로 맞추지 않는다.

r19에서 사용했던 포켓90m/입구155m는 게임 추정치이며 이번 실경 조사에서 그대로 확정하지 않는다. 코드의 GreenCorridor 같은 과거 색상명은 이번 사용자 하늘색 구역과 대응하므로 이름만 보고 새 구역을 분류하지 않는다.

## 하늘색 관찰 근거

| 근거 | 관찰 | 상태 |
|---|---|---|
|[e6KR54odkkyxFfCooHKVxw, 동향](https://map.naver.com/p/entry/coordinates/3zao1h,2AGJhI?c=15.00,0,0,0,adh&p=e6KR54odkkyxFfCooHKVxw,90,-15,80,Float)|①과② **사이** 차로 전환부. 앞쪽은 양방향1차로처럼 좁아지며, 가까운 서행 측에는 분기 차선이 남아 있음. 주황색 분리봉, 검정 보도 난간|2026-05 관찰. '모든 아파트 출입구 이후 계속1+1'로 해석하면 안 됨. 횡단면 양끝 미확정|
|[ad9jTnZIV_ZsTko96f1J7Q, 북향](https://map.naver.com/p/entry/coordinates/3zao1h,2AGJhI?c=15.00,0,0,0,adh&p=ad9jTnZIV_ZsTko96f1J7Q,0,-15,80,Float)|유치원 출입구·황색 횡단보도, 북쪽 굴곡 본선. 출입구에서 난간/분리시설이 끊김|2026-05 관찰|
|[6TImoh_RWHtgZ5-3TeeLhA, 북향](https://map.naver.com/p/entry/coordinates/3zao1h,2AGJhI?c=15.00,0,0,0,adh&p=6TImoh_RWHtgZ5-3TeeLhA,0,10,80,Float)|양방향1차로 사이 넓은 황색 빗금 안전지대, 연석과 식재. 안전지대는 주행 차로가 아님|2026-05 관찰|
|[GqL4M4wh513qOJJUeRQxxw, 북향](https://map.naver.com/p/entry/coordinates/3zao1h,2AGJhI?c=15.00,0,0,0,adh&p=GqL4M4wh513qOJJUeRQxxw,0,-15,80,Float)|주유소 남쪽 본선에도 중앙 황색 빗금, 앞 횡단보도까지 연속되는 구간|2026-05 관찰|
|DkF40U1TW0lSBTHEDCiXoQ|지도 클릭 중 공원 내부 좁은 연결로로 이동함. 2021-10이 최신으로 표시됨|하늘색 본선 근거에서 제외|

## 사용자 교차 확인

질문용 지도: ../images/eungye-road-survey-blue-questions.png. 점①(x910/z636),②(x1000/z637)는 **확인 범위 대표점**, 확정된 전환 좌표가 아니다. 실제 지도 자료에서 새로 그린 위치도이며 첨부 이미지의 변형이 아니다. 생성: pwsh -File docs/content-design/road-survey/draw-question-map.ps1.

- ① 사용자 지정 탭: [2blaP5GG9dcnpKVIPOTksg](https://map.naver.com/p/entry/coordinates/3zao1h,2AGJhI?c=15.00,0,0,0,adh&p=2blaP5GG9dcnpKVIPOTksg,85.97,-13.98,80,Float). 2026-05. 남쪽 아파트 출입구 앞 황색 횡단보도/자전거 횡단 표시. 같은 지점에서 서쪽265.97도도 대조했다. 중앙선 분리봉과 방향별 추가 차로가 출입구 주변에서 바뀜. 기존 임시 전환 거리 대신 출입구와 횡단보도로 정렬할 기준점이다.
- ② 사용자 지정 두 번째 탭: [kqv56x0WRaONNhRIG47jdA](https://map.naver.com/p/entry/coordinates/3zao1h,2AGJhI?c=15.00,0,0,0,adh&p=kqv56x0WRaONNhRIG47jdA,-31.69,-12.4,80,Float). 2026-05. 그 동쪽 맞은편 아파트 출입구, 좌회전 표지·신호·황색 횡단보도. 촬영점이 본선 옆 출입구에 있으므로 카메라 위치를 본선 중심으로 취급하면 안 된다. 사용자가 '그 부근'으로 지정했다.
- 두 탭은 사용자 지정 각도로 복원했다. 후속 탐색은 별도 조사 탭에서 진행한다.

## 구현 전 확인할 사항

### ①·② 연속 이동 교차 확인 결과

두 사용자 탭을 기준으로 조사 탭에서 동쪽으로 연속 이동했다. 모두 2026-05 촬영이다. URL의 `entry/coordinates`는 원래 검색 표식이며 현재 촬영점 좌표가 아니다. panorama ID를 근거로 보존하고, 검색 표식에서 게임 좌표를 추정하지 않는다.

1. ① `2blaP5GG9dcnpKVIPOTksg`: 남측 출입구 앞. 동쪽을 볼 때 서행 측 2차로의 안쪽은 남측 출입구 좌회전 화살표, 바깥쪽은 직진이다. J8066용 좌회전 차로 하나로 연결할 근거가 아니다.
2. `e6KR54odkkyxFfCooHKVxw` → [`-Irdo6fzEpjrC6J86JRGkw`](https://map.naver.com/p/entry/coordinates/3zao1h,2AGJhI?c=15.00,0,0,0,adh&p=-Irdo6fzEpjrC6J86JRGkw,85,-13.98,80,Float): 중앙선이 꺾인 후 동행 측에 흰 점선이 생겨 2차로, 서행1차로가 된다. 전환 중심은 ① 카메라 위치 그 자체가 아니라 그 동쪽이다.
3. [`Ai1Ro0Jq8dqN1MuAdPy9DA`](https://map.naver.com/p/entry/coordinates/3zao1h,2AGJhI?c=15.00,0,0,0,adh&p=Ai1Ro0Jq8dqN1MuAdPy9DA,85,-13.98,80,Float): ②와 동일한 출입구 교차 지점을 본선에서 확인. 북측 제일풍경채 출입구와 남측 NATURE FORE 출입구, 황색 횡단보도. 사용자②는 이 교차 지점의 남측 진입로 촬영점이다.
4. [`gC-7dHQFC4UYvofPeV8Mxw`](https://map.naver.com/p/entry/coordinates/3zao1h,2AGJhI?c=15.00,0,0,0,adh&p=gC-7dHQFC4UYvofPeV8Mxw,85,-13.98,80,Float): ② 동쪽에도 반대 방향 추가 차로가 보인다. 따라서 '② 직후부터 계속1+1'은 잘못된 단순화다.
5. [`I13jqct1pHcpD-LhLdkcjw`](https://map.naver.com/p/entry/coordinates/3zao1h,2AGJhI?c=15.00,0,0,0,adh&p=I13jqct1pHcpD-LhLdkcjw,85,-13.98,80,Float): 더 동쪽 굴곡의 또 다른 신호 지점. 접근 측 안쪽 좌회전, 바깥쪽 직진/우회전 노면 화살표 확인. 앞선 아파트 포켓과 구별해야 한다.
6. [`eMbFN716qi7v-A7Y0VHMvw`](https://map.naver.com/p/entry/coordinates/3zao1h,2AGJhI?c=15.00,0,0,0,adh&p=eMbFN716qi7v-A7Y0VHMvw,0,-13.98,80,Float): 위 신호 지점의 북측에도 남행 접근 차로2개가 확인된다. 이곳까지 모든 단면을1+1로 고정하면 안 된다. 이후 감소 끝점은 추가 정렬 필요.
7. [`pa2a58z2IqEwC2Yq2maslg`](https://map.naver.com/p/entry/coordinates/3zao1h,2AGJhI?c=15.00,0,0,0,adh&p=pa2a58z2IqEwC2Yq2maslg,275,-15,80,Float): 호수 남쪽 도로 서향, 왕복1+1 사이 넓은 황색 빗금 안전지대. 이 구간 중앙에 주황 분리봉/강철 가드레일은 보이지 않는다. 호수 측 보도 난간은 별도 시설이다.

현재 번호 지도는 질문 당시 범위도이며 실제 조사 결과의 정확한 차로 전환 좌표로 재사용하지 않는다. 다음은 위 촬영점과 원본 도로/출입구 위치 정렬, 각 포켓의 시작·끝 및 연결 역할을 별도 구간으로 작성하는 것이다. 이번 대조로 기존 r19 단순 포켓 두 개만으로 실경이 재현되지 않는다는 점을 확인했다.

출입구 좌회전 포켓을 J8066 교차로 좌회전 포켓과 하나로 합치지 않는다. 중앙 황색 안전지대, 주황색 탄성 분리봉, 강철 가드레일, 검정 보행 난간을 서로 다른 시설로 구현한다. 횡단보도·출입구 개구부는 충돌체도 열어야 한다. 치수를 확인하지 못하면 질문을 남기고 확정값처럼 작성하지 않는다.

초록/노랑/주황/분홍은 아직 이번 조사 미완료. 이 문서는 지도 수정·빌드 완료 보고가 아니다.

## 하늘색 북쪽 연속 재조사 — 본 조사 세션의 직접 관찰

아래는 본 조사 세션에서 화면으로 직접 확인한 파노라마와 사용자 최종 확인을 전달받아 기록한 것이다. 별도 표기가 없으면 최신 **2026-05** 촬영이다. 방향값은 카메라 시선이며 URL에서 실제 촬영 좌표를 추정하지 않았다. 이 기록을 남긴 시점에 북쪽 수정 코드는 준비 중이며 지도 적용·빌드 완료를 뜻하지 않는다.

**북쪽 전체를 1+1로 보는 기존 해석을 정정한다.** 호수 남쪽의 넓은 황색 빗금 안전지대 구간은 1+1이지만 J8921 이후 북쪽으로 이어지는 구간은 북행2·남행1이 확인된다. J8066 인근 아파트 포켓, 호수 남쪽 1+1, J8921 접근 확폭, 그 북쪽 2+1을 서로 다른 구간으로 관리한다.

|구간/상태|파노라마 근거|직접 관찰 범위·남은 항목|
|---|---|---|
|호수 남쪽 — 단면 관찰|[pa2a58z2IqEwC2Yq2maslg, 275°](https://map.naver.com/p?c=18,0,0,0,adh&p=pa2a58z2IqEwC2Yq2maslg,275,-15,80,Float), [PM6gptaOw47iv6JR5OojZw, 275°](https://map.naver.com/p?c=18,0,0,0,adh&p=PM6gptaOw47iv6JR5OojZw,275,-15,80,Float)|양방향1차로 사이 넓은 황색 빗금 안전지대. 이 관찰을 북쪽 전체에 확장하지 않는다.|
|J8921 접근 전환 — 끝점 실측 미확정|[8HfgvQk0xZc9ZkE2fAb6Jg, 275°](https://map.naver.com/p?c=18,0,0,0,adh&p=8HfgvQk0xZc9ZkE2fAb6Jg,275,-15,80,Float)|J8921 접근 중 빗금 안전지대가 끝남. 정확한 게임 거리·전환 좌표는 아직 확정하지 않음.|
|J8921 서향 접근 — 추가 차로 확인, 역할 미확정|[GuwRfDF4dTuV66MLaEfu_Q, 275°](https://map.naver.com/p?c=18,0,0,0,adh&p=GuwRfDF4dTuV66MLaEfu_Q,275,-15,80,Float), [dZN3nNMwPHZAnVoUSM_hFA, 275°](https://map.naver.com/p?c=18,0,0,0,adh&p=dZN3nNMwPHZAnVoUSM_hFA,275,-15,80,Float)|서향 진입 추가 차로가 보임. 각 차로의 좌/직/우 역할은 이 관찰만으로 확정하지 않음.|
|J8921 북쪽 — 2+1 및 분리봉 확인|[Svj_tAi7_jOcTxqJuRpgqA, 275°](https://map.naver.com/p?c=18,0,0,0,adh&p=Svj_tAi7_jOcTxqJuRpgqA,275,-15,80,Float), [bJCNXzQ9X5_NMPRn22I0JQ, 330°](https://map.naver.com/p?c=18,0,0,0,adh&p=bJCNXzQ9X5_NMPRn22I0JQ,330,-15,80,Float), [J99mSjoNasR3yLxB-ci28g, 330°](https://map.naver.com/p?c=18,0,0,0,adh&p=J99mSjoNasR3yLxB-ci28g,330,-15,80,Float)|북행2·남행1, 주황색 분리봉. 굴곡 뒤를 단순1+1로 축소하면 안 됨.|
|J7355 — 접근 역할 관찰|[jFGgn9NnhikzoVgrM25uOQ, 330°](https://map.naver.com/p?c=18,0,0,0,adh&p=jFGgn9NnhikzoVgrM25uOQ,330,-15,80,Float), [RWXVUEJTQrhcBXVWhM8hfw, 330°](https://map.naver.com/p?c=18,0,0,0,adh&p=RWXVUEJTQrhcBXVWhM8hfw,330,-15,80,Float)|북행 안쪽 좌회전+직진, 바깥쪽 직진. 남행 단일 차로는 직진+우회전 표시 확인.|
|번호13 J7292 — 사용자 확인 완료: 화살표 없음|[4ar3QJjrUwVlhLDiZXIL0w, 0°/-30°](https://map.naver.com/p?c=18,0,0,0,adh&p=4ar3QJjrUwVlhLDiZXIL0w,0,-30,80,Float), [2024-03 4lTP3CoY49_GTjg3Cnp7iQ, 0°/-30°](https://map.naver.com/p?c=18,0,0,0,adh&p=4lTP3CoY49_GTjg3Cnp7iQ,0,-30,80,Float), [사용자 정렬 49BZzsptK-aM18ceIgg3Xw, 5.79°/-15.38°](https://map.naver.com/p?c=18,0,0,0,adh&p=49BZzsptK-aM18ceIgg3Xw,5.79,-15.38,80,Float)|최신·과거 화면에서 노면 화살표가 보이지 않아 교차 확인했고, 사용자가 번호13에 화살표가 없다고 최종 확인. 본 조사 세션은 사용자 링크가 같은 교차로임을 확인했다. 화살표 없음과 회전 법규상 허용/금지는 별개다.|
|J7254 북행 — 역할 확인|[xLEUJnDRhw1QuTbU-YfD_g, 0°/-25°](https://map.naver.com/p?c=18,0,0,0,adh&p=xLEUJnDRhw1QuTbU-YfD_g,0,-25,80,Float), [gapUd9473aIXMECGhUc2sQ, 0°/-25°](https://map.naver.com/p?c=18,0,0,0,adh&p=gapUd9473aIXMECGhUc2sQ,0,-25,80,Float)|첫 근거에서 안쪽 좌회전+직진, 바깥쪽 **우회전 전용** 확인. 두 번째는 차량에 화살표가 가려져 역할 판단의 독립 근거로 사용하지 않음.|

위 관찰에서 유턴 표시는 확인하지 못했다. 이것을 유턴 허용 근거로 사용하지 않으며, 미관찰을 곧바로 법적 금지 확정으로도 바꾸지 않는다. 파노라마 사이 실제 전환 거리, 분리봉 시작·끝과 개구부는 아직 별도 정렬이 필요하다.

현 지도 연결 사슬은 J8066 `(806.645,636.377)` → J8921 `(892.105,1083.534)` → J7355 `(735.546,1282.144)` → J7292 `(729.158,1379.427)` → J7254 `(725.350,1478.018)`이다. `cyan-stations.json` 및 `../images/cyan-stations.png`는 현 게임 차로를 따라 만든 위치 정렬 보조 자료이며 실측 전환점을 표시한 자료가 아니다. `colored-inventory.json`의 `laneIds`에는 같은 source의 활성 `editor:`·`reviewed:`·`join:` 조각도 포함한다. 조각 수를 실제 차로 수로 세지 않는다.
