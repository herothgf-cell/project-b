# PvP LAN 방 목록

일반 PvP 참가 화면의 IP 입력을 자동 LAN 방 목록으로 교체했다. 호스트가 방을 만들면 호스트 닉네임, 현재 인원, 경기 시간과 참가 상태를 표시한다. 참가 버튼은 검색 응답을 보낸 IPv4와 방이 알려 준 게임 포트를 기존 LAN v17 연결에 전달한다. 호스트의 게임 포트 기본값은 UDP `7777`이다. 검색에는 UDP `47777`을 사용한다.

`PvpLanDiscovery.cs`가 활성 IPv4 인터페이스에서 브로드캐스트 검색을 보내고, 호스트는 같은 서브넷의 검색에 직접 응답한다. `PvpPrototypePanel.cs`에서 약 2초마다 목록을 갱신하며, `PvpBootstrap.cs`와 `PvpNativeLan.cs`가 기존 연결 승인과 방·주소·포트가 일치하는 재참가를 처리한다. 호스트가 좌석을 잠시 예약한 경우 목록에 이를 반영한다. 검색 소켓은 화면을 떠나거나 호스트가 방을 닫을 때 정리한다. 이전 시험장의 개발용 직접 IP 진입도 제공한다.

## 검증

- `tools/check_workspace.ps1 -Strict` 종료 0, 필수 입력 문제 0. `PrototypeBuild.BuildHybrid` 종료 0, 기존 콘텐츠 대조 성공. 검증용 Unity 프로젝트를 복사해 빌드했다. 최초 스크립트 GUID 형식 문제와 Windows Player의 어댑터 조회 예외는 각각 수정했다. 최종 로그는 `artifacts/pvp-lan-discovery-verify/unity-build-release-candidate.log`이다.
- 중간 Player 검사는 LAN IPv4로 방을 찾아 참가했다. 이후 기존의 일시적인 스냅샷 끊김 시 차량 이동 단언에서 종료했다. 로그는 `artifacts/pvp/native-lan-20261112-155612-7afd675b/`에 있다.
- 최종 `tools/test_pvp_native_lan.ps1 -Discover -Reconnect`는 종료 0, 호스트 52개·참가자 69개 검사 통과. 같은 PC의 두 실행본에서 실제 LAN IPv4 방 발견, 호스트가 지정한 UDP 포트로 참가, WheelCollider 자동 주행 약 19.7m, 진행 중인 방 발견과 원래 좌석 재참가를 확인했다. 앞서 실패한 스냅샷 단언도 통과했다. 로그는 `artifacts/pvp/native-lan-20261112-155851-6c5dbf8a/`에 있다.

## 실행과 현장 확인

두 PC에서 같은 통합 실행본을 열고 **PvP → 방 만들기**와 **LAN 방 목록 → 참가**를 사용한다. 같은 LAN에서 호스트 검색 UDP `47777`과 게임 포트가 허용되어야 한다. 다른 유선·Wi-Fi PC에서의 실제 검색과 수동 플레이는 현장 확인 항목이다. [LAN 접속 절차](../content-design/pvp/30-native-lan.md)
