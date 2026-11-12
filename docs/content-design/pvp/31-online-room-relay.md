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

### Northflank Sandbox를 사용하는 경우

1. Northflank Cloud의 무료 Sandbox 프로젝트에서 배포용 Git 저장소를 연결하고 Combined Service를 만든다. 서비스 인스턴스는 하나만 실행한다. [Sandbox 요금제](https://northflank.com/pricing)
2. Dockerfile 경로는 `tools/pvp-relay/Dockerfile`, 빌드 컨텍스트는 `tools/pvp-relay`로 설정한다. 운영용으로 생성한 무작위 토큰을 런타임 시크릿 `PVP_RELAY_ACCESS_TOKEN`에 넣는다. [소스 빌드와 배포](https://northflank.com/docs/v1/application/getting-started/build-and-deploy-your-code)
3. 컨테이너 포트 `8080`을 공개 HTTP/1.1로 설정하고, 자동 TLS가 적용된 `https://…code.run` 주소를 사용한다. 상태 확인은 `GET /healthz`의 204 응답으로 한다. 인증이 필요한 `/rooms`와 인증이 필요 없는 `/probe`도 실제 주소에서 확인한다. [공개 포트와 TLS](https://northflank.com/docs/v1/application/network/configure-ports)

## 검증

- 서버 통합 테스트는 `npm --prefix tools/pvp-relay test`로 실행한다.
- 배포 주소의 어댑터별 왕복은 `pwsh -File tools/test_pvp_relay_network.ps1 -Endpoint wss://relay.example.com/probe`로 확인한다. 스크립트는 Windows IPv4 소켓을 각 어댑터에 지정하며 프록시를 사용하지 않는다. 유선 PC는 `-Network Wired`, Wi-Fi PC는 `-Network WiFi`를 붙인다. 기본값 `Both`에서는 유선과 Wi-Fi 양쪽 모두 성공해야 종료 코드 0을 반환한다.
- 두 Unity Player 자동 검사는 `pwsh -File tools/test_pvp_native_lan.ps1 -Relay -Reconnect -Executable <통합 실행 파일>`로 실행한다. 검사 프로세스에 같은 중계 URL과 토큰을 설정한다. 개발용 중계를 로컬로 띄울 때는 `http://127.0.0.1:8080`도 사용할 수 있다.
- 현장에서는 유선 PC와 Wi-Fi PC 각각에서 해당 연결만 사용한다. 두 PC에 같은 중계 주소·토큰을 설정하고 같은 검증용 빌드의 `EungyeTaxi.exe`를 실행한다. 유선에서 PvP 방을 만들고 Wi-Fi에서 목록을 열어 그 방에 참가한다. 대기실에 두 닉네임이 보이는지, 경기 시작과 상대 차량의 움직임이 두 화면에서 보이는지 확인한다. 방을 닫고 두 PC의 역할을 바꿔 같은 절차를 진행한다. 운영 주소, 빌드 커밋, 두 방향의 결과를 기록한 뒤 기본 배포본과 manifest를 갱신한다.
