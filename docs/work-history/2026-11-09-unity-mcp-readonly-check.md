# 2026-11-09 Unity MCP 읽기 전용 상태 확인

## 요청과 수행 범위

연결된 Unity MCP 상태, 활성 씬 이름과 GameObject 계층을 확인했다. 기존 오브젝트·스크립트는 수정하지 않았다. 시작 커밋e6172bc. 기존 미추적 `unity/EungyeTaxi/ProjectSettings/PackageManagerSettings.asset`은 보존했다.

## 실제 조회와 결과

이 채팅의 도구 목록에는 Unity MCP가 직접 노출되지 않았다. unity-cli 스킬을 확인했으나 CLI는 PATH에서 발견되지 않았다. 로컬 Codex 설정의 실제 MCP 주소 `http://127.0.0.1:8080/mcp`를 확인하고 HTTP MCP 요청으로 읽었다. 샌드박스의 프로세스/localhost 접근 차단은 승인된 읽기 전용 명령으로 확인했다. 설정 파일 변경이나 패키지 설치는 하지 않았다.

- initialize 성공: 서버 `mcp-for-unity-server` 버전3.4.11, 프로토콜2024-11-05.
- `resources/read` custom-tools/instances/editor/state 성공. 연결 인스턴스1개 `EungyeTaxi@2a677756d1f7b653`, Unity2022.3.62f2·WindowsEditor.
- Editor idle, Play/일시정지/컴파일/도메인 리로드 대기/에셋 갱신 모두 false, ready_for_tools=true.
- `manage_scene`의 읽기 전용 `get_active`, `get_loaded_scenes`, `get_hierarchy` 모두 success=true/isError=false, HTTP 조회 셸 exit0.
- 활성 씬1개. MCP name/path는 빈 문자열, buildIndex=-1, isLoaded=true, isDirty=false. Editor 창 제목은 `EungyeTaxi - Untitled`이므로 미저장 Untitled 씬으로 정리한다.
- 계층은 루트2개, next_cursor=null/truncated=false. 두 오브젝트 모두 활성·자식0개다.

```text
Untitled (MCP 이름/경로 없음)
├─ Main Camera
│  └─ 컴포넌트: Transform, Camera, AudioListener
└─ Directional Light
   └─ 컴포넌트: Transform, Light
```

## 변경 파일·미검증·이어가기

이 기록과 LATEST만 갱신했다. 씬 로드/저장·Play 전환·선택/카메라 변경·스크립트 실행/편집·빌드·지도/배포/사용자 저장 변경 없음. 게임 씬은 현재 열려 있지 않으며, 게임 런타임이나 전체 프로젝트의 오류 유무를 검증한 결과는 아니다.

재현: 로컬 서버에 MCP initialize 후 instances/editor/state 리소스를 읽고 manage_scene의 위3개 조회를 실행한다. 세션ID는 새 연결에서 받은 값을 사용한다. 게임 씬 조회가 필요하면 별도 요청 후 해당 씬을 연다.

배포 기준은 기존 은계/A2·LANv16·BuildHybrid·소스548fe8b/출하a978b3b, 필수 입력은 직전 진행 점검 Strict issues0을 인용하며 이번 재검사는 없다. 사람 장시간/전체3일·다른PC LAN 수용은 계속 남아 있다.
