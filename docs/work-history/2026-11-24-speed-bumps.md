# 2026-11-24 과속방지턱을 지도 입력에 넣음 (은계 싱글)

## 요구

사용자 결정 "과속 방지턱 지도 입력"(2026-11-24). [설계안](2026-11-24-next-candidates-design.md) 7장과 [상세 기획](2026-11-24-next-candidates-detail-design.md)의 "새로 확인한 사실"(정밀도로지도 방지턱 4건)을 바탕으로 한다. 같은 날 살아 있는 미터기는 제외로 정했다([결정](2026-11-24-next-bundle-decisions.md)).

> **2026-11-24 후속(사용자 직접 플레이): 정밀도로지도 3건(ngii-1·3·4)을 껐다. 지금 지도 입력의 방지턱은 현장 조사 2곳(P12·P24)뿐이다.** 사용자 캡처에서 ngii-1(823, 378)이 J7973_3744 교차로 횡단보도 바로 뒤, 수인로 큰길 바깥 차로 한 칸에 노란 판으로 떠 있었다. 정밀도로지도의 "수인로 측도"는 게임 지도에 별도의 길로 없다. 그래서 측도 차로 중심선으로 옮긴 점이 실제로는 큰길 차로 위에 놓였다고 판단했다(ngii-3·4도 같은 측도라 함께 껐다). 측도가 게임 지도에 생기면 다시 넣는다. 입력·사본에서 세 항목을 지우고 입력 manifest를 다시 만들었다(60,295바이트, 여섯 입력 대조 불일치 0). 편집기 검사는 2곳·시설 38건 기준으로 고쳐 9건 통과했다. 실행본 `-contentTest -bumpCheck`는 무승객 통과 시험을 P12로 바꿔 배포 실행본에서 12건 통과했다. 아래 표와 검증은 처음 넣을 때의 기록이다.

## 위치와 근거

필수 지도 입력 `config/maps/eungye-road-facilities.json`(사본 `Assets/Resources/eungye-road-facilities.json`)에 새 종류 `speed-bump` 5건을 더했다. 기존 시설 36건은 그대로다.

| id | 근거 | 놓은 곳 |
|---|---|---|
| speed-bump-p12 | 현장 조사 P12 "황백 고원형 감속부"([조사 기록](../content-design/road-survey/2026-11-16-pink-field-check.md)) | 폭 9.6m 이름 없는 양방향 길, 도로 폭 전체(양끝 0.3m 띄움) |
| speed-bump-p24 | 현장 조사 P24 "황백 과속방지턱" | 같은 성격의 길, 도로 폭 전체 |
| speed-bump-ngii-1, -3, -4 | 정밀도로지도 `c4_speedbump`(은계 범위 4건 중 3건) | 수인로 옆 측도 차로의 중심선에 맞춰 옮기고, 바로 옆 수인로 차로에 걸치지 않게 그 차로 폭 안(2.9m)만 가로지름 |

- 원본 점을 가장 가까운 게임 도로 중심선(현장 2건, 0.5m) 또는 측도 차로 중심선(정밀도로지도 3건, 0~3.5m)으로 옮겼고, 방향은 그 자리 차로에 직각이다.
- **정밀도로지도 2번째 점(776, 366)은 넣지 않았다.** 근처에 달릴 수 있는 측도 차로가 없고(수인로 본선 차로만 3.8m 이상 떨어져 있음), 가까운 도로 선분의 방향도 차로와 29° 어긋난다.
- 길이 3.6m(국내 원호형 기준 길이)와 높이 0.08m는 게임 추정이며 각 항목의 `estimated`에 적었다.
- `config/maps/local-input-manifest.json`의 이 파일 크기·SHA256을 실제 파일로 다시 만들었다. 여섯 입력 모두 대조 불일치 0(Python, pwsh 없음).

## 동작

- 그림: 노란 몸체와 흰 띠(높이 8cm). **충돌체는 없다.** 그래서 넘어도 시설 사고나 손상이 생기지 않고, 일반 차량도 그대로 지나간다(일반 차량 감속은 넣지 않음).
- 판정(`SpeedBumps`): 택시 앞바퀴 위치가 방지턱을 지나는 순간의 속도로 본다.
  - 손님을 태우고 25km/h(`bump.comfortKmh`)보다 빠르면 운전 만족 −2(`bump.penalty`, 새 항목 "bump"라 급가감속·커피 미션 판정과 겹치지 않음, 6초 간격)와 손님 한마디.
  - 손님을 태우고 20km/h 이하로 넘으면 운행마다 한 번 "부드럽게 가시네요".
  - 손님이 없으면 감점 없음.
  - 넘을 때 차체가 속도에 따라 위로 살짝 튄다(12km/h 이하 없음, 55km/h에서 약 1m/s, `bump.jolt`).
- 방지턱 15m 안에서 밟은 급제동은 "필요한 제동"으로 본다(급제동 감점 면제).

## 설정 (설정 툴 "법규", 항목 6개)

`bump.enabled`(1), `bump.comfortKmh`(25), `bump.penalty`(2), `bump.cooldownSeconds`(6), `bump.jolt`(1), `bump.brakeLookMetres`(15).

## 변경 파일

- 지도 입력: `config/maps/eungye-road-facilities.json`, `Assets/Resources/eungye-road-facilities.json`, `config/maps/local-input-manifest.json`
- 새 코드: `unity/EungyeTaxi/Assets/Scripts/SpeedBumps.cs`, `SpeedBumpProbe.cs`, `Assets/Editor/SpeedBumpChecks.cs`
- 고친 코드: `ReviewedRoadFacilities.cs`(방지턱 배치·그리기), `DrivingManners.cs`(필요한 제동), `DriveSession.cs`(연결), `ContentTuning.cs`·`tools/content-editor/profile.mjs`(`bump.`)
- 설정: `config/content/catalog.json`, `default-profile.json`과 Resources 사본
- 문서: 이 파일, `docs/work-history/2026-11-24-next-bundle-decisions.md`, `docs/content-design/INDEX.md`, `docs/content-design/08-fun-improvements.md`, `docs/work-history/LATEST.md`

## 검증

사용자가 배포 실행본으로 플레이 중이라 `builds/EungyeTaxi/`에는 빌드하지 않았다. 검사용 빌드 `PrototypeBuild.BuildHybridTrial`(출력 `artifacts/hybrid-trial/`)로 확인했고, 검사는 `-contentTest`로 저장을 임시 폴더에 쓴다.

| 검사 | 종류 | 결과 |
|---|---|---|
| `SpeedBumpChecks.Run` | 편집기 규칙 | 9건 통과. 자료 사본 일치, 5건과 근거, 기존 36건 유지, 방지턱으로만 배치(벽·빗금 아님), 길이 2.9~9.0m, 기본값, 감점 조건, 튀는 정도 |
| `ParkedAlleyChecks.Run` | 편집기 규칙(회귀) | 12건 통과 |
| `-bumpCheck` | 실행본(검사용 빌드, 택시를 실제 차로를 따라 이동) | 12건 통과. 5곳 그림, 충돌체 없음. 손님을 태우고 P12를 40km/h로 넘으면 "방지턱 과속 · 손님이 흔들렸어요 −2점"과 손님 한마디, 그 앞 3~10m에서 필요한 제동 인정. P24를 18km/h로 넘으면 감점 없음. 빈 차로 NGII-1을 45km/h로 넘으면 감점 없음. 사고·수리·내구도 변화 없음 |
| 설정 툴 시험 | Node | 109건 통과, 카탈로그 1,836항목 |
| 지도 입력 manifest | Python | 6건 대조 불일치 0 |

화면: `artifacts/bump/player/bump.png`(지나간 뒤라 방지턱은 화면 밖, 알림 카드만 보임).

## 남은 것

- 사람 플레이: 1인칭·3인칭에서 방지턱이 미리 보이는지(안 보이면 함정), 25km/h 기준이 적당한지, 튀는 정도.
- 일반 차량은 방지턱에서 느려지지 않는다.
- 정밀도로지도 2번째 점, 조사 기록 P23 "방지턱 예고" 주변은 넣지 않았다.
