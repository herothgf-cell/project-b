# 은계 택시 시뮬레이터

**시흥 은계지구 지도만 사용하는 Unity 2022.3.62f2 / Windows 프로젝트입니다.**

## 실행과 Unity 열기

- 게임: `Play-Eungye.cmd`, `Play-Taxi.cmd` 또는 `builds/EungyeTaxi/EungyeTaxi.exe`.
- 개발: Unity Hub에서 **`unity/EungyeTaxi`** 폴더를 열고 **Taxi → Build Eungye Hybrid**로 빌드합니다.
- 3D 지도 편집: **Taxi → 프로젝트 지도 3D 편집 → 프로젝트 지도 열기**. [편집 사용법](docs/unity-map-editor-guide.md).
- 게임 조작·저장: [Unity 프로젝트 안내](unity/EungyeTaxi/README.md). 현재 개발 범위와 기획은 [통합 기획](docs/prototype-scope.md), [체크리스트](docs/prototype-development-checklist.md), [최신 인계](docs/work-history/LATEST.md)를 따릅니다.

## 지도 데이터와 다른 PC

정식 지도는 **`config/maps/project/eungye.project.json`**입니다. 기준 지도와 편집한 건물·보도·도로·주차장·식당/휴식 부지를 함께 저장합니다. 이 파일을 커밋·푸시한 뒤 다른 PC에서 pull하고 빌드하면 수정 내용이 반영됩니다. 빌드된 실행 폴더도 Git에 포함하므로 게임만 실행할 때는 pull 후 exe를 열면 됩니다.

정식 지도만 있으면 원본 GIS나 재수입용 지도 ZIP 없이 빌드할 수 있습니다. 재수입 개발에만 `distribution/maps/Eungye-map-inputs-2026-10-20.zip`을 저장소 루트에 풉니다. ZIP은 은계 배경과 같은 지역의 국도42 검사용 입력 2개를 포함합니다. [PC 이동·입력 준비](docs/work-history/map-transfer-guide.md).

원본을 다시 가져오려면 2025년 NGII 안양022·023 SHP ZIP·XML을 `data/raw/ngii/eungye`에 두고 다음 명령을 실행합니다.

```powershell
python tools/import_ngii_map.py build --config config/maps/eungye.json --report data/inspection/ngii/eungye-validation.json
```

국도42 정밀 차로 원본은 `data/raw/ngii/hdmap/eungye`에서 `tools/import_hdmap.py`로 가져옵니다. 기본 은계 지도는 약 2.1×1.75km이며 원본 형상과 게임용 신호·주행 연결·시설을 구분합니다. [지도 도입 안내](docs/ngii-map-import.md).

## 검증

```powershell
pwsh -File tools/check_workspace.ps1 -Strict
pwsh -File tools/test_prototype.ps1
python -m unittest discover -s tests -v
pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly
```

Unity 지도 검사는 `EungyeOnlyTests.Run`, `ProjectMapBuild.RunTests`를 사용합니다. 검증 결과와 수동 확인이 남은 항목은 작업 이력에 기록합니다.

## 은계 통합 영업 지도

은계 지역 도로·건물·보도·국도42 차로를 하나의 영업 지도에서 사용합니다. 지도 형상과 주행 차로 연결은 각각 편집·검사하며, 정식 JSON에 함께 저장합니다.
