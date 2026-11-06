# 2026-11-06 승객 효과 밸런스 v1 적용

## 요청과 범위

선택지 독립 효과 시스템에 맞춰 승객 요청·대화·공통 영향을 전반 점검하고 첫 플레이 기준값을 적용한 뒤 사용자가 관리창에서 조정하도록 요청했다. [수치표와 근거](../passenger-balance-v1.md)를 따른다. 새 판정/게임 기능을 구현한 작업이 아니다.

## 실제 적용

- 저장된 default(schema2)의 대사·공통 설정을 기반으로 선택 효과 수치를 재조정했다. 저장 전 default revision2의 사용자 시험값16개를 확인했으며 텍스트6개는 모두 유지, 숫자는 이번 요청에 따라 재조정했다. 활성 파일은 구 schema1 출하 기본값이었다.
- 단발 응답 즉시 +1~3, 실제 운행 종료 +3~6, 실패 -3~-4, 미응답 -2. 공감/지역 적극 대화 피로 +1회, 음식 +3, 멀미 M 추가 -1. 음악은 실제 변경 미구현이므로 추가 보상0.
- 모든 선택지 비율·점/분·자연 피로 추가율·중립·보류 효과0. 기존 사용자 ETA 추가 피로/종료% 및 Quiet 보류 자연 피로 -50% 시험값 제거. 기본 자연 피로/법규·운임·기한/발생 확률은 유지.
- 기본 편집 프로필 49개 값 변경, default revision3 저장 및 active에 적용. 별도 passenger-balance-v1 프로필 저장, 배포 가능한 기준 JSON은 config/content/presets/passenger-balance-v1.json으로 버전 관리.
- 적용 hash: 3911f84cf12046e338e486050dbd80cb25f3b30b7f7f49573fcde2d9ff270d28.
- 원본 백업: config/content/backups/1793946992812-default-before-balance-v1.json 및 1793946992817-active-before-balance-v1.json. 관리 서버 save/apply의 자체 백업도 생성. 타 프로필·저장 게임은 변경하지 않음.

## 검증 및 결과

- 관리 서버 validate 및 실제 배포 게임의 contentPreview 호출11개: Quiet 성공, Comfort 성공/실패, OnTime 실패/중립, ETA빠르게, Empathy 수락/대안, Quiet보류, Music, Food. 기대 S/피로와 모두 일치. artifacts/passenger-balance-v1-review.json에 변경49개 및11코어 결과 기록. 실행 스크립트 artifacts/apply-passenger-balance.mjs --apply exit0. 이 스크립트는 새 프로필 생성/저장을 하므로 재적용용으로 반복 실행하지 않는다.
- 저장/active/보관 preset3개 전체 스키마·해시 검증 및 values 동일. 실제 선택 정의71개를 읽어 서로 다른 요청 선택 조합1420개 계산, 최대 요청 보상10 확인. artifacts/passenger-balance-validation.log, exit0.
- 모든 비율/시간 누적/보류/중립 효과0과 사용자 대사 전부 보존 assertion 통과.
- tools/update_distribution_manifest.ps1 -VerifyOnly **exit1**, 동시 다른 작업의 배포 갱신 중 boot.config hash 불일치. 이번 설정 이외 배포 파일을 수정하거나 manifest를 다시 생성해 오류를 덮지 않았다. active는 별도 허용 사용자 파일이며 해당 JSON 자체 검증은 통과했다. 다른 작업의 실행본 갱신 완료 후 전체 배포 검증은 그 작업에서 필요하다.
- 이번에는 Unity 빌드·위치 제어·자동 물리 주행·수동 플레이·전체 캠페인 검증을 수행하지 않았다. 미리보기는 효과 계산 검사이며 자연 피로와 실제 주행 성공/실패 관찰을 재현하지 않는다. 사람 플레이 기반 최종 밸런스 확정은 미완료.

## 변경 파일과 인계

버전 관리: config/content/presets/passenger-balance-v1.json, docs/passenger-balance-v1.md, docs/content-tuning-tool-guide.md, docs/prototype-development-checklist.md, 본 이력, docs/work-history/LATEST.md. 로컬 사용자 설정: profiles/default.json, profiles/passenger-balance-v1.json, builds/EungyeTaxi/content/active-profile.json 및 backups. 기존 출하 기본 catalog/default-profile·게임 소스는 이번 작업에서 변경하지 않았다.

실행 기준은 로컬 C:/git/project-b, 설정 관리.cmd. 이미 열린 편집창은 미저장 작업을 먼저 보존한 후 불러오기 → default. 다음 새 싱글 캠페인/새 PVP방 적용, 이어하기/현재 운행은 기존 snapshot. 기본값으로 버튼은 구 출하값이며 이번 값으로 돌아가려면 passenger-balance-v1을 불러온다. 기본 게임 builds/EungyeTaxi/EungyeTaxi.exe / 은계 싱글·A2 PVP/LANv9/BuildHybrid. 필수 지도 입력/ZIP 미변경·Strict 미재실행. 동시 개인 공간/조작 설정/제안서 작업과 기존 외부 worktree를 보존했다.
