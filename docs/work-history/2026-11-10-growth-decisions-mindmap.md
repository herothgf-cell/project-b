# 2026-11-10 성장 결정 반영·마인드맵 수정·아트 파이프라인 보류

Claude(Cowork)에서 수행. 기획·문서·마인드맵만 변경했고 게임 코드·빌드·지도·저장은 바꾸지 않았다. **Git 커밋 안 함.**

## 요구사항

- 프로토 단계에서 아트 작업 범위 축소: Blender 파이프라인 보류.
- 기획22 결정 4건 정리.
- 마인드맵(`docs/content-design/택시 시뮬레이션.mmst`) 수정.

## 사용자 결정

| # | 항목 | 결정 |
|---|---|---|
| 1 | 차종별 특성 | 채택(연료·트렁크·승차감·내구도·회전 반경) |
| 2 | 기사 평점 | 기획만, 구현 보류 |
| 3 | 캠페인 길이 | 3일 유지, 3일 안 차량 교체 1회(가격 약 1.2~1.6R) |
| 4 | 개인택시 면허 경로 | 본편 단계로 보류, 기획·설정·마인드맵 할거리 추가 |

## 변경 파일

| 파일 | 내용 |
|---|---|
| `docs/content-design/22-growth-direction-and-vehicle-purchase.md` | 결정 반영 개정(3일 범위·가격·PvP 외형 영구 해금·결정 기록·마인드맵 반영표) |
| `docs/content-design/23-license-career-path.md` | 신규. 면허 경로 설정 변형·시스템 연결·할거리 8개·보류 해제 조건 |
| `docs/content-design/11-personal-life-and-growth.md` | 상단에 22에 따른 원칙 변경 안내 |
| `docs/content-design/06-mindmap-review.md` | 11/10 반영 기록 |
| `docs/content-design/README.md` | 22 행 갱신, 23 행 추가 |
| `docs/content-design/택시 시뮬레이션.mmst`, `-목차.md` | 14노드 추가, 3노드 0%→25% |
| `docs/design-history/content-design/2026-11-10-mindmap-before-growth.mmst` | 변경 전 원본 백업(원본과 바이트 동일) |
| `tools/update_growth_mindmap.py` | 반영 스크립트와 `--check` 검사 |
| `docs/proposals/project-b-game-proposal-v9.pptx` | 3·5·8·15·16·28장 문구 갱신(사용자 PC 저장본 기준) |
| `docs/proposals/proposal-v9-review.md` | 갱신 기록 |
| `docs/proposals/art-blender-pipeline.md` | 보류 표시, 1단계 완료(Claude Code 환경 확인 결과 보존) |
| `docs/CURRENT-STATE.md`, `docs/work-history/LATEST.md` | 상태·다음 작업 갱신(LATEST는 PC의 최신본에 덧붙임) |

## 검증

- 마인드맵: `update_growth_mindmap.py` 실행 후 `--check` 통과(`nodes=378 added=14`, 기존 노드 메모·접힘·순서·링크 보존). 목차는 원본에서 생성한 결과와 일치. 직렬화는 원본과 같은 들여쓰기·끝 줄바꿈 없음.
- 제안서: `validate.py --original` 통과, 15·16장 렌더 확인. PowerPoint 앱 미확인.
- 마인드맵 네이티브 앱에서 연 시각 확인은 하지 않음.

## 남은 항목

- Claude Code에서 기획22 구현(3일 안 차량 교체·차종 특성·차고·PvP 외형, LAN v17).
- 사람 플레이 검증.
- 커밋: `git add docs tools && git commit -m "docs: 성장 결정 반영, 기획23, 마인드맵 갱신, 아트 파이프라인 보류"` (커밋 전 `git status --short`로 범위 확인).
