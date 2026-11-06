# Rain prototype implementation plan

Goal: 승인된 비 표현·자동 와이퍼·승차 오염·제동 영향과 콘텐츠 툴 설정을 연결한다.
Spec: docs/content-design/18-rain-prototype.md
Architecture: 기존 ContentTuning snapshot과 VehicleCare.Soil 사건 저장을 재사용한다. RainPolicy는 Unity 독립 계산, RainPresentation은 로컬 시각 표현이다.

- [x] 모델/툴: RainPolicyTests와 프로필 호환 테스트 실패 확인 후 RainPolicy, catalog/default/profile validation·날씨 탭 구현.
- [x] 운행: TaxiController의 제동, PassengerJobs의 성공 승차, TrafficCar 감속 연결. 기본 맑음과 기존 저장 유지.
- [x] 표현: DriveSession 로컬 카메라 중심 파티클·자동 와이퍼, 일시정지/후방 시점 처리.
- [x] 검증: C# 모델/기존 툴 검사, Unity 컴파일/비 전용 실행 검증, 가능하면 실제 제동 비교. 실패/미검증 구분.
- [x] 문서: 마인드맵 노드/기존 메모 보존, 체크리스트/담당 문서/이력 갱신. 다른 작업 변경 보존.

Review focus: 구 저장 강제 비 금지, 승차 재적용 금지, 오염 임계치 현재 운송 유지, 정차 제동 약화 금지, LAN 로컬 설정 우회 금지.

실제20km/h 제동 실측 후 토크 단독 변경은 효과가 없어 제동 중 전후 접지력도 동일 계수로 적용했다. 수동 난도/20분·3일은 미검증이며 확률 미끄러짐을 활성화하지 않는다.
