# 채팅형 운행 알림 시안

## 요구사항

운행 알림을 채팅처럼, 알림 요소별로 UI가 구분되게 하고 시안을 만든다.

## 현재 구조(코드 대조)

- `DriveNotifications.DrawFeed`가 `List<string> feed`(최근 12줄)를 모두 같은 글꼴·`CasualUI.Ink` 색으로 그린다. 종류는 문자열 앞머리("만족도 ·", "운전 ·", "TIP ·")로만 드러난다.
- 출처: 승객 만족도/법규 점수 `Push`, 연료 `fuelLine`(15·10·5%), `business.notice`, `services.notice`, `services.destination`, 20초 TIP, `ServiceLawZones` context(점유·운송 결과·대기 상태).
- 주유소 안내·점유 선택·운전 평가 버튼은 `ServiceLawZones.OnGUI`에서 패널 바닥에 따로 그린다.

## 시안 내용

- 7종 구분: ① 승객 만족도 = 얼굴 아바타 말풍선 ② 법규 = 왼쪽 색 띠(위반 빨강/준수 초록) 방패 줄 ③ 연료 = 게이지가 있는 경고 카드, 5% 이하 빨강, 버튼 내장 ④ 목적지 = 파란 카드(장소 굵게·행동 설명·M 키) ⑤ 상황 = 선택 버튼이 들어간 카드 ⑥ 시스템 = 가운데 회색 한 줄 ⑦ TIP = 점선 메모(최신 1개만 유지).
- 공통: 증감 칩(+초록/−빨강), 게임 시각 구분선, 지난 운송 메시지 흐림, 위로 스크롤 중이면 자동 이동 대신 "새 알림 N ▼", 머리줄 "전체/점수" 필터.
- 패널 위치·크기·1인칭 위치·숨기기 탭은 기존과 같다. 색은 CasualUI 색만 쓴다.

## 변경 파일

- `docs/ui/2026-11-23-chat-notices/`: `index.html`(시안·구현 규칙 표), `on-trip.png`, `vacant.png`, `catalog.png`, `source-drive-1920.png`(Git 제외 `artifacts/father-story-player/.../driver-notebook-1920-third.png`에서 복사), `README.md`
- 이 기록, `LATEST.md`

## 검증

- 헤드리스 Chrome으로 HTML을 1920×1080에 렌더링해 PNG 3장을 만들고 눈으로 확인했다. 패널이 기존 알림 영역 안에 들어가고 승객 카드·라디오 자막과 겹치지 않는다.
- 게임 코드·빌드 변경 없음. 실행본 적용, 수동 주행 가독성은 미검증.

## 재현

`docs/ui/2026-11-23-chat-notices/index.html`을 브라우저로 연다. PNG를 다시 만들 때는 배경 그림을 data URI로 넣은 사본을 만들어 `chrome --headless=new --window-size=1920,1080 --screenshot=...`으로 각 구역(`#shot-on-trip`, `#shot-vacant`, `#shot-catalog`)만 보이게 렌더링한다.

## 남은 항목

- 시안 승인 후 구현: `FeedItem{kind,title,body,delta,time,trip}` 도입, 종류별 그리기, 버튼을 카드 안으로 이동, 스크롤 고정 규칙. `BrightUIProbe`의 알림·안내 비겹침 검사 유지.
