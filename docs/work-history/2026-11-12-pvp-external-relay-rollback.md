# PvP 외부 중계 변경 철회

사내망에서만 사용하는 조건에 따라 외부 TCP 443 중계 변경을 철회했다. `5d61424`, `5cc4b8f`가 건드린 소스, 테스트 스크립트, 기획 및 인계 문서를 `0945570` 기준으로 복원했다. 외부 중계 검토 문서(`2028f5e`)와 중계 전용 검증 빌드, ZIP, 임시 실행 파일, 로그, 설치 의존성을 제거했다. 사내 LAN 접속 점검 기록과 LAN 검증 로그를 보존했다.

`git diff --exit-code 0945570 --`으로 LAN 대기실, 호스트·참가 코드, 자동 검사 스크립트, 통합 실행 폴더와 manifest의 차이 0을 확인했다. `pwsh -NoProfile -File tools/update_distribution_manifest.ps1 -VerifyOnly`와 `pwsh -NoProfile -File tools/update_pvp_distribution_manifest.ps1 -VerifyOnly` 모두 종료 0으로 143개 파일, 131,535,058바이트를 검증했다. 기존 미커밋 장면과 제안서 파일은 작업 대상에서 제외했다.

`builds/EungyeTaxi/EungyeTaxi.exe`에서 PvP LAN을 실행할 수 있다. 이번 철회 검증은 소스 대조와 배포 무결성 검사까지 진행했다. 사내의 서로 다른 PC 간 수동 접속은 후속 확인 항목이다.
