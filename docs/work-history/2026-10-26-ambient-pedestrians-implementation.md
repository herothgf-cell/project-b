# 일반 보행자 런타임 구현 인계 (2026-10-26)

## 요구와 실제 구현

사용자 승인 A안에 따라 J5057/J5076/T1/T2 네 거점에서 **검증된 슬롯만** 만든다. 추적 정식 지도 `config/maps/project/eungye.project.json`을 로드해 보도 다각형의 합집합, 몸체 여유, 실제 높이, 건물·물·도로·활성 차로·중앙 난간을 한 번 검사하고 경로를 캐시한다. 18,200개 보도 다각형을 매 프레임 검색하지 않는다. 기존 횡단보도 페인트의 접근 방향·폭과 편집 시설 좌표를 그대로 사용하고 횡단 전 경로와 양쪽 보도 지면을 확인한다. 위험하거나 연속된 20–40m 경로가 없는 슬롯은 비운다.

행인은 기존 승객 배열과 상호작용·지도 핀에서 분리한 회색·갈색·자주색 보행 외형이다. 보도 속도는 1.2–1.4m/s이며 끝에서 잠시 쉬고 같은 경로로 돌아간다. 횡단은 기존 10초 보행 녹색의 마지막 2초를 제외하고 차량이 비었을 때 시작한다. 적색으로 바뀌어도 완주하고 같은 녹색에 다시 진입하지 않는다. 일반 차량은 정지선을 지난 뒤에도 실제 앞으로 이어지는 경로가 점유된 횡단과 만날 때 멈춘다. R5/PS09 상황 보행자의 횡단 경로도 같은 일반 차량 점유 검사에 포함한다. H 자동 조향의 가감속 소유권은 바꾸지 않았다. 택시와 닿으면 실제 택시 차체 콜라이더의 최근접점을 사용해 기존 충돌 사건을 접촉당 한 번 기록하고 접촉 래치를 저장한다. 행인에 물리 콜라이더를 달지 않아 택시를 튕기지 않는다.

R5/PS09가 해당 거점을 사용할 때 일반 횡단 대기 행인을 숨기고 진입을 막는다. 이미 횡단 중인 행인은 지우지 않으며, 상황 시작은 점유가 끝날 때까지 미룬다. 전체 가시 행인 상한은 12명이다. 새 행인은 최초 월드 구성에서 만들고 위치·횡단 점유를 live checkpoint에 담는다. 구 저장의 보행자 정보가 없으면 안전 보도 대기부터 시작한다. 잘못된 배열·ID·수치는 복원 전 거부한다. `-ambientPedestrianTest`와 `-ambientPedestrianReloadTest`는 `artifacts/ambient-pedestrian-test.json`만 사용하며 사용자 캠페인 저장을 읽거나 덮어쓰지 않는다.

## 정식 지도 배치 결과

`AmbientPedestrianGeometryAudit.Run`에서 **보도 3명 + 횡단 3명 = 6명**을 확인했다. J5057 횡단 1명, J5076 보도 2명/횡단 1명, T2 보도 1명/횡단 1명이다. J5057 보도 두 슬롯, T1 전체 세 슬롯, T2 보도 한 슬롯은 연속된 안전 경로 또는 횡단 양쪽 보도 접지가 확인되지 않아 비웠다. J5057/T2의 긴 횡단은 기존 넓은 접근부의 생성 페인트 띠와 일치하며, 새 횡단보도를 추가한 것이 아니다. 상세 좌표와 캐시 규모는 `artifacts/ambient-final-compile.log`의 `AMBIENT_GEOMETRY` 줄에 있다.

## 변경 파일

- 신규: `unity/EungyeTaxi/Assets/Scripts/AmbientPedestrianPlacement.cs`, `AmbientPedestrianSystem.cs`, `AmbientPedestrianProbe.cs`, `AmbientPedestrianProbeRunner.cs`, 각 `.meta`; `unity/EungyeTaxi/Assets/Editor/AmbientPedestrianGeometryAudit.cs`와 `.meta`.
- 연동: `unity/EungyeTaxi/Assets/Scripts/DriveSession.cs`, `TrafficSystem.cs`, `TrafficCar.cs`, `BusinessProgress.cs`, `RightTurnSituation.cs`, `ScenarioChoiceSituation.cs`.
- 정식 지도·원본 입력·지도 ZIP·실행 빌드·배포 manifest는 이번 구현자 작업에서 변경하지 않았다.

## 실제 수행 검증

- 프로브 선행 RED: Unity 편집기 컴파일에서 `AmbientPedestrianProbe.cs`의 미존재 `DriveSession.ambientPedestrians` CS1061 확인 (`artifacts/ambient-probe-red.log`).
- 최종 집중 지도/컴파일 검사: Unity 2022.3.62f2 `-batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod AmbientPedestrianGeometryAudit.Run -logFile artifacts/ambient-final-compile.log` → `AMBIENT_GEOMETRY_AUDIT_OK count=6 sidewalks=3 crossings=3`, `Exiting batchmode successfully`, 종료 0. 모든 배치 보도 경로를 32개 지점에서 다시 확인했다.
- `git diff --check` 문제 없음. Unity 전체 모델/캠페인 검사와 BuildHybrid는 수행하지 않았다.

## 남은 필수 확인과 재현

최종 빌드 후 같은 실행 파일에서 `-ambientPedestrianTest`를 한 번 실행해 적색 진입 금지·녹색/마지막 2초·실제 NPC 정지·비교통 경로 통과·승객 분리·화면을 확인한다. 종료 결과는 `artifacts/ambient-pedestrian-test.txt`, 이미지는 `artifacts/ambient-pedestrians.png`, 중간 저장은 `artifacts/ambient-pedestrian-test.json`이다. 그 다음 **새 프로세스**로 `-ambientPedestrianReloadTest`를 실행해 실제 `BusinessProgress` 시작 복원과 횡단 점유·위치·재개를 확인한다 (`artifacts/ambient-pedestrian-reload.txt`). 두 실행의 결과는 아직 미검증이며 최종 빌드 담당자가 기록한다. 수동 운전, 전체 캠페인, 장시간 성능은 검사하지 않았다.
