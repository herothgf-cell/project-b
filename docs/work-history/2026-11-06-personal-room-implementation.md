# 작은 방과 첫 구매 구현

사용자가13 기획/시안을 승인하고 현재3일 프로토타입의 정산→내 공간, 조명/러그 구매, 다음 날 유지 구현을 요청했다.

구현 계획: docs/superpowers/plans/2026-11-06-personal-room.md. 계정/소유 및 UI/체크포인트를 분리 구현하고 실제 Player 격리 검사로 통합한다. 방 배경은 생성된 고정 이미지이며 Assets/Resources/personal-room-base.png로 포함했다.

초기 검증: check_workspace.ps1 -Strict 입력 issues0. 기존 test_accidents.ps1은 ChoiceEffects 소스 누락으로 컴파일 실패하여 통과로 세지 않는다. 새 개인 방 순수 테스트는17검사 통과(최종 결과 후속 기록). 게임 사용자 저장은 격리된 fixture 외 수정하지 않는다. 동시 운전 설정 작업/제안서 변경은 이번 커밋에서 제외한다.

실행은 builds/EungyeTaxi/EungyeTaxi.exe의 싱글3일 영업 마감→내 공간. 최종 빌드/화면·복원 검사와 manifest 결과는 완료 후 아래 추가한다.

구현: CampaignAccount의 Room/개인 구매 거래와 Net/Expense 분리, BusinessProgress의 후보 장부 저장/실패 롤백·구 완료 저장 읽기 전용·날짜 이월, OneDayHUD 선택 진입, PersonalRoomHUD의 배경/물건 레이어·찜·미리보기·확인·보관/스타일/조명 조작을 추가했다. 완료 회차 Room을 보관하며 최신 이전 회차 방을 읽기 전용으로 볼 수 있다.

검증: 새 순수17검사 통과. 기존 prototype 묶음의 계정/3일/수명주기 등은 통과했으나 기존 ManualHandlingTests의 ordinary S braking actual8.823529 실패로 전체 exit1(이번 변경과 분리). 사고 실행기 누락 의존성을 명시한 별도 절대경로 컴파일로 AccidentTests77검사 통과. 첫 컴파일 명령은 상대경로의 csc 인수 해석 오류로 실패했고 절대경로로 수정했다.

검토: 구 완료 저장에서 방이 없을 때 쇼핑 소급 허용 문제를 수정하고 JsonUtility의 누락 중첩 필드 합성도 ParseCheckpoint에서 정규화했다. 확인창 뒤 버튼 입력 차단과 제목 대비를 수정했다. 처음방 비교, 카드 숨김/상품 이미지, 전체 과거방 탐색은 이번 최소 구현의 남은 항목이다.

빌드 시 첫 실행은 로그 없이 종료됐고 두 시도는 UnityLockfile 잔존으로 충돌했다. 실제 Unity 프로세스가 없는 것을 확인한 뒤 해당 잠금만 해제하여 BuildHybrid 성공(exit0)을 확인했다. 구 저장 경계 최종 수정 후 다시 빌드/실제 Player 검사한다. 자동 위치/정산 fixture는 실제 물리 주행·사람3일 검증을 대신하지 않는다.

최종 실제 검증: BuildHybrid 로그 artifacts/personal-room-verified-build.log exit0. 검사용 문자열 문법 오류1건(CS8997)을 수정한 뒤 다시 빌드했다. tools/test_personal_room_player.ps1의 실제 Player36검사와 새 프로세스 재실행 검사 exit0. 구매/순수익 유지·중복/미납·원자 저장 실패 롤백·둘째/셋째 날·JSON 누락 필드·최종 읽기 전용 복원을 검증했다. 숨김 창 캡처는 검은 화면으로 시각 검증에 사용하지 않으며 표시 창으로 다시 확인한다.

동시 운전 설정 소스(DriveSession/TaxiController/PVP/PrototypeBuild 등)가 작업 폴더에서 변경 중이다. 이번 소스 선택 커밋에서는 제외한다. 로컬 BuildHybrid는 현재 공유 작업 폴더의 통합 소스로 빌드하므로 이 별도 운전 변경이 실행본에 포함될 수 있으며 그 기능 완료를 이번 방 검증으로 주장하지 않는다. 게임 사용자 저장은 fixture 경로 밖에 쓰지 않았다.
