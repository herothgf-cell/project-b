# Braking Sensitivity and Left Signal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 개인 일반 제동 민감도와 재현된 좌회전 신호 문제를 해결한다.
**Architecture:** DrivingSettings·TaxiController와 입력 패킷에 제한된 개인 감속 계수를 추가한다. 신호 수정은 별도 진단에서 확인한 원인/접근을 기준으로 한다.
**Tech Stack:** Unity2022.3/C#/IMGUI/Netcode.
**Spec:** ../specs/2026-11-06-braking-and-left-signal-design.md

## Global Constraints
- 일반 브레이크50~150%, 기본100%, 기존 preference 누락 기본값 보충.
- Space·자동 서비스/사고/메뉴 제동·봇 보존, H 중 사람 일반 페달은 적용.
- LANv11 서버 범위/유한수 검증·예측과 공통 계산.
- 기본 은계 통합 실행/개인 생활·취객 설정·사용자 현재 저장·다른 작업 보존.

## Review Focus
- 일반 S로 멈춘 후 후진 의도가 바뀌지 않는다.
- UI와 실제 서버의 제동 계수가 다르게 적용되지 않는다.
- 이전4항목 저장은 민감도100%로 읽고 취소/기본값 동작이 유지된다.
- 좌회전은 불가능한 연결이나 보행자 보호를 우회하지 않는다.
- 미점등 재현과 사람 실제 주행을 구분하고 지도 입력을 무단 재생성하지 않는다.

### Task 1: 브레이크 설정
**Files:** DrivingSettings/Panel/Probe/Checks, TaxiController, PvpNativeLan/Prediction/Probe, ContentTuningChecks.
**Interfaces:** DrivingSettings.braking=1; TaxiController.ManualBrakeGain=1; PvpNativeDrive.brakeSensitivity=1.
- [x] 항목 부재 RED, 기본/일반/긴급/봇 제동·이관/패킷 검증.
- [x] 정책·5번째 슬라이더/저장·LANv11 구현.
- [x] 실제 바퀴 제동 토크·PhysX 정지 거리/대칭·기존 조향 회귀 GREEN.

### Task 2: 좌회전 진단·최소 수정
- [x] 이미지 지도/접근 대조·확인 가능한 대기 조건과 Q 여부 구분.
- [x] 재현 RED·최소 수정·실제 맵/보행 충돌·신호 회귀 GREEN.

### Task 3: 통합·배포
- [x] 수리 안내를 중앙에서 속도계 내부 ‘공업사’로 이동·실제 내구도23/속도100·작은 창·안내 열기 확인.
- [x] 신선한 리뷰·두 모드 Player/설정 렌더·LAN/재접속·BuildHybrid·Strict.
- [ ] 기획/체크리스트/LATEST/작업 이력·소스 선택 커밋.
- [ ] 실제 소스 hash로 manifest 갱신/검증·통합 실행 배포 커밋.
