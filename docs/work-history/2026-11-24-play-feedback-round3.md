# 2026-11-24 직접 플레이 피드백 3차 묶음: 제한속도 구간 표시, 키보드 단축키·키캡, 중요 알림 카드, 짐 승객 사진, 급제동음 제거, 캐주얼 조향

## 요구 (사용자 직접 플레이 피드백, 2026-11-24)

1. 짐을 싣는 승객의 사진과 탑승 뒤 승객 프로필 사진이 다르다.
2. 제한속도 시작·종료 구간을 운전 중에도, 지도에서도 볼 수 있게 표시한다.
3. 캐주얼과 시뮬 운전 감각의 차이를 잘 못 느끼겠다.
4. 브레이크(급제동) 소리를 없앤다.
5. 키보드 위주로 편하게: "계속 영업"은 Enter, 닫기는 Esc로 통일, 그 밖의 단축키.
6. 미션이 주어졌는지 잘 모르겠다.
7. 토했는지 잘 모르겠다. 차량 상태의 중요 알림은 사고 팝업처럼 알려야 한다.
8. 조작 단축키 표시가 텍스트뿐이라 지저분하다. 버튼마다 조작키를 깔끔한 키 모양으로 붙인다(예전 시안에 있던 것 같다).
9. (작업 중 추가) 약속이 잡혀 있으면 운행 화면 등 적절한 곳에 표시한다.

## 원인과 결정

- **짐 승객 사진**: 사진은 호출 번호(없으면 운송 번호)로 고른다. 호출 없이 길가에서 태운 승객은 호출 번호가 비어 있고, 짐을 싣는 동안에는 운송 번호도 아직 정해지지 않았다(탑승이 끝날 때 새로 발급). 그래서 짐 싣기 화면은 임시 값("passenger" 또는 직전 운송 번호)으로, 탑승 뒤 카드는 새 번호로 사진을 골라 서로 달랐다. 길가 승객은 F를 누른 순간 운송 번호를 정하고 탑승 때 그대로 쓴다. 호출 승객은 이전과 같다.
- **캐주얼·시뮬 차이**: 두 모드는 40km/h 이상에서 핸들 한계만 달라서 시내 속도에서는 거의 같았다. 캐주얼에 모든 속도에서 느껴지는 차이를 더했다. 핸들 반응 1.7배, 출발 가속이 붙는 속도 1.8배, 코너 옆 미끄럼 저항 1.35→1.6. 시뮬은 그대로다.
- **예전 키캡 시안**: `docs/ui/` 캡처와 문서에서 키캡 시안은 찾지 못했다. 실제 화면에는 "호출 보기" 버튼의 M 배지(`CasualUI.Key`)만 있었다. 이번에 공용 키캡(`KeyHints.Cap`)을 새로 만들었다.

## 구현

| 내용 | 동작 |
|---|---|
| 제한속도 구간 계산 | `SpeedLimitSections`: 도로 그래프의 모든 차로를 6m 간격으로 표본해 같은 규칙(`RoadSpeedLimits.Limit`)으로 제한을 구한다. 제한 구간 선분과 진행 방향 기준 바뀌는 지점(시작·종료·값 변경)을 만든다. 같은 방향 차로의 지점은 하나로 묶고 가장 바깥 차로 쪽에 표지를 둔다 |
| 도로 위 표지 | 바뀌는 지점마다 길 오른쪽 3.1m에 원형 표지를 세운다. 시작은 빨간 테두리에 숫자, 아래 보조판 "여기부터"(보호구역은 "어린이보호구역"). 끝은 회색 테두리, 숫자, 검은 사선에 "여기까지"/"보호구역 끝" |
| 보호구역 바닥 | 어린이보호구역 30 구간 차로에 빨간 포장(폭 3m, 지면에서 2cm 위)을 깐다 |
| 지도 | 전체 지도와 미니맵에 제한 구간을 선으로 그린다(수인로·대은로 주황, 보호구역 빨강). 구간이 시작되는 곳에 숫자 원판을 둔다(미니맵은 650m 이하 확대에서만) |
| 운전 중 알림 | 현재 제한이 0.5초 이상 바뀌어 있으면 속도계 위에 3.5초 띠를 띄운다. "제한속도 60 구간 시작", "어린이보호구역 시작 · 제한 30", "제한속도 30 → 60", "제한속도 구간 끝" |
| 급제동음 | 재생 호출을 뺐다. 녹음 파일과 소리 원본은 그대로 둔다 |
| 캐주얼 조향 | `TaxiController.CasualSteerResponse`·`CasualPickup`·`CasualGrip`. 설정 화면 설명도 바꿨다 |
| 키캡 | `KeyHints.Button`: 버튼 오른쪽 끝에 작은 키 모양(모서리 4px, 아래 테두리 두께)을 붙인다. 기록 F8, 도움말 F1, 지도 M, 전체 지도 닫기 M, 창문 K, 미터기 J, 온도 [ ], 수동 운전 H, 도움말·기록·시설 팝업 닫기 Esc, 계속 영업·영업 시작·계속하기·다음 날 준비·사고 처리 계속 Enter |
| Enter | 계속 영업(운송 결과), 영업 시작(준비 화면), 계속하기(목표 비교), N일차(영업) 준비, 사고 팝업의 주 버튼. 회차 마무리·새 하루·처음부터는 Enter에 묶지 않았다 |
| Esc | 원래 닫기 버튼만 있던 창을 닫는다: 운송 결과(계속 영업과 같음), 운행 기록, 도움말, 승객 행동 창(접어두기), 중요 알림 카드. 같은 Esc로 일시정지가 열리지 않는다. 지도·시설 팝업·설정·수첩은 원래 Esc로 닫혔다 |
| 중요 알림 카드 | `CriticalAlerts`: 화면 위쪽 가운데에 사고 팝업처럼 카드를 띄운다(운전은 멈추지 않음, 6초, X나 Esc로 닫기, 운행 알림에도 남김). 미션 시작(목표·성공 추가금), 손님 구토, 연료 25·10 이하, 내구도 50·20 이하, 청결도 30 미만(새 손님 불가), 포만감 25·10 이하, 피로 75·90 이상. 기준을 넘는 순간 한 번, 5점 여유를 두고 회복해야 다시 알린다. 메뉴·지도·사고 처리 중에는 줄을 세워 뒤에 보여 준다 |
| 약속 칩 | `AppointmentChip`: 오른쪽 열(지도 아래, 운행 중이면 목적지 카드 아래)에 열린 약속을 칩으로 보인다. 학생 재회 약속은 그 전날까지 "학생과 약속 · N일차 HH:MM · 장소", 당일에는 "HH:MM · 장소 · n분 뒤"(1시간 안이면 주황), 약속 시간(±30분)이 되면 초록 "지금 · 장소에서 기다려요"와 알림 카드 한 번. 하차 지점에서 미리 잡은 다음 호출은 "예약 호출 · 승차지 → 하차지" |

## 변경 파일

- 새 파일: `unity/EungyeTaxi/Assets/Scripts/SpeedLimitSections.cs`, `KeyHints.cs`, `CriticalAlerts.cs`, `AppointmentChip.cs`(+ `.meta`)
- `PassengerJobs.cs`(길가 승객 운송 번호), `VehicleAudio.cs`(급제동음), `TaxiController.cs`·`DrivingSettingsPanel.cs`(캐주얼)
- `TrafficSystem.cs`(`LimitGraph`, `TableLimit`, `TableRoadName`), `RoadSpeedLimits.cs`(`SpeedLimitBadge.DrawDisc`), `RoadSignage.cs`(표지 구성 요소 추가), `FullMapOverlay.cs`·`DrivingReadability.cs`(지도 선·키캡)
- `CockpitUI.cs`, `DrivingHudArt.cs`, `DriveHUD.cs`, `DrivingLaw.cs`, `ServiceStops.cs`, `RideCompletionPanel.cs`, `OneDayHUD.cs`, `AccidentPresentation.cs`(키캡·Enter·Esc)
- 검사: `SpeedLimitProbe.cs`(구간·표지·알림·경고 카드 7건 추가)
- 실행본: **이번 커밋에는 넣지 않았다.** 같은 작업 폴더에서 다른 세션이 미커밋 변경으로 실행본 폴더를 빌드·검사하고 있어, 지금 빌드하면 그 변경이 섞인다. 다음 배포 빌드(`PrototypeBuild.BuildHybrid`)와 manifest 갱신 때 함께 들어간다.
- 문서: 이 파일, `docs/ui/2026-11-24-play-feedback-3/`, `docs/prototype-development-checklist.md`, `docs/content-design/08-fun-improvements.md`, `docs/CURRENT-STATE.md`, `docs/work-history/LATEST.md`

## 검증

| 검사 | 종류 | 결과 |
|---|---|---|
| `SpeedLimitChecks.Run` | 편집기 규칙 | 40건 통과(컴파일 확인 겸) |
| `CurveSteeringChecks.Run` | 편집기 물리 | 통과(`CURVE_STEERING_OK`, 캐주얼 주행 27, 운전 설정 181·메뉴 193). 캐주얼 옆 미끄럼 저항을 올린 뒤에도 기울기·접지 기준 안 |
| `DrivingSettingsChecks.Run` | 편집기 | 181건 통과 |
| `CabinSoundChecks.Run` | 편집기 | 통과(장소 39, 녹음 9: 급제동 녹음 파일은 남아 있음) |
| `AccidentFaultChecks.Run` | 편집기 | 25건 통과 |
| `-speedLimitCheck` | 실행본, 위치 제어 + 속도 강제 | 30건 통과(기존 22 + 새 8). 구간 1574선분, 표지 126개(차로 지점 205), 빨간 포장 571칸. 은빛초 진입 표지를 지나면 30, 알림 띠 "어린이보호구역 시작 · 제한 30". 피로 80에서 경고 카드. 학생 약속 칩 |
| `-brightUiCheck` | 실행본 화면 배치 | 104건 통과 |
| `-facilityArtCheck -refreshmentCheck` | 실행본 | 47건 통과 |
| `-callBoardCheck` / `-missionCheck` / `-eveningDriveCheck` | 실행본 | 116 / 96 / 31건 통과 |
| `-drivingSettingsCheck` / `-accidentFaultCheck` / `-accidentSeverityUiCheck` | 실행본 | 23 / 11 / 194건 통과 |
| `-oneDayMode -hybridV2 -passengerActivitiesTest` | 실행본 | 92건 통과(승객 사진 식별·탑승 회귀) |
| `-oneDayMode -hybridV2 -passengerBehaviorTest` | 실행본 | **실패**: "food choice actual live satisfaction 1". 이전 기록에 같은 실패가 이전 배포본에서도 재현된 기존 문제로 남아 있다(시험은 음식 허락 만족도 5를 기대, 현재 기본값 3). 92건까지 통과 |

캡처: [보호구역 진입 전](../ui/2026-11-24-play-feedback-3/school-zone-entry.png) · [진입 알림](../ui/2026-11-24-play-feedback-3/school-zone-notice.png) · [경고 카드](../ui/2026-11-24-play-feedback-3/alert-card.png) · [약속 칩·키캡](../ui/2026-11-24-play-feedback-3/appointment-chip.png)

처음에 화면 배치·시설·승객 활동 검사에 `-contentTest`를 잘못 붙여 검사 전용 저장 경로가 바뀌는 바람에 "isolated … campaign"에서 실패했다. 이전 기록의 인수로 다시 돌려 통과했다. 실행본 검사 전후로 플레이어 저장 폴더를 복사·복원했다.

### 검사의 한계

- 위 실행본은 같은 작업 폴더에서 진행 중이던 다른 세션의 미커밋 변경(급행 관련)을 함께 담고 빌드됐다. 마지막 재빌드는 다른 세션의 검사 실행본이 파일을 잡고 있어 `boot.config` 복사에서 "실패"로 끝났지만, 스크립트 DLL은 새로 들어갔고 위 30·104·47·92건은 그 실행본 결과다.
- 캡처에서 빨간 포장이 보이지 않았다. 지면 높이 맞춤과 양면 그리기로 고쳤지만, 고친 뒤 실행본으로 다시 보지 않았다. 약속 칩 두 줄 줄바꿈도 고친 뒤 다시 캡처하지 않았다.
- 전체 지도 캡처는 검사 중 지도가 열리지 않아(`map-sections.png`가 주행 화면) 미니맵에서만 선을 확인했다.

## 미검증 / 남은 것

- 배포 실행본(`builds/EungyeTaxi/`)과 manifest는 갱신하지 않았다(위 변경 파일 참고). 지금 저장소의 exe에는 이 변경이 없다.
- 사람 플레이: 표지·빨간 포장이 달리면서 눈에 띄는지, 지도 선의 양(미니맵 원판이 많음), 캐주얼과 시뮬이 이제 구분되는지, 알림 카드가 운전을 방해하지 않는지, Enter·Esc가 기대대로인지.
- 미션 시작 카드와 구토 카드는 실행본에서 직접 띄워 보지 않았다(같은 경로의 피로 카드만 확인). 운송 결과 Enter·Esc, 사고 팝업 Enter, 다음 날 Enter는 실행본에서 눌러 보지 않았다.
- 짐 승객 사진은 원인(운송 번호 발급 시점)을 고쳤고 기존 승객 검사로 회귀만 봤다. 길가 짐 승객을 실제로 태워 사진을 비교하지는 않았다.
- 보호구역 반경 60m 원이 옆 도로에도 걸리는 곳은 그 도로에도 표지·포장이 생긴다(규칙 그대로).
- 생활거점·퇴근 버튼은 단축키가 없어 키캡을 붙이지 않았다.

## 실행·재현

- 플레이: `builds/EungyeTaxi/EungyeTaxi.exe` → 은계 싱글. 은빛초(은계로 남쪽) 앞에서 보호구역 표지·빨간 포장·알림 띠, 미니맵의 주황·빨강 선.
- 실행본 검사: `EungyeTaxi.exe -contentTest -speedLimitCheck -speedLimitOutput <폴더>` (캡처: `school-zone-entry.png`, `school-zone-notice.png`, `map-sections.png`, `alert-card.png`)
- 편집기 검사: Unity 배치 `-executeMethod SpeedLimitChecks.Run`, `CurveSteeringChecks.Run`, `DrivingSettingsChecks.Run`, `CabinSoundChecks.Run`, `AccidentFaultChecks.Run`
