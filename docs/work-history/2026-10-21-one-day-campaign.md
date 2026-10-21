# 독립 하루20분·운전자 컨디션 프로토타입

기획 기준 `d8e35a3`, 구현 착수 기준 `21c3601`. 기본 은계 `PrototypeBuild.BuildHybrid` / `builds/EungyeTaxi/EungyeTaxi.exe`.

## 이전 작업의 남은 부분

승차 조건 분리·흰 건물 침범 제외·보도 승객·10% 주유·시나리오 주변 노면 정비는 이전 구현을 유지한다. 캡처 교차로의 정확한 ID를 통한 수동 우회전 재현, 일반 교통 속 수동 운전/연료 밸런스, 거리뷰 기반 전체 차로·신호 확정은 미완료다. 은계 전체가 실측 검증됐다는 뜻이 아니다.

## 이번 실제 구현

| P7 | 구현 |
|---|---|
| 01 | 기본 실행1,200초·08~20시. 고정3만원 승패 제거, 적자도 독립 완료 결과. 탑승 중 마감은 하차까지 대기, 기존 운송 기한 유지. 자발적 퇴근은 주차장. |
| 02 | 포만감80/피로10. 분당포만감−5/피로+4, 포만감25 미만 피로+6. 프레임 분할과 무관한 경계 적분. 피로100은 호출/승차 제한·미탑승 취소, 탑승 유지. 차량 조작/매너/운행 점수 입력은 변경하지 않음. |
| 03 | 반복 식사3,000원·50초·포만감+70, 부족금 미납. 무료 수면100초·피로−40. 둘 다 엔진 정지. 부족 시간 차단·정확한 잔여 시간 허용·확정1회 처리. 마감 자동 식비 제거, 주차500원1회. +10% 주유 유지. |
| 04 | 9코스·17요청 재사용. 초반/후반 짧은 경로, 중반 식당 접근·순환 후보 우선. 경로/운임/기한 변경 없음. 예상 피로와 마감 초과 가능성 표시. 설정 `eungye-day20-v1-courses1`. |
| 05 | 새 파일 `campaign-day20-v1.json`과 `.driving`. 구3일 `campaign-v2.json` 미변경. 시작/정산 경계에서 회계·컨디션 함께 복원, 운행 사건 즉시 저널 유지. 완료 결과를 ID별 중복 없이 보존. 새 하루 확인/저장 성공 후만 초기화, 중도 재시작은 비교 제외. |
| 06 | 자동 모델·위치 제어·기존 요청·시설 물리 회귀 수행. 수동 하루 밸런스와 실제 UI 시각 확인은 남겨둠. |

결과에 수익/비용/미납, 운송 수, 식사·수면 횟수·시간, 종료 연료·컨디션, 예산 경과·마감 이후 시간·메뉴 포함 실제 경과를 표시한다. 같은 설정/교통 추종 ON/OFF 결과만 비교하며 혼합 설정은 자동 비교 제외. 재실행에서도 완료 회차의 교통 설정을 사용한다.

## 변경 파일

- 새 모델 `DriverCondition.cs`, `OneDayOfferPolicy.cs`; `PrototypeState.cs`, `EconomyLedger.cs`, `CampaignAccount.cs`.
- 연결 `BusinessProgress.cs`, `ServiceStops.cs`, `PassengerJobs.cs`; UI `OneDayHUD.cs`, `BusinessHUD.cs`, `DriveHUD.cs`.
- `tests/OneDayCampaignTests.cs`, `tools/test_prototype.ps1`, `OneDayCampaignProbe.cs`, `FuelStationProbe.cs`.
- 개별 기획05/07/09/10, 범위·체크리스트·플레이 가이드·LATEST·본 이력·실행 폴더·배포 manifest.

## 검증 명령과 실제 결과

| 종류 | 명령/인자 | 결과 |
|---|---|---|
| RED 모델 | `OneDayCampaignTests` | 신규1,200초 모드 미존재로 실패 확인 후 구현 |
| RED 런타임 | `-oneDayTest -hybridV2` | 기본 하루 모드 미연결 실패 확인 후 연결 |
| 모델 전체 | `tools/test_prototype.ps1` | 종료0,21묶음 통과. 구3일 회귀도 유지 |
| Unity 빌드 | `PrototypeBuild.BuildHybrid` | 종료0. 기존 그래프/시설/우회전 회귀 포함 |
| 위치 제어 통합 | `-oneDayTest` | 종료0,28 PASS. 마지막 승객 유지, 피로 제한, 메뉴 정지, 식사/수면, 중복 결과, 재시작 취소/파일쓰기 실패 포함 |
| 별도 프로세스 복원 | `-oneDayReloadTest -mitTrafficOn` | 완료2회·최종 식사1회/수면1회·계정 복원 검사. 저장된 교통 설정 복원도 검사 |
| 기존9코스 | `-remainingPrototypeTest -hybridV2 -oneDayMode` | 종료0,56 PASS. 상황 미체험 승하차/주유 유지 |
| 기존17요청 | `-passengerExpansionTest -hybridV2 -oneDayMode` | 종료0,151 PASS |
| 자동 물리 주행 | `-fuelFacilitiesTest -hybridV2 -oneDayMode` | 종료0,주유소3곳 입구→주유→출구·측면/연석 접촉0. 컨디션 전체 하루 검사가 아님 |
| 지도 입력 | `tools/check_workspace.ps1 -Strict` | Input issues:0,필수3지도/ZIP 미변경 |
| 화면 캡처 시도 | 일반 렌더링/배치,1600×900·1280×720 | 출력이 검은 화면이라 시각 검증으로 인정하지 않음 |

로컬 증거: `artifacts/one-day-red-build.log`, `one-day-red.log`, `one-day-final-build.log`, `one-day-final.log`, `one-day-runtime.txt`, `one-day-reload.txt`, `one-day-nine-courses.log`, `one-day-requests.log`, `one-day-fuel-physical.log`, `prototype-model-tests.txt`. 자동 검사 중 테스트 고정 차량의 kinematic 속도 경고와 의도한 파일쓰기 실패 경고는 분리해서 해석한다. `-nographics`의 미지원 셰이더 경고는 실제 화면 검증 근거가 아니다.

## 검토·결정

독립 읽기 전용 코드 검토를 수행했다. 재실행 후 결과 비교 설정을 현재 교통 토글로 오인하는 문제와 단독 새 하루 테스트 인자의 구 모드 선택 문제를 수정했다. 마감 추가시간 문구는 짧게 줄였지만 실제 한글 잘림 여부는 수동 확인이 남는다. 테스트 기본 모델은 구 모드를 유지하고 기본 게임만 `ForOneDay()`를 사용하여 기존 저장을 임의 변환하지 않는다.

## 미검증 / 다음 작업

1. 일반 차량 포함 수동 하루: 운송3~5건 가능성, 식사1회·수면0~1회 선택, 순수익/연료/실제 소요를 측정. P7-06 전체 미완료.
2. 실제 화면에서 준비·거점·수면 확인·결과 비교·마감 후 운송 문구 가독성과 한글 잘림 확인. 자동 캡처 불가를 완료로 바꾸지 않는다.
3. 이전 지도 현장 대조와 정확한 캡처 교차로 수동 재현은 별도 유지.

실행: 새 exe 실행 → 하루 준비 → 영업 → F7 식당/주차장 → 탑승 승객은 목적지 하차 → 결과 → 새 하루 확인. 진행 중 앱 종료는 현재 운송/회계를 중간 저장하지 않으며 시작 경계로 복원한다. 운행 사건 저널은 그대로 남는다.

기존 README 및 라이선스 문서/증거 파일은 본 커밋에서 제외한다. 소스 커밋은 실제 커밋 후 배포 manifest에 기록하며, 본 이력의 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-21-one-day-campaign.md`로 확인한다.
