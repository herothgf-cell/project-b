# 도로 캡처 주석 편집과 이미지 붙여넣기 수정

## 사용자 요청·원인
- Ctrl+Z, 텍스트 영역을 눌러 수정, 글씨 크기에 맞는 영역, 채팅 이미지 붙여넣기 수정.
- 사용자가 확인한 증상은 이미지 복사 완료 후 Ctrl+V하면 좌표 텍스트만 붙는 것이었다. 기존 코드는 PNG/CF_DIB와 CF_UNICODETEXT를 함께 제공했다. 이미지 복사에서 텍스트 형식을 제거하고 위치 정보는 이미지 하단에 유지한다. 지점 클릭의 텍스트 복사는 별도로 유지한다.
- 기존 텍스트 영역은 430×64 고정, 선택은 시작점 반경36px였다. 렌더와 선택 모두 실제 글자 측정값+작은 여백을 사용한다.

## 구현
- `RoadScreenshotEditor`: Ctrl+Z 이벤트 처리. 주석 추가/텍스트 변경/전체 삭제의 이전 상태를 깊은 복사하여 되돌린다. 획의 중간 점은 별도 이력으로 쌓지 않는다.
- 도구와 무관하게 텍스트 사각형 안을 누르면 텍스트 도구와 입력란으로 포커스를 옮겨 바로 수정한다. 영역은 CalcSize/CalcHeight로 계산하고 화면 가장자리 안으로 제한한다.
- `RoadImageClipboard`: 이미지 복사는 PNG와 CF_DIB만 제공한다. 실제 PNG 바이트를 읽는 검사 경로를 추가했다.
- 복사 중 도구만 비활성화하며 캡처의 글씨·위치 설명 색상은 유지한다.
- 회귀 검사: `Assets/Editor/RoadAnnotationEditTests.cs`, 실제 실행 검사: `Assets/Scripts/DevRoadOverviewProbe.cs`. Assets 경로는 `unity/EungyeTaxi/` 기준.

## 검증
- RED: `RoadAnnotationEditTests.Run` 종료1, 새 키보드/영역 편집 메서드 없음 확인(`artifacts/color-road-survey/annotation-red.log`).
- 최초 GREEN: 새 편집4+기존 제보4+뷰6 모델 검사 통과, BuildHybrid 종료0(`annotation-build.log`).
- 실제 사용자 Windows 창에서 Player16검사 통과(`annotation-player.log`): 텍스트 오른쪽 끝 선택, 작은 영역, Ctrl+Z 텍스트 복원, 전체 삭제 복원, 이미지 복사에 텍스트 형식 없음, PNG 재해독과 캔버스 크기 일치, 기존 설정/카메라/일시정지 보존.
- 클립보드에서 직접 읽은 `artifacts/color-road-survey/dev-overview-player/artifacts/dev-overview/clipboard-report.png`를 확인하다 복사 중 글자 흐려짐을 발견해 보정했다. 최종 빌드/검증 결과는 아래에 기록한다.
- 이 채팅창에 직접 Ctrl+V하는 검사는 도구의 창 제어/Chrome 연결 제한으로 수행하지 못했다. PNG 형식 존재 여부만 확인했던 이전 검사와 달리 실제 바이트의 해독 및 화면도 확인한다.

## 실행과 보존
`builds/EungyeTaxi/EungyeTaxi.exe` → 설정 → 개발용 테스트 → 도로 전체 보기 → 현재 화면 캡처·편집.
텍스트 어느 부분이든 클릭해 입력하거나 Ctrl+Z로 이전 작업을 복원한다. 이미지 복사 후 채팅에 Ctrl+V.
지도 r45·필수 입력 변경 없음. 사용자 Scene/PackageManagerSettings, content/, 기획 문서와 LATEST의 기존 우선순위 검토 문단은 보존한다. 캠페인·다른 PC·수동 장시간 편집 검사는 범위 밖이다.

## 최종 결과
- `RoadAnnotationEditTests.Build`: 모델14검사·`PrototypeBuild.BuildHybrid` 종료0 (`artifacts/color-road-survey/annotation-final-build.log`).
- 일반 사용자 창 실행본 `DEV_OVERVIEW_PLAYER_OK checks=16` / `PASS checks=16` (`annotation-final-player.log`). 클립보드 PNG 재해독 파일에서 주석이 선명한 노랑이고 위치 설명이 정상 색상임을 직접 확인했다.
- 사용자 Scene/PackageManagerSettings SHA256은 작업 전과 일치한다. 로컬 빌드 반영, 이번 후속 변경은 아직 원격 푸시하지 않는다.

- 배포 manifest: 소스 c318e52, 143파일·133,382,345바이트 및 SHA256 검증 통과. 실행 경로 builds/EungyeTaxi/EungyeTaxi.exe.
