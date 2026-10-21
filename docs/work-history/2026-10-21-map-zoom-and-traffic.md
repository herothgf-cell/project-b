# 전체지도 확대와 교통 밀도·잔여 범위 대조

2026-10-21. 기본 은계 실행 대상 builds/EungyeTaxi/EungyeTaxi.exe.

요구: 남은 개발과 직접 플레이 목록 정리, M 전체지도 확대/축소, 흰 구조물 설명, 차량 증가.

구현: FullMapOverlay 1~8배 확대, +/- 버튼/키/휠, 드래그, 내 위치/전체 보기. 지도 뷰포트 클리핑과 동일 변환으로 도로/경로/시설 표시. DistrictTraffic 36→48대, 생성 시도144회. 기존 차로/신호 판단 유지.

문서: prototype-playtest-guide.md에 잔여 개발/직접 플레이8단계/문제 기록 양식. 체크리스트의 오래된 P4/P5/PS 미구현 표기 수정. 최초 선택 노출의 재실행 저장은 여전히 남음. content-design/01·06 갱신.

흰 구조물: WorldBuilder의 단색 건물 메시와 부합하지만 이미지 단독으로 개별 ID는 미확정. 정체는 RoadConditions의 창문/비상등 차량이다. 건물 삭제나 지도 입력 변경 없음.

검증 결과는 아래에 기록한다. 실제 키보드 수동 3일 밸런스는 미검증이며 별도 플레이 목록을 따른다. 기존 README/라이선스 변경은 보존한다.

검증: PrototypeBuild.BuildHybrid 종료0(artifacts/map-zoom-build.log). -playabilityUITest 종료0/success=True:1280·1600·1920 확대/축소 상하한·일시정지복구·필터 검사와 실제 확대 화면 캡처 확인. -districtTrafficTest 종료0:48대 모두 이동,45,279m 합계,차로이탈0/겹침0/장시간정체0/연속운행6회. 적색3대대기→녹색3대통과. 자동 교통 흐름 검사이며 수동48대 캠페인 완료 아님. git diff --check 통과.

배포 소스: 12def92bcff11a13a25e44d44c42c099b68cfe97. 실행 폴더 125개 파일 크기/SHA256 전수 검증. 지도 입력3개·지도ZIP 변경 없음.
