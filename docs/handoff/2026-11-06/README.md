# 2026-11-06 다른 PC 작업 인계

## 바로 시작

1. 저장소의 기존 변경을 확인하고 `git switch main`, `git pull --ff-only`를 실행한다. 로컬 변경/분기가 있으면 강제 초기화하지 말고 먼저 보존한다.
2. 플레이만 하려면 `builds/EungyeTaxi/EungyeTaxi.exe`를 실행한다. 실행 폴더 전체가 필요하며 Unity나 지도 ZIP 설치는 필요 없다.
3. 개발은 [PC 준비 안내](../../work-history/README.md)에 따라 Unity2022.3.62f2 + Windows Build Support, PowerShell7을 준비한다. Unity 프로젝트는 `unity/EungyeTaxi`, 빌드는 `PrototypeBuild.BuildHybrid`다. 저장소 루트에서 `pwsh -File tools/check_workspace.ps1 -Strict`를 먼저 실행한다.
4. [LATEST](../../work-history/LATEST.md)와 [현재 상태](../../CURRENT-STATE.md)를 읽는다. 기본 은계 싱글/자체도시A2 PVP·LANv11이다. 다른 PC와 LAN을 쓰면 양쪽 실행본을 함께 갱신한다.

## 오늘 채팅별 작업과 자료

| 채팅 | main 포함 확인 | 자료 / 이어갈 항목 |
|---|---|---|
| 현재 구현에 맞춰 UI 시안 조정 | 실행본0bba941, 인계e2c4cae | [UI/운송174검사 기록](../../work-history/2026-11-06-filled-icons-trip-flow.md), [실제 화면](../../ui/2026-11-06-filled-icons-trip-flow/), [첨부 원본22장](ui-reference/). 다음은 특수승객·경유·기상/도착직전 안내. 맵/차량 외형은 마지막. |
| 다른 PC 작업 내용 확인 | 브레이크/좌회전4202dac 포함 | [통합 이력](../../work-history/2026-11-06-merge-driving-with-main-ui.md), 신호 잔여초와 개인 운전 설정 유지 |
| 프로젝트 전체 점검 | 작은 방 실행본e230a3b 포함 | [생활 기획](../../content-design/), [구현 이력](../../work-history/2026-11-06-personal-room-implementation.md). 조명/러그는 구현, 장기 차량 구매/이사는 기획 |
| 마인드맵·미구현 사항 정리 | 9b2628f 포함 | [마인드맵](../../content-design/택시%20시뮬레이션.mmst), [목차](../../content-design/택시%20시뮬레이션-목차.md) |
| 프로젝트 제안서 PPT 제작 | 605a18c 포함 | [최신5차23장 PPT](../../proposals/project-b-game-proposal-v5.pptx), [검토표](../../proposals/proposal-v5-review.md), [시연 매뉴얼](../../proposals/project-b-prototype-manual-v3.md). 실제 참여자 이름/인원 확인 필요 |
| 승객 상호작용 조정 툴 추가 | d292508 기본값 승격 포함 | [툴 안내](../../content-tuning-tool-guide.md), 아래 사용자 후속 조정 snapshot. ETA/제시간 판정 의미는 [기록](../../work-history/2026-11-06-request-outcomes-audit.md) |

## 최신 사용자 조정 설정 가져오기

[content-profile-revision6.json](content-profile-revision6.json)은 오늘 관리창에서 추가로 조정하고 게임에 적용한 설정이다. revision6, contentHash `09f70dd02cf676b36d6259cf77eaa4c7572517461de110317f196d2220fcdb84`. 원본 PC 저장 프로필과 active-profile의 hash가 동일하고 현재 catalog 검증을 통과했다. 출하 기본값과39항목이 다르다.

다른 PC에서 저장소의 `설정 관리.cmd`를 실행하고 **가져오기 → 위 JSON 선택 → 저장 → 게임에 적용**한다. 다음 새 캠페인/새 방부터 적용되며 기존 이어하기는 저장 당시 설정을 유지한다. 이 파일을 보관하기 위해 출하 기본값을 변경하거나 기존 개인 설정을 자동 덮어쓰지 않았다. 관리창의 ‘변경됨’은 미저장이 아니라 기본값과 다름을 뜻한다.

## 시안·제작 자료

- `ui-reference/`: 이 UI 채팅 첨부 원본22장. 그림에 있는 기능은 구현 명세가 아니며 현재 기능과 대조해야 한다. 실제 실행 캡처는 `docs/ui/`의 날짜별 폴더에 있다.
- `proposal-v3/`, `proposal-v4/`, `proposal-v5/`: 제작 스크립트, outline, validation, 검사 자료와 슬라이드 PNG. 최종 PPT와 원본 생성 시안/프롬프트는 `docs/proposals/`에 이미 포함된다.
- 제작 스크립트는 당시 원본 보존본이다. C:/git 경로 및 Codex artifact-tool 설치 위치가 들어 있으므로 다른 PC에서 재생성할 때 환경에 맞춰 조정한다. 일반 편집은 PPTX를 직접 연다. 캐시 런타임 자체는 배포하지 않는다.
- `historical-copies/`: 원본 PC에 남아 있던 마인드맵 복사본/목차·10월26일 점검. 현재 구현 기준으로 쓰지 않는다.
- `historical-content-tuning-workspace.zip`, `historical-steering-workspace.zip`: 오래된 작업 트리의 미커밋 소스 patch와 미추적 제작 파일. 기준 commit은 ZIP의 BASE_COMMIT.txt에 있다. 이미 후속 main으로 통합된 과거 후보이므로 통째로 적용하지 않는다. 별도 복구/차이 조사용이다. 오래된 바이너리·Library·임시 캐시는 제외했다.
- [files.json](files.json): 새로 보존한117파일, 총69,520,539바이트의 출처·크기·SHA256 목록. 첨부 누락0.

## 검증과 미검증

이번은 자료/인계 게시이며 게임 코드를 바꾸지 않았다. 최신 통합/PVP manifest 전수검증은141파일124,559,137바이트 일치. 자료117파일의 크기/SHA256, ZIP CRC, 조정 프로필을 검증했다. 실제 다른 PC 실행/재생성은 아직 하지 않았다. 직전 UI 빌드의174검사 결과를 이번에 새로 실행한 것으로 간주하지 않는다.

개인 캠페인 저장·운전 설정은 Git으로 자동 동기화되지 않는다. 게임 진행까지 옮기려면 기존 PC 저장을 따로 백업해 이동해야 한다. 원본 NGII/OSM은 지도 재수입 때 별도 준비하며, 일반 개발/실행은 배포 지도와 프로젝트 지도 안내를 따른다.
