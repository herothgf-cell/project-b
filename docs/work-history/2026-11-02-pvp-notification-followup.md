# PVP 참가자 알림·표시 후속

## 요구와 범위
예상 도착 상태 HUD 숨김, LAN 참가자의 신호 위반 운전 점수·승객 만족도 알림 누락과 텍스트만 보이는 알림 수정, 남은 작은 주행 끊김 보정. 사용자 요청에 따라 구현 후 필수 검사만 묶는다. 기존 싱글·다른 체크아웃 작업과 지도 입력은 보존한다.

## 구현
- `PvpNativeReplica`: 2Hz 상세 상태마다 LawLedger를 교체하여 DriveNotifications가 기준값/표시를 초기화하던 원인 수정. 첫 상세 상태만 새 기준을 만들고 이후 동일 객체에 사건 ID별 누적/강화 결과를 적용한다. 반복 패킷은 Revision을 올리지 않는다. 호스트의 만족도 변화 사유도 최종 승객 복원 후 전달한다. 계산/판정은 호스트 권한 유지.
- `DriveNotifications`: 흰 불투명 카드·테두리·분류 배지/색띠·진한 글씨, 4초 유지, 화면 비율 적용과 IMGUI 상태 복원. 점수/배점 변경 없음.
- `CockpitUI`: 운행 HUD 예상 도착 상태 카드와 계산 호출 제거. 지도·시설 비교용 상태 예측은 유지.
- `PvpNativePoseTimeline`/`PvpNativeLan`: 최신 자세 이후 최대100ms 범위에서 회전도 위치와 함께 외삽하여 회전 고정/재개를 줄이고, 호스트10Hz 송신 일정의 프레임별 누적 지연을 줄인다. 클라이언트 위치 권한/호스트 실제 물리는 변경하지 않는다.
- `PvpNativeLanProbe`: 기존 두 프로세스 입력/물리·선점·재접속 검사에 알림 수신/지속/중복 방지와 실제 알림 PNG를 추가한다. 알림만은 명시적 탑승 모델·서버 신호 사건/만족도 fixture이며, 실제 신호를 물리 주행으로 위반한 검사와 구분한다. 위치 이동 명령은 없다.

## 검증과 결과
최종 실제 명령·결과를 아래에 기록한다. 첫 Editor 실행은 회전 예상8도와 Unity 근사 결과7.967도의 차이에 기존 위치 허용오차0.001을 잘못 적용한 검사에서 중단됐다. 각도 비교를0.1도 허용오차로 분리했으며, 실패 실행본은 배포하지 않는다.

## 실행·남은 수용
기본 실행 `builds/EungyeTaxi/EungyeTaxi.exe` → PVP → 방 만들기/IP 참가. 양쪽은 같은 갱신 실행본을 사용한다. 실제 다른PC/4인·사람 장시간 운송/FullHD 두창 동시 표시와 모든 끊김 제거는 별도 수용이다. A2 도시를 유지하고 A3는 시안 상태다.

- Unity2022.3.62f2: `-batchmode -nographics -quit -executeMethod PvpNativePerfBuild.Build` (동일 PrototypeBuild.BuildAt / ProjectMapBuild.Prepare, 별도 출력) `artifacts/pvp/native-notice-build2.log` 최종 return code0/PROTOTYPE_BUILD_OK. 보간9·Native JSON/사건21·테마/내비15와 기존 빌더 Editor 검사를 통과했다. 첫 실패 후 변경은 각도 검사 허용오차뿐이다.
- `pwsh -File tools/test_pvp_native_lan.ps1 -Executable artifacts/pvp/native-notice-candidate1/EungyeTaxi.exe -Reconnect -Rendered`: exit0, `native-lan-20261102-192001-965bd768`, host32/client41 (73검사). 실제 페달 메시지→호스트 WheelCollider14.003m/참가자14.009m, 알림2종/1초 반복 수신 중 유지·중복 없음·같은차 봇승계/재접속·메뉴 정리 통과.
- D3D11/1280×720 실제 참가자 PNG `client-notices.png` 열람: 운전 신호위반−20/만족도 신호위반−12 두 흰 카드·분류 배지 확인, 예상 도착 상태 카드 없음. 호스트는 숨김·참가자만 잠시 표시했고 마우스/키보드 조작 없음. fixture가 만든 알림이며 실제 신호 물리 통과 수용은 아니다.
- 같은 실행의 참가자1308frame 평균16.97ms(약58.9fps)/p9517.83/max92.48ms, steady상세18건 평균0.31/p953.46ms. 로딩1초이상 공백·초기2초 제외, 조향 외삽은9회귀에 포함. 초기/계약 전환 경로 재탐색 단발 지연이 남으므로 모든 끊김 개선 완료로 판정하지 않는다. 앞 이력과 상황이 달라 엄밀한 전후 성능 비교가 아니다.
- `pwsh -File tools/check_workspace.ps1 -Strict`: exit0/Input issues0 (`native-notice-strict.txt`). 필수 입력/지도/ZIP 변경 없음. 별도 최종 집중 코드 검토에서 중대 결함 없음. 추가 전체 캠페인 모델/오프라인 전승객/장시간 반복 검사는 사용자 요청에 따라 수행하지 않았다.

- 최종 실행본 `-batchmode -nographics -unifiedModeCheck`: exit0 / `UNIFIED_MODE_RESULT PASS active_seconds=110.5845` (`native-notice-unified.log`). PVP→메뉴→은계 싱글 임시 실제 체크포인트 저장/복원·손상 저장 거부/원본·백업 보존, 실제 사용자 캠페인4파일 불변 통과. nographics의 기존 셰이더 미지원 메시지는 시각검증 근거로 사용하지 않으며 위 D3D11 실제 캡처를 별도로 확인했다.

## 변경 파일
`unity/EungyeTaxi/Assets/Scripts/`의 `PvpNativeReplica.cs`, `PvpNativeLan.cs`, `PvpNativePoseTimeline.cs`, `PvpNativeLanProbe.cs`, `DriveNotifications.cs`, `CockpitUI.cs`; `Assets/Editor/`의 `PvpNativePoseTests.cs`, `PvpNativeLanTests.cs`, 별도 검증 빌더 `PvpNativePerfBuild.cs`; 빌드로 재생성된 `FirstDrive.unity`(같은 Mode Selection, 내부 fileID만 변경). 관련 콘텐츠30/로드맵·개발 체크리스트·본 이력/LATEST와 최종 통합 배포·manifest를 함께 갱신한다. 지도/GIS/ZIP·캠페인 제품명/저장위치·다른 체크아웃은 변경하지 않는다.
