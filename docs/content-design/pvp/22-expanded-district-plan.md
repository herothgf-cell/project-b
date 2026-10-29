# PVP 확장 시험장·은계형 화면 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기존 PVP 자체 시험장을 확장하고 은계형3D/UI·승객 거점·서비스 시설을 한 번에 구현한 뒤 통합 검증한다.
**Spec:** 사용자 확인: 은계 실제 지도가 아닌 현재 PVP 시험장 확장. 화면/UI 차용→맵·법규 데이터화→확장/승객 거점→서비스 시설을 일괄 구현하고 마지막에 테스트한다. 이 지시를 우선해 단계별 red/green 실행은 생략하고 최종 통합 검사에 모은다.
**Architecture:** 캠페인 HUD의 색/크기/한글/아이콘·차량/도시 표현을 차용하되 PVP snapshot/명령만 연결한다. 기존 작은 회로는 회귀용으로 유지하고 제품 진입은 확장 맵으로 전환한다. 신호/정지선/우회전/제한속도/NPC 경로/보행/시설을 검증·해시 대상 지도 데이터로 정의한다.
**Tech Stack:** Unity2022.3.62f2, 기존 평면 모터·권한50Hz/NGO·로컬 봇.

## 구현 판단과 후보 수치

- 기존 중앙 회로를 유지하고 외곽 약480×400m 순환로1.76km·연결로·바깥 승하차 bay를 추가한다. 승객 거점8, 짧은 도심 운송과 고액 외곽 운송을 공통 공급에 넣는다.
- NPC 차량6/보행4, 정지선/주기/우회전 연결을 지도 데이터로 정의한다. 기존480m 회로의 좌표 전제는 legacy 회귀 어댑터에만 남긴다.
- 서비스는 동쪽 차고지 주유/서쪽 주유소2곳부터 실제 기능을 제공한다. 연료100/5km 후보, 정차 주유5초/2점·연료 고갈 시 제자리 연료지원8초/6점 후보. 경기 시계는 계속 흐르며 완료 시1회 차감, 중단·이탈·마감 시 미완료 청구 없음. 봇도 같은 주유 명령/위치/대기/비용을 적용한다.
- PVP 충돌 내구도·수리는 이번에 새 규칙을 임의 연결하지 않는다. 주유 서비스를 수리 완료로 표시하지 않는다. 캠페인 날짜/생활/장기 잔액·저장과 분리한다.
- 이 수치와 길이는 구현 판단의 시험 후보이며 경기 시간·배점·SDK/충돌 정책의 최종 확정으로 취급하지 않는다.

## 파일 분리

| 구간 | 책임 |
|---|---|
| MapDefinition/MapContent/MapRuntime | 확장 그래프·교통/시설/구역 데이터·복제/검증/해시 |
| NpcAuthority/RuleAuthority/TrafficAuthority/DrivingPilot | 좌표/도로번호 전제 제거·데이터 판정/경로·같은 서버 관측 |
| PassengerAuthority/ScoreLedger/TaxiBot/Network bridge | 다양한 공급·주유 시간/점수/연료·동일 권한·복원 |
| PvpRaceHud/PvpPracticeSession/PvpTaxiVisual | 은계형 화면·카메라·3D 차량, PVP 데이터만 사용 |
| MapLoader/PvpDistrictScenery | 도로·보도·건물·구역·승객/서비스 거점 표시 |
| Expanded editor/Player runner | 모든 구간 연결 후 회귀·실제 입력 운송/주유/동기화·패키지 |

## 진행

- [x] UI/카메라·3D 배경·지도/권한 로직 구현을 합친다. 구현 중에는 Unity/Player 테스트를 실행하지 않는다.
- [ ] 통합 후 Editor/실제 Player/서비스/기존 회귀를 실행하고 발견 문제를 수정·재검증한다.
- [ ] 소스 확정 재빌드·복사본/manifest/Git index·싱글 보존·이력/사용법/로드맵·커밋/push.

검증은 모델/위치 제어/실제 모터 입력/사람·다른PC를 구분한다. 최종 실제 화면 확인도 PC 입력을 방해하지 않는 범위에서만 하고, 하지 못하면 미검증으로 명시한다.
