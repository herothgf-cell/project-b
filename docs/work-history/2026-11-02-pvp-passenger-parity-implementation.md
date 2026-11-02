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

## 호출 선택의 전체 운송·피로 예측 보강
Build3/abc6eab exit0,139파일115,100,493바이트를전수복사/해시대조. Four3는봇1의첫운송0원으로exit1: 나6326m/19,850원, 봇1 5781m/0원, 봇2 5444m/5,970원, 봇3 5083m/21,860원·비용2,800원. 근거리길가추격만제한해도기존“가장가까운픽업”선택은긴운송/졸음위험을무시했다. 전체주행은진행했으며전원양수정산으로표시하지않는다.

봇호출순위를픽업경로/6+native운송ETA+대기+30초로변경했다. 픽업전피로100·예상졸음중도하차/경기종료후완료는배제한다. 시설계획도같은최단전체예상을사용하며휴식필요판단을위해위험추정은유지한다. 경유/목적지응답에도추가경로후졸음한계를검사했다. 실제페달/조향·운임/승객모델·사람안내값은변경없다. Editor6(호출순위) exit0/36040, 최종Editor7(응답피로포함) exit0/36041. 같은최종검사의실패·노출문제를고친재검사이며단계별검사를선행한것은아니다.

## 최종 수용 — 소스 a2ce61beffe52a30e6b9a297ac0fa1a97dee2b95
BuildHybrid4 / passenger-final-build4.log exit0.139파일115,101,005바이트를 passenger-runtime4로복사해전수SHA256동일확인. `tools/test_pvp_passengers.ps1 -Executable artifacts/pvp/passenger-runtime4/EungyeTaxi.exe -OutputDirectory artifacts/pvp/passenger-player4`: exit0/PVP_PASSENGER_PLAYER_OK. 최종 응답 피로 예측까지 포함한 실행본에서8+3/정차선점/17요청응답·음식+5·짐실제준비·공유점유/30초·출차·72NPC진행 재확인. 이 검사는 명시적 위치·유형·요청fixture다.

`tools/test_pvp_simulation.ps1 -FourDrivers -TrafficMonitor -Executable artifacts/pvp/passenger-runtime4/EungyeTaxi.exe -OutputDirectory artifacts/pvp/passenger-four4`: exit0/PVP_FOUR_DRIVER_OK.811.8596게임초까지 실제 페달/조향/WheelCollider로 나4224.228m/14,160원, 봇1 3133.097m/5,970원, 봇2 2684.989m/5,690원, 봇3 4218.078m/21,860원 양수실제정산. 호출/기한/기본운임/승객종류/기본위치·신호를검사용으로바꾸지않았다. 공유소유권/정산중복방지/72NPC(336357.2m)/결과동결/메뉴해제/새4세션·점수·컨디션·공유8호출재시작 통과. 사람택시도기존bot페달조향으로자동운전한검사이며수동플레이아님.

같은최종실행본 `-batchmode -nographics -unifiedModeCheck` / passenger-unified4: exit0/활성101.0348초. PVP→메뉴→싱글준비·저장·복원·손상원본/백업거부·보존→메뉴, 기존사용자캠페인/운행JSON및각백업4파일바이트불변·새저장파일없음. 이전08/b753의동일검사통과도위에분리기록했다.

## 남은 수용/보존
전유형34변주의실제PVP운송/자연랜덤빈도/사람20분플레이·수익균형/다른PC/원격기존물리4유저는미수용. 이번4대검사에서봇1/2는첫정산후유효호출선택대기약297/282게임초가있었다. 봇3은마지막에주유시설접근중이었다. 회복시설의봇실제왕복·장시간연속영업을완료로표시하지않으며이전피로작업의미검증도유지한다. 전도로장기교통도미수용(최대관측대기531초). 양수1회정산통과를이런장기수용으로대체하지않는다.

싱글소스의승객분기/기본규칙·회사/제품/저장경로와필수GIS/지도ZIP은보존. 추가공통API는기존native동작을재사용하고PVP분기로만호출. 다른체크아웃은6244be9/미추적7항목그대로이며자동pull/파일교체없음. 현재작업트리에서만실행본을갱신했다. 배포manifest/게시영수증은아래추가한다.

재현: Unity2022.3.62f2 `-batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod PvpPassengerParityTests.Run`; `pwsh -File tools/test_prototype.ps1`; 빌드 `PrototypeBuild.BuildHybrid`; 위Player러너/4대러너; `-unifiedModeCheck`. 실제실행 builds/EungyeTaxi/EungyeTaxi.exe → PVP도시 → 지도에서공유8호출또는길가승객 → 빈차로같은방향연석에정차/F. 공유점유는보이는임시차량/기한보호안내를따라대기한다.

## 최종 보존/배포 준비
최종Strict exit0/Input issues0. 배포manifest 생성 및 기본/PVPalias VerifyOnly 모두exit0/139파일115,101,005바이트. 원격main 최신ec3bc4b로추가완료커밋없음.

마지막확인중 C:/git/project-b에다른채팅의새UI작업(추적수정23/미추적13)이생긴것을발견했다. 기존7미추적만있던시작상태와구분한다. 그쪽소스/인덱스/브랜치를수정·stash·reset·checkout·pull하지않았고미완료UI를이번PVP실행본에섞지않았다. 읽기전용상태목록/추적diff만 artifacts/pvp/passenger-other-checkout-status.txt 및 passenger-other-ui-preservation.patch에스냅샷했다(ignored, 전체백업아님). 그쪽의후속통합때MapDestinationUI/PassengerRequestObservation/ServiceStops 등겹치는UI파일에서이번native승객/PVP API를유지해야한다. 원격게시후기존미커밋을보존한정상3way통합은그작업에서진행한다. 실행본은이분리작업트리 builds/EungyeTaxi의최종검증본이다.

## 게시 영수증
`git log -1 --oneline -- docs/work-history/2026-11-02-pvp-passenger-parity-implementation.md`로실제배포커밋9aac1f3을확인했다. 최신origin/main이HEAD의조상임을확인한후 `git push --atomic origin HEAD:feature/pvp-prototype HEAD:main` 성공. `git ls-remote` 양쪽9aac1f3f4bd1de30bf8c83238c9b1232d2855aea 동일확인. 게시후기본/PVPaliasmanifest VerifyOnly exit0/139파일115,101,005바이트, 작업트리clean확인. 영수증추가는문서만변경하며빌드소스a2ce61b/동일실행본/실제검증결과를유지한다. 진행중타채팅UI미커밋은그폴더에보존했다.
