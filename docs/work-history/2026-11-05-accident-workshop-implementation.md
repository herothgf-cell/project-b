# 사고 처리·공업사 구현·통합 배포

## 요구와 진행 상태

승인한 `docs/content-design/08-accident-and-workshop-proposal.md`대로 싱글/PVP 사고 처리·공업사 구현과 검증 후 기본 실행 파일 갱신 요청. 차량 구매·물품/보험·과실·구급차는 제외한다.

원래 폴더의 진행 중 HUD 작업을 보존하기 위해 앱 관리 worktree0384, `codex/accident-workshop`에서 구현한다. 시작 기준b0b5245. 별도 HUD 커밋3338612를 최종 빌드 전에 통합한다. 이 문서는 진행 기록이며 최종 배포 완료 여부는 아래 영수증을 따른다.

## 구현

- 엔진 독립 `AccidentState`와 장부: 사건3,000원·45초·미납, 중복 비용/처리 방지, 시간 경과만 피로/포만감·졸음에 적용.
- `AccidentController`, `PassengerJobsAccident`: 보행자/대기 승객 접촉과 swept 검사, 안전 정차/복구, 운송 중단/확정 운임 보존, 결과 화면.
- `BusinessProgress`/차량/세션 및 LAN: 사고 상태 저장/재개, 단일 플레이 확인/일시정지, PVP 호스트 권한/45초·잠금 복제. 프로토콜v4로 양쪽 업데이트 필요.
- `WorkshopLayout`/예약·기존 시설/내구도·지도·봇 연결: 맵별2곳, 일반 수리1·견인2·대기1칸. 기존 유료 주유소 정비 예약 유지, PVP 수리/견인 완료 시 결제.
- 모델/Editor/Player/물리/LAN 검사 추가. 전체 변경 파일은 이력 커밋의 diff를 따른다.

## 현재 확인한 검증

- `tools/test_accidents.ps1`: 초기 사고 API 없음 RED → 최종77개 GREEN. `tools/test_vehicle_damage.ps1`191개 통과. 모델 상세 기록 `artifacts/accident-model-progress.md`.
- `AccidentIntegrationChecks.Run` 초기 controller 없음 RED. Build1은 새 worktree Resources 지도를 준비하기 전 MapCatalog.Load해서 실패; 정식 ProjectMapBuild.Prepare 후 실행하도록 수정.
- Build2: 사고 통합7·공업사 배치/예약42·Live22/체크포인트500 등 회귀 통과, `PrototypeBuild.BuildHybrid` exit0. 지도 입력 파일은 변경하지 않고 정식 project map에서 runtime 공업사를 생성한다.
- 첫 실제 Player: 비용·미납·중복·확정 운임/취소 통과 후 보행자 접촉 fixture 실패. 보이지 않는 actor 선택 가능성을 분리해 진단/수정했으며 최종 재검사 전 완료로 보지 않는다.
- 별도 리뷰: 안전 복구 대기 후 자연 정차에서 잠금이 남는 문제, 봇 출차 component root 오류, 처리 중 졸음 노출 누락을 수정하고 회귀 추가.
- 기존 전체 모델 runner는 기존 ManualHandlingTests의 S제동 기대값 불일치에서 실패(actual8.823529). 해당 생산 코드 수정 없이 관련 나머지 독립 모델은 통과; 최종 별도 기록 필요.

## 재현·남은 작업

Unity2022.3.62f2 `-executeMethod AccidentWorkshopBuild.Build`. 실행 검사 `tools/test_accident_workshop_player.ps1`(기본/`-Reload`/`-Pvp`), `-workshopPhysicalCheck -workshopPhysicalTest -singlePlayer` 및 `-pvpWorkshopPhysicalCheck`, `tools/test_pvp_native_lan.ps1 -Reconnect`.

실제 Player 싱글/재개/PVP·물리 시설 입출차·두 Player LAN·화면·사용자 저장 보존·최신 UI 통합 빌드·배포 manifest는 진행 중이다. 사람 수동20분/3일·다른PC4명 수용은 자동검사와 별도다.

기본 배포는 기존 `builds/EungyeTaxi/EungyeTaxi.exe`/은계 싱글·A2 PVP 유지, 최종 실행본 갱신 아직 미확정. 이번 worktree 필수 입력 Strict exit0/issues0, 원본 입력·ZIP 변경 없음. PC별 작업 경로는 로컬 환경 정보다.

## 통합 검사 진행 (Build4)

- HUD 소스3338612와 배포e29a0cb 병합 완료. 사용자 수정 소스/미추적 자료와 저장 경로 유지.
- Build4: `AccidentWorkshopBuild.Build` → `PrototypeBuild.BuildHybrid`, 종료0. Editor 사고7/공업사51/Live22/checkpoint500 통과.
- PVP Player50개 통과: 실제45초 사고·사람/봇 동일 청구,25초 수리 완료 결제·중단 무결제,4대 동시30초 견인·중복 예약 방지, 수리 중 사고 취소 및 봇 출차 컴포넌트.
- 두 loopback Player LAN `tools/test_pvp_native_lan.ps1 -Reconnect`: 호스트47/클라이언트61, 종료0. 재접속·호스트 물리 입력/공유 호출·사고 이벤트/3,000원/입력 잠금/45초 동기화. 증거 `artifacts/pvp/native-lan-20261105-181038-19bec553`.
- 은계 공업사 실제 WheelCollider 입출차15개/종료0: 두 시설 진입·정차 방향·속도·출차·도로 합류. 실제 사람이 운전한 검사는 아니다. `artifacts/workshop-physical-eungye.txt`, 시설별 PNG.
- 싱글 접촉 fixture는 FreezeAll이 속도 대입을 막는 원인 확인, 이동 시 FreezeRotation으로 수정하고 속도 검사 추가. PVP 물리 probe가 봇을 선택한 원인도 Human 명시로 수정했다. 생산 판정 기준을 완화하지 않았다.
- Build4 ScreenCapture 결과가 검은 프레임이어서 UI 확인 완료로 처리하지 않았다. 백그라운드 렌더와 Splash 완료 대기 후 재검사한다.
- 사용자 저장/호환 백업·지도 autosave14파일 SHA256은 검사 전과 동일(변경0). 최종 빌드/재검사 뒤 다시 비교한다.
## Build5 수용과 한계

- 최종 생산 코드 Build5 종료0, Editor 사고7/공업사51/Live22/checkpoint500 통과.
- 싱글50/PVP50 Player assertions 각각 종료0. 싱글은 실제 보행자 역방향2m/s·대기 승객2m/s 공통 detector,0/1m/s 제외, 운송 취소·확정 운임·장부·공업사 수리 계약을 위치 제어로 검증했다. PVP는 실제 호스트 타이머를 사용했다.
- A2 실제 물리17개/종료0: Human 차량·4개 WheelCollider 확인, 두 공업사 진입/출차. 은계15개와 함께 네 시설의 실제 물리 경로를 확인했다. 시설 캡처 확인, 사고 UI ScreenCapture는 숨김 창에서 검게 나와 시각 검증 미완료로 남긴다.
- 저장 재개 검사기가 StartupReady 직후 비동기 RestoreLive 이전에 조회한 실패를 확인했다. Build6은 검사기만 수정(생산 기능은 Build5와 동일)하며 복원 완료 후 재개/중복 방지를 확인한다.
- `tools/check_workspace.ps1 -Strict`: 종료0/Input issues0. 프로젝트 지도·관련 입력/ZIP 변경 없음.
- 모델 전체 runner는 기존 ManualHandlingTests의 S제동 기대값 불일치(actual8.823529)에서 중단하는 기존 한계가 있다. 이번 사고77/손상191/예약·시설 어댑터 및 별도 주요 회귀 결과와 구분한다. 무관한 조작 수치를 바꾸지 않았다.
- 다른PC LAN, 사람이 직접 조작하는 장시간/전체3일 캠페인, 실제 패드, 사고 UI 시각 수용은 별도다. 자동 위치 제어·물리 경로·loopback 검사를 수동 플레이 완료로 표현하지 않는다.

## 재현

- 기본 실행: `builds/EungyeTaxi/EungyeTaxi.exe`, 은계 싱글/A2 PVP. LAN v4 양쪽 실행본을 함께 갱신한다.
- 빌드: Unity2022.3.62f2 `-batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod AccidentWorkshopBuild.Build -logFile <log>` (검사 뒤 PrototypeBuild.BuildHybrid).
- 모델: `pwsh -File tools/test_accidents.ps1`, `pwsh -File tools/test_vehicle_damage.ps1`, `pwsh -File tools/test_operation_planning_adapters.ps1`.
- Player: `pwsh -File tools/test_accident_workshop_player.ps1`, 이어 `-Reload`, 별도 `-Pvp`. 기본 격리 저장은 artifacts/accident-workshop-test.json, 사용자 저장과 분리된다.
- 물리: exe `-workshopPhysicalCheck -workshopPhysicalTest -singlePlayer`, PVP는 `-pvpWorkshopPhysicalCheck`. 작업 디렉터리를 저장소 루트로 지정. 출력 artifacts/workshop-physical-{map}.txt/PNG. watchdog은 외부180초.
- LAN: `pwsh -File tools/test_pvp_native_lan.ps1 -Reconnect -TimeoutSeconds 300`.
- 배포/지도: `pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly`, `pwsh -File tools/update_pvp_distribution_manifest.ps1 -VerifyOnly`, `pwsh -File tools/check_workspace.ps1 -Strict`.
## 최종 빌드·재개 수용

- Build6 `artifacts/accident-build-6.log`: 종료0, Editor 사고7/공업사51/Live22/checkpoint500. Build5 이후 생산 기능 변경 없이 검사기 복원 대기만 수정했다.
- `artifacts/accident-build6-reload/result.json`: 새 프로세스8개/종료0. 유료 pending 사고 복원,3,000원 한 번,45초 한 번,재확인 멱등.
- 최종 사용자 파일14개 SHA256 변경0. `docs/ui/2026-11-05-accident-workshop/`에 네 공업사 실제 물리 캡처 보관. 소스/기획을 커밋한 뒤 이 실제 커밋으로 manifest를 재생성하고 기본 폴더를 갱신한다.
후보 소스1c50958111e6de4cb2b6a210476a47e82c5d653d(실제 git log 확인)로 manifest 갱신, 통합/PVP VerifyOnly exit0:140파일115,580,745바이트. 원래 폴더에서 콘텐츠 설정 도구의 새 미커밋 변경을 발견해 덮어쓰지 않고 해당 작업 완료 후 통합한다. 아직 기본 폴더 배포 완료가 아니다.
