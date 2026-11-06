# 3일 작은 방과 첫 구매 콘텐츠 기획

## 요구사항

현재3일 프로토타입에서 먼저 검증할 작은 방과 첫 구매 콘텐츠를 구체화한다. 첫날부터 셋째 날의 경험과 기존 구현에 최소한으로 추가할 기능을 정리한다. 개인 생활 중심·선택적 소비·의무/반복 인물 제외를 유지하며 게임 구현 요청은 아니다.

## 실제 작성

`docs/content-design/13-three-day-room-and-first-purchase.md`에 방1개·조명/러그2종×2스타일, 고정2D 레이어, 날짜별 행동과 무구매 분기, 임시5,000/8,000원 가격과 사전 측정, 선택적 마감 진입/구매/자동 배치, 개인 장부/원자 저장·최종 기록 경계, 마지막날 소비 편향을 구분한 관찰안을 작성했다.

최초 범위는 기존5종/3컷보다 축소했고 화분·차량 소품/색·시승·차/집 구매·날짜별 수요 구성은 후속으로 남겼다. 당일/누적 순수익이 개인 구매와 재로딩 때문에 바뀌지 않도록 기존 계정/영업 장부의 수정 필요성을 명시했다. 실제 코드 변경은 없다.

변경 파일: 새13안, `docs/content-design/10-restarting-driver-synopsis.md`, `11-personal-life-and-growth.md`, `12-career-growth-stages.md`, `README.md`; `docs/prototype-scope.md`, `docs/prototype-development-checklist.md`; 이번 이력과 `docs/work-history/LATEST.md`.

## 대조와 검증

- 시작 HEAD `253cc56`. `git status --short`에서 추적 파일 변경 없음, 기존 Unity/빌드/임시 문서 미추적 항목을 확인하여 보존했다.
- LATEST·scope·체크리스트·생활11/성장12/화면05와 `CampaignAccount`, `EconomyLedger`, `BusinessProgress`, `BusinessHUD`를 대조했다. `BusinessHUD`의 실제 싱글은 `OneDayHUD` 분기이며 이름만 보고 구3일 HUD를 수정하도록 기획하지 않았다. 전체 비용 조회/복원 순수익·완료 기록 자동 추가와 최종 개인 상태 분리 필요성을 확인했다.
- PowerShell에서 `OneDay*` 경로 인수 검색이 glob 오류를 내어 `rg -g 'OneDay*.cs'` 방식으로 바꿔 정상 검색했다. 게임 검사 실패가 아니다.
- 브레인스토밍 스킬의 별도 검토자가 중요한 미해결 질문 없음으로 회신했다.2종 축소/기존안 관계, 선택 자유, 마지막날 소비 편향, 개인 지출/정산 복원·최종 생활 기록 분리를 확인했다.
- 새13안·연결된10/11/12·이력의 로컬 링크32개 존재 확인(`ROOM_DESIGN_LINKS_OK 32`), `git diff --check` exit0. 문서 검사와 실제 플레이 재미 검증을 구분한다.
- 게임 코드/사용자 저장·지도 입력/ZIP·빌드/manifest 미변경. Unity 빌드·위치 제어·자동 물리 주행·수동 플레이·전체3일·Strict/배포 검사는 수행하지 않았다. 최초 가격과 사용자 동기는 미검증이다.

## 실행·다음 작업

기획은13 문서로 읽는다. 현재 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`, 은계 싱글·A2 PVP·LANv9, 빌드는 `PrototypeBuild.BuildHybrid`다. 현재 실행 파일에는 이번 방/구매/개인 소유 기능이 없다.

다음은 기존 영업 수익 표본으로 최초 가격 검토, 기획 검토 후 개인 회계/소유·저장/방 UI 구현 계획, 초보/경험자의3일 관찰이다. 작은 구매 검증을 장기 차량/주거 욕구의 성공으로 대신하지 않는다. 커밋 후 `git log -1 --oneline -- docs/work-history/2026-11-06-three-day-room-first-purchase.md`로 이력의 실제 커밋을 확인한다.
