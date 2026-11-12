# PvP 온라인 방과 TCP 443 중계

`PvP → 온라인 대전`에서 호스트는 **방 만들기**, 참가자는 **방 목록 → 참가**를 선택한다. 방 이름은 호스트 닉네임에서 정한다. 목록에는 이름, 인원, 경기 시간이 표시된다. 호스트를 포함해 최대 네 명이며 빈자리는 봇이 맡는다. 이미 시작한 방은 기존 참가자가 같은 프로세스에서 좌석을 되찾을 때 목록에 표시된다. 기존 LAN 직접 접속은 별도 메뉴에서 선택할 수 있다.

호스트와 참가자 모두 HTTPS로 방 목록을 조회하고 WSS로 게임 메시지를 보낸다. 플레이어 PC에서 여는 연결은 중계 주소의 TCP 443을 사용한다. 사내 유선·Wi-Fi 사이의 UDP 7777 허용이 없어도 된다. 회사 네트워크가 해당 주소 또는 WebSocket 연결을 제한하면 사용할 수 있는 주소나 네트워크가 필요하다. 일반 웹 조회 성공과 WSS 왕복 성공은 별도로 확인한다.

경기는 기존 호스트 PC에서 판정한다. 중계는 방별로 호스트·참가자 메시지만 전달하며 호스트가 나가면 목록에서 방을 제거한다. 버전, 지도 지문과 참가 좌석은 기존 게임이 검증한다. 서버의 단일 인스턴스가 방을 메모리로 관리하므로 재시작하면 방이 종료된다.

## 서비스 설정

1. 사용할 외부 서버에 `tools/pvp-relay/`를 올리고 Node.js 22 이상에서 `npm ci --omit=dev --ignore-scripts`를 실행한다.
2. `PVP_RELAY_ACCESS_TOKEN`에 운영용 임의 토큰을 설정하고 `npm start`를 실행한다. 기본 수신 주소는 `127.0.0.1:8080`이다. `PORT`와 `PVP_RELAY_BIND`는 호스팅 환경에서 지정할 수 있다.
3. 공개 도메인의 TCP 443에서 TLS를 종료하는 프록시를 연결한다. `/rooms`는 HTTPS로, `/host`, `/join/<id>`, `/probe`는 WebSocket 업그레이드가 포함된 연결로 동일한 중계 프로세스에 전달한다. `/rooms`, `/host`, `/join/<id>`는 공유 토큰을 요구한다. `/probe`는 고정 형식의 짧은 진단 문자열만 돌려준다.
4. 각 PC에서 `PVP_RELAY_URL`과 `PVP_RELAY_ACCESS_TOKEN` 환경 변수를 설정하거나, 게임 사용자 저장 폴더 `%USERPROFILE%\AppData\LocalLow\Taxi Prototype\택시 시뮬레이션`에 `pvp-relay.local.json`을 배치한다. URL은 포트 443의 HTTPS 서비스 주소다. 토큰은 운영자만 준비하며 Git에 저장하지 않는다.

```json
{
  "url": "https://relay.example.com",
  "accessToken": "<운영자가 배포한 그룹 접속 토큰>"
}
```

## 검증

- 서버 통합 테스트는 `npm --prefix tools/pvp-relay test`로 실행한다.
- 배포 주소의 어댑터별 왕복은 `pwsh -File tools/test_pvp_relay_network.ps1 -Endpoint wss://relay.example.com/probe`로 확인한다. 스크립트는 Windows IPv4 소켓을 각 어댑터에 지정하며 프록시를 사용하지 않는다. 양쪽 어댑터가 연결된 PC에서는 둘 다 성공해야 종료 코드 0을 반환한다.
- 두 Unity Player 자동 검사는 `pwsh -File tools/test_pvp_native_lan.ps1 -Relay -Reconnect -Executable <통합 실행 파일>`로 실행한다. 검사 프로세스에 같은 중계 URL과 토큰을 설정한다. 개발용 중계를 로컬로 띄울 때는 `http://127.0.0.1:8080`도 사용할 수 있다.
- 최종 현장 검증은 각 PC가 실제 유선·Wi-Fi에 연결된 상태에서 방 만들기, 목록 참가, 경기 시작과 주행, 역할 교대를 확인한다.
