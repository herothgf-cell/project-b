# 2026-11-26 단속 카메라 지도 표시와 지도 필터 (은계 싱글)

## 요구 (사용자, 2026-11-26)

1. 단속 카메라를 지도에 표시하고, 지도에서 잘 보이게 한다. 먼저 시안을 본다.
2. 시안 A(노란 카메라 아이콘)로 정하고, 아이콘은 CCTV 모양으로 한다.
3. 카메라만 있거나 제한속도만 있는 곳은 원래대로 보인다(확인 질문).
4. 지도에 제한속도·카메라 보기 켜기/끄기 필터 버튼을 더한다.

## 시안

실제 전체 지도 캡처 위에 지도 좌표로 세 안을 그려 비교했다([비교 그림](../ui/2026-11-26-speed-camera-map/camera-map-options.png)). 지도 좌표는 지도 2100×1750m가 946px로 그려진 캡처에서 쟀다.

- A: 노란 원 카메라 아이콘 + 제한속도 꼬리표
- B: 제한속도 원판 + 검은 카메라 배지
- C: A + 150m 경고 범위

사용자가 A를 골랐다.

## 구현

| 내용 | 동작 |
|---|---|
| 아이콘 | `tools/art/draw_speed_camera_icon.py`로 `Resources/UI/speed-camera-cctv.png`(128px)를 그렸다. 검은 테 노란 원 안에 검은 CCTV(비스듬한 몸체, 차양, 왼쪽 아래를 보는 렌즈, 빨간 표시등, 오른쪽 벽 받침)가 있다 |
| 지도 표시 | `SpeedCameraMap`이 전체 지도(28px)와 미니맵(20px)에 카메라를 그린다. 오른쪽 아래에 그 카메라의 제한속도 꼬리표(빨간 테 흰 원, 30/50/60)를 붙인다. 같은 지점의 양방향 쌍은 하나로 센다. 아이콘이 겹칠 만큼 가까운 카메라는 하나로 묶고 왼쪽 위에 파란 개수(예: 은빛초 앞 2)를 단다. 17개 항목이 16곳으로 그려진다(검사 캡처 기준) |
| 원래대로 | 제한속도만 있는 곳(카메라 없는 구간 시작)은 기존 빨간 테 원판 그대로다. 카메라 아이콘 바로 아래에 같은 제한속도의 구간 시작 원판이 있으면, 원판은 다시 그리지 않는다(아이콘 꼬리표가 같은 숫자를 보여 줌). 카메라 자리와 다른 곳의 원판, 구간 선은 그대로다 |
| 겹침 | 시설 이름표가 카메라 아이콘을 덮지 않도록 이름표 배치 장애물에 카메라 자리를 넣었다. 카메라는 시설 아이콘 위에 그린다 |
| 필터 | 전체 지도 왼쪽 위 보기 버튼(주요 시설·교통 상황·교통 법규) 옆에 "제한속도 구간"(60 원판)과 "단속 카메라"(CCTV) 버튼을 더했다. 끄면 버튼이 흐려지고, 지도와 미니맵 모두에서 감춘다. 처음에는 둘 다 켜져 있다. 카메라 버튼은 은계 싱글에서만 보인다 |
| 검사용 빌드 | `PrototypeBuild.BuildHybridTrial`이 `-trialOutput <경로>`를 받는다(다른 세션이 `artifacts/hybrid-trial`을 쓰는 중이어서 `artifacts/camera-map-trial`에 빌드) |

## 변경 파일

- 새 파일: `unity/EungyeTaxi/Assets/Scripts/SpeedCameraMap.cs`, `Resources/UI/speed-camera-cctv.png`, `tools/art/draw_speed_camera_icon.py`
- `FullMapOverlay.cs`(필터 버튼·그리기 순서·이름표 장애물), `DrivingReadability.cs`(미니맵), `MapFacilityLabelLayout.cs`(버튼 줄 장애물 폭), `SpeedLimitSections.cs`(카메라와 같은 원판 생략)
- 검사: `SpeedLimitProbe.cs`(아이콘·묶음·필터 확인, 필터 끈 캡처 2장)
- `Editor/PrototypeBuild.cs`(`-trialOutput`)
- 문서: 이 파일, `docs/ui/2026-11-26-speed-camera-map/`, `LATEST.md`

## 검증

| 검사 | 종류 | 결과 |
|---|---|---|
| `-contentTest -speedLimitCheck` | 실행본(검사용 빌드 `artifacts/camera-map-trial`), 위치 제어 | 35건 통과. 새 3건: 아이콘 읽음, 카메라 묶음(17개 중 16곳), 두 필터가 켜진 채 시작 |

빌드 당시 작업 폴더에는 다른 세션들의 미커밋 변경(분실물·시설·방지턱 등)이 있었다. 검사용 빌드에는 그 변경이 섞였을 수 있다.

캡처: [전체 지도](../ui/2026-11-26-speed-camera-map/final-map.png) · [확대](../ui/2026-11-26-speed-camera-map/final-map-zoom.png) · [미니맵](../ui/2026-11-26-speed-camera-map/final-minimap-zoom.png) · [카메라 끔](../ui/2026-11-26-speed-camera-map/final-map-cameras-off.png) · [제한속도 끔](../ui/2026-11-26-speed-camera-map/final-map-limits-off.png)

## 미검증 / 남은 것

- 배포: 응급실 지도 안 작업과 함께 소스 5be9a52f에서 배포 실행본을 다시 만들었다([응급실 기록](2026-11-26-in-map-emergency-room.md)의 배포 항목). 배포 실행본 제한속도 검사 35건 통과.
- 필터 버튼을 마우스로 눌러 보지 않았다. 검사는 필터 값을 직접 바꿨다. 필터 상태는 저장하지 않으며, 게임을 다시 켜면 둘 다 켜진다.
- 미니맵에서 내 차가 카메라 위에 있으면 내 차 표시가 아이콘을 가린다.
- 대은로 50 카메라 2대는 4차에서 대은로 제한을 끈 뒤에도 카메라 자체 제한 50으로 단속하고 꼬리표도 50이다(기존 동작 그대로).

## 실행·재현

- 플레이: 은계 싱글 → M(전체 지도). 왼쪽 위 다섯 버튼 중 넷째(60)·다섯째(CCTV)가 필터다.
- 아이콘 다시 그리기: `python tools/art/draw_speed_camera_icon.py`
- 검사: `EungyeTaxi.exe -contentTest -speedLimitCheck -speedLimitOutput <폴더>`
