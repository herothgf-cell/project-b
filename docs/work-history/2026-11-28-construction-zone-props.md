# 2026-11-28 셔터 취소와 도로 공사 구간 소품

## 요구사항

- 상가 셔터 작업은 취소하고 다른 후보로 작업한다(사용자 요청).
- Megascans 시험을 바탕으로 실제 적용 후보를 정리해 문서로 남기고 작업한다(2026-11-27 요청).

## 실제 구현

- 셔터 취소:
  - `Assets/Editor/MegascansShutterTrial.cs`, `Assets/Scripts/ShopShutterTrial.cs`와 meta를 지웠다.
  - Git 제외 로컬 사본 `Assets/ArtTrial/`(FBX, 텍스처, 구운 프리팹)도 지웠다. `art-source/megascans-trial/`의 원본 ZIP과 기록은 남긴다.
  - 시험 README에 취소 표시를 했다.
- 후보 문서: [Fab Megascans 참고 실제 적용 후보](../proposals/2026-11-28-fab-megascans-application-candidates.md).
  - 원본은 공개 저장소 때문에 넣지 않는다. Megascans는 크기·형태 참고로만 쓰고, 게임 모양은 직접 만든 저폴리 카툰 메시로 만든다.
  - 1순위는 도로 공사 구간 소품이다. 공사 카드가 차로를 닫아도 지금까지 지도에 빨간 선으로만 보였다.
- `Assets/Scripts/ConstructionZoneProps.cs`(새 파일):
  - 공사 카드 날 닫히는 계수로 서행 차로(`DailyRoadConstruction.LaneId`, 18점 차로 중심선)를 따라 소품을 세운다.
    - 15 m 테이퍼: 2.5 m 간격 라바콘.
    - 옆 차로 경계: 3 m 간격 라바콘.
    - 끝 6 m 되돌림.
    - 진입부: 노랑 "<<<" 화살표 표지.
    - 가운데: 빨강·흰색 번갈이 임시 방호벽과 굴착 자리.
  - 라바콘은 코드로 만든 12각 메시다. 크기는 0.36×0.36×0.69 m이고, 검정 받침·주황 몸체·흰 반사띠 2줄이다.
  - 충돌체는 없다.
- `DailyRoadConstruction.Apply(session,…)`: 차로 상태를 바꾼 뒤 `ConstructionZoneProps.Sync`로 소품을 세우거나 지운다. 같은 상태로 다시 불려도 소품이 맞춰지도록 이른 반환을 없앴다.
- 검사 인자:
  - `-constructionPropsTest`: 소품만 세워 찍는다.
  - `-constructionPropsShot -contentTest`: 격리된 contentTest 캠페인에서 실제 하루 카드 경로로 차로를 닫았다가 연다.
  - 둘 다 `artifacts/construction-props/`에 화면 3장과 보고서를 쓴다.

## 변경 파일

- `unity/EungyeTaxi/Assets/Scripts/ConstructionZoneProps.cs`(+meta, 새 파일)
- `unity/EungyeTaxi/Assets/Scripts/DailyRoadConstruction.cs`
- 삭제한 파일:
  - `unity/EungyeTaxi/Assets/Editor/MegascansShutterTrial.cs`(+meta)
  - `unity/EungyeTaxi/Assets/Scripts/ShopShutterTrial.cs`(+meta)
- `docs/proposals/2026-11-28-fab-megascans-application-candidates.md`(새 파일)
- `docs/content-design/images/2026-11-28-construction-{approach,alongside,raised}.png`
- `art-source/megascans-trial/README.md`
- `docs/work-history/2026-11-27-megascans-trial-props.md`, `docs/work-history/LATEST.md`

## 검증

- `PrototypeBuild.BuildHybridTrial`로 `artifacts/construction-props-trial/`에 3회 빌드했고 모두 종료 0이었다. 배포 실행 폴더는 바꾸지 않았다.
- `-constructionPropsTest`(위치 제어 검사) 종료 0:
  - 소품 17개, 화면 3장.
  - 이 모드에는 영업(business)이 없어 하루 카드 경로가 지원되지 않아(`apply(closed)=False`) 소품을 직접 세웠다.
- `-constructionPropsShot -contentTest` 종료 0:
  - `business=yes`, `apply(closed)=True`, 소품 17개.
  - 화면 3장(저녁 조명).
  - `apply(open) removed=True`, `success=True`.
  - 첫 시도는 `-contentTest` 없이 실행해 메뉴에서 멈췄다(`no DriveSession map`, 종료 1).
- 플레이어 저장 폴더 46개 파일의 SHA256이 각 실행 전후로 같았다. 백업만 하고 복원은 필요 없었다.
- 화면 판독:
  - 운전석 높이에서 노랑 화살표 표지와 라바콘 테이퍼가 바깥 차로를 막는 모습으로 읽힌다.
  - 라바콘 흰 띠가 잘 보인다.
  - 방호벽 빨강·흰색 번갈이는 1차에서 실수 계산 때문에 둘 다 빨강이었다. 정수 카운터로 고쳤다.
  - 화살표 표지 꺾쇠는 1차에서 뒷면에 붙어 있었다. 앞면 "<" 방향으로 고쳤다.

## 미검증·남은 항목

- 직접 운전하며 보는 수동 플레이 확인은 하지 않았다. 정지 카메라 캡처만 봤다.
- 공사 카드가 실제로 뽑힌 날의 전체 캠페인 흐름은 돌려 보지 않았다. 카드가 부르는 같은 `Apply` 경로를 직접 호출해 검사했다.
- 배포 실행본(`builds/EungyeTaxi/`)에는 아직 없다. 다음 `BuildHybrid` 배포 때 들어간다.
- 후보 2순위 이후는 사용자 선택을 기다린다.

## 실행·재현

    Unity.exe -batchmode -quit -projectPath unity/EungyeTaxi -executeMethod PrototypeBuild.BuildHybridTrial -trialOutput ../../artifacts/construction-props-trial/EungyeTaxi.exe -logFile artifacts/logs/construction-props-build.log
    artifacts/construction-props-trial/EungyeTaxi.exe -constructionPropsShot -contentTest
