# 2026-11-10 싱글 영업 시간 선택 · 개발용 바로 퇴근

## 요구사항 (사용자)

- 싱글 모드에서도 영업 시간을 설정할 수 있게 한다.
- 설정에 바로 퇴근할 수 있는 버튼을 추가한다. 개발·테스트용이며 일반 설정과 분리한다.
- (후속) 영업 시간도 설정에서 테스트 용도로 바꿀 수 있게 한다. 그 뒤 커밋·푸시.

## 구현

### 싱글 영업 시간 (10 / 20 / 30 / 60분)

- 선택지는 실제 시간 10·20·30·60분이고 기본은 지금처럼 20분이다. 게임 안 시계는 그대로 08:00~20:00이며, 실제 하루가 짧아지거나 길어진다. 손님 수요 단계(`SimulationDemandClock`)와 HUD 시계·남은 영업은 이미 `daySeconds` 비율을 쓰므로 함께 따라간다.
- 선택 위치: 1일차는 `영업 준비` 메뉴(첫 버튼 아래), 2·3일차는 다음 날 준비 팝업(영업 시작 위). 팝업 높이 590→640.
- 영업을 시작하기 전(`preparing`, 경과 0초)에만 바꿀 수 있다. 시작 뒤에는 버튼이 잠긴다. 고른 값은 캠페인 계정 설정에 저장되어 다음 날과 이어하기에도 유지된다.
- 이전에는 저장을 불러올 때 `WithCurrentOperationBalance`가 하루 길이를 1200초로 되돌렸다. 이제 선택지 값(600/1200/1800/3600)이면 유지하고, 그 밖의 옛 값(예: 720)은 이전처럼 1200으로 맞춘다.
- PVP(`PvpProfile`)와 연습·저장 복원 실패 상태에서는 바꿀 수 없다. PVP 경기 시간 입력은 기존 그대로다.
- 하루 길이가 바뀌면 피로가 쌓이는 실제 시간도 그만큼 바뀐다(피로 100 경계는 `daySeconds`). 난도·수익 조정은 하지 않았다.

### 개발용 바로 퇴근

- 설정 패널에 세 번째 탭 `개발용 테스트`를 추가했다(운전/날씨/개발용). 안에 `지금 바로 퇴근 (개발용)` 버튼과 `영업 시간 (테스트)` 줄이 있다.
- 동작(`BusinessProgress.DevEndDayNow`): 거점 정차·정지·휴식 조건을 건너뛰고 `RequestClose("개발용 바로 퇴근")`로 당일을 닫은 뒤 일시정지를 풀어 기존 정산 흐름(LateUpdate → Settle)으로 넘어간다. 휴식 비용·수면 횟수는 쓰지 않는다.
- 막는 경우: PVP, 영업 준비 중/영업 중 아님, 사고 처리 중, 차량 고장(정비소 밖), 승객이 타 있을 때(`OnTrip`). 손님에게 가는 중이면 호출을 지운다(실패 집계 없음). 거부 사유는 패널 하단에 표시한다.
- 정산 기록의 종료 사유가 `개발용 바로 퇴근`으로 남는다.

### 개발용 영업 시간 변경 (후속)

- `개발용 테스트` 탭의 `영업 시간 (테스트)` 줄: 같은 10/20/30/60분을 **영업 중에도** 즉시 바꾼다(`BusinessProgress.DevSetShiftLength` → `EconomyLedger.DevSetDaySeconds`). 누르면 바로 적용되고 저장되며 다음 날에도 유지된다. 현재 값과 남은 영업(분)을 표시한다.
- 이미 지난 시간보다 짧게 하면 실제 하루 끝에 닿은 것과 같게 피로 100을 적용한다(초과 영업). 그 외에는 피로를 건드리지 않는다.
- 싱글 영업 중(Open)·영업 준비 중에만, PVP·연습·저장 복원 실패 상태에서는 잠긴다.

## 변경 파일

- `unity/EungyeTaxi/Assets/Scripts/PrototypeState.cs` — `ShiftMinuteChoices`, `IsShiftChoice`, 복원 시 선택값 유지
- `unity/EungyeTaxi/Assets/Scripts/CampaignAccount.cs` — `SetDaySeconds`
- `unity/EungyeTaxi/Assets/Scripts/EconomyLedger.cs` — `SetDaySeconds`(시작 전·싱글·선택지만), `DevSetDaySeconds`(영업 중 허용)
- `unity/EungyeTaxi/Assets/Scripts/BusinessProgress.cs` — `CanChangeShiftLength`, `ShiftMinutes`, `SetShiftLength`, `CanDevChangeShiftLength`, `DevSetShiftLength`, `DevEndDayNow`
- `unity/EungyeTaxi/Assets/Scripts/OneDayHUD.cs` — `DrawShiftLength`, 준비 팝업 배치
- `unity/EungyeTaxi/Assets/Scripts/DayPreparationPresentation.cs` — 팝업 높이 640
- `unity/EungyeTaxi/Assets/Scripts/DriveHUD.cs` — 1일차 영업 준비 메뉴에 선택 줄
- `unity/EungyeTaxi/Assets/Scripts/DrivingSettingsPanel.cs` — `개발용 테스트` 탭
- `unity/EungyeTaxi/Assets/Editor/ShiftLengthChecks.cs` — 신규 검사
- `unity/EungyeTaxi/Assets/Scripts/ShiftDevEndProbe.cs` — 신규 실행본 위치 제어 검사(`-shiftDevEndCheck`, 임시 캠페인 저장만 사용), `BusinessProgress.cs`에 저장 경로 격리 2줄
- 출하 실행본 `builds/EungyeTaxi/`·배포 manifest는 이 작업에서 커밋하지 않았다(아래 '출하 빌드 인계').

## 검증

- Unity 2022.3.62f2 batchmode `-executeMethod ShiftLengthChecks.RunAll` → exit 0, 컴파일 오류 없음.
  - `SHIFT_LENGTH_OK checks=11`: 선택지, 비선택 값 거부, 30분 적용(원장·계정), 남은 시간, JSON 저장/복원 유지, 시작 후 잠금, 옛 720초 → 1200 정규화, PVP 거부, 10분 하루 600초 경계에서 피로 100.
  - `DAY_PREPARATION_OK checks=150`(팝업 640 높이가 1280×720·1920×1080 안에 들어감 포함), `DRIVING_SETTINGS_OK checks=171`.
- 로그: `artifacts/shift-length-checks.log`(Git 제외).

### 실행본 재빌드와 화면 확인 (후속, 사용자 요청 "Rebuild and check")

- 첫 BuildHybrid는 다른 세션의 Unity batch 작업(`LaneFollowupBuild.Diagnostic`)이 프로젝트를 잠그고 있어 시작 직후 실패(0x40000015, 출력 미변경). 사용자 선택 "Wait, then build"로 그 작업 종료(17:07) 뒤 재실행 → `Build Finished, Result: Success`, `PROTOTYPE_BUILD_OK builds/EungyeTaxi/EungyeTaxi.exe`, exit 0.
- 빌드 시점 작업 트리에 다른 세션의 미커밋 변경 `BrightUIProbe.cs`(1줄)·`BrightUIPoseReview.cs`(신규)가 있어 실행본에 함께 컴파일됨. `-brightUiCheck -poseReview` 인자에서만 동작하는 렌더 검사용이라 일반 플레이에는 영향 없음.
- 실행본 위치 제어 검사: `EungyeTaxi.exe -shiftDevEndCheck -shiftCaptureOutput artifacts/shift-dev-end-captures -screen-width 1280 -screen-height 720 -screen-fullscreen 0` → exit 0, `SHIFT_PLAYER_OK checks=12`. 확인 항목: 1일차 준비 메뉴 기본 20분, 준비 중 바로 퇴근 거부, 15분 거부, 10분 적용, 영업 시작 후 잠금·남은 영업 10:00, 개발용 탭 표시, 바로 퇴근 → 1일차 정산(종료 사유 `개발용 바로 퇴근`), 2일차 준비에서 10분 유지·30분 변경, 저장 파일 복원 30분.
- 캡처 7장(`artifacts/shift-dev-end-captures/`, Git 제외)을 직접 보고 확인: 1일차 메뉴의 영업 시간 줄, 설정 3탭·개발용 탭 버튼·설명, 정산 화면, 2일차 팝업(640 높이) 안의 영업 시간 줄과 영업 시작 버튼 겹침 없음.
- 버튼 클릭은 IMGUI 화면 좌표 대신 같은 메서드(`SetShiftLength`, `CanDevChangeShiftLength`, `DevSetShiftLength`, `DevEndDayNow`)를 직접 호출했다. 사람이 마우스로 누른 확인은 아니다.

### 개발용 영업 시간 변경 후속 검증

- `ShiftLengthChecks.RunAll` exit 0: `SHIFT_LENGTH_OK checks=19`(추가: 영업 중 60분 변경·남은 3300초, 저장 유지, 비선택 거부, 경과보다 길게 줄이면 피로 유지, 경과 700초에서 10분으로 줄이면 피로 100·남은 0, PVP 거부), `DAY_PREPARATION_OK 150`, `DRIVING_SETTINGS_OK 171`.
- main 작업 트리에서 BuildHybrid 후 실행본 검사 1회 실패: 새 단계(영업 중 20분 변경) 때문에 2일차가 20분인 것이 맞는데 검사 기대값이 10분으로 남아 있던 검사 오류. 기대값 수정 뒤 재빌드 → `SHIFT_PLAYER_OK checks=13`.
- 그 사이 다른 세션이 미커밋 지도 노면 변경(`ReviewedMarkSuppression.cs`, `HdRouteTrial.cs`)을 main 작업 트리에 추가해, 17:17 main 빌드에 섞인 것을 확인하고 그 빌드는 출하용으로 쓰지 않았다.
- 소스 커밋 3933628(제 변경만 부분 staging) 기준 깨끗한 worktree(`C:\git\project-b-shiftbuild`, Library 복사)에서 BuildHybrid → `PROTOTYPE_BUILD_OK`, exit 0. 그 실행본 `-shiftDevEndCheck` → exit 0, `SHIFT_PLAYER_OK checks=13`, 캡처 8장(`04b-settings-dev-tab-20min` 포함) 확인: 개발용 탭에 바로 퇴근 카드·영업 시간 카드, 20분 선택 시 HUD `남은 영업 20:00`·현재 20분 표시.

### 출하 빌드 인계

- 다른 세션(은계지구 교차로 차선 정리)이 J7973 노면 소스 9302097을 main에 커밋하고, 3933628을 포함한 상태로 BuildHybrid·`builds/EungyeTaxi`·배포 manifest 갱신·커밋·push를 맡겠다고 알려 와 중복 빌드·커밋을 하지 않았다. worktree 빌드를 main `builds/`로 복사하던 robocopy는 도중에 중단했고(일부 파일만 덮였을 수 있음), 그쪽에 새 BuildHybrid 결과로 manifest를 만들도록 전달했다.
- 따라서 출하 exe·manifest의 실제 해시와 검증 결과는 그 세션의 커밋/작업 이력을 따른다.

## 미검증 / 남은 항목

- 출하 빌드·manifest는 다른 세션 담당(위 인계). 그 커밋 뒤 출하 exe로 `-shiftDevEndCheck` 재확인 권장.
- 사람 수동 플레이(마우스 클릭, 10·60분 하루 체감), 자동 물리 주행, 전체 캠페인 검사는 하지 않았다.
- 개발용 버튼은 정식 빌드에서도 보인다. 출시 전 숨길지(빌드 플래그 등)는 미결정.
- 10·60분 하루의 난도·수익·피로 균형은 조정하지 않았다.
- 커밋: 소스 3933628, 이 문서·LATEST 문단은 별도 문서 커밋. 다른 세션/이전의 미커밋 변경(`FirstDrive.unity`, `LATEST.md`의 교차로 위치 안내, `PackageManagerSettings.asset`, 교차로 위치 기록, `BrightUIProbe.cs`·`BrightUIPoseReview.cs`, `ReviewedMarkSuppression.cs` 등)은 커밋에 넣지 않았다.

## 재현

```
Unity.exe -batchmode -nographics -projectPath <저장소>/unity/EungyeTaxi -executeMethod ShiftLengthChecks.RunAll -logFile <저장소>/artifacts/shift-length-checks.log
```

게임: 싱글 → 영업 준비 메뉴의 `영업 시간` 줄에서 분 선택 → 영업 시작. 영업 중 Esc → 설정 → `개발용 테스트` → `지금 바로 퇴근 (개발용)` 또는 `영업 시간 (테스트)`에서 분 선택.

실행본 검사: `EungyeTaxi.exe -shiftDevEndCheck -shiftCaptureOutput <폴더> -screen-width 1280 -screen-height 720 -screen-fullscreen 0`
