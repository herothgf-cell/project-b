# Unity 전체 지도 편집 통합

## 요구

기존 실행파일 Esc 지도 편집 기능을 Unity에서 직접 수행한다. 차로 클릭·일방통행·좌/직/우 화살표·표지·노면·신호, 건물/부지까지 한 화면에서 수정하고 실제 주행 및 다른 PC pull/빌드에 반영한다. 사용자 요청으로 구현·검증·main 푸시까지 수행한다.

## 구현

- ProjectMapSceneWindow + Tools: 건물/부지, 차로/표지/노면/신호, 검사/시험/자료의 같은 창. Scene 표시 문제 수정은 유지. 객체 생성/삭제·꼭짓점 증감/JSON 가져오기·내보내기/정식 저장/빌드/별도 시험.
- ProjectMapTrafficTools: Scene 차로/시설 선택·진행 꺾쇠·제어점·W/E/R, 도로명/진입 방향/차로 번호, 클릭 출구/시설 대상 연결, 차로 폐쇄/반전/일방통행/복제/분할/좌회전 대기차로. 단계/그룹/차량·보행 기본 신호, 적용 대상/시간표와 근거.
- RoadMapAuthoringOperations: 공유 방향/연결/형상·시설 연산. 일방통행은 같은 도로 반대 방향의 가까운 차로를 확인 후 폐쇄한다. 표지 외형과 실제 규칙은 구분한다.
- ProjectMapEditSession + TrafficHandle: 객체별 빠른 갱신 유지, 차로/시설/신호 트랜잭션 Undo/Redo·초안·정식 저장, 신규 객체 및 전체 JSON 가져오기 원자성.
- AuthoringRenderer는 실제 RoadMapFacilities 메시도 함께 표시한다. 교통 편집은 손잡이를 놓은 뒤 미리보기를 재구성하며 실패 시 이전 표시와 경고를 남긴다.
- UnityTrafficProbe 및 새 검사: 실지도 수정본의 저장/합성·실행파일 주행 규칙 적용 확인. 자동 위치/상태 검사이며 수동 운전으로 기록하지 않는다.

## 검증

실제 결과는 아래에 추가한다. RoadMapAuthoringOperationsTests RED 종료1(기능 없음) 후 GREEN 종료0. 세션 테스트 첫 실패는 저장 전 삭제한 신규 객체의 clean tombstone이 draft에서 생략되는 정상 동작을 잘못 가정한 마지막 assertion이었다. 신규 가져오기 객체가 보존되며 삭제된 옛 객체가 없거나 비활성인지 검사하도록 수정했다.

## 실행과 남은 범위

Unity2022.3.62f2 → unity/EungyeTaxi → Taxi/프로젝트 지도 3D 편집. 기존 창은 닫고 다시 연다. 차로/시설을 Scene에서 고르고 검사 후 프로젝트에 저장한다. config/maps/project/eungye.project.json을 커밋/푸시한 뒤 다른 PC는 pull/BuildHybrid. 실행폴더까지 배포하면 pull로 바로 게임 실행 가능.

사람의 Unity 마우스 편집/실제 다른 물리 PC/일반 차량 포함 전체 캠페인은 별도 검증이다. 실제 현장 교통 규제를 자동 확정하지 않는다. 고가/터널/자유 3D 모델 제작과 유턴 경로는 기존 Esc에서도 미지원 범위로 유지한다. 차로 분할/대기차로는 생성 ID의 원래 차로 정보와 sourceOffset으로 정차점·서비스·속도 구간·종점 신호를 재연결한다. 기존 직렬화 필드를 추가하지 않아 정식 지도 contentHash를 유지했다. 분할 계열의 방향 반전은 복원 후 수행하도록 명시적으로 거부한다. 새 대기차로 좌회전 신호는 사용자가 지정하며 부적합 지도는 정식 저장/시험에서 차단한다. 정식 지도 데이터 자체는 기능 구현 중 바꾸지 않는다.


## 검토 중 발견·해결

- 시설 근처에서 연결할 차로를 누르면 시설이 먼저 선택돼 출발 대상이 바뀌는 문제: 차로 대상 선택 모드에서는 시설 hit를 무시하도록 수정, 의도별 회귀 검사.
- 차로 분할 뒤 정차점/시설이 짧아진 원래 차로에 남는 문제: 영구 구간 ID + offset으로 복수 분할·정확한 경계점·서비스·속도 구간 재연결. 제한속도가 분할 경계를 넘으면 양쪽 구간에 규칙 유지.
- 마지막 명시적 신호 차로 삭제가 빈 laneIds(전체 적용)로 바뀌던 문제: 해당 명시 그룹/단계/신호기를 제거. 원래 전체 적용이던 그룹은 유지. 마지막 횡단보도 그룹이 없어지면 빈 프로그램도 제거.
- JSON 가져오기/초안 복구의 중간 실패: 원자적 Undo 트랜잭션과 상태 rollback. 저장 실패 시 세션을 버리지 않는다. 복원은 마지막 성공 저장본을 사용.
- 미리보기 실패: 기존 정상 형상을 유지하고 stale 경고 표시. 전체 지도 import는 증분 재빌드를 수만 번 호출하지 않고 한 번 갱신.
- 반복 검사 fixture 초안 충돌: 테스트마다 고유 폴더를 사용한다. 실제 사용자의 초안을 삭제해 우회하지 않는다.
- 새 빌드 뒤 시험 지도 token 불일치 예방: 빌드마다 실행폴더 project-map-build.json에 실제 baseline/content 해시를 기록하고 Unity 시험이 그 token을 사용한다. 배포 manifest 갱신 여부와 독립적으로 동작한다.


## 통합 검사 실측

- NativeMapEditorChecks.Build 종료0 (`artifacts/native-full-final.log`). 새 operations/traffic session/tools/unified 검사와 기존 edit session/window/renderer/fragment/store/easy/editor/extended/scene 검사를 한 프로세스에서 수행 후 BuildHybrid 성공.
- SceneWindow100회 증분 갱신 평균0.338ms/p95 0.279ms/최대12.467ms. 전체 생성1회 유지. CPU 갱신 표본이며 실제 GUI 프레임 속도/최초 로드 속도와 구분한다.
- tools/test_prototype.ps1 기존 모델23묶음 종료0. check_workspace -Strict -RequireRawInputs 종료0, 필수 입력2개 해시 일치. 정식 지도 SHA256 1b8ab6b86d435935d74cac8c83249daee41eb61a3d89d09fccfca3004c706714 유지.
- 마지막 실제 도로 보완 검토에서 자동 repair 분할이 명시적 제한속도/차로별 신호의 거리/ID를 바꾸는 문제를 별도 RED exit1로 재현했다. 명시적 규칙/차로 신호가 있는 구간은 자동 분할하지 않아 수동 저작 규칙을 우선하도록 보완했다. 이후 별도 회귀/은계 기본/편집-실행 그래프 일치/최종 빌드를 수행한다.

- 자동 repair 우선순위 회귀 GREEN/은계 기본/NativeParity/Scene 카메라 캡처 및 최종 BuildHybrid 모두 종료0 (`artifacts/native-repair-green-build.log`). 지도/원본 입력 변경 없음.
- 실제 GPU 미리보기 PNG `artifacts/unity-scene-editor/game-camera-preview.png`를 열어 도로/주변 건물/서비스 정차 칸이 함께 렌더되는 것을 확인했다. 사람의 Unity GUI 조작으로 기록하지 않는다.

- 최종 exe 자동 위치/상태 검사: -unityTrafficTest 종료0 success=True/6검사(실제 공유 그래프 좌회전 차단·제한속도17·표지/노면 메시·영업격리). -projectMapTest 종료0/23 PASS success=True. 실제 별도 프로세스 -liveSaveTest9/-liveReloadTest23 모두 종료0. 정식 지도/영업 저장을 사용하는 수동 운전은 하지 않았다.
- 소스 a37e1db를 만들고 기존 main의 프로토 잔여 점검 문서5f2e636과 원격 마인드맵 변경을 보존 통합한다. 소스/최종 배포 실제 해시는 git log와 manifest를 기준으로 한다.


## 배포 자료 확정

실제 소스421556431810279083eaceb16b074cf3d5c71dd9 기준 최종 실행폴더130파일/118,418,816바이트. manifest 생성·VerifyOnly 종료0. 기본 폴더에서 다른 채팅의 후속 런타임 구현이 진행 중임을 발견해 사용자 변경을 stash/덮어쓰기하지 않고 메시지로 통합 순서를 조율했다. 이번에는 Unity 프로젝트 경로 이동이 없다. 별도 작업 코드와 빌드 파일은 이 커밋에 섞지 않는다.

- 분리 작업 폴더에서 git push origin HEAD:main 종료0. git ls-remote 원격 main 6b329c491d5da0ca3ff9e9a3ce9b6f28d6779a8f 일치 확인. 다른 PC는 현재 검증본을 pull해 편집/빌드할 수 있다. 기본 폴더는 동시 작업 보호를 위해 수정하지 않았으며 해당 채팅에 푸시 해시와 통합 주의사항을 전달했다.

- 출구가 없는 회전 방향을 켜면 조용히 무시하는 대신 도착 차로 클릭 연결 모드로 전환하도록 개선했다. ProjectMapTrafficToolsTests 최종 재실행 종료0(native-tools-final.log). Editor 전용 변경이며 runtime 재빌드는 불필요하다.

- 신규 건물/도로가 다시 열기 후 원본 접두사가 없어 선택 ID를 잃는 문제를 추가 RED1로 재현하고, 렌더러가 정식 객체 ID를 보존하도록 수정했다. NewObjectIdentity GREEN0. 기존 생성 면과 가져온 안정 ID 모두 같은 방식으로 선택한다.
