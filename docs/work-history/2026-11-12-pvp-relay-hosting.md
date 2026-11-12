# PvP 무료 상시 중계 준비와 한 PC 검증

사내 관리자에게 포트 개방을 요청할 필요가 없는 외부 TCP 443 중계를 무료로 운영하고, 실제 유선 PC와 Wi-Fi PC가 방 목록으로 참가한 뒤 기본 배포본을 갱신하는 요청이다. 먼저 한 Windows PC에서 실행본 두 개로 검증했다.

- 유휴 중단 없는 무료 서비스를 안내하는 Northflank Sandbox를 배포 후보로 골랐다. `tools/pvp-relay/Dockerfile`은 포트 8080에서 기존 중계를 실행한다. 공개 HTTPS/WSS는 Northflank의 TCP 443을 사용한다. `GET /healthz`의 204 응답을 추가하고, 게임 방 목록과 접속은 기존 그룹 토큰을 사용한다. [Sandbox 요금제](https://northflank.com/pricing) · [TCP 443 공개 포트](https://northflank.com/docs/v1/application/network/configure-ports)
- `tools/test_pvp_relay_network.ps1`에 `-Network Wired`와 `-Network WiFi`를 추가했다. 기존 기본값 `Both`로 두 어댑터를 함께 확인할 수도 있다. [운영 설정과 현장 절차](../content-design/pvp/31-online-room-relay.md).
- 실행본 `5d61424`의 143개 파일과 진단 스크립트·안내 문서를 `artifacts/pvp-relay-verify/EungyeTaxi-PvP-field-test-5d61424.zip`으로 묶었다. ZIP 파일 항목 145개 각각의 SHA256을 원본과 대조했다. 운영 주소나 접속 토큰은 별도로 설정한다.

## 검증과 다음 단계

- `npm --prefix tools/pvp-relay test` 종료 0. `/healthz`, 방 목록 인증, 참가와 게임 메시지 중계, 방 정원·퇴장 회귀를 확인했다.
- 공개 테스트 주소에서 `test_pvp_relay_network.ps1 -Network Wired`와 `-Network WiFi` 각각 성공. 임시 실제 중계 주소에서도 같은 PC의 두 어댑터에서 WSS 왕복 성공.
- 같은 PC의 Unity Player 두 개를 임시 실제 중계에 연결한 자동 검사 종료 0. 호스트 50·참가자 65개 검사에서 방 생성, 목록 발견·참가, 차량 주행, 퇴장 정리를 확인했다. 두 Player의 게임 트래픽과 어댑터별 WSS 왕복은 별도 검사다. 증거는 `artifacts/pvp/native-relay-20261112-151334-4991990d/`.
- Northflank 무료 계정과 GitHub 저장소 연결이 필요하다. `origin`의 `codex/pvp-relay-sandbox`를 배포용으로 제안했다. 자동 승인 검토가 해당 원격으로 코드와 문서를 보내는 `git push`에 명시적 승인이 필요하다고 판단해 실행을 막았다. 호스팅 컨테이너 빌드와 고정 주소 배포, 서로 다른 유선·Wi-Fi PC의 사람 플레이는 아직 확인 전이다. 현장 검증 뒤 `builds/EungyeTaxi/`와 통합 manifest를 갱신한다.
