# 지도3개 PC 이동 묶음

## 요청과 수행

새 PC에서 이어서 개발할 수 있도록 필수 생성 지도3개를 한 번에 옮기는 ZIP과 설치·실행 안내를 만들었다. 게임 코드·지도 형상은 변경하지 않았다.

- 전달 파일: `artifacts/handoff/Eungye-map-inputs-2026-10-20.zip` (857,580바이트).
- 별도 압축 해시: 동일 위치 `.zip.sha256`. SHA256 `abe830cfa353f865af2b36511610be60495813941121d1744bbc4ef4e104ed04`.
- 내용: 저장소 상대 경로 그대로 `drive-map.json`, `eungye-map.json`, `route42-map.json`; `_handoff/`에 안내와 manifest 사본. 기존 루트 README/config를 덮어쓰지 않는다.
- 파일 내용은 Git 제외 전달물이다. 사용자가 ZIP과 해시 파일을 새 PC에 따로 옮긴다.
- 코드 기준 `2dde7ea`, Unity2022.3.62f2, 기본 은계 빌드 `PrototypeBuild.BuildHybrid`.

## 안내와 변경 문서

`map-transfer-guide.md`에 Git main 준비, 저장소 루트 압축 해제 예제, 입력 검사, Unity Hub 열기·Taxi > Build Eungye Hybrid·exe 실행·조작법을 기록했다. 실행파일·지도 원본·개인 저장은 묶음에 포함하지 않음을 명시했다. `README.md`(이력 폴더), `LATEST.md`, 개발 체크리스트에 전달 절차를 연결했다. 루트 README와 라이선스 관련 기존 변경은 보존했다.

## 검증

- Python 표준 zipfile/hashlib로 원본3개의 크기·SHA256을 저장소 manifest와 대조: 모두 일치.
- ZIP CRC 검사, 엔트리5개·원본 바이트 동일성·안전한 상대 경로 확인: 통과.
- 새 `artifacts/handoff/extraction-check/`에 실제 압축 해제 후 지도3개 SHA256 재검사: 통과.
- `pwsh -File tools/check_workspace.ps1 -Strict`: 정상 종료0, Input issues:0.
- 근거: `artifacts/handoff/verification.json`, `.zip.sha256` 및 압축 해제 fixture.
- 다른 PC의 Editor 설치·빌드·실행은 수행하지 않았다. 코드 변경이 없어 기존 실행파일을 다시 빌드하지 않았다.

## 이어갈 작업

안내대로 새 PC 저장소 루트에 풀고 strict 검사를 먼저 실행한다. ZIP의 manifest 사본으로 저장소 기준 manifest를 교체하지 않는다. 신규 지도 보정 시 새 입력에 맞는 묶음을 다시 만들어야 한다. 이 기록의 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-20-map-transfer-bundle.md`로 확인한다.
