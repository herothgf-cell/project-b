# 2026-10-26 조향·차량 분산·정산 표시 및 추가 유턴 안내

## 요구와 실제 변경
- 정산창은 최초 예상 운임 / 보너스(실수령−최초 예상) / 실수령으로 통일하고 약정·감액 및 지급 설명을 제거했다. 실제 운임 산식은 유지한다.
- 수동 조향 보정이 속도별 실제 바퀴 각도를 기준으로 감속 보조를 계산하도록 수정했다. H 자동 운전 경로는 유지한다.
- 최대48대 안에서 안전한 일반 차로로 초기 차량을 분산한다. 기존12개250m구역을 보존하고 실제48대/28구역을 확보했다.
- 네이버 거리뷰에서 추가3접근부 유턴 표지·조건을 확인했다. 개인용 M형 안내 이미지에만 ④⑤⑥으로 표시하고 게임 지도는 변경하지 않았다. 근거는 [추가 관찰](../content-design/2026-10-26-additional-uturn-observations.md).
- [보행자 승인안](../content-design/16-ambient-pedestrians-proposal.md)은 4거점 최대12명과 승객 구분을 제안한다. 보행자 런타임은 구현하지 않았다. 사용자 최종 승인 대기.

## 검증과 한계
- 변경 전 모델23묶음 exit0. 이후 사용자 요청에 따라 전체 묶음을 반복하지 않는다.
- 정산 집중 모델11,417단언 exit0. 조향 집중 모델48검사 exit0.
- 실제 지도 차량 배치 편집기 검사 exit0: 보이는 차량12→48, 구역12→28, 기존12구역 보존, 최소 간격68.44m. 편집기에는 택시가 없어 실제 플레이어 간격은 실행 검사로 확인한다.
- 최종 BuildHybrid 및 조향/교통/정산창 실행 결과는 아래 후속 기록에 추가한다. 수동 키보드·패드 플레이, 전체 캠페인·경제 밸런스 검사는 수행하지 않는다.
- 지도 contentHash c14a2a4b277e040dce001f2df7f89cc0404555a10b85ef931f2b9de14b6b05dc 유지. 원본·지도 ZIP·필수 입력 manifest 변경 없음.

## 변경 파일과 재현
- RideReceiptSummary.cs, RideCompletionPanel.cs, DriveMath.cs, DistrictTraffic.cs 및 해당 집중 검사/프로브.
- 기본 실행 builds/EungyeTaxi/EungyeTaxi.exe. 빌드 PrototypeBuild.BuildHybrid.
- 실행 플래그: -manualSteeringCorrectionsTest / -trafficDistributionTest / -receiptCaptureTest. 앞 두 개는 -batchmode -nographics 가능, 캡처는 그래픽 모드에서 실행한다. 테스트 저장은 실제 영업 저장과 분리한다.
- 개별 변경 근거: [조향](2026-10-26-manual-steering-corrections.md), [차량](2026-10-26-traffic-distribution.md).
