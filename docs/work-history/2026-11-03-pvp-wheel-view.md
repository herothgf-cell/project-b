# PVP 참가자 바퀴 표시 보정

## 요청·범위
사용자 영상2222.mp4(11.47초/1594×898/30fps)의 뒷바퀴가 커졌다 작아져 보이는 현상만 수정. 골목/맵·차량 물리·싱글·승객/컨디션/배점과 다른 작업은 변경하지 않는다. 사용자 요청에 따라 필요한 마지막 검사만 수행했다.

## 원인과 구현
타이어 scale은 고정이다. 차체는100ms지연 타임라인으로 보간하지만 바퀴 localPosition/rotation은10Hz수신 즉시 적용해 다른 시각을 표시했다. 또한 raw Rigidbody 차체에 host 표시 시점의 WheelCollider pose를 상대화하면 바퀴 장착점이 앞뒤로 흔들릴 여지가 있었다.

PvpNativeWheelView는 차체와 같은 playback 시각으로 바퀴 로컬 자세를 보간한다. 수신 공백에는 마지막 서스펜션 값을 유지해 외삽 과장을 막고 최초/종료/새세션에는 즉시 정렬한다. 송신 바퀴는 host 표시 Transform 기준으로 상대화하며 X/Z는 실제 WheelCollider 장착점에 고정하고 Y 서스펜션과 회전은 보존한다. 타이어 scale과 실제 물리 WheelCollider는 변경하지 않는다. 참가자의 네 바퀴가 같은 경로를 사용하며 관찰이 쉬운 뒷바퀴 현상을 수정한다.

변경: PvpNativeReplica.cs, PvpNativeWheelView.cs(+meta), PvpNativeWheelTests.cs(+meta), 빌드 생성 FirstDrive 씬 ID, 관련 문서/배포 manifest. PvpCityMap/도로·TaxiController는 변경하지 않는다.

## 수행한 최소 검사
- Unity2022.3.62f2 `PvpNativeWheelTests.Build`: wheel8(수신 즉시 점프 없음·동일시각 서스펜션/회전 보간·공백 외삽금지·scale고정·장착점·중복/초기화), 기존pose11, Windows build return code0. 실패한 검사 없음. 독립 읽기 검토 중대사항 없음.
- `pwsh -File tools/test_pvp_native_lan.ps1 -Executable artifacts/pvp/wheel-fix-candidate/EungyeTaxi.exe -MotionOnly -Rendered -RenderWidth 1600 -RenderHeight 900 -MotionSeconds 30 -TimeoutSeconds 240 -Port 17982`: exit0/host21+client22. 증거 native-lan-20261103-142255-5bf34cb8. 실제16.1863초/249.9023m/최고25.0477m/s·이동971frame/정지0·전체평균16.67/p9517.87/max21.01ms. 위치 제어 없는 native 페달 주행이며 관찰30초 중 실제 NPC 접촉 이후 정차는 이동 구간에서 제외한다.
- 실제 Player에서 표시 경로 실행/예외 없음과 주행 pacing을 확인했다. 바퀴 현상의 수동 전후 영상 A/B 판정·다른PC/장시간은 수행하지 않았다. 바퀴 표시 계약은 위8개 회귀로 검사한다.
- 맵/승객/4대 운송 전체·재접속·캠페인 전체 검사는 이번 바퀴 표시 변경에 반복하지 않았다. 기존 수용 결과와 구분한다.

## 재현·배포
Editor: `-batchmode -nographics -quit -projectPath <루트>/unity/EungyeTaxi -executeMethod PvpNativeWheelTests.Build -logFile <루트>/artifacts/pvp/wheel-fix-build.log`. 일반 배포 기준은 기존 PrototypeBuild.BuildHybrid이며 helper도 같은 BuildAt(eungye)와 ProjectMapBuild.Prepare를 사용한다.
실행은 builds/EungyeTaxi/EungyeTaxi.exe 통합 메뉴. 양쪽 기존 창을 닫고 전체 실행 폴더를 갱신해 같은 실행본으로 접속한다. 다른 체크아웃/사용자 별도 배포 폴더는 자동 교체하지 않는다. 실제 파일/manifest/게시 결과는 후속 영수증을 따른다.


## 실행 폴더 반영
실제 소스 `5f6b9f33dcf88d8b3a3a399d10d622e0d88a9e8e` 확인. 기존139파일115,321,622바이트 manifest검증후 기존canonical실행중없음을확인했다. 기존폴더전체를artifacts에보관하고최종wheel-fix-candidate139파일115,322,814바이트를새복사해후보/배포 상대경로·크기·SHA256전수일치를확인했다. DoNotShip디버그는별도보관했다. 다른체크아웃/사용자배포복사본은바꾸지않았다. manifest와게시영수증은아래를따른다.
