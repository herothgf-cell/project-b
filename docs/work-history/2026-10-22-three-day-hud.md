# 3일 캠페인·운전 HUD·시작 내비 정리 (2026-10-22)

## 요구사항과 실제 구현

| 요구 | 적용 파일/동작 |
|---|---|
| 포만감/피로 아이콘·수치 게이지 | DriverStatusGauges, OneDayHUD: 포만감/피로/연료 도형 아이콘과 숫자·색상 게이지 |
| 내비 UI 통합 | DriveHUD/DrivingReadability: 별도 우하단 카드 제거, 미니맵 하단 전방 신호 패널 아래 안내·거리·분 ETA·경로 선택 |
| 수동 시작·도움말 | DrivingCamera/AssistedDriveController: 시점 변경으로 보조 자동 활성화 제거. H만 전환. 도움말 기본 접힘·좌하단 버튼 |
| 승하차 비상등 | TaxiSignals/PassengerJobs: 승하차 성공 시 자동, 출발1m/s 초과 해제. G 수동·Q/E 회전 의도와 분리 |
| 운행 결과 | RideCompletionPanel/DriveSession: 만족도·요청 결과·확정 운임/보너스/합계. 일시정지·중복 지급 방지·생활거점 선택 |
| 3일×20분 | PrototypeState/EconomyLedger/BusinessProgress/OneDayHUD: 날짜별 정산,3일 최종 누적 수익. 현금·미납·연료·포만감·운행 점수 이월, 밤사이 피로60 회복(최소0). 새 캠페인 초기화 |
| 시작/점수 내비 제거 | RoadNavigation/FullMapOverlay/DrivingReadability: 초기 경로 비움. H 내부 자유 차로 유지는 표시하지 않음. 상황 관찰은 목적지/필수 경유로 전환하지 않음 |

9코스·17요청·만족도/운행 점수 분리·탑승 중 마감 대기는 유지한다. 보험·세금·예약·사고·수리는 추가하지 않았다. 지도 입력/ZIP은 변경하지 않았다. 기존 도로 편집기 병합 상태를 보존했다. README·라이선스·XMind와 편집기 계획/사용성 문서의 기존 미커밋 변경은 이번 커밋에 포함하지 않는다.

## 검증 명령과 실제 결과

| 종류 | 명령/증거 | 결과와 한계 |
|---|---|---|
| 빌드·생성 그래프 검사 | Unity2022.3.62f2 `-batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod PrototypeBuild.BuildHybrid`; artifacts/three-day-hud-build.log | 종료0. 기본 은계·기존 도로 정비 통합 |
| 모델 자동 검사 | `pwsh -File tools/test_prototype.ps1` | 23묶음 종료0. 3일 장부·이월·목표 없는 종료·기존 요청/운전/교통 회귀 포함 |
| 날짜/저장 상태 제어 | exe `-batchmode -nographics -threeDayTest -hybridV2` 뒤 별도 프로세스 `-threeDayReloadTest -hybridV2` | 각각 종료0,9+11 PASS. 1,200초 시작·저장 실패 롤백·피로80→20/포만감55 이월·2일차 복원·법규85 유지·3일 누적51,000수입/1,500지출/49,500순수익·중복 보관 방지 |
| 위치 제어 상호작용 | exe `-batchmode -nographics -presentationHudTest -oneDayMode -hybridV2` | 종료0,20 PASS. 승하차·비상등·팝업 정지/재개·정산 중복 방지·내비 없음/승객 목적지/H 내부 경로 비표시 |
| 시점/입력 상태 제어 | exe `-batchmode -nographics -assistedTest -viewDefaultsOnly -oneDayMode -hybridV2` | 종료0,11 PASS. 수동 시작·C가 H 선택 보존·도움말 접힘·분 ETA·빈 시작 경로 |
| 기본 지도 식별 | exe `-batchmode -nographics -mapIdentityCheck` | 종료0. eungye hybrid,2100×1750,도로1726,승하차12 |
| 로컬 입력 | `pwsh -File tools/check_workspace.ps1 -Strict` | 종료0,입력 오류0. 입력3개/ZIP 변경 없음 |
| 소스 공백 | scoped `git diff --check` | 종료0 |

최초 3일 probe는 시작 직후 고정물리1틱(0.02초)이 먼저 경과해 정확 수치 검사가 실패했다. **테스트 초기화만** 씬 로딩 전 동결로 수정한 뒤 두 단계 모두 재검사 통과했다. 생산 코드의 시간/연료/상태 허용 오차를 느슨하게 만들지 않았다.

자동 캠페인 검사는 운임/시간을 주입한 상태 제어 검사이며 실제60분 운행이 아니다. 위치 제어 검사는 실제 승하차 함수를 호출하되 택시를 이동시킨 검사다. 이번에 자동 물리 연속 주행·사람의 수동 플레이·다른 PC·HUD 실제 화면 픽셀/겹침 확인은 수행하지 않았다. 이전 하루 물리 주행 증거를3일 완료로 재분류하지 않는다.

## 실행과 다음 확인

기본 `builds/EungyeTaxi/EungyeTaxi.exe` 실행 →1일차 준비 →영업 시작. 처음 수동·목적지 없음. H 조향 보조/C 시점/G 비상등/F 승하차/M 전체지도, 좌하단 도움말. 날짜1~2 마감 정산에서 다음 날, 날짜3 누적 결과에서 새 캠페인. 새 저장은 campaign-3day20-v1.json으로 기존 하루 기록을 덮어쓰지 않는다. 진행 중 재실행은 해당 날짜의 시작 장부/상태로 복구하고 누적 운행 사건은 즉시 저장한다.

다음 확인: 1280×720/일반 해상도에서 아이콘·도움말·결과창 가독성, 승하차/마지막 하차 팝업, 일반 차량 포함3일 수익·생활·연료 밸런스와 밤사이 회복 체감. 실제 도로 조사/표지 확정은 재개하지 않았다. 실행 폴더 배포 파일 크기/SHA256과 실제 소스 커밋은 distribution/EungyeTaxi-build-manifest.json을 따른다.

## 배포 검증

실제 소스 기록 커밋은 git log로 확인한 eb37412이다. 실행 폴더127파일/84,705,775바이트 SHA256 전수 검사 종료0. manifest sourceCommit은 실제 소스 커밋 전체 해시를 사용했다. 모델/실행 검사 이후 제품 소스 변경 없음(메타파일 빈칸 정리만 포함). 메인에 소스와 배포를 순차 커밋한다.
