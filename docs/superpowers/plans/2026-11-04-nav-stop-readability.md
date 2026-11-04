# 내비·승하차·지도·글자 가독성 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** 사용자가 승인한 네 가지 인지 개선을 은계 싱글/A2 PVP·LAN 공통으로 적용하고 검증한 통합 실행파일을 main에 배포한다.

**Architecture:** 길찾기/운송/서버 권한은 그대로 두고 유효 경로와 마지막 표시 경로를 분리한다. 실제 정차 조건을 읽는 별도 승하차 안내 모델/월드 표시, 지도 위치 강조와 해상도 반응형 UI를 공유한다.

**Tech Stack:** Unity2022.3.62f2/C#·Windows, 기존 PrototypeBuild.BuildHybrid/배포 manifest.

**Spec:** `docs/future-design/2026-11-04-playtest-feedback-review.md`의 경로/승하차/M/글자 항목과 2026-11-04 사용자 개발 지시. 조향/제동·충돌/신호 변경은 제외한다.

## Global Constraints

- 캠페인 회사/제품/저장 위치·A2 지도/규칙/운임/서비스·LAN 서버 판정 보존.
- 기존 미추적 실험 파일은 제외하며 현재 clean tracked 체크아웃의 별도 codex 브랜치로 진행한다.
- 재탐색 실패한 이전 선은 표시용으로만 유지하고 hdRoute/path/보조 운전/ETA에 유효 경로처럼 넣지 않는다.
- F승하차 방식·속도/방향/위치·점유/보행/적재·하역/기상 대기 보호를 바꾸지 않는다.
- 통합 exe 폴더 전체·manifest를 갱신하고 실제 검사와 수동 플레이를 구분해 이력을 남긴다.

## Review Focus

- 실패 재탐색 뒤 목적지 변경/취소/완료에 옛 길이 새 운송에 남지 않음.
- 시설 출구/선택 호출 미리보기·길가 승객·대체 승하차/경유 안내가 현재 목표와 맞음.
- 내비가 없어도 실제 F조건 안내, 기상 대기/하역 중에는 거짓 완료 안내 없음.
- M시설/호출 포커스 유지·화면 밖 내 위치 표시·배율별 실제 클릭 좌표 일치.
- 작은/큰 해상도에서 긴 한글·상태/운행 카드/대화·미니맵의 겹침 및 사용자 저장 보호.

## Task 1: 경로 표시 보존

Files: `RoadNavigation.cs`, `MapDestinationUI.cs`, `DrivingReadability.cs`, `FullMapOverlay.cs`, 새 `Assets/Editor/ArrivalReadabilityChecks.cs`.

- [x] 실제 nav에 연결된 lane→다른 연결 없는 lane 이탈의 회귀 검사 작성, 기존 코드에서 경로 소실 RED 확인.
- [x] `RoadNavigation.DisplayPath`/`RetainingPreviousRoute`를 추가한다. 성공 시 표시 복사, 실패 시 기존 표시 보존·계산 중 안내. Clear/새 목표/새 지도에는 제거.
- [x] 지도/미니맵/월드 선에서 흐린 옛 길을 표시하고 보조 운전은 기존 유효 경로만 사용. 회귀 GREEN/목표변경·재복귀 검사.

## Task 2: 승하차 위치/F 안내

Files: 새 `StopArrivalGuidance.cs`, `StopArrivalPresentation.cs`, `CockpitUI.cs`, `DriveSession.cs`, 검사 파일.

- [x] MapStop 위치/속도/방향·점유/대체 보행·준비 상태별 기대 안내 검사 RED.
- [x] 실제 조건을 읽는 안내와 현재 목표 월드 정차 테두리/방향/F표시 구현. OnTrip/ToPickup/경유·기상대기 구별.
- [x] nav.Clear 상태에서도 F승차/하차 가능 안내와 실제1회 운송 완료를 Player에서 검사.

## Task 3: M 내 위치와 글자

Files: 새 `UiReadability.cs`, `FullMapOverlay.cs`, `MapPresentation.cs`, `MapDestinationUI.cs`, `CockpitUI.cs`, `DrivingReadability.cs`, `DriveHUD.cs`.

- [x] 해상도별 최소 글자/스케일과 지도 내부/화면 밖 위치 강조·포커스 유지 검사 RED.
- [x] M의 논리 좌표/클릭 변환·짧은 위치 강조/항상 보이는 내 위치 버튼 구현. 기존 호출/시설 선택 보호.
- [x] 기본 글자 확대·최소 읽기 크기·고해상도 UI 확대, 동적 줄높이/영역 보정. UI 텍스트 겹침 렌더 검사.

## Task 4: 통합 검증·배포

- [x] 관련 Editor 집중 회귀와 BuildHybrid, 실제 싱글/PVP Player 위치/입력 검사 및1280/1600/1920 렌더 캡처.2560은 배치 수식 검사만 통과(실제 창은1920으로 제한).
- [x] 기존 실제 LAN 호스트/참가자 검사로 운송/내비/조향 복제 보존 확인, 개인 저장 파일 불변 확인.
- [x] 사용자 미완료(물리/사람 장시간/DPI) 범위 명시. 기획/체크리스트/작업이력/LATEST 갱신·소스 커밋.
- [x] 실제 소스6b7f054로 manifest 갱신·검증, 실행 폴더140파일 커밋. main6da9319 보존 통합/원격 푸시·원본/원격 해시 일치 확인. 문서 영수증 후속은 같은 실행 파일 유지.
