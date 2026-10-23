# 도로 편집기 main 병합

사용자 요청으로 `codex/road-map-editor`를 최신 main에 병합했다. 먼저 원격을 fetch해 main의 마인드맵 추가 커밋 `275d873`을 확인하고 보존했다. main을 원격까지 fast-forward한 뒤 편집기 브랜치를 병합해 실제 병합 커밋 `07205fe`를 생성했다. 병합 충돌은 없었다.

README와 LATEST의 기존 미커밋 변경은 두 파일만 명시적으로 stash한 뒤 병합 직후 복원했다. 줄바꿈을 정규화한 원문 비교에서 두 파일이 모두 일치한 뒤 이번 임시 stash만 제거했다. 라이선스·XMind 등의 untracked 파일은 작업에 포함하지 않았다.

## 병합 후 검증

- `git diff --exit-code codex/road-map-editor HEAD -- unity/EungyeTaxi builds/EungyeTaxi distribution/EungyeTaxi-build-manifest.json`: 종료0. 검증된 편집기 코드·빌드·manifest와 동일하다.
- `pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly`: 종료0, 배포127파일/84,685,847바이트 크기·SHA256 일치.
- `pwsh -File tools/check_workspace.ps1 -Strict`: 종료0, Input issues0.
- 코드/실행 폴더 변경이 없어 Unity 재빌드와 수동/물리/캠페인 검사를 반복하지 않았다. 기존 검증 범위는 [구현 이력](2026-10-22-road-map-editor-implementation.md)을 따른다.

기본 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`, 은계 BuildHybrid다. 필수 입력3개·지도 ZIP과 배포 manifest는 이번 병합에서 변경하지 않았다. 사용자 승인에 따라 이 인계를 커밋한 뒤 origin/main으로 푸시하고 원격 커밋 일치를 확인한다. 이 기록의 실제 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-22-road-map-editor-main-merge.md`로 확인한다.
