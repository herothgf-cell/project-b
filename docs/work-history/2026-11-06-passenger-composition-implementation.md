# 승객 프리셋·특성 조합·관리창 — 2026-11-06

## 요구사항과 범위

사용자가 이전 검토의 프리셋+특성 조합을 선택하고 상세 기획/기존 승객 분해/요소별 추가 후보/효과표/화면 도식과 실제 툴 업데이트를 요청했다. 기획만 작성하지 않고 관리창의 제작·저장·게임 적용과 실제 운송의 조합 실행을 연결했다.

## 실제 구현

- 기본12프리셋(일반+11특수), 프리셋 ID·이름·소개·사용·그룹·가중치·특성 목록. 기존 유형 enum은 배정 호환/행동 실행용이며 새 프리셋은 enum 추가 없이 제작한다.
- 독립 음주/조용함 + 주 행동1개(수면/대화/소지품) + 대표 요청1개(승차감/목적). 미지원 조합은 JS/C# 양쪽에서 명시적으로 차단한다.
- 이름/소개 개별 편집, 실제 행동·대사·71선택 효과 공유. 소개가 작업/안전 경고를 대체하지 않으며 명시적 quiet에서도 흔들림/필요제동·재고정 안내 유지.
- 취객+큰짐은 취객 하루 한도와 짐 적재/하역을 독립 처리한다. 대표 요청은 실제 가용성·기존0~2슬롯·경로 상충 규칙을 유지한다. 비취객 빈/불가 그룹은 추첨에서 제외하며 취객 별도 추첨/최종 실패는 무특성 일반 폴백.
- `passenger.presets` 문자열 설정, schema1/2 구프로필 키 부재 허용·원래 hash/snapshot 보존, 명시적 이관에서만 기본값 추가. 후보 예약seed/프리셋·특성/작업 상태 저장, 재수락/복원 중복 방지.
- LANv12로 신규 프로필/상태 호환을 구분. 구v11과 접속하지 않으며 양쪽 실행본을 갱신한다.
- 관리창 새 승객/복제/삭제·한국어 특성·실시간 조합 오류·공유 효과 수치/링크·저장/재로드·기존 프로필 입출력. 예제3종은 `config/content/presets/passenger-composition-examples.json`; 출하 기본값/사용자 활성 설정에 자동 적용하지 않는다.

상세 기획/기존12매핑/확장 후보/효과/화면 도식/제작 예시: [기획15](../content-design/15-passenger-presets-and-traits.md). 현재 지원과 미구현 장치·메스꺼움·초상·단골 등 후속을 구분한다.

## 변경 파일

- 기획15, 콘텐츠02/07/README, 툴가이드, LAN30, 범위·체크리스트, 상세설계/계획, 이력/LATEST.
- `tools/content-editor/` 프리셋 정의/검증·프로필 이관·생성기·서버·UI/CSS.
- `config/content/`와 Resources 기본 catalog/profile 및 별도 예제 프로필.
- `ContentTuning.cs`, `PassengerBehavior.cs`, `PassengerJobs.cs`, `PassengerRequestUI.cs`, `PassengerSituations.cs`, `VehicleCare.cs`, `PassengerBehaviorUI.cs`, `PassengerBehaviorProbe.cs`, `PvpNativeLan.cs` 및 호환 검사3개.
- 순수C# 조합검사/실행기, Node프리셋/API/공통픽스처, 실제 UI PNG3장.

## 검증

각 결과는 실행 명령/보고서로 구분한다. 모델 시험은 수동 주행이 아니다.

| 검사 | 실제 결과/증거 |
|---|---|
| Node 전체 | `node --test tests/content-editor/*.test.mjs` 73/73 exit0. `artifacts/passenger-composition/node-tests.log`. 최초 loopback 서버검사는 sandbox EACCES4건, 같은 명령 권한 확장 후 모두 통과 |
| 조합 순수 모델 | `tools/test_passenger_composition.ps1` 73 PASS. JS/C# 공통17픽스처·기본 동등성·이관/해시·조합·안전·저장 |
| 콘텐츠/취객/휴식 | ContentTuning760 및 C#/Node 해시2건, care13301, rest40 exit0 |
| 독립 선택 효과/미리보기 | ChoiceEffects50 / ContentTuningRuntime preview29 exit0. 잘못된 입력 fixture의 기본값 폴백 경고는 의도된 검사 |
| 지도 필수 입력 | Strict exit0/issues0. 지도/ZIP 원본 변경 없음 |
| 통합 빌드 | `ContentTuningBuild.Build` 최종 로그 `build-final.log`: runtime32/build3 및 통합 BuildHybrid PROTOTYPE_BUILD_OK/return code0. 첫 실행은 호환 버전 상수/검사 변경의 컴파일 시점 차이로 exit1, 일치시킨 재컴파일 통과 |
| 실제 조합 Player 1차 | 격리 저장/위치 제어14검사 PASS. 적재 중 복원·취객 한도·승차·하역·정산 확인. 후속 확장 fixture 결과는 아래 최종 영수증 |
| 실제 관리창 | 별도 `passenger-composition-example` 생성→취객 복제→음주+조용함+짐+경유→검증→저장→재로드 성공. 졸림 추가 시 충돌 표시/검증 거부 확인. 기본/활성 사용자 프로필을 덮어쓰지 않음 |
| 화면 | IAB 1280×900 / 800×900 실제 열람·캡처. [편집](../ui/2026-11-06-passenger-composition/editor-1280.png), [효과](../ui/2026-11-06-passenger-composition/effects-1280.png), [좁은 창](../ui/2026-11-06-passenger-composition/editor-800.png) |

기존 실패2건: `test_passenger_behavior.ps1`의 `followup remembers actual request response Comfort`, `test_passenger_situations.ps1`의 `awake passenger receives typed neutral reassurance`는 변경 전 HEAD PassengerBehavior로 따로 컴파일해 동일 실패를 재현했다. 전자는 요청 답변 문자열 미포함, 후자는 이전 대사의 검사 기대 문자열 차이이며 이번 작업에서 수정 완료라고 표시하지 않는다. baseline 자료는 `artifacts/passenger-composition/Legacy*`.

독립 검토에서 실제 프리셋 요청 가용성 대신 구그룹으로 걸러지던 부분, quiet가 안전 반응까지 막는 부분, 구토 청결 설명과 조용함 요청 감점 혼동을 수정했다. 최종 재검토의 빈 그룹 사전 제외/비중 재분배는 상세 기획과 일치시켰다.

## 실행·재현 및 미검증

1. `설정 관리.cmd` → 승객 프리셋. 가져오기 예제 파일 선택 또는 새 프로필/승객 생성.
2. 검증→저장→게임에 적용. 새 싱글/새 PVP방에서 적용, 기존 이어하기는 저장 당시 콘텐츠.
3. 실행 `builds/EungyeTaxi/EungyeTaxi.exe` / 은계 싱글·A2 PVP / `PrototypeBuild.BuildHybrid` / LANv12.
4. 실제 조합 fixture: exe에 `-oneDayMode -hybridV2 -passengerBehaviorTest -passengerCompositionTest -batchmode -nographics -logFile <로컬로그>` 전달. `artifacts/passenger-composition/player.txt`의 신선한 PASS 확인. 사용자 저장 대신 artifacts 경로인지 fixture에서 검사한다.

로컬 Unity 설치: `D:/Program Files/Unity Hub/Editor/2022.3.62f2/Editor/Unity.exe`. 다른PC 설치 경로로 고정하지 않는다. 수동 물리 주행·사람 장시간/전체3일·다른PC LAN 검사는 미수행이며 자동 위치 제어/loopback 검사와 구분한다.

작업 시작의 기존 미추적 라이선스·마인드맵·과거 이력/도구는 보존한다. 작업 중 반영된 HUD/대화 가독성 소스c8f5d89·빌드7dc84b6·문서568892b도 보존하며 최신 소스를 포함해 빌드했다. 별도 승객 예제만 새로 저장했고 실제 활성 프로필은 그대로다.

## 최종 영수증

- 최종 `build-acceptance.log` BuildHybrid/PROTOTYPE_BUILD_OK 및 return code0. production source는 앞선 LAN 검사와 동일하며 마지막 빌드는 추가 수용 fixture를 포함한다.
- 최종 실제 Player `player-final.log` / `player.txt` **42검사 PASS**. 구토 경고/발생→즉시호출잠금→복원→청결10이하 하차, 음식+Comfort와 오염짐+ViaStop의 실제 요청슬롯 연결/복원/오염/정산 중복 방지. 위치 제어 lifecycle 검사이며 직접 물리 운전/경유 전구간 수동 이행 아님.
- `test_pvp_native_lan.ps1 -Port18137 -ContentHostProfile config/content/presets/passenger-composition-examples.json -ExpectedContentHash <실제예제hash> -TimeoutSeconds180`: 호스트41/참가자52 PASS, 실제 native입력 약19.6m, 신규 프로필 hash 복제. `artifacts/pvp/native-lan-20261106-235138-fd9aef25`. 같은PC loopback·재접속 옵션 없음, 다른PC 검증 아님.
- 기존 사용자 저장29파일 SHA256 변화0. 새 승객 예제는 별도 프로필로 저장, 실제 활성 프로필 미변경.
- 생성기 `node tools/content-editor/choice-catalog.mjs D:/Git/project-b --verify` exit0(선택71/항목1385/프로필134694바이트). 최초 root 인자 없이 `--verify`를 전달한 검사는 경로 ENOENT로 실패해 올바른 인자로 재검증했다. 예제15프리셋 hash 검증/새 문서 상대링크/diff 검사 exit0.
- 독립 최종 리뷰의4개 P2 수정 및 구그룹/실제 대표 요청·quiet 안전 안내·확률/청결 설명 재대조 완료. 남은 신규 P1/P2 없음; 위 기존 검사 실패2건 유지.
- 소스 및 배포의 실제 commit·manifest 전수검증은 아래 출하 영수증에 기록한다.
