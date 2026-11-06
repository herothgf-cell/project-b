# 선택지별 단계 효과 — 승인 요구의 구현 기준

사용자 승인: 각 선택지가 즉시/운행 중/운송 종료 효과를 독립적으로 소유하고 만족도·기사 피로의 증감량과 변화율을 각각 수정한다. 0은 추가 효과 없음이다. 게임/관리창/저장/LAN/배포까지 구현한다.

## 공통 의미
- 기존 행동·경로·안전 조건은 유지한다. 임의 스크립트/신규 감지기를 만들지 않는다.
- 선택지 ID는 `request.<Kind>.<PassengerReply>`, `type.<PassengerType>.<index>`, `type.SleepingFollowup.<index>`, `situation.<Kind>.<index>`다. General은 선택지 없음. LostItem은 실제 선택지가 없는 하차 후 사건이므로 제외/참조 유지.
- 설정 키는 `choice.<choiceId>.<phase>.<field>`다. `choice.effects.version=1`을 가진 schemaVersion2 프로필만 새 엔진을 사용한다. 기존 schema1 snapshot은 기존 동작으로 복원한다.
- immediate: satisfaction/fatigue(점), satisfactionPercent/fatiguePercent(%). 변화량 = 점 + 적용 직전 현재값×%/100. 적용 후0~100으로 제한. 정확히1회.
- during: satisfactionPerMinute/fatiguePerMinute(점/분), satisfactionPercentPerMinute/fatiguePercentPerMinute(선택 활성화 때 기준값 대비 %/분). 변화율 기준값은 고정해 프레임 분할/이어하기와 무관하게 누적한다. fatigueGrowthPercent는 자연 피로 증가율에 추가하는 %(0=기존,−100=자연 증가 억제). motionPenaltyDelta는 불필요한 급가감속 M 감점에 추가할 점수다(0=추가 없음).
- end.success/end.failure/end.neutral: 각 immediate와 같은4필드. 기존 요청 실제 이행/실패/판정불가를 따르며, 보류는neutral, 유형/상황은 정상 운송 완료시success다. 지원하지 않는 결과 가지는 카탈로그/화면에 만들지 않는다. 취소/사고/견인/기한 실패 중 운송 중단에는 종료 보상을 주지 않는다. 기한 지난 뒤 정상 하차는 실제 기존 요청 판정을 따른다.
- ‘증가율0’은 기존 게임 진행 자체를 멈추는 것이 아니라 해당 선택지의 추가 변화가 없다는 의미다. UI에 단위를 명시한다.
- 즉시/운행중 만족도·유형/상황 종료 효과는 기존 요청 종료 합계 상한과 별도 기여다. 요청 종료 만족도는 선택지마다 점+변화율의 결합 결과를 −100~100점으로 제한해 원시 소수값을 보존하고, 모든 요청 종료 효과와 미응답 결과를 합산한 뒤 한 번 반올림(0.5는 절댓값이 커지는 방향)하여 기존 ±common.requestCap 상한을 적용한다. 피로/S는0~100, M 감점은0이상으로 제한한다.
- 승차 전 유형 응답은 승차 확정 때 즉시 효과를1회 적용하고 운행 중 효과를 활성화한다. 게임 영업 경과 시간을 기준으로 진행하며 정지 중에도 운송이 진행되면 적용한다. 메뉴 일시정지/도착 확정 이후/취소 후에는 진행하지 않는다.

## 기본값·이관
현재 기본 동작을 보존한다. 새로운 증감/율 기본0. 요청의 각 수락 답변 end.success.satisfaction은 기존 request.Kind.success, 실제 실패 지원5종(Comfort/OnTime/SafeStop/DestinationChange/ViaStop)은failure, 판정불가 지원 경로는neutral0, Defer는neutral0이다. ETA 세 답변/RejectIllegal은success0을 독립 편집할 수 있게 한다. Food 허용은 immediate.satisfaction=기존effect.food.satisfaction(5), 기존Food 별도 기여를 새모드에서는 중복 적용하지 않는다. Comfort Accept의motionPenaltyDelta=기존manner.comfortMotion.penalty−manner.motion.penalty(기본2), 새모드에서는 기존Comfort강화 분기를 중복 적용하지 않는다.

기존 request success/failure, effect.food.satisfaction, manner.comfortMotion.penalty는 호환용 값으로 남되 새 화면에서는 이관 원본/참조임을 표시한다. 새 효과는 이것과 독립 편집한다. 기존 프로필은 원본을 쓰지 않고 메모리에서 검증 후schema2로 이관한다. 저장시 기존revision/이관된 의미hash로 충돌 검사하고 원본백업을 보존한다. 기존 저장 snapshot을 읽을 때는schema1을 유지해 운행 중 보상이 새로 지급되지 않게 한다. 새 세션의 구 active 프로필은 보존 이관한다.

## 순수 코어 고정 인터페이스
`ChoiceEffects.cs`(Unity참조 없음) 담당:
- `ChoiceEffectDefinition.Read(string choiceId)`는 ContentTuning의 해당키를 읽고, `Describe(string choiceId)`는 실제 단위로 짧은 한국어 효과 설명을 반환한다. `Enabled`는version키1 여부.
- `ChoiceEffectLedger` 직렬화 가능한 상태 소유. `Select(string eventId,string choiceId,double satisfaction,double fatigue,bool activate=true)`는 선택1회(같은event 재호출은무변화, 다른choice면거부). `ActivatePending(double satisfaction,double fatigue)`는 승차 전 선택을 활성화. 반환 `ChoiceEffectDelta`의 fatigue를 호출자가 기사 피로에 적용한다. S기여는 ledger에 보존.
- `Advance(double seconds,double satisfaction,double fatigue)`는 활성 미완료 효과의 시간 기여를 누적한다. `Complete(string eventId,int outcome,double satisfaction,double fatigue)`는 1success/−1failure/0neutral 결과를1회 적용한다. `CompleteNonRequests(...)`는 정상하차 때 유형/상황을 선택순서대로 완료한다. `Cancel()`은 종료 보상 없이 활성 효과 중지.
- `SatisfactionDelta`(int): 즉시/운행중/비요청 종료 기여. `RequestEndDelta`(int): 요청 종료 기여(상한 적용 전). `RequestEndFor(eventId)`로 해당 요청 평가량 조회. `MotionPenaltyDelta`(int), `FatigueGrowthPercent`(double): 활성 효과 합계. `Copy()`, `Validate()`는 깊은복사/손상·중복·비유한 상태 거부.
- `ChoiceEffectDelta` 필드 satisfaction/fatigue는 double. 피로 실제 변화는 호출자가0~100에 제한. 엔진 자체도 순서대로 현재값 제한을 반영해 다음percent 계산이 일관되게 한다. `Select/Advance/Complete/ActivatePending/CompleteNonRequests` 모두 delta를 반환. 새 설정이 모두0이면 delta0이고 상태의 멱등성만 유지한다.
- eventId는 trip 식별자 포함: `trip:request:<Kind>`, `trip:type:<Type>:initial`, `trip:type:Sleeping:followup`, `trip:situation:<Reactions>`.

## 설정/프로필 인터페이스
catalog 포맷schemaVersion1은 유지하고 프로필은2를 지원한다. 선택지 목록/유효end결과 생성기는 `tools/content-editor/choice-catalog.mjs`로 독립 작성한다. default/catalog와 UnityResources 사본을 결정적으로 생성/검증한다.
- JS `migrateProfile(profile,catalog)` export: 먼저 입력schema의 엄격 검증·원래hash확인, schema1이면legacy값으로 새키를 채워schema2/hash갱신, schema2는검증후clone. 구revision/id유지. 구파일 자동 덮어쓰기 없음.
- C# `ContentTuning.UpgradeJson(string)` 같은 이관. `UseJson`은schema1/2를 모두 엄격히 읽고버전유지, `UseDefaults`는choice카탈로그면schema2, 기존tinyfixture면1. `SchemaVersion`읽기속성 제공. hash헤더는 schema1이면기존content-profile-v1, schema2면content-profile-v2.
- 새프로필은240KiB 제한안에 맞춘다. 추가키를 뺀schema1만 허용하며schema2의 누락은거부한다. 알수없는키/중복/범위/비유한숫자거부유지.

## 런타임·저장·검증
PassengerJobs 별도 partial이 ledger/영업시간 시계/즉시 피로 적용을 소유한다. 요청 응답/유형 응답/상황 응답의 실제 성공 경로에서만 선택 기록한다. 요청 종료 실제판정은 별도 outcome으로 전달한다(점수가0이란이유로실패로추측금지). Request.SatisfactionDelta는새모드의요청종료결과로갱신해기존합계상한/요약을유지한다.

PassengerJobsSnapshot에는presence/version/ledger/마지막영업시각을 직렬화하고 validator에연결한다. 구snapshot은현재운송legacy유지, 다음새운송부터활성화가능. DriverCondition에자연피로modifier를연결하되ledger가유일한원본이며복원/취소시재계산한다. NativeLAN은호스트만적용하고새snapshot을전달, 프로토콜v9로서로다른실행본을명시거부한다. 저장·재접속·봇·미리보기·실제Player회귀를검증한다.
