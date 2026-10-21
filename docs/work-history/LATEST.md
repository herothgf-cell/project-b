# 최신 작업 인계

기준일: 2026-10-21. 기본 실행 맵은 은계, 별도 연습으로 대체하지 않는다.

## 먼저 읽을 것

최신 표시 정정: [미니맵 아이콘·호출 요청 숨김·승객 대화](2026-10-21-compact-minimap-dialogue.md). 맵 정비 계획은 사용자 검토 중이며 미적용.

최신 작업: [자유 하차·남은 세부 기능·승객 말풍선](2026-10-21-free-dropoff-and-remaining.md), [도로·맵 정비 순서](../map-polish-plan.md).

최근 자료 확인: [은계 정밀도로지도 제공 범위](2026-10-21-hdmap-coverage-check.md) — 국도 42호선 등 일부만 겹치며 중심가 T1~T3는 확보 차로 자료 없음.

1. [전체지도 확대·48대 교통·잔여 범위](2026-10-21-map-zoom-and-traffic.md), [직접 플레이 안내](../prototype-playtest-guide.md)
2. [승객 코스·요청·운행 피드백 구현/검증](2026-10-21-passenger-scenarios-and-feedback.md)
3. [도로·주유 시설·운송 마감](2026-10-21-road-facilities-and-operation.md)
4. [개발 체크리스트](../prototype-development-checklist.md), [범위](../prototype-scope.md), [9코스 기획](../content-design/09-prototype-scenarios.md)
5. [실행 안내](../../distribution/README.md), [PC 이동](README.md), [지도 전달](map-transfer-guide.md)

## 현재 구현과 빌드

- Unity 2022.3.62f2, unity/YeongtongTaxi, PrototypeBuild.BuildHybrid. 기본 실행 builds/EungyeTaxi/EungyeTaxi.exe. 소스 커밋과 파일별 해시는 distribution/EungyeTaxi-build-manifest.json 참조.
- 은계 전체 도로망+국도42 실제 차로+OSM. 원본/임시/게임 창작 상태를 구분한다.
- 일반 영업 PS01~09 코스, PS01/02 승차 전 상황, PS08 정상/우회 선택, PS09 실제 정지·보행 대기. PS03~09 미체험도 목적지 하차 가능(평가 기회 없음). 승객 요청17종과 픽업 대기 기한. 호출 카드의 경유 상황 설명 제거.
- 운행 점수는 승객 매너와 별도 누적. 날짜/재실행 유지, 새 캠페인 100점 초기화. 준수 사건당1점, 위반 사건은 최대 감점. 회계는 하루 정산 저장, 운행 사건은 별도 저널 즉시 저장이다.
- 상단 연료15%/5% 경고, 매너/운행 변화 알림. M 전체지도(휠·+/- 확대/축소·드래그·내 위치·전체 보기), +/- 미니맵, 시설/교통/법규 필터. R/패드 복귀 단축키 제거. 창 제목 택시 시뮬레이터.
- 일반 차량48대, 신호 남은 시간, 간략 게임 표지. 실제 시설로 확정한 표지가 아니다.
- 캠페인3일×12분·순수익30,000원. 연료4%포인트/km+0.1%포인트/분·초기60%, 주유소3곳. 탑승 중 마감 금지·목적지 하차 후 정산.
- 보험/세금·개인 예약·단골·사고·수리·보험 청구·그래픽 시안·새 PC/실물 패드 검사는 프로토 제외.

## 검증과 남은 작업

모델19묶음, Unity 빌드, 요청17종133항목, 일반 차량36대/신호대기, 9코스+우회 운송, 점수 저장/초기화, 3해상도 UI 및 3일 캠페인 회계 자동 검사 통과. 자세한 실행 조건과 결과는 최신 기록 참조. 위치 제어 운송 검사는 수동 플레이가 아니다.

다음: 일반 교통 포함 수동 3일 밸런스, 표지/차로/신호 현장 대조. 은계 중심가 정밀도로지도 추가 여부는 정확한 게임 경계로 공식 자료를 조회·취득해 도형 단위로 재검사한다. 기존9코스와17요청을 미구현으로 다시 개발하지 않는다. 새 작업은 개별 기획서·체크리스트·작업 이력을 함께 갱신한다.

## PC 전환

- main을 받은 뒤 builds/EungyeTaxi/EungyeTaxi.exe 바로 실행. 개발 시 distribution/maps/Eungye-map-inputs-2026-10-20.zip을 저장소 루트에 해제하고 pwsh -File tools/check_workspace.ps1 -Strict 실행.
- 입력 지도3개와 ZIP은 변경하지 않았고 Strict 검사 Input issues: 0. 원본 raw 자료는 별도이며 진행 저장은 Git에 포함하지 않는다.
- 기존 사용자 README.md·라이선스 문서3개·licenses/ 변경은 이번 작업에 포함하지 않는다. git status --short로 확인한다.
- 최신 이력의 실제 커밋은 git log -1 --oneline -- docs/work-history/2026-10-21-passenger-scenarios-and-feedback.md로 확인한다.

## MIT 교통 선택 기능 인계 (2026-10-21)

최신 기능 기록: [MIT 앞차 추종 ON/OFF](2026-10-21-mit-traffic-following.md). 기본 은계 빌드의 Esc 메뉴에서 전환하며 최초 ON·선택 저장, OFF는 기존 AI다. 모델20묶음과 실제 차량 전환/정지/출발, 3해상도 검사 완료. 48대 비교 및 배포 해시는 전용 기록과 manifest 참조. 입력 지도 변경 없음, Strict Input issues: 0. 다음은 수동 ON/OFF 주행 체감·3일 밸런스 검사다.

동시 작업의 승객/표지 변경이 포함된 기본 빌드에서 검사했다. 이후 동시 작업이 다시 빌드하면 MIT ON/OFF 회귀와 배포 manifest를 다시 검증한다. 기존 미도입 감사 문구보다 신규 CODE-001 사용 대장·MIT 전문/NOTICE가 우선한다. 이번 이력의 실제 커밋: `git log -1 --oneline -- docs/work-history/2026-10-21-mit-traffic-following.md`.

통합 변경: 가득 주유, 호출 연료 예측, 선택 안내 최초 노출 저널 저장, 좌측 승객 말풍선, 비보호 안내 강화. MIT 교통 작업 완료 후 통합 재빌드. 검증/배포 정보는 최신 작업 기록과 manifest를 따른다.
