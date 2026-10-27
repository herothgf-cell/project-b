# 프로토 시나리오 기획 추가

작업일: 2026-10-21 · 범위: 기획 문서 작성·관련 문서 연결. 코드·지도·빌드 수정 없음.

## 요구사항

- 승객마다 법규 상황 또는 선택 상황을 지정한다.
- 기존 승객 요청은 유지하고 승차·하차를 위한 운행 경로에 상황을 포함한다.
- 기획부터 진행하고 별도 ‘프로토 시나리오’ 내용으로 추가한다.

## 실제 작성 내용

[프로토 시나리오](../design-history/content-design/2026-10-27-before-planning-only/09-prototype-scenarios.md)에 법규 7코스와 선택 2코스, 총 9개 승객 코스를 제안했다. 승차 전 접근 2개, 탑승 후 법규 5개, 출구 선택 1개, 하차 접근 우회전 선택 1개로 나눴다. 생성 지도 승하차 12곳의 이름·좌표·간선 참조, 기존 7구역의 교차로/연결, 새 선택 코스의 후보·확정 조건을 기록했다.

Quiet/Eta, 완료 운송 수에 따른 요청 교대, Y 수락/U 안전상 보류, 기존 지급 규칙을 유지한다. 선택 계획과 실제 운전·법규·매너를 분리하고 승차 전 사건 연결, 우회 대체 완료, 안전 배치 불가 시 보류, 취소/마감/저장 경계를 정리했다. 9코스 모두를 3일 캠페인에 강제하지 않는다. 문서는 검토용 초안이며 게임에 적용하지 않았다.

## 변경 파일

- `docs/content-design/09-prototype-scenarios.md`: 신규 기획.
- `docs/content-design/README.md`: 문서 목록 연결.
- `docs/content-design/04-passengers-and-reservations.md`: 기존 요청 유지와 시나리오 분리 안내.
- `docs/prototype-scope.md`: 별도 기획의 범위·미구현 상태.
- `docs/prototype-development-checklist.md`: PS 기획 완료와 PS-A~E 미착수, P5 요청 확장과 분리.
- `docs/work-history/LATEST.md`: 최신 기록·기본 빌드·입력 상태·다음 작업.
- 이 작업 이력.

## 작업 시작 상태와 대조

`LATEST.md`, 체크리스트, 프로토 범위, 콘텐츠 01/02/04, 기존 7구역 검증 문서를 읽고 `PassengerJobs`, `PassengerRequestUI/PassengerRequest`, `ServiceLawZones`, `EungyeHybridBuilder`, `HdRoadGraph`와 대조했다. HEAD는 시작 시 `447e956`이었다.

`git status --short`에 README·다수 게임 코드/테스트 수정, 라이선스 문서, 도로시설 계획, 신규 도로/주유소 코드가 있었다. 별도 작업으로 보존하며 이 문서 커밋에 포함하지 않는다. 현재 코드의 시간 초과 이후 운송 동작도 수정 중이므로 기획은 기존 요청·지급 원칙을 유지하며 배포 exe와 동일하다고 주장하지 않는다.

## 수행한 검증과 결과

- PowerShell JSON 대조: 입력 `eungye-map.json`은 Stop A~H 8곳, 생성 `eungye-hybrid-map.json`은 은계 통합 승하차 12곳임을 확인했다. 처음 입력 JSON에서 `hdEdges`를 찾는 명령은 속성 부재 오류가 있었고, 생성 지도 파일을 찾아 다시 대조했다. 최종 생성 지도/구역 읽기 명령 종료 0.
- 생성 지도는 5,517,951바이트, SHA256 `01151c6e914b9311f68e024e81e5f77fb5acc73a83d7c1a691535d658e43ad96`. 파일·manifest는 변경하지 않았다.
- 방향 그래프 탐색: PS01 기준8→1182→12, PS02 기준10→1158→8, PS03 8→1180→5, PS04 8→1050→5, PS05 8→1248→7, PS06 5→1133→6, PS07 6→1351→9의 두 구간 연결이 모두 `edgesReachable=True`, 명령 종료 0. 비활성 간선을 제외한 연결 존재만 검사했으며 Unity Route/Via 실행·laneAt·85m 접근·주행 시간 검사는 아니다.
- J7204_2569에서 우회전 간선 1368/1392가 존재함을 확인했지만 PS09 전체 코스와 보행 상황 배치는 미검증이다. PS08 출구 우회 코스도 미검증이다.
- 법규 웹 대조: 공식 점멸 별표 과거본·비보호·우회전·정체 안내를 열람했다. 현행 법령 동적 본문은 도구에서 읽히지 않아 현행 전수 감수로 표시하지 않았다. 출처와 한계는 본문에 남겼다.
- 문서 검사: 대상 7개 파일의 로컬 링크 149개 존재 확인(`DOC_LINKS_OK`), PS01~PS09 표 9행 확인(`SCENARIO_ROWS_OK`), `git diff --check` 종료 0. 줄바꿈 CRLF→LF 안내는 있었지만 공백 오류는 없었다.
- 최종 diff 대조 중 같은 기획 파일에 12분 영업·연료 지원·운전 점수 명칭 등 동시 작업의 변경이 추가됨을 확인했다. 작업 파일에서 보존하고 이번 커밋은 시나리오 관련 문단/표만 선택해 포함한다. 새 시나리오는 영업 시간을 재정의하지 않고 최신 통합 설정을 따르도록 보완했다.

## 미검증·다음 작업

사용자 기획 검토, 실제 내비·차로 내 위치·시야·안전 정차·경로 시간 확인, PS08/09 위치 확정, 코스 고정 배정·승차 전 이벤트·P4 패널 구현이 남는다. 위치 제어 검사, 자동 물리 주행, 수동 플레이, 전체 캠페인 검사, Unity 빌드는 이번 작업에서 실행하지 않았다. 기존 로그는 현재 기반의 참고이며 새 코스의 통과 근거가 아니다.

기본 실행은 계속 `builds/EungyeTaxi/EungyeTaxi.exe`, 지역은 은계, 빌드 메서드는 `PrototypeBuild.BuildHybrid`다. 배포 실행 폴더·manifest·필수 입력 3개는 미변경이다. PC 전환 작업은 아니며 strict 입력 검사는 이번에 재실행하지 않았다.

## 열람·재현

1. `docs/content-design/09-prototype-scenarios.md`를 열고 PS01~PS09와 위치 표를 검토한다. 기존 exe에 새 9코스가 구현된 것으로 안내하지 않는다.
2. 저장소 루트에서 아래 명령으로 생성 지도 후보 위치·구역을 확인한다. 생성 파일이 없으면 기존 개발 준비 절차로 생성하고 스냅샷 차이를 기록한다.

```powershell
$scenarioMap = Get-Content -Raw unity/EungyeTaxi/Assets/Resources/eungye-hybrid-map.json | ConvertFrom-Json
$scenarioMap.stops | Select-Object name,x,z,dx,dz,laneEdge,laneAt
foreach ($scenarioEdge in @(1182,1158,1180,1050,1248,1133,1351)) {
    $scenarioMap.hdEdges[$scenarioEdge] | Select-Object id,ruleId,turn,next
}
Get-FileHash unity/EungyeTaxi/Assets/Resources/eungye-hybrid-map.json -Algorithm SHA256
```

3. 코드 구현 전에 현재 지도/영업 변경과 통합 기획을 대조하고 PS-A부터 진행한다. 경유 연결만으로 실제 운송 검증을 대신하지 않는다.
4. 이 이력의 실제 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-21-prototype-scenarios.md`로 확인한다. 자기 커밋 해시를 본문에 추정 기록하지 않는다.
