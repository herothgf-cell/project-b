# 마인드맵 50/25/0 체크와 미구현 우선순위

## 요청·실제 변경

기존 아이디어와 앞선 추천을 포함해 미구현 요소를 우선순위별로 정리하고, 마인드맵에 구현50%·기획 완료25%·기획 필요0%를 체크하라는 요청이다. 원본 `.mmst`의 기능 말단 `task.progress`를 실제로0.5/0.25/0으로 변경했다. 부모는 하위 개수 집계, 설명/참조는 제외하며, 부분 구현은 구현 동작과 추가 확장으로 분리했다. 새 게임 기능은 구현하지 않았다.

- `docs/future-design/2026-11-02-simulation-priorities.md`: 미구현 기존 아이디어·추천의 P1~P4/선행 조건/기획 단계. 상태 예측의 표시·계산·정보 없음·마감/모드 경계·갱신/저장 계약을 구체화해 미채택 기획25%로 정리했다. 아이디어/후속 메모만 있는 항목은0%다.
- 원본과 목차: 현재 동작160항목50%, 상태 예측1항목25%, 기획 필요63항목0%. 중복 참조가 포함된 기능 노드 수이며 독립 기능 수/전체 개발 완료율이 아니다.
- 방향지시등 수동 자동해제, 서행 자동 감점, 실제 비보호 접근 배치, 원격 통합을 기존 조작/안내/판단/세션 기능과 분리했다. 근거가 미확인인 정류소/회전교차로 전용 기능과 별도 설정 메뉴도 확인·기획 과제를 명시했다.
- 단계 도구 `tools/annotate_mindmap_progress.py`, 기존 Python/PowerShell 검사 도구,06 검토표·체크리스트·LATEST를 갱신했다.

이번 시작 시 다른 UI 작업의 소스/장면/빌드·문서·LATEST 미커밋 변경과 사용자의 마인드맵 접힘/제작 참고 제거 변경을 확인했다. 이를 복원하거나 이번 변경에 섞지 않는다. 작업 중인 UI 스타일을 구현50%의 새 근거로 쓰지 않았다. 기본 근거는 기존 배포/기획/소스의 실제 동작이다.

## 보존·분리

마킹 전 사용자 원본을 `docs/design-history/content-design/2026-11-02-mindmap-before-progress.mmst`에 보관했다. 기존 보관본은 유지한다. 새 노드 ID는 고정 ID이며 기존 사용자 노드·메모·외부 링크를 유지한다. 요청받은 과거 진척도는 새 단계로 변경하므로 이전의 ‘task 진척도 불변’ 검사를 새 사용자 기준으로 바꿨다.

사용자가 제거한 제작 참고를 작업 폴더에 복원하지 않았고 접힘 상태도 유지했다. 커밋 후보는 HEAD 원본에 동일 마킹만 적용해 별도로 생성한다. 따라서 작업 폴더333노드/커밋 후보340노드이며, 차이는 기존 미커밋 제작 참고7노드로 모두 기능 집계 제외다. 마킹 수160/1/63은 양쪽이 같다. 목차도 각 원본에 대응하게 생성하고 기존 편집 차이는 미커밋 상태로 보존한다. LATEST 역시 이번 추가 항목만 분리한다.

## 근거와 검증

핵심 근거는 콘텐츠01/02/03/04/05/27, 체크리스트/범위와 `PassengerBehavior`, `DriverCondition`, `EconomyLedger`, `PassengerDemandPolicy`, `StopOccupancySystem`, `TaxiSignals`, `AssistedDriveController`, `DrivingCamera`, `PvpSimulationBotServices`다. `TryGetAlternative=false`, 독립 포만 서비스 판단 부재, 수동 회전 해제 관찰 부재 등을 현재 구현/확장 경계로 대조했다. 과거 별도 PVP 원안은 현 물리 원격 수용 완료로 표시하지 않았다.

- `python tools/annotate_mindmap_progress.py`: 종료0, `MINDMAP_PROGRESS_OK nodes=333 implemented50=160 planned25=1 needsPlan0=63`.
- `python tools/organize_content_mindmap.py --validate-only`: 종료0, 현재 사용자 노드/메모·외부 링크와397 Detail-Link 존재 확인.
- `pwsh -File tools/update_content_mindmap.ps1 -ValidateOnly`: 종료0,333노드/397링크·현재 사용자 원본 ID 보존.
- 재실행 종료0, `PROGRESS_IDEMPOTENCE_OK` 원본/목차 해시 불변. HEAD에 마킹만 적용한 커밋 후보도340노드·동일160/1/63으로 검증 종료0(`STAGED_PROGRESS_OK`).
- 변경 Markdown5파일의 상대 링크59개 존재 검사 종료0(`PROGRESS_DOC_LINKS_OK`), 관련 변경 diff 검사 종료0. Git 인덱스에는 원본/목차/LATEST의 이번 변경만 별도 blob으로 넣어 기존 사용자 편집과 UI 이력을 분리한다.

네이티브 앱에서 실제 체크 아이콘의 화면 렌더링 검사는 미수행이다. 기존 스키마의 `task.progress`와 태그/메모/목차로 값을 확인했다. 새 Unity 빌드·자동 물리 주행·수동 캠페인 검사는 수행하지 않았다. 50%는 구현 단계이며100% 검증·수용 완료가 아니다.

## 다음 작업·실행 기준

우선 상황 반응형 승객의 실제 이벤트 조건을 기획하고, 기획25%인 상태 예측을 구현 후보로 검토한다. 안전 대체 승하차는 장소 쌍과 보행/평가 계약을 먼저 확정한다. 기타 미구현은 우선순위 표의 선행 기획을 따른다. 후보 순위/25%는 사용자 채택 또는 구현 시작을 뜻하지 않는다.

배포 기준은 기존 통합 `builds/EungyeTaxi/EungyeTaxi.exe`/은계 싱글·자체 도시PVP/`PrototypeBuild.BuildHybrid`다. 직전 배포 manifest 소스61c9e98·140파일115,186,267바이트/Strict issues0 인계를 유지하며 이번 작업은 지도·ZIP·배포를 갱신하지 않았다. 다른 작업이 수정 중인 로컬 실행폴더의 배포 검증 상태를 이번 마킹의 결과로 주장하지 않는다.
