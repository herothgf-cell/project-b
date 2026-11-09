# 2026-11-09 프로젝트 진행 상황 재점검

## 요구사항과 기준

프로젝트 진행 상황 확인 요청. 시작 main `c22fe6c`, `git status --short` 출력 없음. LATEST, 개발 체크리스트, 범위, CURRENT-STATE, 콘텐츠 기획13/21, PC 인계와 최신 배포 manifest를 대조했다. 게임 기능 변경이나 새 배포 요청은 아니다.

## 현재 진행 상황

- 기본 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`, 은계 싱글3일 / A2 자체 도시 PvP(봇 또는 LAN 최대4명), Unity2022.3.62f2·BuildHybrid·LANv16이다. A3는 현재 배포 지도가 아니다.
- 핵심 영업 루프, 승객 요청/특성/응대, 법규·사고·수리/생활거점, 비, 운전 설정, HUD/지도, 작은 방과 조명·러그 구매/저장이 구현된 프로토타입이다. 사람 수용까지 완료했다는 의미는 아니다.
- 최신 게임 소스548fe8b·출하a978b3b. 차로 표시/노면 경계, 런타임 서비스 목표 분할에 따른 주차장 안내 좌회전 교정이 최신이다. `EungyeConnectivityRepair.cs`의 서비스 차로/거리 재매핑과 `LaneFollowupBuild.cs`의 회귀 검사/BuildHybrid 연결을 확인했다.
- 직전 이력에는 실제 차로렌더31·좌회전34·전체UI169·동일PC LAN54/67 통과가 기록되어 있다. 이번에 다시 실행한 결과는 아니다.
- 사용자 마인드맵23646cc·제안서V6 편집0c490ba·PC 인계c22fe6c가 현재 main에 포함되어 있다. V8 제안서도 별도 보존되어 있어 제출본 버전은 별도 확인 대상이다.

## 실제 수행한 검증

| 명령 | 결과 |
|---|---|
| `pwsh -File tools/check_workspace.ps1 -Strict` | exit0, Input issues0. 정식 프로젝트 지도8fb257ed 및 필수 설정 일치 |
| `pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly` | exit0, DISTRIBUTION_OK files143 / bytes127378253 |
| `pwsh -File tools/update_pvp_distribution_manifest.ps1 -VerifyOnly` | exit0, 동일 통합 배포143파일 검증 |

재수입용 `eungye-map.json`/`route42-map.json` 2개는 선택 입력 부재로 표시된다. Git 정식 프로젝트 지도가 준비되어 일반 개발을 막는 필수 입력 오류는 없다. Unity Editor 실제 설치 여부는 Strict 검사 대상이 아니다.

## 남은 항목과 권장 순서

1. 사람 장시간 수동 주행/패드와 전체3일 캠페인: 가독성·승객 빈도·피로/청결·수익/작은 방 첫 구매 동기 관찰.
2. 실제 다른PC 간 LAN: 참가/운송/재접속 및 사람 최대4명 수용. 기존 loopback 통과와 구분한다.
3. 저사양 성능·LOD, 차량 실내/월드 세부 아트·승객 표정·정산 연출 개선.
4. 장기 경력·택시 구매/첫 이사 등 성장 확장은 후속 기획/미구현. 눈·결빙·동적 낮밤과 확률 미끄러짐 활성화도 현행 완료 범위가 아니다.

마인드맵의 구현50%/기획완료25%/기획필요0%는 노드 단계 표기이며 프로젝트 전체 완료율로 환산하지 않는다. 오래된 날짜별 문구는 최신 상단 안내 및 후속 이력과 함께 읽는다.

## 변경 파일과 미검증 범위

이 기록, `docs/work-history/LATEST.md`, `docs/prototype-development-checklist.md`만 갱신한다. 게임/빌드/지도/ZIP/사용자 저장은 변경하지 않았다. 새 Unity 빌드, 위치 제어 검사, 자동 물리 주행, 사람 수동 플레이, 전체 캠페인, 실제 다른PC LAN은 이번에 수행하지 않았다.

재현은 위 세 검사 명령을 저장소 루트에서 실행한다. 플레이는 기본 exe에서 싱글 또는 PVP를 선택한다. 다른PC 준비 및 revision7 선택 프로필은 `docs/handoff/2026-11-09/README.md`를 따른다.
