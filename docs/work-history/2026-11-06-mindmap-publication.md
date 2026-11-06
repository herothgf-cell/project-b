# 마인드맵 최신화·main 게시

## 요청·기준

사용자 요청: 마인드맵 최신화 후 푸시. 최신 원격 main `e230a3b`를 fetch하고 전용 브랜치 `codex/mindmap-refresh-20261106`에서 작업했다. 직전 사고 작업의 오래된90fadf7을 현재 배포로 사용하지 않았다. LATEST·scope·checklist·08/13/14 기획과 AccidentState/PersonalRoomHUD의 현재 구현을 대조했다.

## 변경

- 원본 `docs/content-design/택시 시뮬레이션.mmst`와 목차: 사고3등급/보행자 손상·공업사·설정 관리창·작은 방 구매 구현, 개인 생활 확장 후속6노드 추가. 사고/보험·용무 대기·안전 대체·방향지시등의 과거 표현 정정.
- `docs/content-design/06-mindmap-review.md`: 최신 기준·단계 수·후속/검증 잔여.
- `tools/refresh_mindmap_20261106.cjs`: 기존 노드/메모 보존 검사와 재실행 가능한 갱신기. Node.js 실행, 저장소 루트 기준.
- 체크리스트/LATEST와 이 기록: 게시 인계.

## 검증

`node tools/refresh_mindmap_20261106.cjs` exit0:346노드/433로컬 문서 링크·기능183개 구현50%/0개 기획완료25%/54개 기획필요0%. 기존340노드의 ID·자식 순서·접힘·task·사용자 메모·링크 보존 통과. 부모 생성 집계만 재계산했다. 두 번째 실행도 exit0이며 재실행 바이트 불변은 아래 게시 전 검사로 확인한다. `git diff --check` exit0. 탐색 중 Node 일회 명령의 여분 중괄호와 잘못된 파일명 조회는 수정 후 실제 파일을 확인했으며 제품 오류가 아니다.

네이티브 마인드맵 앱 화면·게임 재빌드·플레이·Strict·배포manifest 재검사는 이번 문서 작업 범위에서 수행하지 않았다. 사람 장시간/전체3일/다른PC LAN 등 기존 수용 잔여를 완료로 바꾸지 않았다.

게시 직전 재실행: 원본과 목차 SHA256 전후 동일 `IDEMPOTENT_OK`, 노드 보존/433링크 검사 exit0, `git diff --check` exit0. 원격 재조회도 e230a3b로 동일했다.

## 실행·인계

마인드맵은 `docs/content-design/택시 시뮬레이션.mmst`, 텍스트는 같은 폴더의 `택시 시뮬레이션-목차.md`. 기본 실행 `builds/EungyeTaxi/EungyeTaxi.exe` / 은계 싱글·A2 PVP·LANv9 / `PrototypeBuild.BuildHybrid`. 기존 출하141파일118,242,753바이트·통합소스84d1605/배포e230a3b 유지. 지도입력·ZIP·게임·저장·manifest 미변경, 필수입력 상태는 직전 배포 Strict issues0 기록 인용.

원래 `C:/git/project-b`의 동시 운전 설정 작업·미커밋 manifest/scene·미추적 사용자 자료는 건드리지 않는다. 원격 main에 fast-forward로 게시하며 강제 푸시하지 않는다. 실제 기록 커밋은 `git log -1 --oneline -- docs/work-history/2026-11-06-mindmap-publication.md`, 게시 일치는 `git ls-remote origin refs/heads/main`으로 확인한다.
