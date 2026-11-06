# 2026-11-06 일시정지 운전 설정

## 요구·결정

일시정지에 설정 메뉴를 추가하여 최근 개선한 좌우 조작감을 조절한다. 사용자는 상세형4항목을 선택했다. 구현 범위/기본값은 [설계](../superpowers/specs/2026-11-06-driving-settings-design.md)·[계획](../superpowers/plans/2026-11-06-driving-settings.md)을 따른다.

- 조향 민감도50~150%, 반응/복원 속도25~200%, 기본100%. 게임패드 데드존0~40%, 기본15%.
- 싱글/연습/오프라인 PVP/LAN 일시정지→설정. 드래프트 편집→적용 후 복귀·취소/Esc는 저장하지 않음. 기본값도 적용 전까지 미저장.
- 개인 PlayerPrefs `driving.controls.v1`; 캠페인·개인 구매·콘텐츠 프로필과 별개. H/봇/제동/가속 유지, 수동 중심각52도 상한. Unity 축 dead0으로 낮은 데드존 사용 가능.
- LAN v10: 드라이버별 민감도/반응/복원 입력을 서버가 제한하고 로컬 예측과 같은 함수를 사용. 양쪽 실행본 갱신 필요. 메뉴 중 경기 시간은 진행·자기 차는 제동.
- 설정 중 M/F8/F7/F6 및 승하차를 차단. 경기 종료 시 미적용 편집을 폐기하고 결과 표시.

## 변경과 동시 작업 보존

`DrivingSettings.cs` 모델, `DrivingSettingsPanel.cs` 편집 UI, `TaxiController` 입력/각도, `DriveHUD`·`DriveSession` 메뉴/단축키, `PvpNativeLan`·`PvpNativePrediction` 입력 계약, 모달 진입4곳·승하차 guard, `PrototypeBuild`/InputManager 축, 콘텐츠 프로토콜 기대값을 변경했다. `DrivingSettingsChecks`·Player probe/runner·LAN probe가 회귀를 소유한다.

작업 시작 미추적 로컬 자료와 콘텐츠 관리창·개인 생활·제안서 작업을 보존한다. 동시 Unity/실행 폴더 변경을 발견해 `artifacts/steering-options` 별도 worktree에서 운전 설정만 검증했다. 이후 개인 생활 소스cb582a7/배포8ab2dec와 대조해 겹친 소스가 없는 것을 확인하고, 원래 폴더 최신 소스에 운전 설정만 동기화했다. 개인 생활 기능을 제거한 후보로 기본 폴더를 덮지 않는다.

## 검증 기록

- `artifacts/driving-settings-red.log`: 모델 부재 RED·Unity exit1.
- `driving-settings-model.log`: 모델171검사·Unity exit0. 기본값225회 조합이 아니라 실제 루프75조합×보간/각도150검사와 독립 옵션/저장/패킷/봇/H 등 합계171이다.
- `driving-settings-menu-red.log`: 메뉴 부재 RED·exit1.
- `driving-settings-modal-red.log`: 설정 중 F8 기록이 열리는 실제 진입 RED·exit1. 리뷰 지적을 재현하고 공통 모달 guard로 수정했다.
- `driving-settings-reviewed-build.log`: 설정171+메뉴12+모달4, 기존 CasualIntegration 주행/신호/내비/네트워크 회귀·콘텐츠32/복사 검사, 후보 BuildHybrid exit0.
- 초기 Player8검사·1280×720/800×600 설정 카드 렌더 확인. 실제 WheelCollider 각 변화와 봇 기본각 유지 검사이며 실제 도로 회전·사람 조작감 검사가 아니다.
- 리뷰1회와 수정 확인에서 중요 잔여 지적 없음. 최종 기본 빌드/Player/LAN/저장·manifest 결과는 아래 배포 영수증에 기록한다.

## 실행·남은 수용

기본 실행 `builds/EungyeTaxi/EungyeTaxi.exe`, `PrototypeBuild.BuildHybrid`, 은계 싱글/A2 PVP·LANv10. Esc/패드 시작→설정→슬라이더→적용→영업 계속하기. 재실행해도 이 PC의 설정 유지. 패드 데드존은 키보드 조향에 영향 없음.

재현: `DrivingSettingsChecks.Build`, `pwsh -File tools/test_driving_settings_player.ps1 [-Pvp] [-Capture -Visible]`, `pwsh -File tools/test_pvp_native_lan.ps1 -Port 18931 -Reconnect -TimeoutSeconds 300`. 테스트 저장은 `-contentTest`로 격리하고 설정 테스트도 고유 임시 키만 사용한다.

사람의 장시간 조작감·실물 패드·다른PC LAN·전체3일 캠페인은 후속 수용이다. 기존 유턴 observer/수하물 도착 후 견인 fixture는 이번 통과와 별개다. 지도 입력/ZIP은 변경하지 않는다.

## 최종 배포 영수증

- 원래 폴더 `driving-settings-final-build.log`: `DrivingSettingsChecks.Build` → 모델171+메뉴12+모달4, CasualIntegration·콘텐츠32/복사 검사·BuildHybrid exit0. 개인 생활 구현을 함께 보존했다.
- 기본 Player: `driving-settings-final-single`12개·`driving-settings-final-pvp`13개 exit0. 실제 모달 진입 차단·기본값/취소/적용·WheelCollider 각·PVP 종료 시 설정 닫힘 검사, 1280×720/800×600 렌더 확인. `tools/test_personal_room_player.ps1` 기본 실행 파일 구매36개와 별도 재실행 유지도 exit0.
- 실제 두 Player `native-lan-20261106-155448-dbe700d6`: host51/client64·재접속 exit0. 게스트 개인 계수 서버 적용·로컬 예측·설정 중 경기 시간/자기 차 제동·한 단계 복귀 포함. 같은PC loopback이며 다른PC 검사가 아니다.
- 실패를 분리한다: 병렬 Unity/Player 부하 중 첫 LAN은 초기 입력 승인10초/호스트 대기 시간 초과. 이후 다른 검사와 분리한 최종 기본 실행본 검사는 위와 같이 통과했다. 부하를 원인으로 확정한 것은 아니며 처음 실패를 삭제하지 않았다. PVP UI 첫 실행은 `-contentTest`가 싱글 시작으로 분류되어 lobby 시간 초과; runner에 명시적 `-pvp`를 추가해 고쳤다.
- Strict exit0/input issues0, 문서 링크·diff 공백 검사 통과. 저장/백업14개는 시작 해시와 동일. 사용자 active 프로필1개는 동시 승인된 승객 밸런스 작업으로 바뀌었으므로 최신 값을 보존하고 별도 해시 기준을 남겼다. 운전 설정 검사는 실제 사용자 preference 키를 쓰지 않았다.
- 이후 동시 취객 콘텐츠/차량 관리 작업이 실행 폴더를 다시 빌드 중임을 발견했다. 그 소스/기본 실행본을 덮거나 자신의 변경으로 커밋하지 않는다. 운전 설정 소스 커밋과 최신 통합 manifest 전수 검증의 확정 영수증은 추가한다.
