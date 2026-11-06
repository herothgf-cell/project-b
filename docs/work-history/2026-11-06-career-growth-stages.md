# 택시 구매와 이사까지의 성장 단계 기획

## 요구사항

실직 후 재출발 시놉시스를 바탕으로 원하는 택시 구매와 첫 이사까지의 구체적 성장 단계, 단계마다 달라지는 영업 경험, 단순 저축 반복 방지를 기획한다. 기존 선택인 개인 생활 중심·반복 인물 제외·차/집 구매 순서 자유를 유지한다. 게임 구현 요청은 아니다.

## 실제 작성과 변경 파일

- `docs/content-design/12-career-growth-stages.md`: 6단계 경험/전환, 집 우선 분기, 새 차/집의 실제 보상과 한계, 하루 수요3구성과 안내, 후보 시승/집 미리보기 범위, 최초12~18회·중앙값 수익 기반 가격 가설, 사람 검증/중단 기준을 작성했다.
- `docs/content-design/11-personal-life-and-growth.md`, `docs/content-design/README.md`, `docs/prototype-scope.md`, `docs/prototype-development-checklist.md`: 새 기획의 담당 범위와 미구현 상태를 연결했다.
- `docs/content-design/03-life-and-economy.md`: 현재 코드와 달랐던 초과 영업 수요 표를 모두1로 정정했다. 900~1,200초의 상업 우세와 구분하며 실제 수요 코드/경제 동작은 변경하지 않았다.
- 이 기록과 `docs/work-history/LATEST.md`를 갱신했다. 신규 코드·사용자 저장·지도·빌드·배포 자료 변경 없음.

## 대조 및 검증

- 시작 HEAD `0cbedb0`. 시작 `git status --short`에서 추적 파일 수정은 없고 기존 Unity/빌드/임시 문서 등의 미추적 자료가 있음을 확인하여 보존했다.
- LATEST·scope·개발 체크리스트·시놉시스10·생활안11·생활/수요03·캠페인04 및 `PassengerDemandPolicy.cs`, `BusinessProgress.cs`, `CampaignAccount.cs`를 대조했다. 현재 수요는 고정 단계이고 초과 영업은 균등, 현행3일과 미래 연속 경력은 다른 범위임을 확인했다.
- 브레인스토밍 스킬의 별도 설계 검토자가 구매 순서/반복 방지/현재와 신규 구분/가격 가설을 검토했으며 중요한 미해결 질문 없음으로 회신했다. 집 우선·소품 생략, 현재/신규 경계, 반복 피로 발생 시 기간 축소가 기존 시놉시스·생활 원칙과 충돌하지 않음을 확인했다.
- 새 성장안·생활안·이번 이력의 로컬 문서 링크15개 존재 확인(`DESIGN_LINKS_OK 15`), `git diff --check` exit0. 검사 시 동시 관리창/테스트 변경이 확인되어 이번 선택 커밋에서 제외한다. 문서 검증은 재미 검증을 뜻하지 않는다.
- Unity 빌드·위치 제어·자동 물리 주행·수동 영업·전체 캠페인 검사를 수행하지 않았다. 사람 수익/기간/소유 만족은 미검증이며 표의 가격/일수는 제안이다. 필수 지도 입력·ZIP·출하 manifest 미변경, 이번 Strict/manifest 재검증 없음.

## 실행·다음 작업

기획은 `docs/content-design/12-career-growth-stages.md`에서 읽는다. 현재 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`, 은계 싱글·A2 PVP·LANv9, 빌드는 `PrototypeBuild.BuildHybrid`다. 지금 실행해도 이번 성장안의 방/구매/이사/날짜별 수요/시승은 제공되지 않는다.

다음은 기획 검토, 사람의 수익 분포/차량·주거 매력 검증, 개인 거래·연속 경력·이관·수요 구성의 구현 계획이다. 실제 차량 교환 산식과 시승 분리 저장 계약은 해당 구현 전 확정한다. 커밋 뒤 `git log -1 --oneline -- docs/work-history/2026-11-06-career-growth-stages.md`로 실제 커밋을 확인한다.
