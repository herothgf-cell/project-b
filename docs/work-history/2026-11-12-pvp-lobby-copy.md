# 2026-11-12 PvP LAN 대기실 문구 정리

## 요청과 구현

첨부 화면에서 밑줄 친 `빈자리는 봇`, 식사·휴식 요약, 피로 증가·최대치 안내를 제거했다. `unity/EungyeTaxi/Assets/Pvp/Runtime/UI/PvpPrototypePanel.cs`의 `DrawNativeLobby`에서 경기 시간은 `경기 900초` 형식으로 표시한다. 방 상태, 호스트 IP·포트, 참가자·차량, 경기 시작과 나가기 버튼을 표시한다. 식사·휴식·피로 요약은 방 규칙 설정에서 볼 수 있다.

관련 기획은 `docs/content-design/pvp/30-native-lan.md`, 구현 항목은 `docs/prototype-development-checklist.md`에 기록했다.

## 검증과 재현

- 대기실 소스 대조에서 수정 전 `빈자리는 봇`, 식사·휴식·피로 요약, 경기 시간 단독 표시 3항목의 실패를 확인했다. 수정 후 대기실의 세 문구 제거와 상태·호스트·시간·참가자·버튼, 방 규칙 설정의 요약 호출 확인이 통과했다.
- `pwsh -NoProfile -File tools/check_workspace.ps1 -Strict` 종료 0, 필수 입력 문제 0. Unity 2022.3.62f2의 `PrototypeBuild.BuildHybrid` 종료 0, 은계 지도 r17. 로그는 `artifacts/pvp-lobby-copy-build.log`에 있다.
- 통합 실행본 복사본을 `-pvpNativeLanCheck -pvpNativeLanRole host -pvpNativeLanPort 19647 -contentTest`로 열었다. 1280×720 호스트 대기실의 방 상태, 호스트 IP·포트, `경기 300초`, 참가자와 차량을 확인했고 요청한 세 문구는 표시되지 않았다. 경기 시작 전에 테스트 창을 닫았으며 프로세스 종료는 0이다.
- 소스 `d8de977` 기준으로 통합 실행본의 `Assembly-CSharp.dll`, `boot.config`와 `distribution/EungyeTaxi-build-manifest.json`을 갱신했다. 테스트 복사본과 실행 파일 및 변경 파일 2개의 해시가 일치했다. 통합/PvP `VerifyOnly` 각각 종료 0, 143파일·131,535,058바이트.
- `builds/EungyeTaxi/EungyeTaxi.exe`에서 PvP를 선택하고 **방 만들기**로 LAN 대기실을 열면 확인할 수 있다.

검증 범위는 호스트 대기실 화면이다. 다른 PC 참가, 경기 진행, 물리 주행, 전체 캠페인은 이번에 검증하지 않았다.
