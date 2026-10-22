# 2026-10-22 간편 편집기 main 통합

## 요구와 실제 처리

사용자가 이전 커밋만 지시를 변경해 main 병합과 푸시를 요청했다. 간편 편집기 `258b5eb`/배포 `c0c40da`와 최신 main의3일 캠페인/HUD `eb37412`/배포 `d561cad`를 통합한다. 별도 worktree에서 main을 합친 뒤 검증하고 main으로 반영한다. 원래 폴더의 README/LATEST/라이선스/XMind/편집기 계획 미커밋 변경은 별도 보존한다.

- 간편 편집기와3일 캠페인/HUD의 제품 소스는 서로 다른 파일이라 자동 병합했다.
- 범위/LATEST는 양쪽 기능을 유지하도록 통합했다. 실행 바이너리·씬·manifest 충돌은 생성 파일이므로 통합 소스의 `PrototypeBuild.BuildEasyRoadMapEditor`로 재생성한다. 과거 브랜치 실행파일을 main 빌드 위에 덮어 배포하지 않는다.
- 배포 manifest의 모델 검사 수를 현재23묶음으로 갱신했다. 간편 편집 핵심7/기존 핵심15/확장 검사, 기본 은계 BuildHybrid를 유지한다.
- 간편 편집 사용법은 `docs/road-map-editor-guide.md`다. main 실행파일도 출발 → Esc → 지도 편집에서 간편 모드로 열린다.

## 변경 파일

병합 대상은 기존 두 커밋 묶음의 소스/사용법/기획/검사와 배포 파일이다. 충돌 해결·인계는 `docs/prototype-scope.md`, `docs/work-history/LATEST.md`, 이 기록, `tools/update_distribution_manifest.ps1`, 생성 `unity/YeongtongTaxi/Assets/Scenes/FirstDrive.unity`, `builds/EungyeTaxi/` 및 `distribution/EungyeTaxi-build-manifest.json`이다.

## 검증

- Unity2022.3.62f2 `-batchmode -quit -projectPath unity/YeongtongTaxi -executeMethod PrototypeBuild.BuildEasyRoadMapEditor`: 종료0. `artifacts/easy-editor/build-integrated.log`, 간편7/기존 편집 핵심15/확장 검사·BuildHybrid 검사를 포함한다.
- `pwsh -File tools/test_prototype.ps1`: 종료0,23모델 묶음. `artifacts/easy-editor/model-integrated.txt`.
- `pwsh -File tools/check_workspace.ps1 -Strict`: 종료0,필수 지도3개 일치/Input issues=0. 지도입력·ZIP·OSM 대조 설정 변경 없음.
- 통합 실행파일 `-roadMapEditorTest`: 종료0,28 PASS/success=True. 시험 자식 success=True/revision4. 기본 간편 화면·3해상도·저장/취소·부모 영업 격리 검사 통과. `artifacts/easy-editor/player-integrated.log`.
- `-threeDayTest`와 새 프로세스 `-threeDayReloadTest`: 각각 종료0,9+11 PASS. 날짜/누적 수익/야간 회복/저장 복원 검사 통과.
- `-presentationHudTest`: 종료0,20 PASS. 승하차/비상등/정산/시간 정지/수동 시작/초기 내비 없음 검사 통과.
- 실제 통합 소스 커밋 후 배포 manifest를 재생성·검증한다. 결과는 아래 최종 검증에 기록한다.

## 실행·재현과 남은 범위

기본 실행은 저장소 루트의 `builds/EungyeTaxi/EungyeTaxi.exe`다. 편집기 검사는 `-eungye -oneDayMode -roadMapEditorTest -screen-fullscreen 0 -screen-width 1600 -screen-height 900`; 캠페인 검사는 각각 별도 프로세스 `-batchmode -nographics -threeDayTest -hybridV2`, 이어 `-threeDayReloadTest -hybridV2`; 위치제어 HUD 검사는 `-batchmode -nographics -presentationHudTest -hybridV2`다. 모든 명령에 `-logFile <절대 경로>`를 붙였다.

로컬 Unity 실행 경로는 `D:/Program Files/Unity Hub/Editor/2022.3.62f2/Editor/Unity.exe`, 통합 작업 폴더는 `C:/Users/USER/.codex/worktrees/easy-road-editor/project-b`다. 로그는 `artifacts/` 로컬 산출물이며 Git 제외다.

자동 날짜 상태/위치 제어/편집 API·렌더·프로세스 검사는 사람의 수동 편집·물리 운전·60분3일 캠페인·모든 형상/신호 조합·다른 PC 재현을 대신하지 않는다. 이 항목은 계속 미검증이며 현장 지도 조사 중단을 유지한다. 다음은 사용자의 통합 실행본 확인과 수동 플레이 검증이다.

## 최종 배포 검증

실제 통합 소스/병합 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-22-easy-editor-main-integration.md`로 확인한 `aa7eb84`다. 이 전체 해시로 manifest sourceCommit을 갱신했다. 배포 `-VerifyOnly` 결과는 `DISTRIBUTION_OK files=128 bytes=84865607`, 종료0이다. 제품 소스 변경 없이 배포 해시/인계만 후속 커밋한다. main을 검증된 통합 커밋으로 fast-forward하고 origin/main에 푸시한다. 기존 사용자 문서 변경은 narrow stash/3-way 복원으로 별도 유지한다.
