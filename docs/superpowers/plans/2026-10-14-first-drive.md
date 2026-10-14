# T1–T2 Implementation Plan

**Goal:** Windows에서 직접 운전할 수 있는 Unity 시제품과 검증 기록을 제공한다.
**Architecture:** 원본 보존, Python 변환, Unity 런타임 장면 생성, 독립 입력/차량/카메라/HUD, 에디터 빌드 도구.
**Tech Stack:** Unity 2022.3.62f2, C#, Python 표준 라이브러리.
**Spec:** ../specs/2026-10-14-first-drive-design.md

## Constraints

- 기존 데이터·문서는 유지한다. Git 저장소가 아니므로 별도 `unity/YeongtongTaxi` 폴더에서 작업한다.
- 기존 Unity 설치를 사용하고 라이선스/실행이 막히면 실제 검증 상태를 구분한다.
- 원본 투영 좌표의 상대 배치만 사용. 임의 EPSG 지정 없음.

## Tasks

- [x] 데이터: `tools/export_drive_map.py`, `tests/test_drive_map.py`. 경계 진출입 선분 클리핑, 경계 외 건물, 원점 이동, T1–T2 연결과 비어 있는 도로 처리 검사. 테스트 실패 후 구현.
- [x] Unity: `unity/YeongtongTaxi`. 런타임 MapData/WorldBuilder/TaxiController/FollowCamera/DriveHUD/DriveSession 작성. 테스트용 수치 조향/제동 정책은 순수 C#으로 검사 가능하게 분리.
- [x] 에디터: 시작 장면 생성, 필수 설정 적용, Windows 빌드 명령. 입력 누락, 리셋, 일시정지, 비정상 위치 복구 확인.
- [x] 검증: Unity 배치 컴파일·빌드, 자동 가속/제동/조향/복구 검사, 가능한 화면 캡처. 실행법과 미검증 항목 기록.

## Review focus

경계에서 끊긴 도로, 폴리곤 지붕 삼각분할, WheelCollider 높이, 새 입력 시스템만 켜져 발생하는 입력 오류, 게임 일시정지 중 복구/재개를 검토한다.

## Execution record

- 작업 환경은 Git 저장소가 아니므로 커밋·worktree를 생성하지 않는다.
- Unity 6 권고 대신 현재 설치된 2022.3.62f2로 검증한다. 업그레이드 작업이 추가되는 것을 피하는 선택이다.

- Windows 빌드, 자동 주행, 렌더링 검증 완료. 상세 결과: ../../first-drive-validation.md
