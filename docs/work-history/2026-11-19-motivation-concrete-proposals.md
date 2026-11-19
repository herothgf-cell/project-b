# 시작·지속 동기 보강 구체안

## 요구와 실제 수행

- 사용자 재요청: 시작/이어갈 동기, 세계관·스토리·시뮬레이션 요소 부족을 전반 점검.
- 같은 날 오전 [동기 진단](2026-11-19-player-motivation-audit.md)이 있어 반복하지 않고, 구체 콘텐츠안·착수 순서·개발 비중 점검을 [구체안](../content-design/2026-11-19-motivation-concrete-proposals.md)으로 추가했다.
- 근거 확인: 기획10·23, `BusinessProgress.cs`(새 회차 `ResetCampaign`·3일 한정 `RoomAvailable`), 비 연출 `RainPresentation.cs` 존재, 낮밤 미구현(CURRENT-STATE 기준). 2026-10-20 이후 커밋 제목 키워드 단순 집계(전체 734, 도로·지도·PvP 약 311, 승객·생활 약 142).

## 변경 파일

- `docs/content-design/2026-11-19-motivation-concrete-proposals.md`
- 이 기록, `docs/work-history/LATEST.md`의 이번 안내 한 줄만. 기존 미커밋 도로·빌드·코드 변경은 건드리지 않았다.

## 검증과 한계

- 문서 작성만. 게임 실행·빌드·Strict·수동 플레이 없음. 커밋 집계는 제목 키워드 기준의 대략치다.
- 제안은 구현 승인이 아니며 보류 항목(기획23·차내 조작·평점)은 그대로다.

## 다음 작업

- 사용자가 1순위(계속 영업 모드)·2순위(하루 조건 카드) 중 착수 대상을 결정하면 해당 기획서를 작성한다.
