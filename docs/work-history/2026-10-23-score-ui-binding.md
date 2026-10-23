# 승객 만족도·운행 점수 UI 실제 데이터 연결

## 요청과 원인

사용자 보고: 주행 HUD는 만족도100, 하차 결과는 시작76으로 보이고 요구85와 청결+3의 의미가 불명확했다. 실제 주행 변화·누적 운행 점수가 UI에 반영되는지도 확인 요청했다.

HUD/알림이 내부 운전 만족도 M(탑승100)을 승객 만족도 S로 표시하는 연결 오류였다. 실제 정산은 기획11의 S0=R-D+청결을 사용했다. 예시 R85-D12+청결3=76은 올바른 시작값이며 76에 청결3을 다시 더하는 것이 아니다. 산식·기본 운임 정책은 변경하지 않았다.

## 실제 구현

| 부분 | 변경 |
|---|---|
| 만족도 공통 표시 | PassengerScoreView로 현재 S·요구R·시작S0·청결·주행·요청·도착시간 변화 공유. 라이브 S는 시간 가산을 예상해서 지급하지 않음 |
| HUD | M100 대신 실제 S와 요구R, 요구 위치 흰 눈금·시작/청결 포함 안내. 승인된 우측 하단 카드 높이를 보완 |
| 변화 알림 | 실제 S의 차이와 변화 후 점수·주행/요청/도착 원인. 승객 교체 시 이전 승객 알림 제거, 긴 문구 높이 자동 조절 |
| 결과/F8 | 기본73+청결3=시작76처럼 이미 포함된 가산 표시. 주행/요청/시간 합산과 동일한 최종 S. 긴 요청 설명은 결과창 내부 스크롤 |
| 운행 점수 | 원래부터 독립 누적 법규 장부 TotalScore에 연결돼 있었음. 실제 위반/준수 증감과 저장 복원 검사. 신규 결과에 하차 시 누적 점수와 존재 플래그 저장, 구 저장의 없는 값을 0점으로 표시하지 않음 |
| 상태 경계 | 기상 대기에서는 확정된 도착 만족도 유지. 새 호출 픽업 기한 만료는 이전 승객 점수/가짜 탑승 알림으로 바뀌지 않게 ArrivalLocked 확인 |
| 기획 | 문서04/11/13과 P8-08에 구현·표시·검증 기준 반영. 내부 M의 기본 운임 기준과 별도 누적 법규 점수 정책 유지 |

## 변경 파일

- `unity/EungyeTaxi/Assets/Scripts/PassengerRequest.cs`, `PassengerJobs.cs`, `CockpitUI.cs`, `DriveHUD.cs`, `DriveNotifications.cs`.
- `RideCompletionPanel.cs`, `PrototypeState.cs`, `BusinessProgress.cs`, `DrivingLaw.cs`, `PassengerRequestUI.cs`.
- `PassengerCareProbe.cs`, `PlayabilityUISmokeTest.cs`, `tests/PassengerCareTests.cs`.
- BuildHybrid가 생성한 `Assets/Scenes/FirstDrive.unity`(객체 식별자만 재생성), 기본 실행 폴더/배포 manifest.
- 관련 기획04/11/13·체크리스트·본 작업 이력과 LATEST.

## 실제 검증

| 종류 | 명령/근거 | 결과 |
|---|---|---|
| 재현/RED | `pwsh -File tools/test_passenger_care.ps1` 청결 포함 내역 회귀 검사 | 수정 전 FAIL: receipt must explain cleanliness already included in76 |
| 순수 모델/GREEN | 같은 명령 | exit0,10098 assertions. S76/R85·주행-2/요청+5=79·도착가산+12·상하한·별도법규20감점/1회복·복원/중복 포함 |
| 기존 모델 | `pwsh -File tools/test_prototype.ps1` | exit0,23묶음. 운임/캠페인/요청/주행/신호 기존 검사 |
| Unity 편집기/빌드 | `PrototypeCareChecks.Build` | exit0. JsonUtility14·회계22·정차850·연결2412·지도 가독성. 내부 실행은 PrototypeBuild.BuildHybrid, 은계 기본exe |
| 실행파일 위치 제어 | `EungyeTaxi.exe -passengerCareTest -scoreCapture` | exit0,123 assertions. 실제 탑승·감점·요청·HUD helper/알림 큐·결과/장부·JsonUtility저장·기상대기고정·픽업만료 포함 |
| 실제 2프로세스 저장/복원 | `-liveSaveTest` 종료 후 새 프로세스 `-liveReloadTest` | 둘 다 exit0,9+23검사. 사용자 저장과 다른 artifacts 테스트 경로 |
| 화면 생성/직접 이미지 검토 | score-hud-1280/1600/1920.png, score-receipt.png | 실제 Player 캡처. 요구85·만족도79·시작76/청결포함 표시와 카드/결과창 겹침·잘림 확인 |
| 검토 | requesting-code-review 지침에 따른 읽기 전용 검토 | 다음 호출 만료 때 이전 결과 재표시 가능성 발견·보정·회귀 검사 추가 |

로그는 `artifacts/score-ui-release-build.log`, `score-ui-release-player.log`, `passenger-care-integration.txt`, `score-liveSaveTest.log`, `score-liveReloadTest.log`에 있다. 첫 실행 검사에서는 이전 테스트 운송이 남긴 요청 종류를 잘못 가정해 실패했으며 Quiet 고정 fixture로 명시한 뒤 재실행했다. 이를 실제 게임 결함으로 계산하지 않았다.

## 실행·재현과 미검증

기본 `builds/EungyeTaxi/EungyeTaxi.exe` → M 호출 수락 → 정차 F 승차. 우측 하단에서 현재 S/요구R와 시작·청결 포함 표시 확인. 운전 사건에 따른 S 변화, 하차 결과의 같은 내역, 정보/F8의 누적 운행 점수와 운송 기록 확인.

자동 위치 제어는 실제 물리 주행/사람의 수동 플레이가 아니다. 이 작업에서 20분·3일 전체 캠페인/경제 체감은 다시 검사하지 않았다. P8-07 안정 주행 M 회복도 이번 범위에 추가하지 않았다. 원본 지도/정식 지도/지도ZIP/입력 해시는 변경하지 않았다. 시작 시 확인한 다른 문서·라이선스/XMind 변경을 보존하고 커밋에 섞지 않는다. PC별 Unity 경로는 로컬 설치 정보다.
