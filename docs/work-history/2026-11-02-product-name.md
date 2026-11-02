# 표시 명칭 통일 — 택시 시뮬레이션

사용자 요청으로 ‘은계 택시’/‘은계 택시 시뮬레이터’ 등 제품 표시를 **택시 시뮬레이션**으로 통일한다. 모드 선택 제목·로딩 제목·Unity 제품명/Windows 창 제목·현재 README/배포·인터페이스 안내를 수정했다. 지도 지역명 ‘시흥 은계지구’와 내부 실행 경로 `builds/EungyeTaxi/EungyeTaxi.exe`는 지역/호환 식별자다. 과거 작업 기록·옛 화면 이미지는 당시 증거로 유지한다.

기존 캠페인·사용자 편집 지도는 제품명 변경 후에도 같은 위치에서 읽는다. `CampaignSaveLocation.DirectoryFor`에 기존 폴더를 고정하고 `BusinessProgress`, `RoadMapStore`, 저장 보호 probe가 함께 사용한다. 기존 저장 폴더명에 남은 이전 제품명은 호환용이며 파일을 이동/삭제하거나 덮어쓰지 않는다. 회사명은 유지한다. 음악·에어컨·창문 실제 조작 보류도 유지한다.

변경 파일: UnifiedModeBootstrap/DriveSession/PrototypeBuild/ProjectSettings, CampaignSaveLocation/BusinessProgress/RoadMapStore/UnifiedModeProbe, 기존 DriveFeedbackSmokeTest의 제품명 기대값, README/배포/현재 인터페이스·지도 편집 안내.

검증은 이름 검색·diff 확인, BuildHybrid, 기존 제목 검사 포함 실제 Player smoke 및 배포 manifest만 최소 수행한다. 빌드/실제 실행/게시 결과는 완료 후 아래에 기록한다.
