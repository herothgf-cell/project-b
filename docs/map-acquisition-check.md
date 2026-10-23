# 은계 지도 자료 확인

현재 제작·실행 대상은 **시흥 은계지구**이며 Unity 프로젝트는 `unity/EungyeTaxi`다.

- 2025년 NGII 안양022·023(37612022·37612023) SHP ZIP·XML·XLSX 원본의 CRS·축척을 대조했다. [원본 검토](eungye-source-review.md).
- 실제 범위는 EPSG:5186에서 잘라낸 약2.1×1.75km다. 은계 지역 도로와 같은 지역 국도42 정밀 차로를 결합한다.
- 실행·객체 편집·배포의 정식 자료는 `config/maps/project/eungye.project.json`이다. [지도 관리](map-data-plan.md).
- 현장 신호·회전 허용·안전한 승하차 위치·입체 연결은 원본 형상과 별도로 검토한다. 임시 게임 설정을 현장 검증 완료로 취급하지 않는다.
- 일반 개발은 정식 지도만으로 빌드한다. 재수입 입력 준비와 해시 검사는 [PC 이동 안내](work-history/map-transfer-guide.md), [재수입 안내](ngii-map-import.md)를 따른다.
