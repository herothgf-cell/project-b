# 시작–주유소1–주요 승하차 우선 동선 정비

## 요구사항

시작 지점부터 주유소1과 주요 승하차1~5를 잇는 구간의 노면 폭·차선·회전 여유·인도/건물 침범을 정리하고 내비/H/일반 차량이 같은 자료를 사용한다. 사용자가 수정한 `docs/map-polish-plan.md`의 네이버 거리뷰 방식으로 관찰하고 같은 문서에 변경 표를 남긴다.

## 실제 구현

- `config/maps/eungye-corridor-polish.json`: 원본 도로ID135개와 선정 경로ID를 보존. Resources에 같은 설정을 배포한다.
- `CorridorPolish.cs`: OSM 적용 후 공통 MapData에 게임용 노면 보정.67개 원본 조각의 과도한 폭 축소,574개 관련 차로·회전·차로변경의 파생 주행 노면. 차로 중심·번호·연결·HD 원본 불변, 새로운 통행 권한 없음.
- `RoadPaintGeometry.cs`: 공유 차로의 황색/백색 구분 유지, 보정된 바깥 노면 여유에 경계선 정렬.
- `RoadJunctionGeometry.cs`: 파생 노면은 교차로 발견에서 제외. 차로변경 분기마다 가짜 교차로가 생겨 선이 끊기지 않게 한다.
- `SceneBuildingVisibility.cs`: 우선 구간은 노면+보행 여유의 침범 건물을 원본 보존 상태로 제외. 표시·충돌·보도 질의 공통.
- `MapCatalog.cs`, `PrototypeBuild.cs`: 동일 보정과 설정을 기본 은계 로딩/빌드에 연결.
- `CorridorPolishTests.cs`, `CorridorPolishProbe.cs`: 실제 지도 노면·공유 참조·충돌 검사,고정 카메라 캡처.
- 계획문서에 지점별 거리뷰 관찰/촬영월/파노라마ID/미확정 사항,수정사항표,67개 도로ID별 이전/이후 폭을 기록. 체크리스트·범위·개별 기획06·LATEST 갱신.

## 실제 검증

- `pwsh -File tools/test_prototype.ps1`: 모델20묶음 종료0.
- `PrototypeBuild.BuildHybrid`: 최종 `artifacts/corridor-build-final.log`, 종료0. 기존 지도·차로·시나리오·시설·OSM 검사와 새 CorridorPolishTests 통과.
- 새 에디터 검사: 노면40,008표본,승하차1~5 상호/주유소1 왕복30경로,차로 배열/HD 형상 불변,중복 적용 방지,파생 노면의 가짜 교차로 배제.
- `-corridorPolishTest -hybridV2`: `artifacts/corridor-runtime-test.txt`, 공유 MapData/edge 참조 일치,6,268충돌 표본 건물 침범0,success=True. 고정 카메라 `corridor-site-0..5.png` 확인. 정적 형상 검사이며 자동/수동 운전으로 부르지 않는다.
- `-fuelFacilitiesTest -hybridV2`: `artifacts/fuel-stations-physical-test.txt`, success=True.3곳 실제 물리로 진입→주유→별도 출구,측면/연석 접촉0. 최종 차선 끊김 수정 전 실행이며 그 수정은 도로/충돌/조향 형상을 바꾸지 않았다.
- 최종 `-districtTrafficTest -hybridV2`: `artifacts/district-traffic-test.txt`, 종료0 success=True.48대 이동45,126.1m,양쪽 회전·두 원본 영역,offpath0/overlaps0/장시간 정체0,적색 대기3대→녹색 통과.
- `pwsh -File tools/check_workspace.ps1 -Strict`: Input issues0. 필수 생성 입력3개·OSM 원본 대조 해시·지도ZIP은 변경 없음. 보정은 별도 런타임 설정이다.

## 거리뷰 결과와 한계

직접 브라우저로 수인로/북부 은계중앙로/대야동605 골목/은계유치원 인근 은계중앙로/은계로142번길을 양 방향으로 살펴봤다.2026-05 영상의 보이는 차로·보행 공간을 기록했으며 이미지·타일을 게임으로 복사하지 않았다. 승하차2에서 자동 선택된2019-03 고속도로 영상은 높이가 다른 도로이므로 현장 확정 근거에서 제외했다. 정확한 동선 전체의 교차로별 차로 수·회전 제한·신호 운영은 아직 미검증이다. 근처 영상과 게임 도로가 같다고 가정하지 않는다.

이번 결과는 1차 게임 형상 정비다. 도로폭 수치는 실측이나 거리뷰에서 잰 값이 아니다. 승하차4 주변은 기본1+1과 접근부 회전 공간이 달라 보이므로 향후 접근 방향별 차로 증감 검토가 필요하다. 실제와 맞춘 차로 재구성 전체 완료로 표시하지 않는다. 수동 영업/3일 캠페인/현장 규정 확인은 수행하지 않았다.

## 실행·재현·인계

기본 `builds/EungyeTaxi/EungyeTaxi.exe` → 영업 준비 후 출발, F7 → 주유소1 길안내. 시작/승하차1에서 주유소1·승하차2~5 연결을 확인한다. 게임 주유소 내부는 기존대로 수동 운전한다. 개별 자동 검사는 위 실행 인자를 사용하며 위치 제어·NPC 주행·택시 물리·수동 플레이를 구분한다.

빌드 명령은 `PrototypeBuild.BuildHybrid`, 로컬 Unity는 `D:/Program Files/Unity Hub/Editor/2022.3.62f2/Editor/Unity.exe`. 설정이 바뀌면 다시 빌드하고 해당 검사를 실행한다. 배포 파일별 해시는 `distribution/EungyeTaxi-build-manifest.json`에 재생성한다. 사용자 README 및 라이선스 감사 초안/증빙 변경은 포함하지 않는다.

추가 최종 검증: -manualHandlingTest -hybridV2 종료0(success=True). 저속 좌/우 회전·가감속 검사와 시작 주차 지점45m 물리 출발(18.98초,경로 오차0.0091m,측면 접촉0) 통과. H는 꺼진 상태의 자동 페달/조향 입력 시험이며 사람의 수동 플레이를 뜻하지 않는다. 최종 -corridorPolishTest 재실행 종료0,동일6,268표본 건물 침범0 확인.

배포 검증: 소스 커밋 0e388e0은 git log -1 --oneline -- 해당 이력 경로로 확인했다. 실행 폴더127개 파일의 크기·SHA256 manifest를 재생성했고 불일치0. 배포는 이 소스 기준이며 후속 배포 커밋 자체 해시는 여기에 추측해 적지 않는다.
