# 2026-10-21 MIT 앞차 추종 ON/OFF

## 요구·선별·실제 구현

작업 시작 시 LATEST, 범위, 체크리스트, 세계/주행 기획과 기존 TrafficCar/TrafficSystem을 대조했다. 기존 AI에 이미 차로·신호·교차로 예약·장애물 제동이 있어, mchrbn/unity-traffic-simulation의 같은 방향 앞차 속도 추종 분기만 수정 도입했다. 사용자 확정에 따라 별도 연습/비교 빌드가 아닌 기본 은계 `builds/EungyeTaxi/EungyeTaxi.exe`에 포함했다.

Esc → 앞차 추종 보완 ON/OFF. 최초 ON, 선택 저장, OFF에서 기존 속도 목표를 그대로 사용한다. 전환은 차량 재생성/경로 초기화 없이 다음 물리 갱신에 반영된다. 기존 안전 상한보다 가속시키지 않는다. 테스트는 사용자 설정을 쓰지 않는다.

## 출처·라이선스·변경 파일

원본 commit `4f03f4c276e2b82f033a5f037f43036ba9ab6be3`, VehicleAI.cs MoveVehicle 143–148행. Copyright (c) 2019 Matthieu Cherubini, MIT. 상업 이용·수정·배포 가능하며 저작권과 허가문 보존. 샘플 에셋은 도입하지 않았다. 이 판단은 도입 코드 범위이며 지도/Unity 등 전체 제품의 권리 정리를 완료했다는 뜻이 아니다.

- `MitTrafficFollowing.cs`: 수정 파생 계산. 실제 앞차 속도, 4~8m 거리 완화, 기존 안전 상한 보존.
- `MitTrafficOptions.cs`, `TrafficSystem.cs`, `TrafficCar.cs`, `DriveHUD.cs`: 설정/저장/실제 차량 호출/메뉴.
- `MitTrafficSmokeTest.cs`, `DriveSession.cs`, `DistrictTrafficSmokeTest.cs`: 실행 검사와 적용 횟수.
- `tests/MitTrafficFollowingTests.cs`, `tools/test_prototype.ps1`: 순수 모델 검사.
- `Assets/Editor/PrototypeBuild.cs`: 빌드에 MIT 전문/수정 고지 자동 동봉.
- `licenses/mchrbn-unity-traffic-simulation/`: 원본 README·코드·커밋·LICENSE·NOTICE·해시 증빙.
- `docs/mit-traffic-following.md`, 범위/체크리스트/세계·주행 기획, 이력/LATEST: 사용·출처·인계.

위 Assets 경로는 `unity/YeongtongTaxi/` 기준이다. 전체 파일 매핑은 [기능 문서](../mit-traffic-following.md). 로컬 제3자 사용 대장에도 CODE-001을 추가했다. 기존 감사 문서/README의 미커밋 변경은 별도 유지한다.

## 실제 검증

- `pwsh -File tools/check_workspace.ps1 -Strict`: 종료0, Input issues: 0. 지도/ZIP 변경 없음.
- `pwsh -File tools/test_prototype.ps1`: 종료0, 20묶음 통과. 새 계산 검사에 ON/OFF·방향·거리·신호 상한·450조합 포함.
- Unity 2022.3.62f2 `PrototypeBuild.BuildHybrid`: 종료0 (`artifacts/mit-integrated-final-build.log`). 로컬 Unity 설치는 `D:/Program Files/Unity Hub/Editor/2022.3.62f2/Editor/Unity.exe`.
- 기본 exe `-mitTrafficTest -hybridV2`: 종료0, success=True. 실제 NPC ON 감속/OFF 기준 복귀/재활성화/경로 유지, 완전 정지·재출발, 테스트 설정 비저장. `artifacts/mit-traffic-test.txt`, `mit-integrated-ui.log`.
- 1280×720, 1600×900, 1920×1080 캡처 직접 확인. ON/OFF 문구와 버튼이 메뉴 안에 표시됨. `artifacts/mit-traffic-menu-{width}.png`. 자동 설정 전환/화면 검사이며 사람의 클릭 검사는 아님.
- `-districtTrafficTest -mitTrafficOn -batchmode -nographics`: 종료0, 180초·48대 모두 이동, 45,639.13m, 추가 제한120,826틱. 경로 이탈0·겹침0·장시간 정체0·연속 여정6, 적색3대 정지/녹색3대 출발 통과. `artifacts/mit-on-district-traffic-test.txt`.
- `-districtTrafficTest -mitTrafficOff -hybridV2 -batchmode -nographics`: 종료0, 180초·48대 모두 이동, 45,926.57m, 추가 제한0틱. 경로 이탈0·겹침0·장시간 정체0·연속 여정6, 적색3대 정지/녹색3대 출발 통과. `artifacts/mit-off-district-traffic-test.txt`.
- 코드 리뷰 반영: 완전 정지를 명시적으로 검사하도록 보강하고 의미 없는 자기 비교 단언 제거.
- `git diff --check`: 종료0.

처음 격리 검사는 지도 밖 fixture를 production Spawn으로 만들려다 실패했다. 지도 검증을 우회하는 제품 수정 없이 검사에서 직접 초기화해 해결했다. 초기 UI 검사는 `-hybridV2` 누락으로 동시 작업의 연료 안내가 예외를 냈으므로 깨끗한 UI 검증으로 계산하지 않는다. 위 최종 UI 실행은 해당 인자 포함, 예외 검색 결과 없음.

## 빌드 출처와 남은 검증

기본 빌드에는 동시에 진행 중인 승객/표지 작업의 당시 변경도 포함된다. 이 작업은 해당 소스 변경을 임의 커밋하지 않는다. 빌드 후 다른 작업에서 RoadSignage/PlayabilityUISmokeTest가 추가 변경되어, 현재 전체 작업트리와 배포 DLL이 완전히 같은 소스라고 주장하지 않는다. 배포 manifest의 sourceCommit은 MIT 소스 기준이며 integratedWorkingTree/주의 기록을 함께 남긴다. 후속 통합 빌드 시 관련 회귀와 manifest 갱신 필수.

수동 3일 캠페인·수동 ON/OFF 체감 비교·실물 패드·다른 PC 설정 저장 복원은 미검증. NPC 자동 주행은 플레이어 자동 물리 주행이나 수동 플레이가 아니다. 기존 출처 불확정 지도/폰트 등은 기존 라이선스 감사의 후속 항목이다.

## 실행/재현

일반 사용: 기본 exe 실행 → Esc → 앞차 추종 보완 버튼 → 계속 운전. 테스트는 위 명령 인자를 사용한다. ON/OFF 강제 인자는 저장 선택을 해당 실행에서만 덮어쓴다. 재현 상세는 기능 문서 참조. 산출 로그/캡처는 Git 제외 artifacts에 보관하며 이 문서에는 실제 결과를 기록한다. 배포 폴더의 ThirdPartyNotices에 MIT 전문과 NOTICE가 존재해야 한다.

배포 확인: 실행 폴더127개 파일의 크기·SHA256 manifest 재생성 후 전부 대조 통과. 원본 증빙5개 해시 대조 통과, 배포 MIT/NOTICE와 저장소 원본 해시 일치. 원본 증빙은 .gitattributes로 줄바꿈 변환을 막아 보존한다. MIT 소스/문서 커밋은 `dfcd821` (`git log -1 --oneline --`로 확인). 배포 커밋은 그 뒤 별도이며 해당 커밋 자체 해시를 문서에 추측해 넣지 않는다.
