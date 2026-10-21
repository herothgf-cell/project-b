# 최신 작업 인계

기준일: 2026-10-21. **기본 실행 맵은 은계**이며 별도 연습 모드로 대체하지 않는다.

## 먼저 읽을 것

1. [이번 작업: 수동 운전·HUD·전체지도·비용 제외](2026-10-21-playability-and-hud.md)
2. [실행 폴더·지도 ZIP Git 배포](2026-10-21-git-playable-distribution.md), [바로 실행 안내](../../distribution/README.md)
3. [지도3개 전달 ZIP](2026-10-20-map-transfer-bundle.md), [새 PC 개발 안내](map-transfer-guide.md)
4. [구역 이름과 PC 인계](2026-10-20-law-zone-labels-and-handoff.md)
5. [직전 완료: 일반 운송 법규 통합 a971d9e](2026-10-20-in-service-law-zones.md)
6. [PC 이동 절차](README.md), [개발 체크리스트](../prototype-development-checklist.md), [프로토 범위](../prototype-scope.md)

## 현재 상태

- Unity2022.3.62f2, `unity/YeongtongTaxi`, 기본 실행 `builds/EungyeTaxi/EungyeTaxi.exe`. 빌드는 `PrototypeBuild.BuildHybrid`.
- 은계 전체 도로망+국도42 실제 차로+OSM 보완. 실제/임시/창작 상태를 구분한다. 승객 운송 중7개 법규 상황 구역을 내비/H가 경유한다. 법규·승객 매너·운임은 별도다.
- 캠페인3일×8분·순수익30,000원. 연료·식사·주차 유지. 보험료·보험 갱신·세금은 프로토 제외(2026-10-21), 비용/출발 제한/완납 목표 조건 제거·기존 저장 이관. 개인 예약·단골·사고·수리·보험 청구와 그래픽/HUD 시안·새 PC/실물 패드 검증은 프로토 제외다.
- 이번 변경: 완만한 가속·저속42도/앞바퀴 안팎 조향, S 일반 제동과 완화된 매너 기준. 은계 임시 시작 주차장은 넓은 국도42 외측 지점으로 이동.
- G 전체지도(영업시간 정지·G/Esc닫기), M 미니맵 범위 전환, 실제 매너/요구선·오늘 법규 점수/유효 건수의 독립 게이지, 한글 버튼·목표 이름표 잘림 보완. 최종 검증 수치와 빌드는 위 최신 기록을 따른다.

## PC 전환 시

- Git 동기화 후 PowerShell7에서 `pwsh -File tools/check_workspace.ps1 -Strict`를 실행한다.
- 테스트만 할 때는 Git main을 받은 뒤 `builds/EungyeTaxi/EungyeTaxi.exe`를 바로 실행한다. Unity와 지도 압축 해제는 필요 없다. 실행 폴더 전체와 배포 manifest를 Git으로 받는다. 새 PC 하드웨어 검사는 미수행이다.
- 개발할 때는 Git의 `distribution/maps/Eungye-map-inputs-2026-10-20.zip`을 저장소 루트에 푼다. 묶음857,580바이트, 지도3개 해시 검증. 원본 재수입용 raw자료는 별도다. 체크 도구는 실제 Editor 설치를 보장하지 않는다.
- 게임 진행 저장은 코드 동기화에 포함되지 않는다. 소스 수정 뒤에는 실행파일도 새로 빌드하고 배포 manifest를 갱신해야 한다.
- 기존 사용자 미커밋 상태: `README.md` 수정, 라이선스 관련3개 문서와 `licenses/` 미추적. 이번 작업과 분리해 보존한다.

## 이어갈 때

현재 코드·기획·체크리스트를 먼저 대조한다. 별도 연습으로만 구현하지 않고 기본 은계 영업의 운송 흐름을 유지한다. 남은 개발 항목은 체크리스트의 미완료 항목을 따른다. 새 작업 종료 시 날짜별 기록을 추가하고 이 LATEST를 갱신한다. 현재 커밋은 `git log -1 --oneline`으로 확인한다.
