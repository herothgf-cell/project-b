# 프로젝트 작업·인계 규칙

## 작업 시작

1. `docs/work-history/LATEST.md`에서 직전 상태와 이어갈 작업을 읽는다.
2. `docs/prototype-development-checklist.md`, `docs/prototype-scope.md`, 관련 `docs/content-design/` 문서와 현재 코드를 대조한다.
3. `git status --short`로 기존 사용자 변경을 확인하고, 해당 작업의 변경과 섞지 않는다. PC를 바꿨다면 `pwsh -File tools/check_workspace.ps1 -Strict`로 필수 지도 입력을 확인한다.

## 작업 이력은 매 작업 종료 시 함께 남긴다

- `docs/work-history/YYYY-MM-DD-주제.md`에 요구사항, 실제 구현, 변경 파일, 수행한 검증과 결과, 미검증/남은 항목, 실행·재현 방법을 기록한다. 같은 날 주제가 다르면 파일을 나눈다.
- `docs/work-history/LATEST.md`를 최신 기록 링크·현재 빌드/지역·다음 작업·필수 로컬 입력 상태로 갱신한다. 대화에만 인계 내용을 남기지 않는다.
- 실제 명령/종료 결과를 보고 완료를 표시한다. 위치 제어 검사, 자동 물리 주행, 수동 플레이, 전체 캠페인 검사를 구분한다.
- 경로는 저장소 기준 상대 경로를 기본으로 쓰고, PC별 Unity/Python 설치 경로는 로컬 환경 정보로 구분한다.
- 관련 체크리스트·기획서와 작업 이력을 변경 코드와 함께 커밋한다. 자신을 포함한 커밋 해시를 추측해 쓰지 말고 `git log -1 --oneline -- <기록 파일>`로 해당 이력의 실제 커밋을 확인한다.

## 지도·빌드와 PC 이동

- 기본 실행 대상은 은계 `builds/EungyeTaxi/EungyeTaxi.exe`, 빌드는 `PrototypeBuild.BuildHybrid`다. 별도 연습과 기본 은계 영업을 혼동하지 않는다.
- `data/raw/`, 생성 지도 JSON, `builds/`, `artifacts/`, Unity `Library/`는 Git에서 제외된다. 다른 PC에서 Git만 받아서는 같은 데이터·실행파일이 생기지 않는다. 준비 방법은 `docs/work-history/README.md`를 따른다.
- 필수 생성 입력 지도를 변경한 작업에서는 `config/maps/local-input-manifest.json`의 파일 크기·SHA256과 관련 OSM 대조 설정도 실제 자료로 다시 갱신/검증한다. 해시 오류를 우회하지 않는다.
