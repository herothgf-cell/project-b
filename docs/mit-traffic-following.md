# MIT 앞차 추종 보완

2026-10-21. 기존 은계 일반 차량 AI에 같은 방향 앞차의 속도에 맞춘 추가 감속만 선택적으로 적용한다. NPC 경로·신호·교차로 예약·기존 장애물 탐지는 유지한다.

## 사용

기본 은계 실행 파일에서 Esc → **앞차 추종 보완: ON/OFF** 버튼으로 변경한다. 최초 실행 기본값 ON, 사용자 선택은 PlayerPrefs `Traffic.MitFollowing.v1`에 저장하며 다음 실행에도 유지한다. 현재 차량을 다시 생성하거나 운송/차로를 초기화하지 않는다. 일시정지를 해제한 다음 물리 갱신부터 반영된다.

OFF는 새 속도 제한만 우회하고 기존 차량 AI를 사용한다. 차량·신호 자체를 끄는 설정이 아니다. `-mitTrafficOn`/`-mitTrafficOff` 실행 인자는 저장값을 이번 실행에만 덮어쓴다(둘 다 있으면 OFF). 메뉴에서 변경하면 정상 실행에서는 저장한다.

자동 검사 인자 `*Test`가 있으면 사용자 저장값을 사용·변경하지 않고 기본 OFF다. 새 기능을 포함한 회귀는 반드시 `-mitTrafficOn`을 명시한다.

## 선별 범위와 수정

원본 [VehicleAI.cs](https://github.com/mchrbn/unity-traffic-simulation/blob/4f03f4c276e2b82f033a5f037f43036ba9ab6be3/Assets/TrafficSimulation/Scripts/VehicleAI.cs)의 MoveVehicle 143–148행: 방향 내적 0.8 초과인 느린 앞차에 맞춰 속도 상한을 낮추는 부분을 이식·수정했다. 알고리즘 참고만이 아닌 수정 파생 코드로 기록한다.

- 원본의 설정 최대속도 대신 현재 NPC 속도를 m/s로 사용한다.
- 기존 탐지가 찾아낸 앞차만 사용한다. 거리 8m 안에서 적용하고 4~8m에서는 추가 제한을 연속 해제한다.
- 앞차 속도−0.5m/s, 최소 0.1m/s의 추종 상한을 기존 안전 속도와 비교해 더 작은 값만 사용한다. 정지선·장애물 안전 속도 0을 덮어쓰지 않는다.
- 원본 레이캐스트·무작위 경로·WheelDrive·후진 회피·교차로 관리·모델은 도입하지 않았다. 택시/일반 장애물은 기존 탐지·제동으로 처리한다.
- 현재 도입은 차로 변경이나 끼어들기 추가가 아니다. 48대 실제 교통 흐름에서 개선 체감은 수동 비교 대상으로 남긴다.

## 파일 매핑과 라이선스

| 파일 | 역할 / 출처 |
|---|---|
| `unity/EungyeTaxi/Assets/Scripts/MitTrafficFollowing.cs` | MIT 샘플의 속도 상한 분기를 수정한 순수 계산 |
| `unity/EungyeTaxi/Assets/Scripts/TrafficCar.cs` | 기존 안전 속도 뒤에 선택적 상한 적용 |
| `unity/EungyeTaxi/Assets/Scripts/MitTrafficOptions.cs` | 프로젝트 작성 설정·저장·테스트 분리 |
| `unity/EungyeTaxi/Assets/Scripts/TrafficSystem.cs` | 차량들이 공유하는 설정 초기화 |
| `unity/EungyeTaxi/Assets/Scripts/DriveHUD.cs` | Esc 메뉴의 ON/OFF 버튼 |
| `unity/EungyeTaxi/Assets/Editor/PrototypeBuild.cs` | 빌드 폴더에 원문 라이선스와 수정 고지 복사 |
| `licenses/mchrbn-unity-traffic-simulation/` | 커밋, 원본 코드·README, MIT 원문·수정 고지 |

Copyright (c) 2019 Matthieu Cherubini, MIT. 원본 커밋 `4f03f4c276e2b82f033a5f037f43036ba9ab6be3`. 상업 이용·수정·배포 허용 조건에 따라 저작권과 MIT 전문을 보존한다. 샘플 에셋은 포함하지 않는다. 사용자 배포물에는 `ThirdPartyNotices/mchrbn-unity-traffic-simulation-MIT.txt` 및 `...-NOTICE.txt`를 동봉한다. 다른 게임 구성요소의 라이선스까지 MIT로 바뀌는 것은 아니다.

## 재현 검사

저장소 루트에서 `pwsh -File tools/test_prototype.ps1`. Unity `PrototypeBuild.BuildHybrid` 후 다음 실행 인자를 사용한다.

- `-mitTrafficTest -hybridV2`: 실제 TrafficCar ON/OFF/재활성화, 앞차 완전 정지/출발, 테스트 설정 비저장, 3해상도 메뉴 캡처.
- `-districtTrafficTest -mitTrafficOn -hybridV2`: 48대 180초 NPC 검사, 추가 감속 적용 횟수, 차로/겹침/정체, 적색 3대 대기 후 녹색 통과.
- `-districtTrafficTest -mitTrafficOff -hybridV2`: 같은 기존 AI 회귀. 로그가 덮어써지므로 각 실행 직후 결과 파일을 별도 보관한다.

검증 결과와 빌드 범위는 [작업 이력](work-history/2026-10-21-mit-traffic-following.md)에 기록한다. 자동 NPC 검사를 수동 플레이나 3일 캠페인 검사로 표시하지 않는다.
