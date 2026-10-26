# 검은 초기 화면·구형 저장 복원·운임 감액 수정

## 요구와 원인

- 실행 직후 긴 검은 화면: 기존 DriveSession.Awake에서 월드/교통/보행자 생성이 끝나야 카메라가 생성됐다. 지도 생성이 주된 초기 비용이며 보행자6명 때문이라고 단정할 근거는 없었다.
- 초기화 전 영업 시작 실패: 실제 Player.log에 `Campaign load failed: Invalid ambient pedestrian count` 2회. Unity JsonUtility가 구형 저장의 누락/null 보행자 필드를 `actors=null`인 객체로 생성하여 엄격한 개수 검사에 실패했다.
- 첨부 결과 S48/R60·최초11,300원→11,680원: 승차 점수100의 할증10% 뒤 기존 미달12점 감액6%가 적용되어 `floor10(11,300×1.1×0.94)=11,680`. 실제 보너스는0인데 최초 예상 대비 순증감380원을 보너스로 표시했다. 화면의 운전 점수85는 하차 시 점수이며 승차 할증 기준과 다르다.

## 실제 수정

| 영역 | 변경 |
|---|---|
| 시작 화면 | 로딩 카메라/안내를 먼저 표시하고 지도→교통→보행자/영업 단계 사이 프레임 양보. StartupReady 전 입력·게임 시간 차단, 준비/이어하기의 기존 일시정지 보존 |
| 저장 | ParseCheckpoint에서 실제 JSON에 보행자 필드가 없거나 null인 경우만 구형 기본 상태로 복원. 실제 객체의 잘못된 개수/ID/횡단 상태는 계속 거부 |
| 신규 승차 정책4 | S<R에서 양의 운전점수 할증을 제외하고 `floor10(min(F,A)×S/R)` 지급. S0은 기본운임/보너스0. S>=R·기한초과 정책 유지 |
| 기존 저장 | 완료 운송의 금액은 재계산하지 않음. 진행 중 구형 운송은 기존 정책2/3 유지. 신규 승차 정책4를 운송 상태에 저장 |
| 정산 | 3줄의 둘째 줄을 운임 조정으로 표시. F8은 실제 보너스 유지. 만족도0 전액 감액을 시간초과로 잘못 표시하지 않음 |

관련 소스: DriveSession, BusinessProgress, PassengerRequest, PassengerJobs, RideReceiptSummary, RideCompletionPanel. 검사: PassengerCareTests, CampaignStartupTests/Probe, PassengerCareProbe, LiveCampaignProbe, AmbientPedestrianGeometryAudit. 기획: 콘텐츠04/05/13, 범위/체크리스트, 구현 계획 및 본 이력/LATEST.

## 검증 결과

- `tools/test_passenger_care.ps1`: 첨부 사례 기존 지급11680→기대9040 실패 재현 후 수정, 최종13,301단언 종료0. 요구60/70/85·승차점수0/70/100·S0~100 단조성/경계·보너스/구형 보존·오표시 검사.
- `CampaignStartupTests.InspectLegacy` RED 종료1: missing/explicitNull 모두 actors-null 객체로 생성됨 확인.
- `CampaignStartupTests.Run` GREEN 종료0,10검사: 누락/null 구형 호환, 실제 잘못된 개수/ID/진행값 거부, 기존 운송/시간 보존.
- 샌드박스 Unity 첫 시도는 라이선스 IPC 연결 문제로199. 승인 실행으로 위 RED/GREEN·빌드 수행.
- `PrototypeBuild.BuildHybrid`: 종료0 (`artifacts/startup-save-fare-build.log`).
- 보행자 배치 진단870ms,6안전 경로 검사 통과. 추가 편집기 진단 월드14,950ms·도로13ms·교통5,640ms·보행자884ms. 교통은 편집기의 런타임 Destroy 경고 비용이 포함된 참고값. 해당 임시 진단 메서드는 삭제했다.
- 소스 집중 리뷰에서 구체적 회귀 없음. 실제 실행파일/화면 결과는 아래 최종 확인에 기록한다.

### 실제 실행의 중간 결과와 검사 보완

- 첫 `-campaignStartupTest`: 종료0/8검사, 초기화 없이 준비→Depart1회→시간 진행 확인. 측정 world11,924ms/roads+traffic5,473ms/ambient756ms/gameplay366ms/total18,527ms. PNG3개는 숨김 창의 검은 버퍼였으므로 시각 검증 완료로 처리하지 않았다.
- 첫 `-liveReloadTest`: 종료1. 보관 fixture는 이미Idle인데 sidecar는 취객 운송 중 상태를 기대했다. 보행자 필드 부재만 확인하고 상태 일치를 확인하지 않은 검사 준비 오류다. 신규 격리 저장을 만든 뒤 ambient 필드만 제거해 실제 복원 회귀를 다시 수행한다.
- 첫 `-passengerCareTest`: 새 코루틴 초기화의 완료 프레임에서 Time.deltaTime=0이라 UpdateCare 검사9에서 실패했다. 제품 계산을 바꾸지 않고 StartupReady 뒤2프레임 기다리도록 probe를 보완했다.
- 캡처 probe는 해상도 전환 뒤 실제 픽셀 명도 차이를 확인해 PNG 저장하도록 보완. 검은 캡처는 실패로 처리한다. 위 검사 도구 수정 후 BuildHybrid를 다시 수행한다.

### 최종 확인

- 최종 `PrototypeBuild.BuildHybrid` 종료0 (`startup-save-fare-build-final.log`).
- 최종 `-campaignStartupTest` 종료0/8검사. 로딩 안내·1일차 준비·영업 시작 뒤 HUD PNG3개를 직접 확인했다. world13,413ms/roads+traffic5,160ms/ambient749ms/gameplay371ms, 총19,704ms. 첫 안내는 지도 생성 전에 렌더링되며 실제 전체 준비 시간은 약18.5~19.7초였다.
- 현재 기획20문서319링크/앵커 오류0, `git diff --check` 오류0, `tools/check_workspace.ps1 -Strict` 종료0/Input issues0. 선택 가져오기2지도 부재 유지.
- 최종 `-passengerCareTest -fareCapture` 종료0/207검사. 실제 운송과 저장 복원에서 정책4를 유지하고 S48/R60·11,300원→9,040원, S0→0원을 확인했다. 정산2장 직접 확인: 운임 조정 -2,260/-11,300원,0점 하단은 만족도0 전액 감액으로 표시.
- 이 고빈도 저장 검사 도중 기존 File.Replace의 “바꿀 파일을 제거할 수 없습니다” 경고1회가 있었다. 후속 저장·복원 단언은 모두 통과했으나 경고가 사라졌다고 주장하지 않는다. 이전 작업에서도 관찰한 원인 미확정 파일 교체 경고이며 실제 사용자 저장 실패 시 별도 진단 대상이다.
- 새 프로세스 `-liveSaveTest` 종료0/9검사로 취객 운송 중 파일을 생성했다. artifacts의 해당 테스트 파일과 백업에서 ambientPedestrians 키만 제거하고 나머지 운송/시간/회계 값 유지, `-liveReloadTest` 종료0/23검사. 구형 필드 부재 복원과 기상 후 정상 정산이 실제 새 프로세스에서 통과했다. 사용자 저장은 사용하지 않았다.

## 한계와 재현

전체 로딩 시간이나 주행 FPS 개선 완료를 주장하지 않는다. 긴 단계 내부는 여전히 동기 계산이고, 이번에는 검은 화면을 단계별 안내로 바꾸고 저장 호환 오류를 수정했다. 수동 전체 캠페인·장시간 프레임·모든 물리 주행은 미검증이다.

기본 지역 은계, 실행 `builds/EungyeTaxi/EungyeTaxi.exe`. 일반 시작은 준비 화면에서 영업 시작1회로 시간과 호출이 열린다. 구형 저장은 초기화 없이 이어하기. 진단은 `-campaignStartupTest`, 기존 격리 저장 복원 `-liveReloadTest`, 운임/화면 `-passengerCareTest -fareCapture`를 사용한다. 사용자 저장은 읽기만 했고 사용자가 실행 중이던 게임에서 저장이 갱신된 것을 확인해 최종 교체 전 게임 종료 답변을 받았다. 실제 테스트 파일은 artifacts의 별도 경로다.

지도 입력/JSON/ZIP은 변경하지 않는다. 사용자 .mmst 및 기존 미추적 자료도 유지한다.
