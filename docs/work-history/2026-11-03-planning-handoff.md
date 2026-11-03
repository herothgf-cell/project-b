# 2026-11-03 기획·마인드맵·다른 PC 인계

## 요구사항과 기준

다른 PC에서 이어 개발할 수 있도록 마인드맵/기획서를 실제 구현에 맞춰 정리하고 구현 내용·준비·남은 작업을 인계한다. 기능 추가나 실행본 재빌드 요청이 아니다. 실제 음악·에어컨·창문 조작 보류와 필요한 최소 검사 원칙을 유지한다.

원래 `C:/git/project-b`에 있던 사용자 마인드맵/목차2파일의 미커밋 접힘·가지 삭제를 **b342cf5**로 먼저 보존했다. 이 수정본333노드를 작업 기준으로 사용했다. 그 외 미추적 Unity 실험 폴더/문서/사용자 생성 실행 ZIP은 이번 기획 변경에 포함하지 않는다. 백업은 `artifacts/handoff-user-before/`, Git 기준 원본은 위 보존 커밋이다.

## 실제 정리

| 대상 | 결과 |
|---|---|
| 마인드맵/목차 | 기존333개 ID·메모·접힘·task키·자식 순서 보존. 최근 구현7개(닉네임/예측·바퀴/짧은 배차/대화 UI/네비 거리·신호/양방향 신호/조향)를50%로 추가 |
| 단계 | 340노드, 기능 말단 구현50%178 / 기획 완료25%0 / 기획 필요0%53. 중복 참조 포함이며 전체 QA 완료율이 아님. 음악·에어컨·창문0% 보류 유지 |
| 오래된 현재 표시 | PvP 피로 후보→실제 적용값, A2 현재/A3 복귀 이력 구분, P1/P2 ‘미채택’ 부모 제목 수정, 운송 중 HUD 예측 카드 숨김 명시 |
| 현재 요약 | `docs/CURRENT-STATE.md`에 모드/실행/Unity/배포 기준, 기능별 동작·구현 근거·검증 잔여, 다음 작업/보류를 표로 정리 |
| 새 PC 준비 | main 동기화·manifest/Strict 검사·Unity/지도 입력 준비, 캠페인/편집 지도·닉네임·원시 입력·artifacts 이동 방법 구분 |
| 기획 정합성 | scope의 대체 승하차/용무 미구현, 대화의 정차 전용 입력, PvP 컨디션 미구현, 배포 안내의3일12분/3만원·옛 조작·48NPC 문구 정정 |
| 기록 분리 | 현재는 CURRENT-STATE/담당 기획, 날짜별 변경·과거 검사 수치는 기존 이력 보존. 사용자 메모는 삭제하지 않고 최신 기준을 덧붙임 |
| 갱신 도구 | `tools/refresh_handoff_mindmap.py`: 기존 ID/접힘/task/메모/순서를 확인하고 고정 ID의7노드 추가, 단계 집계/목차 갱신, 재실행 불변 검사 |

변경 파일은 위 마인드맵2파일·새 요약/도구, README/배포/PC 인계 안내, prototype-scope·기획01~07 중 해당 문서·우선순위/마인드맵 대조·체크리스트/LATEST/이력이다. **C#·지도JSON/ZIP·실행 폴더·배포 manifest는 바꾸지 않는다.** 원래 폴더의 미추적 `builds/EungyeTaxi/EungyeTaxi.zip`은 사용자 파일로 보존하며 정식 배포 목록에 추가하지 않는다.

## 검증·한계

마인드맵 갱신의 첫 실행 결과: `MINDMAP_PROGRESS_OK nodes=340 implemented50=178 planned25=0 needsPlan0=53`, `MINDMAP_ORGANIZED_OK nodes=340 links=416`, 사용자333노드 보존 및 변환 내부 재실행 불변 통과. 파일로 저장한 후의 재검사·링크·입력/manifest 결과는 아래에 기록한다.

새 런타임 빌드·수동 플레이·다른PC 연결·10분 경제·성능 검사를 수행하지 않는다. 직전 실행본/자동 검사 증거는 [주행 흐름 이력](2026-11-03-drive-flow.md), [대화 UI 이력](2026-11-03-passenger-dialogue.md)을 따른다. 네이티브 마인드맵 앱에서 전체 배치를 새로 열어 검토한 결과로 확대하지 않는다.

다른 PC의 첫 작업은 `docs/CURRENT-STATE.md`와 `docs/work-history/LATEST.md`를 읽고 `git status --short`, `git pull --ff-only`, 배포/Strict 검사 후 프로젝트를 여는 것이다. 개인 캠페인/편집 지도 파일과 PvP 닉네임은 Git 동기화 대상이 아니다.


## 최종 실제 검사

- `tools/organize_content_mindmap.py --validate-only` 종료0:340노드·저장소 Detail-Link416개 존재. 사용자333개ID/접힘/task키/원래 메모/자식 순서 보존, 삭제한7개 옛 노드 미복원.
- `tools/refresh_handoff_mindmap.py` 두 번째 실행 후 .mmst와 목차 파일의 SHA256 불변, 중복 추가 없음.
- 변경 문서의 로컬 링크188개(유효한 디렉터리 링크 포함) 존재. `git diff --check` 종료0.
- `pwsh -File tools/check_workspace.ps1 -Strict` 종료0/Input issues0. 프로젝트 지도hash35d1bc43… 유효. 옛 재수입 입력2개 미배치/기존 규칙상 선택 사항.
- `tools/update_distribution_manifest.ps1 -VerifyOnly` 종료0:140파일115,464,894바이트. `git diff --exit-code -- builds/EungyeTaxi config/maps distribution/EungyeTaxi-build-manifest.json` 종료0, 실행파일/지도/manifest 미변경.


## 게시·원래 폴더 반영

기획 최신화2b89d9ad5de479c9c3ec18c16954aa394036549e를 origin/main에 게시하고 원격 동일 해시를 확인했다. 원래 C:/git/project-b도 fast-forward해 최신 마인드맵/목차·인계 문서를 반영했다. 동시 마인드맵 수정 없음 확인 후 적용했으며 기존 미추적 실험·문서·EungyeTaxi.zip은 보존했고 ZIP의 전후SHA256도 일치한다. 원래 폴더의 추가 ZIP은 정식 배포 목록에 없으므로 원래 폴더 전체 VerifyOnly 통과로 기록하지 않는다. 다른PC의 Git 배포 폴더와 작업 worktree의140파일 manifest 검사는 정상이다. 이 영수증 후속은 같은 실행본과 기획을 유지한다.
