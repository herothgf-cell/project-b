# 2026-11-24 제한속도 완화(확인 구간만, +10 여유 안 사고 가중)와 생활시설 비교 카드 가독성

## 요구 (사용자 직접 플레이 피드백, 2026-11-24)

1. 생활시설 비교 UI 항목의 가독성을 높인다(캡처: 문장형 카드 "이동 약 16초 / 36m · 진입·출차 포함 …").
2. 제한속도가 게임을 답답하게 한다. 다음을 검토한다.
   - 제한 +10km/h까지는 넘어도 봐주되, 그 상태에서 사고가 나면 과실을 더 묻는다.
   - +11 이상은 지금처럼 일정 시간 지나면 감점하고, +11 밑으로 내려가면 시간을 초기화한다.
   - 아니면 제한속도를 특정 구간에만 아주 적게 둔다. 전 구간에 있으니 운행을 너무 제한한다.

## 검토와 결정

- **+11 이상 규칙은 이미 그렇게 동작하고 있었다.** 기존 판정은 "제한 + 10 초과가 3초 이어지면 감점, 그 아래로 내려가면 시간 0으로"였다(같은 과속 구간에서는 한 번, 기록 뒤 20초 동안은 다시 기록하지 않음). 그대로 두었다.
- **답답함의 주된 원인은 "전 구간 제한"이었다.** 확인되지 않은 일반 도로 50, 번길 30이 게임 추정으로 모든 길에 걸려 있었고, 일반 차량이 그 90%(번길·학교 앞 27km/h)로 달렸다.
- 그래서 두 안을 합쳤다.
  1. **제한속도는 확인된 구간에만 둔다(기본).** 수인로 60, 대은로 50(단속 카메라로 확인), 어린이보호구역 13곳 반경 60m 안 30. 그 밖의 도로는 제한속도가 없다(속도계 옆 표시도 없음, 일반 차량은 이전처럼 약 47km/h). 이전 동작은 설정 `law.speed.estimatedRoads`=1로 돌아간다.
  2. **+10 여유 안의 과속은 기록하지 않지만, 그 상태의 사고는 무겁게 본다.**

## 구현

| 내용 | 동작 |
|---|---|
| 확인 구간만 제한 | `RoadSpeedLimits.RoadLimit`이 파일에 없는 도로에는 0(제한 없음)을 준다. 제한 없는 도로라도 보호구역 반경 안이면 30. 설정 `law.speed.estimatedRoads`(기본 0) |
| 여유 구간 | 제한 +1 ~ +10km/h. 기록 없음. 속도계 옆 표시의 안쪽이 연한 노랑으로 바뀐다(깜빡임 없음). +10을 넘으면 이전처럼 빨갛게 깜빡인다 |
| 여유 구간 사고 · 과실 | 접촉한 순간 제한을 넘고 있었다면: 뒤에서 받힘(원래 상대 과실)은 쌍방 과실로, 돌발 차량에 피할 시간 없이 받힘(원래 상대 과실)도 쌍방 과실로 바뀐다. 내 과실은 그대로 내 과실 |
| 여유 구간 사고 · 추가 감점 | 사고 처리가 시작되면 법규 기록 "제한속도 초과 중 사고"(운전 점수 −5)를 따로 남긴다. 설정 `law.speed.crashPenalty`(기본 5). 상대 과실로 끝난 접촉은 사고 처리가 없으므로 기록하지 않는다 |
| 생활시설 카드 | 이름 띠 → 아이콘 두 줄(거리·시간 / 대기, 요금 / 이용 시간) → 연료·포만·피로 막대 세 개(지금 → 이용 뒤, 변화량 색 표시: 좋아지면 초록, 나빠지면 빨강, 피로는 낮을수록 좋음) → "길 안내" 버튼. 가장 가까운 시설은 초록 띠·"가장 가까움" 배지·채운 버튼. 예전 문장은 카드에 마우스를 올리면 나온다 |

캡처: [1920](../ui/2026-11-24-facility-cards/facilities-1920.png) · [1280](../ui/2026-11-24-facility-cards/facilities-1280.png). 사용자 캡처의 "생활시설 비교" 문장형 화면은 텍스트 줄이기 1차 이전 실행본으로 보인다. 이번 카드는 그 뒤의 세 줄 요약을 다시 바꾼 것이다.

## 변경 파일

- `unity/EungyeTaxi/Assets/Scripts/RoadSpeedLimits.cs`: `EstimatedRoads`, `CrashPenalty`, `AboveLimit`, 확인 구간만 제한, 속도계 표시 색
- `AccidentFault.cs`: `FaultInput.aboveLimit`, 여유 구간이면 상대 과실 → 쌍방 과실
- `VehicleDamageController.cs`: 첫 접촉 순간의 제한 초과 여부를 기억하고, 사고 처리가 시작되면 추가 감점
- `DrivingLaw.cs`: `RecordSpeedingCrash`
- `ServiceStopsPlannerUI.cs`: 생활시설 카드(`DrawFacilityCard`, `Gauge`)
- 검사: `Assets/Editor/SpeedLimitChecks.cs`(40건), `Assets/Scripts/SpeedLimitProbe.cs`(22건)
- 설정: `config/content/catalog.json`, `default-profile.json`과 Resources 사본(항목 2개, `choice-catalog.mjs generate`로 해시 재생성)
- 문서: 이 파일, `docs/ui/2026-11-24-facility-cards/`, `docs/prototype-development-checklist.md`, `docs/content-design/08-fun-improvements.md`, `docs/CURRENT-STATE.md`, `docs/work-history/LATEST.md`

## 검증

| 검사 | 종류 | 결과 |
|---|---|---|
| `SpeedLimitChecks.Run` | 편집기 규칙 | 40건 통과. 기본은 확인 구간만, 스위치 1이면 이전 추정값, 보호구역은 제한 없는 도로에서도 30, 여유 구간 경계(+1), 과실 표(뒤에서 받힘·돌발 → 쌍방, 내 과실 유지) |
| `AccidentFaultChecks.Run` | 편집기 규칙 | 25건 통과(기존 과실 규칙 회귀) |
| 설정 툴 시험 | Node `node --test tests/content-editor/*.test.mjs` | 109건 통과 |
| `-speedLimitCheck` | 실행본, 위치 제어 + 속도 강제 | 22건 통과. 계수로·마유로·은행로144번길·학교 밖 은계로 = 제한 없음, 수인로 60, 대은로 50, 보호구역 30. 수인로 68km/h 4.5초 기록 없음, 75km/h 3초 넘게 1건. 사고 기록 함수 호출로 5점 기록 |
| `-accidentFaultCheck` | 실행본, 위치 제어 | 11건 통과 |
| `-brightUiCheck` | 실행본 화면 배치 | 104건 통과. 시설 카드 1280·1920 캡처를 눈으로 확인(첫 빌드에서 사실 줄이 잘려 두 줄로 고친 뒤 다시 빌드) |
| `-facilityArtCheck -refreshmentCheck` | 실행본 | 47건 통과 |
| `-callBoardCheck` | 실행본 | 115건 통과 |

실행본 검사 전에 플레이어 저장 폴더(`%USERPROFILE%\AppData\LocalLow\Taxi Prototype`)를 복사해 두었다가 검사 뒤 되돌렸다.

일반 차량: 72대 중 제한이 걸린 차로에 있는 차는 16대(이전 72대). 나머지는 이전 순항 속도로 달린다.

### 검사의 한계

- 여유 구간 사고는 실제 충돌로 만들지 않았다. 과실 표는 편집기에서, 추가 감점은 실행본에서 기록 함수를 직접 불러 확인했다. 실제 접촉에서 "첫 접촉 순간 속도"가 잡히는지는 사람 플레이로 봐야 한다.
- 속도계 표시의 연한 노랑이 눈에 띄는지 보지 않았다.
- 시설 카드의 마우스 올림 문장, 대기 차량이 있을 때와 견적 불가 카드의 모습은 캡처하지 않았다.

## 미검증 / 남은 것

- 첫 커밋(1f9d8e6c)은 소스만 담았다. 작업 중 같은 폴더에서 다른 세션의 주행음 녹음 교체가 진행되고 있었기 때문이다. 그 작업도 커밋(2a5c6261)된 뒤 HEAD 1f9d8e6c에서 다시 빌드했다. 그 실행본으로 제한속도 22, 사고 과실 11, 호출판 115, 화면 배치 104, 시설 47건을 다시 통과했다. 그 뒤 `distribution/EungyeTaxi-build-manifest.json`을 다시 만들어 대조했다(145파일, 불일치 0, pwsh가 없어 같은 규칙의 Python으로 생성·대조). **배포 실행본에는 주행음 녹음 교체도 함께 들어 있다.** 그 작업의 실행본 검사는 여기서 하지 않았다.
- 사람 플레이: 제한 없는 도로의 주행감, 학교 앞 30 구간이 적당한지, 여유 구간 사고가 납득되는지.
- 보호구역 반경 60m가 교차하는 다른 도로까지 30으로 만드는 곳.

## 실행·재현

- 플레이: `builds/EungyeTaxi/EungyeTaxi.exe` → 은계 싱글. M 지도 → 생활시설.
- 이전처럼 모든 도로에 제한속도: 설정 툴 "법규" → `law.speed.estimatedRoads` = 1.
- 규칙 검사: Unity 배치 `-executeMethod SpeedLimitChecks.Run`, `-executeMethod AccidentFaultChecks.Run`
- 실행본 검사: `EungyeTaxi.exe -contentTest -speedLimitCheck -speedLimitOutput <폴더>`, `EungyeTaxi.exe -brightUiCheck -brightUiOutput <폴더>`
