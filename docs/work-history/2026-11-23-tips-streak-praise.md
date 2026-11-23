# 2026-11-23 팁·연속 기록·잘한 운전 칭찬 (08 2장, LAN v25)

## 요구

[08 재미 보강](../content-design/08-fun-improvements.md) 2장 중 "팁", "연속 기록", "잘한 운전의 즉시 피드백"만 진행한다. "살아 있는 미터기"는 경제 재조정이 필요해 제외한다.

- 먼저 코드 대조(정차 오차, 급가감속, 방향지시등, 승차감·온도·조용함 판정, 영수증·장부 위치)를 한다.
- 판정 수치 초안을 표로 제안해 확인을 받는다.
- 원칙: 누적 등급·평점 없음, 연속 기록이 끊겨도 벌칙 없음, 같은 운송의 반복 반응은 줄임, 필요한 제동은 불이익에서 제외, 팁은 영수증에 운임과 별도 줄, 수치는 설정 툴에서 조정, 저장 재실행·불러오기로 팁 중복 지급 없음.
- PvP 적용 여부는 구현 전에 묻는다. 사용자는 **"PvP도 적용"**을 골랐고, 이 선택을 수치 초안 승인으로 받았다.

## 코드 대조 결과 (요약)

| 항목 | 위치 | 재사용 |
|---|---|---|
| 급가감속 | `DrivingManners.FixedUpdate`: 0.25초 저역필터 가속도가 0.45초 넘게 `VehicleCatalog.HarshMotion`(가속 > 4.5, 감속 < −6m/s²)에 걸리면 감점. 필요한 제동(`NecessaryBraking`)은 제외. | 같은 필터값과 급가감속 판정을 정차·차선 변경 판정에 그대로 씀 |
| 잘한 운전 | 이미 `StableDrivingProgress`(안정 주행 350m마다 급가감속 감점 회복, 운송당 6점 한도)가 있으나 화면 피드백은 없음 | 유지. 칭찬은 별도 |
| 정차 위치 | `MapStop.CanStop`: 정차점 `Kerb`에서 앞뒤 ±4.5m·좌우 ±1.15m, 시속 2km 미만. 오차를 기록하지 않음 | 같은 정차점으로 오차를 새로 잼 |
| 방향지시등·차선 변경 | `TaxiSignals.direction`(왼쪽 −1, 오른쪽 +1). `ManualIndicatorPolicy`는 옆으로 2.2m 이상·방향 8° 이내를 차선 변경으로 봄. 실제 차로는 `TrafficSystem.EditorLaneId` | 차로 id 변경 + 옆 이동 + 깜빡이 선행으로 판정 |
| 요청(조용함·차멀미·온도) | `PassengerRequestObservation` → `PassengerRequest.GoalOutcome`(1=성공) | 요청 성공을 팁 점수 1점으로 씀 |
| 영수증 | `PassengerResult`(저장), `RideReceiptSummary`, `RideCompletionPanel`. 운임은 `PassengerReward.CalculateCurrentFare` | 팁 필드·별도 줄 추가 |
| 장부 | `CampaignAccount.Post(id,…)`는 같은 id를 거절. 운임은 `fare:<운송 id>`. PvP 점수는 `PvpSimulationLedger.Settle(id,…)`(같은 id 거절) | 팁을 `tip:<운송 id>`로 넣어 중복 지급을 막음 |
| 설정 툴 | `config/content/catalog.json`은 `tools/content-editor/choice-catalog.mjs`의 `generate()` 생성물. 값은 `ContentTuning.Number/Int` | `goodDriving.*`·`tip.*` 21개 추가 후 생성기로 재생성 |

## 실제 구현

- **`GoodDriving.cs`**:
  - `GoodDrivingRules`: 수치, 점수·확률·금액, 운송 id 고정 난수, 손님 한마디.
  - `GoodDrivingTracker`: 물리 틱 관찰로 부드러운 정차와 깔끔한 차선 변경을 판정.
- **`DrivingManners`**: 칭찬(`Praise`), 승하차 정차 칭찬(`AwardStop`), 운송별 칭찬 횟수, 심각 감점 수(충돌·신호 진행·양보·인도·역주행), 운송 중 급가감속 수, 하루 단위 연속 기록, 팁 알림을 관리한다. 이 값들은 저장 스냅숏과 LAN 복제 스냅숏(`DrivingMannersSnapshot`)에 들어간다.
- **`PassengerJobs`**:
  - 승차 때 `AwardStop`을 부른다.
  - 하차 도착 때 `SettleGoodDriving`: 칼정차·부드러운 정차를 판정하고 팁을 정해 `PassengerReward`(저장됨)에 넣는다.
  - 운송 완료 때 `SettleTip`: 연속 기록을 갱신하고, 팁을 장부 `tip:<운송 id>`, PvP 점수 `tip:<운송 id>`, 알림에 넣는다.
  - 시간 초과와 운송 취소(`BusinessProgress.RecordFailure`)는 연속 기록을 0으로 되돌린다.
- **장부**: `CampaignAccount.Tip/Tips`, `EconomyLedger.RecordTip`(하루 수입에 포함), `BusinessProgress.RecordTip`.
- **영수증**:
  - `PassengerResult.tip/tipChance/tipPoints/tipStreak/tipLine`을 저장한다.
  - 완료 패널은 팁이 있을 때 운임 칸 아래에 노란 "팁 (운임과 별도) +N원 · 손님 한마디" 줄을 넣고 아래 요소를 40px 내린다.
  - 기록(F8)의 운송 내역에도 팁 줄을 넣었다. 운임 3줄(`RideReceiptSummary.Money`)은 그대로다.
- **알림·효과음**(`DriveNotifications.ObserveGoodDriving`):
  - 복제되는 상태를 관찰하므로 LAN 참가자에게도 같은 카드가 뜬다.
  - 칭찬은 첫 번째에 카드·효과음(손님 한마디 50%), 두 번째에 효과음만, 세 번째부터 기록만 한다.
  - 팁은 카드와 3음 효과음이다. 연속 기록은 2회째부터 "깔끔한 운송 N회째" 카드를 띄운다.
  - 효과음은 코드로 만든 짧은 종소리다. 시뮬레이션 봇에서는 울리지 않는다.
- **설정 툴**: `goodDriving.*` 12개와 `tip.*` 9개(기본값은 아래 표).
  - 예전 프로필에도 없을 수 있는 항목으로 처리했다(C# `ContentTuning.IsSingleSetting`, 툴 `SINGLE_SETTING`).
  - 기본 프로필 해시는 `1b3928b9…`에서 `9fc35b31…`로 바뀌었다.
- **LAN**: `native-city-lan-v24` → **`v25`**.

### 수치 (기본값)

| 판정 | 조건 |
|---|---|
| 부드러운 정차 | 승차·하차 정차. 직전 8초 안에 15km/h 이상, 마지막 4초 감속 ≤2.5m/s², 멈추기 직전 0.5초 ≤1.2m/s², 급가감속 판정 없음. 정차당 1회 |
| 깔끔한 차선 변경 | 손님을 태운 채 20km/h 이상에서 실제 차로 id가 바뀜(교차로 연결 제외). 1.5초 전 자세보다 옆으로 0.8m 이상, 방향 차이 12° 이내. 1초 전부터 같은 방향 깜빡이, 3초 안 급가감속 없음. 20초 간격, 운송당 2회 |
| 칼정차 | 하차 F 순간 정차점에서 앞뒤·좌우 30cm 이내, 방향 5° 이내 |
| 팁 대상 | 운임 > 0, 기한 안 도착, 만족도 ≥ 요구, 그 운송의 심각 감점 0 |
| 팁 확률·금액 | 20% + 점수(0~6)×10% + 연속×5%(최대 25%), 상한 80%. 100원 + 2점당 100원 + 칼정차 100원, 최대 500원 |
| 연속 기록 | 팁 대상 + 불필요한 급가감속 0회. 하루 단위, 끊겨도 벌칙 없음 |

초안 대비 조정 1건: 차선 변경의 옆 이동 기준을 "2.2m(차선 변경 완료)"에서 "차로 id가 바뀌는 순간까지 0.8m"로 바꿨다. 차로를 넘는 순간에는 아직 2.2m를 다 옮기지 않았기 때문이다.

## 검증

| 검사 | 결과 |
|---|---|
| `GoodDrivingChecks.Run` (Editor) | `GOOD_DRIVING_OK checks=28`. 규칙 수치, 확률 분포(2,000회 중 50% 근처), 같은 운송 id의 팁 고정, 칭찬 감소, 정차 판정 5종(게임 시간 0 포함), 차선 변경 4종(좌·우, 깜빡이 없음, 같은 차로 분할), 장부·PvP 점수 중복 거절, 스냅숏 왕복, 예전 저장, 하루 단위 연속 기록 |
| 설정 툴 Node 검사 `tests/content-editor/*.test.mjs` | 109건 통과. 변경 전 같은 검사는 3건 실패(생성물 불일치)였고, 생성기로 다시 생성하면서 함께 해소됐다. |
| `ContentTuningChecks`·`CurveSteeringChecks`(설정 181·193건 포함)·`BrakingSettingsChecks`·`MenuConditionFollowupTests`·`JunctionRouteFeedbackTests.VerifyMigration` | 모두 통과(v25 계약 포함) |
| 빌드 | `PrototypeBuild.BuildHybrid` 종료 0, `CONTENT_TUNING_BUILD_COPIES_OK`, 108.8MB |


### 실행본 검사 (통합 실행본, 1600×900 창)

| 검사 | 결과 |
|---|---|
| `-goodDrivingCheck` (새 검사 `GoodDrivingProbe`) | `GOOD_DRIVING_PLAYER_OK checks=43`(팁이 나올 때까지 운송을 반복하므로 실행마다 건수가 다르다. 다른 실행은 25건). 실제 배차 수락 → 승차 → 하차 흐름을 확인했다: 승차 부드러운 정차 칭찬, 점수 5(정차 2·차선 1·칼정차·요청 성공), 확률 70%+연속, 팁 400원, 장부 `tip` 별도 항목, 같은 운송 재입금 거절, 영수증 별도 줄, 저장 파일 유지, 알림 카드, 충돌 운송 팁 0·연속 기록 0, PvP 점수 1회 반영. 정차는 실제로 운전하지 않았다. 택시를 정차점에 놓고 부드러운 감속 기록을 판정기에 넣었으며, 차선 변경은 판정 호출로 대신했다. |
| `-drivingSettingsCheck -drivingSettingsPvp` | `DRIVING_SETTINGS_PLAYER_OK checks=22`(PvP 시작·설정, v25) |
| `-brightUiCheck` | `BRIGHT_UI_RESULT PASS checks=102` |
| 화면 | [팁이 있는 영수증](../ui/2026-11-23-tips/receipt-tip.png): 운임 3줄 아래 노란 "팁 (운임과 별도) +400원 · 손님 한마디" 줄 |
| `node tools/content-editor/choice-catalog.mjs . --verify` | 통과(설정 목록·기본 프로필이 생성기 결과와 같다) |

검사 실행은 이 PC의 실제 싱글 저장 슬롯에서 했다(이전 검사들과 같은 방식). 첫 실행에서 연속 기록이 저장에 남아 다음 실행의 기대값이 어긋났고, 검사 초기화에 연속 기록 비우기를 넣었다. 초반 몇 번의 실행본 검사는 PowerShell 배열 인수가 펼쳐져 잘못 실행된 것이라 결과에서 뺐다.

### 구현 중 고친 문제

- 게임 시간(`Time.time`)이 0인 순간(준비 화면 직후)의 정차를 "정차 기록 없음"(−1 표시)과 구별하지 못했다. 실행본 검사에서 발견해 별도 표시값(`HasStop`)으로 고쳤고, Editor 검사에 같은 사례를 넣었다.
- 기본 프로필의 마지막 값을 지우면 검증 오류가 나야 한다는 툴 검사가 있다. 새 선택 항목이 맨 끝에 오면 이 검사가 무력해지므로, 생성기 순서로 다시 생성했다.

## 남은 것 · 수동 확인

- **사람 체감은 확인하지 않았다.**
  - 부드러운 정차 기준(2.5/1.2m/s²)이 실제 키보드 운전에서 너무 쉽거나 어려운지 봐야 한다.
  - 차선 변경 칭찬이 깜빡이를 켠 보통 차선 변경에서 실제로 뜨는지 봐야 한다. 실제 차로 id의 변화는 Editor 합성 입력으로만 확인했다.
  - 칼정차 30cm가 실제로 가능한 정도인지, 효과음 크기와 손님 한마디 빈도도 봐야 한다.
- PvP: 봇 대전과 LAN 두 PC에서 팁 카드·점수 반영을 보지 않았다. 호스트가 판정하고, 참가자에게는 장부·영수증·운전 상태 스냅숏으로 전달된다(코드 경로와 Editor 검사만 확인).
- `-passengerCareTest`는 변경 전과 같은 137번째 "dirty roadside approach…" 검사에서 실패한다(기존 실패, [채팅 알림 기록](2026-11-23-chat-notice-implementation.md)).
- 하루 정산 화면에 팁 합계를 따로 보여 주지는 않는다. 하루 수입에 포함되고 장부에는 별도 종류로 남는다.

## 재현

```powershell
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod GoodDrivingChecks.Run
node --test tests/content-editor/*.test.mjs
node tools/content-editor/choice-catalog.mjs . --verify     # 설정 목록·기본 프로필이 생성기와 같은지
.\builds\EungyeTaxi\EungyeTaxi.exe -goodDrivingCheck -goodDrivingOutput artifacts/tips/good -logFile artifacts/tips/good/player.log
```

**직접 확인(사람)**

1. `builds/EungyeTaxi/EungyeTaxi.exe` → 은계 싱글 → 영업 시작.
2. 호출을 잡고 승차 지점에 천천히 멈춘다(15km/h 이상에서 서서히 감속). 손님이 타면 운행 알림에 "칭찬 · 부드러운 정차" 카드와 종소리가 나는지 본다.
3. 손님을 태우고 20km/h 이상에서 깜빡이(Q/E)를 1초 이상 먼저 켠 뒤 차로를 바꾼다. "칭찬 · 깔끔한 차선 변경"이 뜨는지 본다.
4. 하차 정차 칸의 중앙(바닥 표시)에 맞춰 멈추고 F. 칼정차 칭찬과, 영수증의 노란 "팁 (운임과 별도)" 줄(나온 경우)을 본다.
5. 무위반으로 운송을 이어 가 2회째부터 "깔끔한 운송 N회째" 카드가 뜨는지, 충돌하면 조용히 0으로 돌아가는지 본다.
6. 설정 툴(`설정 관리.cmd`)의 "운임·팁" 묶음에서 수치를 바꿔 체감을 비교한다(다음 새 세션부터 적용).
7. PvP(봇 3명)에서도 같은 카드와 순수익 반영을 본다.
