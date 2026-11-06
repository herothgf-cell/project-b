# 택시 외형·1인칭 운전석 구현 (2026-11-07)

## 요청과 실제 반영
시안93bc2e5 공유 후 사용자가 진행 승인. 노란 택시 외형부터 교체하고 1인칭 운전 UI를 정리했다. 기존 시안의 스타일을 현재 Unity 생성 모델로 단순화했다. 정밀 실차 모델/도시 건물 아트/손·문 애니메이션은 이번 범위에 포함하지 않는다.

- TaxiArt: 둥근 차체, 경사진 지붕·독립 유리, 둥근 전조등·후미등·방향등, 양면 TAXI 표시·번호판, 스포크 휠. 글자의 깊이/크기를 실제 화면에서 수정.
- 운전석: 짙은 남색 대시보드, 연속 곡면 핸들·조향 연동, 낮은 디지털 속도·D/R·방향등. 승객 카드 오른쪽 아래, 점수/메뉴 왼쪽 아래, 내비 높이 축소. 실제 거울 화면 보존·재배치.
- 내비 도착 직전 정차/다음 회전 안내와 신호 구분 유지. TIP와 영업 알림의 승객 카드 겹침을 독립 검토에서 발견하고 수정.
- 질량1400, 차체 충돌1.8×.7×4, 바퀴 반경.34/질량25/서스펜션.25와 위치·마찰/조향·제동·경제 수치 유지. 시각물에 추가 충돌 없음. 외부차체 layer8, 운전석 layer10과 기존 시점 전환 유지.

## 검증 결과
- Unity TaxiArtChecks.Build → 기존 통합 검사 → PrototypeBuild.BuildHybrid exit0. TaxiArt 검사1283, PassengerAction77 포함. artifacts/taxi-art/editor-final.log.
- 위치/상태 제어 실제 Player fixture35 PASS exit0: 외형3면, 1280/1920 3인칭·1인칭·좌우·후방. 실제 충돌 개수5·바퀴 물리/시점 마스크 검사. 캡처 시 요청 컴포넌트 표시만 멈춰 일반 운전석 화면을 보관함. 사람 운전 검사가 아님.
- 기존 전체 UI/메뉴/생활거점/긴대화 Player213 PASS exit0. artifacts/taxi-art/full-ui.log.
- 실제 운전 설정 Player17, 비/와이퍼/설정 Player31 PASS exit0. 20km/h 제동거리 맑음2.552734m, 비2.833984m.
- 실제 WheelCollider 입력 loopback LAN 두 프로세스 호스트43/클라이언트54 PASS exit0, 약20m 주행. artifacts/pvp/native-lan-20261107-025415-ff2dc455. 다른PC LAN이나 사람 플레이와 구분.
- 보호 저장16개 SHA256 불변, tools/check_workspace.ps1 -Strict exit0/입력 issues0. 지도 입력·ZIP 미변경.
- 독립 코드 재검토: 도착 안내와 TIP/알림 겹침 수정 후 추가 확정 지적 없음.
- 사람 수동 장기 주행·패드·전체3일·다른PC 수용은 미검증. 세밀한 재질/모델링, 월드 아트, 승객 표정/포즈, 정산/경고 연출은 후속 디자인.

## 파일과 재현
- 새 TaxiArt.cs, TaxiArtChecks.cs, BrightUITaxiReview.cs 및 meta.
- TaxiController/Signals, DrivingCamera/Readability, CockpitUI/Mirrors, ServiceLawZones, BrightUIProbe. BuildHybrid가 FirstDrive scene ID를 재생성했으며 시작 모드/지역은 동일.
- 관련 기획05·scope·체크리스트·승인 spec/plan·LATEST, docs/ui/2026-11-07-taxi-exterior-cockpit/implemented-*.png.
- 배포: builds/EungyeTaxi 전체, distribution/EungyeTaxi-build-manifest.json. 은계/A2·LANv16·BuildHybrid.
- 재현: Unity -executeMethod TaxiArtChecks.Build; Player -brightUiCheck -taxiArtReview -brightUiOutput <폴더>; Player -brightUiCheck -fullUiReview -lifeHubUiReview -hudDialogueReview; tools/test_driving_settings_player.ps1; tools/test_rain_player.ps1; tools/test_pvp_native_lan.ps1 -Port 18981.
- 로컬 Unity 설치: D:/Program Files/Unity Hub/Editor/2022.3.62f2/Editor/Unity.exe.

## 출하
소스와 통합 실행본 커밋 후 실제 해시로 manifest 갱신·검증하고 main에 게시한다. 실제 기록 커밋은 git log -1 --oneline -- docs/work-history/2026-11-07-taxi-exterior-cockpit-implementation.md로 확인. 다른 작업의 미커밋 문서/미추적 파일은 보존한다.

출하 검증: 소스 da1667d3033d16dfc8e45c93839d8ccb367b3deb 기준 통합 manifest 생성. 통합/PVP VerifyOnly 모두 exit0, 141파일/124,809,838바이트. 원격 main 및 원본 작업폴더 반영 영수증은 후속 기록 참조.

최종 main 영수증: 실행본 커밋106bf21 원격 push 성공, 원격 heads는 main만 존재. D:/Git/project-b의 로컬 main도106bf21로 fast-forward하고 동일141파일/124,809,838바이트 VerifyOnly exit0 확인. 사용자 LATEST Higgsfield 문단 복원, 사전 미추적56파일 SHA256 불변, 이번 임시 stash만 제거. 갱신 도중 별도 Unity Package manifest/lock·PackageManagerSettings 변경이 생긴 것은 손대지 않고 보존했다. 신규 작업 브랜치는 만들지 않았으며 기존 detached 작업폴더는 유지.
현재 기본 실행: D:/Git/project-b/builds/EungyeTaxi/EungyeTaxi.exe. HUD 작업폴더 실행본도 동일하다.
