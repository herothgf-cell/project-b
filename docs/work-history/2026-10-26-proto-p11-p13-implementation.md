# P11~P13 순차 구현

## 요구·작업 공간

- 사용자 승인: 현재 `C:/git/project-b`의 새 `codex/proto-p11-p13` 브랜치.
- P11→P12→P13 순서. 단계별 집중 검사만 하고 안정화/전체 회귀/배포는 마지막.
- 사용자 `.mmst` 및 기존 미추적 폴더는 미수정·커밋 제외. 원격 push 없음.

## P11 진행

후보 시드 기반0/1/2건(20/60/20), 충돌 요청 제외, 승차/중간 정차 스케줄,20초 실제 주행/별개 정차/85% 미발생 중립을 구현했다. 기존 요청 관찰을 개별 컴포넌트로 분리하여 앞선 Comfort/Quiet/경로 기록을 유지하고 정산·HUD·F8은 합계(-10~+10)를 사용한다. 구형 단일 요청은 재추첨하지 않는다. 분실물은 별도 하차 후 텍스트 사건이다.

변경: `PassengerRequestSchedule.cs`, `PassengerRequestUI.cs`, `PassengerRequestObservation.cs`, `PassengerJobs.cs`, `BusinessProgress.cs`, `Assets/Editor/ProtoExpansionTests.cs`.

검사: Unity batchmode `ProtoExpansionTests.RunP11`.
- `artifacts/p11-red.log`: 미구현 스케줄러 검출, 종료1.
- `artifacts/p11.log`: 3,000후보 재현/분포/충돌, 시점 경계, 두 요청 관찰·상한, 구형/신형 JSON 복원, 교차로 내부/정지선 뒤 판정 통과, `PROTO_P11_OK`, 종료0.
- 검사 후 저장 유효성/내비 설정 보존과 중복 저장 호출을 보강했으며 다음 집중 검사에 포함한다.
- 실제 물리 주행/수동 체감/전체 캠페인/새 배포 빌드는 아직 하지 않았다. 요청 적합성·대화 중첩·저장 통합 최종 검증은 계속 진행한다.

## 다음

P12 지역별 수요·공차 성과 → P13 공간 점유 → 저장 안정화·전체 통합 검증·빌드/배포 manifest.

## P12 진행

게임용 명시 매핑: 주거 EG-GAME-03/04/06/08/10, 업무01/05/09/11, 상업02/07/12, 나머지 중립. 원래 카탈로그의 neighborhood/outer-link를 참고한 **게임 배정**, 실제 상권 통계·용도 주장 아님. 기존 접근/시간/연료 필터를 통과한 일반 호출만 가중 추첨하고 고정 코스 슬롯은 유지한다. 단계0~300/300~900/900초 이후에 각각 주거/업무/상업3배, 나머지1배. 수락한 호출은 교체하지 않는다.

공차/탑승 거리, 미탑승 이동/대기/서비스 시간을 당일·누적 집계한다. 물리 속도 적분을 사용해 위치 순간 이동은 거리로 넣지 않는다. 메뉴/연습 제외, 서비스는 기존 시간 차감과 같이 기록할 뿐 추가 시간을 소비하지 않는다. 구형 저장은 복원 시각부터 새 집계. 지도 호출 목록에 상대 수요·픽업/탑승/총 예상시간, 정산에 공차 성과 추가.

변경: `PassengerDemandPolicy.cs`, `OperationMetrics.cs`, `EconomyLedger.cs`, `PassengerJobs.cs`, `BusinessProgress.cs`, `ServiceStops.cs`, `MapDestinationUI.cs`, `OneDayHUD.cs`, 집중 검사와 순수 모델 컴파일 목록.

`ProtoExpansionTests.RunP12`: 미구현 검사 종료1(`p12-red.log`)→단계/분포2,000후보/중립/거리·시간·서비스/복원/날짜 이월 및 최신P11 묶음 종료0(`p12.log`, PROTO_P11_OK/PROTO_P12_OK). UI 화면/실제 운전/전체 회귀는 마지막에 확인한다.
