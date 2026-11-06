# 재출발 시놉시스와 생활 성장 기획

## 요구와 합의

사용자는 실직 후 다시 일어서는 주인공을 선택했고, 원하는 택시 구매/꾸미기·더 좋은 집 이사 목표가 이어지는 구체 시놉시스와 생활 콘텐츠 기획을 요청했다. 반복해서 만나는 인물은 앞서 제외했다. 이번은 기획 재개이며 코드 구현 지시가 아니다.

## 실제 작성

- `10-restarting-driver-synopsis.md`: 준비를 마친 첫 영업·작은 방/기본 택시·첫 개인 선택·내 방식의 영업·차/집 선택·첫 큰 성취를 연결했다.3일은 도입부이고 차/집을3일 안에 모두 마련하게 압축하지 않는다.
- `11-personal-life-and-growth.md`: 귀가 공간·안정된 판매 목록·찜·5종×2스타일 첫 자산 제안·고정 슬롯/작은 조작·개인 거래·차량교체/이사·장기 경력의 단계와 미구현 경계를 정의했다. 새 생활수치/의무 집안일·고정 인물·강제 기간 과제는 제외했다.
- 기존09친구/사진 초안과 미커밋 이력은 폐기하지 않고 이전 안 표시와 새 기준 링크를 추가했다. 이전 중단 시점의 문서를 현재 채택안으로 오해하지 않도록 README/개요/scope/checklist/LATEST도 연결했다.

## 변경 파일

`docs/content-design/10-restarting-driver-synopsis.md`, `11-personal-life-and-growth.md`, `09-work-and-personal-life-proposal.md`, `00-game-overview.md`, `README.md`; `docs/prototype-scope.md`, `docs/prototype-development-checklist.md`; `docs/work-history/2026-11-06-restarting-life-synopsis.md`, `2026-11-06-personal-life-design.md`, `LATEST.md`.

## 대조와 검증 범위

- 시작 HEAD `d42e82d`; `git status --short`에서 이전 이 작업의 README 수정·09초안/이력 미추적과 기존 사용자 미추적 자료를 확인했다. 관련 이전 초안만 포함하고 다른 미추적 자료는 보존한다.
- LATEST·scope·체크리스트·content-design00/03/04/05/09와 `BusinessProgress.cs`, `CampaignAccount.cs`를 대조했다. 현재 현금/미납과 영업 순수익을 그대로 개인 소비에 쓰면 평가가 왜곡될 수 있어 개인 거래 분류를 신규 요구로 남겼다.
- 브레인스토밍 스킬의 별도 설계 검토자에게 현재3일/장기 저장·회계·자산 범위·반복 인물 제외·의무화 위험을 검토 요청했다. 결과/최종 문서 검사 기록은 아래에 추가한다.
- 검토자 지적2건 반영:3일차 마지막 쇼핑→회차 고정/읽기 전용 전환 시점과 실패/재개를 정의했다. 물건은 종류별1회 구매·2스타일 무료전환·중복불가·미리보기·첫 범위 판매/환불 제외로 확정했다. 큰 차/집의 매력은 작은 장식과 별도 시각/사용 수용으로 검증하도록 추가했다.
- 이번 새 Unity 빌드·모델/Player·위치 제어·자동 물리 주행·수동/전체 캠페인·다른PC 검사는 없다. 필수 입력/ZIP·배포·사용자 프로필/저장은 변경하지 않았다. Strict 정상은 기존 이력 인용이며 이번 재검사 아님.
- 현재/보존 기획과 두 이력의 로컬 링크26개 존재 확인, 검사 exit0. `git diff --check` exit0. 문서 밖 동시 관리창/테스트 수정이 발생했으며 이번 선택 커밋에서 제외한다.

## 다음 작업과 실행

기획을 검토한 뒤 첫 범위 자산/가격 시험값·개인 거래/저장·완료 공간 재열람 계약을 확정하고 구현 계획으로 연결한다. 차/이사는 연속 경력 저장·구3일 이관·교환 견적이 선행하는 장기 범위다. 현재 실행은 `builds/EungyeTaxi/EungyeTaxi.exe` / 은계 싱글·A2 PVP·LANv9, 빌드 `PrototypeBuild.BuildHybrid` 그대로다.

커밋 후 `git log -1 --oneline -- docs/work-history/2026-11-06-restarting-life-synopsis.md`로 실제 이력 커밋을 확인하며 원격 게시하지 않는다.
