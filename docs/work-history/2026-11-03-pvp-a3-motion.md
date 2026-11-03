# A3 적용·참가자 연속 주행 후속

## 요청과 보존
사용자 제공 `녹음 2026-11-03 103527.mp4`의 참가자 덜컥임 개선과 기존 A3 골목 도시 시안 적용. 다른 작업·싱글·저장·지도/GIS/ZIP은 보존하며 마지막에 필수 검사만 묶는다. 영상 원본은 변경하지 않으며 Git에 추가하지 않는다.

## 영상과 원인 대조
로컬 ffmpeg 추출로22.77초/1588×922/30fps/683프레임을 확인했다. 운행 HUD에는 LAN 참가자·속도15→89km/h가 나타난다. 연속 프레임의 장면 차이 분석에서 완전 정지에 가까운 프레임이 초당1~2개 정도이며 2프레임 이상 연속한 긴 정지와 구분한다. 원본 영상의30fps 제한이 있어 실제 게임 GPU 프레임 수는 이 수치로 추정하지 않는다. 원본/프레임·장면 차이CSV는 로컬 `artifacts/pvp/video-20261103/`에 보존한다.

원본 전송은 FixedUpdate의 `match.Elapsed`에 호스트 화면 보간 `taxi.transform` 자세를 붙였다. 권한 속도는body에서 읽었으므로 시각·위치·속도의 기준이 달랐다. 이번에는body.position/rotation과 같은 기준의 바퀴 상대좌표를 전송한다. 표시 커서는 수신 공백에도 계속 앞서가던 것을 최대 newest+100ms로 제한한다. NGO/전달 정책을 증거 없이 교체하거나 클라이언트 위치 권한을 추가하지 않는다.

## 구현·검증 구분
`PvpNativeLanProbe`의 기존6초/14m 뒤 정지 평균FPS만으로 부드러움을 판정하지 않는다. 선택한30초 실제 페달/H조향 연속 주행에서 타깃·카메라 이동량, 시각/수신 간격/표시 여유와 정지 프레임을 기록한다. 호스트body 대 화면자세 차이와 실제 snapshot의 물리 기준 일치도 확인한다. 위치 이동/강제 경로 좌표 제출은 없다. 기존 알림 검사는 명시적 서버 사건 fixture이며 물리 신호 위반 주행과 별개다.

첫 최종 묶음 Editor 실행 `a3-motion-build1.log`는 Native보간11 회귀 뒤 시장 일방 구간 개수 기대값4에서 실패했다. 시장은 대축으로 나뉜2개 셀에4개씩 총8구간이므로 정확한 기대값을8로 정정했고 구간별 합법 복귀 검사는 유지한다. 실패 실행본은 배포하지 않는다. 지도 명칭의 A2 문구도 A3로 맞춘다.

## 최종 수용
다음은 시도별 실제 결과다. 화면/접속은 ‘최종 candidate9’, 마지막 회복/물리 운송은candidate11 및 배포 영수증을 기준으로 한다. 실패 후보는 배포하지 않으며 전체 끊김/모든 도로의 장시간 무정체를 주장하지 않는다.

- 두번째 묶음 `a3-motion-build2.log`는306방향쌍·시설 접근 뒤4출발접근 검사에서 실패했다. 목표 정차점과 완전히 같은 그래프 좌표에서 다시 같은 목표로 길찾기한0거리 사례였으며, 실제 출발 접근을 목표1m앞으로 옮겨 검사한다. 경로 연결 단언을 삭제하지 않았다. 실패 후보 배포 없음.
- 중앙역4/시장·병원·서부·동부 각2개 부분의 서로 다른 구조를 추가했다. 일부 링 변을 제거하고 앞순환·후면T·개방형 북측T·4방향 부채꼴·엇갈린3갈래를 사용한다. 최종 지도는236도로/672방향 표시/6차로8구간/시장8일방구간이다. 원시 연결선 정적 교차 검사16셀/156개 지역 링크/최소명목길이101.2m·교차0은 소스 검사이며 물리 주행 증거가 아니다.
- 세번째 묶음의 `PvpCityA3Tests.Run`:6,289검사/306방향쌍 통과. 실제 곡선 교차·거점/시설 진출입·일방 합법복귀를 확인한다. 단순0거리 도착도 의미 있는 접근 경로로 구분했다. 나머지 최종 결과는 아래에 기록한다.

- 세번째 묶음 합류 검사는 lane/bay/parking/convoy/corridors3090/queue/horizon/futurewindow 등을 통과했고 Guards·DriverIntent의 A2 고정 거리 전제에서 실패했다. A3 실제 진입선12m 앞에서 출발시켜 ‘이동0/허용간격1m미만’을 요구하던 검사 위치를 실제 진입선 바로 앞으로 옮겼다. 차단물도 공통 대표점 대신 해당 차로 출구의 실제 clearance 끝에 배치한다. 운영 코드/안전 단언/몸체 겹침 기준은 변경하지 않는다. 사용자 최소검증 지시에 따라 실패한 두 검사와 아직 미실행인 시설 검사·컴파일/빌드만 `BuildRemaining`으로 이어 수행하며 이미 통과한 동일 운영 코드를 전부 반복하지 않는다.

- `a3-motion-build4.log` / `PvpNativePerfBuild.BuildRemaining` return code0 / PROTOTYPE_BUILD_OK. Guards실제차단/해제후이동(169.95/88.03m)·DriverIntent·시설2,609검사/주차4·식사2 및11시각회귀 통과. 나머지 동일 운영 코드의 통과 증거는build3에 보존했다. 다른 채팅의 simulation-p1-p2 Unity 빌드/미커밋/실행본은 읽기 확인만 하고 변경하지 않았다.
- 네 대 물리 운송 `a3-physical-four1` 실행 중 비볼록 구조물에 지원되지 않는ClosestPoint 호출이 반복되는 실제 오류를 발견했다(예:sim308초까지1,035줄). 오류 로그만 끄지 않고 지원되는 실제 기하 조회로 교체한 뒤 최종 실제 결과를 기록한다. 이 후보는 아직 배포하지 않는다.

- 물리 후보1에서sim500초대 이후 한 대는10,540원 실제 운송을 완료했으나, 회복을 마친 다른 봇이 같은 운송 횟수에서 추가 컨디션 판단을 막는 정책에 걸려 호출 선택 상태로 머무는 것을 재현했다. 소유 프로세스18932의 경로가 후보1임을 확인한 뒤 중단했고 실패를 보존한다(완료 수용 아님). 식사·휴식을 상호 보완적으로 재판단하되 이미 회복한 같은 종류를 반복하지 않도록 보완한다. 식사의 미래 효과가 피로100 클램프에 가려지는 경우도 안전 운행 가능시간으로 판정한다. 비용/효과·승객 안전 수락 기준은 바꾸지 않는다.
- 비볼록Mesh/terrain 조회는Unity2022.3 [ComputePenetration](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Physics.ComputePenetration.html)의 primitive+다른콜라이더 지원 계약을 따른다. Box/Sphere/Capsule/convexMesh는 기존ClosestPoint식을 그대로 유지하며 비볼록형은 실제 구면 교차로 검사한다. 첫probe의 비활성화 collider는 신선한 native shape가 없어 양성 기하 검사가 실패했다(build5); 주행 세계 밖의 고립된trigger로 shape를 유지한 build6은 기하15(실제삼각형/빈AABB간격/회전·스케일/기존9case)·회복5·기존컨디션5그룹/도시·저장·시각11·빌드return0을 통과했다. 단순AABB로 장애물을 대체하거나 로그를 끄지 않았다. 추가 회복 검사는 실제 초기80포만/초당1/12감소와 +60식사의 유효 범위40을 반영한다.
- 게시된 최신main337ab94(승객 대화 UI·응답/효과 분리)를 확인했다. 유일한 미커밋 중복은 빌드 재생성씬의8개 내부fileID였으므로 로컬 artifacts에 바이트백업·SHA256 확인 후 HEAD씬으로 복원했다. 이후origin/main을fast-forward하여 대화/승객 소스·원격 실행본/문서 전부를 보존했고 PVP 미커밋 소스도 유지했다. 다른 체크아웃에는 쓰지 않았다. 통합 새 후보2로 다시 빌드·최종 런타임 검사한다.

- 통합build7/8 return0, main337ab94 보존·기하15/회복6/Native21·승객/테마·내비 통과. 일반차도 raw Rigidbody 위치/회전을 같은물리시각으로 전송하도록 맞췄다.
- 첫1600×900 30초 LAN 기록 `native-lan-20261103-112930-9800a55b`는 **끊김 미수용**이다. 움직인16.00초/249.32m·최고25.02m/s(약90km/h), 참가자움직임frame평균16.67/p9517.85/max20.65ms인데 수신간격p95299.87/max633.11ms, 정지57frame/최장383.33ms였고 커서가 최신자세+100ms 끝에 닿았다. 평균FPS가 원인을 가리므로 통과로 처리하지 않는다. 호스트의화면/물리위치차 최대0.4897m는 시각불일치 원인도 실제로 확인한다. 고속 주행 뒤 충돌/진입차로 이탈 상태에서 뒤이은 수락 검사가 실패했고 러너는클라이언트실패 직후호스트를정리해hostdidnotfinish로보고했다. 이후 주행pacing과수락/알림/재접속은 시작조건을분리한다.
- 경로 비용은 매HD폴리라인 조각마다 느린일반차 전체를 재검사했다. PVP/복제에서100m셀의 후보만 같은OnSegment식으로 정밀검사하도록 바꾸고 큰조회는기존선형식으로돌린다. 도로사건·방향·비용식과싱글분기는유지한다. 기존식대조1,500조회·경계/스냅샷교체1 =1,501회귀 통과(build9). 원래CPU측정scope를native진단에도연결해호스트경로/봇처리 시간을남긴다. 실제주행pacing은연속정지100ms이하·정지비율1%이하를별도러너기준으로검사하며, 아직재실행결과는아래에기록한다.

- Index만 적용한후 `native-lan-20261103-114107-38e4150e`는정지75/971/366.65ms로pacing불합격. 계측에서호스트PvpBot.SelectOffer 평균73.25/p95105.49/max107.64ms,봇Tick최대266.49ms를확인했다. 호출별여러Dijkstra/모든경로추정을반복한비용이었다. 하나의탐색트리로후보거리/같은FixedTick예측을공유하며lanechange12비용/동점순서/Compose/뒤쪽동일차로합법순환은기존식을유지한다. 실제18×18+18루프342대조 통과(build10). 시간만으로도불가능한호출/선형거리250m밖길가손님은같은선택규칙의안전한하한으로먼저제외한다.
- 최종운영소스 candidate4의1600×900·20분설정 주행창30초 `native-lan-20261103-115329-aa89b4c4` exit0: host21/client22, 실제이동16.2037초/248.9225m/최고25.0483m/s(약90.17km/h), 이동972frame 평균16.67/p9517.84/max21.74ms, 정지frame0/최장0ms. 수신300건평균99.54/p95166.55/max199.84ms. 고속으로실제NPC와접촉해이동이멈춘구간은‘네트워크정지’와구분하므로30초전부이동했다고주장하지않는다. 기존실패창은5분설정/봇정지상태였고이번은사용자영상과같은20분설정이므로엄밀한동일조건A/B성능비교는아니다. rawbody/화면자세차약0~20ms도실제관찰했다.
- 같은candidate4 접속/알림/권한/저장격리/봇승계/재접속 `native-lan-20261103-115639-f9a87a47` host30/client38=68, exit0. 기존신호위반−20/만족도−12fixture·반복중복없음·동일좌석복귀/정리통과. 이기능검사는고속충돌후수락이아닌초기정상차로상태에서분리했다.
- candidate4 `-unifiedModeCheck` exit0/`UNIFIED_MODE_RESULT PASS active_seconds=111.1655` (`a3-unified-final.log`):PVP→메뉴→실제싱글임시저장/복원·손상저장거부/원본/백업보존·실사용4파일불변. 기능검사는별도숨김프로세스에서함께수행했으며FPS근거는그전에단독표시한주행창만쓴다. 실제A3 M지도PNG를기획29에보존했다.
- 네대물리후보2는모두3.4~4.2km실제주행/픽업·운송·회복/출차를진행했으나,비동기서비스완료의정상Close가TimeScale1로되돌려검사의3배가속을유지하지못해600초벽시계제한에서실패했다. 이는실제멈춤과구분한다. 검사전용LateUpdate에서비모달운행시의가속3을유지하고같은자연상태/요금/피로/페달/조향단언으로최대900초만재실행한다. 제품의서비스속도/시계정책은변경하지않는다. 마지막candidate5변경은이진단클래스의속도유지/제한뿐이며위운영소스와pacing/68/싱글수용은같다.


### 마지막 골목 정체·main 보존

candidate5의4대 자연 물리 운송은900벽초 내3대만 유료 운송을 완료했고 봇2는3,573m 이동 후 무신호 사선 교차로 `city-junction:alley:a3:6:0:3:a`에서NPC64와 서로 막혔다. 이는 가속검사 문제와 별개의 실제 실패다. NPC만의 junction reservation에는 physical bot taxi가 없었고, 택시는 일반 충돌 센서만으로 접근해 서로 다른 회전 경로 안에서 맞물렸다. A3 무신호 교차로의 앞8m/전체connector/출구8m에 기존 geometry-aware NativeTrafficMergePolicy를 공유한다. 겹치지 않는 경로의 동시 진입·실제 점유 유지/출구 확인을 유지하고, 이 구역에서만 중복 NPC전용 예약을 공통 정책으로 대체한다. 신호 교차로와 싱글은 기존 규칙이다. 현재 player를 순간 이동하거나 겹친 collider를 무시하지 않는다.

main의 후속 신호 카운트다운 `5a5df15`까지 fast-forward 보존 통합했다. 충돌한 변경은 자동 재생성 씬 ID뿐이며 실제 파일을 artifacts에 전체 해시 대조 백업 후 HEAD로 복원하고 main을 반영했다. 대화/카운트다운 소스·스크린샷/기록과 다른 체크아웃은 보존한다.

build13은 새 Editor fixture의 `var` 다중 선언 컴파일 오류로 실패했고 수정했다. build14의 회전 fixture는 두 차의 물리 분리를 유지하고 각618/155m 이동했으나, 자연 goal 도착 후 Continue가 TrafficRoute를 교체/갱신하여 검사 기준 길이가 바뀌었다. 최초 경로 길이를 보관해 그 실제 통과 여부를 검사하도록 고쳤다. 물리 분리/통과 단언은 유지한다. 무신호 공유 정책의 마지막 결과는 아래에 기록한다.


무신호 공유 메타데이터 첫 시도(candidate6/build16)는 접근 차로에도 붙은 ruleId를 connector로 해석하여 구역이 지나치게 길었다. fixture가 남은 통과 거리 실패로 이를 드러냈고, `join:` 연결선만 포함하도록 수정했다. 진행 중이던 소유 진단 player PID21944만 중단했고 사용자 게임 프로세스는 없었다. 이전 candidate6의4대 결과는 수용하지 않는다. 최종 build17 Editor `SIGNAL_COUNTDOWN_OK`, `PVP_UNCONTROLLED_SHARED_OK connectors=1029 physicalPairs=2`: 기록된 사선 교차로의 서로 충돌하는2개 경로쌍을 실제 NPC Tick/Physics.Simulate로 이동시키고 매step 차체 분리·양쪽 최초 경로 통과를 확인했다. NPC/봇 공통 정책은 실제4대 player에서 추가 확인한다.


최종 공유진입 검토에서 수동 사람 택시는 공통 fleet 권한 대상이 아니므로, 기존 NPC의 전체 이동경로 실점유 검사를 조기 반환으로 생략하면 안 된다는 지적을 반영했다. NPC-only 예약 경쟁/Add만 제외하고 ReservedMovementBounds/OccupiesMovement/ExitClear는 유지한다. 출구는 비어 있지만 connector 중앙에 미등록 수동차가 있으면 MayEnter가 거부하는 회귀를 추가했다.

candidate8(신호 카운트다운 포함) 표시 재확인 `native-lan-20261103-124256-c58f8948` exit0/21+22. 실제16.302초/249.046m/최고25.039m/s, 이동978frame 평균16.67/p9517.81/max20.70ms·최장33.40ms 멈춤으로100ms 필수 기준 통과. 추가한 공통 진입의Traffic.Merge 표본평균0.02/p950.10/max0.47ms. 이후 수동 점유 보호 보완의 최종 결과는 아래를 따른다.

작업 시작 때의4개 사용자 저장 해시는 전체 작업 시간 동안 동일하지 않았다. 최종 확인에서 campaign 본문/백업은11:55:05~08, driving/백업은11:30:12~43에 갱신된 상태였다. main 신호 카운트다운 이력에도 별도 사용자 실행본 `C:/git/프로토 게임 모음/EungyeTaxi/EungyeTaxi.exe`의 자동 저장과 겹친 기록이 있다. 이 파일들을 원래 해시로 복원하거나 사용자 게임을 종료하지 않는다. 이번 격리 single probe 자체의전후 PASS와 전체 작업의 불변 주장은 구분하며, 최종 남은 검사 직전 현재 campaign파일 전체 해시를 다시 보관해 이후 차이만 대조한다.


build18 추가 수동 점유 fixture는 기존 forced Route helper가 TrafficStop을 만들지 않아 First 검색에서 실패했다. fixture를 실제 DistrictTraffic.CreateRoute로 구성하고 선택한 connector를 실제로 포함하는지 단언한다. 게임의 검사 조건/이동 권한을 완화하지 않는다.

## 변경 파일과 재현

- 지도: PvpCityMap/PvpCityRoadBuilder/MapCatalog, 기획29/30/README·실제 지도PNG, A3/A2·시설/합류 fixture.
- 복제/계측: PvpNativeReplica/PvpNativePlaybackClock/PvpNativeLanProbe/PvpNativePerf/TrafficPerformanceProbe, tools/test_pvp_native_lan.ps1.
- 병목/봇: HdRoadGraph/RoadConditions/PvpSimulationBot/PvpSimulationBotServices 및 경로342/색인1501/회복 회귀.
- 교통: TrafficColliderGeometry/TrafficCar/TrafficSystem/NativeTrafficMergePolicy/PvpTrafficMergeLayout/UpstreamUturnObserver, Collider15·무신호 공유 회귀.
- 빌드: PvpNativePerfBuild(PrototypeBuild.BuildAt와 동일 eungye entry/ProjectMap 준비), 생성 FirstDrive 씬·최종 실행폴더/manifest. native4대 probe에는 진단3배속 유지/제한만 보완.

로컬 Unity 설치: `C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe`. 저장소 루트에서 `-batchmode -nographics -quit -projectPath <루트>/unity/EungyeTaxi -executeMethod PvpNativePerfBuild.BuildJunction -logFile <루트>/artifacts/pvp/a3-build.log`로 마지막 변경 관련 Editor+Windows 묶음을 실행한다. 일반 전체 배포 빌드는 기존 `PrototypeBuild.BuildHybrid`다. helper 후보 경로는코드의Finish를 따른다.

`pwsh -File tools/test_pvp_native_lan.ps1 -Executable artifacts/pvp/a3-motion-candidate11/EungyeTaxi.exe -MotionOnly -Rendered -RenderWidth 1600 -RenderHeight 900 -MotionSeconds 30 -TimeoutSeconds 240`는 실제 페달/표시 pacing 수용이다. `-Reconnect`를 대신 주면 일반 권한/알림/재접속 검사다. 두 옵션은 함께 쓰지 않는다.

`pwsh -File tools/test_pvp_simulation.ps1 -Executable artifacts/pvp/a3-motion-candidate11/EungyeTaxi.exe -FourDrivers -TrafficMonitor -OutputDirectory artifacts/pvp/a3-physical-final -TimeoutSeconds 1200`은 native4대 물리 운송/정산이며 위치 제어가 아니다. fixture의7200초 경기/3배속은20분 경제 밸런스 수용을 뜻하지 않는다.

## 별도 수용

다른PC 사람4명·공유기별 지연/손실·장시간, 두 창 FullHD 동시 표시, 사람 직접 조작으로 전체 운송/법규,20분 경기에서 식사/휴식·운임 전략의 경제 밸런스는 미수용이다. 짧은 자동 검사에서 모든 끊김/정체가 사라졌다고 주장하지 않는다. 물리 NPC 접촉 후 실제 정차는 snapshot 반복 정지와 구분한다. 초기 지도/첫 경로의 단발 지연도 장시간 체감 검사에 남긴다.


### 최종 candidate9

- build19 / PvpNativePerfBuild.BuildJunction: Unity return code0 / Windows PROTOTYPE_BUILD_OK. main 카운트다운 회귀와 무신호1029connector·2물리 교차쌍(매step 차체 분리/양쪽 진행), 출구clear·미등록 사람차 중앙점유 거부를 통과했다. 독립 읽기 검토의 수동점유 보호 지적을 반영했고 추가 중대 지적은 없었다.
- 단독 표시 연속 주행 `native-lan-20261103-124750-be6fafc1`: exit0/host21+client22. Direct3D11/1600×900, 실제16.1844초/248.7883m/최고25.0501m/s(90.18km/h), 이동971frame·정지0/최장0ms. 이동평균16.67/p9517.75/max20.33ms, 전체참가자2312frame 평균16.67/p9517.90/max20.84ms. 패킷300개평균99.63/p95166.53/max200.02ms. Traffic.Merge평균0.02/p950.10/max0.39ms. 관찰30초 중 실제 물리 접촉 후 정차는 이동구간에서 제외했다. 이실행이 마지막 소스의 화면 pacing 근거다.

- 최종 일반 LAN `native-lan-20261103-124949-19a4fdf7` exit0 / host30+client38=68. 실제 페달13.9246m, 같은 운송/목적지 route재사용, 승객/법규 복제·알림 원인/중복방지, 봇승계·동일좌석 재접속·메뉴 정리 통과. 알림 신호위반은 서버 사건 모델fixture다. 이실행은4대 기능검사와 함께 숨김으로 수행했으므로 화면FPS 근거로 사용하지 않는다.


### 회복 재방문 후속 (최종 candidate10)

candidate9 실제4대 검사에서는3대가 native 양수 운송을 완료했다(11,230/12,570/18,460원). 남은 봇1은 식사 후 휴식을 마쳤지만fatigue60에서 같은 운송을 시작하기 전에 같은 parking4의 rest를 다시 계획해1,140m 재방문 경로를 탔다(time1287.9/DecisionCount3). 이는 차체 교착이 아닌 PVP 판단의 비생산적인 반복이다. 이 소유 player만 중단하고 최종 수용으로 표시하지 않는다.

같은 jobs.completed+failed 동안 이미 완료한 same-kind 회복 detour는 비긴급 상황에서 제외한다. 식사↔휴식의 보완은 허용하고 fatigue100 등 Critical은 같은 행동도 계속 가능하다. 인간/싱글의 상태·가격·시간·시설 이용 조건은 바꾸지 않는다. 기존6회귀에 같은 휴식/식사 반복거부·보완허용·긴급반복허용4개를 추가했다. 마지막 서비스 판단 변경은 네트워크/표시 코드와 지도 기하를 바꾸지 않으며 이미 통과한 전체 검사는 재실행하지 않는다. 실제4대 수용은 이 작은 수정의 실행본으로 확인한다.


build20 `BuildRecoveryFinal` Windows return code0. PVP_A3_RECOVERY_OK와 기존 PVP_CONDITION_EDITOR_OK 통과. 실행은 기존6+추가4조건식을 모두 거쳤으나 로그 문자열의 고정 checks=6이 증가분을 누락했다. 검사 뒤 Editor 출력 문구만10으로 바로잡았으며 런타임 서비스/페달/가격/회복 코드에는 추가 변경이 없다. 독립 읽기 검토도 비긴급 same-kind 제외·긴급/보완 유지·싱글 영향없음을 확인했다. A3와A2의full MapData JSON SHA256은 기존 접속 승인에서 다르게 판정되므로 양쪽을 같은 새 실행본으로 갱신해야 한다.


### 같은 시설에서의 추가 회복

candidate10 `a3-physical-final2`는3대 양수 운송·4대4.8~8.7km실제 이동/공유NPC·정산/종료동결은 통과했으나 봇2가900벽초에미정산하여exit1이다. 피로49 상태에서 모든 OfferRank가 컨디션 위험으로무한대여서600sim초 가만히 호출을검토하다가100이 되어 같은시설로재접근했다. 단순비긴급 재방문 마스킹으로는 충분하지 않았다.

PVP 봇이 이미 이용 가능한 실제 bay에 정차했을 때 native CanUse를 통과하는 추가휴식(피로>=40)·식사(포만<80)를 연속 선택하도록 했다. 각 Buy는 기존요금·15/45초·현금·정차·영업상태·완료ID/계수 조건을 그대로 거친다. 예를 들어 피로90→50→10까지실제45초씩 쉬며, 포만20→80 식사도 실제시간/요금을 쓴다. 가능한회복이없거나 준비가끝나면 기존출차경로로실제이동한다. 같은필요를 도로로돌아오는동안 회복이상쇄되던 고리를 끊고, 부족한상태에서안전하지않은호출을수락하도록 우회하지않는다. 추가4Bay회귀와기존10, PVP condition5그룹을 마지막소스로확인한다.

복잡한A3의 자연운송거리와3배속 실측유효속도를 고려해 진단의 벽시계한도만1500초(외부1800)로 늘렸다. 4대실제이동/양수native운송·정산/점유·NPC·종료/재시작 단언과7200초경기·3배속은 유지한다. 실제20분경기밸런스검사와 구분한다.


## 최종 실행 수용·배포 준비

- 최종 candidate11 build21/BuildRecoveryFinal Windows return code0, 회복14+기존 condition5그룹 통과. 마지막 읽기 검토에서 중대 지적 없음.
- `a3-physical-final3` / `tools/test_pvp_simulation.ps1 -FourDrivers -TrafficMonitor -TimeoutSeconds 1800`: exit0 / PVP_FOUR_DRIVER_OK. 4대 실제 WheelCollider 주행6506.750/6765.740/3895.772/6501.071m, 각각 양수 native 운송 완료. 차량/NPC를 순간 이동하거나 상태/운임을 주입하지 않았다. 공유8호출 중복소유 없음·NPC72대 유지/실제이동·정확히1회 native 정산/비용·경기 종료 점수/페달 동결·늦은정산거부·메뉴 정리·새4세션 재시작과 상태초기화 통과.
- native 수입은0번19,490/1번14,680/2번12,570/3번18,460원. 비용도 각 ledger에서 그대로 차감한다. 7200초·3배속 진단이며20분 경제 밸런스 수용은 아니다. 안전한 호출이 없는 위치/컨디션에서 보수적으로 대기하는 봇 판단·장거리 식사/휴식 선택의 경쟁 밸런스는 별도 조정 대상이다.
- 위 실제 물리 운송 전의 candidate9 단독 화면 주행은249m/약90km/h·반복멈춤0/평균16.67ms를 확인했고 일반LAN68·알림/재접속도exit0이다. 이후 candidate10/11은 PVP 회복 판단·진단 시간한도만 변경했으며 표시/전송/지도 기하는 같다. 최종 운송 검사에서는FPS를 주장하지 않는다.
- 격리 싱글 임시저장/복원·PVP→메뉴→싱글 수용은candidate4 UNIFIED_MODE_RESULT PASS/exit0이다. 이후 싱글 플레이/저장 로직 변경 없음. 사용자 실제 저장은 초기스냅샷과달리 외부실행으로수정된것을보존했고, 마지막재기준4파일은최종Native/LAN/4대검사후전부크기/SHA256 불변이다.
- 필수 지도/ZIP·캠페인 저장경로·다른 체크아웃·main 승객 대화/신호 카운트다운을 보존했다. 최종 실행 폴더/manifest와 원격 게시 영수증은 아래에 추가한다.


## 실행 폴더 교체

실제 소스 커밋 `2807ccec9d0dbbbf8f632982e07d2b9766160f8b` (`git log -1 --oneline -- docs/work-history/2026-11-03-pvp-a3-motion.md` 확인). 기존 통합 manifest140파일115,434,078바이트 VerifyOnly exit0 후 사용자 실행 중인 canonical player가 없음을 확인했다. 기존 폴더 전체를 확인된 artifacts 경로에 보관하고, 최종candidate11에서DoNotShip 디버그만 별도보관 후 새 통합 폴더를 복사했다. 후보/배포139파일115,321,622바이트의상대경로·크기·SHA256 전수일치. 이전빌드의남는level0.resS를 덮어쓰기잔재로남기지않았다. 원래 다른 체크아웃/별도 사용자 실행 폴더는수정하지않았다. manifest 및 원격 게시 결과는 아래 영수증을 따른다.
