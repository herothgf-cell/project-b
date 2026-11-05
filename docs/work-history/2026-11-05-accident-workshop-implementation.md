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
