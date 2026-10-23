# 2026-10-23 간단 점수 알림·대기 승객·운행 정산

## 요구와 구현

- 운행 중 만족도/운행 점수 변화를 `원인 · 증감치`로 즉시 표시한다. 두 종류 배지는 분리하고 최근 4개를 동시에 3초 표시한다. 기존 FIFO 대기로 늦어지는 표시를 제거했다. 실제 만족도 변화와 누적 법규 장부 사건을 관찰하며 승차 시작값을 점수 획득으로 알리지 않는다.
- 24개 안전 정차 후보는 유지하고 실제 대기 인구를 분리했다. 호출 수락 승객은 반드시 존재하며, 일반 길가 대기는 안정된 35% 표본/120초 갱신이다. 90m 안 인구는 갱신으로 갑자기 바꾸지 않는다. 비어 있던 하차지는 90초 동안 새 호출/승객으로 즉시 채우지 않는다. 저장에 인구·갱신시점을 포함하며 옛 저장은 진행 중 운송을 보존해 이행한다.
- 결과 창의 기본 운임은 수락 시 최초 예상 운임, 보너스는 실제 지급액과 최초 예상액 차이(음수 가능), 최종 운임은 실제 지급액이다. 지급 정책은 바꾸지 않았다. 청결/주행/요청/시간 중 실제 비영(非零) 만족도 요인만 짧게 보여준다. F8의 상세 기록은 보존한다.
- 검증 중 발견한 새 영업 시작 시 이전 운송의 임시 RequestReward 잔류를 초기화했다. 과거 운송 기록은 보존한다.
- 수익·빈도·생활 시간 밸런스는 사용자가 직접 테스트한다. 이번 작업은 경제 수치를 조정하지 않았다.

## 변경 파일

- `unity/EungyeTaxi/Assets/Scripts/DriveNotifications.cs`, `ScoreFeedback.cs`, `DrivingManners.cs`, `PrototypeState.cs`: 실제 점수 관찰·간단 즉시 알림·공통 점수 계산.
- `PassengerJobs.cs`, `PassengerWaitingPolicy.cs`: 인구/호출/승하차·저장·마이그레이션, 최초 운임.
- `BusinessProgress.cs`, `RideReceiptSummary.cs`, `RideCompletionPanel.cs`: 최초 운임 보존·실수령 정산·간단 원인.
- `PassengerCareProbe.cs`, `DriveFeedbackSmokeTest.cs`, `tests/PassengerCareTests.cs`, `tools/test_passenger_care.ps1`: 회귀 검사.
- 기획05/13/14·체크리스트·본 이력과 LATEST.

## 실행한 검증

| 구분 | 명령/증거 | 결과 |
|---|---|---|
| 회귀 RED | 기존 전 지점 대기 코드로 `-passengerCareTest` | exit1, 첫 검사에서 빈 지점 없음 재현 (`artifacts/waiting-red-build.log`) |
| 순수 모델 | `pwsh -File tools/test_passenger_care.ps1` | exit0, 11,105 assertions |
| 기존 모델 | `pwsh -File tools/test_prototype.ps1` | exit0, 23 suites |
| Unity/빌드 | `PrototypeCareChecks.Build` (내부 BuildHybrid), `artifacts/compact-ui-shipping-build-retry.log` | exit0, Json14·회계22·정차850·연결2412, PROTOTYPE_BUILD_OK |
| 실행파일 위치 제어 | `EungyeTaxi.exe -passengerCareTest -scoreCapture` | exit0, 167 checks. 빈 지점 상호작용·수락 보장·하차 뒤 재생성/복원·점수 즉시/동시 표시·최초 운임/최종 정산 |
| 실제 시간 경과 | `-driveFeedbackTest -hybridV2` | exit0, 16 checks. 간단 실제 점수 알림과 자동 해제·새 캠페인 초기화 |
| 실제 두 프로세스 | `-liveSaveTest -hybridV2`, 이어서 `-liveReloadTest -hybridV2` | 각각 exit0, 9+23 checks |
| 렌더 | `artifacts/score-hud-{1280,1600,1920}.png`, `score-receipt.png` | HUD·만족도/요구·운행 점수와 간단 정산 생성, 1280 HUD/1920 결과 직접 이미지 확인 |
| 코드 검토 | 읽기 전용 검토·회귀 추가 | 요청 실패 알림을 요청 이행에서 `요청 미이행`으로 정정 |

최초 빌드 시 실행 중 파일의 메모리 매핑 때문에 resources.assets 복사 실패가 한 번 발생했다. 플레이어가 없는 상태를 확인한 재빌드가 성공했고 그 결과로 위 실행 검사를 다시 수행했다. 빌드 재시도 전에 발견한 임시 RequestReward 초기화도 최종 실행 검사에 포함됐다.

## 미검증과 다음 작업

- 위 167개는 위치 제어 검사이며 사람이 운전한 검사나 자동 물리 주행으로 기록하지 않는다. 실제 20분/3일 경제·반복 호출 체감은 사용자 확인 대상이다. P7-06/P8-06/P9-07 수동 항목과 P8-07 안정 주행 회복 구현은 완료 표시하지 않는다.
- 화면 캡처의 HUD 값·결과는 확인했으나 알림의 화면상 위치를 사람의 주행으로 확인한 것은 아니다. 알림 발생/동시성/3초 해제는 데이터·실제 시간 경과 검사로 검증했다.
- 별도 지도 편집 작업과 통합은 사용자에게 전달/통합 허락을 받은 뒤 진행한다. 이 소스 검증 뒤 상대의 main을 통합한 최종 빌드·manifest·검증 결과를 후속 절에 남긴다.
- 기본 지역은 은계, 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`. 생성 입력2개·정식 지도·OSM 대조·지도 ZIP은 본 작업에서 변경하지 않았다. Unity/Python 설치 경로는 PC별 로컬 환경이며 저장소 경로에 포함하지 않는다.
- 소스 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-23-compact-feedback-waiting-receipt.md`로 확인한다.
