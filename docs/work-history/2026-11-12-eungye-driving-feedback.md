# 2026-11-12 은계 도로·교통·생활 제보 수정

## 요청과 거리뷰

- 은계로 J8066_6364 남쪽에서 북쪽으로 진입하는 `(808.5342, 592.396545), 2.8096°`에서 차로 역할, 유턴 노면, 방향별 차로 증감을 맞춘다. 사용자 첨부와 [네이버 2026-05 최신](https://map.naver.com/p/entry/coordinates/3zao1h,2AGJhI?c=15.00,0,0,0,adh&p=VzXWr0yZ-kjmidpHNnS74w,0,-10,80,Float), [2024-03 과거](https://map.naver.com/p/entry/coordinates/3zao1h,2AGJhI?c=15.00,0,0,0,adh&p=OPhaEYvuF9eY7xL5pSnMwA,0,-10,80,Float)에서 북행 5개와 반대 방향 3개, 북행 유턴 표지 및 차로 배열을 대조했다. 포켓 길이와 폭은 게임 치수다.
- J1927_5181 `(201.78096, 545.7985), 212.90007°`는 북동쪽에서 남서로 내려오는 접근이다. [2026-05](https://map.naver.com/p?c=19.00,0,0,0,adh&p=cWFgUsKpLaYz-MMUKXztag,212,-8,75,Float)와 [2025-10](https://map.naver.com/p?c=19.00,0,0,0,adh&p=7XXct_43ETtFHMXb3YDFig,-148,-8,75,Float)은 수인로 촬영점에서 본 사선 뷰라 해당 접근의 현장 신호 단계까지 판독하지 못했다. 게임에서 해당 방향의 기존 수인로 동향 연결 `A222BC004192:0`이 직진으로 분류된 오류를 고쳤다. 별도 서향 접근의 기존 좌회전 금지는 유지한다.
- 일반 차량의 저속·정체, 지나치게 짧은 보호좌회전, 관계없는 보행자로 인한 감점 및 늦은 출발, 구토 청결 감소 알림, 수면 후 피로 회복을 함께 수정한다.

## 구현

- `config/maps/project/eungye.project.json` r15. J8066 북행에만 두 차로가 갈라져 정지선 앞에서 **좌회전·조건부 유턴 / 직진 / 직진 / 직진 / 우회전**이 된다. 기존 직진 연결 두 개의 시작을 새 분기에 맞추고, 종점·좌/우회전·유턴 곡선과 반대 방향 차로는 유지했다. 흰 유턴선은 첫 대향차로와의 중앙을 사용한다. 기존 길과 차로 순서를 유지하고 새 차로 두 개는 끝에 추가했다.
- `TrafficSystem`, `TrafficSignalDemand`, `DriveFlowPolicy`, `DemandSignalClock`. 포털에서 연결되는 기존 좌회전 곡선을 보호좌회전으로 인식한다. 방향지시등 또는 현재 내비의 회전으로 수요를 감지하며 실제 정지선을 기준으로 접근을 찾는다. 은계 싱글 생성 보호좌회전은 녹색 기본 8초·대기 3대부터 최대 12초다.
- `TrafficCar`, `TrafficDriverProfile`. 은계 싱글의 직선 자유 순항은 여유/보통/성급 약 36/44/47km/h. 급곡선·앞차·신호·제한속도·날씨가 우선한다.
- `AmbientPedestrianSystem`, `DrivingLaw`. 일반 보행자의 실제 횡단 시간에 맞춰 생성 보행 녹색을 확보하고 시간이 부족하면 다음 녹색까지 대기한다. 싱글 일반 우회전의 보행 감점은 택시가 지나가는 부분과 가까운 정상 횡단에 한한다. 적색에 진입한 보행자도 물리 접촉과 안전 정지 검사에는 포함한다.
- `VehicleCare`, `PassengerJobs`, `ServiceStops`, `EconomyLedger`. 구토 발생과 동시에 청결도를 최대 10으로 낮추고 승객 카드에 감소량을 알린다. 하차와 저장 복원에서 중복 감소를 방지한다. 무료 수면은 영업 종료 경계를 넘어도 시작 피로에서 최대 40을 회복하고 완료 알림에 전후 값을 표시한다.
- `BusinessProgress`. 기본 r15 프로젝트에서 r14의 캠페인 저장을 이어받는 전환을 추가했다. 영향받는 두 교차로에서 진행 중이던 신호 판정만 다시 관찰한다. 정식 지도와 개인 수정본은 별도 검증한다.

## 검증 및 재현

- `pwsh -File tools/check_workspace.ps1 -Strict` 종료 0, 입력 문제 0. 정식 지도 내용 해시 `4dfb8e75ec93b8dc0f2749b51cfd5a8aba7a93f2d64d22e04ec299cf6d5bddac`, 파일 SHA256 `e85ee65675c1925c072293c640dd0927068286969f5a6c1fe4b3ce5026239b94`. OSM 원본 보완과 도로 연결 대조 입력은 수정 대상에 포함되지 않았다.
- Editor의 `NativeLeftSignalRegressionTests.Run`, `TrafficSignalLeftDemandRegressionTests.Run`(72), `LaneDirectionPaintTests.Run`(49 글리프 조합, 315 차로), `DemandSignalChecks.Run`(1,810) 통과. 방향지시등 켬/끔으로 J1927 제보 좌표의 좌회전 요청을 확인했다.
- `tools/test_passenger_care.ps1` 13,301, `tools/test_vehicle_damage.ps1` 191, 피로 경계 모델 25 통과. `tools/test_prototype.ps1`는 기존 `ManualHandlingTests` S 제동 8.823529 기대값 실패로 종료 1.
- `-liveSaveTest`로 이전 출하본 r14에서 실제 운송 저장 9검사, 새 r15 실행본 `-liveReloadTest`로 이어하기 및 다음 날 이월 23검사 통과. `-fatigueOvertimeTest` 위치 제어 및 휠 입력 검사 통과.
- 화면 재현은 기본 실행 파일에서 `-brightUiCheck -poseReview "808.5342,592.396545,2.80963969;201.78096,545.7985,212.90007"`를 사용한다. 실제 주행은 `-batchmode -nographics -oneDayMode -hybridV2 -oneDayTrafficTest`로 별도 검증한다.
