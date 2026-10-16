# NGII 도엽 추가와 지도 생성

확인일: 2026-10-15. `tools/import_ngii_map.py`는 원본 NGI·NDA·XML 또는 SHP ZIP·XML을 직접 읽는다. ZIP은 메모리에서 읽으며 원본을 변경하거나 파일을 자동 해제하지 않는다. `inspect_ngii.py` 실행이나 `native-features.json` 캐시가 필요 없다. 기존 `export_drive_map.py`와 기존 테스트도 그대로 사용할 수 있다.

현재 실제 확보된 자료는 수원2296 / **377092296 / 2008 / 1:1,000** 및 안양022·023 / **37612022·37612023 / 2025 / 1:5,000**이다. 은계는 사용자가 추가한 두 SHP 패키지로 생성된다. 영통 인접 도엽은 아직 미확보다. [취득 확인 기록](map-acquisition-check.md), [영통 원본 검토](ngii-sample-review.md), [은계 원본 검토](eungye-source-review.md)를 따른다.

## 반복 실행

Python 3.10 이상, 표준 라이브러리만 필요하다. 저장소 루트에서 실행한다. 아래 `python`은 설치된 Python 실행 파일로 바꿀 수 있다. Windows 앱 실행 별칭이 동작하지 않으면 Codex 번들 Python의 절대 경로를 사용한다.

```powershell
python tools/import_ngii_map.py inventory --raw data/raw/ngii --report data/inspection/ngii/packages.json
python tools/import_ngii_map.py build --config config/maps/yeongtong.json --report data/inspection/ngii/yeongtong-validation.json
python -m unittest discover -s tests -p test_ngii_import.py
```

`inventory`는 하위 폴더까지 검색해 같은 기본 파일명의 `.ngi`, `.nda`, `.xml` 또는 `.zip`, `.xml`을 묶는다. 도엽번호는 파일명 추측 대신 XML에서 읽고 SHP는 H0010000 도곽 속성과도 대조한다. 각 파일의 크기·SHA-256과 제작년도·축척·좌표계 표기, 누락 파일을 기록한다. ZIP은 안전한 멤버 이름·중복·용량을 검사하고 압축 상태로 읽는다. NGI/NDA는 CP949, DBF는 동반 CPG의 EUC-KR 등 명시 인코딩을 사용한다. `ngii_shapefile.py`는 표준 라이브러리만으로 필요한 2D PolyLine/Polygon SHP·SHX·DBF의 길이·레코드 수·인덱스·유한 좌표를 검증한다.

`build`의 출력은 설정의 `output` 경로다. `--raw`, `--output`으로 입력과 출력을 지정할 수 있다. 설정 내부 상대 경로는 저장소 루트 기준, CLI 상대 경로는 실행 디렉터리 기준이다. 필수 자료가 없거나 검증에 실패하면 종료 코드 2, 실패 사유가 포함된 보고서를 남기며 기존 지도 파일을 교체하지 않는다. 성공한 지도는 임시 파일에 완성한 뒤 교체한다.

## 영통 확장과 은계

```powershell
python tools/import_ngii_map.py build --config config/maps/yeongtong-expanded.json --report data/inspection/ngii/yeongtong-expanded-validation.json
python tools/import_ngii_map.py build --config config/maps/eungye.json --report data/inspection/ngii/eungye-validation.json
```

영통 확장 명령은 현재 **의도적으로 실패**한다. 확장 설정은 주변 목록에서 확인한 377092297·377130206·377130207을 추가 필수 도엽으로 명시한다. 실제 도곽을 받은 뒤 남쪽 시험 구역에 필요한 조합을 확정하고 `requiredTiles`를 조정한다. 후보 목록을 실제 커버리지 검증으로 간주하지 않는다.

은계 명령은 `data/raw/ngii/eungye`의 안양022·023 / **37612022·37612023 / 2025 / 1:5,000** ZIP·XML로 `eungye-map.json`을 생성한다. 확인된 EPSG:5186에서 `[181900,537200,184000,538950]` 범위를 잘라 **2.1×1.75km** 지도를 만든다. XML 프로젝트명에 포함된 ‘1대 1000’을 도엽 축척으로 오인하지 않는다. 실제 denominator는 5000이다. 은계는 영통과 별도 원점·별도 파일을 가진다.

### 사용자가 요청한 넓은 은계 다운로드 범위

2026-10-15 사용자 확인: 은계호수공원 서남측부터 시작하되 주변까지 넓혀 수집한다. 다운로드 선택 영역은 은계지구 전체, 시흥대야역·신천역 주변, 검바위초등학교 주변, 시흥IC 주변 일반도로를 포함한다. 실제 플레이 구현 순서는 호수공원 서남측–은계중앙로–은계호수로에서 시작한다. 고속도로 본선 구현까지 약속하는 범위는 아니다.

국토정보플랫폼에서 이 지점들을 포함하는 영역과 교차하는 최신 1:5,000 수치지도 V2를 모두 받는다. 도곽 경계의 인접 도엽도 포함하고, 1:1,000 자료가 검색되면 별도로 확보한다. 현재 확인된 37612023 외 추가 도엽번호는 영역 검색 결과와 받은 XML로 확정한다. ZIP 원본과 압축 해제한 NGI·NDA·XML은 `data/raw/ngii/eungye/` 하위에 보관한다. 확대 영역의 원본 목록을 확인한 뒤 `config/maps/eungye.json`의 `requiredTiles`와 실제 좌표계 검토 기록을 보완한다.

동일 도엽의 여러 연도/패키지가 있으면 모호성 오류를 낸다. `requiredTiles` 항목의 `package`에 입력 디렉터리 기준 `.ngi` 또는 `.zip` 경로를 지정해 선택한다. 선택 기록과 파일 해시가 출력에 남는다.

## 좌표 검증

GRS80은 이 자료에서 완전한 투영 좌표 정의가 아니다. 여러 도엽을 결합하려면 **각** `requiredTiles[].crs`에 다음을 기록해야 한다.

```json
{
  "status": "verified",
  "definition": "검증한 전체 CRS 정의 또는 정확한 EPSG 식별자",
  "units": "metre",
  "evidence": "실제 원본 메타데이터/공식 좌표 정의, 검토일, 대조 기준점"
}
```

`definition` 값은 예시 문구로 채우지 않는다. 설정은 검토자의 확인 기록이며 프로그램이 그 진위를 자동 증명하지 않는다. 정의 문자열이 모두 같아야 하고 XML의 좌표계 표기도 일치해야 한다. 정의가 다른 자료의 재투영은 이 도구가 수행하지 않는다. 별도 검증된 변환 후 같은 기준의 패키지를 준비한다.

현재 수원2296만 `provisional`로 허용했다. 단일 도엽 안에서 원점을 빼는 평행 이동만 하고 좌표 단위를 임시로 미터로 해석한다. 완전한 EPSG를 선언하지 않는다. `provisional`을 여러 도엽 결합에 사용할 수 없다.

은계는 XML의 EPSG:5186 및 모든 SHP 레이어의 동일한 PRJ WKT를 함께 확인했다. 설정의 `crs.wkt`를 실제 PRJ와 정확히 비교한다. 공급 XML은 181행에 닫는 태그 오류가 있어 원본을 고치지 않고 유일한 도엽번호·제작년도·축척 분모·EPSG 값만 좁게 읽는다. `metadataReview`에 명시적인 검토 근거가 있어야 가져올 수 있으며 경고를 출력·검증 보고서에 남긴다. 임의 EPSG 추정이나 좌표 재투영은 없다.

H0010000 도곽으로 기본 범위를 정한다. 결합 도엽의 경계 상자가 2m 허용 범위 내에서 하나의 연결 집합을 이뤄야 한다. 이는 먼 지역을 실수로 결합하지 않는 1차 검사다. 실제 도로 연결·중복 면·도곽 사이 틈의 완전한 판정은 아니다. 선택적 `bounds: [minX,minY,maxX,maxY]`는 결합 경계 상자 안의 하위 범위만 허용한다.

## 검토한 보정과 지명

`reviewedOverrides`는 원본 레코드 `도엽:레이어:ID`를 참조한다. `status: verified`와 비어 있지 않은 `evidence`가 있어야 반영한다. 그 외 상태는 보고서의 `ignoredOverrides`에 남기고 적용하지 않는다. 필드 오타, 존재하지 않는 ID, 중복 보정은 오류다. 원본 `sourceLanes`는 항상 보존하며 보정 차로수는 `lanes`에 기록한다.

```json
{
  "feature": "도엽번호:A0020000:레코드ID",
  "status": "verified",
  "evidence": "현장/공식 자료의 위치, 확인일, 정점 순서 대비 방향 확인 근거",
  "reviewedAt": "YYYY-MM-DD",
  "lanes": 2,
  "oneWay": -1,
  "name": "검증한 도로명"
}
```

위 내용은 형식 예시이며 실제 도로에 관한 주장이 아니다. `oneWay`는 원본 정점 순서를 기준으로 +1 순방향, -1 역방향, 0 양방향이다. 원본의 ‘양방향’·‘양방통행’을 0으로 읽는다. 다른 속성값을 정점 방향에 대응하는 방법이 확인되지 않았으면 기본 설정은 오류다. 은계에는 ‘일방통행’ 레코드가 있어 명시한 `roadPolicy.unresolvedDirections: exclude`로 해당 도로를 제외하며 양방향으로 바꾸지 않는다. 이 정책과 폭 6m 미만·고속국도 제외는 게임의 평면 지역도로 범위 설정이며 통행 허용을 판정한 것이 아니다. 근거와 제외 레코드를 보고서에 남긴다. **현재 교통 규제를 현장 확인했다는 의미가 아니다.**

건물 `name`에는 NDA의 `주기`를 보존한다. 별도 `landmarks`는 검증 상태와 근거가 있는 항목만 위치를 표시한다. `feature`를 지정하면 원본 형상의 경계 상자 중심을 라벨 위치로 사용한다. 이는 출입구가 아니다. 또는 `sourceX`, `sourceY`를 명시한다. 현 설정의 호텔심포니·삼성여성병원은 **2008 원본에 기록된 이름**만 확인했으며 화면 이름에도 연도를 붙인다. 현재 영업이나 동일 명칭의 존속을 주장하지 않는다.

`targets`는 `name`, `sourceX`, `sourceY`, `leftTurnMode`, `status`, `evidence`를 가진다. 영통·은계 T1~T3는 각각 원본에서 위치를 확인한 `gameplay-provisional` 훈련 지점이다. 임의의 실제 좌회전 금지 규제는 추가하지 않았다. 정류점은 첫 훈련 지점과 같은 도로 연결 성분에서 최대 8개를 도로와 건물 배치로 자동 선정한다. 게임용 위치이며 실제 승하차 허용 장소가 아니다.

## 출력과 보고서

Unity 호환 필드: `roads{id,width,sourceLanes,oneWay,name,points{x,z}}`, 선택 `lanes`; `buildings{id,height,name,points,triangles}`; `stops{name,x,z,dx,dz,width}`; `targets{name,x,z,leftTurnMode}`; `landmarks{name,x,z,kind,status,evidence}`; `areas{id,name,kind,points,triangles,evidence,status}`. 은계의 물 영역은 실제 E0052114 ‘소래저수지’ 단일 링을 사용한다. 도로 ID는 잘린 조각까지 구분하도록 `도엽:레이어:ID:조각번호`를 사용한다. SHP ID는 1부터 시작하는 원본 레코드 순번이며 UFID도 출처에 보존한다. 다른 도엽의 같은 ID를 덮어쓰지 않는다.

`sources`에는 패키지 메타데이터·해시·도곽·레이어 수, `coordinateStatus`와 `crsReview`에는 좌표 검토 기록이 들어간다. 전체 지도를 하나의 원점으로 평행 이동한다. 도로선은 최대 15m 간격으로 세분하고 경계 상자로 자른다. 건물은 단일 링만 삼각분할하며 복수 링/홀과 바깥 경계에 걸친 건물은 이유를 기록하고 제외한다. 높이는 층수×3m 가정이다.

검증 보고서는 도형 수, 누락·제외 건물, 적용/미적용 보정, 그래프 연결 성분, 같은 정점 배열의 중복 도로 수를 담는다. 중복을 자동 삭제하지 않고 경계 도로를 임의로 스냅하지 않는다. 연결 성분 검사는 0.001m 반올림 좌표를 쓰는 진단이며 교통 방향·합법적 회전·도로 표면 충돌 검사가 아니다. 실제 플레이 검증은 별도다.

2026-10-15 실행 결과: 영통 원본 1,942개 도형에서 **232개 도로, 303개 건물, 8개 게임 정류점**, T1~T3, 과거 자료 기준 지명 2개를 생성했다. 추가 확보한 은계는 선택한 원본 레이어 21,099개 도형에서 **580개 도로, 3,387개 건물, 8개 게임 정류점**, T1~T3, 지명 4개와 소래저수지 물 영역 1개를 생성했다. 은계의 8개 정류점은 T1~T3와 같은 연결 성분에 있으며 56개 순서 있는 정류점 쌍의 구조적 연결 검사를 통과했다. 원본과 파생 지도는 Git 추적 대상에서 제외한다. 영통 확장은 계속 추가 도엽 확보가 필요하다.
