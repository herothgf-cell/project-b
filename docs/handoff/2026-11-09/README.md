# 2026-11-09 다른 PC에서 이어가기

## 시작 순서

저장소가 없다면 `git clone https://github.com/herothgf-cell/project-b.git` 후 해당 폴더로 이동한다. 저장소 루트에서 로컬 변경을 먼저 확인하고 진행한다. 변경/분기로 pull이 막히면 먼저 보존하고 강제 초기화하지 않는다.

```powershell
git status --short
git switch main
git pull --ff-only
pwsh -File tools/check_workspace.ps1 -Strict
pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly
```

- 실행: **`builds/EungyeTaxi/EungyeTaxi.exe`**. 폴더 전체가 Git에 포함돼 Unity 없이 실행할 수 있다.
- 개발: **Unity2022.3.62f2 + Windows Build Support**, 프로젝트 **`unity/EungyeTaxi`**, 빌드 **`PrototypeBuild.BuildHybrid`** / Taxi > Build Eungye Hybrid. PowerShell7 필요, 설정 관리창은 Node.js도 사용한다.
- 지도: **`config/maps/project/eungye.project.json`**이 정식 입력. contentHash `8fb257ed383a25782514bdcf7c203282d63ad3a65c17e5ef73cde147cc522d7b`. 재수입 JSON2개가 OPTIONAL absent여도 정식 지도가 정상이면 개발 가능하다. 일반 개발에 지도 재생성·ZIP 해제가 필요하지 않다.
- NGII/OSM 재수입에만 `distribution/maps/Eungye-map-inputs-2026-10-20.zip`을 저장소 루트에 푼다. ZIP 속 과거 manifest로 현재 manifest를 덮어쓰지 않는다. 원시 NGII/OSM은 별도 자료다. Unity Library/Temp는 복사하지 않는다.
- 은계 싱글/A2 자체 도시 PvP·**LANv16**. 함께 플레이할 PC 모두 최신 실행본으로 갱신한다.

## 포함된 작업과 읽을 자료

게임 소스 **548fe8b**, 검증 실행본 **a978b3b**, 게시 영수증 **a0ecc05**. 사용자 마인드맵 **23646cc**와 제안서V6 수정 **0c490ba**도 main에 포함된다. V7/V8은 별도 제안서이며 사용자가 편집한 V6를 덮어쓰지 않는다.

[차로/좌회전 이력](../../work-history/2026-11-09-lane-movement-roadview-followup.md), [로드뷰·기획21](../../content-design/21-lane-movement-roadview-followup.md), [이전 신호/승객 기획20](../../content-design/20-roadview-signals-passenger-feedback.md), [LATEST](../../work-history/LATEST.md)를 우선 읽는다. J866서향/J5057양방향 표시·녹색 편도1/노란 편도3·중복 노면/노면 밖 선·내비 캡션을 수정했다. 주차장 좌회전 누락은 런타임 차로 분할 후 서비스 목표 거리 변환을 고쳤다. 기존 신호 시간·금지·경로 검사는 유지한다.

## 최신 사용자 설정

`content-profile-revision7.json`은 현재 게임 적용 설정의 스냅샷이다. contentHash **`d22c06f57f400c4921d1466c05944a7f486cc038aab8ac5a0b4a5c6d79eb5b3a`**. 11/06 revision6보다 최신이며 출하 기본값과 구분한다.

다른 PC에서 같은 조정값을 쓰려면 **설정 관리.cmd → 가져오기 → 이 JSON 선택 → 저장 → 게임에 적용**. 대상 PC의 기존 프로필을 자동으로 덮어쓰지 않는다. 새 캠페인/새 PVP 방에 적용되고 기존 이어하기는 저장 당시 계약/설정을 유지한다.

개인 캠페인 진행·개인 운전 설정은 Git과 별도다. 진행까지 옮길 경우 게임 종료 후 기존 PC의 `%USERPROFILE%\AppData\LocalLow\Taxi Prototype`을 별도 백업·이동한다. 대상 PC의 기존 저장도 먼저 보존한다. 이 묶음에 개인 캠페인 저장은 넣지 않았다.

## 검증과 다음 작업

`review-evidence.zip`: 로드뷰·최종 실행 화면. `request-notes.md`: 최신 사용자 이미지별 요구와 위치. 첨부 원본7장의 Temp 파일은 이 인계 시점에 없어 ZIP에 포함하지 않았으며 기존 채팅과 요구 기록을 참고한다. `test-results.txt` / `release-validation.json`: 최근 실제 게임 검사의 요약. `files.json`: 인계 파일 및 ZIP 내부 크기/SHA256. 라이선스 정보가 있는 전체 Editor 로그·개인 저장·캐시는 포함하지 않는다.

게임 변경 때의 결과: BuildHybrid exit0, 차로렌더31/좌회전34/전체UI169, 같은PC LAN54/67 및 재접속 통과. 이번 인계에서 게임을 새로 빌드하거나 사람 주행을 재실행한 것은 아니다. 새로 수행한 검사는 [PC 이동 이력](../../work-history/2026-11-09-pc-transfer.md)을 따른다.

다음은 **사람 장기 주행·전체3일 캠페인·실제 다른PC LAN** 확인이다. 확정된 새 기능 구현 대기는 없다. 다음 피드백 전 임의로 신호 시간/합법 경로/밸런스를 바꾸지 않는다. 기존6유턴 조건·24승하차지/552방향 경로·3생활시설을 보존한다. J866 원본 중앙선/유턴 개구부/반대편 연석 유도선은 중복 임시선과 구분한다.

## 새 채팅에 전달할 문구

> AGENTS.md, docs/handoff/2026-11-09/README.md와 docs/work-history/LATEST.md를 읽고 이전 PC 작업을 이어줘. git status로 기존 변경을 확인하고 Strict 및 배포 manifest를 검증해줘. 최신 은계/A2 통합 실행본·LANv16과 사용자 마인드맵/제안서V6 수정을 유지해줘. 지도 재생성이나 개인 저장 초기화 없이 다음 피드백 작업을 이어가면 돼.
