# 기획서 목록과 현황

**2026-11-26 기준 · 은계 싱글 r67 · 자체 도시 PvP A2 / LAN v25.** 기획 문서 전체를 한 장에서 찾고, 항목마다 구현·미결·개선 필요 상태를 본다. 규칙과 수치는 각 담당 문서가 기준이고, 이 문서는 위치와 상태만 적는다. 상태의 근거는 오른쪽 열의 작업 이력이다.

함께 보는 문서: [기획 목록 README](README.md)(담당 영역·옛 번호) · [현재 상태 한 장](../CURRENT-STATE.md) · [최신 작업 이력](../work-history/LATEST.md) · [개발 체크리스트](../prototype-development-checklist.md) · [마인드맵 목차](택시%20시뮬레이션-목차.md)

## 상태 표기

| 표기 | 뜻 |
|---|---|
| **구현·배포** | 소스와 Git 배포 실행본 `builds/EungyeTaxi/EungyeTaxi.exe` 둘 다 들어 있다 |
| **구현·소스** | 소스에만 있고 배포 실행본에는 아직 없다 |
| **진행 중** | 다른 작업이 같은 폴더에서 고치는 중이며 커밋되지 않았다 |
| **미결: 결정 대기** | 기획자(사용자)의 답이 필요하다 |
| **미결: 승인 전** | 기획은 있으나 구현 승인을 받지 않았다 |
| **미결: 설계안** | 코드 대조 설계안이나 상세 기획만 있다 |
| **보류 / 제외** | 결정으로 미루거나 범위에서 뺐다 |
| **개선 필요** | 구현됐지만 사람 플레이 미확인, 알려진 실패, 피드백 반영 대기 중 하나가 있다(비고에 적음) |

자동 검사 통과는 사람 체감 확인이 아니다. 거의 모든 구현 항목은 위치 제어 검사나 자동 물리 주행까지만 확인했고, 사람 플레이 확인 여부는 비고에 따로 적는다.

## 문서 목록

| 번호 | 문서 | 담당 영역 | 상태 |
|---|---|---|---|
| 00 | [게임 개요·배경·손님 이야기](00-game-overview.md) | 전체 경험, 아버지의 택시, 손님 이야기·기사 수첩, 학생 재회 | 구현·배포 |
| 01 | [주행·교통·운전 평가](01-driving-and-traffic.md) | 조작, 신호, 차로, 법규 판정, 운전 점수, 조향, 제한속도 | 구현·배포 · 개선 필요 |
| 02 | [승객·운송·대화·운임](02-passengers-and-fares.md) | 호출에서 하차까지, 요청·대화, 운임·만족도·팁 | 구현·배포 |
| 03 | [승객 제작·특성·밸런스](03-passenger-content.md) | 프리셋·특성 조합, 개별 덮어쓰기, 다양화 | 구현·배포 · 수치 미확정 |
| 04 | [생활·차량·시설·영업 경제](04-life-vehicle-economy.md) | 피로·포만, 차량 상태, 사고·과실·공업사, 동네 가게, 시설 위치, 물가 | 구현·배포 · 개선 필요 |
| 05 | [화면·조작·길안내](05-interface-and-navigation.md) | HUD, 지도, 길안내, 차내 장치, 면허 제외·평점 보류 | 구현·배포 |
| 06 | [하루·경력·날씨·시간대](06-day-and-career.md) | 하루 정산, 계속 영업, 하루 조건 카드, 비, 낮밤·은계 FM, 퇴근 후 드라이브 | 구현·배포 |
| 07 | [숙소·방·차량 구매](07-home-room-and-vehicles.md) | 덕성프라자 숙소, 작은 방, 차량 구매·차종 손맛·PvP 외형 | 구현·배포 · 일부 후속 |
| 08 | [운전하는 20분의 재미 보강](08-fun-improvements.md) | 1~4장 구현, 5장 다음 단계 후보 | **검토 중** · 1~4장 구현·배포, 5장 상당수 구현·배포(공차 운영·길 지식 등은 미결) |
| 09 | [세부 항목 색인](09-detail-index.md) | 마인드맵에서 옮긴 개별 아이디어 | 색인(상태는 담당 문서) |
| PvP | [자체 도시 PvP 기획](pvp/README.md) | A2 도시, 경기, 공유 승객, LAN | 구현·배포 · 다른 PC LAN 미확인 |
| 보관 | [archive/](archive/) | 대체·제외된 원안, 시점 스냅샷 | 보관 |

## 기획 항목별 현황

### A. 전체·이야기 — [00](00-game-overview.md)

| 항목 | 기획 위치 | 상태 | 근거·남은 것 |
|---|---|---|---|
| 게임 개요·기본 흐름 | [00 1장](00-game-overview.md#s00) | 구현·배포 | 기준 문서. 첫 이용·3일 경영 체감은 사람 플레이 후속 |
| 아버지의 택시·4컷 인트로·운행 수첩 | [00 2장](00-game-overview.md#s33) | 구현·배포 · 개선 필요(사람 미확인) | [P4 기록](../work-history/2026-11-19-father-story-p4.md) · [인트로 개정](../work-history/2026-11-19-intro-rewrite.md) |
| 숙소에서 목표 고르기(벽 메모) | [00 2장 7절](00-game-overview.md#s33) | 구현·배포 · 실물 터치 미확인 | [벽 메모 터치](../work-history/2026-11-20-wall-memo-touch-and-notice-position.md) |
| 완결형 손님 이야기 10편·기사 수첩 | [00 3장](00-game-overview.md#s30) | 구현·배포 · 개선 필요(사람 미확인) | [P6 기록](../work-history/2026-11-19-passenger-stories-p6.md) |
| 기사 수첩 새 소식 레드닷 | [00 3장](00-game-overview.md#s30) | 구현·배포 · 사람 체감 미확인 | [피드백 4차](../work-history/2026-11-24-play-feedback-round4.md) |
| 막차 놓친 학생 재회 | [00 4장](00-game-overview.md#s35) | 구현·배포 | [설계](../work-history/2026-11-20-student-reunion-design.md) · [실행본](../work-history/2026-11-20-student-reunion-player.md) · [시안](student-reunion-preview.html) |

### B. 운전·교통·법규

#### B1. 조작·감각 — [01 1장](01-driving-and-traffic.md#s01), [08 1장](08-fun-improvements.md#1-운전-감각-고속-조향)

| 항목 | 기획 위치 | 상태 | 근거·남은 것 |
|---|---|---|---|
| 개인 운전 설정·조향 보조 | [01 개인 운전 설정](01-driving-and-traffic.md#개인-운전-설정) | 구현·배포 | [중속 커브](../work-history/2026-11-12-curve-steering.md) · [도심 조향](../work-history/2026-11-20-city-steering-response.md) |
| 캐주얼/시뮬 조향 프리셋·화면 연출 4종 | [08 1장 조향 프리셋](08-fun-improvements.md#조향-프리셋) | 구현·배포 · 개선 필요(키보드·패드 체감 미확인, 1.2초 목표는 물리상 불가로 측정만) | [기록](../work-history/2026-11-23-steering-feel-and-build-size.md) |
| 추가 주행음·패드 진동 | [08 1장 감각 연출](08-fun-improvements.md#감각-연출) | 구현·배포 · 개선 필요(실청·실물 패드 미확인) | [기록](../work-history/2026-11-24-drive-sound-rumble.md) |
| 실내 주행음 녹음 교체(사용자 선택 CC0) | [01 차내 주행음](01-driving-and-traffic.md#차내-주행음) | 구현·배포 · 사람 귀 확인 남음 | [기록](../work-history/2026-11-24-drive-sound-recordings.md) |
| 카메라 흔들림·1인칭 눈높이·P51 신호 70초 | [01](01-driving-and-traffic.md#s01) | 구현·배포 · 사람 체감 미확인 | [피드백 1차](../work-history/2026-11-24-test-feedback-fixes.md) |
| 차마다 다른 손맛(부품 제외) | [07 3장 10-1](07-home-room-and-vehicles.md#s22) | 구현·배포 · 사람 체감 미확인 | [기록](../work-history/2026-11-24-know-road-evening-drive-vehicle-feel.md) |

#### B2. 도로·신호·교통 — [01](01-driving-and-traffic.md), [도로 조사](road-survey/)

| 항목 | 기획 위치 | 상태 | 근거·남은 것 |
|---|---|---|---|
| 신호·차로·정지선·유턴·교통섬 | [01 1장](01-driving-and-traffic.md#s01) | 구현·배포 · 직접 운전 확인 남은 교차로 있음 | [r51 P51](../work-history/2026-11-23-p51-junction-signals.md) · [r52 P51 개구부](../work-history/2026-11-23-p51-openings-and-edge-line.md) |
| 거리뷰 신호·차선, 좌회전 대기 | [01 2장](01-driving-and-traffic.md#s20) | 구현·배포 | [체크리스트 2026-11-09](../prototype-development-checklist.md) |
| 차로별 표시·동부 구간·주차장 안내 좌회전 | [01 3장](01-driving-and-traffic.md#s21) | 구현·배포 | [J7917](../work-history/2026-11-12-j7917-roadview-check.md) · [J8066](../work-history/2026-11-13-j8066-three-lanes-and-passenger-voice.md) |
| 일반 차량 좌회전·합류 | [01 은계 북행 차로 후속](01-driving-and-traffic.md#은계-북행-차로좌회전일반-교통-후속) | 구현·배포 · 혼잡 시간대 수동 주행 미확인 | [기록](../work-history/2026-11-20-npc-left-queue.md) |
| 야간 후미등·전조등(80m)·상가·주거 가로등 | [01](01-driving-and-traffic.md#s01) | 구현·배포 · 사람 체감 미확인 | [피드백 2차](../work-history/2026-11-24-test-feedback-round2.md) · [피드백 4차](../work-history/2026-11-24-play-feedback-round4.md) |
| 지도 실측 대조 검토(교차로 77곳, 판정 277건) | [01](01-driving-and-traffic.md#s01) · [도로 조사](road-survey/) | 구현·배포(지도 r54~r67, 2026-11-26 처리 151/277) · **진행 중**(남은 건 대부분 차로 수·교차로 재구성·도로망, 차로 추가·삭제 도구가 먼저) | [검토 시트](../work-history/2026-11-24-map-review-sheet.md) · [남은 일 묶음](../work-history/2026-11-26-map-review-remaining-work.md) · [폭 조정 r66·r67](../work-history/2026-11-26-map-review-cross-sections.md) |
| 구역 교통 검사 `-districtTrafficTest` | — | **개선 필요**(기존 실패. "멈춘 차량"은 11/24 2대 → 지도 검토 r65~r67에서 3대. 2대는 J7801_9629 유턴의 긴 신호 대기로 확인, [기록](../work-history/2026-11-24-traffic-stall-check.md)) | [돌발 1차 기록](../work-history/2026-11-24-road-surprises-1.md) · [r67](../work-history/2026-11-26-map-review-cross-sections.md) |

#### B3. 법규 판정·운전 점수 — [01 행동별 평가](01-driving-and-traffic.md#행동별-평가), [09 2장](09-detail-index.md#s25)

| 항목 | 기획 위치 | 상태 | 근거·남은 것 |
|---|---|---|---|
| 두 운전 평가·행동별 감점·안정 주행 회복 | [01 1장](01-driving-and-traffic.md#두-운전-평가의-구분) | 구현·배포 | [중앙선 감점](../work-history/2026-11-13-centreline-law-tuning.md) |
| 제한속도 바탕(확인 구간만: 수인로 60·보호구역 30. 대은로 50은 피드백 4차에서 끔, 보호구역 반경 40m) | [01 행동별 평가](01-driving-and-traffic.md#행동별-평가) · [08 5.6](08-fun-improvements.md#56-후보-4종-상세-기획-2026-11-24-구현-전) | 구현·배포 · 사람 체감 미확인 | [구현](../work-history/2026-11-24-eungye-speed-limits.md) · [확인 구간·+10 여유](../work-history/2026-11-24-speed-tolerance-and-facility-cards.md) · [조사](../work-history/2026-11-24-speed-limit-camera-survey.md) |
| 제한 구간 시작·끝 표지(한국 표지 224·324·427 형태, 인도 위), 지도 구간 선 | [08 5.6](08-fun-improvements.md#56-후보-4종-상세-기획-2026-11-24-구현-전) | 구현·배포 · 사람 체감 미확인 · 보호구역 전체 빨간 포장은 5차에서 없앴고, 실제 진입부 적색 포장만 지도 검토 r65에서 넣음 | [피드백 3차](../work-history/2026-11-24-play-feedback-round3.md) · [피드백 5차](../work-history/2026-11-24-play-feedback-round5.md) · [r65](../work-history/2026-11-26-map-review-school-paving.md) |
| 일반 도로 50·번길 30 추정값 | [08 5.6](08-fun-improvements.md#56-후보-4종-상세-기획-2026-11-24-구현-전) | 보류(설정 `law.speed.estimatedRoads`로만 켬) | 보호구역 밖 표지 미확인 |

### C. 손님·운송·운임

#### C1. 호출에서 하차까지 — [02 1장](02-passengers-and-fares.md#s02)

| 항목 | 기획 위치 | 상태 | 근거·남은 것 |
|---|---|---|---|
| 호출·승하차·막힌 승하차 공간·시간 난도 | [02 1장](02-passengers-and-fares.md#호출에서-하차까지) | 구현·배포 | [체크리스트](../prototype-development-checklist.md) |
| 요청 14종·하차 후 사건·만족도 | [02 1장](02-passengers-and-fares.md#현재-요청14종과-하차-후-사건1종의-행동과-결과) | 구현·배포 | [요청 판정표](../content-tuning-request-outcomes.md) |
| 승객 부탁 선택지의 대가(피로 등)·효과 칩 | [02 2장](02-passengers-and-fares.md#s07) | 구현·배포 · 사람 체감 미확인 | [피드백 4차](../work-history/2026-11-24-play-feedback-round4.md) |
| 운임과 보너스 | [02 운임과 보너스](02-passengers-and-fares.md#운임과-보너스) | 구현·배포 | [운임·물가 점검](../work-history/2026-11-20-eungye-economy-review.md) |
| 팁·연속 기록·잘한 운전 칭찬(싱글·PvP) | [02 팁](02-passengers-and-fares.md#tips-streak) · [08 2장](08-fun-improvements.md#2-운임과-보상) | 구현·배포 · 사람 체감·LAN 두 PC 미확인 | [기록](../work-history/2026-11-23-tips-streak-praise.md) |
| 살아 있는 미터기 | [08 2장](08-fun-improvements.md#2-운임과-보상) | **제외**(사용자 결정 2026-11-24. 설계안과 기본 꺼짐 스위치 `meter.enabled`=0은 남김) | [설계안](../work-history/2026-11-24-living-meter-design.md) · [결정](../work-history/2026-11-24-next-bundle-decisions.md) |
| 취객 특성과 하차, 재사용 행동 모듈 | [02 1장](02-passengers-and-fares.md#현재-구현-취객-유형의-특성과-하차) | 구현·배포 | |
| 비 오는 날 승차 | [02 1장](02-passengers-and-fares.md#2026-11-07-비-오는-날-승차) | 구현·배포 | |

#### C2. 호출판과 일감 선택 — [08 3장](08-fun-improvements.md#3-호출판과-일감-선택)

| 항목 | 기획 위치 | 상태 | 근거·남은 것 |
|---|---|---|---|
| 호출 카드 판단 재료·성격 4종 | [08 3장](08-fun-improvements.md#호출판-변주와-판단-재료) | 구현·배포 · 사람 체감 미확인 | [기록](../work-history/2026-11-23-call-board-variety.md) |
| 연속 배차(하차점 400m 안 예약) | [02 연속 배차](02-passengers-and-fares.md#2026-11-24-연속-배차-은계-싱글) | 구현·배포 · 사람 체감 미확인 | [기록](../work-history/2026-11-24-chained-dispatch.md) |
| 선택형 미션 4종(급행·초록불·커피·칼정차) | [08 3장 선택형 미션](08-fun-improvements.md#선택형-미션) | 구현·배포 · 사람 체감 미확인 | [기록](../work-history/2026-11-23-optional-missions.md) |
| 미션 "길 아는 기사" | [08 3장](08-fun-improvements.md#선택형-미션) | 구현·배포 · **결정 대기**(L 키, 팁 15%, 내비 단계 3종 확장 여부) | [기록](../work-history/2026-11-24-know-road-evening-drive-vehicle-feel.md) · [3차 체크리스트](../work-history/2026-11-24-away-run-3-checklist.md) |
| 미션 "저 차 따라가 주세요" | [08 5.5 6번](08-fun-improvements.md#55-후보-7종-설계-요약과-추천-순서-2026-11-24-구현-전) | 미결: 설계안(버스 보류, 승용차안) | [설계안](../work-history/2026-11-24-next-candidates-design.md) |

#### C3. 운전 중 빈 시간 — [08 4장](08-fun-improvements.md#4-운전-중-빈-시간)

| 항목 | 기획 위치 | 상태 | 근거·남은 것 |
|---|---|---|---|
| 정차 중 모아 보기 | [08 4장](08-fun-improvements.md#4-운전-중-빈-시간) | 구현·배포 · 사람 체감 미확인 | [기록](../work-history/2026-11-23-idle-time-last-call.md) |
| 퇴근 전 마지막 한 콜 | [06 1장](06-day-and-career.md#s04) · [00 4장](00-game-overview.md#s35) | 구현·배포 · 사람 체감 미확인 | [기록](../work-history/2026-11-23-idle-time-last-call.md) |

#### C4. 승객 제작·밸런스 — [03](03-passenger-content.md)

| 항목 | 기획 위치 | 상태 | 근거·남은 것 |
|---|---|---|---|
| 승객 프리셋·특성 조합·관리창 | [03 1장](03-passenger-content.md#s15) | 구현·배포 | [콘텐츠 설정 관리](../content-tuning-tool-guide.md) |
| 승객별 대사·확률·효과 덮어쓰기 | [03 2장](03-passenger-content.md#s16) | 구현·배포 | |
| 승객 얼굴과 이름·성별·나이대 맞춤 | [03 1장](03-passenger-content.md#s15) | 구현·배포 | [피드백 5차](../work-history/2026-11-24-play-feedback-round5.md) |
| 승객 다양화 1차 수치 | [03 3장](03-passenger-content.md#s17) | 구현·배포 · 개선 필요(1차 수치는 사람 플레이로 확정 전) | [효과 밸런스](../passenger-balance-v1.md) |
| 독립 행동·요청 위반·새 특성·추가 승객 6명 | [03 4장](03-passenger-content.md#s19) | 구현·배포 | [승객 목록](../work-history/2026-11-13-passenger-inventory.md) |
| 승객 대화·요청·선택 구조 | [02 2장](02-passengers-and-fares.md#s07) · [09 1장](09-detail-index.md#s24) | 구현·배포(구조) · 세부 아이디어는 색인 | |

### D. 생활·차량·경제

#### D1. 생활과 영업 경제 — [04 1장](04-life-vehicle-economy.md#s03)

| 항목 | 기획 위치 | 상태 | 근거·남은 것 |
|---|---|---|---|
| 피로·포만·졸음·청결·연료 | [04 1장](04-life-vehicle-economy.md#s03) | 구현·배포 | |
| 지역 수요·공차 성과·현금·미납·순수익 | [04 1장](04-life-vehicle-economy.md#지역-수요와-공차-성과) | 구현·배포 | |
| PvP 전용 피로·포만 | [04 PvP 컨디션](04-life-vehicle-economy.md#pvp-driver-condition) | 구현·배포 | |
| 운임·생활물가 점검 | [04 5장](04-life-vehicle-economy.md#seco) | 구현(점검·재현) · 개선 필요(사람 3일·20분 운행에서 남는 돈 측정 남음) | [기록](../work-history/2026-11-20-eungye-economy-review.md) |

#### D2. 사고·공업사 — [04 2장](04-life-vehicle-economy.md#s08)

| 항목 | 기획 위치 | 상태 | 근거·남은 것 |
|---|---|---|---|
| 사고 등급·처리·수리·견인·내구도 | [04 2장](04-life-vehicle-economy.md#s08) | 구현·배포 | [보존 원안](../future-design/accidents-and-repairs.md) |
| 사고 과실 구분(내 과실/쌍방/상대 과실) | [04](04-life-vehicle-economy.md#s08) · [08 5.6](08-fun-improvements.md#56-후보-4종-상세-기획-2026-11-24-구현-전) | 구현·배포 · 사람 체감 미확인 | [기록](../work-history/2026-11-24-accident-fault.md) · [확인 목록](../work-history/2026-11-24-play-check-speed-fault.md) |
| +10 여유 안 사고를 쌍방 과실·추가 감점 5 | [08 5.6](08-fun-improvements.md#56-후보-4종-상세-기획-2026-11-24-구현-전) | 구현·배포 · 사람 체감 미확인 | [기록](../work-history/2026-11-24-speed-tolerance-and-facility-cards.md) |
| 사람 사고 감경 | [상세 기획 1.1](../work-history/2026-11-24-next-candidates-detail-design.md) | **구현·배포**(킥보드 접촉만, 조건부) · 개선 필요(사람 플레이 미확인) | [기록](../work-history/2026-11-24-road-surprises-2.md) |
| 보험·현실 과실 판정 | [01 행동별 평가](01-driving-and-traffic.md#행동별-평가) | 제외 | |

#### D3. 시설·가게 — [04 3·4장](04-life-vehicle-economy.md#s36)

| 항목 | 기획 위치 | 상태 | 근거·남은 것 |
|---|---|---|---|
| 은계김밥·호수카페·공업사 부품 판매대 | [04 3장](04-life-vehicle-economy.md#s36) | 구현·배포 · 여러 날 가격·승차감 체감 미확인 | [기록](../work-history/2026-11-20-neighbourhood-shops-and-taxi-parts.md) · [건물 점포](../work-history/2026-11-23-storefronts-and-drive-audio.md) |
| 가게 전용 포켓·기사식당 점포 전면 | [04 3장](04-life-vehicle-economy.md#s36) | 구현·배포 · 사람 체감 미확인 | [피드백 2차](../work-history/2026-11-24-test-feedback-round2.md) |
| 김밥·카페 메뉴 그림 카드 | [04 3장](04-life-vehicle-economy.md#s36) | 구현·배포 · 사람 체감 미확인 | [피드백 4차](../work-history/2026-11-24-play-feedback-round4.md) |
| 시설 10곳 위치·M 지도 찾기 | [04 4장](04-life-vehicle-economy.md#sfac) | 구현·배포 | [시설 화면](../ui/2026-11-20-facilities/README.md) |
| 생활시설 비교 카드(아이콘·막대) | [04](04-life-vehicle-economy.md#s03) | 구현·배포 · 사람 체감 미확인 | [기록](../work-history/2026-11-24-speed-tolerance-and-facility-cards.md) |

#### D4. 숙소·방·차량 — [07](07-home-room-and-vehicles.md)

| 항목 | 기획 위치 | 상태 | 근거·남은 것 |
|---|---|---|---|
| 덕성프라자 숙소 거점·월세 | [07 1장](07-home-room-and-vehicles.md#s34) | 구현·배포 · **개선 필요**(`-homeBaseCheck` 단독 실행 기존 실패 원인 조사 남음) | [P1](../work-history/2026-11-19-home-base-p1.md) · [피드백 2차](../work-history/2026-11-24-test-feedback-round2.md) |
| 넓은 호실 이전 | [07 1장](07-home-room-and-vehicles.md#s34) | 미결: 후속 기획 | |
| 작은 방·조명·러그 첫 구매 | [07 2장](07-home-room-and-vehicles.md#s13) | 구현·배포 · 준비 화면 찜 카드는 미구현 제안 | [기록](../work-history/2026-11-12-traffic-audit-room-items.md) |
| 차고 차량 견적·구매·아버지 차 보관 | [07 3장](07-home-room-and-vehicles.md#s22) | 구현·배포 · 가격 밸런스 사람 미검증 | [기록](../work-history/2026-11-10-vehicle-purchase-3day.md) · [아버지 차](../work-history/2026-11-19-father-taxi-home-base.md) |
| 싱글 차량 외형의 PvP 사용 | [07 3장 6절](07-home-room-and-vehicles.md#s22) | 구현·배포 | [PvP README](pvp/README.md) |
| 기사 평점 | [07 3장](07-home-room-and-vehicles.md#s22) · [05 2장](05-interface-and-navigation.md#s32) | 보류 | |

### E. 화면·조작·길안내 — [05](05-interface-and-navigation.md), [UI 폴더](../ui/)

| 항목 | 기획 위치 | 상태 | 근거·남은 것 |
|---|---|---|---|
| 1인칭 운전석·정보 우선순위·HUD | [05 1장](05-interface-and-navigation.md#s05) | 구현·배포 | [화면 방향](../ui/2026-11-06-full-ui-direction.md) · [UI 아트](../ui/2026-11-06-full-ui-art.md) |
| 시작·이어하기·저장 슬롯 3개 | [05 시작 화면](05-interface-and-navigation.md#시작이어하기준비-화면) | 구현·배포 · 슬롯 3개 찬 상태의 덮어쓰기 흐름 미확인 | [피드백 2차](../work-history/2026-11-24-test-feedback-round2.md) |
| 지도·길안내·바닥 안내선·경로 이탈 | [05 지도와 길안내](05-interface-and-navigation.md#지도와-길안내) | 구현·배포 | [전체 경로](../work-history/2026-11-23-route-diner-cafe-hunger.md) |
| 채팅형 운행 알림 | [05 알림](05-interface-and-navigation.md#알림결과가독성) | 구현·배포 · 수동 주행 가독성 미확인 | [구현](../work-history/2026-11-23-chat-notice-implementation.md) · [시안](../ui/2026-11-23-chat-notices/README.md) |
| 텍스트 줄이기 1·2차 | [05](05-interface-and-navigation.md#s05) | 구현·배포 · 남은 것: PvP 전용 문구, 법규·만족도 알림 문구, 마감 화면 목표·기록 묶음 | [1차](../work-history/2026-11-24-text-reduction.md) · [2차](../work-history/2026-11-24-text-reduction-part2.md) |
| 버튼 키캡·Enter/Esc, 미션 시작·구토·차량 상태 알림 카드, 약속 표시 | [05](05-interface-and-navigation.md#s05) | 구현·배포 · 사람 체감 미확인 | [피드백 3차](../work-history/2026-11-24-play-feedback-round3.md) |
| 차내 장치 조작 3종(미터기·창문·온도) | [05 2장 3절](05-interface-and-navigation.md#s32) | 구현·배포 | [P3](../work-history/2026-11-19-cabin-controls-p3.md) |
| 개인택시 면허 경로 | [05 2장](05-interface-and-navigation.md#s32) | 제외(2026-11-19) | [원안 보관](archive/23-license-career-path.md) |

### F. 하루·경력·세계 — [06](06-day-and-career.md)

| 항목 | 기획 위치 | 상태 | 근거·남은 것 |
|---|---|---|---|
| 하루 결과·회고·연습·이어하기 | [06 1장](06-day-and-career.md#s04) | 구현·배포 | [첫 운송 물리 주행](../work-history/2026-11-19-first-fare-and-settlement.md) |
| 3일 뒤 계속 영업·장기 목표 | [06 2장](06-day-and-career.md#s28) | 구현·배포 · 개선 필요("3일 뒤 계속할 이유" 사람 플레이 지표 미측정) | [P2](../work-history/2026-11-19-continuous-career-p2.md) |
| 하루 조건 카드 | [06 3장](06-day-and-career.md#s29) | 구현·배포 | [P5](../work-history/2026-11-19-daily-conditions-p5.md) |
| 비 운행 | [06 4장](06-day-and-career.md#s18) | 구현·배포 · 확률 미끄러짐 보류 | |
| 낮밤 조명·은계 FM 자막 | [06 5장](06-day-and-career.md#s31) | 구현·배포 | [P7](../work-history/2026-11-19-day-night-radio-p7.md) |
| 퇴근 후 드라이브 | [06 1장](06-day-and-career.md#s04) · [08 5장](08-fun-improvements.md#5-다음-단계-후보) | 구현·배포 · **개선 필요**(드라이브 중 영업 시계·생활거점 버튼이 그대로 보이는 알려진 어색함) | [기록](../work-history/2026-11-24-know-road-evening-drive-vehicle-feel.md) · [3차 체크리스트](../work-history/2026-11-24-away-run-3-checklist.md) |

### G. 다음 단계 후보 — [08 5장](08-fun-improvements.md#5-다음-단계-후보)

구현 순서 제안은 [상세 기획](../work-history/2026-11-24-next-candidates-detail-design.md)의 "묶어서 본 구현 순서"다: 사고 과실 구분 → 제한속도 바탕 → 돌발 1차 · 유혹 · 골목 주차 A · 시외 콜 → 돌발 2차 → 단속 카메라 → 골목 주차 B.

#### G1. 도로에서 생기는 일

| 항목 | 기획 위치 | 상태 | 근거·남은 것 |
|---|---|---|---|
| 돌발 1차(문 여는 정차 차량, 끼어드는 차) | [08 5.6](08-fun-improvements.md#56-후보-4종-상세-기획-2026-11-24-구현-전) | 구현·배포 · 개선 필요(자동 발생 빈도·사람 체감 미확인, 회피 시 중앙선 기록 여부 미확인) · 2026-11-25부터 첫날부터·하루 최대 5건(종류별 2건) | [기록](../work-history/2026-11-24-road-surprises-1.md) |
| 돌발 2차(킥보드, 구급차 교차로형) | [상세 기획 1.2](../work-history/2026-11-24-next-candidates-detail-design.md) | **구현·배포** · 개선 필요(사람 플레이 미확인, 구급차 지도 표시 없음) | [기록](../work-history/2026-11-24-road-surprises-2.md) |
| 골목 주차 A(은행로144번길) | [상세 기획 2.3](../work-history/2026-11-24-next-candidates-detail-design.md) | **구현·배포** · 개선 필요(사람 플레이 미확인, 차로 유지 보조와의 충돌 미확인) | [기록](../work-history/2026-11-24-alley-parking.md) |
| 골목 주차 B(대은로104번길) | 같은 곳 | 미결: 설계안(A의 사람 플레이 결과 뒤) | |
| 수인로(국도42) 순항 속도 | [08 5.5 3번](08-fun-improvements.md#55-후보-7종-설계-요약과-추천-순서-2026-11-24-구현-전) | 미결: 설계안 | [설계안](../work-history/2026-11-24-next-candidates-design.md) |
| 과속 방지턱 | [08 5.5 1번](08-fun-improvements.md#55-후보-7종-설계-요약과-추천-순서-2026-11-24-구현-전) | **구현·배포**(현장 조사 2곳 P12·P24. 정밀도로지도 측도 3곳은 큰길 차로에 놓여 사용자 플레이 후 끔) · 개선 필요(사람 플레이 미확인, 일반 차량 감속 없음) | [기록](../work-history/2026-11-24-speed-bumps.md) |

#### G2. 법규를 고민거리로

| 항목 | 기획 위치 | 상태 | 근거·남은 것 |
|---|---|---|---|
| 유혹("좀 빨리 가 주세요") | [08 5.3](08-fun-improvements.md#53-규칙-속-돌발-정해진-사건이-아닌-규칙-위의-상황-구-37-4) · [상세 기획 4.1](../work-history/2026-11-24-next-candidates-detail-design.md) | **구현·배포** · 개선 필요(사람 플레이 미확인) · 사례금 조건 결정됨(과속 기록 있으면 없음 유지) | [기록](../work-history/2026-11-24-hurry-temptation.md) · [결정](../work-history/2026-11-24-next-bundle-decisions.md) |
| 고정 단속 카메라·과태료 | [상세 기획 4.2](../work-history/2026-11-24-next-candidates-detail-design.md) | **구현·배포**(공공데이터 16지점) · 개선 필요(시흥 항목 이용허락범위 재확인, 단속 방향 미반영, 지도 아이콘 없음) | [기록](../work-history/2026-11-24-speed-cameras.md) |

#### G3. 일감과 영업 판단

| 항목 | 기획 위치 | 상태 | 근거·남은 것 |
|---|---|---|---|
| 시외 장거리 콜(A: 암전·시간 경과) | [08 5.5 2번](08-fun-improvements.md#55-후보-7종-설계-요약과-추천-순서-2026-11-24-구현-전) · [상세 기획 3](../work-history/2026-11-24-next-candidates-detail-design.md) | **구현·배포** · 개선 필요(분당 수입 표본 1개, 피로 증가 체감) | [기록](../work-history/2026-11-24-out-of-town-call.md) |
| 공차 운영(영업 동선 정산, 배회·승강장·호출) | [08 5.1](08-fun-improvements.md#51-공차-운영-손님을-내린-다음에-어디로-가나-구-37-2) | 미결: 승인 전(추천 첫 단계: 정산 "오늘의 영업 동선") | [원 기록](../work-history/2026-11-23-driver-judgment-design.md) · [합친 기록](../work-history/2026-11-24-fun-doc-merge.md) |
| 다른 택시와 승강장 | [08 5.1](08-fun-improvements.md#51-공차-운영-손님을-내린-다음에-어디로-가나-구-37-2) · [5.5 7번](08-fun-improvements.md#55-후보-7종-설계-요약과-추천-순서-2026-11-24-구현-전) | **구현·배포**(r57 실행본 `-rankCheck` 16건 통과) · 개선 필요(사람 플레이 미확인, 승강장 지도 표시·길안내 없음, NPC 택시는 지나가며 태움) · 승강장 3곳은 게임 전용 | [기록](../work-history/2026-11-25-taxi-ranks-and-npc-taxis.md) |
| 길 지식(내비 단계 3종, 아는 길 요청, 지름길 기록) | [08 5.2](08-fun-improvements.md#52-길-지식-은계-길을-아는-것이-실력-구-37-3) | 미결: 승인 전(미션 "길 아는 기사" 반응을 본 뒤) | |
| 손님 돌발(분실물, 단거리·승차 거부, 응급 손님) | [08 5.3](08-fun-improvements.md#53-규칙-속-돌발-정해진-사건이-아닌-규칙-위의-상황-구-37-4) | 분실물·응급 손님 **구현·배포**(r57 실행본 `-lostCheck` 22건 통과) · 개선 필요(사람 플레이 미확인, 파출소 관할·응급실 지정 미확인) · 단거리·승차 거부는 승인하지 않음(2026-11-25) | [기록](../work-history/2026-11-25-lost-items-and-emergency.md) |
| 호출 목적지 전면 비공개 | [08 5장 겹침 정리](08-fun-improvements.md#후보-한눈에-보기) | 보류(구현된 판단 재료와 반대 방향) | |

### H. 자체 도시 PvP — [pvp/](pvp/README.md)

| 항목 | 기획 위치 | 상태 | 근거·남은 것 |
|---|---|---|---|
| A2 도시·신호 교차로 37·뒷골목 | [26 A2](pvp/26-city-a2-four-player.md) · [27 통합 모드](pvp/27-flow-unified-mode.md) | 구현·배포 | [신호·골목](../work-history/2026-11-10-pvp-signals-alleys-request-frequency.md) |
| 경기 흐름·나1+봇3 | [25 시뮬레이션 경기](pvp/25-simulation-match.md) · [21 봇 모드](pvp/21-solo-bot-mode.md) | 구현·배포 | |
| 공유 승객·운임·순수익 순위 | [28 승객 공유](pvp/28-passenger-parity.md) | 구현·배포 · 장기 수익 균형 미확인 | |
| LAN 최대 4명(v25) | [30 LAN](pvp/30-native-lan.md) | 구현·배포 · **개선 필요**(다른 PC·사람 4인·지연/손실 수용 미확인) | [LAN 방 목록](../work-history/2026-11-12-pvp-lan-room-list.md) · [안정성](../work-history/2026-11-12-pvp-stability-performance.md) |
| A3 골목 도시 시안 | [29 A3](pvp/29-city-a3-proposal.md) | 보류(현재 지도는 A2) | |
| 전용 서버·인터넷 Relay | — | 제외(제공하지 않음) | [검토](../work-history/2026-11-11-pvp-dedicated-server-options.md) · [Relay 되돌림](../work-history/2026-11-12-pvp-external-relay-rollback.md) |
| 00~24 초기 원안·단계 기록 | [PvP README](pvp/README.md#이전-기획을-읽는-방법) | 보관(현재 규칙 아님) | |

### I. 색인·보관·제안서

| 자료 | 위치 | 상태 |
|---|---|---|
| 세부 항목 색인(승객·주행·모드·성장) | [09](09-detail-index.md) | 색인. 상태는 담당 문서를 따른다 |
| 장기 경력·이사·생활 확장 등 성장 구상 | [09 4장](09-detail-index.md#s27) | 미결: 아이디어 |
| 마인드맵과 목차 | [택시 시뮬레이션.mmst](택시%20시뮬레이션.mmst) · [목차](택시%20시뮬레이션-목차.md) | 탐색용 개요 |
| 기사 생활 경영안(구 37 특별 운행·지도 판매점 등) | [archive/37-driver-life-and-business.md](archive/37-driver-life-and-business.md) | 제외(2026-11-23) |
| 생활·성장 원안(구 09~14), 동기 진단 | [archive/](archive/) | 보관 |
| 발표용 제안서·조작 매뉴얼 | [제안서 목록](../proposals/README.md) | 최신 V10(2026-11-10). 그 뒤 구현(08 재미 보강 등)은 반영 전 |
| 이전 기획·후속 아이디어 | [design-history](../design-history/content-design/README.md) · [future-design](../future-design/README.md) | 보관 |

## 구현 현황 (08 재미 보강 진행 순서)

| 순서 | 묶음 | 상태 | 비고 |
|---|---|---|---|
| 1 | 조향 프리셋·감각 연출 (08 1장) | 구현·배포 | 키보드·패드 체감 미확인 |
| 2 | 팁·연속 기록·칭찬 (08 2장) | 구현·배포 | 싱글·PvP 모두. 미터기는 설계안만 |
| 3 | 호출판 변주·빈 시간·선택형 미션 (08 3~4장) | 구현·배포 | 자리 비움 1차 [체크리스트](../work-history/2026-11-23-away-run-checklist.md) |
| 4 | 추가 주행음·패드 진동·연속 배차·미터기 설계 | 구현·배포 | 자리 비움 2차 [체크리스트](../work-history/2026-11-24-away-run-checklist.md) |
| 5 | 길 아는 기사·퇴근 후 드라이브·차종 손맛 | 구현·배포 | 자리 비움 3차 [체크리스트](../work-history/2026-11-24-away-run-3-checklist.md) |
| 6 | 사고 과실 구분 → 제한속도 바탕(확인 구간만) | 구현·배포 | [확인 목록](../work-history/2026-11-24-play-check-speed-fault.md) |
| 7 | 돌발 1차 | 구현·배포 | 2026-11-24 실행본(7396ee2b) |
| 8 | 유혹 | 구현·배포 | [기록](../work-history/2026-11-24-hurry-temptation.md) · 사람 플레이 미확인 |
| 9 | 골목 주차 A · 시외 콜 | 구현·배포 | [골목 주차](../work-history/2026-11-24-alley-parking.md) · [시외 콜](../work-history/2026-11-24-out-of-town-call.md) |
| 10 | 돌발 2차 → 단속 카메라 → 골목 주차 B | 돌발 2차·단속 카메라 구현·배포, 골목 주차 B 대기 | [돌발 2차](../work-history/2026-11-24-road-surprises-2.md) · [단속 카메라](../work-history/2026-11-24-speed-cameras.md) |
| 11 | 승강장·NPC 택시, 분실물·응급 손님 | 구현·배포(2026-11-26 r57 실행본) | 08 5.1·5.3 · [승강장](../work-history/2026-11-25-taxi-ranks-and-npc-taxis.md) · [분실물·응급](../work-history/2026-11-25-lost-items-and-emergency.md) |
| — | 공차 운영·길 지식 확장·단거리·승차 거부 | 승인 전(단거리·승차 거부는 승인하지 않음) | 08 5.1~5.4 |

## 검토 중인 제안 (결정 대기)

결정되면 해당 담당 문서와 작업 이력에 기록하고 이 표에서 뺀다. 2026-11-24에 시외 콜·단속 카메라·유혹 사례금·사람 사고 감경·구급차 양보를 결정해 뺐다([결정](../work-history/2026-11-24-next-bundle-decisions.md)).

| 항목 | 정할 것 | 근거 |
|---|---|---|
| 미션 "길 아는 기사" | L 키, 팁 15%, 내비 단계 3종으로 넓힐지 | [3차 체크리스트](../work-history/2026-11-24-away-run-3-checklist.md) |
| 37 합치기·겹침 정리 | 5.1~5.4로 합친 방향, 목적지 전면 비공개 보류에 동의하는지 | [3차 체크리스트](../work-history/2026-11-24-away-run-3-checklist.md) |

## 개선점 목록 (사람 플레이·알려진 실패)

| 날짜 | 항목 | 현재 상태 | 처리 |
|---|---|---|---|
| 2026-11-24 | 08 1~5장 구현 전체의 사람 체감 | 자동 검사만 통과. 볼 것은 자리 비움 [1차](../work-history/2026-11-23-away-run-checklist.md)·[2차](../work-history/2026-11-24-away-run-checklist.md)·[3차](../work-history/2026-11-24-away-run-3-checklist.md) 체크리스트와 [확인 목록](../work-history/2026-11-24-play-check-speed-fault.md) | 사용자 직접 플레이 대기 |
| 2026-11-24 | 20분당 완료 건수·신호 대기·공차 비중 측정 | 08 7장 1단계. 사람 녹화 플레이 기록 없음 | 대기 |
| 2026-11-26 | 지도 검토 남은 126건 | 차로 수·교차로 재구성·도로망 항목. 차로 추가·삭제 도구가 먼저 | [남은 일](../work-history/2026-11-26-map-review-remaining-work.md), 진행 중 |
| 2026-11-24 | `-districtTrafficTest` 실패 | 돌발 1차 전 실행본에서도 실패하던 기존 문제. 2026-11-26 r67 멈춤 3대 | 원인 조사 남음 |
| 2026-11-24 | `-homeBaseCheck` 단독 실행 실패 | "next shift also departs from Plaza" 기존 실패 | 원인 조사 남음 |
| 2026-11-24 | 퇴근 후 드라이브 화면 | 영업 시계·생활거점·퇴근 버튼·시설 도움말이 그대로 보임 | 화면 정리 대기 |
| 2026-11-24 | 텍스트 줄이기 남은 묶음 | PvP 전용 문구, 법규·만족도 알림 문구, 마감 화면 목표·기록 묶음 | 대기 |
| 2026-11-24 | 저장 슬롯 덮어쓰기 흐름 | 실행본에서 눌러 보지 않음 | 대기 |
| 2026-11-24 | 주행음·창문 바람·패드 진동 | 사람 귀·실물 패드로 확인하지 않음 | 대기 |
| 2026-11-24 | 직접 플레이 피드백 3~5차의 사람 체감 | 3차(3a0cf27b)·4차·5차 모두 커밋·배포됨. 자동 검사만 통과 | [3차](../work-history/2026-11-24-play-feedback-round3.md) · [4차](../work-history/2026-11-24-play-feedback-round4.md) · [5차](../work-history/2026-11-24-play-feedback-round5.md), 사람 플레이 대기 |
| 2026-11-20 | 운임·생활물가의 장기 체감 | 첫 호출판 계산만 함. 사람 3일·하루 20분 운행 측정 없음 | 대기 |
| 2026-11-12 | LAN 다른 PC·사람 4인 | 같은 PC 두 Player까지만 확인 | 대기 |
| 2026-11-06 | 승객 1차 수치 | 사람 플레이로 확정한 값이 아님 | 대기 |

## 문서 사이의 관계

| 묶음 | 문서 | 역할 |
|---|---|---|
| 틀 | 00 개요·이야기, 06 하루·경력, 07 숙소·차량 | 누가, 왜, 며칠을 일하는지 |
| 운전 | 01 주행·법규, 05 화면·길안내 | 한 번 출차해서 어떻게 달리는지 |
| 손님 | 02 운송·운임, 03 승객 제작 | 누구를 태우고 어떻게 모시는지 |
| 생계 | 04 생활·차량·경제 | 남은 시간과 돈을 어디에 쓰는지 |
| 재미 보강 | 08 | 위 묶음을 손보는 검토안. 채택된 규칙은 01·02·04·06·07로 옮긴다 |
| 경쟁 | pvp/ | 같은 운전·승객 규칙으로 순수익을 겨룬다 |
| 찾기·보관 | 09, archive/, 마인드맵, proposals/ | 아이디어 색인, 이전 결정의 배경, 발표 자료 |

## 미결 사항 결정 방식

- 08과 상세 기획의 "결정할 것"은 이 문서에서 확정하지 않는다. 구현 전에 코드 현황과 함께 선택지와 추천안을 정리하고, 기획자가 고르면 담당 문서와 작업 이력(결정 기록)에 남긴 뒤 구현한다.
- 구현 뒤 상태는 이 표의 "구현·소스 → 구현·배포 → 사람 확인"으로 옮긴다. 실행본 반영 여부는 [LATEST](../work-history/LATEST.md)의 배포 기록이 기준이다.

## 수정 기록

| 날짜 | 내용 |
|---|---|
| 2026-11-24 | 처음 작성. 00~09·PvP·보관·제안서를 카테고리별로 묶고 항목마다 상태와 근거 기록을 붙였다. 결정 대기 표와 개선점 목록을 더했다. [기록](../work-history/2026-11-24-design-doc-index.md) |
| 2026-11-26 | 구현 여부 최신화: 피드백 3차 진행 중 → 구현·배포, 승강장·분실물·응급 손님 구현 → 구현·배포(r57), 지도 실측 대조 검토(r54~r67)와 피드백 4·5차 항목 추가, 제한속도 구간 문구 정정. [기록](../work-history/2026-11-26-design-index-refresh.md) |
