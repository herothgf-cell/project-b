# 은계 동네 가게와 공업사 부품

은계 싱글에서 식사를 하거나 잠시 쉬어 갈 곳을 늘리고, 운행 수입으로 택시 외관과 주행 성능을 바꿀 수 있게 했다. 가격·효과와 네 가지 아이템의 그림은 [생활시설·택시 부품 기획서](../content-design/04-life-vehicle-economy.md)에 있다.

## 변경 및 재현

- M 지도의 생활시설 비교에서 간식·커피를 선택한다. 은계김밥은 EG-GAME-03, 동네 카페는 EG-GAME-06에 있다. 지정된 포켓에 빈차로 정차하고 F로 주문한다. 승차와 겹칠 때는 F7로 주문한다. `RefreshmentShops`, `ServiceStops`, 지도 UI, 운행 계획과 기사·회계 모델을 수정했다.
- 공업사에 정차한 뒤 F로 공업사를 열고 차량 부품 판매대로 들어간다. 구매하면 보관함에 들어가고 현재 택시에 장착할 수 있다. `WorkshopPartsShop`, 차량 카탈로그, 외관, 숙소 차고, 승객 운전 평가, 연료 예측·소모 및 경력 저장을 연결했다. 그림 원본은 `Assets/Resources/WorkshopParts/`에 있다.
- 은계 r50 지도 입력은 기존 정차 포켓을 그대로 사용한다. 기본 배포는 `builds/EungyeTaxi/EungyeTaxi.exe`이다.

## 검증

- `tools/test_vehicle_garage.ps1` 90건, `tools/test_operation_planning.ps1` 66건, `tools/test_operation_planning_adapters.ps1` 통과. `tools/test_prototype.ps1` 전체 통과.
- `tools/check_workspace.ps1 -Strict` 필수 지도 입력 문제 0.
- Unity 2022.3.62f2 `PrototypeBuild.BuildHybrid`로 독립 작업 폴더에서 새로 빌드했다. `PROTOTYPE_BUILD_OK`, 종료 0.
- Windows 실행본에서 임시 경력으로 공업사 접근, 잔액 부족·중복 결제, 아이콘 4종 로드, 장착·해제, 연료 소모와 승차감 판정, 저장 복원을 확인했다. `-workshopPartsReport <검사.json> -shiftCaptureOutput <캡처폴더>`로 재현한다. `WORKSHOP_PARTS_PLAYER_OK checks=21`. 렌더한 택시 외관에서 측면 스트라이프와 트렁크 스포일러를 확인했다.
- 같은 실행본에서 `-facilityArtCheck -refreshmentCheck -shiftCaptureOutput <캡처폴더>` 위치 제어 검사 36건 통과. 두 가게의 길 안내, 현장 주문, 결제와 저장 복원을 확인했다. 실제 키보드로 도로를 주행하는 검사와 여러 날에 걸친 가격 밸런스 평가는 남아 있다.
