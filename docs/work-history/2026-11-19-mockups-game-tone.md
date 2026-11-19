# 시안 게임 톤 재작성·아버지 차 보관 결정

## 요구와 수행

- 사용자 결정: 아버지 차는 교체 시 판매하지 않고 숙소 주차장에 보관.
- 사용자 지적: 시안이 현재 UI·톤앤매너와 맞아야 함. 기존 시안(맑은 고딕·임의 색 와이어프레임)을 폐기하고 다시 만들었다.
- `tools/render_motivation_mockups.py` 재작성: 실제 게임 스크린샷(운전석·3인칭 HUD, 영업 준비, 영업 마감, 방, 차고) 위에 `CasualUI.cs` 색 토큰(Ink·Canvas·Blue·Green·Border·Soft·Warm)과 게임 폰트 Noto Sans KR(Resources/Fonts)로 새 요소를 얹고, 새 요소는 주황 점선으로 표시.
- 시안 11장: 숙소 지도, 3컷 인트로, 아버지 수첩, 수첩 이야기 탭, 첫날 밤 벽 메모, 아버지 차 보관 확인 창(신규), 차내 조작, 영업 준비 목표·조건 카드, 영업 마감 목표 게이지(신규), 하루 조건 카드, 낮밤·라디오.
- 기획 반영: [28](../content-design/28-continuous-career-mode.md) 목표 행·시안 3장, [33](../content-design/33-backstory-presentation.md) 보관 결정 표·구현 범위·시안, [34](../content-design/34-home-base.md) 차고 행, [Codex 프롬프트](../content-design/codex-prompts-motivation.md) 공통 UI 규칙(CasualUI 재사용)·P2/P4 시안 목록·P4 보관 구현 항목.
- 영업 준비 원본 스크린샷이 Git 제외 폴더(artifacts)에 있어 `docs/content-design/images/source-single-preparation.png`로 복사해 재생성 가능하게 했다.

## 검증과 한계

- `python -I tools/render_motivation_mockups.py` 종료 0, 11장 육안 검토 후 겹침(정산 라벨, 벽 메모 줄바꿈, 밤 화면 보도 밝기) 수정·재생성.
- 사용한 원본 스크린샷은 11/06~11/12 촬영본으로, 이후 HUD 세부가 조금 바뀌었을 수 있다(정산 카드는 11/06 화면). 구현 시 현재 화면 기준으로 배치를 맞춘다.
- 게임 코드·빌드·지도 변경 없음, 플레이 검증 없음.

## 변경 파일

- `tools/render_motivation_mockups.py`, `docs/content-design/images/motivation-*.png` 11장, `images/source-single-preparation.png`, `28·33·34-*.md`, `codex-prompts-motivation.md`, 이 기록, `LATEST.md` 한 줄.
