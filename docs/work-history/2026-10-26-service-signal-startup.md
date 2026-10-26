# 주유·세차, 보행 신호와 시작 성능 후속

## 요청과 구현

- P8-07은 기존 구현을 유지한다. 고유 이동 350m 또는 안정 주행 45초마다 급가감속 손실 M +2, 운송당 +6. 회복할 M 손실이 없으면 별도 안심 보정 S +2/최대4/R상한. 이전 미구현 안내를 정정한다.
- 미탑승 호출도 주유·세차 가능. 호출 ID 유지, 주유10/세차30초를 픽업 기한에서도 차감하며 만료 처리한다. 이용 후 픽업 내비 복귀. 주유소 F7에는 현재 주유소와 세차/청결 표시만 남기고, 다른 장소의 빈차 생활 거점 메뉴는 유지한다.
- PS09/R5 횡단 신호가 진입 도로에 연결된 오류를 우회전 후 횡단 도로로 수정한다. 일반 녹색 출발/적색 전환 뒤 완주를 유지한다. 사용자 승인된 일부 무단횡단은 J5076_2580 한 명만 정상5회 뒤 적색8초 및 차량 비점유 조건에서 허용한다. 상태 저장과 구형 기본값 포함.
- 시작 계산은 경로 좌표 이분탐색, 교차로별 동일2m 샘플 공유, 초기 차량 위치 조회 재사용으로 줄인다. 48대/분산/도로 규칙은 유지한다. 지도 검증 최적화와 최종 수치는 아래 검증 결과로 구분한다.
- 내비 671개 방향 경로를 독립 Dijkstra 최소 비용과 비교: 실패0/접근 불가0. 거리 비용에는 기존 차로 변경12m 비용이 포함된다. 실제 기본 빠른 길의 교통비용·실도로 회전 가능 여부까지 증명한 것은 아니다. 동부 주유소 출구→S04 직선23.5m/합법 연결1375.1m 등 긴 우회 확인. 사용자 문제 구간 미지정이므로 임의 도로 연결/역주행 지름길은 추가하지 않았다.

## 변경 파일

- `unity/EungyeTaxi/Assets/Scripts/ServiceStops.cs`, `PassengerJobs.cs`, `PassengerCareProbe.cs`: 이용 조건·메뉴·픽업 시간/안내·실행 검사.
- `TrafficSystem.cs`, `AmbientPedestrianSystem.cs`: 횡단 신호·무단횡단·저장.
- `AssistedRoadNetwork.cs`, `DistrictTraffic.cs`, `WorldBuilder.cs`, `ProjectMapPackage.cs`, `ProjectMapScene.cs`, `RoadMapValidator.cs`: 반복 계산 최적화·시작 측정. 검증 호출 안에서만 도형/건물 경계 캐시, 보수적 AABB 분리 뒤 기존 정밀 교차 검사 유지.
- Editor `ServicePickupTests`, `PedestrianSignalConsistencyTests`, `StartupPathTests`, `ServiceStartupFollowupTests`, `NavigationStopRegressionTests`: 회귀/감사.
- 기획05/15/16, 체크리스트, 본 기록·LATEST 및 배포 manifest.

## 검증 중간 결과와 재현

로컬 Unity: `C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe`. 저장소 기준 프로젝트 `unity/EungyeTaxi`. batchmode/nographics/quit의 executeMethod와 로그:

| 검사 | 실제 결과 |
|---|---|
| `ServicePickupTests.Run` 수정 전 | 실제 주유소3개 로드 뒤 north 탑승 전 주유 불가로 예상 실패1 (`artifacts/service-pickup-red.log`). 최초 fixture는 주유소0개로 무효였으며 Apply/개수 검사를 추가해 재현 |
| `PedestrianSignalConsistencyTests.Run` 수정 전 | 우회전 후 횡단 신호 불일치로 예상 실패1 (`artifacts/pedestrian-signal-red.log`) |
| `ServiceStartupFollowupTests.Run` | 주유3곳·4방향 신호/실제등·횡단/저장·좌표11,672·경로671검사 종료0 (`artifacts/service-startup-green.log`) |
| 옛 `DistrictTrafficTests.Run` | 구 `eungye-hybrid-map` resource 부재로 fixture 실패. 현행 검사로 대체, 제품 성공 근거에서 제외 |
| `StartupPathTests.RunCurrentMapRoutes` | 현행 지도132경로/110,186샘플/589신호/양방향 회전 검증 종료0 (`artifacts/startup-current-map.log`) |
| `tools/test_prototype.ps1` | 24 모델 모음 종료0 |
| `tools/test_passenger_care.ps1` | 13,301 검사 종료0 |
| `tools/check_workspace.ps1 -Strict` | 종료0, 필수 입력 정상·선택 입력2개 부재 유지 |
| 첫 실행파일 통합 | 주유/세차/호출 유지 통과 후 저장 파일 교체 실패로 취객 즉시 체크포인트 검사 실패89. `artifacts/service-startup-runtime.log`; 성공으로 간주하지 않음 |

기존 실행파일 headless 시작 18,730ms(월드12,118/도로교통5,500/행인752/게임플레이351). 첫 변경 빌드17,871ms는 Editor 동시 실행이 있어 최종 성능 비교에서 제외한다. 지도 검증은 약8초로 추가 병목 분석 중.

## 최종 결과·남은 확인

최종 `ServiceStartupFollowupTests.Run` 종료0 (`artifacts/service-startup-final-tests.log`): 위 검사와 함께 편집 신호 남은시간 미상(-1) 녹색 출발, 현행132경로/110,186샘플, AABB 도입 전후 진단 순서 동등3사례(현행/고의 충돌/같은 배열 수정), 장면 검사 및 지도 저장16검사 통과. 편집 신호는 별도 RED 후 수정(현재 지도 편집 신호0개, 잠재 호환 오류). 최초 컴파일 검사에서 동시에 추가되던 보행 테스트가 아직 없는 메서드를 참조한 오류는 테스트 작성 과정에서 수정했다.

최종 `PrototypeBuild.BuildHybrid` 종료0 (`artifacts/service-startup-final-build.log`). 독립 실행파일 headless 준비시간 **14,178ms** (월드8,078/도로교통4,980/행인755/게임플레이357), 기존18,730ms 대비 **24.3% 감소**. 지도 parse302/validate4,353ms, 정밀 교차 후보 필터 이후 overlap66ms. 차량48대/250m셀28개 유지. 한 PC 단회 비교이며 실제 화면 렌더링/콜드디스크 시간 보장은 아니다. 해시·필수 접근·정밀 충돌 검사를 생략하지 않았다. 독립 읽기 전용 리뷰 중대/중요 지적0.

실행파일 `-batchmode -nographics -passengerCareTest` 종료0/201검사 (`artifacts/service-startup-final-runtime.log`). P8 실제 점수 회복/즉시 안내/저장 복원 중복 방지 및 탑승 전 주유·세차/시간/ID/내비/호출 만료 포함. 고빈도 저장 도중 `File.Replace` 실패 경고1회는 여전히 발생했으며 해결된 것으로 간주하지 않는다. 앞선 실패와 별도로 기록한다. 후속 `-careCapture`도 고빈도 저장 이후 체크포인트 비교 예외로 캡처가 멈춰 해당 테스트 프로세스만 종료했다. 메뉴 전용 `-passengerCareTest -fuelMenuCapture`로 반복 저장 없이 별도 확인한다.

화면 전용 보완 후 최종 BuildHybrid 종료0 (`artifacts/service-startup-capture-build.log`), `-passengerCareTest -fuelMenuCapture` 종료0/저장경고0. `artifacts/fuel-menu-1280.png`, `artifacts/fuel-menu-1920.png`를 직접 확인: 주유소1행+세차1행, 두 버튼 활성, 청결/대기 시간 안내, 식사/수면 없음, 잘림 없음. 초기 캡처의 검은 프레임/보간 위치 복귀는 캡처 fixture를 보완해 해결했다. 최종 화면 실행 준비14,662ms. manifest 결과는 배포 절차 후 기록한다. 전체20분/3일 캠페인, 사람의 수동 운전·교통 속 세차 왕복·보행 체감은 수행하지 않았다. 자동 위치 제어 검사는 수동 운전이나 자동 물리주행 검사가 아니다.

지도/입력 ZIP 변경 없음. 현 지도 hash `44ebdedb71e84f436fd15e457841473e1d2761a66989a3bcf5fc50d01654b895`. 사용자 `.mmst`, 콘텐츠 하위 폴더, 별도 Unity/root 폴더 및 이전 status-review 기록은 이번 변경과 섞지 않는다.

## 로컬 배포

소스/기획/본 기록 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-26-service-signal-startup.md`로 확인한 `acb3033`이다. 해당40자리 소스 해시로 배포 manifest 생성. `tools/update_distribution_manifest.ps1 -VerifyOnly` 종료0: **130파일 / 105,654,238바이트**, 각 SHA256 일치. 최종 Strict 종료0. 기본 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`. 원격 push는 하지 않았다.
