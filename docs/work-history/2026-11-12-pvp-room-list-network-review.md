# PvP 방 목록과 사내망 중계 조건 검토

## 요청과 검토 결과

호스트가 방을 만들고 참가자가 목록에서 고르는 흐름을 검토했다. 참가자는 IP나 포트를 입력하지 않는다. 사내 유선·Wi-Fi 간 UDP 7777이 차단된 환경도 대상으로 한다.

- 기존 게임은 Unity 2022.3.62f2, NGO 1.12.0, Unity Transport 1.5.0을 사용한다. `PvpNativeLan`은 호스트·참가자를 직접 UDP로 연결하며 호스트가 주행·승객·신호·점수를 확정한다. 방 목록을 등록하는 것만으로는 게임 패킷 경로가 생기지 않는다.
- 권장 후보는 양쪽 PC에서 외부 서비스로 TCP 443 연결을 여는 방식이다. HTTPS로 방을 등록·조회하고 WSS로 경기 메시지를 중계한다. 기존 호스트 권한과 최대 4인 경기 구조를 활용하되 통신 연결부 변경과 중계 서비스 운영이 필요하다. 회사 PC끼리의 인바운드 UDP 개방은 필요 없다. 회사 정책이 해당 도메인·앱의 WSS 연결을 제한하면 관리자 허용이 필요하다. [WebSocket 표준](https://www.rfc-editor.org/rfc/rfc6455.html#section-4.1)
- 관리형 Unity 방식도 방 조회와 참가를 지원한다. 공식 포트 문서는 게임 중계에 37000–37100을 사용하며 WSS가 TCP를 쓴다고 설명한다. WSS에 필요한 Unity Transport 버전도 현재 잠금 버전보다 높다. TCP 443만을 목표로 할 때는 별도 443 중계 후보와 구분한다. [방 검색·참가](https://docs.unity.com/en-us/mps-sdk/join-session) · [중계 포트](https://docs.unity.com/en-us/mps-sdk/networking/ports) · [전송 요건](https://docs.unity.com/en-us/mps-sdk/networking/networking-toc)

## 변경과 확인 범위

- `git status --short`, `rg`, `Get-Content`로 현재 이력, PvP 범위와 코드, 패키지 잠금을 대조했다. Unity 공식 문서에서 조회·중계 포트·WSS 요건을 확인했다.
- 이 기록과 `docs/work-history/LATEST.md`를 갱신했다. 게임 소스, 실행 파일, 서비스 설정은 이번 검토에서 변경하지 않았다.

## 다음 검증과 재현

1. 양쪽 사내 PC에서 예정된 같은 주소로 HTTPS 방 조회와 WSS 연결·메시지 왕복을 확인한다. 브라우저의 일반 HTTPS 성공만으로 WSS 연결을 판정하지 않는다.
2. 통합 실행본에서 유선 호스트 방 만들기 → Wi-Fi PC 목록 새로고침 → 참가 → 주행을 확인한다. 호스트·참가자를 바꿔 반복한다.
3. 방 종료, 가득 찬 방, 버전·지도 불일치, 중계 연결 실패, 경기 중 지연·끊김을 확인한다. 서비스 주소와 두 대역에서의 실측은 미확정이다.
