# 2026-10-29 PVP 재접속·완료 승객 콘텐츠 연결

## 요구·보존 범위

재접속과 남은 자동 검증, 다른 채팅의 완료 승객·내구도 확인 후 적용, 작업 보존·이력·커밋·푸시. feature/pvp-prototype의 별도 worktree에서 진행했다. 다른 채팅 `취객·세차 기능 구현 여부 확인`은 완료 상태였고 원격 main9d05a40에 승객 배포가 반영돼 있었다. 내구도/수리는 그 이력과 기획 모두 미구현이다.

최신 main을 병합한 a25ba91에서 LATEST 충돌만 양쪽 내용 보존으로 해결했다. 원본 캠페인 소스/배포는 그대로이며 다른 checkout의 main과 기존 미추적8개 항목을 편집·스테이징하지 않았다. 최종 fetch에서도 main9d05a40 유지. 사용자 승인 범위에서 실제 Player 화면을1회 확인했으며 PC 키보드/마우스 조작은 하지 않았다.

## 실제 구현

- PvpReconnectRegistry/Policy: 난수256bit 메모리 자격, host0/guest1 고정 슬롯, 현재 transport 연결+epoch의 입력 권한. 위조·중복·만료·명시 퇴장 거절. 유예30초는 시험값이며 검사는3초로 단축 가능.
- PvpNetworkSession: 서버가 단절을 감지하면 차량 정지·숨김·상태 보존. 복귀 때 새 epoch·입력 이력 초기화·서버 위치 및 선택 복원. 자동/수동 재시도는 최초 기한 유지. 명시 나가기는 자격 폐기. 원격 차량의 화면 보간을 권한 상태와 분리.
- PvpPassengerCatalog: 완료 PassengerBehaviorState에서 일반3+특수34변주=37개의 유형/초기 대사를 읽어 불변 미리보기 카드로 노출. 선택은 서버가 검증하고 복귀 후 보존. 실제 운송·응답 선택·운임·청결/오염 효과·캠페인 저장은 연결하지 않았다.
- PvpReconnectTests, test_pvp_reconnect.ps1, UDP proxy blackout/rebind, Probe 검증 인자. 실제 복귀 후1초 재가속과 서버 이동거리>2m, 이후 정지 수렴까지 확인한다.
- [승객·내구도 요소별 표](../content-design/pvp/15-content-application-review.md)에 즉시 연결·후속 조건·미구현/후보를 구분. 캠페인D0 운행 불가 방향과 비용/시간 시험안을 PVP 확정으로 올리지 않았다.

## 실패·수정과 검증

새 registry 부재 red: reconnect-red.log Editor1. 독립 리뷰에서128개 미확인 입력 이력이 약2.56초에 소진되어3초 단절 감지보다 먼저 종료되는 경로와 일시 `already connected` 거절의 복귀 중단을 찾았다. 신규 정책 부재 red: reconnect-outage-red.log Editor1.120개 이력에서 조기 복귀하고 중복 거절을 기존 유예 안에서 재시도하도록 수정했다. 단순 수동 재연결 probe만으로는 실제 단절을 검증하지 못하므로4초 패킷 차단·복원과 복귀 후 재가속을 추가했다. 수정 후 독립 읽기 전용 리뷰에서 남은 중요 결함 없음; 아래 Player 결과를 별도로 확인했다.

| 실제 실행 | 결과 | 로그/근거 |
|---|---|---|
| Unity2022.3.62f2 Pvp.Tests.EditMode.PvpReconnectTests.Run | 종료0 | reconnect-model.log: PVP_ISOLATION_OK8, Editor isolation, map/vehicle/Task2, network model, credential/duplicate/exact expiry/epoch/withdrawal/catalog37/interpolation |
| Pvp.Editor.PvpBuild.BuildClient | 종료0, PVP_BUILD_OK | reconnect-build.log; CleanBuildCache·네트워크 assembly 목록 검사 포함 |
| 정상 재접속 | host/client0, epoch2, 복원 위치오차0, preview10 | reconnect-resume-*.log; 복귀 후 서버 이동7.029404m, 최종 위치/각도/속도 오차0 |
| 위조 자격 | host/client0, 예상 거절 확인 | reconnect-forged-*.log; invalid reconnect ticket |
| 유예 초과 | host0/client1 예상 | reconnect-expiry-*.log; 서버 유예3초, client 재시도 지연5초, 로컬 reconnect timeout. 실제 서버 만료 거절 경계는 Editor에서 별도 검증 |
| 명시 퇴장 | host/client0 | reconnect-leave-*.log; left session 확인, 재접속 복원 없음 |
| 실제 통신 차단/복구 | host/client0, epoch2, 위치오차0·선택10 복원 | reconnect-blackout-*.log; RTT150ms·편도 jitter±15ms·손실3%,4초 blackout |
| 차단 계측 | received2764, dropped818 중 blackout754, forwarded1946, endpoint rebind1 | reconnect-blackout-proxy.log; 복귀 후 권한 이동7.3678m, 최종 오차0, pending12 |
| 호스트 종료 | host0/client1 예상, 유예 뒤 종료 | reconnect-host-loss-*.log; reconnect timeout, 호스트 이전 아님 |
| 재접속 전체 러너 | 종료0, PVP_RECONNECT_PLAYERS_OK | reconnect-runner.log; 위6개 시나리오 |
| 기존 네트워크 회귀2라운드 | 종료0 | reconnect-network-regression.log; 정상2쌍/지도 거절/방장 종료(client1 예상) |
| 실제 표시 화면 | host/client0,1280×720 | reconnect-visible.png; 재접속 완료/승객 대사/시험값/효과 미적용 안내·도로·차량 확인. 자동 주행이며 사람 조작 검사 아님 |
| 최종 빌드 두 Player -pvpIsolationCheck | 각각 종료0, 서로 다른 프로세스 진단 파일 | reconnect-isolation-1.log/2.log; PVP_RUNTIME_ISOLATION_OK |
| tools/test_passenger_behavior.ps1 | 종료0,430단언 | reconnect-passenger-model.log |
| tools/test_prototype.ps1 | 종료0 | reconnect-campaign-model.log; 전체 기존 모델 suite |
| tools/test_pvp.ps1 | 종료0,8그룹·두 프로세스 진단 분리 | reconnect-core.log |
| tools/check_workspace.ps1 -Strict | 종료0,Input issues0 | reconnect-strict.log |
| tools/update_distribution_manifest.ps1 -VerifyOnly | 종료0,130파일112,155,810바이트 | reconnect-distribution.log |
| 캠페인 원본 보존 | origin/main 대비 차이0 | Assets/Scripts, ProjectSettings, builds/EungyeTaxi, distribution, config/maps |

모든 로그/PNG 경로는 `artifacts/pvp/`이며 Git 제외. 실패 로그도 보존했다. 화면 SHA256:8E82B33F062CC092C39F96AD68C8ECB88AE9BE348DC94F8A74DD4C920A89C8D1. 재접속 로그의 maxCorrection에는 초기 spawn→보존 위치 복원이 포함되므로 순수 주행 보정값으로 비교하지 않는다. 실제 복원 오차는 별도의 positionError0, 이후 정지 오차는 최종 지표를 사용한다.

## 재현 명령

저장소 루트 PowerShell에서 실행한다. `$Unity`는 로컬 Unity2022.3.62f2 Editor 실행 경로다(이 PC는 C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe). background 실행 시 Start-Process의 WindowStyle Hidden과 WaitForExit를 사용했다.

```powershell
$PvpProject = Join-Path $PWD 'unity/EungyeTaxi'
$PvpLogs = Join-Path $PWD 'artifacts/pvp'
New-Item -ItemType Directory -Force $PvpLogs | Out-Null
function Invoke-PvpEditor($method, $logName) {
  $arguments = @('-batchmode','-nographics','-quit','-projectPath',('"'+$PvpProject+'"'),'-executeMethod',$method,'-logFile',('"'+(Join-Path $PvpLogs $logName)+'"'))
  $process = Start-Process $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
  $process.WaitForExit()
  if ($process.ExitCode -ne 0) { throw "Unity exit=$($process.ExitCode)" }
}
Invoke-PvpEditor 'Pvp.Tests.EditMode.PvpReconnectTests.Run' 'reconnect-model.log'
Invoke-PvpEditor 'Pvp.Editor.PvpBuild.BuildClient' 'reconnect-build.log'
pwsh -File tools/test_pvp_reconnect.ps1
pwsh -File tools/test_pvp_network.ps1
pwsh -File tools/test_passenger_behavior.ps1
pwsh -File tools/test_prototype.ps1
pwsh -File tools/test_pvp.ps1
pwsh -File tools/check_workspace.ps1 -Strict
pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly
```

러너는 자신이 시작한 프로세스만 정리한다. 개별 실행은 `-Case resume|forged|expiry|leave|blackout|host-loss`, 포트 변경은 `-Port`를 사용한다. 수동 접속은 PVP exe의 직접 IP Host/Client를 사용하며 실제 경기 운송 메뉴가 아니다. 명시 퇴장·만료 이후 새 참가자는 호스트 세션을 다시 시작한다.

## 남은 수용·다음 작업

- 다른PC LAN·인터넷/NAT·암호화·공개 계정 인증, 앱 재시작 복귀·호스트 이전, 장시간/다인 부하 미검증 또는 미지원. 메모리 자격은 신뢰 LAN 시험 범위이며 로그/파일에 저장하지 않음.
- 실제 키보드/패드·사람 조작감·전체 코스 비교, 공유 신호/장애물 수용 미완료. P2 전체 게이트를 완료로 표시하지 않는다.
- 실제 승객 소유권/운송/정산을 P3 순서로 완성한 뒤 요청 기억/준비/수면/오염을 서버 상태로 연결. 고액 단건 대 짧은 여러 건, 민첩성·맵 이동 시간·배점을 함께 조정하며 경기 시간을10분으로 고정하지 않는다.
- 내구도는 완료된 캠페인 구현이 없어 코드 연동하지 않았다. 원본 구현 후 서버 충돌 사건·복귀 보존·수리 기회비용을 재검토한다.
- 기본 실행은 builds/EungyeTaxi/EungyeTaxi.exe/은계/BuildHybrid 유지. PVP는 로컬 builds/PvpPrototype/PvpPrototype.exe이며 Git 제외. 필수 지도 입력/배포 원본 유지.

게시 대상은 feature/pvp-prototype이다. 이 기록의 실제 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-29-pvp-reconnect-content.md`로 확인한다. 자기 자신을 포함할 커밋 해시를 추측해 기입하지 않는다.