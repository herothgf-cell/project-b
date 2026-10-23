# 은계 도로 연결·TIP·취객/청결·중단 운송 이어하기

상태: **기능 구현·자동 검증·배포 빌드 완료. 소스 e7e0e07 보존, 실행폴더/manifest 해시 검증 완료.** 별도 은계-only 폴더 이동은 이 배포 완료 후 담당 채팅에서 통합한다. 이번 작업은 기존 경로에서 더 이상 기능을 추가하지 않는다.

## 요구와 구현

| 요구 | 실제 변경 | 경계 |
|---|---|---|
| J1927_5181 연결 | 점선 근거 차로변경18·3차로 직진1·전진 남하 합류1, 공통 HdRoadGraph | 실제 서쪽 접근 좌회전 미확정·추가하지 않음. 전부 보완 상태/근거 보존 |
| TIP | 좌측 호출 카드 아래·속도계 위, 제목/숨김/펼침 | 사용자 화살표 요청 배치 |
| 호출 무반응 | 완료 transient 정리 후 다음호출, 거절 사유 표시, 수락 당시 픽업 거리 재계산 | 실제 GUI 클릭1회 ToPickup/지도닫힘/시간재개 확인 |
| P9 | 취객20%/하루1명·구토25%·기상30% 고정시드,15/30초 안전하차, 청결/거부/세차/장부/HUD | 수동20분/3일 밸런스 미검증 |
| P8 선행 | S0/주행·요청·안전 조기 도착/보너스, 일반 정차 포켓12 | 안정 주행 M회복은 미구현 유지 |
| 저장 승인 | v3 중단 위치·운송·요청·대기·상황 선택·시간·회계 복원, v2호환 | 복원 실패 시 원본 덮기 차단. 지도 리비전 충돌은 새 캠페인 선택 전 출발 차단 |
| 지도 입력 | 정식 프로젝트/필수 입력3개/ZIP 그대로, runtime 보완 | 별도 은계-only 경로 이전 작업은 이번 배포 이후 담당 채팅에서 통합 |

## 지금까지 확인된 명령과 결과

- `pwsh -File tools/test_prototype.ps1`:23개 모델 묶음 종료0.
- `pwsh -File tools/test_passenger_care.ps1`:10,089 검사 종료0.
- `PrototypeCareChecks.Build`: 최종 종료0. 실제 JsonUtility14/활성 회계22/정차850/도로2412 및 지도 가독성 검사. 로그 `artifacts/care-build-final.log`.
- 후속 화면 시험 위치 동기화만 수정 후 `PrototypeBuild.BuildHybrid`: 종료0. `artifacts/care-distribution-build.log`의 Build Finished Success / PROTOTYPE_BUILD_OK. 기본 은계 실행파일이다.
- 위치 제어 `-passengerCareTest`:97검사 종료0. 네 가지 취객·승차 거부·3주유소 세차와 빈차/첫픽업, PS08 선택 우회, PS09 보행자 타이머/법규 이벤트를 실제 Jobs/JsonUtility로 복원. `artifacts/passenger-care-integration.txt`.
- 실제2프로세스 `-liveSaveTest`9 / `-liveReloadTest`23검사 모두 종료0. 첫 재검사에서 시간/연료 미세 증가를 발견해 물리 갱신의 paused/preparing/restoring/timeScale 조건을 보완. 재실행 후 같은 시간·연료·미납·운송·첫 도착·16초 기상 대기·법규 ID·지급액과 다음 날 디스크 이력을 확인. 로그 `artifacts/live-campaign-save.txt`, `live-campaign-reload.txt`.
- `-liveInvalidTest`:4검사 종료0. 별도 고의 손상 저장에서 출발 차단·원본/백업 무변경·명시적 초기화·새 v3 시작 경계. 독립 운행 저널도 복원 차단 상태에서 덮지 않는다. `artifacts/live-load-guard.txt`.
- `-mapInteractionTest -uiCapture`:60검사 종료0. 완료 직후 재수락/중복 지급 없음, 사유 표시, TIP 숨김/펼침/기하 배치. 숨김 창 첫 캡처가 검정으로 나온 것을 확인해 보이는 게임 창에서 다시 실행, 1280/1600/1920 HUD·TIP/청결·도움말·지도·팝업 실제 이미지 검사. `artifacts/map-visible-player.log`, `situation-f7-{width}.png`. 작은 화면 긴 TIP은 내부 스크롤이다.
- `-mapClickTest -mapClickAfterSettlement`: 실제 OS마우스1회, artifacts/map-click-test.json phase=passed, trip=ToPickup, mapOpen=false, paused=false, timeScale=1. 자동GUI를 사람 수동 운전으로 기록하지 않는다.
- `pwsh -File tools/check_workspace.ps1 -Strict`: 종료0. 정식 지도 contentHash19d3037e76db5a176eba20568624f32e1d25e2c47c51a4bbbd26d32b8191da1b/필수 입력3개 검증.

## 현장·지도 대조

원본 NGII A2/B2/B3의 해당 구간을 읽고 단절/점선ID를 대조. B2 type212 B2218G610332/2673/2674, B3 B3218G610468 kind5371. 원본 갱신2020/2022와 배포연도2025를 구분한다. 네이버 수인로·119안전센터 삼거리 주변2026-05/2025-10 거리뷰를 열었지만 서쪽 접근의 현재 좌회전 허용을 확정할 수 없어 좌회전 지름길을 생성하지 않았다. [네이버 대조 위치](https://map.naver.com/p/entry/coordinates/3za5Zn,2AGGia). 원본은 artifacts/junction-raw-evidence.json(로컬)에 요약. 그래프 경로는2870.96→2620.24m,700m 지름길 달성으로 주장하지 않는다.

## 남은 검증/다음 작업

신규 구간 자동 물리 주행/사람 수동20분·3일/취객 빈도·세차 경제 밸런스/현장 좌회전 허용은 미검증. 안정 주행 M회복(P8-07)은 미구현. 체크리스트 P8-06/07·P9-07을 남겼다. 이 항목을 완료로 과장하지 않는다. 관련 없는 README·기획06/09·라이선스·XMind 문서 변경은 분리 보존.

## 변경 파일과 재현

- 도로: `MapCatalog.cs`, `EungyeConnectivityRepair.cs`/Editor tests. `MapData` 합성 후 보완하므로 내비/조향 보조/일반 차량이 같은 방향 그래프를 사용한다. 원본/명시적 편집 next를 보존하며 구도 자료와 현재 법적 허용을 구분한다.
- 정차: `PassengerStopCatalog.cs`, `ProjectMapScene.cs`의 인덱스 무효화, `WorldBuilder.cs`, config/Resource 후보 JSON, Python 생성기/Editor 검사.
- 운송: `PassengerJobs.cs`, `VehicleCare.cs`, `FareTrip.cs`, `PassengerRequest*`, `MannerLedger.cs`, `DrivingManners.cs`, `PrototypeState.cs`.
- 저장/회계: `BusinessProgress.cs`, `EconomyLedger.cs`, `CampaignAccount.cs`, `DrivingScoreJournal.cs`; `ServiceLawZones.cs`, `ScenarioChoiceSituation.cs`, `ConfusionPractice*`, `RightPracticeState.cs`, `RoadNavigation.cs` 관찰/선택 DTO.
- UI: `CockpitUI.cs`, `DriverStatusGauges.cs`, `FullMapOverlay.cs`, `MapDestinationUI.cs`, `OneDayHUD.cs`, `DrivingLaw.cs`, `RideCompletionPanel.cs`, `ServiceStops.cs`, `DriveHUD.cs`.
- 검사: `PrototypeCareChecks.Build`, `PassengerCarePersistenceTests.Run`, `LiveCampaignTests.Run`, `PassengerStopCatalogTests.Run`, `EungyeConnectivityRepairTests.Run`; 플레이어 `-passengerCareTest`, `-liveSaveTest` 후 새 프로세스 `-liveReloadTest`, `-mapInteractionTest -uiCapture`, `-mapClickTest -mapClickAfterSettlement`. 모두 저장/로그는 artifacts 테스트 경로. 일반 사용자 저장을 시험 입력으로 사용하지 않았다.
- 사용자 확인: 실행 → M → 호출 선택 → 수락. 운송 완료 후 결과를 닫고 M에서 다음 호출 즉시 수락. 주유소 지정 칸에서 빈차 F7 → 세차. 취객 확률을 기다리지 않고 확인하려면 위 고정 사례 사용.

## 보존 및 배포

README/라이선스/XMind와 문서06/09의 기존4줄 변경은 이 커밋에 섞지 않는다. 문서06/09와 LATEST에는 이번 작업 블록만 별도로 staging한다. 정식 프로젝트와 원본 입력/배포 지도ZIP은 변경하지 않았다. 소스 커밋 뒤 실제40자리 해시로 manifest를 생성하고 실행폴더 전체 해시 검증 후 별도 배포 커밋을 만든다.

## 배포 패키지 확인

소스 `e7e0e071544da7a3932d9d8cdba4104c81c335d1`은 `git log -1 --oneline -- docs/work-history/2026-10-23-passenger-care-connectivity.md`로 확인했다. 이 소스와 최종 `PrototypeBuild.BuildHybrid` 실행폴더를 manifest에 연결했다. `tools/update_distribution_manifest.ps1 -VerifyOnly` 종료0: **128파일/118,737,930바이트** 전체 크기·SHA256 일치. 실행 `builds/EungyeTaxi/EungyeTaxi.exe`. 다른 PC는 main pull 후 이 실행파일을 열면 된다. 정식 지도/필수3입력/지도ZIP은 그대로다.
