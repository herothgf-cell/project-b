# 2026-10-29 PVP 공유 교통·승객 경쟁·정산

## 요구와 작업 범위

공유 신호·장애물 검증부터 승객 소유권·운송·정산까지 진행하고 다른PC/사람 검증은 별도 정리, 다른 채팅 보존, 이력·커밋·푸시. 기존 feature/pvp-prototype worktree와1088461의 기반에서 이어갔다. 실행 계획은 [16](../content-design/pvp/16-shared-world-competition-plan.md), 콘텐츠별 수용/잔여 표는 [17](../content-design/pvp/17-competition-acceptance.md)이다.

시작/검증 중 fetch의 최신 main은9d05a40이다. C:/git/project-b의 main에는 다른 채팅이 내구도/수리 소스·배포를 작업 중인 변경이 생겼다. 이를 읽기 전용으로 확인했으며 복사·복구·스테이징·체크아웃 변경을 하지 않았다. 이번 PVP 커밋에 상대의 미커밋 파일을 포함하지 않는다. 기존 캠페인 코드/실행 폴더/지도는 origin/main 기준 그대로다.

## 실제 구현과 판단

- Runtime/Traffic/PvpTrafficAuthority: 중앙 시험 신호·다음 전환tick·정수 위치의 이동 장애물. 권한 차량/예측 재계산은 해당 tick의 swept blocker를 사용. 교통 이벤트는 서버만 확정한다.
- Runtime/Match/PvpRoundController: 준비/카운트다운·Running/Closing/Settling/Results·무효/이탈, 최종 종료tick 자격 동결. 경기 시간은 호스트 필수 입력이며 제품 기본값 없음.
- Runtime/Passengers/PvpPassengerAuthority/Commands: 유효 합법 경로의 공유 공급·고정 약정, 서버 위치/방향/차로/속도/연속 정차·빈차·revision 검증. 같은 tick 요청은 시드/승객/선수로 중재하고 수신 순서로 승자를 정하지 않는다. 경기 ID·선수·요청 ID로 응답을 보존한다.
- Runtime/Scoring/PvpScoreLedger/SettlementService: 완료와 운임/안전 가산 원자 확정, 사건 상향 차액, 첫 양수 감점 시점의 안전 구간, checked 정수 계산, 음수/동점/무운송 제외. 원장은 client 합계로 덮어쓰지 않는다.
- Runtime/Network/PvpCompetitionBridge와 Session/Motor/Probe: 실제 RPC/공유 snapshot·준비/시간·목표/승하차·거절·결과 화면, 운송 중 같은 TripId 복귀·명시 이탈 취소.32KiB/분할 신뢰 전송, 전체 집계+최근16개 내역으로 상태를 갱신한다. 프로토콜pvp-drive-3/시험 규칙v3.
- PracticeSession/UI: 유효 목표 길안내, 소유권이 바뀐 목표의 경로 해제, M 전체 지도.37변주 초기 대사를 기본 경쟁 운송에 표시한다. 유형별 수면/준비/응답/청결 효과와 내구도는 미연결이다.

판단: 사용자가 다른PC/사람 검증을 별도 항목으로 남기고 후속 구현까지 지시했으므로 P2 전체 수용 표시는 유지하지 않고 Task4/5의 최소 경로를 구현했다. 자동 경기 검사는 **서버 위치 제어 fixture**다. 실제 연결/중재/정산은 검사하지만 전체 코스를 물리 주행해 완료한 증거로 사용하지 않는다. 호스트의 시험용 위치 제어 API는 클라이언트 RPC에 노출하지 않았다.

## 실패 재현·수정

1. 최초 shared traffic 타입 부재: competition-red.log Editor1.
2. 첫 Player의 시작 전 Invalid match snapshot: competition-first-player.log 요약. Unity JsonUtility가 null Match를 기본 객체로 복원하는 것을 competition-empty-world-red.log Editor1로 재현했다. 명시 HasMatch와 Normalize로 수정했다. 최초 실패 Player 원본 로그는 재실행으로 덮였으며 원인 재현 로그와 러너 실패 요약을 보존했다.
3. 첫 감점이0인 관찰 뒤 다음 구간에서 양수로 상향될 때 과거 구간에 고정되는 경계: competition-safety-red.log Editor1. 최초 **양수** 감점의 현재 안전 구간에 귀속하도록 수정했다.
4. 이전 경기 요청을 분리할 MatchId 계약 부재: competition-match-scope-red.log 컴파일1. 요청/응답과 캐시 키를 경기 ID로 묶고 이전 경기 요청 거절/동일 요청ID의 다른 경기 분리를 검사했다.
5. 독립 읽기 전용 리뷰: 실제 장애물 차단과 완료 재전송이 기존 로그 조건만으로 입증되지 않는다는 지적을 반영했다. 실제 authority motor 차단 표식과 이미 완료된 승객에 대한 동일ID/새ID RPC 수신2회를 러너가 요구한다. 수정 확인에서 남은 중요 결함 없음. 다른PC/사람·실제 적색선 주행 판정은 미검증이다.
6. 첫 visible 검사: 화면은 Results/원장4로 표시됐으나 그래픽 초기화 지연으로 host28초가 client24초 검사보다 먼저 종료되어 client1/host0. 검사용 host 수명을45초로 늘린 재검사에서 host/client0을 확인했다. 게임 시간12+2초 설정은 그대로다.

## 실제 명령·결과

모든 로그는 artifacts/pvp/ 아래이며 Git 제외. 표의 Player는 실제 별도 프로세스다.

| 실행 | 실제 결과 | 로그·측정 |
|---|---|---|
| Pvp.Tests.EditMode.PvpCompetitionTests.Run | 종료0, 신규33검사+기존 isolation/Task2/network/reconnect | competition-scoped-model.log |
| Pvp.Editor.PvpBuild.BuildClient | 종료0, PVP_BUILD_OK | competition-release-build.log; clean build/네트워크 DLL 목록 검사 |
| test_pvp_competition.ps1 baseline | host/client0, 최종 결과 동일 | competition-baseline-*.log; 공통 교통 exact sample24개 |
| 실제 같은 tick 승차 | queued2, 단일 소유자/운송 | PVP_MATCH_SAME_TICK_REQUESTS |
| 실제 장애물 차단 | authority step 위치 유지·속도0 | PVP_SHARED_OBSTACLE_BLOCKED tick365 (baseline) |
| 실제 완료 재전송 | player1 동일ID/새ID2회 수신, 원장4 유지 | PVP_MATCH_COMPLETION_REPLAY_RECEIVED, player0도 확인 |
| 완료/결과 | host4000원→40+4=44점/1위, guest3000원→30+3=33점/2위 | 각1회 완료, 양쪽 최종 signature 일치 |
| 지연·손실+운송 중 복귀 | host/client0, epoch2/위치오차0·기존 TripId 복원 | RTT150ms/편도 jitter±15ms/손실3%; received3629/drop113/forward3516/rebind1 |
| 복귀 뒤 최종 결과 | 같은 원장4·점수44/33, 공통 교통 sample20개 일치 | competition-reconnect-*.log; pending23·최종 정지 오차0 |
| 명시적 guest 이탈 | client0 예상 퇴장, host0 정상 마감 | guest미완료0원/완료0/승자 자격 제외, host44점·원장2 |
| client 로컬 메뉴 시계 | Time.timeScale0에서 공통 경기 완료 | PVP_LOCAL_MENU_CHECK, 위 세 시나리오 |
| 경기 전체 러너 | 종료0 | competition-release-players.log: PVP_COMPETITION_PLAYERS_OK |
| 기존 network 회귀 | 종료0, 정상/지도 거절/host 종료(client1 예상) | competition-network-regression.log, Rounds1 |
| 기존 reconnect6시나리오 | 종료0 | competition-reconnect-regression.log; 실제4초 blackout복귀도 포함 |
| 기존 blackout | host/client0, 복원 오차0 | received3165/drop1030 중 blackout965/rebind1; 복귀 후7.029404m 이동 |
| test_passenger_behavior.ps1 | 종료0,430단언 | competition-passenger-model.log |
| test_prototype.ps1 | 종료0 | competition-campaign-model.log |
| test_pvp.ps1 | 종료0,8그룹/두 프로세스 진단 분리 | competition-core.log |
| 최종 Player isolation2개 | 각각0, 서로 다른 진단 파일 | competition-isolation-1.log/2.log |
| check_workspace.ps1 -Strict | 종료0,Input issues0 | competition-strict.log |
| distribution manifest -VerifyOnly | 종료0,130파일112,155,810바이트 | competition-distribution.log, 최종 재확인 동일 |
| 캠페인 원본 | origin/main 대비 변경0 | Assets/Scripts, ProjectSettings, builds/EungyeTaxi, distribution, config/maps |

위치 제어 때문에 경기 로그의 ownDistance/postResumeDistance/maxCorrection은 실제 주행거리/네트워크 보정 품질 수치로 사용하지 않는다. 실제 다른PC/사람 주행 품질은 별도 수용이다. visible 재검사 host/client0. competition-visible2-host.log/client.log와 competition-result-visible2.png에서1280×720 한글·확정 결과v1·원장4·순위1/2·점수 구성 확인. 화면 SHA256:49221C617CA660FAEBD7B1363AC6F509FBEBC3AC057B60AA6B15B109DBFCF088. 실제 입력 조작은 없었다.

## 실행·재현

저장소 루트 PowerShell에서 사용한다. 로컬 Unity2022.3.62f2 경로를 `$Unity`에 넣는다. 이 PC는 C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe다.

```powershell
$PvpProject = Join-Path $PWD 'unity/EungyeTaxi'
$PvpLogs = Join-Path $PWD 'artifacts/pvp'
New-Item -ItemType Directory -Force $PvpLogs | Out-Null
function Invoke-PvpEditor($method,$logName) {
  $arguments = @('-batchmode','-nographics','-quit','-projectPath',('"'+$PvpProject+'"'),'-executeMethod',$method,'-logFile',('"'+(Join-Path $PvpLogs $logName)+'"'))
  $process = Start-Process $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
  $process.WaitForExit()
  if ($process.ExitCode -ne 0) { throw "Unity exit=$($process.ExitCode)" }
}
Invoke-PvpEditor 'Pvp.Tests.EditMode.PvpCompetitionTests.Run' 'competition-scoped-model.log'
Invoke-PvpEditor 'Pvp.Editor.PvpBuild.BuildClient' 'competition-release-build.log'
pwsh -File tools/test_pvp_competition.ps1
pwsh -File tools/test_pvp_reconnect.ps1 -Port 18101
pwsh -File tools/test_pvp_network.ps1 -Rounds 1
pwsh -File tools/test_passenger_behavior.ps1
pwsh -File tools/test_prototype.ps1
pwsh -File tools/test_pvp.ps1
pwsh -File tools/check_workspace.ps1 -Strict
pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly
```

경기 러너는 -Case baseline/reconnect/leave와 -Port를 지원하고 자신이 실행한 프로세스만 정리한다. UI는 PVP exe→Host/Client→게스트 준비→호스트 경기 시간/유예→시작. 목록 선택은 내비만 설정하며 현장에서 E/버튼으로 승하차한다. M은 전체 지도, Q는 정차 전후진이다. 결과 뒤 재경기는 새 네트워크 세션을 시작한다. 기본 캠페인 exe/BuildHybrid와 혼동하지 않는다.

## 미검증·다음 작업

[17의 환경별 목록](../content-design/pvp/17-competition-acceptance.md)을 따른다. 다른PC LAN/역할 교환/실제 인터넷, 사람 키보드·패드·전 코스 물리 운송, 실제 적색선 접근과 판정, 일반 NPC/전체 법규·피해 귀속·반복 센서·부하, 공급 접근 편향과 고액 단건/짧은 여러 건/경기 시간 밸런스가 남았다. P2 전체를 통과했다고 표시하지 않는다.

공개 계정 인증/암호화/호스트 이전/전체 서버 재시작 복구는 미지원이다. 같은 프로세스 재접속만 기존 운송/원장을 복원한다. 유형별 준비/수면/응답/오염과 내구도는 후속이며 상대 작업의 완료 커밋부터 재검토한다. 기본 은계 배포·지도는 보존했고 PVP exe는 builds/PvpPrototype/PvpPrototype.exe의 로컬 빌드/Git 제외다.

게시 대상은 feature/pvp-prototype. 이 기록의 커밋은 git log -1 --oneline -- docs/work-history/2026-10-29-pvp-shared-competition.md로 확인한다. 실제 게시 확인은 후속 receipt에 남긴다.