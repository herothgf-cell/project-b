# 택시 시뮬레이션 콘텐츠 기획

은계 싱글에서 기사는 아버지에게서 물려받은 택시로 생계를 잇는다. 어떤 호출을 받을지, 손님을 어떻게 모실지, 남은 시간과 돈을 어디에 쓸지가 하루의 일이다. 자체 도시 A2에서는 같은 운전·승객 규칙을 바탕으로 네 기사가 영업 순수익을 겨룬다. [게임 개요](00-game-overview.md) · [아버지의 택시](33-backstory-presentation.md) · [PvP 기획](pvp/README.md)

## 먼저 볼 문서

| 찾는 내용 | 읽을 곳 |
|---|---|
| 첫 운임을 받고 나면, 다음엔 무엇을 위해 달릴까? | **[37 은계 기사 생활과 영업 목표](37-driver-life-and-business.md)**. 모은 돈으로 특별 운행을 열고 직접 운전하는 목표를 제안한다. 첫 출차, 차량 판매점, 정비, 장부, 기사 성장은 그 목표를 준비하는 수단이다. |
| 은계에서 지금 어디로 가야 하는지 | **[은계 시설 위치 안내](eungye-facility-locations.md)**. 현재 지도에서 찾는 곳과 이용 기능 |
| 실제 실행본에 들어간 기능과 남은 검증 | [최근 배포·검증 기록](../work-history/LATEST.md) → [개발 체크리스트](../prototype-development-checklist.md). 날짜가 있는 기획과 시안은 그 시점의 기록으로 읽는다. |
| 아이디어 전체의 관계 | [마인드맵 목차](택시%20시뮬레이션-목차.md) · [원본 마인드맵](택시%20시뮬레이션.mmst) · [마인드맵 관리 기준](06-mindmap-review.md) |

## 현재 은계 싱글과 새 제안

**2026-11-20 기준.** 통합 실행 파일 `builds/EungyeTaxi/EungyeTaxi.exe`의 은계 싱글을 기준으로 읽는다. 3일은 도입부이며 같은 경력으로 4일차부터 영업을 이어갈 수 있다. 가격과 시간의 장기 체감, 초보자가 첫날 행동을 알아내는지는 사람 플레이로 확인할 항목이다. [계속 영업](28-continuous-career-mode.md) · [검증 이력](../work-history/LATEST.md)

| 플레이어의 질문 | 현재 제공 | 기획 37에서 제안하는 변화 |
|---|---|---|
| 처음 무엇을 하면 될까? | 새 경력 또는 불러오기. 첫날은 준비 화면에서 숙소 벽 메모를 고르고 준비로 돌아와 영업을 시작한다. 인트로 뒤 덕성프라자에서 가까운 첫 호출을 확인한다. | 첫 출발부터 첫 운임까지의 동선을 줄이고, 나머지 선택은 그 의미가 생길 때 보여 준다. |
| 차를 바꾸려면 어디로 갈까? | 숙소 차고에서 차량 견적·구매·외형을 다룬다. 공업사에서는 부품을 구입·장착한다. | 지도에서 판매점을 찾아 차량을 비교·구매한다. 숙소 차고에서는 보유한 차를 관리한다. |
| 운행을 더 할까, 지금 들를까? | 사고 처리·수리·견인, 주유·세차, 식사·커피·휴식, 숙소 퇴근과 월세가 있다. | 오일 교환 등 예정된 정비의 비용·시간을 보고 다음 호출과 비교한다. |
| 오늘 얼마나 남았고, 나는 무엇에 익숙해졌을까? | 운송 영수증·하루 정산, 방·차 구매 기록, 수첩의 이야기·동네 숙련도와 운전 평가가 있다. | 소지금 변화를 거래별로 찾아보는 장부 메뉴와 운전·체력 성장의 근거를 확인한다. |
| 다음 영업에서 어떤 운전을 해 보고 싶을까? | 숙소 벽 메모로 장기 목표를 고르고, 만난 손님의 이야기와 학생과의 약속을 수첩에 남긴다. | 벌어 둔 돈으로 특별 운행을 열어 직접 달린다. 정산에서 다음에 도전할 운행과 준비할 항목을 확인한다. |

현재 동선을 확인한 기록은 [첫날 준비·벽 메모](../work-history/2026-11-20-first-departure-taxi.md), [싱글 저장 메뉴](../work-history/2026-11-20-single-save-menu.md), [숙소 출차와 시설 이용](../work-history/2026-11-20-plaza-feed-facility-input.md), [동네 가게·공업사 부품 판매대](../work-history/2026-11-20-neighbourhood-shops-and-taxi-parts.md)에서 볼 수 있다.

## 플레이 규칙별 담당 문서

수치와 조건을 바꿀 때는 해당 기능의 담당 문서를 기준으로 삼는다. 표의 세부 색인(24~27)은 마인드맵에서 옮겨 온 개별 아이디어를 찾는 용도다.

| 영역 | 담당 기획 | 함께 볼 자료 |
|---|---|---|
| 주행·지도·교통법규 | [01 조작·교통·운전 점수](01-driving-and-traffic.md), [20 신호·승객 반응](20-roadview-signals-passenger-feedback.md), [21 차로별 진행 안내](21-lane-movement-roadview-followup.md) | [주행·법규 세부 색인 25](25-driving-rule-detail-index.md) · [은계 도로 조사](road-survey/2026-11-16-coverage.md) |
| 승객·운송·운임 | [02 승하차·운임·만족도](02-passengers-and-fares.md), [07 대화·응답](07-passenger-dialogue.md), [19 행동·요청 위반](19-passenger-trait-expansion.md) | [승객 세부 색인 24](24-passenger-detail-index.md) · [요청 판정표](../content-tuning-request-outcomes.md) |
| 승객 제작·밸런스 | [15 프리셋·특성 조합](15-passenger-presets-and-traits.md), [16 승객별 덮어쓰기](16-passenger-preset-overrides.md), [17 첫 다양화 구성](17-passenger-roster-v1.md) | [11월 13일 구성 대조표](2026-11-13-passenger-current-inventory.md) · [효과 밸런스](../passenger-balance-v1.md) · [출하 기본 프로필](../../config/content/default-profile.json) |
| 시설·차량·영업 경제 | [03 생활·차량·회계](03-life-and-economy.md), [08 사고·공업사](08-accident-and-workshop-proposal.md), [22 차종별 특성·차고·PvP 외형](22-growth-direction-and-vehicle-purchase.md), [36 김밥·카페·공업사 부품](36-neighbourhood-shops-and-taxi-parts.md) | [은계 시설 위치](eungye-facility-locations.md) · [모드·생활·화면 세부 색인 26](26-system-detail-index.md) |
| 하루와 경력 | [04 영업 마감·3일 결과](04-campaign-and-feedback.md), [28 계속 영업·목표](28-continuous-career-mode.md), [29 하루 조건 카드](29-daily-conditions.md) | [18 빗길 운전·차량 상태](18-rain-prototype.md) · [31 시간대 조명·은계 FM](31-day-night-and-radio.md) |
| 개인 공간·이야기 | [13 작은 방·첫 구매](13-three-day-room-and-first-purchase.md), [33 아버지의 택시](33-backstory-presentation.md), [34 덕성프라자 숙소](34-home-base.md), [30 손님 이야기·기사 수첩](30-passenger-stories-and-logbook.md), [35 학생과의 재회](35-student-reunion.md) | [학생 재회 실행본 기록](../work-history/2026-11-20-student-reunion-player.md). 35의 재회는 구현됐고, 마지막 한 콜의 복귀 거리 비교는 후속안이다. |
| 운전 화면·조작 | [05 HUD·지도·길안내](05-interface-and-navigation.md), [32 차내 장치·면허 경로 결정](32-deferred-systems-review.md) | [화면 방향](../ui/2026-11-06-full-ui-direction.md) · [시설 화면과 시안](../ui/2026-11-20-facilities/README.md) · [콘텐츠 설정 관리](../content-tuning-tool-guide.md) |
| 자체 도시 PvP | [PvP 기획 목록](pvp/README.md), [A2 도시](pvp/26-city-a2-four-player.md), [LAN·봇 경기](pvp/30-native-lan.md) | 경기 시간·공유 호출·순수익 순위는 PvP 문서에서 정한다. 기획 37의 생활·경영 제안은 은계 싱글 대상이다. |

<a id="2026-11-19-시작지속-동기-보강"></a>

## 이전 아이디어에서 이어지는 것

| 찾아보려는 결정 | 자료와 현재 위치 |
|---|---|
| 실직한 기사가 무엇을 위해 사는가 | [09 일과 생활 초안](09-work-and-personal-life-proposal.md) · [10 재출발 시놉시스](10-restarting-driver-synopsis.md) · [11 생활·소유 원칙](11-personal-life-and-growth.md) · [12 단계별 성장](12-career-growth-stages.md) · [14 생활 기획 안내](14-personal-life-design-guide.md). 현재 출발 배경은 33, 3일 이후 경력은 28, 다음 생활·경영 선택의 제안은 37에서 이어진다. |
| 성장·면허·관계 확장 가운데 무엇을 채택했는가 | [22 차량 성장](22-growth-direction-and-vehicle-purchase.md)의 차고와 외형은 구현됐다. [23 면허 경로](23-license-career-path.md)는 32에서 제외를 결정했다. 학생 한 명의 약속은 35에서 다룬다. 일반 단골·예약, 주거 이전 등 넓은 구상은 [성장·후속 세부 색인 27](27-future-detail-index.md)에서 찾는다. |
| 왜 하루의 변화와 수첩을 넣었는가 | [시작·지속 동기 진단](2026-11-19-player-motivation-audit.md) → [구체안](2026-11-19-motivation-concrete-proposals.md) → 기획 28~35. 제작 당시 작업 단위는 [P1~P7 프롬프트](codex-prompts-motivation.md)에 남아 있다. |

## 제작·배포·보관 자료

- 개발 범위는 [프로토 범위](../prototype-scope.md), 실제 구현 및 검증 방식은 [체크리스트](../prototype-development-checklist.md)와 [작업 이력](../work-history/LATEST.md)을 따른다. [작업 재개와 필수 지도 입력](../work-history/README.md)은 다른 PC에서 개발을 이어갈 때 사용한다.
- UI 관련 자료는 [UI 폴더](../ui/), 발표용 제안서와 프로토 매뉴얼은 [제안서 목록](../proposals/README.md), 시스템 산식과 제작 도구는 [기술 참고](../technical-reference/README.md)에 있다. 사용자 프로필과 진행 중인 저장은 출하 기본값과 다를 수 있다.
- [후속 아이디어](../future-design/README.md), [이전 기획](../design-history/content-design/README.md), [정리 전 마인드맵](../design-history/content-design/2026-11-13-mindmap-before-simplify.mmst)은 당시 결정의 배경을 확인할 때 사용한다. 시설 비용·신호·점수는 게임 안에서 사용하는 값이다.
