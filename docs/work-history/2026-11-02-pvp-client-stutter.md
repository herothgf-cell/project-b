# 2026-11-02 LAN 참가자 화면 끊김

## 요청·보존
같은PC 두창에서 연결은 유지되나 참가자 화면/차량이 버벅임. PC 입력을 건드리지 않고 마지막 필수검사·실행본/이력/커밋·푸시. 시작6ab68d9/clean에서 원격main4900cea(P1/P2+지도·두카드UI)를 fast-forward 보존했다. 다른worktree 게임PID12248은 조작/종료하지 않았다. A2지도/기존싱글/미커밋맵은보존.

## 원인조사·실패 포함
`systematic-debugging`으로연결단절이아닌시각/처리부하로확인. source에서10Hz메시지마다수신현재위치부터0.1초보간재시작+기존Rigidbody보간이겹침,2Hz상세복원때SetGoal/RestoreScenario경로재생성,매프레임NPC표시중복적용,동일시설정보캐시초기화를확인했다.

- 계측기만추가한 baseline BuildValidated exit0, native-lan-20261102-175515-ba21b195 host26/client31/재접속 exit0. client상세복원23건 평균10.76ms/p9534.52/max140.72ms. 수신112건평균3.28ms/p952.64/max144.09ms. frame최대에는재접속23초로딩이섞여성능근거에서제외한다.
- 첫후보 BuildValidated exit0/보간6회귀. native-lan-20261102-180127-add40d33 host26/client31 exit0이나 client상세평균13.97/p9566.99/max160.46ms로개선미수용. 계측을경제/법규/승객/최종목적지로세분화하고경로재사용/재계산횟수를추가했다.
- fixed-build2는Editor검사후asset파일매핑잠금으로Windows빌드 exit1. 실행중게임은종료하지않았으며사용자실행폴더와다른artifacts/pvp/native-perf-candidate2로빌드를분리한다. 실패실행본은배포하지않는다.

## 구현/검사 진행
새PvpNativePoseTimeline:실제host경기시간순16샘플/100ms지연보간/최대100ms위치외삽/warp·역시간·정지시간처리. 참가자body의물리보간중지,호스트물리는그대로. 변화없는운송/목적지/옵션만network전용복원경로재사용, 일반싱글복원경로는그대로. 공유점유차표시객체재사용. 진단은probe에서만표본보관.

세분계측/최종실행·렌더링/배포결과는아래에실제값을추가한다. 아직새수정의성능완료를주장하지않는다. headless는GPU/사용자두창체감/다른PC를대신하지않는다.

## 직접원인·같은빌드비교
세분계측에서 nav-reused22회인데도jobs복원이52ms였다. ServiceLawZones.BeginPickup→nav.RequireVia(-1)→hdRoute=null/Refresh가캐시뒤에서재실행되는원인을확인했다. networkSnapshot에서만캠페인코스Prepare/Begin을제외하고네비옵션변경때만추가Refresh한다. 일반RestoreLive는기존경로유지. 시설정보동일시캐시유지/NPC표시10Hz만적용. 후보3 host-model IOException은검사phase파일공유읽기경합으로판명, tmp완료후원자Move로수정했다.

candidate4 동일빌드의10초같은승객유지구간:
- LegacyRouting 182623-2fa41571: steady복원평균68.75/p95103.82/최대103.82ms. host26/client31 exit0.
- 수정182735-f9c7c655: 평균0.15/p950.60/최대0.60ms. host26/client31 exit0/경로재사용22회.
- 숨김D3D 182846-c2039e74: 검사는exit0이나PNG가black이므로실제렌더수용에서제외.
- 참가자표시D3D11 183138-768b62ed: host28/client33 exit0,1280x720 PNG실제열람. steady복원0.13/p950.24/최대0.24ms. 참가자1208frame 평균16.90ms(약59.2fps)/p9517.97/max98.52ms. 로딩1초이상공백/초기2초제외. 초기경로등단발지연은남으며두창동시노출FullHD/다른GPU/장시간60fps보장은아니다.

후보4 PvpNativePerfBuild.Build (동일PrototypeBuild.BuildAt/ProjectMapBuild.RunTests+Prepare/별도출력) exit0/타임라인6·Native18·기존전체Editor. tools/test_prototype.ps1 28묶음 exit0; test_pvp_passengers.ps1 -Executable artifacts/pvp/native-perf-candidate4/EungyeTaxi.exe -OutputDirectory artifacts/pvp/client-perf-passenger4 exit0/38PASS.

게시직전원격main81b48b1의제품명통일을보존통합한다. 자동안전검토가보존확인없는git restore를거부해명령은실행되지않았다. artifacts/pvp/pre-naming-integration-preserved에현재빌드139파일과씬을전수백업하고SHA256동일검증을완료했다. 해당데이터는통합/최종배포뒤에도남긴다. 위비교는명칭통합전성능근거이며최종결과는아래에추가한다.

명칭81b48b1 통합은확인된백업검증뒤git restore/merge가허용돼완료됐다. 캠페인/편집지도저장호환도원격main소스를그대로유지한다. 후보5에서제품명실제assert와최종재접속/렌더·싱글복원검사를진행한다.

승차후큰길/짧은길요청복원도같은옵션에서SetPassengerPreference를반복하는경로가있어replica에한해옵션변경때만적용한다. 캠페인복원은기존대로다. 실제client내임시OnTrip모델로동일짧은길요청복원뒤hdRoute객체유지를assert하고즉시원래상태로복원한다(서버상태/차량위치변경없음;실제운송증거아님). 후보6최종검사에포함한다.
