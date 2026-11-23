# 채팅형 운행 알림 시안

2026-11-23. 운행 알림의 줄을 출처별로 다른 모양(승객 말풍선·법규 방패 줄·연료 경고 카드·목적지 카드·상황 카드·시스템 줄·TIP 메모)으로 나눈 시안이다. 배경은 실제 1920×1080 운전 화면([원본](source-drive-1920.png))이고, 패널 위치·크기는 기존 왼쪽 운행 알림 영역(360×265)과 같다. 주황 표시는 새 요소다.

| 화면 | 그림 |
|---|---|
| 승객 탑승 중(만족도·법규 감점·연료 경고, 버튼 비활성) | [on-trip.png](on-trip.png) |
| 빈차·시설 이동 중(지난 운송 흐림·시각선·“새 알림” 버튼) | [vacant.png](vacant.png) |
| 알림 요소 7종과 공통 규칙 | [catalog.png](catalog.png) |
| 전체 시안·구현 규칙 표(HTML) | [index.html](index.html) |

구현 제안의 핵심: `DriveNotifications`의 `List<string>`을 `kind`·증감·게임 시각·운송 ID를 가진 항목으로 바꾸고, 패널 바닥 버튼(주유소 안내·점유 선택·운전 평가)을 해당 카드 안으로 옮긴다. 2026-11-23 실행본에 적용했다. 실제 화면: [외부 시점](game-chase.png) · [1인칭](game-first-person.png) · [숨김 탭 미확인 수](game-hidden.png). [구현 기록](../../work-history/2026-11-23-chat-notice-implementation.md).
