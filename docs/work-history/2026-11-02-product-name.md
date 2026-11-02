# 표시 명칭 통일 — 택시 시뮬레이션

사용자 요청으로 ‘은계 택시’/‘은계 택시 시뮬레이터’ 등 제품 표시를 **택시 시뮬레이션**으로 통일한다. 모드 선택 제목·로딩 제목·Unity 제품명/Windows 창 제목·현재 README/배포·인터페이스 안내를 수정했다. 지도 지역명 ‘시흥 은계지구’와 내부 실행 경로 `builds/EungyeTaxi/EungyeTaxi.exe`는 지역/호환 식별자다. 과거 작업 기록·옛 화면 이미지는 당시 증거로 유지한다.

기존 캠페인·사용자 편집 지도는 제품명 변경 후에도 같은 위치에서 읽는다. `CampaignSaveLocation.DirectoryFor`에 기존 폴더를 고정하고 `BusinessProgress`, `RoadMapStore`, 저장 보호 probe가 함께 사용한다. 기존 저장 폴더명에 남은 이전 제품명은 호환용이며 파일을 이동/삭제하거나 덮어쓰지 않는다. 회사명은 유지한다. 음악·에어컨·창문 실제 조작 보류도 유지한다.

변경 파일: UnifiedModeBootstrap/DriveSession/PrototypeBuild/ProjectSettings, CampaignSaveLocation/BusinessProgress/RoadMapStore/UnifiedModeProbe, 기존 DriveFeedbackSmokeTest의 제품명 기대값, README/배포/현재 인터페이스·지도 편집 안내.

검증은 이름 검색·diff 확인, BuildHybrid, 기존 제목 검사 포함 실제 Player smoke 및 배포 manifest만 최소 수행한다. 빌드/실제 실행/게시 결과는 완료 후 아래에 기록한다.

## 실제 검증·실행본

- 소스 `500dd9800fe77e57654d373b9cba8ccb69e7b305`, `PrototypeBuild.BuildHybrid` 실제 종료0 (`artifacts/product-name-build.log`).
- 실제 Player `-batchmode -nographics -driveFeedbackTest -hybridV2` 종료0. 기존 검사20개 통과, `PASS player title`. 패키지 `EungyeTaxi_Data/app.info`도 회사명 Taxi Prototype/제품명 택시 시뮬레이션으로 일치한다.
- 기존 사용자 저장4파일 SHA256 불변. 저장/편집 지도 경로만 고정된 이전 폴더를 사용한다. 추가 리뷰·전체 캠페인·성능 반복 검사는 수행하지 않았다.
- 실행폴더139파일/115,283,958바이트, 배포 manifest 크기/SHA256 전수 일치. 자체 생성 DoNotShip 디버그 폴더는 artifacts에 보관했다. 현재 실행 경로는 `builds/EungyeTaxi/EungyeTaxi.exe`다.
- 메뉴·로딩·빌드 제품명·현재 README/안내에는 이전 제품명 표시가 남지 않았다. 저장 호환 식별자, 지역명, 과거 기록/이미지는 구분해서 유지한다.

## 게시 영수증

`git log -1 --oneline -- docs/work-history/2026-11-02-product-name.md`로 실제 배포 커밋 `bfa26d65cb3aa32a13cc3c33152943d28e519248`을 확인했다. main push 종료0과 원격 동일 해시를 확인했고, 원래 main도 fast-forward했다. 원래 실행폴더139파일/115,283,958바이트의 VerifyOnly 종료0이며 기존 마인드맵 두 파일·미추적 작업은 보존했다. 영수증 자신의 해시는 추측해 기록하지 않는다.
