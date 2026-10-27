# 2026-10-27 PVP 별도 브랜치 기획·개발 계획

## 요구사항

- 사용자가 docs/content-design/pvp/에 개발용 로드맵과 시스템/콘텐츠별 PVP 규칙 작성을 요청했다.
- 기존 docs/content-design/ 구성을 참고하고, 별도 개발 브랜치를 먼저 추가하도록 요청했다.
- 실제 사용자 운송 경쟁, 운임의 충분한 승패 기여와 운전 점수, 자체 제작 맵, 민첩한 조작이 기준이다.
- 경기 시간은 아직 정하지 않았다. 이전 10분 예시는 확정 규칙이 아니다.

## 작업 기준과 범위

- 기준 커밋: main 60b15edd6094d9a6383de4d0c75c4a40d49eda40.
- 새 브랜치: feature/pvp-prototype. 위 기준 커밋에서 GitHub 커넥터로 생성했다.
- 이 작업은 문서만 다룬다. 게임 코드·씬·맵·Unity 패키지·실행파일·배포 manifest·사용자 .mmst의 변경은 없다.
- 로컬에 원본 저장소 체크아웃은 없으며, 고정 커밋의 참고 사본과 문서 초안을 분리해 편집했다. git status는 로컬 저장소 부재를 확인했다. 원격 branch/tree 비교로 변경 범위를 검증한다.

## 작성한 내용

| 묶음 | 산출물 |
|---|---|
| 모드·경기 | 문서 입구, 경험 목표/대안, 경기 상태·마감·이탈 정책 초안 |
| 조작·맵 | 빠른 가속·조향·방향 전환, 교통 판정, 자체 제작 맵과 콘텐츠 카탈로그 |
| 운송·승패 | 공용 승객 소유권, 약정 운임, 전체 주행 감점, 정산 원장과 승자 자격 |
| 화면·기술 | 주행 정보·결과 설명, 서버 권한·예측, 별도 시작점·빌드·저장 |
| 계획·검증 | 단계별 로드맵, 결정 상태·시험 설정, QA 항목·실제 플레이 게이트 |

기존 기획 README에는 PVP 문서 입구만 연결한다. 캠페인 본문의 규칙과 완료 체크는 변경하지 않는다.

## 변경 파일

- docs/content-design/pvp/README.md
- docs/content-design/pvp/00-mode-overview.md
- docs/content-design/pvp/01-match-flow-and-rules.md
- docs/content-design/pvp/02-driving-and-traffic.md
- docs/content-design/pvp/03-map-and-content.md
- docs/content-design/pvp/04-passengers-and-fares.md
- docs/content-design/pvp/05-scoring-and-results.md
- docs/content-design/pvp/06-ui-and-feedback.md
- docs/content-design/pvp/07-network-and-build.md
- docs/content-design/pvp/08-development-roadmap.md
- docs/content-design/pvp/09-decisions-and-balance.md
- docs/content-design/pvp/10-playtest-and-acceptance.md
- docs/content-design/README.md
- docs/work-history/LATEST.md
- docs/work-history/2026-10-27-pvp-planning.md

## 검증 기록

### 자동 문서 검사

- 게시용 로컬 문서 사본에서 `python pvp-design-work/validate_docs.py`를 실행해 종료 코드 0을 확인했다. 임시 검증 스크립트와 참고 사본은 저장소 반영 대상이 아니다.
- 의도한 문서 15개만 존재하며, PVP 본문/입구 12개·상위 기획 입구 1개·작업 이력 2개로 구성된다.
- 상대 링크 253개, 제목 앵커 9개, 표 78개를 검사해 오류 0개다. 기존 대상은 고정 기준 커밋의 트리와 대조했다.
- QA ID 19개와 결정/미결정 ID 26개를 대조했다. 제품 작업 체크 44개는 모두 미완료이며, P0의 문서 작성·검사만 완료로 표시했다.
- UTF-8·줄바꿈·코드 블록·상태 표기·구식 설정 이름 혼용을 검사했다. 파일별 Git blob SHA를 산출해 원격 게시 내용과 대조할 기준을 만들었다.
- 상위 기획 README는 PVP 연결 섹션 외 원문이 같고, LATEST의 기존 캠페인 인계 본문은 그대로 보존됨을 확인했다.

### 내용 검토

- 기준 기획과 코드의 시작·지도·저장·빌드 결합을 대조하고, 현재 사실과 후속 제안 경로를 구분했다.
- 담당자 교차 검토와 별도 읽기 전용 검토에서 운임 원약정·빈차 감점·사건 상향분·원자적 승차/정산·경계 복구·마감 틱을 대조했다.
- 검토에서 발견한 재접속 유예 만료와 정산 지연의 자격 충돌을 최종 종료 틱의 자격 동결로 해소했다. 결과 확정 전/후 Host 종료 처리도 구분했다.
- 로드맵의 선행 부분 QA와 후속 전체 QA, 초기 조작 화면, 길안내 코드 책임, 시험 사건과 실제 법규 감지를 구분했다. 해당 수정의 재검토에서 남은 중요한 문제는 없었다.
- 경기 시간·인원·배점·충돌·SDK를 사용자 확정으로 오기하지 않았는지 확인했다. 수치 후보와 미정 설정은 09에서 관리한다.

### 실행하지 않은 검증

Unity Editor·Player 빌드·규칙 코드 자동검사·실제 두 PC 접속·사람 플레이는 수행하지 않았다. 본문에 적힌 제품 QA와 Unity 명령은 후속 계획이며 이번 문서 검사의 성공으로 대체하지 않는다.

게시 후 확인 기준은 새 브랜치의 실제 head, 기준 커밋 대비 문서 15개 차이, 로컬/원격 파일별 Git blob SHA 일치다. 기준 main으로 병합하는 작업은 포함하지 않는다.

## 미구현·미검증

- PVP 실행파일, 전용 맵, 차량 조작, 네트워크와 경기 기능은 모두 미구현.
- Photon Fusion 2 Host는 권고 후보이며 실제 차량 예측 검증 전 최종 채택 아님.
- 경기 시간·최종 인원·배점·충돌·마감 유예·공개 운영 범위는 [결정 대장](../content-design/pvp/09-decisions-and-balance.md)의 상태를 따른다.
- 작업 환경에서 Unity와 실제 다른 PC 플레이를 실행하지 않았다. 문서 검사를 제품 검증으로 기록하지 않는다.

## 실행·재현 및 인계

- 현재 실행 가능한 기존 게임은 builds/EungyeTaxi/EungyeTaxi.exe, 기존 빌드는 PrototypeBuild.BuildHybrid다. 이번 문서 작업으로 새 실행파일을 만들지 않았다.
- 다음 작업은 [개발 로드맵](../content-design/pvp/08-development-roadmap.md)의 독립 실행·자체 제작 시험장·조작 비교부터 시작한다.
- 첫 온라인 검증은 실제 두 클라이언트의 예측·권한·승객 소유권을 포함한다. 상세 기준은 [QA](../content-design/pvp/10-playtest-and-acceptance.md).
- 필수 로컬 지도 입력과 배포 manifest는 변경하지 않았다. 실제 PC에서 기존 시뮬레이션 개발을 시작할 때는 AGENTS.md의 작업공간 점검을 따른다.
- 본 기록 자체의 커밋 해시는 추측해 적지 않는다. 확인 명령: git log -1 --oneline -- docs/work-history/2026-10-27-pvp-planning.md.
