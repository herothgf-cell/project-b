# 은계 택시 시뮬레이터

Unity 2022.3.62f2 / Windows 64비트용 3D 택시 운전 시제품입니다. 키보드·패드 운전, 1·3인칭 시점, 실제 지도 기반 도로, 일반 차량과 신호, 호출·길가 승객, 내비게이션, 매너 점수, 기간 수익 목표와 하루 정산을 구현합니다.

## 개발 환경 및 실행

1. Unity Hub에서 **Unity 2022.3.62f2**와 Windows Build Support를 설치합니다.
2. 아래 지도 데이터 준비 절차를 완료합니다. 지도 데이터는 이 저장소에 포함하지 않습니다.
3. Unity Hub에서 `unity/YeongtongTaxi`를 엽니다. 첫 실행은 아래 은계 빌드를 먼저 수행해 생성 지도와 보완 설정을 준비합니다.
4. `Taxi > Build Eungye Hybrid` 메뉴로 빌드하면 `builds/EungyeTaxi/EungyeTaxi.exe`가 생성됩니다. 현재 은계 실행은 `Play-Eungye.cmd`를 사용합니다.

조작법과 게임 규칙은 [Unity 프로젝트 안내](unity/YeongtongTaxi/README.md)를 참고하세요. 현재 캠페인은 **3일·하루 8분·순수익 30,000원**이며 보험 갱신·세금 납부까지 완료해야 성공합니다. 비용은 게임용 초기 균형값입니다. 최신 화면·저장·검증은 [통합 마무리 기록](docs/prototype-completion-validation.md)을 참고하세요.

보조 운전 시제품은 `builds/AssistedTaxi/AssistedTaxi.exe`, 가상 도로 시험장은 `Play-RoadRulesTrial.cmd`로 실행합니다. 시작 시 3인칭 보조 운전이며, C로 시점을 바꾸면 1인칭은 수동·3인칭은 보조 운전을 기본 적용합니다. H로 현재 운전 모드를 변경할 수 있습니다. 보조 모드에서도 가감속·신호 판단은 직접 하며, A/D는 차선 변경, Q/E는 좌우회전 예약입니다. 차로 밖·후진 등 보조 시작이 불가능한 상태에서는 수동을 유지하고 이유를 표시합니다.

## 교통 판단과 경로 선택

### 은계 통합 영업 지도

은계 실행기는 1600×900 창으로 시작하며 창 크기를 조절할 수 있습니다. **F11**로 모니터 전체화면과 창 모드를 전환합니다. 중앙 내비게이션·신호 안내는 화면 아래로 옮겨 전방 신호등 시야를 확보했습니다. 임시 신호등 기둥은 연결된 모든 도로 영역과 건물 외곽을 검사해 배치하며, 실제 설치 위치를 확인했다는 의미는 아닙니다.

`config/maps/eungye-scene-overrides.json`에는 생성된 주행 차로와 충돌하는 건물 22개의 임시 숨김 목록을 기록했습니다. 렌더링과 충돌체만 제외하며 원본 건물 도형은 보존합니다. 실제 도로 폭·차로 경로·건물 외곽 대조 후 목록을 수정하고 다시 빌드하세요. 이것은 현장 검증을 대신하는 보정이 아닙니다. `-eungye -visibilityTest`는 현재 주행 차로의 건물 충돌 표본 검사와 1280/1600 화면 캡처를 `artifacts`에 남깁니다.

`Play-Eungye.cmd`로 통합 실행 파일을 실행합니다. 기존 은계 지도 범위 약 **2.1 × 1.75km**에서 지역 도로와 국도 42호선 정밀 차로를 하나의 경로망으로 결합했습니다. 승하차 지점 12곳에서 호출 선택과 길가 승객 운송, 요금·매너·하루 정산을 이용합니다. 1~3으로 호출 선택, F로 승하차, H로 조향 보조, W/S/Space로 가감속·제동, Tab으로 빠른 길/짧은 길을 선택합니다. 호출 전 H는 자유 주행 경로를 만들고, 호출을 받으면 승객 경로로 전환합니다.

오른쪽 상태창은 **국도42 원본 차로 / 임시 차로·교차로 / 임시 접속 구간**과 설정 ID를 표시합니다. 원본도 현행 규제 검증 자료는 아닙니다. 지역 차로 수·회전 연결·신호·승하차 위치는 시제품 설정이며, 실제와 대조한 뒤 보정해야 합니다. 끊어진 도로·자료 경계까지 모두 연결한 것은 아니며, 영업 지점은 왕복 경로가 확인된 도로망에 배치합니다. 일반 차량 24대는 같은 통합 차로망을 이용해 지역별로 분산 운행하며, 목적지에 도착하면 다음 경로로 이어집니다. 건물과 겹치거나 후속 운행이 불가능한 경로는 차량 배정에서 제외합니다. 교차로마다 회전 방향·감속·깜빡이·앞차 추종·출구 공간 확인을 적용하고, 설정된 신호에서는 정지선 대기 후 출발합니다. 임시 무신호 교차로는 진입 순서에 따라 교차 영역을 예약합니다. 원본 지도만으로 현재 신호 운영을 확인할 수 없는 곳에 실제 신호를 임의로 확정하지 않습니다.

설정은 `config/maps/eungye-traffic-overrides.json`에서 관리합니다. `data/inspection/ngii/eungye-junction-catalog.json`에서 화면 ID와 일치하는 교차로 항목을 복사하여 `junctions` 배열에 넣고 수정하세요. 원본 파일은 수정하지 않습니다.

주요 교차로 20곳(사거리 17곳·세 갈래 3곳)에 임시 신호등·횡단보도·정지선을 배치했습니다. 시설은 실제 연결된 77개 진입 방향에만 생성됩니다. 일반 차량은 같은 신호 판정에 따라 직진·보호 좌회전 대기하며, 우회전은 정지 후 교차 교통을 확인합니다. 신호 주기는 게임용 임시 설정입니다. 이번에 추가한 시설은 모두 현장 미확인입니다. 신호등 표지·우측 상태창·미니맵에서 주황색은 임시, 초록색은 근거를 등록한 현장 확인 시설을 뜻합니다. 원본 차로라는 표시와 시설의 확인 상태는 별개입니다.

- `crosswalk`, `stopLine`: 횡단보도·정지선 생성 여부.
- `approaches`: 실제 연결된 진입 방향의 `dx`, `dz`와 도로 전체 폭 `width`(m). 없는 방향에 시설을 추가하지 않습니다.
- `facilityStatus`: `provisional` 또는 `verified`. 확인 상태로 바꾸려면 `facilityEvidence`에 확인 근거, `facilityCheckedAt`에 `YYYY-MM-DD` 확인일을 함께 기록해야 합니다. 시설 설정 변경 후 다시 빌드하세요.

- `allowLeft`, `allowRight`, `allowStraight`: 해당 임시 교차로의 허용 방향. 기존 항목 전체를 복사한 뒤 바꿉니다.
- `signalMode`: 0은 신호 없음, 1은 비보호, 2는 보호 신호 시제품. `dx`, `dz`는 주 신호축 방향입니다. 실제 신호 주기 재현은 별도 보정 대상입니다.
- `roads`: `{ "roadId": "원본 도로 ID", "lanes": 4 }` 형식으로 양방향 합계 차로 수를 지정합니다(2·4·6·8). 국도42 원본 차로에는 적용하지 않습니다.
- `disabledPortals`: 사용하지 않을 임시 접속 ID 문자열 목록. 원본 차로의 좌표와 기존 연결은 그대로 보존합니다.

두 배경 자료를 준비한 뒤 Unity 메뉴 **Taxi > Build Eungye Hybrid**를 실행하면 설정 반영·연결 검사 후 `builds/EungyeTaxi/EungyeTaxi.exe`를 생성합니다. 명령행은 `-executeMethod PrototypeBuild.BuildHybrid`입니다. 생성 결과는 `data/inspection/ngii/eungye-hybrid-summary.txt`, 실행 파일의 `-hybridTest -batchmode` 검증 결과는 `artifacts/hybrid-drive-test.txt`에 기록됩니다. 일반 차량 검사는 `-eungye -districtTrafficTest -batchmode -nographics`로 실행하며 `artifacts/district-traffic-test.txt`에 기록됩니다.

### 국도 42호선 실제 차로 시험

`Play-Route42.cmd`를 실행합니다. 별도 실행 파일은 `builds/Route42Taxi/Route42Taxi.exe`이며 `-route42` 인자가 필요합니다. F6/F7은 R42-A 좌/우회전, F8/F9는 R42-B 좌/우회전, F10은 점선 차로 변경입니다. H를 켜면 내비게이션과 같은 실제 차로 경로를 조향하며, **W/S/Space 가감속·제동은 직접** 합니다. A/D는 목적지와 연결되는 검증된 변경 구간을 요청하고 Q/E는 가능한 회전을 요청합니다. R은 선택한 시험을 재시작합니다. 초록 도착 지점에서 직접 정차합니다.

2025 패키지의 국도42호선 SEC002에서 완전한 원본 링크 309개를 사용합니다. 노드 ID 연결을 보존하고, 상호 인접 ID와 백색 단선 점선(212)을 확인한 49개 방향별 변경 경로만 추가했습니다. 기존 은계·영통 영업 모드와 저장은 유지됩니다. **이 모드는 차로 형상·경로·조향 시험이며 일반 차량, 실시간 신호 운영과 승객 영업은 제외합니다.** 곁도로는 회전 직후 자료가 끝나므로 시험도 해당 범위에서 끝납니다. 원본 갱신일은 2020/2022이며 현행 규제 보증이 아닙니다.

재생성: 은계 배경 지도를 먼저 준비한 뒤 `pyshp`, `pyproj`를 설치하고 `python tools/import_hdmap.py`를 실행합니다. 원본 ZIP은 `data/raw/ngii/hdmap/eungye`에 둡니다. `artifacts/geo-inspection-deps`에 로컬 설치한 라이브러리도 인식합니다. Unity 명령행의 `-executeMethod PrototypeBuild.BuildHd`로 빌드하고, 생성한 플레이어에 `-hdTest -batchmode`를 전달하면 물리 검사를 실행합니다. 기록은 `data/inspection/ngii/route42-validation.json`과 `artifacts/hd-drive-test.txt`에 저장됩니다.

일반 운송은 `Play-Taxi.cmd` 또는 `builds/YeongtongTaxi/YeongtongTaxi.exe`로 실행합니다. **Tab / 내비게이션 버튼**으로 빠른 길과 짧은 길을 선택합니다. 정체·비상 정차 차량과 느린 일반 차량을 반영해 예상 시간을 비교하고 경로를 재탐색합니다. 영업 요금은 교통 상황과 무관한 거리 기준을 유지합니다. Esc 메뉴에 지역 자료 준비 여부와 다음 실행 지역을 표시합니다. 시흥 은계는 `Play-Eungye.cmd`로 바로 실행합니다. 2025년 안양022·023 원본에서 약2.1×1.75km를 구성했으며 현행 신호 운영과 입체교차는 별도 보완 대상입니다.

## 지도 데이터 준비

원본 NGII 자료, 추출 도형, 변환된 Unity 지도, 빌드 결과 및 설치 프로그램은 Git 추적에서 제외합니다. 필요한 지도 자료를 별도로 확보한 개발 환경에서 실행하세요. 실제 원본은 2008년 수원2296(377092296) NGI/NDA와 2025년 안양022·023(37612022·37612023) SHP ZIP입니다. 도입 도구는 좌표계·범위·출처를 검증한 자료를 결합합니다. 영통 인접 도엽은 아직 추가 자료가 필요합니다.

1. 영통은 NGI·NDA·XML 세트를 `data/raw/ngii` 하위에 압축 해제합니다.
2. Python 3.10 이상에서 `python tools/import_ngii_map.py inventory --raw data/raw/ngii --report data/inspection/ngii/packages.json`을 실행합니다.
3. `python tools/import_ngii_map.py build --config config/maps/yeongtong.json --report data/inspection/ngii/yeongtong-validation.json`으로 Unity 지도를 생성합니다.
4. 은계 SHP ZIP·XML은 `data/raw/ngii/eungye`에 두고 `python tools/import_ngii_map.py build --config config/maps/eungye.json --report data/inspection/ngii/eungye-validation.json`을 실행합니다. ZIP은 원본 그대로 읽습니다.
5. 인접 도엽과 은계의 다운로드 범위·좌표 검증·현장 보정은 [지도 추가 안내](docs/ngii-map-import.md)를 참고하세요. 기본 도입 도구는 표준 라이브러리만 사용합니다. 별도 원본 이미지 검사는 기존 `inspect_ngii.py`와 Pillow를 사용합니다.

출처와 데이터 해석 한계는 [원본 검토](docs/ngii-sample-review.md) 및 [지도 도입안](docs/map-data-plan.md)에 기록했습니다. 현재 도로·신호·교통 법규를 그대로 재현하는 제품은 아닙니다.

## 검증

- Python 지도 검사: `python -m unittest discover -s tests -v` (지도 데이터 준비 후)
- C# 독립 검사: `tests` 폴더의 각 검사 파일을 대응하는 `Assets/Scripts` 계산 클래스와 함께 C# 컴파일러로 빌드하여 실행
- Unity Player 검사: 실행 인자 `-smokeTest`, `-trafficTest`, `-jobsTest`, `-stabilityTest`, `-cockpitTest`, `-flowTest`, `-economyTest`, `-turnTest`, `-junctionTurnTest`, `-greenFlowTest`, `-navigationChoiceTest`, `-roadConditionsTest`, `-roadIndexTest`, `-eungyeTest`

검사 결과·캡처는 로컬 `artifacts`에 기록합니다. 검사 실행은 실제 플레이 저장 파일을 변경하지 않습니다. 설계와 기능별 검증 기록은 `docs`에 있습니다.
