# 2026-11-24 직접 플레이 피드백 4차: 수첩 레드닷, 김밥·카페 메뉴 카드, 과속 구간 축소, 헤드라이트·가로등, 승객 부탁의 대가와 UI

## 요구 (사용자, 2026-11-24)

1. 기사 수첩에 새 정보가 들어오면 레드닷을 단다.
2. 야채김밥·카페도 기사식당처럼 메뉴를 그림으로 보여 주고 비슷한 UI로 꾸민다.
3. 과속 구간을 더 줄인다.
4. 헤드라이트가 너무 어둡다. 더 밝게 비춘다.
5. 주요 상가·주거 지역에는 가로등이 많았으면 한다(밤에도 밝게).
6. 승객 부탁의 모든 선택에 패널티가 너무 없다. 예: 만족도를 올리되 피로가 쌓이는 선택지.
7. 승객 부탁 UI가 텍스트뿐이라 가독성을 높인다.

## 구현

| 내용 | 동작 |
|---|---|
| 수첩 레드닷 | 탭마다 내용 열쇠(아버지: 열린 페이지 수·학생 약속 상태, 이야기: 결말 수, 손님: 만난 유형 수, 동네: 별 합계, 기록: 업적 수)를 두고, 그 탭을 마지막으로 열었을 때와 다르면 빨간 점을 단다. 운행 화면 "기사 수첩" 버튼, 숙소 메뉴의 "기사 수첩" 버튼, 수첩 안 탭 버튼에 표시한다. 본 값은 저장(`CampaignLogbook.seenTabs`)에 남는다. 이전 저장을 처음 열 때는 아버지 탭만 새로 표시한다 |
| 김밥·카페 메뉴 | 가게 팝업이 텍스트 한 줄 대신 기사식당과 같은 그림 카드(파란 테두리, 메뉴 이름 띠)를 보여 준다. 오른쪽에는 가격, 이용 시간, 포만감 또는 피로를 아이콘 줄로 보여 주고, 지금 → 이용 뒤 값을 같이 적는다. 주문하기(Enter)·닫기(Esc)·다른 시설. 그림 `kimbap.png`·`coffee.png`는 `tools/art/draw_refreshment_cards.py`로 그렸다(기사식당 그림은 승인된 생성 그림이라 같은 방식으로 만들 수 없어 삽화풍으로 맞춤) |
| 과속 구간 | 대은로 50을 끈다(`inactive`, 단속 카메라 근거는 파일에 남김). 어린이보호구역 13곳의 반경을 60m에서 40m로 줄였다(구역별 `radius`, 설정 `law.speed.zoneRadius`는 상한). 남은 제한은 수인로 60과 보호구역 30뿐이다. `config/maps/` 사본도 같게 맞췄다 |
| 헤드라이트 | 택시 전조등 빛 거리 24→80m, 퍼짐 47°→58°, 세기 0.94→2.6배, 아래로 숙인 각 6°→1.5°. 일반 차량은 그대로 |
| 가로등 | 이름이 있는 건물(아파트·단지·상가·프라자·타워 등. 공장·물류·창고·산업은 제외)이 55m 안에 2채 이상인 도로 구간에 22m 간격으로 가로등을 더 세운다(차로 오른쪽 5.2m에서 인도 쪽으로 자리 찾기). 전체 한도 140→560개. 실제 Light 없이 표면만 늘리므로 밤 성능 부담은 작다 |
| 부탁 선택지의 대가 | 설정 `default-overrides.json`과 승인 밸런스 `presets/passenger-balance-v1.json`(해시 재계산)을 함께 바꿨다. 그냥 넘기기("지금은 어려워요")는 만족도 −3, 안전한 거절(안전 하차·목적지 변경·경유)은 −2다. 말 상대(공감·응원·동네 이야기)는 만족 +5에 피로 +3~4다. 조용한 배려(대안)는 만족 +2에 피로가 없다. 서두르는 선택(ETA 빠르게 +3, 빠른 길 성공 +5, 제시간)은 피로 +2~3, 부드럽게 운전 +2, 경유 수락 +3, 목적지 변경 +2다 |
| 부탁 UI | 선택지 아래의 효과 문장을 칩으로 바꿨다. 웃는 얼굴 "만족 +5"(초록)·"만족 −3"(빨강), 침대 "피로 +4"(주황), "성공 시"/"실패 시" 앞말을 붙인다. 숫자가 아닌 설명은 회색 한 줄로 줄이고 마우스를 올리면 전부 보인다 |

## 변경 파일

- 새 파일: `tools/art/draw_refreshment_cards.py`, `unity/EungyeTaxi/Assets/Resources/UI/Facilities/kimbap.png`, `coffee.png`(+ `.meta`)
- `CampaignLogbook.cs`, `LogbookHUD.cs`, `CockpitUI.cs`, `DriveHUD.cs`(레드닷)
- `FacilityArt.cs`, `ServiceStops.cs`(메뉴 카드)
- `RoadSpeedLimits.cs`, `Resources/eungye-speed-limits.json`, `config/maps/eungye-speed-limits.json`(과속 구간)
- `DayLightCity.cs`, `TrafficSystem.cs`(`RoadGraph`)(전조등·가로등)
- `PassengerDialogueUI.cs`(효과 칩), `config/content/default-overrides.json`, `config/content/presets/passenger-balance-v1.json`, 생성된 `catalog.json`·`default-profile.json`과 Resources 사본
- 검사: `Editor/SpeedLimitChecks.cs`, `Scripts/SpeedLimitProbe.cs`(대은로 비활성, 40m)
- 실행본: `builds/EungyeTaxi/`, `distribution/EungyeTaxi-build-manifest.json`
- 문서: 이 파일, `docs/ui/2026-11-24-play-feedback-4/`, 체크리스트, `CURRENT-STATE.md`, `LATEST.md`

## 검증

| 검사 | 종류 | 결과 |
|---|---|---|
| 설정 툴 `node --test tests/content-editor/*.test.mjs` | Node | 109건 통과(승인 밸런스 묶음을 새 값으로 갱신, 해시 재계산) |
| `SpeedLimitChecks.Run` | 편집기 | 40건 통과(대은로 비활성, 보호구역 40m) |
| `HudDialogueReadabilityChecks.Run` | 편집기 | 16건 통과 |
| `-speedLimitCheck` | 실행본, 위치 제어 | 30건 통과. 구간 1134선분, 표지 84개(차로 지점 156), 빨간 포장 419칸. 진입 표지 넘어 30·알림 띠 |
| `-facilityArtCheck -refreshmentCheck` | 실행본 | 47건 통과. 김밥·카페 팝업 캡처 추가 |
| `-brightUiCheck` | 실행본 화면 배치 | 104건 통과(승객 요청 칩 화면 포함) |
| `-eveningDriveCheck` | 실행본(밤) | 31건 통과. 전조등 캡처 |
| `-dayLightNightProbe -speedLimitCheck` | 실행본(밤 강제) | 30건 통과, 로그 `streetLamps=336`(이전 상한 140) |
| `-oneDayMode -hybridV2 -passengerActivitiesTest` / `-missionCheck` / `-accidentFaultCheck` / `-callBoardCheck` | 실행본 | 92 / 96 / 11 / 115건 통과(전조등 각도·검사 동선 수정 전 빌드) |
| `-oneDayMode -hybridV2 -passengerBehaviorTest` | 실행본 | 실패(기존 문제, 음식 만족도 기대값) |
| `-hurryCheck`(다른 세션의 유혹 기능) | 실행본 | 실패. 다른 세션이 커밋한 실행본(11310729)에서도 다른 지점에서 실패해 이번 변경과 무관한 불안정으로 봤다 |

캡처: [김밥 메뉴](../ui/2026-11-24-play-feedback-4/menu-snack.png) · [카페 메뉴](../ui/2026-11-24-play-feedback-4/menu-coffee.png) · [승객 요청 칩](../ui/2026-11-24-play-feedback-4/passenger-request.png) · [밤 전조등](../ui/2026-11-24-play-feedback-4/night-headlamps.png) · [보호구역 빨간 포장](../ui/2026-11-24-play-feedback-4/school-zone-red-paint.png)

고친 점: 3차의 빨간 포장은 앞뒤 양면을 그려 법선이 상쇄되면서 **검게** 칠해져 있었다(캡처에서 "안 보임"). 위를 향한 한쪽 면만 그리고 지면 위 6cm로 올렸다. 차선·횡단보도 표시를 덮는다. 전조등은 Unity 감쇠 곡선 때문에 빛 거리의 약 4분의 1까지만 밝아, 거리를 80m(밝은 곳 약 20m)로 잡았다. 실행본 검사 전후로 플레이어 저장 폴더를 복사·복원했다.

## 미검증 / 남은 것

- 사람 플레이: 밤 주행의 전조등 밝기와 상가·주거 지역 가로등 양, 부탁 선택지의 대가가 고민이 되는지(피로 수치가 적당한지), 칩 UI 가독성, 김밥·카페 그림의 어울림.
- 빨간 포장이 차선·횡단보도 표시를 덮는다. 표시를 위로 다시 그릴지 결정이 필요하다.
- 레드닷은 수첩을 실제로 열어 사라지는 흐름을 실행본에서 눌러 보지 않았다.

## 실행·재현

- 플레이: `builds/EungyeTaxi/EungyeTaxi.exe` → 은계 싱글. 은계김밥·호수카페에 정차 후 F. 승객 요청 창. 밤(퇴근 후 드라이브)에 상가 거리.
- 그림 다시 그리기: `python tools/art/draw_refreshment_cards.py`
- 설정 다시 만들기: `node -e "import('./tools/content-editor/choice-catalog.mjs').then(m=>m.generate(process.cwd()))"`, 시험 `node --test tests/content-editor/*.test.mjs`
