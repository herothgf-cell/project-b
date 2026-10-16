# 은계 자료 대조·적용 계획

목표: 사용자 요청대로 기존 NGII 형상을 보존하며 OSM 도로명/방향/회전 제한/횡단보도를 공유 그래프와 시설에 반영하고 충돌·미확인을 구분한다.

- [x] 추가 NGII ZIP의 좌표계·범위 점검. 비중첩 자료는 제외 근거를 남긴다.
- [x] 원본 ID와 위치·방향을 대조한 보완 설정 생성. 모호한 대응, 정밀 차로 충돌, 조건부 규제, 연결 단절은 보류한다.
- [x] 같은 MapData를 내비·NPC·보조 조향에 전달. 적용 근거와 미확인 상태를 HUD에 표시하고 OSM 출처를 표시한다.
- [x] 매칭/일방통행/회전 제한/공유 그래프/전체 승하차 연결과 Player 운송·교통 검사. 통합 대상: main.

기존 config/maps/eungye-traffic-overrides.json과 HdRoadGraph 경로를 재사용한다. 외부 원본·전체 추출 geometry는 Git에 넣지 않는다. 은계 밖 새 정밀 자료를 임의 이동하지 않는다. 국도42 원본은 우선 보존한다. OSM 표기는 현장 확인과 다르다. 횡단보도 위치를 신호 주기나 법적 운행 규제로 오인하지 않는다.

검증: Python 41개, C# 모델 5종, Unity 빌드, 132개 서비스 경로, NPC 24대 180초/신호 대기열 및 혼합 지도 물리 운송 통과. 근거는 docs/eungye-source-review.md와 config/maps/eungye-supplement-review.json에 기록.
