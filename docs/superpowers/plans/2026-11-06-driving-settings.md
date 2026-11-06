# Driving Settings Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 일시정지에서 네 가지 운전 설정을 편집·저장한다.
**Architecture:** 개인 PlayerPrefs 모델과 공통 수동 조향 정책, 공통 설정 패널을 연결한다. LAN 입력은 검증한 드라이버별 계수를 전달한다.
**Tech Stack:** Unity 2022.3 C#, IMGUI, 기존 Netcode.
**Spec:** ../specs/2026-11-06-driving-settings-design.md

## Global Constraints
- 기본값100%/100%/100%/15%, 민감도50~150%, 반응/복원25~200%, 데드존0~40%.
- 기존 H·봇·가속/제동/사용자 저장·필수 지도 입력 보존. 중심각52도 상한.
- LAN v10, 서버 권한/로컬 예측 공통 계산, 사용자 콘텐츠 프로필과 별도 저장.

## Review Focus
- NaN/Infinity/누락된 저장값과 패킷은 안전한 기본값/범위로 처리한다.
- 취소·Esc는 저장하지 않고 한 단계 복귀한다.
- 작은 창과 LAN 메뉴에서 설정 패널이 겹치지 않는다.
- 데드존0%는 Unity 입력 축의 고정15%에 막히지 않는다.
- 설정이 서버 다른 사람·봇·H에 전파되지 않는다.

### Task 1: 설정 모델·수동 입력·LAN
**Files:** DrivingSettings.cs, TaxiController.cs, DriveFlowPolicy.cs, PvpNativeLan.cs, PvpNativePrediction.cs, PrototypeBuild.cs, InputManager.asset, Editor/DrivingSettingsChecks.cs.
**Interfaces:** DrivingSettings.Current/Copy/Normalize/Advance/Input/Angle/Save/Load; TaxiController.ManualSteeringGain; PvpNativeDrive.steeringGain/steeringResponse/steeringReturn.
- [x] 모델 부재 RED 확인.
- [x] 개인 설정·기본값 보존·입력/각도/복원·서버 제한 구현.
- [x] 독립 옵션·대칭·직렬화·입력 큐/예측·H/봇 회귀 GREEN.

### Task 2: 메뉴·설정 UI
**Files:** DrivingSettingsPanel.cs, DriveHUD.cs, DriveSession.cs, PvpNativeLan.cs, Editor/DrivingSettingsChecks.cs, runtime probe/runner.
**Interfaces:** 패널 Open/Apply/Cancel/Reset/HandleBack/Draw, 드래프트만 편집.
- [x] 취소/기본값/저장·Esc 기대 RED.
- [x] 싱글/연습/PVP pause 버튼, 공유 패널, 축 dead0 구현.
- [x] 저장·재시작/취소/기본값·화면 렌더·실제 바퀴 검사.

### Task 3: 통합 검증·배포
- [x] 현재 코드 리뷰, 중요 문제 수정 및 회귀.
- [x] 통합 BuildHybrid·싱글/PVP 실제 Player·LAN·Strict·사용자 저장 보존 확인.
- [x] 관련 기획/체크리스트/LATEST/작업 이력과 소스 커밋.
- [x] 실제 소스 해시로 통합 manifest 갱신·VerifyOnly 후 실행 폴더·기록 커밋.
