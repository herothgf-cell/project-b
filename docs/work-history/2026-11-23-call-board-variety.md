# 2026-11-23 호출판 변주와 판단 재료 (08 3장 1단계, 은계 싱글)

## 요구

자리 비움 중 일괄 진행 요청의 1단계. [08 재미 보강](../content-design/08-fun-improvements.md) 3장 "호출판 변주와 판단 재료".

- 호출 카드에 픽업까지 거리, 예상 소요 시간, 하차지 주변 수요(높음/보통/낮음)를 표시한다.
- 한 호출판에 성격이 다른 호출(무난한 근거리, 기한 짧은 고액, 큰 짐, 목적지 미공개 길가 손님)을 섞는다.
- 연속 배차는 제외. 은계 싱글만 대상이고, PvP 규칙과 공유 주행 물리는 건드리지 않는다.

## 코드 대조

- 호출 생성은 `PassengerJobs.MixOrdinaryOffers`다(일반 후보 순위, 첫 출차, 재회, 시나리오 1건을 3칸에 배치).
  - 운임은 `3000 + ceil(거리×4/100)×100`, 기한은 `OneDayOfferPolicy.TransportSeconds`(긴급도 0~2)다.
  - 호출별 운임 배율은 없었다.
- 카드는 `MapDestinationUI.DrawSidebar`가 그린다. 기존에는 출발·도착·예상 운임만 보였다. `DriveHUD.OfferText`, `PassengerJobs.FuelHint` 같은 문구 함수는 있었지만 카드에서 쓰지 않았다.
- 수요는 `PassengerDemandPolicy`(지역 주거/업무/상업/중립, 시간대별 우대 3배)에 있다. 위치 기반 수요나 높음/보통/낮음 구분은 없었다.
- 승객 유형은 승차 순간 `VehicleCare.ReservePassenger`로 정해진다(호출과 무관). 짐 손님은 트렁크 없는 차에서 빠진다.
- 저장: `PassengerJobsSnapshot`이 `JobOffer`를 그대로 저장한다. 그래서 새 필드에 기본값만 주면 기존 저장이 열린다.

## 실제 구현

- `JobOffer.variety`(0 일반, 1 근거리, 2 급한 고액, 3 큰 짐, 4 목적지 미공개). 기본값 0이라 예전 저장은 일반 호출로 열린다.
- `PassengerJobsCallBoard.cs`(새 부분 클래스):
  - `ApplyOfferVariety`: 새 호출판을 만든 직후 한 번 부른다.
  - 대상은 시나리오·재회가 아닌 일반 호출이고, 첫 출차 안내 중에는 붙이지 않는다.
  - 성격 4종의 순서를 호출판 시드로 돌리고, 호출마다 조건에 맞는 첫 성격을 하나씩 붙인다. 같은 판에서 같은 성격은 반복되지 않는다.
- 카드 문구 `CallCardCaption`: [성격] / 승차지 → 하차지(또는 "목적지 비공개 (탑승 후 안내)") / 예상 운임·기한 / 픽업 거리·약 N분·하차지 수요.
- 목적지 미공개: 탑승 전까지 카드, 선택 상태 줄, 하차 포커스 버튼, 지도의 하차 핀에서 하차지를 숨긴다. 탑승하면 기존 안내가 그대로 나온다.
- 큰 짐: 승차 손님 유형을 큰 짐으로 정한다(`VarietyPassengerType`). 기존 유형 풀에 없으면 일반으로 되돌리는 규칙에서 이 경우만 예외로 둔다.
- 설정 툴 "호출판" 묶음 7개(`offer.*`). 예전 프로필에서도 빠져 있어도 되는 항목으로 처리했다.

### 판단 기록 (임의로 정한 값)

| 항목 | 값 | 이유 |
|---|---|---|
| 하차지 수요 구분 | 시간대 우대 지역=높음, 중립=보통, 그 밖=낮음 | 기존 수요 정책의 3배/1배 가중치를 그대로 쓰는 가장 단순한 구분 |
| 예상 소요 | 픽업 ÷ 4m/s + 안전 운송 시간 + 신호 대기 | 기존 계획 화면(`OperationForecastAdapter`)과 같은 관례 |
| 근거리 기준 | 픽업 ≤450m, 운송 ≤1,000m | 첫 호출판 9건 중 짧은 운송 수준. 검사에서는 차고 출발 기준 픽업이 587m 이상이라 근거리가 한 번도 붙지 않았다. 체감 확인 후 조정 필요 |
| 급한 손님 | 기한 ×0.75, 운임 ×1.4 | "기한 짧은 고액"을 기존 긴급도(급함 1.2배)보다 한 단계 더 짧게, 운임은 위험만큼 높게 |
| 큰 짐 | +800원 | 짐 손님의 준비·하차 시간 보상 수준 |
| 목적지 미공개 | 운임·기한 변화 없음 | 정보가 없는 것 자체가 선택 재료. 추가 보상은 체감 후 결정 |

기존 지각 0원 규칙은 바꾸지 않았다. 급한 손님도 기한을 넘기면 0원이다.

## 검증

| 검사 | 결과 |
|---|---|
| `CallBoardChecks.Run` (Editor) | `CALL_BOARD_OK checks=6`(기본값, 문구, 예전 호출의 기본 성격, 저장 왕복) |
| `PlaytestBalanceTests`·`ContentTuningChecks`·`GoodDrivingChecks` | 통과 |
| 설정 툴 Node 검사 109건, `choice-catalog.mjs --verify` | 통과 |
| `-callBoardCheck` (새 실행본 검사 `CallBoardProbe`) | `CALL_BOARD_PLAYER_OK checks=111`. 호출판 12세대에서 성격 중복 없음, 카드에 거리·시간·기한·수요, 미공개 호출은 하차지와 수요가 안 보임, 급한 호출 긴급도·운임 단위, 큰 짐 호출 승차 시 짐 손님, 저장 왕복을 확인했다. 관찰된 성격은 큰 짐·급한 고액·미공개(근거리는 위 이유로 없음). [화면](../ui/2026-11-23-call-board/call-board-1920.png) |
| `-contentTest -offerVarietyTest` (기존) | `OFFER_VARIETY_PLAYER_OK checks=968` |
| `-brightUiCheck` | `BRIGHT_UI_RESULT PASS checks=102` |
| `-contentTest -worldFollowupTest` (기존) | 실패(`ServiceStops.FuelAmount/AttentionFor` NullReference). **변경 전 커밋(HEAD)의 실행본도 같은 오류로 실패**함을 따로 확인했다. 이전 기록에 남은 기존 시험 장치 문제다. |

검사 첫 시도에서 `ResetOffers()`가 호출판 세대를 0으로 되돌려 매번 같은 판이 나오는 것을 발견했다. 검사를 처음 한 번만 초기화하도록 고쳤다(게임 코드 문제가 아님). 또 길목 구역이 붙은 일반 호출을 대상에서 빼면 성격이 거의 붙지 않아서, 시나리오만 빼도록 대상을 넓혔다.

## 남은 것 · 직접 확인

- 사람 플레이 체감은 확인하지 않았다(카드가 길어진 가독성, 성격 선택이 실제로 고민되는지).
- 근거리 호출의 기준 거리 조정 여부.
- 목적지 미공개 호출에 추가 보상을 줄지.
- 운행 중 HUD 일부가 승차 전에도 하차지를 보여 줄 가능성: 카드·지도 핀·하차 버튼은 숨겼지만, 다른 HUD 줄은 전부 점검하지 못했다.

## 재현

```powershell
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod CallBoardChecks.Run
.\builds\EungyeTaxi\EungyeTaxi.exe -callBoardCheck -callBoardOutput artifacts/call-board -logFile artifacts/call-board/player.log
```

게임에서: 은계 싱글 → 영업 시작 → M → 오른쪽 "호출 목록" 카드를 본다. 성격 표시와 거리·시간·수요, 미공개 호출의 하차지 숨김을 확인한다.
