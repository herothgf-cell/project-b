# 운전·승객 만족도 회복 완화 기획 (2026-10-26)

## 요구와 현행 대조

감점에 비해 회복이 어려운 체감을 조금 완화한다. 사용자가 지정한 `docs/content-design/04-passengers-and-reservations-history.md`에 신규 기획 절을 추가하고, 현행 기준 문서와 체크리스트에서 이를 연결한다. 기존 날짜별 변경 기록은 보존한다.

현재 `M`은 운송마다 100점에서 시작하고 실제 급가속·급제동 감점만 고유 이동 500m 또는 이동 중 60초마다 +2, 운송당 최대 +6 회복한다. `S`는 그 `M` 회복만 간접 반영하므로 시작 만족도 `S0<R` 또는 요청 실패 후의 만회 수단이 제한적이다. 법규 성향의 장기 누적 운행 점수는 별도 시스템이다.

## 문서 변경과 제안

- `docs/content-design/04-passengers-and-reservations-history.md`: 안정 주행 기회 350m 또는 45초, `M` 회복량·상한 유지, `M` 회복이 없고 `S<R`인 경우에만 `S` 직접 +2/회·운송당 최대 +4·`R` 상한을 제안한다. 한 기회에 두 점수를 동시에 지급하지 않으며, 직접 회복을 `Δcomfort`로 따로 기록한다.
- `docs/content-design/04-passengers-and-reservations.md`, `docs/prototype-development-checklist.md`: 현행 구현 500m/60초와 미구현 완화안을 분명히 구분하고 상세 기획에 연결한다.
- `docs/work-history/LATEST.md`: 최신 문서 기획, 현행 빌드, 다음 구현·검증 단계를 인계한다.
- 감점값·운임 감액률·보너스 산식·법규 성향 장부는 변경하지 않는다. 중대한 위반 감점은 `M`에서 회복하지 않고, 기한 초과 운임 0원도 유지한다.

## 검증과 남은 항목

문서·코드 대조만 수행했다. `git branch --show-current`는 `main`, `git diff --check`는 종료0, `git diff --stat`는 수정한 기존 문서 4개만 표시했다. 새 작업 이력 1개를 추가했으며, 작업 전부터 있던 미추적 Unity 폴더·상태 검토 기록은 변경하지 않았다. 게임 코드, Unity 빌드, 배포 실행 파일, 지도·입력 ZIP은 변경하지 않았다. 따라서 새 회복 수치는 **현재 exe에 적용되지 않는다**.

현행 `docs/content-design/04-passengers-and-reservations.md`의 속도 1m/s와 `StableDrivingProgress.Observe`의 실제 0.5m/s가 달라, 완화안은 현행 코드의 0.5m/s 이상을 제안하고 구현 시 문서·검사를 통일한다. 구현 시 350m/45초 경계·한 기회 한 회복·운송별 상한·왕복 방지·저장/복원·`S/R` 표시와 운임을 검사한다. 이후 수동 20분 주행과 3일 경제 체감으로 빈도·수익을 비교한다. 현행 빌드의 안정성 자동 물리·교통 전체 검사는 [이전 통합 기록](2026-10-26-satisfaction-main-integration.md)의 종료1 결과를 그대로 유지한다.

## 재현

저장소 루트에서 `git diff --check`, `git diff -- docs/content-design/04-passengers-and-reservations-history.md docs/content-design/04-passengers-and-reservations.md docs/prototype-development-checklist.md`로 문서 변경을 확인한다. 현행 동작은 `unity/EungyeTaxi/Assets/Scripts/MannerLedger.cs`, `DrivingManners.cs`, `PassengerRequest.cs`를 따른다. 필수 로컬 지도 입력 상태는 [최신 인계](LATEST.md)의 직전 통합 기록과 `pwsh -File tools/check_workspace.ps1 -Strict` 기준이다.

## main 반영

기획·체크리스트·이력은 로컬 `main`의 `9ccdda76b70c5d39123d57655d964db8746408a6`에 함께 커밋했다. `git fetch origin main` 뒤 원격 선행 상태를 확인하고 `git push origin main` 종료0, `git ls-remote origin refs/heads/main`에서 해당 해시 일치를 확인했다. 이 푸시 기록 추가 후 문서 자신의 최종 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-26-score-recovery-easing-spec.md`로 다시 확인한다.
