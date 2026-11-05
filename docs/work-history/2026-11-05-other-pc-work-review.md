# 2026-11-05 다른 PC 작업 확인

## 요청·범위

사용자 요청: 다른 PC에서 작업한 내용을 확인한다. 최신 이력·체크리스트·범위·관련 콘텐츠 기획과 현재 코드를 대조하고, 이 PC의 Git 동기화·지도 입력·배포 파일 상태를 검사했다. 게임 기능 수정·재빌드·배포·실행·저장 변경은 수행하지 않았다.

## 반영된 작업

| 작업 | 실제 소스 / 배포 / 이력 커밋 | 현재 코드 대조 |
|---|---|---|
| 내비·승하차·M 내 위치·가독성 | `6b7f054` / `6da9319` / `a8b4c0d` | RoadNavigation의 이전 표시 경로 분리, StopArrivalGuidance의 정차·준비/용무 조건, UiReadability의 최소 글자·배율 구현 확인 |
| 캐주얼 조작·수요 기반 신호 | `8871fcc` / `3a7f7ae` / `aea632c` | DriverPedalIntent의 정차 유지/놓고 반대 기어, DemandSignalClock·TrafficSignalDemand의 수요/빈 단계/보행 보호, LAN `native-city-lan-v3` 확인 |
| 조향 추가 완화 | `bb0165f` / `0806937` / `0091d71` | DriveFlowPolicy의 입력률/반대 조향, DriveMath의 저중속 최대각·yaw 보정 조건, TaxiController의 접지·졸음 보호 연결 확인 |

원격 main은 GitHub 연결 도구의 `GET /repos/herothgf-cell/project-b/branches/main`으로 조회해 `0091d7196588a811aebc80a3ffc57f14da4a9b31`을 확인했다. 점검 시작 시 로컬 HEAD·origin/main과 동일했다. 셸의 `git ls-remote origin refs/heads/main`은 `getaddrinfo() thread failed to start`로 실패해 연결 도구로 대조했다. pull/브랜치 전환은 필요하지 않았다. 이 점검 이력을 커밋한 뒤에는 로컬 문서 커밋만 추가될 수 있으며 원격 게시와 구분한다.

## 이 PC에서 수행한 검증

| 검사 | 실제 결과 |
|---|---|
| `git status --short`, tracked/staged `git diff --quiet` | 시작 시 tracked/staged 변경 없음(각 exit0). 기존 미추적 8항목 보존 |
| `pwsh -File tools/check_workspace.ps1 -Strict` | exit0, Input issues 0. Unity 프로젝트 버전 2022.3.62f2·정식 프로젝트 지도/보정 입력 일치. 두 Resources 재수입 JSON 부재는 정식 프로젝트 지도 존재 시 허용되는 선택 입력 |
| `pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly` | **exit1**, Distribution file count mismatch |
| `pwsh -File tools/update_pvp_distribution_manifest.ps1 -VerifyOnly` | **exit1**, 통합 manifest 위임 검사에서 같은 파일 수 오류 |
| manifest 등재 파일별 크기·SHA256 전수 대조 | **140파일·122,952,638바이트 일치**, 누락0/불일치0. 실제 실행 폴더141파일이며 추가 파일은 `builds/EungyeTaxi/EungyeTaxi.zip` 41,640,903바이트 한 개 |
| `git log -1 --oneline -- <11/04 이력 파일>` | 위 표의 실제 이력 커밋 확인 |
| 문서 기록 전 `git diff --check` | exit0 |

manifest 검사는 파일 개수를 먼저 검사하므로 기존 미추적 ZIP 때문에 실패한다. 이번 읽기 중심 점검에서는 사용자 파일을 이동/삭제하거나 ZIP을 정식 manifest에 추가하지 않았다. 등재 파일의 해시 일치를 VerifyOnly 전체 성공으로 표시하지 않는다. 추후 정식 VerifyOnly를 통과시키려면 ZIP을 배포 실행 폴더 밖에 별도 보관한 뒤 위 두 명령을 다시 실행한다.

기존 미추적 항목: 루트 `Library/`, `Packages/`, `ProjectSettings/`, `UserSettings/`, 위 실행 ZIP, `docs/work-history/2026-10-26-status-review.md`, `unity/EungyeTaxi/ProjectSettings/PackageManagerSettings.asset`, `unity/YeongtongTaxi/`. 포함·삭제·수정하지 않았다.

## 검증 구분·남은 항목

11/04 이력과 manifest에는 최종 BuildHybrid exit0, 싱글226/PVP213 위치 제어 검사, 같은 PC LAN host40/client55·재접속 통과, 평지 PhysX 90도 코너 변위 약6~8% 감소가 기록되어 있다. **이번 PC에서는 해당 실행 검사를 다시 하지 않았다.** `artifacts/`의 원본 증거는 Git 배포 대상이 아니므로 이력상의 결과와 이번 재검증을 구분한다.

사람 키보드/실물 패드 조작감, 실제 다른 PC LAN/4인 장시간, 10/20분 신호 대기 비중, 전체3일 캠페인·전체 졸음 사건 물리 검증은 남아 있다. 기존 충돌 피드백·일반 보행자/대기 승객 처리 일관성도 체크리스트의 미완료 항목이다. 이 점검으로 완료 표시하지 않았다.

## 실행·재현

기본 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`, 은계 싱글/A2 자체 도시 PVP·LAN, 개발 빌드는 `PrototypeBuild.BuildHybrid`다. LAN은 양쪽 PC 모두 현재 v3 실행본을 사용한다. 실행 폴더 전체가 필요하며 캠페인 저장·PlayerPrefs는 Git으로 이전되지 않는다.

위 표의 명령은 저장소 루트에서 실행한다. 파일 수 오류를 유지한 상태에서도 등재 파일 자체의 상태는 다음 읽기 전용 명령으로 재현할 수 있다.

```powershell
$reviewManifest = Get-Content -LiteralPath 'distribution/EungyeTaxi-build-manifest.json' -Raw | ConvertFrom-Json
[long]$reviewBytes = 0
foreach ($reviewEntry in $reviewManifest.files) {
    $reviewItem = Get-Item -LiteralPath $reviewEntry.path
    if ($reviewItem.Length -ne $reviewEntry.bytes -or
        (Get-FileHash -LiteralPath $reviewEntry.path -Algorithm SHA256).Hash -ne $reviewEntry.sha256) {
        throw "Mismatch: $($reviewEntry.path)"
    }
    $reviewBytes += $reviewItem.Length
}
if ($reviewBytes -ne $reviewManifest.totalBytes) { throw 'Listed byte count mismatch' }
"LISTED_FILES_OK files=$($reviewManifest.files.Count) bytes=$reviewBytes"
```

변경 파일은 이 기록과 `docs/work-history/LATEST.md`뿐이다. 코드·지도·빌드·체크리스트의 구현 판정은 변경하지 않았다.
