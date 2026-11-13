# 2026-11-13 콘텐츠 관리 툴 운전 만족도 표기

- 요청에 따라 M을 ‘운전 만족도’로 풀어 썼다. 법규의 ‘같이 관찰하는 M’은 ‘함께 기록되는 운전 만족도’로 표시한다. 점수 설명, 요청 미응답, 선택지, 프리셋, 미리보기와 사용 안내에 같은 명칭을 적용했다. 운전 만족도는 탑승 때 100점으로 시작하며 주행 중 변화량을 하차 만족도에 반영한다.
- `tools/content-editor/public/`, 툴·Unity 카탈로그의 표시 메타데이터와 고정 참조, `docs/content-tuning-tool-guide.md`를 갱신했다. 설정 ID·편집 가능 기본값·프로필·감점 계산과 통합 실행본은 기존 값을 사용한다.
- `node --test --test-reporter=dot tests/content-editor/*.test.mjs tests/content-manifest.test.mjs` 종료 0, 104개. 우회전 명칭 회귀는 수정 전 기존 ‘같이 관찰하는 M’에서 실패했다. 배포 테스트의 임시 툴 복사에서 빠진 의존 모듈도 함께 보완했다.
- `node tools/content-editor/choice-catalog.mjs . --verify` 종료 0. `pwsh -NoProfile -File tools/test_content_tuning.ps1` 종료 0, 846 검사 및 C#/Node 해시 일치.
- `pwsh -NoProfile -File tools/start_content_editor.ps1`로 관리창을 열고 **법규 → 적색 우회전 전 일시정지**에서 **함께 기록되는 운전 만족도**를 확인한다. 새 Unity 빌드·주행 검사는 해당하지 않는다. 현재 배포는 은계 r18 / A2 PvP LAN v19이며 필수 입력은 직전 작업의 Strict 검사 기준이다.
