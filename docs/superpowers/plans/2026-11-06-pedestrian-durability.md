# Pedestrian Durability Implementation Plan

> Execute inline with superpowers:executing-plans; perform one read-only whole-change review at the end.

**Goal:** 승인된 보행자 사고별0~2/5/10점 손상을 호스트가 한 번 적용하고 저장/복제한다.
**Architecture:** 기존 AccidentState에 선택/적용 상태를 추가하고 VehicleDamage snapshot에 적용 사건 ID를 보존한다. 안전 정차 정산과 기존 D0/공업사를 재사용한다.
**Tech Stack:** Unity2022.3.62f2 / C# / PowerShell.
**Spec:** [승인된 짧은 설계](../specs/2026-11-06-pedestrian-durability-design.md).

- [x] 순수 C# 실패 검사: 손상 범위/0/상한, 재선택 차단, 중복 적용/수리 후 재생, legacy 호환·복사/직렬화·D0 하한.
- [x] AccidentSeverityPolicy/AccidentState/VehicleCare에 선택·적용 ID 보존 구현, Controller 신규/사람 우선 분류·정산 연결.
- [x] 사고 팝업 실제 감소량, LANv7/기존 probe의 새 손상 기대값과 전용 실제 제어기 검사.
- [x] 순수 검사/Editor·실제 싱글/PVP/저장·LAN/렌더 확인, 기본 BuildHybrid와 manifest 전수 검증.
- [x] 사용자 저장/프로필 보존, 기획·체크리스트·작업 이력/LATEST, 명시 파일 로컬 커밋. 기존 유턴/수하물 fixture 미해결은 별도 유지.
