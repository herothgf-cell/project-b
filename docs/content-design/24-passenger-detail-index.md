# 승객·대화·요청 세부 기획

[간단 마인드맵 목차](택시%20시뮬레이션-목차.md) · [기획 목록](README.md)

마인드맵에서 옮긴 세부 항목을 분야별로 찾는 문서다. **아래 이관 목록은 2026-11-13 정리 직전의 항목명·단계 기록이며, 최신 규칙은 담당 기획을 우선한다.** 과거 수치·승객 수·LAN 버전을 현재 값으로 다시 채택하지 않는다.

## 현재 담당 기획

- [승객·운송·요청·운임](02-passengers-and-fares.md)
- [승객 대화·요청·선택 기획](07-passenger-dialogue.md)
- [승객 프리셋·특성 조합과 제작 도구](15-passenger-presets-and-traits.md)
- [승객별 대사·확률·효과 설정](16-passenger-preset-overrides.md)
- [19. 승객 독립 행동·요청 위반·새 특성](19-passenger-trait-expansion.md)
- [현재 승객 유형·요청·선택·효과 대조표](2026-11-13-passenger-current-inventory.md)

승객 유형·개별 대사·선택·효과는 최신 현황표에서 확인한다. 기본 프리셋 30종 중 29종 활성 상태이며, 이관 목록의 24명/23활성 표기는 과거 기록이다.

## 마인드맵에서 이관한 세부 항목

50% = 당시 구현 단계, 25% = 당시 기획 완료, 0% = 당시 기획 필요/보류. 사람 플레이 검증 완료율이 아니다. 원래 메모·태그·작업 속성은 [정리 전 원본](../design-history/content-design/2026-11-13-mindmap-before-simplify.mmst)에 모두 보존했다.

- 승객&보행자 <!-- source-node:98b78122-12dd-496b-bf32-2c6ecdd736ab --> · [참조1](02-passengers-and-fares.md) · [참조2](../future-design/2026-11-07-planning-gaps-and-weather.md)
  - 승객 프리셋·특성·행동 (기존11행동·34변주 재사용) <!-- source-node:aa01a7cd-4f01-5bd4-84c5-3fde4c9fa772 --> · [참조1](02-passengers-and-fares.md) · [참조2](15-passenger-presets-and-traits.md) · [참조3](../future-design/2026-11-07-planning-gaps-and-weather.md)
    - 취객 <!-- source-node:96962066-a854-49ed-87ca-ab05f3514727 --> · [참조1](02-passengers-and-fares.md) · [참조2](../future-design/2026-11-07-planning-gaps-and-weather.md)
      - 취객 특성·운송별 발생·하루 한도 — 당시 50% <!-- source-node:d7adc8be-16b2-5c0f-b8a7-c06ee2d58646 --> · [참조1](02-passengers-and-fares.md)
      - 운송 중 구토·하차 후 청결 저하 — 당시 50% <!-- source-node:808c0209-b1b6-54d0-b261-f7e4366a2286 --> · [참조1](02-passengers-and-fares.md)
      - 도착 시각 확정·잠든 승객 깨우기 지연·중복 정산 방지 — 당시 50% <!-- source-node:d23b8da8-271e-5b46-8e41-6ab51347175d --> · [참조1](02-passengers-and-fares.md)
      - 음주 특성은 공격성·불법 요구와 별개 (기존 취객 행동 재사용) <!-- source-node:23a411f2-4d96-560d-9d20-7db982c15fd1 --> · [참조1](02-passengers-and-fares.md) · [참조2](15-passenger-presets-and-traits.md)
      - 배정30%·미등장 보완·하루 최대2명·일반 승객 휴식 구간 — 당시 50% <!-- source-node:efc6e7c1-2b9c-55e4-97ac-4b4d4106beb4 --> · [참조1](02-passengers-and-fares.md)
      - 구토 전 예고·구토 후 즉시 호출 잠금·세차로 해제 — 당시 50% <!-- source-node:51ca7841-4105-5177-b134-5d3e46221695 --> · [참조1](02-passengers-and-fares.md)
    - 일반 승객 (유형 사건 없이 기존 요청) — 당시 50% <!-- source-node:e21504fe-d153-50ef-991a-9ae2551aed5c --> · [참조1](02-passengers-and-fares.md)
    - 잠자는 승객 — 당시 50% <!-- source-node:fecb075f-5e95-56af-b6d4-90f3d40f51f2 --> · [참조1](02-passengers-and-fares.md)
    - 승차감 민감형 — 당시 50% <!-- source-node:e2f299df-5a8c-57c0-a51d-2ef466950459 --> · [참조1](02-passengers-and-fares.md)
    - 재촉·불만형 — 당시 50% <!-- source-node:27e81483-ec64-54fc-a5a1-82b00013dfe7 --> · [참조1](02-passengers-and-fares.md)
    - 경로 확인형 — 당시 50% <!-- source-node:86653138-3c86-5198-a180-673c44ba39b1 --> · [참조1](02-passengers-and-fares.md)
    - 용무·경유형: 합의한 용무 대기 구현 — 당시 50% <!-- source-node:a4579c31-e8de-5074-9bf6-9f985e0d85dc --> · [참조1](02-passengers-and-fares.md) · [참조2](../CURRENT-STATE.md)
    - 짐 많은형 — 당시 50% <!-- source-node:865892da-5729-537c-8f37-b5e72344ad65 --> · [참조1](02-passengers-and-fares.md)
    - 오염 가능 짐형 — 당시 50% <!-- source-node:98514f86-0843-59e4-8925-536c3574083a --> · [참조1](02-passengers-and-fares.md)
    - 음식·음료 소지형 — 당시 50% <!-- source-node:cfa94e5e-7a65-563d-ace4-3fd4ab0c150c --> · [참조1](02-passengers-and-fares.md)
    - 정밀 승하차형 — 당시 50% <!-- source-node:1fa912d5-df5b-5975-8188-743898ee305d --> · [참조1](02-passengers-and-fares.md)
    - 대화·반복 질문형 — 당시 50% <!-- source-node:9f7c39db-3b3e-52bf-8f9b-ff8d07e4d2d5 --> · [참조1](02-passengers-and-fares.md)
  - 승객 만족도 점수 <!-- source-node:d1744e7e-6def-46ed-8645-1c70e344ae85 --> · [참조1](02-passengers-and-fares.md) · [참조2](../future-design/2026-11-07-planning-gaps-and-weather.md)
    - 만족도에 따른 효과 <!-- source-node:ede02fbc-4df2-4f0a-ad5c-128505160ccd --> · [참조1](02-passengers-and-fares.md) · [참조2](../future-design/2026-11-07-planning-gaps-and-weather.md)
      - 요구 미달 감액·충족 전액/보너스·0점/지각0원 (운임 규칙 참조) — 당시 50% <!-- source-node:87186155-e58a-4a4b-8d52-4cdb6b48fe8c --> · [참조1](02-passengers-and-fares.md)
    - 만족도 증감 요소 정리 — 당시 50% <!-- source-node:52cdadd0-aece-449d-b6d4-bcd26acc0c61 --> · [참조1](02-passengers-and-fares.md)
    - 요구치보다 낮음/같음/높음 가능·재수락/이어하기 시작값 유지 — 당시 50% <!-- source-node:c80e110d-c67d-41a3-bf2d-caed40b033d6 --> · [참조1](02-passengers-and-fares.md)
  - 승객 대화 <!-- source-node:01d05ca1-a7f4-469c-b412-eb3860acede9 --> · [참조1](02-passengers-and-fares.md) · [참조2](../future-design/2026-11-07-planning-gaps-and-weather.md)
    - 승객 대화와 처리 방향에 따른 효과 정리 — 당시 50% <!-- source-node:1d28b789-9d59-4d29-b9e1-4df4a8f8b694 --> · [참조1](02-passengers-and-fares.md)
    - 재촉·간섭 응대 구상 (현재 승객 유형 참고) <!-- source-node:e6e1333d-5b73-4034-9f44-a91fa13eba63 --> · [참조1](02-passengers-and-fares.md)
    - 요청 17종·선택 및 평가 <!-- source-node:1da7bc04-0216-554b-82d9-3bc9ee5caff5 --> · [참조1](02-passengers-and-fares.md) · [참조2](../future-design/2026-11-07-planning-gaps-and-weather.md)
      - 기본 2종·운전 5종·대화 6종·사건 4종 — 당시 50% <!-- source-node:41a079fc-288d-559e-a9c3-2b000e272386 --> · [참조1](02-passengers-and-fares.md)
      - 마우스·키보드 응답·안전상 보류 — 당시 50% <!-- source-node:0bbfedda-205a-57ad-95bc-177003c2748f --> · [참조1](02-passengers-and-fares.md)
      - 선택 효과 적용·하차 실제 이행 평가 — 당시 50% <!-- source-node:1921d3b2-10a1-54d5-a219-c5c9f4ea1c62 --> · [참조1](02-passengers-and-fares.md)
      - 운송 변경 요청 <!-- source-node:6c541078-f5f8-54cf-b384-b81c3359e877 --> · [참조1](02-passengers-and-fares.md) · [참조2](../future-design/2026-11-07-planning-gaps-and-weather.md)
        - 안전 하차지 변경 — 당시 50% <!-- source-node:4cbe3b0b-35e4-59bd-85ad-812cd8306207 --> · [참조1](02-passengers-and-fares.md)
        - 운송 중 목적지 변경 — 당시 50% <!-- source-node:36ac1212-7c4f-5191-90eb-8de55b2c596f --> · [참조1](02-passengers-and-fares.md)
        - 중간 경유 정차·원래 목적지 복귀 — 당시 50% <!-- source-node:2002af6a-9972-502b-b7bb-e3bb5d666985 --> · [참조1](02-passengers-and-fares.md)
      - 분실물 후속 안내·텍스트 기록 — 당시 50% <!-- source-node:220e62c3-7e5d-53bb-bf16-aa2fb19e6b23 --> · [참조1](02-passengers-and-fares.md)
      - 불법 요구 안전 거절 — 당시 50% <!-- source-node:8b7d4def-c0db-5f65-976b-0a4d74f55759 --> · [참조1](02-passengers-and-fares.md)
      - 운송별0~2건·승차/중간 안전 정차·상충 제외·개별 결과 합계 — 당시 50% <!-- source-node:3a2b28d3-9122-4fd0-b004-dd9f0b220cfe --> · [참조1](02-passengers-and-fares.md)
      - 분실물은 하차 후 별도 사건 — 당시 50% <!-- source-node:2bea996c-e907-470e-8834-8429fc8a4896 --> · [참조1](02-passengers-and-fares.md)
    - 승객 추가 상황·복수 행동 확장 (기존 신호/충격 반응 제외) — 당시 0% <!-- source-node:73118cc4-0126-55b2-8725-3037c01c7f90 --> · [참조1](02-passengers-and-fares.md) · [참조2](../future-design/2026-11-02-simulation-priorities.md) · [참조3](15-passenger-presets-and-traits.md)
    - 선택의 만족도·청결·시간 효과 표시·정당한 보류 중립 — 당시 50% <!-- source-node:05deb444-81a8-5656-9a49-07af5e52bf93 --> · [참조1](02-passengers-and-fares.md)
    - 승객 약속 위반·실패 관찰의 추가 범위 — 당시 0% <!-- source-node:review-20261107-request-observation --> · [참조1](../content-tuning-request-outcomes.md)
  - 승객 외 보행자 추가 <!-- source-node:c20ce8ee-e109-431d-aaa3-ef51e4b0dd60 --> · [참조1](02-passengers-and-fares.md) · [참조2](../future-design/2026-11-07-planning-gaps-and-weather.md)
    - 일반 녹색 출발·횡단 중 적색 완주·지정1명 드문 무단횡단 — 당시 50% <!-- source-node:e40bcafd-3e5e-4bc5-836d-9a2d8fdf1816 --> · [참조1](02-passengers-and-fares.md)
  - 호출·배차와 길가 승객 <!-- source-node:2437b07b-8c49-518a-8628-627d051393b6 --> · [참조1](02-passengers-and-fares.md) · [참조2](../future-design/2026-11-07-planning-gaps-and-weather.md)
    - 지도 호출 목록·핀 선택 — 당시 50% <!-- source-node:0478e6c3-86cc-53f8-8600-8fc8900c8016 --> · [참조1](02-passengers-and-fares.md)
    - 목록은 출발/도착·예상 운임·선택 미리보기와 수락 별도 — 당시 50% <!-- source-node:3d8bde49-556a-5419-bc39-e5a8dc066fbd --> · [참조1](02-passengers-and-fares.md)
    - 길가 승객 직접 승차 — 당시 50% <!-- source-node:fa897abb-7e6a-5e7e-9e63-f1331a7aedba --> · [참조1](02-passengers-and-fares.md)
  - 승객 프리셋 제작·특성 조합·미지원 조합 차단 — 당시 50% <!-- source-node:review-20261107-composition --> · [참조1](../work-history/2026-11-06-passenger-composition-implementation.md)
  - 승객별 대사·선택 효과·확률·작업/오염 개별 설정 — 당시 50% <!-- source-node:review-20261107-overrides --> · [참조1](../work-history/2026-11-07-passenger-overrides.md)
  - 승객 다양화1차: 총24명·23활성·신규12명 — 당시 50% <!-- source-node:review-20261107-roster --> · [참조1](../work-history/2026-11-07-passenger-roster-v1.md)
