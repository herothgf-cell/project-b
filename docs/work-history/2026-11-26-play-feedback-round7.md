# 2026-11-26 직접 플레이 피드백 7차 (은계 싱글)

## 요구 (사용자, 2026-11-26, 스크린샷 2장)

1. 승객 요청의 1/2 키 선택도 키캡 아이콘으로 바꾼다.
2. 분실물 미션 중 사고가 나면 안내가 사라져 취소된 것처럼 보이는데, 다음 승객을 태우면 미션이 남아 있다.
3. 은계로 교차로(J6199_11987)에서 좌회전 차들이 줄줄이 서 있다. 원인을 찾고 수정안을 낸다.
4. 파출소도 생활시설처럼 꾸미고, 분실물이 있으면 김밥집처럼 직접 "맡기기"를 고르게 한다.
5. 미터기 J 버튼이 기능이 없으면 J 표시를 지운다.
6. 킥보드가 실제로 부딪힐 위험이 있게 한다.
7. 길 위 "[F] 식사"의 F도 키캡으로 보이게 한다.
8. 공업사 등 생활시설은 정해진 칸이 아니라 시설 구역 안이면 F로 이용하게 한다.
9. 구급차는 잠시 끄고(도로가 한산해 양보가 의미 없음), 다른 돌발을 더 자주 낸다.
10. 내비는 현재 차선이 아니라 현재 위치에서 가장 빠른 길로 안내한다.
11. 학생이 기다리는 "수인로 서쪽"을 지도에 알아보기 쉽게 표시한다.

## 구현

| # | 내용 | 동작 |
|---|---|---|
| 1 | 요청 선택 키캡 | 선택지 번호를 색 상자 대신 `KeyHints.Cap` 키캡(1, 2, 3)으로 그린다. 고를 수 없는 선택지는 흐리게 |
| 2 | 분실물 안내 복구 | 돌려주기·파출소 가기 중 사고나 다른 운행으로 안내가 지워지면, 빈 차로 돌아온 뒤 2초 안에 분실물 안내를 다시 걸고 "안내를 다시 이어요" 카드를 띄운다(`LostItems.Reguide`). 다른 목적지를 안내 중이면 덮어쓰지 않는다 |
| 4 | 파출소 | 도착만으로 처리하지 않는다. 파출소 앞에 서면 "F 은행파출소 · 분실물 맡기기" 띠가 뜨고, F로 신고 창(맡길 물건, 사례 +300원, 맡기기 Enter / 닫기 Esc)을 연다. 주인이 아직 기다리면 "직접 돌려주면 사례가 더 커요"라고 알려 준다. 건물 쪽에 파란 "은행파출소 POLICE" 간판, 지붕 적·청 경광등, 연석→입구 보도, 연석 쪽 노란 정차 칸을 만든다 |
| 5 | 미터기 J | 미터기가 자동이면 J 키캡 없이 "미터기"만, 수동일 때만 J 키캡 |
| 6 | 킥보드 | 나오는 거리 22m→12m, 차 속도×0.9초를 더한 거리에서 출발, 속도 4→5.5m/s. 벨·모습은 0.7초 먼저 |
| 7 | 길 위 F 키캡 | "[F] 식사/휴식/주유/주문" 안내에서 F를 키캡 그림(`Resources/UI/key-cap-f.png`, `tools/art/draw_key_cap.py`)으로 바꿔 글자 왼쪽에 붙인다(`WorldKeyCap`). 멀리서 숨길 때 키캡도 함께 숨긴다 |
| 8 | 시설 구역 | 공업사(부지 포장, 수리·대기 칸 6m), 주유소(부지 윤곽, 주유기 4m), 식당(부지 포장, 주차 칸 6m), 지도 편집 시설(시설 윤곽), 김밥·카페(정차 칸 9m), 숙소 차고(차고 안)에서 정지(0.56m/s 미만)만 하면 F가 된다. 칸·방향 맞추기 조건을 없앴다(`FacilityZone`) |
| 9 | 돌발 | 구급차 기본 꺼짐(`surprise.ambulance.enabled` 0). 하루 최대 5→8, 종류별 2→3, 최소 간격 120→60초. 설정 카탈로그·기본 프로필(해시 재계산)·게임 사본을 함께 바꿨다 |
| 10 | 내비 | 경로를 현재 차선뿐 아니라 옆 같은 방향 차선(1.2~6.5m 옆, 차선 끝까지 25m 이상)에서도 계산한다. 차선 변경 4초를 더해도 2초 이상 빠르면 그 길을 안내하고, "오른쪽/왼쪽 차로로 이동 · 그 차로가 더 빠름"을 앞에 붙인다. 옆 차선 출발 경로는 첫 60m 동안 차선 폭만큼 벗어나도 재탐색하지 않는다 |
| 11 | 학생 약속 | 약속을 한 날부터 재회까지 전체 지도·미니맵의 약속 정차 칸에 학생 얼굴 핀(파란 테, 그 시간이면 초록)과 "학생 약속 · N일차 20:00"(미니맵은 핀만)을 그린다. 운행 화면의 약속 칩을 누르면 전체 지도가 그 장소에 맞춰 열린다 |

## 좌회전 정체 원인 (3번, 수정 제안만)

`-contentTest -districtTrafficTest`(검사용 빌드)로 확인했다([결과](../ui/2026-11-26-play-feedback-7/district-traffic-test.txt)).

- 원인: 은계로 북서행 1차로(`reviewed:G05:NW:left`)는 좌회전과 유턴을 함께 쓴다. 이 유턴은 "좌회전·보행 신호 때만" 허용이라 240초 중 40초만 갈 수 있다. 맨 앞에 유턴 차가 서면 뒤 좌회전 차들이 함께 묶인다. 같은 일이 남동행 J7801_9629 1차로에서도 나왔다(정체 3대).
- 제안 A(권장): 일반 차량은 두 교차로의 공용 차로에서 유턴 경로를 고르지 않게 한다. 택시(플레이어)의 유턴 규칙은 그대로다.
- 제안 B: 유턴 허용을 "좌회전 신호 때"까지 넓히거나 좌회전 신호 시간을 늘린다. 실제 신호와 달라질 수 있다.
- 기존 실패: 같은 검사의 "green queue clears"(신호 대기열 시험 3대) 실패는 이번 변경과 무관한 기존 문제다.

## 변경 파일

- 새 파일: `FacilityZone.cs`, `WorldKeyCap.cs`, `Resources/UI/key-cap-f.png`, `tools/art/draw_key_cap.py`
- `PassengerDialogueUI.cs`(1), `LostItems.cs`(2·4), `CockpitUI.cs`(5), `RoadSurprisesTwo.cs`·`RoadSurprises.cs`(6·9), `FuelStationLayout.cs`·`RefreshmentStorefronts.cs`·`ProjectMapScene.cs`·`WorldBuilder.cs`(7), `WorkshopLayout.cs`·`RestaurantSiteLayout.cs`·`RefreshmentShops.cs`·`ServiceStops.cs`(8), `RoadNavigation.cs`(10), `AppointmentChip.cs`·`FullMapOverlay.cs`·`DrivingReadability.cs`(11)
- 설정: `config/content/catalog.json`, `default-profile.json`과 Resources 사본
- 검사: `LostItemProbe.cs`(안내 복구·파출소 메뉴·꾸밈), `ShiftDevEndProbe.cs`·`FuelStationProbe.cs`·`RestaurantLotProbe.cs`(새 시설 구역 규칙)

## 검증

검사용 빌드 `artifacts/r7-trial`(지도 r74 포함)로 했다.

| 검사 | 결과 |
|---|---|
| 설정 툴 `node --test tests/content-editor/*.test.mjs` | 109건 통과 |
| `-contentTest -lostCheck` | 29건 통과. 안내 지운 뒤 다시 이어짐, 파출소 앞 정지만으로는 처리 안 됨, F 메뉴, 꾸밈 물체, 맡기기 +300원 |
| `-contentTest -surpriseCheck` / `-surprise2Check` | 30 / 22건 통과(구급차 장면은 강제 실행이라 그대로 확인) |
| `-facilityArtCheck -refreshmentCheck` | 47건 통과(가게는 어느 방향이든 이용) |
| `-brightUiCheck` | 104건 통과. 요청 키캡 캡처 |
| `-contentTest -districtTrafficTest` | 실패(위 정체 3대와 기존 신호 대기열 시험). 원인 분석용 |
| `-fuelFacilitiesTest`, `-restaurantLotTest` | 시작하자마자 NullReference로 멈춤. 배포 실행본(이번 변경 전)에서도 같아 기존에 깨진 검사다. 기대값만 새 규칙으로 고쳐 두었다 |

캡처: [요청 키캡](../ui/2026-11-26-play-feedback-7/request-keycaps.png) · [길 위 F 키캡·미터기](../ui/2026-11-26-play-feedback-7/world-keycap-and-meter.png) · [파출소 신고 창](../ui/2026-11-26-play-feedback-7/police-desk.png)

## 미검증 / 남은 것

- 좌회전 정체는 원인만 찾았고 고치지 않았다(제안 A/B 결정 대기).
- 파출소 간판·경광등·보도는 물체가 생긴 것만 검사했고 화면으로 보지 않았다. 학생 약속 핀, 내비 옆 차선 안내, 공업사 구역 F는 캡처·주행으로 보지 않았다.
- 내비 옆 차선 출발은 실선 구간에서도 차로 변경을 권할 수 있다(차로 변경 위반 판정은 그대로).
- 배포 실행본은 갱신하지 않았다.

## 실행·재현

- 아이콘 다시 그리기: `python tools/art/draw_key_cap.py`
- 검사: `EungyeTaxi.exe -contentTest -lostCheck`, `-contentTest -districtTrafficTest`(결과는 `artifacts/district-traffic-test.txt`)
