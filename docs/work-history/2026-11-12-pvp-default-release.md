# PvP 최적화·조향 개선 기본 배포

## 요청·범위
- 최적화와 40~50km/h 조향 개선/설정 프리셋을 기본 `builds/EungyeTaxi/`에 반영, 폴더 전체 ZIP 전달, main 푸시.
- 최적화 `8cff8e1`, 조향 `c2e30f0`, 직전 배포 `f98d129`. 직전 배포는 최적화 소스의 별도 worktree 빌드여서 조향 기능을 포함하지 않는다.
- 직전 최적화 빌드가 이미 v18 상수를 포함하므로, 서로 다른 물리 실행본의 연결을 차단하도록 최종 조향 배포를 LAN v19로 구분한다. 두 PC 모두 새 폴더로 갱신해야 한다.
- 사용자 FirstDrive 씬은 빌드 전에 백업하고 빌드 후 복원한다. 기획/제안서 등 다른 미커밋 파일은 포함하지 않는다.

## 검증·출하
- 최종 소스 `e7d84a6cb38e040ae87265eb9949c0f9597826fc`, 정식 `PrototypeBuild.BuildHybrid` 종료0, `PROTOTYPE_BUILD_OK`. 로그 `artifacts/pvp-stability/default-v19-build.log`.
- 사용자 FirstDrive 씬을 빌드 전 백업과 SHA256 일치하도록 복원. 배포 DLL에서 v19 및 SteeringPreset 포함 확인.
- 기본 실행본 PvP 설정 저장·취소·프리셋18건 통과, 종료0. `artifacts/pvp-stability/default-v19-settings/result.json`.
- 기본 실행본 방 목록 발견/참가·주행: 호스트32/참가자46 검사 통과, 종료0. 28.81초 이동566.31m, 검사상 freeze0ms. 호스트 frame p95 22.24ms(최대62.53ms), 참가자18.80ms. 실제 사내 두PC 결과가 아니다. 증거 `artifacts/pvp/native-lan-20261112-185715-d63a0b47/`.
- 통합/PvP manifest VerifyOnly 각각 종료0: 143파일, 131,590,858바이트. `distribution/EungyeTaxi-build-manifest.json`에 실제 소스/검사 결과/파일 SHA256 갱신.
- 전달 ZIP `artifacts/distribution/EungyeTaxi-20261112.zip`, 46,355,303바이트. 기본 폴더 전체143파일을 포함하며 ZIP 내부 각 파일의 SHA256이 manifest와 일치. ZIP SHA256 `d4ec56b0dd87d85a30bd0a51ebc0ad43e1a4b54c81e0f98b194315c8bda9187b`.
- 실행: `builds/EungyeTaxi` 전체 폴더를 압축해 전달하거나 위 ZIP을 사용. 받은 사람은 전체 압축 해제 후 EungyeTaxi.exe 실행. Unity 설치 불필요. 양쪽 같은 새 실행본 필수. Esc → 설정 → 운전 관련 설정에서 수치/프리셋을 바꾸고 적용.
- 재현: `pwsh -File tools/test_driving_settings_player.ps1 -Executable builds/EungyeTaxi/EungyeTaxi.exe -Pvp`; `pwsh -File tools/test_pvp_native_lan.ps1 -Discover -MotionOnly -MotionSeconds 30 -Port 17987`; `pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly`.

## 미검증
실제 사내 유선·Wi-Fi 두 PC, 수동 장시간 주행, 전체 3일 캠페인. 같은 PC 두 실행본 검증과 구분한다. 지도 입력 변경 없음.
