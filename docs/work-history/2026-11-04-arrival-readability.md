# 2026-11-04 내비 이탈·승하차·M 내 위치·글자 개선

## 요구와 구현

사용자가 [앞선 조사](2026-11-04-playtest-feedback-audit.md)의 네 가지부터 싱글/PVP 공통 구현·검증·통합 exe 빌드·main 푸시를 요청했다. 조향/제동·신호·사고 범위를 이번에 변경하지 않는다.

| 부분 | 실제 구현 |
|---|---|
| 내비 이탈 | 새 연결 경로를 찾지 못하면 마지막 유효 선을 흐린 색으로 유지하고 재탐색 안내. `DisplayPath`와 유효 `path/hdRoute` 분리. 옛 선은 H·ETA·운송 경유 판정에 쓰지 않음. 복귀 성공 시 교체, 목표 변경/취소/완료/지도 초기화 시 제거 |
| 승하차 | 현재 목표 정차 칸의 테두리/방향/현장 글자, HUD의 큰 `F · 승객 승차/하차`. 위치·방향·속도·점유·대체 보행을 읽어 부족한 조건을 설명. 내비가 없어도 실제 정차 조건으로 판단 |
| 준비/예외 | 적재 준비/선택, 하역, 짐 재고정,20초 경유 용무, 취객 기상 대기는 준비/대기 안내. 실제 가능하지 않은 F를 표시하지 않음. 기존 입력·요금·정산 계약 보존 |
| M 내 위치 | 흰/파란 이중 원·노란 방향 화살표·내 위치 라벨, 열기/내 위치 버튼의 짧은 강조. 화면 밖 위치는 가장자리 방향 표시. 기존 호출/시설 포커스를 강제로 옮기지 않음 |
| 글자 | 주요 HUD 최소16픽셀·고해상도 전체 확대, M 논리/화면 좌표 분리·글꼴 확대. 긴 도로명은 글자를 작게 줄이는 대신 말줄임/툴팁. 상태 게이지 숫자는0~100 수치를 간단히 표시. 도움말/운행/상태 영역과 PVP 목록 행높이 보정 |
| LAN | 같은 운송·같은 목표·같은 경로 옵션이면 유효 경로뿐 아니라 표시 보존 상태도 재사용. 실제 승하차 속도는 기존 replica/authority 판정과 같은 값을 읽음. 서버 승하차·정산 권한 유지 |

## 파일과 작업 판단

- 경로: `RoadNavigation.cs`, `MapDestinationUI.cs`, `DrivingReadability.cs`, `PassengerJobs.cs`.
- 정차: 새 `StopArrivalGuidance.cs`, `StopArrivalPresentation.cs`, `DriveSession.cs`, `CockpitUI.cs`.
- 화면: 새 `UiReadability.cs`, `FullMapOverlay.cs`, `MapPresentation.cs`, `DriveHUD.cs`, `DriverStatusGauges.cs`, `ServiceStopsPlannerUI.cs`.
- 검사: 새 `Assets/Editor/ArrivalReadabilityChecks.cs`, `Assets/Scripts/ArrivalReadabilityProbe.cs`와 각 meta.
- 관련 기획05/현재상태·조사 후속·체크리스트·작업 계획/LATEST를 함께 갱신한다.
- Ruling: 기존 tracked 변경이 없고 Unity 캐시/프로젝트 위치를 유지해야 하므로 D:/Git/project-b의 `codex/nav-stop-readability` 브랜치에서 진행했다. 기존 미추적 라이선스/마인드맵/도구 자료를 포함·덮어쓰기하지 않았다.
- Ruling: 사용자가 요청한 글자 개선은 별도 설정/저장 스키마 대신 해상도 자동 배율과 주요 글자 최소 크기로 구현했다. 캠페인·PlayerPrefs 저장을 늘리지 않는다.

## 실제 검증

| 구분 | 명령/증거 | 결과 |
|---|---|---|
| 경로 RED | `ArrivalReadabilityChecks.Run`, `artifacts/arrival-readability-red.log` | exit1: 연결 없는 차로 이탈 때 이전 선 소실 재현 |
| 안내/크기 RED | `artifacts/arrival-guidance-red.log` | 경로 수정7개 통과, 미구현 안내/글자 정책2그룹 실패 |
| 준비 RED | `artifacts/arrival-preparation-red.log` | exit1: 짐 준비 중 잘못된 승차 준비 완료 표시 재현 |
| 집중 Editor | `ArrivalReadabilityChecks.Build` | 최종30 assertions·기존 NavigationCue/MapReadability 통과. H 거부/ETA0·목표교체·복귀·정상1점 도착·준비/용무/작업 우선·배율 검사 |
| 최종 통합 빌드 | `artifacts/arrival-final-build-retry.log` | exit0, 내부 `PrototypeBuild.BuildHybrid`, PROTOTYPE_BUILD_OK |
| 실제 싱글 객체 | `-arrivalReadabilityTest -hybridV2 -arrivalCapture` | exit0,19 state/interaction checks. 실제 경로 상실·회색 표시 데이터·nav 없는 F승차/하차·1회 정산·방향/칸·시설 포커스 |
| 실제 PVP 객체 | `-pvpArrivalReadabilityCheck -arrivalCapture` | exit0,18 checks. A2 같은 검사·봇3 모드 보존. 캠페인 파일 저장 없음 |
| 실제 GUI 클릭 | `-arrivalReadabilityTest -hybridV2 -arrivalClick` | exit0,19 state checks+실제 마우스 호출 수락1회. `computer-use` 도구로 반환된 테스트 게임 창만 조작. 고해상도 최대화 지도에서 실제 GUI.Button 수락·운전 복귀 확인 |
| LAN 두 프로세스 | `pwsh -File tools/test_pvp_native_lan.ps1 -Port 18391 -Reconnect -TimeoutSeconds 240` | exit0,host40/client54·재접속 통과. `artifacts/pvp/native-lan-20261104-144611-7610391d` |
| 화면 | `artifacts/arrival-{single,pvp}-{hud,map}-{1280,1600,1920}.png`, `arrival-{single,pvp}-drop.png` | 실제1280×720/1600×900/1920×1080 렌더·M Repaint 확인, 픽업/하차와 지도 이미지 직접 열람 |
| 사용자 데이터 | `artifacts/arrival-user-saves-before.json`의4파일 크기/SHA256 재대조 | 변경0. 검사 저장은 artifacts 또는 PVP 메모리만 사용 |
| 검토 | 별도 읽기 전용 whole-branch 리뷰 | 이중 확대 버튼·짐 준비·경유 대기3건 수정, 후속 리뷰에서 추가 중요 문제 없음 |
| 커밋 전 재검사 | `ArrivalReadabilityChecks.Run`, `artifacts/arrival-precommit-checks.log` |30개 통과·Unity 정상 batchmode 종료. 기획/이력7문서 로컬링크124개 누락0·`git diff --check`와 workspace Strict 종료0/input issues0 |

첫 GUI 캡처는 M을 연 첫 프레임만 잡아 지도 없이 나온 것으로 확인했다. Repaint 계측과 프레임 대기를 추가한 후 실제 지도 렌더를 확인했다. 또한 예전 `MapInteractionProbe`의 고정2초 대기는 이 PC의 초기 월드 생성보다 짧아 실패했으므로 합격 근거에서 제외했다. 새 검사는 `StartupReady` 이후 실제 클릭을 검증했다.

최종 빌드 한 번은 Unity 자체 MovedFromExtractor가 출력 없이 exit1로 실패했다(`arrival-final-build.log`). C# 오류가 아니며 동일 소스 재시도에서30검사/BuildHybrid가 모두 exit0이었다. 실패한 빌드를 배포 근거로 쓰지 않는다.

## 범위와 남은 확인

- 위 운송 검사는 위치를 제어한 실제 게임 객체/입력 경로 검사다. 자동 물리로 전 구간 운전하거나 사람이20분/3일 플레이한 검사와 다르다. LAN은 같은PC loopback이며 다른PC/실물 패드 장시간 체감은 남아 있다.
- 2560×1440의 수식/배치 정책은 Editor에서 검사했다. 이 PC의 Windowed 화면은 최대1920×1080으로 제한되어2560 요청 캡처도 실제1920이었다. 파일 이름만으로2560 실화면 통과를 주장하지 않는다.
- 실제 지도 형상·필수 입력·OSM 대조·지도 ZIP·운임/신호/주행 물리·회사/제품/캠페인 경로는 유지한다. Unity가 생성한 DoNotShip 디버그 폴더는 artifacts로 보관해 실행 배포에서 제외한다.
- 기본 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`. 은계 영업/PVP 메뉴, M지도·F승하차 방식은 동일하다. 새 소스/배포 실제 해시와 manifest·main 게시 확인은 아래 후속 절에 남긴다.
- 소스 기록은 `git log -1 --oneline -- docs/work-history/2026-11-04-arrival-readability.md`와 배포 manifest를 기준으로 확인한다.

## 소스·배포 확정

- 실제 소스 커밋: `6b7f054177a95f44846f14505f3bcd9a8b2883c4` (`git log -1 --oneline -- 이 기록`으로 확인). 빌드 후 코드는 바꾸지 않았고 커밋 전30개 Editor 검사를 다시 통과했다.
- 통합 실행 폴더140파일,122,934,612바이트. 실제 위 소스 해시와 이번 `artifacts/arrival-validation.json`으로 manifest를 갱신했다. 통합/PVP VerifyOnly 모두 종료0, 전파일 크기/SHA256 일치.
- 배포에는 Unity 생성 `FirstDrive.unity`의 Mode Selection/eungye 장면과 실행 폴더 전체를 포함한다. 생성된 Burst DoNotShip 디버그 자료는 배포 밖 artifacts에 보존했다. 지도 원본/프로젝트 지도/ZIP 변경 없음.
- main 원격 게시 해시는 실제 push·원격 대조 후 아래 게시 영수증으로 추가한다.
