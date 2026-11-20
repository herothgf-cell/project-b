# 은계 지명·차량 효과음·차내 요청 손님과 생활 시설 시안

2026-11-20. 임시 승하차명을 실제 지명으로 통일하고, 경적 및 네 가지 효과음 설정, 기존 조작을 요청하는 승객을 추가했다. 음식 메뉴 및 생활 시설 배경은 시안별 승인 대상이다.

## 플레이와 변경 파일

- M 호출 목록, 승차 표지, 운전석, 기사 수첩, F8 운송 기록과 영수증에서 같은 표시명을 사용한다. 기본 12곳과 추가 12곳의 도로·지점 위치를 프로젝트 지도의 도로명 및 좌표와 대조했다. 같은 도로는 동서·남북 구간 또는 반대편으로 구별한다. 차로 이름이 빈 지점은 가까운 수인로(약 128m), 은계로(44m), 은계호수로(38m) 일대·인근으로 표기했다. 저장 ID·좌표·원본 지도는 유지했다. 주요 코드는 KoreanText, PassengerStopCatalog, LogbookNeighborhood, DrivingLaw, BusinessProgress, ServiceStops다.
- 운전 중 Y/게임패드 버튼 3으로 짧게 두 번 울리는 경적을 재생한다. 속도와 모터 출력으로 주행음 음량·음높이를, 바퀴 제동 토크와 속도로 제동음을, 강수 강도로 빗소리를 결정한다. Esc → 설정 → 소리에서 개별 켜기·끄기, 적용·취소·기본값을 사용한다. 기존 기기 설정에는 네 소리가 기본 켜짐이다. 싱글·PvP의 로컬 운전자 기준이다. 관련 코드는 VehicleAudio, DriveSession, DriveHUD, BusinessHUD, DrivingSettings, DrivingSettingsPanel이다.
- 이야기 승객 배정 후 일반 풀을 뽑은 호출 중 후보 시드의 1/3에 창문 또는 온도 요청이 배정된다. 창문은 현재 상태의 반대, 온도는 20°C 또는 24°C를 요청한다. 실제 조작과 상태 일치 때 대사·만족도 +2·경력 체크포인트를 반영한다. 관련 코드는 PassengerBehavior, PassengerBehaviorUI, PassengerJobs, BusinessProgress다.
- 첫 실사풍 시안에 대한 사용자 피드백에 따라 기존 게임 시안을 이미지 참조로 사용해 다시 제작했다. 기사식당 메뉴 선택 화면 1장과 시설별 배경 8장을 개별 검토한다. [개정 시안과 생성 조건](../ui/2026-11-20-facilities/README.md).

## 검증과 재현

- tools/test_passenger_traits.ps1 53건, tools/test_passenger_behavior.ps1 707건 통과. 전자는 창문 양방향·원하는 온도·중복 만족도 방지를 검사한다. tools/test_passenger_reactions.ps1은 다음 요청까지 20초 이동 대기 검사에서 실패했다. 변경 전 17ad160의 승객 모델로 같은 실패를 재현했다.
- tools/check_workspace.ps1 -Strict 필수 입력 문제 0. Unity 2022.3.62f2의 분리된 프로젝트 사본에서 -batchmode -nographics -quit -executeMethod CabinSoundChecks.VerifyAndBuild로 지점 39곳과 대체 지점, 네 클립의 진폭·경적 간격 및 설정 저장/취소/복원을 검사하고 PrototypeBuild.BuildHybrid 종료 0. 최초 사본은 .git 루트 식별 조건으로 중단돼 사본을 Git 저장소로 초기화했다.
- tools/test_cabin_controls_player.ps1 -Executable <통합 실행본> 전용 임시 경력 위치 제어 27건 통과. 일반 풀 창문 요청, K로 요청 충족, 저장 반영, 오디오 소스, 기존 온도·미터기·운임을 확인했다. 관련 검사 파일은 CabinSoundChecks.cs, ShiftDevEndProbe.cs, tests/PassengerTraitTests.cs다.
- 음향 출력 장치와 실물 키보드·패드 청취, 다른 PC에서의 체감, 자동 물리 주행과 전체 캠페인 수동 운행은 이번에 검사하지 않았다. 음식 메뉴와 시설별 배경은 승인 대기다.

## 기본 실행본 배포

소스 커밋 50115ad의 BuildHybrid 산출물 중 게임 코드 DLL을 기본 실행 폴더에 반영했다. 기본 builds/EungyeTaxi/EungyeTaxi.exe의 위치 제어 검사를 다시 실행해 27건 통과와 종료 0을 확인했다. 통합 및 PvP VerifyOnly 모두 출하 파일 143개(135,321,366바이트)의 크기·SHA256과 별도 사용자 콘텐츠 1개를 확인했다. 변경된 배포 파일은 EungyeTaxi_Data/Managed/Assembly-CSharp.dll과 distribution/EungyeTaxi-build-manifest.json이다.
