# 2026-11-24 골목 주차 A: 은행로144번길 (은계 싱글)

## 요구

사용자 요청 "순차적으로 모두 진행"(2026-11-24)의 첫 묶음. [상세 기획](2026-11-24-next-candidates-detail-design.md) 2.3과 사용자 결정([결정 기록](2026-11-24-play-check-speed-fault.md#결정-기록)): 실제와 대수·위치가 같지 않은 주차 차량을 "게임 연출"로 밝히고 넣는다. 그 골목에서 일반 차량을 뺀다. 대상은 로드뷰에서 실제 길가 주차 줄을 본 은행로144번길 한 곳이다. 골목을 뺄 때 주변 일반 차량 흐름이 눈에 띄게 막히면 다시 묻는다.

## 구현

- 자료: `config/maps/eungye-parked-cars.json`(사본 `Assets/Resources/eungye-parked-cars.json`). 도로 이름, 한쪽 연석, 일반 차량 제외, 근거("게임 연출. 근거: 로드뷰 표본 조사 … 대수·위치·어느 쪽 연석인지는 게임 추정")를 적는다. 좌표는 적지 않고 실행 때 그 도로의 차로에서 자리를 찾는다.
- 배치(`ParkedAlley`):
  - 한쪽 연석만 쓴다(그 도로의 첫 차로와 같은 방향의 차로 오른쪽). 실제 조사에서 한쪽 길가에만 줄이 있었다.
  - 묶음 3~5대, 차 간격 6.5m, 묶음 사이 20m. 차로마다 같은 배치가 나온다.
  - 세우지 않는 곳: 승강장·횡단보도 15m, 교차로·교차로 규칙 지점 25m 안, 굽은 곳, 서 있는 물체가 있는 곳.
  - 연석에서 0.2m 띄워 세운다. **게임 지도에서 이 골목의 차로는 연석 가까이 있어(차로 중심에서 연석까지 1.8~2.2m) 주차 차량이 차로를 0.1~0.2m 넘게 가린다.** 택시는 중앙선 쪽으로 1.0~1.4m 비켜 지나가야 한다. 비켜 간 택시가 중앙선을 넘지 않는 자리(중앙선까지 0.5m 여유)에만 세운다.
  - 네 대 중 한 대는 1톤 화물차 모양이다(조사에서 승용차와 화물차를 봤다).
  - 퇴근 후 드라이브(밤)에는 묶음 대수를 1.5배로 다시 세운다.
  - 주차 차량은 고정 물체다. 닿으면 시설 사고(내 과실)다.
- 일반 차량: `DistrictTraffic`이 그 도로의 차로를 지나는 경로를 쓰지 않는다(은계 싱글만). 이미 있던 경로 캐시와 같은 방식으로 막는다.

### 상세 기획과 다르게 한 것

- 상세 기획은 B 유형(10~11.9m)을 "양쪽에 엇갈려"로 적었다. 실제 조사에 맞춰 한쪽 연석만 썼다.
- 상세 기획의 계산은 차로 중심이 도로 폭의 1/4 지점이라고 가정했다. 실제 게임 차로는 연석 쪽에 붙어 있어, 처음 구현(차로를 가리지 않는 자리만)으로는 한 대도 놓이지 않았다. 차로를 일부 가리되 중앙선을 넘지 않고 지날 수 있는 자리로 바꿨다.

## 설정 (설정 툴 "법규", 항목 8개)

| id | 기본값 | 뜻 |
|---|---:|---|
| `road.parked.enabled` | 1 | 켜기(다음 시작부터) |
| `road.parked.groupMin` / `groupMax` | 3 / 5 | 묶음 대수 |
| `road.parked.gapMetres` | 6.5m | 차 간격 |
| `road.parked.groupSpacingMetres` | 20m | 묶음 사이 |
| `road.parked.clearMetres` | 15m | 승강장·횡단보도에서 비우는 거리(교차로는 +10m) |
| `road.parked.nightMultiplier` | 1.5 | 밤 배수 |
| `road.parked.passMarginMetres` | 0.3m | 택시가 남길 여유 |

## 변경 파일

- 새 코드: `unity/EungyeTaxi/Assets/Scripts/ParkedAlley.cs`(`ParkedAlleyRules`, `ParkedAlley`), `ParkedAlleyProbe.cs`, `Assets/Editor/ParkedAlleyChecks.cs`
- 새 자료: `config/maps/eungye-parked-cars.json`, `Assets/Resources/eungye-parked-cars.json`
- 고친 코드: `DistrictTraffic.cs`(골목 경로 제외), `DriveSession.cs`(연결), `DistrictTrafficSmokeTest.cs`(시험 경로가 주차 골목을 피함), `ContentTuning.cs`·`tools/content-editor/profile.mjs`(`road.parked.`)
- 설정: `config/content/catalog.json`, `default-profile.json`과 Resources 사본
- 문서: 이 파일, `docs/content-design/08-fun-improvements.md`, `docs/content-design/INDEX.md`, `docs/prototype-development-checklist.md`, `docs/work-history/LATEST.md`

필수 생성 입력 지도(`config/maps/local-input-manifest.json` 대상)는 바꾸지 않았다.

## 검증

| 검사 | 종류 | 결과 |
|---|---|---|
| `ParkedAlleyChecks.Run` | 편집기 규칙 | 12건 통과. 자료 사본 일치, 대상 도로, 기본값, 묶음 배치(3~5대·6.5m·20m), 같은 배치 반복, 밤 배수, 비켜 가기 규칙 |
| `-alleyParkingCheck` | 실행본, 위치 제어 | 아래 표 |
| 설정 툴 시험 | Node | 109건 통과, 카탈로그 1,804항목 |

| 본 것 | 결과 |
|---|---|
| 주차 차량 | 19대(세 차로에 7·6·6대), 13곳은 교차로 규칙 지점 근처라 빠짐 |
| 위치 | 모두 은행로144번길, 포장 노면 위, 승강장·교차로·횡단보도에서 떨어짐 |
| 일반 차량 | 시작 때와 20초 뒤 72대 중 골목을 지나는 경로 0대. 일반 차량 승하차 지점 수는 골목 제외 전과 같음(11) |
| 택시 통과 | 세 차로마다 주차 차량 옆을 1.0~1.4m 비켜 6m/s로 지나감: 접촉·사고·법규 기록(중앙선·역주행 포함)·운전 점수 변화 없음 |
| 밤 | 19대 → 22대, 낮으로 돌리면 19대 |

화면: `artifacts/alley/player/alley.png`(저장소에 넣지 않음).

### 회귀

골목 주차를 넣은 실행본에서 돌렸다.

| 검사 | 결과 |
|---|---|
| `-junctionGridlockTest` | 통과(200초 교착·90초 정지 없음) |
| `-callBoardCheck` | 115건 통과 |
| `-districtTrafficTest` | 4건 실패. 같은 4건이 이전 실행본에서도 실패하는 기존 문제다. 다만 "신호 대기 줄" 시험이 고르는 시험용 경로가 은행로144번길을 지나 이번에는 주차 차량에 막혔다. 실제 일반 차량은 이 골목을 쓰지 않으므로, 시험도 주차 골목을 지나지 않는 경로를 고르게 고쳤다(`DistrictTrafficSmokeTest.cs`). 고친 시험에서도 같은 4건이 실패한다. 이번에는 시험용 경로가 골목을 지나지 않아 주차 차량에 막히지 않는다. 다만 "멈춘 차량"이 2대(4차 커밋본)에서 4대로 늘었다. 원인은 확인하지 않았다([시외 콜 기록](2026-11-24-out-of-town-call.md)의 회귀 참고) |

골목 주차 검사는 한 번 "법규 기록 1건(−1점)"으로 실패한 뒤 다시 돌린 세 번은 통과했다. 그때 어떤 기록이었는지는 남지 않아 확인하지 못했다(지금은 실패 시 법규 기록을 남기도록 검사를 고쳤다).

### 검사의 한계

- 택시 통과는 검사가 위치를 직접 옮긴 것이다(사람이 핸들로 비켜 가는 것이 아님).
- 차로 유지 보조(H)를 켜고 이 골목을 달리면 보조가 차로 가운데로 끌어 주차 차량에 닿을 수 있다. 보조는 제동을 대신하지 않는다. 확인하지 않았다.
- 길안내는 이 골목을 여전히 고를 수 있다.

## 미검증 / 남은 것

- 사람 플레이: 좁은 골목을 비켜 가는 것이 재미인지 번거로움인지, 대수가 적당한지.
- 주변 일반 차량 흐름의 사람 눈 확인(자동 검사는 아래 회귀만).
- 골목 주차 B(다른 골목)는 하지 않았다.

## 실행·재현

- 플레이: 은계 싱글에서 은행로144번길(지도 좌표 약 (450~600, 1065~1175)).
- 끄기: `road.parked.enabled`=0(다음 시작부터).
- 규칙 검사: Unity 배치 `-executeMethod ParkedAlleyChecks.Run`
- 실행본 검사: `EungyeTaxi.exe -contentTest -alleyParkingCheck -alleyParkingOutput <폴더>`
