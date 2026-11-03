# 2026-11-03 신호 변경 잔여 초 복원

## 요청과 구현

사용자가 첨부한 네비 카드의 신호 안내 아래에 ‘신호 바뀌기까지 몇 초’ 표시를 텍스트로 복원하도록 요청했다. 기존 main 게시/통합 실행본 갱신 지시의 후속이다.

- 작업 시작 git status clean. 최신 원격 main337ab94까지37커밋을 먼저 fast-forward해 승객 대화/PVP/생활 계획 변경을 보존했다.
- `DrivingReadability.SignalCountdownText`는 기존 `TrafficSystem.SecondsToSignalChange`를 읽어 ‘신호 변경까지 N초’를 표시한다. 신호 시계나 주기는 바꾸지 않으며 음수/NaN/Infinity·대상 없음은 숫자를 만들지 않는다.
- 네비 하단 안내를 기존196px 카드 안의18px 세 줄로 배치했다. 실제 신호 상태·우회전/저속 회전 조건·교육용 비보호 안내는 유지한다. 숫자를 알 수 없는 교육/개별 프로그램 신호는 기존 조건 문구만 표시한다.
- 네비/운행 요약 두 카드·지도·차량·길찾기·게임 규칙은 유지한다.

## 파일·검증 계획

변경: `unity/EungyeTaxi/Assets/Scripts/DrivingReadability.cs`, 격리 화면 확인용 `BrightUIProbe.cs`, `Assets/Editor/SignalCountdownTests.cs`/meta, 관련 UI 기준/체크리스트/작업 이력. 최종 BuildHybrid 실행 폴더와 manifest도 함께 갱신한다.

SignalCountdownTests의 실제 신호 시계를 사용한 녹색/황색/적색·초 올림·미확인 신호 숨김·읽기 후 시계 불변 검사. 초기 RED는 exit1 ‘countdown text is missing’으로 확인했다. 최초 구현 Build exit0 / SIGNAL_COUNTDOWN_OK·테마·내비15·BuildHybrid 통과.

독립 읽기 전용 리뷰에서 계산/행 배치의 결함 없음. 명시적 테스트 플래그에서만 실제 신호 접근 위치를 맞춰 잔여 초를 캡처한다. 사용자 저장 대신 기존 brightUiCheck 임시 저장 격리를 사용한다. 최종 빌드/화면/게시 결과는 아래에 실제 결과로 추가한다.

## 재현

로컬 Unity 경로(PC 환경 정보): `C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe`.

`Unity.exe -batchmode -nographics -quit -buildTarget Win64 -projectPath <repo>/unity/EungyeTaxi -executeMethod SignalCountdownTests.Build -logFile <repo>/artifacts/signal-countdown/final-build.log`

`builds/EungyeTaxi/EungyeTaxi.exe -oneDayTest -brightUiCheck -signalCountdownOnly -brightUiOutput <repo>/artifacts/signal-countdown/player -screen-fullscreen 0 -screen-width 1600 -screen-height 900 -logFile <repo>/artifacts/signal-countdown/player.log`

이번 화면 검사는 제어된 신호/위치 fixture이며 사람의 직접 운전·전체 캠페인·실제 현장 신호 주기 확인이 아니다. 계산할 수 없는 개별 신호 프로그램의 남은 초를 새로 개발하는 범위도 아니다.

최초 화면 fixture는 도달 불가능한 먼 목적지를 선택해 파란 네비 assertion만 실패했고, 흰 fallback 카드의 카운트다운은 실제 표시됐다. fixture는 기존 StartFreeGuidance의 실제 전방 경로로 바꿨다. 일반 저장 SHA 비교 차이는 별도 사용자 실행본 C:/git/프로토 게임 모음/EungyeTaxi/EungyeTaxi.exe(PID5664)의 지속 자동 저장과 겹쳐 불변 수용으로 사용하지 않는다. 이 프로세스를 종료하거나 저장을 되돌리지 않았으며, 사용자 저장14개를 artifacts/signal-countdown/user-save-snapshot에 복사/해시 확인했다. 자동 승인 검토의 첫 재실행 거절 후 실제 임시 SavePath 분기·백업을 증명하고 -oneDayTest/-brightUiCheck의 이중 검사 격리로 재실행을 승인받았다.

추가 전방 경로 fixture도 이 신호 위치에서는 연결되지 않아 원래 요청과 무관한 경로 assertion은 제외했다. 최종 화면 검증은 기본 안내 카드의 실제 잔여 초/배치를 대상으로 한다. 파란 활성 경로 카드 자체의 별도 캡처는 이번에 확보하지 않았으며, 같은 DrawCompactSignal 함수가 흰색/파란색 양쪽에서 호출되는 코드와4px 하단 여유는 독립 검토했다. 두 번째 격리 실행에서 일반 저장 비교는0차이였지만 상시 별도 실행본이 있으므로 최종 수용은 임시 SavePath 확인을 기준으로 한다.

## 최종 검증

accepted-build.log Unity exit0: SIGNAL_COUNTDOWN_OK·테마·내비15·BuildHybrid 통과. accepted.log Player exit0 / SIGNAL_COUNTDOWN_PLAYER_RESULT PASS checks8: 임시 SavePath·실제 신호 잔여 초·1280/1920 캡처4개(신호 화면2개) 확인. 해당 신호 화면2개를 docs/ui/2026-11-03-signal-countdown에 보관했다. 마지막 실행 전후 사용자 저장14파일 SHA 비교0차이. 별도 게임의 자동 저장과 겹친 최초 비교 실패와 구분한다. Strict exit0 / issues0, 프로젝트 지도 hash35d1bc43… 유지. UI 변경 파일 외 최신 승객/PVP/대화 소스는 보존했다. 배포140파일115,434,078바이트이며 manifest/게시 결과는 후속 기록을 따른다.

소스 커밋은 git log -1 --oneline -- <이력 파일>로 확인한 ba1b7e88948b4b346d4a7c36adcf5fd9ea59aa7a. 이 실제 해시로 validation.json의 신호/화면/격리 결과를 반영해 manifest를 재생성했다. 통합/PVP VerifyOnly 각각 exit0:140파일115,434,078바이트 크기·SHA256 전수 일치. main 게시 결과는 후속 절에 기록한다.
