# 2026-11-23 기사의 판단 기획(37)

## 요구사항

사용자 질문 “시뮬레이션 요소로 무엇을 해야 더 재미있을까”에 세 안(공차 운영, 길 지식, 규칙 속 돌발)을 제안했고, 사용자가 “세 가지 모두” 기획으로 구체화하기로 했다.

## 실제 작업

- 새 기획 [37 공차 운영·길 지식·규칙 속 돌발](../content-design/37-driver-judgment-systems.md) 작성.
  - 기존 소스 대조: `OperationMetrics`(공차 집계), `PassengerDemandPolicy`, `DailyConditions`/`DailyRoadConstruction`, `JobOffer`·주변 호출, `RequestKind.LargeRoad/ShortRoad/RouteAdvice/LostItem/RejectIllegal`, 청결 승차 거부, 기획30 동네 숙련도.
  - 단계별 구현 범위와 권장 순서(정산 동선 → 내비 단계 → 분실물 → 승강장 …), 하지 않을 것, 사람 검증 질문.
- `docs/content-design/README.md` 목록, `docs/CURRENT-STATE.md` “미구현(기획 있음)” 갱신.

## 변경 파일

- `docs/content-design/37-driver-judgment-systems.md` (신규)
- `docs/content-design/README.md`
- `docs/CURRENT-STATE.md`
- `docs/work-history/2026-11-23-driver-judgment-design.md` (이 파일)
- `docs/work-history/LATEST.md`

## 검증

- 문서 작업만. 게임 코드·빌드·지도 변경 없음. 빌드·위치 제어 검사·수동 플레이 미실행.
- 기존 시스템 존재 여부는 소스 grep으로 확인했다(동작 검증 아님).

## 남은 항목

- 사용자 구현 승인과 착수 순서 결정.
- 승강장 3곳·지구대·고정 단속 카메라의 실제 위치는 거리뷰·공공 자료 조사 전 미정(임의 배치 금지).
- 팁·신고 확률·과태료 등 수치는 모두 시험값.

## 재현

문서만 변경. 기획은 `docs/content-design/37-driver-judgment-systems.md`에서 확인한다.
