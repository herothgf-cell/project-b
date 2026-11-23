# 전체 경로 안내·기사식당 팝업·카페 아이콘·공복 TIP

## 요구사항(2026-11-23 사용자)

1. 내비게이션이 차 앞부분만 순차적으로 보여주는데, 예전처럼 처음부터 전체 경로를 안내한다.
2. 기사식당 팝업: "다른 시설 찾기: M 지도" 문구와 "다른 시설·견적 비교" 버튼을 없앤다. 닫기 버튼은 "닫기"만 쓰고, F7로 닫는 동작은 빼고 ESC는 유지한다.
3. 포만감이 떨어지면 피로가 빨리 쌓인다는 알림에 TIP을 추가한다.
4. 지도에서 아메리카노 생활 시설을 카페 아이콘으로 바꾼다.

## 실제 구현

1. `RoadNavigation.RefreshDisplay`: 2026-11-20(172138d)에 넣은 바닥 안내선 120m 제한을 없앴다. 이제 남은 전체 경로를 그린다. 지도와 같다.
2. `ServiceStops`: 기사식당이 있는 팝업에서는 M 지도 문구와 견적 비교 버튼을 지우고 "닫기" 버튼을 전체 폭으로 키웠다. 다른 시설 팝업의 버튼 글자도 "닫기  F7"에서 "닫기"로 바꿨다. F7은 팝업을 열기만 하고 닫지 않는다. 열린 팝업은 ESC로 닫는다. 같은 프레임에 일시정지가 열리지 않도록 `EscapeFrame`으로 막는다. 기존에는 ESC로 팝업이 닫히지 않았다. 아래쪽 안내 칸에 보여줄 글이 없으면 노란 칸도 그리지 않는다.
3. `DriveNotifications`: 실제 규칙(`DriverCondition.Advance`: 포만 25 이하 피로 1.5배, 10 이하 2배)을 TIP으로 만들었다. 이 TIP을 반복 TIP에 넣고, 포만감이 25·10 아래로 처음 내려가는 순간 바로 띄운다.
4. `HudGlyph`에 커피잔 그림(20번)을 추가했다. 동네 카페(아메리카노)의 지도 아이콘을 휴식 그림(11)에서 카페 그림(20)으로 바꿨다. 기사 수첩의 주변 장소 검색에도 20번을 포함했다.
- 검사: `ShiftDevEndProbe -facilityArtCheck`에 기사식당 팝업 첫 화면 캡처, 카페 아이콘 확인과 지도 캡처를 추가했다.

## 변경 파일

`RoadNavigation.cs`, `ServiceStops.cs`, `DriveSession.cs`, `DriveNotifications.cs`, `HudGlyph.cs`, `MapDestinationUI.cs`, `LogbookNeighborhood.cs`, `ShiftDevEndProbe.cs`, `builds/EungyeTaxi/`, `distribution/EungyeTaxi-build-manifest.json`, `docs/ui/2026-11-23-route-diner-cafe/`, 이 기록, `LATEST.md`, 체크리스트.

## 검증

- Unity 2022.3.62f2 `PrototypeBuild.BuildHybrid`: 종료 0, 컴파일 오류 0.
- `-facilityArtCheck` 1920×1080: 28건 통과. 실패 0. 새 검사(식당 팝업 열림, 카페 아이콘 20번)를 포함한다. 화면: [기사식당 팝업](../ui/2026-11-23-route-diner-cafe/diner-popup.png), [지도 카페 아이콘](../ui/2026-11-23-route-diner-cafe/map-cafe-icon.png).
- `pwsh -File tools/test_home_base_player.ps1`: 위치 제어 24건 통과.
- `-homeBaseCheck -chatNoticeCapture`: 9건 통과. [출차 화면의 안내선](../ui/2026-11-23-route-diner-cafe/route-full.png).
- `-brightUiCheck`: 102건 통과.

## 미검증

- F7·ESC 실제 키 입력은 자동 검사에서 누르지 않았다. 코드 경로만 바꿨고 수동 확인이 필요하다.
- 공복 TIP이 실제 운행 중 25·10 경계에서 뜨는 장면은 캡처하지 않았다.
- 전체 경로 바닥선이 같은 교차로를 다시 지나는 경로에서 겹쳐 보일 수 있다(120m 제한을 넣었던 이유). 수동 주행으로 확인이 필요하다.
