# 2026-11-09 PC 교체 인계

요청: 다른 PC에서 현재 작업을 바로 이어갈 수 있도록 최신 자료와 준비 절차를 정리하고 main에 반영한다. 게임 코드/실행본/지도/개인 저장/출하 기본값을 바꾸는 작업은 아니다.

## 기준과 실제 준비

- 시작 main/원격 `0c490bad39c9f55428f1f9c672201b7c6dc4add4`. 사용자 마인드맵23646cc·제안서V6 0c490ba가 이미 게시된 상태를 확인하고 보존했다. 최신 게임 소스548fe8b/출하a978b3b/게시a0ecc05를 포함한다.
- [다른 PC 시작 문서](../handoff/2026-11-09/README.md)에 pull/검사/Unity2022.3.62f2/Windows Build Support/프로젝트 경로/BuildHybrid·은계/A2/LANv16/다음 확인/새 채팅 전달 문구를 정리했다.
- 현재 active-profile을 변경하지 않고 revision7 스냅샷으로 복사했다. 프로필 contentHash d22c06f57f400c4921d1466c05944a7f486cc038aab8ac5a0b4a5c6d79eb5b3a,132782바이트. 새 PC 관리창에서 선택적으로 가져와 적용한다. 기존11/06 revision6와 출하 기본값을 구분한다.
- 검증 요약/요청 메모와 로드뷰7장·최종 실제 렌더8장을 ZIP에 보존했다. ZIP6448078바이트/15항목. 인계6개 파일의 크기/SHA256 및 ZIP 내부 항목별 크기/SHA256을 files.json에 기록했다(목록 자기 자신은 제외하므로 폴더 파일은 총7개).
- 최신 사용자 첨부7장 원본 Temp 경로는 현재 없었다. 원본이 포함됐다고 표시하지 않고 이미지별 요구/좌표를 request-notes.md에 보존했다. 이전 채팅/기획21과 ZIP의 실제 로드뷰/결과 그림을 참고한다.
- CURRENT-STATE 상단과 실행 기준 표의 오래된 파일 수/통신 버전을 최신으로 갱신하고 work-history/README 및 LATEST 연결을 추가했다.
- 해당 날짜 인계 폴더에만 `.gitattributes -text`를 적용해 다른 PC checkout 시 줄바꿈 변환으로 스냅샷/목록의 SHA256이 달라지지 않게 한다. 게임 파일과 다른 문서의 변환 정책은 유지한다.
- 미추적 마인드맵 복사본/목차 복사본/10월26일 점검은11/06 historical-copies와 SHA256이 각각 같음을 확인했다. 원본은 그대로 둔다. 미추적 예전 unity/YeongtongTaxi는 PackageManagerSettings.asset1개로 개발 소스가 아니며 현재 프로젝트와 혼동하지 않는다. 캐시·로컬 Editor 설정은 그대로 보존하고 배포하지 않는다.

## 이번에 수행한 검사

- `tools/check_workspace.ps1 -Strict`: exit0, issues0. 정식 지도8fb257ed 검증, 재수입 JSON2개는 선택 입력 부재.
- 통합 및 PVP manifest VerifyOnly: 각각 exit0. 출하143파일127378253바이트, 유효한 로컬 프로필 포함144파일127511035바이트.
- snapshot `verify-profile.mjs`: exit0/CONTENT_PROFILE_VALID. 원본 active-profile 복사 전/후 SHA256 동일.
- ZIP15개 항목을 다시 열어 압축 해제 스트림의 전체 SHA256/크기를 원본과 대조했다. 인계6파일 SHA256은 files.json 기준으로 다시 검사한다.
- 기존 게임 BuildHybrid/Player31·34·169/LAN54·67 결과는 직전 실제 검증의 보관본이다. 이번 문서 작업에서 새 게임 빌드·플레이·전체 캠페인·다른PC 실행 검사를 수행했다고 간주하지 않는다.

## 다음 PC·별도 이동 범위

Git main만 받으면 실행/일반 개발을 이어갈 수 있다. 정식 프로젝트 지도가 있으므로 ZIP 재수입·지도 재생성·Unity Library 복사가 필요 없다. 지도/실행본/검증 파일 해시 불일치를 우회하지 않는다.

개인 캠페인 저장/개인 운전 설정은 Git에 넣지 않았다. 진행까지 옮길 경우 게임 종료 후 기존 PC LocalLow/Taxi Prototype 폴더를 별도 백업·이동한다. NGII/OSM 원시 자료는 새 재수입 때만 별도 준비한다. 다음 수용 대상은 사람 장기 주행·전체3일·실제 다른PC LAN이다.

인계 커밋 해시는 완료 후 `git log -1 --oneline -- docs/work-history/2026-11-09-pc-transfer.md`와 원격 main으로 확인하며 자기 해시를 추측해 문서에 쓰지 않는다.
