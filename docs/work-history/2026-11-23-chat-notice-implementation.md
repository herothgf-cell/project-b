# 채팅형 운행 알림 구현

## 요구사항

승인된 [채팅형 운행 알림 시안](../ui/2026-11-23-chat-notices/README.md)을 은계 싱글 실행본에 적용한다.

## 실제 구현

- `DriveNotifications.cs`: 문자열 목록을 `FeedItem{kind,text,delta,minute,trip}`로 바꿨다. 종류는 승객 만족도·법규·연료·목적지·상황·시스템·TIP 7가지다.
  - 승객 만족도는 얼굴 아바타가 있는 말풍선이다. 같은 운송에서 이어지면 아바타를 생략하고, 운송이 끝나면 흐리게 표시한다.
  - 법규 점수는 왼쪽 색 띠와 방패/경고 그림이 있는 줄로 표시한다. 증감은 초록/빨강 칩으로 보여준다.
  - 연료는 게이지가 있는 경고 카드다. 5% 이하는 빨강이다. 목적지는 파란 카드(장소 굵게·설명·M 지도), 시스템은 가운데 회색 줄, TIP은 연한 메모로 표시하고 최신 1개만 남긴다.
  - 게임 시각이 10분 이상 벌어지면 시각 구분선을 넣는다. 위로 스크롤한 동안은 자동으로 내리지 않고 "새 알림 N ▼"를 띄운다. 머리줄 "전체/점수" 필터에서 점수는 승객·법규만 보여준다.
  - 패널을 숨긴 동안 온 알림 수는 "알림 N ▶" 탭에 표시한다.
- `ServiceLawZones.cs`: 패널 바닥에 따로 있던 버튼을 해당 카드 안으로 옮겼다. 주유소 안내, 정차 지점 점유의 "안전한 곳에서 대기/대체 지점 선택", 운송 결과의 "운전 평가"가 해당한다. 버튼이 있는 카드는 기록 아래에 고정하고, 누를 수 없을 때는 이유를 한 줄로 보여준다.
- 기존 공개 값(`VisibleText`, `VisibleTexts`, `Feed` 문자열 앞머리, `FuelActionVisible`)은 유지했다. 패널 위치·크기·1인칭 위치·PvP 화면은 그대로다.
- 검사: `ShiftDevEndProbe`에 종류별 카드 확인과 `-chatNoticeCapture` 캡처를 추가했다. TIP 검사는 새 규칙(최신 1개 유지, 게시 횟수 증가)으로 바꿨다. `PassengerCareProbe`에는 점수 흐름만 실행하는 `-scoreFlowOnly`를 추가했다.

## 변경 파일

- `unity/EungyeTaxi/Assets/Scripts/DriveNotifications.cs`, `ServiceLawZones.cs`, `ShiftDevEndProbe.cs`, `PassengerCareProbe.cs`
- `builds/EungyeTaxi/` 실행 폴더, `distribution/EungyeTaxi-build-manifest.json`
- `docs/ui/2026-11-23-chat-notices/game-*.png`, `README.md`, 이 기록, `LATEST.md`, 체크리스트

## 검증(실제 명령과 결과)

- Unity 2022.3.62f2 `PrototypeBuild.BuildHybrid` 종료 0, 컴파일 오류 0. `tools/check_workspace.ps1 -Strict` 필수 입력 문제 0.
- `pwsh -File tools/test_home_base_player.ps1`: 위치 제어 검사 24건 통과. 기존 23건에 종류별 카드 검사 1건을 더했다. 저연료 1회 게시·소멸, TIP 반복도 포함한다.
- `-homeBaseCheck -chatNoticeCapture` 1920×1080: 9건 통과. 법규 카드, 고정된 연료 카드·버튼, 1인칭, 숨긴 동안의 미확인 수를 캡처했다([외부 시점](../ui/2026-11-23-chat-notices/game-chase.png), [1인칭](../ui/2026-11-23-chat-notices/game-first-person.png), [숨김](../ui/2026-11-23-chat-notices/game-hidden.png)).
- `-brightUiCheck`: 102건 통과. 세 해상도·두 시점에서 알림과 길 안내가 겹치지 않는다.
- `-passengerCareTest -scoreFlowOnly`: 알림 관련 13건(감점·요청 이행·법규 위반·회복 알림, 재실행 복원) 통과. 이어지는 영수증 점수 1건은 실패했다(알림 외 영역, 아래 참고).
- `-navTipFeedbackTest`: 패널 숨김/표시·점유 대기 버튼 등 8건 통과. 마지막 서쪽 유턴 경로 검사 1건은 실패했다(지도 경로, 알림 외).

## 기존 실패(이번 변경 전부터)

- `-passengerCareTest` 전체 실행은 현재 HEAD 배포본에서도 같은 "dirty roadside approach…" 검사(137건째)에서 실패한다. 이 기록의 변경 전 실행본으로 확인했다.
- `-driveFeedbackTest`는 `ServiceStops.FuelAmount`/`AttentionFor`의 NullReference가 반복되어 끝나지 않아 중단했다. 알림 코드가 아니라 시험 장치의 계정 준비 문제로 보인다.
- 영수증 점수 검사, 서쪽 유턴 경로 검사의 원인은 따로 확인해야 한다.

## 미검증

- 수동 주행 중 가독성, 승객 말풍선의 실제 화면 캡처(점수 캡처 단계가 위 영수증 실패 뒤라 실행되지 않음), PvP 두 PC 화면.

## 재현

```powershell
.\builds\EungyeTaxi\EungyeTaxi.exe -homeBaseCheck -chatNoticeCapture -screen-width 1920 -screen-height 1080 -shiftCaptureOutput artifacts/chat-notices-player -logFile artifacts/chat-notices-player/player.log
```
