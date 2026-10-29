# 2026-10-29 PVP 시작·저장·빌드 기반 구현

## 요구와 현재 상태

사용자의 “이어서 진행해줘” 요청에 따라 `feature/pvp-prototype`의 개발 로드맵 Task 1을 진행했다. 기준 커밋은 `b7b88725fc2cbc2f309a7fa0020aa1e07132074c`다. 새 격리 사본의 시작 상태는 `git status --short` 출력 없음으로 확인했다.

**전용 시작 씬·설정·저장·빌드 소스와 순수 코드 검사는 추가했다. Unity Editor/Player 실행·실행파일 제작·P1 수용 게이트는 아직 완료하지 않았다.** 현재 화면은 초기 설정 확인용이며 맵 주행이나 온라인 경기를 시작하지 않는다.

## 실제 구현

| 영역 | 구현한 동작 | 적용 경계 |
|---|---|---|
| 전용 시작 | `PvpBootstrap.unity`가 자기 시작 객체·카메라·초기 UI만 구성 | `DriveSession` 생성·은계 지도 로드·캠페인 시작 없음. 기존 한글 표시 테마는 공유 가능 |
| 초기 화면 | 한글 경기 시간·참가 인원·마감 유예 입력, 설정 확인, 나가기 | 시간·인원 자동 기본값 없음. 임의 맵 해시나 가짜 온라인 경기를 만들지 않음 |
| 설정 | 필수 양수 시간·유예·인원·식별자 검증, 실제 맵 SHA256을 받아 불변 `PvpMatchSettings` 생성 | 현재 정의된 설정과 지도 해시를 버전 있는 불변 SHA256으로 식별. MatchId는 별도 고정 |
| 저장 경로 | OS LocalApplicationData 아래 `PvpPrototype` 폴더, 단순 파일명만 허용 | exe/productName 및 캠페인 저장 함수에 의존하지 않음. Resolve는 기존 저장 읽기·이관·덮어쓰기 없음 |
| 진단 | 프로세스/세션/무작위 식별자와 `CreateNew`로 새 파일만 생성 | 동시에 실행한 프로세스가 같은 로그를 잘라 쓰지 않음 |
| Windows 빌드 | `Pvp.Editor.PvpBuild.BuildClient`가 PVP 씬 하나와 전용 출력만 지정 | 기존 `PrototypeBuild` 호출 없음. 활성 Windows64 타깃과 저장된 씬 상태를 먼저 요구 |
| 빌드 복원 | 변경한 제품명·백그라운드 실행 값을 성공/실패 모두 복원 | 기존 씬 목록·씬 구성은 직접 변경하지 않고 엔진 호출이 바꾼 경우 복원. InputManager 편집 없음 |
| 패키징 | 성공한 빌드의 전용 폴더에 기존 교통 코드·한글 폰트 고지 복사 | 기존 라이선스 원문 유지. 같은 프로젝트의 Resources/스크립트가 모두 제거됐다고 주장하지 않음 |
| 검사 진입점 | 순수 `Run()`/CLI, Editor 통합 검사, 명시적 Player 진단 인자 | 기존 저장소 검사 방식 사용. NUnit/Unity Test Framework 패키지를 추가하지 않음 |

설정 해시는 현재 존재하는 시간·유예·인원·규칙 버전·지도 해시만 포함한다. 점수·차량·교통 설정을 후속 Task에서 추가할 때 해당 값도 버전 있는 해시 입력에 포함해야 한다. 해시는 설정 식별이며 인증이나 서버 권한의 대체물이 아니다.

## 변경 파일

신규 Unity 경로는 `unity/EungyeTaxi/Assets/Pvp/` 아래다. 각 자산과 디렉터리의 `.meta`도 함께 추가했다.

- `Scenes/PvpBootstrap.unity`
- `Runtime/PvpBootstrap.cs`
- `Runtime/PvpRuntimeIsolationProbe.cs`
- `Runtime/Core/PvpSessionConfig.cs`
- `Runtime/Core/PvpBuildTransaction.cs`
- `Runtime/Persistence/PvpLocalStore.cs`
- `Runtime/UI/PvpPrototypePanel.cs`
- `Tests/EditMode/PvpIsolationTests.cs`
- `Editor/PvpBuild.cs`
- `Editor/PvpEditorIsolationTests.cs`

저장소 루트 기준 신규 검사 도구는 `tests/PvpIsolationTests.cs`, `tests/test_pvp_runner.py`, `tools/test_pvp.py`, `tools/test_pvp.ps1`이다. 기획 진척은 `docs/content-design/pvp/README.md`, `07-network-and-build.md`, `08-development-roadmap.md`, `09-decisions-and-balance.md`, `10-playtest-and-acceptance.md`와 이 기록·`LATEST.md`에 반영한다.

기존 캠페인 코드·씬·ProjectSettings·Packages·지도 입력·실행파일·배포 manifest·사용자 마인드맵은 이번 변경 대상이 아니다. `main` 병합도 수행하지 않는다.

## 실제 수행한 검증

### 1. 기존 캠페인 기준 검사

`tools/test_prototype.ps1`이 지정한 기존 생산 코드 20개와 검사 클래스 28개를 수정 없이 읽어 임시 .NET 8 프로젝트로 컴파일했다. 각 원래 `Main`은 별도 프로세스에서 실행했다.

- .NET SDK 8.0.427 / runtime 8.0.33, Linux x64.
- 빌드 경고 0 / 오류 0, **28/28 검사 클래스 종료 0**.
- 원본 생산/검사 코드 48개 SHA256 전후 동일.
- 실제 명령과 하네스: `/tmp/pvp-baseline/run_suite.py`; 로그 `/tmp/pvp-baseline/build.log`, `/tmp/pvp-baseline/results.log`, `/tmp/pvp-baseline/results.json`.
- 이는 원래 검사 로직의 .NET 8 실행이다. 원래 Windows PowerShell/.NET Framework 명령이나 Unity 캠페인 검증을 실행한 것으로 기록하지 않는다.

### 2. PVP 순수 코드 실패→통과

검증·분리·복원이 없는 최소 구현에서 아래 8개 그룹이 의도대로 실패한 뒤, 구현 후 모두 통과했다. 테스트는 실제 설정 객체·임시 파일·변경 가능한 빌드 상태를 사용한다. 빌드 검사는 엔진 호출 경계만 대체하고 복원 로직은 실제 생산 코드를 실행한다.

| 검사 | 확인한 동작 |
|---|---|
| MissingDuration_IsRejected | 누락·0·음수 시간 거절 |
| InvalidSettings_AreRejected | 잘못된 유예·인원·식별자·지도 해시 거절 |
| Freeze_IsImmutableAndCultureIndependent | 저작 값 변경 후에도 스냅샷 보존, 문화권 독립 해시 |
| SavePath_DoesNotTouchCampaign | 전용 경로, 기존 캠페인 파일의 byte 보존 |
| UnsafeFileNames_AreRejected | 절대 경로·상위 경로·구분자·Windows 장치명 거절 |
| DiagnosticCreation_NeverTruncatesExistingFiles | 같은 프로세스·세션에서도 기존 진단 파일 보존 |
| BuildFailure_RestoresSettings | 빌드 예외 후 설정 복원 |
| BuildSuccessAndConfigureFailure_RestoreSettings | 성공 및 일부 설정 적용 실패 뒤 복원 |

실제 재현 명령은 `python tools/test_pvp.py --dotnet /tmp/pvp-dotnet/dotnet`이다. 이 환경에서는 `DOTNET_CLI_HOME=/tmp/pvp-dotnet-cli-home`를 지정했고, 프로세스 메타데이터 접근 제한 때문에 승인된 확장 실행 환경에서 수행했다. 해당 경로는 작업 환경의 임시 설치이며 저장소 필수 경로가 아니다.

- 실패 단계: 정상 컴파일 후 의도한 8개 행동 실패, 종료 1.
- 구현 후: **8/8 그룹 통과, 종료 0, 빌드 경고/오류 0**.
- 같은 임시 저장 루트에 실제 .NET 검사 프로세스 두 개를 동시에 실행해 서로 다른 진단 파일·내용을 유지했다. 캠페인 sentinel 파일은 변경되지 않았다.
- 결과 표식: `PVP_ISOLATION_OK cases=8`, `PVP_TWO_PROCESS_DIAGNOSTICS_OK clients=2 distinct=2 campaign=unchanged`.
- Python 재현 도구의 성공 결과 파일은 `artifacts/pvp-tests/result.txt`이며 Git 반영 대상은 아니다. Windows .NET Framework 대체 경로는 콘솔 출력과 종료 코드를 확인한다.
- 두 프로세스는 Unity Player나 네트워크 클라이언트가 아니다. 파일 분리 검증 범위로만 인정한다.

### 3. 실제 Unity 참조를 사용한 런타임 컴파일

기준 배포본의 UnityEngine Core/IMGUI/InputLegacy/TextRendering/Audio 및 facade DLL을 읽기 전용 참조로 사용해 새 `Runtime/**/*.cs`를 .NET Standard 2.1 / C# 9 기준으로 컴파일했다.

- 임시 프로젝트: `/tmp/pvp-unity-reference/PvpRuntimeCompatibility.csproj`.
- 명령: `/tmp/pvp-dotnet/dotnet build /tmp/pvp-unity-reference/PvpRuntimeCompatibility.csproj --no-restore --nologo`.
- 종료 0, 경고 0 / 오류 0. 가짜 Unity 타입으로 대체하지 않았다.
- Unity Editor 소스의 컴파일, 엔진 실행, 씬 import·렌더링까지 검증한 결과는 아니다.

### 4. 소스 검토와 최종 회귀 확인

별도 소스 검토에서는 치명적·주요 결함이 발견되지 않았으며, 파일명 경계·성공 기록·실행 문서의 작은 보완점 세 가지를 수정한 뒤 해당 변경만 재검토했다.

- Windows 예약 장치명 `COM¹`·`COM²`·`COM³` 및 `LPT¹`·`LPT²`·`LPT³`를 확장자 유무 모두 거절한다. [Microsoft 파일명 규칙](https://learn.microsoft.com/en-us/windows/win32/fileio/naming-a-file)을 근거로 기존 파일명 검사 그룹에 12개 사례를 추가했다. 수정 전에는 해당 그룹만 의도대로 실패했고 수정 후 8개 그룹 전체가 통과했다.
- Python 러너가 검증 시작 시 이전 `result.txt`를 삭제한다. 실제 러너를 임시 저장소로 복사하고 실패하는 자식 프로세스를 실행하는 회귀 검사로, 실패 후 오래된 성공 기록이 남는 문제를 재현하고 수정했다.
- 로드맵의 명령을 실제 정적 `Run()` 진입점·필수 Win64 타깃과 맞추고, 결과 파일이 Python 경로에서만 생성된다는 점을 명시했다.

최종 실행 결과는 다음과 같다.

| 실제 명령 | 결과 |
|---|---|
| `python3 -m unittest discover -s tests -p test_pvp_runner.py -v` | 종료 0, 1/1 통과 |
| `DOTNET_CLI_HOME=/tmp/pvp-dotnet-cli-home python3 tools/test_pvp.py --dotnet /tmp/pvp-dotnet/dotnet` | 종료 0, 8/8 그룹 통과, 두 프로세스 진단 분리, 캠페인 sentinel 보존, 컴파일 경고/오류 0 |
| 위 3절의 Runtime 참조 컴파일 재실행 | 종료 0, 경고/오류 0 |

변경 범위 재검토 결과 세 항목 모두 해결됐고 새 결함은 발견되지 않았다. 이 결과는 아래 실제 엔진·배포·사람 검증을 대신하지 않는다.

## 준비했지만 수행하지 않은 검증

이 환경에는 Unity Editor·라이선스·Windows 빌드 지원 모듈이 없어 아래 검증을 수행하지 않았다. 완성된 실행파일도 만들지 않았다.

- Unity 프로젝트 import·Editor 코드 컴파일·씬의 실제 스크립트 연결.
- `Pvp.Tests.EditMode.PvpEditorIsolationTests.Run`: 실제 PlayerSettings·씬/입력 보존과 dirty-scene 보호.
- `Pvp.Editor.PvpBuild.BuildClient`: 실제 Windows EXE 빌드.
- `-pvpIsolationCheck`: 실제 Unity Player 두 개의 시작·로그 분리·캠페인 객체 부재.
- 화면 한글·배치·입력·나가기, 기존 실제 저장/백업과 배포 폴더 전후 해시, 다른 PC 실행.
- Windows PowerShell/.NET Framework 검사 경로 자체의 실행.
- 자체 맵·차량 주행·승객·정산·네트워크·SDK 채택과 P2 이후 모든 게이트.

## 실행과 다음 작업

실행 가능한 현재 검사 명령과 아직 실행하지 않은 Unity 명령은 [로드맵의 현재 검증 진입점](../content-design/pvp/08-development-roadmap.md#현재-구현된-검증-진입점)에 모았다.

1. Windows의 Unity `2022.3.62f2`에서 이 브랜치를 열고 import/컴파일을 확인한다. 현재 PVP는 은계 지도 로더를 시작하지 않지만, 기존 캠페인 개발을 이어갈 때는 AGENTS.md의 지도 ZIP/작업공간 점검 절차를 그대로 따른다.
2. Windows x86_64 타깃에서 PVP Editor 검사를 실행하고 실제 종료 코드·로그를 기록한다.
3. `Taxi > PVP > Build isolated Windows client` 또는 문서의 배치 명령으로 `builds/PvpPrototype/PvpPrototype.exe`를 만든다.
4. 두 Player 진단과 정상 화면을 확인해 Task 1의 Unity 게이트를 닫는다. PVP 빌드 폴더는 현재 Git 제외 대상이며 생성 사실만으로 원격 배포 완료가 되지 않는다.
5. 그 뒤 Task 2의 자체 제작 시험장·가속/조향 후보를 구현한다. 초기 설정 화면을 완성된 PVP 경기로 해석하지 않는다.

현재 기존 게임의 실행 경로는 `builds/EungyeTaxi/EungyeTaxi.exe`, 빌드는 `PrototypeBuild.BuildHybrid`다. 이 작업으로 교체하지 않았다. 본 기록의 실제 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-29-pvp-startup-isolation.md`로 확인한다.
