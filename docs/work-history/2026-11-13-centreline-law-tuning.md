# 2026-11-13 중앙선 침범 법규 감점 분리

## 요구사항·구현

- 콘텐츠 관리 툴의 법규에 `중앙선 침범`을 추가하고 `law.centreLine.penalty`로 법규 감점을 별도 편집한다. 기본값은 1점, 허용 범위는 0~100 정수다.
- 실제 중앙선 관찰의 L11 감점이 새 설정을 사용한다. 역주행 L10(기본10), 중앙선 침범과 역주행이 공유하는 운전 만족도(기본10), 비허용 유턴(법규20·운전 만족도12)은 유지한다. 툴에 운전 만족도 공유 관계를 설명한다.
- Node/C# 프로필 검증은 새 필드가 없는 기존 프로필을 허용하고, 이관 시 기본1을 추가한다. 다른 사용자 설정과 기존 프로필 해시 검증을 보존한다. 기본 프로필과 Unity Resources 사본은 기존 생성기로 재생성했다. 사용자 로컬 프로필·과거 프리셋 파일은 수정하지 않았다.

## 변경 파일

- `config/content/catalog.json`, `config/content/default-profile.json`과 `unity/EungyeTaxi/Assets/Resources/Content/`의 같은 두 파일.
- `tools/content-editor/profile.mjs`, `tools/content-editor/public/content-model.js`.
- `unity/EungyeTaxi/Assets/Scripts/ContentTuning.cs`, `unity/EungyeTaxi/Assets/Scripts/DrivingLaw.cs`.
- `tests/content-editor/centre-line.test.mjs`, `tests/ContentTuningTests.cs`, `unity/EungyeTaxi/Assets/Editor/PlaytestBalanceTests.cs`.

## 수행 검증·재현

- 먼저 새 Node 검사2개와 C# 검사가 중앙선 설정 부재로 실패하는 것을 확인했다.
- 구현 후 `pwsh -NoProfile -File tools/test_content_tuning.ps1`: 종료0, 총849 검사, C#/Node UTF8 및 이관 해시 일치.
- `node --test tests/content-editor/*.test.mjs tests/content-manifest.test.mjs`: sandbox에서 HTTP 접근6개 및 임시 Git 접근1개가 권한 오류로 실패했다. 동일 명령을 승인된 sandbox 외부 실행으로 재검사하여 종료0, 106/106 통과.
- `git diff --check`: 종료0.
- `PlaytestBalanceTests.Run`에 실제 중앙선 관찰의 기본1·사용자7·0 감점, 운전 만족도 유지 및 비허용 유턴 감점 유지 검사를 준비했다. 이 하위 작업에서는 Unity·빌드·직접 플레이를 실행하지 않았다. 통합 작업의 실행 결과는 해당 작업 이력에 기록한다.
- 콘텐츠 관리 툴에서 법규 → 중앙선 침범 → 법규 점수 감점을 편집하고 새 세션에 적용한다. 실제 황색 실선 중앙선을 통과해 법규 기록을 확인한다.

## 남은 항목

Unity 위치 제어 검사·통합 실행본 배포는 상위 작업에서 수행한다. 수동 운전·전체 캠페인·다른 PC 검사는 수행하지 않았다. 지도 입력 및 기존 배포본은 이 하위 작업에서 변경하지 않았다.
