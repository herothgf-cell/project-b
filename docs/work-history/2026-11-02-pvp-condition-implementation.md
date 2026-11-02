# 2026-11-02 PVP 컨디션·식사·휴식 실제 적용

## 요구/보존
사용자 승인: PVP피로25%증가, 간편식1,000원/15초/+60, 휴식무료/45초/−40·기존졸음해제 실제적용. 싱글/다른채팅작업보존, 마지막필수검증과실행본·커밋·푸시. 시작HEADb2bb85e·원격main동일·작업트리clean·Editor/Player없음. 기존139파일115,057,083바이트배포는수용전백업한다. 지도원본/ZIP/회사/제품/저장경로유지.

## 실제 구현
- DriverCondition.Advance의기존기본값1을보존하고PVPledgerruntimeprofile만1.25적용(분당5/7.5/10). 포만감·초기값유지. 기존캠페인직렬화/meal/sleep카운터불변, PVP완료카운터별도. PVP프로필은캠페인복원에전파하지않음.
- PvpSimulationMatch가공통물리시계로경과를진행. 식사/휴식은각차량의실제pending경과중포만/피로/졸음·서비스지표를진행하며비용/효과는완료에1회. 취소/이탈/탑승/경기종료는미완료비용/효과없이소비시간만유지. 경기끝과정확히동시인완료도취소. 기존주유/정비등의legacy시간잠금은유지.
- PVP식사는현금1,000원이필요(새미납없음), 잔액부족이면무료휴식선택가능. 기존캠페인의3,000원/50초/미납·100초수면정책은유지. 기존완전졸음100초/−20긴급회복도유지.
- F7PVP메뉴는공통시계를정지하지않고해당차만제동/입력차단. 실제견적/완료예상/타이머/취소·경고표시. 기존운임감액없음. 호출안내에종료피로·졸음위험예측, 별도PVP상태요약추가.
- 기존주차장4/12의같은포장/진출입을식사시설2곳과공유(도로·승객18·주차4·주유3보존). 식사/휴식을함께선택할수있고중복표지는통합표시. 봇은같은비용/경과/효과와실제주행으로이용하며다음운송·우회·남은시간을비교. 성공한회복의반복구매는완료카운터로막음.

## 마지막검사 기록
최종읽기전용검토에서duration double→float할당, 봇legacy완료카운터참조를찾아실행전수정했다. 렌더Update와물리pending의종료경계를같은시계로맞추고catch-up회귀를추가했다. 단계별Editor/Player검사는하지않았다.

`PvpConditionChecks.Run` / Unity2022.3.62f2 batchmode/nographics / artifacts/pvp/condition-final-editor1.log: exit0. 모델/캠페인분리·실시간pending/취소/중복/종료·물리catch-up·봇예측/완료카운터·전체도시306거점방향쌍/시설·통합모드계약통과. `tools/test_prototype.ps1` / condition-campaign-models.log: exit0, 기존캠페인28클래스모델검사통과. 모델명시소스목록3개러너에새공통PvpConditionRules를포함했다.

실제Player·시설사용·주행/저장·배포결과는아래추가한다.20분은상태검증사례이며경기시간고정아님. 장기수익/운전감각/다른PC/원격실제4명수용은별도다.

최종검사 fixture가 fixedDeltaTime=1로물리catch-up을확인할때 Unity가 MaximumAllowedTimestep도1로올리는것을발견했다. 테스트finally에서 두값모두원복하도록수정하고ProjectSettings기존0.33333334를복원했다. `PvpConditionServiceTests.Run` / condition-final-editor2.log exit0, TimeManager.asset diff없음. 첫빌드는수용배포로사용하지않고원래시간설정으로재빌드한다. 실제호출지도에피로예측표시를연결하고, 진단의선택적실제화면캡처를추가했다.

## 실제 Player·화면 (소스399a6d6)
BuildHybrid2 exit0.139파일115,079,212바이트를새폴더로복사하고전수SHA256동일확인. `tools/test_pvp_condition.ps1 -Capture -Executable artifacts/pvp/condition-runtime2/EungyeTaxi.exe -OutputDirectory artifacts/pvp/condition-player2`: exit0.4세션프로필/저장분리, 실제shared15초와45초·가격1000/무료·포만+60·피로−40/졸음해제, 부분이용/취소/재시도·중복비용방지·타드라이버/NPC진행·미완료경기종료무청구 통과. 초기시설배치/상태준비fixture는운송주행의완료증거와구분했다. 실제1600×900 식사견적/남은12초/휴식남은35초 화면확인·PC입력없음.

같은복사본 `-batchmode -nographics -unifiedModeCheck` / condition-unified2: exit0,활성95.62311초. PVP→메뉴→싱글저장/같은캠페인복원→손상원본/백업거부·보존→메뉴, 기존사용자캠페인JSON/백업/운행JSON/백업4파일바이트불변·새파일없음. PVP프로필이싱글로전파되지않는것을모델/실행양쪽으로검사했다. 현재4대실제운송및봇회복후행동검사계속중.

## 4대 장시간 주행: 미통과 기록
`tools/test_pvp_simulation.ps1 -FourDrivers -TrafficMonitor -Executable artifacts/pvp/condition-runtime2/EungyeTaxi.exe -OutputDirectory artifacts/pvp/condition-four2`: exit1.600실초 제한까지1342.559게임초 진행. 인간3392.664m/11,030원, 봇1 3752.208m/0원, 봇2 7644.464m/29,760원, 봇3 6826.402m/5,810원. 공유8호출/소유권/중복정산방지/72NPC유지/결과동결은 통과했으나 봇1은 city-junction:2:2 대기열에 막혀 첫운송완료 실패. NPC총414,171.7m, 전체최대대기1175.91초. 전도로 교통수용을 완료로 표시하지 않는다. 이 검사에서 새식사/휴식 시설완료까지 입증되지 않았으므로 별도봇실제시설검사를 추가했다. 교통정체를 회복기능 성공으로 간주하거나 차량/신호를 위치제어로 통과시키지 않았다.

## 최종 진단 보강
소스 d3b87ea는 기능수정없이 선택적 `-pvpConditionDriveCheck`와 `tools/test_pvp_condition.ps1 -Drive`를 추가한다. 기존봇 중 생산시설선택기로400예상초 내 도달가능한 후보를 선택, 빈차만물리제동후미탑승호출취소, 포만20/피로0의조건fixture를준비한다. 이후 위치제어없이생산봇의실제주행/식사/출차를검사한다. 해당 초기조건준비와 자연장시간판단의증거를구분한다. BuildHybrid3 exit0, 새runtime139파일115,082,284바이트. Burst DoNotShip자료는 artifacts로이동해배포에서제외했다.

소스d3b87ea의추가Drive검사는식사/휴식기존검사를통과했으나, 그뒤에는봇들이이미탑승운송중이어서적합한빈차후보가없었다(exit1). 이를기능성공으로기록하지않고진단순서만수정했다(5547466): 경기시작직후빈차를선택하고시설주행검사를먼저진행한다. 기능코드는399a6d6과동일하며추가진단만달라진다.

최종 BuildHybrid4: source5547466b04108ddbf796d537bba1e0f04207bfe2, exit0, 139 files/115082284 bytes. condition-drive4 exit1: all human service assertions passed, but no eligible empty bot under400 estimated seconds. Bot meal roundtrip remains unverified. Four-driver queue failure remains unresolved. Strict inputs exit0/issues0; other checkout6244be9/untracked7 preserved.

## 최종 수용 범위·재현
PVP피로/사람시설실제경과·결제·회복·화면/싱글분리는통과했다. 장시간4대전원운송은교차로정체로실패했고봇식사왕복은조건에맞는후보미확보로미검증이다.20분수익/운전감각·다른PC/원격4인도별도다. 자동물리/초기시설배치fixture를사람직접플레이로표시하지않는다. 교통제어/지도원본/ZIP/캠페인회사·제품·저장경로불변.

재현: Unity2022.3.62f2 `-batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod PvpConditionChecks.Run`; `pwsh -File tools/test_prototype.ps1`; 빌드 `PrototypeBuild.BuildHybrid`; 서비스 `pwsh -File tools/test_pvp_condition.ps1`; 선택적봇시설 `-Drive`;4대 `pwsh -File tools/test_pvp_simulation.ps1 -FourDrivers -TrafficMonitor`. 실제게임 builds/EungyeTaxi/EungyeTaxi.exe → PVP도시 → 지도시설/남부·북부주차장간편식 → 빈차로안전정차 → F7.

배포manifest생성및기본/PVPalias VerifyOnly 모두exit0:139파일115,082,284바이트. build4↔condition-runtime4전수SHA256동일. 배포후게시영수증은아래추가한다.

## 게시 영수증
`git log -1 --oneline -- docs/work-history/2026-11-02-pvp-condition-implementation.md`로 실제배포커밋4802ecf를확인했다. `git push --atomic origin HEAD:feature/pvp-prototype HEAD:main` 성공. `git ls-remote` 양쪽4802ecfa97cd6811c8e0112739b043a5d25abe0c 동일확인. 게시후manifest VerifyOnly exit0/139파일115,082,284바이트, 작업트리clean. 이후영수증추가는문서만변경하며동일실행본/검증범위를유지한다. 다른체크아웃은자동pull/실행본교체없이보존했다.
