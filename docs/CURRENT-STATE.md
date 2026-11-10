# 현재 상태 한 장 요약

갱신: **2026-11-10**(성장 결정 반영). 새로 합류한 사람이나 다른 PC에서 이어갈 때 이 문서부터 읽는다. 날짜별 상세 기록은 [최신 작업 이력](work-history/LATEST.md), 이전 판 전문은 [보관본](design-history/CURRENT-STATE-until-2026-11-09.md)에 있다.

## 한 줄 요약

한국 동네(시흥 은계)를 배경으로 **안전 운전 + 승객 응대 + 기사 생활 관리**를 하는 택시 시뮬레이션. 싱글 3일 캠페인과 4인 PvP가 하나의 실행 파일로 동작하는 프로토타입이며, **사람이 직접 오래 플레이한 검증은 아직 없다.**

## 실행·개발 기준

| 항목 | 현재 |
|---|---|
| 게임 실행 | `builds/EungyeTaxi/EungyeTaxi.exe` (또는 `Play-Eungye.cmd`). 실행 폴더 전체가 필요 |
| 모드 | 은계 싱글 3일(하루 예정 20분) / A2 자체 도시 PvP(봇 3 또는 LAN 최대 4명) |
| 개발 | Unity **2022.3.62f2**, 프로젝트 `unity/EungyeTaxi`, 빌드 `PrototypeBuild.BuildHybrid` |
| 최신 배포 | 게임 소스 548fe8b · 출하 a978b3b (2026-11-09) |
| 통신 | LAN **v16**. 양쪽 PC가 같은 실행본이어야 함 |
| 지도 | 정식 지도 `config/maps/project/eungye.project.json`(Git 포함). 원본 재수입할 때만 지도 ZIP 필요 |
| 설정 도구 | 루트 `설정 관리.cmd` — 승객·대사·효과·법규·밸런스 값 조정 |

## 구현된 것

| 영역 | 내용 |
|---|---|
| 운전·교통 | 실측 기반 도로·차로·신호, NPC 차량·보행자, 신호/과속/보행자 보호 판정과 운전 점수, 운전 설정 5종, 비(와이퍼·제동 영향) |
| 승객 | 일반 + 특수 11유형, 요청 17종, 운행 중 응답, 상황 반응(수면·취객·음식·짐), 만족도와 운임 조정, 승객 프리셋·개별 설정 |
| 생활·차량 | 연료·포만감·피로·청결, 주유·식사·휴식·세차, 내구도·사고 3등급·공업사 수리·견인 |
| 캠페인 | 3일 영업, 하루 정산, 이어하기, 작은 방 꾸미기(조명·러그) |
| PvP | 공유 승객 경쟁, 순수익 순위, 봇 3명, LAN 호스트 판정·자기 차 예측 |
| 화면 | 1·3인칭, 내비, 지도·호출, 생활거점, 결과 카드, 캐주얼 HUD, 둥근 노란 택시 모델 |

## 아직 안 된 것

| 구분 | 내용 |
|---|---|
| 검증 | 사람의 싱글 20분·전체 3일 플레이, 다른 PC 간 LAN, 실물 게임패드, 저사양 성능, 첫 이용자 이해도 |
| 기획 확정·미구현 | [기획22](content-design/22-growth-direction-and-vehicle-purchase.md): 3일 안 차량 교체 1회(차종 특성)·차고 화면·싱글 차량 외형 PvP 사용 |
| 기획 보류 | 기사 평점(기획22 §3-1), 개인택시 면허 경로([기획23](content-design/23-license-career-path.md)), Blender 아트 파이프라인 2단계 이후 |
| 미구현(기획 있음) | 이사, 처음/완료 방 비교, 다음 날 찜 카드 |
| 미구현(기획 필요) | 눈·동적 낮밤, 단골·예약, 보험·과실, 버스·회전교차로, 단속 법규 |
| 보류(재지시 전 금지) | 음악·에어컨·창문 실제 조작 |
| 아트 | 건물은 회색 상자. 목표 카툰 아트 전환은 [아트 전환 가이드](proposals/art-conversion-guide.md) 단계 |

## 다음 작업 순서

1. Claude Code에서 기획22 구현(3일 안 차량 교체·차종 특성·차고·PvP 외형, LAN v17)
2. 사람 플레이 검증(싱글 3일, PvP 10분, 다른 PC)
3. 장기 방향(이사·면허 경로)은 본편 기획 때 결정
4. 아트 전환은 본개발 승인 후(지금은 목표 시안으로 충분)

## 문서 위치 규칙

새 문서는 종류에 따라 아래 폴더에 둔다(2026-11-10 사용자 지정).

| 종류 | 폴더 |
|---|---|
| 주요 기획 문서 | `docs/content-design/` (PvP는 `docs/content-design/pvp/`) |
| 마인드맵(전체 아이디어 지도) | `docs/content-design/택시 시뮬레이션.mmst` · 글 목차 `택시 시뮬레이션-목차.md` |
| UI 관련 문서·시안 | `docs/ui/` |
| 제안서와 관련 문서(아트 시안 포함) | `docs/proposals/` |
| 작업 이력 | `docs/work-history/` |

## 어디서 무엇을 보나

| 알고 싶은 것 | 문서 |
|---|---|
| 작업 규칙 | [AGENTS.md](../AGENTS.md) |
| 최근 작업과 현재 빌드 | [work-history/LATEST.md](work-history/LATEST.md) |
| 게임 규칙(기획) | [content-design/README.md](content-design/README.md) — 기능별 담당 문서 목록 |
| PvP 규칙 | [content-design/pvp/README.md](content-design/pvp/README.md) |
| 구현 체크리스트 | [prototype-development-checklist.md](prototype-development-checklist.md) |
| 기획 미정·미구현 전체 | [future-design/2026-11-07-planning-gaps-and-weather.md](future-design/2026-11-07-planning-gaps-and-weather.md) |
| 회사 제안서 | [proposals/README.md](proposals/README.md), 최신 [V9](proposals/project-b-game-proposal-v9.pptx) |
| 다른 PC 인계 자료 | [handoff/2026-11-09/README.md](handoff/2026-11-09/README.md) |
| 지도 입력·PC 이동 | [work-history/README.md](work-history/README.md), [map-transfer-guide](work-history/map-transfer-guide.md) |

## 다른 PC에서 시작

```powershell
git status --short          # 기존 로컬 변경 먼저 확인
git switch main
git pull --ff-only
pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly
pwsh -File tools/check_workspace.ps1 -Strict
```

- 게임만 할 때는 Unity 없이 exe 실행.
- 개발할 때는 Unity Hub에서 2022.3.62f2 + Windows 빌드 지원 설치 후 `unity/EungyeTaxi`를 연다.
- 검사 스크립트는 **PowerShell 7(`pwsh`)** 이 필요하다. Windows PowerShell 5.1에서는 실패한다(`winget install --id Microsoft.PowerShell`).
- 존재하는 입력 파일의 해시 오류는 우회하지 않는다.

## Git으로 옮겨지지 않는 것

| 대상 | 옮기는 방법 |
|---|---|
| 개인 캠페인 저장·사용자 편집 지도 | 게임 종료 후 `%USERPROFILE%/AppData/LocalLow/Taxi Prototype/은계 택시 시뮬레이터/` 전체 복사 |
| PvP 닉네임 | 새 PC에서 다시 입력 |
| 원시 NGII/OSM 자료, `artifacts/` | 재수입·증거가 필요할 때만 별도 복사 |
