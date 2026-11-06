# 승객 독립 행동·신규 특성 확장 — 2026-11-07

## 요청·범위

사용자 승인: 수면+음식/짐 조합, 요청 실패 감지, 메스꺼움·온도 민감·젖은 우산을 기획/관리 툴/게임에 구현하고1차 대사·선택·효과를 검증한 후 main push. 동시 비/운전설정 작업 보존을 위해 별도 managed worktree의 codex/passenger-traits-expansion에서 진행했다.

## 실제 구현

[요소별 기획19](../content-design/19-passenger-trait-expansion.md)를 기준으로 기존24+신규6=30/활성29를 제공한다. 수면은 소지품 Type과 독립이며 잠든 동안 섭취/흘림·선택을 억제한다. 일반짐은 기상/하역 완료 후 정산; 오염짐은 기존 보호/오염 처리(별도 하역시간 없음)를 유지한다. 메스꺼움은 고정 예정확률·이동/거친조작 누적·정차휴식/봉투·1회 오염, 온도는 선호/설정/실제 유지시간·수정 결과 교체, 우산은 탑승 전 감싸기/그대로와1회 오염을 연결했다.

Quiet 수락 후 실제 운전자 선택적 잡담 입력만 새 위반으로 감지한다. 필수 안전 안내 제외, 위반 근거/대사/쿨다운/종료 효과 저장, 반복 정산 방지. 기존 Comfort/경유/정시 등 관찰 판정은 유지한다. 전체 모든 요청에 가상의 실패 조건을 추가한 것은 아니다. 실제 경적·외부 날씨·물리 냉난방 시스템을 승객 구현으로 주장하지 않는다.

편집기 특성 탭·조합 검증·공통/개별 대사/확률/수치·가져오기 이관을 확장했다. 구프로필은 기존 승객 목록과 당시 효과 보존; 새 항목 묶음 전부 누락만 호환하고 부분 누락은 오류. C#/Node 이관 해시 일치. 새 기본 profile 및 Resources, 가져오기 passenger-roster-v2 포함. 짧은길/빠른길 기본 제외 유지.

## 변경 파일군

- PassengerBehavior/ContentTuning/PassengerJobs/PassengerJobsChoiceEffects/PassengerSituations/BehaviorUI: 독립 상태·안전정차·스냅샷·효과.
- PassengerRequest/Observation/UI/PvpNativeLan: Quiet 위반·사용자 입력·저장·호스트 권한·새 계약 버전.
- 카탈로그/기본프로필/roster-v2/Resources, tools/content-editor의 설정/화면/검증/이관.
- C#/Node 회귀 및 신규 검사, 실제Player probe와 빌드 계약 검사.
- 기획02/07/15/16/17/19·툴안내·범위·체크리스트·이력 및 통합 배포 실행본/manifest.

## 중간 검증·발견 수정

- Node97/97, C# traits46/behavior537/composition73/overrides42/roster1933/situations82/rest adapter41/care13301/choice50/content751 및 C#/Node hash 통과(최종 통합 후 결과는 아래 영수증).
- 독립 검토 발견: 복합 수면 후속 효과ID의 저장검증 불일치, 편집기4000자와 저장500자 불일치 수정. 추가로 실제 UI 안전정차 게이트, 과거 Unity 누락 필드0 초기화 호환(TraitVersion), 오염/선택 모순 스냅샷 검증 보강.
- 첫 Unity import/build는 옛 LANv13 하드코딩 검사에서 실패. 실제 프로토콜과 검사 일치 수정 후 build2 종료0. 이후 수정과 main 병합 때문에 최종 재빌드 필요.
- 실제 관리 툴 브라우저에서30프리셋·새 특성3종·공통수치·대사 표시와 검증 통과 확인. 활성 사용자프로필 적용 안 함.
- broad test_prototype는 PassengerRequestTests 포함 앞선 모델 통과 후 기존 ManualHandling ordinary S brake actual8.823529에서 중단. 이 범위외 기존 실패를 이번 기능 회귀로 숨기거나 전체 검사를 통과했다고 표시하지 않는다.

## 미검증·다음

이 소스 기록 시점 실제 Player/최종 main 통합 빌드·LAN·배포 전수검증은 진행 중이며 아래에 실제 결과를 덧붙인다. 사람 운전/장기 밸런스/전체3일/다른PC LAN은 별도 후속. 필수 지도 Strict issues0, 지도·ZIP 변경 없음. 기본 실행 은계/A2 통합exe, PrototypeBuild.BuildHybrid.

재현: tools/test_passenger_traits.ps1, test_passenger_roster.ps1, test_content_tuning.ps1, node --test tests/content-editor/*.test.mjs. Player는 -passengerBehaviorTest -passengerTraitsTest 또는 -passengerRequestFailureTest -contentTest로 각각 분리 실행. 증거 artifacts/passenger-traits/, artifacts/passenger-request-failure/. Unity 설치 경로는 로컬 환경 정보이며2022.3.62f2.
