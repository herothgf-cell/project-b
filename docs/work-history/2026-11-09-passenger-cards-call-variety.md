# 2026-11-09 승객 카드·호출 다양성·지도 정리

## 요청과 범위

- 사용자: 선택적 ‘승객에게 말 걸기’ 숨김/제거, 단조로운 승차 호출지 점검, 카드 제목에 임의 승객 이름, 대사의 ‘손님’ 화자 표시 제거, M지도 상단 겹치는 보조 문구 및 우측 예상 피로 등 호출 예측 정보 제거.
- 앞선 내비85% 축소는 유지하고 이번 수정과 함께 main 통합·푸시한다. 내부 잡담 데이터/API는 구저장/LAN 호환용으로 남기며 UI 진입/답변 표시는 숨긴다.
- 현재 지도/생성 입력/ZIP·차량 물리/저장 제품명/경로는 보존한다. 기본 exe/은계 싱글·A2 PVP/LANv16/BuildHybrid 유지.

## 실제 구현

- PassengerPresentation: 기존 offer/trip ID에서 30개 한국어 이름 중 안정적으로 하나를 선택한다. 전역 난수 사용/추가 저장 필드 없음. 발언 행 시작의 화자 라벨만 표시 단계에서 제거하며 상태 설명과 4,000자 사용자 대사는 보존한다.
- CockpitUI/PassengerDialogueUI/PassengerRequestUI: 말 걸기 버튼·답변 표시 제거, 활성 승객 카드 제목 이름, 카드/대화창의 화자 표기 정리. 기존 요청·행동·기사 선택 효과 유지.
- FullMapOverlay/MapDestinationUI: 상단 경로 범례 보조 문구와 호출 예측 카드를 표시하지 않는다. 운임·승하차 지점·선택/수락·불가 사유·시설 비교는 유지한다.
- 호출 다양성: 기존 offers의 승차지를 임시 이력으로 참고해 같은 공간 분리 조건에서 새 승차지를 우선한다. 실제 지도 후보24곳과 배정 규칙을 확인했으며 순수 미러 수치와 실제 게임 생성을 구분한다.

## 실제 검증·보호

- 사용자가 게임 종료를 확인했고 실제 게임 프로세스 부재를 확인했다. 직전 사용자 플레이로 갱신된 저장을 과거 해시로 복원하지 않았다.
- 종료 후 보호 기준은17파일이며 바이트 보관본/해시는 `artifacts/compact-nav-chat/protected-closed.json`과 `protected-closed/`에 둔다. 이전 진행 중 기준과 구분한다.
- 표시/잡담 순수 검사: `tools/test_passenger_chat_history.ps1`, exit0/16검사. 이름 안정성/다양성·화자 제거/상태 설명 보존·긴 대사를 확인했다.
- 독립 호출 조사와 최종 Unity/Player/화면/LAN·배포 수치는 아래 실제 결과를 따른다. 이전 숨김 UI 실패나 다른 PC/사람 장기 검사를 이번 성공으로 계산하지 않는다.

## 변경 파일

코드: `PassengerPresentation.cs`, `CockpitUI.cs`, `PassengerDialogueUI.cs`, `PassengerRequestUI.cs`, `FullMapOverlay.cs`, `MapDestinationUI.cs` 및 후속 호출 배정 파일. 검사: 표시 순수 검사와 실제 Player probes. 설계/계획·관련 승객 기획·체크리스트·LATEST와 최종 실행 폴더/manifest를 함께 기록한다.

## 재현

`builds/EungyeTaxi/EungyeTaxi.exe` → 싱글/PVP → 호출 수락/탑승. 승객 이름과 발언을 확인하고 ‘말 걸기’ 입력이 없는지 확인한다. M지도에서 호출을 선택해 우측에 운임/승하차/수락만 표시되고 예상 피로/예측 카드가 없는지 확인한다. 상단 작은 내비·지도 헤더와 기존 요청 선택지를 확인한다.
## 호출 반복 원인·수정 근거

- 기존24정차 후보가 이미3x3 지역으로 분산돼 있으나 직전 호출판의 승차지를 후순위 처리하지 않아 연속 재노출됐다. 가까운 후보/수요 순위·대표 코스는 그대로 두고 새 판을 생성할 때 기존 offers pickup을 임시로 참고한다.
- OneDayOfferPolicy.SpreadPickup은 기존 공간 분리/120m/지역 우선 및 희소 후보 대체 순서를 유지하며 같은 공간 pass에서는 직전판에 없는 후보를 우선한다. 일반·대표 코스에 공통 적용, 저장 필드/배정 난수/PVP 공유 호출 경로는 변경하지 않았다.
- 순수 회귀593개·기존 수요38개 통과. 이전 선택 동작을 복원한 artifact는 새 반복 방지 검사에서 실패했다. 실제24좌표 기반 순수 미러는4수요단계×48세대 coverage24/24/23/24·재노출0이다. 이 수치는 실제 네이티브 경로/대표 코스를 포함한 Player 통계와 구분한다.
- 통합 FullUIBuild.Build exit0. 실제 은계 Player 초기 위치 제어 호출 생성은4수요단계×24세대=96판,968검사 통과. coverage15/14/15/16, 직전판 pickup 재노출0·동일호출판0, 대표 슬롯 coverage 각5. 각 판의 서로 다른 승차지/실제 방향 경로/현재 도달 가능성/점유 여부를 검사했다. 로그/JSON: artifacts/pickup-variety/native-initial/.
- 독립 검토에서 probe의 ‘신선한 대표 코스가 하나라도 있으면 반복 금지’ 단정이 더 강한 공간 pass와 충돌할 수 있음을 확인했다. 과도한 실패 조건만 제거하고 대표 슬롯 반복 통계와 경로/동일판 대체 검사를 유지했다. 제품 코드는 바꾸지 않았으며 최종 probe 소스와 실행본을 맞춰 재빌드한다.
- 전체 tools/test_prototype.ps1은 앞20묶음 통과 후 기존 ManualHandlingTests의 일반 S 제동 기대값 불일치(actual8.823529)로 중단했다. 이번 범위의 TaxiController/ManualHandlingTests는 시작07ea5b8과 동일(git diff exit0)이며 이 기존 실패를 이번 호출/UI 검사 통과로 계산하지 않는다.

## 최종 실행본 검사

- 최종 `PrototypeBuild.BuildHybrid` exit0 (`artifacts/compact-nav-chat/build-cards-release.log`). 마지막 probe 소스 저장11:29:02보다 출하 Assembly-CSharp.dll 생성11:30:43이 이후임을 확인했다.
- 실제 화면 `-brightUiCheck -fullUiReview`: exit0/161검사. 메뉴·설정·기존 요청·작은 내비·이름 복원·승객 카드·M지도 선택·싱글/PVP·1인칭·결과 캡처를 확인했다. 초기 숨김 렌더 실패는 검사 시작 때 다른 해상도에서 Windowed로 전환해 해소했다. 최종 결과 `artifacts/compact-nav-chat/ui-final-player.log`, 캡처 `ui-final/`.
- 이름 카드 `05d-single-named-passenger.png`, 선택 M지도 `04c-single-forecast-1920.png`, 1인칭 PVP `10d-pvp-named-first-person.png`을 직접 열어 확인했다. 기존 forecast 이름의 probe 항목은 모델 데이터 보존 검사이며, 예측 카드 표시 제거는 실제 화면/코드에서 확인했다.
- 최종 호출 Player: exit0/968검사, 실제96판/4수요시점, coverage15/14/15/16·직전판 반복0·동일판0. 최종 요청/저장 복원: exit0/79검사. `artifacts/compact-nav-chat/final-variety/`, `final-requests/`.
- 위 호출 검사는 초기 위치에서 생성·경로 존재를 검사한 위치 제어 검사다. 차량을 직접 몰아 모든 지역에서 운송한 검사가 아니다. PVP 공유 배정은 기존 규칙을 유지한다.

## LAN·입력·저장 최종 확인

- 최종 두 프로세스 `tools/test_pvp_native_lan.ps1 -Port18949 -Reconnect -TimeoutSeconds360`: exit0, 호스트54/참가자67. 실제 네이티브 입력/WheelCollider 이동 약19.7/19.9m, 재접속·같은 차량·날씨/사고/상태 권한 회귀를 확인했다. evidence: `artifacts/pvp/native-lan-20261109-113503-3033be6f/`. 같은PC loopback이며 다른PC 검사는 아니다.
- 필수 입력 Strict 재검사 exit0/issues0, 지도/ZIP diff 없음. 종료 후 기준 사용자17파일 SHA256 변경0. 기존 미추적 자료와 작업 중 나타난 PowerPoint 잠금 파일은 포함/삭제하지 않는다.
- 새 로컬 빌드는 기존 main6936a4e의 Unity MCP 패키지 변경도 반영하므로 새 런타임 의존 DLL/scene 리소스를 실행 폴더 전체와 manifest에 함께 배포한다. 선택 사용자 `content/active-profile.json`은 출하 커밋에서 제외하고 유효성만 검사한다.
- 독립 코드 검토: 최종 제품 코드 추가 문제 없음. 검사 코드의 과도한 대표 코스 단정은 제거했으며 실제 결과 통계는 유지한다. 수동 장기 운전/전체3일/다른PC/저사양 성능은 미검증이다.
