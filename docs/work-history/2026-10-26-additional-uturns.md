# 추가 유턴 ④·⑤·⑥ 게임 적용 — 2026-10-26

## 요구와 구현
이전 개인 안내 지도 전용에서 사용자가 게임 적용을 요청했다. ④ J7254_14780 남서 계수로 진입, ⑤ J8066_6364 남쪽 은계로 진입, ⑥ J7973_3744 동쪽 수인로 진입에 유턴을 추가했다. 조건은 좌회전 신호 또는 보행 신호이며 기존 보행자/차량 안전 확인을 유지한다. 기존 T1 세 곳을 포함해 유턴 연결은 총6개다. 다른 접근은 허용하지 않았다.

AdditionalReviewedUturnMap.Apply가 재현 가능한3개 join·조건 표지·접근 방향 정지선을 생성한다. 기존 직진 링크를 보존하고⑥의 짧은 원본 진입 차로 A2218G611877:0만 J7973 규칙에 연결했다. 원본 baseline/T1/중앙분리대는 동일하다. 곡선·출구 차로·정지선 치수와 신호 주기는 게임 추정치이며 실측이라고 표시하지 않았다. 근거는 docs/map-observations/2026-10-26-additional-uturn-observations.md.

## 변경 파일
- config/maps/project/eungye.project.json:1807차로, contentHash44ebdedb71e84f436fd15e457841473e1d2761a66989a3bcf5fc50d01654b895.
- unity/EungyeTaxi/Assets/Editor/AdditionalReviewedUturnMap.cs 및 AdditionalUturnObserverTests.cs(+meta).
- unity/EungyeTaxi/Assets/Scripts/AdditionalUturnIntegrationProbe.cs(+meta), UturnIntegrationProbe.cs의 전체 개수6 갱신.
- 콘텐츠06, scope/checklist, 관찰 문서, 계획, 이력. 기본 builds/EungyeTaxi 및 배포 manifest.

## 실제 검증
- 첫 Apply/observer: 종료1. ④의 기존 축 기준 정지선이 곡선 안에 있어 표지 진입 판정 실패. 위치 진단 후 접근 방향 정지선을 곡선 전에 배치해 수정했다.
- AdditionalReviewedUturnMap.Apply (apply-aligned.log): 종료0,3경로/반경/해시 및 위치 제어 법규 검사 통과.
- AdditionalUturnObserverTests.Run (observer-final.log): 좌회전·보행 각각 허용/금지 구간, 반대 접근 미바인딩, 실제 위치 샘플 기반 법규·매너 유턴 사건 검사. 실행 종료 결과는 아래 최종 배포 절.
- PrototypeBuild.BuildHybrid: 종료0.
- 기본 exe -additionalUturnTest: 종료0. 플레이어/NPC 공유 그래프,3경로, 정적 차량 외곽61샘플 차단0, 각 NPC 허용 신호 진입/완주 통과(약6.1~6.4시뮬레이션초).
- pwsh -File tools/check_workspace.ps1 -Strict: 종료0, 입력 오류0. 원본 가져오기2개는 OPTIONAL 부재이며 원본 입력/ZIP/입력manifest는 변경하지 않았다.
- 최종 별도 코드 검토: Critical/Important 없음. 전체맵 nearest 차로·기존직진/T1 보존·정지선 중복 없음 확인.
- 광범위 회귀/전체 캠페인/실제 Rigidbody 자동 주행/사람 수동 주행은 이번에 수행하지 않았다. NPC 완주는 kinematic 교통 검사다.

## 실행·재현과 남은 항목
기본 builds/EungyeTaxi/EungyeTaxi.exe. 새 지도에서 M좌표 약725/1478,807/636,797/374에서 각각 위 진입 방향으로 접근한다. 표지 조건과 교통 안전을 확인하고 유턴한다. 개발 재현은 Unity2022.3.62f2의 AdditionalReviewedUturnMap.Apply, AdditionalUturnObserverTests.Run, PrototypeBuild.BuildHybrid 순서. 실행 검사는 -batchmode -nographics -additionalUturnTest이며 artifacts/additional-uturn-probe.txt에 기록한다.

기존 저장의 지도 해시가 달라지는 경우 현행 저장 호환 검사를 우회하지 않는다. 실측 정지선/신호 주기와 사람 주행 체감은 미검증이다. 로컬 검사 로그는 artifacts/additional-uturns/에 보존하며 Git 제외다.

## 검증된 독립 배포

소스 efc59e88db1aec8cf413f0c531c51c4589a13a98. 최종 observer 종료0. 배포 manifest 재생성/VerifyOnly 종료0:130파일105,636,520바이트. main의 다른 대화에서 시작/저장/운임 수정이 진행 중이므로 해당 변경을 보존해 통합 조율 중이다. 이 단계의 독립 빌드가 최종 공동 배포라는 뜻은 아니다.

## 최신 main 공동 통합

main84757ef(시작/구형 저장/운임 수정)을 병합하고 양쪽 기획·체크리스트·이력을 보존했다. 유턴 검사 도구는 StartupReady 후2프레임 기다리도록 맞췄다. 통합 BuildHybrid 종료0, 통합 exe -additionalUturnTest 종료0:3경로/정적61샘플 차단0/kinematic NPC3완주. 원래 작업의 운임·저장 검사는 해당 이력의 결과를 유지하며 이번에 전체 재실행하지 않았다. 통합 로그는 artifacts/2026-10-26-additional-uturns/에 보존한다.
