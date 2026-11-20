# 저속 코너 감속과 운전 점수 표시

운전 점수 100의 끝자리가 기사 수첩에 가려지고, 저속에서 회전하면 페달을 밟아도 차가 급격히 느려진다는 피드백을 반영했다.

## 원인과 변경

- 기존 점수 라벨 폭은 113이고 끝 4단위가 수첩 버튼과 겹쳤다. 은계 싱글의 1·3인칭에서 카드 첫 줄 220단위를 점수에 쓰고, 수첩을 발견하면 카드를 위로 20단위 늘려 게이지 오른쪽에 버튼을 놓는다. 좌표는 1600×900 기준이다.
- 수동 조향의 속도 보정은 타이어 횡가속 목표로부터 선회 속도를 구했다. 그 속도를 넘으면 가속을 끊고 바퀴당 최대 280Nm까지 제동했다. 이제 30km/h까지 운전자가 가속·타력·제동을 선택한다. 30~45km/h에서는 기존 보정의 적용 비율이 0에서 100%까지 연속적으로 증가한다. 노면 마찰과 타이어의 선회 저항은 물리 엔진에서 계산한다.
- 싱글 운전자와 PvP 호스트·참가자 예측이 같은 페달 규칙을 사용한다. LAN 계약은 v21이다. 봇의 코너 제어와 H 조향 보조는 각각 기존 경로를 사용한다.

## 변경 전후 주행

평지에서 기본 택시를 가속해 진입 속도를 맞춘 뒤 가속 페달을 유지하거나 놓았다. 좌우 최대 조향은 0.2초에 걸쳐 입력했다. 이후 차체 속도를 고정하지 않고 WheelCollider로 1초간 주행했다. 단위는 km/h이고 아래는 우회전 값이다. 좌회전 결과는 각 값에서 0.01km/h 이내였다.

| 진입·페달 | 직진 | 변경 전 회전 | 변경 후 회전 |
|---|---:|---:|---:|
| 15.10 · 가속 | 22.21 | 10.99 | 12.20 |
| 15.10 · 타력 | 13.00 | 7.31 | 7.31 |
| 25.22 · 가속 | 32.07 | 12.04 | 25.18 |
| 25.22 · 타력 | 22.70 | 11.09 | 13.69 |

25km/h 가속 회전에서 바퀴당 최대 제동 토크가 280Nm에서 0Nm로 줄었다. 같은 조건에서 차체가 57.4° 회전했다. 15km/h 타력 회전에서는 원래도 추가 자동 제동이 발생하지 않았고 선회 저항에 의한 감속이 남았다.

## 관련 파일과 재현

- 런타임은 `unity/EungyeTaxi/Assets/Scripts/`의 `CockpitUI.cs`, `CockpitMirrors.cs`, `DriveMath.cs`, `TaxiController.cs`, `PvpNativeLan.cs`다. 첫 화면 캡처에서 1인칭 왼쪽 거울 테두리가 카드 상단을 덮는 것도 발견해 거울을 24단위 올렸다. HUD 글자 측정과 캡처는 `ShiftDevEndProbe.cs`에 있고 `tools/test_father_story_player.ps1`은 창 모드로 실행한다.
- 저속 물리 비교는 Unity 2022.3.62f2에서 `-batchmode -nographics -quit -executeMethod CurveSteeringChecks.RunCornerPedals`로 재현한다. 기존 중속·조향 설정 회귀는 같은 방식으로 `CurveSteeringChecks.Run`을 실행한다. LAN 기대 버전은 BrakingSettingsChecks, ContentTuningChecks, DrivingSettingsChecks, MenuConditionFollowupTests에서 갱신했다. 페달 경계값은 `tests/CornerAssistTests.cs`, 전체 모델 검사는 `pwsh -File tools/test_prototype.ps1`에 있다.
- 게임 확인은 `builds/EungyeTaxi/EungyeTaxi.exe`에서 은계 싱글을 시작하고 첫 운송을 마친 뒤 C로 시점을 바꾼다. 운전 점수 100과 그 아래 수첩 버튼을 확인한다. 평지에서는 H 보조를 끄고 15·25km/h에 W를 유지한 경우와 놓은 경우의 코너를 비교한다.

## 검증

- 물리 회귀 검사는 기존 코드에서 종료 1, 수정 후 직진·좌우·가속·타력 12조건에서 종료 0이었다. 기존 중속 조향·설정 회귀 검사도 종료 0이었다. 분리해 보관한 로그는 `artifacts/score-corner-nov20/baseline.log`, `physics-final.log`, `steering.log`다.
- `tools/test_prototype.ps1` 종료 0, 코너 페달 검사 67건 통과. 첫 모델 실행에서 30km/h를 m/s에서 되돌릴 때 경계값 오차를 발견해 m/s 단위에서 하한을 뺐다. `tools/check_workspace.ps1 -Strict` 필수 입력 문제 0. 은계 지도 r49.
- 기본 싱글 경력 메뉴와 기존 차고·도로 수정을 포함한 격리 프로젝트에서 `PrototypeBuild.BuildHybrid` 종료 0. 기본 게임 폴더에 설치 후 격리 빌드의 배포 대상 143개 파일과 크기·SHA256을 대조했다. 검증용 사용자 경력은 별도 임시 경로를 사용했다. 수동 키보드·실물 패드 체감, 실제 두 PC 간 통신, 전체 캠페인 완주는 이번 검사 범위 밖이다.
