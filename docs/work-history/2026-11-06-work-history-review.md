# 2026-11-06 현재 작업 이력 파악

## 요청과 확인 기준

현재까지 이어진 작업 흐름을 파악하고, 사용자가 지정한 `docs/handoff/2026-11-06/README.md`를 인계 기준으로 읽었다. `LATEST.md`, `CURRENT-STATE.md`, 개발 체크리스트, 범위 문서, 콘텐츠 기획과 최신 구현 이력·Git 로그·관련 코드를 대조했다. 이번 작업은 현황 확인이며 신규 기능 구현이 아니다.

## 이어진 작업과 현재 기준

- 은계 도로/법규·3일 영업 기반에 승객 요청17종·특수11유형/34변주, 생활 시설·피로/청결·내구도/수리, 자체 도시 PVP와 LAN을 확장했다.
- 최근에는 운송/내비·조향/신호 개선, 사고 등급과 공업사, 승객 설정 관리창·선택지별 독립 효과, 밸런스 기본값 승격, 작은 방/첫 구매, 개인 운전 설정을 통합했다.
- 최종 UI 작업은 캐주얼 카드/초상 → 운전 수정 병합 → 생활거점 수리 레드닷 → 채움 아이콘20종·승차/도착 구분·초상 일관성·정산 생활거점 연결 순서다. 관련 실제 코드는 `CockpitPresentation.cs`, `RideCompletionPanel.cs`, `DrivingSettings.cs`, `PersonalRoomHUD.cs`에서 대조했다.
- 시작 로컬 브랜치는 `main`, HEAD는 `e0744c5`. 최신 게임 실행본 커밋은 `0bba941`, manifest 소스 기준은 `04e2e878c3c9505fffbf12c39eb475f2a47e8f67`이다. 이후 커밋은 게시 영수증과 자료/인계 보존이다. 이번 원격 조회나 pull/push는 수행하지 않았다.
- 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`의 은계 싱글/A2 PVP, LANv11. Unity2022.3.62f2 / `PrototypeBuild.BuildHybrid`가 개발 기준이며 A3·별도 PVP 실행폴더는 현재 배포 기준이 아니다.
- 작은 방·조명5,000원/러그8,000원은 구현됐다. 장기 차량 구매·이사·성장 단계는 기획이다. 일부 기획/체크리스트에는 작은 방 미구현, 안전 대체 승하차 미구현, 구 LAN/파일 수 등 과거 문구가 남아 있어 최신 구현 이력과 현재 코드를 우선한다. 문서 전면 정리는 이번에 수행하지 않았다.
- 사용자 후속 조정 `docs/handoff/2026-11-06/content-profile-revision6.json`은 출하 기본값과39항목이 다른 별도 보관본이다. 다른 PC에서는 설정 관리창의 가져오기 → 저장 → 게임에 적용이 필요하며 새 캠페인/새 방부터 적용한다. 이번 가져오기/적용이나 개인 저장 이전은 수행하지 않았다.
- UI 첨부22장·제안서 제작 자료 등117자료의 인계 목록과 최신5차23장 제안서는 지정 인계 문서에서 연결된다. 참여자·인력/예산·일정 확정은 별도 후속이다.

## 이번 수행한 검증

저장소 루트에서 아래 명령을 실제 실행했다.

- `pwsh -File tools/check_workspace.ps1 -Strict`: exit0, Input issues0. 이 PC에 생성 입력2개와 정식 프로젝트 지도·관련 설정이 존재하며 검사 통과. 프로젝트 지도 contentHash는 `35d1bc43d92c996d431acd0464bfc7a175b926cd1e35adb995e99dd7ab91cbac`다. Unity 설치 자체와 전체 형상 검증은 이 명령의 확인 범위 밖이다.
- `pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly`: exit0, `DISTRIBUTION_OK files=141 bytes=124559137 shippedBytes=124559137`.
- `pwsh -File tools/update_pvp_distribution_manifest.ps1 -VerifyOnly`: exit0, 동일 통합 실행폴더141파일 검증 통과.
- 시작/기록 작성 직전 `git status --short`: 기존 미추적10항목 확인. 라이선스/마인드맵/과거 이력·도구를 보존하며 이번 변경에 포함하지 않는다.
- 인계의6개 기준 커밋(`0bba941`, `4202dac`, `e230a3b`, `9b2628f`, `605a18c`, `d292508`)에 대한 `git merge-base --is-ancestor <commit> HEAD`: 모두 exit0, 현재 로컬 main 포함 확인.
- 기록/인계/설정 snapshot 링크3개 존재 확인 통과. `git diff --check`: exit0. 최종 커밋 전에는 새 기록을 포함한 staged diff도 확인한다.

직전 UI 기록의 Player174검사/60PNG·물리 검사·사용자 저장 불변은 이전 작업의 증거이며 이번에 재실행한 결과가 아니다. 새 Unity 빌드, 위치 제어 검사, 자동 물리 주행, 수동 플레이, 전체3일 캠페인, 다른PC/2인LAN·실물패드 검사는 수행하지 않았다.

## 다음 작업과 변경 파일

우선 특수승객·경유·취객 기상/준비 대기와 도착 직전 안내를 실제 주행으로 수용 점검한다. 이어 사람20분/전체3일의 조작감·생활 경제·방 구매 동기, 다른PC/LAN·패드 체감을 확인한다. 맵/차량 외형은 마지막 별도 검토다. 음악·에어컨·창문 실제 조작 보류와 추가 원격 개발 범위 제한은 유지한다.

이번 변경은 본 기록과 `docs/work-history/LATEST.md`의 인계 링크뿐이다. 게임 소스·실행본·지도·배포 manifest·사용자 설정/저장은 변경하지 않았다. 재확인은 위 Strict/VerifyOnly 명령을 사용한다. 기록 커밋은 `git log -1 --oneline -- docs/work-history/2026-11-06-work-history-review.md`로 실제 값을 확인한다.
