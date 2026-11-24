# 현재 상태 한 장 요약

**2026-11-24 · 은계 싱글 r52 · 자체 도시 PvP A2 / LAN v25.** 기본 실행 파일은 `builds/EungyeTaxi/EungyeTaxi.exe`다. 싱글에서는 시흥 은계에서 아버지의 택시로 생계를 시작한다. 같은 실행 파일에서 봇 3명 또는 LAN 최대 4명으로 PvP를 할 수 있다. [최신 이력](work-history/LATEST.md) · [전체 기획 목차](content-design/README.md) · [PvP 기획](content-design/pvp/README.md)

## 지금 플레이할 수 있는 것

| 영역 | 배포 동작 |
|---|---|
| 시작과 경력 | 싱글에서 새 슬롯을 만들거나 기존 저장을 불러온다. 새 경력은 준비 → 숙소 목표 메모 → 준비 → 4컷 인트로 → 덕성프라자 실내 주차 칸 출차 순이다. 정상 퇴근은 빈차로 같은 칸에 돌아와 F를 누른다. 3일 도입 뒤 같은 슬롯으로 4일차를 시작할 수 있다. [경력](content-design/06-day-and-career.md) · [숙소](content-design/07-home-room-and-vehicles.md) |
| 운전과 손님 | M 지도에서 호출을 고르고 실제 도로·신호·보행자에 맞춰 운전한다. 차내 미터기·창문·온도와 승객 요청을 다룬다. 첫 운송 후 기사 수첩을 얻으며, 막차를 놓친 학생과는 첫 만남 이틀 뒤 다시 만날 수 있다. [운송](content-design/02-passengers-and-fares.md) · [학생 재회](content-design/00-game-overview.md) |
| 생활과 차량 | 연료·포만·피로·청결·내구를 관리한다. 주유·세차·식사·휴식·사고 수리와 견인, 은계김밥·동네 카페·공업사 부품 판매대를 이용한다. 숙소에서 조명·러그를 꾸미고, 정산 뒤 차고에서 현금으로 새 택시 한 대를 살 수 있다. 아버지 차를 보관하고 다음 날 몰 차를 고른다. [동네 시설과 부품](content-design/04-life-vehicle-economy.md) · [시설 위치](content-design/04-life-vehicle-economy.md) |
| 세계와 경쟁 | 하루 조건과 수요, 비·낮밤 조명·은계 FM·차량음이 주행에 동반된다. PvP는 공유 승객을 두고 순수익을 겨룬다. 싱글에서 얻은 차량 외형을 사용하고 경기 성능은 표준 차량 기준이다. [낮밤·라디오](content-design/06-day-and-career.md) · [차량음 검증](work-history/2026-11-20-recorded-vehicle-audio.md) |

## 기획 검토와 남은 확인

2026-11-24 직접 플레이 피드백(카메라 흔들림·1인칭 높이·주행음·설정 막대·지도 호출 패널·P51 신호 70초)을 소스와 지도 r53에 반영했다. 배포 실행본은 아직 r52이며 제한속도 작업과 합쳐 한 번 재빌드해야 한다. [기록](work-history/2026-11-24-test-feedback-fixes.md)

운전 중 판단을 늘리는 공차 운영·길 지식·규칙 속 돌발(구 기획 37)은 2026-11-24에 [08 재미 보강 5장](content-design/08-fun-improvements.md)으로 합쳤고 구현 승인 전이다. 2026-11-24에 미션 "길 아는 기사", 정산 뒤 퇴근 후 드라이브, 차종별 손맛을 넣었고, 방지턱·시외 콜·도로 구간·유혹·돌발·따라가기·승강장은 [설계안](work-history/2026-11-24-next-candidates-design.md)만 있다. [확인 체크리스트](work-history/2026-11-24-away-run-3-checklist.md) 같은 날 은계 제한속도 바탕(수인로 60, 대은로 50, 어린이보호구역 30, 그 밖은 게임 추정 50/번길 30)을 넣었다. [구현·검증](work-history/2026-11-24-eungye-speed-limits.md)

기획 문서는 2026-11-23 주제별 10개(00~09)로 합쳤다. 이전 기획 37(특별 운행·지도 판매점·주기 정비·소지금 장부·기사 성장)은 개발 범위에서 제외해 [보관](content-design/archive/37-driver-life-and-business.md)했고, 다음 검토 주제는 [08 운전하는 20분의 재미 보강](content-design/08-fun-improvements.md)이다. 숙소의 실제 호실 이전과 기사 평점은 후속 항목이다. [기획 목록](content-design/README.md) · [개발 체크리스트](prototype-development-checklist.md)

지정 위치에서 시설·저장·재회를 확인한 검사와, 차고에서 첫 운송을 마치고 귀가한 자동 물리 주행 기록이 있다. 사람의 첫 이용·3일 전체 경영 체감, 실물 입력·음향, 다른 PC 간 LAN은 후속 검증이다. 도로별 관찰·화면·검사 결과는 날짜별로 보관한다. [첫 운송 물리 주행](work-history/2026-11-19-first-fare-and-settlement.md) · [r43~46 도로 조사](work-history/2026-11-17-road-transitions-and-facilities.md) · [J7841](work-history/2026-11-17-j7841-road-report.md) · [r48~49 수인로](work-history/2026-11-20-suinro-repaint-review.md) · [r50 계수로](work-history/2026-11-20-gyesu-navigation-and-signals.md) · [r51 P51 교차로](work-history/2026-11-23-p51-junction-signals.md) · [r52 P51 개구부](work-history/2026-11-23-p51-openings-and-edge-line.md) · [이전 요약](design-history/CURRENT-STATE-until-2026-11-09.md)

11월 17일 도로 판단과 원본 제보 화면도 다시 볼 수 있다. [도색 연결](work-history/2026-11-17-road-paint-continuity.md) · [은행사거리 중앙선](work-history/2026-11-17-bank-approach-centreline.md) · [회전교차로·횡단보도](work-history/2026-11-17-roundabout-and-crosswalk.md) · [차로 폭·전체 보기](work-history/2026-11-17-road-width-and-dev-overview.md) · [제보 캡처](work-history/2026-11-17-road-report-capture.md) · [표시 수정](work-history/2026-11-17-road-annotation-edit-fix.md)

## 다른 PC에서 실행·개발

- 실행은 Git으로 받은 `builds/EungyeTaxi/` 전체를 사용한다. PowerShell 7에서 `pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly`로 배포 manifest의 크기·SHA256을 확인한다.
- 개발 환경은 Unity 2022.3.62f2와 Windows Build Support, 프로젝트는 `unity/EungyeTaxi`, 빌드는 `PrototypeBuild.BuildHybrid`다. PC 이동 시 `git status --short`로 기존 변경을 확인하고 `pwsh -File tools/check_workspace.ps1 -Strict`로 필수 입력을 검증한다. 정식 지도는 `config/maps/project/eungye.project.json`이다. 재수입에 쓸 지도 ZIP은 `distribution/maps/`에서 저장소 루트에 풀고 입력 manifest와 대조한다. [PC 인계 절차](work-history/README.md) · [지도 이동 안내](work-history/map-transfer-guide.md)
- 개인 경력과 편집 지도는 게임을 종료한 뒤 사용자 계정의 `AppData/LocalLow/Taxi Prototype/은계 택시 시뮬레이터/`에서 별도로 옮긴다. 기획은 `docs/content-design/`, UI는 `docs/ui/`, 제안서는 `docs/proposals/`에서 찾는다. [작업 규칙](../AGENTS.md) · [제안서 목록](proposals/README.md)
