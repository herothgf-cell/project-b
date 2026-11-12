# PvP IP 입력과 사내 유선·Wi-Fi 접속 점검

## 요청과 확인 결과

숫자 IP 없이 PvP에 참가할 수 있는지와 사내 유선끼리는 연결되지만 유선과 Wi-Fi 사이에서 실패한 경우를 검토했다. 현행 통합 빌드의 `IP로 참가`는 숫자 IPv4와 UDP 포트(기본 7777)를 받는다. `PvpNativeLan.Begin`은 IPv4만 허용하고, 호스트는 모든 IPv4 인터페이스에서 수신한다. 대기실은 로컬에서 찾은 첫 번째 비루프백 IPv4를 보여 주므로 여러 인터페이스가 있으면 실제 사용할 유선 주소를 확인한다.

대역이 다르더라도 사내 라우팅과 접근 정책이 해당 UDP 통신을 허용하면 직접 연결할 수 있다. VLAN 간 정책, Wi-Fi 격리, 호스트 방화벽, 표시 주소를 확인할 수 있다. PC 이름이나 방 자동 검색으로 숫자 입력을 줄일 수 있지만 직접 게임 통신이 가능해야 한다. 망 분리 환경까지 지원하는 방식으로는 양쪽 PC가 접근 가능한 중계 서버를 통한 방 코드 참가가 있다. [Cisco 무선 ACL](https://www.cisco.com/c/en/us/support/docs/wireless-mobility/wireless-lan-wlan/81733-contr-acls-rle.html) · [Unity 세션 네트워킹](https://docs.unity.com/en-us/mps-sdk/session-networking)

## 변경과 검증

- 구현은 검토 단계다. 이 기록과 `docs/work-history/LATEST.md`를 갱신했다.
- `git status --short`로 기존 작업을 확인하고 `rg`로 `PvpPrototypePanel.cs`의 방 만들기·IP로 참가와 `PvpNativeLan.cs`의 주소 검사·수신 설정을 대조했다. `docs/prototype-development-checklist.md`, `docs/prototype-scope.md`, `docs/content-design/pvp/30-native-lan.md`도 확인했다.
- Unity 실행, 빌드, 두 PC 간 통신 재현은 수행하지 않았다. 회사 네트워크 정책과 실제 접속 실패 원인은 미확인이다.

## 재현 방법

양쪽 PC에서 같은 `builds/EungyeTaxi/EungyeTaxi.exe`를 실행한다. 유선 PC에서 기본 포트로 방을 만들고 `ipconfig`에서 이더넷 IPv4를 확인한다. Wi-Fi PC에서 해당 주소와 포트로 참가한다. 실패하면 같은 호스트와 포트로 유선 PC 참가 결과를 비교하고, 네트워크 담당자에게 Wi-Fi에서 해당 유선 호스트의 UDP 7777로 통신이 허용되는지 확인한다.
