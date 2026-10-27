# P11~P13 최종 안정화와 main 통합

## 요구와 기준

사용자 요청: 남은 검증·발견 오류 수정·안정화 후 main 병합 및 push. 기존 `codex/proto-p11-p13`에서 이어서 진행한다. 수동 체감과 자동 물리·위치 제어 검사를 구분한다. 사용자 마인드맵과 기존 미추적 파일은 보존한다.

## 진단 진행

- 기존 물리 운전기는 직진 신호만 보고 좌회전하며, 추가 포켓의 3.5m 연석 이동을 수행하지 않는다. 게임의 신호/승차 조건을 완화하지 않고 운전기와 판정 데이터를 대조한다.
- 분리된 좌/직 신호 fixture로 자동 운전기의 적색 좌회전 진입을 먼저 재현한다.
- 기존 시작 화면 캡처를 실행해 숨긴 창 검정 PNG의 원인을 구분한다.

## 확인된 원인·수정

- 자동 운전기가 좌회전을 직진 신호로 판정했다. 분리 좌/직 신호 fixture 실패 후 진행 방향별 신호·편집 정지선·차로 접근·양보 조건으로 수정, 집중 검사0.
- 추가 포켓 진입을 생략하던 운전기에 별도 저속 페달/조향 진입·복귀를 추가했다. 첫 연속 주행에서 포켓 승차/하차·복귀, PS07 운임8,220원/매너100/충돌0·신호0·급제동0, 식당 진입·식사·출차까지 통과했다. 이후 주유 경로 선택 실패로 전체0은 아니며 출구 실제 위치/방향을 별도 물리 검사로 추적한다.
- 숨긴 창은 로딩부터 검정 픽셀, 실제 창에서는 로딩/준비/출발8단언0. 새 지도도 실제 창1280/1920에서 확인했다. 비동기 파일 캡처 대신 프레임 픽셀 직접 캡처로 보완했다.
- 실제 지도 화면에서 ‘일시 점유’ 마커가 시설 이름 필터에 걸려 그려지지 않음, 아래쪽 하차 라벨 잘림을 확인하여 수정했다. 수정 후 최종 화면 재확인 예정.
- 날짜 전환 시 전날 점유 사건/재발생 시계가 남는 문제를 회귀 검사에서 재현(종료1)한 뒤 날짜·캠페인 ID·시계 초기화로 정리했다. 저장에 소유 날짜/캠페인 추가, 구형 누락은 현재 경계 사용. 날짜 및 명시 새 캠페인 회귀0.
- `StabilizationRegressionTests.RunFinal` 종료0: 신호 fixture, P11/P12/P13·기존 리뷰 회귀·저장500회/잠금·주유·보행·지도/경로·live JSON. 이후 캠페인 초기화 추가분은 `RunFollowup` 종료0.

## 현재 로그

`artifacts/stabilize-signal-red.log` / `stabilize-signal-green.log`, `stabilize-day-red.log`, `stabilize-editor-final.log`, `stabilize-restaurant-exit.log`, `stabilize-physical-day.log`, `stabilize-visible-startup.log`, `stabilize-ui.log`. 검사 도구의 fixture 초기 문법/문맥 오류는 수정하고 기능 실패와 구분했다.

최종 결과·배포 및 원격 커밋은 실제 실행 후 아래에 추가한다.

## 추가 확인

- 식당 출구 형상 종점 자체는 올바른 차로에 연결된다. 실제 자동 운전기는 곡선 마지막0.85m 앞에서 멈추어 차로와의 방향 내적0.577(그래프 요구0.6)에 머물렀다. 출구 뒤 도로12m를 실제 조작으로 진행해 정렬하도록 수정, 짧은 물리 회귀0. 지도/게임 경로 조건은 유지.
- 신규 복원11·3일 저장/복원·기존 live9/23·승객201 모두 최신 점유 필드로 실행0. 모델24묶음·care13,301도 재확인0.
- 병합 전 read-only 리뷰1회: Critical/Important0, Minor1(캡처 실패 시 검사 프로세스가 끝나지 않을 수 있음). 캡처 iterator의 예외를 포착해 실패 종료·지도 정리를 보장하도록 보완. 재리뷰 반복 없이 마지막 정상/의도 실패 검사를 수행한다.
- 리뷰에서 판단을 유보한 최종 물리 하루/UI/manifest/원격 상태는 본 작업의 실행 결과로 확인한다. 사람의 조작감·20분/3일 경제 체감은 자동 검증과 구분한다. 전역 모든 교차로/승객/보행 상호작용과 기존 P11~P13 전체 재리뷰는 이번 변경 검토 밖이며 기존 회귀 증거를 유지한다.
- 지도 점유 라벨·하차 버튼 겹침을 해소한1280/1920 PNG 시각 확인. 실행 시작/준비/출발도 실제 창에서 정상 표시.
- 두 번째 연속 물리 검사에서 운송1·식사1·주유1·수면1 및 각 시설 입출차, 충돌/적색 매너/급제동/독립 법규 감점0을 확인했다(1,811.26게임초/5.89km). 다만 옛 검사의 ‘휴식 거점 마감 불가’ 단언과 주차장 내부 좌표를 H 차로 종점처럼 사용한 마지막 동선 때문에 전체 종료1. 실제 정책은 휴식 거점 마감을 허용한다. 검사 종료점을 실제 도착한 휴식 주차 칸으로 변경하고, 거점 마감 가능 조건을 현재 기획과 일치시켰다. 성공 기준을 삭제하지 않고 실제 위치의 마감·정산·저장까지 재검증한다. 이전 시설 입출차 증거는 `stabilize-physical-facilities-before-closing.txt`에 보존했다.

## 최종 기술 검증

- `-oneDayMode -hybridV2 -oneDayTrafficTest` **종료0**. 48대 실제 교통, 위치/회계 주입 없이 주차장 출발→포켓 승차/하차→식당 입출차/식사→주유소 입출차/+10% 주유→휴식 주차→예정20분 초과 영업 후 명시 마감·정산·저장. 1,630.64게임초/4,514.49m, 운송1·식사1·명시 수면1·주유1, 수입8,220원·순수익4,320원, 충돌/적색 매너/급제동/독립 법규 감점0. 마감 시 추가 휴식은 게임 정책대로 적용한다. `stabilized-physical-complete.txt`, `stabilized-physical-driver.txt`, `stabilized-physical-checkpoint.json`.
- 마지막 휴식 거점은 영업 종료 장소이므로 출차하지 않는다. 같은 수정 운전기의 별도 연속 검사에서 휴식 거점 출차도 통과한 증거를 위 이전 기록에 유지한다. 자동 주행을 사람의 수동 플레이로 표시하지 않는다.
- 캡처 의도 실패 `-protoExpansionTest -protoCapture -protoCaptureFail` 실제 종료1, 멈춤 없이 실패 보고/프로세스 종료 확인. 정상 지도 캡처10단언은0. `stabilized-capture-failure.log`.
- 빌드 중 이전 실행 파일의 메모리 매핑이 남아 asset 복사 실패1회를 확인했다(`stabilized-build.log`). 모든 Player 프로세스 종료를 확인한 뒤 재빌드하여 `PrototypeBuild.BuildHybrid` **종료0**(`stabilized-build-clean.log`). 실패 빌드를 배포 확정하지 않았다.
- 실제 물리 검사와 최종 정상 빌드의 `Assembly-CSharp.dll` SHA256은 모두 `352F953948D794EF655EC9D77850F59F4525520623625838EB794229178C393E`. 최종 소스는 `1d068bae198e146af0ee57664e6d1c18dced6232`. 씬의 빌드 생성ID 변경은 동일 설정이며 최종 실행 스모크/manifest를 추가 확인한다.
- 필수입력 Strict0/선택 import2개 부재. 마인드맵 SHA256 `F1951492F9C5A13941E60A17D92EB31B1A7471FB8ABA7996D47A0C2E02F00937` 유지. 지도 입력/ZIP 미수정.

실제 렌더링 단독 성능 검사 **종료0**: Direct3D11/1280×720/J5057 정차/48대 교통, 300.0154초/14,827프레임, 평균49.4208FPS, p50 19.2319ms/p95 24.9534ms/p99 34.6192ms, GC0 107회. NPC 누적 이동69,264.55m, 관리 메모리127,250,432→141,778,944바이트(강제GC 없는 두 경계값이며 누수 부재를 증명하는 수치는 아님). 다른 Player 검사와 겹치지 않게 실행했다. `stabilized-performance.txt`/`.log`. 정상 실행 스모크의 시작 준비: headless14,347ms, 렌더13,596ms 단일 표본이며 냉시작 보장/모든 구간60FPS 주장은 하지 않는다.

최종 정상 빌드의 손상 저장 기본/백업 보존·출발 금지·명시 재시작4단언도 **종료0**(`stabilized-corrupt-save.log`). 기술 검증에서 발견된 게임 오류 및 검사 도구 오류의 수정/재검증을 마쳤다. 사람의 수동 조작감·경제 체감, 실제3일 전체 물리 주행·다른PC/실물 패드는 자동 결과와 구분해 남긴다. P13 안전 대체 이동은 현재 지도 범위에서 제외한다.

## 변경 파일과 재현

게임: `StopOccupancyPolicy.cs`, `StopOccupancySystem.cs`, `FullMapOverlay.cs`. 검증: `LiveTrafficValidationTest.cs`, `OneDayTrafficDriveTest.cs`, `ProtoExpansionProbe.cs`, `TrafficPerformanceProbe.cs`, `FacilityExitProbe.cs`, Editor `StabilizationRegressionTests.cs`, `ProtoReviewRegressionTests.cs`. 관련 체크리스트·기획·플레이 가이드·LATEST 및 기본 은계 실행폴더/manifest를 함께 갱신한다. 아래 파일은 모두 `unity/EungyeTaxi/Assets/` 기준이다.

- Unity `-batchmode -nographics -quit -projectPath <unity/EungyeTaxi 절대경로> -executeMethod StabilizationRegressionTests.RunFinal`.
- 기본 exe `-batchmode -nographics -oneDayMode -hybridV2 -oneDayTrafficTest`로 연속 물리 하루.
- `-protoExpansionTest` → 별도 프로세스 `-protoExpansionReloadTest`; `-liveSaveTest` → `-liveReloadTest`; `-threeDayTest` → `-threeDayReloadTest`; `-passengerCareTest`.
- 실제 창 `-protoExpansionTest -protoCapture`로1280/1920 지도 픽셀, `-campaignStartupTest`로 로딩/준비/출발 화면. 숨긴 창/`-nographics`는 시각 검증에 사용하지 않는다.
- 실제 창 `-trafficPerformanceTest -trafficPerfSeconds 300 -oneDayMode -hybridV2 -screen-width 1280 -screen-height 720 -screen-fullscreen 0`로 단독 성능. 물리 하루와 동일 검사 저장 경로를 사용하므로 동시에 실행하지 않는다.
- `-restaurantExitTest -oneDayMode -hybridV2`는 출차만 분리한 물리 회귀(초기 주차 칸 배치1회).
- 각 실행에 `-logFile <artifacts 내 절대경로>`를 지정한다. 검사 저장은 artifacts에 격리하며 실제 사용자 저장을 손상 fixture로 바꾸지 않는다.
- 최종 입력/배포: `pwsh -File tools/check_workspace.ps1 -Strict`, `pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly`.

배포 파일 SHA256과 main/원격 결과는 아래 통합 기록을 따른다.

## 배포

소스 `1d068bae198e146af0ee57664e6d1c18dced6232`, 기본 `builds/EungyeTaxi/EungyeTaxi.exe`, 은계 BuildHybrid0. `distribution/EungyeTaxi-build-manifest.json`을 실제 검증 보고서로 갱신하고130파일/105,694,422바이트 전체 SHA256 대조 **종료0**. 사람의 수동 플레이/3일 전체 물리는 false로, 연속 물리 하루·기술 안정화는 pass로 명시했다. 구현하지 않은 안전 대체 이동도 wait-only로 기록했다.

## main 병합·원격 확인

- 배포 커밋 `a6f65cab34327d01326021b13d3b8df36a605261` 생성 후 origin/main의 조상 관계를 다시 확인했다.
- `git switch main`, `git merge --ff-only codex/proto-p11-p13` **종료0**. 병합된 Assets/실행폴더와 검증 브랜치의 차이0, main에서 manifest 재검사0.
- `git push origin main` **종료0**: `83230c6..a6f65ca main -> main`. `git ls-remote origin refs/heads/main`이 실제 배포 커밋 `a6f65cab34327d01326021b13d3b8df36a605261`과 일치했다.
- 이 확인 결과를 LATEST와 본 기록에 문서 커밋으로 추가한다. 실행파일/검증 소스는 바뀌지 않는다. 자신을 포함하는 문서 커밋 해시를 추측하지 않으며 최신 기록 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-27-proto-stabilization.md`로 조회한다.
- 사용자 마인드맵 수정과 기존 미추적 환경 폴더는 원격 커밋에서 제외하고 로컬에 보존했다. 작업 브랜치는 배포 커밋에 남겨 두고 현재 체크아웃은 main이다.
