# 원본 컨셉에 가까운 주행 HUD 2차 적용

## 요청·구현

사용자가 원본 컨셉 비교 후 제안한 2차 주행 HUD 시안과 적용을 진행하도록 요청했다. 실제 게임 화면을 시안 결과물로 사용한다. [계획](../superpowers/plans/2026-11-06-driving-hud-concept-v2.md).

- 남색 반투명 패널의 좌상단 시간/잔액/5종 상태, 상단 중앙 내비, 지도 아래 운송 정보, 중앙 하단 반원 속도계와 기어/방향등, 좌하단 큰 승객 초상/만족도, 우하단 점수/기존 메뉴.
- 승객 정체성과 기존 4종 초상 유지, 대화 초상 확대·남색 제목·선택/효과 구분과 긴 내용 스크롤. 속도계 위에 대화 패널 배치. 긴 취객/세차 안내도 말풍선 스크롤로 원문 보존.
- PVP 현황, 피로 안내, 생활거점 패널을 새 HUD와 겹치지 않게 배치. 경로 복귀/시설 출구 안내와 3구/4구·현장 근거 표시 유지. 차량/도시 모델·새 캐릭터 표정 아트는 이번 범위 밖이다.

## 검증 과정

`artifacts/hud-concept-v2/`에 로그와 실제 캡처 보관.

- red.log: 새 중앙 계기판 위치 API 부재로 의도한 실패. build.log: 대화창이 계기판을 덮는 배치 실패 확인 후 수정.
- build-2.log 및 review-build.log: 통합 빌드 exit0. HUD 배치12/기존 대화·신호16/선택효과72와 기존 메뉴·캠페인·내비·지도 신호 회귀 통과.
- 최초 숨긴 Player: 182검사 중 시작 메뉴/준비 화면 캡처2개 실패. 후속 실제 창 실행에서 해당 캡처는 정상 확인했다.
- 독립 리뷰에서 PVP 피로/생활거점 겹침, 긴 care 문구, 경로 복귀 문구4건을 확인했다. review-red.log에서 보조 패널 배치 검사 실패를 확인한 뒤 수정했다. 재리뷰에서4건 해결/새 중요 문제 없음.
- final-player.log:188검사 중 care 말풍선 스크롤2건 실패. 검사 오버레이보다 먼저 그려지는 일반 승객 카드가 매 프레임 scroll identity를 초기화하는 원인을 확인했다. 연속 렌더 프레임 기준 초기화로 수정했다. 최종 통합 재검증은 아래 영수증에 기록한다.

## 통합·보존·남은 항목

작업 중 원래 실행폴더에 승객 조합 기능 df7ef19가 추가되어 해당 변경을 합친 뒤 다시 빌드한다. 기존 미추적 승객 override 계획/테스트와 사용자 자료는 건드리지 않는다. 사용자 저장16파일의 시작 해시를 기록했고 중간 비교 변화0.

수동 주행·전체3일 캠페인·다른PC/실물 패드 수용은 미검증. 이번 Player는 격리 저장과 위치/상태 제어 검사이며 자동 물리 주행이 아니다. 실제 현장3구 신호 자료 추가 조사 없음. 기존 지도/ZIP은 그대로다.

## 변경 파일·재현

코드: CockpitUI, DrivingHudArt(신규), DrivingReadability, PassengerDialogueUI, PvpConditionHUD, ServiceStops. 검사: DrivingHudConceptChecks(신규), BrightUIProbe, MapInteractionProbe. 계획/기획05/체크리스트/이력과 대표 캡처·배포 manifest를 함께 남긴다.

실행 `builds/EungyeTaxi/EungyeTaxi.exe`, 은계 싱글/A2 PVP, 빌드 `PrototypeBuild.BuildHybrid`. 로컬 Unity `D:/Program Files/Unity Hub/Editor/2022.3.62f2/Editor/Unity.exe`에 `-batchmode -nographics -quit -projectPath <root>/unity/EungyeTaxi -executeMethod DrivingHudConceptChecks.Build -logFile <log>`를 사용한다. Player는 `-brightUiCheck -fullUiReview -lifeHubUiReview -hudDialogueReview -brightUiOutput <folder> -screen-fullscreen 0 -logFile <log>`다. 신호 화면은 `-brightUiCheck -signalCountdownOnly`로 별도 확인한다.
