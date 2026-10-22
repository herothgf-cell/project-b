# 식당 건물·진출입로·도로 밖 식사 주차 (2026-10-22)

## 요구사항·구현

남부 식당에 건물/간판/차량 진입·정차 공간을 추가하고 식당 앞 주차 후에만 식사하도록 요청받았다. 사용자 지정 블록 남측의 빈 부지를 원본 건물/도로와 대조해 선정했다. 원본 건물 철거·숨김 추가 없이 크림색 식당/적갈색 지붕·은계 밥집 간판/창문·문,진입/출구 안내·화살표·노란 주차 칸을 배치했다.

| 변경 | 파일/동작 |
|---|---|
| 독립 부지 | RestaurantSiteLayout.cs:남측 진행 차로780/기준거리45m,입구 안내27m,실제 칸은 도로 우측12m. 입·출구 곡선과10m 폭 주차 노면 |
| 건물/간판 | WorldBuilder가 RestaurantSiteLayout.Apply/Draw 호출. 건물/지붕 충돌체,유리/간판/주차선·안내 표시는 비충돌 장식 |
| 공간 검증 | FuelStationLayout.ValidatePlot 공통 검증 재사용,도로/건물/수면/지도경계 침범 검사. 원본 건물/HD연결 불변 |
| 식사 위치 | ServiceStops:MealStop은 입구 도로 접근,MealPosition/MealParkingStop은 내부 칸. AtRestaurant가 칸/방향/정차/높이 판정 |
| 내비·지도 | F7/하차 후 식당 안내는 입구 앞,전체/미니맵 핀은 실제 주차 칸. 내비/H는 도로 접근까지만,내부는 직접 운전 |
| 기존 검사 갱신 | OneDayCampaignProbe/CompletionSmokeTest는 내부 칸에서 식사. OneDayTrafficDriveTest는 접근 후 실제 내부 주행/식사/출구 단계 사용 |
| 신규 물리 검사 | RestaurantLotProbe:최초 접근 배치1회 이후 실제 페달/조향으로 진입·정차·식사·출구,일반 차량 유지 |
| 식당 회귀 | FuelStationLayoutTests/MapIdentityProbe:주차 판정/원본 보존/반복 적용/지도 노면수 갱신 |

식사3,000원/50초·포만감 회복·엔진 정지·취소 무차감 유지. 도로변/입구/칸 밖/이동/반대 방향/탑승 중 식사 불가. 식당 칸에서 주유·수면·영업마감 불가. 승객12지점과 별도 주차/휴식 거점 유지. 원본 그래프에 차량 지름길을 추가하지 않는다. 게임시설이며 실제 상호/필지 확정이 아니다.

## 수행 검증·결과

- 먼저 부지/주차 제한 테스트 추가 후 FuelStationLayoutTests.Run:RestaurantSiteLayout 미구현 컴파일 실패/종료1 확인.
- PrototypeBuild.BuildHybrid:기하 검사 통과 후 식당 물리 검사 코드를 추가한 최종 빌드에서 boot.config의 일시 파일 잠금으로 실패했다. 실행 중 Unity/게임이 없는 것을 확인하고 동일 코드 재시도,`artifacts/restaurant-lot-build-retry.log` 종료0. 소스/물리 정책 변경으로 우회하지 않았다.
- `pwsh -File tools/test_prototype.ps1`:23묶음 종료0.
- 기본 exe `-batchmode -nographics -restaurantLotTest -oneDayMode -hybridV2`:종료0,16 PASS. `artifacts/restaurant-lot-player.txt`, `artifacts/restaurant-lot-physics.log`. 일반 차량48대 이상 유지/입구 앞 안내·도로변 구매 차단/실제 진입·칸 정차/3,000원·50초/포만감 회복·연료 무소모/물리 출구/칸 이탈 후 식사 불가/건물·간판·주차 표시 생성/지도 핀 확인. 공통 물리 추종기의 양쪽 주행 완료와 측면·연석 접촉0 확인.
- 기본 exe `-batchmode -nographics -oneDayTest -hybridV2`:종료0,28 PASS. 새 내부 식사 위치/취소/정산/저장 회귀.
- 기본 exe `-batchmode -nographics -mapIdentityCheck`:종료0,eungye/2100×1750/노면1729/승객12. 식당 노면3개 증가,원본 HD연결·승객지점 보존.
- `pwsh -File tools/check_workspace.ps1 -Strict`:종료0/오류0. 필수 생성 입력3개/ZIP/OSM 변경 없음.

기하 검사·위치 제어·자동 물리 주행을 구분한다. 이번 물리 검사는 시설 접근에서 최초1회 배치한 후 실제 주행이며 전체20분/60분 캠페인이나 사람의 수동 운전을 대신하지 않는다. 사람의 수동 진입 체감·간판 실제 픽셀 가독성/다른 해상도·다른 PC는 미검증. 별도 일반 차량 혼잡 합류/전 방향 접근은 추가 플레이 확인 대상이다.

## 실행·인계

기본 `builds/EungyeTaxi/EungyeTaxi.exe` →빈차 정차 후 F7 식당 길 안내 →도로의 입구 앞 도착 →H 해제 →입구 화살표를 따라 노란 주차 칸에 같은 방향으로 정차 →F7 식사 →별도 출구에서 도로 차량에 양보. 지도 핀은 칸을 가리킨다. 기존 도로변에서는 결제가 안 된다.

변경 코드와 기획05/체크리스트/이력을 함께 커밋한다. 기존 README·라이선스·XMind·편집기 계획 미커밋 변경은 보존한다. 배포 실행폴더 크기/SHA256·실제 소스 커밋은 manifest 기준이다.

## 배포 검증

실제 기록/소스 커밋은 git log로 확인한 c2b57a6이다. 배포128파일/84,894,468바이트 SHA256 전수 검사 종료0. 실제 소스 전체 해시는 manifest에 기록했다.
