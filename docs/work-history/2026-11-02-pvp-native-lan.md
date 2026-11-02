# 2026-11-02 기존 차량 LAN 대전·A3 골목 지도 제안

## 요청·보존

사각 반복 도시를 개선할 지도 시안, 같은 공유기·직접IP 사람최대4/빈자리봇. 싱글/다른채팅을 보존하고 구현 후 마지막 필수 검증·통합실행본/이력/커밋·푸시. PC 화면/입력은 제어하지 않는다.

작업기준72fa437에서 시작. 원격main7597eaa의 교통법규 문서16파일을 fast-forward로 반영했다. 다른 C:/git/project-b의 UI·빌드·manifest·문서·미추적 작업에는 쓰지 않았다. 이 작업은 별도 worktree feature/pvp-prototype에서 진행한다.

## 구현

[A3](../content-design/pvp/29-city-a3-proposal.md) 전체/시장 SVG·PNG, 비정형9생활권 시안. 미승인/미구현이므로 A2·지도JSON/OSM·ZIP 유지.

[LAN30](../content-design/pvp/30-native-lan.md): 기존 NGO/Transport로 native host physics4택시·게스트 입력/명령·자기모델과공유월드복제. ready barrier·빈자리봇·동일차30초메모리ticket복귀,온라인부분UI제동/연습신호변경차단·캠페인저장분리. 소유권/ID/sequence native검증. 기존평면motor결과를이번native통과근거로쓰지않는다.

변경파일: PvpNativeLan/Replica/Probe 신규; PvpBootstrap/Panel·SimulationMatch/Mode/CallBoard/Bot/PassengerWorld/DriverPresentation; 기존Taxi/Business/Economy/Law/Manners/PassengerJobs/Requests/Service/Signals/Assist/Navigation/Damage/Receipt/Unified·TrafficCar의온라인조건부연결. 새 two-process runner 및 기획·체크리스트/로드맵.

## 실제 검사 기록 (진행 중)

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
