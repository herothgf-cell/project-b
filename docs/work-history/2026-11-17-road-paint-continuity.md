# 2026-11-17 도로 도색 연결 검사

사용자 제보는 은계로 미니맵 위치에서 차선이 끊기고 여러 횡단보도가 짧아 보이는 현상이다. 좌표 대신 첨부 미니맵으로 위치를 대조한다.

## 확인한 원인과 수정

`RoadPaintGeometry.PaintCompiled`는 도로별 첫 1차로의 원본 계보만 도색 기준으로 사용했다. 새 좌회전·직진 차로 ID로 분기하면 원래 계보가 끝난 이후의 구간이 누락됐다. 기존 경로를 우선 사용하고 그 경로가 덮지 못하는 부분은 다른 활성 차로 경로로 보완했다. 활성 차로 번호가 3부터 시작하는 도로를 생략하던 조건도 수정했다. 곡선 내부의 투영 오차를 끝점 이탈로 처리하던 조건은 실제 경로 시작·끝에서만 적용한다.

## 후속 곡선·횡단보도 수정

- 네이처포레 곡선: 도색 조각의 방향·길이를 기준 차로가 아닌 실제 경계선 양 끝으로 계산한다. 합류 차로 간격이 0.5m 미만일 때 안쪽 차로를 버리던 중복 제거를 정밀 좌표 중복만 제거하도록 고쳤다. 합류 간격을 차로 폭으로 사용해 바깥 경계가 안으로 당겨지던 계산도 보완했다. 도색용 접선만 완화하며, 실제 차로 경로는 유지한다. 이어진 짧은 중앙선의 날카로운 내부 접점만 두 번 완화하고 교차로·유턴 개구부를 다시 검사한다. 떨어진 도색 띠나 흰 점선은 연결하지 않는다.
- `GeneratedCrosswalkGeometry`는 렌더러와 동일한 도로 사각형·12각 종단의 합집합 및 보도 경계에서 횡단보도의 양 끝을 독립적으로 찾는다. J1927 서쪽은 30m에서 39.155m로 맞췄다. 동쪽 42.6m는 유지된다.
- 수동 배치 횡단보도는 기존 띠 안에 양방향 차로가 겹치는 경우만 표시용 복제본을 보정한다. P03 남쪽·Y05 양측을 포함한다. 보행섬·분리 우회전의 짧은 횡단보도는 유지한다. 지도 r43 원본과 차로·통행 권한은 바꾸지 않았다.
- 표시 형상을 횡단보도 메시, 보행 경로, 보행 신호 배치, 차선 도색 제외 영역에 함께 사용한다. 교차로 ID 없는 독립 횡단보도도 띠 영역은 제외하되, 임의의 교차로 연결 영역은 만들지 않는다.
- `RoadPresentationProbe`에 전체 지도 격자 촬영과 도로 구간 포함 검사를 추가했다. `tools/check_road_continuity_player.ps1`은 manifest의 실행 파일만 격리 폴더로 복사해 사용자 저장·콘텐츠를 보호하며 실행한다.

## 실제 검증

- `RoadContinuityAudit.Run`, `continuity-release-audit-final.log`: 종료 0. 첫 곡선 회귀 399건, 실제 도색 25,967점 검사 누락 0건. 접선 완화 후 `continuity-publish-verified.log`도 종료 0, 곡선 회귀 403건, 도색 조각 24,472개.
- 곡선 원호·기울어진 차로의 해석적 기대값으로 도색 방향/길이와 접합을 검증했다. 원호 중앙선 끝점 최대 차이 약 0.020008m(의도한 2cm 겹침), 점선 간격·1차로 번호 없는 입력·원본 불변 확인.
- 생성 횡단보도 테스트는 수정 전 실패(`crossing-red.log`) 후 통과. 71개 중 45개 보정, 미해결 0. 작성 횡단보도 테스트도 수정 전 실패(`authored-crossing-red.log`) 후 통과. 전체 24개 중 18개 보정; 실제 stripe 메시 끝점과 표시용 ID·방향·원본 불변 및 보행섬 보존 검증.
- 독립 횡단보도 제외 영역 누락은 통합 검사에서 발견해 수정했다. 수정 전 `continuity-release-audit.log` 실패, 최종 통합 검사 종료 0. 결과는 `artifacts/color-road-survey/crosswalk-endpoints.csv`.
- `ConfirmedCorridorsR43.BuildReviewed`, `continuity-reviewed-build.log`: 종료 0. 39곳 승하차 1,482개 경로, 29개 이전 지도 버전 저장 이관 906건, r42/r43 회전 방향 및 시설 회귀 검사 통과.
- 최종 `PrototypeBuild.BuildHybrid`, `continuity-release-build.log`: 종료 0. 지도 r43 콘텐츠 지문은 `e0f8ce711e8c1b5dcd814d0bea2c9f7f454d63baa28be4b3d8cf86df37b4d6bb` 유지.
- `pwsh -File tools/check_workspace.ps1 -Strict`: 종료 0, 필수 입력 문제 0. 선택 재수입 지도 2개는 기존처럼 없다.

## 화면·배포 확인

최종 RoadContinuityAudit.BuildVerified(continuity-shipped-verified.log) 종료 0: 곡선 403건, 짧은 V 접점/틈/끝점/원호/개구부/흰선 회귀 통과, 도색 25,967점 중 누락 0, 조각 24,472개. 생성 71개/보정45개·작성24개/보정18개 횡단보도 검사와 BuildHybrid 통과.

pwsh -File tools/check_road_continuity_player.ps1 최종 종료 0: 실행본 신호·표지 327검사, 전체 지도 99타일 촬영. 활성 차로·도로 경로 4,008개의 구간 외곽이 촬영 범위에 포함됨을 검사했다. 앞선 후보의 9개 전체 연락시트를 직접 검토했고, 최종 빌드의 네이처포레·수인로·Y05·P03 화면을 다시 열어 도색 겹침 해소와 횡단보도 끝점을 확인했다.

증거: [네이처포레](../content-design/images/eungye-continuity-nature.png), [수인로](../content-design/images/eungye-continuity-suin.png), [Y05](../content-design/images/eungye-continuity-y05.png), [P03](../content-design/images/eungye-continuity-p03.png).
전체 캡처·coverage.json·로그는 artifacts/color-road-survey/road-continuity-player/artifacts/와 상위 로그 폴더에 있다.

검증은 실제 실행본 고정 카메라와 자동 기하 검사다. 수동 물리 주행·전체 캠페인 완주·모든 현장 거리뷰 재조사를 뜻하지 않는다. 건물에 가린 부분까지 격자 포함 검사만으로 보장하지 않는다. 원본 지도 r43과 필수 입력 해시는 유지하며, 사용자 씬·패키지 설정·개인 ZIP의 작업 전 지문 일치를 확인했다.

## 변경 파일·재현

- 런타임: RoadPaintGeometry, RoadPaintStrokeContinuity, GeneratedCrosswalkGeometry, RoadJunctionGeometry, RoadMapFacilities, TrafficSystem, AmbientPedestrianPlacement, PedestrianSignalDisplay, RoadPresentationProbe.
- Editor: RoadContinuityAudit, RoadPaintCurveTests, RoadPaintStrokeContinuityTests, GeneratedCrosswalkGeometryTests, AuthoredCrosswalkGeometryTests, CrosswalkEndpointAudit.
- 실행 검사: tools/check_road_continuity_player.ps1. Unity batchmode에서 RoadContinuityAudit.BuildVerified를 실행해 검사 후 통합 빌드를 만들고, 이 PowerShell 도구로 최종 실행본을 재검사한다.
- 관련 기획 점검표·CURRENT-STATE·LATEST와 통합 실행 폴더 및 배포 manifest를 함께 배포한다. 실제 배포 검증 결과는 아래 배포 기록에 추가한다.

## 이전 진단 기록

- `RoadContinuityAudit.Run`이 전체 임시 생성 도로의 경계 도색을 5m 간격으로 검사한다. 교차로·무차선 생활도로·기존 실측 표시와 유턴 표시 구역은 해당 렌더러의 조건으로 제외한다. 실제 메시를 대상으로 한 검사는 별도다.
- 최초 검사 25,832점에서 누락 의심 1,607점. 계보 연결 보완 후 143점, 번호 조건 수정 및 유턴 표시 보정 후 10점, 곡선 판정 수정 후 25,967점에서 0점이다. `road-continuity-curves.log` 종료 0. 검사는 누락 후보가 남으면 실패하도록 구성했다.
- 무차선 생활도로 경계·원본 계보 회귀와 실측 차선 표시 회귀는 통과했다. 로그는 `artifacts/color-road-survey/road-continuity-complete.log`이며 종료 0이다.
- 횡단보도와 활성 차로의 교차 폭을 대조한 후보는 `artifacts/color-road-survey/crossing-width-audit.json`에 있다. 보행섬 옆 횡단보도가 포함되어 있어 일괄 확장은 하지 않았다. 실제 화면과 양 끝 연결 검증이 남았다.
- `PrototypeBuild.BuildHybrid` 종료 0(`road-continuity-final-build.log`). 지도 r43의 경로·신호 데이터는 변경하지 않았다. DX11 실제 Player 신호·표지 325검사 통과(`road-continuity-final-player.log`).
- 고정 카메라 캡처는 `artifacts/color-road-survey/road-continuity-player/artifacts/road-continuity-*.png` 10장이다. 은계로 `(590,1240)`와 `(700,1100)` 차선 연속을 확인했다. `(1035,642)` 곡선에는 분기 도색 겹침이 여전히 보인다. `(193,518)` 수인로 교차로는 도로 면과 횡단보도 종점의 비대칭을 추가 확인해야 한다. 이 두 시각적 문제는 자동 누락 0건으로 해결됐다고 판정할 수 없다.
- 현재 로컬 실행본은 검증 중 후보다. 최종 배포 manifest 갱신·커밋·푸시는 미완료이며 원격의 정식 실행본은 기존 r43이다. 다음은 위 10장 전체 검토, 횡단보도 종점과 곡선 중복선의 실제 메시 검증 및 수정이다.

## 배포 기록

소스와 이력의 실제 커밋: 833a67b (git log -1 --oneline -- 해당 이력으로 확인). 이 소스로 생성한 통합 실행본의 기존 배포 목록 143파일만 크기·SHA256을 갱신했다. 격리 실행 폴더에서 update_distribution_manifest.ps1 -VerifyOnly 종료 0: DISTRIBUTION_OK files=143 bytes=133348746. 사용자 Taxi.zip·content는 배포 목록에 추가하지 않았다. 사용자 요청에 따라 main에 소스·실행본을 함께 게시한다. 푸시 결과와 원격 HEAD는 명령 결과로 확인한다.
