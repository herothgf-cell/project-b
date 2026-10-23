# 은계 지도 데이터 관리

현재 배포·개발 대상은 시흥 은계지구 한 곳이다.

| 자료 | 역할 |
|---|---|
| `config/maps/project/eungye.project.json` | Git 정식 기준 지도·차로·객체 편집 문서. 일반 빌드의 입력 |
| `unity/EungyeTaxi/Assets/Resources/eungye-project-map.json` | 정식 지도를 빌드에 포함하기 위한 생성 복사본 |
| `config/maps/eungye.json` | 은계 NGII 재수입 범위·좌표계·원본 근거 |
| `eungye-map.json` / `route42-map.json` | 같은 은계 지역의 재수입·차로 검사용 생성 입력 |
| `distribution/maps/` | 위 입력2개와 해시를 Git으로 전달하는 ZIP |
| `data/raw/ngii/eungye`, `data/raw/ngii/hdmap/eungye` | 별도 보관하는 은계 NGII/정밀차로 원본 |

Unity 편집기 또는 실행파일 편집기의 정식 저장→JSON 커밋·푸시→다른 PC pull·빌드로 배포한다. 원본을 다시 생성하는 작업과 객체를 편집하는 작업을 구분한다. 현재 도로 규제·신호·승하차 허용 여부와 생성형 게임 시설의 검증 상태는 별도 근거로 관리한다.

[Unity 편집 사용법](unity-map-editor-guide.md) · [재수입](ngii-map-import.md) · [PC 이동](work-history/map-transfer-guide.md).
