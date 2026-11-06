# 2026-11-06 선택지 효과 구조 조사·평가 화면 설명 정리

> 이 기록은 독립 효과 구현 전 조사 시점의 이력이다. 이후 사용자가 구현을 승인했으며 현재 상태는 [독립 효과 구현 기록](2026-11-06-independent-choice-effects.md)과 [설정 안내](../content-tuning-tool-guide.md)를 따른다.

## 요구와 실제 차이
사용자는 하차 평가/미응답/공통 설정을 구분하고 중복 점수를 정리하도록 요청했다. 이어서 ETA 점수 잠금·공감의 동일+5/미사용−5를 지적하며, 선택지별 즉시/운행 중/종료 후 만족도·피로 증감/율의 실제 지원 여부를 확인해 달라고 했다.

조사 결과 현재는 선택지별 효과 엔진이 아니라 요청 종류별 공통 평가와 전용 행동이다. ETA/불법 요구 거절은 readonly0, 공감 Accept/Alternative는 모두 정상 수락 후 하차+5, 일반 failure−5는 미사용이고 기회 후 미응답−5는 별도다. 선택지별 즉시 점수·피로 변화·증가율은 없다. 피로는 시간/허기/휴식 등에 의해 변하고 우회/대기는 시간 경과로 간접 영향을 준다. [실제 구조·근거·미구현 확장안](../future-design/2026-11-06-choice-effects-structure-review.md)을 작성했다.

## 실제 구현
- 관리창을 운송 종료 평가/답하지 않았을 때/모든 요청에 적용되는 공통 설정으로 나눴다. 일반 요청의 이행·미이행·미응답 편집칸을 각각 한 곳에 두고 선택지에는 결과 요약과 평가 이동을 제공한다.
- 같은 화면의 공유 설정 중복도 제거했다. 다른 화면은 같은 프로필 ID를 참조한다.
- 각 항목에 게임 내 적용 시점·영향 대상을 표시한다. 상한 예시, 진행률 단위, 청결 감소·M과 요청 점수의 구분을 설명한다.
- 미사용 failure는 참고값으로 표시하고 실제 선택 결과 미리보기에서 제외한다. ETA/불법 요구 거절의 편집 가능한 이행값이라는 기존 잘못된 설명을 고정0으로 바로잡았다.
- 후속 제한이 확률 감소가 아니라 추가 상황 대화·취객 수다 제한이며 별도 배정 요청과 필수 안내를 유지한다는 범위를 표시했다.
- 게임의 선택지별 독립 효과/피로 엔진은 추가하지 않았다. 최신 요청은 실제 구조 확인이므로 확장안은 제안/미구현으로 명시한다.

## 파일
`tools/content-editor/public/{content-model.js,app.js,style.css}`, `tests/content-editor/{clarity.test.mjs,model.test.mjs,context-ui.test.mjs}`, `tests/ChoiceStructureAudit.cs`, `tools/test_choice_structure.ps1`, 설정 안내·계획·구조 조사·이 기록/최신 이력·체크리스트. 실제 게임 코드·카탈로그/기본 프로필·사용자 프로필·실행본·manifest·지도 입력/ZIP은 이번 변경에 포함하지 않는다.

## 검증
- writable overlay에서 전체 Node **42/42·exit0**, JS 구문 검사2개 exit0.
- 실제 PassengerRequest의 순수 클래스와 실제 ContentJson/ContentTuning을 분리 컴파일한 모델 조사 **16검사·exit0**: 공감 두 선택 직후0/종료+5·수정 시 둘 다11·미응답−5·보류0·ETA 세 선택0·ETA/임의 reply 키 검증 거부·0은 변화 없음.
- 독립 검토에서 ETA/불법 거절의 readonly 잠금과 미사용 실패의 요약 노출을 지적해 수정했다. 게임 프로필 validator의 readonly 제외까지 대조했다.
- 최초 분리 컴파일은 Windows CSC의 경로 표기와 별도 Reply enum 포함 문제로 실패했고, 실제 Windows 절대 경로와 원본 enum을 포함한 뒤 통과했다. 이를 게임 코드 실패로 집계하지 않는다.
- 실제 폴더 반영·브라우저·최종 검사 영수증은 아래에 이어 기록한다.

Unity 위치 제어·자동 물리 주행·Player 수동/전체 캠페인 검사는 하지 않았다. 동시 사고/지도 작업이 게임 실행 폴더를 갱신 중이므로 이번 UI 작업에서 배포 manifest를 생성하거나 다른 작업의 빌드 검증을 대신 표시하지 않는다. 필수 로컬 입력은 변경하지 않았다.

## 실행
루트 `설정 관리.cmd`. 기존 편집창에서 저장하지 않은 값이 있으면 먼저 저장한 뒤 새로고침한다. API/route 변경이 없어 현재 서버에서도 새 UI를 읽는다. 공감 요청에서 듣기/조용한 배려 결과와 운송 종료 평가의 단일 편집칸·미응답−5·미사용 failure−5 표기를 확인한다.

재현: `node --test tests/content-editor/*.test.mjs tests/content-manifest.test.mjs`, `pwsh -File tools/test_choice_structure.ps1`.

## 실제 폴더 반영 영수증

- C:/git/project-b에서 Node42/42·JS구문2개·순수 C#구조16 모두 exit0. C# 검사는 실제 요청 클래스/enum과 실제 설정 validator를 사용했다.
- 별도 브라우저 검사창에서 공감 이행 점수 편집칸1개, 5→7 수정 시 두 정상 답변 결과에 같은7 반영(현재공유구조), 원복5·미사용 failure 제외·평가 이동·적용시점/영향대상 확인. 기존 사용자 편집창은 새로고침하지 않았다. 브라우저 오류0.
- 사용자 sample-profile SHA256 9003379B3B01F4B368B4FFDBE8563D244E77314B40C8253CB126E4B1237F65B7, active SHA256 92F6120BECD3B51D98791EF4C63FEC7825D969E10E05653A258093D8205EC94F로 이전과 동일. 사용자 프로필 저장/적용 없음.
- 화면: [미사용 실패와 미응답 분리](../ui/2026-11-06-context-editor/evaluation-clarity.png). 게임의 즉시/운행 중/종료 후 독립 선택지 효과는 미구현이며 구조 확장안만 기록했다.
- 이 작업 경로만 선택 커밋, 브랜치 전환/원격 게시 없음. 실제 해시는 git log -1 --oneline -- docs/work-history/2026-11-06-choice-effects-audit.md로 확인한다.
