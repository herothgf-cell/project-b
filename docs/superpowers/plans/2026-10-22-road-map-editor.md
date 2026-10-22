# 도로·교차로 편집기 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. 이 문서는 구현 전 검토본이며 제품 기능 완료를 뜻하지 않는다.

**Goal:** 은계 실행파일에서 사용자가 차로·노면·표지·차량/보행 신호를 수정하고, 저장·복원 및 영업 기록과 분리된 시험 주행을 수행하게 한다.

**Architecture:** 기존 기준 지도와 ReviewedLaneLayout 보정 뒤에 안정적 ID 기반 사용자 수정본을 적용하고, 노면·충돌체·신호·공유 경로를 같은 리비전에서 생성한다. 편집은 정지된 영업 창의 초안에서 진행하며 시험은 기존 PracticeSession과 같은 별도 실행 프로세스를 사용한다. 새 지도는 새 영업 세션부터 적용하고, 검증이나 생성 실패 시 기준 지도 전체로 복원한다.

**Tech Stack:** Unity 2022.3.62f2, C#, 기존 IMGUI/KoreanText, JsonUtility, SHA256, Windows Standalone.

**Spec:** `docs/content-design/12-road-map-editor.md`

## Global Constraints

- 기본 대상은 은계 `builds/EungyeTaxi/EungyeTaxi.exe`, 빌드는 `PrototypeBuild.BuildHybrid`다.
- 원본 NGII/OSM·필수 생성 입력은 덮어쓰지 않는다. 사용자 수정은 `Application.persistentDataPath` 아래 별도 JSON이다.
- 차로 순번 대신 기존 `HdEdge.id`와 신규 GUID를 참조 키로 쓴다. 런타임 정수 인덱스는 최종 구성 시 변환한다.
- 유턴은 관찰 정보만 저장하고 주행 가능 선택은 비활성화한다.
- 오류 초안은 저장 가능하나 시험 적용·기본 사용 지정은 막는다.
- 실제 확인/추정/게임 창작과 외형/규칙 확인 상태를 나눈다. 거리뷰 조회·현장 조사 재개는 이번 구현에 포함하지 않는다.
- 시험 세션은 영업 저장·점수·연료·시간을 변경하지 않는다.
- 기존 README·라이선스·기획 문서의 사용자 변경을 보존하고 관련 문서는 추가분만 편집한다.

## Review Focus

1. 기존 T1 짧은 전용 차로를 불러오고 다시 적용할 때 중복 차로가 생기지 않아야 한다.
2. 차로 삭제는 신호·표지·정지선·승하차 참조까지 영향 목록에 표시하며 취소 한 번으로 함께 복원되어야 한다.
3. 손상 파일·쓰기 실패·기준 해시 불일치에서 마지막 정상본과 영업 저장을 보존해야 한다.
4. 화면 점등·NPC·직접 운전 법규 판정이 같은 신호 그룹/리비전을 사용해야 한다.
5. 시험 창의 비정상 종료·반복 실행 및 편집 UI의 Esc 입력으로 원래 영업 정지 상태가 깨지면 안 된다.

## 파일 구성과 인터페이스

아래 새 파일은 `unity/YeongtongTaxi/Assets/Scripts/`에 둔다. 테스트는 `Assets/Editor/`와 실행파일용 Probe로 나눈다.

| 파일 | 책임·외부 인터페이스 |
|---|---|
| `RoadMapDocument.cs` | 스키마1 수정본, 근거, 차로/연결/시설/신호 DTO. 지도ID·기준해시·수정본ID·리비전·UTC 저장일 포함 |
| `RoadMapStore.cs` | `Load(string path)`, `Save(string path, RoadMapDocument document)`, `SetDefault(string path)`; 임시파일 재파싱 후 원자 교체/이전 정상본 보존 |
| `RoadMapComposer.cs` | `Compose(MapData baseline, RoadMapDocument document)`; 원본을 수정하지 않고 완성된 후보 지도 반환, 안정적 ID를 최종 인덱스로 변환 |
| `RoadMapValidator.cs` | `Validate(MapData baseline, RoadMapDocument document)`; 대상ID·위치·수정법을 가진 오류/경고 목록 반환 |
| `RoadMapEditHistory.cs` | 문서 전체 트랜잭션의 `Commit`, `Undo`, `Redo`; 삭제/연결 변경을 원자적으로 기록 |
| `RoadMapEditor.cs` | 열기/닫기·선택·미저장 상태·초안·검사·저장·복원, 다른 모달/영업 입력 차단 |
| `RoadMapEditorCanvas.cs` | 평면 확대/이동/클릭·제어점/연결 드래그·원본 겹침·3D 미리보기 |
| `RoadMapEditorProperties.cs` | 차로·노면·표지·신호와 근거 속성 패널 |
| `RoadMapFacilities.cs` | 사용자 노면/표지/신호 외형 생성과 정지선·횡단 충돌영역 구성 |
| `RoadMapSignalProgram.cs` | 진행/보행 그룹·단계·황색/정리 시간 및 공통 신호 상태 조회 |
| `RoadMapTrialSession.cs` | 별도 시험 프로세스와 임시 수정본·출발/도착·NPC 통과 요청 관리 |
| `RoadMapEditorProbe.cs` | 실행파일 UI·저장 왕복·시험 격리·리비전 검증과 결과 출력 |

기존 변경 대상: `MapCatalog.cs`, `MapData.cs`, `DriveSession.cs`, `DriveHUD.cs`, `WorldBuilder.cs`, `RoadEnvironment.cs`, `RoadNavigation.cs`, `HdRoadNetwork.cs`, `AssistedDriveController.cs`, `TrafficSystem.cs`, `PedestrianSignalDisplay.cs`, `RoadSignage.cs`, `DrivingLaw.cs`, `BusinessProgress.cs`, `DrivingScoreJournal.cs`, `PrototypeBuild.cs`. 필요하지 않은 소비자 수정은 하지 않고 공통 지도 입력 변경으로 해결한다.

## Task 1: 기준 지도·수정본·저장·트랜잭션

- [ ] `RoadMapDocumentTests.cs` 작성: JSON 왕복에서 ID/근거/연결 일치, 스키마 불일치 차단, T1 가져오기 두 번 후 차로 수 동일, 원본 직렬화 불변 확인. 구현 전 실패를 확인한다.
- [ ] 기준 지도 구성은 MapCatalog의 기존 보조자료→검토차로까지 완료한 결과로 고정한다. 기준 해시는 원본 리소스와 적용 설정 바이트를 파일명 순서·길이를 포함해 SHA256으로 계산한다. 노면 보정 전 사용자 수정을 적용한다.
- [ ] DTO는 차로 중심선/높이 보정·폭·번호·허용 이동·차로 변경 구간·안정적 연결ID·시설 참조·신호 그룹·근거를 명시적으로 저장한다. 기준에 존재하지 않는 ID를 묵시적으로 무시하지 않는다.
- [ ] `RoadMapStoreTests.cs` 작성/실행: 손상 JSON, 중복ID, 잠긴 파일/쓰기 실패, 해시 불일치에서 정상본 바이트 유지 및 자동 적용 금지 확인.
- [ ] Save는 임시파일 작성→재파싱→이전본 보존→교체 순서로 구현한다. 초안 저장과 기본 지도 지정의 검증 조건을 분리한다. 가져오기/내보내기는 파일 경로 입력과 폴더 열기를 제공한다.
- [ ] `RoadMapEditHistoryTests.cs`: 차로 삭제와 종속 참조 변경 후 Undo에서 직렬화 동일, Redo 일치, 새 편집 후 redo 비움 확인.
- [ ] Unity batch 실행용 `RoadMapEditorTests.Run`에 위 테스트를 묶고 실패 예외가 프로세스 실패로 이어지게 한다. 실제 실행 명령/결과를 이력에 기록한다.

## Task 2: E1 편집 화면과 차로·공유 경로

- [ ] `RoadMapCompositionTests.cs`부터 작성: T1 좌회전 전용 변경 뒤 직진 경로에 금지 연결 없음, 출구 없는 차로/반대 연결/잘못된 폭/자기 교차/끊김 차단, 필수 승하차와 주유·휴식 경로 유지 확인.
- [ ] composer에서 기존/추가 차로를 동일한 중간 ID 그래프로 정규화한다. 삭제·형상·폭·대기차로·연결 변경 후 정수 인덱스와 서비스 접근을 한 번만 재구성한다. 검증은 렌더·신호 구성까지 완료된 후보 전체를 대상으로 한다.
- [ ] Esc 메뉴에 ‘지도 편집’을 추가한다. T1 초기 포커스, 교차로ID 검색, 접근/차로 목록, 지도 클릭 선택, 제어점 이동·차로 추가/삭제·폭·대기차로 길이·방향/출구/변경 구간 편집을 구현한다.
- [ ] 유턴 입력은 관찰 메모만 허용한다. 삭제 전 영향 목록과 대상 재지정/제거를 제시하며 단일 취소 작업으로 처리한다.
- [ ] 상단 도구/취소·다시 실행/검사/시험/저장, 왼쪽 목록·확인 상태, 오른쪽 속성/근거, 하단 오류/리비전을 배치한다. 1280×720에서도 속성 스크롤과 지도 조작 영역을 확보한다.
- [ ] 원본 겹침과 전용 미리보기 카메라의 3D 확인을 제공한다. 닫을 때 미저장 초안 유지/버리기를 선택하게 하고 진입 전 정지 상태와 timeScale을 복원한다.
- [ ] RoadNavigation은 변경 그래프에서 추천 차로·허용 화살표·도달 불가 사유를 표시한다. 내비/H/NPC에 동일 리비전을 기록하고 기존 경로/가까운 차로 캐시를 새 세션에서 다시 생성한다.
- [ ] 실행 Probe로 UI 열기→편집→취소/다시 실행→저장→새 프로세스 불러오기, Esc/다른 모달 입력 충돌, 내비/H/NPC 리비전 일치를 검사한다.

## Task 3: E2 노면·표지·규칙 연결

- [ ] `RoadMapFacilityTests.cs` 작성: 정지선/횡단보도 이동 후 외형과 판정 영역 일치, 참조 없는 규칙표지 차단, 같은 구간의 상충 제한속도 차단, 안내용 미지원 표지의 규칙 적용 거부 확인.
- [ ] 정지선·횡단보도·중앙선·차로선·화살표의 위치/폭/각도를 편집한다. 화살표 허용 방향은 공유 차로 데이터에서 생성한다.
- [ ] 기획서의 표지 종류와 위치·높이·회전·크기·문구 및 적용 차로/구간을 제공한다. 제한속도는 시작/끝 범위를 필수로 하고 경로 ETA·법규 판정과 같은 값을 소비한다.
- [ ] 지원 가능한 회전/진입 제한은 그래프 이동을 변경한다. 추가 구현 없이 기능을 보장할 수 없는 표지는 ‘안내용’만 선택 가능하게 하고 이유를 표시한다.
- [ ] 근거 상태·거리뷰 URL·촬영월·확인일·메모를 저장한다. 현장 미확인 경고는 초안/시험의 게임 설정임을 유지한다.
- [ ] T1 변경 전후 외형/판정 및 기존 원본 표지와 중복 생성 여부를 실행파일에서 확인한다.

## Task 4: E3 차량·보행 신호 그룹과 충돌 검사

- [ ] `RoadMapSignalTests.cs` 작성: 차량 교차 진행 동시 허용·보행과 차량 충돌·없는 그룹·0/음수 시간 차단, 황색→정리→다음 단계 순서, 신호기 이동 후 정지선/대상 불변 확인.
- [ ] 기존 신호를 ID가 있는 신호기/그룹/단계로 가져오고 기본 동작이 동일한지 회귀 검사한다. 미편집 교차로는 기존 방식과 동등하게 유지한다.
- [ ] 차량 신호기는 이동 연결/정지선/그룹, 보행 신호기는 횡단보도/방향/그룹에 연결한다. 미연결 외형은 경고 색상과 적용 차단 사유를 표시한다.
- [ ] 그룹별 진행 허용·보행 허용·단계 지속/황색/정리 시간을 편집한다. 연결 곡선·차량 폭/보행 영역으로 충돌 행렬을 생성하고 상충 단계는 시험/기본 적용을 차단한다.
- [ ] TrafficSystem, PedestrianSignalDisplay, NPC 진입, DrivingLaw/우회전 관찰이 같은 RoadMapSignalProgram 상태를 조회하게 한다. 임의 그룹 제어를 외형 이동과 분리해 검증한다.
- [ ] 비보호·양보는 기존 동작과 연동되고 충돌/양보 검증이 있는 조합만 활성화한다. 나머지는 미지원 오류로 막는다.
- [ ] 실행파일에서 적색 정지/녹색 출발·보행 녹색 횡단과 차량 정지·정지선 이동·점등 일치를 검증한다. 위치 제어 검사는 물리 통과와 구분한다.

## Task 5: 시험 격리·E4 확장·복원

- [ ] `RoadMapTrialTests.cs` 작성: 시험 반복/비정상 종료 뒤 부모 시간·계정·법규저널·연료·점수·저장파일 바이트 불변 확인. 저장 실패·구성 실패 때 기준 지도 전체 복원 확인.
- [ ] 시험 실행 인자는 `-roadMapTrial -roadMapInput <임시파일> -roadMapTrialRequest <출발/도착 JSON>`으로 고정한다. 하위 프로세스는 캠페인/법규/PlayerPrefs 저장을 호출하지 않고 독립 메모리 상태로 실행한다. 경로 프리뷰·NPC 통과·직접 운전을 제공한다.
- [ ] 부모 영업은 수정 전 지도와 상태로 정지한다. 시험 종료 시 초안을 보존해 편집으로 돌아오며, 변경 지도를 현재 영업 중간에 갈아끼우지 않는다.
- [ ] 기본 지정은 정상 검증본만 허용한다. 다음 새 세션에 적용하고 이름/리비전을 표시한다. 입력 해시가 다르면 적용하지 않고 충돌 대상·기준 지도 시작 선택을 제공한다.
- [ ] 마지막 정상 적용본/교차로/전체 기본 지도 복원, T2/T3와 일반 교차로 선택, 동일 기준 해시의 내보내기/가져오기 왕복을 검증한다.
- [ ] `-roadMapEditorTest` Probe로 재실행·손상파일·시험 격리 결과를 `artifacts/road-map-editor-test.txt`에 쓰고 PASS/실패 종료코드를 반환한다.

## Task 6: 사용법·회귀·기본 빌드와 인계

- [ ] `docs/road-map-editor-guide.md` 작성: 실행→메뉴→T1 선택→차로 방향/출구 수정→검사→저장→시험 주행→기본 지정→다음 세션 확인의 따라하기. 각 도구, 신호/표지 배치, 오류 해결, 저장 폴더·PC 이동·복원, 미지원 조합을 설명한다. 실제 구현 단축키와 화면을 기준으로 작성한다.
- [ ] `tools/check_workspace.ps1 -Strict`, `tools/test_prototype.ps1`, Unity의 `RoadMapEditorTests.Run`과 `PrototypeBuild.BuildHybrid`를 실행하고 명령·종료코드를 기록한다.
- [ ] 기본 은계 실행파일 Probe를 1280×720/1600×900/1920×1080에서 검사하고 화면 캡처를 확인한다. T1 자동 물리 좌/직/우·NPC 통과·48대 회귀를 검사한다.
- [ ] 동일 교차로·48대·30초로 편집 적용 전후 프레임과 지도 구성 시간을 측정한다. 사전 기준 대비 악화 여부와 실제 수치를 보고한다. 사람의 수동 편집/주행·전체 캠페인 검사는 수행 여부를 따로 기록한다.
- [ ] 배포 manifest 파일 크기·SHA256을 재생성하고 실제 실행 폴더 전체를 대조한다. 필수 생성 입력을 변경하지 않았음을 확인하고 변경 시에만 입력 manifest/OSM 대조/ZIP을 실제 자료로 갱신한다.
- [ ] 기획서 상태, 체크리스트 E1~E4, 범위 문서, 날짜별 이력과 LATEST를 실제 완료 범위로 갱신한다. 관련 코드/문서/배포물만 선별 커밋한다. 이력의 커밋은 커밋 후 `git log -1 --oneline -- <이력 파일>`로 확인한다.

## 계획 검토 결과

기획서 3~9절은 Task 1~5, 10~11절의 단계·검증은 Task 2~6, 사용 안내는 Task 6에 대응한다. E1만으로 요청 전체 완료를 표시하지 않으며 E2/E3 및 저장·시험 격리까지 필요하다. 현재 코드는 읽기만 했고 이 계획에 적힌 클래스·검사는 아직 만들거나 실행하지 않았다.

다음 단계는 계획 검토 후 실행 방식 선택이다. 공통 데이터/신호/경로 인터페이스가 밀접하게 연결되어 있어 이 세션에서 순차 구현하는 Native 방식을 권장한다.

최종 구현 상태: 제품 코드·실행파일·사용법 작성. 상세 계획 체크는 미실행 수동/전체 형상 검증까지 섞어 일괄 완료 처리하지 않는다. 실제 근거는 `docs/work-history/2026-10-22-road-map-editor-implementation.md`와 `docs/prototype-development-checklist.md`의 구현/검증 구분을 따른다.
