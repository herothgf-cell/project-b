# 2026-11-04 현재 현황·플레이 피드백 조사

## 요구사항과 작업 범위

사용자는 현재 구현을 파악하고 조향 반응·제동·차/사람 충돌·이탈 시 내비 소실·M 자기 위치·승차 칸·F하차 인지·해상도별 가독성·신호 대기를 검토하도록 요청했다. 피드백은 싱글 플레이에서 얻었으며 두 모드 공통이라고 확인했다.

이번 작업은 **기획/코드/배포 대조와 개선 후보 정리**다. 게임 코드·수치·지도·빌드·개인 저장은 수정하지 않았고 실제 체감 문제가 해결됐다고 표시하지 않는다. 사고 확장과 신호 변경의 중요한 결정은 개발 전에 별도 확인할 항목이다.

## 시작 상태와 조사 결과

- 시작 main `703d3e9`, tracked dirty 없음. 라이선스/마인드맵 실험·과거 편집 계획·도구 등 기존 untracked 파일은 그대로 보존했다.
- AGENTS·LATEST·CURRENT-STATE·scope·체크리스트와 주행01/생활03/화면05·사고 후속/PVP 문서를 대조했다.
- 실행 `builds/EungyeTaxi/EungyeTaxi.exe`, 은계 싱글/3일·A2 PVP/봇3·LAN. 배포 `50b0ad0`, 소스 `57bab6f`를 그대로 사용한다.
- 이전 대화의 ‘사고 수리 미구현’과 달리 P15 손상/수리/견인은 구현됐다. 일반 보행자는 비물리 접촉 감점, 대기 승객은 별도 외형이라 같은 사고 시스템이 아니다.
- 재탐색 결과가 null일 때 이전 경로를 지우는 흐름, 작은 F안내의 내비 잔여거리 의존, 속도별 조향 감소/제동18%·화면 축척의 가독성 후보를 확인했다. 코드로 설명 가능한 경로이며 사용자 보고 장소를 실행 재현한 것은 아니다.
- [상세 근거·수치·개선 순서·검증 후보](../future-design/2026-11-04-playtest-feedback-review.md)에 각 피드백을 현재 구현과 구분해 정리했다.

## 변경 파일

| 파일 | 실제 변경 |
|---|---|
| `docs/future-design/2026-11-04-playtest-feedback-review.md` | 현재 상태, 피드백별 조작/접촉 구분 대조, 우선순위·미검증·개선 제안 |
| `docs/prototype-development-checklist.md` | 조사만 완료, 실제 개선6묶음은 미완료로 추가 |
| `docs/content-design/01-driving-and-traffic.md` | 조작·제동·신호 재검토 연결, 기존 수치 유지 |
| `docs/content-design/03-life-and-economy.md` | P15 구현과 사람 접촉/사고 확장 구분 |
| `docs/content-design/05-interface-and-navigation.md` | 경로·승하차·가독성 후보 연결, 개발 전 명시 |
| `docs/CURRENT-STATE.md`, `docs/work-history/LATEST.md` | 최신 조사와 다음 작업 연결, 배포 기준 유지 |

## 실제 수행한 검증

| 구분 | 명령/방법 | 결과 |
|---|---|---|
| 배포 파일 | `pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly` | exit0,140파일/115,464,894바이트 일치 |
| 로컬 입력 | `pwsh -File tools/check_workspace.ps1 -Strict` | exit0,issues0 |
| 정적 코드·기획 대조 | `rg`/파일 읽기, TaxiController/DriveMath/DriveFlowPolicy/RoadNavigation/FullMapOverlay/MapPresentation/CockpitUI/PassengerJobs/MapData/DrivingManners/VehicleDamageController/AmbientPedestrianSystem/PvpNativePrediction 등 | 각 피드백의 실제 구현 경로 확인 |
| 코드 수치 계산 | PowerShell double 수식으로 현재 조향/보간·축척식 대입 | 20/30/40km/h 중심각30.69/10.60/6.01도, S토크630/Space3500. 1280 HUD글자11.2/14.4px·2560은14/18px |
| 문서 변경 | `git diff --check`, 관련 Markdown 로컬 링크 검사 | 실제 종료 결과는 아래 최종 확인 절 |

수치 계산 도중 PowerShell Math 오버로드가 정수 인자로 반올림한 첫 출력은 근거로 사용하지 않았다. 모든 상수를 double로 지정해 재계산한 결과만 상세 문서에 남겼다. 토크 비율을 실제 감속도/정지 거리의 비율로 확대하지 않는다.

## 미검증·다음 작업

- 위치 제어 검사, 자동 물리 주행, 수동 플레이, 전체 캠페인, 실제 LAN/다른 PC 검사는 이번에 하지 않았다. 새 Unity 빌드·자동 게임 기능 테스트도 필요하지 않아 실행하지 않았다.
- 제안 순서는 인지/안내 → 조작·제동 → 사고/신호 납득성이다. 사람 접촉의 후속 처리 수준과 신호 최적화 방식은 기획 결정이 필요하다. 사용자의 운전 편의 피드백을 이미 해결됐다고 간주하지 않는다.
- 구현 시 마지막 경로의 표시와 제어용 유효 경로를 분리하고, 승하차 행동은 내비 선 소실과 독립시키며, 기존 법규/만족도·요금·저장·서버 권한을 보존한다.
- 실행법은 위 기본 exe. 개발 입력/정식 지도·ZIP·manifest는 그대로다. 배포/푸시 요청이 없는 현황 검토이므로 새 실행파일을 배포하지 않는다.
- 실제 기록 커밋은 `git log -1 --oneline -- docs/work-history/2026-11-04-playtest-feedback-audit.md`로 확인한다.

## 최종 문서 확인

변경8문서의 Markdown 로컬 링크136개를 실제 파일 존재로 검사해 누락0/종료0을 확인했다. `git diff --check` 종료0. 게임 소스/배포/지도 변경 없이 이번8문서만 커밋하며, 기존 미추적 실험/라이선스/마인드맵 자료는 포함하지 않는다. 새 빌드/푸시/플레이 결과로 표시하지 않는다.
