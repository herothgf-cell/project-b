# 2026-11-12 PvP LAN 대기실 문구 정리

## 요청과 구현

첨부 화면에서 밑줄 친 `빈자리는 봇`, 식사·휴식 요약, 피로 증가·최대치 안내를 제거했다. `unity/EungyeTaxi/Assets/Pvp/Runtime/UI/PvpPrototypePanel.cs`의 `DrawNativeLobby`에서 경기 시간은 `경기 900초` 형식으로 표시한다. 방 상태, 호스트 IP·포트, 참가자·차량, 경기 시작과 나가기 버튼을 표시한다. 식사·휴식·피로 요약은 방 규칙 설정에서 볼 수 있다.

관련 기획은 `docs/content-design/pvp/30-native-lan.md`, 구현 항목은 `docs/prototype-development-checklist.md`에 기록했다.

## 검증과 재현

- 대기실 소스 대조에서 수정 전 `빈자리는 봇`, 식사·휴식·피로 요약, 경기 시간 단독 표시 3항목의 실패를 확인했다. 수정 후 대기실의 세 문구 제거와 상태·호스트·시간·참가자·버튼, 방 규칙 설정의 요약 호출 확인이 통과했다.
- `builds/EungyeTaxi/EungyeTaxi.exe`에서 PvP를 선택하고 **방 만들기**로 LAN 대기실을 열면 확인할 수 있다.
