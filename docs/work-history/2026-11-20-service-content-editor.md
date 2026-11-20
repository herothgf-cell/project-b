# 설정 관리에 시설·차량음·차내 요청·지명 연결

2026-11-20 · 은계 r49 · 자체 도시 PvP A2. 게임에서 구현한 기능을 저장소 루트의 `설정 관리.cmd`에서도 조정하도록 연결했다. 사용자 지정 프로필과 기존 캠페인 저장은 보존했다.

## 구현과 변경 파일

- 승객 요청에서 창문·온도 사용 여부, 적격 일반 손님 추첨 비율, 대사 네 줄, 이행 만족도를 편집한다. 기본값은 기존 1/3과 +2다. 차량에서 네 소리를 개별 설정하며 기기의 개인 음소거 설정도 적용한다.
- 생활 시설에서 승인된 배경 8종과 메뉴 그림 3종을 각각 미리 보고 선택하며 메뉴명을 변경한다. 승하차 지명에서 기본 12개·추가 12개·대체 정차면 1개의 이름을 바꾼다. 같은 지점 반대편, 호출·수첩 표시 및 현장 정차 표지에 반영한다.
- 관리 서버·모델·화면은 `tools/content-editor/`, 프로필 원본과 Unity 동봉 데이터는 `config/content/`와 `unity/EungyeTaxi/Assets/Resources/Content/`에 있다. `tests/content-editor/context-ui.test.mjs`에서 관리 화면과 그림 복원 동작을 검사한다.
- 게임 코드는 ContentTuning, VehicleAudio, PassengerJobs, PassengerBehavior, KoreanText, PassengerStopCatalog, WorldBuilder, FacilityArt, ServiceStops, PersonalRoomHUD와 ShiftDevEndProbe다. LAN 방의 공용 프로필에 새 키가 들어가므로 PvpNativeLan 및 관련 Editor 검사에서 호환 버전을 v22로 올렸다.
- [실제 관리창 화면](../ui/2026-11-20-facilities/settings-editor.jpg) · [설정 도구 안내](../content-tuning-tool-guide.md)

## 검증과 재현

1. `설정 관리.cmd` → 생활 시설 → 제육백반에서 메뉴 그림을 소고기국밥으로 바꾸고 미리보기를 확인한다. 기본값 버튼으로 원복할 수 있다. 새 프로필에서 저장 → 게임에 적용하면 다음 새 캠페인/새 방에 사용한다.
2. `node tools/content-editor/choice-catalog.mjs . --verify`로 설정과 내장 리소스 일치를 확인했다. 카탈로그 1,666개, 기본 프로필 186,702바이트. 필수 지도 입력 검사 `tools/check_workspace.ps1 -Strict` 문제 0.
3. `node --test tests/content-editor/*.test.mjs tests/content-manifest.test.mjs` 111/111 통과. localhost 및 임시 Git 쓰기가 차단된 샌드박스의 초기 7건은 해당 작업을 허용한 환경에서 재실행했다. `tools/test_content_tuning.ps1` C# 누적 1,471건과 C#/Node 프로필·이관 해시 대조 통과.
4. 분리된 관리 서버에서 승인 이미지 11개의 응답 바이트를 Unity 리소스와 대조했다. 시험값 7개를 저장·불러오기·적용한 뒤 파일 내용을 대조했다. 별도 Chrome에서 실제 관리 화면의 그림 선택·미리보기·변경 개수·원복을 확인했다. 산출물은 `artifacts/editor-services/`다.
5. 분리한 개발 사본에서 Unity 2022.3.62f2 `CabinSoundChecks.VerifyAndBuild` 실행. 지점 39개 및 오디오 클립 4개 검사 후 `PrototypeBuild.BuildHybrid` 종료 0. 기본 실행본에 `-facilityArtCheck -facilityProfile <시험 프로필 절대 경로> -shiftCaptureOutput <출력 폴더>`를 전달해 위치 제어 32건을 통과했다. 이 중 6건은 실제 관리 서버가 적용한 파일 로딩, 맞은편 지명, 소리 플래그, 배경·메뉴 텍스처, 온도 요청 대사·이행 보상, 메뉴명에 대한 검사다.
6. 재현용 프로필의 변경값은 경적 끔, 기사식당 배경을 휴식 구역으로, 제육백반 그림을 국밥으로, 해당 메뉴명을 ‘시험 정식’으로, 수인로 서쪽을 ‘시험도로’로, 이행 만족도 7, 온도 대사를 ‘시험 {온도}도’로 설정한다.

자동 물리 주행, 전체 캠페인 수동 플레이, 두 PC LAN 연결과 실물 장치의 출력 음량은 이번 검사 범위에 포함하지 않았다.

## 통합 실행본

소스 커밋 `0b2b116`에서 빌드된 관리 DLL, boot.config 및 resources.assets를 `builds/EungyeTaxi/`에 반영했다. 기본 실행본 `tools/test_pvp_condition.ps1 -OutputDirectory artifacts/editor-services/pvp`에서 위치 제어 24건과 `PVP_CONDITION_PLAYER_OK`를 확인했다(종료 0). `tools/update_distribution_manifest.ps1 -VerifyOnly` 및 `tools/update_pvp_distribution_manifest.ps1 -VerifyOnly` 모두 출하 143개 파일 145,388,655바이트에 대해 크기·SHA256 검증 통과. 로컬 사용자 프로필 1개는 별도 검증했다.
