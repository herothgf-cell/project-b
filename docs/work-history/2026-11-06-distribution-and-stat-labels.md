# 2026-11-06 배포 검증 완료·게임 지표 명칭 정리

## 요청과 구현

남은 배포 검증, 새 밸런스 게임 적용/실행, 취객·수면 빈도 설명, 효과가 바꾸는 실제 게임 지표 명확화를 요청했다.

- 다른 개인 공간 작업의 통합 빌드/manifest 갱신 완료 후 재검증했다. 이번 작업은 바이너리나 manifest를 수정하지 않았다. 배포는 8ab2dec의 cb582a7 기반 통합 실행본, 은계 싱글/A2 PVP/LANv9/BuildHybrid. 동시 운전 설정 작업은 보존.
- 관리창 새 탭에서 default의 **게임에 적용** 버튼을 실제 클릭했다. active hash 3911f84cf12046e338e486050dbd80cb25f3b30b7f7f49573fcde2d9ff270d28 그대로 확인. 기존 탭/미저장 작업은 보존.
- 같은 관리창에서 **분리 테스트 실행** 클릭. PID39520의 기본 EungyeTaxi.exe가 -contentTest -contentProfile artifacts/content-editor/17958d1590726e68ebfd15ed.json으로 실행됨을 확인. 게임 로그 startup gameplay 완료, 프로필 경고/예외 검색 없음. 임시 저장 사용으로 기존 캠페인 변경 없음. 테스트 실행은 사용자 조작용으로 남겼다. 네이티브 화면 캡처는 가려진 Chrome 화면으로 반환되어 게임 렌더 확인 증거로 세지 않았다.
- 오염 짐/부스러기/흘림 항목 제목을 **차량 청결도 변화**로 명시. 입력 아래 영향 지표(0~100)와 음수/0의 의미를 짧게 상시 표시. 상세 설명은 기존처럼 접어 두며 오염 짐에는 100→95·적용 시점·직접 S/피로 변화 없음·청결도80 이상 승차 기본+3/30 미만 세차 필요 설명 추가.
- 선택지의 기사 피로 증감/변화율/분당 항목을 **기사 피로도**로 통일. 카탈로그 표시 이름은 관리창 메모리에서만 변환해 생성 원본/프로필/게임 감점 의미 보존.
- 취객의 별도 배정 안내와 수면 가중치의 적용 후보/전체확률 차이를 관리창에 추가.

## 등장 방식 확인

VehicleCare.ReservePassenger/CommitPassenger 기준: 특수 승객 뒤 다음 탑승은 General. 나머지 차례에서 하루 취객2명 미만이면 기본30% 추첨, 취객 없이3회 탑승한 경우 다음 가능한 차례에 보장. 취객이 선택되지 않은 차례에는 별도30% 기준 특수 유형 추첨 후 가능한 유형 사이의 상대 가중치를 사용한다. 취객은 type.weight 대상이 아니다. Sleeping은 특수 유형 가중치 대상, 모두1이면 가능한 후보 간 동일 비중이며 전체 탑승의 동일 비율 보장 아님. 취객의 잠듦은 별도30%, 구토25% 조건이며 Sleeping 가중치와 독립. 이번에 등장 빈도 수치는 바꾸지 않았다.

## 검증

- UI 선택/연결 tests/content-editor/choice-ui.test.mjs 및 context-ui.test.mjs:23개 통과, exit0(최종 로그 artifacts/clear-stat-ui-tests.log).
- 통합/PVP alias VerifyOnly 모두 exit0: 실제142파일118,385,564바이트, 출하141파일118,253,104바이트. active 사용자 프로필 별도 유효성 통과. artifacts/balance-distribution-verified.log, balance-pvp-verified.log. 지난 작업의 boot.config 불일치 해결 확인.
- 실제 관리창: 적용 완료 문구, 오염 짐 -5와 차량 청결도 설명 펼침, 분리 테스트 시작 완료 문구 확인. artifacts/clear-cleanliness-editor.png 캡처.
- 게임 새 빌드/물리 주행/사람 장시간/전체 캠페인 검사는 이번에 수행하지 않았다. 기존 작업의 빌드·Player 검사와 이번 파일 검증/프로필·실행 확인을 구분한다. 필수 지도/ZIP 미변경, Strict 미재실행.

## 실행·후속

기본 실행 builds/EungyeTaxi/EungyeTaxi.exe. 새 싱글 캠페인/새 PVP 방에서 active 사용, 이어하기는 기존 snapshot. 현재 켜 둔 분리 테스트에서는 싱글/영업 시작으로 시험하며 정규 캠페인을 바꾸지 않는다. 관리창은 새 탭의 효과→흙 묻은 짐에 개선 설명이 표시된다. 이후 프로필 수정→저장→게임에 적용. 관련 안내 docs/content-tuning-tool-guide.md.
