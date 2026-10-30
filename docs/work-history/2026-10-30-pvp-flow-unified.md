# 2026-10-30 PVP 교통 흐름·도로 안내·통합 실행

## 요구와 보존
사용자: 병렬 차로 동시 통과/실제 충돌만 양보, 유사 도로 전역 확인, 방향별 노면 화살표, 편도3개씩 왕복6차로, 추가 골목, 좌표1271.5697/1777.091 북향358.478821의 끊김, 빨간 NPC 자세 조사, 단일 실행 파일의 싱글/PVP 모드 선택(PVP는 자체 도시만),20분 가정의 피로·포만 기획. 마지막에 필수 검사만 묶어서 수행하도록 명시했다.

시작HEAD dd709ae, 원격main 추가 변경 없음. 현재 worktree 기존 변경 없음. 다른 체크아웃의 Library/Packages/ProjectSettings/UserSettings와 기존 이력/PackageManagerSettings/YeongtongTaxi 미추적 항목은 보존한다. PC 입력 조작과 사용자 게임 종료는 하지 않는다.

## 구현/검증 방침
[요소별 설계·생활 기획27](../content-design/pvp/27-flow-unified-mode.md), [실행 계획](../superpowers/plans/2026-10-30-pvp-flow-unified.md). 구현을 나눠 일괄 진행했고 중간 Editor/Player/단계별 리뷰를 실행하지 않았다. 아래 최종 검사에서 컴파일/필수 회귀/복사 실행/저장 보존/지정 지점 성능과 실제 주행을 구분한다. 현재 결과는 미기록이며 검사 이후 실제 종료값을 적는다.

빨간 차량의 색은 원인 근거가 아니다. 사진만으로 개별 NPC ID·회전 의도·경로 이탈을 확정하지 않는다. 도로의 짧은 연결부에서 압축된 S자 접선과 기존의 전차로 단일 합류 소유권, 반복 fleet/물리 조회가 코드 조사 대상이다. 수정 후 경로 연속성/차체 진행/프레임 측정으로 확인한다.

## 현재 소스 구현
- NativeTrafficMergePolicy: 폭을 포함한 경로별 병렬 허가, 동일 직진 경로의 움직이는 선행차6m 이상 차간 추종, 먼저 대기한 교차 이동의 우선권. 진출 막힘/후미 소유/주행 중 경로 해제 보호 유지. fleet/차체 관측과 물리 조회 재사용.
- PvpCityRoadBuilder/LaneDirectionPaint: 편도3개씩6차로9구간, 추가 골목45도로/27접점, 짧은 연결부 S자 접선 정리, 허용 연결 기반606개 화살표. 전체201도로/2874차로 링크/420차로변경/4유턴.18승객 거점·기존 서비스 유지.
- UnifiedModeBootstrap/PrototypeBuild/BusinessProgress/PvpBootstrap: 단일 실행 메뉴, 은계 싱글과 자체 도시 PVP, 세션에 고정한 캠페인 저장 금지, 메뉴 왕복/재시작. 기존 회사·제품명을 유지해 싱글 저장 위치 보존.
- 진단: PvpTrafficFlowMonitor(읽기 전용 관측), 지정 좌표용 TrafficPerformanceProbe, UnifiedModeProbe(임시 저장으로 실제 모드 왕복·원본/백업 보존), 최종 Editor coordinator 및 검사 러너 출력 폴더 옵션.
- 별도 생활 개선안은 콘텐츠27의 후보 표로만 작성. 기존 피로/포만 수치·캠페인 밸런스는 변경하지 않음.

## 최종 검사 진행 기록
1. `PvpFlowUnifiedChecks.Run`, Unity2022.3.62f2 batchmode/nographics, `artifacts/pvp/flow-final-editor1.log`, exit1. 컴파일 통과. 모드9, 교통(병렬2358쌍 중1122호환/1236충돌·연속추종 최소10.00288m/후행 정지0프레임 포함), A2기하71646/차로준비37,4인/지도/호출 표시 통과. 추가 골목의 건물 제거로 옛 전체건물>=900 고정검사 실패.
2. 해당 고정값을9구역 각48개 이상 가로변 건물로 바꾸고 기존 전역 도로·건물·승객·시설 여유 검사는 그대로 유지. 실제 구역별60~87개. EditMode의 NPC 바퀴 시각 콜라이더 제거도 수정.
3. `PvpFlowUnifiedChecks.Corrections`, `artifacts/pvp/flow-final-editor2.log`, exit0. 교통 전체 재통과, 지도90645검사/거점306방향쌍 통과. 이후 마지막 코드 검토에서 '진입 후 다른 차로로 경로 재탐색 시 예전 corridor 허가를 그대로 사용'하는 경계 사례를 발견하여 수정·회귀 추가 중. 이 검토는 단계별 검토가 아니라 구현 후 한 차례 최종 검토다.
4. `tools/check_workspace.ps1 -Strict`: issues0, 선택 가져오기2맵 미배치. 원격main 재확인dd709ae. 이전 싱글130파일112,309,087바이트 manifest 일치 확인 후 통합 빌드 교체 예정.
5. 最終 리뷰 지적의 진입 후 경로 변경은 기존 점유 보호와 새 경로 허가를 분리해 수정했다. `PvpNpcMergeTests.Run`, `artifacts/pvp/flow-final-editor3.log`, exit0. 새 CommittedReroute와 기존 병렬/추종/막힌 진출/대기열/경로Clear·Continue 전부 통과. TaxiController의 Editor 시각 바퀴 콜라이더 제거를 정상화했다. 이후 관측 보고서가 재시작으로 이전 세션을 잃기 전에 저장되도록 진단만 연결했다.
