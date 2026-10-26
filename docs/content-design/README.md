# 택시 게임 콘텐츠 기획

여기는 **현재 콘텐츠의 요소·조건·효과·예외**를 읽는 곳이다. 날짜별 개발 경위와 이전 규칙은 [기획 이력](../design-history/content-design/README.md), 실제 검증·빌드·명령 결과는 [작업 이력](../work-history/LATEST.md)에서 관리한다.

## 읽는 순서

| 목적 | 읽을 문서 |
|---|---|
| 게임 전체 범위와 다음 개발 | [00 로드맵](00-RoadMap.md), [Proto 범위](../prototype-scope.md), [개발 체크리스트](../prototype-development-checklist.md) |
| 운전 판단 | [01 상황](01-driving-situations.md) → [02 법규·점수](02-driving-evaluation.md) → [03 기록·복습](03-history-and-mastery.md) |
| 승객 운송 | [04 운송·요청 배정·정산](04-passengers-and-reservations.md) → [11 요청별 콘텐츠](11-passenger-request-effects.md) → [15 취객·청결](15-drunk-passengers-and-cleanliness.md) |
| 영업 운영 | [05 경제·거점](05-daily-operation.md) → [10 운전자 상태](10-driver-condition.md) → [07 캠페인](07-campaign.md) |
| 월드 제작 | [06 맵·주행](06-world-map-and-driving.md) → [09 코스](09-prototype-scenarios.md) → [14 정차 후보](14-passenger-stop-variety.md) |
| 도구·표시 | [12 지도 편집](12-road-map-editor.md), [13 HUD·전체지도](13-driving-map-ui.md) |
| 후속 검토 | [08 사고·수리](08-accidents-and-repairs.md): Proto 제외 / [16 일반 보행자](16-ambient-pedestrians-proposal.md): 승인 A안 안전6슬롯 구현 |

## 중복을 막는 관리 기준

| 정보 | 한 곳에서 관리할 기준 |
|---|---|
| 요청 개수·무작위 배정·등장 시점·복수 처리 | 04. 11은 개별 요청의 대사·효과·발생 가능 조건만 정의 |
| 만족도·운임·보너스 | 04. 경제05·캠페인07은 결과를 비용·하루 흐름에 연결 |
| 법규 사건·누적 운전 점수 | 02. 운송의 M/S와 구분하고04의 승차 운임 보정에서 참조 |
| 피로·포만감·졸음 | 10. 식사/휴식의 결제·시간은05, 날짜 이월은07 |
| 취객·구토·호출 잠금·거부·세차 | 15. 04/05/13은 같은 규칙을 링크 |
| 정차 후보·대기 인구 | 14.09는 상황 코스 연결,06은 월드/내비 기반 |
| 화면 배치·입력 | 13. 개별 기획서는 필요한 정보·행동만 정의 |
| 구현 상태와 완료 체크 | 개발 체크리스트. 날짜별 변경 사항은 작업 이력 |
| 과거 설계·지도 관찰 | 기획 이력 / 지도 관찰 자료. 현재 규칙 본문에 연속으로 덧붙이지 않음 |

`구현`과 `검증 완료`를 같은 뜻으로 사용하지 않는다. 모델·위치 제어·자동 물리 주행·사람의 수동 플레이·전체 캠페인은 각각의 결과를 기록한다. `미구현 기획`과 `승인 대기`, `Proto 제외`도 구분한다.

기본 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`, 개발 빌드는 `PrototypeBuild.BuildHybrid`다. PC 설치 경로는 기획 규칙에 넣지 않는다. 사용자 마인드맵 `.mmst`와 개인 메모 폴더는 편집 가능한 원자료로 보존하고, 현재 콘텐츠 규칙의 기준은 위 Markdown 문서로 둔다.
