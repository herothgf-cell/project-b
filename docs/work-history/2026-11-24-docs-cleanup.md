# 2026-11-24 docs 정리 (미사용 문서 제거)

## 요구

푸시 뒤 docs 전반을 정리하고 미사용 문서를 지운다. 삭제 대상은 후보를 보여 드린 뒤 사용자가 골랐다(2026-11-24).

- 고른 것: 옛 PC 인계 묶음, 링크 없는 옛 구현 계획, 옛 제안서 PPTX V1~V8.
- 고르지 않은 것: 링크 없는 루트 문서 5개·질문용 ps1 2개·V9 프롬프트 txt 6개(남김).
- 루트의 10월 검증 문서 약 20개를 보관 폴더로 옮기는 일은 "지금은 그대로"로 정했다. 그 문서들을 가리키는 개발 체크리스트에 다른 작업의 미커밋 변경이 있어서다.

## 조사 방법

- Git이 관리하는 docs 1,233파일(약 355MB, Git 밖의 `docs/mindmaps/`는 제외)을 대상으로, 저장소의 다른 텍스트 파일(md·html·cs·ps1·py·json 등)에 파일 이름이 한 번이라도 나오는지 셌다.
- 이름이 안 나오는 파일은 109개였다. 대부분은 폴더 단위로 링크된 UI 캡처라 지우지 않았다.

## 실제 변경

| 묶음 | 파일 | 크기 | 판단 근거 |
|---|---|---|---|
| `docs/handoff/2026-11-06`, `2026-11-09` | 127 | 약 72MB | 예전 PC 이동 묶음. 시작 절차는 `work-history/README.md`와 `CURRENT-STATE.md`가 대신한다. 옛 기록 몇 곳에서만 가리킨다. |
| `docs/superpowers/plans`·`specs` 중 어디서도 이름이 안 나오는 것 | 33 | 작음 | 10월~11/12 구현 계획·명세. 모두 구현이 끝났다. 이름이 나오는 나머지 69개는 남겼다. |
| `docs/proposals/project-b-game-proposal-v1~v8.pptx` | 8 | 약 102MB | V10(최신)과 V9만 남겼다. 각 버전의 반영 검토 md와 매뉴얼은 남겼다. |

합계 168파일. 지운 파일은 커밋 5f408f5d 등 이전 커밋에 그대로 있다(`git show 5f408f5d:<경로>`). 저장소 이력 크기는 줄지 않는다.

링크 처리:

- `work-history/README.md`: 첫 줄의 "최신 인계 묶음을 먼저 읽는다"를 현재 절차와 Git 이력 안내로 바꿨다.
- `proposals/README.md`: 맨 위에 정리 안내를 넣고, V1~V8 PPTX 링크를 파일 이름 + "삭제, Git 이력" 표기로 바꿨다.
- 옛 기록 8개(`design-history/CURRENT-STATE-until-2026-11-09.md`, `proposals/proposal-v5-review.md`·`v6-review.md`, `work-history/2026-11-02-before-traffic-law-catalog.md`·`2026-11-06-cross-chat-pc-handoff.md`·`2026-11-09-pc-transfer.md`, LATEST 보관본 2개): 지운 파일로 가는 링크만 "(2026-11-24 정리로 삭제, Git 이력에 있음)" 표기로 바꿨다. 본문은 고치지 않았다.
- `.gitattributes`: 지운 `docs/handoff/2026-11-09/**` 줄을 뺐다.

## 검증

- 남은 md·html 전체에서 지운 파일·`docs/handoff`로 가는 링크: 0건(스크립트 대조).
- `tools/`, `unity/`, `config/`, `AGENTS.md`에서 지운 파일 이름 참조: 0건.
- 다른 작업이 고치는 중인 파일(INDEX.md·index.html·LATEST.md의 다른 변경·체크리스트·CURRENT-STATE·08)은 링크 수정 대상에서 뺐다. 그중 지운 파일을 가리키는 링크는 없었다.

## 남은 것

- 루트 10월 검증 문서의 보관 폴더 이동(다른 작업이 끝난 뒤).
- [기획서 목록](../content-design/INDEX.md)에서 "진행 중"으로 적은 피드백 3차는 3a0cf27b로 커밋됐다. INDEX.md를 다른 작업이 고치는 중이라 이번에는 손대지 않았다.
- 큰 이미지·PPTX를 저장소 이력에서도 빼려면 이력 재작성이 필요하다. 하지 않는다(공유 저장소 강제 푸시 필요).
