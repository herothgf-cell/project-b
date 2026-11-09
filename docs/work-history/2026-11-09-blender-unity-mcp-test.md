# 2026-11-09 Blender·Unity MCP 직접 호출 검사

## 요청·수행 범위

두 MCP가 실제로 작동하는지 직접 호출하여 현재 씬/오브젝트 목록, 도구 이름과 성공 여부를 확인했다. 기존 Blender 파일·Unity 씬·프로젝트 코드 변경 금지 요청을 준수했다. 기존 미추적 `unity/EungyeTaxi/ProjectSettings/PackageManagerSettings.asset`은 보존했다.

## 실제 도구와 결과

| MCP | 직접 호출 도구 | 결과 |
|---|---|---|
| Blender | `mcp__blender__get_objects_summary({})` | status=ok, 내부 result.status=ok, isError=false |
| Unity | `mcp__unityMCP__manage_scene(action="get_active")` | success=true, isError=false |
| Unity | `mcp__unityMCP__manage_scene(action="get_hierarchy", page_size=50, max_depth=8, max_nodes=200)` | success=true, isError=false, next_cursor=null, truncated=false |

Unity는 먼저 `read_mcp_resource(server="unityMCP", uri="mcpforunity://custom-tools")`와 `mcpforunity://instances`를 읽었다. 연결 인스턴스는 `EungyeTaxi@2a677756d1f7b653` 1개, Unity2022.3.62f2이며 프로젝트 식별이 일치했다. 이번에는 셸 HTTP 우회가 아닌 노출된 MCP 도구를 직접 호출했다.

Blender 활성 씬은 `Scene`, 작업공간 Layout·OBJECT 모드, 활성/선택 오브젝트 Cube. 모두 visible=true, 부모 없음.

```text
Scene
└─ Scene Collection
   └─ Collection
      ├─ Camera (CAMERA)
      ├─ Cube (MESH)
      └─ Light (LIGHT)
```

Unity 활성 씬의 API name/path는 빈 문자열, buildIndex=-1, isLoaded=true, isDirty=false, rootCount=2. 직전 창 제목 기준 Untitled에 해당하며 이번 응답에는 정식 씬 이름이 없다. 두 오브젝트 모두 활성/자식0개다.

```text
이름·저장 경로 없는 씬 (Untitled)
├─ Main Camera [Transform, Camera, AudioListener]
└─ Directional Light [Transform, Light]
```

연결 실패/도구 오류는 발생하지 않았다. Unity의 빈 씬 이름은 조회 실패가 아니라 현재 이름·경로 없는 씬의 응답이다. 과거 채팅의 도구 미노출/샌드박스 HTTP 차단과 이번 정상 직접 호출을 구분한다.

## 변경·검증 한계·재현

이 기록과 LATEST만 갱신한다. 저장/로드/생성/편집·Play 전환·코드 실행·빌드 없음. 이번 검증은 현재 씬 읽기 연결 검사이며 렌더/모델 제작/게임 플레이/전체 캠페인 정상 동작까지 검증하지 않았다. 재현은 위 읽기 전용 MCP 호출을 동일하게 수행한다.

기존 배포 은계/A2·LANv16·BuildHybrid·소스548fe8b/출하a978b3b 유지. 필수 지도는 직전 Strict issues0 인용(이번 재검사 없음). 다음은 요청 시 게임 씬 조회 및 기존 사람 장시간/전체3일·다른PC LAN 수용이다.
