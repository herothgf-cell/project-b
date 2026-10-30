# 비보호 좌회전 거리뷰 촬영 지점 재확인

## 요청과 범위

이전 검토의 미확정 접근에 대해 거리뷰 촬영 위치와 시선을 바꿔 재확인한다. 실제 비보호 좌회전 표지와 연결 신호를 확인한 접근 방향에만 허용을 추가한다. 자료가 불명확하면 추정 허용하지 않는다. 최종 검증 후 기본 실행 폴더와 manifest를 갱신해 main에 게시한다.

## 조사 진행

- J8066 북→남: 북측 촬영점을 지도에서 다시 선택하고 남쪽으로 회전, 정지선 가까운 촬영점으로 이동·확대했다.2026-05 최신3D에서4구 차량 신호와 유턴 표지/좌회전·보행 신호 조건판을 정면 판독했다. 비보호 좌회전329 표지를 긍정 확인하지 못했다.
- J7841 서→동: 지도에서 좌표 대상 서측 촬영점BOo를 선택, 동향87°로 회전한 뒤 dTMT 정지선 가까운 지점으로 전진·확대.4구 차량 신호와 어린이보호 안내를 정면 판독. 비보호329 긍정 근거 없음.
- J6858 남남동→북북서: 기존 Ix6 정면과 근접 Ksp 지점을 대조.4구 차량 신호·유턴 조건을 확인했다. Ksp에서 유턴판은 시야 밖이므로 판독 근거는 Ix6 정면과 구분. 비보호329 긍정 근거 없음.
- 조사 중 은계남로의 서쪽 인접 회전교차로 후보83Hd를 발견했으나 좌표의 대상 교차로와 다르므로 제외했다. 거리뷰 자동 선택 위치나 주변 사진을 대상 접근의 허용 근거로 사용하지 않는다.
- 모두 네이버2026년05월 최신3D 표시 기준. 반대 방향/카메라 원경/근접 판독 범위를 분리했다. 이번 확인에서 비보호 긍정 확인0곳이며 은계 전체 부재 판정이 아니다.

| 접근 | 정면 판독 근거 |
|---|---|
| J7841 서→동 | [정지선 근접 dTMT /87°](https://map.naver.com/p/entry/coordinates/3zanok,2AGwoP?c=15.00,0,0,0,adh&p=dTMTFEM4C8evOk9yhEBvYw,87,10,55.94,Float) |
| J8066 북→남 | [북측 근접 _HDj /180°](https://map.naver.com/p/entry/coordinates/3zao1h,2AGJhI?c=15.00,0,0,0,adh&p=_HDjObykfS0QFN1mRvdhww,-180,10,38.75,Float) |
| J6858 남남동→북북서 | [Ix6 정면 /338°](https://map.naver.com/p/entry/coordinates/3zakqC,2AGXWh?c=15,0,0,0,adh&p=Ix6rbIKQdDKEt2ehapts1A,-22,4,55,Float) · [Ksp 근접 대조](https://map.naver.com/p/entry/coordinates/3zakqC,2AGXWh?c=15.00,0,0,0,adh&p=KspRbguk-UHfD1yxbRcpTg,-22,4,55,Float) |

이전 자료·사진별 역할·제외 사유는 [누적 근거 JSON](../../config/maps/eungye-permissive-left-review.json)에 함께 보존했다.

## 구현·검증·배포

긍정 근거가 없는 접근을 비보호로 만들지 않는다. 신규 허용0곳으로 게임의 운행 규칙/승객/저장 로직은 변경하지 않았다. 기본 영업의 창작 비보호 제거 정책과9운송/4기타 서비스 조건을 유지한다. 지도 원본·프로젝트 지도·입력 manifest/ZIP도 미변경. 기획서·체크리스트·근거 파일만 갱신하고 요청에 따라 최종 실행 폴더를 재빌드한다.

- `RealLeftBuildChecks.Build`: 실제 근거3/비보호 긍정0 검사, 시나리오·이전 저장 호환, 전체 WorldFollowup·Startup11,672, `PrototypeBuild.BuildHybrid` 모두 종료0. 로그 `artifacts/real-left-recheck-build.log`.
- 최종 실제 Player2프로세스 종료0: `-realLeftOnlyTest -hybridV2`95검사/24.62초, `-prototypeScenarioTest -hybridV2`96검사/29.21초. 새 실행 폴더에서191검사 통과. 위치 제어·실제 승하차 API 검사이며 사람의 물리 주행/전체 캠페인 검사가 아니다.
- 각 프로세스는 `artifacts/` 격리 저장 사용. 사용자 AppData 저장을 변경하지 않았다. `real-left-recheck-native-results.json`, `real-left-recheck-validation.json`에 실제 결과 기록.
- `tools/check_workspace.ps1 -Strict`: 종료0/issues0. 원시2맵 JSON은 현 PC 미배치인 선택 입력이며 Git 프로젝트 지도 해시 정상. 지도/원시/ZIP 미변경.
- 독립 문서/근거 검토에 남은P1/P2 없음. 회전교차로와 대상 접근을 구분하고 긍정 확인0과 지역 전체 부재를 구분했다.

기본 실행 `builds/EungyeTaxi/EungyeTaxi.exe` / 은계 / `PrototypeBuild.BuildHybrid`. 새로 확인된 비보호 허용 지점은0곳이며 기존 일반 영업 동작을 유지한다. 지역 전수 조사·현장 실시간 확인·사람20분/3일·다른 실제 PC 검증은 수행하지 않았다. 배포 해시 검증/게시 영수증은 아래 실제 결과를 따른다.
