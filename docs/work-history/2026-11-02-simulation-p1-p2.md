# P1~P2 시뮬레이션 구현

## 요구사항과 범위

사용자가 기존 우선순위의 P1~P2 구현을 승인했다. 음악·에어컨·창문의 실제 조작은 보류한다. 기존 텍스트 요청 응대는 유지한다. P3/P4, 원격4유저, 임의 실제 비보호 장소는 포함하지 않는다. 기본 배포는 `builds/EungyeTaxi/EungyeTaxi.exe`, `PrototypeBuild.BuildHybrid`, 은계 싱글/자체 도시 봇 경쟁이다.

## 소스 구현

| 영역 | 실제 변경 | 주요 파일 |
|---|---|---|
| 승객 | 신호 대기·실제 충격의 유형별 반응, 수면 복귀, 음식 섭취와 예정 흘림 분리, 안전 짐 재고정, 무료20초 경유 용무 | PassengerJobsActivities/PassengerSituations/PassengerBehavior/PassengerJobs |
| 운영 | 읽기 전용 도착 상태 예측, 공유 시설 실제 진입 대기와 취소, 이동·대기·견적 비교, 동일 비용/시간의 봇 식사·서비스 계획 | OperationPlanningPolicy/OperationForecastAdapter/ServiceStopsPlanning/ServiceStopsPlannerUI/PvpSimulationBotServices |
| 교통 | 게임 시간별 출발/도착 가중과 기존 호출 유지,72대 풀의 방향 선호,3가지 일반차 성향, 수동 회전 후 방향지시등 해제 | SimulationDemandClock/PassengerJobsDemand/DistrictTraffic/TrafficCar/TaxiSignals |
| 승하차 | 게임용 EG-GAME-02→EG-ALT-02-A 포켓, 실제 보행/복귀 진행·빈 포켓 검사·같은 운송ID/운임·목적지/지도 핀 | AlternativePassengerStops/StopOccupancyAlternatives/StopOccupancyPolicy/PassengerJobs/FullMapOverlay |
| 법규·저장 | 명시한 게임 서행 관찰과 같은 사건 최대 감점, 필요한 회피 중립. 방향지시등·진행 중 관찰·승객/용무·대체 장소 복원 | DrivingLawSlowTurns/SlowRightTurnState/BusinessProgress |

상세 계약은 현재 콘텐츠01/02/03/05와 [계획·검토 원장](../superpowers/plans/2026-11-02-simulation-p1-p2.md)을 따른다. 음식 새 운송은 이동 섭취20초, 예정 흘림은 추가5초 또는 실제 충격으로 앞당긴다. 기존 저장의 시간 정책은 유지한다. 게임 서행10km/h·여유2km/h·0.75초는 법정 고정속도 주장과 구분한다. 실제 비보호 신규 허용 근거는 아직0곳이며, 기존3접근 검토를 지역 전수 부재로 표현하지 않는다.

## 수행한 검사와 발견/수정

- 작업 시작 Strict: `pwsh -File tools/check_workspace.ps1 -Strict` 종료0/ issues0 (`artifacts/p1p2-baseline-workspace.log`).
- 기존 모델28개: `pwsh -File tools/test_prototype.ps1` 종료0 (`artifacts/p1p2-baseline-model.log`).
- 승객 실제 어댑터40·상황82·기존 행동537. 3회 검토에서 유형별 충격 효과, 질문 제한 시 수면 복귀, 도착 접근의 이미 닫힌 초기 선택 처리까지 RED/GREEN 보완했다. 마지막 검토는 추가 중요한 문제 없음. 보고서는 `artifacts/passenger-round1-review-fix.md`~`passenger-round3-review-fix.md`.
- 운영 모델39·실제 생산 Tick 어댑터6그룹. 입구 입장 승계/취소 자동 재등록/한 방문 비용·시간 묶음/출차 후 남은 긴급 상태의 호출 차단을 RED/GREEN 보완했다. 보고서는 `artifacts/operation-planning/fix-round1.md`, `fix-round2.md`, `ui-integration.md`.
- 대체 장소 작성 Editor: 카탈로그871·장소372 검사 종료0 (`artifacts/alternative-passenger-final6-editor.log`). 실제 런타임 정책44·대기/보행/취소복귀/복원/하차 Editor 종료0. 최종 통합 재검사 필요.
- 게임 서행 모델: `pwsh -File tools/test_slow_turn.ps1` 종료0/10검사. 실제 법규 장부·JSON 복원은 통합 Editor에 추가했다.
- 첫 통합 Editor는 시설 테스트가 실제 차로가 아닌 원점에서 경로를 조회해 종료1(`artifacts/p1p2-integrated-editor.log`). 성공으로 세지 않는다. 원인과 수정 후 결과는 아래 최종 검사에 기록한다.

## 검증 분류와 현재 한계

최종 소스 연결의 두 번째 통합 Editor `P1P2BuildChecks.CompileGate`는 실제 프로세스 종료0(`artifacts/p1p2-integrated-editor2.log`). 승객·교통/깜빡이·시설22·법규/체크포인트15·대체 보행/복원 검사를 포함한다. 이전 `SendMessage` 방식 Editor 검사에서 나온 비실행 Behaviour assertion은 private Tick을 직접 reflection 호출하도록 고쳐 실제 함수 실행과 assertion 없는 결과를 확인했다. 시설의 원점 경로 실패는 테스트 시작 좌표 문제로 분리해 실제 차로로 고쳤으며, 도로 밖은 계속 예측 불가다.

위 모델/어댑터/Editor 검사는 수동 플레이, 자동 물리 주행, 전체3일 캠페인, 다른 PC 검사가 아니다. 네이티브 실행·시각·성능·실행폴더 manifest·main 게시 결과는 최종 수용 후 이 문서에 추가한다. 현재 빌드를 새 기능 검증 완료로 표시하지 않는다.

### 밝은 UI·현행 PVP 통합 최종 Editor

완료된 main `d818c33`을 병합하면서 현재 PVP 피로1.25배·식사1,000원/15초·휴식무료/45초와 완료/중단 정책을 유지했다. 예측 복사본에도 실제 PVP 프로필을 복원하고, 경기 말미의 회복 불가능한 예측 피로만으로 무한 정차하지 않도록 했다. 대체 승차 후 예측·캐시·미니맵도 실제 대체 지점으로 일치시켰다.

곡선도로의 기존 직선 추정 좌표가 차선 밖으로 벗어나던 PVP 주차장4곳/공유 식당2곳의 실제 진입·출차 끝점을 올바른 방향 차선에 연결했다. 포장·보도·건물·연결 기하2708 및 실제 시설 어댑터30을 검사했다. 은계 프로젝트 지도/입력/ZIP은 변경하지 않았다. 기존 최소 승객 fixture의 새 서비스/nav 의존성 누락도 실제 세션과 같이 보완했으며 운영 코드에 null 우회를 넣지 않았다.

`P1P2BuildChecks.ValidateMerged` 최종 실제 Unity 프로세스 종료0 (`artifacts/p1p2-merged-editor-final.log`): 승객26·시설30·체크포인트/실제 회전 관찰18·대체 보행/예측·기하2708·밝은 테마·내비15·승객36041·도시90645·PVP 상태/서비스/봇·모드9. 앞선 `p1p2-merged-editor.log`의 fixture NullReference 실패는 성공 근거에서 제외한다.

최종 순수/추출 어댑터: 기존28묶음, 상황82·휴식 실제 어댑터40·행동537·care13301·교통38·대체44·서행21·운영49 모두 종료0. 서행의 연속 초과 시간이 정차/관찰 공백을 넘어 합산되던 문제는 RED→GREEN21과 별도 재검토로 해결했다. 마인드맵340노드의 기능 말단은50%170/25%0/0%54이며 중복 참조 포함, 보류 장치0% 유지. 재실행 바이트 불변과 사용자 ID/메모/접힘 보존을 검사했다.

## 입력·원본 보존

### 동시 main 변경 보존과 네이티브 첫 검사 보완

작업 중 원격 main에 지도/두 카드 HUD·기존 차량 LAN 기능이 `5548897`로 게시되어 추가 병합했다. 이는 이번에 원격 대전을 새로 기획/승인한 것이 아니라 이미 게시된 기능을 보존하는 통합이다. P1/P2 상황 응답·시설 안내/취소는 호스트에서 운송/종류/시설ID를 재검증하고, 게스트는 자기 대기 상태와 시설별 실제 점유를 받는다. 주기 전송에서는 고정 bay 목록+FIFO/물리 점유만 조회하며 경로/견적 계산0을 RED/GREEN 어댑터로 확인했다. 기존 신호/일반차는 클라이언트에서 별도 AI/깜빡이 취소를 실행하지 않는다.

첫 새 Player의 승객 본 검사 자체는 종료0이나 reload는 테스트 플래그가 저장 복원 허용 목록에서 빠져 실패했다. 전용 `artifacts/passenger-activities-test.json`, 명시적 reload/실패 보호를 연결했다. 대체 승하차 네이티브 fixture도 실제 Awake가 일시정지시키는 차이를 누락해 실패했으므로 fixture의 활성 상태와 전역 설정 정리를 보완했다. 실패 로그(`p1p2-native-integration.log`, `passengerActivitiesReloadTest.log`)를 수용 완료로 세지 않는다. 재빌드한 두 Player 검사가 최종 근거다.

추가 병합 최종 Editor `P1P2BuildChecks.ValidateMerged` 종료0 (`artifacts/p1p2-lan-merged-editor.log`): 앞선 P1/P2 전체 검사와 새 지도 표현, LAN JSON18·승객36041·도시90645·모드/프로필 검사를 통과했다. 실제 native/물리/화면/배포는 아래 최종 수용 기록을 따른다. 마인드맵은 기존 main LAN 구현도50%에 반영해340노드·기능말단171/0/53이며, 음악·에어컨·창문 실제 조작0%·보류 유지.

필수 프로젝트 지도 원본과 입력 해시/지도 ZIP은 변경하지 않는다. 대체 포켓은 명시적 게임 오버레이 구성/Resources 한 쌍으로 분리한다. 기존 일반 승하차24개 인덱스는 유지한다. 원래 체크아웃의 사용자 마인드맵 접힘/참고 가지 삭제 및 미추적 소스는 별도 작업으로 보존한다. 완료된 밝은 UI main `d818c33`은 기능 통합 후 다시 빌드/화면 검사한다. 배포 소스 해시는 실제 커밋이 생성된 뒤 확인한다.

### 최종 수용 전 발견한 저장·시설 경계

소스 `a539cd0`의 BuildHybrid 종료0 후 네이티브 대체/시설/교통/법규 fixture는 종료0(`p1p2-native-integration2.log`), 승객 본 검사는42개 종료0이다. 새 프로세스 재개는 실제 JSON의 빈 law entry/upstream 객체를 실제 진입으로 오판해 거부했다. 새 법규 관찰에 version/존재 플래그를 기록하고, 기존 빈 객체만 중립으로 정규화하며 비어 있지 않은 손상/미지원 버전은 계속 거부한다. 실제 회전/필요 제동과 upstream 관찰 증거의 신/구 JSON을 함께 검사했다. 재개 fixture는 실제 UI와 같이 계속하기 전에는 시간 불변, 계속하기 후에만 용무 완료를 검사한다. 이 실패는 일반 저장에도 영향을 줄 수 있어 테스트 회피가 아닌 생산 복원 정책에서 수정했다.

별도 검토에서 FIFO 대기 후 PVP 식사/휴식을 시작하자마자 시설 창을 닫아 서비스가 취소되는 문제도 발견했다. 성공한 활성 시간제 서비스만 창을 유지하며 실패/즉시 작업은 닫는다. 실제 Unity에서 공유 FIFO→Buy 대기→Tick 입장→15초 식사/45초 휴식의 완료/금액·회복,5초 후 명시 취소의 비용·회복0을 RED/GREEN으로 검사하고 별도 재검토에서 해결을 확인했다.

이 두 수정까지 포함한 `P1P2BuildChecks.ValidateMerged` 실제 종료0 (`artifacts/p1p2-final-acceptance-editor.log`): 체크포인트28, 시설30·기하2708, 승객26/36041, LAN JSON18, 도시90645, 테마·지도·내비·현재 프로필/모드 검사를 통과했다. 관리형 운영49·어댑터·서행21도 종료0. 앞선 실패는 보관한 `artifacts/p1p2-a539-native-evidence/`와 구분한다. 다음 최종 실행본에서 저장 재실행/실시간/물리/화면을 재검사한다.

## 최종 최소 수용·실행본 갱신

사용자의 마지막 요청에 따라 추가 리뷰·장시간/전체 주행·시각/성능 반복 검사를 중단하고, 실제 저장 재개와 새 기능 연결·기존 LAN 기본 연결만 최종 실행했다.

| 검사 | 실제 결과·범위 |
|---|---|
| 최종 소스 | `a8fcb88a9a9d96d1e9a96fa2f6582fe9bc764922`, 현재 main 지도/HUD/LAN 보존 |
| BuildHybrid | `artifacts/p1p2-build-release.log` 실제 종료0, 통합 EungyeTaxi.exe/실행폴더 |
| 새 기능 실제 Player | `pwsh -File tools/test_p1p2_integration_player.ps1` 종료0. 시설30·교통/깜빡이·법규 JSON28·대체 승하차/예측. 위치 제어 fixture이며 전 구간 자동 주행이 아님 |
| 승객·재개 | `pwsh -File tools/test_passenger_activities_player.ps1` 두 프로세스 모두 종료0. 본42·새 프로세스6: 용무 잔여/ID/목적지 유지, 계속하기 전 시간 불변, 계속하기 후1회 완료 |
| 기존 LAN 기본 연결 | `pwsh -File tools/test_pvp_native_lan.ps1 -Port 18973 -TimeoutSeconds 180` 종료0. 호스트23/클라이언트26, 실제 WheelCollider 페달14.0097m·선점/새 상황의 오래된 운송 명령 거절·깜빡이/비상등·시설 점유 수신·72NPC/신호·봇 승계. 두 loopback 프로세스이며 다른PC/사람4인/전체 운송·재접속 반복 검사가 아님 |
| 입력/저장/문서 | Strict issues0, 사용자 저장4파일 크기/SHA256 불변. 마인드맵340·171/0/53, 사용자ID/메모/접힘·재실행 불변. 변경 문서 상대 링크146개 누락0 |

실행은 `builds/EungyeTaxi/EungyeTaxi.exe`에서 은계 싱글 또는 현재 PVP/기존 LAN을 선택한다. 일반 승하차24개와 은계 입력 지도/ZIP·회사/제품명·실제 저장 위치를 유지했다. 음악·에어컨·창문 실제 조작은 계속 보류한다. 실제 비보호 신규 장소는 판독된 긍정 표지 근거가 없어 추가하지 않았다. 사람 직접 운전·장시간 경제/성능·다른PC 수용은 미수행이며 이번 완료율로 과장하지 않는다.

빌드의 자체 생성 `*_BurstDebugInformation_DoNotShip` 진단 폴더는 artifacts에 보관하고 실행에 필요한 폴더 전체를 배포한다. 배포 manifest는 위 실제 소스 SHA와 최종 크기/SHA256을 사용한다. 게시 명령과 원래 main 반영 결과는 게시 후 아래 영수증에 기록한다.

## 게시 영수증

- `git log -1 --oneline -- docs/work-history/2026-11-02-simulation-p1-p2.md`로 실제 배포 커밋 `664ea41c6a66f2c0c28c66e0e84e7f03b7cdd380`을 확인했다. `git push origin HEAD:main` 종료0, `git ls-remote origin refs/heads/main`이 동일 해시를 반환했다. 동시 main5548897을 보존한 fast-forward 게시이며 강제 푸시하지 않았다.
- 최종 실행폴더 **139파일/115,283,461바이트**, manifest 소스a8fcb88. 통합 및 PVP 위임 VerifyOnly 종료0, 모든 크기/SHA256 일치.
- 원래 main 체크아웃도 `664ea41`로 fast-forward하고 실행폴더의 VerifyOnly 종료0을 확인했다. 사용자 마인드맵 두 파일은 SHA256 확인한 보관본으로 정확히 복구한 다음 요청한 단계만 재표시했다. 작업 지도333노드의 ID/접힘/사용자 메모/기존 체크와7개 참고 노드 삭제를 보존했으며, 사용자 미추적 폴더/파일도 그대로다. 주 저장소의 원본340노드를 사용자 삭제 작업과 섞어 커밋하지 않았다.
- 사용자 저장4파일의 크기/SHA256과 파일 집합 불변, 필수 입력 Strict issues0. 최종 실행/입력/보류 범위는 위 최종 최소 수용을 따른다. 이 영수증은 완료된 배포 커밋을 가리키며 영수증 자신을 포함한 해시를 추측하지 않는다.
