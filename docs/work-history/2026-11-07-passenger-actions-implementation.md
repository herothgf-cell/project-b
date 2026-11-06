# 2026-11-07 승객 행동 팝업 구현

승인 설계: ../superpowers/specs/2026-11-07-passenger-actions-design.md. 행동만 팝업/일반 발언은 카드, 기존 모델·시간·수치·자동승하차 유지.

## 구현 진행
PassengerActionPresentation/UI를 추가하여 깨우기/적재/보호/하역/음식정리/재고정/경유대기를 기존 모델에 연결. 도착15초 후 깨우기와30초 자동하차 유지. 접기는 표시만 변경하고 카드에서 재열기, 기존 요청/상황/지도/메뉴와 우선순위 조정. 일반 답변/후속 발언은 카드 스크롤. 숫자1~n·기존F 유지.

## 검증 진행과 발견
- TDD: 표시 계약 누락 Editor 검사 실패 확인→상태/접기/배치69통과. 통합 BuildHybrid exit0.
- 첫 실제 행동 Player59개 중 복원 직후 적재창 가림1건 실패. 탑승전 legacy 요청이 적재창을 막는 표시 우선순위를 수정. 모델/저장 정책은 변경하지 않음.
- 독립 코드 검토2건: 음식정리 중 도착을 하역으로 오인, 작업 중 상황 선택지가 숨겨짐. 실제 UnloadRemaining 구분/선택팝업 우선으로 수정, 해당 Player 검사 추가. 수정 후 최종 검증 대기.
- 실제1280/1920 깨우기·적재 화면 캡처 확보. 이 단계는 개발 중 보존이며 출하 완료 주장이 아님.

## 통합·남은 작업

### 1차 통합 검증 완료 / 동시 작업 대기
- 소스 cce12ee, 비 프로토 보존 병합6ba7cb1. 통합 Editor69·BuildHybrid exit0.
- 실제1280/1920 행동 Player67 통과. 첫 숨김 실행은 초기 캡처3장 빈 화면으로 exit2였으며 표시 창으로 재실행해 모든 캡처/상태 검사 통과.
- 현행 프리셋 Player131·개별 효과 Player57, 비 Player20·RainPolicy11, 실제 loopback LAN 호스트40/클라이언트51 통과. 사용자 저장16개 SHA256 변화0. Strict 입력 issues0.
- 기존 전체 승객 검사는 음식 허락 만족도5 고정 기대값에서 checks92로 실패. 변경 전 D main 실행본도 동일 실패를 재현했으므로 이번 UI 회귀와 구분한다. 기존 검사는 수정하지 않았다.
- 독립 재검토에서 두 지적 수정 확인, 추가 확정 문제 없음. 실제 화면은 docs/ui/2026-11-07-passenger-actions/implemented-wake.png 및 implemented-load.png.
- 정리 직전 새 승객 특성 확장 브랜치와 D main 날씨 설정 미커밋 작업 발견. 사용자 결정: **다른 작업 완료 후 전체 통합·정리**. 진행 중 브랜치·파일은 보존하며 최종 main push/삭제는 대기 중.
- 재현: Unity executeMethod PassengerActionChecks.Build; Player -brightUiCheck -passengerActionReview; tools/test_rain_player.ps1; tools/test_pvp_native_lan.ps1 -Port 18977. 상세 로그 artifacts/passenger-actions/와 artifacts/pvp/native-lan-20261107-014724-24b0373b.
로컬 main에 완료된 비 프로토타입(a0ed24f)와 운전점수 검토 문서가 있어 보존 통합 후 최종 빌드/화면·LAN·기존승객 회귀, manifest·이력·main push 및 브랜치 정리 예정.
필수 지도/ZIP 변경 없음. 사용자 저장 보호 기준 artifacts/passenger-actions/protected-files.json(16개). 실제 사람 주행/전체3일/원격PC 미검증. 기본 실행 builds/EungyeTaxi/EungyeTaxi.exe, BuildHybrid.
