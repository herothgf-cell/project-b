# 2026-10-29 PVP 기획 요소 검토·Task 1 실제 Unity 검증

## 요구·범위

기존 PVP 기획을 콘텐츠별 요소 표로 정리하고 main 동시 채팅의 충돌을 검토한다. 이전 f8700af의 미수행 Unity 검사부터 이어서 진행하고 Task 2 시험장·차량까지 계속한다는 사용자 답변을 받았다. Task 2 결과는 별도 후속 이력으로 기록한다.

## 저장소·동시 작업 보존

- 원격 fetch 종료0, PVP 최신 f8700af, main 0db3904. 기존 로컬 PVP b7b8872에서 fast-forward.
- main C:/git/project-b의 기존 Library/Packages/ProjectSettings/UserSettings, 임시 PPT, 이전 이력, Unity 설정·YeongtongTaxi 미추적 파일을 보존했다.
- 별도 managed worktree의 feature/pvp-prototype에서 작업. main checkout·사용자 파일 변경 없음.
- `git merge-tree --write-tree origin/main HEAD` 종료1: 실제 내용 충돌은 docs/work-history/LATEST.md. 병합·충돌 해결을 실행하지 않았다.
- main 채팅 「취객·세차 기능 구현 여부 확인」의 최신 요청은 취객 빈도·상호작용 승객·충돌 강도별 내구도 기획. PVP 전용 소스/문서와 직접 겹치지 않지만 공용 인계와 규칙 이식은 주의해야 한다. 읽기 시점 이후 변경은 재검사 대상이다.

## 변경

| 파일 | 실제 변경 |
|---|---|
| docs/content-design/pvp/11-content-elements-review.md | 콘텐츠별 행동·결과·예외·상태·개발 단계 표, main 충돌 및 기획 경계 검토 |
| docs/content-design/pvp/README.md, 00~08 관련 상태 | 검토표 연결, 기반 구현과 실제 플레이 구현 상태 구분 |
| unity/EungyeTaxi/Assets/Pvp/Editor/PvpEditorIsolationTests.cs | 2022.3에 없는 OpenPreviewScene 교체, 검사 전 열린 씬 보존, 임시 저장 씬으로 dirty-scene 검사 |
| tools/test_pvp.ps1 | dotnet 실행파일만 있는 runtime-only 환경에서 SDK>=8을 확인하고 기존 Framework 검사로 대체 |
| docs/content-design/pvp/12-task2-implementation-plan.md | 기존 Task 2의 지도/차량/통합 계약·검증 계획 구체화 |
| docs/work-history/LATEST.md | PVP 결과와 캠페인 인계 분리 |

## 실패 재현과 수정

1. Unity 첫 import 종료1: `CS0117 EditorSceneManager.OpenPreviewScene` 없음. 로그 `artifacts/pvp/editor-isolation-before-fix.log`.
2. additive 검사로 변경한 뒤 빈 초기 SceneManagerSetup 복원 실패를 확인했다. 원래 씬을 닫지 않고 검사에서 연 씬만 닫고 활성 씬을 복원하도록 수정했다. 로그 `editor-isolation-empty-setup.log`.
3. 초기 untitled 씬에서 NewScene(Additive) 실패를 확인했다. 고유 임시 씬 자산을 복사·열기·dirty 검사·삭제하는 방식으로 수정했다. 로그 `editor-isolation-untitled-fixture.log`.
4. PowerShell 러너는 dotnet runtime만 설치된 PC에서 build를 시도해 실패했다. SDK 목록 확인 후 .NET Framework fallback으로 실제 재검사 통과.

Unity API 참고: [2022.3 EditorSceneManager](https://docs.unity.cn/2022.3/Documentation/ScriptReference/SceneManagement.EditorSceneManager.html). 실제 설치 Editor 컴파일 결과를 최종 근거로 사용했다.

## 실제 검사 결과

| 검사 | 명령·표식 | 결과 |
|---|---|---|
| 필수 입력 | `pwsh -File tools/check_workspace.ps1 -Strict` | 종료0, issues0, 선택 입력2개 없음 허용 |
| PVP Windows 모델 | `pwsh -File tools/test_pvp.ps1` | 수정 후 종료0, PVP_ISOLATION_OK cases=8, 두 .NET 프로세스 진단 분리 |
| 러너 회귀 | `python -m unittest discover -s tests -p test_pvp_runner.py -v` | 종료0, 1/1 |
| 기존 캠페인 모델 | `pwsh -File tools/test_prototype.ps1` | 종료0, 기존28 클래스 검사 |
| Unity import·Editor | 아래 Editor 명령 | 종료0, PVP_EDITOR_ISOLATION_OK |
| Windows64 실제 빌드 | 아래 BuildClient 명령 | 종료0, PVP_BUILD_OK |
| Unity Player 2개 | `-pvpIsolationCheck -logFile <각기 다른 절대 로그>` | 두 프로세스 종료0, PVP_RUNTIME_ISOLATION_OK, 서로 다른 진단 파일 |
| 실제 초기 UI | 진단 인자 없이 1280×720 창 실행 | 한글·빈 입력 오류·시간180/인원2/유예0 유효 안내·나가기 종료0 확인 |
| 보존 해시 | artifacts/pvp/preservation-before.json 전후 SHA256 | main+worktree 캠페인 배포, worktree ProjectSettings, 실제 LocalLow 캠페인 저장 총298파일 변경0 |

180초는 UI 입력 시험값이며 제품 기본 경기 시간이 아니다. UI는 에이전트가 Windows 입력·화면으로 확인했으며 사람의 주행 체감 검사와 다르다. 기존 캠페인 수동 실행·20분/3일 전체 플레이는 이번에 수행하지 않았다.

## 정확한 재현 명령

저장소 루트에서 실제 로컬 Unity 경로를 `$PvpUnityEditor`에 지정한다. 이번 PC 경로는 C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe이며 저장소 필수 경로가 아니다.

```powershell
$PvpProject = (Resolve-Path unity/EungyeTaxi).Path
$PvpLog = Join-Path (Get-Location) 'artifacts/pvp/editor-isolation.log'
$p = Start-Process $PvpUnityEditor -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$PvpProject+'"'),'-buildTarget','Win64','-executeMethod','Pvp.Tests.EditMode.PvpEditorIsolationTests.Run','-logFile',('"'+$PvpLog+'"')) -WindowStyle Hidden -PassThru -Wait
$p.ExitCode
# 빌드: executeMethod를 Pvp.Editor.PvpBuild.BuildClient, 로그를 build-client.log로 변경
# Player: builds/PvpPrototype/PvpPrototype.exe -pvpIsolationCheck -logFile <절대 경로>
```

자동 로그는 artifacts/pvp/ 아래 editor-isolation.log, build-client.log, player-a.log, player-b.log, player-ui.log, workspace-strict.log, model-tests-fixed.log, campaign-model-tests.log에 있다. Git 제외 로컬 증거이며 다른 PC에 자동 배포되지 않는다.

## 완료·미검증·다음 작업

Task 1 독립 시작/저장/빌드의 이 PC 실행 게이트를 통과했다. QA-ISO-01의 현 단계 검증이며 QA-BUILD-01 전체(자체 맵·다른 PC 배포)는 미완료다. PVP exe는 로컬 builds/PvpPrototype/PvpPrototype.exe이며 Git 제외, 원격 실행파일 배포로 표시하지 않는다.

기본 캠페인 실행은 builds/EungyeTaxi/EungyeTaxi.exe / 은계 / PrototypeBuild.BuildHybrid 유지. 캠페인 배포·manifest·지도 입력을 변경하지 않았으므로 재생성하지 않았다. Task 2의 자체 시험장과 차량 후보 구현·검증을 이어 간다. 네트워크 SDK·최종 경기 시간·인원·배점·충돌은 미정 상태 유지.

이 기록의 실제 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-29-pvp-review-validation.md`로 확인한다.
