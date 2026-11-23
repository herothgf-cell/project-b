# 동기 보강 기획 28~32 작성

## 요구와 수행

- 사용자 요청: 구체안 1~5번을 각각 기획, `docs/content-design/`에 저장, 텍스트는 간결하게·요소는 표/이미지로, 푸시.
- 작성: [28 계속 영업](../content-design/06-day-and-career.md), [29 하루 조건](../content-design/06-day-and-career.md), [30 손님 이야기·수첩](../content-design/00-game-overview.md), [31 낮밤·라디오](../content-design/06-day-and-career.md), [32 보류 재검토](../content-design/05-interface-and-navigation.md). 각 문서는 목표·표·Mermaid 흐름도·구현 범위(현재 파일)·검증 기준으로 구성.
- 근거로 확인한 현재 값: 수요 3배 가중(`PassengerDemandPolicy`), 차량 하이브리드 ₩26,000·LPG ₩22,000·색 ₩1,500·장식 ₩2,000(`VehicleCatalog`), 조명 ₩5,000·러그 ₩8,000(`PersonalRoomHUD`), 비 설정 기본 꺼짐·탑승 청결 −2(`RainPolicy`), 승객 프리셋·요청(11/13 인벤토리).

## 변경 파일

- `docs/content-design/28~32-*.md`, `docs/content-design/README.md` 색인 추가, 이 기록, `docs/work-history/LATEST.md` 한 줄.
- 기존 미커밋 도로·빌드·코드 변경은 건드리지 않았다.

## 검증과 한계

- 문서 작성만. 게임 실행·빌드·Strict·플레이 검증 없음. 가격·배수·조건 값은 모두 초안이며 밸런스 검증 전.
- Mermaid 도표는 GitHub/VS Code 미리보기 기준이며 렌더링을 별도 확인하지 않았다.
- 보류 항목(기획23·차내 조작)은 32에서 선택지만 제시, 해제하지 않았다.

## 다음 작업

- 28 착수 여부 결정 → 저장 버전 4 설계. 32의 결정 사항(면허 축소안·차내 조작 범위) 사용자 답변.
