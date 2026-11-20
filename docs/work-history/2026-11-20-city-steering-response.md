# 2026-11-20 은계·PvP 수동 중속 조향 개선

## 요청과 구현

45~50km/h에서 수동 조향각이 급격히 작아지는 문제를 개선했다. 기본 차량에서 50km/h의 목표 앞바퀴 중심각을 제안값 6.5°에 맞췄다. `DriveMath.ManualSteerAngle`은 35km/h의 17°와 60km/h의 기존 한계각 사이를 단조 감소하는 3차 베지어 곡선으로 연결한다. 입력 반응·복원 시간, 타이어 마찰, 차체 회전 보조, H 조향 보조는 이전 설정을 쓴다. 입력이 짧은 경우에도 방향 변화가 커지는 것을 확인해 반응 시간은 추가로 조정하지 않았다.

공유 주행 물리가 바뀌어 LAN 호환 계약을 v20으로 올렸다. 방 검색과 호스트 참가 승인은 같은 버전 문자열을 검사한다. 두 PC 모두 새 통합 실행 폴더로 갱신한다.

## 변경 전후 회전 측정

기본 택시와 민감도 100%, H 보조 끔. Unity WheelCollider 평지 검사에서 전진 속도만 고정하고 횡방향 속도와 차체 회전을 물리로 계산했다. 유지 입력은 조향 65%를 4초간 준다. 각 칸은 이전 → 변경 후 값이다.

| 주행 속도 | 최대 앞바퀴 중심각 | 4초간 차체 방향 변화 |
|---|---:|---:|
| 40km/h | 13.49° → 13.51° | 118.44° → 118.58° |
| 45km/h | 9.73° → 9.83° | 94.39° → 95.43° |
| 50km/h | 4.55° → 6.48° | 48.75° → 69.36° |
| 55km/h | 3.41° → 3.93° | 38.87° → 44.99° |
| 60km/h | 2.68° → 2.68° | 32.14° → 32.14° |

추가 검사는 기본 운전 설정의 입력 보간을 적용했다. 키는 한쪽으로 0.1초 누르고 놓았다. 패드 입력은 스틱을 65%로 0.8초 주고 반대쪽으로 같은 시간 되받은 후 1.1초간 놓았다. 각 시험을 좌우 양쪽으로 수행했다.

| 속도 | 키 입력 시작 0.5초 뒤 방향 변화 | 패드 차선 변경 후 횡이동량 |
|---|---:|---:|
| 45km/h | 3.94° → 3.98° | 3.57m → 3.61m |
| 50km/h | 1.71° → 2.47° | 2.10m → 2.98m |
| 55km/h | 1.30° → 1.51° | 1.85m → 2.15m |

50km/h 차선 변경 후 시작 방향과의 오차는 0.38°, 마지막 0.2초의 최대 차체 회전 속도는 0.01°/s 이하였다. 개선 후 좌우 횡이동량의 절댓값은 표시한 자릿수까지 일치했다. 진입부터 입력 해제까지 차체 기울기는 17개 시험 모두 1.14° 이하였다. 이 결과는 입력을 재생한 자동 물리 측정이며, 사람이 조작한 주행 체감은 별도 확인 대상이다.

## 변경 파일

- `unity/EungyeTaxi/Assets/Scripts/DriveMath.cs`, `PvpNativeLan.cs`(같은 폴더)에서 조향 곡선과 LAN 계약 변경.
- `unity/EungyeTaxi/Assets/Editor/CurveSteeringChecks.cs`에 입력·회전·횡이동·복원 검사 추가. 같은 폴더의 `BrakingSettingsChecks.cs`, `ContentTuningChecks.cs`, `DrivingSettingsChecks.cs`, `MenuConditionFollowupTests.cs`에서 LAN 계약 기대값 갱신.
- `tests/DriveMathTests.cs`에 속도 5km/h 증가 때의 조향각 감소 검사 추가. `tests/ManualHandlingTests.cs`에서 기존 제동 토크와 고속 제한에 대한 옛 검사 조건 교정.
- 주행·교통 기획, PvP LAN 기획, 프로토타입 범위·체크리스트, 현재 상태·배포 안내·최신 인계 갱신.

## 검증과 재현

- 변경 전 `CurveSteeringChecks.Run`은 50km/h 조향각 검사에서 종료 1. 변경 후 17개 물리 시나리오와 연속성 검사 통과, 기본 수동/보조/입력 설정 검사와 함께 종료 0. Unity 2022.3.62f2의 batchmode에서 동일한 배치 검사로 비교했다. 로컬 로그는 `artifacts/steering-nov20/baseline.log`, `after.log`다.
- `pwsh -File tools/test_prototype.ps1` 종료 0. 최초 실행은 기존 수동 조작 검사에서 중단됐다. 11월 4일 승인된 일반 제동 계수 0.30과 11월 6일 물리 검사의 바퀴당 1,050Nm를 기준으로 낡은 0.18 계수의 감속 추정값 검사를 바로잡았다. 고속 조향 동일성 검사도 현재 설계의 60km/h 이상을 검사한다. 실제 제동 코드 변경은 없다.
- `pwsh -File tools/check_workspace.ps1 -Strict` 입력 문제 0. 사용자 씬과 설정을 보존하기 위해 이전 작업의 격리된 Unity 프로젝트에 이번 소스를 복사해 `PrototypeBuild.BuildHybrid`를 실행했다. 콘텐츠 대조 및 빌드 종료 0. 은계 r49 지도와 저장 형식은 기존 기준이다.
- 재현하려면 `builds/EungyeTaxi/EungyeTaxi.exe`에서 은계 싱글 또는 PvP를 고르고 H가 수동인지 확인한다. 비교 조건은 기본 택시·표준 조향·평지, 45/50/55km/h다. 짧은 좌우 입력과 완만한 커브를 각각 확인한다. Unity 검사는 `-batchmode -nographics -quit -executeMethod CurveSteeringChecks.Run`을 사용한다.
- 같은 PC에서 두 Windows Player를 `pwsh -File tools/test_pvp_native_lan.ps1 -Discover -MotionOnly -MotionSeconds 30 -Port 17986`으로 구동했다. LAN v20 방 검색·접속·설정 동기화·움직임 검사에서 호스트 32개, 참가자 46개 통과, 종료 0. 참가자 이동 관찰 28.83초·566.84m, 최대 화면 위치 정체 0ms였다. 로그는 `artifacts/pvp/native-lan-20261120-144701-cf87036c/`에 있다.
- `tools/test_driving_settings_player.ps1 -Pvp` 실행본 검사 18개 통과, 종료 0. 기존 프리셋 선택·저장·취소가 적용된다.
- 은계 r49 첫 호출을 일반 교통에서 자동 물리 주행했다. 2,587.99m, 운송 1회, 운임 5,310원, 순수입 3,310원이었다. 승하차·숙소 복귀·첫날 정산 저장 통과, 충돌 0·적신호 위반 0. `pwsh -File tools/test_first_fare_player.ps1` 종료 0, 로그는 `artifacts/first-fare/aa6746bfff254827a7859ed224d911fa/`다.

## 배포와 남은 확인

소스 `4498978`에서 만든 실행본을 `builds/EungyeTaxi/`에 반영했다. 변경된 구성 요소는 `EungyeTaxi_Data/boot.config`, `EungyeTaxi_Data/Managed/Assembly-CSharp.dll`이다. 통합 manifest를 재생성하고 일반·PvP VerifyOnly가 각각 종료 0이었다. 출하 143개 파일 135,302,213바이트의 SHA256과 별도 사용자 설정 1개를 검증했다. 사용자 씬과 로컬 설정은 보존했다. 키보드·실물 패드 체감, 장시간 수동 캠페인과 다른 두 PC 간 LAN 주행은 별도 확인 항목이다.
