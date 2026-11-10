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

PS5.1 결과는 도구 실행 환경 차이로 판단되며, 지도·배포 파일 자체의 오류로 확인된 것은 아니다. 11-09 다른 PC의 pwsh 검사(issues0, 143파일127378253바이트)가 마지막 유효 결과다.

## 남은 항목

- 이 PC에서 검사하려면 PowerShell 7 설치 후 `pwsh -File tools/check_workspace.ps1 -Strict`와 통합/PVP `-VerifyOnly`를 다시 실행한다.
- 로컬 3커밋은 아직 push되지 않았다.
- 새 빌드, 위치 제어 검사, 자동 물리 주행, 수동 플레이, 전체 캠페인, 실제 다른PC LAN은 수행하지 않았다.

## 변경 파일

이 기록과 `docs/work-history/LATEST.md`만 갱신. 게임/빌드/지도/ZIP/저장/미추적 설정 파일은 변경하지 않았다.
