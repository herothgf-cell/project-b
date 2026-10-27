# 승객 만족도·보너스·승하차 지점 기획 개정

## 요구와 결정

- `감사 보너스`의 신규 표시명을 `보너스`로 정리한다.
- 요구 만족도에 **도달하면** 모든 정상 운송에서 보너스를 지급하고, 초과 달성하면 금액을 늘린다. CH04/05·EV03/04와 안전상 보류도 요청 종류만으로 보너스에서 제외하지 않는다.
- 현재 승차 시 매너 100점 때문에 자동 최대 보너스가 되는 문제를 해결하기 위해, 시작 만족도를 요구치보다 4·8·12점 낮게 운송별 무작위 확정한다. 실제 주행·요청 이행·안전한 조기 도착의 변화량을 하차 때 합산한다.
- 무작위 시작 만족도가 운전자의 기본 운임을 깎지 않도록 현행 매너 `M` 기준 운임 100%/70%와 시간 초과 0원은 유지한다. 보너스는 별도 최종 만족도 `S`로 정한다.
- 시뮬레이션 체감을 위해 기존 은계 승하차 12곳과 9개 고정 상황 코스를 보존하고, 일반 호출·길가 승객용 안전 정차 후보를 별도로 늘리는 기획을 작성한다.

## 변경 파일

| 구분 | 파일과 내용 |
|---|---|
| 승객 핵심 | `docs/content-design/04-passengers-and-reservations.md`: 최신 기획/현행 구현 차이 |
| 산식·요청 | `docs/content-design/11-passenger-request-effects.md`: S0/주행/요청/시간 만족도, 17종 요청의 새 Δ 경계, 전 운송 보너스 산식 |
| 승하차 | `docs/content-design/14-passenger-stop-variety.md`: 후보 유형·안전·호출·검증·기존 코스 보존 |
| 연결 | `docs/content-design/06-world-map-and-driving.md`, `09-prototype-scenarios.md`, `05-daily-operation.md`, `07-campaign.md`, `00-RoadMap.md`, `README.md` |
| 범위·후속 | `docs/prototype-scope.md`, `docs/prototype-development-checklist.md`의 P8 신규 미완료 목록, `docs/work-history/LATEST.md` |

## 검증·미검증

- 기존 기획04/09/11, 지도·정차 코드의 `MapStop.CanStop`, `PassengerJobs` 호출·하차, 현재 보너스 코드 `PassengerReward.Calculate`를 읽고 구현과 제안의 차이를 문서에 표시했다.
- 정차 후보의 법규 검토 항목은 국가법령정보센터 도로교통법 제32조를 확인해 14 문서에 연결했다. 개별 후보의 실제 정차 적합성/거리 실측은 수행하지 않았다.
- Markdown 링크 대상 12개 존재, 신규 산식 예시(5,000원·요구85점에서 최종85/93/100점 → 100/310/500원)를 PowerShell 계산으로 확인했다. `git diff --check -- docs/content-design docs/prototype-scope.md docs/prototype-development-checklist.md docs/work-history/LATEST.md` 종료0(개행 변환 경고 외 오류 없음).
- 문서 기획만 변경했다. 코드·실행파일·맵·필수 입력·배포 manifest·지도 ZIP은 수정하거나 재검증하지 않았다. 실제 자동/물리/수동 운행에서 새 산식을 검증한 결과는 없다.
- 시작 만족도 범위·시간 가산 계수·보너스 상한·새 정차 후보 좌표와 개수는 테스트 초깃값/미확정이다. 합법적 지연·위험 운전·ETA 결측·지도 편집 후 후보 손상·중복 지급을 구현 단계에서 별도 검증한다.

## 다음 작업과 재현

다음 개발은 [P8 체크리스트](../prototype-development-checklist.md#p8-승객-만족도보너스-및-승하차-다양화-2026-10-23-기획-미구현)를 따른다. 현재 게임은 `builds/EungyeTaxi/EungyeTaxi.exe`를 실행하면 **기존** 보너스 규칙으로 동작한다. 새 기획은 [11](../design-history/content-design/11-passenger-request-effects-history.md#2026-10-23-이전-정책-기록--만족도와-보너스-개편)과 [14](../design-history/content-design/2026-10-27-before-planning-only/14-passenger-stop-variety.md)에서 읽는다.
