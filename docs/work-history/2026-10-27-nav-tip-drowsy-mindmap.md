# 2026-10-27 내비·TIP·표지·졸음·마인드맵

## 요구사항과 범위

국도42 승하차1→게임 승하차2의 과도한 우회 원인 수정, 승하차 점유 안내를 왼쪽 슬라이드 TIP으로 통합, 신호 상부 표지 가독성, 졸음 중 간헐적 조작 불응. 후속 승인으로 마인드맵 원본과 전체 Detail-Links도 직접 갱신하고 main에 통합·푸시한다.

사용자는 직접 좌회전이 금지임을 확인하고 서쪽 유턴 구역을 표시했다. 새 좌회전 허용을 만들지 않는다. 세부 밸런스는 기존 위임에 따라 결정했다.

## 실제 변경

| 영역 | 변경·영향 |
|---|---|
| 내비 | 기존 NGII 서쪽 유턴 A2218G611778:0을 보존. 접근 직진과 복귀 우회전의 전진 연결2개, 기존 B2 점선 구간 안의 차로변경3개 보완. 원본 차로의 서로 다른 거리 원점은 세계 좌표로 정렬 |
| 제보 경로 | 재현2503.569m →703.277m. 북쪽 블록 순환 대신 서쪽 유턴→복귀 우회전. 교차로 직접 좌회전 금지는 유지 |
| TIP | 점유 설명/정차 대기 버튼을 기존 TIP에 통합. 별도 중앙 패널 제거. 왼쪽으로0.25초 슬라이드, 가장자리 TIP 탭으로 복귀, 스크롤/평가 유지 |
| 표지 | 전용 굵은 한글 글꼴/실제 글꼴별 텍스처 결합, 표지·글자 면적 확대, 조건 중복 축약. 신호등 상부 부착/간격 유지 |
| 졸음 | 초기 눈감김 중 시야 손실50% 이상에서 가속 중단·직전 휠 조향 유지. 제동 가능, 재개방 후 입력 복귀. 메뉴/정지 화면은 대상 제외.120초 안전 취소/180초 암전·당일 마감 유지 |
| 승하차 회귀 | 기존 남하 차로 편집의 인위적 분할이 EG-GAME-10을 탈락시키는 문제 발견. 같은 원본·연속 노드/끝점/거리/활성 차로만 복원해 검증하고 실제 소유 세그먼트로 정차 참조. 위치 스냅·기하 기준 완화 없음 |
| 기획 |03 생활·졸음,05 UI/TIP/표지,06 대조표와 입구 갱신. 개발 수단/실행 결과는 본 이력에 분리 |
| 마인드맵 | 기존 ID 전체 보존, 현재/후속/제작 참고 분리, 중복 규칙의 대표 가지와 기획 연결.290노드의 네이티브 Detail-Links290개를 현재 파일로 갱신 |

마인드맵의 시작 사용자 변경은 [변경 전 보존본](../design-history/content-design/2026-10-27-mindmap-before-update.mmst)에 바이트 그대로 보존했다. SHA256 F1951492F9C5A13941E60A17D92EB31B1A7471FB8ABA7996D47A0C2E02F00937. 현재 .mmst의 ID·레이아웃 속성은 보존하며 낡은 개발 이력 노트는 보존본으로 분리했다.

## 검증과 증거

| 검사 | 실제 결과 |
|---|---|
| 변경 전 모델 | tools/test_prototype.ps1의 기존27개 프로그램 종료0 |
| 집중 회귀 | 제보 경로2503m 상한 검사 실패 후703m 통과. 초기 눈감김 입력 차단 부재를 확인 후 신규 모델 검사15항목 통과 |
| 최종 모델 | 신규 DrowsyInputTests 포함28개 프로그램 종료0 |
| 그래프 보완 회귀 | EungyeConnectivityRepairTests:2549검사,25개 보완(차로변경21/전진4), 기존132방향 경로 유지 |
| 마인드맵 | update_content_mindmap.ps1 -ValidateOnly 종료0:290 ID/링크, 기존 사용자 ID 소실0. 시작 보존본 해시 일치 |
| 필수 로컬 입력 | check_workspace.ps1 -Strict: Input issues0, 정식 지도 해시76ebb91c8e5ae202f81c15a5df399375db2273157406eccc4b07ef258ebfe758 유지 |
| Editor 최종 | NavTipFeedbackTests.RunFinal 완료, NAV_TIP_FINAL_EDITOR_OK·PROJECT_MAP_TESTS_OK·UTURN_BLINK_FINAL_EDITOR_OK 확인, Unity 종료0 |
| 기본 빌드/Player | 사용자 종료 응답 후 BuildHybrid 종료0. 최초 Player의8개 구조 검사는 통과했지만 화면 확인에서 굵은 HUD 글꼴 깨짐 발견, 배포 보류 후 수정. 아래 후속 검증 참조 |

## 게임 종료 후 렌더 회귀 수정

Font.Instantiate는 글꼴 개체만 복사하고 원본 HUD Bold와 같은 material을 공유했다. 표지80픽셀 문자를 요청하면 공유 재질의 atlas가 바뀌어 HUD 글자 UV와 맞지 않았다. FontIsolation 집중 검사는 두 material ID가 같은17264임을 출력하며 종료1로 재현했다. 기존 Noto Bold와 동일 바이트/OFL인 별도 임포트 리소스 NotoSansKR-Signs.ttf를 두고 로드하도록 수정했다. material17268/17282 및 texture 분리 검사 FONT_ISOLATION_OK·종료0 확인. 별도 OS 글꼴 의존성은 추가하지 않았다.

최초 실행 검사 FatigueOvertimeProbe는38항목·종료0: 깊은 눈감김 중 실제 휠 가속0/조향 유지/브레이크 유지, 재개방 복귀, 메뉴 제외, 탑승객 정차 취소/0원,180초 입력 차단·귀가/피로-20·당일 마감·복원 포함. 이 결과만으로 글꼴 가독성까지 통과했다고 판단하지 않았다. 수정 빌드에서 다시 화면을 확인한다.

읽기 전용 최종 리뷰는 Critical/Important 지적0. EOF 공백 정리 후 diff --check 통과. 리뷰에서 실제 클릭/렌더/물리 유턴·사람 조작감/전체 캠페인은 자동 확인으로 대체할 수 없다고 구분했으며, 앞의 세 가지 중 렌더·실제 입력 검사는 후속 Player probe로 확인한다. 물리 유턴 전 구간과 사람 전체 캠페인은 미검증으로 유지한다.

로그/이미지는 Git 제외 artifacts/nav-tip-*, artifacts/fatigue-*에 둔다. 실제 검사 명령은 NavTipFeedbackTests.RunFinal, 기본 PrototypeBuild.BuildHybrid, Player -navTipFeedbackTest 및 -fatigueOvertimeTest -fatigueCapture다. Player 검사는 테스트 저장 위치를 사용하며 사용자 영업 저장을 덮지 않는다.

## 판단 기록·미검증

- Ruling: 직진 전용은 유지하고 사용자 제보와 기존 캡처 유턴을 활용한다. 현장 신호 조건의 신규 인증은 하지 않음. 잘못된 현장 제보라면 위치/규제 재대조가 필요하다.
- Ruling: 졸음 중 임의 가속/조향 대신 가속 중단·조향 유지, 제동 허용. 안전 확보를 위한 게임 규칙이며 실제 사람의 반응 체감은 수동 검토가 필요하다.
- Ruling: 이전 원안/사용자 메모는 제거하지 않고 이력 보존본으로 분리. 현재 가지 설명은 최신 기획 링크 중심으로 축약한다.
- 현재 폴더 새 작업 브랜치 사용은 기존 사용자 선택을 따른다. 중간 전체 테스트 반복 대신 집중 원인 검사와 마지막 통합 검사를 구분했다.
- 사람의 실제 저속 유턴/졸음 빈도·조작감, 실제 거리뷰 신호 조건, 전체20분/3일 경제, 다른PC/DPI/장시간 성능은 이번 자동 검사와 별개다.
- 생성 지도/필수 입력·배포 지도 ZIP은 변경하지 않았다. 일반 영업의 출구 대기열 팝업과 안전 대체 승하차 이동은 재도입하지 않았다.

## 변경 파일과 재현

unity/EungyeTaxi/Assets/Scripts의 EungyeConnectivityRepair·PassengerStopCatalog·ServiceLawZones·StopOccupancySystem·CockpitUI·DriverCondition·BusinessProgress·TaxiController·KoreanText·RoadSignLabel·RoadSignage·RoadMapFacilities·TrafficSystem, 집중 Editor/Player probe, tests/DrowsyInputTests.cs와 모델 실행기, 기획/마인드맵/변경 스크립트/보존본, 배포 빌드·manifest가 대상이다. 관련 없는 기존 untracked는 stage하지 않는다.

기본 실행은 builds/EungyeTaxi/EungyeTaxi.exe. 내비는 국도42 승하차1→게임 승하차2 경로를 비교한다. TIP은 가장자리 탭과 점유 중 대기 동작을 확인한다. 졸음은 피로100 이후 활성 누적30/50/70/90/110초의 눈감김,120초 취소와180초 마감을 구분한다.

자신을 포함한 커밋 해시는 기록에 추측해 적지 않는다. git log -1 --oneline -- docs/work-history/2026-10-27-nav-tip-drowsy-mindmap.md로 확인한다.
