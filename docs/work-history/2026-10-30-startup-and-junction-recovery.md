# 영업 시작 복구와 시작 교차로 정체·신호등

## 요구와 원인

| 신고 | 확인한 원인 | 수정 |
|---|---|---|
| 1일차 영업 시작 무반응 | 저장 복원 거부 후 `Depart`가 반환하지만 준비 화면은 정상 버튼을 표시 | 복원 실패 전용 안내, 복구 확인창 연결, 원본·백업 보존 후에만 새 캠페인 |
| 시작 교차로 파란 봇 3대 정체 | J1927_5181의 실제 회전 곡선 밖을 가정한 중심 사각형 검색이 대기 차량을 누락. 진입한 회전차와 대기차가 서로 막음 | 정지점~출구의 실제 곡선을 포함하는 캐시 범위에서 검색. 기존 몸체 점유 판정·신호·예약 유지 |
| 좌회전 불가 방향의 좌회전 신호 | 교차로 전체의 좌회전 허용 값을 모든 접근 신호등에 사용 | 접근 방향별 활성 좌회전 연결/진출로로 3색·4색 결정. HUD도 좌회전 불가 안내 |

사용자 실행 경로는 PvP 작업 폴더의 기본 싱글 exe였다. 수정 전 Managed DLL은 main 기본 싱글과 동일했다. 사용자 Player.log는 `Invalid live checkpoint/map revision`을 기록했지만 사용자가 새 캠페인을 시작한 뒤 원래 거부된 저장이 교체되어, 최초 실패 필드를 특정하거나 해당 원본을 복구했다고 주장할 수 없다. 사용자 AppData는 읽기만 했고 모든 검사는 `artifacts/` 격리 저장을 쓴다.

새 캠페인 확인 전 취소하면 기존 파일을 유지한다. 확인 시 `.recovery-UTC-guid/primary.json`, `backup.json`에 존재하는 파일을 바이트 그대로 보존한다. 보존 실패 시 초기화를 중단한다. 지도/진행 중 운송의 복원 검증을 우회하지 않는다. 정상 준비 저장은 새 캠페인 없이 한 번의 출발로 영업한다.

## 변경 파일

- 런타임: `BusinessProgress.cs`, `OneDayHUD.cs`, `TrafficSystem.cs`, `DrivingReadability.cs`.
- 검증: `CampaignStartupProbe.cs`, `LiveLoadGuardProbe.cs`, 신규 `JunctionGridlockProbe.cs`, `ApproachSignalVisualProbe.cs`, `CurveFootprintTests.cs`, `ApproachSignalHeadTests.cs`, `StartJunctionChecks.cs`와 meta.
- 빌드 생성 장면 `Assets/Scenes/FirstDrive.unity`, 기본 실행 폴더, 배포 manifest, 관련 기획/체크리스트/범위/최신 이력.

위 소스 경로는 `unity/EungyeTaxi/Assets/` 기준이다. 지도 JSON·필수 입력 manifest·지도 ZIP은 변경하지 않는다. 기존 사용자 untracked와 Office 임시 잠금 파일을 보존한다.

## 검증 증거

| 단계 | 명령·보고서 | 실제 결과 |
|---|---|---|
| 저장 RED | 기존 동작 빌드 후 `-liveInvalidTest`, `artifacts/start-junction-save-red.log` | 종료1, 복원 거부 상태 출발이 확인창을 열지 않음 |
| 신호등 RED | `ApproachSignalHeadTests.Run`, `artifacts/start-junction-head-red.log` | 종료1, 닫힌 서쪽 접근에도 좌회전 화살표 |
| 자연 NPC RED | `-junctionGridlockTest`, `artifacts/start-junction-gridlock-baseline.txt` | 종료1, 149.67초 시뮬레이션에서 선두 90초 정체. 72차량/일반 보행 유지 |
| 곡선 검색 RED | `CurveFootprintTests.Run`, `artifacts/start-junction-curve-red.log` | 종료1, 몸체 판정은 점유를 발견하나 중심 검색에서 빠져 진입 허용 |
| 최종 Editor·빌드 | `StartJunctionChecks.Build`, `artifacts/start-junction-final-build.log` | 종료0. 곡선 점유 거부/해제 후 진입, 방향별 신호등, Startup11,672, BuildHybrid 통과 |
| 최종 실제 Player 7종 | `artifacts/start-junction-final-hidden-results.json` 및 각 로그 | 모두 종료0. 자연 NPC200.92초/정체 없음, 저장 복구, 월드468, 운송33, 서비스 법규, 운전 피드백, 실제 좌회전 정책95 |
| 최종 정상 창 Player 3종 | `artifacts/start-junction-final-visible-results.json` | 모두 종료0. 신규 준비/출발10검사, 별도 프로세스 준비 저장 재개12검사, 신호/HUD1280·1920 실제 캡처 |
| 필수 입력 | `pwsh -File tools/check_workspace.ps1 -Strict`, `artifacts/start-junction-workspace-strict.log` | 종료0, Input issues0 |

초기 숨긴 창의 시작 화면 캡처는 검은 이미지로 실패했으므로 렌더링 검증으로 인정하지 않는다. 최종 정상 창에서 준비/출발과 3색등·좌회전 불가 문구를 실제 캡처로 확인했다. 신호 캡처는 고정 위치의 진단용 HUD fixture이며 전체 캠페인 배치를 대신하지 않는다. 코드 독립 검토에서 구체적인 P1/P2 지적은 없었다. 위치/상태 제어 검사와 자연 NPC 물리 진행을 구분하며 사람의 20분/3일 전체 캠페인 검사는 수행하지 않았다. 현재 수정의 별도 FPS 측정은 하지 않았다.

## 실행·재현

기본 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`, 기본 은계 지역, `PrototypeBuild.BuildHybrid`. Unity 로컬 환경은 2022.3.62f2다. Editor `-batchmode -quit -executeMethod StartJunctionChecks.Build`로 빌드 후 `-hybridV2`와 표의 검사 플래그를 사용한다. `-liveInvalidTest` 전 격리 저장의 mapRevision을 불일치로 설정하고 primary와 bak를 준비한다. `-campaignStartupTest` 뒤 `-campaignStartupPreparedReloadTest`를 실행하면 별도 프로세스 준비 저장 재개를 확인한다.

## 배포 검증

소스 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-30-startup-and-junction-recovery.md`로 확인한 `7b38ac0`이다. 이 소스에 대해 배포 manifest를 생성했고 `pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly` 종료0, 130파일/112,309,087바이트의 크기·SHA256을 확인했다. 실제 main 게시 해시는 게시 후 기록한다.

실제로 열려 있던 PvP 작업 폴더는 별도의 소스 수정이 새로 생겼고 게임도 실행 중이다. 이 작업에서 해당 미커밋 소스에 main을 강제 병합하거나 사용자 게임을 종료하지 않는다. 기본 main 실행 폴더의 검증·게시와 해당 작업 폴더의 교체 완료를 구분한다.
