# 은계 실행파일 도로·교차로 편집기

## 요구사항과 구현

사용자가 기획12를 기준으로 편집기와 사용법 전체 구현을 승인했다. `codex/road-map-editor` 브랜치에서 기존 사용자 기획·라이선스 변경을 보존하며 작업했다.

- Esc 메뉴의 지도 편집, 교차로/차로 검색·선택, 평면 확대/이동·제어점 이동/추가/삭제·분할, 폭/높이/번호, 허용 방향·출구/차로변경 연결, 평행 차로·짧은 좌회전 대기차로 생성.
- 원본 겹침·3D 확인, 삭제 영향과 종속 참조 제거, 최근30개 문서 트랜잭션 취소/다시 실행, 교차로/전체 초안 복원.
- 노면·표지·차량/보행 신호의 위치·회전·높이·문구/크기·근거. 좌/우회전 금지·진입 금지와 구간 제한속도는 실제 게임 규칙에 연결. 다른 표지는 안내용으로 제한한다.
- 신호 그룹·진입 차로ID·단계 시간·횡단보도 연결. 보수적인 상충 검사, 그룹별 표시·NPC 진입·직접 운전/좌회전 판정, 보행 신호와 횡단 보행자.
- 안정적 ID·기준 지도 해시·수정본 리비전의 별도 JSON. 임시파일 검증 후 교체와 백업, 초안 저장과 기본 적용 분리, 다음 실행 적용, 불일치/손상 시 기준 지도 복원.
- 별도 실행 프로세스의 지도 시험. 부모 영업 정지 상태 유지, 시험 계정/운행저널/PlayerPrefs 저장 차단, 출발/도착·내비/H·일반 교통 확인.
- [사용 안내](../road-map-editor-guide.md). 도구별 조작, 신호/표지 연결, 적용·전달·복원과 제한 사항 포함.

기존 T1 보정 이후 사용자 수정을 합성하고 노면/신호/공유 경로를 구성한다. 원본 입력3개를 바꾸지 않는다. 런타임 메타/높이는 원본 지도 JSON 직렬화에 포함하지 않아 기존 OSM 기준 해시를 유지한다. 수정본의 높이는 별도 배열로 저장하고 합성 시 도로 메시/경로에 반영한다.

## 변경 파일

새 `Assets/Scripts/RoadMap*.cs`는 문서·저장·합성·검사·편집/캔버스/속성·신호·표지·보행·시험·Probe로 나눈다. 새 `Assets/Editor/RoadMapEditorTests.cs`, `RoadMapExtendedTests.cs`를 추가했다. `MapCatalog`, `MapData`, `ReviewedLaneLayout`, `WorldBuilder`, `DriveSession`, `DriveHUD`, `RoadNavigation`, `TrafficSystem`, `DistrictTraffic`, `TrafficCar`, `DrivingLaw`, `DrivingManners`, `BusinessProgress`, `DrivingScoreJournal`, `MitTrafficOptions`, `ServiceLawZones`, `PrototypeBuild`를 연결했다. 관련 기획·체크리스트·범위·인계를 갱신한다.

## 검증 기록

- `pwsh -File tools/check_workspace.ps1 -Strict`: 종료0, 입력3개 모두 일치, Input issues0.
- `pwsh -File tools/test_prototype.ps1`: 기존 모델22묶음 종료0.
- Unity `RoadMapEditorTests.Run`: 미구현 클래스의 실패를 `artifacts/road-map-editor/red.log`에서 확인한 뒤15개 검사 통과. 저장 왕복·안정적 ID·원본 보존·T1 중복 없음·취소/재실행·스키마/해시/폭/참조 오류·시간표·손상 파일 검사.
- `RoadMapExtendedTests.Run`: 정상 원본의 유효성, 속도 구간/공유값, 중복ID, 차량/보행 충돌, 파일 잠금 시 기존 파일 보존, 보행 그룹 참조·차로별 신호 분리·손상 시설·높이 보존 검사.
- `PrototypeBuild.BuildRoadMapEditor`는 위 검사 후 기존 `BuildHybrid` 회귀와 기본 은계 빌드를 수행한다. `build4.log`에서 종료0과 `PROTOTYPE_BUILD_OK` 확인. 화면/높이 후속 수정이 있어 최종 결과는 아래 추가 검증란으로 확정한다.
- 실행파일 `-eungye -oneDayMode -roadMapEditorTest`: 16개 편집/저장/취소·신호 가져오기·부모 영업 시간/잔액/연료/저장파일 보존 검사 통과, 종료0. 별도 자식 `trial-child-test.txt` success=True, 리비전 일치. 세 해상도(1280×720/1600×900/1920×1080) 캡처 수행. `player-final.log`의 예외 검색 결과 없음.
- 독립 읽기 검토의 신호 없는 교차로 적용, 차로별/좌회전 판정, 보행 대상 연결, 숫자 캐시, 과속 판정 지적을 수정했다. 높이 배열 추가/삭제/분할도 함께 수정했다.

로컬 환경 Unity: `D:/Program Files/Unity Hub/Editor/2022.3.62f2/Editor/Unity.exe`. PC 고유 경로이며 프로젝트 의존 경로로 저장하지 않는다. 로그/캡처는 Git 제외 `artifacts/road-map-editor/`에 보관한다.

## 구현상의 결정과 한계

- 신호 가져오기는 접근 방향별 분리 단계로 변환한다. 기존 비보호 주기 복제가 아니며 실제 운영 확정이 아니다. 다른 축 동시 진행, 보행/차량 동시 진행과 마주 오는 회전 조합은 보수적으로 차단한다.
- 지원 표지의 규칙만 활성화한다. 임의 비보호/양보 혼합, 유턴 주행, 고가/터널/건물/지형 조각은 지원하지 않는다.
- 기존 원본의 중복점/지도 경계 밖 점은 보존하고 새 형상 변경에 검사를 적용한다. 현장 조사 중단 지시는 유지했다.
- 자동 자료·위치 제어·프로세스/UI 검사는 사람의 수동 편집/하루 캠페인 또는 택시의 자동 물리 주행이 아니다. 신규 형상에서 모든 회전반경·충돌·전체 교차로의 차량/보행 조합을 검증한 것으로 표시하지 않는다.
- 원본 기준 해시가 다른 수정본의 자동 변환은 제공하지 않는다. 오류를 표시하고 동일 기준 빌드에서 다시 편집하도록 한다.

## 실행·재현

`builds/EungyeTaxi/EungyeTaxi.exe` 실행 → 출발 → Esc → 지도 편집. 세부 절차는 사용 안내 참조.

자동 UI 검사는 저장소 루트에서 기본 exe에 `-eungye -oneDayMode -roadMapEditorTest -screen-fullscreen 0 -logFile <절대경로>`를 전달한다. 결과는 `artifacts/road-map-editor/editor-player-test.txt`와 `trial-child-test.txt`다. Unity 배치는 `-batchmode -nographics -quit -projectPath <저장소>/unity/EungyeTaxi -executeMethod PrototypeBuild.BuildRoadMapEditor -logFile <절대경로>`다.

## 최종 추가 검증·인계

최종 빌드·화면·추가 실행 결과와 배포 manifest 검증을 이 절에 확정한다. 현재 기본 실행은 은계이며 필수 입력3개·지도 ZIP은 변경하지 않았다. 실제 이력 커밋은 커밋 뒤 `git log -1 --oneline -- docs/work-history/2026-10-22-road-map-editor-implementation.md`로 조회한다.

- 최종 `build7.log`: Unity 종료0, 코어15항목/추가검사 통과 후 BuildHybrid 전체 회귀와 `PROTOTYPE_BUILD_OK`.
- 48대·T2·30초 렌더: 기본 평균59.97FPS/p99 16.91ms → 최종 수정 지도59.93FPS/p99 17.03ms. 수정본은 T1 폭3.2m와 가져온 차량/보행 프로그램을 포함한다. `perf-base.txt`, `perf-final.txt` 참조. 전 맵/다른 PC 성능 보장은 아니다.
- 중간 성능 검사0.43FPS의 원인은 NPC당 전 차로 검색이었다. 속도 규칙이 없으면 검색하지 않고, 속도/신호 판정에 현재 NPC 경로 차로를 사용하며, 높이 차로에만 충돌체를 만든 뒤 같은 조건으로 재측정했다.
- 원본 파생 지도 SHA256은 `01151c6e914b9311f68e024e81e5f77fb5acc73a83d7c1a691535d658e43ad96`로 OSM 설정과 일치한다. 필수 입력3개 Strict 오류0/지도 ZIP 변경 없음.
- 최종 실행파일 추가 검사: `-reviewedLaneTrafficTest -roadMapInput <수정본>` 종료0. T1 NPC2대 각각 경로1회 완료, 최소 차간 거리11.99949m. 공유 그래프 일치·실행 충돌 표본762개 건물 침범0. **Kinematic NPC 검사이며 택시 물리 주행/신호 시간표 검사는 아니다.**
- 최종 `player7.log`: `-roadMapEditorTest -oneDayMode` 종료0, 편집/저장/프로세스 격리16항목 success=True, 자식 리비전 일치. 세 해상도 캡처 확인, 예외 검색0. 고정 지도 텍스처로 영역 밖 선 그리기 문제를 해결했다.
- 신규 `tools/update_distribution_manifest.ps1`로 실제 소스 커밋과 파일 크기·SHA256을 생성하고 `-VerifyOnly`로 실행 폴더 전체 대조한다. 최종 배포 커밋에서 manifest를 함께 보관한다.
- 최종 배포: 소스 커밋 0cb57ea158345b0aa6673fe1b39aff0635205748, manifest 실제 127파일/84685847바이트 SHA256·크기 재검증 종료0. 필수 입력 Strict 재검사 종료0. sourceCommit은 실제 소스 커밋을 기록했다.
