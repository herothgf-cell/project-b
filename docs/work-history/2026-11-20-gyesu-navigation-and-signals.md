# 계수로 북행 경로와 신호·야간 지주

은계 r50. 제보한 세 지점을 지도 데이터, 실제 경로 탐색기, 야간 가로등 메시에서 확인했다.

- J7254_14780 남쪽에서 S02-B(계수로 동쪽 반대편)로 가는 바깥 차로의 r49 경로가 약 2.4km로 교차로를 재방문했다. 나란한 북행 두 차로 사이에 차로 변경 연결이 빠져 있었다. r50에서는 접근로 중간에서 안쪽으로 한 번 변경하고 정지선 앞에서 직진한다. 이미 변경 지점을 지난 바깥 차로는 우회전 전용이다. 본선 직진과 반대편 승하차 지점의 방향은 보존했다. 노면에는 경로의 다음 120m를 그리고 미니맵에는 남은 전체 경로를 유지한다.
- J5692_14220 남동쪽 지선의 신호 지주가 (580.11, 1420.91)에 서 있었다. 카카오 항공사진으로 주도로 건너편 보도와 학교 출입구를, 네이버 거리뷰 촬영본으로 도로 옆 지주 및 차량 신호를 대조했다. 북쪽 보도 8270 안의 (578.79, 1447.21)로 옮겼다. 실측 지주 좌표가 아닌 게임 보도 기준점이다. 기존 신호 그룹과 현시는 유지했다. [네이버 거리뷰](https://map.naver.com/p?c=19,0,0,0,adh&p=MlEGsgvMqyj2M7JYnOVUGA,64,5,75,Float) · [카카오 지도](https://map.kakao.com/link/map/37.4468354,126.8018831). 대조 이미지 artifacts/map-audit-20261119/compare/011-J5692_14220.jpg 및 artifacts/color-road-survey/yellow-pano-audit/Y05-stop.jpg.
- J866_5285에서 제보한 좌표 (139.16, 523.59)와 1.7m 떨어진 (140.68, 522.88)이 유턴 보조표지로부터 생성한 가로등 후보였다. 배치 시 보도 지지 여부와 도로 가장자리 여유를 검사하고 가까운 같은 쪽 보도에 놓을 수 없는 후보는 생략한다. 조명은 차도를 비출 수 있다.

수정 대상은 정식 프로젝트 지도, 입력 manifest, RoadNavigation, DayLightCity, BusinessProgress의 r50 저장 이관과 JunctionRouteFeedbackTests, RoadPresentationProbe다. OSM 원본과 기본 도로 입력은 동일하다.

## 재현

Unity에서 ProjectMapBuild.Prepare, JunctionRouteFeedbackTests.VerifyMigration, NavigationStopRegressionTests.AuditRoutes, PrototypeBuild.BuildHybrid 순으로 실행한다. 빌드한 실행 파일에 `-singlePlayer -presentationTest -dayLightNightProbe -reportedLampViews`를 넘기면 모든 야간 가로등의 바닥 좌표와 두 제보 교차로를 검사하고 J866-review.png 및 J5692-review.png를 저장한다. 시험은 격리된 저장 경로를 사용한다.

## 검증 결과

Unity의 실제 그래프에서 S02-B까지 795.1644m, 경로 1,676건 도달 실패·비용 불일치 0건, r14~49 저장 이관 검사 1,159건 통과. Strict 입력 검사 문제 0. PrototypeBuild.BuildHybrid 종료 0. 격리 Windows 실행본에서 Direct3D 11 야간 검사 343건 통과, 가로등 지주 23개 전체의 보도 지지 및 차도 밖 여유를 확인했다. J866·J5692 탑뷰를 캡처해 확인했다. 위치 제어 및 렌더 검사이며 수동 주행은 수행하지 않았다.

이번 검증 실행본은 artifacts/j5692-nav-review/builds/EungyeTaxi/EungyeTaxi.exe에 있다. 기본 배포 폴더에는 동시에 진행된 다른 빌드 변경이 있어 이 격리 실행본을 덮어쓰지 않았다. 로그 및 화면은 같은 작업 폴더의 final.log, player.log 및 artifacts/에 있다.
