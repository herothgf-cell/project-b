# 2026-11-26 공유용 빌드 압축 도구

## 요구사항

- 빌드를 공유할 때 압축 파일을 만들고, 빌드와 같은 폴더(`builds/`)에 둔다.

## 실제 구현

- `tools/package_build_zip.ps1` 추가. Windows PowerShell 5.1과 PowerShell 7 모두 동작한다(이 PC에는 PowerShell 7 없음).
  1. `distribution/EungyeTaxi-build-manifest.json`의 145파일을 크기·SHA256으로 대조한다. 하나라도 다르면 압축하지 않고 멈춘다(빌드 후 manifest를 갱신하지 않은 경우).
  2. `builds/EungyeTaxi-<yyyyMMdd>-<짧은 커밋>.zip`을 만든다. 압축 안 최상위 폴더는 `EungyeTaxi/`이고 파일 이름은 UTF-8로 기록한다.
  3. 실행에 필요 없는 `택시 시뮬레이션_BurstDebugInformation_DoNotShip/` 폴더는 기본으로 뺀다. 넣으려면 `-IncludeDebugInfo`를 쓴다. 같은 이름의 압축이 있으면 `-Force`로 덮어쓴다.
- `builds/*`는 `.gitignore` 대상이라 압축 파일은 Git에 올라가지 않는다(`git check-ignore`로 확인).
- `distribution/README.md`에 사용법 한 줄 추가.
- 저장소 루트에 `빌드 압축.cmd` 추가(사용자 요청). 더블클릭하면 `-Force`로 압축하고, 성공하면 `builds` 폴더를 열며, 실패하면 메시지를 보여 주고 멈춘다.

## 변경 파일

- `tools/package_build_zip.ps1` (새 파일)
- `distribution/README.md`
- `docs/work-history/2026-11-26-build-share-zip.md`, `docs/work-history/LATEST.md`

## 검증

- 실행: `MANIFEST_OK files=145 bytes=117693237`, `ZIP_OK entries=144 uncompressed=117582534 zipBytes=52478556`.
- Python으로 압축 안 144개 항목을 `builds/EungyeTaxi/` 원본과 SHA256 대조: 불일치 0, `EungyeTaxi/EungyeTaxi.exe` 포함.
- `빌드 압축.cmd`의 명령 줄을 cmd로 실행: `MANIFEST_OK files=145`, `ZIP_OK entries=144`, 종료 코드 0(빌드가 갱신돼 `EungyeTaxi-20261126-40a07af8.zip` 생성). 탐색기 열기와 pause는 직접 더블클릭으로 확인하지 않았다.
- 미검증: 다른 PC에서 압축을 풀어 실제로 실행하는 확인.

## 실행

저장소 루트의 `빌드 압축.cmd`를 더블클릭한다. 또는:

```powershell
powershell -ExecutionPolicy Bypass -File tools\package_build_zip.ps1
```
