# 최신 작업 이력

이 파일은 **현재 기준 + 최근 기록**만 둔다. 2026-11-10 이전 전체 원문은 [보관본](LATEST-archive-until-2026-11-09.md)에 있다. 프로젝트 전체 상태는 [CURRENT-STATE](../CURRENT-STATE.md)를 먼저 읽는다.

## 현재 기준 (2026-11-10)

| 항목 | 상태 |
|---|---|
| 실행 | `builds/EungyeTaxi/EungyeTaxi.exe` — 은계 싱글 3일 / A2 자체 도시 PvP(봇3 또는 LAN 최대 4명) |
| 빌드 | Unity 2022.3.62f2, `PrototypeBuild.BuildHybrid`. **브랜치 `feature/vehicle-purchase-3day`**: 게임 소스 c3fd1d1 · 출하 30bce76 (11/10, main 미병합). main 기준은 소스 548fe8b · 출하 a978b3b (11/09) |
| 통신 | 브랜치 LAN **v17**(내 차 외형) / main LAN v16. 양쪽 PC 같은 실행본 필요, v16↔v17 접속 거부 |
| 배포 검사 | 브랜치 출하 143파일 127,430,378바이트, 통합/PvP manifest VerifyOnly 통과(11/10 kimhs-NW2). main은 127,378,253바이트 |
| 필수 로컬 입력 | 정식 지도 `config/maps/project/eungye.project.json`(Git) 준비됨, 11/10 Strict issues 0(PowerShell 7 필요, 5.1에서는 검사 스크립트 실패). 재수입용 JSON 2개는 선택 입력이며 없음 |
| 기획 | [기획22](../content-design/22-growth-direction-and-vehicle-purchase.md) **구현(사람 검증 전, 브랜치)** · 평점 보류 · [기획23](../content-design/23-license-career-path.md) 면허 경로 본편 보류 |
| 제안서 | [V9 28장](../proposals/project-b-game-proposal-v9.pptx) · [반영 검토](../proposals/proposal-v9-review.md) |

## 다음 작업

1. `feature/vehicle-purchase-3day` 검토 후 main 병합 여부 결정(병합하면 LAN v17, 모든 PC 실행본 갱신).
2. [거리뷰 대조 수정 후보](2026-11-10-roadview-lane-audit.md#수정-후보와-우선순위-사용자-확인-전-미적용) 사용자 선택(스쿨존 제한속도, 유턴 표시↔연결, 미확인 교차로).
3. 다른 PC LAN v17, 첫 이용자 안내.
5. 보류: 기사 평점(기획22 §3-1), 개인택시 면허 경로([기획23](../content-design/23-license-career-path.md)), Blender 아트 파이프라인 2단계 이후([문서](../proposals/art-blender-pipeline.md)).

## 2026-11-10 은계 도로·차로 거리뷰 대조와 시스템 점검 (조사만)

[결과·수정 후보](2026-11-10-roadview-lane-audit.md) · [게임 렌더](../ui/2026-11-10-roadview-audit/). 내장 브라우저가 map.naver.com을 차단해 저장소에 기록된 파노라마 26개 이미지로 대조(7곳 재확인, 나머지 이전 기록 인용). 차로 수·좌회전·유턴 표시는 기존 반영 구간에서 대체로 일치. 주요 발견: 기본 지도 제한속도 표지 0개라 과속 판정 미작동(실제는 은계로 일대 스쿨존30), J866·J5057 유턴은 표시되나 조건 표지가 없어 유턴하면 항상 위반(코드 확인), 은계로 중앙분리대가 노란 실선. 대은로·은계로 북쪽 등 7곳 이상 미확인. 실행본 렌더 checks=31 PASS. 지도·코드·빌드·저장 변경 없음. 수정은 사용자 확인 후.

## 2026-11-10 기획22 구현: 차량 교체·차고·PvP 내 차 (브랜치)

[작업 이력](2026-11-10-growth-decisions-mindmap.md). 사용자 결정 4건: 차종 특성 채택, 기사 평점 기획만·구현 보류, 캠페인 3일 유지(3일 안 차량 교체로 가격 조정), 면허 경로 본편 보류(기획23·마인드맵 할거리). 기획22 개정·기획23 신규, 마인드맵 14노드 추가·3노드 25% 전환(`tools/update_growth_mindmap.py --check` 통과, 원본 백업 `design-history/content-design/2026-11-10-mindmap-before-growth.mmst`), 제안서 V9 문구 갱신, Blender 파이프라인 2단계 이후 보류. 게임 코드·빌드 변경 없음. Git 커밋 안 함.

## 2026-11-10 아트 Blender 파이프라인 1-4 환경 확인

[결과](2026-11-10-art-pipeline-env-check.md) · [파이프라인 문서 1-4](../proposals/art-blender-pipeline.md). 읽기 전용 확인: Blender 5.2.3 LTS·MCP 응답 ok·씬 `Scene`(Camera/Cube/Light, 미저장·변경 없음)·EEVEE 사용 가능(렌더 미실행), RTX 5060 Laptop 8GB VRAM, C: 342.5GB 여유, Unity MCP 3.4.11 응답·EungyeTaxi 인스턴스 idle. MCP 도구가 세션에 노출되지 않아 9876 소켓·8080 HTTP로 직접 조회. 씬/코드/빌드 변경 없음, Git 커밋 안 함.

## 2026-11-10 기획 V9·성장 방향·문서 정리

[작업 이력](2026-11-10-proposal-v9-growth-docs.md). Cowork에서 기획·문서만 작업. 성장 구조 검토와 차량 구매·싱글 차량 PvP 활용 기획(기획22), 아트 전환 시안 목록·프롬프트·이미지 생성 도구 설치 가이드와 `tools/generate_concept.py`, 제안서 V9(28장), CURRENT-STATE 한 장 요약과 LATEST 보관본 분리. 게임 코드·빌드·지도·저장 변경 없음, 새 플레이·Strict·빌드 검사 없음. 로드맵·참여자 슬라이드는 사용자 지시로 유지. **Git 커밋은 이 세션에서 하지 않았다.**

## 2026-11-10 진행 상황 확인 (PC kimhs-NW2)

[결과](2026-11-10-progress-check.md). 11-09 이후 MCP 읽기 기록 2건 외 게임/빌드/지도 변경 없음, 배포 은계/A2·LANv16·BuildHybrid·548fe8b/a978b3b·지도8fb257ed 유지. 이 PC에 PowerShell 7.6.9를 winget으로 설치(PS5.1에서는 검사 스크립트 실패) 후 Strict exit0/issues0, 통합·PVP VerifyOnly exit0(143파일127378253바이트). 다음: 사람 장기/전체3일·다른PC LAN 수용. 로컬 문서 커밋 미push.

## 2026-11-09 Blender·Unity MCP 직접 호출 검사

[도구·실제 응답·오브젝트 목록](2026-11-09-blender-unity-mcp-test.md). Blender get_objects_summary status=ok/isError=false, Scene의 Camera/Cube/Light3개. Unity manage_scene get_active/get_hierarchy success=true/isError=false, EungyeTaxi 인스턴스1개·이름/경로 없는 씬의 Main Camera/Directional Light2개. 연결 실패 없음·기존 파일/씬/코드 미변경·기존 미추적 설정 보존. 배포 은계/A2·LANv16·BuildHybrid·548fe8b/a978b3b와 직전 Strict issues0 유지(이번 빌드/입력 재검사 없음). 다음은 요청 시 게임 씬 조회·기존 사람 장기/전체3일·다른PC LAN 수용.

## 2026-11-09 Unity MCP 읽기 전용 상태 확인

[실제 연결·활성 씬·계층](2026-11-09-unity-mcp-readonly-check.md). 로컬 MCP3.4.11·EungyeTaxi/Unity2022.3.62f2 인스턴스1개 응답, idle/ready·Play/컴파일 아님. 활성 씬은 이름/경로 없는 Untitled, Main Camera·Directional Light 두 루트/자식0. 씬/오브젝트/스크립트 미변경·기존 미추적 PackageManagerSettings 보존. 배포 은계/A2·LANv16·BuildHybrid·548fe8b/a978b3b 및 직전 Strict issues0 유지(이번 재검사 없음). 다음은 요청 시 게임 씬 열기/조회, 기존 사람 장기/전체3일·다른PC LAN 수용.

## 2026-11-09 프로젝트 진행 상황 재점검

[현황·이번 검사·우선순위](2026-11-09-progress-check.md). 시작 main c22fe6c·작업 트리 깨끗함. 이번 Strict exit0/issues0·통합/PVP VerifyOnly 각각 exit0, 출하143파일127378253바이트 확인. 은계/A2·LANv16·BuildHybrid·소스548fe8b/출하a978b3b 유지. 정식 지도8fb257ed 준비됨·재수입 JSON2개는 선택 입력 부재. 게임/빌드/지도/저장 변경 및 새 플레이 없음. 다음은 사람 장시간/패드·전체3일과 첫 구매 동기, 실제 다른PC LAN, 저사양 성능·아트 수용. 장기 경력/차량 구매·이사는 후속 범위다.

## 2026-11-09 PC 교체 인계

[다른 PC 시작·최신 설정·검증 자료](../handoff/2026-11-09/README.md) · [실제 준비/검사 이력](2026-11-09-pc-transfer.md). 사용자 마인드맵23646cc·제안서V6 0c490ba 보존, 최신 설정revision7/검증 요약/로드뷰7·실제 렌더8장·요청 위치 메모를 Git 인계한다. 새 Strict issues0·통합/PVP manifest 검증0, 출하143파일127378253바이트·선택 프로필 별도. 은계/A2·LANv16·BuildHybrid·정식 지도8fb257ed·게임 소스548fe8b/출하a978b3b 유지. 일반 개발은 pull 후 정식 지도로 진행하며 캐시/원본 지도 재생성 불필요. 개인 진행 저장·원시 NGII/OSM은 별도, 다음은 사람 장기/전체3일/다른PC LAN 수용. 최근 실제 구현은 아래 이력을 따른다.

## 2026-11-09 차로별 표시·주차장 안내 좌회전 후속

[요구·로드뷰·실제 검사·배포 인계](2026-11-09-lane-movement-roadview-followup.md) · [기획21](../content-design/21-lane-movement-roadview-followup.md). J866서향/J5057양방향32조각·12역할, 녹색 편도1/노란색 편도3 확인·겹친 노면/경계 교정, 내비 외형 캡션 제거. 실제 M지도 주차장 목표 분할 오류 RED34중18실패→GREEN34중0실패. 정식 지도8fb257ed·원본/주행망/신호/기존시설 보존·24정류장552경로/6유턴/3생활시설·Strict issues0. 최종 BuildHybrid exit0·차로렌더31/좌회전34/전체UI169/LAN54·67 통과·보호19파일 불변. 소스548fe8b·출하143파일127378253바이트·통합/PVP manifest 전수 검증0. 출하a978b3b를 기존 main 보존 fast-forward·일반 push하고 원격 동일 해시 확인, 영수증 문서는 연결 이력 참조. 은계/A2 통합exe·LANv16·원본 지도ZIP 유지. 다음은 사람 장기/전체3일/다른PC 수용이다.

## 2026-11-09 거리뷰 신호·차선·승객 응대

[구현·근거·최종 검사](2026-11-09-roadview-signals-passenger-feedback.md) · [접근별 기획20](../content-design/20-roadview-signals-passenger-feedback.md). 좌회전 수요69·접근별4/4/3/3·NGII 좌1/직2 표시/중복선 교정, 점수 알림 내비 아래 배치·NPC 반응/바깥 따옴표 제거·주행 요청/가중치 상향. 기본 BuildHybrid exit0·실제 신호48/전체UI169/요청·복원66+6/LAN54+67·552방향 경로·Node100·모델158 통과. 사용자 종료 후16저장 불변·프로필6대상5실제값만 변경, 변경 후17보호파일 불변. 은계/A2·LANv16·기존 지도 baseline/graph/scene/신호 프로그램/ZIP 보존, 프로젝트 시설 메타데이터만 갱신·필수입력 Strict issues0. 출하143파일/127,323,898바이트·통합/PVP manifest 전수 검증0·소스dfdea96. 기존 main/제안서 작업 보존·출하906a89a 일반 push/원격동일해시 확인, 영수증은 연결 이력 참조. 다음은 사람 장기/전체3일/다른PC·상가 서향 전면 별도 확인. 기존 캠페인 설정/계약은 보존하며 새 가중치는 새 캠페인/새PVP방 기준.

## 2026-11-09 제안서 V8·시스템과 최신 화면

[26장 PPT](../proposals/project-b-game-proposal-v8.pptx) · [반영 검토](../proposals/proposal-v8-review.md) · [이력](2026-11-09-game-proposal-v8.md). USP와 재미 연결·경쟁작 한곳/유로트럭2 성장참고 설명, 시스템6영역·싱글/PvP 각페이지·11.09 실제UI와 새 둥근택시 시안2장.26장 렌더/구조·레이아웃/재가져오기 통과. 게임/빌드/지도/ZIP/저장 미변경·새플레이/Strict 없음. 은계/A2·LANv16·BuildHybrid와 필수입력 상태는 아래 게임 이력 인용. 참여자 미정·인력/일정 가정 유지. 다음은 회사 제출 전 실제 참여자·확정 범위/예산 검토.

## 2026-11-09 제안서 7차 재구성

[25장 PPT](../proposals/project-b-game-proposal-v7.pptx) · [구성 검토](../proposals/proposal-v7-review.md) · [이력](2026-11-09-game-proposal-v7.md). 사용자 편집 V6 보존, 개요2페이지부터 USP/재미/참여자 배치 후 게임성·세계관·시장·로드맵 순서로 정리. 제목 장식·현재 택시 기반 표지 생성, 별도 Manual 연결.25장 렌더/구조·레이아웃/재가져오기 통과. 참여자 미정·일정/인력 가정. 게임/빌드/지도/ZIP/저장 미변경·새 플레이/Strict 없음. 현재 은계/A2·LANv16·BuildHybrid·필수 로컬 입력 상태는 아래 게임 배포 이력을 따른다. 다음은 회사 제출 전 참여자·범위/인력/예산 확정.

## 2026-11-09 승객 이름·말 걸기 숨김·호출 다양성

[최종 구현·검증·배포 인계](2026-11-09-passenger-cards-call-variety.md). 사용자 후속에 따라 말 걸기 UI/답변 숨김, 이름/화자 표기 정리, M지도 상단 보조/우측 예측 카드 제거, 내비85% 유지. 기존 합법 후보/대표 코스 안에서 직전 호출판 반복을 우선 회피한다. 최종 화면161/실제 생성96판968검사(14~16곳·직전판반복0)/요청·복원79, 모델593/표시16/수요38·BuildHybrid 통과. LAN54/67·Strict issues0 통과, 출하144파일127,416,554바이트·통합/PVP manifest 전수 검증0. 소스35b85a9·출하5dcaf3a를 main에 일반 push하고 원격 동일 해시 확인. 문서 영수증 후속/정확한 인계는 연결 이력을 따른다. 종료 후 보호17파일 불변 확인, 지도/ZIP불변·은계/A2 통합exe·LANv16. 사람 장기/전체3일/다른PC는 후속.
## 2026-11-09 작은 내비·반복 없는 잡담

[구현·실제 검사·배포 인계](2026-11-09-compact-nav-passenger-chat.md). 상단 내비85%, 선택지 없는 잡담은 왼쪽 아래 카드·기본8개·운송별 동일 대사1회/저장 이관. 실제 요청/Quiet 효과 유지. 모델10·Player79·BuildHybrid exit0, 필수지도 Strict issues0·지도/ZIP불변. 최종 화면/사용자 플레이 종료 후 저장 보존·배포 검증 진행 중. 은계/A2 통합exe·LANv16 유지. 사람 장기/전체3일/다른PC는 후속.
## 2026-11-09 작업 현황 점검

[현황·실제 검사·남은 작업](2026-11-09-status-review.md). 점검 시작 main6936a4e, 최신 게임은11/07 택시/가로 속도계·신호 겹침 교정. 은계/A2·LANv16·BuildHybrid·기본 exe 유지. 이번 Strict exit0/issues0, 배포 VerifyOnly exit0(출하141파일126,543,288바이트+유효한 로컬 사용자설정1개). 재수입 JSON2개는 선택 입력 부재, Git 프로젝트 지도 준비됨. 추적 미커밋0/기존 미추적 보존, 새 빌드·주행 검사 없음. 다음은 사람 장기/패드·전체3일·다른PC LAN/저사양 성능, 월드/실내·승객 표정·정산 연출. CURRENT-STATE의11/06 요약과 최신 이력 차이 및 빌드 이후 패키지 변경은 연결 기록 참조.

