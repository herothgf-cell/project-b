# 피로100 졸음·초과 영업·운전 점수와 회복

## 요구와 구현

- main에서 기획02/04/10/11과 코드 동시 변경. 피로100 승차 차단 대신 이동20초마다25% 졸음 추첨, 예고2초 뒤 시야 축소3초 또는 조작 중단1초. 진행 중 정차해도 증상 종료, 메뉴·연습 정지, 수면 해제, 난수·상태 저장.
- 예정20분 경계에서 피로100을 한 번 적용하고 초과 영업 허용. 빈차로 주차장/휴식 구역에 정차 후 N 또는 F7의 종료 선택: 무료 수면100초·피로40 회복 후 정산. 다음 날 야간 회복60 유지. 휴식·식사 예상치는20분 경계를 포함해 실제 적용과 같은 함수 사용.
- 운행 점수 표시를 운전 점수로 변경. 신규 법규 준수 사건+3, 이전 저장 사건+1 유지, 동일 사건 중복 회복 차단. 승차 점수0/70/100에서 기본 약정90/100/110%, 중간 선형·10원 내림. 승차 때 고정하고 최종 S-R 감액을 한 번 적용. 보너스 원약정 기준 유지, 구형 진행 운송 소급 변경 없음.
- 안정 주행350m 또는45초, 실제 이동0.5m/s 이상 및 고유10m 구간. 급가감속 M 손실+2·운송당최대6 우선, M 회복이 없는 기회에 S<R이면 안심+2·최대4·R 상한. 저장/실시간 표시/정산에 한 번 반영.

## 변경 파일

- 컨디션/마감: DriverCondition, EconomyLedger, BusinessProgress, ServiceStops, TaxiController, OneDayHUD, 신규 DrowsinessHUD, PostTripServicePolicy.
- 점수/운임/회복: PrototypeState, DrivingLaw, DrivingScoreJournal, DrivingManners, MannerLedger, PassengerRequest, PassengerJobs, RideReceiptSummary 및 관련 HUD·요청·지도·결과 UI. 신규 법규 사건 생성부도 정책 버전 기록.
- 검증: FatigueOvertimeTests/Probe, PassengerCareTests/Probe, PassengerScoreRecoveryTests, 하루/3일 모델·프로브, 회복 검사 스크립트.
- 문서: content-design의 현재 용어 및02/04/07/10/11 사양, scope, checklist, 구현 계획, LATEST. 배포 폴더와 manifest는 최종 검증 후 별도 커밋.

## 검증과 발견한 문제

- `pwsh -File tools/check_workspace.ps1 -Strict`: 종료0, 입력 오류0. 정식 지도 contentHash `c14a2a4b277e040dce001f2df7f89cc0404555a10b85ef931f2b9de14b6b05dc`. 선택 원본 입력2개 부재, 필수 지도/ZIP 변경 없음.
- `tools/test_prototype.ps1`: 24묶음 종료0, 피로·초과 영업26검사 포함. `tools/test_passenger_care.ps1`: 11,424단언 종료0. `tools/test_manner_recovery.ps1`:16검사 종료0. 마감 직전 서비스 권장 회귀는 실패 확인 후 수정·통과.
- Unity 편집기 `PassengerScoreRecoveryTests.Run`:13검사 통과, 종료0. JsonUtility 구형/신규 점수 저장과 중복 사건 검사.
- 첫 컴파일의 프로브 enum 오타 수정. 다음 빌드는 실행 중 사용자 게임의 파일 잠금으로 실패. 사용자가 저장·종료한 후 `PrototypeBuild.BuildHybrid` 성공·종료0 (`artifacts/fatigue-score-build-final.log`). 로컬 Unity2022.3.62f2 사용.
- 새 exe `-fatigueOvertimeTest -fatigueCapture`: 종료0·14검사. 실제 WheelCollider 입력0, 정차 후 조작 복구, JSON 상태, 초과 거점 휴식 및 중복 마감 방지. 첫 캡처는 검은 화면이므로 화면 검증 성공으로 취급하지 않음. 창 모드 렌더링 준비·차량 낙하 방지·PNG 대비 검사 추가 후 재빌드 종료0 (`fatigue-score-build-capture.log`), 재실행17검사·종료0. 실제 시야 축소/조작 중단 PNG를 열어 확인.
- 새 exe `-passengerCareTest -scoreCapture`: 종료0·180검사. 고정 승차 점수/구형 운임/회복 체크포인트 포함. 1280/1600/1920 HUD와 영수증 캡처 생성, 1280 HUD·1920 영수증을 직접 열어 표시 확인.
- 별도 코드 검토의 P2 서비스 예상 피로 경계 오류 수정·회귀 추가·재검토. 남은 P1/P2 지적 없음.

- 새 exe 하루/재실행 프로브 각각 종료0. 3일 최초 검사는 Windows 시험 저장 File.Replace 실패로 다음 날 전환에 실패(종료1). 기존 시험 JSON/백업을 별도 폴더에 보존 후 재실행9검사·종료0, 최종 빌드의 별도 프로세스 복원11검사·종료0. 실제 day2 피로27.333333333333332·포만감95·elapsed110, 최종 누적 순수익47,734원 확인. 지속적인 저장 오류로 재현되지 않았으며 생산 저장 코드는 변경하지 않음.

## 실행과 남은 확인

기본 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`. 소스 빌드는 `PrototypeBuild.BuildHybrid`. 자동 프로브는 별도 시험 저장을 사용한다. 하루/3일은 `-oneDayTest` 다음 `-oneDayReloadTest`, `-threeDayTest` 다음 `-threeDayReloadTest`를 별도 프로세스로 실행한다.

자동 상태/위치 제어 검사는 사람의 수동 주행과 다르다. 수동20분·초과 영업·3일 경제/졸음 체감, 전체 교통 물리 주행은 미검증. 이전 통합 기록의 안정성 자동 물리 검사 전체 실패를 이번 상태 검사로 해결했다고 보지 않는다. 수치·빈도는 시험값이다. 사용자 기존 미추적 파일은 변경/커밋하지 않는다.
