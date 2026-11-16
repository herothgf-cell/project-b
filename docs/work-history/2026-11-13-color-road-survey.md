# 색상별 도로 조사와 북쪽 차로·짧은 도로 연결 r21 부분 반영

## 요청과 현재 결과

첨부 지도 다섯 색상 구간의 양방향 차로·중앙선 전환·분리시설·진행 화살표를 거리뷰와 대조한다. 모호한 부분은 번호 지도와 함께 질문한다. 사용자가 하늘색①·② 거리뷰 탭을 지정했고, 두 출입구 사이와 동쪽 다음 신호 지점까지 연속 이동하여 교차 확인했다.

① 남측 출입구와② 북측 아파트 출입구를 기준으로 삼을 수 있으나, 두 촬영점 자체가 차로 전환 끝점인 것은 아니다. ② 동쪽에도 반대 방향 추가 차로가 있고, 다음 굴곡 신호 지점 양쪽에도 접근 차로가 추가된다. 'J8066 부근3차로→②부터 계속2차로'라는 단순화는 부정확하다. 호수 남쪽은 왕복2차로와 넓은 중앙 황색 빗금 안전지대가 확인됐다.

**부분 반영 상태:** 정식 작성 지도는 r21이다. r20에서 하늘색 북쪽 J8921→J7355→J7292→J7254의 3개 source 그룹을 북행2·남행1로 수정했다. J7355 북행은 안쪽 좌/직·바깥 직진, J7254 북행은 안쪽 좌/직·바깥 우회전 전용으로 구분한다. 번호13 J7292는 사용자 최종 확인에 따라 노면 화살표가 없는 지점으로 관리한다. 상세 관찰과 규칙/표시의 구분은 `docs/content-design/road-survey/2026-11-13-color-road-audit.md`를 따른다. 폭·연결 곡선은 게임 치수이며 실측값이 아니다.

r21에서는 대은로 J1882→J2225의 짧은 본선 누락을 복구했다. 원본 `37612022:A0020000:{1086,1094,1786}:0` 길이40.18695m가 자동 생성의41m 최소 조건보다 짧아 전체 구간이 빠져 있었다. 인접 현 그래프의 방향별2차로에 맞춘 내부 차로4개·직진 연결8개를 추가하고 두 교차로를 별도 규칙으로 유지했다. 기존 좌/우 연결·기존 차로 ID·원본 baseline·전역 수입 조건을 보존했다. 방향별2차로는 이 연결 복구의 잠정 게임 배치이며, 조사 중인 대은로3/2 전환 전체를 실경 재현했다는 뜻이 아니다.

현재 r21 `contentHash`는 `aadeeb2f24c026a000b393aa938b9f625bd3f0d9db4a8a85d79a5ac13408aa45`, 보존된 `baselineHash`는 `81b34b282c27bb03e2ead75ba3385d2c4e5e2bdd0730c53fcbe14fb8f30aebbe`다. 이 값들은 패키지 내부 지문이며 파일 SHA256과 다르다. 확인 시 파일 SHA256은 `e441986ed62f770277602da490ea5753c92279cd132312cd5601edea75344bd8`였다. 이전 r20 콘텐츠 지문은 `7bf7fb04facef74f2c4027ac93cdbc24244a278a5980f805854c70611e2cc35b`다. **배포 실행본은 아직 r19이며 BuildHybrid 재빌드는 대기 중이다. 이번 작업은 push하지 않았다.**

주황 탄성 분리봉의 개별 기둥 렌더·충돌 검사 구현은 통과했으나 실제 조사 도로에는 아직 배치하지 않았다. 다섯 색상 전수조사는 미완료다. 노란 계수로에서는 최신 `x43UqW58YuCBsD-bLEHtWA`(2026-05)와 과거 `Sk8XArPP8Gk-BFGX6H2KaQ`(2024-03)의 서행 L/S/S/S와 중앙 검정 기둥·연결 난간을 관찰했지만 J5692 위치 정렬은 후보 상태다. 이 부분을 지도에 적용했다고 보고하지 않는다.

차로 거리 기준점 헬퍼는 곡선·경사·역방향 기준선의 정확한 표본, 기존 계보 ID와 서비스 위치 재연결 검사를 통과했다. 헬퍼 준비와 실제 현장 포켓/분리봉 배치는 별도다.

## 변경 파일

- `docs/content-design/road-survey/2026-11-13-color-road-audit.md`: 촬영 ID·각도·월·관찰과 불확실성.
- `docs/content-design/road-survey/draw-question-map.ps1`: 정식 지도에서 질문용 위치도 작성.
- `docs/content-design/images/eungye-road-survey-blue-questions.png`: 질문 범위①·②. 실제 전환 좌표 아님.
- `docs/prototype-development-checklist.md`, `docs/work-history/LATEST.md`: 조사 진행 상태.
- `config/maps/project/eungye.project.json`: 하늘색 북쪽 r20과 대은로 짧은 본선 복구 r21 부분 반영.
- `unity/EungyeTaxi/Assets/Editor/CyanNorthLaneMapEdit.cs`, `CyanNorthLaneMapTests.cs`: 결정적 미리보기·명시적 적용·연결/경로 검증.
- `unity/EungyeTaxi/Assets/Editor/ShortCorridorGapEdit.cs`, `ShortCorridorGapTests.cs`: 정확한 r20 지문을 요구하는 r21 적용과 직접 경로/기존 데이터 보존 검사.
- `unity/EungyeTaxi/Assets/Editor/LaneStationAuthoring.cs`, `LaneStationAuthoringTests.cs`: 실제 차로 거리 기준점과 분할 계보/서비스 재연결 헬퍼.
- `unity/EungyeTaxi/Assets/Scripts/RoadviewCorridorPresentation.cs`, `ReviewedNativeLanePresentation.cs`, `LaneDirectionPaint.cs`, `RoadPaintGeometry.cs`, `EungyeConnectivityRepair.cs` 및 `Assets/Editor/RoadviewCorridorMapEdits.cs`, `RoadviewCorridorTests.cs`: 하늘색 북쪽 단면·차로 표현/연결 및 기존 구간 구분 보완.
- `unity/EungyeTaxi/Assets/Scripts/ReviewedRoadFacilities.cs`, `Assets/Editor/FlexibleBollardTests.cs`: 탄성 분리봉 표시/개별 충돌체 지원과 검사. 현장 배치 미수행.
- `docs/content-design/road-survey/colored-inventory.json`, `build-colored-inventory.ps1`, `cyan-stations.json`, `build-cyan-stations.ps1`, `2026-11-13-yellow-observations.md` 및 `docs/content-design/images/eungye-colored-road-audit-map.png`, `cyan-stations.png`: 다섯 색상 목록·실제 교차로 번호·현재 차로 거리 기준점·노랑 조사 장부. 거리 기준점은 실측 전환점이 아니다.

## 검증과 재현

- Naver 2026-05 거리뷰 두 사용자 탭 및 별도 조사 탭의 스크린샷을 직접 대조. 사용자 탭 원래 각도 보존.
- 질문 지도 생성 스크립트 실행 및 이미지 표시 확인. 스크립트 입력은 `config/maps/project/eungye.project.json`.
- r19 `J8066EastArmMapEdit`와 시설 렌더 코드 대조: 기존90m/100~155m 구간은 임시 추정치이며, 출입구별 추가 차로와 동일하다고 확정할 수 없다.
- `CyanNorthLaneMapTests.PreviewChecks`: `artifacts/color-road-survey/cyan-north-preview-2.log`에 `CYAN_NORTH_CHECKS_OK stops=39 pairs=1482`, `CYAN_NORTH_PREVIEW_OK input-immutable deterministic identity-preserved`, 종료0 확인.
- `CyanNorthLaneMapEdit.Apply`: `artifacts/color-road-survey/cyan-north-apply.log`에 39곳·1,482방향 경로 검사와 `CYAN_NORTH_MAP_APPLIED 7bf7fb04…` 및 종료0 확인. 작성 지도 revision20과 콘텐츠 지문을 다시 읽어 일치 확인.
- `FlexibleBollardTests.Run`: `artifacts/color-road-survey/bollard-green.log`에 `FLEXIBLE_BOLLARD_TEST_OK posts=7 preview/live geometry and individual collision checked`, 종료0 확인. 테스트 기둥7개이며 현장 배치 수가 아니다.
- 짧은 본선 회귀 RED: `artifacts/color-road-survey/short-gap-red.log`에 `missing direct short mainline`으로 종료1. 미리보기 `short-gap-preview.log`는 `SHORT_CORRIDOR_CHECKS_OK direct=4 stops=39 pairs=1482`, 결정성·입력 불변 검사와 종료0.
- `ShortCorridorGapEdit.Apply`: `artifacts/color-road-survey/short-gap-apply.log`에 r21 실제 콘텐츠·baseline 지문과 `SHORT_CORRIDOR_MAP_APPLIED`, 직접4경로·39곳1,482경로·별도 교차로/차체 공간 검사, 저장본 재검증 및 종료0 확인. 현재 파일을 다시 읽어 revision21·지문 일치 확인.
- 거리 기준점 헬퍼: `artifacts/color-road-survey/stations-green.log`에 `LANE_STATION_AUTHORING_TEST_OK exact curved/inclined samples, reversed baseline, existing lineage IDs, service remap`, 종료0.
- r20 저장 이관: `artifacts/color-road-survey/migration-r20.log`에 `JUNCTION_MIGRATION_OK versions=6 checks=58 metadata=preserved strict-default-hash=required`, 종료0. 이어서 `migration-r21.log`의 r21 이관도 7버전·81검사·메타데이터 보존·기본 지도 지문 엄격 확인·r20의 변경되지 않은 교차로 관측 보존, 종료0을 확인했다.
- 긴 곡선 전체를 짧은 합류 검사기에 넘긴 `reverse/abrupt heading` 진단 로그가 남는다. 검사는 구간별 접선 연속성과 겹치는 10m 창/8m 간격으로 전체 차체 공간을 검사한다. 이 통과를 자동 물리 주행 통과로 부르지 않는다.
- BuildHybrid 재빌드, r21 필수 로컬 입력 manifest 갱신/Strict 재검증, 배포 manifest 검증, 위치 제어 렌더, 자동 물리 주행, 수동 게임 플레이, 전체 캠페인 검사는 아직 수행하지 않았다. 위 Editor 검사는 전체 최종검증 완료를 뜻하지 않는다.
- 기존 사용자 씬·ZIP·콘텐츠 프로필·제안서 등 미커밋 파일 보존.

## 이어갈 작업

촬영점과 게임 원본 도로/출입구를 정렬해 하늘색 남쪽 포켓·J8921 접근 확폭과 안전지대 끝점을 개별 모델링한다. 주황 탄성 분리봉·검정 보도 난간·황색 빗금·강철 가드레일을 구별하고 실제 개구부에 맞춰 배치한다. 초록/노랑/주황/분홍은 이번 조사 미완료. 확인되지 않은 치수나 위치는 실제 관측값으로 만들지 않는다. 남은 지도 수정 후 연결·차로·충돌체 개구부 검사, 필수 입력 manifest 갱신/Strict, BuildHybrid 및 배포 manifest 검증을 진행한다. push는 하지 않는다.

재현 시 기존 Unity 설치에서 프로젝트 `unity/EungyeTaxi`를 대상으로 Editor `-executeMethod CyanNorthLaneMapTests.Verify`, `-executeMethod ShortCorridorGapTests.Verify`를 사용한다. 각각의 `PreviewChecks`·`Apply`는 미적용 원본을 전제로 하므로 이미 r21인 현재 지도에 재적용하지 않는다. 짧은 본선 Apply는 정확한 r20 지문만 허용한다. 분리봉 검사는 `-executeMethod FlexibleBollardTests.Run`이다. 기존 로그는 `artifacts/color-road-survey/`에서 위 완료·종료 표식을 확인한다. 로컬 Unity 실행 파일 경로는 PC별 환경 정보이며 저장소 기준 경로와 구분한다.

현재 기본 실행본 `builds/EungyeTaxi/EungyeTaxi.exe`는 이전 r19 배포다. r21 작성 지도 부분 반영과 실행본 배포 반영을 혼동하지 않는다.

### Y07 수정안 검증 및 보류 (후속)
- `Y07MapEdit.cs`, `Y07MapTests.cs` 미리보기 구현. 초기 E3/W4, 서행 L/S/S/R 안을 검증하면서 보도 모서리/승하차 보도 이동, 옆길 진입 경계8m 조정과 연결 곡선 보정, 게임 승하차9 앵커 이관을 추가했다. 각 실패는 `artifacts/color-road-survey/y07-preview*.log` 참조.
- 최종 종전안 Preview9 종료0: 원본 불변/결정성/기존 ID 보존, 차량 공간, 노면선1,732표본, 정차지39·경로1,482 검사 통과. 이전 StopDiagnostics는 baseline39→preview37로 게임9/반대편 누락 원인을 특정했다. `PassengerStopCatalog.cs`는 검토 Y07 구성이 존재하고 원래 기본 게임9 후보가 정확히 일치할 때만 같은 차로40m 지점으로 앵커를 옮긴다. 검증 우회 없음.
- **적용 보류:** 사용자가 ⑦→⑧ 최신 동행 접근을 L/S/S/S **4차로**로 정정했다. 종전 Preview9의 E3/LS/S/S 안은 통과했어도 적용하지 않았으며 현재 작성 지도는 r21 그대로다. 본선3→접근4 전환인지 전체4인지 사용자 확인 대기. 다음은 이 사실을 반영한 새 미리보기 검증.
- 북서 초록 본선 신규 조사 중, J2145를 기존 번호 뒤31번으로 추가. 전체 지도 식별 불편 제보에 따라 은계로×은행로 사거리만 확대한 `eungye-j2145-question.png`를 생성하고 A/B 접근을 구분해 다시 질문했다.
- 노랑 Y12는 ⑫→⑧ 연결을 연속 이동/공개좌표로 대조하여 본선1+1 확인. 모든 방향 표시/시설 station 완료는 아님. 색상별 장부에 관찰과 미확정 항목을 분리했다. 전수조사와 빌드 여전히 미완료, push 없음.

### 2026-11-13 후속: Y07 전환 기준점 확보
- 사용자 최종 사진 확인: ⑬→⑦ 우회전 유입의 바깥 추가 차로만 합류하여 4→3, 고가 **아래부터** 안쪽 좌회전 포켓이 생겨 3→4. 직진 본선3개는 유지한다. 방향 질문은 해결됐으며 종전 E3/LS/S/S Preview9는 적용하지 않는다.
- 합류 끝 근처 최신 촬영 `6o9n0Vawfz-KerD6vhtTXA`: 미니뷰어 카메라 끝점을 지도 주소 조회한 `3zanpp,2AH3na` → lon126.8043523,lat37.4474832 → 게임 x787.6474,z1493.4379. 본선 중심선 약64m. UI 클릭 근사값이며 정밀 측량값 아님.
- 고가 아래 최신 촬영 `rgr44aiZkfzorjzqYdnmYw`: `3zas0n,2AH4Xk` → lon126.8061191,lat37.4480918 → 게임 x944.1260,z1560.6610. 반대 차도 촬영점이므로 본선 중심선에 투영하여 약230m로 사용. 25m 전환 길이는 게임 형상 추정치로 별도 표기한다.
- Y07 후속 구현은 기존 우회전 연결을 바깥 합류 차로로 연결하고, 고가 아래에서 직진3차로를 유지하며 좌회전 포켓을 분리하는 중이다. 실제 지도 저장/빌드 검증 전.
- 새 Y07 Preview10 종료1: 차로 변경창 분할에서 끝점 잔여길이0.001m 미만 때문에 중복 제어점3건. validator 완화 없이 정확한 마지막 점으로 교체하고 근접 끝점 회귀 추가. Preview11 종료0: 동행 직진3/aux4→3/pocket3→4, W7 L/S/S/R, E8 L/S/S/S, 정차39·경로1482·도색1767, 결정성/원본불변/ID 계보 검사 통과.
- 합류·양보 표시 별도 기능: `LaneMergePaintTests.Run` RED 종료1(렌더러 없음) → GREEN 종료0(회전 방향별 경계·명시 근거·시설 검증 통합·통행마스크 불변). Y07 실제 시설 배치 후 통합 preview는 추가 실행 예정. 기본exe/지도r21은 아직 유지.

### Y07 r22 실제 적용
- `Y07MapEdit.Apply` 실행 로그 `artifacts/color-road-survey/y07-apply-r22.log`, 실제 Unity 종료0. 적용 전/저장 후 각각 Y07_CHECKS_OK, 직진3·합류4→3·포켓3→4·서행L/S/S/R·동행최종L/S/S/S, 정차39/경로1482·도색1767.
- 작성 지도 revision22, 콘텐츠 지문 `8c9a5e7f386aeeac9114b7a9c7da0c938fb8019c5456bd20bf6e6b956de388f6`. 원본 baseline 불변. 적용 전 백업 `artifacts/color-road-survey/eungye.r21.before-y07.json`.
- Preview12는 Y07 테스트의 HdEdge.width 참조 컴파일 오류, Preview13은 추가된 회전교차로 테스트의 List.Length 참조 컴파일 오류로 종료1. 둘 다 실제 API 타입에 맞춰 수정했다. Preview14는 비교용 새 문서의 자동GUID 차이를 지도변경으로 오인한 테스트 실패; 같은 wrapper를 재사용하여 Preview15 Y07 검사를 통과했다. 이 실패를 게임 동작 오류로 보고하지 않는다.
- Preview15 회전교차로는 기존 직진이 중앙섬을 통과하는 RED 재현, 무신호 제어/점유 양보 검사는 통과했으나 닫힌 원형 도로 중심선 자기교차 검사 실패. 네 열린 호로 수정한 후 별도 재검사 중, 아직 적용 아님. J5057 보도 오류는 scene 검증 실패 후 원본 노면 fallback의 연쇄 진단이며 관계없는 J5057을 수정하지 않았다.
- r22 저장 이관·필수입력 manifest/Strict 및 BuildHybrid는 후속. 사용자 FirstDrive SHA256은 적용 직전 `1494FF1D97725151FE4E3EA3AF312A5019E68DD94D67A9F207C2BE42567D9B64`로 보존 확인. 실행본은 r19 그대로이며 push 없음.

### r22 저장 이관 및 회전교차로 후속 검사
- `JunctionRouteFeedbackTests.VerifyMigration`: Unity 종료0, `JUNCTION_MIGRATION_OK versions=8 checks=100`; r14~r21→정확한 r22 기본지문만 허용, 사용자 편집/다른 지문/동일 버전 거절, 계약·신호 상태 보존. 로그 `artifacts/color-road-survey/migration-r22.log`.
- 회전교차로 preview2 종료1: 원형 도로 자기 교차 해결 후 생성 보도 8조각이 실제 회전 경로를 침범. 원형 영역뿐 아니라 차량 통과 영역으로 국소 보도 절삭 수정.
- preview3 종료1: 전체 지도 검증/제어·점유 우선권 검사는 통과했으나 서쪽 좌회전 경로 시작점의 방향 급변 검출. 형상 수정 중. 아직 지도 적용/빌드하지 않음.
- Green31 양쪽5차로·보행섬 수정안 preview 종료0,39정차/1482경로 유지 (`green31-preview-1.log`). U 조건 미확정으로 U연결은 보류, 아직 미적용. 정확한 실경 거리/렌더/5차로 도색 검증은 별도 남음.
- 생활도로 외형 전용 범위 도색 억제 지원: RED 예상 실패 후 GREEN 종료0 (`unmarked-paint-green.log`), 계보 거리 연속성/범위 바깥·교차방향·옆도로/가장자리 보존 검사. 실제 지도 시설 배치는 아직 없음.
- 지도32 보행 통로 사용자 확인 완료. 별도 PinkPassage 수정안 준비 중.

### r23 보행 통로·생활도로 실제 적용
- 사용자 지도32(P07) 보행 전용 확정에 따라 차량 차로/연결11개 비활성 및 해당 도로 리본만 제거, 건물 사이 보도 포장 추가(폭5.5m/높이.14m 게임 추정). 실제 관찰되지 않은 차단봉은 만들지 않았다. 물리 차량 강제 진입 차단은 보장하지 않는다.
- P12/P21 동쪽 내부 남북길 네 방향 차로의 무차선·방향표시 없음 메타데이터8개 추가. 도로 가장자리/폭/그래프/기존 시설 보존. 적색 고원부/과속방지턱은 아직 미반영.
- `PinkPassageMapTests.PreviewChecks`: 첫 검사 종료1은 polygon.Position을 서비스 앵커0,0으로 읽은 테스트 결함. 실제 polygon centroid와0.5m 연속 검사를 쓰도록 수정 후 종료0(통합preview5의P07부분). `PinkEastUnmarkedMapTests.PreviewChecks` RED 예상실패→GREEN 종료0.
- `PinkLivingRoadMapApply.Apply` 실제 Unity 종료0. 적용 전/저장 후 모두 보행 연결 제외·범위 도색·39정차1482경로 검사 통과. 로그 `artifacts/color-road-survey/pink-living-apply-r23.log`, 백업 `eungye.r22.before-pink-living.json`.
- 작성 지도r23 콘텐츠 지문 `1233c5ca6b88d5de3c75d7542ebc294409750bd1d2daa5041658149457f7e42d`, baseline불변. 저장 이관·입력manifest/Strict·최종BuildHybrid는 후속. 실행본r19,푸시없음.
- 회전교차로preview4/5는 새로 검증되는 원본 남쪽 차로의 건물 겹침과 진출입 노면 공백을 검출. 원본20.1m 노면 내부에서 최대1.2m 게임 충돌 보정, 실제 진출입 노면 추가 후preview6검사 중. 미적용.
- 31번A/B 사용자 유턴 조건확정: 모두 좌회전 또는 보행신호시. 조건부U를 포함한 수정안 재검사 예정.
