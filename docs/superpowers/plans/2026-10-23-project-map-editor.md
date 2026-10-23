# 프로젝트 지도 편집·배포 Implementation Plan

> **For agentic workers:** Use superpowers:subagent-driven-development for independent model/UI tasks; parent integrates persistence/build and verification.

**Goal:** 지도 외형·서비스·도로 편집을 Git 추적 프로젝트 자료에 저장하고 pull/build로 재현한다.
**Architecture:** 패키지 기준 지도 + 기존 도로 문서 + 전체 장면 객체; 검증된 정식 자료를 빌드 Resources에 포함한다.
**Tech Stack:** Unity2022.3.62f2/C#/JSON/PowerShell.
**Spec:** docs/superpowers/specs/2026-10-23-project-map-editor.md

## Global Constraints

기존 미커밋 자료 보존, 최신 은계/3일/식당 기능 유지, Windows 실행파일, 재생성으로 편집 유실 금지, 해시 오류 우회 금지, 자동 플레이와 수동 검증 구분.

## Review Focus

- 기존 보정 코드가 사용자 폴리곤/생활시설을 덮거나 중복 생성하지 않는지.
- 임의 다각형/중복ID/손상/잘못된 접근을 저장 전 차단하는지.
- 개인 저장 없는 PC에서도 동일한 패키지가 로드되는지.
- 프로젝트 경로 실수/다중 인스턴스 충돌 시 기존 파일이 보존되는지.
- UI 선택·드래그·팝업·Undo가 객체 참조 및 숫자 캐시와 일치하는지.

## Tasks

- [x] 1. 장면 모델·검증·실제 월드/서비스 구성. Files: ProjectMapScene.cs + WorldBuilder/RestaurantSiteLayout/FuelStationLayout/ServiceStops/MapData, RoadMapDocument/Composer/Validator. Build editor tests geometry/roundtrip/invalid cases.
- [x] 2. 독립 ProjectMapStore.cs/ProjectMapPackage.cs 및 ProjectMapBuild.cs: 검증/원자 저장/동시 변경/저장소 발견/복제 재현. Runtime/MapCatalog/PrototypeBuild 연결. RED→GREEN 검사 후 기본 프로젝트 자료 초기화.
- [x] 3. RoadMapEditorScene.cs/Project.cs 및 Simple/Canvas/Preview: 객체 CRUD·이동·꼭짓점·서비스 접근과 프로젝트 저장/불러오기. 현재 간편 차로 기능 유지.
- [x] 4. 독립 리뷰/수정, Unity 핵심/모델/실행 프로브·빌드/복제 재현, 사용법·기획·이력·LATEST/배포manifest/커밋 및 검증된 전달.

## Ledger

Ruling: 사용자 요청은 전체 기능 구현을 진행하라는 명시적 승인이다. 추가 단계별 승인 요청 없이 구체 설계와 실행을 진행한다.
Ruling: 기존 attached worktree를 최신 main037b066으로 갱신하고 codex/project-map-editor에서 작업한다. 기본 폴더의 다른 사용자 변경은 보존한다.
