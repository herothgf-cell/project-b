# 2026-10-26 북쪽 두 교차로 차로·교통섬

## 요구와 근거

- J5057_4449 북쪽 진입: 좌1/직2/우3, 북서 분리 우회전과 보행 교통섬. J5076_2580 북쪽 진입: 좌1/직2, 반대 북행1. 두 곳의 노면 방향·연결·횡단보도와 차량 여유를 일치시킨다.
- 상위 작업자가 2026-10-26 직접 확인한 네이버 거리뷰(촬영 2026-05)를 사용했다. J5057 `dcuIEvnfmTeYM4mY2DWaXA`, J5076 `Po3cjZOgY9v4qPfVBPGTYg`. URL·접근별 관찰·미확인 범위는 `config/maps/eungye-intersection-review.json` 및 차로 evidence에 기록했다.
- J5076 동쪽 팔은 은계번영길의 기존 그래프다. 사유지 입구로 보았던 예비 기록은 정정한다. 폭·테이퍼·섬 외곽·난간·신호 시간은 게임 추정이며 실측이 아니다.

## 실제 구현

- `ReviewedJunctionMap.Apply`가 정식 패키지를 복사하고 지원되는 SplitLane/Connect/Create/Store.Save로 저장한다. 원본 baselineHash `0e2eac931017dea133a0f8355fbae99c3fb1960098fbbd91dfb95c3b9f7419bd` 유지, 저장 contentHash `c14a2a4b277e040dce001f2df7f89cc0404555a10b85ef931f2b9de14b6b05dc`.
- 북쪽 긴 접근의 마지막 55m를 분할한다. J5057 가운데 직진을 안쪽3.5m로 옮기고 왼쪽6.8m 위치에 좌회전을 둔다(25m 전이). 원래 바깥 선형은 우회전 전용이 되어 교차로 약28m 전부터 분리된다. J5076은55m 중25m 전이 후3.5m 안쪽 좌회전 대기차로다. 분할의 정차/시설 참조 계보를 보존한다.
- J5057 새 좌회전은 기존 동쪽 NGII `A2218G611338:0`으로, 직진은 기존 남쪽 로컬 도로로 연결한다. 우회전은 서쪽 바깥 NGII `A2218G611369:0`으로 합류한다. 그 바깥 차로는 아파트 진입370으로만 끝나므로 369의20m→375의45m 지점에25.23m 차로변경을 추가하고 기존 진입도 유지했다. 경계 `B2218G612649`의212 점선 원자료를 확인했으며, 이 합류는 현장 규제 확정이 아닌 게임 추정이다.
- 중복된 북쪽 NGII 진입/회전322/323/324/326/349와 대체된 로컬3연결은 비활성화했다. 동·서·반대 북행 원래 연결은 유지한다.
- 우회전 곡선46.04m, 표본 최소 곡률반경13.06m. 합류25.23m, 최소43.77m(폴리라인 이산 계산). 새 우회전 노면의2.35m 반폭 밖 보도를 보존하도록50개 기존 보도 도형을 비활성화하고52개 외측 조각을 생성했다.
- 새 섬은 지원되는 `sidewalk`, 높이0.14m, 삼각형(497,473.7)/(499.8,473.7)/(499.8,481.5), 면적 약10.92㎡다. 기존 보도와 같은 렌더·충돌 경로를 쓴다.
- J5057 북쪽 횡단보도 중심(507.75,474.5), 폭15.9m는 섬499.8부터 동쪽 보도515.7까지 전체3개 본선 차로를 덮는다. 정지선z477. J5076 횡단보도 중심(507.9,273), 폭13.2m는 서쪽 보도501.3부터 동쪽 보도514.5까지, 정지선z276.5다. 우회전 통로에는 별도 횡단보도 도형을 둔다. 별도 현장 신호 주기나 독립 보행 신호를 새로 확정하지 않는다.
- J5076 중앙 검정/노랑 기둥 난간 설정: x509.48,z280..299, 폭0.25m/높이0.9m/기둥 간격 최대2m. renderer의 post-rail 지원은 상위 작업의 별도 변경이다.
- `RoadPaintGeometry`가 지원 편집기의 분할 계보 ID도 원래 도로 가족으로 처리하여 분할 뒤 노면선이 사라지지 않는다. 정식 패키지와 시설 Resources 준비 완료.

## 변경 파일

`unity/EungyeTaxi/Assets/Editor/ReviewedJunctionMap.cs`, `ReviewedJunctionMapTests.cs`와 meta, `Assets/Scripts/RoadPaintGeometry.cs`, `config/maps/project/eungye.project.json`, `config/maps/eungye-intersection-review.json`, `config/maps/eungye-road-facilities.json` 및 해당 Resources, 이 이력.

## 수행한 검증

- 실제 패키지 회귀 RED: `ReviewedJunctionMapTests.Run`, Unity 종료1, `observed approach exists reviewed:J5057-N:left`로 실패. Python 실제 패키지 역할 검사도 같은 미구현을 확인했다.
- 초기 Apply는 보도와 우회전/섬의 침범 및 끝점 중복을 거부했다. 보도 절단·곡선/섬 조정·부동소수점 끝점 중복 제거 후 다시 검증했다. 해시 검증이나 충돌 검사를 우회하지 않았다.
- 최종 `ReviewedJunctionMap.Apply`: Unity 실제 ExitCode0, `REVIEWED_JUNCTION_MAP_OK`, `REVIEWED_JUNCTION_MAP_APPLIED`. 저장 전과 저장 후 재로딩 모두 통과. 로그 `artifacts/junction-map-apply3.log`.
- 좌/직/우 및 좌/직 전용 차로 번호와 각 방향 화살표, 북쪽에서 동·남·서 출구와 서쪽 합류/아파트 진입·반대 북행까지의8개 경로, 기존 유턴3곳 보존.
- 모든 활성 차로의 반폭+0.2m와 섬 비중첩, 우회전 곡선을0.5m 간격으로 검사한 길이4.6m/폭2.1m 차량 직사각형과 섬 비중첩.
- 두 본선 횡단보도 양끝의 보도/섬 착지, 접근/반대 차로 전폭 포함, 분할 전후 중앙선 생성, 패키지 전체 구조/시설/형상 검사.
- 후보 지도 평면 래스터를 생성해 우회전 분리와 보도 절단을 검토했다. 실제 게임 렌더 캡처와 NPC/자동 물리 주행은 상위 통합 작업에서 별도로 수행한다.

## 남은 항목과 재현

- 이 작업의 기하 검사와 위치/경로 검사는 사람의 수동 운전, 물리 주행, 전체 캠페인 검사와 다르다. 통합 BuildHybrid·새 실행파일 NPC/물리·카메라 검토는 상위 작업 이력을 따른다.
- 두 북쪽 접근 이외의 전 방향 현장 대조, 실측 치수, 실제 신호 시간, 별도 우회전 횡단보도 보행 운영은 확정하지 않았다. 원본 입력/입력 ZIP을 변경하지 않았으므로 입력 manifest 갱신 대상은 아니다.
- Unity 프로젝트 `unity/EungyeTaxi`에서 `-batchmode -nographics -quit -executeMethod ReviewedJunctionMapTests.Run`으로 검사한다. 적용은 `ReviewedJunctionMap.Apply` 또는 Taxi/Project Map/Apply Reviewed Northern Junctions. 이미 적용됐으면 재적용하지 않고 검사한다.
- 로컬 검사 환경: Unity2022.3.62f2, Windows. 프로세스는 Start-Process -WindowStyle Hidden -Wait -PassThru로 실행하고 ExitCode를 확인했다. 기본 실행/최종 배포 해시는 상위 통합 기록을 따른다.
