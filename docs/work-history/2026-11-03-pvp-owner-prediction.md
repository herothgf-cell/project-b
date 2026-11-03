# PVP 참가자 자기 차 로컬 예측·서버 보정

## 요구사항과 범위

참가자 자신의 차를 로컬에서 즉시 움직이고 호스트 결과로 보정한다. A2 지도·은계 싱글·다른 채팅 작업을 보존하고 마지막 관련 검사만 수행한다. 통합 실행 폴더·manifest를 갱신하고 main/feature/pvp-prototype 게시 후 C:/git/project-b의 main에도 fast-forward한다. PC 마우스/키보드 입력이나 기존 사용자 게임 강제 종료는 하지 않았다.

## 실제 구현

- `TaxiController.NetworkPredicted`를 참가자 local0에만 켜서 기존 동적 Rigidbody/WheelCollider·로컬 바퀴·페달/조향을 실행한다. 다른 택시/NPC는 기존 서버 보간/kinematic 표시를 유지한다. 기본값 false이므로 싱글·오프라인 경로는 유지한다.
- native-city-lan-v2. 50Hz 고정 tick의 번호 입력만 송신한다. 호스트는 bounded FIFO32/유효값·중복·순서 검증 후 물리 tick당 하나 처리한다. 실제 새 입력을 적용한 tick에만 ack를 전달한다. 위치/운임을 참가자가 제출하지 않는다.
- 256개 입력 이후 body 상태 이력과 ack를 비교해 그 당시 오차를 현재 물리와 미확인 이력에 점진 적용한다. 현재 진행 위치를 과거 서버 위치로 매번 끌어오지 않는다. 중복/오래된 ack 제외·오차가 없는 tick의 Rigidbody pose 쓰기 생략. 최초/8m·100도 초과/시설/경기 종료는 권한 재설정. snapshot/ack0.5초 공백은 안전 제동한다. 완전한 PhysX 롤백/재생은 아니다.
- H 자유 주행도 호스트가 선택한 edge chain/시작·끝 절단점을 복제해 같은 경로로 예측 조향한다. 동일 경로 객체 재사용·복원 보존, 참가자 자유 주행 HUD는 경로 투영만 하여 교통 비용/Dijkstra 재탐색을 피한다. Unity inline null 복원의 빈 객체는 명시적 hasFreeRoute로 구분한다.
- 호스트의 승객·운임·법규·만족도·충돌 손상/내구도·시설·정산 권한과 기존 알림·저장 분리는 유지한다. 새 프로토콜이므로 호스트/참가자를 모두 새 실행본으로 재시작한다.

변경 파일: `PvpNativePrediction.cs`(큐/과거 상태·보정/자유 경로 DTO), `PvpNativeLan.cs`, `PvpNativeReplica.cs`, `TaxiController.cs`, `AssistedDriveController.cs`, `RoadNavigation.cs`, `PassengerJobs.cs`; 관련 probe/runner·전용 Editor helper/.meta·통합 빌드가 다시 생성한 FirstDrive scene ID. 지도 소스/입력/ZIP은 변경하지 않는다. 기획30·로드맵·체크리스트·LATEST·구현계획을 함께 갱신한다.

## 최종 필수 검증: 실제 명령과 결과

로컬 Unity 설치 `C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe`에서 다음 인자로 실행했다.

```powershell
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod PvpNativePredictionTests.Build -logFile artifacts/pvp/owner-prediction-ready-build2.log
pwsh -File tools/test_pvp_native_lan.ps1 -Executable artifacts/pvp/owner-prediction-ready/EungyeTaxi.exe -Rendered -RenderBoth -RenderWidth 1920 -RenderHeight 1080 -Reconnect -MotionSeconds 10 -Port 17989 -TimeoutSeconds 360
```

첫 명령은 실제 실행 시 절대 project/log 경로를 사용한다. Editor 관련 입력/이력/보정17·자유 경로/JSON 존재5·바퀴8·pose11 통과, `PROTOTYPE_BUILD_OK`와 Unity return code0. 전용 helper가 기존 `PrototypeBuild.BuildAt`의 eungye 통합 메뉴/싱글/PVP 빌드를 사용하며 별도 제품/저장 경로를 만들지 않는다.

두 번째는 증거 `artifacts/pvp/native-lan-20261103-153609-26d11068`, host35+client60=95검사/두 프로세스/runner 모두 exit0. 양쪽 Direct3D11·Normal 1920×1080 실행이다. 창을 이동하거나 동시에 완전히 노출시키지는 않았으므로 겹침/가림 가능성이 있다.

- 진단상 입력 송신을0.35초 보류한 동안 자기 차0.0354m 이동·바퀴 조향12.12도(그때 ack9/input30) 확인. 서버 응답 이전 로컬 물리 반응이다. snapshot0.3초 수신 보류 중 주행 유지·복귀 보정, 정상 주행의 반복 hard reset 없음·후진/제동·H 실제 경로 복제를 확인했다.
- 추가 요청10초 중 실제 이동 표시 구간8.8177071초/89.68505m/529프레임·최대17.3916m/s. 측정된 frozen0/최대freeze0ms. 이동 frame평균16.6686/p9517.7555/max24.8858ms. 전체 참가자2263frame평균16.8027/p9517.7375/max78.7204ms, 호스트 전체평균17.99/max142.41ms. 이동 패킷 공백 최대233.20ms. 정지/초기/전환 구간은 이동 통계와 구분한다.
- 기존 서버 모델·공유72NPC/신호·승객 선점/중복·알림/재접속·같은 차 봇 승계·메뉴 정리를 같은 실행에서 확인했다. 알림과 일부 승객 상태는 서버 사건/model fixture이며 실제 승객 전구간 운송·실제 신호위반 주행 검사로 계산하지 않는다. 차 위치를 probe에서 제어하지 않았다.
- A2 `PvpCityMap.cs`/`PvpCityRoadBuilder.cs`는5a5df15의 Git blob과 LF 기준 바이트 동일. 입력 지도/ZIP 변경 없음. `git diff --check` 종료0. 다른PC로 옮긴 작업이 아니므로 기존 입력 해시를 유지하며 Strict/전체 캠페인/4봇 운송/경제 검사는 반복하지 않았다.

## 중간 실패와 구분

초기 owner build의 송신 보류 검사는 시작 준비가 ack grace를 소모해 실패했다. 최초 물리 tick에서 grace 시작·정상 ack 대기 후 통과했다. 이후 motion H 검사는 probe가 loss 검사 뒤 조향0.25를 계속 유지해 실제 호스트 차가 차로 밖으로 이동한 원인이어서 probe를 직진 입력으로 복귀하고 실제 제동 대기 후 통과했다. H 상태만 보지 않고 실제 자유 경로도 요구하도록 보강했다.

H 경로 복제 추가 뒤 native-lan-20261103-152844-be27ca5f는 Unity가 null inline route를 빈 객체로 복원해 접속이 거절되어 실패했다. `hasFreeRoute`와 JSON 존재 회귀 검사로 수정했고, 위 ready-build2/153609 실행이 수정 후 최종 수용이다. 앞선151302의95검사는 H 경로 추가 전이므로 최종 실행본 수용으로 대신하지 않는다.

## 남은 검증·한계

다른PC/사람4명·장시간 충돌/운송·실제 지연/손실·패드 수동 체감은 미검증이다. 호스트 부하/네트워크 지터·큰 권한 보정·경로 전환의 단발 지연이 남을 수 있다. 짧은 loopback의 정지0을 모든 끊김 해결 판정으로 사용하지 않는다. 실제 입력 반응/물리 보정 구조를 변경한 단계이며, 사용자 실행 조건의 지속 체감은 다음 확인 대상이다. 전용 서버/Relay/앱 종료 후 재접속은 추가하지 않았다.

## 배포·다른 작업 보존

검사된 candidate를 통합 `builds/EungyeTaxi/`에 새 복사하고 파일 크기/SHA256 전수 대조·manifest 검증 뒤 게시한다. C:/git/project-b의 미커밋 마인드맵/목차 및 미추적2파일은 사전 크기/SHA256, 미추적5디렉터리는 존재 상태를 기록했다. stash/reset/다른 작업 커밋 없이 fast-forward하며 실제 게시·배포 해시는 후속 영수증에 기록한다. 현재 다른 원격 변경은 fetch 결과7e1a7f7로 baseline과 같았다.

## 실행 폴더 갱신

실제 소스 `f13f8fb136712ff28f91c2fd65d60655e487affc` 확인. 기존139파일115,316,158바이트 manifest 검증0과 canonical 게임 미실행 확인 후 기존 폴더를 artifacts에 보관하고 최종 owner-prediction-ready를 새 복사했다. 후보/배포 **139파일115,329,118바이트** 상대경로·크기·SHA256 전수 일치. DoNotShip은 별도 보관하고 배포하지 않는다. 최종95검사/17+5/wheel8/pose11과 미검증 범위를 실제 validation JSON으로 기록한다.
