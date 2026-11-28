# 2026-11-28 셔터 취소와 도로 공사 구간 소품·7곳 순환·분리봉

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
- 7곳 순환과 분리봉에서 바꾼 파일:
  - 스크립트: `Assets/Editor/DailyRoadConstructionSiteFinder.cs`(+meta, 새 파일), `Assets/Editor/DailyRoadConstructionChecks.cs`, `Assets/Scripts/BusinessProgress.cs`, `DailyConditions.cs`, `RadioTicker.cs`, `ReviewedRoadFacilities.cs`
  - 콘텐츠: `config/content/{catalog,default-profile}.json`, `unity/EungyeTaxi/Assets/Resources/Content/{catalog,default-profile}.json`
  - 문서: `docs/content-design/06-day-and-career.{md,html}`, `docs/prototype-development-checklist.md`
  - 이미지: `docs/content-design/images/2026-11-28-{construction-sites,flexible-bollard}.png`

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

## 추가: 공사 구간 7곳으로 늘리기와 분리봉 모양 (2026-11-28)

요구사항: 도로 공사를 게임에 적용하고, 현대오일뱅크 앞뿐 아니라 여러 지역에서 생기게 한다. 다음 후보(분리봉 모양)도 진행한다(사용자 요청).

구현:
- `Assets/Editor/DailyRoadConstructionSiteFinder.cs`(새 파일, 보고만 함)는 다음 조건을 모두 갖춘 차로를 찾는다. 57곳이 조건에 맞았고, 그중 56곳은 닫아도 승하차 경로가 모두 이어졌다.
  - 곧은 바깥 차로다.
  - 왼쪽에 같은 방향의 열린 차로가 있다.
  - 길이가 25~90 m다.
  - 승하차 지점이 없다.
  - 앞뒤 차로에 다른 갈 길이 있다.
- 그중 지역이 고르게 퍼진 7곳을 골랐다. 이름은 가까운 건물에서 땄다.

  | 번호 | 도로 | 위치 |
  |---|---|---|
  | 0 | 계수로 | 현대오일뱅크 앞(기존) |
  | 1 | 대은로 | 우석타워 앞 |
  | 2 | 은계로 | 은행우남아파트 앞 |
  | 3 | 은행로 | 벽산3차아파트 앞 |
  | 4 | 은계로 | 은계브리즈힐 앞 |
  | 5 | 마유로 | 진성테크 앞 |
  | 6 | 은계중앙로 | 우미린레이크 앞 |

- `DailyRoadConstruction`:
  - 지점 목록 `Sites`, 현재 지점 `ActiveSite`, `SiteFor(캠페인, 조건 일차)`를 추가했다.
  - 공사 카드는 5일 순환마다 한 번 나온다. 같은 캠페인·같은 날은 같은 지점이고, 다음 공사일에는 다음 지점으로 넘어가 7번에 한 바퀴를 돈다.
  - 지점을 바꾸면 이전 지점 차로와 소품을 먼저 연다.
- `BusinessProgress`가 하루를 시작할 때 지점 번호를 넘긴다.
- 문구:
  - 조건 카드 단서·설명과 라디오 공사 문구 3개를 `{road}`·`{place}` 자리표시로 바꿨다. 두 콘텐츠 사본(`config/content`, `Resources/Content`)의 catalog·default-profile을 같이 고치고, 프로필 해시를 콘텐츠 편집기 `hashProfile`로 다시 계산했다(검증 오류 0).
  - 예전 문구가 저장된 프로필은 `Fill`이 "계수로/현대오일뱅크 앞"을 현재 지점으로 바꾼다.
- `ConstructionZoneProps.Sync`는 지울 소품을 먼저 떼어 낸 뒤 지운다. 그래서 같은 프레임에 지점을 바꿔도 새 소품이 선다.
- 분리봉(`ReviewedRoadFacilities`):
  - 원통 3개이던 주황 탄성 분리봉을 12각 메시 하나로 바꿨다. 검정 둥근 받침, 넓어지는 발, 주황 몸체, 흰 반사띠 2줄, 둥근 머리로 이뤄진다.
  - 충돌이 켜진 곳은 캡슐 충돌체를 쓴다.

검증:
- `DailyRoadConstructionChecks.Run` 종료 0(`DAILY_ROAD_CONSTRUCTION_OK`). 7곳 각각 다음을 확인했다.
  - 닫았을 때 승하차 30곳 870쌍에서 끊김 0, 경로가 공사 차로를 지나지 않음.
  - 다른 지점은 열린 채로 남음.
  - 다시 열림, 문구 채움.
  - `SiteFor`가 같은 날 같은 값이고, 공사일 7번에 7곳을 모두 돎.
  - 기존 계수로 합류·교통 캐시 검사도 통과했다.
  - 승하차 지점이 39곳에서 30곳으로 바뀌어 있어, 개수를 고정으로 대조하던 부분은 "모든 쌍"으로 바꿨다.
- 시험 빌드 `-constructionPropsShot -contentTest` 종료 0, `success=True`:
  - 7곳 모두 실제 카드 경로로 닫혔고(`applied=True`), 소품 17개, 소품 묶음 1개, 다른 지점 닫힘 0이었다.
  - 설명 문구가 지점 이름으로 바뀌었다.
  - 다시 열자 소품이 지워졌다.
  - 분리봉은 서브메시 3개로 확인했다.
  - 첫 시도는 프로필 해시 불일치(`Content hash mismatch`)로 세션이 뜨지 않아 해시를 갱신했다.
- 플레이어 저장 폴더 46개 파일 SHA256이 실행 전후로 같았다.
- 화면: [7곳 접근 시점과 분리봉](../content-design/images/2026-11-28-construction-sites.png) · [분리봉 가까이](../content-design/images/2026-11-28-flexible-bollard.png)
- 시험 빌드에는 다른 세션의 커밋 안 된 지도 r87 편집이 함께 들어갔다. 7곳 검사는 그 지도 기준이다.

## 미검증·남은 항목

- 직접 운전하며 보는 수동 플레이 확인은 하지 않았다. 정지 카메라 캡처만 봤다.
- 공사 카드가 실제로 뽑힌 날의 전체 캠페인 흐름은 돌려 보지 않았다. 카드가 부르는 같은 `Apply` 경로를 직접 호출해 검사했다.
- 배포 실행본(`builds/EungyeTaxi/`)에는 아직 없다. 다른 세션의 커밋 안 된 지도 r87 편집과 배포 폴더 재빌드가 작업 트리에 있어, 섞이지 않도록 배포 빌드를 미뤘다.
- 후보 3순위(전신주) 이후는 사용자 선택을 기다린다.

## 실행·재현

    Unity.exe -batchmode -quit -projectPath unity/EungyeTaxi -executeMethod PrototypeBuild.BuildHybridTrial -trialOutput ../../artifacts/construction-props-trial/EungyeTaxi.exe -logFile artifacts/logs/construction-props-build.log
    artifacts/construction-props-trial/EungyeTaxi.exe -constructionPropsShot -contentTest
