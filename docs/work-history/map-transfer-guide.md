# 은계 지도 입력 묶음 — 새 PC 사용법

이 ZIP은 개발에 필요한 생성 지도 3개와 안내·검증용 목록을 담는다. 코드, Unity Editor, 원본 NGII/OSM, 실행파일은 포함하지 않는다. `unity/YeongtongTaxi`는 기존 프로젝트 폴더 이름이며 기본 빌드 지역은 은계다.

## 1. 코드와 개발 도구 준비

- GitHub `https://github.com/herothgf-cell/project-b`의 main을 복제한다. 이미 있으면 로컬 작업을 보존한 뒤 `git pull --ff-only`로 갱신한다.
- Windows PC의 Unity Hub에서 **Unity 2022.3.62f2**를 설치하고 Windows 빌드 대상을 사용할 수 있는지 확인한다. 현재 기본 Mono 빌드를 사용한다. IL2CPP로 변경할 때만 해당 Windows Build Support 모듈과 C++ 도구를 추가한다.
- 입력 검사에는 **PowerShell 7 (`pwsh`)**을 사용한다.
- 이 묶음의 기준 코드는 `2dde7ea`다. 이후 지도가 바뀐 커밋에서는 저장소 manifest와 일치하는 새 묶음이 필요할 수 있다.

## 2. ZIP을 저장소 루트에 풀기

예를 들어 새 PC의 저장소가 `C:\git\project-b`면 **그 폴더 안에** 압축을 푼다. ZIP 파일명 폴더가 중간에 생기면 안 된다. 기존 Unity Editor를 닫고, 같은 이름의 로컬 지도를 수정한 적이 있다면 먼저 별도 백업한다.

압축을 푼 뒤 다음 파일이 있어야 한다.

```text
C:\git\project-b\
  unity\YeongtongTaxi\Assets\Resources\drive-map.json
  unity\YeongtongTaxi\Assets\Resources\eungye-map.json
  unity\YeongtongTaxi\Assets\Resources\route42-map.json
  _handoff\map-transfer-guide.md
  _handoff\local-input-manifest.json
```

PowerShell에서 경로를 명시해 풀 수도 있다. 기존 파일 덮어쓰기는 자동으로 강제하지 않는다.

```powershell
Expand-Archive -LiteralPath "$env:USERPROFILE\Downloads\Eungye-map-inputs-2026-10-20.zip" -DestinationPath 'C:\git\project-b'
Set-Location 'C:\git\project-b'
pwsh -File tools/check_workspace.ps1 -Strict
```

`Input issues: 0`과 정상 종료를 확인한다. 누락·불일치가 나오면 압축 위치와 코드/묶음 버전을 대조한다. 검사기는 저장소의 `config/maps/local-input-manifest.json`을 기준으로 사용하며 ZIP의 목록으로 덮어쓰지 않는다. `_handoff`는 전달용 사본이므로 확인 후 프로젝트 밖으로 옮겨도 된다.

ZIP과 함께 받은 `.zip.sha256`은 압축파일 전체 확인용이다. 아래 출력과 그 파일의 첫 SHA256 값이 같아야 한다.

```powershell
Get-FileHash -LiteralPath "$env:USERPROFILE\Downloads\Eungye-map-inputs-2026-10-20.zip" -Algorithm SHA256
```

## 3. 빌드하고 실행

1. Unity Hub의 Add로 **`C:\git\project-b\unity\YeongtongTaxi`**를 추가해 2022.3.62f2로 연다. 첫 Asset import가 끝날 때까지 기다린다.
2. Unity 상단 **Taxi > Build Eungye Hybrid**를 선택한다. 이 명령이 통합 지도·OSM 보완 입력과 기본 은계 장면을 준비하고 검사 후 Windows 실행파일을 만든다.
3. 빌드가 성공하면 **`C:\git\project-b\builds\EungyeTaxi\EungyeTaxi.exe`**를 실행한다. 화면 지역명이 **시흥 은계지구**인지 확인한다. 일반 Windows Prototype 빌드 메뉴는 사용하지 않는다.
4. WASD 운전, Space 제동, 1/2/3 호출 선택, F 승하차, Q/E 깜빡이, C 시점, H 조향 보조, M 지도 전체/주변, Esc 일시정지. H를 켜도 가감속·신호 판단은 직접 한다.

지도 ZIP만으로 exe가 생기지는 않는다. 빌드 없이 바로 플레이하려면 기존 PC의 **`builds/EungyeTaxi` 폴더 전체**를 별도로 옮긴다. exe만 복사하면 실행되지 않는다. 게임 진행 저장은 이 묶음에 포함하지 않는다.

## 4. 개발을 이어가기

`docs/work-history/LATEST.md`와 개발 체크리스트를 읽고 다음 작업을 시작한다. 작업 결과와 검증 근거는 날짜별 이력으로 남기고 코드와 함께 커밋/푸시한다. 지도 원본부터 재수입하려면 `data/raw/ngii/`, OSM PBF 등도 별도로 옮겨야 한다. 이 ZIP은 현재 코드로 개발·빌드하기 위한 생성 입력 묶음이다.
