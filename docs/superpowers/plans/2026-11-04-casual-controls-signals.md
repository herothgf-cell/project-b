# Casual Controls and Signals Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** 싱글/PVP의 조향·일상 제동과 불필요한 신호 대기를 개선한다.

**Architecture:** 기존 DriveMath/TaxiController와 LAN 입력에 공통 정책을 적용한다. 기본 신호 프로그램은 보존하고 별도 runtime 시계가 수요별 단계 시간을 제어한다. 호스트가 결정한 runtime 상태를 복제한다.

**Tech Stack:** Unity2022.3.62f2, C#, 기존 WheelCollider/TrafficSystem/native LAN.

**Spec:** [승인 설계](../specs/2026-11-04-casual-controls-signals-design.md).

## Global Constraints

- 기본 은계/A2·지도 원본·회사/제품/저장 경로·경제·H 경로 기하 보존.
- 편집/교육 신호는 수요 제어 대상이 아님. 보행 보호·황색/정리 생략 금지.
- 사용자 기존 미추적 자료 제외. 새 수치는 시험값이며 사람 체감 검증을 자동 검사와 구분.

## Review Focus

- S 유지 중 의도치 않은 후진, 리셋/네트워크 보정 때 상태 초기화.
- 좌우 대칭·고속 안정·입력 해제 시 중앙 복귀, H 최대각 보존.
- 같은 차로 출발을 상충 차량처럼 잠그지 않고 실제 장애물은 멈춤.
- 무내비 플레이어/원격 택시도 신호 수요 반영, 차량/보행 기아 방지.
- 호스트/참가자 신호·예상 대기 일치, 편집/교육 신호 보존.

## Task 1: 조작 정책과 PhysX 비교

Files: DriveMath.cs, DriveFlowPolicy.cs, TaxiController.cs, PvpNativePrediction.cs, 새 CasualDriveChecks.cs/Probe.cs.
- [x] 기존 설정에서 제동/중속 조향 기대와 정차 유지 회귀 RED.
- [x] 공통 반응·조향 곡선과 상태 있는 페달 정책 구현, 모델 GREEN.
- [x]20/30/40 제동과90도 코너/고속 전복을 실제 물리로 비교하고 수치 확정.

## Task 2: 대기열과 신호 수요

Files: TrafficSystem.cs, 새 DemandSignalClock.cs/TrafficSignalDemand.cs, 기존 TrafficCar.cs(필요시).
- [x] 같은 차로3대 출발·출구막힘 검사(기존 동작 통과), 새 수요/최소 녹색·보행/경쟁 순환은 RED→GREEN.
- [x] 재현된 예약 문제만 수정. 기본 신호 수요 runtime·접근 선행 감지·상충 보호 구현.
- [x] 수요 없는 접근/점유 횡단·교육/편집 제외·동시 녹색0을 GREEN으로 확인.

## Task 3: 공유 상태와 표시

Files: PvpNativeReplica.cs/프로토콜, DrivingReadability.cs, 검사 파일.
- [x] runtime 상태·예상 진행 시간 직렬화/복원 RED.
- [x] 호스트 복제·예상 표기·우회전 조건 보존·같은 입력 정책 적용, GREEN.

## Task 4: 통합·기록·빌드

- [x] 관련 기존 Editor/모델 회귀·실제 싱글/PVP/LAN 검사. 사용자 저장 해시 보존 확인.
- [x] 담당 기획01/체크리스트/작업 이력/LATEST 갱신·소스 커밋.
- [ ] BuildHybrid/실행 폴더 manifest 재생성·검증. 실제 배포/미검증 범위를 기록.
