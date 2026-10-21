# 승차 독립·보도·10% 주유·우회전 대기 수정

## 요구와 구현

| 요구 | 실제 변경 |
|---|---|
| 교통 상황과 승하차 분리 | PS01~09 모두 상황 통과 없이 승차/하차 가능. 픽업 제한 시간은 직접 접근 경로로 계산. 상황은 운송 경로 안내이며 미체험은 감점하지 않는다. 승객이 별도 수락한 경유 요청은 유지한다. |
| 도로 흰 박스 제거 | 건물 윤곽을 공유 차로 노면과 원본 도로 폭·보도 여유에 대조하여 침범 건물의 표시/충돌을 제외. 도로 밖 건물은 유지하며 정체 표식이 아니다. |
| 승객 보도 배치 | 12개 승객 위치를 도로·건물 밖 보도에 배치. 차량 정차 기준과 승객 표시 위치를 분리했다. |
| +10% 주유 | 실제 빈 용량 한도 내 10%포인트 버튼, 10%당 400원. 반복 구매 가능하며 창은 유지한다. F7로 닫고 출차한다. |
| 시나리오 주변 정비 | 9개 코스·우회 경로·픽업 접근을 포함하여 126개 도로 폭과 828개 차로 노면에 공통 보정 적용. 내비/NPC 연결 정보 유지. |
| 녹색 우회전 대기 | 같은 우회전 경로의 이동 앞차를 교차 차량으로 판단하던 조건 수정. 앞차 간격·정지 출구·보행자 안전 조건은 유지. |

지도 변경표는 [맵 정비 계획](../map-polish-plan.md), 직접 확인 절차는 [플레이 가이드](../prototype-playtest-guide.md)에 추가했다. 실제 거리뷰 미확인 구간은 임시 게임 설정이며 현장 시설 확정이 아니다.

## 변경 파일

- `PassengerJobs.cs`, `PrototypeScenario.cs`, `ServiceLawZones.cs`: 승차 독립과 경로.
- `CorridorPolish.cs`, `SceneBuildingVisibility.cs`, `MapData.cs`, `RoadEnvironment.cs`, `config/maps/eungye-corridor-polish.json` 및 동일 Unity Resource: 노면·건물·보도.
- `ServiceStops.cs`: 10% 주유 UI.
- `TrafficSystem.cs`: 동일 우회전 흐름 판단.
- `RightTurnWaitingTests.cs`, `RightTurnQueueProbe.cs`, 기존 주유·캠페인·코스 검사: 회귀 검증. `PrototypeBuild.cs`에서 우회전 회귀 검사 실행.
- 관련 콘텐츠 04/05/06/09, 체크리스트·범위·이력·배포 실행 폴더/manifest.

## 실제 수행 검증

Unity 2022.3.62f2, 기본 `PrototypeBuild.BuildHybrid`, 은계 실행파일. 아래 명령은 저장소 루트 기준이다. 실행 검사는 `builds/EungyeTaxi/EungyeTaxi.exe -batchmode -nographics` 뒤 해당 인자를 추가했다.

| 검사 | 결과·검사 종류 |
|---|---|
| `tools/test_prototype.ps1` | 종료 0, 20개 모델 검사 통과 |
| `tools/check_workspace.ps1 -Strict` | 입력 오류 0, 필수 원본 3개 변경 없음 |
| `PrototypeBuild.BuildHybrid` | 종료 0, 9개 코스·56,976개 노면 표본·30개 서비스 연결 검증 후 빌드 |
| Editor `RunConvoy` | 수정 전 25/25 이동 앞차 뒤 진입 차단 재현. 수정 후 25개 이동 허용·25개 정지 출구 차단·25개 보행자 차단 통과 |
| `-remainingPrototypeTest -hybridV2` | 종료 0, 56개 검사. 미체험 승하차·10% 반복 구매/가격/가득 상태 포함. 위치 제어 검사 |
| `-prototypeScenarioTest -hybridV2` | 종료 0, 9개 코스·우회·보행자·픽업 시간 검사. 위치 제어 검사 |
| `-corridorPolishTest` | 종료 0, 8,897개 표본의 건물 충돌 0, 승객 12곳 도로 밖. 정적 배치 검사이며 전체 수동 주행 아님. 8번 승객 캡처 시각 확인 |
| `-fuelFacilitiesTest -hybridV2` | 종료 0, 주유소 3곳 물리 자동 주행으로 입구→주유→분리 출구, 측면 접촉/연석 충돌 0 |
| `-rightTurnQueueTest -hybridV2` 및 `-mitTrafficOn` 추가 | 각각 종료 0, 실제 도로 우회전 26곳 2대 대기열 물리 주행, 실패/겹침 0 |
| `-districtTrafficTest -hybridV2` 및 `-mitTrafficOn` 추가 | 각각 종료 0, NPC 48대 주행, 경로 이탈/겹침/장기 정차 0, 적색 3대 대기 후 녹색 통과 |

증거는 로컬 `artifacts/service-traffic-release-build.log`, `remaining-prototype-test.txt`, `prototype-scenario-test.txt`, `corridor-runtime-test.txt`, `fuel-stations-physical-test.txt`, `right-turn-queue-off.txt`, `right-turn-queue-test.txt`, `district-traffic-off.txt`, `district-traffic-test.txt`에 남겼다. artifacts는 Git 배포 대상이 아니다.

## 남은 확인과 인계

- 사용자 캡처의 교차로 ID는 확인되지 않았다. 동일 실패 원인은 실제 25개 접근로에서 재현했지만 캡처 위치 수동 재현을 완료했다고 주장하지 않는다.
- 전체 캠페인 수동 플레이/연료 밸런스 및 실제 거리뷰 차로·규제 확정은 별도다.
- 별도 작업에서 커밋한 하루20분·운전자 컨디션 기획은 보존했다. 이번 실행파일에 그 미래 기획을 구현했다고 표시하지 않는다.
- 필수 지도 입력과 지도 ZIP은 변경하지 않았다. 다른 PC는 pull 후 기본 은계 exe 실행. 기존 실행 중 게임은 종료 후 다시 실행해야 새 코드가 적용된다.
- 배포 파일별 크기·SHA256과 빌드 소스 커밋은 `distribution/EungyeTaxi-build-manifest.json` 참조. 이 문서의 실제 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-21-pickup-sidewalk-right-turn.md`로 확인한다.
