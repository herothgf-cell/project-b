# 2026-11-02 PVP 누락 승객 요소·봇 응대 구현

## 요청/시작 상태
누락 승객요소를 PVP에 맞게 반영·봇응대 개선, 싱글/다른채팅 보존, 구현완료후 마지막필수검사·통합실행본·이력/커밋/푸시. 시작HEAD/원격main ec3bc4b, 작업트리clean. 다른체크아웃localmain6244be9/미추적7항목 보존. Unity/Player 실행없음. 지도/회사·제품/캠페인 저장경로 불변.

## 구현
[요소별 기준](../content-design/pvp/28-passenger-parity.md). PvpPassengerPolicy 경기비율별 가상9구역 수요·봇 선택, 공유8호출+별도길가3명/같은 장부/원격길가선점금지, PvpPassengerWorld 단일 임시점유차와 각 운송1회 보호, PassengerJobs/지도·승객표시 연결. PvpBotPassengerResponses는 native 요청/유형 응답 API만 사용하고 실제 경로·청결·경기잔여를 비교한다. 싱글 모델/입력/날짜·저장/은계PS코스는 유지.

## 마지막 검토·검사
executing-plans로 직접구현, 사용자지시로 단계별승인/검사 생략. 전체읽기전용검토에서 길가봇 원격선점과 경유 잔여시간검사를 발견해 실제정차선점/전체ETA검사로 수정했다. 마지막 Editor1은 새기능검사는 통과했으나 기존 FourDriver의 후보생성 상한 회귀에서 실패(exit1). 길가 없는 기존 생성자의 계산예산을 원복했고 Editor2 exit0. 이후 수락→탑승 전환의 중복 기한보호를 같은 OfferId로 고정·회귀 추가하고, 빈 슬롯 재시도2초/경로계산전 중복필터를 추가했다. 최종 Editor3 결과는 아래 추가한다.

`tools/test_prototype.ps1`: exit0, 기존 캠페인28모델 클래스. `tools/check_workspace.ps1 -Strict`: exit0/Input issues0. 이후 실제Player/빌드·배포 결과를 추가한다. 실제사람/전유형PVP플레이/다른PC/원격4인 검증으로 표시하지 않는다.

최종 PvpPassengerParityTests.Run / passenger-final-editor3.log: exit0/PVP_PASSENGER_EDITOR_OK checks36035. 신규 수요·8+3선점·4차량점유·수락/탑승전환중복보호·봇선택과 기존 FourDriver/유형Release/컨디션·도시접근·통합모드 계약 통과. 단계별검사대신 구현후최종실패를수정해재확인했다.

## Windows 실행본 실제 API 검사
소스08d7ee4135bc3fe726f480318981fa37636c0a91 / PrototypeBuild.BuildHybrid / passenger-final-build1.log exit0. 이전139파일배포를 artifacts/pvp/pre-passenger-ec3bc4b로백업. Burst DoNotShip자료를배포에서분리하고139파일115,099,981바이트를 passenger-runtime1로복사해전수SHA256동일확인.

`pwsh -File tools/test_pvp_passengers.ps1 -Executable artifacts/pvp/passenger-runtime1/EungyeTaxi.exe -OutputDirectory artifacts/pvp/passenger-player1`: exit0/PVP_PASSENGER_PLAYER_OK. 공유8+별도길가3/실제연석F동등API탑승/중복·원격선점거부,17종요청의native봇응답(유효수락12종이상), 청결한차음식허용+5, 실제10초짐준비후탑승, 한점유차의네세션동일조회/중복기한보호없음/실제30초대기·7m출차/72NPC지속진행통과.

이 검사는 초기위치·유형·명시적BeginKind요청fixture를사용했고봇응대컴포넌트를사람fixture세션에서실행했다. 요청의자연랜덤발생빈도/사람입력/전유형실제운송·수익을입증하지않는다. 실제4대페달/조향운송은별도검사중이다. PC입력/화면조작없음.

동일소스08d7ee4 -unifiedModeCheck: exit0/활성105.0523초, PVP→메뉴→싱글저장/복원·손상거부·원본4파일보존. Four1은Idle길가접근에서시설후보를매물리tick재계산해실제52게임초에과도한CPU를소모하는문제를발견했고소유PID24684만중단했다(운송수용결과없음). 시설의새판단만5초간격으로제한하고실제서비스/주행tick은유지, 시설전환시미선점길가목표해제·상태문구수정. Editor4 exit0/36036:150동일시각IdleTick에판단1회회귀추가.

## 성능 수정 후 최종 후보
소스b75380a5ab7d8a4685dd44a6b773969826c84bf5 / BuildHybrid2 exit0.139파일115,100,493바이트를 passenger-runtime2로전수복사/해시대조. 동일 `tools/test_pvp_passengers.ps1`의 passenger-player2 재검사 exit0/PVP_PASSENGER_PLAYER_OK. 실제4대주행 passenger-four2에서133.3게임초의미선점길가접근중시설판단27회로5초간격제한을확인했다. 길가접근표시와실제진행을확인했고최종운송결과는아래추가한다.

Build2 -unifiedModeCheck/passenger-unified2: exit0/활성94.63808초·기존사용자4파일보존. Four2는1261.22게임초/실제6.0~6.7km 진행했으나봇1이장거리미선점길가추적에서패한후장거리운송중졸음취소되어첫정산0원(exit1). 다른3차량14,160/5,970/5,690원. 위치제어없이실제주행/선점경쟁을확인한결과다. 먼길가추격은불리하므로봇의기회접근을실제경로250m이내로제한하고먼곳은호출예약을우선한다(사람규칙제한아님). Editor5 exit0/36037·경계/NaN 회귀추가.
