# 2026-11-09 차로 표시·동부 차로 수·주차장 좌회전 후속

## 요구와 범위

사용자 화면7개 중6/7은 동일 위치다. J866_5285 서향 정확 포즈, J5057_4449 양방향의 차로별 화살표/어긋난 선, 내비 외형 근거 문구 제거, 동쪽 은계중앙로·은계로 차로 수 확인, J7917_2565 동향의 지속 녹색/좌회전 누락을 대상으로 한다. [접근별 자료·기획](../content-design/21-lane-movement-roadview-followup.md), [설계](../superpowers/specs/2026-11-09-lane-movement-followup-design.md), [계획](../superpowers/plans/2026-11-09-lane-movement-followup.md).

시작 main/원격은 `45b9a30f408672e0f5c302d19c0e8ceeb3ba84ee`. 작업 브랜치 `codex/lane-movement-roadview-followup`. 사용자 제안서V6, 한국어 mmst 및 기존 미추적 폴더는 보존한다. 기본 실행 대상 은계/A2 통합 `builds/EungyeTaxi/EungyeTaxi.exe`, `PrototypeBuild.BuildHybrid`, LANv16이다.

## 실제 원인과 구현

- 실제 Player의 M지도 주차장 안내에서 목표가52.25m 앞에 잘못 잡힘을 재현했다. 런타임 연결 보완이 원본 A2218G611368:2를 나눈 뒤 scene 서비스의 기존 laneAt74.98429를 길이22.7363인 첫 조각에 남겼다. 기존 유효 경로 검사가 이를 거부해 Q없는 좌회전 수요가 빠졌다. 런타임 복제 scene의 서비스 앵커에 기존 정류장과 같은 분할 변환을 적용한다. 작성 지도/신호 단계/금지/경로 검사를 보존한다.
- 짧은 NGII 조각을 지나 활성 출구까지 한정 탐색하여 기존 유턴·좌/우회전·직진 역할을 표시한다.3접근32조각을 명시하고 물리 차로별 상류12개 표시로 중복을 방지한다. 말단 조각도 겹친 임시 선 제거 범위에 포함한다. 신규 통행 허용·유턴 조건은 추가하지 않는다.
- 녹색 은계중앙로는 거리뷰상 편도1차로, 노란 은계로는 편도3차로다. 사용자의 잠정4/2차로 추정을 그대로 적용하지 않는다. 원본 폭을 차로 폭으로 재사용한 노면 밖 경계와 겹친 main polish 노면을 지정 구간에서 정리한다. 기존24정류장/3생활시설/6유턴 조건/주행 그래프를 유지한다. 중심선/도로 폭은 게임용 추정값이다.
- DrivingReadability의 신호 외형/출처 캡션2줄을 제거하고 램프/초 표시는 유지한다.

## 수행한 검증

로그는 Git 제외 `artifacts/lane-movement-followup/`에 있다. 위치 제어/렌더 검사는 자동 물리 주행·사람 플레이·전체 캠페인 검사와 구분한다.

| 검사 | 실제 결과 |
|---|---|
|Editor 기존 좌회전 진단/수명주기|30/192검사 통과. Editor만으로는 재현되지 않음.|
|실제 Player 수정 전 주차장 안내|exit1,34검사 중18실패. Q꺼짐 수요0, Q켜짐 수요/좌회전 통과. 남은 거리976.4246m로 화면977m와 일치.|
|서비스 앵커 RED→GREEN|40검사, 수정 전17실패→수정 후0. 실제3서비스 및 분할 경계9개·위치/방향/유효거리·재적용/작성자료 불변.|
|실제 Player 수정 후|exit0,34검사0실패. 정지선 전35/20/10/6m 접근, Q꺼짐60초/켜짐60초 각각 기존 보호 좌회전 진입. 정상 목표 남은 거리1028.665m.|
|연결 추적/중복 표시 RED→GREEN|기존1-hop 오분류 RED 후37검사 통과. 연속 말단 중복 RED 후상류1회/말단 선 제거 검사 통과.|
|차로 표시 초안|202검사 통과:32구간/12역할·별도 유턴+좌회전 글리프·기존 데이터 불변·반복 작성·표시 영역/횡단보도 회피.|
|녹색 노면 경계 RED→GREEN|수정 전 x59.55가 half6.8m 노면 밖으로 나감 RED. 수정 후 외곽240/중앙120 및 다른 구간 정책 유지.|
|녹색 지도 초안|24정류장/552방향 경로/6유턴/3시설·차량7272점·실제 선3649개 검사 통과.|
|노란 노면 경계 RED→GREEN|수정 전 x66.95가 half16.164m 노면 밖으로 나감 RED. 수정 후 바깥선240/내부 구분240, 실제 지정 구간1082개 표시 footprint 통과. 기존 편도3차로/폭/그래프 보존.|
|정식 저장 후 기존 표시 검사|기존1-hop 가정이 새 원본 유턴을 직진으로 보아 실패. 선택 접근은 한정 연결 추적 및 독립적인12차로 예상값으로 검사하도록 보완하고, 임시 차로·단절 출구·글리프49종 검사는 유지.|
|독립 코드/지도 검토|실행 없이 원본 baseline·1891차로·신호·기존37시설·23230scene ID/순서 보존 확인. 정확히32메타데이터/14도로폭/6중복노면 활성값만 변경. 확정 문제 없음; 말단 구분선 외형은 렌더로 별도 확인.|
|최종 기본 실행본|`final-build-green.log`: `PrototypeBuild.BuildHybrid` exit0. 추적37/글리프49종·313차로/88교차로·서비스40/좌회전69 및 실제 녹색3649/노란1082표시 검사 통과.|
|최종 실제 Player|`verify-final.ps1` exit0. 차로 렌더31·실제 주차장 좌회전34·전체UI169 모두0실패. 세 접근의 별도 U턴/좌회전·직진/우회전 글리프와 구분선, 두 동부 구간, 내비 외형 문구 제거를 실제 캡처로 확인.|
|LAN 재접속|기본 실행본, 같은PC loopback2프로세스, native 입력/WheelCollider 권위, 호스트54/클라이언트67 통과·재접속True. `artifacts/pvp/native-lan-20261109-151310-cdce2c35/`. 다른PC 검사는 아님.|

렌더에서 남은 노란 짧은 선을 다시 대조했다. J866의 kind501 원본 B222BC004106/B2218G610348 및 B2218G610934/610935/B222BC004046은 양방향 원본 차로 사이의 중앙선이며 유턴 개구부를 남긴다. 반대편 외곽 kind515 B222BC004267/004273, 교차로 kind525 유도선도 원본이므로 임의로 지우지 않는다. 선택 원본 구간의 겹친 임시 선과 정상 원본 표지를 구분했다.

정식 저장 및 입력 Strict exit0/issues0. 프로젝트 지도 contentHash `8fb257ed383a25782514bdcf7c203282d63ad3a65c17e5ef73cde147cc522d7b`,21655462바이트/SHA256 `4d47913df4c140e5c324c08d0868a1d0cae8aac6cdf760a08e2bead4af5567a6`. 원본 지도 ZIP은 SHA256 `50a2e4d92ccc42a39927490060d884be46505dd917c6a46bf9fa45883a3d3d51`로 불변이며 원본 재수입 입력2개는 선택 입력 부재다. OSM/교통 override/원본 corridor 설정은 불변이다.

최종 BuildHybrid·실제 화면·좌회전·LAN 검증 완료, manifest/게시 진행 중. 사용자 게임이 없는 상태를 확인한 뒤 현재 저장/백업/복구 파일18개+로컬 사용자 프로필1개를 새로 byte backup했다(`protected-current.json`,19파일). 프로필 변경은 이번 범위에 없다. 최종 실행본 테스트 후19파일 SHA256 불변을 재확인했다.

## 변경 파일·재현

핵심 런타임: `EungyeConnectivityRepair.cs`, `ReviewedLaneMovementTrace.cs`, `ReviewedNativeLanePresentation.cs`, `RoadviewCorridorPresentation.cs`, `RoadPaintGeometry.cs`, `DrivingReadability.cs`.
작성/검사: `ReviewedApproachLaneMap*.cs`, `RoadviewCorridorMapEdits.cs`, `RoadviewCorridorTests.cs`, `RuntimeServiceAnchorRegressionTests.cs`, `PersistentGreenLeftTests.cs`, `PersistentGreenLeftProbe.cs`, `BrightUILaneMovementReview.cs`, `LaneFollowupBuild.cs` 및 연결 `BrightUIProbe.cs`.

- Editor: `LaneFollowupBuild.ApplyMaps`(정식 수정), `LaneFollowupBuild.Drafts`(읽기 전용 초안 검사).
- 실제 수명주기: 별도 테스트 실행본에 `-singlePlayer -contentTest -persistentGreenLeftCheck -batchmode -nographics`.
- 렌더: `-brightUiCheck -laneMovementReview -brightUiOutput <출력 폴더>`(임시 캠페인 격리).

## 미검증·남은 항목

사람 장기 주행, 전체3일, 다른PC LAN, 실제 치수 측량은 수행하지 않았다. 전체 저장소 검사 전체를 재실행한 것이 아니라 변경에 맞춘 위 검사들을 수행했다. 이전 이력에 알려진 일반S제동 fixture의 기존 수치 불일치는 이번 조향/물리 변경 범위가 아니며 재실행하지 않았다. 별도 우회전 끝의 불완전 원본 연결을 임의로 만들어 새 통행 권한을 부여하지 않는다.
