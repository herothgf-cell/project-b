# 거리뷰 신호·차선·승객 응대 후속

## 요구와 기준

사용자 이미지1/2의 좌회전 신호,2의 차선별 로드뷰 대조,3/4의3구 신호, 점수 알림과 내비 겹침, 기사 선택 후 승객 반응, 활동 요청 빈도와 큰따옴표 제거를 다룬다. 사용자 답변으로 새 요청은 주행 중에도 표시하며 실제 작업의 정차 조건은 유지한다. [접근별 근거·기획](../content-design/20-roadview-signals-passenger-feedback.md)·[계획](../superpowers/plans/2026-11-09-roadview-signals-reactions.md).

시작은 main91709c1 기반 `codex/roadview-signals-passenger-feedback`. 동시 제안서 V8 커밋09a195f가 같은 작업 트리에 추가됐으며 그대로 보존한다. 사용자 편집 V6 PPTX와 기존 미추적 파일은 이번 커밋 대상에서 제외한다. 은계/A2 통합·LANv16·`PrototypeBuild.BuildHybrid`·`builds/EungyeTaxi/EungyeTaxi.exe`를 배포 기준으로 유지한다.

## 실제 구현

- `TrafficSignalDemand`: 명시 방향지시등 우선. 정지선 전40m/current lane/활성 연결을 검증한 전용 좌회전 차로 또는 유효한 현재 내비의 공유 좌회전 수요를 보완한다. 기존 보호 신호 단계·권한을 유지한다.
- `SignalHeadPresentation`, `TrafficSystem`, 시설/검증기: 기존 문서의 `signalHead`로 접근별 외형 지정. 미검토 접근은 기존4구, 작성 신호는 `vehicleSignal.type`. 월드/HUD의 같은 식별자를 사용하며 메타데이터로 중복 기둥을 만들지 않는다.
- `ReviewedNativeLanePresentation`, `LaneDirectionPaint`, `RoadPaintGeometry`: 확인한 NGII91/1/2 입력 약10~13m 안의 좌회전1/직진2 화살표·내부 흰 구분선. 겹치는 임시 역할 표시만 대체, 중앙선/대향/우회전 곡선/graph를 보존한다. 상류 도로 전 구간 재시공이 아니다.
- `RoadviewMapEdits`: 공식 저장기의 stale-token/검증/해시를 거쳐 시설 외형6개를 명시하고 상가 양방향 신호기를3구로 변경한다. 북향 head는 실제2024-03 근거, lane은2026-05 근거. 상가 서향은 사용자 지정이며 전면 현장확인으로 표시하지 않는다. 재실행은 시설 중복 없이 교체하되 revision/savedAt은 갱신하는 명시적 버전 작업이다.
- 승객 behavior/request/situation의 선택 결과는 NPC 반응을 카드에 표시한다. 기사 응답 기록/선택 효과/저장 필드 보존. 바깥 쌍 큰따옴표 제거·문장 속 인용 보존.
- 요청0/1/2건15/60/25, SafeStop3·DestinationChange2·ViaStop2. 주행 중 표시 허용, 실제20초 간격/진행도/응답·수면·모달·작업 차단 유지. 적재/재고정/휴식 등 정차 조건 유지. 기본 프로필/카탈로그/런타임 사본과 검사 갱신.
- `DriveNotifications`: 실제 내비 아래12배율픽셀 간격. `BrightUIProbe`에 실점수 알림·두 시점×3해상도 비겹침 검사/캡처 추가.

## 수행한 검사

- 실제 지도 Q-on 기준8개 통과, Q-off0 수요를 재현한 뒤 새 기대에서 실패: `artifacts/roadview-signals/left-demand-red-fixed-fixture.log` exit1. 최초 `left-demand-red.log`는 fixture CS0200 컴파일 실패여서 제품 RED로 계산하지 않는다.
- 수정 후 `LEFT_DEMAND_REGRESSION_OK checks=69`: 두 위치 Q-off수요1, 경쟁 수요에서 보호 좌회전 녹색 및 잘못된 차로/목표/연결/UTurn 추론 차단.
- head GREEN+native paint RED: `head-green-paint-red.log` exit1, 외형 검사 통과 후 새 helper 없음의 정확한 기대 실패.
- 첫 정식 적용 `paint-green-map-apply.log` exit0:69개/`GENERATED_SIGNAL_HEAD_OK`/native paint fixture/기존 glyphCases49·lanes300·junctions73, 실제 nativeRoles3·stops24·방향 경로552 통과. 이 뒤 북향 촬영월·정확한 상가2신호 precondition 및 일반화 U턴 최소공간 guard를 추가하여 최종 재검사를 진행한다.
- 승객 반응·주행 요청 순수 회귀158개 exit0. 고정1만seed/가용14종 모델에서 기본 배정 슬롯8937→10934, 새 주행 게이트상 표시8920→10903. 실제 사람/Player 발생 통계가 아니다. 기존 안전정차 게이트에서 연속주행 표시0을 재현했다.
- Node 최초 샌드박스 실행94/100은 로컬 서버6개 EACCES로 실패. 권한 있는 동일 검사 재실행100/100 exit0: `content-editor-tests.txt`. 제품 수정으로 해결한 실패가 아니다.
- 기존 전체 prototype의 일반 S제동 기대값 불일치(actual8.823529)는 이전 main에서도 알려진 실패이며 이번 물리 수정 범위가 아니다. 전체 검사를 통과했다고 표시하지 않는다.
- 독립 검토에서 probe의 이전 따옴표 기대값, 임의 GUID wrapper 불변해시, 근거 월/URL 불일치, 비어 있는2head 검사의 오판을 찾아 수정했다. 검토자들은 제품의 새 통행 권한 추가나 승객 응답 효과 변화는 발견하지 않았다.

## 최종 실행본 검사·저장 보존

- 별도 Candidate 빌드 exit0로 전체 HUD/설정/체크포인트 회귀를 확인했다. 최초 별도 Player의 activities 경로 검사는 출력 폴더가 한 단계 더 깊어 실패했으며, 테스트의 작업 경로를 맞춰66+6 통과했다. 제품 저장 경로를 바꾸지 않았다. 최초 roadview48+보행 신호2개 혼입 검사 실패는 probe가 같은 프로그램의 보행 신호까지 RGB 검사한 원인으로, 차량 신호 필터를 보완했다.
- 실제 상부 화면에서 좌회전 화살표를 관통하는 합성 중앙선과 원본/보완 흰 경계 중복을 발견했다. 새 fixture의 정확한 RED(overlap-red.log exit1)를 확인한 뒤, 검토 차로 내부 합성 Centre line만 제외하고 이미 원본503/506 흰 경계가 덮는 보완선은 생략했다. 원본 중앙선 두 줄·대향·구역 밖·도로 가장자리와 수입 노면선은 보존한다.
- 최종 `RoadviewRevisionBuild.Release` → `PrototypeBuild.BuildHybrid` exit0 (`release-build.log`). 새 중복 회귀·기존 glyph49/lanes301/junctions76·실제 정차지24/552방향 경로 통과. 정식 기본 exe와 모든 의존 파일을 갱신했다.
- 최종 기본 exe의 roadview Player exit0/48, 전체 UI exit0/169. `final-ui-roadview/`와 `final-ui-full/`을 생성하고 상부 원본 차선·3구 월드/HUD·내비 아래 점수 안내를 직접 열어 확인했다. 네 접근은4/4/3/3이며 북향 보호 좌회전 녹색도 실제 기존 단계로 확인했다. 고정 포즈/신호 단계 검사이며 사람 운전이 아니다.
- 최종 `tools/test_passenger_activities_player.ps1` exit0, 실제 요청/반응66·새 프로세스 복원6. 주행 중 요청·실제 응답·다른 선택/작업 차단·짐 재고정의 정차 전용 진행·NPC 반응을 검사했다. `artifacts/passenger-activities-player.txt`, `artifacts/passenger-activities-reload.txt`.
- 최종 `tools/test_pvp_native_lan.ps1 -Port18959 -Reconnect -TimeoutSeconds360` exit0. 호스트54·참가자67, 실제 네이티브 입력/WheelCollider 권한·재접속·날씨/사고/상태 회귀. 같은PC loopback이며 다른PC LAN은 아니다. `artifacts/pvp/native-lan-20261109-132921-8490a671/`.
- 사용자가 종료한 뒤17개 보호 파일을 byte 백업했다. 로컬 사용자 프로필의6대상 중5실제값(count1은이미60)을 변경하고 revision/정상contentHash를 갱신했다. 나머지 설정은 동일, 다른 사용자 저장16파일 불변. 테스트 기준을 의도된 프로필 변경 이후로 잡아 최종17/17불변을 확인한다. 사용자 저장/프로필 byte는 artifacts에만 보존하며 Git에 포함하지 않는다.
- 원본 지도 대조 결과 baseline/모든 lanes/scene/작성 signals가 동일, 시설6개 추가·기존 상가2head 외형만 변경했다. baselineHash=81b34b282c27bb03e2ead75ba3385d2c4e5e2bdd0730c53fcbe14fb8f30aebbe, contentHash=b36ac96bb5aa8d9ade794916b00c607fb0aed6cdcb3a2d537e46a69dc7a4a6b7. 프로젝트 입력21,622,773바이트/SHA256=5bee977ca763578698e27f3e4458563ce62cedd5b90336434713495ea1cce180. 필수 local-input-manifest 갱신 후 Strict exit0/issues0.
- 재수입 ZIP은 원본2JSON이 그대로여서 변경하지 않았다. SHA256=50a2e4d92ccc42a39927490060d884be46505dd917c6a46bf9fa45883a3d3d51 일치. OSM 비교/기존 통행 자료도 불변이다. 배포 manifest와 main 게시 영수증은 실제 명령 후 아래에 추가한다.

사람 장기 수동 운전·전체3일·다른PC LAN·현장 실측·상가 서향 전면 별도 확인은 미검증이다. 로드뷰 촬영일은2024-03/2026-05이며 확인일2026-11-09와 구분한다. 재수입용 기존 지도ZIP의 원본 JSON2개는 이번 시설 메타데이터 수정 대상이 아니며 Git 프로젝트 지도를 우선한다.

재현: Unity2022.3.62f2 `-batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod RoadviewRevisionBuild.Build`; 순수 검사는 `pwsh -File tools/test_passenger_reactions.ps1`, `node --test tests/content-editor/*.test.mjs`. 실제 Player는 위 CLI/결과와 `-brightUiCheck -roadviewSignalReview` 또는 `-brightUiCheck -fullUiReview -brightUiOutput <출력폴더>`로 재현한다.

## 출하 파일 확인

- 소스·지도·기획/이력 최초 커밋은 `dfdea9687129268e1149ac9b5a4981a4be9f9aa5`이며 커밋 직후 `git log -1 --oneline -- 본 이력`으로 확인했다.
- 이40자 소스로 통합 manifest를 다시 생성했다. 출하143파일/127,323,898바이트, 로컬 선택 프로필을 포함한 전수 검사는144파일/127,456,680바이트다. 통합 및 PVP 위임 VerifyOnly 모두 exit0. 빌드 재생성으로 이전 level0.resS는 없어졌고 현재 전체 폴더를 기준으로 배포한다.
- 최종 보호17파일 SHA256 전부 테스트 기준과 동일하다. 캠페인 등16개는 사용자 종료 직후 백업과도 동일하며, 사용자 프로필만 승인된6대상 중5값/revision/contentHash의 의도된 변경이다. 기본 게임은 강제 종료하지 않았다.
- 원격 main4245255가 현재 작업의 조상임을 fetch 후 확인했다. 기존 main91709c1과 동시 제안서09a195f를 보존하며 일반 fast-forward/main push 영수증은 실제 명령 후 추가한다.

## 실제 main 게시 영수증

- 출하 커밋 `906a89aab9ab21990c12c134b007b187dc27fa22`, 검증 소스 `dfdea9687129268e1149ac9b5a4981a4be9f9aa5`. 각 단계에서 이력 파일의 실제 git log를 확인했다.
- 기존 로컬 main91709c1에서906a89a로 fast-forward하여 제안서 V8 커밋09a195f도 보존했다. 원격4245255 →906a89a 일반 push 성공, `git ls-remote origin refs/heads/main`이 동일40자 해시임을 확인했다. force/reset 없음.
- 열린 V8 PPTX 때문에 첫 switch에서 unlink가 거절되어 main에서 untracked로 남았고 병합이 중단됐다. 실제 파일 blob6216fbea507b5adbfd0272f6e090b73dcbe5f6b9이 병합 대상과 동일함을 확인하고 그 파일만 추적 상태에 반영한 뒤 fast-forward했다. 열린 파일의 내용은 변경·이동·삭제하지 않았다.
- 사용자 편집 V6 PPTX·열린 V8 잠금 파일 및 기존 미추적 자료를 보존했다. V6 변경과 로컬 사용자 프로필은 소스/출하 커밋에 포함하지 않았다.
- main 전환 후 통합 manifest 전수 검증 exit0, 필수 입력 Strict exit0/issues0, 보호17파일 불변을 재확인했다. 이 영수증 후속은 게임·지도·배포 manifest의 sourceCommit을 변경하지 않는다. 최종 원격 head는 문서 영수증 커밋일 수 있다.
