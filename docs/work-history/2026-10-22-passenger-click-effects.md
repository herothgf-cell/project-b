# 승객 마우스 응답과 전체 요청 효과 (2026-10-22)

## 요구사항·구현

승객 요구 응답을 마우스로 선택하고 수락에 따른 효과를 모두 설정하라는 요청을 처리했다. 기존17종의 실제 효과는 대부분 구현돼 있었지만 UI는 키보드 안내 문자열뿐이었다. 기존 기능을 재사용해17종/42선택의 대사·효과 목록을 명시하고 마우스와 키보드를 동일 처리에 연결했다.

| 파일 | 변경 |
|---|---|
| PassengerRequest.cs | PassengerReply/PassengerRequestChoices:17종 모든 응답의 대사·실제 효과·보류 옵션 정의 |
| PassengerRequestUI.cs | 말풍선 응답 버튼·선택별 효과·ETA3답변, CanChoose/Choose 공통 처리, 정차/교차로/일시정지/경로 후보/중복 입력 차단, 선택 후 실제 응답·효과 표시 |
| PassengerExpansionTest.cs | 기존17종 통합 검사를 클릭과 같은 디스패처로 전환,42선택 전체의 적용/움직임/일시정지/중복/경로/조용한 배려/보류 중립·버튼 사각형 검사 |
| tests/PassengerRequestTests.cs | 모든17종의 선택지/효과/보류/ETA3선택·불법 요구 거절·멀미 효과와 음악 미제공 설명 회귀 |
| 개별 기획04/11 | 클릭 응답과17종 실제 효과 표, 기존 대사 전용 범위 명시 |

큰길/최단/추천 경로·안전 하차/목적지 변경/경유의 내비와 운임·기한, 멀미 요청 이후 -4점 대체, ETA 약속 시각, 조용한 응대, 하차 만족도/보너스·장부를 재사용한다. 승객 응답만으로 운행 점수를 주지 않는다. 상황 구역을 승하차 경유지로 강제하지 않는다.

CH04는 현재 기획대로 실제 음악 변경 없이 범위를 설명하며 보너스가 없다. CH05는 할인/추가 미수금을 만들지 않는다. EV03은 텍스트 보관·신고 후속 기록이며 반환 퀘스트/보너스가 없다. EV04는 안전 거절만 제공한다. 예약/사고/수리/보험/세금은 추가하지 않았다.

## 검증과 결과

- 테스트 먼저 추가 후 모델 검사: 선택지 API 미구현으로 PassengerRequestTests 컴파일 실패 확인. 구현 후 `pwsh -File tools/test_prototype.ps1`23묶음 종료0.
- Unity2022.3.62f2 `-batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod PrototypeBuild.BuildHybrid`: 종료0, `artifacts/passenger-buttons-build.log`. 기존 간편 도로 편집기 통합 소스 유지.
- 기본 exe `-batchmode -nographics -passengerExpansionTest -oneDayMode -hybridV2`: 종료0,525 PASS/FAIL0. 기존17종 정산/JSON복원/중복 지급 방지와42선택 전체 디스패처·안전 조건·선택별 경로/대화 효과를 위치 제어로 검사. `artifacts/passenger-expansion-test.txt`, `artifacts/passenger-buttons-player.log`.
- 기본 exe `-batchmode -nographics -presentationHudTest -oneDayMode -hybridV2`: 종료0,20 PASS. 승하차·비상등·결과창/지급·초기 경로 없음 회귀.
- 최초 실행 검사에서는 -oneDayMode를 누락해 캠페인 장부가 비활성화되어 정산/생활거점 검사51개가 실패했다. 옵션을 바로잡고 동일 생산코드로525개 통과를 확인했다. 검사를 통과시키려고 장부/지급 정책을 바꾸지 않았다.
- 입력Strict·배포 해시 전수 검사 결과는 아래 배포 절에 기록한다. 지도3개/ZIP/OSM 입력은 변경하지 않았다.

## 실행·미검증

기본 `builds/EungyeTaxi/EungyeTaxi.exe`에서 승객 탑승 →요청 말풍선 →안전 정차 후 버튼 클릭. ETA는3개 예상, 대화는 가능한 대안과 보류를 표시한다. 후보 목적지/경유는 새 운임·기한 미리보기 후 클릭한다. Y/I/O/U도 호환되며 움직일 때 수락은 비활성, 안전 보류는 가능하다.

이번 검사는 버튼과 동일한 응답 함수를 호출하는 자동 위치 제어/사각형 검사이며 실제 OS 마우스 클릭·화면 픽셀 가독성·사람의 수동 플레이·자동 물리 연속주행·60분 캠페인을 대신하지 않는다. 다음은 작은 화면에서 버튼/한글 가독성과 실제 클릭 동선, 운행 중 요청 효과의 체감 확인이다. 기존 README/라이선스/XMind/편집기 계획 미커밋 변경은 보존하고 커밋에서 제외한다.

## 배포 검증

실제 소스 커밋은 git log로 확인한579d5b8이다. 로컬 입력Strict 종료0/오류0, 실행폴더128파일/84,882,735.0바이트 SHA256 전수 검사를 수행했다. 전체 소스 해시는 manifest에 기록한다.
