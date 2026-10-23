# Unity 실행 화면 기반 3D 지도 편집

## 요구사항과 구현

기존 프로젝트 지도와 원본 입력을 유지하면서 Unity에서 실제 게임과 같은 형상을 선택·이동·회전·크기 조절하고, 빠르게 편집한 내용을 Git pull→빌드로 재현하도록 요청받았다.

- `Taxi → 프로젝트 지도 3D 편집`: 독립 Preview Scene와 전용 SceneView, 클릭/이름/종류 선택, W/E/R/F, 수평 이동·회전·X/Z 크기·건물 높이·꼭짓점.
- 주차장/식당/휴식의 정차점·방향을 부지와 따로 편집하고 접근 차로는 Scene 클릭으로 지정한다.
- 게임 WorldBuilder의 메시/재질/시설 생성 코드를 공유한다. 기본 게임 카메라 62도/거리700m, 운전자 미리보기82도. NPC/차량/HUD/영업 시간 진행은 포함하지 않는다.
- 전체 장면은 최초 한 번 생성하고 선택 객체와 이전/새 128m 구역만 갱신한다. 드래그에 전체 JSON 복사/지도 합성/파일 저장을 넣지 않는다. Undo는 편집한 객체의 ScriptableObject만 기록한다.
- 정식 저장은 기존 ProjectMapStore/Package 검증·해시·잠금·백업·충돌 방지를 사용한다. 정식 파일은 `config/maps/project/eungye.project.json`이며 이번 도구 구현 자체는 기존 지도를 변경하지 않았다.
- 희소 초안은 저장소 루트 `Library/ProjectMapEditor/`에 저장하고 Git 제외. 자동 복구, Undo-to-clean 제거, 미복구 초안 덮어쓰기 차단, 기준 충돌 초안 별도 보관을 지원한다.
- 미저장 사용자 씬을 덮어쓰지 않는다. Unity2022.3의 내부 `SceneView.customScene`은 한 곳의 버전 어댑터로 접근하며 지원하지 않는 버전은 명시적으로 실패한다. 전용 창을 닫으면 다시 열 수 있다.

## 변경 파일

- `unity/YeongtongTaxi/Assets/Editor/ProjectMapEditSession.cs`, `ProjectMapEditObjectHandle.cs`, 해당 테스트: 객체 단위 편집/Undo/정식 저장/초안.
- `ProjectMapAuthoringRenderer.cs`, 해당 테스트: 공유 형상/재질, 구역별 갱신, 삼각형 선택 판정, 리소스 정리.
- `ProjectMapSceneWindow.cs`, 해당 테스트: 독립 Scene/도구/게임 카메라/패널/수명주기.
- `Assets/Scripts/WorldBuilder.cs`: 기존 생성식을 공통 함수로 추출하고 편집 모드에서 콜라이더 제거를 안전하게 처리.
- `Assets/Scripts/ProjectMapScene.cs`: 원본 수입 링의 신뢰된 삼각분할을 방향을 보존하는 유효한 아핀 변환에 한해 재사용. 미리보기와 정식 저장의 형상 허용 범위를 일치시킴.
- `.gitignore`, 사용법/기획12/범위/체크리스트/작업 이력. 배포 실행 폴더와 manifest는 별도 배포 커밋에서 기록.

## 검증

로그 기준 폴더: `artifacts/unity-scene-editor/`. Unity2022.3.62f2 배치 실행의 실제 종료 결과를 확인했다.

| 검증 | 실제 결과 |
|---|---|
| Session/Renderer/Window 초기 RED | 구현 타입 없음으로 각각 exit1 확인 후 구현 |
| `ProjectMapEditSessionTests.Run` | exit0: 수치 변환, Undo/Redo, 음수 배율·잘못된 꼭짓점 거부, 알림 실패 롤백, 정식 저장/기준 보존, 외부 충돌, 초안 수명·소유권·Undo-clean |
| `ProjectMapAuthoringRendererTests.Run` | exit0: runtime 메시/삼각형/색 동등, 오목 면 정확한 선택, 콜라이더 없음, 무관 구역 메시 유지, 구역 경계 이동/비활성화 |
| `ProjectMapAuthoringRendererTests.ClippedRingRoundTrip` | 기존 수입 링 변환 저장 오류를 RED로 재현 후 exit0. 회전/비균일 배율 허용, 반사/붕괴/비아핀 오류 거부, Store 저장/읽기/Compose |
| `ProjectMapSceneWindowTests.Run` | exit0: 선택→수정→저장→재읽기, 실제 정식 원본 바이트 보존, SceneView 버전 어댑터, 기존 씬 개수/dirty 상태 보존 |
| 실제 전체 지도 100회 편집 | 전용 SceneView 포함 최종 측정 평균 **0.204ms**, p95 **0.345ms**, 최대 **0.454ms**. 전체 생성 횟수1 유지. CPU 객체/구역 갱신 표본이며 GPU/전체 FPS/사람 드래그 수치는 아님 |
| `ProjectMapSceneWindowTests.RenderVisual` | exit0, 실제 GPU 카메라 RenderTexture PNG 생성/확인. 식당 부지·노란 칸·건물/노면/그림자 표시 |
| `ProjectMapBuild.RunTests` | exit0: 기존 프로젝트 장면/편집/Store16·간편7·도로15·확장 검사 회귀 통과 |
| `PrototypeBuild.BuildHybrid` | 원본 drive/eungye/route42/hybrid JSON4개와 meta를 임시 분리한 상태에서 exit0. finally에서8파일 복원 |
| `tools/check_workspace.ps1 -Strict` | exit0, 필수 입력3개 해시 일치, 이슈0 |

초기 additive scene 방식은 이름 없는 미저장 씬에서 열 수 없어 Preview Scene로 수정했다. 숨김 batch SceneView의 Close가 호스트 부재로 실패하여 batch에서는 DestroyImmediate로 정리하고 재검증했다. 독립 코드 리뷰에서 지적한 회전 누적, 중심점 불일치, 초안 덮어쓰기/되살아남, 잘못된 꼭짓점의 숨은 변경, 수입 링 저장 불일치, 정차점 독립 편집, Scene 재열기와 충돌 초안 복구 경로를 수정했다. 최종 리뷰에서 추가 중대 항목은 없었다.

## 실행·재현

[사용법](../unity-map-editor-guide.md). Unity에서 `Taxi → 프로젝트 지도 3D 편집`을 연다. 정식 저장한 JSON을 커밋·푸시하고 다른 PC에서 pull한 뒤 `Taxi → Build Eungye Hybrid`를 실행한다. 기본 exe는 `builds/EungyeTaxi/EungyeTaxi.exe`다.

검사 메서드는 위 표와 같이 `-batchmode -quit -projectPath <Unity프로젝트> -executeMethod <메서드> -logFile <로그>`로 실행한다. 화면 렌더 검사는 `-nographics`를 사용하지 않는다. 로컬 Unity 설치 경로는 `D:/Program Files/Unity Hub/Editor/2022.3.62f2/Editor/Unity.exe`이며 PC별 환경 정보다.

## 미검증·범위 제한

사람이 실제 보이는 Unity 창에서 마우스/단축키로 연속 조작하고 dock/닫기/재열기를 확인하는 과정, 실제 다른 물리 PC, 수동 전체 캠페인 운전은 미검증이다. 자동 API/상태/위치 제어와 사람이 운전한 결과를 구분한다. 노면 이동은 주행 차로 그래프를 자동 이동하지 않는다. 객체 추가/삭제·주행 연결은 기존 실행파일 편집기를 사용한다. 원본 입력/지도 ZIP/기존 정식 지도 값은 보존했다.

## 배포 상태

빌드 및 새 exe 회귀 검사, 실제 소스 커밋·manifest·main 푸시 결과는 검증 완료 후 아래에 추가한다.

- 새 은계 exe의 -eungye -hybridV2 -oneDayMode -projectMapTest 종료0: 23항목 PASS, success=True. 프로젝트 정식 저장/복제/재열기/충돌 거부, 건물·보도·식당 재현, 서비스 위치/핀, 기존 편집 버튼3해상도, 영업 시간 격리 확인. 이는 API·위치 제어 검사이며 실제 수동 운전이 아니다. 숨김 exe 스크린샷은 시각 근거로 사용하지 않는다; 별도 GPU RenderTexture 결과를 확인했다.
