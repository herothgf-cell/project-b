# 2026-11-10 아트 Blender 파이프라인 1-4 환경 확인

## 요구사항

[art-blender-pipeline.md](../proposals/art-blender-pipeline.md) 1단계 1-4의 환경 확인만 수행. 파일·씬 변경 금지, 결과를 문서 1-4 "확인 결과" 표와 작업 이력에 기록.

## 실제 수행 방법

이 Claude Code 세션 도구 목록에는 Blender/Unity MCP 도구가 노출되지 않았다. 그래서 같은 서버에 직접 읽기 전용 요청을 보냈다(설정 변경·패키지 설치 없음, 임시 스크립트는 세션 scratchpad에만 둠).

- 포트 확인: `127.0.0.1:9876` = blender.exe(PID 7052), `127.0.0.1:8080` = python(Unity MCP 서버). Unity Editor 2022.3.62f2 실행 중.
- Blender: 로컬 `~/blender_mcp/mcp`의 `blmcp/tools_helpers/connection.py`와 같은 프로토콜(`{"type":"execute","code":...,"strict_json":true}` + NUL 구분)로 `bpy.app.version_string`, `bpy.data.scenes`, 렌더 엔진 목록, `is_dirty`만 읽는 코드를 실행. 처음 다른 형식(`get_scene_info`)으로 보낸 요청은 응답 없이 시간 초과 → 프로토콜 확인 후 재요청 성공.
- GPU: `nvidia-smi --query-gpu=name,memory.total,memory.used,driver_version --format=csv` exit 0.
- 디스크: PowerShell `Get-PSDrive C`.
- Unity: `http://127.0.0.1:8080/mcp`에 MCP `initialize`(2024-11-05) 후 `resources/read`로 `mcpforunity://instances`, `mcpforunity://editor/state` 조회.

## 결과

| 항목 | 결과 |
|---|---|
| Blender | 5.2.3 LTS, MCP 응답 status=ok |
| 씬 | `Scene` 1개(활성), 오브젝트 3개(Camera/Cube/Light), 파일 경로 없음(새 파일), is_dirty=false(조회 전후 변경 없음) |
| 렌더 엔진 | 정적 enum `BLENDER_EEVEE`, 등록 엔진 클래스 `CYCLES`·`HydraRenderEngine`. 현재 씬 엔진 BLENDER_EEVEE → EEVEE 사용 가능. GPU 백엔드 OPENGL |
| GPU | NVIDIA GeForce RTX 5060 Laptop GPU, 8151 MiB, 사용 1491 MiB, 드라이버 621.14 |
| C: | 여유 342.5 GB / 전체 475.9 GB |
| Unity MCP | initialize 성공 `mcp-for-unity-server` 3.4.11. editor/state success=true, 인스턴스 `EungyeTaxi@2a677756d1f7b653`, Unity 2022.3.62f2, WindowsEditor, phase idle, Play/컴파일/에셋 갱신 아님, 활성 씬 이름·경로 없음(Untitled). instances 리소스는 첫 SSE 줄이 로그 알림이라 본문은 이번 출력에서 확인하지 못함 |

판단: VRAM 8GB는 6단계 SDXL 1.0 + ControlNet의 하한. FLUX.1 [schnell]은 경량(양자화) 버전으로만 시도.

## 변경 파일

- `docs/proposals/art-blender-pipeline.md`: 1-4 "확인 결과" 표, 진행 기록 1줄
- `docs/work-history/2026-11-10-art-pipeline-env-check.md`(이 파일)
- `docs/work-history/LATEST.md`: 최근 기록 1건

Blender 씬·Unity 씬·게임 코드·빌드·지도·설정 변경 없음. 기존 미커밋 사용자 변경(제안서 V9·기획22·AGENTS 등)과 미추적 `PackageManagerSettings.asset`은 그대로 두었고, **Git 커밋은 하지 않았다**(기존 미커밋 문서와 섞이지 않게).

## 미검증

- EEVEE 실제 렌더, GPU 렌더 속도, 로컬 이미지 모델 실행은 하지 않음.
- Unity 씬 계층·게임 씬 조회, 빌드/Strict/플레이 검사 없음. 배포 기준(은계/A2·LANv16·BuildHybrid·548fe8b/a978b3b)은 기존 기록 인용.

## 재현

1. Blender에서 MCP 애드온 서버를 켠 상태로 9876 포트에 위 `execute` 형식으로 읽기 전용 코드를 보낸다(또는 MCP 도구가 노출된 세션에서 `get_objects_summary`).
2. `nvidia-smi --query-gpu=name,memory.total --format=csv`, `Get-PSDrive C`.
3. Unity Editor + MCP for Unity 서버 실행 후 `http://127.0.0.1:8080/mcp`에 initialize → `resources/read mcpforunity://editor/state`.
