# 2026-11-07 HUD 사용성 후속

## 요구·구현
첨부 번호1~5에 따라 빈차 중앙 안내 제거, 호출 버튼을 승객 카드로 이동, 생활거점 자동 열림 제거 및 연료/포만25 이하·피로75 이상·내구도50 이하 레드닷, 내구도 우측 수리 아이콘 적용. PVP 현황을 좌측4행으로 확대하고 대화 중 유지. 수동 짧은길/빠른길 버튼·Tab 숨김(작성된 승객 요청 효과 유지). 밝은 카드/남색 글자, 반원 속도계 채움 개선. 퇴근 거절 사유 노출, 도움말 그룹화, 숫자1~n 선택, 앞차 추종/모드 선택을 일시정지 메뉴로 이동.
PVP 방 생성에서 피로배율·식사/휴식 비용·시간 설정. 기본1.25·1000원/15초·무료/45초 보존. LANv14 호스트 설정 검증/동기화. 설정은 접이식과 명시적인 −/+ 버튼.

## 변경 파일
CockpitUI/DrivingHudArt/DrivingReadability/ServiceStops/DriveHUD/DrivingSettingsPanel, 승객 선택 UI와 NumericChoiceInput, PvpConditionRules/PvpNativeLan/PvpPrototypePanel 및 장부/봇 서비스. 관련 Editor/Player 검사, 기획05·체크리스트·범위와 배포본.

## 현재 검증
첫 통합 Editor/BuildHybrid exit0, 숫자137·메뉴16·기존 HUD/신호/선택효과72 포함. 첫 실제 Player208 PASS(1280/1920, 속도0/35/70/140·후진), Strict issues0. LAN 두 프로세스 host40/client51 PASS: artifacts/pvp/native-lan-20261107-010139-739c9483. 순수 방설정26·운영판단49·adapterall(담당 검사). 독립 검토로 밝은 대화 제목 대비/안내창 겹침 수정. JsonUtility 누락 객체 생성으로 첫 검사 실패→원본 JSON 필수 필드 검증 후 통과.
실제 화면에서 방 설정 slider 미표시 확인→−/+ 버튼으로 수정했으며 최종 캡처/통합 검증은 출하 영수증에서 확정한다.

## 제한·다음 작업
고정 상태/위치 제어 UI 검사와 LAN 물리 입력 검사이며, 사람 수동 운전·전체3일·다른PC LAN은 미검증. 남은 디자인: 월드 건물/도로·차량 아트, 캐릭터 표정과 반응, 경고/정산 연출, 실제 주행 중 장시간 정보 밀도·접근성 수용. 지도/수치 기본값/사용자 저장을 유지한다.
실행: builds/EungyeTaxi/EungyeTaxi.exe (은계/A2, PrototypeBuild.BuildHybrid). 검사: DrivingHudConceptChecks.Build; -brightUiCheck -fullUiReview -lifeHubUiReview -hudDialogueReview. 산출물 artifacts/hud-usability/. 필수 입력 Strict issues0, 지도/ZIP 변경 없음.
