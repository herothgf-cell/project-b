# Blender·Unity MCP 택시 품질 개선 (2026-11-07)

## 요청·결과
사용자가 Unity MCP에 이어 Blender MCP와 Unity MCP를 모두 사용하여 기존 승인 택시 시안에 가깝게 개선하도록 요청했다. 최초 Blender 연결 실패 후 사용자가 서버 연결 완료했고, 이후 실제 두 MCP로 제작·반입·렌더 확인했다.

- Blender5.2: 기존 기본 씬 보존, 별도 TaxiConcept_Blender 반복 씬. 곡면 차체/연속 지붕, 휠아치, 넓은 유리와 고무 씰, 도어 경계·손잡이·미러, 프로젝터/링 전조등·세로 후미등, 크롬 그릴·범퍼, 둥근 타이어·스포크/허브 제작.
- Unity MCP: 원본 D 프로젝트 Editor 연결 확인, 이번 파일만 동기화. 평가 메시/코너 노멀을 Mesh·7개 Material·Body/Wheel prefab으로 가져옴. 선형 Blender 색을 Unity 재질 색으로 변환. 같은 카메라/조명의 실제 렌더를 반복하여 램프 가림·유리 겹침 수정.
- TaxiArt는 Blender prefab 우선 사용, 기존 procedural 외형을 fallback으로 보존. TAXI/한글 번호판·실제 방향지시등과 1인칭/UI 유지. Rigidbody/차체 충돌/휠 위치·반경/마찰/조향/제동/경제·LAN 규칙 변경 없음.
- Blender 원본·FBX·평가 메시 JSON과 재생성 스크립트 보관. 출하 실행에는 Blender/MCP/새 패키지 필요 없음. 원본 D의 사용자 MCP Package 설정은 이번 커밋에 섞지 않음.
- 차체55,456tri+바퀴13,584tri×4=109,792tri, 공유 재질7종. 저사양/다수 차량 LOD 최적화는 후속. 시안의 모든 사진풍 재질/도시 배경과 동일하다고 주장하지 않음.

## 검증
- MCP 컴파일 성공, TaxiBlenderImport.Import()/TaxiArtChecks.Run() 성공. 최종 메시69,040tri(차체+바퀴1), artchecks36,510. 독립 검토에서 모든 삼각형/노멀 정렬·바퀴반경.34·기존+90°Z 회전 상쇄·추가 collider 없음 확인. 고정 작업폴더 경로 지적은 TAXI_REPO_ROOT 명시/검증으로 수정하고 재확인.
- Unity TaxiArtChecks.Build → PrototypeBuild.BuildHybrid exit0. artifacts/taxi-blender/editor-release.log. Editor36,510 및 기존 행동77 등 통합 검사 통과.
- 실제 Player 위치/상태 제어 fixture35 PASS exit0. 앞/뒤/측면, 1280/1920 3인칭·1인칭·좌우·후방과 기존 물리값/5collider 확인. artifacts/taxi-blender/player.log. 사람 운전 검사가 아님.
- 운전 설정 실제Player17 PASS exit0. 두 실제 프로세스 loopback LAN 호스트43/클라이언트54 PASS, WheelCollider 입력 약20m. artifacts/pvp/native-lan-20261107-035042-35efe5b5. 원격PC/사람 수동 운전과 구분.
- 보호 저장16개 SHA256 불변. tools/check_workspace.ps1 -Strict exit0, Input issues0. 지도/지도ZIP 미변경. 은계/A2·LANv16·BuildHybrid 유지.
- 중간 Unity 전용 메시 빌드는 사용자 Blender 지시로 중단됐으며 출하 근거로 쓰지 않는다. MCP 편집모드 캡처에서 SendMessage(Awake) 사용 시 ShouldRunBehaviour assertion을 관측했고 캡처용 글자 재질 직접 할당으로 교체. 제품 코드에 이 호출을 추가하지 않았고 최종 Player에는 해당 assertion 없음. 기존 Editor 씬은 root2/dirty=false 유지.
- 이번 전체3일/장시간/패드/다른PC·렌더 성능 한계 검사는 미수행. 주행 UI 기능 자체는 변경하지 않음.

## 변경·재현
- art-source/taxi/TaxiConcept.blend·fbx·unity-meshes.json·README, tools/art/build_taxi_blender.py/export_taxi_blender.py.
- Unity TaxiBlenderImport.cs, TaxiBlenderVisual.cs, TaxiArt.cs, TaxiArtChecks.cs 및 meta; Resources/TaxiConcept의 공유 mesh/material/prefab. BuildHybrid가 시작 scene ID 재생성(기능/시작지역 동일).
- docs/ui/2026-11-07-taxi-blender: MCP 스튜디오·실제 Player 이미지. Blender 파일은 현재 최종 씬 TaxiConcept_Blender.005, 이전 작업 씬과 초기 기본 씬 보존.
- 제작 재현은 art-source/taxi/README.md. 검사: Unity executeMethod TaxiArtChecks.Build; Player -brightUiCheck -taxiArtReview -brightUiOutput <폴더>; tools/test_driving_settings_player.ps1; tools/test_pvp_native_lan.ps1 -Port18983.
- 로컬 Unity 설치 D:/Program Files/Unity Hub/Editor/2022.3.62f2/Editor/Unity.exe. 실행 builds/EungyeTaxi/EungyeTaxi.exe.
- 다음 디자인: 운전석 실내 디테일, 승객 표정/포즈, 도시 건물/가로수, 정산/경고 연출. 모델LOD는 실제 저사양 프로파일 후 결정.

## 출하 영수증
소스 커밋 후 실제 SHA로 통합 manifest 생성·검증하고 main에 게시한다. 실제 기록 커밋은 git log -1 --oneline -- docs/work-history/2026-11-07-taxi-blender-quality.md로 확인한다.

출하 검증: 소스11223cad7011db0e736005b96f92a3a9cdf1cc17 기준 통합 manifest 생성. 통합/PVP VerifyOnly 모두 exit0, 141파일/126,501,464바이트. 실제 main 게시·원본 작업폴더 보존 영수증은 후속 기록 참조.
