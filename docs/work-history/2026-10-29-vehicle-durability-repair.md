# 2026-10-29 충돌 내구도·수리

## 요구·범위

사용자가 확인한 기획을 구현하고 실제 검사 후 기본 실행파일 갱신·main 푸시까지 요청했다. 충돌 세기에 따른 손상, 수리비·시간 중심, D0 운행 불가. 기존 승객·청결·운임·PVP 작업과 사용자 untracked8항목을 보존한다.

## 구현 계약

- 새 캠페인 D100, 접촉면 수직 접근 속도 구간별0/2/6/15/30/50 차감. 접선/지지면 제외, 접촉0.8초 단절 전 같은 사건, 높은 구간의 차액만 적용.
- 수리200원/점·10+0.5초/점, 견인3,000원/30초. 현금 우선·부족금 미납, 단일 서비스 정산, 엔진 꺼짐·연료 보존. 청결/구토/졸음과 독립.
- D0 구동·호출/승차 차단, 안전 정차 후 운송 취소0원1회. 첫 도착 운임 확정은 보존한다. 지원 주유소 내부 전용 정비 공간을 표시하고 물리 점유 검사 후 견인한다.
- Ruling: 기존 식사/세차와 같은 원자 서비스 확인/저장 방식을 사용한다. 진행 프레임이 없는 서비스의 임의 중간 단계를 추가하지 않고 전체 체크포인트로 결제·시간·상태·위치를 함께 확정한다.
- Ruling: 기존 작업 공간에서 전용 브랜치를 사용해 지도·Unity 캐시를 재사용한다. 기존 미추적 파일은 추가/삭제하지 않는다.

## RED·검토·진행 기록

- 모델 RED: VehicleCare에 영속 손상 상태가 없다는 계약 검사 실패. 구현 후190검사 GREEN. 소수 유예 만료, 불가능한 사건 기차감량, account 없는 복원에서 care 누락도 회귀로 보완했다.
- 실제 JsonUtility용 damageVersion 존재 표시: legacy 중첩 객체 합성이 D0을 만들지 않게 구형은100으로 처리. 손상 상태·기차감 구간·유예·견인 공간을 저장한다.
- 기존 순수 검사: care13,301·승객430·prototype28클래스 모두 종료0(모델 구현자가 최종 수정 후 실행). 독립 모델 검토에서 중대한 문제 없음.
- Editor RED: VehicleDamageController 없음으로 종료1. 최초 실제 Build 검사9개 + 기존 승객1,526/care JSON14/live22/P11/PVP 분리와 BuildHybrid 종료0.
- 최초 Player RED: 정면/측면/표본화된 정차 추돌은 D85, 아직 표본화되지 않은 새 동적 물체의 후방 추돌만 D100으로 실패. 실제 PhysX 상대속도 부호를 확인해 fallback 방향을 수정하고 같은 물리 회귀를 유지했다.
- 독립 실제 연결 검토가 날짜 이월·졸음 귀가에서 D0 견인 구역 이탈/재청구와 도착 확정 짐 하역 중 견인 불가를 발견했다. 별도 실행 회귀로 추가했다. 화면 검사에서 손상 컴포넌트를 꺼둔 fixture도 검토 지적대로 보완 대상에 포함한다.
- 검사 실행 대기가 아직 완료되기 전에 검사 프로세스를 시작한1회는 해당 test 인자/로그 경로 프로세스만 종료하고 결과로 사용하지 않았다. 빌드 종료0 확인 후 새로 실행했고, 러너에 Unity 실행 중 차단을 추가했다.

## 검증·배포 상태

진행 중. 최종 명령·종료 결과·화면·배포 해시·커밋·남은 검증은 완료 후 아래에 기록한다. 기본 빌드는 PrototypeBuild.BuildHybrid, 실행은 builds/EungyeTaxi/EungyeTaxi.exe다. 새 검사는 artifacts/vehicle-durability-test.json 전용으로 사용자 저장을 사용하지 않는다.

## 경계 보완·최종 검사

- 실제 JsonUtility 추가 RED: damageVersion1에서 손상 payload 자체가 누락돼도 개체 합성/초기값으로 유효하게 읽히는 검증 문제. 내구도0이 발생했다고 확정한 앞선 설명은 바로잡는다. 내부 payload 버전은 기본0·Snapshot만1을 기록해 누락을 거부, 구형 외부버전0은 D100을 유지했다. 모델191·Editor10 GREEN.
- 전용 공간 최초 위치는 캐노피 기둥과 겹쳐 실제 무점유 견인이 실패했다. 기존 forecourt 내부에서 방향13m·오른쪽4m 위치와 차체 사각형/승객 지점 이격·물리 점유를 검사하도록 보완했다. 지도 입력/좌표 자료는 바꾸지 않았다.
- 날짜 이월/졸음 마감의 기존 주차장 이동을 막아 예약된 D0 차량 위치를 유지했다. 도착 확정 짐은 교차로에서 하역을 강요하지 않고 운임/하역 상태를 유지해 견인 후 안전 구역에서 마친다.
- 새 프로세스 정산 복원 RED를 실제 재현(artifacts/vehicle-durability-summary-reload-red.txt). 활성/정산/준비/점유 대기 모든 저장에서 유료 예약을 수리 완료까지 유지하고 검증된 빈 공간에 무결제/무시간 복원한다. 복원 중에는 체크포인트를 재진입해 SaveFailed를 만들지 않는다. 점유면 무결제 재시도, 이동 중이면 정차/5초 안전 복구 규칙을 지킨다.
- D0 정비 보류 마감도 정상/긴급 수면·기존 회계 흐름을 사용한다. 내구도·청결·연료를 수면으로 회복하지 않는다.
- 캡처 fixture의 손상 컴포넌트를 켜고 페이드가 끝난 프레임을 기다렸다. 검은 페이드 프레임을 정상 화면으로 통과시키지 않았다. HUD 권장 바와 내구도 게이지 겹침, D0에서 잘못된 ‘계속 영업 가능’ 표시도 보완했다.

| 최종 검사 | 실제 결과·증거 |
|---|---|
| 모델 | test_vehicle_damage.ps1 191 / test_passenger_care.ps1 13,301 / test_passenger_behavior.ps1 430 모두 종료0 |
| 기존 모델 전체 | test_prototype.ps1 28클래스 종료0, artifacts/vehicle-durability-prototype-tests.txt |
| Unity Editor+빌드 | VehicleDurabilityIntegrationTests.Build 종료0. 차량10, 기존 승객1,526/care JSON14/live22/P11/PVP 분리 및 PrototypeBuild.BuildHybrid. artifacts/vehicle-durability-release-build.log |
| 실제 local PhysX | 속도 강도별·정면/측면·동적/kinematic 후방 추돌·미표본 동적 물체·지면/접선/분리·연속 밀기·다중 collider 강도 증가 등15시나리오. 실제 도로 전체 자동 주행과 구분 |
| 실제 객체 생명주기/화면 | test_vehicle_durability_player.ps1 -Capture -Visible 첫 프로세스70검사/57.1초 종료0. D0 제동/구동/승차/취소, 운임 보존, 점유/미납, 실제 F7 취소/확정, 날짜/졸음/하역 포함 |
| 새 프로세스 재개 | 활성8/18.6초, 정산2/17.5초, 준비7/18.5초, 점유 대기 live2/20초 모두 종료0. 각 vehicle-durability-*-reload.txt 보고서 |
| 기존 실제 Player | care201 종료0(artifacts/vehicle-durability-legacy-care.log), behavior164/새 프로세스8 종료0. headless164는 기존 capture 포함174와 검사 범위가 다름 |
| 화면 | 1280×720 D0/견인 후 수리/정산3장 직접 확인. 견적·시간·미납·수리/견인 비용·내구도·버튼이 읽히고 겹치지 않음. artifacts/vehicle-durability-*.png |
| 독립 검토 | 모델·실제 연결·최종 전역 검토 및 예약 복원 재검토. 지적한 유료 예약 손실/하역 경계를 수정했고 최종 중대한 추가 지적 없음 |

## 변경 파일·실행

- VehicleCare.cs(순수 손상/버전), CampaignAccount.cs/EconomyLedger.cs(비용/시간/정산), VehicleDamageController.cs(물리 관측/견인 구역/정비/HUD/예약 복원).
- TaxiController.cs(구동/제동/기술 복구), PassengerJobs.cs(고장 취소·확정 운임/하역), BusinessProgress.cs(격리 검사/저장/복귀/마감), ServiceStops.cs(F7 수리·안내), DriveSession.cs/OneDayHUD.cs(연결/정산), 관련 .meta/FirstDrive.unity.
- VehicleDurabilityIntegrationTests.cs/VehicleDurabilityProbe.cs, tests/VehicleDamageTests.cs, tools/test_vehicle_damage.ps1/test_vehicle_durability_player.ps1. 컨텐츠00/03/05/06/README·scope/checklist·계획/이력/LATEST, 실행폴더/manifest.
- 재현: pwsh -File tools/test_vehicle_damage.ps1; Unity -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod VehicleDurabilityIntegrationTests.Build -logFile <로그>; pwsh -File tools/test_vehicle_durability_player.ps1. 실제 화면 검사에는 승인된 -Capture -Visible 옵션을 추가한다. 첫 프로세스만 창을 표시하며 후속 재개는 headless다.
- 이 PC Unity2022.3.62f2. 실행 파일은 기본 은계 builds/EungyeTaxi/EungyeTaxi.exe. 사용자 저장 대신 artifacts/vehicle-durability-test.json 및 기존 회귀 전용 저장만 사용했다.
- 미수행: 사람20분/3일 운전·전체 도로/경계석·다른 PC/패드·장시간 체감/비용 밸런스. 로컬 PhysX fixture를 전체 차량/도로 실주행 완료로 표시하지 않는다.
