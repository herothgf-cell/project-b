# 새 PC에서 빌드 없이 테스트

Windows 64비트 PC에서 저장소 main을 clone하거나 기존 작업을 보존한 뒤 `git pull --ff-only`한다. Unity Editor 설치와 지도 압축 해제 없이 **`builds/EungyeTaxi/EungyeTaxi.exe`**를 실행한다. 기본 맵은 시흥 은계지구다.

실행에 필요한 `EungyeTaxi_Data`, `MonoBleedingEdge`, `UnityPlayer.dll` 등은 같은 실행 폴더에 포함돼 있다. exe만 따로 이동하면 안 된다. GitHub Download ZIP을 사용했다면 저장소 ZIP 전체를 먼저 압축 해제한다. Git LFS는 사용하지 않는다.

- 조작: WASD 운전, Space 제동, 1/2/3 호출, F 승하차, Q/E 깜빡이, C 시점, H 조향 보조, G 전체지도, M 미니맵 범위, Esc 일시정지. H의 가감속·신호 판단은 직접 한다.
- 게임 진행 저장은 PC별로 유지되며 Git으로 옮겨지지 않는다.
- 수동 가속·저속 회전·일반 제동을 쉽게 조정했다. G 전체지도는 시간을 멈추고 G/Esc로 닫는다. 매너 게이지는 승객별, 법규 게이지는 오늘 기록이다. 보험료·세금은 프로토에서 제외했고 구 저장 비용을 이관한다.
- 빌드 파일 목록·크기·SHA256은 `distribution/EungyeTaxi-build-manifest.json`에서 확인한다. 실제 빌드 소스 커밋은 manifest의 `sourceCommit`을 따른다. Unity2022.3.62f2·은계 기본 빌드다.
- 현재 PC의 실행 폴더 복사본으로 기본 은계·7개 법규 구역·호출·미니맵 자동 검사 통과. 새 PC 하드웨어에서의 실제 구동은 별도 확인이 필요하다.

## 개발도 이어가려면

지도 입력 ZIP은 **`distribution/maps/Eungye-map-inputs-2026-10-20.zip`**에 있다. 저장소 루트에서 다음 명령을 사용한다.

```powershell
Expand-Archive -LiteralPath distribution/maps/Eungye-map-inputs-2026-10-20.zip -DestinationPath .
pwsh -File tools/check_workspace.ps1 -Strict
```

기존 지도를 수정한 경우 먼저 백업한다. 검사에서 `Input issues: 0`을 확인한 뒤 [Unity 준비·개발 안내](../docs/work-history/map-transfer-guide.md)를 따른다. ZIP 안의 안내는 최초 묶음 생성 당시 문서 사본이며, 최신 배포 방식은 이 문서를 우선한다. 원본 NGII/OSM 자료는 Git에 포함하지 않는다.
