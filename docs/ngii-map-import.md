# 은계 지도 도입·재수입

현재 프로젝트는 **시흥 은계지구**를 사용한다. 정식 지도 `config/maps/project/eungye.project.json`에 기준 형상과 편집 문서를 포함하며, 보통의 편집·빌드는 이 파일로 수행한다. 재수입은 기존 편집 내용을 바꿀 수 있으므로 원본을 다시 생성해야 할 때만 수행한다.

## 원본과 좌표

2025년 NGII 안양022·023(37612022·37612023) 1:5,000 SHP ZIP·XML을 `data/raw/ngii/eungye`에 둔다. XML·PRJ에서 확인한 EPSG:5186 좌표에서 `[181900,537200,184000,538950]` 범위를 잘라 약 2.1×1.75km의 미터 단위 지도를 만든다. XML 프로젝트명과 실제 도엽 축척을 혼동하지 않는다. 도로 높이·입체 연결·현행 규제는 원본 평면 형상만으로 확정하지 않는다.

## 재수입 명령

```powershell
python tools/import_ngii_map.py inventory --raw data/raw/ngii/eungye --report data/inspection/ngii/packages.json
python tools/import_ngii_map.py build --config config/maps/eungye.json --report data/inspection/ngii/eungye-validation.json
```

설정의 `id`, `rawDirectory`, `output`, `bounds`, `requiredTiles`, CRS·검토 근거를 확인한다. 결과는 `unity/EungyeTaxi/Assets/Resources/eungye-map.json`이다. 공통 자르기·삼각분할·도로 샘플링은 `tools/map_geometry.py`를 사용한다. 같은 지역의 국도42 정밀 차로 원본은 `data/raw/ngii/hdmap/eungye`에서 `tools/import_hdmap.py`로 읽는다.

재수입 뒤 지도/연결/OSM 대조를 검증하고 `config/maps/local-input-manifest.json`의 실제 크기·SHA256과 배포 ZIP을 함께 갱신한다. 해시 오류를 무시하지 않는다. 정식 프로젝트 지도를 지우거나 다시 초기화하면 편집 내용이 사라질 수 있으므로 Git 이력을 보존한다.

## 다른 PC

일반 개발 빌드는 정식 프로젝트 지도만 있으면 된다. 재수입 개발용 생성 입력은 `distribution/maps/Eungye-map-inputs-2026-10-20.zip`을 저장소 루트에 푼다. 은계 배경·국도42 입력2개이며 원본 GIS는 별도 보관한다. `pwsh -File tools/check_workspace.ps1 -Strict -RequireRawInputs`로 확인한다.
