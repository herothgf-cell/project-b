# PVP A3·참가자 주행 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 사용자 영상의 참가자 주행 덜컥임을 줄이고 승인된 A3 비정형 골목 도시를 기존 native PVP에 적용한다.
**Architecture:** 싱글 공통 차량/승객/법규/시설 유지. 서버 물리 자세와 경기 시각을 일치시키고 수신 공백 후 표시 시각의 과도한 선행을 제한한다. 기존 MapData/HD 차로 그래프에서 대축과 생활권별 골목을 작성한다.
**Tech Stack:** Unity2022.3.62f2/C#/NGO·UnityTransport/기존 MapData.
**Spec:** docs/content-design/pvp/29-city-a3-proposal.md 및 2026-11-03 사용자 적용 지시.

## Global Constraints
- 다른 체크아웃 미커밋 작업·은계 지도/GIS/ZIP/저장 위치 유지.
- 실행은 builds/EungyeTaxi/EungyeTaxi.exe 통합 메뉴. 사람최대4/빈자리봇, 공용8호출·기존18거점/필수시설 유지.
- 중간 전체검사 반복 없이 마지막 필수 수용을 묶는다. PC 입력은 하지 않으며 실제 표시 확인만 필요시 수행.

## Review Focus
- 고속 연속 주행과 카메라에서 실제 위치 변화가 고른지, 평균 FPS와 별도 판정.
- 패킷 공백/시각 중복/경기 종료·재접속 기준값과 표시 복귀.
- 사선/T자/짧은 골목의 차로 접선·연결 및 일방 출구.
- 18승객306방향쌍·시설 진출입·4대 물리 주행.
- 싱글 저장/모드 전환과 알림 후속 보존.

## Tasks
- [x] 영상 메타/프레임 추출·멈춤 구간 확인, 현재 시각/자세 계약 대조.
- [x] body.position/rotation 기반 전송·바퀴 상대 좌표 일치, 제한된 표시 시각 처리·집중 회귀.
- [x] 생활권별 A3 도로/거점·시설 적용 및 집중 그래프 검사 작성.
- [x] 연속 주행 진단을 기존 LAN probe에 추가: 수신/표시 시각·타깃/카메라 변화·실제 물리 입력 기록. 위치 제어 금지.
- [x] 최종 Windows 빌드, 필수 지도/교차/306경로·실제 물리/LAN 연속 주행/알림/재접속·싱글 저장 검사. 미수용은 구분.
- [x] 실행폴더/manifest SHA256 검증, 날짜별 이력/LATEST/기획 갱신, 최신main 보존 통합·커밋/푸시.
