# 2026-11-12 수인로 J7973 남행 3→4차로

## 요청과 위치

사용자 화면의 미니맵, 정지선, 횡단보도, 신호등 배치를 이전 실행본의 위치 제어 캡처와 대조했다. 은계로에서 수인로 교차로 `J7973_3744`로 남행하는 북쪽 접근이다. 게임 확인 위치는 `x=796, z=404, 방향 183.68°`다. 북행 J8066의 이전 작업과 다른 접근이다.

네이버 2026-05 [남행 접근](https://map.naver.com/p/entry/coordinates/3zanLi,2AGD8X?c=15,0,0,0,adh&p=7vnm_N8HXCvrp53jDXLU3Q,-180,10,70,Float)과 [수인로 정지선](https://map.naver.com/p/entry/coordinates/3zanLi,2AGD8X?c=15,0,0,0,adh&p=lohUFQ3bJjGzrsILL9f15g,-180,10,70,Float)에서 위치와 차로를 대조했다. 상류 3차로, 정지선 앞 좌회전 1·직진 2·우회전 1은 사용자 요청을 따른다. 포켓 폭과 테이퍼 길이는 게임 치수다.

## 구현

- `config/maps/project/eungye.project.json` r16에 남행 중앙선 쪽 좌회전 포켓과 수인로 동향 기존 출구로 연결되는 좌회전 곡선을 추가했다. 남행 바깥 차로의 직진 분기만 비활성화했다. 두 기존 직진 차로와 우회전 연결, 북행 차로, 기존 지형 및 지도 기준 해시는 유지했다.
- 남행 정지선의 횡단 방향 길이를 2.5m 늘렸다. 동일 정지 평면에서 좌회전 포켓까지 덮으며 보호좌회전, 내비, NPC와 운전 판정은 같은 그래프를 사용한다.
- 이전 기본 프로젝트 지도 r14/r15 저장을 r16에서 불러올 수 있게 했다. 이번 변경으로 영향받은 J7973 남행의 진행 중 신호 관찰은 다시 시작한다. r14에는 J8066/J1927의 기존 전환 처리도 적용한다.
- 지도 생성 및 차로·연결 회귀는 `Editor/J7973SouthboundMapEdit.cs`, 저장 전환은 `Scripts/BusinessProgress.cs`에 있다. 관련 기획·범위·체크리스트와 입력 manifest를 갱신했다.

## 실제 검증과 재현

| 검사 | 결과 |
|---|---|
| 이전 출하본 `-brightUiCheck -poseReview "796,404,183.68"` | 위치 제어 렌더 9검사 통과, 첨부 화면과 교차로 확인 |
| `J7973SouthboundMapEdit.Apply` | Editor 종료 0. 정식 지도 r16 내용 해시 `d9596da33d4efc0d4f862c714eb05bb70c94365bfc30ab6171a702e2b672e3df` |
| `J7973SouthboundMapEdit.Verify` | 남행 화살표 4개 순서, 포켓 분기·좌회전 출구, 우회전 전용 연결, 보호좌회전과 정지선 접근 판정, 승하차 24곳·552경로 확인 |
| `pwsh -File tools/check_workspace.ps1 -Strict` | 입력 문제 0. 정식 지도 파일 SHA256 `522368dabe6be69293a0b1bd66f1febf98168b4db7c7af9526bdcdeef9afd7b2`. OSM 대조 입력과 지도 ZIP은 동일 자료. |
