# 2026-10-29 PVP 자체 시험장·차량 후보 및 통합

## 요구·구현

사용자는 Task 2까지 계속 구현하고, 이후 PC 화면/키보드/마우스 조작 없이 남은 수정·커밋→최신 main 반영→충돌 해결·검증→main 병합·푸시를 요청했다. 다른 채팅의 진행 중 변경은 보존한다.

| 영역 | 실제 구현 | 한계 |
|---|---|---|
| 자체 지도 | pvp_circuit_01 자산, 외곽 일방향 순환·내부 연결, 4정차점/4출발점 | 회색 상자 시험장, 신호·NPC 없음 |
| 경로 | 명시적 방향 연결, 최단 거리/시험 속도 기준 예상 최속, 경로 없음 | 실제 교통·신호 대기 반영은 Task 6 |
| 도로 추적 | 이전 차로와 합법 전이를 함께 검사, 분기점 동거리 이전 차로 유지 | 차량 중심점 기준, 접촉/차체 전폭 충돌 모사 아님 |
| 복구 | 마지막 유효 차로의 후방 위치로 정지 복구 | 온라인 권한·점유 차량·운송/원장 보존은 후속 |
| 차량 | 고정 tick 평면 운동 A/B, 가속·조향·제동·명시적 후진·입력 유실 감속 | WheelCollider/접촉 물리 아님, 네트워크 결정성 미검증 |
| 상태 | 위치·방향·속도·조향·후보 프로필 캡처/복원 | 동일 로컬 tick 재생 검사, 플랫폼 간 동일성 주장 없음 |
| 실행 연결 | 초기 화면의 오프라인 시험장 버튼, 차량/카메라/HUD/경로/목표 선택 | 경기 시간·점수·승객·온라인을 시작하지 않음 |
| 실패 복구 | 잘못된 지도는 시작 실패 안내·생성 자원 정리 후 재시도 | 실제 오류 로그를 회귀로 재현 후 수정 |

변경 파일은 unity/EungyeTaxi/Assets/Pvp/Runtime/{Map,Driving}, Content/Maps/PvpArena01.asset, PvpPracticeSession.cs, PvpPracticeProbe.cs, Bootstrap/UI/Scene 연결, Editor/PvpMapTests.cs·PvpVehicleTests.cs·PvpTask2Tests.cs 및 .meta다. 기존 캠페인 소스·지도·배포 실행물은 수정하지 않는다.

## 검사와 수정 근거

- 지도/차량은 테스트 작성 후 없는 타입의 컴파일 실패→구현 후 Unity 참조 컴파일 통과. 이를 행동 실패 실행으로 표현하지 않는다.
- 통합은 실제 Unity에서 `PVP_TASK2: offline practice session missing` 종료1을 확인한 뒤 구현했다.
- 분기점에서 같은 거리의 마지막 도로를 선택하던 결함을 이전 차로 기반 후보 필터/동거리 유지로 수정했다. 동쪽/내부 분기 회귀 포함.
- 지도 생성 재진입 시 material 누수를 막기 위해 지도 root가 공유 material3개를 소유·정리한다.
- 빈 출발점 지도 시작 후 재시도 불가를 task2-retry-red.log의 실제 예외/종료1로 재현했다. 시작 전 도달성 검사·실패 component/world 정리·원인 표시 후 동일 회귀 통과.
- `Pvp.Tests.EditMode.PvpTask2Tests.Run`: task2-final-editor.log 종료0. 기존 격리8그룹·실제 Editor 격리·지도 계약·차량·통합/실패재시도 통과.
- Task2 첫 BuildClient 종료0, 실제 Player `-pvpPracticeCheck` 종료0. 입력에 따른 누적 이동28.116m·가속·제동·경로·후방 복구·캠페인 객체 부재를 확인. **운동 모델의 자동 입력 검사이며 Unity 접촉 물리 주행/사람 플레이/전체 경기 검사가 아니다.**
- 첫 자동 캡처 PNG는 검은 화면으로 확인되어 화면 검증 증거에서 제외했다. 이후 사용자가 PC 조작 중단을 요청했고 Task2 화면·실물 입력 검증은 미완료로 유지한다. Task1 초기 설정 화면 검증은 별도 직전 이력 참조.

## 차량 후보 측정

오프라인50Hz, 교통/접촉 없음. 최종 채택값이 아니다.

| 후보 | 가속3초 후 m/s | 20m/s 제동 거리/시간 | 5m/s 조향0.5초 방향 변화 | 25m/s 동일 조향 |
|---|---:|---|---:|---:|
| A | 25.50001 | 15.38479m / 1.54초 | 38.26392도 | 6.572621도 |
| B | 21.59997 | 13.33401m / 1.34초 | 29.56595도 | 5.084655도 |

조향 시험은 입력 초기0에서 관성 감속하며 측정. A는 출발·저속 회전, B는 제동·완만한 조향 후보다. 기존 캠페인 대비 우수성 또는 사람 조작감 통과로 해석하지 않는다.

## 실행·검증 방법

- Unity2022.3.62f2, 프로젝트 unity/EungyeTaxi, Windows64.
- Editor: `-batchmode -nographics -quit -projectPath <절대 프로젝트> -buildTarget Win64 -executeMethod Pvp.Tests.EditMode.PvpTask2Tests.Run -logFile <절대 로그>`.
- 빌드: 같은 명령의 executeMethod를 `Pvp.Editor.PvpBuild.BuildClient`로 변경.
- 화면 없는 Player 회귀: `builds/PvpPrototype/PvpPrototype.exe -batchmode -nographics -pvpPracticeCheck -logFile <절대 로그>`.
- 사용자 직접 실행: 초기 화면→오프라인 시험장. W/↑가속, S/↓/Space제동, A/D조향, 정차 후 Q전후진, 1/2후보, Tab목표, M전체지도, Backspace후방복구, Esc초기 화면.
- 기본 캠페인: builds/EungyeTaxi/EungyeTaxi.exe / 은계 / PrototypeBuild.BuildHybrid 유지.

## 미완료·다음 작업

Task 2 구현·자동 검사 범위만 반영한다. 화면·키보드/패드 실제 조작·사람 체감·자동 전체 코스 완주·신호/NPC 포함 주행·다른 PC 배포는 미검증이다. 따라서 Task 2/P1 전체 수용 게이트를 닫지 않고 Task 3 SDK 채택·온라인 개발 완료로 넘기지 않는다. 경기 시간/최종 인원/배점/충돌/SDK는 미정·후보 유지.

PVP exe/로그/캡처는 Git 제외 로컬 산출물이다. main 병합은 소스·기획 반영이며 PVP 실행파일 원격 배포가 아니다. 캠페인 실행파일과 manifest는 보존하므로 재생성하지 않는다.

## 통합 결과

최신 main 반영·검증·원격 게시 결과는 아래 후속 기록으로 추가한다. 이 파일의 실제 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-29-pvp-arena-integration.md`로 확인한다.

### 통합 검증 완료 (2026-10-29)

- PVP 구현 커밋 `66c5368`; 최신 main `3293e3b`를 반영한 병합 `2507300`.
- 실제 충돌은 docs/content-design/README.md와 docs/work-history/LATEST.md. PVP와 main의 캠페인 기획·인계 블록을 모두 유지해 해결했다.
- 통합 후 `PvpTask2Tests.Run` 종료0 (merged-editor.log), `PvpBuild.BuildClient` 종료0 (merged-build.log).
- `-batchmode -nographics` Player 격리2프로세스 및 시험장 입력 모델 검사 모두 종료0. 진단 파일이 서로 다르고 자동 이동28.116m 확인. 이 재검사는 순차 프로세스이며 앞선 Task1 동시2프로세스 검사와 구분한다.
- 캠페인 모델·PVP 모델·Strict·배포 manifest 검사 모두 종료0. manifest130파일/112072682바이트 일치. main 대비 캠페인 Assets/Scripts·ProjectSettings·기본 배포·distribution·config/maps 변경 없음.
- 검토에서 발견한 분기점 선택·material 정리·시작 실패 재시도 문제 해결 후 검토자 재확인. 추가 병합 차단 문제 없음.
- 다른 채팅은 C:/git/project-b의 codex/passenger-type-variants에서 계속 작업 중이다. 해당 checkout·미커밋/미추적 파일·작업 브랜치를 수정하지 않았다. 완료 대기 없이 이미 main에 게시된 커밋만 통합했다. 해당 작업은 나중에 최신 main을 병합하고 검증해야 한다.
- 사용자 요청 이후 UI 도구·키보드·마우스·창 활성화 없음. 기존 실행 중 PVP 창도 임의 종료하지 않았다. Task2 실제 화면·입력·사람 수용 게이트는 여전히 미완료.
- 원격 main 게시에는 일반 fast-forward push만 사용하며 강제 push하지 않는다. 게시 성공 여부/최종 SHA는 Git 원격 확인으로 보고한다.

### 원격 반영 확인

일반 atomic push 종료0. main과 feature/pvp-prototype가 모두 835f6cfc7aa8fb067da9b74e3126e275fca32498인 것을 git ls-remote로 확인했다. 로컬 main도 같은 통합 상태로 갱신했고 다른 checkout은 codex/passenger-type-variants 그대로 유지됐다. 이 문단은 해당 게시 결과의 후속 기록이며 자기 커밋 해시를 미리 기재하지 않는다.

