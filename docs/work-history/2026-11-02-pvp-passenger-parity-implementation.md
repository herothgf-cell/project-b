# 2026-11-02 PVP 누락 승객 요소·봇 응대 구현

## 요청/시작 상태
누락 승객요소를 PVP에 맞게 반영·봇응대 개선, 싱글/다른채팅 보존, 구현완료후 마지막필수검사·통합실행본·이력/커밋/푸시. 시작HEAD/원격main ec3bc4b, 작업트리clean. 다른체크아웃localmain6244be9/미추적7항목 보존. Unity/Player 실행없음. 지도/회사·제품/캠페인 저장경로 불변.

## 구현
[요소별 기준](../content-design/pvp/28-passenger-parity.md). PvpPassengerPolicy 경기비율별 가상9구역 수요·봇 선택, 공유8호출+별도길가3명/같은 장부/원격길가선점금지, PvpPassengerWorld 단일 임시점유차와 각 운송1회 보호, PassengerJobs/지도·승객표시 연결. PvpBotPassengerResponses는 native 요청/유형 응답 API만 사용하고 실제 경로·청결·경기잔여를 비교한다. 싱글 모델/입력/날짜·저장/은계PS코스는 유지.

## 마지막 검토·검사
executing-plans로 직접구현, 사용자지시로 단계별승인/검사 생략. 전체읽기전용검토에서 길가봇 원격선점과 경유 잔여시간검사를 발견해 실제정차선점/전체ETA검사로 수정했다. 마지막 Editor1은 새기능검사는 통과했으나 기존 FourDriver의 후보생성 상한 회귀에서 실패(exit1). 길가 없는 기존 생성자의 계산예산을 원복했고 Editor2 exit0. 이후 수락→탑승 전환의 중복 기한보호를 같은 OfferId로 고정·회귀 추가하고, 빈 슬롯 재시도2초/경로계산전 중복필터를 추가했다. 최종 Editor3 결과는 아래 추가한다.

`tools/test_prototype.ps1`: exit0, 기존 캠페인28모델 클래스. `tools/check_workspace.ps1 -Strict`: exit0/Input issues0. 이후 실제Player/빌드·배포 결과를 추가한다. 실제사람/전유형PVP플레이/다른PC/원격4인 검증으로 표시하지 않는다.

최종 PvpPassengerParityTests.Run / passenger-final-editor3.log: exit0/PVP_PASSENGER_EDITOR_OK checks36035. 신규 수요·8+3선점·4차량점유·수락/탑승전환중복보호·봇선택과 기존 FourDriver/유형Release/컨디션·도시접근·통합모드 계약 통과. 단계별검사대신 구현후최종실패를수정해재확인했다.
