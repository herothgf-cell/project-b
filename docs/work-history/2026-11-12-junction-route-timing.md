# 2026-11-12 좌회전 경로와 운송 시간 조정

## 요구와 실제 구현

- 제보 미니맵의 남행 J7973 부근 → `국도42 승하차 1` 우회와 J7917/J7973 좌회전 준비 연결을 조사했다. 사용자 제공 정확 좌표는 없으므로 `(791,423)` 남행과 `(797,470)` 남행 두 위치로 재현했다.
- r17에 추가한 J7917~J7973 중간 차로에는 인접 차로 변경이 없었다. 정식 지도 r18에 인접 차로 변경 8개를 추가했다. 가까운 좌회전까지 1,658.879m → 100.9074m, 반대 방향 109.4663m.
- J7973 북행 좌회전 이후 서향 1차로에서 목적지 4차로까지 이동할 때 기존 동시 시작 차로 변경과 32m 보완만으로는 마지막 변경이 승하차 지점을 지나서 끝났다. `EungyeConnectivityRepair`에 원본 점선 내 순차 바깥쪽 18m 변경 3개를 추가했다. 기준 거리 22/44/66m, 기존 점선 범위·방향·차로 간격·차량 점유 검사 유지. 새 좌회전 허가를 만들지 않았다.
- 실제 런타임 보완 그래프에서 제보 방향 경로 1,431.797m → 1,063.201m, 두 번째 위치 1,109.963m. 두 경로 모두 J7973 북행 좌회전 `A2218G611104:0`을 사용하고 원래 목적지에 도달한다. 남쪽 회차 구간 자체는 도로 진행 방향에 따른 경로이며 제거하지 않는다.
- 새 승차/하차 제한 시간은 기존 최종 계산값의 80%. 승차 최소150→120초, 하차 예시 급함185→148/보통210→168/여유240→192초. 운송 시간 계수는 double로 계산해 Unity float 올림 오차로 1초가 늘어나는 문제도 제거했다. 기존 저장에 확정된 운송 시간은 유지한다.
- r14~r17 정식 지도 저장은 확인된 r18 해시에서만 이관한다. 그래프 변경으로 낡은 내비 edge 정수는 복원하지 않고 현재 시나리오 연결을 다시 계산한다. 빠른/짧은 길 선호는 복원 경로 계산 전에 적용한다.

## 변경 파일

- `config/maps/project/eungye.project.json`, `config/maps/local-input-manifest.json`
- `Assets/Editor/PlaytestLaneChangeRepair.cs`, `JunctionRouteFeedbackTests.cs`, `PlaytestBalanceTests.cs` (Unity 프로젝트 기준)
- `Assets/Scripts/EungyeConnectivityRepair.cs`, `BusinessProgress.cs`, `OneDayOfferPolicy.cs`, `CampaignSmokeTest.cs`
- 모델 테스트, 콘텐츠/범위/체크리스트, 이력, 통합 배포 실행본과 manifest.

## 실제 검증

- `JunctionRouteFeedbackTests.Verify` 수정 전 exit1: 가까운 좌회전까지1,658.879m. 수정 후 두 방향100.9074/109.4663m.
- `JunctionRouteFeedbackTests.Run` 최종 exit0: 실제 목적지 두 경로·J7917 552경로·J7973 552경로·차로 방향 표시·PersistentGreenLeft·PlaytestBalance 통과. `artifacts/junction-route-feedback/final-regression.log`.
- `OneDayCampaignTests` 독립 재컴파일/실행 exit0, `tools/test_content_tuning.ps1` exit0(729+840 검사 및 해시).
- `tools/test_prototype.ps1` 전체 실행은 기존 `ManualHandlingTests` 일반 S 제동 검사 actual8.823529 실패에서 중단. 해당 운전 물리 코드 변경 없음. 전체 모델 검사 통과로 표시하지 않는다.
- Strict 필수 입력 문제0. 선택 재수입 지도2개는 정식 프로젝트 지도 사용으로 불필요.
- 저장 이관/BuildHybrid/배포 검사는 아래 출하 기록에서 갱신한다.

## 보존과 한계

- 기존 수정 `FirstDrive.unity`, 제안서 및 프로젝트 설정은 작업 커밋에서 제외한다.
- 사용자 미추적 `builds/EungyeTaxi/TAXI.zip`은 오래된 압축본이 새 실행 폴더 배포에 섞이지 않도록 `artifacts/junction-route-feedback/TAXI-original.zip`에 그대로 보존했다. SHA256 `39e0e0b388daa211aa16d433233ea14addc4917a9f3b06bf0410e7055e48f4d8`.
- 경로 검사는 그래프/위치 기반 자동 검사다. 사람 주행, 전체3일, 다른 PC LAN은 수행하지 않았다. 모든 사거리의 새 현장 실측을 주장하지 않는다.

## 실행

- 기본 `builds/EungyeTaxi/EungyeTaxi.exe`, 은계 싱글.
- Unity 실행 메서드 `JunctionRouteFeedbackTests.Run`, `JunctionRouteFeedbackTests.VerifyMigration`, 빌드 `PrototypeBuild.BuildHybrid`.
- 로컬 Unity: `C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe`.

## 저장 이관 검사

JunctionRouteFeedbackTests.VerifyMigration exit0, r14/r15/r16/r17 네 버전 모두 r18 이관과 낡은 내비 참조 제거 확인. artifacts/junction-route-feedback/migration.log.

## 출하 검증

- 소스 커밋 `ec7aad92cb1447927fb0ae2aed3104217a0ab1a4` (`git log -1 --oneline -- docs/work-history/2026-11-12-junction-route-timing.md`로 확인).
- `PrototypeBuild.BuildHybrid` 실제 exit0. `artifacts/junction-route-feedback/build.log`.
- 수정 전 배포 r17 `-singlePlayer -liveSaveTest` exit0 → 새 r18 `-singlePlayer -liveReloadTest` exit0, 23검사. 운송·구토/수면 대기·장부·다음날 거래 보존. 사용자 저장 대신 테스트 전용 파일 사용.
- 통합/PvP VerifyOnly exit0, 143파일. manifest는 실제 소스 커밋·현재 파일 SHA256·이번 검사 보고서로 갱신했다.
- 작업 중 다른 작업이 추가한 `TrafficPerformanceProbe.cs` 변경은 컴파일 산출물 시각 이후에 생겼으며 소스/배포 커밋에서 제외했다. 원본 그대로 보존한다.
- 전체 자동 모델 검사의 기존 제동 실패 및 사람 주행 미검증은 위 한계를 유지한다.
