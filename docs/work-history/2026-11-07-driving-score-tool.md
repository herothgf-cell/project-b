# 2026-11-07 운전 점수 툴 설정 연결

## 요청·구현

사용자가 선택한 1번 범위: 현재 구현된 규칙의 누락된 수치만 설정 툴에 연결했다. 신규 법규·단계 과속·안전배려B·획득 상한은 추가하지 않았다.

- 기존 법규/M/회복 설정30개를 확인하고, 법규 메뉴에 고정 감점9개를 추가했다.
- 적색 점멸 정지 / 비보호 좌회전 상황 / 출구 막힘 진입: 각 기본15점.
- 보행자 사고 경미/중간/중대: 10/25/40점.
- 차량·시설 사고 경미/중간/중대: 3/8/15점. 두 사고 대상은 기존처럼 값을 공유한다.
- 모두0~100점 감점량. 화면에서는 기존 음수 입력 규칙을 사용한다. 기본값은 유지한다. 상황 판정의 감점0도 위반 사실은 유지한다.
- 이전 프로필에 신규9개가 전부 없으면 기본값 보충, 일부만 누락되면 오류. 원본 hash를 먼저 확인한다. 기존 사용자 수치·대사는 유지한다.
- 사고 발생 때 감점을 확정하고 저장한다. 이후 설정 변경으로 진행 중 사고를 재평가하지 않는다. 저장 감점은0~100 범위를 검증하고 비용/시간/견인 등 나머지 무결성 검사는 유지한다.

## 사용법·범위

`tools/start_content_editor.ps1` 실행 → **법규** → 항목 검색/선택 → 감점 수정 → 저장 → 게임에 적용 → 새 캠페인/새 PVP방. 이어하기는 저장 당시 설정을 유지한다. LAN 참가자는 동일 최신 실행본을 사용한다.

적색 직진/좌회전/유턴 조건의 일반 관찰은 기존 설정을 공유하며, 이번 ‘비보호 좌회전 상황’은 교육/상황 판정 경로의 고정15점을 조정한다. 실제 은계 전역에 비보호 장소를 새로 만드는 설정이 아니다. 황색 점멸의 관찰 보류도 그대로다.

## 변경 파일

- `config/content/{catalog,default-profile}.json`, Unity Resources 동명 파일: 추가9개 카탈로그/기본 프로필.
- `tools/content-editor/profile.mjs`, `public/content-model.js`: 이전 프로필 보충·메뉴/설명.
- `ContentTuning.cs`, `ConfusionPracticeState.cs`, `AccidentSeverityPolicy.cs`, `AccidentState.cs` (Unity Scripts): 소비·이관·진행 사고 보존.
- `tests/content-editor/driving-settings.test.mjs`, `tests/ContentTuningTests.cs`, `tools/test_content_tuning.ps1`, `tools/test_accidents.ps1`: 설정·판정·저장 검증. 기존 사고 테스트 스크립트의 ChoiceEffects 의존성 누락도 수정.
- 주행 기획·툴 안내·체크리스트·이력, 통합 실행본·배포 manifest.

## 검증

- 신규 Node 검사 먼저 실패 확인(필드 부재), 구현 후 통과.
- `node --test tests/content-editor/*.test.mjs`: 90/90, exit0. 첫 sandbox 실행은 localhost EACCES로 서버 검사 실패, 허용된 재실행에서 통과.
- `pwsh -File tools/test_content_tuning.ps1`: C# 781 및 Node/C# hash·이관 동등성 통과. 실제 상황 감점37·사고감점38을 주입해 판정/설정변경 후 사고 보존 확인. 추가 테스트의 변수명 충돌은 수정 후 재검사했다.
- `pwsh -File tools/test_accidents.ps1`: 77개 통과. 최초 ChoiceEffects 의존성 누락 컴파일 실패 후 스크립트 수정·재검사.
- Unity `PrototypeBuild.BuildHybrid`: `artifacts/driving-settings-build.log`에 PROTOTYPE_BUILD_OK·종료코드0 확인.
- 사람 수동 플레이·자동 물리 주행·전체3일·다른PC/LAN 신규 검사는 하지 않았다. 기존 연습/지도 범위는 유지한다.

## 실행·인계

은계/A2 통합 `builds/EungyeTaxi/EungyeTaxi.exe`, LANv14, BuildHybrid. 지도/ZIP·사용자 활성 설정을 변경하지 않았다. 필수지도 입력은 직전 비 프로토 작업의 Strict issues0를 인용하며 이번 재검사 없음. 기존 LATEST의 다른 작업 기록·미추적 자료를 보존했다. 다음은 사용자가 툴로 수치를 조정한 뒤 새 세션에서 밸런스를 확인하는 작업이다.

## 배포 영수증

소스 커밋46ef637 확인 후 통합 manifest를 실제40자리 커밋으로 생성했다. 통합/PVP VerifyOnly 모두 exit0, 142파일132,276,206바이트. 최초 짧은 해시 인자는 스크립트가 거부했고 전체해시로 재실행했다. 실제 Player 자동 실행/사람 플레이 검사는 이번 범위에서 하지 않았다.
