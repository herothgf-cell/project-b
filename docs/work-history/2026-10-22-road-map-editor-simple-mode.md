# 2026-10-22 도로 편집기 간편 편집 기본화

## 요구와 범위

- 사용자 승인 구성: 간편 편집 기본, 도로명/지도 클릭 → 진입 방향 → 차로 번호 → 큰 방향 버튼, 클릭 연결/배치, 신호 기본형, 저장하고 시험, 고급 편집 유지.
- 수정할 도로 선택 방법과 사용법 갱신, 버튼 영역 밖 텍스트 개선.
- 검증 후 로컬 커밋만 한다. 이번 작업에서 main 병합·원격 푸시하지 않는다.
- a9c44f6에서 분기한 `codex/easy-road-editor` 전용 worktree에서 구현했다. 원래 작업 폴더의 별도 게임플레이/문서 미커밋 변경과 배포 폴더를 건드리지 않았다.

## 실제 구현

- 기본 간편 화면에서 내부 ID 대신 도로 이름·교차로·8방향 진입·차로 번호를 보여준다. 지도 클릭/도로명 검색/T1/내 위치로 도로를 찾는다. 같은 진입 방향을 강조하고 선택 차로는 노란색으로 표시한다.
- 좌/직/우 버튼은 실제 그래프 연결을 한 번의 취소 단위로 변경한다. 출구 클릭 연결, 폭 버튼, 제어점 표시 전환, 평행/좌회전 대기차로를 제공한다.
- 표지·노면·신호를 종류 선택 후 지도에 놓고 드래그/회전한다. 적용 차로·정지선·횡단보도를 클릭해 연결한다. 신호 기본 시간표와 단계별 녹색 대상 체크를 제공한다.
- 저장본/자동 초안 목록은 이름·리비전으로 선택하고 스크롤한다. 변경분 2초 자동 초안, 저장하고 시험의 자동 출발/도착 추천, 기본 지도 복원/다음 실행 적용을 제공한다.
- 버튼은 줄바꿈·최소 글자 크기·필요시 말줄임으로 영역에 맞추고 작업 버튼 높이를 늘렸다. 전체 문구는 툴팁으로 확인한다. 체크박스 상태를 표시하며 신호기 배치 중복과 간편 지도 시설명 겹침을 줄였다.
- 별도 코드 리뷰 지적 수정: 팝업 뒤 지도의 클릭 소비 차단, 모양 수정 중 다른 클릭 작업 우선, 단계/기본형 변경의 숫자 캐시 초기화, 저장본 10개 제한 제거. 수정본 재리뷰에서 추가 주요 문제 없음. 리뷰는 정적 검사이며 사람의 클릭 검증과 구분한다.

## 변경 파일

- `unity/YeongtongTaxi/Assets/Scripts/RoadMapEasySelection.cs`: 도로 선택/접근 묶기/친숙한 이름/버튼 문구 맞춤.
- `RoadMapEditorSimple.cs`, `RoadMapEditorSimpleInteraction.cs`(같은 Scripts 폴더): 간편 화면·클릭 작업·자동 초안·시험 추천.
- 같은 폴더의 `RoadMapEditor.cs`, `RoadMapEditorCanvas.cs`, `RoadMapEditorProperties.cs`, `RoadMapEditorProbe.cs`: 화면 라우팅/지도/버튼/프로세스 검사.
- `unity/YeongtongTaxi/Assets/Editor/EasyRoadEditorTests.cs`, `PrototypeBuild.cs`, 생성된 `.meta`, `Assets/Scenes/FirstDrive.unity`.
- `docs/road-map-editor-guide.md`, `docs/prototype-development-checklist.md`, `docs/prototype-scope.md`, `docs/content-design/12-road-map-editor.md`, 이 기록과 `LATEST.md`.
- `tools/update_distribution_manifest.ps1`, `builds/EungyeTaxi/` 변경 바이너리, `distribution/EungyeTaxi-build-manifest.json`.

## 검증과 결과

- 간편 선택/방향 표기/버튼 맞춤/기본 모드 회귀 검사: RED 로그에서 신규 타입/기본 필드 누락을 확인한 뒤 구현, Unity `EasyRoadEditorTests.Run` 7 assertions 통과.
- `PrototypeBuild.BuildEasyRoadMapEditor`: 리뷰 수정 후 `artifacts/easy-editor/build-reviewed.log`, 종료0. 기존 편집기 핵심15/확장 검사와 BuildHybrid 지도/교통 검사를 포함한다.
- `pwsh -File tools/test_prototype.ps1`: 22 모델 묶음 종료0, 로그 `artifacts/easy-editor/model-tests.txt`.
- 최종 실행파일 `-roadMapEditorTest`: 리뷰 수정 후 `artifacts/easy-editor/player-reviewed.log` 종료0, `editor-player-test.txt` 28 PASS/success=True, `trial-child-test.txt` success=True/revision4. 로그 Exception/FAIL 없음. 검사 내용은 기본 모드/도로 클릭/방향과 취소/표지 배치/저장·불러오기/3해상도/표지·노면·신호·고급 버튼/자동 추천 시험/부모 시간·돈·연료·저장파일 격리다.
- `pwsh -File tools/check_workspace.ps1 -Strict`: 필수 지도 입력3개 해시 일치, Input issues=0, 종료0. 입력/ZIP/OSM 설정 변경 없음.
- 화면 캡처: `artifacts/road-map-editor/editor-1280.png`, `editor-1600.png`, `editor-1920.png`, `editor-tab-표지.png`, `editor-tab-노면.png`, `editor-tab-신호.png`, `editor-advanced.png`. 기본/신호/고급 화면 시각 확인.
- 배포 해시는 실제 소스 커밋 `258b5eb` 후 manifest를 재생성했다. `pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly`: DISTRIBUTION_OK files=128 bytes=84846191, 종료0. 새 Unity 씬의 `EungyeTaxi_Data/level0.resS`도 배포에 포함한다. 로그/캡처는 Git 제외 로컬 산출물이다.

## 실행·재현

1. **이번 브랜치 worktree의** `builds/EungyeTaxi/EungyeTaxi.exe` 실행 → 출발 → Esc → 지도 편집. 원래 main 폴더의 exe는 이번 작업에서 교체하지 않았다.
2. 도로 선 클릭 → 왼쪽 진입 방향 → 오른쪽 차로 번호 → ←/↑/→ → 저장하고 시험. 남쪽 진입은 남쪽에서 교차로로 들어오는 방향이다.
3. 자동 재현: 저장소 루트에서 exe에 `-eungye -oneDayMode -roadMapEditorTest -screen-fullscreen 0 -screen-width 1600 -screen-height 900 -logFile <로그 절대 경로>`를 전달한다.
4. 빌드: Unity batchmode `-projectPath unity/YeongtongTaxi -executeMethod PrototypeBuild.BuildEasyRoadMapEditor -quit -logFile <로그 절대 경로>`.
5. PC별 로컬 Unity: `D:/Program Files/Unity Hub/Editor/2022.3.62f2/Editor/Unity.exe`. 이번 worktree의 절대 위치: `C:/Users/USER/.codex/worktrees/easy-road-editor/project-b`.

## 미검증·다음 작업

- 사람의 전체 수동 편집/모든 팝업 클릭·자동차 물리 주행·하루 캠페인·임의 신규 형상/신호 조합·다른 PC 실행은 미검증이다. 자동 API/GUI 렌더/별도 프로세스 검사로 이 항목을 완료 처리하지 않는다.
- 현장 조사 중단 지시 유지. 생성 지도 원본을 재작성하거나 실측 규제를 확정하지 않았다.
- 사용자가 이번 실행본을 확인한 뒤 별도 요청으로 병합/푸시한다. worktree와 브랜치를 유지한다.

## 커밋과 인계

소스/사용법 커밋은 실제 git log -1 --oneline -- docs/work-history/2026-10-22-road-map-editor-simple-mode.md로 확인한 258b5eb다. 후속 배포 커밋에는 실행 폴더 변경·실제 소스 해시가 담긴 manifest·이 검증 결과를 함께 넣는다. 이번 작업은 로컬 커밋만이며 병합/푸시하지 않았다.
