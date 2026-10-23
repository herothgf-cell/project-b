# T1 동남 진입 좌회전 차로 적용

## 요구사항과 현장 판단

거리뷰를 직접 확인해 교차로의 차로별 좌회전/직진을 실제 게임 지도에 반영한다. T1 `J4396_13458`의 은계로 동남→북서 진입을 우선 적용했다.

네이버 2026-05 파노라마 `lygcigyMAFesMYe7CZrtIA`를 이동·회전·확대해 중앙 쪽 좌회전·유턴 공간과 기본 직진 차로를 구분했다. 이전 1차 기록의 ‘3개 표시 차로’는 접근부 전체 차로 수의 확정이 아니었다. 이번 모델은 짧은 좌회전 차로1 + 기존 직진 차로3으로 보완한다. 유턴 경로·운영 조건, 외측 우회전 통로, 다른3방향은 미완료다. 원본 영상은 관찰에만 사용했다.

Ruling: 기존 안쪽 차로를 좌회전 전용으로 바꾸면 직진 차로 하나가 사라진다. 원본 기본3차로를 유지하고 중앙 쪽 짧은 차로를 별도로 추가한다. 길이25m·진입30m·이격3m는 실측값이 아닌 게임 도로 안의 임시 치수이며, 실제 형상 정합은 후속 과제다.

## 실제 구현

- `ReviewedLaneLayout.Apply`는 OSM 대조 후·노면 생성 전에 원본 도로의 마지막 구간을 분할하고 진입 연결·좌회전 차로·새 좌회전 join을 추가한다. 기존 안쪽 직진 차로의 좌회전 join은 비활성화한다. 바뀐 승하차 edge가 있으면 인덱스/거리도 보정한다.
- 진입 이후 번호는 좌회전1·직진2/3/4로 구분한다. NGII 차로·필수 원본3개·OSM 대조 기준 JSON은 변경하지 않는다. 이 변경은 로드 후의 별도 구간ID 설정이며 내비·H·NPC가 같은 edge 배열을 공유한다.
- `RoadPaintGeometry`가 새 차로를 함께 사용하고, 중앙선이 진입부를 따라 부드럽게 이동하도록 보완했다. 좌회전과 직진 화살표를 별도로 표시한다. 회전 도형 상태는 provisional이다.
- NPC가 새 join을 같은 T1 교차로의 좌회전으로 인식하는지 검사했다. `join:` ID 규칙을 유지해 신호/교차로 예약 처리와 연결한다.
- `PrototypeBuild.BuildHybrid`가 새 설정을 Resources로 복사하고 새 회귀 검사를 수행한다. 별도 검증 빌드는 배포 폴더/FirstDrive를 덮어쓰지 않는 `ReviewedLaneLayoutTests.BuildChecks`다. 생성한 전용 장면은 완료 시 삭제한다.

## 변경 파일

- `config/maps/eungye-reviewed-lanes.json`, 동일한 `Assets/Resources/eungye-reviewed-lanes.json` 및 meta
- `unity/EungyeTaxi/Assets/Scripts/ReviewedLaneLayout.cs`, `ReviewedLaneLayoutProbe.cs` 및 meta
- `MapCatalog.cs`, `RoadPaintGeometry.cs`, `TrafficSystem.cs`의 차로 적용/표시 호출
- `unity/EungyeTaxi/Assets/Editor/ReviewedLaneLayoutTests.cs` 및 meta, `PrototypeBuild.cs`
- `docs/map-polish-plan.md`, `docs/prototype-development-checklist.md`, `docs/prototype-scope.md`, `docs/work-history/LATEST.md`, 이 파일

공유 `TrafficSystem.cs`·체크리스트·LATEST의 다른 채팅 변경은 해당 작업의 부분만 스테이징한다. 다른 승객·교통·가독성 코드와 배포 폴더의 동시 변경은 이번 소스 커밋에 포함하지 않는다.

## 검증과 결과

1. 변경 전 은계를 명시한 검사에서 `T1 southeast approach has a separate short left-turn lane` 실패를 확인했다(`artifacts/reviewed-lane-layout-red.log`). 최초 검사는 지역 미지정으로 실패해 은계를 고정한 뒤 다시 확인했다.
2. 1차 `PrototypeBuild.BuildHybrid` 종료0: 기존 차로·시나리오·주유소·OSM·교통·노면 검사와 새 차로 검사 통과(`artifacts/t1-pocket-build.log`). 이 빌드는 최종 중앙선 표시 수정 전 상태다.
3. 최종 별도 `ReviewedLaneLayoutTests.BuildChecks` 종료0, `REVIEWED_LANE_LAYOUT_TEST_OK edges=1771 pocketLength=24.99946 clearanceSamples=294 arrows=13` 및 `REVIEWED_LANE_CHECK_BUILD_OK`(`artifacts/t1-pocket-check-build-final.log`). 승하차132쌍 경로, 원래 좌회전 join 차단, 직진 차로 분리, NPC 좌회전 인식, 중복 적용 방지, 화살표 생성과 차량 폭 노면을 검사했다.
4. 최종 실행파일 `-reviewedLaneTrafficTest` 종료0. 공유 그래프 참조 일치, 차로/회전의 차량 충돌체98표본에서 건물 침범0. NPC2대 같은 좌회전 경로 각각1회 완료, 최소 경로상 간격11.99949m(`artifacts/t1-reviewed-lane-probe.txt`, `artifacts/t1-reviewed-lane-traffic.txt`). 이 검사는 기구학 NPC 통과이며 택시 물리 주행/신호 운영/수동 플레이는 아니다.
5. 최종 게임 렌더 `artifacts/t1-reviewed-lane-overview.png`, `artifacts/t1-reviewed-lane-approach.png`를 직접 열어 좌/직 화살표와 진입부 중앙선의 연속 표시를 확인했다.
6. `pwsh -File tools/check_workspace.ps1 -Strict` 종료0, `Input issues: 0`. 필수 생성 입력 지도3개·입력 manifest 해시 변경 없음.

## 빌드·동시 작업 상태

최초 배포 빌드는 자동 승인 검토가 사용자 변경 덮어쓰기 가능성으로 거절했다. 당시 배포 폴더 git status가 깨끗하고 2deaa0a의 manifest128파일 해시가 모두 일치함을 확인한 뒤 같은 빌드를 재승인받아 성공했다.

최종 표시 재빌드는 다른 채팅의 Unity 실행과 잠금 충돌로 실행되지 않았다(프로젝트 중복 열기 종료코드1073741845). 프로세스를 중단하거나 잠금파일을 삭제하지 않았다. 이후 잠금이 사라진 것을 확인하고 배포/FirstDrive와 분리된 체크 빌드를 완료했다.

기본 `builds/EungyeTaxi/`는 1차 로컬 검증 빌드 상태이고, 최종 중앙선 수정의 확정 검증본은 **`artifacts/T1PocketChecks/EungyeTaxi.exe`**다. 기본 폴더의 실제 파일을 재해시한 로컬 manifest에는 `sourceWorktreeDirty=true`, `buildStatus=local-validation-intermediate`를 명시했다. 배포 실행 폴더와 이 임시 manifest는 이번 소스 커밋에서 제외한다. 동시 작업 소스가 정리된 뒤 BuildHybrid를 통합 실행하고 배포 manifest를 확정해야 한다.

## 남은 범위

- T1 전체4방향 중 동남1방향의 좌회전 기능만 부분 적용했다. 다른3방향, 유턴, 별도 우회전 통로, 실제 치수·시점 정합·신호 운영, 승하차4 보호구역 표시와 나머지 동선은 미완료다.
- 실제 택시 저속 물리 회전·일반 교통을 포함한 수동 플레이·전체 캠페인 검사는 수행하지 않았다.

## 실행·재현

- 에디터 검사/별도 빌드: Unity 배치 `-executeMethod ReviewedLaneLayoutTests.BuildChecks`.
- 최종 실행: `artifacts/T1PocketChecks/EungyeTaxi.exe -reviewedLaneProbe`(고정 화면/충돌체) 또는 `-reviewedLaneTrafficTest`(NPC2대 통과).
- 검사 로그를 보려면 `-logFile artifacts/<파일명>.log`를 붙이고 저장소 루트에서 실행한다.
- 일반 은계 영업으로 확인할 때는 체크 exe를 인자 없이 실행한다. 최종 배포는 `PrototypeBuild.BuildHybrid` 이후 manifest 재생성/검증이 필요하다.
