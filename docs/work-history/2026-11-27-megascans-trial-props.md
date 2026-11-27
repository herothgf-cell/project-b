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

## 미검증·남은 항목

- Unity 반입·게임 화면 확인은 하지 않았다. 반입하면 `Assets/`도 공개 저장소에 올라가므로, 저장소 공개 범위 또는 직접 다시 만든 메시 사용을 먼저 정해야 한다.
- 라바콘·시선유도봉의 2색 재질 분리는 하지 않았다.

## 실행·재현

    blender -b --factory-startup -P tools/art/render_megascans_trial.py -- art-source/megascans-trial art-source/megascans-trial/trial-render.png

다른 PC에서는 README의 Fab 상품 주소에서 로그인 후 FBX Low를 받아 같은 이름의 폴더에 푼다.
