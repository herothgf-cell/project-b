# 기본 은계 영업의 법규 상황 통합

- 완료 커밋: `a971d9e` (main, 원격 반영 확인).
- 사용자 의도: 별도 연습 맵이 아니라 기본 은계 맵에서 승객 운송 중 법규 상황을 경험한다.
- 구현: 서로 다른 7개 교차로의 점멸·녹색 출구 정체/여유·비보호 좌회전. 호출/길가 승차 → 내비/H 경유 → 상황 관찰 → 하차·정상 요금. 법규·매너·운임 분리.
- 주요 코드: `ServiceLawZones`, `ConfusionPractice`, `PassengerJobs`, `RoadNavigation`, `DrivingLaw`, `DrivingManners`, `TrafficSystem`.
- 검증: 7조건 및 위반/보류 추가 2회 위치 제어 운송, 일반 차량 24대와 출구 정체를 포함한 1,104m 자동 WheelCollider/H 운송 약350.2초 완주·지급, 충돌·신호·급제동·법규 감점0. 독립 모델9종과 기존 연습·신호 회귀 통과.
- 한계: 수동 플레이/은계 모든 경로/3일 전체 밸런스 검사가 아니다. 모든 구역은 창작 상황이며 현행 현장 규제 확정이 아니다.
- 상세: [상황 구역 배치와 검증](../eungye-in-service-law-zones.md), [체크리스트](../prototype-development-checklist.md).
- 실행파일: `builds/EungyeTaxi/EungyeTaxi.exe`. 기본 지역 eungye, Unity2022.3.62f2. 원본·생성 지도·실행파일·로그는 Git 제외.

이 문서는 당시 완료 기록을 별도 이력 체계로 옮긴 것이다. 다음 작업은 미니맵 상황 이름과 호출별 경유명 표시다.
