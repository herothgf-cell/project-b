# PVP 참가자 자기 차 로컬 예측·호스트 보정

사용자 승인 범위: 자기 차 로컬 즉시 주행+서버 보정, A2/다른 작업 보존, 마지막 최소검사·통합EXE·main/feature push·C:/git/project-b 보존 fast-forward. 추가 승인 질문 없이 진행한다.

## 설계
- 참가자 local0만 기존 TaxiController/WheelCollider 동적 물리·로컬 입력으로 주행. 다른 차/NPC는 서버 snapshot 보간/kinematic 충돌체, 경제/법규/승객/판정은 계속host.
- 50Hz 고정 physics 입력에 단조 증가 번호, host는 bounded FIFO로 순서대로 한 tick당 하나 처리. snapshot에 실제 처리 번호 포함. 위치/속도를client에서서버로보내지않는다. 프로토콜v2로혼합실행본거절.
- 참가자 고정 tick의 body상태 히스토리와 ack상태 비교. 서버위치를현재위치와직접비교하지않고당시예측오차를현재body와미확인히스토리에점진적용한다. 큰위치오차/최초/경기종료/서비스는권한상태로재설정. 완전한PhysX롤백/재생은아니다.
- 키/패드는로컬매physics에서반영. 입력차단/timeout/서비스/끝/재접속/메뉴정리와바퀴고정표시유지. 동일조향/페달코드를호스트/예측에서공유. 서버안전판정·단일저장경로변경없음.

## 구현 파일
PvpNativePrediction.cs: bounded 입력큐·과거상태/점진보정·자기차예측. PvpNativeLan: v2/inputseq/FIFO/ack/lifecycle. PvpNativeReplica: local0 예측 활성/서버보정/바퀴시계, remote보간 유지. TaxiController: NetworkPredicted만기존물리허용·로컬바퀴. Native probe: 입력응답/수신보류/보정·재접속/알림 수용. 전용Editor helper.

## 마지막 필수검증
- 입력순서/중복/상한, 지연ack오차비교/재보정·큰오차/초기화/완료, wheel8/pose11 관련 Editor.
- 실제 두Player 로컬입력→서버수신전이동(진단상송신보류), steering/brake/reverse·수신공백·복귀/서버동기화·원격차보간·알림/재접속/메뉴정리.
- Windows최종실행, 파일/manifest SHA, A2 factory/road bytes·싱글/다른미커밋보존. 별도PC/사람장시간/FHD두창은자동완료하지않는다.

## Tasks
- [x] local owner prediction + bounded numbered inputs + ack/historical reconciliation.
- [x] final relevant Editor/Windows/two Player acceptance, actual results and limitations.
- [ ] canonical EXE/manifest/docs commit + atomic push + Cgit ff preserving user changes.
