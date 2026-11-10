# 아트 전환 시안 준비 가이드

2026-11-10. 현재 프로토타입(회색 박스 건물·실측 도로)을 제안서의 목표 아트(따뜻한 3D 카툰)로 바꿀 때 필요한 시안 목록, 생성 프롬프트, 이미지 생성 도구 설치 방법을 정리한다. 시안은 목표 아트이며 실제 게임 캡처가 아니다.

## 1. 전환 방향: 지도 데이터는 유지하고 외형만 교체

| 층 | 현재 | 목표 | 처리 |
|---|---|---|---|
| 도로 형상·차로·신호 | NGII 실측 기반 | **그대로 유지** | 게임 판정·내비가 이 데이터를 쓰므로 변경하지 않음 |
| 도로 재질 | 회색 단색 | 따뜻한 아스팔트, 굵고 둥근 차선, 보도블록 | 머티리얼 교체 |
| 건물 | 높이만 있는 회색 상자 | 용도별 카툰 건물 | **건물 외곽선·높이·용도를 읽어 모듈 키트로 자동 치환** |
| 가로수·소품 | 거의 없음 | 가로수, 화단, 벤치, 간판(가상) | 보도 데이터 기준 자동 배치 |
| 차량·인물 | 단순 형상 | 둥근 카툰 차량·인물 | 모델 교체(택시는 11/07 개선본 기준) |
| 조명 | 고정 태양광 | 따뜻한 햇빛, 시간대별 색 | 조명·후처리 설정 |

핵심 주장은 **"실측 지도 위에 카툰 키트를 입히는 방식이라 지도를 다시 만들지 않아도 된다"**이다. 이것을 보여주는 시안이 가장 중요하다(아래 A-1).

## 2. 필요한 시안 목록

우선순위 1은 제안서 V9에 바로 필요하다. 2~3은 본개발 착수 전 아트 공수 산정용이다.

| ID | 우선 | 시안 | 입력 참조 | 용도 |
|---|---|---|---|---|
| A-1 | 1 | **같은 시점 전·후 비교(1인칭)** | `assets/v8/04a-single-cockpit.png` | 실측 도로를 유지한 채 외형만 바뀌는 것을 증명 |
| A-2 | 1 | 같은 시점 전·후 비교(3인칭 추격 시점) | `docs/ui/2026-11-07-taxi-hud-corrections/taxi-chase-1920.png` | 차량·건물 전환 |
| A-3 | 1 | 같은 범위 전·후 비교(상공 시점) | 은계 지도 캡처 | 동네 전체 분위기 |
| B-1 | 1 | **차량 구매 화면** (후보 2대 비교) | `assets/v8/rounded-taxi-purchase.png` | 22 기획의 차고 화면 |
| B-2 | 1 | **PvP에 내 차 등장** (색이 다른 택시 4대 경쟁) | `assets/v8/09-pvp-hud-1920.png` | 싱글→PvP 연동 |
| C-1 | 2 | 건물 모듈 키트 시트: 아파트 단지 / 저층 상가 / 다세대·단독 / 학교·관공서 | A-1 결과 | 키트 수량·공수 산정 |
| C-2 | 2 | 시설 건물 시트: 주유소 / 세차장 / 식당 / 공업사 / 주차장 / 생활거점 | 기존 시설 캡처 | 시설 식별성 |
| C-3 | 2 | 도로 소품 시트: 신호등 / 표지판 / 횡단보도 / 가로수 / 버스정류장 / 볼라드 | — | 법규 판정 가독성 유지 확인 |
| D-1 | 2 | 차량 시트: 택시 3종(시작·후보 A·B) + NPC 차량 6종 | 현재 택시 prefab | 차종 구분 |
| D-2 | 2 | 승객·보행자 라인업(일반 + 특수 유형 대표 6명) | 기존 승객 초상 | 인물 톤 |
| E-1 | 3 | 시간대 컬러 스크립트: 아침 / 낮 / 저녁 / 비 | A-2 결과 | 조명 기준 |
| E-2 | 3 | 원거리·저사양 표현(LOD) 비교 | A-3 결과 | 성능 대비 |

## 3. 공통 스타일 문구

기존 V4~V8 프롬프트의 스타일을 이어 쓴다. 모든 프롬프트 앞에 붙인다.

```
Polished stylized 3D cartoon game art for a Korean taxi driving and life simulator.
Rounded friendly forms, warm soft sunlight, saturated yellow/blue/green accents,
clean readable shapes, achievable game-ready detail (not photoreal).
The taxi is the current small rounded yellow taxi: curved roof, round lamps,
compact four-door body, TAXI roof sign. Korean suburban apartment neighborhood.
No real brand logos, no famous landmarks, no watermark.
```

## 4. 시안별 프롬프트

### A-1 같은 시점 전·후 비교(1인칭)

참조 이미지: 실제 캡처 1장. "이미지 편집(참조 포함)" 방식으로 생성한다.

```
[공통 스타일]
Repaint this exact in-game first-person screenshot into the target art style.
KEEP EXACTLY: road layout, lane count, lane markings positions, yellow center line,
traffic signal positions, camera position and angle, steering wheel position,
all HUD panels in the same place.
CHANGE: grey box buildings become varied stylized Korean buildings that match
their footprint and height (apartment towers, low shops with awnings, small offices);
add roadside trees, flower beds, sidewalk paving, warm asphalt texture,
soft blue sky with clouds, warm sunlight and soft shadows.
HUD text may be simplified but keep layout. 16:9.
```

결과물은 원본 캡처와 나란히 배치해 "전 / 후"로 쓴다.

### A-2 3인칭 추격 시점

```
[공통 스타일]
Repaint this exact third-person chase-camera screenshot. Keep the road geometry,
lanes, intersection and taxi position identical. Replace the simple taxi with the
polished rounded yellow taxi, grey buildings with stylized Korean buildings of the
same size, add trees, pedestrians on sidewalks and a few rounded NPC cars. 16:9.
```

### A-3 상공 시점

```
[공통 스타일]
Repaint this top-down aerial view of a small Korean district. Keep every road,
block shape and building footprint position identical. Render buildings as cute
stylized 3D blocks by use: apartment complexes, shopping streets, houses, school.
Add trees, parks, parking lots. Slight tilt allowed, 16:9.
```

### B-1 차량 구매 화면

```
[공통 스타일]
Game UI concept for choosing a new taxi in a small garage at evening after work.
Two taxis side by side: (A) sleek rounded hybrid sedan taxi in yellow,
(B) taller roomy rounded taxi in yellow with bigger trunk.
Below each car, a clean white card with icon-only rows for fuel cost, trunk size,
ride comfort (use simple bar icons, no readable numbers).
One large confirm button, the current old taxi shown smaller at the side
as trade-in. Clean white and navy UI, warm garage lighting. 16:9.
```

### B-2 PvP에 내 차 등장

```
[공통 스타일]
Bird's-eye game scene of four rounded taxis racing to pick up passengers in a
compact stylized Korean city block: each taxi has a different body color
(yellow, mint, coral, navy) and a small roof decoration, a floating name tag
above each car (no readable text), passengers waiting on sidewalks with pickup
markers. Fun, competitive but friendly mood. 16:9.
```

### C·D·E 시트 공통 틀

```
[공통 스타일]
Clean asset reference sheet on a plain light background, orthographic three-quarter
views, evenly spaced, consistent scale, no text labels.
Assets: {여기에 목록}
```

- C-1 목록 예: `Korean apartment tower 15 floors, apartment tower 25 floors, 3-story shop row with awnings, 4-story multi-family house, small detached house, elementary school building`
- D-1 목록 예: `starting old rounded taxi, hybrid sedan taxi, roomy LPG taxi, compact car, SUV, small truck, city bus, delivery scooter`

## 5. 이미지 생성 도구 설치

이 Cowork 세션에는 이미지 생성 도구가 없다. 아래 중 하나를 고른다.

| 방법 | 장점 | 준비물 | 추천 상황 |
|---|---|---|---|
| ① ChatGPT/Codex 이미지 생성 | 기존 V4~V8 시안을 만든 방식 그대로 | 기존 계정 | **가장 빠름.** 프롬프트만 위에서 복사 |
| ② Claude Code + 생성 스크립트 | 저장소 안에서 생성·저장·이력까지 한 번에 | OpenAI API 키(유료) | 시안이 많고 반복 수정할 때 |
| ③ Claude Code + 이미지 생성 MCP 서버 | Claude가 대화 중 직접 호출 | API 키 + MCP 서버 | 여러 시안을 대화로 다듬을 때 |
| ④ Cowork 플러그인(CellCog 등) | 설치 버튼만 누르면 됨 | 해당 서비스 계정 | Cowork에서 바로 쓰고 싶을 때 |

### ② Claude Code + 생성 스크립트 (권장)

1. API 키 발급: OpenAI 플랫폼에서 키를 만든다. 키는 저장소에 넣지 않는다.
2. PowerShell에서 환경 변수 설정(사용자 계정에만 저장):
   ```powershell
   setx OPENAI_API_KEY "sk-..."
   ```
   새 터미널을 열어야 적용된다.
3. 패키지 설치:
   ```powershell
   python -m pip install openai
   ```
4. 스크립트 실행(참조 이미지 편집 방식):
   ```powershell
   python tools/generate_concept.py --prompt-file docs/proposals/assets/v9/prompts/A-1.txt `
     --ref docs/proposals/assets/v8/04a-single-cockpit.png `
     --out docs/proposals/assets/v9/A-1-after.png
   ```
   스크립트는 `tools/generate_concept.py`에 있다. 모델 이름은 기본 `gpt-image-1`이며, 더 새로운 모델이 나왔으면 `--model`로 바꾼다(OpenAI 문서에서 최신 이름 확인).
5. Claude Code에서는 "A-1 프롬프트로 시안 만들어줘"라고 하면 위 명령을 실행하고 결과를 확인할 수 있다.

### ③ 이미지 생성 MCP 서버

커뮤니티에서 만든 이미지 생성 MCP 서버가 여러 개 있다(예: OpenAI·Replicate·fal 등을 감싼 서버). 공식 제품이 아닌 경우가 많으므로 소스와 권한을 확인한 뒤 설치한다. 등록 형식:

```powershell
claude mcp add <이름> -e OPENAI_API_KEY=$env:OPENAI_API_KEY -- npx -y <패키지명>
```

### ④ Cowork 플러그인

Claude 앱의 플러그인 목록에서 "CellCog"를 검색하면 이미지·3D 모델·게임 에셋 생성 플러그인이 있다. 커뮤니티 플러그인이며 별도 계정·요금이 필요할 수 있다.

## 6. 결과물 관리

- 저장 위치: `docs/proposals/assets/v9/`
- 각 시안마다 사용한 프롬프트와 참조 이미지를 `docs/proposals/assets/v9/prompts.md`에 기록한다(기존 V4~V8 방식).
- 제안서에 넣을 때는 반드시 "목표 아트 시안" 표기를 붙여 실제 캡처와 구분한다.
