# 미니맵 법규 이름·호출 경유명·PC 인계 이력

## 요청과 설계

은계 맵 각 법규 상황을 미니맵에서 이름으로 찾고, 호출 선택 전에 실제 경유 상황을 알 수 있어야 한다. 여러 PC를 오가도 작업 상태·검증·필수 데이터가 대화에만 남지 않도록 이력을 Git에서 관리한다. 기준 커밋은 `a971d9e`다.

## 구현

- `DrivingReadability.cs`: 가까운 지도에는 보이는 구역과 이번 경유 구역, M 전체 지도에는 7개 구역의 한국어 이름표·위치 연결선. 승객에게 이동 중인 단계부터 선택 구역을 분홍 강조하고, 지도 밖이면 이름과 지도 밖 표시를 유지한다. 실제 승차/도착 표지와 같은 좌표 계산으로 이름 배치의 제외 영역을 예약한다.
- `LawZoneMapLayout.cs`: 이름끼리·플레이어·목표 표시가 겹치지 않게 배치하고 지도 내부에 제한한다. 상황 수가 적은 현재 7곳 기준이다.
- `ServiceLawZones.cs`, `DriveHUD.cs`: 같은 실제 ZoneFor 호출 데이터에서 두 줄 지도 이름과 정확한 경유 상황명을 가져온다. 호출 버튼은 기존 거리·요금·제한 시간·요구 점수를 유지하고 다음 줄에 경유명을 표시한다.
- `AGENTS.md`, `docs/work-history/`: 이후 작업의 시작/완료 기록 규칙, 날짜별 기록, 최신 인계 및 PC 이동 절차.
- `config/maps/local-input-manifest.json`, `tools/check_workspace.ps1`: Git 제외 생성 입력 지도3개 SHA256·크기 및 Unity 프로젝트 버전 확인. Root 인자로 다른 폴더에서도 검사 가능하며 -Strict에서 누락/불일치는 종료1이다. 실제 Unity 설치 여부·다른 PC 실행 성공을 대신 판정하지 않는다.

## 검증

- 최초 이름 배치 검사는 밀집 구역 중첩으로 실패했고, 배치 알고리즘 적용 후 `LAW_ZONE_MAP_LAYOUT_OK` 통과. 원인은 컴파일 오류가 아니라 겹침 조건이다.
- 빌드: Unity `PrototypeBuild.BuildHybrid`, 로그 `artifacts/law-zone-labels-build.log`. 기존 지도망/원본HD 보존/OSM 대조 검사 포함.
- 화면 검사: `EungyeTaxi.exe -eungye -lawZoneLabelsTest -hybridV2`, `artifacts/law-zone-labels-test.txt`, 1280×720·1600×900 캡처. 실제 호출의7종 상황명, 전체 지도7이름, 비중첩/화면 내부, 승차 전 선택 강조·지도 밖 이름 유지, 도착 텍스트 비가림을 확인한다. 최종 종료0 및 success=True 확인. 세 화면 캡처를 실제로 열어 1280/1600 호출 카드의 전체 경유명과7개 지도 이름, 선택 시 승차·도착 표지가 보이는 것을 확인했다.
- PC 입력 검사: `pwsh -File tools/check_workspace.ps1 -Strict` 현재 자료 종료0. 별도 테스트 폴더에서 누락 종료1, 잘못된 해시 종료1, 일치 자료 종료0 확인. `artifacts/workspace-check-*.txt`.
- 기본 Windows PowerShell5 호스트에서는 로컬 실행 정책으로 실행되지 않아 검사 도구의 안내는 PowerShell7(pwsh) 기준이다. 실행 정책을 변경하지 않았다.

## 범위와 다음 작업

이번 변경은 안내 표시·작업 인계다. 운송 경로·법규·매너·운임 계산은 기존 기능을 사용한다. 다른 PC를 실제로 사용한 빌드/패드/하드웨어 검사는 수행하지 않았다. 지도 3개와 raw/builds/artifacts는 Git에 포함되지 않아 [PC 이동 절차](README.md)를 따라 별도 준비한다.

시작 전부터 `README.md` 수정과 `docs/license-maintenance.md`, `docs/open-source-audit.md`, `docs/third-party-register.md`, `licenses/` 미추적 작업이 있었다. 기존 사용자 변경이며 이번 기능 커밋과 분리해서 보존한다. PC 이동 시 이 로컬 작업도 별도 보존/커밋 대상인지 확인한다.

현재 실행: `builds/EungyeTaxi/EungyeTaxi.exe`, 기본 은계 영업에서 M 전체 지도 및 호출 카드 확인. 작업 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-20-law-zone-labels-and-handoff.md`로 확인한다.

최종 상태: 구현·검증·작업 이력 갱신 완료. 기본 은계 실행파일 빌드 성공. Git 기준과 미커밋 사용자 작업은 LATEST를 함께 확인한다.
