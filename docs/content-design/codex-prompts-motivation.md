# Codex 구현 프롬프트 — 동기 보강 (기획 28~34)

2026-11-19 작성. 아래 프롬프트를 **순서대로 하나씩** Codex에 붙여 넣는다. 각 단계가 커밋·푸시된 뒤 다음 단계로 간다.

| 단계 | 기획 | 내용 | 의존 |
|---|---|---|---|
| P1 | [34](34-home-base.md) | 숙소 거점·숙소 퇴근·월세 | — |
| P2 | [28](28-continuous-career-mode.md) | 계속 영업 모드·목표 카드 | P1 |
| P3 | [32 §3](32-deferred-systems-review.md) | 차내 조작 3종(미터기·빈차등·에어컨) | — |
| P4 | [33](33-backstory-presentation.md) | 아버지 설정·3컷 인트로·수첩 첫 탭·벽 메모 | P1·P2·P3 |
| P5 | [29](29-daily-conditions.md) | 하루 조건 카드 | P2 |
| P6 | [30](30-passenger-stories-and-logbook.md) | 손님 이야기 10편·기사 수첩 | P4 |
| P7 | [31](31-day-night-and-radio.md) | 낮밤 조명·라디오 자막 | P5 |

시안 이미지: `docs/content-design/images/motivation-*.png` (재생성 `python -I tools/render_motivation_mockups.py`). 시안은 배치용 와이어프레임이며 최종 아트가 아니다.

---

## 공통 규칙 (모든 프롬프트에 포함됨)

```text
[공통 규칙]
- 먼저 AGENTS.md, docs/work-history/LATEST.md, docs/CURRENT-STATE.md, 해당 기획서와 시안 이미지를 읽는다.
- git status --short로 기존 미커밋 변경을 확인하고 섞지 않는다. 이번 작업 파일만 스테이징한다.
- 지도 필수 입력(config/maps/project/eungye.project.json 등)은 기획서가 요구하지 않으면 수정하지 않는다. 수정했다면 local-input-manifest 해시를 실제 자료로 갱신하고 pwsh -File tools/check_workspace.ps1 -Strict로 검증한다.
- 기존 저장(r14~)의 이관·이어하기가 깨지지 않게 저장 버전을 올리고 이관 코드를 넣는다. 사용자 실제 저장은 건드리지 않고 테스트 저장으로 검증한다.
- 수치(가격·배수·확률)는 하드코딩하지 말고 ContentTuning 키로 두고, 콘텐츠 관리 툴에서 편집 가능하게 한다.
- UI는 기존 CasualUI/OneDayHUD 스타일(1200x720 기준 IMGUI)을 따른다. 한국어 문구는 기획서 표의 문구를 그대로 쓴다.
- 검증: 관련 tools/test_*.ps1와 *_player.ps1, 저장 이관 검사, Unity 배치 빌드 PrototypeBuild.BuildHybrid 종료 0. 위치 제어 검사·자동 주행·수동 플레이를 구분해 기록하고, 하지 못한 검증은 미검증으로 적는다.
- 빌드가 바뀌면 builds/EungyeTaxi/ 실행 폴더와 distribution/EungyeTaxi-build-manifest.json(크기·SHA256)을 다시 생성/검증한다.
- docs/work-history/YYYY-MM-DD-주제.md에 요구·구현·변경 파일·검증 명령과 결과·남은 항목·재현 방법을 남기고 LATEST.md 맨 위에 한 줄 추가. 기획서 상태 줄(“미구현”)과 docs/prototype-development-checklist.md를 갱신한다.
- main 브랜치에서 커밋 후 push하고, git log -1 --oneline -- <기록 파일>로 실제 커밋 해시를 기록에 확인한다.
```

---

## P1 숙소 거점

```text
기획 docs/content-design/34-home-base.md 를 구현해줘. 시안: docs/content-design/images/motivation-home-base-map.png

범위
1. 숙소 거점: 은계 싱글에서 ServiceStops.ParkingStop(지도 오브젝트 '영업 시작·마감 주차장', serviceX 296.3 / serviceZ 506.5)을 "기사 숙소 주차장"으로 표시한다. 지도 JSON은 수정하지 말고 코드에서 표시 이름·라벨만 바꾼다(ProjectMapScene 시설 라벨, MapDestinationUI, FullMapOverlay).
2. 퇴근: BusinessProgress의 영업 종료 조건(AtEndDayHub)을 은계 싱글에서는 숙소 주차 칸만 허용한다. 동부 휴식 주차구역은 짧은 휴식 전용. 거부 문구: "숙소 주차 칸에 정차해야 퇴근할 수 있습니다. F7에서 숙소 길 안내를 받으세요." PvP 규칙은 바꾸지 않는다.
3. 지도 UI: 전체 지도·F7 생활거점 목록 첫 항목에 집 아이콘 "숙소".
4. 귀갓길 손님: 영업 종료 시각 이후 호출은 목적지가 숙소 방향(현재 위치→숙소 경로 진행 방향)인 손님을 가중(ContentTuning home.returnTrip.weight, 기본 3). 할증 없음.
5. 월세: 정산에 "숙소 월세" 일할분 한 줄(ContentTuning home.rent.daily, 기본값은 현재 하루 순수익 중앙값 R의 약 15%로 잡고 근거를 기록). 연체·퇴거 없음. 준비 화면 비용 목록(DayPreparationPresentation)에도 표시.
6. 방 HUD 제목을 "숙소"로. 3일 회차 외에도 열 수 있게 하는 것은 P2에서 하므로 이번엔 문구만.

검증
- 위치 제어 검사: 휴식 구역에서 F → 거부, 숙소에서 F → 정산 진입. 졸음 강제 종료 시 숙소 칸 배치 유지.
- 승하차 39곳/1,482경로, 저장 이관·이어하기, BuildHybrid, Strict 문제 0(지도 미수정 확인).

[공통 규칙] (codex-prompts-motivation.md의 공통 규칙 블록 전체를 따른다)
```

## P2 계속 영업 모드

```text
기획 docs/content-design/28-continuous-career-mode.md 를 구현해줘. 시안: docs/content-design/images/motivation-goal-card.png. P1(숙소 거점)이 반영된 main 기준.

범위
1. 3일 완료 정산에서 "계속하기"를 추가해 같은 저장으로 4일차~무기한 진행. 현금·차량(색/장식)·방 소품 유지. 기존 "새로 시작"은 "새 경력"으로 바꾸고 기존 저장을 덮지 않는 별도 슬롯(CampaignSaveLocation)으로.
2. BusinessProgress.ResetCampaign / RoomAvailable / finalized 잠금을 계속 영업에서 해제. 3일 체험 모드는 그대로 유지.
3. EconomyLedger: 일차 무제한 누적, 최근 3일 순수익 중앙값 계산.
4. 장기 목표 상태: car(아버지 차를 내 차로) / home(이 집을 내 집답게) / none. 목표별 대상 금액(차: 선택한 차량 가격, home: ContentTuning home.upgrade.price).
5. 준비 화면 목표 카드(목표명·대상·게이지·"오늘 평균이면 약 N일"), 정산 화면 어제→오늘 게이지와 "오늘의 기록" 3줄(최장 운송, 무위반 연속일, 기타).
6. 저장 버전 4 + 기존 3일 저장 이관(완료된 3일 저장도 "계속하기" 가능).
7. 목표 선택 UI는 P4(벽 메모)에서 만든다. 이번엔 준비 화면에서 임시 선택 버튼으로 둔다.

검증
- 3일 완료 → 계속하기 → 4·5일차 진행 후 재실행 이어하기, 소유 유지.
- 새 경력이 기존 저장을 덮지 않음. 3일 체험 저장 별도 유지.
- 기존 r14~ 저장 이관, test_personal_room*, test_vehicle_garage*, BuildHybrid.

[공통 규칙] (codex-prompts-motivation.md의 공통 규칙 블록 전체를 따른다)
```

## P3 차내 조작 3종

```text
기획 docs/content-design/32-deferred-systems-review.md §3 을 구현해줘. 시안: docs/content-design/images/motivation-cabin-controls.png

범위
1. 운전 화면 하단 패널 3개: 미터기[J], 빈차등[K](빈차/예약/휴무 순환), 에어컨[L](따뜻/끔/시원 순환). 키 충돌이 없는지 코드 전체(Input.GetKey*, Input System 액션) 확인 후 확정하고, 설정에서 재지정 가능하면 그 체계에 등록.
2. 미터기: 손님 탑승 후 켜야 운임 정산. 켜지 않고 하차하면 운임 0이 아니라 "미터기를 안 켜셨네요" 승객 반응 + 운임 감액(ContentTuning cabin.meter.missedFareRate, 기본 0.5). 하차 시 끄기 잊으면 다음 호출 수락 불가 안내.
3. 빈차등: 휴무면 새 호출 제안 없음(시설 이동·휴식용). 예약은 현재 진행 중 호출 표시.
4. 에어컨: 기존 온도 민감 승객의 선택지 응답을 실제 에어컨 상태로 판정(현재 선택지만으로 상태를 설정하는 로직을 에어컨 상태로 연결). 선택지 응답은 유지하되 "켜드릴게요" 후 실제 조작이 필요.
5. 보조: 처음 3 영업일은 자동 보조 켬(미터기 자동 켜기·끄기, 화면 안내만). 설정에서 끄기. PvP는 기본 자동.
6. 콕핏/승객 UI 겹침 없게 배치(CockpitUI, DriveHUD, PassengerDialogueUI).

검증
- 위치 제어 검사: 미터기 켜기/누락 정산, 휴무 시 호출 없음, 에어컨 판정 성공/실패.
- test_passenger_traits*, test_choice_effects*, PvP 프로필 검사(tools/check_pvp_profile.ps1), BuildHybrid. LAN 프로토콜이 바뀌면 버전 올리고 기록.

[공통 규칙] (codex-prompts-motivation.md의 공통 규칙 블록 전체를 따른다)
```

## P4 아버지 설정·인트로·수첩·벽 메모

```text
기획 docs/content-design/33-backstory-presentation.md 를 구현해줘. 시안: images/motivation-intro-storyboard.png, motivation-father-notebook.png, motivation-goal-card.png(오른쪽 벽 메모). P1~P3 반영된 main 기준.

범위
1. 설정 문구: 시작 차 이름 "중고 택시" → "아버지의 택시"(VehicleCatalog Starter), 차량 요약 문구도 갱신. 대시보드에 작은 사진 액자 오브젝트(CockpitPresentation).
2. 3컷 인트로: 새 경력/첫 3일 시작 시 1회, 컷별 8/10/8초, 건너뛰기(Esc/클릭), 메뉴 "처음 이야기 다시 보기". 이미지는 우선 시안 PNG를 플레이스홀더로 쓰고 아트 교체 지점을 분리(신규 IntroCutscene.cs). 컷3 직후 숙소 주차 칸 운전석에서 빈차등 켜기 → 첫 손님 미터기 안내(P3 조작과 연결).
3. 아버지 수첩: 첫 운송 후 "글러브박스에서 수첩을 발견했다" 알림. 5페이지 해금 조건은 기획 표대로(동네 ★, 금요일 카드, 무위반 하루, 목표 50%). 아직 없는 조건(동네 ★·금요일 카드)은 P5·P6에서 연결되도록 조건 인터페이스만 두고 잠금 유지. 수첩 UI는 신규 LogbookHUD.cs(탭: 아버지만 이번에 구현, 나머지 탭은 P6).
4. 왜 — 승객 질문 3종×선택 3개: 수다 유형 승객이 하루 최대 1회. 효과 공통 S+1, 답 저장(콘텐츠 프로필 backstory.*).
5. 무엇을 위해 — 1일차 정산 후 숙소에서 벽 메모 3장 중 선택(차/집/일단 버티자), 언제든 교체. P2 임시 목표 버튼을 이것으로 대체. "일단 버티자"는 3일차에 다시 묻기.
6. 독백 1줄: 첫 정산·첫 구매·목표 50%·목표 달성(첫날 컷3과 비교 화면).

검증
- 새 경력: 인트로→빈차등→미터기→첫 운송→수첩 알림→정산(월세)→벽 메모 흐름 위치 제어 검사.
- 인트로 건너뛰기, 다시 보기, 저장 후 재실행 시 인트로 재생 안 함.
- 첫 운송까지 시간 증가 ≤ 30초 확인, BuildHybrid.

[공통 규칙] (codex-prompts-motivation.md의 공통 규칙 블록 전체를 따른다)
```

## P5 하루 조건 카드

```text
기획 docs/content-design/29-daily-conditions.md 를 구현해줘. 시안: docs/content-design/images/motivation-day-condition-card.png

범위
1. 조건 5종(평일·비·상가 행사·금요일·도로 공사) 정의를 ContentTuning day.condition.* 로. 일차·저장 시드로 결정, 4일 주기 내 중복 없음, 재시작해도 같은 날 같은 카드.
2. 수요: PassengerDemandPolicy.Weight에 조건 배수(지역·시간대). 비=전 지역 1.3, 상가=저녁 상업 출발 2, 금요일=저녁·초과 상업→주거 2.
3. 비: 기존 RainPolicy를 조건 카드로 켜기(설정 플래그 대신 당일 조건). 탑승 청결 -2 유지.
4. 승객 구성: PassengerBehavior 특성 선택 가중(비=젖은 우산, 상가=짐·음식, 금요일=음주·수면, 공사=급함).
5. 도로 공사: 기존 48번 공사 구간 연결을 당일 통제 구간으로 재사용(지도 입력 수정 없이 런타임 비활성). 지도에 통제 표시.
6. UI: 준비 화면 카드 1장 + 수요 지도 강조, 영업 중 상단 아이콘, 정산 회고 1줄.
7. 아버지 수첩 p.2(비)·p.3(금요일) 해금 조건 연결.

검증
- 카드별 수요 가중치 회귀, 공사 구간 경로(승하차 1,482경로 재계산 시 도달 불가 0), 시드 재현성, 저장 이관, BuildHybrid.

[공통 규칙] (codex-prompts-motivation.md의 공통 규칙 블록 전체를 따른다)
```

## P6 손님 이야기·기사 수첩

```text
기획 docs/content-design/30-passenger-stories-and-logbook.md 를 구현해줘. 시안: docs/content-design/images/motivation-logbook.png. 현재 승객 표: docs/content-design/2026-11-13-passenger-current-inventory.md

범위
1. 이야기 10편 데이터(콘텐츠 프로필 story.*): 기반 프리셋, 사연 1줄, 요청, 결말 3종 대사, 판정 매핑(기존 OnTime/Comfort/SafeStop/Quiet/ViaStop/짐 판정 결과 → 좋은/보통/아쉬운).
2. 배정: 하루 2~3편(PassengerJobs), 하루 조건 카드 가중(금요일=회식, 상가=이사·짐).
3. 결말 대사를 하차 시 표시하고 수첩에 기록. 막차 학생의 "요금 부족" 선택지는 신규 선택지로 추가.
4. LogbookHUD 탭 완성: 아버지(P4)·이야기(30칸)·손님(프리셋 첫 탑승)·동네(12지점 운송 횟수 → ★ 3/10/25, ★★ 대기 명당 표시, ★★★ 수요 힌트)·기록(업적 12개).
5. 아버지 수첩 p.2(동네 ★ 3곳)·p.4(무위반 하루) 해금 연결.
6. 콘텐츠 관리 툴에서 이야기 편집.

검증
- 이야기별 성공/실패 결말 자동 회귀(위치 제어 검사), 수첩 저장·이관, test_passenger_* 회귀, BuildHybrid.

[공통 규칙] (codex-prompts-motivation.md의 공통 규칙 블록 전체를 따른다)
```

## P7 낮밤 조명·라디오 자막

```text
기획 docs/content-design/31-day-night-and-radio.md 를 구현해줘. 시안: docs/content-design/images/motivation-day-night-radio.png

범위
1. 신규 DayLightController: 영업 경과(08:00~20:00 매핑, 초과는 밤)로 아침/낮/노을/밤 4단계 Directional Light·환경광 보간.
2. 노을·밤: 가로등(도로 시설 위치 발광 머티리얼+바닥 텍스처), 건물 창 발광 무작위, 전조등(근처 차량만). 비 조건이면 환경광 낮춤.
3. PvP는 설정으로 끌 수 있게, 기본은 성능 기준(호스트 frame p95 22ms) 측정 후 결정.
4. 신규 RadioTicker: 하단 자막 1줄 6초, 승객 대화 중 숨김, 종류(아침 인사·교통·동네 소식·사연·마감), 하루 조건과 연동 문구, 콘텐츠 프로필 radio.*, 설정에서 끄기.
5. 숙소 방 배경 낮/밤 2종(PersonalRoomHUD).

검증
- 밤 조건 신호·표지 Player 검사(기존 336건 계열) 통과, 성능 측정(은계 싱글·PvP 루프백 호스트 p95), BuildHybrid, 배포 manifest.

[공통 규칙] (codex-prompts-motivation.md의 공통 규칙 블록 전체를 따른다)
```
