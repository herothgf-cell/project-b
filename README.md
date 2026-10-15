# 영통 택시 시뮬레이터

Unity 2022.3.62f2 / Windows 64비트용 3D 택시 운전 시제품입니다. 키보드·패드 운전, 1·3인칭 시점, 실제 지도 기반 도로, 일반 차량과 신호, 호출·길가 승객, 내비게이션, 매너 점수, 기간 수익 목표와 하루 정산을 구현합니다.

## 개발 환경 및 실행

1. Unity Hub에서 **Unity 2022.3.62f2**와 Windows Build Support를 설치합니다.
2. 아래 지도 데이터 준비 절차를 완료합니다. 지도 데이터는 이 저장소에 포함하지 않습니다.
3. Unity Hub에서 `unity/YeongtongTaxi`를 열고 `Assets/Scenes/FirstDrive.unity`를 Play합니다.
4. `Taxi > Build Windows Prototype` 메뉴로 빌드하면 `builds/YeongtongTaxi/YeongtongTaxi.exe`가 생성됩니다.

조작법과 게임 규칙은 [Unity 프로젝트 안내](unity/YeongtongTaxi/README.md)를 참고하세요. 경제 목표는 기본 7일·하루 10분·누적 순수익 100,000원이며 게임용 초기 균형값입니다.

보조 운전 시제품은 `builds/AssistedTaxi/AssistedTaxi.exe`, 가상 도로 시험장은 `Play-RoadRulesTrial.cmd`로 실행합니다. 시작 시 3인칭 보조 운전이며, C로 시점을 바꾸면 1인칭은 수동·3인칭은 보조 운전을 기본 적용합니다. H로 현재 운전 모드를 변경할 수 있습니다. 보조 모드에서도 가감속·신호 판단은 직접 하며, A/D는 차선 변경, Q/E는 좌우회전 예약입니다. 차로 밖·후진 등 보조 시작이 불가능한 상태에서는 수동을 유지하고 이유를 표시합니다.

## 지도 데이터 준비

원본 NGII 자료, 추출 도형, 변환된 Unity 지도, 빌드 결과 및 설치 프로그램은 Git 추적에서 제외합니다. 필요한 지도 자료를 별도로 확보한 개발 환경에서 실행하세요. 현재 변환기는 2008년 수원2296 도엽(377092296)에 맞춘 시제품이며 다른 지역을 바로 대입할 수 없습니다.

1. 해당 도엽의 NGI·NDA·XML 파일을 `data/raw/ngii`에 배치합니다.
2. Windows에서 Python 3와 Pillow를 준비합니다 (`python -m pip install Pillow`). 검사 도구는 Windows 맑은 고딕을 사용합니다.
3. 프로젝트 루트에서 `python tools/inspect_ngii.py`를 실행하여 `data/inspection/ngii`에 추출 자료를 생성합니다.
4. `python tools/export_drive_map.py`를 실행하여 `unity/YeongtongTaxi/Assets/Resources/drive-map.json`을 생성합니다.

출처와 데이터 해석 한계는 [원본 검토](docs/ngii-sample-review.md) 및 [지도 도입안](docs/map-data-plan.md)에 기록했습니다. 현재 도로·신호·교통 법규를 그대로 재현하는 제품은 아닙니다.

## 검증

- Python 지도 검사: `python -m unittest discover -s tests -v` (지도 데이터 준비 후)
- C# 독립 검사: `tests` 폴더의 각 검사 파일을 대응하는 `Assets/Scripts` 계산 클래스와 함께 C# 컴파일러로 빌드하여 실행
- Unity Player 검사: 실행 인자 `-smokeTest`, `-trafficTest`, `-jobsTest`, `-stabilityTest`, `-cockpitTest`, `-flowTest`, `-economyTest`, `-turnTest`, `-junctionTurnTest`

검사 결과·캡처는 로컬 `artifacts`에 기록합니다. 검사 실행은 실제 플레이 저장 파일을 변경하지 않습니다. 설계와 기능별 검증 기록은 `docs`에 있습니다.
