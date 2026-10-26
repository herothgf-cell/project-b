# 2026-10-26 수동 조향 중 불필요한 자동 감속 수정

## 요구·원인·구현

작은 좌우 입력에서 택시가 감속하는 현상을 줄인다. `TaxiController`는 속도에 따라 줄어드는 `ManualSteerAngle`을 바퀴에 적용했지만, `DriveMath.CornerAssist`는 같은 정규화 입력을 고정 주차각 42도로 해석했다. 25% 조향에서 실제보다 큰 회전으로 판단해 가속을 끊고 브레이크를 추가했다.

수동 조향도 실제 `ManualSteerAngle`로 환산한 뒤 기존 코너 보정을 적용한다. H의 `SteerAngle` 경로, 타이어·횡가속 상한, 모터/입력 상승률, 서비스 브레이크·후진·저속 주차 회전 수치는 유지한다. 입력 상승/복귀는 추가 측정 없이 변경하지 않았다.

## 변경 파일

- `unity/EungyeTaxi/Assets/Scripts/DriveMath.cs`
- `tests/CornerAssistTests.cs`
- `unity/EungyeTaxi/Assets/Scripts/ManualSteeringCorrectionProbe.cs` 및 `.meta`

## 실제 검증

`csc.exe`로 각 모델과 DriveMath를 컴파일해 실행했다. 수정 전 새 회귀 검사 종료 -532462766(단언 예외): 좌우 입력 ±.25에서 30km/h 모터 요구 `.5 → .02086592`, 50/70km/h 모터 `0`·자동 브레이크 `.08`. 수정 후 세 속도 좌우 모두 모터 `.5`·브레이크 `0`.

최종 모델 명령은 다음과 같다.

```powershell
foreach($taskName in @('CornerAssistTests','DriveMathTests','ManualHandlingTests')) {
  & "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo "/out:artifacts\$taskName.exe" "tests\$taskName.cs" unity\EungyeTaxi\Assets\Scripts\DriveMath.cs
  if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
  & ".\artifacts\$taskName.exe"
  if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
}
```

종료0: 코너 보정14·주행 정책9·수동 조향25 검사. 기존 저속 회전·H 각도·S/후진·비상 브레이크·고속 횡가속 계산 검사 포함. 사용자 요청에 따라 전체 모델·캠페인·안정성 반복 검사는 수행하지 않았다. 기존 전체 안정성 자동 주행의 운임 관련 실패가 통과한 것으로 간주하지 않는다.

## 실행·남은 항목

기본 빌드 `PrototypeBuild.BuildHybrid`, 기본 실행 `builds/EungyeTaxi/EungyeTaxi.exe`. 통합 담당자가 최종 빌드 후 `-batchmode -nographics -manualSteeringCorrectionsTest -logFile <절대 로그 경로>`로 실행하고 종료코드 및 `artifacts/manual-steering-corrections.txt`를 확인한다. 이 기록 작성 시 실제 Rigidbody 검사는 준비만 되었으며 **미실행**이다. 후속 통합 기록의 실제 결과를 따른다.

전용 검사는 기존 TaxiController/WheelCollider를 외딴 평면에서 구동한다. 초기 위치/속도만 설정하고 이후 물리는 강제하지 않는다. 30/50/70km/h 좌우 .25 입력의 속도 유지·횡이동·모터/브레이크·기울기·타이어 미끄러짐·조향 복귀를 확인한다. `Test` 접미사로 기존 시험 저장 격리를 적용하며 실제 캠페인을 불러오지 않는다. 사람의 키보드/패드 체감·전체 캠페인 플레이는 미검증이다. 지도 입력·배포 빌드·manifest는 이 하위 작업에서 변경하지 않았다.
