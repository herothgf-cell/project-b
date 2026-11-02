# 08. 현행 코드 연결·데이터 설계·개발 순서

법규 코드 검토 기준 `ec3bc4bcd066570643ef2d1b878052bbbffbbded`。게시 전 main의 후속6개 커밋을 대조하고 `72fa437070dbcb2d9db57ccb6be20c6ef9dab3fa`의 승객 동등성 구현·배포를 보존한다. 비교에서 DriveSession/DrivingLaw와 주행 법규 기획은 변경되지 않았고, 승객·봇·배포 상태는 갱신되었다. **문서 설계이며 아래 신규 법규 데이터·관찰자를 구현한 것이 아니다.**

## 1. 실제 확인한 연결 지점

| 현재 파일·문서 | 확인한 역할 | 이번 작업에서 하지 않은 것 |
|---|---|---|
| `unity/EungyeTaxi/Assets/Scripts/DriveSession.cs` | DrivingLaw, DrivingManners, 서비스·승객, 연습·영업 법규 구역 연결 | 전체 호출 그래프·모든 모드 동등성 인증 |
| `unity/EungyeTaxi/Assets/Scripts/DrivingLaw.cs` | 방향별 신호/정지 관찰, 사건 기록, 역주행, 게임 제한속도 | 법정 처분 엔진 추가·배점 변경 |
| [주행 기획](../content-design/01-driving-and-traffic.md) | 현재 운전 점수·M·준수 회복·행동별 평가 | 과거 기획 초안으로 현행 값 덮어쓰기 |
| [기술 참조](../technical-reference/driving-evaluation-reference.md) | LawLedger, 기존 L/F 규칙, 준수/위반/보류/기회 없음 | 법정 벌점을 기존 L 번호로 직접 lookup |
| [영업 법규 구역](../eungye-in-service-law-zones.md) | 점멸·비보호 좌회전·출구 정체 7조건과 과거 검증 | 7조건을 신규 제안 또는 현실 현장 규제 인증으로 표시 |
| [최신 인계](../work-history/LATEST.md) | 통합 실행본, 후속 PVP 구현·검증 범위 | 이전 실패를 현재 상태로 되돌리거나 과거 통과를 이번 실행 결과로 표시 |

현행 `DrivingLaw`의 유턴 기록은 L01을 사용하는 경로도 있다. 따라서 **L01=특정 법조문 하나**처럼 영구 매핑해서는 안 된다. 새 법규 ID와 기존 사건 ID 사이에 행동·조건 기반 어댑터를 둔다.

## 2. 구현 상태의 구분

- **기존 구현 연결 확인**: 신호·우회전·정지선·일부 유턴, 역주행, 게임 지정속도 관찰. 코드 열람과 문서 대조이며 이번 런타임 재검증은 아님.
- **기존 콘텐츠·검증 기록 있음**: 적/황 점멸, 비보호 좌회전 조건, 출구 정체 7조건. 과거 특정 자동 물리 경로 성공을 모든 지도·사람 운전 검증으로 확대하지 않음.
- **게시 직전 반영된 다른 작업**: PVP 지역 수요·8호출+별도 길가3명·정차 선점/점유·기한 보호·봇 응대의 구현과 배포. 기존 미구현 목록을 현재 사실로 되돌리지 않는다. [해당 이력](../work-history/2026-11-02-pvp-passenger-parity-implementation.md)
- **이번 신규 기획**: 76개 관리 목록, 시험 채점 연결, 법정 처분 자료, 24개 상황 카드.
- **별도 구현·감수 필요**: 모든 구역의 완전한 법규 판정, 현실 단속·처분, 전용 면허시험, 신규 상태·시설. 장시간 후속 배차·전도로 교통·전변주 검증 등은 최신 작업 이력의 범위를 따른다.

## 3. 보존할 현재 게임 정책

운전 점수는 새 캠페인 100, 0~100 범위다. 확인된 새 준수 사건 +3, 사건별 최대 감점 1회, 보류·버튼 선택·단순 대기는 보상 없음. 이전 저장의 +1 정책 사건을 소급 변경하지 않는다. M은 별도 승객 운전 만족도다.

현재 게임값: 적색 직진 -20, 전방 적색 우회전 단순 정지 누락 -5, 전용 적색 우회전 -20, 횡단 보행 보호 -25, 끼어들기/위험 진로변경 -15, 정지선 침범 -1, 역주행 -10, 게임 제한속도 -5. 이 값은 실제 법정 벌점이 아니다. 이번에는 이 값을 수정하지 않는다.

속도 관찰의 +3km/h·2초, 역주행의 2초·8m 및 전진 조건, 정지 인식 속도 오차는 게임 관측 설정이다. 운임의 운전 점수 보정도 게임 경제 규칙이며 실제 택시 운임법과 혼동하지 않는다.

## 4. 권고 데이터 구조

| 데이터 | 필수 필드 |
|---|---|
| RuleDefinition | stableId, title, applicableModes, legalBasis, requiredConditions, exceptions, evidenceRequirements |
| LegalSourceVersion | sourceUrl, documentName, articleOrAnnexRow, promulgatedAt, effectiveFrom, effectiveTo, retrievedAt, verificationStatus |
| IncidentEvidence | incidentId, tripId, driverId, mode, simulationTime, approachLane, signalSnapshot, vehicleTrajectory, pedestrianState, visibility, mapRegulationVersion |
| LegalAssessment | applicableRuleId, verdict, establishedFacts, missingEvidence, exceptionApplied |
| EnforcementCase | detectedBy, identifiedSubject, evidenceLinks, procedureType, duplicateCaseKey, dispositionStatus |
| PenaltyDefinition | vehicleClass, roadClass, zoneType, activeTime, excessSpeedRange, recurrenceCondition, amount, statutoryPoints, sourceVersion |
| ExamAssessment | examType, licenceClass, transmission, powertrain, itemId, deduction, disqualification, occurrencePolicy |
| GameAssessment | existingRuleId, drivingScoreChange, mannerChange, incidentAggregationPolicy, policyVersion |

ID 명칭은 제안이다. 신규 코드 클래스가 이미 존재한다고 가정하지 않는다. 금액 미확정은 null, 관찰 불충분은 Deferred, 적용 대상 아님은 NotApplicable/기회 없음으로 구분한다. 보류를 준수로 채점하지 않는다.

## 5. 사건 판정 파이프라인

1. 유효한 지도 규제·법령 버전·게임 모드를 결정한다.
2. 접근·정지·진입 등 사건 경계를 만들고 그 시점의 증거를 기록한다.
3. 법규 적용 조건·예외를 검사한다. 증거가 없으면 보류한다.
4. 법적 평가, 시험 평가, 게임 평가를 각각 산출한다.
5. 단속 시스템이 실제 증거와 대상자를 확보한 경우에만 처분 경로를 선택한다.
6. 기존 게임 장부의 중복 방지·저장 버전 정책으로 결과를 기록한다.

**법적 하나의 행위, 게임 하나의 사건, 시험 하나의 채점 항목은 같은 단위가 아니다.** 현실 처분 병합은 해당 법령의 별도 기준, 도로주행시험 반복 감점은 별표26의 채점요령, 기존 게임은 사건 최대 감점으로 각각 처리한다.

## 6. 모드별 적용

| 모드 | 우선 정책 |
|---|---|
| 은계 싱글 | 자연 운송 경로에서 실제 발생한 판단을 평가. 법정 처분·납부는 별도 경제 확장 |
| 자체 도시 PVP | 사람/봇 동일 신호·표지·관측 기준. 경기 결과용 운전 점수와 법정 처분 분리 |
| 도로주행 연습 | 선택한 전용 시험 프로필, 도로주행 점수·실격 기록 |
| 장내기능 연습 | 전용 코스·검지선·시간·차량 프로필, 일상 영업 기록과 분리 |

PVP에서 설명 패널을 열었다고 공유 경기 시간을 임의로 멈추지 않는다. 봇에게 정지·시선·방향지시 의무를 묵시 면제하지 않는다. 시선 확인은 플레이어 카메라 회전만으로 실제 사람의 눈동자를 알 수 없으므로 대체 조작/평가 범위를 명시한다. 조향 보조 연습과 무보조 시험 성적을 구분할 수 있다.

## 7. 단계별 작업 제안

| 단계 | 작업 | 완료 조건 |
|---|---|---|
| P0-A | 기존 L 사건과 법규 목록 연결, 출처 버전·보류 사유 | 기존 점수·저장·운임 결과 불변 회귀 |
| P0-B | 방향별 신호·보행 의도·출구 점유 경계 강화 | 정상/위반/예외/보류 재현, 중복 없는 사건 |
| P1-A | 주정차·합법 승하차 구역 + 시설 출입 보행자 | 목적지가 위반을 강제하지 않음, 사람/봇 동일 대안 |
| P1-B | 보호구역·방향지시·정당한 합류 | 구역/시간/차종 예외와 경계값 테스트 |
| P1-C | 법정 처분 데이터 | 핵심 금액·벌점·단서 원문 고정, 확인된 행만 활성화 |
| P2-A | 야간·날씨·긴급차·통학버스·특수도로 | 상태·표현·가시성·AI 준비 후 법규 적용 |
| P2-B | 면허시험 연습 | 별표23 코스와 별표24/26 채점, 시험 프로필 분리 |

## 8. 공통 수용 체크리스트

- [ ] 실제 원문 시행일/항·별표 행과 예외 확정. 미래 시행 규정 혼입 없음.
- [ ] 합법 예시·위반 예시·면책/예외·불명확 보류 최소 4종.
- [ ] 공간 경계·시간 경계·물리 틱/저프레임·단위 변환 검증.
- [ ] 텔레포트·저장 복원·반복 진입·일시정지·재시작 중복 없음.
- [ ] 차량 전체 형상, 보행자 가시성, NPC 갑작스런 생성에 의한 불공정 감점 없음.
- [ ] 사람/봇·싱글/PVP별 재현과 동일 입력 동일 결과 확인.
- [ ] 정상 대기·위험 회피 제동·정당한 거절 무불이익 유지.
- [ ] 기존 모델 테스트, Unity 자동 물리 테스트, 사람 수동 플레이 결과를 각각 기록.

체크박스는 후속 구현의 수용 조건이다. 이번 문서 작업에서 실행 테스트를 수행했다는 표시가 아니다.
