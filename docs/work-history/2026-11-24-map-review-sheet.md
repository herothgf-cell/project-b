# 2026-11-24 은계 지도 실측 대조와 수정 검토 시트 (조사·시안, 지도 미변경)

## 요구

사용자 요청(밤사이 진행, 확인 요청 없이 문의는 문서에 남김):
- 은계 카카오 로드뷰·항공뷰를 내부 브라우저로 열어 실제와 비교한다. 인도·횡단보도·도로 폭 조정 허용. 도로선이 자연스러워야 하고 실선·점선·노란선이 구분되어야 한다. 신호등·표지판이 차도 위에서 주행을 막으면 안 된다. 2차로인데 1차로로 잘못된 곳을 찾는다.
- 도시를 구역으로 나누고 M지도에 보이는 도로에 번호를 붙인다.
- 고칠 곳을 전(게임)/후(시안)로 정리한 HTML 시트를 만든다. 시트에서 승인·재검토 표시, 의견 글, 이미지 첨부, 시안 위 표시를 할 수 있어야 한다.

## 한 일

| 단계 | 방법 | 결과 |
|---|---|---|
| 게임 화면 | 검사용 빌드 `artifacts/hybrid-trial/EungyeTaxi.exe -singlePlayer -contentTest -dayLightDayProbe -mapAudit -mapAuditSettle 1 -mapAuditObserve 1` (저장 격리, 사용자가 켜 둔 배포 게임과 저장은 건드리지 않음) | 교차로 77곳 위 시점 화면, 적신호 진입 0·정체 0 |
| 실제 항공 | 새 `tools/kakao_aerial_compare.py`: 게임 좌표 → EPSG:5186 → WGS84 → 카카오 CONGNAMUL(베셀 TM, 측지계 변환 없음, 실측 보정 (1.79, 6.60) m) → 스카이뷰 L0 타일(0.125 m/픽셀). 보정값은 카카오 링크 변환 두 점(J7973, 서쪽 끝)에서 같은 값으로 나왔다 | 77곳 같은 중심·축척(가로 154 m, 북쪽 위) 항공사진. 겹쳐 보면 도로 가장자리가 약 1 m 안에서 맞는다 |
| 판정 | 하위 에이전트 4개가 77쌍을 하나씩 보고 W 폭 / L 선형 / M 선 종류 / F 차도 위 시설 / N 차로 수 / C 연결 / G 노면 / X 기타로 기록 | 277건(로드뷰 확인 필요 121건). 심각 12곳·보통 52·경미 19·일치 1 |
| 차도 위 기둥 자동 검사 | 같은 실행본 `-mapAuditDump "1050,875,2000"`로 모든 렌더러 위치를 뽑아, 높이 1.2 m 이상·굵기 0.8 m 이하 기둥이 차로(중심선 ± 폭/2) 안에 있는지 계산 | **기둥 16개가 차로 안**(유턴 조건 표지 받침대 15, 신호등 기둥 1). 차로 가장자리에서 0.2~1.7 m 안쪽 |
| 로드뷰 직접 확인 | 내장 브라우저 카카오맵(로드뷰 2026-06) | J866 은행사거리: 표지는 신호등 가로대에 매달리고 기둥은 모서리 인도에 있다. J043: 대은로 옆은 볼라드가 선 보행 광장이다(게임은 차도). J030 고가는 확인하지 못함 |
| 구역·번호 | 새 `tools/map_review_zones.py` → `config/maps/eungye-road-zones.json` | 구역 A~H 8개, 도로 146개 번호(예: C-01 수인로) |
| 시트 | 새 `tools/map_review_sheet.py`, `tools/map_review_html.py` | 항목 93개(교차로 77 + 기둥 P01~P16) |

## 결과 요약

- **차도 위 기둥(F, P01~P16)**: 11/19 점검에서 "공중에 뜬 유턴 표지"를 고칠 때 붙인 바닥 받침대가 차로 안에 섰다. 차로 한가운데라 3 m 안에 차로 밖 자리가 없는 곳이 대부분이다. 시안은 바닥 기둥을 없애고 신호등 가로대에 다는 것이다(로드뷰의 실제 방식).
- **실제와 다른 도로망(C)**: 은행로 블록(E 구역)과 H 구역에 많다. 보행 광장이 차도로 그려졌고, 실제 어린이보호구역 남북 도로가 빠졌으며, 없는 대각선 차도와 막다른 회차로가 있다(043·046·047·048·049·050·051·042). 원본 2020 측량 뒤 은계지구 정비로 바뀐 것으로 보인다.
- **노면(G)**: 횡단보도 누락, 어린이보호구역 황색 횡단보도·적색 포장 누락, 자전거 횡단도 누락이 가장 흔하다(105건).
- **폭(W)·차로 수(N)**: J866 동서 차도 약 8 m 넓음, 017 남북 도로 약 6 m 넓음, 077 서쪽 도로 서행 5차로(실제 3), 063·066 남행 3차로(실제 4).
- **고가(C)**: 030 J14455_13437은 실제 고가인데 게임은 평면 T자다(로드뷰 미확인).

## 산출물

- 검토 시트: `artifacts/map-review-20261124/sheet/index.html`(브라우저로 바로 열기, 서버 불필요). 다른 PC용 묶음은 `artifacts/map-review-20261124/map-review-sheet-20261124.zip`. 카카오 항공·로드뷰 이미지가 들어 있어 Git에는 넣지 않는다(11/20 계획과 같은 기준).
- 시트 기능: 구역 지도(점 누르면 이동), 구역·유형·판정·상태·글자 필터, 항목별 승인/다시 검토/보류/반려와 세부 항목별 판정, 의견, 이미지 첨부(끌어다 놓기·붙여넣기·파일), 시안 위 펜·화살표·상자·글자 표시(되돌리기, 표시 포함 PNG 저장), 질문 Q1~Q5 답변란. 내용은 브라우저 localStorage에 자동 저장되고 "검토 결과 내보내기"로 JSON을 받는다.
- 원자료: `artifacts/map-review-20261124/`의 `game/`(게임), `kakao/`(항공·pairs.json), `pair/`(격자 대조), `judge/`(판정 JSON), `poles-near-lanes.json`, `dump/`, `overview/zones.png`.

## 검증·한계

- 게임 화면은 위치 제어 고정 화면이다. 자동 물리 주행, 사람 플레이, 전체 캠페인 검사는 하지 않았다.
- 판정은 항공사진 비교이며 하위 에이전트 판정이다. 기둥 16개는 좌표 계산 근거라 확실도가 높다. 내가 직접 다시 본 것은 J043·J051과 P03이다. 나머지 판정, 특히 "로드뷰 확인 필요" 121건은 고치기 전에 로드뷰로 다시 본다.
- 항공사진 촬영 시점은 확인하지 못했다(로드뷰는 2026-06). 게임 원본은 2020 측량이다.
- 시트 HTML은 내장 브라우저(로컬 서버)에서 열어 그리기 저장, 증거 이미지, 콘솔 오류 0을 확인했다. `file://`로 직접 여는 경우는 열어 보지 않았다(같은 코드, 외부 요청 없음).
- 지도·게임 코드·빌드·배포 manifest는 바꾸지 않았다.

## 변경 파일

- 추가: `tools/kakao_aerial_compare.py`, `tools/map_review_zones.py`, `tools/map_review_sheet.py`, `tools/map_review_html.py`, `config/maps/eungye-road-zones.json`, 이 기록
- 갱신: `docs/work-history/LATEST.md`

## 재현

```
python -m pip install --target artifacts/py314-deps pyproj pillow numpy
artifacts/hybrid-trial/EungyeTaxi.exe -singlePlayer -contentTest -dayLightDayProbe -mapAudit -mapAuditSettle 1 -mapAuditObserve 1 -mapAuditOutput artifacts/map-review-20261124/game
artifacts/hybrid-trial/EungyeTaxi.exe -singlePlayer -contentTest -dayLightDayProbe -mapAudit -mapAuditDump "1050,875,2000" -mapAuditOutput artifacts/map-review-20261124/dump
PYTHONPATH=artifacts/py314-deps python tools/kakao_aerial_compare.py artifacts/map-review-20261124/game artifacts/map-review-20261124/kakao
PYTHONPATH=artifacts/py314-deps python tools/map_review_zones.py artifacts/map-review-20261124/overview/zones.png artifacts/map-review-20261124/game/map-audit.txt
(차도 위 기둥 계산 → poles-near-lanes.json, 판정 judge/*.json 준비 후)
PYTHONPATH=artifacts/py314-deps python tools/map_review_sheet.py
PYTHONPATH=artifacts/py314-deps python tools/map_review_html.py
```

차도 위 기둥 계산은 이번에 인라인 스크립트로 했다. 다음 작업에서 `tools/`로 옮기면 좋다.

## 다음 작업

1. 사용자 검토 JSON을 받아 승인 항목만 고친다. 우선순위: P01~P16 기둥(경로·신호 영향 없음, 바로 가능) → 횡단보도·보호구역 노면 → 폭·차로 수 → 도로망(C) 순서.
2. 고칠 때마다 `-districtTrafficTest`, 실행본 회귀, 위 대조를 다시 돌린다.
