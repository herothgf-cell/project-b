# 2026-11-10 기획22 구현: 3일 캠페인 안 차량 교체·차종 특성·차고·PvP 내 차

브랜치 `feature/vehicle-purchase-3day`(main `fb5b16d`에서 분기, 문서 선행 커밋 `c1302e7`). PC kimhs-NW2. main 병합·main push·PR 없음.

## 요구사항

사용자 작업 지시(2026-11-10)와 [기획22](../content-design/22-growth-direction-and-vehicle-purchase.md) §5·§6. 시작 중고 택시 + 후보 A(하이브리드 세단) + 후보 B(넓은 LPG 택시). 차종별로 다른 값은 연료 소모·트렁크(짐 승객 수락)·승차감(부드럽게 요청 판정)·내구도 감소·회전 반경뿐이며 운임 배율·최고 속도·가속은 넣지 않는다. 차고 화면(정산 → 내 공간 → 차고), 정산 후 현금 구매·할부 없음, 다음 날부터 새 차. 색 2종 + 대시보드 장식 1슬롯(방 소품과 같은 개인 지출). 가격은 관리창 조정값(시험값). 저장은 원자적·중복 없음·구 저장 이관·새 회차 초기화. 산 외형은 캠페인과 분리된 파일에 영구 해금, PvP "내 차" 선택, 봇 무작위, 성능 표준, LAN v16→v17. 아트는 기존 둥근 택시 변형. 평점·7일·이사·면허·할부·다중 차고·음악/에어컨/창문은 하지 않음.

## 실제 구현

| 영역 | 내용 | 파일 |
|---|---|---|
| 차종 데이터 | `VehicleSpec`/`VehicleState`/`VehicleQuote`, 3종 계수(연료 1/0.65/0.95, 트렁크 가능/불가/가능, 승차감 1/1/1.08, 내구도 1/0.75/0.8, 회전 1/1/0.86), 보상 판매(내구도100=기준액, 0=절반), 승차감 판정 함수, 충돌 강도 계수. Unity 비의존(csc 검사 가능) | `Assets/Scripts/VehicleCatalog.cs` |
| 계정·저장 | `Vehicle` 상태, `BuyVehicle/BuyVehicleColor/BuyVehicleOrnament/SelectVehicleColor/ToggleOrnament`, 연료 소모 계수, 스냅샷, 복원 검증(장부 금액·소유 일치, 바꾼 차의 색 기록, 구 저장 이관) | `CampaignAccount.cs` |
| 영업 장부 | `QuoteVehicle/PurchaseVehicle`(정산 후·3일 캠페인만, 새 차 수리·세차) | `EconomyLedger.cs` |
| 런타임 적용 | 차고 작업(사본 변경→한 번 저장→실패 시 이전 장부), `ActiveVehicle`, 외형·조향 동기화, 해금 기록, 차고 Player 검사 모드 | `BusinessProgress.cs` |
| 특성 연결 | 조향각×계수 / 급가감속 판정 계수 / 충돌 강도 계수 / 하이브리드 짐 승객 유형 제외 | `TaxiController.cs`, `DrivingManners.cs`, `VehicleDamageController.cs`, `PassengerRequestUI.cs` |
| 화면 | 내 공간 방/차고 탭, 차고(측면 외형·특성 5줄·견적·구매 확인·색·화분), 정산 버튼 "내 공간 · 방/차고" | `VehicleGarageHUD.cs`, `PersonalRoomHUD.cs`, `OneDayHUD.cs` |
| 외형 | 기존 택시 차체 비율·도색 변형, 대시보드 화분(외부/운전석), 시각 전용 | `VehicleAppearance.cs` |
| PvP 해금 | 저장 폴더 `pvp-vehicle-unlocks.json`(합집합, 손상 파일은 옆에 보존 후 새로 작성), 선택은 PlayerPrefs, 시험용 `-vehicleUnlocks`/`-pvpVehicleLook` | `VehicleAppearance.cs`(`VehicleUnlockStore`), `VehicleCatalog.cs`(`VehicleUnlocks`, `PvpVehicleLook`) |
| PvP 화면·경기 | 입장 화면 "내 차"(잠김 표시), LAN 대기실 자리별 차량, 오프라인 경기 내 차·봇 무작위 외형 | `Pvp/Runtime/UI/PvpPrototypePanel.cs`, `PvpSimulationMatch.cs` |
| LAN v17 | `native-city-lan-v17`, hello `look`, room `looks[4]`(필수·검증), 호스트 정리·봇 외형·시작 후 고정, Bind 시 적용 | `PvpNativeLan.cs` |
| 관리창 | `vehicle.*` 5항목(차량 그룹), 이전 프로필 호환(C#·JS), 카탈로그/기본 프로필 재생성 | `tools/content-editor/vehicle-settings.mjs`, `choice-catalog.mjs`, `profile.mjs`, `public/app.js`, `public/content-model.js`, `ContentTuning.cs`, `config/content/*`, `Assets/Resources/Content/*` |
| 검사 | 단위 55, 차고 Player 41+재시작 11, PvP 내 차 12, LAN 프로브 v17 추가, 에디터 묶음, 기존 csc 스크립트에 `VehicleCatalog` 추가, v17 기대값 갱신 | `tests/VehicleGarageTests.cs`, `tools/test_vehicle_garage.ps1`, `tools/test_vehicle_garage_player.ps1`, `VehicleGarageProbe.cs`, `PvpVehicleLookProbe.cs`, `PvpNativeLanProbe.cs`, `tools/test_pvp_native_lan.ps1`, `Editor/VehicleGarageEditorChecks.cs`, `Editor/MenuConditionFollowupTests.cs` 등 |
| 문서·마인드맵 | 기획22 상태·§11, 03·11·13 연결, pvp README·30, 관리창 안내, CURRENT-STATE, 체크리스트, 마인드맵 6노드 25%→50% | `tools/update_vehicle_mindmap.py`, 백업 `docs/design-history/content-design/2026-11-10-mindmap-before-vehicle-impl.mmst` |

시험 가격 기본값: 하이브리드 26,000 · LPG 22,000 · 보상 판매 기준 10,000 · 색 1,500 · 장식 2,000원. R(사람 20분 순수익 중앙값) 미측정이라 R≈10,000원(3일 목표 30,000원÷3) 가정의 **시험값이며 확정 밸런스가 아니다.**

## 환경 변경

- Node.js가 없어 사용자 승인 후 `winget install --id OpenJS.NodeJS.LTS -e`로 **Node.js v26.13.0** 설치(관리창·카탈로그 생성·manifest 프로필 검증에 필요).
- 열려 있던 Unity Editor(빈 Untitled 씬)를 사용자 승인 후 종료하고 배치 모드로 빌드·검사.

## 검증 (실제 명령과 종료 코드)

위치 제어 검사(정산·주차 위치를 코드로 지정)와 자동 물리 주행, 수동 플레이, 전체 캠페인을 구분한다. 이번에는 **수동 플레이·사람 3일 캠페인·다른 PC LAN을 하지 않았다.**

| 구분 | 명령 | 결과 |
|---|---|---|
| 새 단위 | `pwsh -File tools/test_vehicle_garage.ps1` | exit0, `PASS vehicle garage 55` (시작 차 연료·판정 회귀, 구매/부족/중복/운행 중 거부, 보상 판매, 복원, 색·장식, 바꾼 차 색 기록, 연료 계수, 변조 거부, 구 저장 이관, 새 회차, 해금·외형 선택·봇) |
| 기존 단위 | `test_personal_room` 17, `test_content_tuning`, `test_content_preview` 29, `test_rain_settings` 2790, `test_accidents` 77, `test_vehicle_damage` 191, `test_passenger_care` 13301, `test_operation_planning` 49, `test_operation_planning_adapters`, `test_passenger_roster` 2224, `test_passenger_reactions` 158, `test_passenger_overrides` 42, `test_pvp_condition_settings` 26 | 모두 exit0 |
| 기존 단위(기존 실패) | `pwsh -File tools/test_prototype.ps1` | exit1: **ManualHandlingTests** `ordinary S braking ... actual=8.823529`. 변경 전 기준(같은 브랜치 시작 상태)에서도 동일 실패. 스크립트가 첫 실패에서 멈추므로 28개 검사를 개별 실행: 27 PASS(CampaignAccount/ThreeDayCampaign/Economy/DriveMath/Manner 등), ManualHandling만 실패 |
| 기존 실패(무관) | `test_choice_structure`(Empathy unanswered opportunity), `test_pedestrian_damage`(스크립트 소스 목록에 ChoiceEffects 누락 컴파일 오류) | 둘 다 exit1. 깨끗한 `c1302e7` worktree에서도 동일 실패 확인 |
| Python | `python -m unittest discover -s tests` | 42개 중 2 실패/3 건너뜀. `test_eungye_distribution` 2건(지도 ZIP/프로젝트 경로 목록)으로 변경 전과 동일 |
| 관리창 | `node tools/content-editor/choice-catalog.mjs . --verify` / `node --test tests/content-editor/` | exit0 / 100 pass |
| 마인드맵 | `python tools/update_vehicle_mindmap.py --check` / `python tools/update_growth_mindmap.py --check` | `VEHICLE_MINDMAP_OK nodes=378 raised=6` / `GROWTH_MINDMAP_OK` |
| 에디터 | Unity `-executeMethod VehicleGarageEditorChecks.Run` | exit0: MENU_CONDITION_FOLLOWUP 19, BRAKING_SETTINGS 22, DRIVING_SETTINGS 171, CONTENT_TUNING_RUNTIME 32, CASUAL_DRIVE 27, VEHICLE_DURABILITY_EDITOR 10, DRIVE_FLOW 1161 |
| 빌드 | Unity `-batchmode -nographics -quit -executeMethod PrototypeBuild.BuildHybrid` (Start-Process -PassThru + WaitForExit) | 최종 exit0. 첫 시도는 검사 코드 변수명 중복 컴파일 오류(수정) |
| 차고 Player(위치 제어) | `pwsh -File tools/test_vehicle_garage_player.ps1 -Visible` | exit0: `VEHICLE_GARAGE_PLAYER_OK checks=41`, `VEHICLE_GARAGE_RELOAD_OK checks=11`(새 프로세스 복원·새 회차 시작 차·해금 유지), `PVP_VEHICLE_LOOK_OK checks=12`. 3일차 실제 WheelCollider 조향각이 LPG 계수로 줄어듦 확인 |
| LAN v17(같은 PC 두 실행본, 자동 물리 입력) | `pwsh -File tools/test_pvp_native_lan.ps1` | exit0, `artifacts/pvp/native-lan-20261110-142036-834bfc2e`: host 48/0, client 60/0. 호스트가 게스트 외형(LPG·남색·화분) 수신, 4자리 외형 렌더·표준 성능, 게스트가 호스트 배포 외형을 로컬 순서로 렌더 |
| 기존 Player | `test_personal_room_player`, `test_driving_settings_player`(17), `test_accident_workshop_player` | 모두 exit0 |
| 기존 Player(기존 실패) | `test_vehicle_durability_player`(`arrival locked luggage can tow from unsafe crossing`), `test_passenger_behavior_player`(`food choice actual live satisfaction 1`) | exit1. 변경 전 실행본(`c1302e7` worktree의 커밋된 builds)에서도 같은 메시지로 실패 → 이번 변경과 무관 || Strict | `pwsh -File tools/check_workspace.ps1 -Strict` | exit0, Input issues 0 |
| 배포 manifest | `pwsh -File tools/update_distribution_manifest.ps1 -SourceCommit e254c55… -ValidatedOn 2026-11-10 -ValidationReport artifacts/vehicle-purchase-suite/validation.json` → `-VerifyOnly`, `tools/update_pvp_distribution_manifest.ps1 -VerifyOnly` | 각각 exit0: 143파일 127,423,944바이트 크기·SHA256 전수 일치 |
화면: [docs/ui/2026-11-10-vehicle-purchase](../ui/2026-11-10-vehicle-purchase/README.md)(차고 1·2일차, 구매 후, 3일차 새 차 운행, 완료·복원 차고, PvP 내 차 입장·경기).

## 미검증·남은 항목

- 사람 3일 플레이: 차고를 여는지, 차종 차이를 느끼는지, 차 vs 방 선택, 가격(기획22 §7). R 실측 후 가격 재조정.
- 다른 PC LAN v17(같은 PC 두 실행본만 검사), 실물 패드, 장시간 경기.
- 하이브리드 짐 승객 제외·LPG 승차감의 실제 운행 중 체감(단위·판정 함수와 후보 목록 코드로만 확인).
- 지붕 TAXI 표시등까지 도색되는 외형 한계, 외관 회전·운전석 미리보기 미구현(측면 그림 대체).
- 기존 실패 4종(ManualHandlingTests, choice_structure, pedestrian_damage 스크립트, Python distribution 2건)은 이번 변경과 무관하며 별도 작업.

## 커밋

| 커밋 | 내용 |
|---|---|
| `c1302e7` | 이전 세션 문서 선행 커밋(제안서 V9·기획22·23 등) |
| `fc5d342` | 관리창 차량 가격 5항목·카탈로그·호환 |
| `6891784` | 게임 코드(차종·계정·차고·외형·PvP·LAN v17·Player 검사) |
| `e254c55` | 검사 코드·스크립트(배포 manifest의 소스 커밋) |
| `484c9bc` | BuildHybrid 실행 폴더·배포 manifest |

이 이력·문서 커밋 해시는 `git log -1 --oneline -- docs/work-history/2026-11-10-vehicle-purchase-3day.md`로 확인한다.

## 실행·재현

- 게임: `builds/EungyeTaxi/EungyeTaxi.exe` → 은계 싱글 → 하루 정산 → "내 공간 · 방/차고" → 차고. PvP는 입장 화면 "내 차".
- 개발 검사: 위 표의 명령. Player 검사는 실행본을 쓰며 `artifacts/vehicle-garage-player/`만 초기화한다(개인 저장·해금 파일 미사용).
- 관리창: `설정 관리.cmd`(Node.js 필요) → 차량 → 차량 구매·꾸미기.
