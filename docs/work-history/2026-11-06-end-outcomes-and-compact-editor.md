# 2026-11-06 종료 실패 효과 구분과 관리창 설명 간소화

## 요청·실제 구조 확인
사용자는 운송 종료 후 이행 실패 설정을 찾기 어렵고, 입력칸마다 반복되는 적용 시점/영향 안내가 가독성을 낮춘다고 지적했다. 실제 `PassengerRequestObservation.Finish`, `ChoiceEffectDefinition.Read`, 선택지 카탈로그를 대조했다. 현행 실패 분기는 편안한 운행(Comfort)·제시간 도착(OnTime)·안전 하차(SafeStop)·목적지 변경(DestinationChange)·경유 용무(ViaStop)의 수락 응답에 있다. 조용한 운행·ETA·공감 등에는 별도 실패 판정이 없고, 보류는 중립/판정 불가이며 미응답은 별도다.

## 구현
- 각 선택지의 종료 단계를 **운송 종료 후** 영역으로 묶고 **이행 성공 / 이행 실패 (미이행) / 판정 불가·보류**로 구분했다. 지원하는 실패 단계의 만족도/기사 피로 점·%4항목은 기존 실제 설정을 그대로 편집한다. 코어 계산 샘플의 결과 선택 이름도 맞췄다.
- 실패 분기가 없는 응답에는 별도 판정이 없다는 짧은 안내를 표시한다. 사용되지 않는 가짜 실패 설정이나 새 판정 조건을 만들지 않았다. 미응답 결과 위치와 보류 구분을 안내한다.
- 입력칸은 이름·현재 값·기본값/저장값 복원 버튼을 먼저 표시하고, 적용 시점/영향·계산 예시·기본값/입력 범위·기술 정보는 기본으로 접힌 **설정 설명**으로 옮겼다. min/max/step과 부호 변환·실시간 공유 편집은 유지한다.
- 사용 여부/가중치의 같은 안내는 **등장 설정 안내** 한 곳에 모았다. 선택지의 긴 설명/메모는 **선택 행동과 판정 조건**, 단계 전체 설명은 **단계 적용 방식**에 모았다. 이전 변화율 계산법과 자연 피로 표도 계속 펼쳐 볼 수 있다.

## 변경 파일
`tools/content-editor/public/app.js`, `content-model.js`, `style.css`; `tests/content-editor/context-ui.test.mjs`, `choice-ui.test.mjs`; 사용 안내·체크리스트·LATEST·이번 기록. 게임 C#·카탈로그·기본 프로필·배포 파일은 변경하지 않는다.

## 검증·결과
- 신규 화면 회귀2개가 기존 중복 안내2회/종료 영역 없음으로 실패한 뒤 구현했다. 실패5종의 실제 편집 필드4개와 비지원 응답 안내도 검사한다.
- Node 전체 관리창/프로필/서버/manifest63개 통과·실패0·exit0. 실제 C# 선택 효과50개, 콘텐츠/자연 피로/감점663개 통과. Unicode/이관 C#·Node hash 일치.
- 실제 브라우저에서 반복 설명 제거·접힌 설정 설명의 펼침/접기·실패4항목 편집을 확인했다. Comfort 실패 S를-17로 변경해 API 검증과 실제 C# 샘플 종료 결과-17을 확인했다. 기본 요청 합계 상한±10에 따라 최종 반영-10인 기존 규칙도 확인했다. 시험값은 저장값-10으로 복원했고 저장/게임 적용을 누르지 않았다. 최종 새 탭에는 실패 편집 화면을 열어뒀다. 기존 탭의 편집 상태는 건드리지 않았다.
- 화면 증거: `artifacts/compact-appearance.png`, `artifacts/end-failure-editor.png`. 로그: `artifacts/compact-effects-node.log`, `artifacts/compact-effects-core.log`, `artifacts/compact-effects-runtime.log`.
- 통합 manifest VerifyOnly exit0: 사용자 active 포함141파일, 출하140파일116,826,789바이트 유지. 사용자 active·default/sample 프로필 및 Assembly/manifest 총5파일 시작/종료 SHA256 일치(`artifacts/compact-effects-preserve.json`). `git diff --check` 통과.

## 실행·남은 항목
이 PC의 실제 실행 루트 `C:/git/project-b`에서 `설정 관리.cmd`를 실행한다. **승객 요청 → 편안한 운행 → 수락 답변 → 운송 종료 후 → 이행 실패 (미이행)**를 펼치면 만족도·기사 피로 점/%를 각각 편집한다. 조용한 운행에서는 별도 실패 판정이 없는 이유를 확인한다. 각 입력의 상세 안내는 **설정 설명**에서 펼친다.

이번은 UI 배치와 안내 변경이며 Unity 재빌드·새 Player/물리 주행·수동 전체 캠페인·다른PC LAN 검사는 수행하지 않는다. 기존 은계 싱글/A2 PVP·LANv9·BuildHybrid `builds/EungyeTaxi/EungyeTaxi.exe`와 필수 지도 입력/ZIP을 유지한다. 이번 Strict 재실행 없음. 외부 관리 worktree와 기존 미추적/동시 기획 자료를 보존한다. 다음은 사용자 직접 편집 수용이며, 실패가 없던 요청에 새 실패 조건을 추가하는 작업은 별도 요청이 필요하다.
