# 2026-10-29 대화 선택의 실제 효과 표시

## 요구·설계·실행 계획

간식/음료/도시락 섭취 허용에 만족도 +5를 제공한다. 실제 섭취 청결 -2,25% 사전 결정 흘림 때 추가 -6은 유지한다. 밀봉/정리는 이 유형의 추가 청결 감소0이며 일반 하차-2와 구분한다. 확정 만족도와 조건부 오염을 선택 전에 구분해 표시한다. 다른 대화 선택도 만족도·시간·경로·작업 변화를 실제 규칙과 일치시킨다. 기존 무보상 대화에 임의로 보상을 추가하지 않는다.

- [x] 모델 실패 검사: 허용 보상/거절·미응답 중립/중복·복원/모든 선택 효과.
- [x] 유형 효과를 단일 모델에서 제공, 허용 +5를 라이브와 확정 정산 양쪽에 반영. 저장된 선택에서 도출해 반복 가산을 막는다.
- [x] 선택 버튼 행동/효과 2줄, 선택 직후 실제 만족도 변화와 후속 청결 위험을 안내. 기존 요청 선택도 실제 만족도 판정 수치·조건을 표시한다.
- [x] 모델·기존 care/request·격리 Player/새 프로세스 복원·실제 화면 검증. 사용자 저장과 진행 중 PvP 작업은 보존.
- [ ] 기획/체크리스트/인계 갱신, BuildHybrid 실행 폴더·manifest 갱신, main 게시.

초기 시험값 +5는 기존 공감 응대 보상과 같은 수준으로 선택했다. 만족도 상한100을 유지하며 운전 점수·추가 운임 자체를 직접 가산하지 않는다. 기존 진행 중 허용 선택도 복원 시 +5로 계산하되 이미 확정된 정산 결과는 바꾸지 않는다.

## 구현·중간 검사

변경 파일: PassengerBehavior.cs(허용 보상/선택 효과), PassengerJobs.cs(라이브/정산 합산), PassengerBehaviorUI.cs(가변 버튼·전후 수치·독립 응답 타이머), PassengerRequest.cs(실제 Delta 기반 선택 설명), PassengerRequestObservation.cs(긴 효과 버튼/본문), PassengerBehaviorProbe.cs(실제 허용/보관/정리·정산·재개 검사/화면), PassengerBehaviorTests.cs, 기획02/05·범위·체크리스트·이력·LATEST와 생성 FirstDrive.unity/실행 폴더/manifest.

- 먼저 tools/test_passenger_behavior.ps1에서 ‘food permission must reward satisfaction’ 실패(종료1)를 확인하고 구현 뒤437검사 통과/종료0.
- tools/test_passenger_care.ps1:13,301검사 종료0. tools/test_prototype.ps1:28클래스 종료0.
- 초기 최종 BuildHybrid 종료0 뒤 실제 창 Player189검사·새 프로세스 복원10검사 종료0. 허용/미응답/밀봉/정리의 라이브·정산 +5/0 구분과 재개 중복 방지를 검증했다.
- 실제1280×720 음식 선택·허용·강제 흘림3화면 확인:81→86, 청결100→92. 첫 일반 요청 화면은 fixture가 요청 컴포넌트를 꺼 둬 누락됐으므로 시각 성공으로 세지 않고 캡처 때만 활성화하도록 보완했다. 최종 실행 재검사는 아래에 기록한다.
- 독립 코드 검토 중대/중요 지적0. 응답 알림이 후속 사건에 다시 표시되는 경미한 문제는 responseUntil 분리로 수정하고 재검토 확인했다.
- 사람의 도로 주행·전체 캠페인·다른 물리PC 검증은 이번 범위가 아니다. 위치/시간 fixture와 실제 렌더 검사를 구분한다. 사용자 저장과 진행 중 PvP 작업/기존 미추적 파일은 보존한다.

재현: pwsh -File tools/test_passenger_behavior_player.ps1 -Capture -Visible. 기본 은계 BuildHybrid exe를 사용자 저장과 격리한 별도 파일로 검사한다. 간식/음료/도시락은 같은 허용·섭취 규칙을 쓴다. 기획의 수치 표는 docs/content-design/02-passengers-and-fares.md의 ‘대화 선택의 실제 효과 표시’를 따른다.

## 최종 검증

- PrototypeBuild.BuildHybrid 종료0: artifacts/passenger-choice-effects-release-build.log.
- 보완한 최종 exe의 test_passenger_behavior_player.ps1 -Capture -Visible 종료0. 첫 프로세스189검사(71.3초), 새 프로세스 복원10검사(20.4초). 보고서는 artifacts/passenger-behavior-player.txt / passenger-behavior-reload.txt, 로그는 passengerBehaviorTest.log / passengerBehaviorReloadTest.log.
- 최종1280×720 실제 화면4개 확인: 음식 선택, 허용81→86, 강제 흘림 청결100→92, 일반 승차감 요청 이행+8/미이행-10/보류0. 버튼·본문 잘림 없음. 음식/짐 일반 하차-2 별도 규칙 유지.
- 추가 캡처는 artifacts/passenger-behavior-{food-choice,food-allowed,food-spill,request-effects}.png. 이전 이력처럼 단순 위치/상태 fixture이며 수동 실제 도로 주행은 수행하지 않았다.
- check_workspace.ps1 -Strict 종료0/Input issues0. 지도·ZIP 변경 없음. 기획 수치와 UI 표현을 동기화하고 독립 검토 지적을 해결했다.
