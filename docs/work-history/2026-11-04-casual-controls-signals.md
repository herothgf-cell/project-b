# 2026-11-04 캐주얼 조작·신호 대기 개선

## 요청·범위

사용자는 조향/제동 답답함·신호 대기를 줄이는 제안을 승인했다. S 일반제동 → 핸들/중속 회전 → 대기열 출발 → 신호 수요 순으로 진행한다. [승인 설계](../superpowers/specs/2026-11-04-casual-controls-signals-design.md), [계획](../superpowers/plans/2026-11-04-casual-controls-signals.md). 은계 싱글/A2 PVP와 기존 H·법규/만족도·운송·경제·회사/제품/저장·지도/ZIP을 보존한다.

## 실제 변경

| 부분 | 구현 |
|---|---|
| S 일반제동 | 토크 계수0.18→0.30(1.67배), Space1.0 유지. 사람 운전은 반대 입력 제동 후 계속 누르면 정차 유지, 놓고 다시 눌러 반대 기어. 자동 봇은 기존 자동 전후진 유지 |
| 핸들 | 입력률12~7/s·반대 조향1.4배·해제 복귀0.65배. 수동20km/h32°/30km/h19°/35km/h12.5°로 완만한 곡선,50km/h부터 기존 고속 한계. H는 기존 최대각/Ackermann 기하 유지 |
| 대기열 | 예약 알고리즘을 불필요하게 바꾸지 않음. 다른 route 객체의 같은 방향3대가0.34/3.98/6.14초에 같은 녹색 정지선 통과. 같은 출구 실제 정차 장애물은 계속 대기 |
| 신호 | 기본 gameProgram에 별도 runtime. 최소 차량 녹색4초·황색/정리 보존·빈 단계 생략·대기열 최대 기본+4초·경쟁 수요 순환. 다른 수요가 없으면 현재 차량 녹색 유지 |
| 수요 | NPC 경로 접근·모든 택시 실제 위치/방향/깜빡이·보행 대기,0.25초 간격/택시 목록2초 캐시. 접근 선행 감지로 다음 신호 준비. 완전한 간선 녹색 연동/연속 무정차 보장은 아님 |
| 보호 | 보행 녹색 전체 유지·횡단 점유가 끝날 때까지 정리 이후 전적색. 편집/교육 신호 주기·실제 자료·연습 상황 제외. 출구/보행/예약의 실제 진입 보호 유지 |
| 표시/LAN | 현재 색 변경과 자기 방향 녹색 예상 시간 구분, 우회전은 조건 확인. 호스트 runtime을v3 snapshot으로 공유하며 참가자는 독립 스케줄 안 함. 황색→적색을 녹색 ETA로 표시하지 않도록 별도 계산 |
| 제동 복원 | 사람의 정차 입력 상태도 권한 motion으로 복제. 작은 재설정도 식별해 호스트와 예측 제동 상태 불일치 방지 |

## 확인한 근거

| 검사 | 결과/증거 |
|---|---|
| 조작 RED/GREEN | `casual-controls-red.log` exit1, `casual-controls-green.log`14개/exit0 |
| 수요 RED/GREEN | `demand-signal-red.log` exit1, `demand-signal-green.log`1810개/exit0(빈 단계/상충 녹색/최소 녹색/보행 점유/공평 순환/JSON) |
| 자동 물리 제동 | `casual-physics-baseline.json`→`casual-physics-candidate.json`.20/30/40km/h 진입 정차 시간1.16/1.60/2.02→0.90/1.36/1.80초, 거리2.66/5.77/10.01→2.55/5.68/9.92m. S 유지 후 역주행 모두true→false |
| 감점 경계 | 같은 평지의 필터 감속 약−5.66/−5.81/−5.96m/s², 기존 급제동 기준−6 초과 감점에 도달하지 않음. 토크1.67배가 정지 거리1.67배 단축을 뜻하지 않음 |
| 자동 물리 코너 | `casual-physics-powered.json`: 가속 입력을 유지한20/30km/h 진입 좌우90도 약1.80/1.62초, 좌우 대칭·평지 차체 기울기 낮음. 진입 속도 이후 자연 감속하며 일정 속도 원운동 검사가 아님 |
| 자동 물리 대기열 | `casual-queue-first.log`1004개/exit0.3대 출발·몸체 간격·정차 출구 유지 |
| 첫 실제 실행 | `casual-single-first.log`226개, `casual-pvp-first.log`213개/exit0. 실제 맵 객체의 무내비 좌회전 수요/유한 예상 대기/JSON lamp 일치/보호 녹색 도달. 위치 제어 검사이며 도로 전구간 운전과 다름 |
| 중간 통합 | `casual-integrated-build.log` exit0/BuildHybrid. 기존 조향/신호1161·내비30·예약/보행·LAN 예측 회귀 포함. 후속 리뷰 수정 뒤 최종 재검사 필요 |
| 리뷰 RED | `casual-reset-red.log`·`casual-yellow-red.log`: 제동 상태 재설정·황색 ETA 오류 재현. 후속 작은 재설정 epoch 회귀를 추가 |

기존 예약 corridor 검사는 첫 통합/격리 실행에서 실패했다. 계측값은 정지선29m 대비28.6876m·속도0.244m/s였고 아직 정차 증거를 만들기 전이었다. 기존4.8초 예산을 최대12초의 실제 정차 조건 대기로 바꾸어 동일 엄격한 위치/정차 증거를 검사했다. 예약/진입 production 조건은 바꾸지 않았다. `casual-reserved-diagnostic.log`와 통합 후속 통과로 구분한다.

## 파일·판단

- 공통 조작: DriveMath/DriveFlowPolicy/TaxiController, 새 DriverPedalIntent.
- 신호: TrafficSystem partial, 새 DemandSignalClock/TrafficSignalDemand, RoadMapSignalProgram의 녹색 ETA.
- LAN/표시: PvpNativeLan/Prediction/Replica/LanProbe, DrivingReadability.
- 검증: 새 CasualDriveChecks/Physics/QueueChecks/IntegrationBuild/DemandSignalChecks, runtime CasualDriveProbe. 기존 DriveFlowBuild 기대값·예약 검사 대기 예산만 갱신.
- Ruling: tracked 변경이 없는 기존 Unity 캐시를 재사용해 별도`codex/casual-controls-signals`에서 실행. 기존 미추적 라이선스/마인드맵/도구 자료를 포함하거나 되돌리지 않는다.
- Ruling: 입력을 더 쉽게 하되 물리 엔진/타이어/가속/최고속/급제동 배점은 바꾸지 않았다. 순수 신호 주기만 짧게 하는 대신 실제 수요를 사용한다. 명시적 신호 프로그램은 자동 최적화하지 않는다.

## 남은 수용·실행

기본 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`. 양쪽 PC는 같은v3 실행본 사용. S로 멈춘 뒤 후진하려면 S를 놓았다 다시 누른다. Space는 긴급제동이며 H 가감속은 계속 직접 입력한다.

자동 물리 평지 fixture·모델·위치 제어 검사와 사람의 연속 운전/실물 패드/DPI/다른PC LAN·10/20분 적색 대기 비중을 구분한다. 실제 지도/차량 점유에 따라 기다리는 것은 남아 있고 정체를 강제로 제거하지 않는다. 사람의20분 체감 및 전체3일 캠페인 수익 검사는 이번 검증에 포함하지 않는다.

## 최종 검증 확정

- `artifacts/casual-shipping-build.log`: Unity 종료0, 조작14+리뷰 회귀6·수요1810·물리 대기열1004·기존 DriveFlow1161·Arrival30·Navigation15·예약/보행·예측17/자유경로5/바퀴8/자세11, `CASUAL_INTEGRATION_OK`와 `PROTOTYPE_BUILD_OK` 확인.
- 최종 exe의 `casual-single-final.log`226개·`casual-pvp-final.log`213개/각 종료0. 실제 은계/A2 도로 객체에서 내비 없는 접근과 좌회전 수요·호스트/복제 신호 일치를 검사했다.
- `pwsh -File tools/test_pvp_native_lan.ps1 -Port 18439 -Reconnect -TimeoutSeconds 240`: 종료0, host40/client55·재접속 통과. 증거 `artifacts/pvp/native-lan-20261104-173142-01c1116e`. 실제 입력/WheelCollider 이동·역주행/제동·신호 runtime 복제·기존 운송/정산을 확인했다. 같은PC loopback이며 다른PC 검사가 아니다.
- 표시: 숨김 창 캡처가 검게 나와 합격 근거에서 제외했다. 보이는 단독1280×720 실행의 `casual-single-rendered.log`/`casual-pvp-rendered-retry.log` 종료0, 두 PNG를 직접 열어 신호 변경/자기 좌회전 안내를 확인했다. 첫 보이는 PVP 실행은 초기화 이전90초 timeout으로 자체 프로세스만 종료했고, 단독 재시도에서213개/실제 렌더를 통과했다. 초기 지연 원인은 확정하지 않았으며 실패를 통과로 계산하지 않는다.
- 사용자 실제 저장4개를 `casual-user-saves-before.json`과 재대조해 변경0. workspace Strict 종료0/input issues0·관련9문서 로컬 링크135개 누락0·`git diff --check` 통과.
- 리뷰에서 지적한 권한 제동 상태와 황색 ETA를 수정했다. 근거리 복귀도0.5m/reset=false의 명시 epoch 회귀로 확인했다(`casual-nearby-reset-red.log`→최종 GREEN). 후속 읽기 전용 리뷰에서 남은 중요 문제 없음.
- 빌드 이후 production 코드는 바꾸지 않았다. 소스/배포 실제 커밋·manifest·main 게시 영수증은 아래에 추가한다.

## 배포 확정

- 실제 소스 커밋 `8871fcc4fa0b0390d1f16635dbb7887d0e37f876`. `git log -1 --oneline -- docs/work-history/2026-11-04-casual-controls-signals.md`로 확인했다.
- 위 소스와 `artifacts/casual-validation.json`으로 통합 manifest를 갱신했다. 실행 폴더140파일/122,952,126바이트, 통합/PVP VerifyOnly 종료0·전수 SHA256/크기 일치. 생성된 Burst DoNotShip 폴더는 경로를 확인한 뒤 artifacts에 보존해 배포에서 제외했다.
- 시작 원격 main a8b4c0d를 보존하고 fast-forward 통합했다. 배포 커밋 `3a7f7ae5bc768222294b44573fe0881a28642e12`, `git push origin main` 종료0·`git ls-remote origin refs/heads/main`과 HEAD 일치를 확인했다. main의 실행 폴더도 manifest140파일 전수 일치다.
- 후속 게시 영수증 문서 커밋은 실행 파일을 바꾸지 않는다. 실제 이력 커밋은 `git log -1 --oneline -- docs/work-history/2026-11-04-casual-controls-signals.md`로 확인한다. 기존 사용자 미추적 자료만 남기고 이번 tracked 소스/문서/배포는 커밋했다.
