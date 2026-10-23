# 운전 편의·식당 메뉴와 UI 승인용 시안

## 요청과 범위

1인칭 흔들림,3인칭 회전 편의,법규 표시 기본 해제,잔액/순수익 표현,기사식당 메뉴,메인/M 지도 UI 개편 요청. 사용자는 **메인·M UI 배치는 시안 승인 후 개발**, 다른 기능은 바로 개발하도록 지정했다. 기존 README/라이선스/XMind/편집기 계획과 LATEST 사용자 추가는 보존했다.

## 구현

| 변경 | 실제 반영 |
|---|---|
| DrivingCamera | 자동 코너 시선 기본 false,선택 시18→6도. 차체 롤/피치 분리, yaw14/s 평활. 수동 둘러보기/후방 유지 |
| DriveMath/TaxiController | 수동 저속 최대47.88도(상한48),추가14%를14~30km/h에서 감쇠. 30이상 기존 한도,H 기존 조향 계산 유지. 시점별 성능 차이 없음 |
| FullMapOverlay | ShowLaw=false 기본. 사용자가 활성화 가능,법규 판정/경로 변경 없음 |
| ServiceStops | 식사 확인 중3메뉴 클릭 선택. 비용/시간/효과 공통. 선택만으로 결제 안 됨,기존 확정 경로 재사용 |
| 잔액 | 기존 Account.Cash에 입출금 즉시 반영됨을 모델 검사. 새 중복 장부를 추가하지 않음. 표시 개편은 승인 대기 |
| UI 시안 | docs/ui/2026-10-23-cockpit-map-preview.html 및 주행/지도 PNG,설계13. 실제 제품 배치 미변경 |

## 검증

- `pwsh -File tools/test_prototype.ps1`:23묶음 종료0,수동조향25검사. 현금/식사/중복결제/저장 회귀 포함.
- `pwsh -File tools/check_workspace.ps1 -Strict`:종료0,입력 오류0. 필수 입력3개/ZIP 변경 없음.
- Unity `PrototypeBuild.BuildHybrid`:첫 실행 라이선스IPC 연결 실패199,서비스 연결 권한으로 재시도 종료0. `artifacts/comfort-menu-build-retry.log`.
- 실행파일 `-manualHandlingTest -hybridV2 -batchmode -nographics`:종료0. 물리 좌/우90도 반경2.6936/2.6904m,기울기0.333도. 90km/h 조향기울기1.144도,기존주차장45m 출발 측면접촉0. 1인칭 시선/롤 검사 통과. 별도 평지 물리 검사이며 수동 은계 플레이가 아니다. `artifacts/comfort-manual-physics.log`.
- 실행파일 `-restaurantLotTest -oneDayMode -hybridV2 -batchmode -nographics`:종료0,20검사. 일반차량48대 유지,식당실제 진출입 물리/주차·메뉴3선택·부정index·확정1회 결제 통과. `artifacts/comfort-restaurant-physics.log`, `artifacts/restaurant-lot-player.txt`.
- 시안PNG 로컬 렌더/육안 검토. HTML 브라우저 자동 확인은 file URL 정책 차단으로 미수행. 브라우저 우회 없이 정적 배치 그림을 제시했다. 아이콘 글꼴 빈문자는 PNG에서 제거하고 최종 UI는 자체 도형으로 계획했다.

## 남은 확인과 재현

- 실행: `builds/EungyeTaxi/EungyeTaxi.exe`. C 1/3인칭, V 선택적 코너 시선,저속 A/D 회전. 식당 내부 칸 정차→F7→식사 비용 확인→메뉴→확정.
- 메인/M 시안 승인 후 실제 UI/호출핀·목록 포커스·위치팝업·아이콘을 구현. 승인 없이 배치 적용하지 않는다.
- 사람의 멀미/조작감,클릭 가독성,전체3일/20분 실주행·모든교차로는 미검증. 메뉴별 가격/회복 차별화는 현재 범위에 넣지 않았다.
- `docs/content-design/05-daily-operation.md`, `06-world-map-and-driving.md`, `13-driving-map-ui.md`와 체크리스트 갱신. 실행폴더 변경 후 배포manifest 별도 갱신/해시 검증한다.
