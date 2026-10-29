# 2026-10-29 승객 유형·대화 변주·준비 작업 구현

## 요구사항·구현 범위

- 사용자 지시: 다른 승객도 잠자는 승객처럼 대화/행동 변주를 다양하게 정리한 뒤 구현.
- 범위 확인 응답: 준비 작업까지 포함. 음식/음료를 먹거나 흘려 청결이 떨어지는 등 승객 행동이 차량 상태에 영향을 주도록 구현.
- 전체11유형과 일반 승객. 정밀 승하차는 기존 SafeStop 검증 경로만 사용, 신규 지도/P13 대체 지점 생성 없음.
- 내구도/수리는1차 확인 완료 상태 유지, 이번 승객 구현과 별도.
- 실행 계획: ../superpowers/plans/2026-10-29-passenger-type-variants.md.

## 결정·작업 상태

- 승객 유형/변주/응답/준비·하역/수면/오염 사건을 직렬화 가능한 모델로 분리하고 현재 운송/요청/장부에 연결한다.
- 음식 허용 후 실제 섭취 부스러기2, 고정 시드25% 흘림 추가6을 시험값으로 채택. 밀봉 보관0 또는 안전 정차 정리10초0, 미응답은 섭취 허용 아님. 위험은 선택 전에 안내.
- 적재/하역 각10초, 오염 짐 보호10초 또는 실제 하차 추가 청결5. 실제 준비/하역은 안전 정차에서만 진행, 같은 작업/사건 재시작 중복 적용 금지.
- 전용 브랜치 codex/passenger-type-variants, 기존 Unity 캐시/로컬 입력을 활용하도록 기존 체크아웃에서 작업. 사용자 미추적 파일은 포함하지 않는다.
- 소스/기능 검증 완료. 최종 화면·배포 결과는 아래에 따로 기록한다.

## 검증 기록

- 변경 전 tools/check_workspace.ps1 -Strict: 종료0, 필수 입력 오류0, 선택 import2개 부재 허용. 지도 contentHash76ebb91c8e5ae202f81c15a5df399375db2273157406eccc4b07ef258ebfe758 유지.
- 변경 전 tools/test_passenger_care.ps1: 종료0,13,301단언 통과.

## 진행 중 검증·리뷰

- 새 모델 RED는 누락된 API 검사 실패를 먼저 확인, 구현 후 tools/test_passenger_behavior.ps1 392단언 종료0(독립 리뷰도 재실행). 기존 care13,301 및 전체 prototype 모델 suite 종료0.
- Unity 연결 RED: PassengerBehaviorIntegrationTests.Run이 jobs.Behavior 부재로 종료1. GREEN은1,510검사 종료0.
- PassengerBehaviorReleaseTests.Run 통합: 신규1,510·기존 JsonUtility care14·live22·P11 종료0. 로그 artifacts/passenger-behavior-release-editor.log.
- 모델 독립 리뷰: 중요 결함 없음, 음식 정리가 도착 뒤 짐 하역으로 표시되는 경미한 문구 수정.
- 통합 리뷰: 요청/유형의 같은 프레임 중복 키 입력, 준비 완료 순간 영업 수락 조건 재검사 누락을 발견하여 수정 중. 자기 검토에서는 픽업 중 큰길 후보 판단의 목표 경로 오류를 발견. 캡처 검사 예외 보고도 보완한다.
- 위 통과 기록만으로 최종 실행 파일 완료를 표시하지 않으며 수정 후 재검사/빌드/Player 검증을 이어간다.

## 첫 실행 파일 검증

- 입력/승차 경계 수정 후 Release Editor1,514·care14·live22·P11 종료0. 독립 범위 재검토에서 중요한 두 문제 해소 확인.
- 최초 PrototypeBuild.BuildHybrid 종료0, artifacts/passenger-behavior-build.log.
- tools/test_passenger_behavior_player.ps1의 첫 Player154검사 종료0. 위치 제어·명시 상태 진행 검사이며 실제 물리 주행 아님.
- 새 프로세스 복원은2번째 검사에서 종료1. artifacts/passenger-behavior-test.json에 Food 소비/흘림·운송 상태가 저장됐으나 최초 로그에 복원 시도 실패 경고가 없어 로드 경로를 조사했다. 초기 등록 누락 가설은 실제 코드에 인자가 있음을 확인해 기각했다. 복원 coroutine 완료 시점/검사 동결 순서를 진단하고 재검사한다. 실패 로그·저장 입력은 artifacts/passenger-behavior-first-reload-*로 보존했다. 사용자 저장은 무관하다.
- 수면/실제 오염 상태 전이를6초 안내로 표시하는 보완도 포함해 최종 재빌드한다.

## 복원 진단 결과

- 두 번째 Release+Build 종료0. 기존 저장 입력으로 새 프로세스 재개 진단8검사 종료0.
- PRE_BOUNDARY: loaded/live=true지만 jobs=Idle, RESTORE 이후 jobs=OnTrip/Food. 등록 누락 가설은 기각, 실제 원인은 검사 coroutine이 BusinessProgress.Start/RestoreAfterInitialization 완료 전 검사한 순서였다.3프레임과 명시 복원 경계를 기다린 뒤 실제 저장 재개·중복 오염 방지·도착/정산을 검증했다.
- 최종 리뷰에서 실제 운송 간 candidateSeed 재사용에 따른 오염 사건ID 충돌을 발견, variant/spill seed는 유지하고 효과ID를 실제 tripId로 분리한다. 대사도 대표 요청의 실제 응답/이행 상태를 기억하도록 마지막 보완한다.
- 마지막 source 보완 후 전체 Player/렌더 검사를 다시 실행한다.

## 최종 소스 검증

- 실제 운송 ID를 오염 효과ID에 결합해 같은 후보를 다른 운송에서 태워도 정당한 오염이 발생하고 같은 운송 재개는 중복 차감하지 않게 수정했다. 기존 typed 저장은 과거 효과ID를 보존한다.
- 대표 요청의 미제시/대기/수락/보류/이행/중립/실패 상태와 실제 응답을 저장하여 유형의 후속·하차 대사를 연결했다. 같은 요청에 추가 점수를 붙이지 않는다.
- 최종 모델430, 기존 care13,301 종료0. 전체 prototype 모델 suite도 종료0.
- 최종 Release Editor1,526·JsonUtility care14·Live22·P11, PrototypeBuild.BuildHybrid 종료0. 로그 artifacts/passenger-behavior-verified-build.log.
- 독립 최종 리뷰와 수정 범위 재검토에서 남은 중요한 결함 없음. 최종 Player/렌더 결과는 아래 기록한다.

## 최종 Player 결과

- tools/test_passenger_behavior_player.ps1 -Capture: main174검사(79.3초)·새 프로세스 reload8검사(21.9초) 모두 종료0. 실제 승차/적재/하역/오염/수면/정산/입력 경계/같은 후보의 다른 운송·세차 뒤 재오염/JSON/재개를 위치 제어 방식으로 확인했다.
- 숨긴 창에서 만든4개 PNG는 검은 이미지임을 직접 확인. Direct3D11 디바이스 생성만으로 화면 검증을 완료 처리하지 않는다. 실제 창 검증 여부를 사용자에게 요청했고 이미지 증거는 미검증으로 남긴다.
- 실제 물리 주행·수동20분/3일 전체 캠페인 검증은 수행하지 않았다.

## 미검증·실행 기준

기본 프로젝트 unity/EungyeTaxi, 실행 builds/EungyeTaxi/EungyeTaxi.exe, 빌드 PrototypeBuild.BuildHybrid. 모델·Editor·위치 제어 Player·수동 플레이·실제 물리 주행/전체 캠페인은 구분해 기록한다. 사용자 저장 대신 artifacts의 검사 저장을 사용한다. 다른PC/실물 패드/사람 전체20분·3일 체감은 별도다.

## 실제 창 화면 확인·최종 회귀

- 사용자 명시 승인 후 tools/test_passenger_behavior_player.ps1 -Capture -Visible 실행: main174검사69.2초·reload8검사21.3초, 종료0. 숨김 실행과 명시적 Visible 선택을 분리하고4개 캡처의 생성 시각·다양한 색상 여부를 검사해 검은 화면이 UI PASS로 통과하지 않게 했다.
- 1280×720 실제 렌더4장(preparation/food-choice/sleeping/receipt)을 직접 확인. 적재/하역10초 안내, 음식 밀봉·허용(부스러기-2·흘림25%/-6)·정리10초 선택, 수면 상태, 정산창의 텍스트/버튼이 잘리지 않고 읽힌다. 캡처는 artifacts/passenger-behavior-*.png이며 위치 제어 시험 화면이다.
- 최종 exe -passengerCareTest: 기존 취객/구토/세차/운임 회귀201검사, 종료0. 로그 artifacts/passenger-behavior-legacy-care-player.log, 보고 artifacts/passenger-care-integration.txt.
- 최종 소스에서 tools/test_prototype.ps1 재실행 종료0. 로그 artifacts/prototype-model-tests-final.txt. docs11개 링크128/앵커24/표69개 검사 오류0(최종 표 추가 전 검사; 배포 전 재검사).

## 주요 변경 파일·재현

- PassengerBehavior.cs/VehicleCare.cs: 유형·변주·응답/대표 요청 상태·준비/수면/음식/짐 사건·배정 빈도·JSON 상태/구형 저장 호환.
- PassengerJobs.cs/PassengerRequestUI.cs/PassengerRequestSchedule.cs/PassengerRequestObservation.cs/BusinessProgress.cs/RoadNavigation.cs/DriveSession.cs: 실제 승차·안전 입력·요청·기한·오염·기상·정산·재개 연결.
- PassengerBehaviorUI.cs: 대사/선택/작업/상태 안내. PassengerBehaviorProbe.cs 및 Editor PassengerBehaviorIntegrationTests.cs/PassengerBehaviorReleaseTests.cs: 실제 객체와 최종 빌드 검증.
- tests/PassengerBehaviorTests.cs, tools/test_passenger_behavior.ps1, tools/test_passenger_behavior_player.ps1 및 기존 모델 compiler 목록; 관련 Unity .meta와 FirstDrive.unity.
- 컨텐츠02를 중심으로00/03/05/06/README, scope/checklist, 계획/이력/LATEST 및 기본 실행 폴더/배포 manifest.
- 재현: pwsh -File tools/test_passenger_behavior.ps1; pwsh -File tools/test_passenger_care.ps1; pwsh -File tools/test_prototype.ps1; Unity -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod PassengerBehaviorReleaseTests.Build -logFile <로그>; pwsh -File tools/test_passenger_behavior_player.ps1. 화면 검사는 사용자 승인 후 -Capture -Visible 옵션을 추가한다.
- 로컬 도구 정보: Unity2022.3.62f2. 사용자 저장 대신 artifacts/passenger-behavior-test.json/passenger-care-test.json만 사용한다. 작업 전부터 있던 사용자 untracked·.mmst·지도입력/지도ZIP은 유지했다.
