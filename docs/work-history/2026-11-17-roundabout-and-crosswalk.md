# 2026-11-17 회전교차로 신호 오생성·6번 횡단보도

## 요청과 원인

- J5074_877 서쪽 x481.4858/z85.9970856, 동행91.44도: 회전교차로의 빨간 신호등과 불필요한 진입 대기.
- RoadMapFacilities가 비시각 junctionControl 시설을 일반 신호등으로 처리했다. 프로그램이 없어 항상 빨간 등으로 표시되었다. 실제 교차로 제어는 이미 비신호였다.
- ReviewedRoundaboutControl은 순환도로 전체에 차 한 대만 있어도 모든 접근을 막았다. 실제 진입 지점으로 접근하는 차량의 간격으로 범위를 좁힌다. 기존 충돌 경로 예약·차체·출구 검사도 유지한다.
- 사용자 지정 6번 J5692_14220 은계호수로 접근: 횡단보도가 보도 모서리보다 교차로 안으로 나왔고 RoadMapWalker는 보도 확인 없이 도색 끝에서1m 떨어져 기다렸다.
- 유턴 허용 노면은 사용자 요청대로 모든 명시적 허용 구간에 연속 흰색 실선으로 표시한다. 통행 허용 구역을 임의 확장하지 않는다.

## 구현

- 회전교차로 제어 메타데이터는 신호등을 생성하지 않는다. 진입점 근처6m 및 상류 최소9m/3초 접근 간격으로 양보하고 다른 교차로의 규칙은 유지한다.
- 6번 작은 진입로 횡단보도는 상류4m 이동하고 실제 노면·보도 끝에 맞춰 중심/폭을 재계산했다. 정지선도 같이4m 옮겨 간격을 유지한다. 정확한 현장 측량치가 아닌 게임 보도 기하 정합이다.
- 작성 지도 r45: be92b289b8c5243d653df405a689f5e032c34c6cda073b345b0aa2e0be314d26. 원본 baseline/OSM은 동일, 작성 지도 입력 manifest 갱신.
- CrosswalkLanding으로 양쪽 대기점의 몸체 폭까지 보도 지지 여부를 확인한다. 지원 지점이 없는 작성 횡단보도에는 보행자를 도로 위에 생성하지 않는다.
- 보행자는 녹색 잔여시간 안에 완료할 때만 출발하며 다음 녹색에는 반대편에서 돌아온다. 대기 중 순간이동·한 녹색에 반복 진입을 제거한다.

## 실제 검증

- roundabout-flow-red.log: 신호등 오생성 실패 재현.
- roundabout-flow-green.log: 표시 메타데이터/근거리 양보/먼 순환 차량 통과 검사 종료0.
- solid-uturn-red.log: 연속 흰 실선 요구 실패 재현(기존은 대부분 점선).
- j6-crosswalk-red.log: 양쪽 보도 연결 실패 재현.
- j6-crosswalk-apply.log: 적용 종료0. 중심(582.54,1405.62), 폭10.58404m, 양쪽 보도 높이0.16m.
- 최종 통합 검사·실행본 화면·배포 결과는 아래에 추가한다.

## 실행·한계

Unity JunctionSixRepair.BuildVerified로 회귀와 BuildHybrid를 실행하고 tools/check_road_continuity_player.ps1로 실제 화면을 촬영한다. 자동 물리 업데이트 회귀·고정 카메라를 수동 운행이나 전체 캠페인 완주로 주장하지 않는다. 사용자 씬·개인 콘텐츠·제안서 변경은 보존한다.

추가 검증: roundabout-j6-build.log에서 실제 지도 회전 경로를 TrafficCar.FixedUpdate/Physics.Simulate로 진행, 빈 진입에서 강제정지 없이 통과(최소속도5m/s). J6_WALKER_OK:35m 횡단 녹색25초 내 완료·적색 대기·다음 녹색 반대 방향 완료. UTURN_SOLID_ZONES15. 옛 노면 검사에 남은9개 기대값을 현재 모든 명시적 유턴 경로 ID 집합과 비교하도록 갱신했다. 작성 지도 비교 결과 baseline 및 전체 차로 데이터 동일, 변경 시설은6번 정지선/횡단보도2개뿐이다.

최종 JunctionSixRepair.BuildVerified / roundabout-j6-build.log 종료0, PROTOTYPE_BUILD_OK. 저장 r14~r44의31버전 이관999검사, 기존39승하차1,482경로·31시설 검사 통과. 은행사거리 정상 경로 중앙선 교차0 유지. Strict 필수 입력 문제0, 선택 원본 재수입2개 부재는 이전과 동일. 사용자 씬/PackageManagerSettings는 작업 전 SHA256과 동일 확인.

첫 실행본 화면에서 흰 유턴 도색이 연결 차로 안쪽에만 있어 접근 화살표 옆 황색이 남는 것을 발견했다. uturn-approach-red.log로 실패를 재현했고, 각 허용 접근의 마지막 최대12m(연속 접근 경로 길이 내)를 포함하도록 도색 범위를 보정했다. J8066의 기존 별도 현장 보정은 유지한다. 이 길이는 게임 표시 근사이며 통행 권한/차로 그래프는 추가하지 않는다. 최종 빌드는 roundabout-j6-final-build.log를 기준으로 한다.

최종 유턴 접근부 보정 빌드: roundabout-j6-final-build.log 종료0/PROTOTYPE_BUILD_OK, 접근14곳 검사 및 기존 J8066 전용표시 유지, 전체 유턴15곳 원본/합성 중앙선 마스크 검사 통과. 지도 콘텐츠 지문과 차로 연결은 이 마지막 노면 수정에서 변하지 않았다.

최종 실행본 ROAD_SIGNAGE_PLAYER_OK336검사 종료0. 전체99타일·4008도로/차로 영역 촬영 범위 검사 통과. [회전교차로 신호 제거](../content-design/images/eungye-roundabout-fixed.png), [6번 제보 방향](../content-design/images/eungye-junction-six-fixed.png), [6번 상공](../content-design/images/eungye-junction-six-overview.png), [유턴 접근부 흰 실선](../content-design/images/eungye-uturn-solid-approach.png)을 확인했다. 고정 카메라 화면으로 수동 운행을 대신했다고 주장하지 않는다.

최종 배포 검증: r44 탑승 중 저장→r45 이어하기23검사, r45 새 저장9/새 프로세스 이어하기23검사 모두 종료0. 소스 커밋 c9a36d06dcb154cc87dbf9a557d8329d6905fab2. 기존 배포143파일만 해시/크기를 갱신하고 격리 실행 폴더 VerifyOnly 종료0: DISTRIBUTION_OK files=143 bytes=133354762. 기본 실행은 builds/EungyeTaxi/EungyeTaxi.exe. 이전 사용자 씬·개인 콘텐츠·제안서는 커밋하지 않았다.
