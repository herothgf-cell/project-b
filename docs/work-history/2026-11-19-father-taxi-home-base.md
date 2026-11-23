# 아버지의 택시·숙소 거점 기획, 시안, Codex 프롬프트

## 요구와 수행

- 사용자 설정: 실직 후 남은 것은 돌아가신 아버지가 남긴 개인택시 하나, 그 차로 생계. 숙소는 지정된 한 곳을 거점으로.
- 기획 후 푸시, 구현은 Codex가 이어받도록 시안 이미지를 기획서에 첨부하고 프롬프트 작성.
- [33](../content-design/00-game-overview.md) 전면 개정(은퇴 기사 → 아버지, 생계·월세, 벽 메모 문구), 신규 [34 기사 숙소 거점](../content-design/07-home-room-and-vehicles.md), [10](../content-design/archive/10-restarting-driver-synopsis.md) 머리말 개정, [28](../content-design/06-day-and-career.md) 집 목표를 같은 건물 넓은 호실로 변경.
- 시안 8장 `docs/content-design/images/motivation-*.png`를 `tools/render_motivation_mockups.py`(PIL, 맑은 고딕)로 생성하고 28~34에 삽입. 숙소 지도는 정식 지도 r47의 도로·건물·승하차·시설 좌표로 그림.
- [Codex 구현 프롬프트 P1~P7](../content-design/archive/codex-prompts-motivation.md): 공통 규칙 + 단계별 범위·검증.

## 근거 확인

- 숙소 위치: 지도 오브젝트 `영업 시작·마감 주차장`(serviceX 296.3, serviceZ 506.5) = 기본 승하차 1(S01) 옆. 현재 `ServiceStops.ParkingStop`·졸음 종료 배치·퇴근(`AtEndDayHub`: 주차장 또는 휴식 구역)에 쓰임.
- 차내 조작 단축키: 코드의 KeyCode 사용 목록에 J·K·L 없음(M=지도, N 사용 중). Input System 액션 여부는 P3에서 재확인하도록 프롬프트에 명시.

## 변경 파일

- `docs/content-design/10-*.md`, `28-*.md`~`32-*.md`(시안 삽입), `33-*.md`, 신규 `34-home-base.md`, 신규 `codex-prompts-motivation.md`, `README.md`, `images/motivation-*.png` 8장, 신규 `tools/render_motivation_mockups.py`, 이 기록, `LATEST.md` 한 줄.

## 검증과 한계

- `python -I tools/render_motivation_mockups.py` 종료 0, 8장 육안 확인(글리프 누락 수정 후 재생성).
- 게임 코드·빌드·지도 입력 변경 없음, 플레이 검증 없음. 월세·가격 등은 밸런스 검증 전 초안.
- 시안은 배치 와이어프레임이며 최종 아트가 아니다.

## 다음 작업

- Codex에서 P1(숙소 거점)부터 순서대로 진행.
