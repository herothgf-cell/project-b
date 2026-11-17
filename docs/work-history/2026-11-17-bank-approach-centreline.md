# 2026-11-17 은행사거리 북동쪽 중앙선 침범 수정

## 요청·실경 근거

- 제보: 은계 x=209.792358, z=548.4707, 북동행 33.986042도, J1927_5181 북동쪽 약35m에서 차량이 중앙선을 침범한다.
- 네이버 2026년 5월 [사거리 북동쪽](https://map.naver.com/p?c=19,0,0,0,adh&p=MPnR6iU-beh9-23u0XgJJw,34,-20,80,Float)과 [은행고길 본선](https://map.naver.com/p?c=19,0,0,0,adh&p=j8Ur4TbCTTo_KOSprwLleA,34,-20,80,Float)을 내부 브라우저에서 직접 확인했다. 본선은 1+1이고 교차로 앞에서 남행 회전 차로가 벌어진다. 기존 사용자 확인 남행 안쪽 직진·좌회전, 바깥 우회전 통행 권한은 유지한다.

## 원인·수정

r38의 SouthboundSurveyMapEdit가 남행 안쪽 연결 차로를 상류18m까지 늘리고 북행 쪽으로 최대2.9m 옮겼다. 남행 바깥 차로도 별도로 서쪽으로 확폭했으므로 안쪽 차로의 추가 이동은 필요하지 않았다. 실제 반대 방향 차로 중심 간격이 2.100012m로 줄었고, 정상 남행 경로가 표시 중앙선을 가로질렀다. 판정 완화로 숨길 문제가 아니라 차량 경로 오류다.

BankApproachR44는 원본 남행 안쪽 연결선을 기준으로 공통 상류 경로 뒤에서만 회전 차로를 분기한다. 짧은 종단부 안에만 최대1.4m의 완만한 변위를 주어 같은 방향 회전 차로 간격도 확보한다. 잘못된 상류 백색 분리선 두 개를 제거하고 정지선 중심·폭을 새 단면에 맞춘다. 경로 ID·기존 회전 허용·북행 경로를 유지한다. 차로 치수는 기존 지도 기하이며 현장 실측값으로 주장하지 않는다.

## 검증

- 수정 전 BankApproachRegression.Run 종료1: 반대 방향 최소 간격2.100012m, 정상 경로의 실제 노면 중앙선 교차1회. bank-approach-red.log.
- 후보 BankApproachR44.Preview 종료0: 최소 간격5.000008m, 중앙선 교차0회. bank-approach-preview.log.
- 실제 원본 노면선과 생성 도색을 CentreLineCrossing에 넣고 0.25m 경로 간격으로 검사한다. 차로 연결 끝점과 직진·좌회전/우회전 전용 권한도 확인한다.
- 초기 후보는 반대 방향 간격을 해결했지만 정지선의 같은 방향 간격이1.832343m였다. 추가 회귀 검사로 실패를 재현한 후 짧은 종단부만 보정했다(bank-r44-terminal-correction.log 종료0). 최종 반대 방향 최소4.049891m, 정지선 남행 두 차로3.227346m, 정상 경로 중앙선 교차0회.
- 최종 지도r44 콘텐츠 지문: 4bc4690b4e3fd618571b4ed82a53e5bb1f49e5c3339a7a6af32fd5a403cb901f. 원본 baseline과 OSM 입력은 변경하지 않았으며 작성 지도 입력 manifest를 실제 크기·SHA256으로 갱신, Strict 종료0(필수 입력 문제0).

## 변경·재현

- 작성 지도 r44, BankApproachR44, BankApproachRegression, BusinessProgress의 정확한 지도 버전 저장 이관, JunctionRouteFeedbackTests, RoadPresentationProbe.
- Unity batchmode의 BankApproachR44.BuildVerified로 회귀 검사 후 통합 실행본 생성. tools/check_road_continuity_player.ps1로 최종 실행본의 제보 시점·상공 화면과 실제 중앙선 판정을 확인한다.
- 자동 경로 샘플과 고정 카메라 검증이며 수동 물리 주행·전체 캠페인 완주와 구분한다. 사용자 씬·개인 콘텐츠를 변경하지 않았다. 작업 중 사용자가 기존 개인 ZIP을 제거했으며 이 작업에서는 삭제하지 않았다.

## 최종 회귀 결과

최종 bank-r44-final-build.log에서 반대 방향4.049891m·남행 정지선3.227346m·정상 경로 중앙선 교차0회, 기존39승하차1,482경로 및 시설31곳 검사 통과. 도색25,967점 누락0, 곡선403검사, 생성횡단71/작성24 검사 통과. r14~r43 저장30버전 이관937검사 통과(점수·계약 보존, J1927의 이전 경로/교차로 관찰만 무효화).

최종 BuildHybrid 종료0. 최종 실행본 ROAD_SIGNAGE_PLAYER_OK 331검사, 전체99타일·4008차로/도로 영역 촬영 통과. [제보 시점](../content-design/images/eungye-bank-approach-reported.png)과 [상공](../content-design/images/eungye-bank-approach-overview.png)을 직접 검토했다. 고정 카메라/경로 샘플 검사이며 수동 주행을 주장하지 않는다.

실행본 이관 검사 첫 시도는 이전 테스트가 이미 2일차까지 진행한 파일을 탑승 중 fixture로 복사하여 첫 조건에서 실패했다. 게임 코드 오류로 취급하거나 검사를 생략하지 않고, 보관된 r43 실행본의 liveSaveTest로 탑승 중 자료를 다시 생성해 재검증한다.


재생성한 r43 탑승 중 저장 → r44 이어하기23검사, r44 새 저장9검사/새 프로세스 이어하기23검사 모두 종료0. 검사 실패는 완료된 fixture 사용으로 확인되었으며 게임 소스 변경 없이 해결했다. 실제 실행 로그: r43-fixture-save.log, r44-reload-r43.log, r44-live-save.log, r44-live-reload.log.
