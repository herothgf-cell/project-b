# 승객 운임 개정 최신 main 통합 (2026-10-26)

## 요구와 통합 결정

사용자는 만족도별 운임 변경 브랜치를 `main`에 병합하고 푸시하도록 요청했다. 원래 브랜치가 갈라진 뒤 `main`에 은계 지도·내비·정산 표시·P8-07 안정 주행 회복이 먼저 통합되었다. 운임 변경과 사용자 결정인 시작 만족도 `R` 미만·동일·초과를 최신 `main`에 이식하고, 이미 검증된 `main`의 P8-07 구현·지도·배포 파일을 유지한다. 원래 기능 브랜치와 그 커밋은 보존한다.

## 실제 변경

- `PassengerRequest.cs`, `PassengerJobs.cs`: `S-R` 미달 1점당 기본 운임 0.5% 감액(최대20%), `R=60/70/85` 보너스 가중치 0.8/1.0/1.3, 시작 만족도의 부호 있는 승객별 차이 `-12,-8,-4,0,4,8,12`.
- `RideReceiptSummary.cs`, `DrivingLaw.cs`, `BusinessProgress.cs`, `PrototypeState.cs`: 신규 정산은 약정·감액·보너스·실수령 및 `S/R`을 보여준다. 구형 완료 기록은 기존 지급액과 `M` 기준 설명을 유지한다.
- 승객·정산 모델/플레이어 검사와 `docs/content-design/04-passengers-and-reservations.md`, `11-passenger-request-effects.md`, `docs/prototype-development-checklist.md`, `docs/prototype-scope.md`를 현행 기준으로 갱신한다.
- `main`의 `DrivingManners.cs`, `MannerLedger.cs` 및 고유 안정 주행 적립 모델은 그대로 유지한다. P8-07의 실제 검증은 [기존 통합 기록](2026-10-26-junction-trip-followups.md)에 있다.

## 검증

| 종류 | 명령·결과 |
|---|---|
| 전체 모델 | `pwsh -File tools/test_prototype.ps1`: 23묶음 PASS, 종료0 |
| 승객 관리 모델 | `pwsh -File tools/test_passenger_care.ps1`: 11,412단언 PASS, 종료0 |
| P8-07 모델 | `pwsh -File tools/test_manner_recovery.ps1`: 16검사 PASS, `MANNER_RECOVERY_MODEL_OK`, 종료0 |

첫 통합 `PrototypeBuild.BuildHybrid`는 `Build Finished, Result: Success`/`PROTOTYPE_BUILD_OK`였다. 그 실행 파일의 `-passengerCareTest`는 157번째 검사에서 이전 `M100/S60/R70` 전액 지급 기대값 때문에 종료1로 실패했다. 실제 새 정책의 기본 운임은 4,750원이다. `PassengerCareProbe.cs`와 `PassengerScoreRecoveryTests.cs`의 옛 기대값을 수정한 뒤 빌드와 검사를 다시 실행한다.

최종 `PrototypeBuild.BuildHybrid`는 `artifacts/satisfaction-main-integration-final-build.log`의 `Build Finished, Result: Success`와 `PROTOTYPE_BUILD_OK`를 확인했다. `PassengerScoreRecoveryTests.Run`은 `PASSENGER_SCORE_RECOVERY_TESTS_OK checks=10`, 편집기 종료0. 기본 은계 실행 파일의 `-passengerCareTest`는 `PASSENGER_CARE_INTEGRATION_OK checks=172`, 종료0. `pwsh -File tools/check_workspace.ps1 -Strict`는 Input issues 0이었다.

기본 은계 실행 파일의 `-passengerExpansionTest`는 17종 요청·선택·목적지 변경·정산 위치 제어 검사 526줄을 남기고 종료0이었다. `FAIL` 항목은 없다. 플레이어 로그에는 저장 파일 교체 경고가 한 번 있었으므로 두 프로세스 저장·복원 결과를 별도로 확인한다.

`-liveSaveTest`와 `-liveReloadTest`를 순서대로 실행해 각각 종료0을 확인했다. 저장 로그는 `LIVE_CAMPAIGN_SAVE_OK checks=9`를 기록했다.

HUD smoke 재검사에서는 기록된 실행 인자 `-presentationHudTest -oneDayMode -hybridV2`를 사용했다. 첫 정차점에 대기 승객이 없어서 `controlled roadside boarding`이 실패했고 이후 결과 참조가 중단되었다. 실제 승객 배치의 무작위성에 의존하지 않도록 고정 호출·수락을 검사 준비에 넣고, 하차의 `TripState.Complete`도 확인하도록 수정했다. 수정 후 재빌드와 동일 인자 재실행 결과를 별도 확인한다.

고정 호출 검사 수정 뒤 `PrototypeBuild.BuildHybrid`는 `artifacts/satisfaction-main-hud-fixture-build.log`의 `Build Finished, Result: Success`/`PROTOTYPE_BUILD_OK`를 확인했다. `-presentationHudTest -oneDayMode -hybridV2`는 결과 팝업의 감액·보너스·실수령과 중복 지급 방지를 포함한 모든 항목 PASS, 종료0이었다.

첫 `-serviceLawZonesTest` 위치 제어 검사는 7개 운송 중 8·9·14 코스의 `no duplicate fare` 3항목만 실패해 종료1이었다. 하차 완료 직후 같은 연석에서 새 길가 승객을 태울 수 있어, 검사 준비가 다른 운송의 승차를 원래 운송의 중복 지급으로 오인했다. 첫 운송의 `TripState.Complete`를 확인하고 재입력은 승차 범위 밖에서 수행하도록 수정했다. 나머지 법규·상황 관찰 항목은 통과했다.

검사 수정 후 `PrototypeBuild.BuildHybrid`는 `artifacts/satisfaction-main-final-release-build.log`의 `Build Finished, Result: Success`/`PROTOTYPE_BUILD_OK`를 확인했다. `-serviceLawZonesTest` 최종 보고서는 7개 코스 `success=True`, `FAIL` 없음, 프로세스 종료0이다.

최종 실행 파일의 `-stabilityTest`는 프로세스 종료1, `artifacts/stability-smoke.txt`의 `success=False`였다. 실제 물리·교통 이동 항목의 충돌·가감속·경계·48대 혼합 흐름은 통과했으나, 연석 방향·신호·회전·교차로 예약·정차 대기열 등이 실패했다. 이번 변경 대상인 감액 검사는 `reducedFare=True`였다. `StabilitySmokeTest.cs`의 이번 수정은 해당 감액 기대값 한 줄뿐이고 나머지 실패 항목의 검사/제품 로직은 변경하지 않았다. 이 검사 전체는 통과로 표시하지 않으며 후속 주행 진단이 필요하다.

## 남은 확인과 실행

Unity 2022.3.62f2 `PrototypeBuild.BuildHybrid`로 기본 은계 `builds/EungyeTaxi/EungyeTaxi.exe`를 갱신했다. 승객 관리·17종 요청·저장/복원·HUD·서비스 지역 플레이어 검사는 위 결과를 따른다. 안정성 자동 물리 검사 실패 항목의 원인 진단, 사람의 수동20분·3일 경제 체감은 남아 있다. 지도 생성 입력과 ZIP은 변경하지 않았다. 다른 PC의 필수 입력은 `pwsh -File tools/check_workspace.ps1 -Strict`로 확인한다. 배포 manifest 파일 크기/SHA256 결과는 아래 배포 기록에 추가한다.
