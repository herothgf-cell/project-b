# 40~50km/h 완만한 커브와 조향 설정

## 요청·설계·계획
- 사용자 승인: 완만한 커브는 40~50km/h에서 편하게, 좁은 직각 교차로는 감속 필요. 설정에서 조향 수치 수정.
- 기존 민감도50~150%, 반응/복원25~200% 저장·LAN 전달을 재사용한다. 새 네트워크 설정 필드를 중복 추가하지 않는다. 조향 전용 빠른 설정 버튼을 추가하고 브레이크/데드존은 보존한다.
- 무제한 회전력이나 횡그립 증가는 제외한다. 35~60km/h 조향각 여유를 부드럽게 추가하고 회전 보조를 60km/h까지 점차 줄인다. 중속 보조 목표는 횡가속 한도로 제한한다.
- 순서: 기존 물리 곡선과 새 요구 실패 확인 → 계산/설정 UI 변경 → 물리·저장·LAN 예측 검사 → 별도 실행본 검증 → 결과 기록.
- 기존 미커밋 최적화 및 다른 작업은 보존한다. 기본 배포본 덮어쓰기는 이 작업의 검사 결과를 확인한 후 판단한다.

## 검증
진행 중. 수동 체감 및 실제 사내 두 PC 검사는 미완료.

## 구현·검증 결과
- `DriveMath`: 기존 중속 조향각에 최대20%의 연속적인 여유(35~60km/h)를 더했다. 회전 보조는 40~50km/h에서도 작동하며 60km/h에서0. 30~40km/h는 횡가속 목표 한도5m/s²로 연속 전환한다. 실제 노면 그립이나 급코너 통과를 보장하는 값은 아니다.
- `DrivingSettingsPanel`: 기존 수치 슬라이더와 저장/취소 유지, 조향 프리셋3개 추가. `DrivingSettingsProbe`: 실제 플레이어에서 프리셋 저장/취소/브레이크 보존 확인.
- LAN 물리 불일치 방지를 위해 v18. 새 메시지 필드 없이 기존 개인 조향값 전달을 재사용한다. 기존 v17과는 참가 불가.
- RED: 변경 전 `CurveSteeringChecks.Run`은 50km/h 조향 여유 요구에서 종료1. GREEN: 변경 후 조향·설정171검사/메뉴 누적183검사/기존조작27검사와 새 프리셋 검사 통과. 기존 PvP 안정성 core도 통과. Unity 비개발 빌드 종료0.
- 물리 검사: 평지·입력65%·전진 속도 고정4초(측면 속도는 실제 물리). 회전각40/45/50km/h가 110.49/79.44/39.90도 → 118.44/94.39/48.75도. 최대기울기1.135도, 최대수직속도0.039m/s 이하. 실제 도로 수동 주행과 구분한다.
- 전체 속도 연속성 초기 검사에서 기존15km/h 영역의 큰 기울기에 걸렸다. 변경 구간35km/h 이상으로 검사를 좁혔으며 기존 저속 곡선은 변경하지 않았다.
- 로그: `artifacts/pvp-stability/curve-before.log`, `curve-build.log`. 설정 플레이어 첫 시도는 sandbox PlayerPrefs 쓰기 오류(검사0건)로 실패하여 임시 레지스트리 키 쓰기 권한으로 재실행한다.
- 기본 배포본과 사용자 FirstDrive 씬은 보존. 현재 작업공간의 기존 최적화/다른 변경도 포함된 별도 후보 `artifacts/pvp-stability/release/EungyeTaxi.exe`로 검증한다. 기본 배포 갱신·수동 화면 검증·실제 사내 두PC 검증은 미완료.

## 최종 실행 결과·전달
- PvP 실제 플레이어 설정 검사18건 통과, 종료0 (`curve-settings-verified/result.json`).
- LAN 방 발견·참가·개인 조향 동기화·주행: 호스트32/참가자46 검사 통과, 종료0. 28.81초 이동566.90m, 검사상 freeze0ms. 증거 `artifacts/pvp/native-lan-20261112-183534-3bbe09b5/`. 같은 PC 두 프로세스이며 실제 유선/Wi-Fi 두 PC 검증이 아니다.
- 전체 실행 폴더 ZIP: `artifacts/pvp-stability/EungyeTaxi-curve-steering-20261112.zip`, 46,355,320바이트. 143파일 크기/SHA256 및 ZIP 내부 해시 일치 확인. ZIP SHA256 `9ab393aab0d4facae451325da86f64e138b4c71a55570c8dbf480e5fb5b7f043`. 별도 파일 목록 `artifacts/pvp-stability/curve-build-manifest.json`.
- 실행: ZIP 전체 해제 → EungyeTaxi/EungyeTaxi.exe → PvP. Esc → 설정 → 운전 관련 설정에서 슬라이더/조향 빠른 선택 → 적용. 두 PC 모두 이 ZIP을 사용한다.
- 재현: Unity batchmode `-executeMethod CurveSteeringChecks.Run`; `pwsh -File tools/test_driving_settings_player.ps1 -Executable <검증본exe> -Pvp`; `pwsh -File tools/test_pvp_native_lan.ps1 -Executable <검증본exe> -Discover -MotionOnly -MotionSeconds 30 -Port 17986`.
- 빌드 당시 임시 실행 진입점은 조향 검사 후 기존 진단 빌더를 호출했다. 최종 테스트 소스에는 Run만 남겨 별도 최적화 작업의 미커밋 빌더에 의존하지 않게 했다. 제품 런타임 코드는 검증한 ZIP과 같다.
