# 전체 교차로 대조와 차로 중앙 화살표 (현장 작업 중단·현재분 배포)

사용자가 현장 수정 작업을 중단하고 현재 변경분의 배포를 요청했다. 아래의 미확인 구간은 보존하며 추가 현장 조사·지도 확장은 중단한다. 배포 검증 결과는 하단에 기록한다.

## 요구사항과 범위

- 사용자 요청: T1 나머지 방향부터 모든 교차로의 거리뷰 대조·수정을 계속하고 화살표를 차로 중앙에 배치한다.
- 전체 생성 후보128개 중 실제 회전 join이 있는 교차로73곳/진입 방향232개다. 교통 설정20곳/77개 방향은 전체 목록이 아니다. join이 없는55개 후보도 목록에 보존했다.
- `config/maps/eungye-intersection-review.json`이 전체 목록과 진입 차로ID·기존 게임 연결·위경도·거리뷰 관찰·미확인 상태를 보관한다. `tools/map_review_inventory.py`는 관찰을 보존하며 목록을 재생성한다. pyproj 필요; 지도 원본은 수정하지 않는다.
- 모든 화살표 적용과 모든 교차로의 현장 대조 완료는 서로 다르다. 미확인 차로의 화살표는 기존 임시 게임 연결을 표현한다.

## 현재 구현

- `LaneDirectionPaint`가 공유 그래프의 활성 join에서 좌/직/우·겸용 화살표를 생성한다. 화살촉·선 폭을 포함한 전체 도형의 가로 경계를 중앙 정렬한다. 분할 지점 전의 차로에는 잘못된 단독 회전 화살표를 만들지 않는다.
- T1 북서: 직진3개 노면 화살표와 별도 중앙측 좌회전/유턴 공간, 분리섬 우회전 통로를 관찰했다. 좌회전 pocket과 우회전 분기 경로를 추가했다. 길이25m/테이퍼30m/폭3m 등은 게임 임시 치수다.
- T1 북동: 본선3개 중 가운데·바깥 차로의 직진 화살표와 분리섬 우회전 통로·노란 고원식 횡단보도를 관찰했다. 바깥 본선을 직진으로 분리하고 우회전 통로를 별도로 연결했다. 안쪽 노면 일부는 차량에 가려 확정하지 않았다.
- T1 남서: 양방향·분리섬/고원식 횡단보도를 가진 별도 우회전 통로·본선3개 접근 차로를 관찰했다. 우회전 통로는 추가했다. 게임 본선은 기존2개이며 추가 안쪽 차로의 방향 화살표가 차량에 가려 차로 증설·역할 확정은 보류했다.
- `CorridorPolish`가 초기 영업 동선 밖의 새 reviewed 경로에도 노면·건물 여유 정책을 적용한다.
- 기존 T1 동남 좌회전 pocket은 유지한다. 유턴·실제 신호 시간·현장 실측은 구현/확정하지 않았다.

## 현재까지 실행한 검증

- 기존 화살표 검사: `artifacts/lane-arrow-red-licensed.log` 종료1, 중앙 정렬 검사 실패를 확인했다. 샌드박스 첫 실행은 라이선스 IPC 연결 실패199였으며 정상 라이선스 연결로 다시 실행했다.
- `LaneDirectionPaintTests.Run`: `artifacts/lane-arrow-green.log` 종료0, 49개 방향/회전 조합의 도형 중앙 정렬, 293차로/73교차로의 공유 연결 일치. 이후 새 차로 추가분을 포함한 최종 검사는 별도로 실행해야 한다.
- 신규 우회전 통로 첫 검사: `artifacts/t1-all-directions-model.log` 종료1, 북서 우회전 테이퍼의 노면 폭 부족을 검출했다.
- 노면 보정 뒤 `ReviewedLaneLayoutTests.Run`: `artifacts/t1-all-directions-model-green.log` 종료0, edges1785·차량 폭 노면2007표본·승하차132쌍 경로 검사 통과.
- 아래 최종 배포 검증 전의 중간 기록이다. 수동 물리 주행은 미완료다.

## 중단 시 남은 일 (재개 지시 전에는 진행하지 않음)

1. T1 남서/동남 우회전·북동 안쪽 가림을 가능한 촬영점에서 재확인하고 확인된 차로 형상만 추가한다.
2. 같은 목록의 T2/T3와 나머지73개 교차로의232개 접근을 순차 대조한다. 현장 대조 전체 완료로 표시하지 않는다.
3. 전체 방향 화살표와 추가 형상을 한 BuildHybrid로 통합해 화면·경로·충돌·회귀 검사를 묶어 실행한다.
4. 기본 배포 실행 폴더를 갱신하면 manifest 전체 SHA256을 재생성·검증한다. 필수 생성 지도3개/OSM 기준/입력 ZIP은 그대로다.

T2 좌표 검색으로 열린 은행동677의 아파트 내부 2025-12 촬영점과 주변 은계호수로 촬영점은 실제 T2 정지선 대응이 확인되지 않았다. T2 방향 규칙·형상 변경 근거에 사용하지 않았다. T1 4방향은 부분 관찰 상태이며 나머지72개 교차로의 현장 대조는 미완료다.

## 배포 검증

- `PrototypeBuild.BuildReviewedMap` 종료0 (`artifacts/intersection-release-build.log`). 최신 설정 기준 화살표49개 도형 조합·297개 차로/73개 교차로, 기존 `BuildHybrid` 전체 회귀 검사 통과 후 기본 `builds/EungyeTaxi/EungyeTaxi.exe` 빌드 완료. 새 그래프1789edge, 차량 폭 노면2286표본, 승하차132쌍 경로 검사 통과.
- 배포 실행파일 `-reviewedLaneTrafficTest` 종료0 (`artifacts/intersection-release-runtime.log`). reviewed 경로 전체 충돌체762표본에서 건물 침범0, 내비/NPC 그래프 참조 일치. T1 동남 좌회전 NPC2대 각각1회 통과, 최소 경로상 간격11.99949m. 기구학 NPC 검사이며 택시 물리·수동 운전·모든 추가 통로의 차량 통과나 실제 신호 시간 검사가 아니다.
- `artifacts/t1-all-directions.png`, `artifacts/lane-arrow-reviewed_T1-NW_pocket.png`를 열어 배포본의 차선·화살표 배치를 확인했다. 다른 T1 차로별 고정 화면도 같은 probe가 생성한다.
- `pwsh -File tools/test_prototype.ps1` 종료0, 모델22묶음 PASS (`artifacts/prototype-model-tests.txt`).
- 독립 읽기 전용 코드 검토에서 현재 변경분의 배포를 막는 결함 없음.
- Strict 로컬 입력 검사 종료0, `Input issues: 0`. 입력3개·입력 manifest·OSM 기준·ZIP은 변경 없음.
- 배포 실행 폴더 전체와 manifest를 Git으로 전달한다. manifest의 `sourceCommit`은 실제 소스 커밋을 사용하고 전체 파일의 크기/SHA256을 생성 후 재대조한다. 이력의 실제 커밋 확인: `git log -1 --oneline -- docs/work-history/2026-10-22-all-intersections-and-arrows.md`.

## 실행 재현

- 목록 생성: pyproj가 설치된 Python으로 `python tools/map_review_inventory.py`.
- 도형 검사: Unity 배치 `-executeMethod LaneDirectionPaintTests.Run`.
- 노면/경로 검사: `-executeMethod ReviewedLaneLayoutTests.Run`.
- 최종 빌드 진입점: `-executeMethod PrototypeBuild.BuildReviewedMap` (화살표 검사 + 기존 BuildHybrid 전체 검사/기본 은계 배포).
- 실행파일 `-reviewedLaneProbe`는 새 reviewed 경로의 건물 충돌 표본과 T1 전체/각 화살표 고정 화면을 출력한다. 수동 운전 검사가 아니다.
