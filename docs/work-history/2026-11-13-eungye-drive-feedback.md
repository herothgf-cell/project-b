# 2026-11-13 은계 영업 피드백

## 요청과 구현

- 왕복 2차선의 기존 승하차 지점 중 S04, S08, EG-GAME-02·04·06·07·08에 반대편 게임용 정차 칸과 보도 연결을 추가했다. 기존 24개 ID에 7개를 추가해 31개를 유지한다. 일반 호출 생성은 점유·쿨다운을 확인하고 현재 위치에서 실제 방향별 접근 거리가 짧은 쪽을 선택한다. 일반 운송의 하차는 양쪽 칸 중 하나에 올바른 방향으로 정차 후 F로 확정할 수 있다. 반대편 하차 시 약정 운임·기한을 유지하며 대표 코스와 승객이 합의한 변경 목적지는 지정 정차 칸을 사용한다.
- 3인칭의 좌·우 방향지시 및 후진 보조 카메라를 숨겼다. 1인칭 좌·우·룸미러와 후진 보조 영상, B 뒤보기를 사용할 수 있다.
- 운전 만족도 충돌 감점은 상대 속도 크기를, 내구도는 접촉면에 수직인 접근 속도를 사용한다. 기존 교통차 샘플은 속력에 표시 방향을 곱해 측면 이동 및 회전 접촉점의 속도를 놓칠 수 있었다. 실제 물리 스텝의 이동·회전 목표에서 접촉점 속도를 계산하도록 변경했다. 1.5m/s 미만의 약한 긁힘에는 여전히 M만 차감될 수 있다.
- 적색 신호에서 교차로·횡단보도 밖에 10초 이상 정차하면, 배정된 미발생 요청을 정차 1회당 한 건 앞당겨 표시한다. 기존 배정 수, 대화 중첩 제한, 하차 마감 진행률을 적용한다. 승객 요청 다양화 기획은 별도 채팅 범위다.
- 은계 싱글 일반 보행자 1.2~1.4→1.8~2.0m/s, 자동 보행 단계 기본 녹색 10→8초. 실제 배치된 횡단에는 횡단 시간+2초 이상과 전적색 2초, 점유 보호를 적용한다.
- 적색 우회전 전 정지 생략은 기본 법규 점수 −5 및 M −4다. 탑승 중 M은 하차 S에 반영된다. [콘텐츠 관리창 안내](../content-tuning-tool-guide.md#운전-점수-추가-설정-2026-11-07).

## 변경 파일

- `unity/EungyeTaxi/Assets/Resources/eungye-passenger-stops.json`, `PassengerStopCatalog`, `PassengerJobs`, `FullMapOverlay`, `StopArrivalGuidance`와 관련 Editor 회귀.
- `AssistCamera`, `TrafficCar`, `VehicleDamageController`, `AccidentPhysicalCollisionProbe`, `PassengerRequestSchedule`, `PassengerRequestUI`, `PassengerJobsActivities`, `AmbientPedestrianSystem`, `DriveFlowPolicy`, `JobsSmokeTest` 및 모델 검사.
- [승하차 지도](../content-design/images/eungye-pickup-map-2026-11-13.png), 운전·승객·UI 문서, 설정 안내, 범위·체크리스트.

## 검증과 재현

- `pwsh -NoProfile -File tools/check_workspace.ps1 -Strict` 종료 0, 필수 입력 문제 0. 정식 r18과 지도 ZIP은 그대로이며 재수입 JSON 두 개는 선택 입력이다.
- `pwsh -NoProfile -File tools/test_content_tuning.ps1` 종료 0. 신호 대기의 요청 진행률·주행 시간·중첩·마감 검사를 포함해 총 846 검사와 Node/C# 해시 일치.
- Unity Editor `PassengerStopCatalogTests.Run` 종료 0, 1,635 검사, 신규/기존 정차 칸 방향 경로 검사 1,140건, 후보 거절 0. `AlternativePassengerStopTests.Run` 386 검사 통과. 지도의 배치·차로·보도 검증이며 실제 운행 검사는 아래에 기록한다.
- 작업 복제본에서 Unity 2022.3.62f2 `PrototypeBuild.BuildHybrid` 종료 0. 작업 전 사용자 씬·프로필을 보존했다.
- 새 실행본 `tools/test_accident_physical_collision_player.ps1` 종료 0, 32 검사. 격리된 평지에서 옆으로 들어오는 교통차가 정지 택시에 접촉했다. 접촉 속도 2.938m/s, 내구도 100→98, 차량 사고 기록 확인. 정적 시설·다른 택시와의 충돌, 지속 접촉 중 중복 정산 방지, 저장도 통과했다. 샌드박스 안에서 처음 실행한 구 배포본은 테스트 저장의 파일 교체 오류에서 멈췄으며 최종 검사는 격리된 테스트 캠페인을 샌드박스 밖에서 사용했다.
- 직전 배포본 `-singlePlayer -liveSaveTest` 종료 0, 9 검사. 그 저장을 새 실행본에서 `-singlePlayer -liveReloadTest`로 이어가기 종료 0, 23 검사. 확정 운송·시간·수면/구토·장부 유지.

## 남은 확인

- 사람 직접 주행, 전체 3일 캠페인, 다른 PC LAN, 실제 신고 당시 차선 변경 장면은 미검증.
- `builds/EungyeTaxi/EungyeTaxi.exe`의 은계 싱글에서 M 지도와 [위치 지도](../content-design/images/eungye-pickup-map-2026-11-13.png)를 참고한다. 새 일반 호출을 선택해 승차 방향을 확인한다. 양방향 지점 일반 운송에서는 반대편 표시 칸에 방향을 맞춰 정차하고 F를 누른다. 탑승 중 적색 신호에 10초 정차해 남은 요청이 표시되는지 확인한다. C로 3인칭을 선택하고 방향지시·후진 시 작은 영상이 숨겨지는지 확인한다.
