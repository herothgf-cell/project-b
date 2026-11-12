# PvP 온라인 방과 TCP 443 중계

## 요구와 구현

사내 관리자에게 UDP 7777 개방을 요청하지 않고 유선·Wi-Fi에서 접속할 수 있는지 먼저 검사했다. 성공 후 PvP 메뉴에 `방 만들기 → 방 목록 → 참가` 흐름을 추가했다. 방 목록에는 호스트 닉네임으로 만든 방 이름, 참가 인원, 경기 시간이 표시된다. 같은 방을 찾아 재참가할 수 있고 기존 LAN 직접 접속과 봇 경기 진입도 제공한다.

- `tools/pvp-relay/`는 인증된 그룹에 HTTPS 방 목록과 WebSocket 중계를 제공한다. 네 명 정원, 종료한 방 정리, 소켓 생존 검사와 대기열 상한을 적용한다. 방과 경기 권한은 기존 호스트 PC가 맡는다.
- `PvpRelayClient.cs`, `PvpRelayTransport.cs`, `PvpBootstrap.cs`, `PvpPrototypePanel.cs`, `PvpNativeLan.cs`로 방 목록 조회와 기존 NGO 메시지의 중계를 연결했다. 빌드·지도 검사와 30초 좌석 복원 조건은 기존 방 규칙을 따른다.
- `tools/test_pvp_relay_network.ps1`는 어댑터별 WSS 왕복을 확인한다. `tools/test_pvp_native_lan.ps1 -Relay` 및 `PvpNativeLanProbe.cs`는 두 실제 Unity Player로 방 목록 참가와 경기 회귀를 확인한다.

## 검증 결과

- 공개 Postman Echo 주소에 대한 첫 검사는 기본 셸의 DNS 제한으로 실패했다. 네트워크 접근이 허용된 셸에서 `pwsh -NoProfile -File tools/test_pvp_relay_network.ps1` 종료 0. Windows 소켓의 IPv4 유니캐스트 인터페이스를 지정해 `이더넷 2`와 `Wi-Fi`의 WSS 메시지 왕복을 각각 확인했다.
- 중계 서버에 임시 외부 TCP 443 주소를 연결하고 `/probe`를 검사했다. 이더넷과 Wi-Fi 모두 `PASS WSS echo`, HTTPS `/rooms` 200. 연결 확인 이후에도 Windows·사내 방화벽 또는 어댑터 설정은 변경하지 않았다. 임시 중계 주소와 프로세스는 검증 후 종료했다.
- `npm --prefix tools/pvp-relay test` 종료 0. 권한 없는 조회, 방 목록, 전송 방향, 재접속, 정원 4명과 추가 참가 거부, 호스트 종료 후 방 제거를 검사했다. 만석에서 응답을 멈춘 참가자의 종료 확인을 기다리면 자리가 막히는 문제를 재현한 뒤 즉시 자리를 비우도록 수정했다.
- Unity 2022.3.62f2의 `PrototypeBuild.BuildHybrid`를 `artifacts/pvp-relay-verify/` 복사본에서 실행해 종료 0. 기본 셸의 라이선스 IPC와 최초 복사본의 저장소 루트 구조 문제를 확인한 뒤 다시 실행했다. 로그는 `artifacts/pvp-relay-verify/unity-build-final.log`.
- 최종 빌드는 중계 프레임 크기 안에서 NGO 배치를 나누도록 제한하고 온라인 카드 안에 접속 오류를 표시한다. 이전 검증본과 충돌을 피하기 위해 복사본의 출력 위치만 `builds-final/`로 바꿔 `BuildHybrid` 종료 0을 확인했다. 로그는 `artifacts/pvp-relay-verify/unity-build-bounded.log`. 복사본에 콘텐츠 원본 경로가 누락되어 빌드 중 대조 콜백이 경고를 남겼다. 원본 두 파일을 복사하고 Resources의 SHA256 일치와 `ContentTuningBuildChecks.Run` 3개 통과·종료 0을 별도로 확인했다.
- `tools/test_pvp_native_lan.ps1 -Relay -Reconnect -Executable <검증본>` 종료 0. 실제 임시 WSS 주소로 같은 PC의 두 Unity Player를 연결해 호스트 60, 참가자 86 검사 통과. 목록 발견·참가, 호스트 WheelCollider 주행 약 19.74m, 참가자 예측 약 19.86m, 동일 좌석 재참가, 사고 처리 및 종료 정리를 확인했다. `artifacts/pvp/native-relay-20261112-145126-423fc05a/`.
- 중계 서버의 만석 재접속 처리를 보완한 뒤 반복한 `-Reconnect`도 동일 좌석까지 확인했다. 이 반복의 전체 실행은 이후 사고 처리 대기 중 230초 검사 제한에 걸려 종료 1이었다. `artifacts/pvp/native-relay-20261112-145957-28eb1277/`.
- 최종 빌드와 중계 서버로 `-Relay`를 실행해 종료 0. 이더넷·Wi-Fi의 임시 실제 주소 WSS 왕복과 호스트 50·참가자 74 검사를 통과했다. 방 목록 참가, 양쪽 NGO 배치 제한, 호스트·참가자 물리 주행 약 20.74m·20.91m, 퇴장 후 정리를 확인했다. `artifacts/pvp/native-relay-20261112-150305-04e1bd8b/`.
- 기존 LAN 직접 접속은 5분 경기 자동 검사에서 호스트 48, 참가자 60 검사 통과. `artifacts/pvp/native-lan-20261112-145546-3bc821b8/`. 앞서 `-MotionOnly`의 20분 경기 설정으로는 짧은 경기 호출 후보 단언에서 중단됐다.
- `tools/check_workspace.ps1 -Strict` 종료 0, 필수 입력 문제 0. 기존 통합/PvP 배포 manifest `VerifyOnly` 각각 종료 0, 143개 파일·131,535,058바이트. 지도와 Git 배포 실행 폴더는 변경하지 않았다.

## 재현과 인계

[온라인 방 배포·재현 문서](../content-design/pvp/31-online-room-relay.md)를 따라 운영 중계 주소와 그룹 토큰을 배치한다. 게임에서는 `PvP → 온라인 대전`에서 방을 만들거나 목록으로 들어간다. 로컬 검증 실행본은 `artifacts/pvp-relay-verify/builds-final/EungyeTaxi/EungyeTaxi.exe`.

상시 운영 중계 주소, 해당 주소에 대한 두 사내망의 재검사, 서로 다른 PC의 역할 교대 및 수동 주행은 미검증이다. 두 어댑터의 소켓 검사와 같은 PC의 두 Player 검사는 각각 수행했다. 상시 운영 설정과 검증을 마친 뒤 Git 배포 실행본과 manifest를 갱신한다.
