# 2026-11-07 교통법규·운전 점수 구조 및 웹 GPT 적용 검토

## 요구사항·결과

현재 교통법규와 운전 점수 영향 요소를 구조 도식으로 정리하고, 사용자가 첨부한 웹 GPT 배점안의 적용 범위를 조사했다.

- [검토 보고서·도식·현재/제안 비교표](../future-design/2026-11-07-driving-score-application-review.md).
- [첨부 원문 보존](../future-design/2026-11-07-driving-score-web-gpt-source.txt). PC별 입력 원본은 Codex attachments의 붙여넣은 텍스트이며 문서 안에서는 저장소 상대 경로를 사용한다.
- 기존 법규/M/S 분리·사건 중복 방지·필요한 제동 면제는 구현. 난이도별 +1~4·단계 과속·B·운송/일일 획득 상한·중대 위반 시 운송 전체 획득 취소는 미구현이다.
- 전방 적색 우회전 -5/-4 대 제안 -20/-6, 보행 보호 -25/양보M-8 대 기본 -15/-10+위험 가중, 공통 준수+3 대 +1~4가 주요 차이다.
- 11/02 법규 연결/기술 문서의 당시 범위를 후속 게임 서행·사고 등급 구현과 구분했다. 범용 법정 L08은 보류, 제한된 장소의 게임 서행은 별도 finding으로 구현돼 있다.

## 변경 파일

1. `docs/future-design/2026-11-07-driving-score-application-review.md`: 현재 도식/실제 수치/전체 카테고리/정책 차이/근거.
2. `docs/future-design/2026-11-07-driving-score-web-gpt-source.txt`: 사용자 첨부 원문.
3. `docs/content-design/01-driving-and-traffic.md`: 검토 진입 링크, 미채택 상태 명시.
4. `docs/prototype-development-checklist.md`: 조사 완료와 정책 미적용 구분.
5. 이 이력과 `docs/work-history/LATEST.md`: 결과·검증·현재 실행/입력 상태·다음 작업.

이번 작업은 게임 소스·설정·빌드·지도/ZIP·manifest·개인 저장을 수정하지 않았다. 시작 전 LATEST의 Higgsfield 프리셋 안내와 기존 미추적 자료를 보존했다. 작업 중 별도 비 프로토타입 변경이 설정/소스/테스트에 추가됐으며 이번 커밋에 포함하지 않는다.

## 실제 검증

| 명령/조사 | 실제 결과 | 범위 |
|---|---|---|
| `git status --short`·기획/코드/첨부 열람 | 기존 및 동시 작업 변경 식별 | 정적 대조 |
| 기본 프로필과 Resources의 `law.*`, `manner.*`, `common.comfort*` 비교 | 30개, 차이0 | 새 세션 기본값. 개인 저장/실행 프로세스 값 아님 |
| `pwsh -NoProfile -File tools/test_prototype.ps1` | exit1 | 법규 등 앞선 검사 통과 후 `ManualHandlingTests`: 일반 제동 기대3.5~6, 실제8.823529. 전 suite 통과 아님 |
| 아래 관련 5종 절대경로 CSC 컴파일·실행 | exit0, 5종 모두 PASS | 법규 사건/준수/중복, 혼동상황, 요청, M, 신호·입력 정책 |
| `pwsh -NoProfile -File tools/test_slow_turn.ps1` | exit0, `PASS SlowRightTurnTests 21` | 제한된 게임 서행 순수 모델 |
| 관련 문서 `git diff --check`·상대 링크 검사·원문 SHA256 비교 | exit0, 깨진 상대 링크0·원문 일치 | 문서/입력 보존 검증 |

전체 검사 실패는 `DriveMath.Resolve`의 일반 제동 .30과 기존 테스트 환산 기대 구간의 불일치다. 이는 이번 문서 변경 전에 존재하던 코드이며 수정하지 않았다. 실제 PhysX 제동 체감 실패를 뜻한다고 단정하지 않는다. 단독 검사 첫 호출은 CSC 상대 경로 해석 CS1504/exit1이었고, 절대 경로로 교정한 아래 명령에서 통과했다. 탐색 중 PowerShell brace 경로 및 rg 경로 glob 오류는 실제 파일 경로/검색으로 교정했다.

관련 5종 재현(저장소 루트 PowerShell):

```powershell
$taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$taskNames = @('VehicleCare','PassengerBehavior','PostTripServicePolicy','OneDayOfferPolicy','PvpConditionRules','DriverCondition','MitTrafficFollowing','RoadGeometry','TrafficRules','PrototypeState','CampaignAccount','EconomyLedger','OperationMetrics','PassengerRequest','RightPracticeState','PracticeFeedback','ConfusionPracticeState','DriveMath','MannerLedger','FareTrip','AssistedDrivingPolicy','KoreanSignalRules','ContentJson','ContentTuning','ChoiceEffects')
$taskSources = $taskNames | ForEach-Object { Join-Path (Get-Location) "unity/EungyeTaxi/Assets/Scripts/$_.cs" }
New-Item -ItemType Directory -Force artifacts | Out-Null
foreach ($taskTest in @('PrototypeStateTests','ConfusionPracticeTests','PassengerRequestTests','MannerTests','TrafficRulesTests')) {
    $taskExe = Join-Path (Get-Location) "artifacts/$taskTest.exe"
    $taskTestSource = Join-Path (Get-Location) "tests/$taskTest.cs"
    & $taskCompiler /nologo "/out:$taskExe" $taskTestSource @taskSources
    if ($LASTEXITCODE -ne 0) { throw "$taskTest compile failed" }
    & $taskExe
    if ($LASTEXITCODE -ne 0) { throw "$taskTest failed" }
}
```

## 미검증·다음 작업·실행 상태

- 새 Unity 빌드·위치 제어 검사·자동 물리 주행·사람 수동 플레이·전체3일 캠페인·다른PC/LAN 검사는 하지 않았다. 법령 원문 새 감수도 없다.
- 다음은 첨부안 채택 여부·적용 범위를 결정하는 것. 특히 사고 -25/-40의 중대 위반 취소 포함 여부, 빈차/취소/마감의 획득 확정, 구 저장·사용자 감점0 정책을 먼저 정해야 한다. 이번에는 구현 요청으로 확대하지 않는다.
- 전체 prototype의 일반 제동 테스트 불일치는 별도 조작감 검토에서 추적할 잔여다.
- 기본 실행 `builds/EungyeTaxi/EungyeTaxi.exe`, 은계 싱글/자체 A2 PVP·LANv13, 빌드 `PrototypeBuild.BuildHybrid`. 이번 재빌드/manifest 갱신 없음.
- 필수 입력은 직전 LATEST 이력의 Strict issues0 인용. PC 이동이나 지도 변경 작업이 아니므로 이번 Strict 재검사 없음. 배포/지도 ZIP·사용자 저장 보존.
- 기록의 실제 커밋은 저장 후 `git log -1 --oneline -- docs/work-history/2026-11-07-driving-score-application-review.md`로 확인한다.
