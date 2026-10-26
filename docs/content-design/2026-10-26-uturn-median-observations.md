# 네이버 거리뷰 관찰 — 유턴·중앙분리대 (2026-10-26)

## 방법과 범위
2026-05 촬영 파노라마를 직접 열어 확대/회전해 표지와 노면을 확인했다. 영상 자체를 게임 자산으로 복제하지 않았다. 현재 법규 인증이나 실측이 아니다. 전체 지도는 데이터 검사, 현장 관찰은 아래 명시한 접근 방향에 한정한다.

| 위치 | 관찰 | 적용 |
|---|---|---|
| T1 동남 은계로 | 좌회전/유턴 겸용 노면 화살표와 `좌회전시 / 보행신호시` 표지 | 조건부 유턴 연결 |
| T1 북서 은계로 | `좌회전시 / 보행신호시` 유턴 표지 | 조건부 유턴 연결 |
| T1 북동 계수로 | `좌회전시 / 보행신호시` 유턴 표지 | 조건부 유턴 연결 |
| 수인로 J1927_5181 동쪽 접근 ~ J5057_4449 서쪽 | 중앙의 황색 사선 안전지대, 금속 방호울타리와 교차로 앞 충격 완충시설 | 관찰 구간에 분리대/안전지대 표시. 폭은 원본 황색 경계선 사용 |
| 첨부 이미지의 서쪽 교차로 동쪽 접근 | 세 직진 화살표, 중앙 방호울타리 끝 확인; 유턴 허용 표지 확인 못 함 | 유턴을 임의 추가하지 않음. 미확인 상태 기록 |

## 출처
- 동남 표지: https://map.naver.com/p/entry/coordinates/3zadbu,2AGZTu?c=16,0,0,0,adh&p=NeRkbKpCLPyjXB6lsBHKhw,-70,12,50,Float
- 동남 노면: https://map.naver.com/p/entry/coordinates/3zadbu,2AGZTu?c=16,0,0,0,adh&p=lygcigyMAFesMYe7CZrtIA,-64.83,-12.12,38.75,Float
- 북서 표지: https://map.naver.com/p/entry/coordinates/3zadbu,2AGZTu?c=16,0,0,0,adh&p=pbIhvNw5ofVGzsLRZ9jVWQ,125,8,25,Float
- 북동 표지: https://map.naver.com/p/entry/coordinates/3zadbu,2AGZTu?c=16,0,0,0,adh&p=C05LjSoKPvjCfwsrheEcxQ,230,12,30,Float
- 수인로 분리대: https://map.naver.com/p/entry/coordinates/3za91A,2AGGdk?c=16.18,0,0,0,adh&p=RH2sSoLSyA5LtdOijavljA,159.2,-24.29,80,Float
- 서쪽 접근 노면: https://map.naver.com/p/entry/coordinates/3za91A,2AGGdk?c=16.18,0,0,0,adh&p=1Gm31IvlTUi2qYt42j5Vlg,-80,0,80,Float

곡선 반경과 출구 차로, 시설 높이/설치 길이, 신호 주기는 게임 추정치다. 2026-05 이후 변경 여부와 다른 접근 방향의 유턴 허용은 미확인이다.

## 전체 노면선 데이터 검사
- Python 감사는 정식 문서의 1,792개 차로·889개 도로·578개 수입 노면선을 대상으로 한다. 역방향을 포함한 동일 선 14쌍을 제거하면 564개이다.
- Unity 실제 합성 지도(1,843개 차로)의 교차로 클리핑 이후 황색 노면 조각 15,707개를 검사했다. 생성 조각 중심 중복은 0개, 수입선과 0.25m 이내인 표본은 2개, 그 외 6m 이내 표본은 144개였다. 두 근접 지점은 (1066.10,162.93), (1051.55,123.94), 수입 kind515 선이다. 두 표본은 평행 중복이 아니라 약50°/54°로 교차하는 지점이다. kind515는 주정차 금지선이며 도로 가장자리 역할도 할 수 있다. J10580_1415의 실행 화면에서 수입 가장자리선이 합성 도로를 가로지르는 현상을 확인했다. 실제 로컬 도로와 횡방향으로 겹치는 kind505/515 조각을 잘라 표시하도록 수정했고, 평행 가장자리와 kind501 중앙선은 보존했다. 근접 수치만으로 중앙선을 삭제하거나 분리대 시설로 판단하지 않는다.
- 재현: `tools/audit_road_markings.py`, Unity `RoadMarkingAudit.Run`. 결과는 `artifacts/road-markings-audit.*`, `artifacts/road-markings-runtime-audit.json`에 생성한다. 현장 답사는 위 표의 접근 방향에 한정한다.
- kind515 분류 근거: [국토교통 기술자료의 정밀도로지도 속성표 그림2-62](https://www.codil.or.kr/filebank/original/RK/OTKCRK240337/OTKCRK240337.pdf), p38. 도로 가장자리와 중복 역할 가능성을 고려하며 원본 자료의 코드와 기하를 보존한다.
