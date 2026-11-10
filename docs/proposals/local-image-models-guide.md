# 로컬 이미지 모델 설치·사용 가이드 (무료)

2026-11-10. [Blender 단계별 진행](art-blender-pipeline.md) 6단계와 키아트 제작용. 모든 처리는 내 PC에서 하며 사용료가 없다. 파일 이름·폴더는 작성일 기준 공식 문서를 따랐다. 바뀌었을 수 있으므로 아래 출처에서 다시 확인한다.

| 용도 | 모델 | 라이선스 |
|---|---|---|
| Blender 렌더 형상 유지 + 카툰풍 덧칠 | **SDXL 1.0 base** + **ControlNet Union SDXL (xinsir)** | SDXL: CreativeML OpenRAIL++-M(상업 사용 허용, 사용 제한 조항 있음) / ControlNet: Apache-2.0 |
| 키아트·인물 새 그림 | **FLUX.1 [schnell]** | Apache-2.0 |

> FLUX.1 [dev]는 비상업 라이선스라 회사 시안에 쓰지 않는다. 커뮤니티 SDXL 파생 모델(카툰 특화 등)은 모델마다 라이선스가 다르므로 사용 전 개별 확인한다.

## 0. 준비

- NVIDIA GPU 권장. 대략 SDXL은 VRAM 8GB 이상, FLUX schnell fp8은 12GB 이상에서 무난하다(적으면 느려지거나 실패).
- 디스크 여유 40GB 이상(모델 파일이 크다).
- 7-Zip 설치.

## 1. ComfyUI 설치 (Windows 포터블)

1. ComfyUI 공식 문서의 Windows Portable 안내에서 GPU에 맞는 파일을 받는다.
   - RTX 계열: `ComfyUI_windows_portable_nvidia.7z`
   - GTX 10 시리즈 이하: `ComfyUI_windows_portable_nvidia_cu126.7z`
2. 7-Zip으로 원하는 곳에 푼다. 예: `C:\AI\ComfyUI_windows_portable\`
   - 저장소(`C:\git\project-b`) 안에는 풀지 않는다. 모델이 수십 GB라 Git에 섞이면 안 된다.
3. `run_nvidia_gpu.bat`을 더블클릭한다.
4. 창에 `http://127.0.0.1:8188`이 뜨면 브라우저로 연다. **검은 창을 닫으면 ComfyUI가 꺼진다.**

설치형 앱(Comfy Desktop)도 있다. 포터블과 기능은 같고 모델 폴더 위치만 다르다.

## 2. 모델 받기와 넣을 위치

모두 `ComfyUI\models\` 아래 폴더에 넣는다. Hugging Face에서 받는다.

### SDXL + ControlNet

| 파일 | 받는 곳 | 넣을 폴더 |
|---|---|---|
| `sd_xl_base_1.0.safetensors` | stabilityai/stable-diffusion-xl-base-1.0 | `models\checkpoints\` |
| ControlNet Union SDXL (`diffusion_pytorch_model_promax.safetensors` 등, 받은 뒤 `controlnet-union-sdxl-promax.safetensors`처럼 알아보기 쉽게 이름 변경) | xinsir/controlnet-union-sdxl-1.0 | `models\controlnet\` |

Union 모델 하나로 Depth·Canny·Lineart 등 12가지 조건을 쓴다.

### FLUX.1 schnell (가장 쉬운 단일 파일 방식)

| 파일 | 넣을 폴더 |
|---|---|
| `flux1-schnell-fp8.safetensors` | `models\checkpoints\` |

ComfyUI 공식 FLUX 튜토리얼 페이지에서 받는다. 품질을 더 원하면 같은 페이지의 "전체 버전"(diffusion model + clip_l + t5xxl + ae 4개 파일)을 쓴다.

파일을 넣은 뒤 ComfyUI 화면을 새로고침(R)하면 목록에 나타난다.

## 3. 사용법 A — Blender 렌더를 카툰풍으로 (SDXL + ControlNet)

이 프로젝트의 핵심 용도다. **Blender에서 깊이 이미지를 직접 뽑으므로 별도 깊이 추정 모델이 필요 없다.**

### 3-1. Blender에서 두 장 렌더

1. 같은 카메라로 **컬러 렌더**(`A-1-color.png`)
2. **깊이 이미지**(`A-1-depth.png`): 뷰 레이어에서 Mist 패스를 켜고, 가까운 곳이 밝게 나오도록 반전해 흑백으로 저장한다(ControlNet Depth는 가까울수록 흰색). 2단계 이후 Claude Code가 자동으로 뽑도록 스크립트에 넣는다.
3. 크기는 SDXL에 맞게 1344×768(16:9 근사) 또는 1216×832로 한다.

### 3-2. ComfyUI 노드 연결

빈 화면에서 더블클릭하면 노드를 검색해 추가할 수 있다.

```
Load Checkpoint (sd_xl_base_1.0)
 ├─ CLIP Text Encode (긍정 프롬프트)
 ├─ CLIP Text Encode (부정 프롬프트)
 └─ MODEL ──────────────────────────────┐
Load Image (A-1-depth.png)               │
Load ControlNet Model (union promax)     │
 └─ SetUnionControlNetType: depth        │
Apply ControlNet (강도 0.7~0.9) ← 긍정/부정 프롬프트 연결
 └─▶ KSampler ◀──────────────────────────┘
      ▲ latent: VAE Encode(A-1-color.png)  ← 컬러 렌더를 바탕으로 덧칠할 때
        또는 Empty Latent Image 1344×768    ← 깊이만 따르고 새로 그릴 때
 └─ VAE Decode → Save Image
```

권장 시작값:

| 항목 | 값 | 설명 |
|---|---|---|
| steps | 25~30 | |
| cfg | 5~7 | |
| sampler / scheduler | dpmpp_2m / karras | |
| denoise | 0.55~0.75 (컬러 렌더 바탕일 때) | 낮을수록 원본 유지, 높을수록 카툰 변형 |
| ControlNet 강도 | 0.7~0.9 | 높을수록 도로·건물 위치 고정 |

### 3-3. 프롬프트 예

긍정:
```
stylized 3D cartoon game art, warm Korean suburban neighborhood, rounded friendly buildings,
apartment towers, small shops with awnings, roadside trees, clean asphalt road with bright lane
markings, soft warm sunlight, saturated yellow blue green accents, clean readable shapes
```

부정:
```
photo, photorealistic, text, letters, logo, watermark, blurry, distorted road, extra lanes, people deformed
```

택시를 보존하려면 컬러 렌더 바탕(VAE Encode) 방식에서 denoise를 0.5 근처로 낮춘다.

## 4. 사용법 B — 키아트·인물 (FLUX.1 schnell)

1. ComfyUI 공식 FLUX 튜토리얼 또는 Comfy 워크플로 허브의 "Flux.1 Schnell FP8" 워크플로를 받아 화면에 끌어다 놓는다. 노드가 자동으로 구성된다.
2. Load Checkpoint에서 `flux1-schnell-fp8.safetensors`를 고른다.
3. 프롬프트에 [V9 프롬프트](assets/v9/prompts/)의 `_style.txt` + 원하는 장면(B-2, D-2 등)을 넣는다.
4. **steps 4** (schnell은 4단계면 충분), 크기 1344×768.
5. Queue(실행) → 결과는 `ComfyUI\output\`에 저장된다.

FLUX는 부정 프롬프트를 거의 쓰지 않는다. 원하지 않는 요소는 긍정 프롬프트에서 빼고 표현을 바꾼다.

## 5. 결과 관리

- 쓸 결과만 `docs/proposals/assets/v9/`로 복사한다. 이름 예: `A-1-after-sdxl.png`
- 같은 폴더 `prompts.md`에 **모델·프롬프트·seed·denoise·ControlNet 강도**를 기록한다(같은 결과를 다시 만들 수 있게).
- 워크플로는 ComfyUI 메뉴 → Save(JSON)로 `docs/proposals/assets/v9/workflows/`에 저장한다. 작은 JSON이라 Git에 넣어도 된다. 모델 파일은 넣지 않는다.
- 제안서에는 "목표 아트 시안(생성)" 표기를 유지한다.

## 6. 자주 생기는 문제

| 증상 | 조치 |
|---|---|
| 모델이 목록에 없음 | 폴더 위치 확인 후 화면 새로고침(R) 또는 재시작 |
| `CUDA out of memory` | 크기를 1024×576으로 낮추기, 다른 GPU 프로그램(Unity·Blender) 종료, FLUX는 fp8 사용 |
| 도로·차선이 흐트러짐 | ControlNet 강도 올리기, denoise 낮추기 |
| 너무 원본 렌더 같음 | denoise 올리기(0.75), 프롬프트에 스타일 단어 보강 |
| `SetUnionControlNetType` 노드가 없음 | ComfyUI 업데이트(`update\update_comfyui.bat`) |

## 출처

- ComfyUI Windows 포터블 설치: https://docs.comfy.org/installation/comfyui_portable_windows
- ComfyUI FLUX.1 튜토리얼(파일 이름·폴더·schnell 4 steps·라이선스): https://docs.comfy.org/tutorials/flux/flux-1-text-to-image
- Flux.1 Schnell FP8 워크플로: https://comfy.org/hub/workflows/flux-1-schnell-fp8-75fb83052003/
- ControlNet Union SDXL(Apache-2.0, 지원 조건 12종): https://huggingface.co/xinsir/controlnet-union-sdxl-1.0
- ComfyUI 폴더 구조: https://comfyui-wiki.com/en/interface/files
