# 2026-10-27 지속 졸음·중도 하차·긴급 귀가·평상시 청결 소모

## 요구와 구현

- 사용자 피드백: 피로 시야가 잠깐 깜박이고 복구되는 대신 계속 좁아지며, 승객은 중도 하차/취소하고, 완전 시야 상실 때 귀가 휴식과 회복 감소를 제공한다. 평상시 청결을 천천히 소모한다. 배고픔은 아이디어만 제시하고 코드로 구현하지 않는다.
- 피로100의 활성 영업 시간을 저장한다. 최초0~60초 경고,60초부터 점진 시야 축소,120초부터 신규 승차 차단·탑승/픽업 운송 취소,180초 완전 시야 상실이다. 정차만으로 복구하지 않는다. 메뉴·일시정지·지도·상황 선택·결과·연습·시설 확인 중에는 경제/청결/졸음 누적을 진행하지 않는다.
- 취소 전에 차량 입력을 막고6m/s²로 감속,0.56m/s 미만에서 정차 후 하차한다. 운임/보너스0원·실패/취소 결과1회이며 실제 요청 미응답에 대한 추가 M감점도 적용하지 않는다. 청결 하차·구토 처리는 기존 운송ID로 한 번 적용한다. 취소 결과를 시간 초과로 잘못 표시하지 않도록 별도 저장 플래그와 영수증 문구를 둔다.
- 완전 시야 상실 후 정차한 차량에서 명시 `귀가 휴식(임시 주차 거점)` 버튼을 누르면 기존 임시 주차 거점으로 복귀한다.100게임초/피로20회복으로 정상100초/40회복보다 적다. **같은 날 계속 영업**을 선택했으며 자동 마감·날짜 이동·주택 신규 제작은 없다. 완전 상실 후 일반 시설 수면으로 회복량을 우회할 수 없다.
- 청결은 활성120게임초마다1 감소한다(20분10). 일반 하차-2/구토min(C,10)은 유지한다. 소수초 나머지를 저장·날짜 이월하며 세차만100/나머지0으로 복구한다. 시설 소요시간은 청결 소모에 중복 합산하지 않는다.
- 구형 저장의 확률 졸음 필드도 유효성 검사·읽기를 유지한다. 신규 시야 누적과 청결 나머지는 누락 시0이다. 배고픔은 기존25미만 피로1.5배 그대로이며 HUD/TIP·호출 전 예상 포만감·소식 메뉴는 기획10의 **미구현 선택안**이다.

## 변경 파일

`unity/EungyeTaxi/Assets/Scripts/DriverCondition.cs`, `DrowsinessHUD.cs`, `BusinessProgress.cs`, `EconomyLedger.cs`, `VehicleCare.cs`, `PassengerJobs.cs`, `PassengerRequestUI.cs`, `PrototypeState.cs`, `RideReceiptSummary.cs`, `ServiceStops.cs`, `FatigueOvertimeProbe.cs`.

`tests/ProgressiveFatigueTests.cs`, `tests/FatigueOvertimeTests.cs`, `tools/test_prototype.ps1`, `unity/EungyeTaxi/Assets/Editor/ProgressiveFatiguePersistenceTests.cs`(+meta), `docs/content-design/10-driver-condition.md`, `docs/content-design/15-drunk-passengers-and-cleanliness.md`.

## 실제 검증

| 구분 | 실행·결과 | 범위 |
|---|---|---|
| RED 모델 | 새 `ProgressiveFatigueTests`를 기존 코드로 컴파일 후 실행,종료1: `fatigue exposure must persistently narrow vision after 60 active seconds` | 기존3초복구가 새 지속 축소 요구를 만족하지 않음을 먼저 확인. 초기 상대경로/누락 의존성 컴파일 실패는 RED 근거로 계산하지 않음 |
| GREEN 모델 | `pwsh -File tools/test_prototype.ps1`,종료0 |25묶음. 신규 지속 피로15검사·피로/초과영업25검사 및 기존 회계/운임/요청/운전 규칙 |
| Unity 직렬화 | 부모 통합 실행 `ProgressiveFatiguePersistenceTests.Run`,종료0, `PROGRESSIVE_FATIGUE_PERSISTENCE_TESTS_OK checks=12` | 실제 JsonUtility119.5초 시야/청결 나머지·120/180경계·동일날 긴급 회복·중복/우회 방지·구형 저장·취소 영수증. 로그 `artifacts/progressive-fatigue-editor.log` |
| 소스 공백 | `git diff --check`,종료0 | 사용자 마인드맵 제외 여부와 별개로 공백 오류 없음 |

## 실행·남은 확인

- 기본 대상은 은계 `builds/EungyeTaxi/EungyeTaxi.exe`, 빌드 `PrototypeBuild.BuildHybrid`다. 이 하위 작업은 Unity 빌드/배포·커밋/병합/푸시를 실행하지 않으며 부모 통합 작업이 담당한다.
- 새 실행파일에서는 `-fatigueOvertimeTest -fatigueCapture`로 `FatigueOvertimeProbe`를 실행한다. `artifacts/fatigue-overtime-runtime.txt`에 실제 탑승·감속/취소/중복·M/법규 보존·바퀴 입력차단·동일날 귀가/정상수면/마감을 기록하며, `fatigue-warning.png`, `fatigue-tunnel.png`, `fatigue-blackout.png`를 생성한다. **이 문서 작성 시 해당 신규 실행 검사는 아직 미실행**이다. 과거2026-10-26 동명 PNG/로그를 새 검증으로 계산하지 않는다.
- 후속: 새 빌드의 위 위치 제어 실행·화면 확인·일반 회귀, 사람의20분/3일 시야 강도·휴식/청결 빈도·안전 감속 체감. 위치 제어 실행은 전체 자동 물리 주행/수동 플레이 완료가 아니다.
- 지도 입력·ZIP·배포 manifest는 이 하위 작업에서 변경하지 않았다. 사용자 `.mmst`와 기존 미추적 폴더는 수정하지 않았다. 부모가 병행한 내비/신호 작업과 최종 빌드·배포 검증은 별도 최신 인계에 기록한다.
- 이력의 실제 커밋은 커밋 후 `git log -1 --oneline -- docs/work-history/2026-10-27-progressive-fatigue-cleanliness.md`로 확인한다.
