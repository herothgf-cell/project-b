# 개발용 도로 지점 제보와 이미지 주석

## 요청과 구현
- 개발용 도로 전체 보기에서 클릭한 지점의 지역·지도 x/z·가까운 교차로 ID/거리 복사. 택시 위치나 주행 방향으로 대체하지 않는다.
- 6px 초과 드래그는 클릭과 구분하며 원위치로 돌아와 놓아도 복사하지 않는다.
- 현재 화면 캡처·편집: 도로 렌더를 고정하고 펜, 화살표, 한글 텍스트, 색상, 되돌리기/모두 지우기를 제공한다. 텍스트를 다시 클릭해 내용을 수정한다.
- 이미지 복사는 도로·주석·지점 표식·위치 정보를 포함하고 편집 버튼은 제외한다. 선택점이 화면 밖이면 현재 화면 중앙을 사용한다.
- Windows PNG/CF_DIB와 위치 텍스트를 함께 기록한다. 실패 시 재시도 안내. 이미지 주석은 지도 데이터를 바꾸지 않는다.
- Esc는 편집→도로 전체 보기→설정 순서이며 설정 임시값과 일시정지를 유지한다.

## 변경 파일
- `Assets/Scripts/DevRoadOverview.cs`, `DrivingSettingsPanel.cs`: 클릭 제보와 편집 진입/복귀.
- `RoadReportGesture.cs`, `RoadScreenshotEditor.cs`, `RoadImageClipboard.cs`: 제스처, 주석, Windows 클립보드.
- `Assets/Editor/DevRoadReportTests.cs`, `Assets/Scripts/DevRoadOverviewProbe.cs`: 모델 및 실제 실행본 검사.
- 위 Assets 경로는 `unity/EungyeTaxi/` 기준이다. UI 설계, 개발 체크리스트, 배포 실행 폴더와 manifest를 함께 갱신한다.

## 검증
- RED: 구현 전 `DevRoadReportTests.Run`은 제스처 타입 누락으로 종료1 (`artifacts/color-road-survey/dev-report-red.log`).
- GREEN: `DevRoadReportTests.Build`에서 새 모델4 + 기존 뷰 모델6 통과, `PrototypeBuild.BuildHybrid` 종료0 (`dev-report-build3.log`).
- 직접 실행한 테스트 프로세스에서는 클립보드가 빈 값이었다. Windows OpenClipboard 진단이 access denied(오류5)를 반환하여 일반 Explorer 사용자 창 실행과 분리해 검증한다. 좌표 계산 자체는 x640/z260, J6480_2573/8.4m로 정상이었다.
- Explorer의 일반 사용자 창에서 실제 Player 12검사 통과(`dev-report-interactive.log`, `DEV_OVERVIEW_PLAYER_OK checks=12`). 위치 텍스트를 Windows에서 다시 읽어 일치 확인, PNG/DIB 두 이미지 형식 확인, 주석/되돌리기/Esc/설정 임시값/주행 카메라·일시정지 보존 및 작은 창 재진입 검사.
- 실제 `artifacts/color-road-survey/dev-overview-player/artifacts/dev-overview/annotated-report.png`에서 한글 주석·화살표·위치 표식·좌표/교차로 정보의 배치 확인. 자동 UI 상태 검사이며 사람의 장시간 수동 편집 테스트는 아님.

## 실행·재현
1. `builds/EungyeTaxi/EungyeTaxi.exe` → 설정 → 개발용 테스트 → 도로 전체 보기.
2. 휠 확대/드래그 이동 후 문제 지점 클릭. 좌표와 교차로 정보가 복사된다.
3. 현재 화면 캡처·편집 → 펜/화살표 또는 텍스트 입력 후 이미지 클릭.
4. 이미지 복사 → 채팅에 Ctrl+V. 텍스트 주석 수정은 텍스트 도구로 해당 글씨 시작 부분을 클릭한다.

## 경계와 보존
- 지도 r45와 필수 로컬 입력 변경 없음. 주행·경제·3일 캠페인·다른 PC LAN 검사는 이번 범위가 아니다.
- 사용자 Scene, PackageManagerSettings, 배포 content/ 및 기획 문서 변경을 보존한다. LATEST의 사용자 우선순위 검토 문단도 별도 변경으로 남긴다.
- 클립보드 메모리 소유권은 [Microsoft SetClipboardData](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setclipboarddata) 규칙에 따라 성공 시 OS에 넘기고 실패한 핸들만 해제한다.

## 로컬 빌드 정리
- 소스 커밋 `e80e56c` 확인. 배포 manifest 143파일, 133,379,785바이트와 SHA256 검증 통과(`DISTRIBUTION_OK`). 사용자 content/는 배포 목록에 추가하지 않았다.
- 소스/실행본은 로컬 main에 기록한다. 이번 요청에서 원격 push는 수행하지 않았다.
- 실제 Windows 이미지 형식 등록과 위치 텍스트 readback은 검증했고, 채팅 앱에 사람이 직접 Ctrl+V하는 검사는 남아 있다.
