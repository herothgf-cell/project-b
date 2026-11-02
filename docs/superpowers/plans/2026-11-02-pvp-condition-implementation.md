# PVP 피로·간편식·짧은 휴식 실제 적용 계획

사용자 승인: PVP피로1.25배(분당5/7.5/10), 식사1,000원/15초/포만+60, 휴식무료/45초/피로−40·기존졸음해제. 기존싱글/다른채팅작업 보존, 구현후마지막 필수검증만 수행하고 실행본/이력/커밋·푸시.

- [x] 모델: 원래DriverCondition/campaign 저장·카운터 불변, ledger별runtimePVPprofile만활성. 자연/기존서비스 피로1.25, 포만소모/초기값 그대로. 새pending서비스 경과/완료 분리·중복결제보호·조건forecast.
- [x] 서비스/UI: 경기공통시간·다른드라이버 계속, 이용자만정차/입력차단. 식사/휴식 완료시에만 비용/회복1회, 취소/경기끝은소모시간만유지. 기존F7/시설 재사용, 안전빈차·가격·예상상태·남은시간 안내. 기존 싱글의 메뉴/즉시서비스/저장 유지.
- [x] 봇: 같은규칙/실제시설주행, 식사예방/휴식회복 선택, 다음운송·시설우회·남은시간으로판단, 반복구매/종료직전불필요방문방지.
- [x] 최종필수검사 수행(4대전원운송은미통과, 이력참조): 모델20분사례/분할dt/중복완료·취소·종료/캠페인저장회귀, 서비스경과·타드라이버진행/실제시설동작,4대운송·성능필요부분. 실패한검사만수정/재확인.
- [x] 소스커밋후Unity2022.3.62f2 통합BuildHybrid, 전체폴더복사해시·실행검사·manifest갱신, 관련기획/체크리스트/날짜이력/LATEST와main/feature푸시. 다른작업/현재게임보존.

역할: condition_model은DriverCondition/EconomyLedger/CampaignAccount/PvpConditionRules/모델검사, condition_service는PvpSimulationMatch/BusinessProgress/ServiceStops 및필요입력훅/서비스검사, condition_bots는PvpSimulationBotServices/PvpSimulationBot/봇검사, root통합/마지막필수검사/배포. subagent-driven-development 사용, 사용자의단계별검토/검사생략지시우선.

시작HEAD b2bb85e·원격main동일·작업트리clean·Unity/Player없음. 현재139파일115,057,083바이트배포는새빌드수용전보존. 캠페인회사/제품/저장경로·지도입력/ZIP유지.20분은검증사례이며경기시간확정아님. 운임감액없음.

실제배포4802ecf/원격main·feature동일확인. 필수검증의통과/실패/미검증은작업이력에구분했다. 전원운송정체와봇식사실제왕복은미수용이며전체게임수용완료를뜻하지않는다.
