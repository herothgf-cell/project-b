# 2026-11-04 조향 추가 완화

요구: 직전 배포의 반대 조향/저중속 좌우회전을 더 쉽게 조정. 기존 승인된 캐주얼 조작의 후속 시험값 변경이다. 새 기능 선택이나 지도/신호/경제 변경은 하지 않는다.

## 선택·실행 순서

1. 기존20km/h32°·30km/h19°에서 약40°·25°로 늘리고 저중속 입력률을 높인다. 고속 최대각/H 기하/중앙 복귀 속도/주차 최대52°는 보존한다.
2. 실제 평지 물리 비교에서 각도만 늘린 첫 안은 코너 폭이 거의 줄지 않았다(20km/h 변위4.50→4.53m,30km/h5.39→5.50m). 이 안만으로 개선됐다고 판정하지 않는다.
3. 수동 사람 운전의 접지 상태에서만 약한 yaw 추종 보정을 적용하고 실제90도 회전 반경을 확인한다. 정차/40km/h 이상/H/봇/공중에서는 보정0, 자연 회전이 목표보다 빠르면 가속 보정0. 역주행/좌우 대칭을 검사한다.
4. 집중 회귀 RED→GREEN, 실제 물리 좌우/제동/고속, 통합 BuildHybrid·두 모드와 LAN 입력을 확인한다. 기획/체크리스트/현재상태/LATEST·실제 소스/배포 manifest를 함께 갱신한다.

Ruling: 사용자의 “더 완화”를 조향 제한/응답과 작은 수동 회전 보정의 조정으로 구현한다. 차체 폭·타이어·가속·최고속·급제동 점수는 유지한다. 기존 Unity 캐시와 사용자 미추적 자료를 보존해 `codex/steering-ease-followup`에서 작업한다.

## 검증

- `artifacts/steering-followup-red.log`: 기존20km/h 조향 기대 실패(exit1).
- `steering-followup-physics.log`/`casual-physics-steering-followup.json`: 각도만 늘린 첫 안의 실제 물리 비교. 제동은 동일, 좌우 대칭/고속 보존. 이 안의 코너 축소는 미달이다.
- `steering-yaw-red.log`: 수동 yaw 보정 미구현 기대 실패.

최종 수치·검사 결과와 실제 빌드/배포는 후속 기록으로 확정한다. 물리 fixture/위치 제어와 사람의 연속 운전·실물 패드·다른PC/전체 캠페인은 구분한다.

## 확정한 조정과 물리 비교

- 최대 중심 조향각20km/h32→40°·30km/h19→약24.7°·35km/h12.5→17°. 주차52°와50km/h 이상 곡선은 동일. 입력률15~7/s·저중속 반대 전환1.6배가 속도에 따라1.4배로 줄며, 해제 중앙 복귀는 이전 속도를 유지한다.
- 접지2개 이상/차체 위 방향 내적>.8·사람 수동에만 yaw 추종 보정. 목표1.4rad/s·가속5.2rad/s² 상한·40km/h부터0. 자연 회전이 목표를 넘으면 추가 가속0. 봇/H/정차/공중/큰 기울기/졸음 핸들 고정에는0.
- `steering-final-physics.log` 종료0·`STEERING_FOLLOWUP_PHYSICS_OK`, 실제 PhysX9샘플.20/30km/h 진입 좌우90도 시간1.80/1.62→1.48/1.48초, 코너 변위4.50/5.39→4.13/5.04m(약8.3%/6.5% 감소). 일정 속도 원운동 반경이 아니라 같은 진입 조건의90도 변위다. 좌우 대칭·최대 기울기 약1.35°.
- 60km/h 샘플의 거리/회전/기울기와20/30/40 제동 시간/거리는 이전과 동일. 고속 샘플은8초 안정/좌우 대칭 검사이며90도 완료 검사가 아니다.
- 읽기 전용 리뷰에서 졸음 중 고정된 바퀴와 새 yaw 토크의 입력 불일치를 발견했다. `MayAssistManualYaw`에 실제 microsleep 플래그를 연결하고 guard 회귀 RED(`steering-frozen-guard-red.log`)→GREEN. 후속 리뷰에서 남은 중요 문제 없음. 전체 실제 졸음 사건 물리 재현은 수행하지 않았다.
- 사용자가 기본 exe를 실행 중이어서 강제 종료하지 않고 `CasualIntegrationBuild.SteeringCandidate`로 같은 BuildAt/은계 자료의 후보를 먼저 빌드했다. 집중 조작27개·기존 통합 회귀/후보 빌드 종료0(`steering-candidate-build.log`). 사용자 종료 확인 후 정식 BuildHybrid로 기본 실행 파일을 갱신한다. 종료 저장 이후의4파일 해시를 보호 기준으로 다시 잡았다.

## 최종 실행·배포

검증·소스/실행 파일 manifest와 실제 게시 결과는 아래에 기록한다. 다른PC/실물 패드와 장시간 사람의 조작감은 남는다.

- `artifacts/steering-final-build.log`: Unity 종료0·집중 조작27개와 기존 통합 회귀 통과, `PROTOTYPE_BUILD_OK` 기본 은계/PVP 실행본 확인.
- 기본 exe의 `steering-single.log`226개·`steering-pvp.log`213개 종료0. 실제 맵/운송/내비/신호·권한 snapshot의 위치 제어 검사다. 새 HUD 화면 검사가 아니며 물리 코너 검사는 위 평지 PhysX 결과와 구분한다.
- 사용자 게임 종료 후 기준4파일과 최종 재대조해 변경0. 원래 실행 중이던 게임의 사용자 종료 저장을 덮어쓰거나 과거 해시로 되돌리지 않았다.
- 실제 두 Player LAN: `pwsh -File tools/test_pvp_native_lan.ps1 -Port 18517 -Reconnect -TimeoutSeconds 240` 종료0, host40/client55·재접속 통과. 증거 `artifacts/pvp/native-lan-20261104-201536-533a9388`. 실제 입력/바퀴 물리/로컬 예측·호스트 권한/기존 운송 보존을 확인했다. 같은PC loopback이며 다른PC는 별도다.
- 관련10문서 로컬 링크139개 누락0, workspace Strict 종료0/input issues0, `git diff --check` 통과. 생성된 Burst 디버그 폴더는 실제 경로를 확인한 뒤 artifacts에 보존해 배포에서 제외한다. 지도/필수 입력/ZIP은 바꾸지 않았다.

## 배포 확정

- 실제 소스 `bb0165f66fdd2ff22e644e8e369ec96990ea2e7d`, 이 기록의 `git log -1 --oneline --` 결과로 확인했다. 빌드 이후 코드 변경 없음.
- `artifacts/steering-validation.json`과 위 소스로 manifest를 갱신했다. 기본 실행 폴더140파일/122,952,638바이트, 통합/PVP VerifyOnly 모두 종료0·전수 크기/SHA256 일치.
- 실제 main 게시 해시는 push/원격 대조 후 아래에 기록한다. 작업 도중 실행 중이던 사용자 게임을 강제 종료하지 않았으며 사용자 종료 확인 후 기본 실행 파일을 교체했다.
