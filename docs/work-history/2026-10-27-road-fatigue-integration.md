# 2026-10-27 도로·지속 졸음·청결 후속 통합

## 요구·범위

새 첨부1 좌회전/2 우회전 단축,3 교차로 네이버 거리뷰 시설 대조,4 소교차로 신호/차선 확인, 지속 시야 제한과 승객 중도 하차·취소/시야 상실 후 회복 페널티 귀가, 평상시 청결 소모, 배고픔 아이디어. 세부 수치는 사용자 위임, 마인드맵 미수정. 사용자가 이번 변경과 앞선 피드백을 포함한 로컬 main 병합·푸시를 명시 요청했다.

## 실제 구현

- [직접 회전 분석/회귀](2026-10-27-direct-turn-repair.md): J7973 서쪽3→2→1 기존 좌회전 포켓, J7973 이후 북행 후반1→2→3을 연결14개로 보완. 원본 시작 차로를 인접 차로로 속이지 않는다. 정식 문서에 저장했고1846차로, contentHash `48a84e21ffb5c6803fef8edddc90015695cf9806ca42c6137563895ef307f998`다.
- [거리뷰/제보 관찰](../map-observations/2026-10-27-junction-followup.md): J7973 남/북 중앙 난간44조각, 교차로/포켓 여유 검사. 분리 횡단 섬은 확인 못해 추가하지 않음. J5750 신호 제거는 **사용자 제보 기준**, 인접 거리뷰만 확인했으며 해당 분기 전체 실측/독립 확인 완료로 표시하지 않는다. 기존 T형 좌/우 진입 연결·횡단보도 유지, 새 좌회전 대기 차로를 만들지 않음.
- [지속 피로/청결 구현](2026-10-27-progressive-fatigue-cleanliness.md): 피로100에서0~60초 경고,60초 지속 시야축소,120초 안전 감속·정차·0원 취소와 승차 제한,180초 완전 시야 상실. 명시 귀가 버튼→기존 주차 거점·같은 날100초/피로-20(정상-40). 활성120초당 청결-1·나머지 저장. 배고픔 추가 코드는 없음.
- 앞선 소스7a22200/배포ae9b045의 시설별 F7/TIP·지도 방향·표지/시간 난도·감점·PS08 팝업 제외를 보존하며 함께 통합한다.

## 검증 증거와 수정

| 구분 | 실제 명령·결과 |
|---|---|
| 최소 RED | `PlaytestDirectTurnTests.Run`의 정확 위치 회귀2종 종료1→새 연결 후 GREEN. 초기에 한 블록 북쪽으로 잘못 잡은 fixture는 완료 근거에서 제외 |
| 미리보기 | `PlaytestDirectTurnTests.Preview`, 종료0; 원문서 불변/근거 필수/14연결 전체 validator·차체 footprint |
| 정식 지도 | `RoadFollowupAuthoring.Apply`, 최종 종료0, `PLAYTEST_DIRECT_TURNS_OK` / `ROAD_FOLLOWUP_TESTS_OK rails=44`. 서쪽 일반3차로8m 출발915.38m, 포켓742.73m, 북행1차로 후반40m 출발590.92m. 첫 회전/북쪽 루프 미진입 검사 |
| 발견·수정 | 난간 첫 배치가 기존 포켓 진입선을 막아 종료1. 포켓 바깥으로 이동 후 모든 지도 경로에1.3m 초과 중앙 여유 통과. authoring offset38m 누락도 preview 실패 후 전역 offset 보존으로 수정 |
| 1차 통합 Editor | `RoadFatigueFinalVerification.Run`, 종료0; 기존 안정화·시설/승객/감점·저장·현재 지도/직렬화·새 회전/난간 검사. `artifacts/road-fatigue-final-editor.log` |
| 모델/청결 | `tools/test_prototype.ps1`25묶음·`tools/test_passenger_care.ps1`13,301단언·`tools/check_workspace.ps1 -Strict`, 각각 종료0. 필수 입력 문제0/선택 가져오기2개 없음 |
| 리뷰 수정 | 단일 독립 리뷰 Critical0/Important1/Minor1. 자동6m/s² 감속을 일반 급제동으로 관찰할 수 있어, 실제 자동 감속을 한 직후의 짧은 물리 순서 여유에 한해서 필요 제동으로 분류. 충돌·신호 관찰은 끄지 않음. `FatigueSystemBrakeTests` clean RED 종료1→GREEN. 수정 후 모델25묶음/전체 `RoadFatigueFinalVerification.Run` 종료0 (`artifacts/road-fatigue-reviewed-editor.log`, `FATIGUE_SYSTEM_BRAKE_TESTS_OK` / `ROAD_FATIGUE_FINAL_EDITOR_OK`) |
| 접지 장치 수정 | 첫 Player 검사 종료1: law100→100/M100→98. 진단 실행에서 `급가속 -2점`임을 확인했다. 실제 제동이 아니라 정차한 시험 차량에16m/s를 순간 주입한 장치 오류였다. 초기 속도 주입 후 `Suppress(0)`으로 관찰 기준만 재설정하며, 관찰 유예나 컴포넌트 비활성화 없이 실제 접지/활성 M 제동을 검증한다. 해당 실패를 감점 없는 완료로 계산하지 않음 |

## 리뷰 판단·남은 범위

- Minor 보류: 피로 취소 전에 요청 UI를 Clear하여 취소 영수증의 이미 응답한 요청 이력이 `요청 없음`으로 보일 수 있다. 운임0/법규·M 무감점 취소는 유지되지만 요청 이력 표시는 후속이다.
- 위치 제어·직렬화·부분 감속 검사는 전체 새 경로 물리 완주/사람의20분·3일 캠페인/실물 패드·다른PC 완료를 뜻하지 않는다. 다른 미수정 차로 구간의 기존 우회나 마지막 변경 창을 지난 차량까지 단축 보장하지 않는다.
- 리뷰의 실행화면/접지 감속/최종빌드·저장/배포 무결성 판단 보류는 아래 통합 실행에서 확인한다. 실제 도로 규제 전수와 수동 밸런스는 계속 미확인으로 남긴다.
- 임시 귀가를 새 집 모델이 아닌 기존 주차 거점·같은 날 회복으로 구현했다. 이는 사용자 위임 수치/흐름 판단이며, 별도 주택/다음 날 전환을 의도했다면 후속 변경이 필요하다.

## 파일·재현·배포

기획06/10/15·체크리스트/scope와 본 이력, 지도 source/config, Editor authoring/test, DriverCondition/DrowsinessHUD/BusinessProgress/EconomyLedger/VehicleCare/PassengerJobs/PassengerRequestUI/RideReceiptSummary/DrivingManners/Probe가 변경된다. 기본 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`, 빌드는 `PrototypeBuild.BuildHybrid`다.

- 지도 재검사: Unity `RoadFollowupTests.Run`, `PlaytestDirectTurnTests.Run`. `RoadFollowupAuthoring.Apply`는 원본 저장 작업이므로 단순 재검증 때 반복 호출하지 않는다.
- 실제 실행 신규 검사: `-hybridV2 -fatigueOvertimeTest -fatigueCapture`; 실제 접지 차량/활성 M 관찰과 경고·축소·상실 PNG. 실행 결과는 아래 최종 항목에 추가한다.
- 필수 입력2파일/원시 자료/지도 ZIP은 변경하지 않아 기존 manifest 해시를 유지한다. 정식 Git 프로젝트 JSON/빌드 내 resource와 배포 manifest는 새 지도와 일치시킨다. 지도 리비전 변경으로 진행 중 구형 저장의 임의 재배치는 하지 않으며 기존 확인 절차를 따른다.
- 사용자 `.mmst` SHA256 `F1951492F9C5A13941E60A17D92EB31B1A7471FB8ABA7996D47A0C2E02F00937` 유지. 기존 미추적 사용자 폴더/파일을 커밋하지 않는다.

## 최종 배포·통합 결과

검증·빌드·실행·manifest·main 통합은 진행 중이다. 실제 종료 결과와 원격 SHA를 확인한 뒤 갱신한다. 이력의 실제 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-27-road-fatigue-integration.md`로 확인한다.
