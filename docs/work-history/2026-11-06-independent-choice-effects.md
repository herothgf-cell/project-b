# 2026-11-06 선택지별 독립 단계 효과

## 승인 요구
각 선택지의 즉시 / 운행 중 / 운송 종료 후 효과를 게임 시스템과 별도 관리창에서 독립적으로 수정한다. 승객 만족도 S와 기사 피로의 증감량·변화율을 각각 설정하며 0은 추가 효과 없음으로 처리한다. ETA 각 안내와 공감의 듣기/조용한 배려도 별개다.

## 구현
- 요청39·승객 유형26·상황6, 실제71개 선택지에 단계 효과를 추가했다. 즉시/종료는 점 + 현재값 대비 %, 운행 중은 점/분 + 활성화 때 고정한 기준값 대비 %/분이다. 자연 피로 증가율 추가와 불필요한 급가감속 M 감점 추가도 따로 편집한다.
- 선택은 호스트 장부에1회 기록한다. 승차 전 응답은 승차 확정 때 활성화한다. 메뉴 일시정지/도착 확정/취소 뒤 시간 효과를 중지하며 취소에는 새 종료 보상을 지급하지 않는다. 도착 확정에서 실제 기존 요청 판정의 성공/실패/판정 불가를 사용한다.
- Food 허용의 기존5점과 Comfort의 기존 강화 감점을 중복하지 않는다. 이전 공유 점수는 이관 원본으로 남기고 새 편집창에서는 읽기 전용이다. 요청 종료 효과는 개별 합성값±100 제한 → 소수 합산 후 반올림 → 공통 합계 상한을 적용한다. 즉시/운행 중/유형·상황 종료 기여는 요청 종료 상한과 별도다. 최종 S와 피로는0~100이다.
- 프로필schema2, 엄격한 전체 키/범위/hash 검증, schema1 원본 보존 메모리 이관과 저장 전 원본 백업을 구현했다. 기존 이어하기의schema1 설정은 유지한다. 새 세션의 구 active 프로필만 메모리에서 이관한다.
- 선택·활성화 기준값·경과 시간·완료 결과·원래 운송 호환 표식을 snapshot에 보존한다. Unity JSON의 빈 중첩 객체와 명시적 presence를 구분하고 실제 답변/운송/유형과 효과 ID의 연결을 검증한다. 저장·복원은 즉시 효과를 다시 지급하지 않는다.
- 관리창은 답변마다3단계 편집/단위/적용 시점/영향 대상을 표시한다. 실제 C# 코어 미리보기에서 초기 S·피로·경과 시간·종료 판정과 단계별 결과를 확인한다. 긴 효과 설명의 게임 대화 패널은 화면 안에서 세로 스크롤한다.
- 보행자 내구도 후속 소스9275f2a/배포6e73f0f를 보존 통합하며 새 계약은LANv9다. 다른 PC도 같은 실행본이 필요하다. 인터넷 중계/새 감지기/임의 행동 스크립트는 추가하지 않았다.

## 변경 파일
`ChoiceEffects.cs`, `PassengerJobsChoiceEffects.cs`와 기존PassengerJobs/Request/Behavior/Situations/DrivingManners/DriverCondition/BusinessProgress/콘텐츠codec·미리보기·LAN을 연결했다. `config/content/`와Unity Resources 사본, `tools/content-editor/` 모델·화면·서버·이관, 관련 순수/Editor/Player/LAN 검사와 실행기를 갱신했다. 설정 가이드·승객/대화 기획·scope/checklist/CURRENT-STATE/LATEST를 함께 갱신한다.

## 실제 검증과 조사
- 새 코어50, 기사 피로6, 콘텐츠643검사와 C#/Node Unicode·이관hash 동등성 통과. 콘텐츠 미리보기의 실제 C# 소스+Unity stub29검사 통과. stub 검사는 실제 Unity Player 검사와 구분한다.
- 관리창/프로필/서버/manifest Node55검사 통과 후 사용자 active를 출하 목록에서 제외하는 생성 회귀를 추가했다. 최종 Node 합계56개 모두 통과했다.
- 실제 브라우저에서 독립 입력·다른 답변 불변과 코어 계산을 확인했다. 초기S70/피로20, 즉시S+3/F+2·60초 동안S+6·종료S+5의 결과는S84/F22다. 자연 피로와 다른 선택/주행 사건을 포함하지 않는 단독 계산이라고 표시한다.
- 후보 BuildHybrid exit0. 실제 싱글 Player54검사 exit0: 별도 ETA Normal/Faster 운송, 실제 Accept/Interact/Choose, 즉시 S/F·분당 효과·자연 피로 배율, 실제 저장→메뉴→이어하기 무중복, 종료 시 현재S 대비 %와 요청 종료 합계 상한, 실제 하차와 취소를 확인했다. 위치/영업시계 제어 검사이며 자동 물리 주행/사람 전체 캠페인이 아니다.
- 초기 Player 이어하기 실패는 legacy observation의 빈 observations 배열을 실제 새 관찰 목록으로 취급한 검증 오류였다. snapshot.version 분기로 수정하고 실제 재검사를 통과했다. 빈 ledger의 presence·이전 픽업 운송 호환과 정확한 소수 S도 Editor 회귀로 추가했다.
- 후보 실제 Unity Editor72·콘텐츠32·빌드사본3 통과, 통합 BuildHybrid exit0. 패널은1280×720/1920×1080 및 배율1/1.5/2에서 화면 경계 회귀를 확인했다.
- PVP 초기검사에서10초 NGO 승인 제한이23초 월드 구성보다 짧아 접속이 종료되는 것을 확인해 기존120초 ready 예산과 맞췄다. 212KB room 전송 동안 기존128패킷 큐의PONG 실패가 재현되어1024로 늘렸다. 그 뒤 client world bind가 NGO 연결 승인 완료보다 앞서 ready를1회 보낸 상태를 진단해, 승인 후 첫 own frame까지1초 간격 재전송하고 호스트 ready 처리를 멱등화했다. 서로 다른 프로필의 두 실제 Player에서 host60/client76검사·재접속·효과/기준값 복제·반복snapshot 무중복·기존 사고 내구도 복제까지 exit0. 실패한 초기 시도는 통과에 합산하지 않는다. 직접 주행 입력/바퀴 권한과 효과 모델 fixture를 구분하며 다른PC 검사는 아니다.
- 기존 순수 회귀의 대표 요청 후속 문구/깨어 있는 안심 대사/수동 제동8.823529 실패3건은 수정하지 않은 기준 소스에서도 재현했다. 이번 선택지 효과 통과와 별도로 남긴다. 그 밖의 passenger care13301·vehicle damage191·operation49/adapter·rest40·slow turn21·traffic demand38 및Prototype 나머지 묶음은 통과했다.

## 실행·재현
루트 `설정 관리.cmd` → 요청/상황 → 답변 → 즉시/운행 중/운송 종료 효과를 펼친다. 검증·저장 후 게임에 적용하면 다음 새 싱글 캠페인 또는 새 PVP 방부터 사용한다. 기존 이어하기는 저장 당시 설정을 유지한다. 기본 게임은 `builds/EungyeTaxi/EungyeTaxi.exe`, Unity 빌드는 `PrototypeBuild.BuildHybrid`다.

자동 검사는 `tools/test_choice_effects.ps1`, `test_choice_driver.ps1`, `test_content_tuning.ps1`, `test_content_preview.ps1`, `test_choice_effects_player.ps1`, Node의`tests/content-editor/*.test.mjs`와`tests/content-manifest.test.mjs`를 사용한다. Editor 묶음은`ChoiceEffectsAcceptanceChecks.Run/Build`, LAN 효과 fixture는`test_pvp_native_lan.ps1 -Reconnect -ChoiceEffects -ContentHostProfile <호스트> -ContentClientProfile <다른프로필> -ExpectedContentHash <호스트hash>`다. 실제 대화 스크롤은 `-contentTest -choiceEffectsDialogueCheck`로 격리된 시각 검사 창을 띄운다.

## 배포 영수증·남은 검증
최종 소스 통합·Unity 빌드·PVP·사용자 파일/Strict·manifest 검증은 진행 중이다. 완료한 명령과 실제 커밋은 여기와LATEST에 후속 기록한다. 사람 장시간·전체3일·다른PC4인/실물패드 수용은 이번 자동 검사로 대체하지 않는다. 지도 입력/ZIP·OSM 대조 설정·사용자 저장/프로필은 변경하지 않는다. 원래 외부 관리 worktree의 미커밋 변경도 보존한다.
