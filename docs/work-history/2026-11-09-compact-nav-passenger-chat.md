# 2026-11-09 작은 내비·반복 없는 승객 잡담

## 요구·구현

- 상단 내비를 중앙/상단 기준 85%로 축소했다. 다음 회전·거리·신호도 같은 변환을 사용하며 미니맵과 길찾기 모델은 그대로다.
- 사용자 지정: 선택지 없는 잡담은 왼쪽 아래에서 승객 발언으로 표시, 동일 문구는 한 운송에서 한 번만, 다른 문구는 기존 간격 유지, 대사 추가.
- 큰 무선택 대화창·큰 말 걸기 버튼을 제거하고 기존 승객 카드에 작은 ‘말 걸기’ 입력과 발언을 연결했다. 기사 질문을 카드 표시에서 제외한다. 자동 잡담은 추가하지 않는다.
- 기존 편집 가능한 기본 답변에 리소스 JSON의 7개 발언을 더해 일반 잡담 8개를 구성했다. 원래 12초 간격·6초 표시, 실제 요청/행동 선택지·Quiet 약속 위반 판정은 유지한다.
- 표시 문구로 중복을 판정하는 PassengerChatHistory를 추가하고 snapshot에 기록한다. 같은 문구가 다른 키에도 있어도 반복하지 않는다. 구저장은 마지막 답변을 사용 기록으로 이관하며 새 승객은 초기화한다.
- 1인칭 잡담 중 승객 카드는 왼쪽 아래, 운전 점수는 오른쪽 아래로 배치해 PVP 순위표·디지털 속도계와 겹치지 않게 했다. 평상시 1인칭 배치는 유지한다.

## 변경 파일

- `unity/EungyeTaxi/Assets/Scripts/PassengerChatHistory.cs`, `PassengerRequestUI.cs`: 중복 기록/저장 호환/기존 입력 권한과 효과 유지.
- `unity/EungyeTaxi/Assets/Resources/Content/passenger-chat-lines.json`: 추가 발언 7개. 기존 콘텐츠 프로필 형식이나 사용자 override를 교체하지 않는다.
- `CockpitUI.cs`, `DrivingReadability.cs`: 카드 입력/발언·시점별 배치와 내비 변환.
- `PassengerRequestFailureProbe.cs`, `BrightUIProbe.cs`, `tests/PassengerChatHistoryTests.cs`, `tools/test_passenger_chat_history.ps1`: 실제 복원·표시·중복·긴 대사·시점별 화면 검사.
- 설계/계획·승객 기획19·체크리스트·LATEST·기본 배포 실행 폴더/manifest를 함께 기록한다.

## 실제 검사·진행 기록

- 시작 HEAD `07ea5b8d41f7af2c5fb0dfbb8f18c41f510224e1`, 추적 미커밋 없음. 기존 미추적 자료는 보존했다. 별도 작업 브랜치 `codex/compact-nav-passenger-chat` 사용.
- 필수 입력 `tools/check_workspace.ps1 -Strict`: exit0/issues0. 지도/ZIP·로컬 입력 manifest 변경 없음.
- 순수 모델 검사: 새 모델 구현 전 컴파일 실패(CS2001), 구현 후 `PASSENGER_CHAT_HISTORY_OK checks=10` exit0. 대사 기준 중복·소진·구저장·신규 승객·4,000자·잘못된 이력을 확인했다.
- `FullUIBuild.Build` 2회 exit0: 기존 테마/브레이크22/실제 물리8/운전설정171·메뉴183/내비86/HUD 통합 및 BuildHybrid 통과.
- 첫 실제 요청 회귀는 검사 준비가 같은 운송에 Quiet 선택을 기록한 뒤 LocalChat으로 관찰을 덮어써 저장 ledger 검증에서 실패했다. Begin fixture가 한 종류만 선택하도록 고쳤으며 제품 검증을 완화하지 않았다.
- 수정 후 실제 Player `-singlePlayer -contentTest -passengerRequestFailureTest`: exit0/79검사. Quiet 중복/구저장, 일반 8개 각각 디스크 JSON 복원, 501/4,000자 사용자 대사와 잠든 승객 차단을 확인했다.
- 최종 UI 위치 수정 후 `PrototypeBuild.BuildHybrid`: exit0. 로그 `artifacts/compact-nav-chat/build-release.log`.
- 최초 숨김 화면 검사는 초기 캡처 8개가 검게 나와 전체 실패했다. 임시 캠페인 격리는 PASS, 이후 해상도 변경 후 캡처 일부는 정상이나 이 실행을 전체 UI 통과로 계산하지 않는다. 최종 실행본 화면 검사는 후속 기록한다.
- 독립 정적 검토에서 1인칭 PVP 순위표 겹침을 찾아 수정했고 재검토 추가 문제 없음. 정적 검토는 실제 플레이 검사가 아니다.

## 사용자 저장·남은 검사

검사 시작 보호 대상은 사용자 JSON/백업·콘텐츠 프로필 17개다. 사용자 기본 게임이 별도로 실행 중(PID39264/검사 인자 없음)이라 캠페인/백업/운전 기록 4개가 실제 플레이로 갱신됐다. 테스트 게임의 임시 캠페인 경로 격리는 확인했다. 사용자 플레이 자료를 과거 해시로 복원하지 않고 종료 후 새 기준을 잡아 최종 검증한다.

현재 최종 화면·LAN·manifest와 게시 영수증은 검증 후 아래에 추가한다. 사람 장기 운전/전체3일/다른PC LAN은 이번 자동 검사 범위가 아니다.

## 재현

기본 실행 `builds/EungyeTaxi/EungyeTaxi.exe` → 싱글 또는 A2 PVP → 승객 탑승 → 카드의 ‘말 걸기’. 기존 12초 간격으로 다른 발언을 확인하고 전체 8개를 사용하면 버튼이 사라지는지 확인한다. Quiet 수락 후 말을 걸면 기존 약속 위반이 기록되며 같은 반응은 반복하지 않는다. C로 두 시점의 왼쪽 아래 발언과 작은 상단 내비를 확인한다.
