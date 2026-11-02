# 2026-11-02 기존 차량 LAN 대전·A3 골목 지도 제안

## 요청·보존

사각 반복 도시를 개선할 지도 시안, 같은 공유기·직접IP 사람최대4/빈자리봇. 싱글/다른채팅을 보존하고 구현 후 마지막 필수 검증·통합실행본/이력/커밋·푸시. PC 화면/입력은 제어하지 않는다.

작업기준72fa437에서 시작. 원격main7597eaa의 교통법규 문서16파일을 fast-forward로 반영했다. 다른 C:/git/project-b의 UI·빌드·manifest·문서·미추적 작업에는 쓰지 않았다. 이 작업은 별도 worktree feature/pvp-prototype에서 진행한다.

## 구현

[A3](../content-design/pvp/29-city-a3-proposal.md) 전체/시장 SVG·PNG, 비정형9생활권 시안. 미승인/미구현이므로 A2·지도JSON/OSM·ZIP 유지.

[LAN30](../content-design/pvp/30-native-lan.md): 기존 NGO/Transport로 native host physics4택시·게스트 입력/명령·자기모델과공유월드복제. ready barrier·빈자리봇·동일차30초메모리ticket복귀,온라인부분UI제동/연습신호변경차단·캠페인저장분리. 소유권/ID/sequence native검증. 기존평면motor결과를이번native통과근거로쓰지않는다.

변경파일: PvpNativeLan/Replica/Probe 신규; PvpBootstrap/Panel·SimulationMatch/Mode/CallBoard/Bot/PassengerWorld/DriverPresentation; 기존Taxi/Business/Economy/Law/Manners/PassengerJobs/Requests/Service/Signals/Assist/Navigation/Damage/Receipt/Unified·TrafficCar의온라인조건부연결. 새 two-process runner 및 기획·체크리스트/로드맵.

## 실제 검사 기록 (초기·실패 포함)

- Unity2022.3.62f2 `-batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod PvpPassengerParityTests.Run`, native-lan-editor1.log: 정상종료/PVP_PASSENGER_EDITOR_OK36041, 기존4인/격리/컨디션/도시90645 포함. 이후 최종 통합수정은 재검사 대상.
- 배포/실제native LAN 검사는 아직 진행 전. 아래에 최종 exit/실제 수치를 추가한다.

## 미검증·한계

A3 실제 지도 적용, 다른PC LAN/방화벽·사람주행·원격4명·장시간 대역/지연/수익·전도로 교통은 별도다. 기존 봇 후속배차 긴대기와 완전한 식사/휴식왕복 수용은 이번 기능 추가로 해결됐다고 주장하지 않는다. 인터넷 초대/중계/호스트이전/앱재시작후ticket복구는 미제공.

실행: 통합EXE → PVP → 시간·방생성 → 상대LAN IPv4/포트 참가 → 호스트시작. 자동 재현은 `pwsh -File tools/test_pvp_native_lan.ps1 -Reconnect`, 오프라인 승객은 tools/test_pvp_passengers.ps1. 캠페인28모델 tools/test_prototype.ps1. 빌드 PrototypeBuild.BuildHybrid.

- BuildHybrid1 native-lan-build1.log exit0. 첫two-player실행에서NGO NetworkManager를PVP아래자식으로둔초기화오류를재현했다(두로그동일). 완료근거로쓰지않으며,transport를독립root로옮기고종료시명시해제한다. 같은최종수정에원격인간시설추천·기존조향완화도연결한다.

- BuildHybrid2 exit0. native-lan-20261102-153819-49223233은guest ready전연결탈락으로봇비활성assert실패(exit1). 빌드중정상연결통과로표시하지않는다. 월드생성중transport생존120초/경기중입력0.5초제동·4초탈락을분리하고ready시입력기준시간을초기화했다. kinematicvelocity직접쓰기를제거하고UI가서버속도를읽게했으며저널에sticky SimulationSession보호추가.

- BuildHybrid3 exit0. 154435-a9451ec1은ready/4native차량·게스트봇중지는통과했으나첫희소frame에서Unity JSON의null class가빈인라인객체로복원돼invalid PVP counters 거부가발생해미완료. 상세payload full존재태그와빈호출seed정규화를추가하고Editor JSONroundtrip회귀를작성했다. 완료근거에서제외.

- PvpNativeLanTests.BuildValidated / native-lan-build4.log exit0: 승객36041+native JSON회귀17 및 BuildHybrid 통과. native-lan-20261102-154949-5c85b464 -Reconnect: 두프로세스 exit0, host24/client26 checks. 실제guest물리13.92146m/최고4.051594m/s, 선점·경제/내구도/점수 복제·잘못된명령상태불변·같은차봇승계/승객보존·30초ticket복귀/자리회수·양쪽메뉴제거 통과. 위치제어없음. 이후최종빌드에는클라이언트6초응답감시/정확한호스트daySeconds/중복시설표시방지·거부gate/NPC·신호시계/운전점수추가assert를포함한다.

- 최종후보 BuildValidated5 exit0/승객36041+JSON17. 실행본을runtime5로전수SHA동일백업. 작업중원격main에밝은UI·마인드맵8커밋(d818c33)이게시됨을확인하여최종배포전에보존통합한다. 현재소스커밋은통합/배포완료주장이아니며최종결과는아래후속검사를따른다.

- 최신main d818c33 밝은UI/마인드맵8커밋을통합. 충돌5파일(Panel/CockpitUI/driver표현/통합메뉴/체크리스트)은 밝은화면·간결문구·새내비를유지하면서 LAN메뉴/좌석표현/온라인종료를합쳤다. 자동병합된승객/시설/결과/법규/매치도조건을보존. 통합후BrightUITheme/NavigationCue+기존승객+LAN회귀/BuildHybrid를다시실행한다.

- runtime5追加검사는시작직후client응답감시가발동해미통과. 23초동기월드생성중Time.unscaledTime이프레임시작값으로고정돼Bind의시간기준이오래된값인원인을확인했다. 감시기준을실시간realtimeSinceStartup으로통일한다. 밝은UI통합 Build6는Editor테마/내비15/승객36041/LAN17 및BuildHybrid exit0이나,이감시수정후Build7을최종수용대상으로삼는다.

- 통합 Build7 테마/내비15·승객36041/LAN17/BuildHybrid exit0. 최종 client경기종료적용은권한없는view의 Ledger.Finish만사용하도록수정(호스트정산/비용관찰을client에서다시실행하지않음). 최종Build8에서검사/배포한다.

## 최종 통합 실행본 검사

- 실제 빌드 소스 `d40554436c643c39db8ce27c22a2c41b414d9ba2`, `PvpNativeLanTests.BuildValidated` / native-lan-build8.log: exit0. BRIGHT_UI_THEME_OK / NAVIGATION_CUE_OK15 / PVP_PASSENGER_EDITOR_OK36041 / PVP_NATIVE_LAN_EDITOR_OK17 / PROTOTYPE_BUILD_OK. 캠페인 모델 tools/test_prototype.ps1 28클래스 exit0.
- 두 실제 Player `tools/test_pvp_native_lan.ps1 -Reconnect` / native-lan-20261102-160337-f678c818: host25/client28, 양쪽 exit0. 클라이언트 페달 메시지로 호스트 WheelCollider 실제13.831477m·최고4.039638m/s, 클라이언트 표시13.831476m. 위치제어/순간이동 없음. 공용승객 명령·원자선점/경제·내구도·점수·운전 M/72NPC수·신호시계 복제·잘못된ID/중복/이전운송 gate 거부, 끊김 같은차/승객/claim 봇승계, 원래 ticket 좌석 재접속·인간 재제어·완전메뉴제거 통과. 호스트 경기36.74065초 동안 진행. 같은 PC loopback2프로세스이며 다른PC/4인 실제LAN/전체운송/네트워크지연 체감 검사가 아니다.
- 기존 오프라인 `tools/test_pvp_passengers.ps1 -OutputDirectory artifacts/pvp/native-lan-final-passenger8`: exit0/38 PASS/PVP_PASSENGER_PLAYER_OK. 공용8+길가3/정차선점/17요청·음식/짐/공유점유30초·출차·72NPC진행. 명시적 위치·유형·요청 fixture이며 실제 주행 운송 증거와 구분한다.
- Unity 내부 PC 화면·키보드·마우스 제어 없음. 새 LAN 화면 실제 렌더/사람 조작은 별도이며 최신 main의 밝은 UI·내비 소스를 보존하고 Editor 계약을 재검사했다.
- 배포폴더는 Burst DoNotShip 자료를 검증된 artifacts 경로로 분리한139파일115,168,912바이트다. 싱글 전환/저장·최종해시/원격게시 결과는 이어서 기록한다.

- 최종 독립 코드 검토에서 추가 P2 한 건 발견: Gamepad JoystickButton7이 기존 pause로 빠져 호스트 공통 시계가 멈추는 문제다. Esc와 같은 온라인 메뉴로 연결하고, 이 메뉴 중 공통 시계 진행/자기 차 제동의 실제 Player assertion을 추가했다. 추가 critical/important 차단점은 없었다. 물리 패드 입력은 별도다.
- Unified8 실제 앱은 exit0/UNIFIED_MODE_RESULT PASS, 사용자 JSON4파일/별도 전체6파일 SHA 불변. 부모 셸에서 기대 marker를 UNIFIED_MODE_OK로 잘못 쓴 탓에 runner만 missing marker로 종료했다. 실제 로그/종료 조건으로 구분하고 최종 재실행은 올바른 UNIFIED_MODE_RESULT PASS를 사용한다.

## 배포 수용 — 최종 Build9

- 실제 소스 `70200c703a00bcdfc32c8669e54aa812575e70fc`, PvpNativeLanTests.BuildValidated / native-lan-build9.log exit0: 밝은테마·회전안내15·승객36041·LAN JSON17·기존격리/도시/컨디션/4인계약 및 BuildHybrid 통과. main d818c33의 밝은UI/내비/마인드맵/법규 문서 보존.
- `tools/test_pvp_native_lan.ps1 -Reconnect` / artifacts/pvp/native-lan-20261102-161231-07938bb5: host26/client28 **exit0**, 합계54. 메뉴 중 자기차 제동/공통시계 계속, 실제 페달 주행 **13.742274 m / 최고 4.027434 m/s**, 승객/소유권/모델/72NPC·신호/운전점수·명령거부/같은차봇/재접속/종료 통과. 같은PC 두프로세스이며 다른PC/4명/완전한원격운송/수동입력 수용은 아니다.
- 최종 실행본 `test_pvp_passengers.ps1 -OutputDirectory artifacts/pvp/native-lan-final-passenger9` exit0/38PASS/17요청·점유·음식/짐/8+3. 명시적fixture다.
- 최종 실행본 `-unifiedModeCheck -logFile artifacts/pvp/native-lan-final-unified9.log` exit0/UNIFIED_MODE_RESULT PASS active_seconds114.136. 초기메뉴/4인PVP/재시작/언로드/싱글 임시 실제저장·복원·거부원본/backup보존/메뉴 통과. 실제사용자JSON4파일 및 별도전체6파일 SHA256·파일수 불변 확인. PC 화면/입력 조작 없음.
- 필수지도 `tools/check_workspace.ps1 -Strict` exit0/issues0; 선택import2맵미배치 유지. config/maps·distribution/maps·원본GIS·회사/제품명/저장위치 보존. 다른Cgit의 마인드맵 미커밋2파일과미추적폴더/설정에는쓰기없음.
- 현재 배포/실행은 이 worktree의 `builds/EungyeTaxi/EungyeTaxi.exe`. 은계 영업 또는PVP → 시간/방만들기 → 동일LAN IP·포트참가 → 호스트시작. 빈자리봇/앱실행유지30초재참가. A3는시안이며게임은현재A2도시. 다른PC/사람4명·물리패드·네트워크상태/장기경제/전도로·A3구현은미검증/별도.

- 배포 manifest `tools/update_distribution_manifest.ps1 -SourceCommit 70200c703a00bcdfc32c8669e54aa812575e70fc -ValidatedOn 2026-11-02 -ValidationReport artifacts/pvp/native-lan-validation9.json` exit0. 통합/PVP위임 VerifyOnly 각각 exit0: **139파일115,169,424바이트** 크기/SHA256 전수일치.
