# 승객 다양화 1차 적용 — 2026-11-07

## 요구사항·구현

승객/요청을 프리셋+특성+개별 덮어쓰기 구조의 요소별 표로 정리하고, 신규12명을 대사·선택·확률·효과와 함께 출하 기본값에 추가했다. 기존12종을 보존하되 경로확인 프리셋을 비활성화하여 총24/활성23이다. ShortRoad/RouteAdvice는 비활성·가중치0, RouteChecking 그룹0. 큰길/도착예상시간은 유지한다. 요청 수0/1/2는25/60/15. 신규 목록과 모든 수치는 [기획17](../content-design/17-passenger-roster-v1.md)에 있다.

관리창의 전체 기본값 복원과 구형 프로필의 누락 필드 이관은 실제 출하24종을 따른다. 기존 사용자 프로필을 덮어쓰지 않으며 새 기본값은 활성프로필이 없는 새 게임에 적용된다. 기존 저장의 콘텐츠 스냅샷은 유지한다. 요청별 대사를 추가해도 해당 요청의 확정 배정을 의미하지 않는다. 준비6~8초, 복합경유15초, 음식/취객 확률과 청결 손실 등은 프리셋별1차 값이다.

## 변경 파일

- config/content/passenger-roster-v1.json, presets/passenger-roster-v1.json, default-overrides.json, catalog.json, default-profile.json 및 Unity Resources 사본.
- tools/content-editor/{choice-catalog.mjs,profile.mjs,public/app.js}: 현재 기본값 일치.
- tests/PassengerRosterTests.cs, content-editor/passenger-roster.test.mjs, legacy fixtures 및 관련 기존 검사/실행기; PassengerBehaviorProbe.cs 실제12종 수용 검사.
- 기획02/07/15/17·기획목록·밸런스/툴/요청효과 안내·scope/checklist·설계계획·이력.
- 통합 실행폴더 및 배포 manifest. 지도/지도ZIP 변경 없음.

## 검증

- Node 전체87 PASS, 신규 승객 C#1821 PASS, 기존조합73 PASS, 개별설정42 PASS, 콘텐츠751 PASS 및 C#/Node UTF8/이관 해시 일치. 이전12종/300개 고정seed 규칙을 별도fixture로 보존했다. 새 요청 가중치로 데이터 의존 검사 수가 변경되었다.
- ContentTuningBuild.Build → PrototypeBuild.BuildHybrid 종료0. 런타임32·빌드3 검사 통과. 빌드 자동생성 장면 fileID 차이만 원복했다.
- 실제 실행본 -passengerBehaviorTest -passengerRosterTest: PASS131. 패키징24/활성23·신규12종 탑승/선택/준비/요청슬롯/JSON복원/도착/하차 수용. 위치제어 기반 검사이며 사람 운전 검사가 아니다.
- LAN 로컬2프로세스 host41/client52 PASS, 새 프로필 해시8191049b9bb60e470e8f79ff8d0d9e7e60aea447810961059182c029d15df86a 일치. native input/WheelCollider 검사. 다른PC 검사는 아니다.
- catalog --verify:71선택/1385항목/154704bytes, check_workspace.ps1 -Strict issues0, git diff --check 통과.
- 독립 콘텐츠 검토에서 거절 후에도 작업한 듯한 후속대사와 미치환 경유 응답을 수정 후 승인. 경유 응답은15초 리터럴이므로 시간 조정 시 함께 편집한다.
- 기존4개 범위외 실패(Comfort후속/기상중립/Empathy기대값/일반S제동)는 [전 작업 기록](2026-11-07-passenger-overrides.md) 참조. 이번 전체 레거시 스위트를 재실행해 모두 통과했다고 주장하지 않는다.

## 저장 보존·제한

활성 사용자프로필 파일은 없었고 새로 쓰지 않았다. 기존 저장22개 해시 비교 중20개 일치, 캠페인본체/백업2개 변화 관측. 시작 당시 별도 실행이 Player.log를 점유했고 이후 종료되어 변경 주체를 확정할 수 없다. 임의 복원하지 않았다. 이번 Player는 isolated campaign save 단언을 통과했고 LAN은 캠페인 저장 없는 SimulationSession이다. 저장 전체 불변으로 표시하지 않는다. 관련 관측은 artifacts/passenger-roster-v1/protected-*.json에 보관했다.

사람 실제 운전/장기 밸런스/전체3일/다른PC LAN은 미검증. 사용자 기존 프로필에서는 예전 경로 요청이 계속 존재할 수 있다. 새 기본값 복원 또는 새 passenger-roster-v1 프로필 적용으로 전환한다. 기존 사용자 미추적 자료와 동시 기획 검토 이력은 보존한다.

## 재현·다음 작업

node tools/content-editor/choice-catalog.mjs D:/Git/project-b --verify
pwsh -File tools/test_passenger_roster.ps1
node --test tests/content-editor/*.test.mjs
builds/EungyeTaxi/EungyeTaxi.exe -oneDayMode -hybridV2 -passengerBehaviorTest -passengerRosterTest -batchmode -nographics -logFile D:/Git/project-b/artifacts/passenger-roster-v1/player.log

기본 실행은 builds/EungyeTaxi/EungyeTaxi.exe, 은계싱글/A2·LANv13. 로컬 Unity2022.3.62f2. 상세 실행 증거 artifacts/passenger-roster-v1/ 및 artifacts/pvp/native-lan-20261107-004531-ad269195/. 다음은 사람 운전으로 복합작업 피로도와 만족도 누적을 확인해2차 조정한다.

## 출하 영수증

소스 커밋6ef7d8345860a1746953e2ab27461e4455b53729. 통합 manifest 생성 후 통합/PVP VerifyOnly 모두 exit0:141파일132,103,684bytes. 기본 실행본을 새 데이터로 갱신했다. 최종 staged whitespace 검사는 EOF공백3곳 정리 후 통과. 출하 커밋은 `git log -1 --oneline -- docs/work-history/2026-11-07-passenger-roster-v1.md`로 확인하며 자기 해시를 추측하지 않는다. 원격 push 없음.
