# 2026-10-23 프로젝트 지도 편집·Git 배포

## 요구사항과 구현

사용자 승인: 도로뿐 아니라 건물·보도·주차장·식당 부지를 프로젝트 지도 자체로 편집하고, 다른 PC에서 pull 후 빌드해도 수정 내용을 유지한다.

- 정식 자료: `config/maps/project/eungye.project.json`. 기준 지도 + 편집 문서/전체 장면을 포함하며 Git으로 관리한다. 초기 장면23,171객체·건물3,293·차로1,789. 원본 NGII/OSM와 기존 입력3개는 변경하지 않는다.
- 간편 **건물·부지**: 종류/이름 검색·지도 선택, 생성/삭제, 드래그·회전·배율·높이·꼭짓점. 도로 외형과 실제 차로 그래프를 구분한다. 서비스 부지의 정차점/방향/접근 차로를 실제 시작·마감·식사·휴식, 내비와 지도 핀에 연결한다.
- **프로젝트**: 저장소 자동 발견/수동 경로, 정식 저장/다시 열기/폴더 열기. 저장 전 형상·주행 침범·서비스 차량 영역·왕복 접근·필수 주차장을 검사한다. 파일 잠금·읽은 버전 검사·원자 교체/백업으로 동시 편집/pull 덮어쓰기를 막는다.
- BuildHybrid는 정식 자료를 검증·Resources 복사해 포함한다. 프로젝트 자료가 있으면 원본 입력/재생성에 의존하지 않는다. 실행 시 예전 자동 보도/시설 생성이 편집 장면을 덮지 않는다.
- 개인 초안과 정식 자료는 구분한다. 프로젝트 버전이 다른 개인 기본 수정본은 자동 적용하지 않는다. 기존 scene 없는 문서의 null/빈 장면 차이와 Undo를 보존한다.
- 검토 보도의 삼각형 집합은 공유 변을 제거해 외곽선으로 복원하고, 구멍/비정상 접합은 정확한 면적을 유지하는 ‘분할’ 면으로 제공한다. 높은 차로의 노면/충돌체를 유지한다.
- 실제 직렬화 내용 해시로 최근 기준 장면2개를 캐시하며, 편집/Undo 문서 참조·리비전으로 미리보기와 변경 상태를 갱신한다. 프로젝트 검증 캐시는 매번 내용 해시를 확인한 뒤에만 재사용한다.

## 변경 파일

- 모델/런타임: `Assets/Scripts/ProjectMapScene.cs`, `ProjectMapPackage.cs`, `ProjectMapStore.cs`, `RoadMapDocumentJson.cs`, `RoadSurfaceIndex.cs` 신규. MapData/MapCatalog, RoadMapDocument/Composer/Validator/Runtime/Store, WorldBuilder, RoadEnvironment, CorridorPolish, ReviewedPavement, SceneBuildingVisibility, RestaurantSiteLayout, FuelStationLayout, ServiceStops, BusinessProgress, FullMapOverlay, DrivingReadability 연결.
- 편집 UI: RoadMapEditorScene/Project 신규, RoadMapEditor/Simple/SimpleInteraction/Canvas/Facilities 및 RoadMapTrialSession. 기존 간편·고급·분리 시험 기능 유지.
- 검사/빌드: ProjectMapBuild, ProjectMapSceneTests, ProjectMapSceneEditorTests, ProjectMapStoreTests, ProjectMapEditorProbe 신규; PrototypeBuild/MapIdentityProbe/RoadMapEditorProbe/RestaurantLotProbe 조정. 위 Assets 경로는 모두 `unity/YeongtongTaxi/` 아래다.
- `tools/check_workspace.ps1`: 프로젝트가 있으면 없는 재수입 입력을 허용하되, 존재하는 입력의 잘못된 해시는 계속 거부한다. 재수입용 `-RequireRawInputs` 추가.
- `tools/update_distribution_manifest.ps1`: 실제 검증 JSON 입력과 프로젝트 지도 SHA256/내용 해시/크기를 기록.
- 사용법/PC 이동/기획12/범위/체크리스트/설계·계획/이력/배포 실행폴더와 manifest 동기화.

## 검증과 실제 결과

- RED: 저장 구현 없는 반사 검사 exit1(`red.log`), 장면 구현 없는 검사 exit1(`scene-red.log`).
- 초기 실패 원인 수정: 보도 triangle soup를 외곽선으로 오인, Unity inline class의 null 역직렬화, 동일 차로/거리의 0m 경로를 접근 불가로 오인. 음수/NaN/경계/자기교차/중복/서비스 침범·접근 실패는 그대로 거부한다.
- 독립 코드 리뷰3건(변경 장애물과 기존 정차 칸, 높은 차로, 접근 불가능 시설) 수정 후 재검토에서 남은 차단 사항 없음.
- Unity 장면·UI·저장16검사·간편7·기존 도로15·확장 검사 exit0(`cache-green.log`, `final-build.log`).
- 원본 입력3개와 기존 hybrid JSON/각 meta를 artifacts 임시 폴더로 분리하여 Strict와 ProjectMapBuild.Build exit0(`clean-input-check.txt`, `clean-build.log`). finally에서 원래 위치로 복원.
- 프로젝트 실행 검사23항목 exit0: 저장/다시 열기/동시 변경 차단/다른 폴더에서 동일 지도 구성/서비스 위치와 핀/3해상도 버튼/영업 시간 보존. 처음 실행은 테스트의 영업 모드 옵션 누락으로 HUD 오류가 발생하여 `-hybridV2 -oneDayMode`로 재실행해 오류 없이 통과.
- 1280×720,1600×900,1920×1080의 평면/프로젝트 화면과3D 캡처 확인. 최초 해상도 전환 직후 캡처 지연을 발견해 렌더 완료 프레임에서 동기 캡처하도록 검사 보완.
- 기존 시험 창 검사의 고정12초 대기가 새 프로젝트 초기 로딩보다 짧아 실패했다. 최대90초 안에서 실제 자식 종료와 exit0를 확인하도록 개선했다.
- 3일 상태/새 프로세스 복원, HUD 회귀 exit0(통합 직전 실행 검사).
- 모든 로그는 `artifacts/project-map/`이며 원본 입력은 변경 없음. 후속 통합 최종 결과는 아래에 기록한다.

## 통합과 로컬 변경 보존

기존 관리 worktree `codex/project-map-editor`에서 작업했다. 시작037b066 이후 main의 운전·식당 개선0735c58/배포92b725e를 작업 스냅샷으로 보존 후 fast-forward하고 지도 소스를 재적용했다. FullMapOverlay/ServiceStops/RestaurantLotProbe는 충돌 없이 합쳐졌으며 법규 기본OFF·카메라/조향·메뉴3종을 보존한다. 주행/M UI 재배치는 원래 채팅의 승인 대기 상태를 유지한다. 기본 폴더의 README/LATEST 및 라이선스/XMind 미커밋 변경은 별도로 보존한다.

## 재현

1. [편집 사용법](../road-map-editor-guide.md): 은계 exe → 출발 → Esc → 지도 편집 → 건물·부지 → 검사/시험 → 프로젝트 저장.
2. 변경된 정식 JSON 커밋·푸시 → 다른 PC pull → Unity2022.3.62f2의 `PrototypeBuild.BuildHybrid` → 새 exe 실행. 정식 자료가 있으면 입력 ZIP 없이 빌드 가능.
3. Unity 배치 `-executeMethod ProjectMapBuild.RunTests` 또는 `ProjectMapBuild.Build`.
4. 실행 검사는 `-eungye -hybridV2 -oneDayMode -projectMapTest`, 기존 편집은 `-roadMapEditorTest`. 오래된 개인본 검사에는 `-projectMapStaleTest -roadMapInput <stale-personal.json>`.
5. 모델 `pwsh -File tools/test_prototype.ps1`, 입력 `pwsh -File tools/check_workspace.ps1 -Strict -RequireRawInputs`, 배포 `pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly`.

## 미검증/남은 범위

실제 다른 물리 PC 실행, 사람이 마우스로 모든 형상을 편집하며 전체3일을 운전하는 검사, 모든 새 다각형·교차로·부지 조합, 현장 규제/거리뷰 대조는 별도다. 자동 위치 제어/API/화면 캡처와 실제 차량 물리 검사를 구분한다. 부지 외형을 바꾸면 도로 노면·차로와의 연결 및 실제 진입 폭을 시험해야 한다. 원본 재수입 작업과 주행/M UI 승인 대기는 이번 완료로 처리하지 않는다.
## 최종 통합 검증

- 최신 main92b725e의 소스와 통합한 `ProjectMapBuild.Build`에서 장면/편집/저장·기존 도로 검사와 원본 입력 없는 빌드 종료0(`integrated-clean-build.log`). 최종 `PrototypeBuild.BuildHybrid`도 원본 JSON4개/각 meta를 임시 분리한 상태에서 종료0(`release-final-build.log`); finally 복원 완료. `BuildOptions.CleanBuildCache` 적용.
- 통합 후 새 실행 프로브의 RuntimeInitializeOnLoads 등록 누락으로 검사가 시작되지 않아 제한시간을 넘긴 사례가 있었다. 캐시 정리만으로 해결되지 않아 기존 도로 검사와 동일하게 `DriveSession` 시작점에서 검사 옵션을 보고 직접 연결했다. 최종 프로젝트23검사(`release-project.log`), 개인 구버전 차단3검사(`release-stale.log`), 기존 도로 편집28검사/시험 자식 종료0·부모 저장 바이트 보존(`release-editor.log`) 모두 종료0.
- 최종 은계 식별 종료0(`release-identity.log`): project contentHash `19d3037e76db5a176eba20568624f32e1d25e2c47c51a4bbbd26d32b8191da1b`, roads1727/stops12/extent2100×1750.
- 통합 후3일 상태/새 프로세스 복원20항목과 HUD20항목 종료0(`integrated-threeDayTest.log`, `integrated-threeDayReloadTest.log`, `integrated-presentationHudTest.log`). 이는 상태/API 검사이며 수동3일 주행이 아니다.
- 최종 숨김 창 실행의 스크린샷은 검게 캡처되어 시각 검증 자료로 사용하지 않는다. UI 시각 확인은 통합 전 표시된 창의1280/1600/1920 화면과3D 확인 결과이며, 최종 실행에서는 저장/API·버튼 크기 계측만 회귀 검사했다.
- 최종 식당20항목 종료0(`release-restaurant.log`): 일반 차량 유지, 차량 물리 진입/출차 측면·연석 접촉0, 실제 정차 칸/방향, 도로변 식사 차단, 메뉴3종/중복 결제 방지. 모델23묶음(`tools/test_prototype.ps1`) 종료0, 필수 입력 Strict/RequireRawInputs 종료0. 원본 입력 해시는 변경하지 않았다.
- 실제 소스 커밋: 31b176a2115269eb841ed4994f146e0077b3f260 (이력 파일 git log 확인). 배포 실행폴더 128파일/92861804바이트를 manifest로 생성·SHA256 전수 확인 종료0. 정식 지도 21342670바이트/파일 SHA256 1b8ab6b86d435935d74cac8c83249daee41eb61a3d89d09fccfca3004c706714. main에는 이 소스 및 검증된 실행폴더를 함께 반영한다.
