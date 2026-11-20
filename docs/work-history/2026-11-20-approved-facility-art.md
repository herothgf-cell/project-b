# 승인된 기사식당 메뉴와 생활 시설 배경 적용

2026-11-20 · 은계 싱글 r49 / 자체 도시 PvP A2, LAN v21. 사용자가 게임 콘셉트로 개정한 메뉴 카드와 여덟 장소를 각각 승인했다. [승인 그림](../ui/2026-11-20-facilities/README.md) · [지명·경적·차내 조작 요청 승객 기록](2026-11-20-cabin-sound-places.md)

## 게임에서 확인하는 방법

1. 기본 실행본 `builds/EungyeTaxi/EungyeTaxi.exe`에서 은계 싱글 영업을 시작한다. 지도에서 식당을 선택하고 주차 칸에 정차한 뒤 F7 → 식사하기를 누른다.
2. 제육백반·된장찌개 정식·소고기국밥 중 한 장을 고른다. 파란 테두리와 체크가 선택 카드를 따라간다. 취소하면 잔액이 유지되고 식사하기로 확정하면 기존 이용 조건을 적용한다. 은계 하루 영업은 3,000원·50초·포만감 +70, PvP는 1,000원·15초·포만감 +60이다.
3. 주유소에서 F7을 누르고 세차 견적을 열면 주유기에서 세차 부스로 배경이 전환된다. 공업사 수리 칸과 휴식 구역, 덕성프라자 1층 주차 칸에도 각각 장소 그림이 뜬다. 야외 주차는 해당 모드의 주차 서비스에서 확인할 수 있다. 기사 숙소에서는 새 배경 위에 기존 구매 상태에 따라 조명과 러그가 놓인다.

## 변경 파일

- `unity/EungyeTaxi/Assets/Resources/UI/Facilities/`에 장소 8장과 음식 3장 및 Unity 임포트 설정을 추가했다. 배경은 승인한 그림과 같은 JPEG다. 음식은 승인한 메뉴 카드에서 분리해 선택 표시와 메뉴명을 런타임에 그린다.
- `unity/EungyeTaxi/Assets/Scripts/`의 FacilityArt, ServiceStops, PersonalRoomHUD, VehicleDamageController가 이미지 표시, 이용·견적별 전환, 메뉴 선택, 숙소 물건 위치를 담당한다. ShiftDevEndProbe에 격리된 경력으로 실행하는 `-facilityArtCheck`를 추가했다.
- 기획 및 현행 상태는 `docs/content-design/03-life-and-economy.md`, `docs/prototype-scope.md`, `docs/prototype-development-checklist.md`, 승인 시안 README와 LATEST에 반영했다.

## 검증과 범위

- `tools/check_workspace.ps1 -Strict` 필수 지도 입력 문제 0. 분리한 프로젝트에서 Unity 2022.3.62f2의 `-batchmode -nographics -quit -executeMethod PrototypeBuild.BuildHybrid` 실행. `PROTOTYPE_BUILD_OK`, 종료 0.
- 기본 배포 폴더의 Player를 `-facilityArtCheck -force-d3d11 -screen-fullscreen 0 -screen-width 1600 -screen-height 900 -shiftCaptureOutput <출력 폴더> -logFile <로그 파일>`로 실행했다. 임시 경력과 차량 위치 제어 검사 26건 통과, 종료 0. 텍스처 11개의 실제 로딩, 메뉴 세 장의 선택, 결제 전 잔액, 취소, 한 번만 결제, 주유소·세차·공업사·휴식 구역·숙소 주차 칸의 배경 선택, 정산과 숙소 가구 구매를 확인했다.
- `tools/test_pvp_condition.ps1 -OutputDirectory artifacts/approved-facilities/pvp`로 기본 실행본 재검사. 위치를 지정한 PvP 상태 검사 24건 통과, 종료 0. 식사 중단·완료와 금액, 15초 식사 중 다른 운전자 진행, 무료 45초 휴식, 경기 종료 처리를 확인했다.
- 숨김 창과 D3D11 배치 실행에서 얻은 PNG는 모두 투명했다. OpenGL Core는 이 빌드에 포함되지 않아 그래픽 초기화 단계에서 중단했다. 최종 화면의 실기 가독성·버튼 마우스 조작, 야외 주차장 현장 시각 확인, 실물 운전 및 전체 캠페인 수동 플레이는 미검증이다. 진단 로그는 `artifacts/approved-facilities/`에 있다.
