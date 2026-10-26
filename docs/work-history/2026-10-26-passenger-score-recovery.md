# 승객 만족도 회복·운임 기준 표시

## 요구사항과 실제 구현

- P8-07의 급가속·급제동 감점만 안정 주행으로 2점씩 회복한다. 실제 이동 500m 또는 이동 중 60초마다 1회, 운송당 최대 6점, M 100점 상한이다. 충돌·신호·양보·인도 침범 손실은 회복하지 않는다.
- 운송 중 실제 위치의 10m 칸마다 거리 14m·이동 시간 20초만 적립한다. 정차·순간 이동·같은 구간 왕복으로 회복 조건을 반복 충족하지 못하며, 급가감속 사건은 진행 중 구간을 끊는다. 회복 가능한 감점·회복 상한·방문 구간과 부분 진행을 운송 저장에 포함한다. 기존 저장은 회복 채무 없는 점수로 복원한다.
- M 회복 후 기존 S 계산과 상단 즉시 알림을 갱신한다. 요청의 확정 변화도 하차 판정 시 즉시 갱신한다. 결과 팝업의 최근 3줄 금액 요약은 유지하고 아래에 `기본 운임=운전 만족도 M 대 R`, `보너스=승객 만족도 S 대 R`을 설명한다. S60/R70이더라도 M100·기한 내이면 기본 운임은 전액, 보너스는 0원이다. 기존 기본 운임 100%/70%/기한초과0원, S 보너스 산식은 변경하지 않았다.

## 변경 파일

- `unity/EungyeTaxi/Assets/Scripts/MannerLedger.cs`, `DrivingManners.cs`: 회복 가능 손실과 안전 이동 적립·저장/복원.
- `unity/EungyeTaxi/Assets/Scripts/PassengerRequestUI.cs`: 확정 요청 변화 즉시 알림.
- `unity/EungyeTaxi/Assets/Scripts/RideReceiptSummary.cs`, `RideCompletionPanel.cs`: 정산 기준 설명.
- `unity/EungyeTaxi/Assets/Editor/PassengerScoreRecoveryTests.cs`, `unity/EungyeTaxi/Assets/Scripts/PassengerCareProbe.cs`, `tools/test_manner_recovery.ps1`: 모델·직렬화·표시·위치 제어 검사.
- `docs/content-design/04-passengers-and-reservations.md`, `11-passenger-request-effects.md`, `13-driving-map-ui.md`: 구현 상태와 운임 기준 갱신.

## 수행한 검증과 결과

- `pwsh -NoProfile -File tools/test_manner_recovery.ps1`: 초기 RED `FAIL stable recovery API exists` 종료 1. 저속 경계 RED `FAIL sixty moving seconds also works at the minimum accepted speed` 종료 1도 확인했다. 최종 12개 PASS와 `MANNER_RECOVERY_MODEL_OK` 종료 0. 법규 감점 비회복, 급가감속 회복, +6 상한, 60초/500m, 최소 이동 속도, 왕복·정차·급가감속 단절, 부분 진행 복원을 검사했다.
- Unity 2022.3.62f2 편집기 `-batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod PassengerScoreRecoveryTests.Run`: 초기 RED `PASSENGER_SCORE_RECOVERY_FAIL receipt explains separate M and S criteria`, Unity 로그 반환 코드 1. 구현 후 `PASSENGER_SCORE_RECOVERY_TESTS_OK checks=8`, 실제 프로세스 종료 코드 0. `JsonUtility`를 통한 회복 채무/상한과 S60/R70 정산을 확인했다.
- 마지막 결과 문구·위치 제어 검사 편집 뒤 같은 편집기 검사를 재시도했다. `artifacts/task-a-final-unity.log`에서 별도 지도 작업의 `PedestrianSignalPlacementTests.cs`가 아직 없는 `PedestrianSignalPlacement` 등을 참조해 CS0103/CS1501이 발생했고 Unity 로그 반환 코드 1이다. Task A 검사 함수 실행 전 컴파일에서 중단되었으므로 이 재시도는 통과로 표시하지 않는다.
- `git diff --check`: 종료 0. 회복 모델 검사 이후에도 마지막 변경을 반영해 다시 종료 0으로 확인했다.

## 미검증·남은 항목

- 추가한 `-passengerCareTest` 위치 제어 검사와 최신 결과 팝업 문구는 별도 지도 작업의 컴파일 오류 해결 후 통합 BuildHybrid·실행파일 검사에서 확인한다. 기존 기본 실행 파일은 이 소스 변경 전 버전이다.
- 자동 물리 주행, 사람의 수동 운전, 하루/3일 전체 캠페인 경제 체감은 이 작업에서 수행하지 않았다. 특히 차체 움직임의 실제 회복 속도와 10m 구간 제한의 체감은 주행 검사가 필요하다.
- 지도 입력·정식 지도·지도 ZIP·빌드 배포 폴더를 변경하지 않았다. 통합 빌드·manifest 갱신과 `LATEST.md`·체크리스트 갱신은 상위 통합 작업에서 기록한다.

## 실행·재현

1. 저장소 루트에서 `pwsh -NoProfile -File tools/test_manner_recovery.ps1`을 실행한다.
2. Unity 2022.3.62f2로 `unity/EungyeTaxi`를 배치 실행하고 `-executeMethod PassengerScoreRecoveryTests.Run`을 지정해 편집기 회귀를 확인한다.
3. 통합 `PrototypeBuild.BuildHybrid` 뒤 기본 `builds/EungyeTaxi/EungyeTaxi.exe -passengerCareTest`를 실행해 `artifacts/passenger-care-integration.txt`의 `PASSENGER_CARE_INTEGRATION_OK`와 추가 회복 검사를 확인한다. 이 실행은 위치 제어 검사이며 물리·수동 주행이 아니다.

## 코드 검토 P2 후속 수정

- 검토에서 기존 10m 칸의 남은 시간이 역주행 때 적립되는 문제를 확인했다. 50m 전진 후 22m 역주행해 60초 회복 구간이 완성되는 모델 검사를 먼저 추가했고 `FAIL a pre-award out-and-back cannot turn fifty unique metres into sixty moving seconds` 종료 1을 확인했다. 셀 재진입을 닫은 뒤에는 같은 셀 내부 왕복도 남은 시간을 적립하는 것을 별도 RED로 확인했다.
- 활성 칸의 진입점에서 더 멀어진 거리만 시간·거리로 인정하고, 떠난 칸의 재진입을 닫는다. 저장에는 활성 칸과 진입점·최대 도달 거리를 포함한다. 이전 버전의 회복 저장은 활성 칸을 닫힌 것으로 안전하게 복원한다. `pwsh -NoProfile -File tools/test_manner_recovery.ps1` 최종 16개 PASS, `MANNER_RECOVERY_MODEL_OK`, 종료 0. 저장 후 새 구간의 진행과 재진입 차단도 검사했다.
- 보행 신호 P2: 현재 렌더·충돌이 지원하는 `sidewalk` 영역만 신호 지지면으로 인정한다. 실제 중앙 안전지대는 `sidewalk` 종류이므로 높이 0.58m의 보도형 중앙 안전지대 fixture를 유지하고, 미지원 `island` 종류는 배제하는 검사를 추가했다. 이 변경 뒤 Unity 편집기 검사는 상위 통합 작업에서 실행한다.
