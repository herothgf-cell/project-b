# 새 PC에서 빌드 없이 테스트

Windows 64비트 PC에서 저장소 main을 clone하거나 기존 작업을 보존한 뒤 `git pull --ff-only`한다. Unity Editor 설치와 지도 압축 해제 없이 **`builds/EungyeTaxi/EungyeTaxi.exe`**를 실행한다. 기본 맵은 시흥 은계지구다.

실행에 필요한 `EungyeTaxi_Data`, `MonoBleedingEdge`, `UnityPlayer.dll` 등은 같은 실행 폴더에 포함돼 있다. exe만 따로 이동하면 안 된다. GitHub Download ZIP을 사용했다면 저장소 ZIP 전체를 먼저 압축 해제한다. Git LFS는 사용하지 않는다.

- 조작: WASD 운전, Space 제동, 1/2/3 호출, F 승하차, Q/E 깜빡이, C 시점, H 조향 보조, M 전체지도, +/- 미니맵 확대·축소, Esc 일시정지. H의 가감속·신호 판단은 직접 한다.
- 3일×12분·순수익30,000원. 탑승 중 N 마감은 막고 목적지 하차 후 정산한다. 새 캠페인 연료60%, 소모는 1km당4%포인트+1분당0.1%포인트다. 기존 저장 잔량은 유지한다.
- F7: 북부/남부/동부 주유소 게임시설 선택 → 입구까지 길 안내 → H 해제 후 수동 진입 → 같은 방향 주유 구역 정차 → F7 결제 → 별도 출구. 노란1·2·3 표시가 미니맵/전체지도에 있다.
- 게임 진행 저장은 PC별로 유지되며 Git으로 옮겨지지 않는다.
- 수동 가속·저속 회전·일반 제동을 쉽게 조정했다. M 전체지도는 시간을 멈추고 M/Esc로 닫는다. 시설·교통·법규 필터를 지원한다. 매너는 승객별이며 운행 점수는 날짜·재실행에 유지하고 새 캠페인에서만 초기화한다. 보험료·세금은 프로토에서 제외했고 구 저장 비용을 이관한다.
- 빌드 파일 목록·크기·SHA256은 `distribution/EungyeTaxi-build-manifest.json`에서 확인한다. 실제 빌드 소스 커밋은 manifest의 `sourceCommit`을 따른다. Unity2022.3.62f2·은계 기본 빌드다.
- 현재 PC의 실행 폴더 복사본으로 기본 은계·7개 법규 구역·9개 운송 코스·승객 요청·지도 필터를 검사한다. 최신 결과는 docs/work-history/LATEST.md를 따른다. 새 PC 하드웨어에서의 실제 구동은 별도 확인이 필요하다.

## 개발도 이어가려면

지도 입력 ZIP은 **`distribution/maps/Eungye-map-inputs-2026-10-20.zip`**에 있다. 저장소 루트에서 다음 명령을 사용한다.

```powershell
Expand-Archive -LiteralPath distribution/maps/Eungye-map-inputs-2026-10-20.zip -DestinationPath .
pwsh -File tools/check_workspace.ps1 -Strict
```

기존 지도를 수정한 경우 먼저 백업한다. 검사에서 `Input issues: 0`을 확인한 뒤 [Unity 준비·개발 안내](../docs/work-history/map-transfer-guide.md)를 따른다. ZIP 안의 안내는 최초 묶음 생성 당시 문서 사본이며, 최신 배포 방식은 이 문서를 우선한다. 원본 NGII/OSM 자료는 Git에 포함하지 않는다.

승차하러 가는 동안에는 별도 대기 시간이 흐른다. 연료15% 이하 경고와 점수 변화는 화면 상단에 표시한다. R/패드 복귀 단축키는 제거했으며 새 캠페인은 메뉴의 확인 버튼을 사용한다. 창 제목은 ‘택시 시뮬레이터’다.

전체지도: M으로 열고 +/- 버튼·키·마우스 휠로 확대/축소, 드래그로 이동. 내 위치/전체 보기 버튼 지원. 일반 차량48대. 직접 플레이 확인 목록: docs/prototype-playtest-guide.md.
