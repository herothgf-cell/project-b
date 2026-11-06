# 인게임 날씨 설정

요구: 일시정지 설정을 운전/날씨 탭으로 분리하고, 외부 툴과 인게임에서 비를 시험한다. 다른 작업 완료 후 통합 배포한다.

구현: RainSettings/RainSettingsRuntime으로 날씨 6값 draft·검증·적용·저장실패 복구. DrivingSettingsPanel 탭/취소/탭별 기본값. 싱글 checkpoint 복원, 현재 승객 오염 미소급. LAN 호스트 변경을 weather frame으로 공유, 게스트 변경 차단(v15). 툴 디스크 프로필 보존, 새 캠페인은 툴 프로필 사용. 확률 미끄러짐 잠금 유지. 기획18/운전1/툴 안내/마인드맵 갱신.

검증: 모델2718, Node90, Unity 설정171/메뉴183/modal/casual 및 ContentTuning32, Player29, LAN host43/client54 통과. 실제20km/h 물리 제동 맑음2.552734m/비2.833984m. Player는 위치제어/실제 승차/저장/짧은 물리 검사이며 사람 수동·20분·3일·다른PC 검사가 아니다. 추가 파일잠금 복구와 통합 최종 결과는 아래에 기록한다.

재현: Esc → 설정 → 날씨 관련 설정 → 비 켜짐 → 적용. `tools/test_rain_settings.ps1`, `tools/test_rain_player.ps1`, `tools/test_pvp_native_lan.ps1`, Unity `DrivingSettingsChecks.All`. 빌드 `RainBuild.Build`에서 재질 준비 후 `PrototypeBuild.BuildHybrid`. 은계/A2 통합, 필수 지도·ZIP 불변(직전 Strict issues0, 이번 재검사 없음).

남은 검증: 사람 화면 가독성/장기 운전/다른PC LAN·확률 미끄러짐 별도 검증. 기존 미커밋 자료는 포함하지 않는다.
