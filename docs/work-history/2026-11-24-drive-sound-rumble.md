# 2026-11-24 감각 연출 남은 부분: 추가 주행음과 패드 진동 (08 1장, 은계 싱글)

## 요구

자리 비움 일괄 작업(2차)의 1단계. [08](../content-design/08-fun-improvements.md) 1장 감각 연출 중 남은 소리와 패드 진동.

- 엔진 부하음, 한계 근처 타이어 소리, 속도별 바람 소리. 손님 대사와 라디오 자막이 우선이다.
- 패드 진동: 노면, 제동, 접촉. 설정 감각 탭에서 0~100% 조절.
- 새 음원은 외부에서 받지 않는다. 기존 녹음을 가공할 수 있는 범위만 하고, 부족한 음원 목록을 남긴다.

## 코드 대조

- `VehicleAudio`: 엔진(`Engine.wav`, 6단 자동변속 회전 모델로 피치·음량)과 노면(`Road.wav`, 속도²)을 루프로 재생한다. 깜빡이·경적·빗소리도 있다. 병합한 `storefront-drive-audio` 작업에서 엔진이 세단 수준으로 낮아졌다.
- 녹음 파일은 `Resources/Audio/Taxi`의 Engine·Road·Rain·Horn·Indicator 6개뿐이다. 타이어 스키드음과 바람음 전용 녹음은 없다.
- 패드: 이 프로젝트는 Unity 기본 입력(Input Manager)만 쓰고 Input System 패키지가 없다. 그래서 Unity 쪽에는 진동 API가 없다.
- 대사 표시: `PassengerRequestUI.HasVisiblePanel`(손님 요청 창), `RadioTicker.VisibleKind`(라디오 자막).

## 실제 구현

- `DriveFeelAudio.cs`:
  - 엔진 부하음: 기존 엔진 녹음을 낮게(피치 0.62~0.87) 재생하는 층이다. 가속 페달 세기와 회전수에 비례하고 최대 음량은 0.16이다.
  - 타이어 한계음: 기존 노면 녹음을 높게(피치 1.9) 재생하는 층이다. 바퀴 미끄러짐(옆 방향 또는 앞뒤 ×0.7)이 0.12에서 시작해 0.35에서 최대(0.22)가 된다. 시속 10.8km 미만에서는 나지 않는다.
  - 바람 소리: 기존 노면 녹음을 낮게(피치 0.5~0.62) 재생하는 층이다. 시속 35km에서 시작해 90km에서 최대(0.18)가 된다.
  - 손님 요청 창이나 라디오 자막이 보이면 세 층 모두 ×0.45로 줄인다. 기존 엔진·노면·깜빡이 소리는 그대로다.
  - 패드 진동:
    - 노면: 저주파 모터, 최대 0.08, 시속 54km까지 비례
    - 제동: 고주파 모터, 2m/s² 넘는 감속 1m/s²마다 0.12
    - 접촉: 충돌 감점 순간 양쪽 0.9로 0.25초
- `XInputRumble`: Windows 기본 `xinput1_4.dll`의 `XInputSetState`를 직접 부른다. 새 패키지나 외부 파일은 받지 않았다. 대상은 Xbox 계열(XInput) 패드 1번이다. 실패하면 조용히 끈다. 일시정지, 메뉴, 창 포커스 잃음, 종료 때는 진동을 멈춘다.
- `VehicleAudio`: 세 층을 별도 자식 오브젝트("Extra drive layers")에 붙였다. 기존 검사가 원래 엔진·노면 음원을 하나씩 찾기 때문이다. 진동은 패드가 연결돼 있고 싱글 주행 중일 때만 보낸다. PvP에서는 둘 다 꺼진다.
- 설정 감각 탭:
  - "추가 주행음"과 "패드 진동" 막대(0~100%, 기본 100%)를 추가했다.
  - 막대 6개가 들어가도록 2줄짜리 작은 행으로 바꿨다.
  - 예전 설정 파일에는 이 값이 없으므로 100%로 열린다.
- 설정 툴 "감각 연출" 묶음 13개(`feel.*`).

### 판단 기록 (임의로 정한 값)

| 항목 | 값 | 이유 |
|---|---|---|
| 진동 방식 | Windows XInput 직접 호출 | 새 패키지(Input System)를 받지 않는 가장 무난한 방법. Xbox 계열 패드만 지원 |
| 음원 | 기존 Engine·Road 녹음 피치 가공 | 외부 음원 금지 조건. 아래 부족 목록 참고 |
| 대사 우선 | 새 층만 ×0.45 | 기존 엔진·노면 소리는 이미 검증된 혼합이라 건드리지 않음 |
| 각 음량·임계값 | 위 구현 절의 수치 | 기존 엔진(0.12~0.38)·노면(최대 0.62)보다 작게 해서 덮지 않게 |
| 적용 범위 | 은계 싱글 | 요청 범위. PvP 소리·물리 무변경 |

## 부족한 음원 목록 (녹음이 생기면 교체할 것)

1. **타이어 스키드음**: 아스팔트에서 옆으로 미끄러지는 고음 루프(2~4초, 이음매 없는 루프). 지금은 노면 녹음을 높인 소리라 "쉬익"에 가깝다.
2. **바람음**: 시속 60~100km에서 창을 닫았을 때의 실내 바람 루프. 지금은 노면 녹음을 낮춘 소리로 대신한다.
3. **엔진 부하음**: 같은 차량의 가속 부하 상태 실내 녹음(저회전 부하). 지금은 공회전 녹음을 낮춘 소리다.
4. (선택) 제동 때 패드·디스크 마찰음, 연석 접촉음.

## 검증

| 검사 | 결과 |
|---|---|
| `FeelSoundChecks.Run` (Editor) | `FEEL_SOUND_OK checks=11`(각 층의 시작·증가, 주차 중 무음, 대사 감쇠, 진동 3종, 설정 0~1 범위·예전 설정·복사) |
| `CurveSteeringChecks.Run`(설정 181·193건 포함), `CabinSoundChecks.Run` | 통과 |
| 설정 툴 Node 109건 | 통과 |
| `-drivingSettingsCheck -drivingSettingsCapture` | `DRIVING_SETTINGS_PLAYER_OK checks=23`, [감각 탭 화면](../ui/2026-11-24-feel/settings-feel-tab.png) |
| `-brightUiCheck` | `BRIGHT_UI_RESULT PASS checks=102` |
| `-cabinControlsCheck -taxiAudioCheck` | `CABIN_PLAYER_OK checks=35`, `TAXI_ENGINE_MIX` 기존 엔진 측정값은 병합 직후와 같음 |
| `-facilityArtCheck -refreshmentCheck` | `REFRESHMENT_PLAYER_OK checks=40` |

첫 실행에서 `-taxiAudioCheck`가 "Sequence contains more than one matching element"로 멈췄다. 새 층이 같은 녹음을 쓰면서 엔진·노면 음원이 여러 개가 된 탓이라, 새 층을 자식 오브젝트로 옮겼다(검사 기대값은 바꾸지 않음).

## 남은 것 · 직접 확인

- **소리 체감 전부**: 세 층이 실제로 엔진 부하·타이어·바람처럼 들리는지, 시끄럽지 않은지, 대사 중 감쇠가 충분한지 확인해야 한다(이 PC에서는 듣지 못함).
- **실물 패드 진동**: Xbox 계열 패드로 노면·제동·충돌 진동을 느끼는지, 세기가 적당한지 확인해야 한다. 그 밖의 패드(DualSense 등)는 지원하지 않는다.
- 녹음 교체 여부(위 부족 목록).

## 재현

```powershell
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod FeelSoundChecks.Run
```

게임에서: 은계 싱글 → Esc → 설정 → 감각 탭에서 "추가 주행음"과 "패드 진동"을 각각 0%와 100%로 바꿔 가며 비교한다. 시속 60km 이상 주행, 급한 코너, 급제동, 손님 요청 창이 떠 있을 때를 들어 본다.
