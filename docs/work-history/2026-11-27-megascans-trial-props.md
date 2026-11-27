# 2026-11-27 Megascans 도로 소품 시험

## 요구사항

- Fab의 Quixel Megascans 무료 3D 모델(도로·공사, 가구, 건물, 차량 카테고리) 중 택시 프로젝트에 쓸 만한 것을 정리한다(대화로 답함).
- 그중 몇 개를 브라우저로 직접 받아 `art-source/` 아래 새 폴더에서 시험하고 기록으로 남긴다.

## 실제 구현

- 카테고리 4곳 전체 목록(도로·공사 159, 가구 530, 건물 960, 차량 14)을 Fab 검색 API로 받아 훑었다. 쓸 만한 것은 도로·공사 소품과 건물 카테고리의 Urban Japanese Street·Urban Miscellaneous 일부였다. 가구는 사무실 소품 정도만 해당했고, 차량은 쓸 것이 없었다.
- 사용자가 Fab EULA에 직접 동의한 뒤, 도로 소품 5종의 FBX Low ZIP을 받아 `art-source/megascans-trial/`에 풀었다(5개 합계 12,904,483바이트). 상품 주소·크기·SHA256은 [시험 README](../../art-source/megascans-trial/README.md)에 있다.
- GitHub 저장소가 공개(`api.github.com` 200)이고 Fab 표준 라이선스는 원본을 비공개 저장소의 협업자에게만 공유하도록 허용하므로, `.gitignore`로 원본을 제외했다. README와 비교 렌더만 추적한다.
- `tools/art/render_megascans_trial.py`(Blender 헤드리스)를 추가했다. 물건마다 스캔 텍스처와 BaseColor 대표색 단색을 나란히 렌더해 `trial-render.png`를 만든다. FBX 가져오기에서 생기는 축척 부모를 떼고, 텍스처 sRGB 값을 선형 색으로 바꾸는 처리를 넣었다.
- `docs/third-party-register.md`의 "아직 도입하지 않은 자료"에 시험 취득 기록을 추가했다. 이 파일은 이전부터 Git 미추적 상태라(사용자 작업) 이번 커밋에 넣지 않았다.

## 변경 파일

- `.gitignore`
- `art-source/megascans-trial/README.md`, `art-source/megascans-trial/trial-render.png` (새 파일, 원본 FBX·텍스처·ZIP은 Git 제외)
- `tools/art/render_megascans_trial.py` (새 파일)
- `docs/third-party-register.md` (미추적 파일에 항목 추가, 커밋 안 함)
- `docs/work-history/2026-11-27-megascans-trial-props.md`, `docs/work-history/LATEST.md`

## 검증

- 받은 ZIP 5개의 바이트 수가 Fab API가 알려 준 크기와 모두 일치했다. 압축 항목은 평평한 파일 이름(FBX·JPG·JSON)뿐이었다.
- `git check-ignore`로 ZIP과 FBX가 제외되는 것, `git status -uall`로 README와 렌더만 추적 후보인 것을 확인했다.
- Blender 5.2 헤드리스 렌더 종료 정상, 크기 출력이 Fab 설명의 실제 크기와 일치(라바콘 0.36×0.36×0.69 m 등).
- 렌더 판독: 상가 셔터는 단색에서도 형태로 읽힌다. 라바콘·시선유도봉은 흰 반사띠가 사라진다. 콘크리트 방호벽은 스캔 표면 요철이 드러나고 색이 누렇게 뜬다. 가드레일 키트는 한국 W형 가드레일이 아니다.

## 추가: 상가 셔터 Unity 시험

요구사항: 셔터를 Unity에 넣어 시험한다(사용자 요청, 바로 진행).

구현:
- 셔터 FBX·텍스처를 Git에서 제외되는 `unity/EungyeTaxi/Assets/ArtTrial/MegascansShutter/`에 복사했다(`.gitignore`에 `unity/**/Assets/ArtTrial/` 추가). Resources 밖이라 배포 빌드에 들어가지 않는다.
- `Assets/Editor/MegascansShutterTrial.cs`(메뉴 Taxi → Art → Megascans Shutter Trial)를 추가했다. 저장하지 않는 빈 씬에 게임 색의 도로·인도·상자 건물 두 채를 놓는다. 1층에 셔터를 5개씩 붙이는데, 왼쪽은 스캔 재질(Standard Specular setup, Specular+Gloss 합성 PNG, Normal, AO), 오른쪽은 단색(BaseColor 평균, 광택 0.12)이다. 거리·운전석 높이 두 장을 `art-source/megascans-trial/unity-trial-*.png`로 찍는다.
- 시험 중 고친 것: FBX 앞면이 +Z라 180° 회전, 서브메시 전체에 재질 지정, 해 방향을 건물 정면 쪽으로, 빈 씬을 연 뒤에 재질 생성(먼저 만들면 씬 전환 때 정리되어 분홍색으로 나옴).

검증:
- Unity 2022.3.62f2 배치 모드(`-nographics` 없이) 종료 0, 로그 `SHUTTER_TRIAL_OK`. 크기 1.84×2.51×0.41 m(Fab 설명과 같음).
- 첫 실행은 다른 세션의 r81 시험 빌드가 프로젝트를 열고 있어 종료 21로 거부됐다. 그 빌드가 끝난 뒤 다시 실행했다.
- `git status`에서 `FirstDrive.unity` 등 다른 작업의 변경을 건드리지 않았음을 확인했다. 새 추적 대상은 시험 스크립트와 meta뿐이다.
- 판독: 스캔판은 회색 상자 건물에서도 상가 셔터로 바로 읽힌다. 단색판은 골은 보이지만 어두운 슬레이트처럼 보인다. 셔터 하나가 3,163면이다.

## 추가: 셔터 카툰 색 다듬기

요구사항: 단색판 셔터를 밝은 카툰 색과 2색으로 다듬는다(사용자 선택).

구현:
- `MegascansShutterTrial.RunCartoon`(메뉴 Taxi → Art → Megascans Shutter Cartoon Trial)을 추가했다. 셔터 메시를 복사해 서브메시 두 개로 나눈다. 0번은 몸체(셔터 판), 1번은 테두리(상단 16% 셔터함, 폭 양끝 각 5.5% 레일)다. 텍스처 없이 광택 0.12의 `Standard` 단색을 입힌다.
- 오른쪽 건물에 5가지 조합(navy·green·brick·skyblue·mustard)을 놓고, 왼쪽 건물에는 비교용 스캔판을 놓아 `unity-trial-cartoon-street.png`·`unity-trial-cartoon-close.png`를 찍는다. 색 값은 시험 README 표에 있다.
- 1차는 삼각형 중심점으로 나눴더니 레일 경계에 톱니가 생겼다. 그래서 세 꼭짓점이 모두 테두리 영역일 때만 테두리로 칠하도록 바꿨다. 몸체 색도 1차보다 약 0.08 낮춰 하얗게 뜨지 않게 했다.

검증:
- Unity 배치 모드 종료 0, 로그 `SHUTTER_TRIAL_CARTOON_OK`. 테두리 삼각형은 5개 합계 3,260/15,850(셔터 하나 652/3,170)이다.
- 판독: 톱니가 없어졌다. 회색 상자 건물에서도 가게마다 다른 상가 1층으로 읽힌다. 셔터함 표면에 스캔 메시의 미세한 주름 음영이 남는다.

## 미검증·남은 항목

- 실제 은계 지도 건물에 배치하거나 게임을 실행해 보지는 않았다. 시험 씬은 게임 색을 흉내 낸 별도 장면이다.
- 게임에 정식으로 넣으려면 원본이 공개 저장소에 올라가지 않게 해야 한다. 저장소를 비공개로 돌리거나, 참고만 하고 메시를 직접 다시 만드는 방법이 있다.
- 셔터 카툰판의 색 조합 중 무엇을 쓸지는 정하지 않았다. 셔터함 주름을 없애려면 셔터함을 단순 상자로 바꿔야 한다.
- 라바콘·시선유도봉의 2색 재질 분리는 하지 않았다.

## 실행·재현

    blender -b --factory-startup -P tools/art/render_megascans_trial.py -- art-source/megascans-trial art-source/megascans-trial/trial-render.png

다른 PC에서는 README의 Fab 상품 주소에서 로그인 후 FBX Low를 받아 같은 이름의 폴더에 푼다.

Unity 시험은 셔터 폴더의 FBX·JPG를 `unity/EungyeTaxi/Assets/ArtTrial/MegascansShutter/`에 복사한 뒤 실행한다.

    Unity.exe -batchmode -quit -projectPath unity/EungyeTaxi -executeMethod MegascansShutterTrial.Run -logFile artifacts/logs/shutter-trial.log
