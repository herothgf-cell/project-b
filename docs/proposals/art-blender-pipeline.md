# Blender 무료 아트 전환 단계별 진행

> **보류 (2026-11-10 사용자 결정).** 프로토타입 단계에서는 재미·제작 가능성 검증이 우선이므로 1단계(조사·환경 확인)까지만 하고 중단한다. 본개발 승인 후 아트 담당이 정해지면 2단계부터 재개한다. 제안서 설득용 시안은 기존 목표 시안과 V9 22장으로 충분하다고 판단했다.

2026-11-10 시작. [아트 전환 가이드](art-conversion-guide.md)의 결제 없는 경로다. 실측 지도를 Blender로 가져와 카툰 재질로 렌더하고, 필요하면 로컬 이미지 모델로 다듬는다. **한 단계씩 끝내고 사용자 확인 후 다음 단계로 간다.**

## 전체 단계

| 단계 | 내용 | 실행 위치 | 결과물 | 상태 |
|---|---|---|---|---|
| 1 | 준비 조사: 지도 데이터 구조, 기존 Blender 자산, 실행 환경 확인 | Cowork(조사) + Claude Code(환경 확인) | 이 문서 §1 | **완료** |
| 2 | 지도 → Blender 변환 스크립트(도로·보도·건물 조각 병합·물) | Claude Code + Blender MCP | `tools/art/build_eungye_blender.py`, `.blend` | 보류 |
| 3 | 게임 캡처와 같은 카메라 시점 맞추기(1인칭·3인칭·상공) | Claude Code + Unity MCP + Blender MCP | 카메라 3개 | 보류 |
| 4 | 카툰 재질·건물 용도별 외형·가로수·조명 | Blender MCP | 스타일 1차 | 보류 |
| 5 | 렌더와 전·후 비교 이미지 | Blender | `docs/proposals/assets/v9/A-1~A-3` | 보류 |
| 6 | (선택) 로컬 이미지 모델로 다듬기, 제안서 22장 교체 | 로컬 GPU | V9 갱신 | 보류 |

## 1단계 결과: 준비 조사

### 1-1. 지도 데이터 (`config/maps/project/eungye.project.json`, 21.7MB)

정식 지도 하나에 필요한 형상이 모두 들어 있어 **별도 원본(NGII) 없이 Blender 변환이 가능하다.**

| 항목 | 위치 | 수량 | Blender에서 쓰는 방법 |
|---|---|---|---|
| 지도 범위 | `baseline.width/depth` | 2,100 × 1,750m | 장면 크기. 좌표는 x·z(미터), 높이는 y |
| 도로 | `document.scene.objects` kind=`road` | 1,732개 | 중심선 `points` + `width` → 띠 메시 |
| 보도·자전거도로 | kind=`sidewalk` | 18,201개 | 같은 방식, 높이 약 0.15m 올림 |
| 건물 | kind=`building` | 3,293조각 | 외곽선 `points` + `height` → 돌출 |
| 물 | kind=`water` | 1 (소래저수지) | 평면 + 물 재질 |
| 시설 | kind=`restaurant/rest/parking` + `document.facilities` 69 | — | 식당·휴식·주차장, 표지판 위치 |
| 차로·신호 | `document.lanes` 1,891, `baseline.hdSignals` 98 | — | 차선 도색·신호등 소품 위치 |

### 1-2. 건물 데이터에서 발견한 점 (2단계 설계에 반영)

- **건물이 잘게 나뉘어 있다.** 조각 면적 중앙값 24㎡, 한 단지가 수십~85조각. 그대로 카툰 키트를 씌우면 조각마다 다른 건물처럼 보인다. → 2단계에서 **이름 + 인접 조각을 하나로 병합**한다.
- **높이 3m 조각이 2,745개(83%)** 다. 이 중 1,295개는 아파트 단지 이름이 있다. 동(棟) 본체는 30m 이상(137조각), 3m 조각은 부속 건물이거나 높이 정보가 없는 것으로 보인다. → 3m는 "저층(1~3층)"으로 처리하고, 실제 모습은 3단계 캡처와 비교해 확인한다.
- **용도 정보가 없다.** 이름만 있다. → 용도 분류 규칙(안):

| 조건 | 분류 | 카툰 키트 |
|---|---|---|
| 높이 ≥ 30m | 아파트 동 | 흰 판상형·탑상형, 층 줄무늬, 옥상 구조물 |
| 이름에 아파트·단지·자이·푸르지오 등 + 높이 < 30m | 단지 부속 | 낮은 상가동·경비실 |
| 이름에 학교·교 | 학교 | 붉은 벽돌 + 운동장 |
| 4~30m, 이름 없음 또는 빌딩 | 상가·근린 | 1층 차양·간판(가상 문구 없음) |
| 3m, 이름 없음 | 주택·소형 | 2~3층 다세대, 박공지붕 일부 |

### 1-3. 이미 있는 Blender 자산

- `art-source/taxi/TaxiConcept.blend` / `.fbx`: 11/07 Blender MCP로 만든 둥근 택시. **그대로 장면에 배치 가능.**
- `tools/art/build_taxi_blender.py`: `TAXI_REPO_ROOT` 지정 → 새 씬 생성 방식. 좌표 규칙 **Blender (x,y,z) = Unity (x, −z, y)**. 지도 변환도 같은 규칙과 같은 실행 방식을 따른다.
- 11/09 기록: Blender MCP 직접 호출 정상(`get_objects_summary` status=ok). Unity MCP도 응답.

### 1-4. Claude Code에서 확인할 환경 (사용자 PC)

이 Cowork 세션은 PC의 Blender를 직접 실행할 수 없다. Claude Code에 아래를 그대로 붙여 넣는다.

```
docs/proposals/art-blender-pipeline.md 1단계 1-4 환경 확인만 해줘. 파일은 바꾸지 마.
1) Blender MCP로 Blender 버전과 현재 씬 목록 조회
2) Blender 렌더 엔진 목록에서 EEVEE 사용 가능 여부
3) nvidia-smi로 GPU 이름과 VRAM 확인(없으면 없다고)
4) 디스크 여유 공간(C:)
5) Unity MCP 응답 여부
결과를 이 문서 1-4 아래 "확인 결과" 표로 추가하고 작업 이력에 남겨줘.
```

GPU/VRAM 결과로 6단계의 로컬 이미지 모델 사용 가능 여부가 정해진다.

#### 확인 결과

2026-11-10 Claude Code(PC kimhs-NW2)에서 읽기 전용으로 확인. 상세는 [작업 이력](../work-history/2026-11-10-art-pipeline-env-check.md).

| 항목 | 결과 | 판정 |
|---|---|---|
| Blender 버전 | 5.2.3 LTS (`C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`) | 정상 |
| Blender MCP 응답 | 애드온 소켓 `127.0.0.1:9876` `execute` 요청 status=ok | 정상 |
| 현재 씬 목록 | `Scene` 1개(활성, 오브젝트 3개 = Camera/Cube/Light), 저장 안 된 새 파일, is_dirty=false | 기본 빈 씬 |
| 렌더 엔진 | 내장 `BLENDER_EEVEE`, 애드온 `CYCLES`·`HydraRenderEngine`. 현재 씬 엔진이 이미 BLENDER_EEVEE, GPU 백엔드 OpenGL | **EEVEE 사용 가능** (실제 렌더는 미실행) |
| GPU / VRAM | NVIDIA GeForce RTX 5060 Laptop GPU, 8151 MiB(약 8GB), 드라이버 621.14 | SDXL(8GB~) 가능 범위 하한, FLUX(12GB~)는 경량판 필요 |
| 디스크 여유(C:) | 342.5 GB 여유 / 475.9 GB | 충분 |
| Unity MCP 응답 | `http://127.0.0.1:8080/mcp` initialize 성공(`mcp-for-unity-server` 3.4.11), editor/state success=true. 인스턴스 `EungyeTaxi@2a677756d1f7b653`, Unity 2022.3.62f2, idle·Play/컴파일 아님 | 정상 |

6단계 판단: VRAM 8GB이므로 **SDXL 1.0 + ControlNet을 1순위**로 하되 해상도·배치 크기를 낮추거나 저VRAM 옵션이 필요할 수 있다. FLUX.1 [schnell]은 양자화(경량) 버전으로만 시도한다. 실제 이미지 생성 속도는 미검증.

## 추천 모델

### 무료 로컬 이미지 모델 (6단계용, 회사 사용 전제)

회사 프로젝트이므로 **상업적 사용 허용 여부**를 먼저 본다. 라이선스는 바뀔 수 있으므로 사용 전 각 모델 페이지에서 다시 확인한다.

| 모델 | 필요 VRAM(대략) | 상업 사용 | 추천 용도 |
|---|---|---|---|
| **SDXL 1.0** + ControlNet(깊이·윤곽) | 8GB~ | 허용(OpenRAIL++) | **1순위.** Blender 렌더의 형상을 유지한 채 카툰풍 덧칠. 생태계(ControlNet·LoRA)가 가장 넓음 |
| **FLUX.1 [schnell]** | 12GB~(경량 버전은 더 적게) | 허용(Apache 2.0) | 키아트·인물 등 새 그림. 품질 높고 빠름 |
| Stable Diffusion 3.5 | 10GB~ | 조건부(커뮤니티 라이선스, 매출 기준 있음) | 회사 규모상 라이선스 확인 필요 |
| FLUX.1 [dev] | 12GB~ | **비상업** | 회사 시안에는 쓰지 않음 |

실행 도구는 **ComfyUI**(무료, Windows 설치판)를 권한다. Blender 렌더 → ControlNet(Depth) → SDXL 순서의 작업 흐름을 저장해 반복할 수 있다. 설치·사용법은 [로컬 이미지 모델 가이드](local-image-models-guide.md).

### Claude 모델 선택 (Claude Code)

| 작업 | 권장 |
|---|---|
| 2단계 변환 스크립트, 3단계 카메라 맞추기(설계·디버깅이 많은 작업) | 가장 성능이 높은 모델(Opus 계열) |
| 4~5단계 재질 조정·반복 렌더(짧은 반복 작업) | Sonnet 계열로도 충분, 사용량 절약 |

Claude Code에서 `/model`로 바꿀 수 있다.

## 진행 기록

| 날짜 | 단계 | 내용 |
|---|---|---|
| 2026-11-10 | 1 | 지도 JSON 구조·건물 통계·기존 Blender 자산 조사(Cowork). 환경 확인은 Claude Code 대기 |
| 2026-11-10 | 1 | 1-4 환경 확인(Claude Code): Blender 5.2.3 LTS·EEVEE 가능, RTX 5060 Laptop 8GB, C: 342.5GB 여유, Blender/Unity MCP 응답 정상 |
| 2026-11-10 | — | 사용자 결정으로 2단계 이후 보류(본개발 승인 후 재개) |
