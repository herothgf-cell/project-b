# 2026-11-26 신호 남은 시간이 튀는 문제 (감응 신호, 은계 싱글)

## 요구 (사용자, 2026-11-26)

J7973_3744(지도 791, 400 부근) 북행 신호의 남은 시간이 "초록 2초 → 16초로 늘었다가 줄고 → 다시 22초쯤으로 늘어남". 원인 설명 뒤 사용자가 A안(초록 길이를 단계 시작 때 정하고 중간에 바꾸지 않음)을 골랐다.

## 원인 (`-contentTest -signalCountdownCheck`로 0.5초마다 기록)

1. 감응 신호가 초록 도중에 길이를 바꿨다. 짧게 시작(대기 적으면 4~6초)한 뒤 대기 3대 이상이면 +4초, 끝날 때 대기가 있으면 +2초씩 늘리고, 다른 방향만 기다리면 잘랐다. 표시 숫자는 그때그때의 남은 길이라 연장되는 순간 위로 튀었다.
2. 화면 숫자가 운전자가 보는 직진 신호가 아니라 같은 접근로의 직진·좌회전·우회전 중 어느 하나라도 바뀌는 시점을 셌다. 빨간불에서 "3·2·1"이 끝나도 빨간불 그대로 "12초", "36초"로 다시 떴다.
3. 빨간불 동안 앞 단계 방향에 차가 새로 오면 그 단계가 끼어들거나 길어져 예측이 늘었다.

고치기 전 기록: 위로 튄 횟수 14번.

## 구현

| 내용 | 동작 |
|---|---|
| 초록 길이 고정 | `DemandSignalClock.Step`에서 초록 도중 연장(+4, +2)과 조기 종료를 없앴다. 단계가 시작될 때의 대기로 길이를 정한다(대기 3대 이상이면 기준+4초, 그 밖은 최소~6초, 보행 단계는 기준). 다른 방향에 아무도 없으면 초록이 끝에서 기다리는 동작은 그대로다(그동안 숫자 대신 "—") |
| 내 신호 기준 | HUD 남은 시간은 운전자가 보는 신호 하나(차선 신호등이 좌회전용이면 좌회전, 아니면 직진)가 바뀌는 시점만 센다(`TrafficSystem.PreviewLight`, `SignalPhasePreview.ProgramFor/AdaptiveFor`). 기존 이름의 함수는 예전 동작 그대로 둬 다른 검사와 호환한다 |
| 빨간불 예측 | 내 신호가 초록이 아닐 때는 앞 단계들이 모두 대기 차가 있어 최대 길이로 진행된다고 가정한 "가장 늦은" 시점을 보여 준다. 숫자는 늘지 않고, 앞 단계가 건너뛰어지면 한 번에 줄어든다 |

## 변경 파일

- `DemandSignalClock.cs`, `SignalPhasePreview.cs`, `TrafficSignalPreview.cs`, `DrivingReadability.cs`, `TrafficSignalDemand.cs`(검사용 읽기 전용 상태)
- 새 검사 `SignalCountdownProbe.cs`(`-signalCountdownCheck [-signalJunction ID]`, 결과 `artifacts/signal-countdown.txt`)
- `Editor/NavigationHudChecks.cs`(도중 연장 기대값을 고정 길이로)
- 기록: 이 파일, `docs/ui/2026-11-26-signal-countdown/after.txt`, `LATEST.md`

## 검증

검사용 빌드(당시 작업 폴더: HEAD 6eb5aee4 + 지도 세션의 미커밋 r78)로 했다.

| 검사 | 결과 |
|---|---|
| `DemandSignalChecks.Run` | 1,810건 통과(대기 많은 방향은 시작부터 늘린 길이, 굶는 단계 없음, 보행 정리) |
| `NavigationHudChecks.Run` | 86건 통과 |
| `NativeLeftSignalRegressionTests.Run` | 통과 |
| `-signalCountdownCheck`(J7973_3744 북행, 약 450초) | 숫자가 위로 튄 횟수 0(초록 0, 대기 차가 있는 빨간불 0). [기록](../ui/2026-11-26-signal-countdown/after.txt) |
| `-brightUiCheck` / `-speedLimitCheck` | 104 / 35건 통과 |
| `-districtTrafficTest` | 정체 0대. 기존 "green queue clears" 실패만 그대로 |
| `TrafficSignalLeftDemandRegressionTests.Run`, `PersistentGreenLeftTests.Run` | 실패. 이번 수정을 뺀 상태에서도 같은 지점에서 실패해 기존 문제다 |
| `-missionCheck` | 34번째 뒤 `PassengerJobs.Interact` 오류로 실패. 이번 수정을 뺀 빌드에서도 같아(직전 배포 306e2f5e에서는 96건 통과) 지도 세션의 미커밋 r78 또는 6eb5aee4 쪽으로 보고 그 세션에 알렸다 |

## 미검증 / 남은 것

- 사람 플레이로 다른 교차로의 신호 흐름(대기 줄이 길어지는지)을 보지 않았다. 도중 연장이 없어져 초록 도중 도착한 차는 다음 차례를 기다린다.
- 대기 차가 없는 방향은 빨간불 예측이 크게 바뀔 수 있다(그 방향 단계를 건너뜀). 택시가 서 있으면 대기로 잡힌다.
- 배포 실행본은 갱신하지 않았다.

## 실행·재현

- `EungyeTaxi.exe -contentTest -signalCountdownCheck [-signalJunction J7973_3744]` → `artifacts/signal-countdown.txt`
