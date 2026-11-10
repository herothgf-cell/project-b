# 2026-11-10 제안서 V9·성장 방향·문서 정리

Claude(Cowork)에서 수행. PC 셸·Unity·git을 쓸 수 없는 세션이라 **기획·문서 작업만** 했다.

## 요구사항

- 제안서 V9: 차 1대 구매로 성장 주장 뒷받침(기획까지), 싱글에서 구매·꾸민 차량을 PvP에서 활용, 아트 목표와 프로토 지도 방식 차이 설명. 로드맵·참여자는 그대로 유지.
- 아트 전환 시안: 이미지 생성 도구가 없어 **도구 설치 가이드**로 대체.
- 문서 체계 정리: CURRENT-STATE 한 장 요약, LATEST 최근분만 두고 보관본 분리.
- 성장 목표 부족 검토와 새 방향 제안.
- 문서 위치 지정: 기획 `docs/content-design`(마인드맵 `택시 시뮬레이션.mmst` 포함), UI `docs/ui`, 제안서 `docs/proposals`.

## 실제 결과와 변경 파일

| 파일 | 내용 |
|---|---|
| `docs/content-design/22-growth-direction-and-vehicle-purchase.md` | 신규. 현재 성장안 검토, 목표 3층(기사 평점/내 택시/내 집), 차종 특성 제안, 7일 시연판 범위, 싱글 차량 PvP 외형 규칙, 결정 대기 4건 |
| `docs/content-design/README.md` | 22 행 추가 |
| `docs/proposals/project-b-game-proposal-v9.pptx` | 신규 28장. 15장 재작성, 16·22장 신규, 1·2·3·5·8·19·28장 문구 갱신, 쪽 번호 재정렬 |
| `docs/proposals/proposal-v9-review.md` | 신규. 반영 위치와 주의 |
| `docs/proposals/art-conversion-guide.md` | 신규. 전환 방향, 시안 목록 A~E, 프롬프트, 이미지 생성 도구 4가지 설치 방법 |
| `docs/proposals/assets/v9/` | `README.md`, `prompts/*.txt`(공통 스타일+시안 7종), `art-before-after-v9.png`(22장 비교 이미지: 11/07 실제 캡처 + V7 표지 시안) |
| `docs/proposals/README.md` | 9차 항목 추가 |
| `tools/generate_concept.py` | 신규. OpenAI 이미지 API로 시안 생성, 키는 환경 변수만 사용, 기존 파일 덮어쓰기 금지, 프롬프트 자동 기록 |
| `docs/CURRENT-STATE.md` | 한 장 요약으로 새로 작성, 문서 위치 규칙 포함 |
| `docs/design-history/CURRENT-STATE-until-2026-11-09.md` | 이전 CURRENT-STATE 원문 보관(링크 경로만 보정) |
| `docs/work-history/LATEST.md` | 현재 기준·다음 작업·11/10·11/09 기록만 유지 |
| `docs/work-history/LATEST-archive-until-2026-11-09.md` | 이전 LATEST 원문 전체 보관 |
| `AGENTS.md` | 문서 위치 규칙 추가 |

기획22 §9에 마인드맵 대조표를 넣었다. `차량 구매 > 다양한 차종`(연비·트렁크·승차감, P4·0%)이 22의 차종 특성 제안과 같은 아이디어임을 확인했다. **마인드맵 원본(.mmst)은 수정하지 않았다**(결정 후 06 기준으로 반영).

작업 도중 같은 날 다른 세션이 남긴 [11/10 진행 확인](2026-11-10-progress-check.md)과 LATEST 갱신을 발견해, 새 LATEST·보관본에 그 내용을 포함하고 PowerShell 7 필요 사항을 CURRENT-STATE에 반영했다.

게임 코드·빌드·지도·저장·배포 폴더는 변경하지 않았다.

## 수행한 검증

- 제안서: `validate.py --original v8` 통과, LibreOffice 렌더로 28장 전체 확인, 문구 넘침 4건 수정 후 재확인. 실제 PowerPoint 앱 확인은 하지 않음.
- `tools/generate_concept.py`: 문법 검사, API 키 없음 경로(종료 코드 2) 확인. 실제 이미지 생성은 하지 않음(키 없음).
- 새 문서의 상대 링크 대상 존재 확인.
- Strict·manifest·Unity 빌드·플레이 검사는 하지 않음(게임 변경 없음). 현재 수치는 11/09 기록을 인용.

## 남은 항목

- 기획22 §8 사용자 결정 4건.
- 7일 시연판 구현(Claude Code), LAN v17.
- 같은 시점 전·후 시안(A-1~A-3) 생성 후 22장 이미지 교체, 27장 "3일 체험" 문구는 7일 채택 시 수정.
- **Git 커밋 미실행.** 아래 명령으로 커밋한다.

## 재현·커밋

```powershell
git status --short
git add AGENTS.md docs/CURRENT-STATE.md docs/design-history/CURRENT-STATE-until-2026-11-09.md `
  docs/work-history/LATEST.md docs/work-history/LATEST-archive-until-2026-11-09.md `
  docs/work-history/2026-11-10-proposal-v9-growth-docs.md `
  docs/content-design/22-growth-direction-and-vehicle-purchase.md docs/content-design/README.md `
  docs/proposals/README.md docs/proposals/proposal-v9-review.md docs/proposals/art-conversion-guide.md `
  docs/proposals/project-b-game-proposal-v9.pptx docs/proposals/assets/v9 tools/generate_concept.py
git commit -m "docs: 제안서 V9, 성장 방향 기획22, 아트 전환 가이드, 문서 정리"
```
