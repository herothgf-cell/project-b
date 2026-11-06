# 2026-10-26 현황 점검

## 요구와 확인 기준

현재 개발·배포 상태와 다음 작업을 파악했다. AGENTS, LATEST, 최신 통합 편집기/잔여 개발 기록, prototype-scope, prototype-development-checklist와 MannerLedger/DrivingManners/PrototypeState를 대조했다.

## 현재 상태

- 로컬 main HEAD와 원격 main 모두 `12935f52db31e5632731a4bc25f36509070edcc5`다. 배포 manifest의 소스는 `20b659375a4d0d0beef8ad018bf51119096804ce`, 검증일은 2026-10-23이다.
- 기본 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`, Unity 프로젝트는 `unity/EungyeTaxi`, 빌드는 `PrototypeBuild.BuildHybrid`다.
- 현재 캠페인은 3일×20분·순수익 비교다. 9코스/17요청, 24개 정차 후보와 대기 승객, 운전자 컨디션/거점, 취객/청결/세차, 중단 운송 복원, 점수 알림/결과 정산 및 Unity 전체 지도 편집이 구현된 상태다.
- P8-07 안정 주행의 급가감속 감점 회복은 미구현이다. MannerLedger는 감점/초기화/복원만 제공하며, DrivingManners에도 안정 주행 회복 누적 처리가 없다.
- P7-06/P8-06/P9-07의 일반 교통 포함 수동 20분/3일 경제 검증, 대표 상황/거점 전 구간 주행 및 Unity GUI 수동 편집이 남아 있다. 현장 조사 중단은 유지한다.

## 이번 수행 검증

- `git status --short`: 기존 미추적 루트 Library/Packages/ProjectSettings/UserSettings, 콘텐츠 하위 폴더, Unity PackageManagerSettings 및 구 YeongtongTaxi 폴더 확인·보존. 추적 파일 변경은 없었다.
- `git ls-remote origin refs/heads/main`: 종료 0, 로컬 HEAD 일치. 최초 sandbox 네트워크 실패 후 승인된 외부 읽기 검사로 확인했다.
- `pwsh -NoProfile -File tools/check_workspace.ps1 -Strict`: 종료 0, Input issues: 0. Git 정식 지도 존재. eungye-map.json/route42-map.json 가져오기 입력은 부재하며 현재 검사에서는 OPTIONAL이다. 원본 재도입용 입력 해시 검증 통과를 뜻하지 않는다.
- `pwsh -NoProfile -File tools/update_distribution_manifest.ps1 -VerifyOnly`: 종료 0, DISTRIBUTION_OK files=130 bytes=118426992.
- 새 Unity 빌드/게임 실행/모델 검사/자동 물리/수동 플레이/전체 캠페인은 이번 점검에서 수행하지 않았다. 10월 23일의 자동 검사 결과는 해당 기록의 조건을 따른다.

## 변경 파일과 다음 작업

이번 변경은 이 기록과 LATEST의 점검 요약뿐이다. 게임 코드·지도·빌드·manifest·기존 사용자 파일은 수정하지 않았다. 별도 커밋/푸시는 수행하지 않았다.

다음 개발은 P8-07 회복 구현과 저장/S 표시 검증, 이후 일반 교통 포함 대표 상황/24정차/거점 주행, 최신 승객·청결·보너스를 포함한 20분/3일 밸런스 확인 순서다. 현재 빌드는 위 exe로 실행하며 Unity 편집은 Taxi → 프로젝트 지도 3D 편집을 사용한다.
