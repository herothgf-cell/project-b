# 최신 작업 인계

기준일: 2026-10-20. **기본 실행 맵은 은계**이며 별도 연습 모드로 대체하지 않는다.

## 먼저 읽을 것

1. [이번 작업: 지도3개 전달 ZIP](2026-10-20-map-transfer-bundle.md), [새 PC 설치 안내](map-transfer-guide.md)
2. [구역 이름과 PC 인계](2026-10-20-law-zone-labels-and-handoff.md)
3. [직전 완료: 일반 운송 법규 통합 a971d9e](2026-10-20-in-service-law-zones.md)
4. [PC 이동 절차](README.md), [개발 체크리스트](../prototype-development-checklist.md), [프로토 범위](../prototype-scope.md)

## 현재 상태

- Unity2022.3.62f2, `unity/YeongtongTaxi`, 기본 실행 `builds/EungyeTaxi/EungyeTaxi.exe`. 빌드는 `PrototypeBuild.BuildHybrid`.
- 은계 전체 도로망+국도42 실제 차로+OSM 보완. 실제/임시/창작 상태를 구분한다. 승객 운송 중7개 법규 상황 구역을 내비/H가 경유한다. 법규·승객 매너·운임은 별도다.
- 캠페인3일×8분·순수익30,000원. 연료·식사·주차·보험 갱신·세금 납부 유지. 개인 예약·단골·사고·수리·보험 청구와 그래픽/HUD 시안·새 PC/실물 패드 검증은 프로토 제외다.
- 이번 작업 완료: 미니맵7개 상황 이름/선택 강조와 호출별 정확한 경유명, 작업 이력·PC 입력 검사. 빌드 성공, 1280/1600 화면 및 선택 시 도착 표지 비가림 검사 통과, 현재 입력3개 해시 일치. 누락/불일치 거부 검사도 통과. 상세 결과는 위 날짜별 기록을 따른다.

## PC 전환 시

- Git 동기화 후 PowerShell7에서 `pwsh -File tools/check_workspace.ps1 -Strict`를 실행한다.
- Git 제외 지도 입력3개는 `artifacts/handoff/Eungye-map-inputs-2026-10-20.zip`과 `.zip.sha256`을 별도 전송해 저장소 루트에 푼다. 묶음857,580바이트, 원본/ZIP/압축 해제3개 해시 일치 확인. 설치·실행법은 위 안내를 따른다. 원본부터 재수입할 때는 raw자료도 필요하다. 체크 도구는 실제 Editor 설치/다른 PC 구동을 보장하지 않는다.
- 이미 생성된 실행파일을 옮긴다면 `builds/EungyeTaxi` 폴더 전체를 옮긴다. 게임 진행 저장은 코드 동기화에 포함되지 않는다.
- 기존 사용자 미커밋 상태: `README.md` 수정, 라이선스 관련3개 문서와 `licenses/` 미추적. 이번 작업과 분리해 보존한다.

## 이어갈 때

현재 코드·기획·체크리스트를 먼저 대조한다. 별도 연습으로만 구현하지 않고 기본 은계 영업의 운송 흐름을 유지한다. 남은 개발 항목은 체크리스트의 미완료 항목을 따른다. 새 작업 종료 시 날짜별 기록을 추가하고 이 LATEST를 갱신한다. 현재 커밋은 `git log -1 --oneline`으로 확인한다.
