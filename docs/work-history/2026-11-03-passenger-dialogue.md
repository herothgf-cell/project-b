# 2026-11-03 승객 대화 UI·스크립트

## 요청·실제 변경

첨부 시안 기준으로 선택 가능 여부가 드러나는 대화창, 승객 대사/선택지/효과 분리, 구체적인 기사 대사, 운행 중 선택, 방향키 조작 안내를 요청받았다. 반복 리뷰/광범위 검증을 줄이는 기존 사용자 지시를 유지했다.

- PassengerDialogueUI 공통 흰 카드·파란 말풍선·테두리와 키가 있는 선택 버튼·마우스 반응·효과 별도 행. 빈 효과는 효과 없음. 기존 요청/유형/실제 상황 모두 적용.
- 운행 중 대화 선택 허용. 새 요청 발생 시점, 미응답 불이익의 기존 정차 기회 판정은 유지. 안전 정차/실제 적재·하역·음식 정리·짐 재고정/용무 작업 조건을 바꾸지 않음. 일시정지·지도/결과·다른 선택·수면·도착 잠금·중복 응답 방지 유지.
- 유형별 기사 선택을 실제 문장으로 교체. 신호 대기/제동/잠 깸/음식/짐 상황마다 구체적인 설명, 선택 후 기사 답변 표시. 요청의 내부 상세 기록과 화면 발화를 분리. 보류 선택은 실제로 보류임을 설명하고 나중에 자동 처리된다는 약속을 하지 않음.
- DriveHUD/OneDayHUD/BusinessHUD/HdRouteTrial 및 이전 PVP 시험 안내를 방향키 기준으로 수정. WASD 실제 입력은 유지.
- 17요청/유형/실제 상황 스크립트를 `docs/content-design/07-passenger-dialogue.md`에 표로 정리, 기존 승객·화면 문서와 연결. 음악·에어컨·창문 실제 조작 보류 유지.

주요 변경: PassengerDialogueUI/PassengerBehaviorUI/PassengerRequestObservation/PassengerRequestUI/PassengerBehavior/PassengerSituations/PassengerJobs/PassengerJobsActivities, 위 HUD 안내, PassengerActivitiesProbe, 관련 기획·계획·이력.

## 검증 범위·초기 결과

`PrototypeBuild.BuildHybrid` 종료0 (`artifacts/dialogue-build-final.log`, `.exit`). 첫 실행을 기다리는 셸을 유지하지 않아 종료 결과를 회수하지 못했고, 중복 실행은 프로젝트 잠금으로 종료했다. 중복 종료 후 별도 로그의 정식 빌드는 실제 종료0이다.

통합 exe `-oneDayMode -hybridV2 -passengerActivitiesTest -dialogueCapture` 실제 종료0,60검사 통과 (`artifacts/dialogue-player.log`, `passenger-activities-player.txt`). 전용 artifacts 저장으로 실제 응답·저장 adapter를 위치/속도 제어해 검사한 것으로 수동 운전/전체 캠페인 검사가 아니다. 운행 중 요청 수락/음식 허용+5/같은 프레임 중복 차단/일시정지 차단/운행 중 짐 작업 대기/안전 정차 후5초 완료 및 기존 용무 저장 검사를 포함한다.

1600×900 실제 렌더 요청·음식3선택·신호 대기·답변4캡처 (`artifacts/dialogue-*.png`). 요청·음식·신호 화면의 대사·키·효과 구분과 글자 경계를 직접 확인했다. 다른PC/장시간/전체 경제·성능 검사를 반복하지 않는다.

게시 전 원격 main의9개 후속 커밋(PvP 복제 경로/포즈·알림·HUD)을 발견했다. 최종 배포는 이를 보존해 통합한 뒤 빌드한다. 아래 최종 결과는 실제 실행 후 추가한다.


## 원격 통합·효과 표시 보완

원격 main f2f0a37까지를 병합54ff3fb로 보존했다. 변경 겹침은 PassengerJobs/PassengerRequestUI의 서로 다른 구간이며 자동 병합 후 기존 네트워크 경로 유지 분기와 신규 대화 입력 분기가 함께 존재함을 확인했다. 잠 깸 응답이 다시 수면으로 이어지는 효과/짐 작업 연료 소모를 명시했고, 음식 발생 수치는 승객 말풍선 밖 효과 행으로 이동했다. 대표 요청의 긴 내부 기록을 승객 발화에 반복하지 않는다. 최종 소스·실행 검증/배포는 아래에 기록한다.


최종 통합 d4da20e의 BuildHybrid 종료0/실제 응답60·새 프로세스 복원6 종료0/사용자 저장6파일 SHA256 불변을 확인했다. 캡처 fixture의 Freeze가 PassengerRequestUI 컴포넌트를 비활성화해 요청창 첫 이미지가 누락된 것을 열람 중 발견했다. 캡처 동안만 해당 컴포넌트를 켜도록 수정하고, 일반/유형별 상황 답변의 효과를 말풍선 밖에 분리했다. 이 보완 이후의 최종 결과는 아래에 추가한다.


## 최종 실행본 검증

- 소스 `101abf74a88ca318fd2e495d5dcdad97c147be7f`, `PrototypeBuild.BuildHybrid` 실제 종료0 (`artifacts/dialogue-delivery-build.log`/`.exit`).
- 최종 exe `-oneDayMode -hybridV2 -passengerActivitiesTest -dialogueCapture`: 실제 종료0/60검사. 같은 exe의 `-passengerActivitiesReloadTest -batchmode -nographics`: 종료0/6검사. 전용 artifacts 저장만 사용했다. 로그는 `artifacts/dialogue-delivery-passengerActivitiesTest.log`와 `dialogue-delivery-passengerActivitiesReloadTest.log`다.
- 네 대화 화면의 실제1600×900 렌더를 [요청](../ui/2026-11-03-dialogue/request.png)/[음식](../ui/2026-11-03-dialogue/food.png)/[상황](../ui/2026-11-03-dialogue/signal.png)/[기사 답변](../ui/2026-11-03-dialogue/reply.png)에 보존했다. 요청창 활성화 후 재캡처한 최종 요청/답변 이미지에서 대사·선택키·효과의 영역과 줄바꿈을 열람 확인했다. 위치/상태 제어 장면이며 전 HUD·수동 주행 수용으로 확대하지 않는다.
- 사용자 저장6파일 SHA256 불변. `git diff --check` 종료0. 지도/OSM/ZIP·제품명/저장 위치·사용자 마인드맵은 변경하지 않았다.
- 배포 실행폴더 **140파일/115,432,830바이트**. `tools/update_distribution_manifest.ps1 -VerifyOnly` 종료0, 파일 크기·SHA256 전수 일치. 새 `level0.resS`도 실행 폴더에 포함한다. 생성 DoNotShip 폴더는 경로 확인 후 artifacts로 옮겼다. 초기 자체 빌드만 별도 Git stash로 보존했으며 통합본으로 되돌려 적용하지 않았다.
- 추가 리뷰·LAN/다른PC/장시간/성능/전체 캠페인 검사는 반복하지 않았다. 원격 main의 해당 기능 수정과 이전 검증 이력은 그대로 보존한다.

재현: 통합 `builds/EungyeTaxi/EungyeTaxi.exe` → 은계 영업 또는 통합PVP → 승객 대화 표시 → Y/I/O/U 또는 클릭으로 선택. 운행 중에도 대답할 수 있으나, 짐/음식 정리/재고정은 안전한 정차 상태에서만 시간이 진행된다. 도움말은 방향키 기준이다. 실제 음악·에어컨·창문 조작은 계속 보류한다.

자동 승인 검토가 생성 장면 파일의 복원을 미커밋 폐기 위험으로 거부했다(명령 미실행). diff를 확인해 빌드가 재생성한 Mode Selection 오브젝트 fileID만 변경됨을 확인했으며, 복원하지 않고 해당 파일도 배포 커밋에 보존한다.

## 게시 영수증

배포 커밋30d22fede139ee19397010bd046a3a6cbc63c02f를 origin/main에 게시하고 원격 동일 해시를 확인했다. 원래 C:/git/project-b의 main/실행 폴더도 fast-forward했으며140파일115,432,830바이트 VerifyOnly 종료0이다. 사용자 마인드맵 두 파일의 SHA256과 기존 미추적 작업을 보존했다. 영수증 후속은 같은 실행본을 유지한다.
