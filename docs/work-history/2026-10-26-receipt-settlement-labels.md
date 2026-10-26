# 운송 완료 정산 표시 명칭 정정

## 요구사항과 구현

- 실제 결과 화면에서 제시 운임 5,000원, 지급액 3,500원인 운송을 `보너스 -1,500원`으로 표시해, 아래의 `S38/R85 → 보너스 0원` 설명과 충돌했다.
- `RideReceiptSummary.Money`의 첫 줄을 `제시 운임`으로 바꾸고, 둘째 줄의 부호 있는 금액은 기존처럼 `지급액 − 제시 운임`으로 유지한다. 실제 기본 운임이 제시 운임과 같고 이 증감액이 계산된 보너스와 같을 때만 `보너스`, 그 외에는 `정산 증감`으로 표시한다. 계산 성분이 없는 이전 저장 결과도 `정산 증감`으로 표시한다. 세 줄 금액·지급 계산은 바꾸지 않았다.
- 변경 파일: `unity/EungyeTaxi/Assets/Scripts/RideReceiptSummary.cs`, `tests/PassengerCareTests.cs`, `docs/content-design/13-driving-map-ui.md`, 이 기록. `docs/work-history/LATEST.md`는 병행 통합 작업에서 갱신한다.

## 검증과 남은 항목

- RED: `pwsh -NoProfile -File tools/test_passenger_care.ps1` → 기존 명칭과 새 기대값 불일치, `FAIL legacy receipt names a net fare change without claiming an actual bonus`, 종료 1.
- GREEN: 같은 명령 → `PASS passenger care 11108 assertions`, 종료 0. 이전 기록, M 기준 기본 운임 감액, 실제 보너스, 감액과 보너스 혼합을 검사했다.
- Unity 화면/빌드/사람의 수동 플레이는 이 작은 표시 수정 직후 별도로 실행하지 않았다. 상위 통합 작업의 BuildHybrid와 결과 화면 확인을 따른다.

## 재현

저장소 루트에서 `pwsh -NoProfile -File tools/test_passenger_care.ps1`를 실행한다. 제시 5,000원·기본 운임 3,500원·계산 보너스 0원·실지급 3,500원 사례의 둘째 줄은 `정산 증감  -1,500원`이다.

## 후속 통합 확인
통합 BuildHybrid와 `-passengerCareTest -scoreCapture`가 종료0/176검사로 통과했다.1920 결과창에서 제시5,000원/정산 증감−1,500원/최종3,500원 및 실제 M·S 기준 설명을 확인했다. [통합·배포 기록](2026-10-26-junction-trip-followups.md)을 따른다.
