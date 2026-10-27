# 플레이 피드백 반영·검증

## 요구사항·작업 기준

사용자 스크린샷13장과 추가 PS08 제외 요청을 반영한다. 사용자는 세부 수치 선택을 위임하고 즉시 진행을 승인했다. 현재 폴더 새 브랜치 `codex/playtest-feedback-20261027`, 시작 `abc1c4f`. 이전 작업의 main 병합·push는 이미 완료된 별도 작업이며, 이번 변경은 새 브랜치에 보존한다. 마인드맵·기존 사용자 변경과 루트 Unity 관련 미추적 디렉터리는 제외한다.

계획: [피드백 작업](../superpowers/plans/2026-10-27-playtest-feedback.md). 기본 지역/빌드: 은계, `PrototypeBuild.BuildHybrid`, `builds/EungyeTaxi/EungyeTaxi.exe`.

## 구현·결정

- 시작 화면 표시를 사용자 지정 `택시 시뮬래이터` / `지도를 준비 중입니다.`로 변경. 이후 도로/교통·보행자/영업 단계별 진행 문구는 유지한다. 저장 폴더명·제품 저장 경로는 바꾸지 않는다.
- 일반 게임용 도로변 횡단 안내판을 해당 방향 신호함 위로 이동. 이미 신호 위에 비보호 표지가 있으면 중복 생성하지 않는다. 현장 표지 실측 위치 보장 아님.
- 지역명·내비 회전 제목 보통 굵기. 승하차 핀에 도로 진행 방향 화살표와 지도 범례 추가.
- 시설 밖 F7은 메뉴·시간 정지 없이 M 안내. 주유소는 현재 시설 주유·세차만, 식당은 식사만, 휴식 거점은 해당 서비스만 표시한다. 시설 능력은 지도/F7/실행 검사가 공유하며 현재 주유소3곳의 세차 지원은 유지한다.
- 서비스 목적지·알림은 기존 좌측 TIP에 합쳤다. 픽업 중 M에서 주유소 길안내는 명시 선택으로 가능하고 호출·대기시간을 유지한다. 탑승 중 시설 안내는 허용하지 않는다.
- 새 운송은 저장된 후보 ID로 여유/보통/급함. 안전 경로 ETA×1.75/1.45/1.20 + 상황 대기 +45초, 최소90초. 픽업은 거리/3+150초로30초만 축소. 기존 진행 약정·기한은 유지, 경로 변경도 난도를 재추첨하지 않는다.
- 우회전 단순 정지 누락은 운전 점수5/M4, 전용 적색·보행 위험 배점 유지. 역주행은 차로 근거·전진속도2m/s·2초/8m 지속에 운전 점수10/M10, 연속 사건1회. 후진·주차·교차로/유턴·불명확/근접 평행 차로 제외.
- 적색 정지선 침범 후 정차는 운전 점수1만 차감. 같은 접근의 큰 위반으로 이어지면 사건 최대 감점으로 갱신한다. 동일 ID 갱신을 저널·즉시 알림에 전달한다.
- PS08 운전 계획 팝업/선택 노출 제외. PS09와 일반 도로 대기열·법규는 유지. 구형 열린 팝업은 다시 열지 않고 과거 명시 선택 우회는 진행 호환을 유지한다.
- 내비 우회는 [진단](2026-10-27-navigation-playtest-diagnosis.md)에 실제 차로 ID·거리로 정리했다. 차로변경 창이 도로 초입10~42m 한 구간뿐인 그래프 한계이며, 일방통행이나 탐색 최단비용 오류로 단정하지 않는다. **우회 자체를 해결한 변경은 아니다.** 안전한 후속 변경 연결 작성·확인이 남는다. 지도/입력 manifest/ZIP 미변경.

## 검증 증거·수정 전 실패

전체 회귀/빌드는 개발 후 묶고 변경부 회귀만 최소 실행했다. Unity는 한 번에 한 프로세스만 실행했다.

- `tools/test_prototype.ps1`:24개 모델 묶음 종료0(픽업·시간 난도·법규 사건 포함).
- `tools/test_passenger_care.ps1`:13,301단언 종료0.
- `tools/check_workspace.ps1 -Strict`: 종료0, Input issues0·선택 import2개 없음. 지도 contentHash `44ebdedb71e84f436fd15e457841473e1d2761a66989a3bcf5fc50d01654b895` 유지.
- `feedback-ui-red.log`: 시설 밖 F7 전체 메뉴 문제로 의도한 실패.
- `feedback-rules-presentation-red.log`: 신호 상부 표지·배점·시간 정책·신규 관찰자 누락 실패. 초기 PS08 fixture는 business 미초기화 오류여서 보완 후 `feedback-focused-interim.log`에서 `PS08 never offers removed driving-plan popup` 실패를 확인했다. 중간 `feedback-choice-red.log`는 병렬 코드 작성 중 Snapshot 형식 미완성으로 컴파일 실패였으며 제품 회귀 결과로 세지 않는다.
- `feedback-pickup-map-red.log`: M 주유 미리보기는 통과하나 실제 안내 시작이 막히는 오류 실패→수정.
- `feedback-focused-check.log`:메뉴·픽업주유·표지·PS08·지도 진단 통과. 같은 ID 사건 갱신의 Revision/즉시 알림2건 실패→수정.
- `feedback-route-extension-red.log`: 같은 거리 목적지 변경에서 불필요한 기한45초 추가 오류 실패→수정. 기본 요청 여유30초만 보존.
- `feedback-final-editor.log`:최종 집중 및 기존 안정화·저장·지도·시설 통합 검사 종료0. 후속 독립 검토 발견의 저장/복귀 경계는 아래 후속 검사로 별도 완료해야 한다.

## 독립 검토·후속

단일 읽기 전용 리뷰를 진행했다. 주유 우회 후 실제 픽업 복귀, 미확정 M 교차로 사건 저장, 정상 정지 후 다시 선을 넘은 침범, 더 최신 저널의 동일 ID 갱신을 후속 회귀로 다룬다. 적색 침범 후 녹색을 기다렸다가 정상 진입한 경우도 단순 침범1점에 그쳐야 한다.

리뷰의 시작 문구 지적은 미채택: 사용자는 기존 `은계 지도` 문구의 교체를 요청했고 후속 단계 안내 삭제를 요청하지 않았다. 새 초기 문구와 이후 실제 준비 단계 안내를 함께 유지하는 것이 더 정확하다. 오판 비용은 문구 선호 차이이며 저장/주행 영향은 없다.

후속 검토의 픽업 복귀·미확정 M 관찰 저장·정상 정지 후 재침범·저널 동일 ID 업그레이드/구형 저널 무감액·적색 침범 후 녹색 출발을 실패 회귀로 재현하고 수정했다. `feedback-review-findings-red.log`의 실패는 수정 전 증거다. `feedback-review-onepass-check.log`에서 앞선 수정은 통과했고 M의 녹색 출발 일관성1건이 남아 이를 함께 수정했다. 최종 `feedback-final-build.log`의 `PLAYTEST_FOCUSED_OK`/`PLAYTEST_FINAL_EDITOR_OK`와 BuildHybrid 모두 종료0.

실제 렌더 검증에서 정식 지도는 시작부터 비보호로 설정된 교차로가 아니라 상황 시작 때 해당 모드로 바뀐다는 점을 확인했다. 신호 상부 보드가 활성 접근/모드 변화를 따라 갱신하도록 하고 이전 별도 도로변 비보호 글씨는 없앴다. `feedback-dynamic-sign-red-fixed-fixture.log`에서 보호→비보호 전환 실패를 확인한 뒤 `feedback-final-presentation-build.log`의 집중 검사·BuildHybrid0으로 수정했다. 최초 동적 표지 fixture의 열거형 이름 오류(`Protected` 대신 `ProtectedShared`)는 테스트 코드 컴파일 오류이며 실제 제품 오류와 구분한다.

`feedback-player-ui.log` 최초 위치 제어 도구는 legacy 테스트 모드에서 계정·서비스 컴포넌트를 초기화하지 않아 Places의 NullReference와 빈 메뉴 캡처가 있었고, 시작부터 비보호 교차로가 있다고 가정해 종료1이었다. 실제 게임 일반 모드는 계정이 준비된다. 도구를 `-hybridV2`로 실행하고 활성 비보호 상황을 실제 시작하는 것으로 바로잡았다. 최종 `feedback-player-ui-final.log`: **14검사·종료0·실행 예외0**. 시작 문구, 식당1행, 주유/세차2행, TIP 통합, 비보호 신호 상부 표지, 지도1280/1920의 화살표를 실제 픽셀로 확인했다.

최종 빌드 실행의 `feedback-player-passengerCareTest.log`:201검사/종료0, `feedback-player-scenarioTest.log`:기존 R상황 검사 종료0, `feedback-player-liveSaveTest.log`/`liveReloadTest.log`:9/23검사·각 종료0. 이는 위치/상태 제어·별도 프로세스 복원이며 자동 물리 주행이나 사람의 전체 캠페인이 아니다.

9코스 전용 검사 첫 실행은 PS01~09·PS08 미노출·PS09 유지가 통과했으나 끝부분의 오래된 만료 fixture2건이 실패했다. 사용되지 않는 `pickupSeconds=1` 필드를 넣고1초만 소모해 만료로 가정하던 테스트를, 실제 남은 픽업 기한을 소비한 뒤 만료/내비 해제를 검증하도록 수정했다. 게임의 타이머·기한은 테스트를 위해 바꾸지 않았다. 최종 재검사·배포 manifest 결과는 아래 마감 절을 따른다.

시각 확인의 작은 잔여: 밀집 주유/휴식 시설과 활성 승차 핀 근처에서 시설 능력 배지가 일부 겹친다. 시설 상세를 선택하면 전체 서비스 구분을 확인할 수 있으며 별도 라벨 충돌 배치 개선은 후속이다. 현재3곳 모두 세차 가능하다는 사실은 유지한다.

리뷰가 별도 판단하지 않은 영역은 주 작업자가 실제 화면·빌드·저장/실행으로 확인했다. 실제 도로 규제 전수 검증/지도 연결 보수와 새 역주행 관찰의 대표 장시간 프로파일은 수행하지 않았고 해당 성공을 주장하지 않는다. 사용자 기존 마인드맵 SHA256 `F1951492F9C5A13941E60A17D92EB31B1A7471FB8ABA7996D47A0C2E02F00937` 보존.

## 마감

`feedback-deployment-build.log`:최종 BuildHybrid 종료0. `feedback-player-prototypeScenario-final.log`:9코스·PS08 미노출·PS09 유지·실제 기한 만료/내비 해제 종료0, FAIL0. 최종 배포 Assembly-CSharp.dll SHA256은 `ACBE4201E8848A1920CB513B35B2650BC9D249D265DD65DA4623C7347CB60642`다. 표시/운송 코드는 앞선 UI14·승객201·저장9/복원23 검사와 동일하며 마지막 빌드 변경은 코스 검사 fixture뿐이다.

소스·문서 커밋 `7a222002b3b2578ee0810a0e2e4ed30ae39ba2b0`으로 배포 manifest를 생성했다. `tools/update_distribution_manifest.ps1 -VerifyOnly` 종료0: **130파일·105,710,114바이트 전체 크기/SHA256 일치**. 최종9코스는99검사·FAIL0·종료0. 실행 폴더와 manifest/인계 문서를 함께 별도 배포 커밋한다. 현재 작업 브랜치에 보존하고 main 변경/push는 하지 않는다. 실제 이력 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-27-playtest-feedback.md`로 확인한다.

## 재현 방법·제한

Unity 로컬 설치는 이 PC의 `C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe`. 프로젝트 `unity/EungyeTaxi`에서 `-batchmode -nographics -quit -executeMethod PlaytestPresentationTests.RunFinal`로 통합 검사한다. 빌드 후 저장과 분리된 `-hybridV2 -feedbackUITest -screen-width 1280 -screen-height 720 -screen-fullscreen 0`는 위치 제어 UI 검사와 `artifacts/feedback-*.png`를 생성한다. **사람의 수동 운전·경제 체감이나 전체 캠페인 검사를 뜻하지 않는다.**

미확인: 후속 차로변경 지도 연결, 사람의 시간 난도·수익·표지/방향 인지 체감, 다른 물리PC/패드. 이번 작업으로 전 구간 주행 안정성 또는 실제 도로 법규 전수 검증을 주장하지 않는다.
