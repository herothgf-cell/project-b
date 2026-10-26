# 승객 만족도별 운임 개정·P8-07 회복 사양 (2026-10-26)

## 요구사항과 기획 결과

- 기본 운임을 요구 만족도 미달 폭에 따라 조금씩 감액하고, 요구치가 높은 승객의 보너스 상승폭을 키운다.
- [11 요청별 효과](../content-design/11-passenger-request-effects.md#2026-10-26-운임·보너스-개정안)에 최종 승객 만족도 `S` 기준의 점진 감액과 요구치별 보너스 가중치, 계산 예시·경계·정산 표시를 **미구현 기획**으로 기록했다. 수치는 수동 경제 검증 전 시험값이다.
- 후속 사용자 답변에서 기본 운임 감액의 비교 기준을 **`S-R` 차이로 확인**했다. 문서의 대안 비교를 결정 사항으로 갱신했다. 세부 수치와 P8-07 구현 조건은 별도 검토 중이다.
- [04 승객](../content-design/04-passengers-and-reservations.md#p8-07-안정-주행-회복-구현-사양-2026-10-26-검토안)에 P8-07의 실제 이동, 중복 구간, 회복 가능한 급가감속 손실, 상한, 저장·복원, 알림·결과 기준을 정리했다.
- [개발 체크리스트](../prototype-development-checklist.md)와 [프로토 범위](../prototype-scope.md)에서 현행 빌드와 새 기획을 구분했다. 기존 완료 표시를 새 기획의 구현 완료로 바꾸지 않았다.

## 현재 코드 대조

- `unity/EungyeTaxi/Assets/Scripts/MannerLedger.cs`: 현행 기본 운임은 `M >= R`일 때 100%, 미달이면 70%다.
- `unity/EungyeTaxi/Assets/Scripts/PassengerRequest.cs`: 최종 `S`와 현행 공통 보너스 계산·결과를 보관한다.
- `unity/EungyeTaxi/Assets/Scripts/PassengerJobs.cs`: 첫 유효 도착에서 운임·보너스를 한 번 확정한다.
- `unity/EungyeTaxi/Assets/Scripts/DrivingManners.cs`: 급가감속 감점과 저장 스냅샷은 있지만 안정 주행 회복은 없다.

## 변경 파일과 검증

변경: `docs/content-design/04-passengers-and-reservations.md`, `docs/content-design/11-passenger-request-effects.md`, `docs/prototype-development-checklist.md`, `docs/prototype-scope.md`, 이 기록, `docs/work-history/LATEST.md`.

문서 변경만 수행했다. `git diff --check` 종료 0. 문서 예시를 JavaScript 계산으로 대조해 기본 운임 `5,000/4,900/4,700/4,000원`, 보너스 요구치별 `80/240/400`, `100/300/500`, `130/400/650원`을 확인했다. 로컬 `python.exe`는 WindowsApps 실행 오류로 사용하지 못했다. 운임 계산·회복 코드, Unity 빌드, 위치 제어, 자동 물리 주행, 사람의 수동 20분/3일 캠페인 검사는 수행하지 않았다. 기존 사용자 작업 트리 변경은 보존한다.

## 다음 작업·재현

기획 수치와 `S-R` 기준을 검토한 뒤 코드 구현 시 감액 경계·요구치별 보너스·시간 초과·최종 약정·중복 정산, P8-07의 500m/60초·중복 이동·비회복 손실·상한·저장 복원을 테스트한다. 수동 경제 밸런스는 별도로 측정한다. 기본 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`, 개발 빌드는 `PrototypeBuild.BuildHybrid`다. 지도 입력·배포 실행 폴더는 변경하지 않았다.
