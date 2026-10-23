# 은계 차로별 회전 지도 생성과 거리뷰 1차 대조

## 요구사항

은계 실제 거리뷰를 교차로별로 살펴 도로 차선·회전 방향·노면 표시·표지·횡단보도 등을 정밀하게 게임 맵에 반영한다. 우선 동선부터 진행한다.

## 실제 구현과 확인

- `HybridOverrides.laneTurns`에 교차로ID·진입 차로ID·허용 방향(`L/S/R`)·상태·근거·확인일을 입력할 수 있게 했다. `EungyeHybridBuilder.Generate`는 해당 차로의 금지된 `join:` edge를 만들지 않는다. 그래프를 같이 쓰는 내비·조향 보조·일반 차량에 동일하게 적용된다. 교차로 전체 허용 규칙을 확대하지 않는다.
- 존재하지 않는/중복된 차로ID, 잘못된 방향 문자열과 상태, 근거·날짜 없는 verified 입력은 생성 시 오류로 처리한다. 검증된 회전 허용 여부와 임시 회전 **도형**은 별개로 남아 있으므로 생성 edge의 도형 상태는 provisional을 유지한다.
- T1 `J4396_13458`의 동남→북서 진입에 게임 임시 차로 3개와 기존 연결(좌+직 / 직 / 직+우)을 확인했다. 네이버 거리뷰 2026-05 장면에서 중앙 분리시설, 3개 표시 차로, 횡단보도·정지선, 신호·50 표지가 보인다. 각 화살표가 허용하는 회전과 정확한 출구 연결은 여러 시점 대조가 끝나지 않아 설정하지 않았다.
- 승하차4 주변 은계중앙로 거리뷰에서 어린이보호구역 30 표지·노면 표시, 노란 횡단보도, 보행·자전거 공간을 관찰했다. 검색 주소 핀과 실제 촬영점이 다르고 원본 도로 2071/2080의 횡단 위치 대응이 미완료여서 도형을 임의 배치하지 않았다.
- `config/maps/eungye-traffic-overrides.json`의 `laneTurns`는 현재 빈 배열이다. **확정된 차로별 현장 회전 규제나 게임 주행 형상은 아직 바뀌지 않았다.** 관찰·남은 판단은 `docs/map-polish-plan.md`에 기록했다.

## 변경 파일

- `unity/EungyeTaxi/Assets/Scripts/MapData.cs`
- `unity/EungyeTaxi/Assets/Editor/EungyeHybridBuilder.cs`
- `unity/EungyeTaxi/Assets/Editor/HybridNetworkTests.cs`
- `config/maps/eungye-traffic-overrides.json`
- `docs/map-polish-plan.md`
- `docs/work-history/LATEST.md`
- 이 파일

기존 사용자 변경 중 `PrototypeBuild.cs`, 배포 실행 폴더, 체크리스트·범위 문서 등은 이번 커밋에 섞지 않는다. 체크리스트의 `우선 동선 전 구간의 정확한 촬영점/양 방향/교차로별 차로 및 신호 운영 확정` 항목은 계속 미완료다.

## 수행한 검증과 결과

- `pwsh -File tools/check_workspace.ps1 -Strict`: 종료 0, 필수 입력 지도 3개 존재, `Input issues: 0`. 원본 입력 지도·OSM 대조 설정·입력 manifest 변경 없음.
- Unity 2022.3.62f2 배치 명령 `-batchmode -nographics -quit -projectPath D:/Git/project-b/unity/EungyeTaxi -executeMethod HybridNetworkTests.Run`: 종료 0, `HYBRID_NETWORK_TEST_OK edges=1765 stops=12 pairs=132 portals=24`. 한 진입 차로의 금지 회전을 제거해도 같은 교차로의 다른 차로 회전은 유지되는 검사를 추가했다. 로그는 `artifacts/hybrid-lane-turn-test-final.log`.
- 최초 일반 샌드박스 Unity 실행은 라이선스 IPC 60초 타임아웃(코드 검사 미실행)이었고, 같은 배치 검사를 권한 상승으로 재실행해 통과했다.
- 위치 제어 검사, 자동 물리 주행, 수동 플레이, 전체 캠페인 검사는 수행하지 않았다. 배포 BuildHybrid도 실행하지 않았다. 사용자 변경이 이미 있는 `builds/EungyeTaxi/`와 `PrototypeBuild.cs`를 덮어쓰지 않았다.

## 미검증·남은 작업

1. T1 4개 진입 방향의 정지선 전후 파노라마를 정확한 촬영점에 대응시키고 각 차로의 화살표·표지·유턴·출구 차로를 확정한다. 확인된 항목만 `laneTurns`에 입력하고 경로/AI/노면을 다시 검사한다.
2. 승하차4 횡단보도·30 표시의 게임 좌표를 원본 도로ID와 일치시킨 뒤 보행 공간·노면에 적용한다.
3. 우선 운행 동선 나머지 교차로→전체 맵 순으로 방향별 점검 수를 기록한다. 회전 규칙 확정 후 BuildHybrid, 배포 manifest, 자동 물리 주행과 수동 플레이를 각각 검증한다.

## 실행·재현

1. `docs/map-polish-plan.md`의 거리뷰 링크·게임 차로ID를 대조한다. 사진 자체는 게임 에셋에 복제하지 않는다.
2. 검증된 차로만 `config/maps/eungye-traffic-overrides.json`의 `laneTurns`에 넣는다. 예: `{"junctionId":"J...","laneId":"local:...","turns":"LS","status":"verified","evidence":"촬영점/화살표/표지","checkedAt":"2026-10-22"}`.
3. 위 Unity 배치 검사로 금지 방향 edge 부재와 전체 운송 경로 보존을 확인한다. 배포할 때는 `PrototypeBuild.BuildHybrid`와 `distribution/EungyeTaxi-build-manifest.json` 재생성/검증이 추가로 필요하다.
