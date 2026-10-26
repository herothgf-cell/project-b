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

P11~P13 핵심 구현 후 최종 실행파일 통합 검증·빌드/배포 manifest 진행. 아래 단계별 기록은 당시 상태다.

## P12 진행

게임용 명시 매핑: 주거 EG-GAME-03/04/06/08/10, 업무01/05/09/11, 상업02/07/12, 나머지 중립. 원래 카탈로그의 neighborhood/outer-link를 참고한 **게임 배정**, 실제 상권 통계·용도 주장 아님. 기존 접근/시간/연료 필터를 통과한 일반 호출만 가중 추첨하고 고정 코스 슬롯은 유지한다. 단계0~300/300~900/900초 이후에 각각 주거/업무/상업3배, 나머지1배. 수락한 호출은 교체하지 않는다.

공차/탑승 거리, 미탑승 이동/대기/서비스 시간을 당일·누적 집계한다. 물리 속도 적분을 사용해 위치 순간 이동은 거리로 넣지 않는다. 메뉴/연습 제외, 서비스는 기존 시간 차감과 같이 기록할 뿐 추가 시간을 소비하지 않는다. 구형 저장은 복원 시각부터 새 집계. 지도 호출 목록에 상대 수요·픽업/탑승/총 예상시간, 정산에 공차 성과 추가.

변경: `PassengerDemandPolicy.cs`, `OperationMetrics.cs`, `EconomyLedger.cs`, `PassengerJobs.cs`, `BusinessProgress.cs`, `ServiceStops.cs`, `MapDestinationUI.cs`, `OneDayHUD.cs`, 집중 검사와 순수 모델 컴파일 목록.

`ProtoExpansionTests.RunP12`: 미구현 검사 종료1(`p12-red.log`)→단계/분포2,000후보/중립/거리·시간·서비스/복원/날짜 이월 및 최신P11 묶음 종료0(`p12.log`, PROTO_P11_OK/PROTO_P12_OK). UI 화면/실제 운전/전체 회귀는 마지막에 확인한다.

## P13 진행

검증된 일반 게임 포켓만 점유, 동시1대·30게임초 후 포켓 앞7m로 저속 이동, 시야 밖 제거. 접근 중 생성/차량·승객 겹침을 막고 원래 승하차 위치가 비면 그대로 사용한다. 새 승차 후보/수락/정차 판정에 점유를 반영하고 진행 운송은 유지한다. 사건당 한 번 잔여 점유+이동 여유(상한90초) 기한 보호, 같은 운송/운임 유지, 경로 요청에 시스템 사유 기록. 점유 상태·기한 보호 여부를 진행 저장에 포함한다.

**지도에 따른 제한:** 현재 추가12후보 중 직선150m 이내 쌍이0개다. 계획의 ‘안전한 대안 없음’ 경로를 적용하여 현재 배포 범위는 대기만 제공한다. `TryGetAlternative`는 false를 반환하며, 안전 대체 이동 기능이 완성됐다고 주장하지 않는다. 새 후보·지도 연결을 임의로 만들지 않았다. 차량 이동 경로를 플레이어/NPC가 막으면 안전을 위해 해소가 늦어질 수 있다.

변경: `StopOccupancyPolicy.cs`, `StopOccupancySystem.cs`, `FareTrip.cs`, `DriveSession.cs`, `PassengerJobs.cs`, `BusinessProgress.cs`, 요청 관찰/UI, 지도 핀, 집중 검사.

`ProtoExpansionTests.RunP13`: 미구현 경계 종료1(`p13-red.log`)→점유1대·해소·기한 한 번·복원·원운임 유지·도착 잠금·실제 후보12/근거리쌍0 확인 종료0(`p13.log`). 실제 렌더링/물리 진입·통합 저장은 최종 단계에서 확인한다.

## 최종 안정화 시작

P13 핵심 구현 후 기존 고빈도 File.Replace 저장 경고를 먼저 재현/진단하고 전체 회귀·독립 리뷰·빌드를 진행한다. 아직 최종 완료/배포 갱신 상태가 아니다.

저장 진단: 격리된 파일의 기존 방식500회 중90회 IOException(HResult -2147023721, 원본/임시/백업 존재), 마지막 반영453. 고정 백업/유일 백업 비교200회에서는 각각36/0회 실패했다. 원인은 기존 백업 경로를 다시 교체하는 Windows/Mono 파일 교체 경계로 좁혔다(외부 잠금 주체는 미확정). 새 `CampaignCheckpointWriter`는 유일한 임시/이전 저장 이름, 원본 선삭제 없는 원자 교체, 제한 재시도, 실패 payload 보존을 사용한다. 이전 정상본을 `.bak`에 복사하지 못하면 유일한 이전본을 남기고 상태에 경로를 표시한다.

`CampaignCheckpointWriterTests.Run` 종료0:500연속 쓰기/마지막 backup, 일시 잠금 재시도, 지속 잠금 실패·원본 보존·pending 보존, 잠금 해소 후 정상 저장. 로그 `artifacts/checkpoint-verified.log`. 기존 모델24묶음(`proto-final-models.log`)·승객13,301단언(`proto-final-care.log`) 종료0. 독립 전체 리뷰1회 진행 중. 최종 실제 실행파일/화면/프로세스 복원/물리 주행은 아직 수행 전.

## 최종 코드 검토 및 Editor 통합

독립 리뷰1회에서 중요3건: 늦게 제시된 큰길 요청의 ETA 대체로 저장 종류 불일치, 복원한 점유 차량 뒤5.5m 대기 시 타이머 정지, 취객 대화에 가려진 요청의 입력/미응답 기회 누적. 세 조건을 집중 검사에서 모두 재현한 뒤 수정했다. 배정 종류 유지·불가능 요청 중립 보류, 표시 여부와 입력/응답 기회 일치, 시각 객체 재생성 여부와 무관한 점유 카운트다운을 적용했다. accepted 요청의 주행 관찰은 계속한다.

- `ProtoReviewRegressionTests.Run`: 검사 코드 readonly 초기화 오류를 먼저 수정. 이후 `proto-review-red.log` 세 결함 실패, 종료1.
- `ProtoReviewRegressionTests.RunFinal`: 세 회귀, P11/P12/P13, 500회 저장/잠금, 기존 주유·보행·시작/지도/경로 묶음 통과, `proto-final-editor.log`, 종료0.
- 기존 저장 실패 fixture의 고정 `.tmp` 폴더 주입은 유일 임시 이름 정책과 맞지 않아 실제 원본 파일 잠금으로 교체했다. 고정 Quiet 응답 검사도 무작위 정상 운송에 의존하지 않고 명시 fixture로 지정했다.
- 자동 물리 하루 검사 종료 부분을 현재 초과 영업 정책에 맞춰 주차장 물리 복귀→20분 이후 명시 마감으로 변경했다. 아직 실행 결과는 아래 후속 기록 대상이다.
- 리뷰의 실제 물리/화면/새 프로세스 검증은 코드 리뷰가 보증하지 않는 범위이며 다음 실행 단계에서 따로 확인한다. 재리뷰 반복은 하지 않는다.

첫 BuildHybrid 종료0(소스66ec20b). 신규 저장6/복원11, 기존 진행 저장9/복원23, 승객 통합 실행 종료0. 기존 하루/3일/자동 물리 검사 도구는 비동기 시작 화면 도입 전의 고정2초/즉시 초기화를 가정하여 NullReference가 발생했다. 해당 검사 프로세스를 종료하고 `StartupReady` 대기로 변경 후 영향 검사만 재실행한다. 첫 물리 호출의 하루 모드 인수 누락도 별도로 정정했다. 이 실패를 게임 운송 성공으로 계산하지 않는다.

소스117f9bc 재빌드 종료0. 하루 저장/복원과3일 저장/복원 모두 종료0. 자동 물리 하루는 48차량 유지,255.7게임초/1.30km 픽업 접근까지 진행했으나 신호 매너 판정1건 및 포켓 승차 실패로 종료1(`verified-physical-day.log`, `one-day-traffic-drive.txt`, `live-traffic-validation.txt`). 충돌0·독립 법규 감점0이며 전체 하루/거점 왕복 성공이 아니다. 자동 컨트롤러는 내비 차로 종점까지 주행하며 추가 포켓의 연석 정차를 별도로 수행하지 않는다. 신호 매너와 법규 평가 차이는 추가 재현이 필요하다. 검사 시작 대기 수정과 달리 이 결과는 성공으로 바꾸지 않는다.

화면 캡처는 상대 경로가 Player 데이터 폴더 기준으로 해석되어 최초 저장 실패. 절대 경로로 수정 후 다시 확인한다. 손상 저장 검사 도구도 동일하게 StartupReady 대기를 적용했다. 제품 동작 코드는 변경하지 않는다.

## 최종 실행 결과·배포 인계

최종 소스 `1e78d56b5e3297a28cc543990537d333c6188621`, 브랜치 `codex/proto-p11-p13`. `PrototypeBuild.BuildHybrid` 종료0, `artifacts/proto-final-build-capture.log`. 기본 실행 `builds/EungyeTaxi/EungyeTaxi.exe`. 원격 push/main 병합은 하지 않는다. 생성 씬은 빌드가 갱신한 동일 설정의 객체ID 변경만 포함한다.

| 검사 | 결과 | 실제 범위 |
|---|---|---|
| 모델24묶음 / care13,301 | 종료0 | 순수 규칙·회계·상태 검사 |
| 최종 Editor 묶음 | 종료0 | P11~P13·리뷰 회귀3건·저장500회/잠금·기존 지도/경로/주유/보행 |
| 신규 저장/새 프로세스 복원 | 6/11단언, 각각0 | 복수 약속·공차 지표·점유 대기/실제 퇴장·한 번 정산, 위치/시계 제어 |
| 기존 진행 운송 저장/복원 | 9/23단언, 각각0 | 구형 단일 요청·취객·동일 운송/법규/회계 경계 |
| 승객 통합 | 201단언,0 | 주유/세차·만족도·회복·취객·상황 저장, 위치 제어 |
| 하루 및3일 저장/새 프로세스 복원 | 각각0 | 식사/수면/초과 영업/명시 마감·날짜·회계·실패 보존. 위치/시계/회계 fixture이며 실제3일 주행 아님 |
| 손상 저장 새 프로세스 | 4단언,0 | 기본/백업 보존, 출발 금지, 명시 재시작으로 정상 저장 복구 |
| 자동 물리 하루 | **종료1** | 255.74초/1,304.45m/운송0, 충돌0·독립 법규 감점0·신호 매너1. 픽업 연석 정차와 신호 평가 재현 남음 |
| 실제 렌더 캡처 | **시각 확인 미완료** | 절대 경로 PNG는 생성됐으나 숨긴 창 캡처가 검정 화면. 실행 종료0을 가독성 통과로 취급하지 않음 |
| 필수입력 Strict | 종료0 | Input issues0, 선택 import2개 부재, 지도/ZIP 미수정 |
| 배포 manifest | 종료0 | 130파일/105,686,443바이트 SHA256 일치, 검증 한계도 manifest에 명시 |

독립 headless 신규 저장/복원 시작 준비16,394/16,468ms, 마지막 렌더16,307ms. 과거 약19초보다 짧지만 직전 후속 작업의14,178ms보다 빠르다고 주장하지 않는다. 캐시·부하가 같은 반복 성능 비교나 장시간 FPS 검사는 아니다.

손상 저장 로그 `verified-final-liveInvalidTest.log`; 하루/3일 로그 `verified-oneDayTest.log`, `verified-oneDayReloadTest.log`, `verified-threeDayTest.log`, `verified-threeDayReloadTest.log`. 최종 렌더 `verified-final-protoExpansionTest.log`와 `proto-expansion-map.png`(검정 캡처). 다른 실행 로그는 위 단계별 기록 참고. `artifacts/`는 Git 배포 제외이며 주요 결과를 본 기록과 manifest에 보존했다.

### 재현 명령

- Unity batchmode executeMethod: `ProtoReviewRegressionTests.RunFinal`, 빌드는 `PrototypeBuild.BuildHybrid`.
- 저장/복원: 기본 exe에 `-batchmode -nographics -protoExpansionTest`, 다음 별도 프로세스에 `-protoExpansionReloadTest`. 각각 `-logFile <절대 로그 경로>` 지정.
- 기존 진행 운송은 `-liveSaveTest` → `-liveReloadTest`, 승객은 `-passengerCareTest`.
- 회계 경계는 `-threeDayTest` → `-threeDayReloadTest`, `-oneDayTest` → `-oneDayReloadTest`. 동일 저장을 쓰는 물리 하루와 동시에 실행하지 않는다.
- 자동 물리는 `-batchmode -nographics -oneDayMode -hybridV2 -oneDayTrafficTest`. 현재 결과는 실패이며 합격 기준을 완화하지 않았다.
- 손상 저장은 **격리 검사 경로만** `artifacts/live-invalid-test.json`과 `.bak`에 잘못된 JSON을 준비한 뒤 `-liveInvalidTest`. 실제 사용자 저장에 실행하지 않는다.
- 배포 `pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly`, 입력 `pwsh -File tools/check_workspace.ps1 -Strict`.

### 남은 작업과 완료 판단

P11·P12·P13 대기 경로의 기능 구현/자동 저장·회계 검증은 반영됐다. **최종 안정화 전체 완료는 아니다.** 자동 주행 컨트롤러의 포켓 진입·정차, 신호 매너1건과 독립 법규0의 차이를 재현/수정하고 물리 하루를 통과해야 한다. 실제 보이는 창의 UI·사람의20분/3일 경제 체감·장시간 안정성도 남는다. P13 안전 대체 이동은 현재 맵의 검증 후보 쌍 확보 전까지 제외한다.

마인드맵 원본 SHA256 `F1951492F9C5A13941E60A17D92EB31B1A7471FB8ABA7996D47A0C2E02F00937` 유지. 추가/수정 가지 목록은 `docs/content-design/17-proto-operation-expansion.md` 마지막 절에서 사용자 검토용으로 제공하며 원본에 적용하지 않았다.
