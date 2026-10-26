# 2026-10-26 교차로·승객·내비 후속 수정

## 요청과 작업 범위
유턴·중앙분리대 작업에 이어 사용자가 지정한 두 교차로의 차로/노면선, 교통섬과 보행 신호 위치를 대조한다. 만족도 실시간 상승/운임 설명, 차로 변경 내비 끊김, 전체 지도 운송 경로 복귀, 은계 승하차9 하차 문제를 수정한다.

## 구현과 근거
- 지도 좌표(504.753723,475.289825)는 J5057_4449 북쪽 진입부. 네이버 2026-05 거리뷰에서 좌회전/직진/우회전3개 차로, 북서 교통섬과 분리 우회전로를 확인했다.
- (505.3904,273.581268)는 J5076_2580 북쪽 진입부. 남하2차로 중 안쪽 좌회전, 바깥 직진 및 반대편 북행1차로를 확인했다. 좌회전 출구는 기존 은계번영길 동쪽 가지다.
- 실제 영상/지도 이미지를 게임 자산으로 복제하지 않는다. 시설 종류·차로 역할은 관찰 근거, 길이/폭/신호 주기는 게임 추정값이다. 전체 교차로 현장 전수 대조를 뜻하지 않는다.
- 보행 신호 기본 위치가 대칭 도로폭 가정에 의존해 비대칭 교차로에서 차로에 놓이는 원인이 있었다. 실제 보도/교통섬 내부와 활성 차로 여유폭을 확인하고20m 이내에서 배치하며, 적절한 곳이 없으면 도로에 대신 세우지 않는다.
- 안정 주행의 급가감속 감점 회복/즉시 S 게이지·알림/운임 M·S 기준 설명은 [별도 기록](2026-10-26-passenger-score-recovery.md)을 따른다.
- 차로 변경 중 실제 연결 선분 위에 있고 방향·출발 접점이 일치할 때 기존 내비 경로를 유지한다. 반대 방향/끊긴 길에서는 다시 탐색한다. 전체 지도에 승차·하차·경유 핀을 유지하고 `운송 경로로 돌아가기`로 승객 안내를 복구한다.
- 은계 승하차9의 기본 정차 폭을 실행용 복제에서6.6m로 맞춰 차량을 오른쪽 연석에 정렬한다. 기존 기본 승객 위치만 보도 쪽으로 보정하고, 사용자가 따로 편집한 보도 위치는 보존한다. 정식 지도/기존 저장의 목적지는 유지한다.
- 두 교차로의 관찰 차로·교통섬·횡단보도·우회전 분리 및 중앙 난간은 [지도 기록](2026-10-26-reviewed-northern-junctions.md)을 따른다. 정식 문서1804차로, 저장 해시 `c14a2a4b277e040dce001f2df7f89cc0404555a10b85ef931f2b9de14b6b05dc`.
- 작업 중 main에 추가된 운임 점진 감액·요구치별 보너스 문서 개정안을 병합해 보존했다. 해당 운임 개정은 별도 미구현 기획이며 이번 결과창은 현행 M/R 기본 운임과 S/R 보너스를 설명한다.

## 관찰 출처
- J5057 차로: https://map.naver.com/p/entry/coordinates/3zafbw,2AGEMu?c=16,0,0,0,adh&p=dcuIEvnfmTeYM4mY2DWaXA,35,-8,50,Float
- J5057 교통섬: 같은 파노라마190,-12,70.
- J5076 차로: https://map.naver.com/p/entry/coordinates/3zaffK,2AGAoR?c=17,0,0,0,adh&p=Po3cjZOgY9v4qPfVBPGTYg,0,-12,65,Float

## 초기 검증 기록
- `pwsh -NoProfile -File tools/test_prototype.ps1` 실제 종료0,23개 모델 묶음 통과(점수 회복 포함 현재 소스).
- 보행 신호 배치 `PedestrianSignalPlacementTests.Run` 종료0: J5057 8/8, 전체 후보153/154,1,618ms. 이후 추가한 섬 높이 검사/미배치 위치 로그는 최종 통합 실행에서 확인한다.
- 회복 전용 초기 모델12검사 종료0. 이후16검사와 통합 결과는 아래 최종 기록을 따른다.

## 미검증 범위와 적용 판단
사람의 수동 하루/3일 캠페인, 모든 교차로의 최신 현장 상황, 시설의 실측 치수는 검증하지 않았다. 위치를 설정하는 검사는 실제 물리 운전과 별도로 기록한다.
- 전체 맵은 데이터/렌더 감사, 거리뷰 확인은 명시한 접근부에 한정했다. 미확인 구역까지 통행 허용을 추정하면 잘못된 유턴/시설이 생길 수 있어 추가하지 않았다.
- 원래 첨부 위치는 미니맵 모양을 통해 수인로 시작 주차장 동쪽 구간으로 대응했다. 정확한 촬영 위치가 다르다면 해당 구역은 다시 대조해야 한다. 후속 두 교차로는 제공된 x/z 좌표로 확인했다.

## 추가 검토·회귀 보완
- 독립 검토에서 안정 주행50m 후22m를 되돌아갈 때도60초를 채우는 예외를 발견했다. 같은 셀 안에서 되돌아감·다른 셀에 다녀온 뒤 재진입 모두 적립을 차단하고 진행 경계/활성 셀을 저장한다. 회귀 RED→GREEN, 전용 모델16검사 종료0, 수정 범위 재검토에서 두 지적 사항 해결 확인.
- 보행섬은 프로젝트가 지원하는 `sidewalk` 종류로 만든다. 지원되지 않는 `island` 종류에 신호등만 높게 배치하는 테스트/처리를 제거했다. 실제 섬 높이 회귀는 최종 Unity에서 실행한다.
- 검정 난간/노랑 반사띠의 포스트형 중앙분리대를 별도 `post-rail` 기하로 추가했다.2m 이하 구간과 동일 충돌 외곽을 사용한다. `ReviewedRoadFacilitiesTests.Run` RED 종료1(종류 미지원), GREEN 종료0(`artifacts/post-rail-green.log`). 새 차로를 포함한 통합 맵의 간격은 최종 검사에서 재확인한다.

## 통합 편집기 검사
- `FollowupIntegrationChecks.Run`: `artifacts/followup-integration-checks2.log`, 실제 프로세스 종료0, `FOLLOWUP_INTEGRATION_CHECKS_OK`.
- 유턴 기하·조건·실제3경로의 수동 관찰자 합법/위반6경우, NPC 추종 회귀, 가져온 노면선 렌더, 지도 편집/저장/참조 검사 통과.
- 최종 지도1855실행 차로, 방향 화살표2070개. 두 교차로 역할·8경로·교통섬/차량·횡단보도 착지 검사 통과.
- 점수 회복 Unity10검사, 내비·승하차9 및 독립 편집 보도 앵커 회귀 통과.
- 보행 신호 J5057 8/8 안전 배치, 전체153/154. J1882_11791 axis0/sign1/side-1은20m 안의 안전한 보도 면을 찾지 못해 생략한다. 도로 위 대체 배치는 하지 않는다. 전체 검사1,038ms이며 매 프레임 재탐색하지 않는다.
- 실제 생성 황색선15,707개, 생성 중심 중복0. 원본 황색선578개와 비교해0.25m 이내2표본·6m 이내145표본을 기록했다.2표본은 원본515 가장자리선의 교차이며 실제 렌더에서는 횡단 겹침을 절단한다. 원본 근접 감사 수치를 현장 중앙선 오류 수로 해석하지 않는다.
- 중앙 난간은 전 선분·끝점과 실제 차로 반폭/난간 반폭+0.4m 여유를 검사한다. 최소 물리 경계 간격0.5198102m, 실제 충돌체 차량664표본 통과. 기존2.4m 고정 중심 거리 검사를 실제 폭 기준으로 대체했으며 지도 형상을 억지로 변경하지 않았다.
- 전체 소스 독립 검토에서 J5057 새 좌회전/직진 차로의 신호 관찰 누락1건을 발견했다. 별도 회귀 수정·최종 실행파일 결과는 아래에 기록한다.

## 최종 신호 관찰 수정
- J5057 북쪽의 새 좌회전·직진 차로는 기존 대칭 폭 조건의 반대쪽에 있어 실제 정지선을 넘더라도 관찰되지 않았다. 실제 실행 지도1855차로에서 적색/녹색 네 경우 모두 법규 사건0개인 RED 종료1로 재현했다.
- 가까운 정지선 범위에서 방향·차로 폭·교차로/접점 연결을 검사하는 `DirectedApproach`로 두 관찰자를 보완했다. 멀리 있는 교차로는 그래프 탐색 전에 제외한다. 기존 미확인 교차로 좌회전 법규 판정 보류 정책은 유지한다.
- `ReviewedJunctionObserverTests.Run`:45검사 GREEN 종료0, `artifacts/reviewed-observer-green.log`. 기존 `UturnMapObserverTests.Run`:3개 유턴의 합법/위반6경우 종료0(`artifacts/reviewed-observer-uturn-regression.log`). 소스 a26dc9f, 독립 수정 범위 재검토에서 기존 P2 해결·추가 P1/P2 없음.
- `PrototypeBuild.BuildHybrid`: `artifacts/followup-build.log`, 실제 종료0. 이후 첫 새 exe 주행 검사 `-uturnIntegrationTest` 종료0: 유턴3개+두 교차로 연결4개 NPC완주, 유턴 정적 충돌62표본 차단0, 자동 물리 H유턴 도착/조향 손실0. 위치 설정만 하는 검사와 달리 마지막 H검사는 실제 Rigidbody 가속·조향을 사용한다. 전체 사람 운전 검사는 아니다.
- 당시 렌더 캡처에서 J5057 교차로 안 노란 선 조각이 남은 것을 확인했다. 아래 원본/생성 노면선 수정으로 해결했다. 최종 배포 해시는 아래 결과를 따른다.

## 화면 확인 후 보완·최종 실행 검사
- J5057에 남은 V/노란 선은 원본501 `B2218G612626`,505 `B2218G610908/610911`,515 `B2218G612624`였다. 새 횡단보도 폭·깊이 및 교차로 쪽 활성 차로 내부에 들어오는 원본 황색선만 렌더에서 절단한다. 원본 지도는 보존하며 접근부 중앙선·진짜 가장자리·점선 주기는 유지한다. 실제 위치 회귀 RED 종료1 → `artifacts/j5057-marking-final.log` GREEN 종료0, 소스 fc51a70.
- 결과창의 첫 금액은 초기 제시액, 두 번째는 실수령과의 차이다. 기본 운임 감액을 `보너스 -1,500원`으로 오표시하지 않도록 `제시 운임 / 정산 증감 / 최종 운임`으로 설명하고, 실제 보너스만 변한 경우에는 `보너스`를 유지한다. 지급액/산식 변경 없음. [별도 기록](2026-10-26-receipt-settlement-labels.md), 소스5ac3aac, 전용 모델11,108검사 종료0.
- 위 두 렌더/표시 수정 및 내비 검사 시작 위치 보정은 독립 검토에서 추가 P1/P2 없음.
- 최종 `PrototypeBuild.BuildHybrid`: `artifacts/followup-build-final.log`, 실제 프로세스 종료0.
- 최종 `-navigationTripTest`: `artifacts/followup-navigation-player-final.log`,30검사·프로세스 종료0. M 미리보기 중 승차/하차/경유 핀 유지, 각 운송 경로 복귀, 목적지 불변, S09 일반·취객 기상 대기·복원·안전 정차 재진입과 중복 정산 금지 확인.
- 이전 내비 검사 첫 실패는 시작 거리 `laneAt-25`가 음수가 되어 방향이0인 검사 준비 오류였다. 실제 차로 범위와 정차점 방향으로 시작 위치를 고치고, 수락/목적지/내비 목표를 각각 진단하도록 했다(55e2bc5). 제품의 수락 조건을 완화하지 않았다.
- 최종 승객 실행 검사 `-passengerCareTest -scoreCapture`: `artifacts/followup-passenger-player-final.log`,176검사·프로세스 종료0. 실제 M회복→S 게이지·즉시 알림·체크포인트/복원, S60/R70·M100 기본 운임 전액/보너스0, 취객·기존 요청/저장 검사를 통과했다.1280/1600/1920 HUD와1920 결과창을 캡처했고, 결과창 제시 운임/정산 증감/실제 M·S 기준 표시를 확인했다.
- 최종 `pwsh -NoProfile -File tools/test_prototype.ps1`:23모델 묶음 종료0, 로그 `artifacts/prototype-model-tests-final.txt`. 이 자동 모델 결과를 사람의 수동 운전/캠페인 완료로 표기하지 않는다.

## 마지막 생성 노면선 보완
- 원본 황색선 수정 뒤 자동 생성 노랑/흰색 차로선도 새 북쪽 횡단보도까지 이어지는 것을 발견했다. 기존 중심 반경18m 검사로는 중심에서30m 떨어진 새 횡단보도를 덮지 못했다.
- `RoadPaintGeometry`에서 실제 편집 횡단보도 폭·깊이와 교차로 쪽 구간을 한 번 준비해 생성 선을 절단한다. 진입부와 분리 우회전 바깥 선은 보존한다. 실제 J5057 표본 RED: 횡단보도 노랑3/흰6, 안쪽 노랑4/흰11 → GREEN: 모두0, 진입부24 유지. `artifacts/j5057-generated-green.log` Unity 종료0, 소스 ab8e4da. 독립 수정 범위 검토에서 추가 지적 없음.
- 최종 `RoadMarkingAudit.Run`: `artifacts/j5057-generated-audit.log`, Unity 종료0. 실행 차로1855, 생성 황색선15,700, 원본578, 근접0.25m 이내2/6m 이내138, 생성 중심 중복0. 위15,707/145는 수정 전 중간 수치다. 원본 근접 수치는 현장 오류 개수가 아니다.

## 재현과 검증 자료
- Unity2022.3.62f2에서 `unity/EungyeTaxi`를 열고 `PrototypeBuild.BuildHybrid`로 기본 은계 실행파일을 만든다. 편집기 검사는 `FollowupIntegrationChecks.Run`, 추가 신호는 `ReviewedJunctionObserverTests.Run`, 노면선은 `ImportedMarkingRendererTests.Run`이다.
- 저장소 루트에서 `pwsh -NoProfile -File tools/test_prototype.ps1`, `tools/check_workspace.ps1 -Strict`, `tools/update_distribution_manifest.ps1 -VerifyOnly`를 실행한다.
- 실행파일 인자 `-uturnIntegrationTest`는 카메라/NPC/유턴 물리 검사, `-passengerCareTest -scoreCapture`는 위치 제어 점수/요청/결과 화면 검사, `-navigationTripTest`는 위치 제어 내비/승하차 검사다. 시험 상태는 실제 영업 저장과 분리된다.
- 보존 로그·캡처·검토 보고서는 최종 통합 시 기본 작업 폴더 `artifacts/2026-10-26-map-trip/`에 복사한다. artifacts는 Git 제외이며 실행파일과 manifest는 Git 배포 대상이다.

## 변경 파일 묶음
- 유턴: `UturnMovement.cs`, `RoadMapAuthoringOperations.cs`, `RoadMapValidator.cs`, `RoadNavigation.cs`, `AssistedDriveController.cs`, `TrafficSystem.cs`, 편집기/방향 표시와 유턴 회귀 파일.
- 지도/노면/시설: `config/maps/project/eungye.project.json`, `config/maps/eungye-road-facilities.json`과 Resources 사본, `ReviewedRoadFacilities.cs`, `RoadPaintGeometry.cs`, `HdRouteTrial.cs`, `PedestrianSignalDisplay.cs`, `ReviewedJunctionMap.cs` 및 관련 검사/감사 도구.
- 승객: `DrivingManners.cs`, `MannerLedger.cs`, `PassengerRequestUI.cs`, `RideReceiptSummary.cs`, 점수 UI/저장·검사 파일. 내비/정차: `RoadNavigation.cs`, `FullMapOverlay.cs`/`MapDestinationUI.cs`, `PassengerStopCatalog.cs`와 내비 회귀 파일.
- 경로 접두어: Unity 런타임은 `unity/EungyeTaxi/Assets/Scripts/`, 편집기·Unity 검사는 `unity/EungyeTaxi/Assets/Editor/`. 관련 기획·체크리스트·편집기 안내·날짜별 작업 이력 및 `builds/EungyeTaxi/`·`distribution/EungyeTaxi-build-manifest.json`을 함께 관리한다. 정확한 전체 변경은 작업 브랜치 시작12935f5 이후 Git 이력을 따른다.

## 사거리 동·서쪽 노면선 최종 대조
- 첫 최종 카메라에서 북쪽은 정리됐지만 동·서 횡단보도에 노란 굽은 선이 남았다. 같은 네이버 파노라마 `dcuIEvnfmTeYM4mY2DWaXA`의125도/−125도 방향(2026-05)을 추가 확인했다. 실제 바깥 연석 곡선은 존재하지만, 게임에 남은 내부505/515 표시는 현재 횡단보도와 차로에 겹쳤다.
- 편집 횡단보도가 있는 교차로의 나머지 접근에는 `TrafficSystem.AddApproach`와 같은 방향·폭·깊이의 기본 횡단보도 영역을 계산한다. 원본 황색선은 횡단보도 띠, 교차로 쪽 실제 차로 내부에서만 절단하고 바깥 연석/진입부 선은 유지한다. 생성 선과 원본 선이 같은 영역을 사용한다. 지도 JSON/차로 연결/법규/물리 시설은 변경하지 않았다.
- 실제 J5057 동·서 띠에 겹친 황색505/515 원본10개를 확인했다. 흰색505/515 원본은 해당 띠에서0개로 흰 안내선을 임의 삭제하지 않았다. 실제 양쪽 표본과 유지해야 할 바깥 경계/상류 표본을 회귀로 검사했다.
- `artifacts/j5057-side-yellow-red.log` 종료1 → `j5057-side-yellow-green.log` 종료0/`IMPORTED_MARKING_RENDERER_TEST_OK`. 소스 a8d2a17. `j5057-side-yellow-audit.log` 종료0:1855차로/황색15,700/원본578/근접2·138/생성 중심 중복0. 북쪽 생성 선의0/0 및 정상 진입부24표본도 유지했다.

## 최종 패키지 검증
- 마지막 소스 a8d2a17까지 포함한 `PrototypeBuild.BuildHybrid`: `artifacts/followup-build-release2.log`, 실제 프로세스 종료0. 마지막 수정 범위 독립 검토도 추가 지적 없음.
- 같은 실행파일 `-uturnIntegrationTest`: `artifacts/followup-uturn-player-release2.log`, 실제 프로세스 종료0, `uturn-integration-probe.txt` success=True. 유턴3개+새 교차로 연결4개의 키네마틱 NPC 완주, 정적 충돌62표본 차단0, Rigidbody 자동 물리 H유턴 출구 도착/조향 손실0(12.53459초 시뮬레이션)을 확인했다.
- 최신 `j5057-reviewed-junction.png`·`j5057-reviewed-island.png`에서 북쪽과 동·서 횡단보도 안 겹친 노란 선 제거, 바깥 연석 곡선/교통섬/분리 우회전 유지를 직접 확인했다. `j5076-reviewed-left-lane.png`, `suin-reviewed-median.png`, `t1-reviewed-uturns.png`도 직접 열어 확인했다.
- 마지막 두 변경은 노면 렌더에 한정된다. 같은 운송 로직의 앞선 승객176/내비30/모델23 결과를 유지하고, 변경된 렌더는 전용 회귀·감사·새 패키지 화면/주행으로 확인했다. 사람의 수동 운전/전체 캠페인/시설 실측은 미검증이다.
- 작업 폴더 `tools/check_workspace.ps1 -Strict`: 종료0/Input issues0. 정식 지도 해시 일치, 가져오기용 두 입력은 OPTIONAL 부재. 원본 입력/ZIP과 입력 manifest는 변경하지 않았다.

## 배포 파일 검증
- 실제 소스/기록 커밋 `dfca2f48c1df65d3ab43ce9876d8050be2027eac` 기준으로 BuildHybrid 실행 폴더 전체의 크기·SHA256을 갱신했다.
- `tools/update_distribution_manifest.ps1 -SourceCommit dfca2f48c1df65d3ab43ce9876d8050be2027eac -ValidatedOn 2026-10-26 -ValidationReport artifacts/map-trip-release-validation.json`: 종료0,130파일. 이어 `-VerifyOnly`: 종료0,130파일/105,536,133바이트 일치.
- manifest 검증 항목은 실제 BuildHybrid·모델·편집기·위치 제어·NPC·자동 물리·화면 결과로 구성했고, 사람 수동/캠페인/현장 전수 검증은 false로 기록했다. 기본 은계 실행폴더와 정식 지도 사본을 함께 커밋한다.

## 기본 작업 폴더 반영 완료
- 배포 커밋 `5a30bfb`를 기본 작업 폴더의 로컬 main에 `git merge --ff-only codex/eungye-uturn-medians`로 반영했다(종료0). 기준 소스는 manifest의 실제40자리 커밋이다. 원격 푸시는 수행하지 않았다.
- 기본 폴더에서 `tools/update_distribution_manifest.ps1 -VerifyOnly`: 종료0,130파일/105,536,133바이트. `tools/check_workspace.ps1 -Strict`: 종료0/Input issues0. 실행 경로 `builds/EungyeTaxi/EungyeTaxi.exe`에 새 실행파일이 있다.
- 기존 사용자 `LATEST.md`의 현황 재확인 절을 백업·정확히 복원해 미커밋 상태로 남겼고, 기존 미추적 Unity/기획 파일도 보존했다. 사용자 변경을 이번 커밋에 섞지 않았다.
- 검사 로그·최종 화면·독립 검토·당시 계획/판단 기록은 `artifacts/2026-10-26-map-trip/`에 보존했다. `primary-distribution-verify.txt`, `primary-workspace-strict.txt`는 반영 후의 실제 검증 결과다.
- 다음 작업은 사람의 수동20분/3일 플레이·Unity GUI 편집 검증 및 명시하지 않은 접근부 거리뷰 조사다. 별도 운임 점진 감액 기획을 이 작업의 현행 지급 규칙과 혼동하지 않는다.

## 사용자 요청에 따른 main 푸시
- 사용자가 main 병합과 푸시를 요청했다. 로컬 main에는 이미 구현·빌드·검증 기록이 병합돼 있었다. `git fetch origin` 종료0, 원격 대비 뒤처짐0/앞섬30이며 원격 main이 로컬 main의 조상임을 확인했다.
- `git push origin main` 종료0: 원격 main이12935f5에서 `c572fb9952ff76769e26de43133ef9683200be4a`로 갱신됐다. `git ls-remote origin refs/heads/main`의 실제 해시와 로컬 HEAD 일치를 확인했다.
- 이 푸시 기록도 별도 문서 커밋으로 main에 반영한다. 실행파일·지도·manifest는 앞서 검증한 배포 그대로이며, 기존 사용자 LATEST 수정과 미추적 파일은 보존했다.
