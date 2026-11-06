# 2026-11-06 브레이크 감속·좌회전 신호·수리 안내

## 요구와 범위

- 설정에서 브레이크 감속 조작 민감도를 조절한다. 기존4항목에 일반 브레이크50~150%, 기본100%를 추가한다. 실제 감속 압력에 적용하되 Space/잠금/자동 서비스 제동·봇은 유지하고 H 중 사람 페달에는 적용한다.
- 첨부 은계로 대기에서 Q를 켜도 좌회전 신호가 나오지 않는 문제를 재현/수정한다.
- 중앙 수리 안내를 왼쪽 아래 속도 카드의 빈 영역으로 옮겨 주행 시야를 확보한다. 공업사 비교/길안내 동작은 유지한다.

[설계](../superpowers/specs/2026-11-06-braking-and-left-signal-design.md)·[계획](../superpowers/plans/2026-11-06-braking-and-left-signal.md). 독립 후보 worktree `artifacts/steering-options`에서 완료된5baced5를 기준으로 개발해 다른 콘텐츠/문서 변경을 보존했다. 실제 사용자 게임·저장에 쓰기하지 않고 진단 복사와 격리 테스트만 사용한다.

## 실제 원인·구현

좌회전 지점은 음식점(698.612,306.302)·지도 오프셋·좌회전까지34m를 대조한 `J7973_3744`, axis0/sign+1이다. 실제 NGII incoming `A2218G611886:0` → left `A2218G611452:0` → 활성 exit 연결이 존재하나, 기존 신호 탐색이 교차로와 동일 ruleId의 로컬 접근만 보아 ‘좌회전 없음’으로 결정했다. 따라서 방향지시등을 켜도 요청할 신호 단계가 없었다. 외부 사용자 실행본과 원래 실행본의 신호 탐색 IL은 같아 이 부분의 구 실행본 문제도 아니었다.

`TrafficSystem`은 기존 작성 지도 권한과 폐쇄 판정을 유지하고, 실제 활성 NGII 진입→좌회전→출구를 방향별로 찾아 DistrictTraffic과 같은 중간점의 가장 가까운 교차로32m 연결을 반영한다. J7973 외 기존 누락9접근도 같은 규칙으로 회복한다. 없는 회전 권한을 지도에 새로 추가하지 않는다. 합성 반대 방향/다른 축/폐쇄 진입·연결·출구/연결 없음/가까운 이웃/32m 밖/명시 금지와 기존 J1927 서측 폐쇄를 검사한다.

`DrivingSettings.braking`은 기존 preference 누락 시1로 보충한다. 일반 사람 페달의 제동만 제한된 계수로 조절하고 관성 감속/정차 자동 유지·안전 제동은 별개다. `TaxiController`에서 서버·예측 공통 계산하며, 입력 `brakeSensitivity`를 서버가 유한수/50~150%로 제한한다. LANv11로 양쪽 갱신이 필요하다. 설정 취소/기본값/적용과 화면 높이를5항목으로 확장한다.

`VehicleDamageController.ShowWorkshopShortcut`은 기존 노출 조건을 보존한다. `CockpitUI` 속도 카드 안 논리 좌표(280,h−216,148,40)의 ‘공업사’ 버튼이 기존 `OpenFacilityPlanner(5)`를 호출한다. 중앙 버튼을 제거하고 내구도 게이지/수리 권장·고장 결과 UI는 유지한다.

## 검증·실패 구분

- `braking-settings-red.log`: 항목 부재 RED·Unity exit1.
- `left-signal-q-on-red.log`: 실제 지도에서 hasLeft=false/stage−1 RED·Unity exit1.
- 기본 제동 모델22·기존 조향171+메뉴12+모달4·CasualIntegration·콘텐츠32 검사를 수행했다.
- `braking-physics.json` 실제 PhysX8샘플: 바퀴를 실제 모터로30km/h까지 가속한 뒤50/100/150% 제동 거리7.32/5.47/5.79m·첫 토크525/1050/1575Nm. 낮은 압력은 실제 감속/정지 거리를 바꾸고, 높은 압력은 타이어 마찰 때문에 짧은 정지를 보장하지 않는다. 긴급제동은 두 계수 모두5.86m·3500Nm, 봇은 두 계수 모두5.54m·1050Nm. 사람의 제동 유지 후 자동 후진 없음.
- 초기 속도 주입 fixture는 바퀴RPM0에서 미끄러짐으로 모두 같은 정지 거리를 보였다. 실제 가속/회전 상태로 수정했다. ‘150%이면 반드시 더 짧은 거리’라는 과한 기대도 실제 마찰 한계/토크 검사와 구분했다. 단순 각도/위치 제어 주행이 아니다.
- 신호 테스트의 첫 후속 실패는 pending 수요를 clock.Step 전에 캡처한 fixture 순서였고, 실제 갱신 순서대로 검사해 pending1/captured1과 보호 녹색120초 내 진입·화살표를 통과했다. 제품에 임의 강제 녹색을 넣지 않았다.
- 독립 후보 `braking-signal-repair-candidate-build.log`, 원래 `braking-signal-repair-final-build.log`: 모델22·물리8·실제 지도 회귀·BuildHybrid exit0.
- 첫 Player는 FreezeAll 차량에 속도를 주입하여 브레이크 대신 후진 입력으로 해석된 fixture 때문에 토크 검사 실패. 정지 상태의 제동 유지 의도를 명시해 검사하며 제품 코드 변경 없이 검증기를 보완했다.
- 별도 신선한 코드 리뷰에서 중요 지적 없음. 추가 교차로49개 후 NGII 중간점 연결 변경0도 실제 지도에서 대조했다.

## 최종 실행·인계

기본 실행 `builds/EungyeTaxi/EungyeTaxi.exe` / 은계 싱글·A2 PVP·LANv11 / `PrototypeBuild.BuildHybrid`. Esc→설정→브레이크 감속 민감도→적용. 공업사는 왼쪽 아래 속도 카드에 표시된다. 실제 Player/화면·LAN/저장 보존·manifest·실제 소스 커밋 영수증은 검증 후 추가한다.

필수 지도 입력/ZIP 미변경. 실제 사람의 같은 교차로 연속 주행·긴 플레이·실물 패드·다른PC LAN/전체 캠페인은 자동 검사와 별도 수용이다. 이전 유턴 observer/수하물 도착 후 견인 fixture는 이번 검사로 해결했다고 표시하지 않는다.

## 최종 검증 영수증

- `braking-signal-repair-final-build.log`의 `BrakingSettingsChecks.Build` exit0. 모델22·물리8·기존 설정/조향/메뉴/모달·CasualIntegration·콘텐츠/사본·실제 지도 좌회전 회귀 통과. 검증기 fixture만 보완한 `braking-signal-repair-probe-build.log`의 기본 BuildHybrid도 exit0.
- 기본 Player `braking-signal-repair-final-single`17개·`braking-signal-repair-final-pvp`15개 exit0. 5항목 기본값/저장/취소·일반 제동525→1575Nm/긴급3500Nm·봇 격리·PVP 종료 시 닫힘, 내구도51 숨김/23 표시·공업사 플래너 열기 확인. 1280×720/800×600 설정 및 속도100/정차·양측 지시등 상태 렌더 확인. 작은 창의 기존 상단 HUD 글자 잘림은 이번 수리 버튼 이동과 별개이며 전체 HUD 재설계를 했다고 표시하지 않는다.
- 실제2 Player: `pwsh -File tools/test_pvp_native_lan.ps1 -Port 18937 -Reconnect -TimeoutSeconds 360` exit0, `native-lan-20261106-164653-c3cdf21f` host51/client64·재접속. 개인 조향/브레이크 값의 서버 독립 적용·로컬 예측·권한·설정 중 경기 진행·사고/재개까지 통과. 같은PC loopback이며 다른PC는 미검증.
- `tools/check_workspace.ps1 -Strict` exit0/input issues0. 지도/필수 입력/ZIP 미변경. 문서/소스 선택 커밋과 실제40자리 소스 해시/통합 manifest 검증을 이어 확정한다.
