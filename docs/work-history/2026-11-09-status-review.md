# 2026-11-09 작업 현황 점검

## 요구와 수행 범위

- 요청: 현재 작업 현황 확인.
- LATEST, 개발 체크리스트, 프로토 범위, 콘텐츠 목록/승객 특성19, 최신 택시·HUD 배포 이력과 코드 및 manifest를 대조했다.
- 게임 코드·실행본·지도·ZIP·사용자 설정은 변경하지 않았다. 변경 파일은 본 기록과 `docs/work-history/LATEST.md`다.

## 현재 상태

- 점검 시작 main HEAD: `6936a4e` (`11/7 작업 푸시`). 추적 파일의 미커밋 변경은 없었다.
- 최신 게임 구현: 택시 하단 몰딩/휠 개선, 신호 제목 분리, 낮은 가로형 속도계와 기어/조작 안내 분리. `DrivingHudArt.cs`의 가로 게이지와 `PassengerActionUI.cs`/`PassengerBehavior.cs`의 우산·멀미 상태 연결을 확인했다.
- 기존 승객 행동 팝업·복합 특성·날씨/운전 설정·작은 방을 포함한 통합 제품이다. 은계 싱글 / A2 자체 도시 PVP(봇3 또는 LAN 최대4명), LANv16. A3는 현재 배포 지도가 아니다.
- 실행: `builds/EungyeTaxi/EungyeTaxi.exe`. 개발 프로젝트 `unity/EungyeTaxi`, Unity2022.3.62f2, 빌드 `PrototypeBuild.BuildHybrid`.
- 배포 manifest 소스 `95fa0b0af562a5fc66bc80e8cbaaaf5045a3b0ad`, 실행본 배포 커밋 `4fa16c8`, 출하141파일/126,543,288바이트. 해당 배포 이력의 실제 기록 커밋은 `git log -1 --oneline -- docs/work-history/2026-11-07-taxi-hud-corrections.md` 결과 `88cdf13`이다.
- 이후 `6936a4e`는 Unity MCP 패키지와 lock을 추가/갱신했다. manifest 소스부터 HEAD까지 Unity 변경은 패키지 두 파일이며, 이번 점검에서 최신 패키지 구성의 새 빌드를 실행하지 않았다.

## 이번 실제 검증

| 명령 | 결과 |
|---|---|
| `pwsh -NoProfile -File tools/check_workspace.ps1 -Strict` | exit0, Input issues0. Git 프로젝트 지도/설정 해시 일치, 재수입용 eungye-map/route42-map JSON 부재는 선택 입력으로 허용 |
| `pwsh -NoProfile -File tools/update_distribution_manifest.ps1 -VerifyOnly` | exit0, DISTRIBUTION_OK files142 bytes126676070 shippedBytes126543288 |
| 위 명령의 로컬 콘텐츠 프로필 검사 | CONTENT_PROFILE_VALID, 출하 파일 외 선택 사용자 active-profile 1개를 별도 유효성 검사. 출하 파일 수141과 로컬 파일 수142를 구분 |
| `git status --short --untracked-files=no` | 출력 없음. 기존 추적 파일 미커밋 변경 없음 |
| `git rev-list --left-right --count HEAD...origin/main` | 0/0. 로컬에 저장된 origin/main 기준이며 이번에 fetch/push하지 않음 |

검색 중 PowerShell의 brace/glob 경로 전달 오류가 있었으며, 명시 경로/rg의 `-g` 방식으로 다시 읽었다. 이 오류를 게임 검사 실패로 계산하지 않는다.

## 기존 자료 보존

점검 시작부터 있던 미추적 항목은 그대로 보존했다: 루트 `Library/`, `Packages/`, `ProjectSettings/`, `UserSettings/`, `builds/EungyeTaxi/content/`, 콘텐츠 마인드맵/목차 복사본2개, `docs/work-history/2026-10-26-status-review.md`, `unity/EungyeTaxi/ProjectSettings/PackageManagerSettings.asset`, `unity/YeongtongTaxi/`. 이번 기록 커밋에 포함하지 않는다.

## 남은 작업과 검증 한계

- 이번에는 Unity 재빌드, 위치 제어 Player 검사, 자동 물리 주행, 수동 플레이, 전체 캠페인, 다른PC LAN을 실행하지 않았다. 11/07의 Editor/Player 성공 수치는 당시 배포 이력의 증거이며 이번 재실행 결과가 아니다.
- 우선 후속: 사람 장시간 주행/패드 조작감, 전체3일의 운송·시설·경제·비 수용, 다른PC LAN, 저사양LOD/성능.
- 아트 후속: 차체/실내 재질·월드·승객 표정·정산 연출.
- 확률 미끄러짐은 잠금 유지, 눈·결빙·동적 낮밤은 후속 범위다.
- `docs/CURRENT-STATE.md`는 11/06의 파일 수/프로토콜 요약을 포함하고 콘텐츠 목록의 일부 표는 확장 전 승객 수를 포함한다. 현황은 11/07 최신 이력·담당 확장 기획19·manifest를 우선한다. 이번에는 오래된 요약 문서 전체를 재작성하지 않았다.

## 재현

저장소 루트에서 위 Strict/VerifyOnly 명령으로 입력과 배포 파일을 확인한다. 실행 폴더 전체를 유지하고 exe에서 은계 싱글 또는 A2 PVP를 선택한다. 정식 프로젝트 지도는 준비돼 있으며 원본 재수입이 필요할 때만 지도 ZIP을 풀고 `-RequireRawInputs`로 추가 검사한다.
