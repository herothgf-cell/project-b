# 택시 시뮬레이션 콘텐츠 기획

은계 싱글에서 기사는 아버지에게서 물려받은 택시로 생계를 잇는다. 어떤 호출을 받을지, 손님을 어떻게 모실지, 남은 시간과 돈을 어디에 쓸지가 하루의 일이다. 자체 도시 A2에서는 같은 운전·승객 규칙을 바탕으로 네 기사가 영업 순수익을 겨룬다. [게임 개요](00-game-overview.md) · [PvP 기획](pvp/README.md)

**2026-11-23 정리.** 46개였던 기획 문서를 주제별 10개로 합쳤다(같은 날 별도로 추가된 37 공차 운영 기획은 2026-11-24에 08의 5장으로 합쳤다). 대체됐거나 제외된 문서, 시점 스냅샷, 제작용 프롬프트는 [보관 폴더](archive/)로 옮겼다. 기획 37(특별 운행·지도 판매점·주기 정비·소지금 장부·기사 성장)은 제외했고, 다음 검토 주제는 [08 재미 보강](08-fun-improvements.md)이다.

## 문서 목록

수치와 조건을 바꿀 때는 해당 영역의 담당 문서를 기준으로 삼는다. 새 기획은 새 파일을 만들기 전에 담당 문서의 장으로 넣는다. 항목별 구현·미결·개선 필요 상태는 [기획서 목록과 현황](INDEX.md)에서 한눈에 본다(브라우저용 [HTML](index.html), 상태별 필터 포함).

**HTML로 읽기.** 이 폴더와 `pvp/`의 기획서는 같은 이름의 `.html`로도 열 수 있다(예: [00 게임 개요](00-game-overview.html), [기획서 목록](index.html)). 문서 사이 링크·목차·이전/다음 이동이 있다. 원본은 md이며, md를 고친 뒤 `python tools/render_design_docs.py`로 다시 만든다. 다른 작업의 미커밋 변경을 빼려면 `--rev HEAD`를 붙인다.

| 문서 | 담당 영역 | 함께 볼 자료 |
|---|---|---|
| [00 게임 개요·배경·손님 이야기](00-game-overview.md) | 전체 경험, 아버지의 택시, 손님 이야기·기사 수첩, 학생 재회 | [학생 재회 시안](student-reunion-preview.html) |
| [01 주행·교통·운전 평가](01-driving-and-traffic.md) | 조작, 신호, 차로, 법규 판정, 운전 점수, 조향 수치 | [은계 도로 조사](road-survey/2026-11-16-coverage.md) |
| [02 승객·운송·대화·운임](02-passengers-and-fares.md) | 호출에서 하차까지, 요청과 대화 선택, 운임과 만족도 | [요청 판정표](../content-tuning-request-outcomes.md) |
| [03 승객 제작·특성·밸런스](03-passenger-content.md) | 프리셋·특성 조합, 승객별 덮어쓰기, 다양화 구성, 새 특성 | [효과 밸런스](../passenger-balance-v1.md) · [출하 기본 프로필](../../config/content/default-profile.json) |
| [04 생활·차량·시설·영업 경제](04-life-vehicle-economy.md) | 피로·포만, 차량 상태, 사고·공업사, 동네 가게·부품, 시설 위치, 운임·물가 점검 | |
| [05 화면·조작·길안내](05-interface-and-navigation.md) | HUD, 지도, 길안내, 차내 장치, 면허 경로 제외·평점 보류 | [화면 방향](../ui/2026-11-06-full-ui-direction.md) · [시설 화면](../ui/2026-11-20-facilities/README.md) · [콘텐츠 설정 관리](../content-tuning-tool-guide.md) |
| [06 하루·경력·날씨·시간대](06-day-and-career.md) | 하루 정산, 3일 뒤 계속 영업, 하루 조건 카드, 비, 낮밤 조명·은계 FM | |
| [07 숙소·방·차량 구매](07-home-room-and-vehicles.md) | 덕성프라자 숙소, 작은 방과 첫 구매, 차종 특성·차고 구매·PvP 외형 | |
| [08 운전하는 20분의 재미 보강](08-fun-improvements.md) | **검토 중.** 고속 조향, 팁·미터기, 호출판 변주·선택형 미션, 신호 대기 활용, 다음 단계 후보(공차 운영·길 지식·돌발·승강장·단속 카메라) | [구 37 원문](archive/37-driver-judgment-systems.md) |
| [09 세부 항목 색인](09-detail-index.md) | 마인드맵에서 옮긴 개별 아이디어 찾기 | [마인드맵 목차](택시%20시뮬레이션-목차.md) · [원본 마인드맵](택시%20시뮬레이션.mmst) |

자체 도시 PvP의 경기 시간·공유 호출·순수익 순위는 [PvP 기획 목록](pvp/README.md)에서 정한다.

## 현재 은계 싱글

**2026-11-20 배포본 기준.** 통합 실행 파일 `builds/EungyeTaxi/EungyeTaxi.exe`의 은계 싱글을 기준으로 읽는다. 3일은 도입부이며 같은 경력으로 4일차부터 영업을 이어갈 수 있다. 가격과 시간의 장기 체감, 초보자가 첫날 행동을 알아내는지는 사람 플레이로 확인할 항목이다.

| 플레이어의 질문 | 현재 제공 | 담당 |
|---|---|---|
| 처음 무엇을 하면 될까? | 새 경력 또는 불러오기. 첫날은 준비 화면에서 숙소 벽 메모를 고르고 영업을 시작한다. 인트로 뒤 덕성프라자에서 가까운 첫 호출을 확인한다. | [00](00-game-overview.md) · [06](06-day-and-career.md) |
| 어떤 손님을 어떻게 모실까? | 호출판에서 수락하고, 요청과 대화에 응답하며, 만족도와 운임을 받는다. | [02](02-passengers-and-fares.md) |
| 운행을 더 할까, 지금 들를까? | 사고 처리·수리·견인, 주유·세차, 김밥·커피·식사·휴식, 숙소 퇴근과 월세가 있다. | [04](04-life-vehicle-economy.md) |
| 차와 방을 바꾸려면? | 숙소 차고에서 차량 견적·구매·외형을, 공업사에서 부품 구입·장착을, 방에서 조명·러그 구매를 한다. | [07](07-home-room-and-vehicles.md) |
| 오늘은 어제와 무엇이 다를까? | 하루 조건 카드, 비, 시간대 조명과 은계 FM, 수첩의 이야기와 학생과의 약속이 있다. | [06](06-day-and-career.md) · [00](00-game-overview.md) |
| 달리는 20분이 재미있을까? | 검토 중이다. 먼저 사람 플레이로 20분당 완료 건수, 신호 대기·공차 비중을 잰다. | [08](08-fun-improvements.md) |

실제 실행본에 들어간 기능과 남은 검증은 [최근 배포·검증 기록](../work-history/LATEST.md)과 [개발 체크리스트](../prototype-development-checklist.md)를 따른다.

## 옛 번호로 찾기

본문 속 "기획 NN"은 합치기 전 번호다. 합친 문서에는 각 장 제목에 "구 NN"을 붙였다.

| 옛 번호 | 지금 위치 |
|---|---|
| 00 · 30 · 33 · 35 | [00 게임 개요·배경·손님 이야기](00-game-overview.md) |
| 01 · 20 · 21 | [01 주행·교통·운전 평가](01-driving-and-traffic.md) |
| 02 · 07 | [02 승객·운송·대화·운임](02-passengers-and-fares.md) |
| 15 · 16 · 17 · 19 | [03 승객 제작·특성·밸런스](03-passenger-content.md) |
| 03 · 08 · 36 · 은계 시설 위치 · 11/20 운임·물가 점검 | [04 생활·차량·시설·영업 경제](04-life-vehicle-economy.md) |
| 05 · 32 | [05 화면·조작·길안내](05-interface-and-navigation.md) |
| 04 · 18 · 28 · 29 · 31 | [06 하루·경력·날씨·시간대](06-day-and-career.md) |
| 13 · 22 · 34 | [07 숙소·방·차량 구매](07-home-room-and-vehicles.md) |
| 24 · 25 · 26 · 27 | [09 세부 항목 색인](09-detail-index.md) |
| 06 마인드맵 관리 기준 | [보관](archive/06-mindmap-review.md). 현재 관리 기준은 [마인드맵 목차](택시%20시뮬레이션-목차.md)에 요약했다. |
| 09 · 10 · 11 · 12 · 14 생활·성장 원안 | [보관](archive/). 현재 배경은 00, 3일 이후 경력은 06, 숙소와 차량은 07을 따른다. |
| 23 개인택시 면허 경로 | [보관](archive/23-license-career-path.md). 2026-11-19 제외 결정. |
| 37 기사 생활과 영업 목표 | [보관](archive/37-driver-life-and-business.md). 2026-11-23 제외 결정. |
| 37 공차 운영·길 지식·규칙 속 돌발 | [08 재미 보강 5장](08-fun-improvements.md#5-다음-단계-후보). 2026-11-24에 합쳤다. 원문은 [보관](archive/37-driver-judgment-systems.md). |
| 11/13 승객 대조표 · 11/19 동기 진단·구체안 · Codex 프롬프트 | [보관](archive/). 동기 보강 결과는 06·00·07에 구현 기획으로 남아 있다. |

## 제작·배포·보관 자료

- 개발 범위는 [프로토 범위](../prototype-scope.md), 구현 및 검증 방식은 [체크리스트](../prototype-development-checklist.md)와 [작업 이력](../work-history/LATEST.md)을 따른다. [작업 재개와 필수 지도 입력](../work-history/README.md)은 다른 PC에서 개발을 이어갈 때 사용한다.
- UI 자료는 [UI 폴더](../ui/), 발표용 제안서와 프로토 매뉴얼은 [제안서 목록](../proposals/README.md), 시스템 산식과 제작 도구는 [기술 참고](../technical-reference/README.md)에 있다. 사용자 프로필과 진행 중인 저장은 출하 기본값과 다를 수 있다.
- [후속 아이디어](../future-design/README.md), [이전 기획](../design-history/content-design/README.md), [정리 전 마인드맵](../design-history/content-design/2026-11-13-mindmap-before-simplify.mmst)은 당시 결정의 배경을 확인할 때 사용한다.
