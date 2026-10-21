# 교통 상황 강제 경유 제거 · 자연 운송 경로

## 요구와 기존 상태

사용자는 승차/하차를 상황 체험과 분리하는 것뿐 아니라, 교통 상황을 내비 경유지로 강제하지 말고 승하차 위치를 고려한 자연 경로에 배치하도록 정정했다. 직전 소스2525a54는 승하차 차단만 풀었으며 탑승 후 RequireVia와 코스 구성 Via가 남아 있었다.

작업 시작 시 README.md, docs/license-maintenance.md, docs/open-source-audit.md, docs/third-party-register.md, licenses/evidence/는 별도 변경이었다. 이번 커밋에서 제외한다.

## 실제 구현

- 12개 기존 정류장 간 일반 방향 최단 경로를 계산하여9개 코스를 구성한다. 기존 승하차 조합을 우선하며 적합한 상황이 없으면 기존 정류장 중 다른 조합을 고른다. 상황 공간을 일반 경로의 접근65m/출구20m 이상, 유효 회전·신호 교차로에 배치한다.
- 기존1180/1133/1351 연결은 어떤 기존 정류장 쌍의 일반 최단 경로에도 적합하지 않아 게임 상황 위치를 변경했다. 실제 도로 시설을 새로 확인했다는 의미가 아니다.
- 픽업은 승차지만, 탑승 후에는 목적지만 안내한다. 교통 상황용 Via/RequireVia 호출을 제거하고 통과 관찰은 ObserveEdge로 분리했다. 빠른 길/큰길/우회 재탐색에서 상황을 빼더라도 정상 승하차할 수 있다.
- PS08은 막힌 연결 제외 후 목적지로 일반 재탐색하며 대체 교차로도 강제 경유지가 아니다. 운임 기본 거리는 일반 경로 길이로 계산한다.
- 상황 미체험은 승객 목적지 변경 요청도 차단하지 않는다. 실제 상황 관찰/선택 중 안전상 보류는 유지한다. 승객이 요청하고 수락한 EV02 중간 정차 기능은 교통 상황과 별개로 유지한다.
- 상세 코스·위치·거리 표는 docs/content-design/09-prototype-scenarios.md에 갱신했다. 04 승객/07 캠페인/범위/체크리스트/맵 정비표도 수정했다.
- 비교 버전 eungye-day20-v2-natural. 기존 완료 기록을 지우지 않고 다른 코스 버전의 수익과 자동 비교하지 않는다.
- 내비·보조 운전·NPC가 공유하는 원본 차로 연결은 변경하지 않았다. 새 코스 대상으로 기존 도로 폭/인도/건물 제외 보정을 재사용한다. 필수 입력3개와 배포 지도ZIP 변경 없음.

## 변경 파일

Scripts: PrototypeScenario, HdRoadGraph, ServiceLawZones, RoadNavigation, ScenarioChoiceSituation, PassengerJobs, PassengerRequestUI, PrototypeState. 회귀: PrototypeScenarioTests, PrototypeScenarioProbe, RemainingPrototypeTest, ServiceLawZonesSmokeTest. 빌드 생성 FirstDrive.unity와 기본 실행 폴더, 배포manifest 및 위 기획/체크리스트/이력 문서.

## 검증

| 종류 | 실행·근거 | 결과 |
|---|---|---|
| 경로 조사 | PrototypeScenarioTests.Coverage / artifacts/natural-coverage.log | 기존3개 연결 자연 경로 후보0 확인 |
| 자동 데이터·빌드 | PrototypeBuild.BuildHybrid / artifacts/natural-reviewed-build.log | 종료0.9개 일반 경로와 기준 Route의 차로 목록 일치, 방향/우회/차로·교차로·NPC 검사 통과 |
| 지도 보정 | CorridorPolishTests | 115개 폭·768개 차로 노면·53,526개 표본·30개 서비스 연결, 그래프 불변/승객12곳 보도 확인 |
| 모델 | pwsh -File tools/test_prototype.ps1 | 21묶음 통과 |
| 위치 제어 운송 | -prototypeScenarioTest -hybridV2 -oneDayMode | 120항목 통과.9코스 승하차·경유 없음·PS08 선택·PS09 보행 관찰 |
| 미체험·요청·주유 | -remainingPrototypeTest -hybridV2 -oneDayMode | 74항목 통과.미체험 승하차·목적지 변경 제안·+10% 반복 주유·중복 지급 방지 |
| 승객17요청 | -passengerExpansionTest -hybridV2 -oneDayMode | 종료0,151항목 통과 |
| 하루 상태·저장 | -oneDayTest, -oneDayReloadTest -mitTrafficOn | 종료0.하루28항목/새 프로세스2항목 통과 |
| 위치 제어 상황 | -batchmode -nographics -serviceLawZonesTest -hybridV2 | 111항목 통과.7조건 실제 상태·위반/보류·통과 경계·텔레포트 불인정·정상 운임. 주변 차량은 격리 |
| 입력 무결성 | pwsh -File tools/check_workspace.ps1 -Strict | Input issues:0 |

위 로그는 artifacts/에 있으며 Git 배포 대상이 아니다. 비배치 숨김 검사 실행은 로그가 생기지 않아 그 검사 프로세스만 종료하고 배치로 다시 검증했다. 배치에서는 화면 캡처를 건너뛰며 화면 가독성 검증으로 계산하지 않는다.

## 최종 코드 리뷰

독립 읽기 전용 리뷰에서 PS09 실제 보행 상황 중 목적지 변경 허용 경계를 발견해 ActiveObservation 보호 조건과 회귀 검사를 추가했다. 재검토에서 중요 지적 해소를 확인했다.

## 실행·재현과 남은 작업

기본 builds/EungyeTaxi/EungyeTaxi.exe 실행, 은계 일반 영업에서 호출 또는 길가 승차. 상황을 지나지 않고 승차점/목적지에 올바른 방향·저속으로 정차해F 승하차 가능. 선택한 교통 상황은 정상 목적지 경로 위에 나타나며 돌아가서 체크할 경유지가 없다. 승객이 따로 요청한 중간 정차와 혼동하지 않는다.

이번 작업의 새 코스 자동 물리 완주·수동 하루20분·시야 확인은 하지 않았다. P7-06은 미완료 유지. 일반 교통 중3~5건 체험량·식사/수면 시점·연료/순수익, 상황 발생 여유와 실제 네이버 거리뷰 시설 대조가 다음 작업이다. 이전 흰 박스/보도/+10%주유/승하차 분리는 이미 구현되어 있으며 새 동선의 위치별 수동 시야 점검은 남아 있다.

소스 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-21-natural-service-routes.md`로 확인한다. 배포 실행파일의 소스·크기·SHA256은 distribution/EungyeTaxi-build-manifest.json을 따른다.
