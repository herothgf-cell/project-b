# 2026-11-10 프로젝트 진행 상황 확인

## 요구사항과 기준

진행 상황 확인 요청. 시작 main `c4e28ad`(origin/main보다 3커밋 앞섬, 모두 문서 이력), 기존 미추적 `unity/EungyeTaxi/ProjectSettings/PackageManagerSettings.asset` 보존. LATEST와 [2026-11-09 진행 점검](2026-11-09-progress-check.md)을 대조했다. 게임 기능 변경·새 배포 요청은 아니다.

## 현재 진행 상황

- 11-09 점검 이후 추가 커밋은 Unity/Blender MCP 읽기 전용 검사 기록 2건뿐이며 게임 소스·빌드·지도 변경은 없다.
- 배포 기준은 그대로: `builds/EungyeTaxi/EungyeTaxi.exe`, 은계 싱글3일 / A2 PvP(봇 또는 LAN 최대4명), BuildHybrid·LANv16, 게임 소스548fe8b·출하a978b3b, 정식 지도8fb257ed.
- 남은 우선순위도 동일: 사람 장시간/패드·전체3일 캠페인, 실제 다른PC LAN, 저사양 성능·아트, 성장 확장(후속 범위).

## 실제 수행한 검증 (PC `kimhs-NW2`)

| 명령 | 결과 |
|---|---|
| `pwsh ...` | 실행 불가: 이 PC에 PowerShell 7(`pwsh`) 미설치 |
| `powershell -File tools/check_workspace.ps1 -Strict` (Windows PowerShell 5.1) | exit1, Input issues3: 프로젝트 지도 검사가 `ConvertFrom-Json -Depth`(PS7 전용) 미지원으로 INVALID, 선택 입력 `eungye-map.json`/`route42-map.json` MISSING |
| `powershell -File tools/update_distribution_manifest.ps1 -VerifyOnly` (5.1) | exit1, 스크립트 20행 `GetFullPath` 예외(PS5.1 경로 처리 차이) |

PS5.1 결과는 도구 실행 환경 차이였다. 사용자 요청으로 `winget install --id Microsoft.PowerShell`(7.6.9, exit0)을 설치했다. 사용자 범위 MSIX 설치라 `pwsh`는 `%LOCALAPPDATA%\Microsoft\WindowsApps\pwsh.exe` 별칭으로 실행된다(새 셸에서는 PATH로 잡힘).

### PowerShell 7.6.9 재검사

| 명령 | 결과 |
|---|---|
| `pwsh -File tools/check_workspace.ps1 -Strict` | exit0, Input issues0. 재수입 JSON 2개는 OPTIONAL 부재 표시 |
| `pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly` | exit0, DISTRIBUTION_OK files143 / bytes127378253 |
| `pwsh -File tools/update_pvp_distribution_manifest.ps1 -VerifyOnly` | exit0, 동일 통합 배포143파일 검증 |

이 PC의 지도 입력과 배포 실행 폴더는 11-09 기준과 일치한다.

## 남은 항목

- 검사 스크립트는 PS5.1에서 동작하지 않으므로 다른 PC도 PowerShell 7을 먼저 설치한다.
- 로컬 문서 커밋은 아직 push되지 않았다.
- 새 빌드, 위치 제어 검사, 자동 물리 주행, 수동 플레이, 전체 캠페인, 실제 다른PC LAN은 수행하지 않았다.

## 변경 파일

이 기록과 `docs/work-history/LATEST.md`만 갱신. 게임/빌드/지도/ZIP/저장/미추적 설정 파일은 변경하지 않았다.
