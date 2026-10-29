# 2026-10-29 PVP 시험장 화면·직접 IP 네트워크 주행

## 요구·격리

시험장 잔여 검증 후 네트워크 주행까지 진행, 다른 채팅 작업 보존, 필요한 화면 확인만 수행, 승객·내구도는 상대 작업 완료 후 연동. 기존 feature/pvp-prototype worktree에서 작업했다. main/다른 checkout의 소스·미커밋 파일은 편집하지 않았다. 원격 최신은 시작 시 fetch했으며 최종 게시 대상은 PVP 브랜치다.

사용자가 Fusion SDK/App ID 미준비와 대안 확인을 답했다. 공식 Unity 패키지 레지스트리에서 NGO1.12.0의 최소 Unity2021.3을 확인하고 로드맵의 NGO 대안을 직접 IP 시험에 사용했다. 실제 lock은 NGO1.12.0, Transport1.5.0, Mono.Cecil1.11.4다. Photon 채택 실패나 NGO 최종 채택을 뜻하지 않는다. Relay/계정 생성/방화벽 변경 없음.

## 구현

- 전용 초기 화면의 호스트/직접 IP 참가. 기본127.0.0.1:7777; LAN 시험은 호스트의 실제 IPv4를 양쪽에 입력한다. 호스트는 입력한 주소에만 bind한다. 두 참가자 한정, 인터넷 초대 코드/매칭 없음.
- 서버50Hz 차량A, 입력 sequence·범위·버퍼 제한. 클라이언트는 입력을 즉시 예측하고 권한 ACK까지 제거한 뒤 미승인 입력을 재계산. 상태는 위치뿐 아니라 속도·조향·차로 anchor를 포함한다.
- 빌드/프로토콜/맵/주행 규칙 불일치 거절. 소유자는 NGO 전송자 연결 ID로 정하며 클라이언트 명령에 선수 ID·위치·점수를 받지 않는다. 공개 계정 인증·재접속 자격은 없다.
- 고정 경기 시간·승객·정산을 생성하지 않는 주행 실험. 네트워크에서 오프라인 초기 경로 표시를 숨기고 별도 상태 HUD를 표시한다. 원격 차량은10Hz 권한 위치 직접 표시이며 보간은 미구현.
- 두 Player/불일치/방장종료 검사 러너와 loopback 지연·지터·손실 proxy를 추가했다. 실제 외부망 검사를 대체하지 않는다.
- SDK가 자동 생성한 빈 Assets/DefaultNetworkPrefabs.asset과 meta, manifest/lock 변경 포함. 캠페인 소스·씬·배포·지도 데이터 변경 없음. 같은 프로젝트 전체에 패키지는 추가되지만 PVP 시작점만 세션을 생성한다.

## 발견·수정 근거

1. network-red.log: 미구현 direct-IP session 검사 실패. Unity 로그 return1 (호출 프로세스 종료값1073741845) 기록.
2. 첫 실제 Player: ConnectionRequestMessage KeyNotFoundException. NGO DLL·RuntimeInitializeOnLoads에는 새 내용, ScriptingAssemblies.json은 이전 엔진/Assembly-CSharp 목록이었다. 메시지 등록 진단0개. 3초 대기로도 실패하여 시간 문제 가설을 기각했다. 내부 메서드 reflection 호출 우회 없이 BuildOptions.CleanBuildCache로 메타데이터 재생성, 필수 NGO/Transport DLL 목록의 빌드 후 검사 추가. 이후 실제 접속·거절 동작 확인.
3. 실제 client 권한 이동0m 검사 실패. NGO1.12의 scene management OFF 연결 콜백이 local ID가 아닌 서버 sender ID를 전달함을 SDK 소스에서 확인, IsConnectedClient로 상태 확인 수정.
4. 리뷰의 거짓 성공 가능성 수정: 거절을 기대한 probe는 정상 연결 유지로 성공하지 않음. 실제 이동>10m와 제동 후 권한 위치<0.05m/회전<0.1도/속도<0.05m/s 오차를 검사.
5. 숨김 창 PNG는 검은 화면. 스플래시 대기로도 해결되지 않아 필요한 한 번의 표시 실행으로 실제 렌더링을 확인했다. 키보드/마우스 입력 전송 없음. practice-visible.png에서 차량·한글HUD·도로 화살표·합법 경로·복구 안내 확인. 자동 probe 입력이며 사람 조작감 판정 아님.

6. RTT150ms·손실3%에서 기존 입력 대기열 기준 실패를 관측했다. 최신 held 조작을 합쳐 서버 한 tick에 한 번 적용하고, 수신이 빈 tick에도 최대10tick(200ms 시험값) 유지한 뒤 입력 유실 감속한다. 묶음/균등 가속·즉시 제동·유효기간 회귀 실패→통과와 실제 Player 재검사를 수행한다. buildGUID는 실행물별 일치 검사이며 Editor↔Player와 별도 재빌드는 거절될 수 있다.

## 검증 결과

최종 실제 명령 종료 결과는 아래에 추가한다. artifacts/pvp 로그/PNG는 Git 제외이며 실패 로그도 보존했다.

## 미완료·다음 단계

Task2 사람 조작감/실물 패드/전체 코스 비교, 별도PC·실제 인터넷 접속, 지연별 운전 체감·진행 중 보정 품질·tick 부하, 재접속 자격/상태 복원, 공유 이동 장애물·신호 시험은 미완료. 화면 확인과 정지 후 수렴 성공만으로 P1/P2 전체 게이트를 닫지 않는다. 현재는 계정 없는 네트워크 실험 경로를 추가한 상태다. Task4/5 승객·정산은 미착수. 다른 채팅 승객·내구도 완료 커밋을 확인하고 별도 통합/밸런스 검토 후 연결한다.

## 실행·재현

로컬 Unity: C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe. 저장소 기준 프로젝트 unity/EungyeTaxi.

- Unity -batchmode -nographics -quit -projectPath <프로젝트> -executeMethod Pvp.Tests.EditMode.PvpNetworkTests.Run -logFile <로그>
- Unity -batchmode -nographics -quit -projectPath <프로젝트> -executeMethod Pvp.Editor.PvpBuild.BuildClient -logFile <로그>
- pwsh -File tools/test_pvp_network.ps1
- builds/PvpPrototype/PvpPrototype.exe 실행 → 직접 IP 호스트/참가. 두 프로세스 기본127.0.0.1 동일 포트. WASD/Space/Q/Esc. 경기 시간이 없는 주행 시험이다.
- tools/pvp_udp_test_proxy.py --listen 17890 --server 17889 --rtt-ms 100 --jitter-ms 10 --loss .01 --seconds 30 후 host는17889, client는17890. proxy는127.0.0.1에만 바인드한다.

PVP exe는 로컬 빌드이며 Git 배포 제외. 기존 기본 실행 builds/EungyeTaxi/EungyeTaxi.exe와 BuildHybrid는 유지한다.

공식 근거: https://packages.unity.com/com.unity.netcode.gameobjects (버전 메타데이터), https://doc.photonengine.com/fusion/v2/tutorials/host-mode-basics/1-getting-started (Fusion App ID 요구), https://mp-docs.dl.it.unity3d.com/netcode/2.2.0/advanced-topics/client-anticipation/ (완전 예측을 별도 구현해야 하는 대안 평가 근거; 설치 버전과 문서 버전 구분).
### 최종 실제 결과 (입력 유지 수정 포함)

| 검사 | 실제 결과 | 로그/증거 |
|---|---|---|
| Unity2022.3.62f2 Editor 전체 PVP/맵/차량/네트워크 모델 | 종료0, 입장·범위·순서·재계산·중복·미래ACK·이력상한·묶음/held/만료 검사 통과 | artifacts/pvp/network-held-green.log |
| PVP clean Windows 빌드 | 종료0, 필수 어셈블리 목록 확인 | artifacts/pvp/network-held-build.log |
| 두 Player 정상 접속2회 및 맵 불일치 거절2회 | 모두 종료0, 실제 이동28.68061m/28.116m, 수신149개씩, pending4, 정지 후 위치/회전/속도 오차0 | artifacts/pvp/network-held-suite.log, net-0/1-*.log |
| 방장 정상 종료에 따른 client 중단 | host종료0, client예상종료1, host shutting down 이유 확인; 전체 러너 종료0 | net-host-loss-*.log |
| RTT100ms·지터±10ms(방향별)·손실목표1% | host/client/proxy 모두종료0, 이동27.5564m, 수신157개, pending6, 최대보정0m, 정지 후 모든 오차0 | net-held-100-*.log;1920패킷 중25개 drop |
| RTT150ms·지터±10ms(방향별)·손실목표3% | host/client/proxy 모두종료0, 이동26.1184m, 수신155개, pending20, 최대보정0.8390007m, 정지 후 모든 오차0 | net-held-150-*.log;1925패킷 중60개 drop |
| 기존 PVP CLI/캠페인 모델 | 각각종료0 (네트워크 구현 중 실시, 이후 캠페인 소스 변경 없음) | network-pvp-model.log / network-campaign-model.log |
| Strict 필수 입력 / 배포 manifest | 각각종료0, 기존 배포130파일·112072682bytes 일치 | network-strict.log / network-distribution.log |
| 실제 시험장 화면 | 표시 실행종료0, 한글HUD·차량·도로·경로·복구안내 관찰 | practice-visible.png / practice-visible.log |
| 보존 검사 | git diff상 캠페인 Assets/Scripts·ProjectSettings·builds/EungyeTaxi·distribution·config/maps 변경0 | 다른 checkout은 codex/passenger-type-variants 유지 |
| 리뷰 | 거절검사 거짓통과·권한수렴 측정·stale build metadata·held 입력 보완 후 추가 중대 문제 없음 | 최종 소스 읽기 검토; 사람 플레이 검증 아님 |

네트워크 조건은 같은PC의 독립 실행물과 UDP proxy로 주입한 값이다. 최종 정지 후 오차0이 주행 중 항상 오차0이라는 뜻은 아니다. 특히150ms/3% 최대0.839m 보정은 사람 체감과 화면 보간을 추가 검토해야 한다. 두 번의 프로세스 실행은 별도 물리PC의 Host/Client 교환 검증이 아니다.

화면 PNG SHA256: 8B763386E24CC5C0DEE4EF8FF30079575B36FEE7AC0752CF0F3A5073AFFB819D. 이 화면은 네트워크 수정 전 같은 시험장의 시각 확인이며 네트워크 HUD를 직접 확인한 증거는 아니다. 최종 빌드의 화면 없는 두 Player 결과와 구분한다.

### 주요 변경 파일

- unity/EungyeTaxi/Assets/Pvp/Runtime/Network/PvpNetworkMotor.cs, PvpNetworkSession.cs, PvpNetworkProbe.cs 및 meta.
- unity/EungyeTaxi/Assets/Pvp/Runtime/PvpBootstrap.cs, PvpPracticeSession.cs, PvpPracticeProbe.cs, UI/PvpPrototypePanel.cs.
- unity/EungyeTaxi/Assets/Pvp/Editor/PvpNetworkTests.cs 및 meta, PvpBuild.cs.
- unity/EungyeTaxi/Packages/manifest.json, packages-lock.json; Assets/DefaultNetworkPrefabs.asset 및 meta.
- tools/test_pvp_network.ps1, tools/pvp_udp_test_proxy.py.
- docs/content-design/pvp/07-network-and-build.md, 08-development-roadmap.md, 09-decisions-and-balance.md, 13-direct-ip-network-plan.md.
- 이 작업 이력과 docs/work-history/LATEST.md. 실제 커밋은 git log -1 --oneline -- 이력경로로 확인한다.