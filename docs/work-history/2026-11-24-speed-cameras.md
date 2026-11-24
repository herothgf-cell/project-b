# 2026-11-24 고정 단속 카메라와 과태료 (은계 싱글)

## 요구

사용자 요청 "순차적으로 모두 진행"(2026-11-24)의 네 번째 묶음. [상세 기획](2026-11-24-next-candidates-detail-design.md) 4.2와 사용자 결정: 과태료(돈으로 내는 벌금) 도입 허용([결정 기록](2026-11-24-play-check-speed-fault.md#결정-기록)), 위치는 공공데이터 16곳, 넣기 전에 이용 허락 조건 확인([결정](2026-11-24-next-bundle-decisions.md)).

## 이용 조건 확인 (2026-11-24, 앱 내장 브라우저로 공공데이터포털 열람)

- 자료: 공공데이터포털 "전국무인교통단속카메라표준데이터"(https://www.data.go.kr/data/15028200/standard.do). 소관 경찰청, 제공 경찰청(지방경찰청 기초자료 입력)·지방자치단체, 무료, 갱신 반기.
- **통합 표준데이터 페이지에는 공공누리 유형이나 이용허락범위 문구가 표시되어 있지 않다.** 포털 공통 "이용허락범위 안내"로만 연결된다.
- 같은 표준데이터의 기여 파일 두 개를 열어 봤다: 경상남도 함양군, 경기도 수원시 "무인교통단속카메라" 파일 모두 **"이용허락범위 제한 없음"**.
- 시흥시 파일은 목록 검색에서 찾지 못했다(시흥 자료는 경찰청 입력분으로 보인다). 은계 카메라의 이용허락범위를 직접 확인한 것은 아니다.
- 그래서 자료 파일에 출처("출처: 공공데이터포털", 자료명·주소·기준일·제공기관)와 위 확인 내용을 함께 적었다. 배포 전에 시흥 항목의 이용허락범위를 다시 확인하는 것을 남은 항목으로 둔다.

## 구현

- 자료: `config/maps/eungye-speed-cameras.json`(사본 `Assets/Resources/eungye-speed-cameras.json`). 은계 지도 범위의 고정식 카메라 중 게임 차로 위 17건(16지점, J6913·J6914는 같은 지점), 관리번호·도로명·설치 장소·제한속도·게임 좌표. 좌표 변환은 [조사 기록](2026-11-24-speed-limit-camera-survey.md).
- 예고: 진행 방향 앞 150m 안(옆으로 15m 안)에 카메라가 있으면 화면 아래 가운데에 "단속 카메라 {제한} · {남은 거리}m"가 깜빡인다. 숨은 카메라는 없다. 지도(M) 아이콘은 넣지 않았다.
- 단속: 카메라 지점을 지나는 순간(옆으로 12m 안) 속도가 카메라 제한 + 10km/h를 넘으면 단속한다. 같은 카메라(같은 지점의 쌍 포함)는 하루 한 번.
- 과태료: +10 초과 ~ +20 2,000원, ~ +40 4,000원, 그 위 7,000원. 현금에서 바로 빠지고, 모자라면 부족분은 사고 처리비 등과 같은 미납으로 남는다. 장부 종류는 `fine`(마감 화면에서는 "기타"로 합쳐진다). 벌점·면허 같은 누적은 없다. 카드 "단속 카메라 · 제한 60 · 78km/h · 과태료 2,000원".
- 유혹: 그 운행에 카메라 단속이 있으면 사례금이 없다(과속 기록과 같음).
- 퇴근 후 드라이브와 영업 준비 중에는 단속하지 않는다.
- **카메라의 단속 방향은 반영하지 않았다**(자료의 방향 문구를 게임 차로 방향으로 옮기지 않음). 어느 방향으로 지나가도 단속한다.
- 대은로는 4차 피드백으로 속도계 제한 표시가 꺼져 있지만, 대은로 카메라 두 곳은 자기 제한(50)으로 단속하고 예고 표시에 50이 나온다.

## 설정 (설정 툴 "법규", 항목 6개)

`camera.enabled`(1), `camera.toleranceKmh`(10), `camera.warnMetres`(150m), `camera.fine.low`(2,000원), `camera.fine.mid`(4,000원), `camera.fine.high`(7,000원).

## 변경 파일

- 새 코드: `unity/EungyeTaxi/Assets/Scripts/SpeedCameras.cs`(`SpeedCameraRules`, `SpeedCameras`), `SpeedCameraProbe.cs`, `Assets/Editor/SpeedCameraChecks.cs`
- 새 자료: `config/maps/eungye-speed-cameras.json`, `Assets/Resources/eungye-speed-cameras.json`
- 고친 코드: `CampaignAccount.cs`(`Fine`), `HurryRequest.cs`(카메라 단속도 사례금 없음), `DriveSession.cs`(연결), `ContentTuning.cs`·`tools/content-editor/profile.mjs`(`camera.`)
- 설정: `config/content/catalog.json`, `default-profile.json`과 Resources 사본
- 문서: 이 파일, `docs/content-design/08-fun-improvements.md`, `docs/content-design/INDEX.md`, `docs/prototype-development-checklist.md`, `docs/work-history/LATEST.md`

## 검증

| 검사 | 종류 | 결과 |
|---|---|---|
| `SpeedCameraChecks.Run` | 편집기 규칙 | 8건 통과. 자료 사본 일치, 17건·16지점, 제한속도 구성, 출처 기록, 기본값, 과태료 경계, 보호구역 30, 제한 없음 |
| `-cameraCheck` | 실행본(택시를 실제 차로를 따라 지정 속도로 이동) | 10건 통과. 수인로 H0649(60)를 78km/h로 지나 150m 안 예고, 단속, 현금 10,000 → 8,000원, 장부 `fine` 2,000원, 카드. 같은 날 90km/h로 다시 지나도 단속 없음. 은계유치원 앞 J1375(30)를 35km/h로 지나면 없음, 46km/h면 2,000원 |

화면: `artifacts/cam/player/camera.png`(저장소에 넣지 않음).

### 회귀

마지막 실행본(골목 주차·시외 콜·돌발 2차·단속 카메라 모두 포함)과 그 직전 실행본에서 돌렸다. 플레이어 저장 폴더는 검사 전에 복사해 두고 검사 뒤 되돌렸다.

| 검사 | 결과 |
|---|---|
| `-alleyParkingCheck` | 16건 통과 |
| `-outOfTownCheck` | 19~20건 통과 |
| `-surprise2Check` | 22건 통과 |
| `-cameraCheck` | 10건 통과 |
| `-surpriseCheck`(돌발 1차) | 30건 통과 |
| `-hurryCheck` | 32건 통과(아래 "하차 막힘" 참고) |
| `-accidentFaultCheck` | 11건 통과 |
| `-speedLimitCheck` | 30건 통과 |
| `-callBoardCheck` | 113~115건 통과 |
| `-missionCheck` | 96건 통과 |
| `-brightUiCheck` | 104건 통과 |
| `-junctionGridlockTest` | 통과 |
| `-districtTrafficTest` | 기존 실패 4건. 다만 "멈춘 차량"이 4차 커밋본(927c9ec2) 실행본에서는 2대, 골목 주차 제외를 넣은 뒤 4대다. 늘어난 2대도 기존과 같이 신호 대기 중인 앞차와 그 뒤차다. 골목을 빼서 일반 차량 경로가 바뀐 영향일 수 있으며 원인은 확인하지 않았다 |

**하차 막힘(새로 본 문제, 원인 미확인):** 유혹 검사에서 정류장에 택시를 세워도 "공간 점유·승객 보행 대기 · 정차 유지"로 25초 넘게 하차가 끝나지 않는 운행이 자주 생겼다(과속 기록을 넣은 운행에서 특히). 일반 차량을 끄고 경유지까지 따라 세워도 남았다. 4차 커밋 뒤 처음 봤다. 검사는 이 경우 사례금이 지급되지 않았는지만 확인하도록 바꿨다. 사람 플레이에서 같은 일이 생기는지 확인이 필요하다.

### 검사의 한계

- 카메라 단속에 따른 유혹 사례금 취소는 실행본에서 따로 재현하지 않았다(유혹 검사의 과속 기록 장면만 확인).
- 미납(현금 부족) 경우를 실행본에서 만들지 않았다.
- 예고 표시가 다른 화면 요소와 겹치는지는 1920 캡처 한 장만 봤다.

## 실행·재현

- 끄기: `camera.enabled`=0.
- 규칙 검사: Unity 배치 `-executeMethod SpeedCameraChecks.Run`
- 실행본 검사: `EungyeTaxi.exe -contentTest -cameraCheck -cameraOutput <폴더>`
