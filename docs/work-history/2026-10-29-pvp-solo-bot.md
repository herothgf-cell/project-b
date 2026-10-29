# 2026-10-29 한 창 1인 봇 경기

## 요청·범위

사용자가 직접 혼자 승객 운송·NPC·법규·정산을 시험할 수 있는1인 모드 또는 봇 버전을 요청했다. 기존 경기 모델을 재사용하는 **혼자 경기 시작 · 봇 상대**를 추가했다. 초기 화면에서 시간/유예 입력만 필요하며 나1대+로컬 봇1대다. 제품 경기 시간을10분으로 확정하지 않는다. 상세 사용법/요소 표는 [21](../content-design/pvp/21-solo-bot-mode.md), 구현 판단/순서는 [20](../content-design/pvp/20-solo-bot-mode-plan.md).

## 실제 구현·변경 파일

- `Runtime/Competition/PvpTaxiBot.cs`: 공개 승객 상태/합법 경로 거리로 목표를 고르고 소유 승객을 우선한다. 가속/조향/제동과 일반 승하차 명령만 만들며 위치·운임·원장을 직접 변경하지 않는다. 여러 운송을 연속 수행한다.
- `Runtime/Network/PvpNetworkSession.cs`: transport/포트/다른 창 없이 기존50Hz 권한 tick과 두 모터를 실행한다. 일반 사람 입력은 유지하고 봇도 같은 NPC/법규/정차 검사로 운송한다. 종료 시 월드/머티리얼/세션/검사 입력을 정리하고 초기 화면으로 복귀한다.
- `Runtime/Network/PvpCompetitionBridge.cs`: 같은 경기/교통 snapshot 제공, 사람/봇 점수 표시. 소유권/승하차/법규/정산 규칙과 온라인2인 준비 조건은 그대로 재사용한다.
- `Runtime/PvpBootstrap.cs`, `Runtime/UI/PvpPrototypePanel.cs`: 한글1인 진입/시간 검증/스크롤, 오프라인 단순 주행·직접IP 진입 보존.
- `Editor/PvpSoloBotTests.cs`, `Runtime/PvpSoloBotProbe.cs`, `tools/test_pvp_solo_bot.ps1`: 모델 연속 운송과 실제 단일 Player의 잘못된 시간 거부/운송/법규/결과/종료/재시작 검사. 자동 사람 입력은 명시적 검사 인자에서만 설치한다.
- 관련 `.meta`,20/21기획서·README/로드맵/19수용·이 이력/LATEST, 최종 실행 폴더/전용 manifest.

위 Runtime/Editor 경로는 `unity/EungyeTaxi/Assets/Pvp/` 기준이다. 기존 `C:/git/project-b`의 다른 채팅 checkout/실행 프로세스/입력은 변경하지 않았고 이 worktree의 PVP 파일만 수정했다.

## 실제 명령·결과

| 검사 | 종료/실제 결과 | 증거(로컬 Git 제외) |
|---|---|---|
| 새 검사 red | Editor1, `one-window bot controller is missing` | `artifacts/pvp/solo-bot-red.log` |
| 봇 모델 green | Editor0,360+10초, 각7회/1872.957m·2033.457m, 경계·감점0, 단일 원장 합계 | `solo-bot-model1.log` |
| PvpSoloBotTests.RunAll | Editor0, 기존 isolation/Task2/network/reconnect/경쟁33/16접근·12연속/법규NPC25,628관측+새 봇 모델 | `solo-bot-all-model.log` |
| BuildClient | 종료0, `PVP_BUILD_OK` | `solo-bot-build.log`, `solo-bot-build2.log` |
| 실제 단일 Player 정상 | 종료0,transport없음, 각2회,673.243m·746.522m,77/88점, 경계/fixture/감점0, 결과→메뉴→새3초 경기→빈 정산→메뉴 통과 | `solo-bot-baseline.log` |
| 위반 시험기 최초 red | 종료1, 법규 무시를 계속해 보행자 앞을 봉쇄, 운송 완료 실패. 감점3 및 같은 접근2→9가 관측됐지만 합격 근거로 사용하지 않음 | `solo-bot-violation-blocking-red.log` |
| 보완 위반 green | 종료0,첫 감점 확인 후 정상 입력 복귀. 각2회,673.247m·746.522m, 사람운임70+보너스4−감점3=71점/봇88점, 경계/fixture0, 결과·재시작 통과 | `solo-bot-violation-green.log`, `solo-bot-violation.log` |
| 기존 실제2Player 입력 운송 | 러너0,각1회/199.541m·319.430m,33/44점, 같은 결과/NPC144표본, 경계/fixture0 | `solo-bot-network-regression.log` |
| 기존 재접속6시나리오 | 러너0. 유예 만료/host-loss의 예상 client1은 negative 수용이며 성공으로 바꾸지 않음 | `solo-bot-reconnect-regression.log` |
| PVP Core | 러너0,8그룹·프로세스2개 별도 진단 | `solo-bot-core-regression.log` |
| Strict/싱글 manifest | Strict0/input issues0, 싱글130파일112,194,710바이트 검증0 | `solo-bot-strict.log`, manifest명령 stdout |
| 독립 리뷰 | 읽기 전용,최종 시험기 보완까지 중대한 발견 없음 | 이 대화의 전용 리뷰 에이전트 |

최초 위반 실패는 의도적 불법 주행을 무한히 지속한 검사 입력 때문이었다. NPC를 밀거나 순간이동시키지 않고 과속 감점을 확인한 뒤 정상 주행하는 검사로 범위를 한정했다. 제품에는 보행자 앞을 막았으면 Q/후진으로 공간을 비키는 안내만 추가했다. 충돌/양보/감점 규칙을 완화하지 않았다.

실제 Player도 사람 자리에 검사 입력을 공급했다. 물리 키보드/패드 조작감이나 화면 고지 수용, WheelCollider 검증으로 표시하지 않는다. 이 작업에서는 PC 화면/마우스/키 입력을 조작하지 않았다.

## 재현

로컬 Unity: `C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe`. `-batchmode -nographics -quit -projectPath <저장소>/unity/EungyeTaxi -executeMethod Pvp.Tests.EditMode.PvpSoloBotTests.RunAll -logFile <절대 로그>` 실행 후 같은 방식의 `Pvp.Editor.PvpBuild.BuildClient`. 각 Start-Process는 WindowStyle Hidden/WaitForExit로 종료 코드를 확인했다.

```powershell
pwsh -File tools/test_pvp_solo_bot.ps1
pwsh -File tools/test_pvp_solo_bot.ps1 -Case violation
pwsh -File tools/test_pvp_solo_bot.ps1 -Case baseline -Executable <복사본 exe 절대 경로>
pwsh -File tools/test_pvp_driving.ps1 -Case baseline -Port 18601
pwsh -File tools/test_pvp_reconnect.ps1 -Port 18701
pwsh -File tools/test_pvp.ps1
pwsh -File tools/check_workspace.ps1 -Strict
pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly
pwsh -File tools/update_pvp_distribution_manifest.ps1 -VerifyOnly
```

각 Player 러너는 자신이 만든 프로세스만 정리한다. 실제 직접 플레이는 [21번 사용법](../content-design/pvp/21-solo-bot-mode.md)을 따른다.

## 잔여·배포

사람 키보드/화면 시야/봇 난도·수익 경쟁 체감·장기 부하와 다른PC직접IP는 미검증이다. 현재 사람 입력은 키보드이며 패드 연동/체감은 후속이다. 유형별 수면/응답/오염 효과, PVP 내구도/수리, 간접 NPC봉쇄 밸런스와 복합 법규는 기존 잔여 그대로다. 봇 경기 결과는 로컬 시험용이며 P2전체/공개 랭킹 수용으로 올리지 않는다.

소스 커밋을 확정한 뒤 재빌드·프로젝트/Library 밖 복사본 운송/격리·원본/복사본/Git index 해시·전용 manifest를 검증해 전체 PVP 실행 폴더를 배포한다. 실제 소스/배포 해시와 게시 결과는 확인 후 아래에 남긴다.

## 최종 커밋 소스 재검사·복사본 배포

- 소스 `758753dcfc1de9638c4e041352c4875c306a6993`에 Unity 소스 미커밋 변경이 없는 상태에서 `PvpSoloBotTests.RunAll`/`BuildClient`를 다시 실행했다. 각각 종료0: `solo-packaged-model.log`, `solo-packaged-build.log`.
- 프로젝트/Library 밖 `artifacts/pvp/runtime-copy-<고유값>/`에 전체 폴더를 복사해139개 파일의 크기/SHA256 일치를 확인했다. 복사본 자체 디렉터리에서 실행했다.
- 복사본 `-pvpIsolationCheck`: 종료0/`PVP_RUNTIME_ISOLATION_OK`, `solo-copied-isolation.log`.
- 복사본 `test_pvp_solo_bot.ps1 -Case baseline -Executable <복사본>`: 러너/Player0,각2회·673.243m/746.522m·77/88점·fixture/경계0,transport없음, 시간 오류 거부/같은 NPC/정산/메뉴 복귀/빈 새 경기/재시작 확인. `solo-packaged-baseline.log`.
- `solo-package-validation.json`에 근거해 `update_pvp_distribution_manifest.ps1 -SourceCommit <위40자리> -ValidatedOn 2026-10-29 -Rebuilt -ValidationReport artifacts/pvp/solo-package-validation.json` 종료0. **PVP139파일93,207,621바이트**, VerifyOnly0. 여기 driveBaselineExit는 복사본의 입력 기반1인 운송 검사 종료0을 뜻하며 기존2Player/6재접속의 별도 회귀도 report에 구분했다.
- 기존 싱글 **130파일112,194,710바이트** manifest 재검증0. 싱글 실행 폴더/지도/공통 Unity 소스 변경 없음. 화면·사람 입력·다른 물리PC는 미검증이다.

소스758753d와 별도로 이 최종 실행 폴더·전용 manifest/기록을 배포 커밋한다. 게시 확인에는 실제 원격 반영 결과를 남긴다.
