# 게임 개발 착수 제안서 3차 수정

## 요청과 실제 변경

단골·예약 콘텐츠를 제외하고 최신 생활/시놉시스 기획을 회사 제안 PPT에 반영했다. `content-design/10~13`을 기준으로 재출발 배경, 작은 방·조명/러그 첫 구매, 개인 소비와 영업 회계 분리, 장기 차량 교체·이사를 32장으로 정리했다. 반복 인물/관계 콘텐츠를 사용하지 않으며 현재 구현과 미구현 생활 제안을 구분했다. KPI·우선순위·로드맵·투자 판단에도 같은 방향을 반영했다.

수정 전 V1/V2와 시안을 보존했다. V3 매뉴얼은 통합 실행본과 사고·수리·PVP 구현 상태를 반영한다. 참여자 실명/인원은 사용자 지시대로 회사 제출 전 확인 필요, 인력·예산·일정은 심의용 가정이다.

## 변경 파일

- `docs/proposals/project-b-game-proposal-v3.pptx`
- `docs/proposals/project-b-prototype-manual-v3.md`
- `docs/proposals/proposal-v3-review.md`
- `docs/proposals/README.md`
- 본 기록과 `docs/work-history/LATEST.md`

비공개 빌드/검증 자료는 Git 제외 `artifacts/proposal-v3-20261106/`에 둔다. 작성 스크립트 `revise.py`는 기존 V2 생성기를 바탕으로 V3 `build.mjs`를 만든다. 원본 시안은 기존 자산을 재사용했다.

## 검증과 결과

- 시작 시 LATEST·prototype-scope·checklist·관련 03/05/10~13 및 계정/하루전환 코드를 대조하고 `git status --short`로 기존 변경 확인.
- 번들 Python으로 `revise.py` 실행, Node로 `build.mjs` 실행. 첫 실행은 Windows sandbox의 최종 하드링크 EPERM으로 종료1. 자동 승인된 권한 확장 실행에서 동일 최종화 절차가 종료0으로 완료됨.
- 최종화: 32장, 편집 가능한 표11개. 패키지/레이아웃 finding0, warning0, Malgun Gothic 정책 통과, Artifact Tool 재가져오기32장 통과.
- 32장 PNG를 개별 열람. 마지막 본문 변경의 10장 재확인. 겹침·잘림 없음. 실제 PowerPoint 앱 검사는 수행하지 않음.
- 최종 PPT ZIP의 슬라이드·노트64개 XML 검사: `단골|예약` 일치0, 종료0.
- 최종 파일 11,593,586바이트, SHA256 `861516f5fcc50ff6ec9270baea28b6ac2188cc0fe0b7153e0e794c7607fe8752`.
- 기존 사용자 자료/잠금파일과 동시 작업 변경은 수정·스테이징하지 않음.

## 미검증과 다음 작업

게임 소스·빌드·지도·저장·manifest 변경 없음. 게임 실행, 위치 제어, 자동 물리 주행, 사람 수동 플레이, 전체 캠페인 검사 및 Strict/manifest 재검사는 이번에 수행하지 않았다. 작은 방·거래·차량 교체·이사는 기획안이며 구현 완료가 아니다. 회사 제출 전 참여자 정보와 범위/예산/일정을 확인한다. 기존 게임의 사람 장시간/전체3일·다른PC 수용은 별도 진행한다.

## 열기와 현재 실행 상태

`docs/proposals/project-b-game-proposal-v3.pptx`를 PowerPoint로 연다. 별도 조작 문서는 같은 폴더의 V3 매뉴얼이다. 개발 재생성은 번들 Python/Node를 쓰며 설치 절대 경로는 PC 로컬 정보다. 최종화는 기존 파일을 덮어쓰지 않으므로 재생성 시 새 출력명을 사용한다.

게임은 `builds/EungyeTaxi/EungyeTaxi.exe`, 은계 싱글·A2 PVP·LANv9 / `PrototypeBuild.BuildHybrid`를 유지한다. 필수 지도 입력·ZIP 상태는 기존 게임 작업 이력을 인용하며 이번 변경 없음.
