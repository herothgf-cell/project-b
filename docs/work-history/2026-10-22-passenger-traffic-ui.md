# 2026-10-22 승객 효과·교차로 흐름·표시 통합

## 요구와 기존 변경

승객 요청 개정04/11 적용, 내비 분 표시, 직진 차로 차량이 한 대씩 진입하는 문제·T2 끊김, 승객 만족도 용어, 보행 신호, 하차 후 생활 목적지, 신호함 위 비보호 표지, M 현재 위치/복사, 한글 표지 경계를 요청받았다. 사용자 승인을 받아 동시 T1 도로 정비(c1e00b4)도 통합한다. 시작 시 README/라이선스 문서·evidence, 승객 기획04/11·관련 이력이 미커밋 상태였다. 요청과 관련된 승객 기획만 함께 반영하고 라이선스 변경은 보존한다.

## 실제 구현

|요구|구현|주요 파일|
|---|---|---|
|승객 요청 효과|ETA Y/I/O·U 보류, DR01 수락 이후 -4점 대체/필요한 제동 제외, 범주별 Δ·최종 S와 조건부 10원 단위 비례 감사 보너스|PassengerRequest/UI, DrivingManners, PassengerJobs, BusinessProgress, PrototypeState|
|결과·저장|기본 운임/보너스/합계, M+Δ=S·요구/초과 점수·판정 이유, F8 가변 높이·이전 저장 대응|DrivingLaw, DriveHUD, PassengerResult|
|내비·지도|ETA 분 단위, M 지역/정확 x,z/방향/최근접 교차로 표시·선택·복사|RoadNavigation, DriveHUD, FullMapOverlay|
|동시 진입|전체 교차로 독점 대신 경로 충돌 캐시 비교, 평행 직진 함께 진행, 정지 앞차·막힌 출구·교차 경로 차단|TrafficSystem, TrafficFlowTests|
|T2 처리|차량 투영 범위를 이웃65m로 제한, 직진의 불필요한 우회전 양보 계산 제거, 신호 재질은 상태 변경 시만 갱신, 표지 문자 재질 공유|TrafficRoute/TrafficSystem, RoadSignLabel, TrafficPerformanceProbe|
|보행 신호|횡단보도 양쪽 신호기와 보행/정지 표시, 일반 임시 신호 차량 구간 이후 보행10초·정리2초; 시나리오 관찰 우선|PedestrianSignalDisplay, TrafficSystem|
|표지|해당 신호함 위 비보호 표지, 중복 도로변 안내 제거, 실제 한글 렌더 경계를 표지 안에 맞춤|KoreanText, RoadSignage, TrafficSystem|
|하차 후 생활|컨디션/방향 경로 거리로 주유·식사·수면·귀가 추천, 직접 선택 시 안내, 새 호출·닫기로 계속 영업|ServiceStops, PostTripServicePolicy|

승객 만족도는 기본 M과 하차 S를 기록하고 누적 운행 점수와 분리한다. 기본 운임100%/70%/시간 초과0원, 기존9코스·17요청과 강제 상황 경유 없음 유지. DR02/03/04에서 합법적 대기/우회 여부를 기존 관찰로 확정할 수 없는 미달은 중립이다. CH04/05·EV03/04는 보너스 제외. 실제 음악·분실물 반환·개인 예약·보험·세금은 추가하지 않았다.

## 수행한 검증

- 모델22묶음 exit0: 승객 비례 보너스 예시260원/상한/제외, ETA 선택·오차, 조건부 감점, 중복 방지 및 생활 권고 정책 포함.
- TrafficFlowTests: 수정 전 평행 녹색 차로가 전체 교차로를 기다려 실패(exit1), 수정 후 동시 진입·평행 점유·막힌 출구·교차 경로·보행 구간·투영 검사 통과. 리뷰에서 같은 경로 정지 앞차의 내부 점유 누락을 찾아 추가 거리55 회귀로 보완했다.
- MapReadabilityTests: 분 올림·계산 불가, 문화권 독립 원본 x/z와 방향·근접 교차로, 한글 렌더 실제 경계 검사 통과.
- 17종 요청 위치 제어 통합 검사 exit0/255 PASS: 실제 DrivingManners 분기에서 DR01 -4점 단일 감점, 운임+보너스 장부, 같은 운송 중복 지급 방지, 결과 JSON 필드 복원, 하차 후 생활 목적지 선택/내비 연결. 위치/속도·관찰 상태를 제어한 검사이며 연속 물리 운송이나 수동 주행이 아니다. 로그 artifacts/passenger-effects-runtime.log, passenger-expansion-test.txt.
- T2 J6199_11987 인근 최근접 공유 접근 차로 고정/48대/Direct3D11/30초: 평균59.955FPS, p99 17.1099ms, 차량 FixedUpdate 합계2365.5ms/30초. artifacts/t2-performance-final.log, traffic-performance.txt, t2-road-review.png. 수정 전 같은 T2의 수치는 없어 개선 비율을 주장하지 않는다. 전체 구간 이동·수동 회전 성능은 미검증이다. 첫 진단의 방향 가정으로 차로 검색 실패한 경우를 수정해 근접 공유 차로를 직접 선택하도록 했다.
- 통합 BuildHybrid는 기본 은계 빌드이며 T1 동남 좌회전 차로 정비와 위 검사 포함. 최종 로그 artifacts/passenger-traffic-ui-verified-build.log.
- Strict 입력3개 오류0/원본·ZIP 변경 없음. 실행 폴더 배포 시128파일의 실제 크기/SHA256을 재생성한다.

## 미검증·다음 작업

일반 차량 포함 수동20분/다건 수익, 사용자가 실제 제보한 이동 경로와 PC 설정의 끊김, 전체 교차로 대기열, 실제 보행/차량 신호 운영 현장 대조, 거리별 표지 읽기를 이어간다. 보행10초는 게임 임시 운영값이며 현장 신호 확정이 아니다. DR02/03/04 판정 불가를 실제 우회 원인 관찰로 정교화한다. 숨김 창의 ScreenCapture는 검은 결과여서 전체 HUD 픽셀 검증 완료로 표시하지 않는다. 카메라 수동 렌더는 주행 장면만 보장하며 UI 배치는 별도 검사다.

## 실행·재현

기본 builds/EungyeTaxi/EungyeTaxi.exe. M에서 위치를 복사해 제보한다(게임 로컬 미터 좌표, 위경도 아님). 승객 요청은 정차 상태 Y/I/O 및 U, 하차 결과는 F8, 생활 거점은 하차 선택 또는 F7. 재현 옵션은 -passengerExpansionTest -oneDayMode -hybridV2 및 -trafficPerformanceTest -trafficT2 -oneDayMode -hybridV2 -mitTrafficOn. 최신 실제 커밋은 git log -1 --oneline -- docs/work-history/2026-10-22-passenger-traffic-ui.md로 확인한다.

최종 UI 기능 검사: artifacts/passenger-traffic-ui-screen-final.log 종료0, 1280/1600/1920 지도 확대·필터·시간 정지·정확한 현재 위치 클립보드 복사 검사 통과. 최초 검사는 기존 시설5곳 기준으로 휴식 구역을 누락해 실패했으며, 휴식 포함6곳 및 복사 실제 경로를 추가해 재통과했다. 이 결과가 검은 캡처의 픽셀 가시성 검증을 대신하지는 않는다.

최종 기본 실행 -mapIdentityCheck 종료0: 은계2100×1750/노면1674/승하차12/주유소3/휴식1. T1 정비 노면도 포함하며 기본 은계 빌드에서 확인했다.
