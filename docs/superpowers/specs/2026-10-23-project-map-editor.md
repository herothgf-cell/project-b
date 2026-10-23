# 프로젝트 지도 편집·공유 배포 설계 (2026-10-23)

사용자 승인: 도로·교차로·차로·표지·신호뿐 아니라 건물·보도·주차장·식당 부지를 프로젝트 지도 자체로 편집하고, 다른 PC의 pull/빌드에서도 유지한다. 이 지시에 따라 설계·구현·검증을 연속 진행한다.

## 저장과 재현

`config/maps/project/eungye.project.json`을 Git 추적하는 정식 지도 자료로 둔다. ProjectMapPackage(schema1, baseline: MapData, document: RoadMapDocument) 한 파일에 수입 기준 지도 스냅샷과 편집 문서를 함께 보관한다. 외부 측량 원본은 그대로 보존하고, 프로젝트 자료로부터 빌드하므로 원본 재수입이나 로컬 AppData에 의존하지 않는다. 초안/자동 초안과 프로젝트 배포 저장을 분리한다. 검증 실패/동시 변경 시 기존 프로젝트 파일을 보존한다. 게시 루트는 저장소 마커로 확인하고 표준 상대 경로에 원자적으로 저장한다.

BuildHybrid는 프로젝트 자료가 있으면 이를 검증해 Resources에 포함한다. 런타임은 포함된 패키지의 기준 지도와 문서를 구성한다. 프로젝트 지도 버전이 개인 수정본보다 우선 기본값이며 개인 수정본은 호환되는 경우만 별도로 적용한다. `-roadMapInput` 시험도 프로젝트 기준에 맞게 동작한다. 오래된 수정본은 명확한 사유와 함께 적용하지 않는다.

## 편집 자료 계약

RoadMapDocument에 nullable `ProjectMapScene scene`을 추가하며 기존 schema1 문서를 지원한다.
ProjectMapScene은 `ProjectMapObject[] objects`를 가진다. 객체 필드: string id,kind,name,laneId; MapPoint[] points; float width,height,laneAt,serviceX,serviceZ,yaw; int lanes; bool enabled=true,oneWay,suppressPaint. kind: road/building/sidewalk/parking/restaurant/rest/water/land. road는 열린 중심선, 나머지는 닫힌 폴리곤(끝점 반복 없이)이다. 서비스는 주차·식사 위치 serviceX/Z, 방향yaw, 도로 접근 laneId/laneAt을 가진다. 기존 서비스는 포착하여 편집 가능하게 옮긴다.

`ProjectMapScene.Capture(MapData)`는 현재 기본 외형·보정·생활시설을 객체로 포착한다. `ProjectMapScene.Apply(MapData, ProjectMapScene)`는 실제 roads/buildings/areas와 서비스 정의를 구성한다. `ProjectMapScene.Validate(MapData,RoadMapDocument)`는 null/중복/유한수/지도 경계/폴리곤·삼각분할/높이·폭/서비스 접근 및 주차 위치 등을 검사한다. 구성 후 기본 보정 코드가 사용자 형상을 덮거나 옛 시설을 중복 생성하지 않게 한다. 도로 노면과 차로 그래프는 각각 편집하며 잘못된 구성은 검사/시험으로 확인한다.

## 화면

기존 간편 도로 편집을 유지한다. 건물·부지/노면 편집 탭에서 객체 종류 필터/이름 목록/지도 클릭, 사각형 추가, 이동·회전·크기·꼭짓점 수정·삭제·취소를 제공한다. 건물 높이, 도로 폭, 시설 종류·주차 위치·접근 차로를 편집한다. 평면도는 폴리곤을 표시하고 3D에서는 실제 구성 결과를 확인한다. 프로젝트 경로 탐색/직접 지정, 프로젝트에 저장, 프로젝트 지도 다시 열기, 변경 상태·버전을 보여준다. 자동 Git 커밋/푸시 대신 사용자 명령 또는 이 개발 작업의 명시된 권한에 따른다.

## 검증과 완료

기존 문서 호환, 폴리곤 오류/손상/해시·동시 변경의 저장 차단, 모든 객체 왕복/삭제/취소, 서비스 실제 판정, 재생성 반복, Resources 재복사, 별도 PC를 모사한 디렉터리·개인 저장 없는 로드, 실제 실행·3D·버튼 크기 및 빌드 검사를 수행한다. 필수 생성 입력을 변경하지 않는다. 배포 실행 폴더 manifest는 실 파일 크기/SHA256으로 갱신한다. 사람의 수동 운전/다른 물리 PC는 미검증으로 구분한다.
