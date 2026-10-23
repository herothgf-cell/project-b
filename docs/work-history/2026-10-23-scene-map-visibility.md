# Unity 지도 Scene 표시 복구

## 요구와 원인

사용자 화면에서 오른쪽 게임 카메라에는 건물/도로가 보이지만 왼쪽 Scene에는 선택 테두리만 표시됐다. ProjectMapSceneWindow는 독립 preview scene을 연결하면서 SceneView 생성 시 상속한 main-stage overrideSceneCullingMask를 그대로 뒀다. 이 필터는 preview scene 메시를 제외하지만 Handles 테두리는 그려 증상과 일치했다. Unity2022.3 공개 참조 소스의 PreviewSceneStage.SyncSceneViewToStage는 customScene 설정과 함께 overrideSceneCullingMask=0을 사용한다.

참조: https://github.com/Unity-Technologies/UnityCsReference/blob/2022.3/Editor/Mono/SceneManagement/StageManager/PreviewSceneStage.cs

## 수정 파일

- unity/EungyeTaxi/Assets/Editor/ProjectMapSceneWindow.cs: 독립 Scene 연결/해제 시 SceneView 표시 마스크 초기화. 지원되지 않는 버전은 명시적 오류.
- unity/EungyeTaxi/Assets/Editor/ProjectMapSceneWindowTests.cs: 상속 마스크 회귀 및 실제 SceneView 카메라 픽셀 렌더 검사.
- docs/unity-map-editor-guide.md와 LATEST: 창 재오픈 방법과 최신 인계.

## 실제 검증

분리 작업 폴더에서 Unity2022.3.62f2로 ProjectMapSceneWindowTests.Run 실행. 수정 전 종료1 SCENE_WINDOW_FAIL inherited main-stage mask로 재현, 수정 후 종료0. GPU를 사용하는 batchmode 재검사도 종료0, SceneView 카메라가 독립 preview scene 검사 큐브를 실제144픽셀로 렌더했다. 기존 선택/100회 증분 편집/정식 지도 복제본 저장/사용자 씬 비변경 검사도 통과. 로그 artifacts/scene-culling-{red,green,render}.log (분리 작업 폴더 로컬).

실제 사용자의 Unity GUI 재오픈은 미검증이다. 실행파일은 편집기 코드를 포함하지 않으므로 재빌드하지 않았다. 기본 작업 폴더의 승객/법규/HUD 및 빌드 미커밋 변경, 별도 문서 변경을 보존한다. 정식 지도와 저장 데이터 변경 없음.

## 적용·재현

Unity 컴파일 완료 후 지도3D편집 패널/Scene 창을 모두 닫고 Taxi → 프로젝트 지도3D편집 → 프로젝트 지도 열기로 다시 연다. Scene에서도 주변 지도 메시가 표시되어야 한다. 수정 전후 동일 지도와 카메라를 사용하며 데이터 삭제는 하지 않는다.
