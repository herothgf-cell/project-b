# 2026-11-23 기획 문서 통합 정리

## 요구사항

- `docs/content-design/`의 기획 문서 46개를 주제별로 합쳐 정리한 결과를 커밋·푸시한다.

## 실제 작업

- 주제별 통합 문서 10개로 재구성했다: 00 게임 개요·배경·손님 이야기, 01 주행·교통·운전 평가, 02 승객·운송·대화·운임, 03 승객 제작·특성·밸런스, 04 생활·차량·시설·영업 경제, 05 화면·조작·길안내, 06 하루·경력·날씨·시간대, 07 숙소·방·차량 구매, 08 운전하는 20분의 재미 보강(검토 중), 09 세부 항목 색인.
- 합친 장에는 "구 NN" 표기와 `#sNN` 앵커를 달았다. 옛 번호 → 새 위치 표는 [기획 목록](../content-design/README.md#옛-번호로-찾기)에 있다.
- 대체·제외 문서와 시점 스냅샷은 `docs/content-design/archive/`로 옮겼다(06 마인드맵 관리, 09~12·14 생활·성장 원안, 23 면허 경로, 37 기사 생활과 영업 목표, 11/13 승객 대조표, 11/19 동기 진단·구체안, Codex 프롬프트).
- **결정:** 이전 기획 37(특별 운행·지도 판매점·주기 정비·소지금 장부·기사 성장)은 개발 범위에서 제외했다. 체크리스트의 해당 후속 검토 항목도 제외로 표시했다.
- 같은 날 원격에서 추가된 [37 공차 운영·길 지식·규칙 속 돌발](../content-design/37-driver-judgment-systems.md)은 독립 문서로 유지하고 목록에 넣었다. 링크 2개를 새 위치로 고쳤다.
- `docs/content-design/cleanup-merged-docs.ps1`로 옛 문서 41개를 `git rm`하고, 저장소 안 `.md`/`.html`의 링크를 새 위치로 바꿨다(`docs/design-history/`, `archive/`는 그대로). 도로 조사(`road-survey/`)의 md·ps1·json 출처 경로는 수동으로 01로 바꿨다.
- 마인드맵 `택시 시뮬레이션.mmst`와 `택시 시뮬레이션-목차.md`의 링크도 통합 위치로 갱신했다.

## 진행 중 복구한 문제

- 정리 직후 `git pull`이 작업 트리를 stash한 뒤 문서 4개(CURRENT-STATE, content-design README, 체크리스트, LATEST)에서 충돌한 채 멈췄고, stash(00·01·02·05·README·목차·mmst 수정분)는 복원되지 않았다. 남아 있던 dangling stash 커밋 `c907fb8b`에서 복원했다.
- 병합 충돌은 로컬 쪽을 기준으로, 원격 변경(r51 P51 교차로, 11/23 채팅 알림·경로·기사식당 체크리스트, 기획37 판단 시스템)을 합쳐 해결하고 병합 커밋을 먼저 만들었다.

## 변경 파일

- `docs/content-design/` 00~09, `README.md`, `archive/`, 삭제된 옛 문서, 마인드맵·목차, `pvp/`·`road-survey/` 링크
- 저장소 안 링크 갱신: `docs/CURRENT-STATE.md`, `docs/prototype-development-checklist.md`, `docs/prototype-scope.md`, `docs/game-design.md`, `docs/work-history/*.md` 등

## 검증

- 추적 중인 `docs/` 아래 `.md`와 `docs/content-design/` 전체에서 상대 링크(.md/.html/.mmst) 대상 파일 존재를 검사했다: `design-history/`·`archive/` 제외 시 깨진 링크 0건.
- 앵커(`#sNN`) 존재와 문서 본문 내용 대조는 하지 않았다. 게임 코드·빌드·지도 변경 없음.

## 남은 항목

- 08 재미 보강안 검토와 기획37(공차 운영) 구현 승인 여부 결정.
- `docs/handoff/` 등 과거 제안서 빌드 스크립트 안의 옛 파일명은 당시 기록이라 그대로 두었다.

## 재현

```powershell
git show --stat HEAD
```
