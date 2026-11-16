# 2026-11-16 색상 도로 조사 이어가기

## 요구사항·기준
- 색칠한 모든 도로의 양방향 차로/전환/시설을 확인하고 실제 지도 반영 후 BuildHybrid. 사용자 최신 지시: **푸시 금지**.
- 31번 A/B 유턴은 사용자 확인에 따라 **좌회전 또는 보행 신호 시**. 양쪽 L+U / S / S / S / 분리 R 5차로.
- 전체 조사 완료 아님. 거리뷰가 ERR_NETWORK_IO_SUSPENDED로 중단되었으며 미확인 구간은 추정하여 확정하지 않는다.

## 확인한 검증
- `r23-migration-green31-preview-2.log`: Unity 종료0. 저장 이관9버전130검사, Green31 조건부 U/양방향5차로/39정차·1482경로 검사 통과. 미리보기이며 아직 Green31 실제 저장 전.
- 작성 지도 r23, 실행본 r19에서 시작. FirstDrive 사용자 씬 SHA256 `1494FF1D97725151FE4E3EA3AF312A5019E68DD94D67A9F207C2BE42567D9B64` 보존 대상.
- J5074 회전교차로 Apply 검증 진행. 일반 sandbox 실행은 라이선스 IPC 연결 실패, 라이선스 연결 가능한 실행에서 재시도.
- 추가 생활도로 P08/P09/P17/P23/P24/P25/P27/P28의 실제 무차선 도색만 수정하는 후보 준비. 학교 구간 P16/P19는 황색 중앙선이 있어 제외.

## 남은 작업
- 실제 지도 저장 후 동일 검증, 저장 이관 및 필수 입력 manifest/Strict, 통합 빌드와 배포 manifest 검증.
- 모든 색상 전수 조사와 미확인 차로/분리시설/출입구별 정확한 전환점은 남아 있음. 조사 목록은 도로 목록이지 현장 확인 완료표가 아님.
- 전체 자동 물리 주행·사람 플레이·전체 캠페인은 이번 위치/경로 검사와 구분한다.
- 다른 작업 미커밋 파일/프로필/ZIP 보존. 커밋·푸시 없음.

## 실제 적용 결과
- r24 J5074 회전교차로: `roundabout-apply-r24-elevated.log` Unity 종료0, 저장 전후 모두39정차·1482경로 검증. hash `38bad072a26d23185eb9f902ea347eb84dbe34c7e554b22637916ee52645f8af`. 중앙섬·순환2차로, 봇 순환은 외측 차로에 한정하는 임시 배정. 크기/연결 곡선은 게임 추정이며 현장 실측 아님.
- r25 Green31: `green31-apply-r25-retry.log` 종료0, 저장 전후 양방향5차로·L+U/S/S/S/분리R·조건부U·39정차1482경로 통과. hash `970525154e204fdb578e57f1c63cb9c2b602ace906b8d56a0b68baf8bf4ec5fb`. 최초 시도는 추가 검사 파일 meta GUID33자 오류로 컴파일 실패, 유효 GUID32자로 교정 후 재실행.
- 생활도로8구간 Apply 첫 시도는 P23 중앙선4개 잔존 검사에서 종료1. **저장 전에 중단**, 현재 지도r25 유지. 원인 분석 후 재검증 대상.
- 11/16 거리뷰 재접속도 ERR_NETWORK_IO_SUSPENDED 재현. 네트워크/보안 설정은 변경하지 않음.

## r26 적용 및 중간 빌드
- `pink-grid-apply-r26-fixed.log` 종료0. 곡선 내부 꼭짓점이 실제 구간 끝처럼 판정되던 오류를 지정 구간의 시작/끝 경계로 한정하고 유클리드 거리로 폭을 제한. 곡선/분할 seam/외부/끝점 회귀 통과. 8그룹 기존 내부 선16/45/12/60/38/14/12/53개가 모두0개로 감소, 저장 전후 동일 검사 통과. 차로/폭/정차점은 불변.
- 실제 작성 r26 hash `f6f057397dfbf09070db6da001b1a69c0eed3a545b5e0b8ae824683c0c2c0d88`. 원본baselineHash 불변.
- 필수 입력 manifest 실제크기/SHA 갱신, `r26-workspace-strict.log` 종료0/issues0. 원본 NGII 재수입 자료는 변경하지 않아 OSM 대조 원본 설정 유지.
- `r26-migration-build.log` 종료0: r14~25 저장 이관12버전196검사, BuildHybrid 통합 실행본 생성. `builds/EungyeTaxi/project-map-build.json` r26 지문과 일치. 사용자 FirstDrive 씬 원래SHA 복원 확인.
- 이것은 현재 확인분 중간 빌드이며 전체 색상 조사 완료 아님. 새 질문33 확인분 P29 및 P13/P14 실제 전환 추가 조사/수정 진행.
- r26 실행본 위치 제어 저장/재시작 검사: `r26-live-save.log` 9검사, `r26-live-reload.log` 23검사, 두 실행 종료0. 사람 주행·전체 캠페인 검사는 아님.
- r26 배포 manifest: 사용자 Taxi.zip/content를 보존하고 배포 파일만 별도 복사한 경로에서 VerifyOnly 종료0, 143파일/132424850바이트. sourceCommit은 현재 HEAD를 기반 커밋으로만 기록하고 미커밋 소스임을 명시.

## 33번 후속 및 조사 도구 상태
- 사용자 확정 E안쪽 L-only / E바깥 painted-S, legal-S+R. P29 후보는 W2를 W1로 통합하며 보도/외곽 포장 유지, E차로변경 구간10~31m는 게임 추정.
- 첫 `p29-preview.log` 종료1: 전체 polish 순회에서 대상 밖의 기존 disabled lane 도로까지 비활성화. 변경 대상을 WOuter와 제거한 EInner 직진 connector에 한정. `p29-preview-scoped.log` 재실행 종료0, 실제 E2/W1 단면·화살표와 권한 분리·차량 footprint·39정차1482경로 통과.
- 새 Computer Use 런타임은 `@oai/sky`만 노출. 이전 IAB 세션에 바로 재접속할 수 없어 반환된 네이버 Chrome 창 확인. 최소화 오류 후 복원/재조회 1회 시도에서도 `failed to activate captured window` 발생. 사용자에게 거리뷰 창을 앞으로 꺼내 달라고 요청했으며 이후 UI 입력 중단. 지도/빌드 검증은 계속 진행.

## r27 실제 지도 저장
- `p29-apply-r27.log` Unity 종료0. 저장 전/재로드 후 모두 P29 동일 검증 통과. 콘텐츠 지문 `33db5a0c7c12a1bfc40d8c7cba19033173c74d866c92528c8b1992f91fd660bf`, baselineHash 불변.
- 변경: P29 E2/W1, E안쪽 L전용, E바깥 기존 S+R 연결 유지 및 S화살표만 렌더. WOuter ID는 비활성으로 보존하고 접속은 생존 WInner로 재연결. 기존 보도/건물/포장 외곽 유지. 위치 제어 기하/경로 검사이며 실제 물리 주행은 아님.
- 파일: `P29MapEdit.cs`, `P29MapTests.cs`, `LaneArrowAppearance.cs`, `LaneArrowAppearanceTests.cs`, `LaneDirectionPaint.cs`, `ReviewedNativeLanePresentation.cs`. r27 저장 이관은 `BusinessProgress.cs`, `JunctionRouteFeedbackTests.cs`에서 실제 저장 해시로만 허용. 새 topology로 내비 캐시를 다시 만들고 J7917/J7204 관측만 추가 무효화.
- `r27-workspace-strict.log` 종료0. 생성 입력의 실제 크기/SHA 갱신. 사용자 FirstDrive 씬 해시 기존과 동일.
- 다음 실행: `ColorSurveyPreviewTests.BuildReviewedCurrent` (현재 진행 중), Player 저장/이어하기 및 배포 manifest 갱신. 전체 도로 전수 조사는 여전히 미완료.
- 사용자 후속: 거리뷰는 Codex 내부 브라우저에 열려 있으며 **내부 브라우저 연결 복구 후 조사 진행**을 선택. 외부 Chrome으로 전환하지 않는다. 현재 제공 도구 검색/플러그인 조회에서는 내부 탭 제어가 확인되지 않았으므로 추가 현장 관측은 보류하고 확인분 빌드만 마무리한다. 브라우저 자체 오류나 사용자 측 연결 장애로 단정하지 않는다.

## r27 빌드 검증 결과
- `r27-migration-build.log` Unity 종료0: r14~26 13버전·225검사 통과, `PROTOTYPE_BUILD_OK`. 실행본 project-map-build.json은 실제 r27 contentHash/baselineHash와 일치.
- 사용자 FirstDrive 씬 빌드 후 원래 SHA256 `1494FF1D97725151FE4E3EA3AF312A5019E68DD94D67A9F207C2BE42567D9B64` 복원 확인.
- 배포 manifest 실제 파일 크기/SHA 갱신, `r27-distribution-verify` 격리 복사본 VerifyOnly 종료0: 143파일/132468290바이트. 사용자 기존 Taxi.zip/content 보존, 원격 push 없음.
- `r27-live-save.log` 새 실행본 저장9검사 종료0. `r27-live-reload.log` 이어하기23검사도 종료0. 전체 캠페인/수동 플레이/자동 물리 주행 완료로 표시하지 않는다.
- 재현: Unity `-batchmode -nographics -quit -executeMethod ColorSurveyPreviewTests.BuildReviewedCurrent`; 실행본 `-batchmode -nographics -liveSaveTest` 후 `-liveReloadTest`. 저장은 artifacts/live-campaign-test.json 격리 파일 사용.
- 남음: 내부 브라우저 제어 복구, P20/P26 중앙 전환 촬영점 공개 좌표 정렬, P14 마모 화살표 과거 대조, coverage 표의 모든 색상 미확인 구간/시설 점검 및 최종 통합 빌드. 현재 r27은 확인분 중간 결과이다.

## 내부 브라우저 재연결, r28–r30 실제 적용과 로컬 빌드

- 사용자 내부 탭 연결 복구 확인. 최신 영상의 ⑥→⑦ 유턴 표지는 좌회전 신호 또는 보행 신호 때 허용. ⑦→⑥는 은계호수로가 만나는 접근으로 정렬하고 최신과 2024년 영상에서 좌/직/직/직 확인. [거리뷰와 근사 좌표](../content-design/road-survey/2026-11-13-yellow-observations.md).
- r28 `Y06EastApproachEdit`: ⑥→⑦ 좌·유턴/직/직/직/우, 3개 직진 차로는 서로 다른 ⑦ 동쪽 출구로 연결. 좌/우/유턴 곡선은 유지. S09-B는 기존 차로의 분할 계보를 이어 같은 연석과 승하차 위치를 유지. 마지막58m, 테이퍼26m, 간격3.04m는 게임 추정. `y06-east-apply-r28.log` 저장 전후 검증·종료0, hash `0b270a36e452439324eedb558a0f01c06f54d8aa11cdae6c666b5cc3a4ce784e`.
- r29 `Y06WestApproachEdit`: ⑦→⑥ 좌/직/직/직. 기존 좌회전 출구와 세 직진 출구 유지. 최종55m와 테이퍼25m는 근사 게임 배치. `y06-west-apply-r29.log` 저장 전후 검증·종료0, hash `0ac94059842e80819c2182dc84c2600937e512c535e323022ad92d9bc8d296da`.
- r30 `P10PedestrianPassageEdit`: 사용자가 보행 전용으로 확인한 34번 통로의 차량 차로와 연결 7개 차단. 높이0.14m, 폭5.5m는 보행로 게임 추정값. 차단 후 출구가 사라지는 수인로 임시 동행 접근은 기존 본선 `A2218G611419:0`으로 복구. 다른 임시 접근으로 보내는 최초 시안은 건물 침범으로 실패했고 저장하지 않았다. `p10-pass-apply-native.log` 저장 전후 검증·종료0, hash `731caa156ccba7c85d261df3ed1f44eef6315b725e0a69ce55b5f831030e3c48`.
- 세 저장에서 각 39개 정차점 및 1,482개 방향 경로 확인. 계수로는 차로별 화살표·연결·새 주행 경로의 자동차 공간 검사, 34번은 차량 경로가 다시 생기지 않는지와 보행 포장 존재 검사. Y06 처음 후보는 오른쪽 차로의 보도 침범, 이후 기존 회전 곡선 검사 방식 및 S09-B 계보 누락에서 저장 전에 탈락. 연석 내 배치, 기존 회전 곡선 보존, 기존 정차 계보 연결로 교정했다.
- `r30-workspace-strict.log` 종료0, 문제0. 입력 지도 크기·SHA256 갱신. `r30-migration-build.log` 종료0, r14~29 16개 버전 저장 이관324검사, `PrototypeBuild.BuildHybrid` 성공, 실행본 메타데이터와 실제 지도 지문 일치. FirstDrive 사용자 씬 SHA256 `1494FF1D97725151FE4E3EA3AF312A5019E68DD94D67A9F207C2BE42567D9B64` 복원 확인.
- 새 실행본 `-batchmode -nographics -liveSaveTest`·`-liveReloadTest` 모두 종료0 (저장9, 이어하기23). `artifacts/live-campaign-test.json` 격리 사용자 데이터. 배포 파일 사본 `r30-distribution-verify`에서 VerifyOnly 종료0, 143파일·132522514바이트. 원격 푸시 없음. 이번 검사에는 수동 주행, 자동 물리 주행, 전체 캠페인이 포함되지 않았다.

## 은계번영길 P13/P14와 사용자 35번 (r31–r32)

- 요구사항은 은계번영길의 방향별 차로수, 중앙선 전환과 방향 표시를 재현하고 나머지 색상 도로도 연속 확인하는 것이다. 사용자 지시로 원격 푸시를 하지 않는다. [전체 35번 표시 지도](../content-design/images/eungye-p13-question-overview.png)에서 사용자가 지정한 방향 화살표 생략과 좌회전·직진 안내도 반영했다.
- r31 `EungyeCommerceLaneEdit.Apply`는 P13을 동2/서1로 맞추면서 중앙 횡단보도 서행 정지선·신호를 생존 차로에 연결했다. 서행 승하차 12번 근처의 기존 정차 위치를 기준으로 차로를 부드럽게 이동시켰다. P14는 서쪽 동1/서2, 동쪽 동2/서1로 바뀐다. 전환 중심 x541.3 및 테이퍼 9m는 게임 거리이며 실측 위치는 확인하지 않았다. J5750 동행 좌/직, J5076 서행 좌/직·우 화살표를 표시한다. J5076 바깥의 직진 표시는 아파트 방향이고 공공차량 그래프에는 기존 우회전만 연결했다.
- r32 `EungyeCommerceLaneEdit.Apply35`는 J7204 동행 두 차로의 방향 화살표만 생략한다. 안쪽 좌회전·직진과 바깥쪽 직진, 중앙선·차로 구분선을 유지한다. 사용자 표시 요청을 현장 검증과 구분해 기록하고 `ReviewedNativeLanePresentation.cs`가 그 요청에 근거한 외형 지정만 받도록 했다. 외형과 기존 턴을 분리하는 `LaneArrowAppearance.cs`, r31/r32 저장 이관을 처리하는 `BusinessProgress.cs`와 `JunctionRouteFeedbackTests.cs`를 변경했다.
- `r31-apply.log` 종료0. 저장 전후 P13/P14 6단면, 차로별 표시·회전 연결, 승하차 12번 연석 정렬, 전체 39곳 1,482개 경로를 통과했다. r31 지문 `c42ef1391c7bf32f72a528ce2fd40d8ac20dcd13d37fd738ec501c4b98f43e54`. r31 통합 빌드와 17개 버전 저장 이관 395검사도 종료0.
- `r32-question35-apply.log` 첫 적용 종료1. 관측 외형만 허용하는 기존 검증에서 사용자 외형 지정이 거부돼 지도는 저장되지 않았다. 사용자 근거를 명시한 뒤 `r32-question35-final.log` 종료0. 저장 전후 차로 JSON 동일성, 두 진입 화살표 없음, 실제 좌회전·직진 연결과 1,482개 전체 경로를 재검증했다. r32 지문 `a0591bde41c622c2dbee164aef373086226769e4c126611c9780d5607195eb70`. `r32-workspace-strict.log` 종료0·문제0. 선택 재수입 JSON 2개 부재.
- 이어서 조사한 구간은 [P20/P26 공영주차장](../content-design/road-survey/2026-11-13-pink-orange-observations.md), [G05 은계로 ⑮ 조건부 유턴](../content-design/road-survey/2026-11-13-green-observations.md), [Y04 대은로 ④–⑤](../content-design/road-survey/2026-11-13-yellow-observations.md)다. 해당 새 근거, P13 탄성봉, 58그룹의 남은 방향·시설을 지도에 반영하는 작업이 남아 있다.
- `r32-migration-build.log`에서 r14~31 저장 이관 18개 버전·418검사가 통과했다. 같은 실행의 빌드는 Windows의 `resources.assets.resS` 매핑 잠금으로 종료1이었다. 파일을 단독 열 수 있음을 확인하고 Unity `-executeMethod PrototypeBuild.BuildHybrid`로 재실행했다. `r32-build-retry.log` 종료0·`PROTOTYPE_BUILD_OK`, 실행본의 `project-map-build.json`은 실제 r32 지문과 일치한다. 사용자 FirstDrive 씬 원본 SHA256 복원을 확인했다.
- `builds/EungyeTaxi/EungyeTaxi.exe`를 `-batchmode -nographics -liveSaveTest`와 `-liveReloadTest`로 실행했다. 두 검사 종료0, 격리 파일 `artifacts/live-campaign-test.json`의 저장9·이어하기23을 확인했다. 수동 주행, 자동 물리 주행과 전체 캠페인은 미검증이다.
- r32 소스 커밋 `8e90a74`와 작성 지도 크기·SHA256을 통합 manifest에 기록했다. 사용자 작업 파일을 포함하지 않은 런타임 사본 `artifacts/color-road-survey/r32-distribution-verify`에서 `tools/update_distribution_manifest.ps1 -VerifyOnly` 종료0, 143파일·132576210바이트. 기존 `Taxi.zip`, `content/`를 보존했다. 원격 푸시 없음.

## 공영주차장 P20/P26 (r33)

- 색칠한 58그룹의 실경 대조를 이어갔다. P20은 동1/서2와 북1/남2, P26은 동2/서1과 북1/남2를 적용했다. J5750은 2024년 10월 영상에서, J7204는 2026년 5월에서 중앙선 옆 좌회전 전용 및 바깥쪽 우회전 전용 표시를 확인했다. [거리뷰 근거와 미확인 시설](../content-design/road-survey/2026-11-13-pink-orange-observations.md).
- `ParkingEastLaneEdit.cs`에서 기존 공영주차장 앞 직진 연결 뒤 방향별 차로를 분기하고 두 T자 교차로의 종전 회전 목적지를 전용 차로에 옮겼다. 3m 분기점, 10m 테이퍼, 3.15m 차간 중심 간격과 곡선 보간은 게임 근사값이다. 동쪽 식당의 진입 위치·진입 방향을 유지하고 식당 앞에서 나온 뒤 좌회전 차로로 옮길 수 있게 했다.
- `r33-parking-final.log` 종료0. 저장 전후 6개 단면·4개 방향 화살표·회전 경로와 차량 외곽 통과 공간, 식당 바깥쪽 바인딩과 좌·우회전 경로, 승하차 39곳의 1,482개 방향별 경로를 확인했다. 서쪽 급커브에서 첫 Offset 시안의 자기 교차를 검출하고 위치 및 접선 보간을 수정했다.
- `r33-workspace-strict.log` 종료0·입력 문제0. 로컬 입력 크기 및 해시 갱신. `r33-migration-build.log` 종료0, r14~32 저장 이관 19개 버전 441검사와 BuildHybrid 성공. 지도 및 실행본 내용 지문 `575ca884e867d4d3eecd0ee2c2488fce2d4aee7d24d370413391ddb3c84fb791`.
- 주차장 횡단보도 옆 황색 빗금·분리봉, P13 분리봉과 주차장 진출입로는 추가 정렬해야 한다. [36번](../content-design/images/eungye-g05-question.png)은 사용자가 바깥 차로의 우회전 전용을 확인했다. [P05 은행고길 인접 남단](../content-design/road-survey/2026-11-13-pink-orange-observations.md)의 1+1·중앙선·난간 후보 근거도 기록했다.
- r33 Player 위치 제어 저장9·이어하기23 및 런타임 사본 143파일·132608530바이트 VerifyOnly 종료0. `distribution/EungyeTaxi-build-manifest.json`은 실제 로컬 소스 커밋 `69f3eb9`와 입력을 가리킨다. 사용자 FirstDrive 씬 SHA256 복원을 확인했다. 자동 물리 주행과 수동 플레이, 전체 캠페인은 아직 확인하지 않았다.

## 은계로 G05 ⑭ 접근과 사용자 36번 (r34)

- 사용자가 [36번](../content-design/images/eungye-g05-question.png)의 바깥 차로를 우회전 전용으로 확인했다. [최신 확대](https://map.naver.com/p?c=19,0,0,0,adh&p=Ix6rbIKQdDKEt2ehapts1A,318,5,50,Float)에서 유턴 보조표지 ‘좌회전시 / 보행신호시’를 판독했다. 최신·2024년 3월의 정지선 영상에서 중간 직진 3개를 대조했다. 안쪽 화살표는 차량에 가렸으며 종전 좌회전 연결을 사용했다.
- `G05NorthWestMapEdit.cs`는 ⑮→⑭ 북서행이 기존 3차로에서 좌회전·조건부 유턴 / 직진 / 직진 / 직진 / 우회전 5차로로 갈라지게 한다. 기존 세 직진 차로가 각각 별도 출구로 연결된다. 70m 접근·24m 테이퍼, 차로 중심·유턴 진입 곡선은 게임 근사값이다. 반대 ⑮ 접근에는 추가 거리뷰 근거를 기록했다.
- Unity `G05NorthWestMapEdit.PreviewChecks`와 `G05NorthWestMapEdit.Apply` 종료0. 저장 전후 5차로의 화살표와 이동 연결, 유턴 조건, 차량 외곽, 기존 은계 승하차 6번 좌표, 전체 39곳·1,482개 방향별 경로를 확인했다. r34 지문 `12cb90de24c98418e2d8bb628ffd8f7eb7d51d79f7211f867b32f11cf8aa0ea5`.
- `r34-workspace-strict.log` 종료0·입력 문제0, 선택 입력 JSON 두 개 부재. `ColorSurveyPreviewTests.BuildReviewedCurrent` 종료0. r14~33 저장 이관20버전·484검사, BuildHybrid 성공 및 실행 파일 지문 일치. 사용자 FirstDrive 씬 SHA256 원본 복원을 확인했다.
- 조사 중인 대은로 Y04 서남행 ④ 앞은 비보호 좌회전 전용 / 직진 / 직진·우회전 겸용이다. ⑤ 쪽 보행섬 우회전과 P13 횡단보도 양쪽의 분리봉도 재대조했다. 해당 위치의 형상·시설 적용과 58그룹의 남은 방향이 후속이다.
- r34 Player `-batchmode -nographics -liveSaveTest`·`-liveReloadTest` 종료0. 격리 위치 제어 검증 저장9·이어하기23 통과. `r34-distribution-verify`의 VerifyOnly 종료0, 143파일·132642354바이트. 통합 manifest에 소스 커밋 `6b5be76`를 기록했다. 자동 물리 주행, 수동 플레이, 전체 캠페인은 미검증이다. 원격 푸시 없음.
- 다음 조사에서 은계번영길 P13/P14/P20/P26의 차로 수와 중앙선 전환을 적용하고, G04 은계로142번길 남쪽 접근의 최신 외측 화살표 및 은행사거리 P02 포켓과 은계남로 P03 서행 화살표를 대조한다. 내부 브라우저 조사 탭은 P03의 마지막 서향 촬영점에 유지했다. 5색 58그룹 전체 검토는 진행 중이다.

## 은계로 ⑮·대은로 ④ 정지선 (r35)

- G05 ⑭→⑮ 동남행은 마지막 70m에서 3→5로 넓어진다. 안쪽부터 좌회전·조건부 유턴 / 직진 / 직진 / 직진 / 우회전 전용이다. 사진에 가렸던 안쪽 좌회전은 기존 경로를 적용했다. 유턴은 좌회전 또는 보행 신호 시이며 각각의 직진 차로는 별도 출구로 이어진다. [거리뷰 근거](../content-design/road-survey/2026-11-13-green-observations.md#2026-11-16-g05-⑭⑮-정지선).
- Y04 ⑤→④ 서남행은 마지막 43m에서 2→3으로 넓어지며 안쪽 비보호 좌회전 / 직진 / 직진·우회전으로 연결된다. 해당 방향에서 보호 좌회전 단계를 빼고 직진 녹색에서 마주 오는 교통에 양보하도록 했다. 나머지 방향의 신호는 기존 설정을 따른다. 차로 길이와 테이퍼, 유턴 곡선은 게임 추정이다. [2026년·2024년 근거](../content-design/road-survey/2026-11-13-yellow-observations.md#2026-11-16-y04-대은로-⑤④-정지선).
- 지도는 `ColorSurveyR35MapEdit.cs`와 `G05NorthWestMapEdit.cs`로 작성했다. `RoadMapRules.cs`·`RoadMapValidator.cs`·편집기에서 신호 접근 방향과 좌회전 연결로를 지정하는 비보호 표지를 지원한다. `DriveFlowPolicy.cs`·`TrafficSystem.cs`·`TrafficSignalDemand.cs` 및 주행 평가/표시 경로에 연결했다. `BusinessProgress.cs`·`JunctionRouteFeedbackTests.cs`에서 이전 저장의 내비 인덱스와 두 교차로의 진행 중 관측을 새 지도에 맞춰 다시 계산한다.
- Unity 2022.3.62f2 `-batchmode -nographics -quit -executeMethod ColorSurveyR35MapEdit.Apply` 종료0. 저장 전후 화살표, 실제 회전 연결·차량 외곽, 조건부 유턴, 승하차 39곳과 방향별 1,482개 경로를 검증했다. 지도 지문 `a528216cb878fa6fce9af1e2fc64c067416078a2c8e6c4874c8563792a829591`. `pwsh -File tools/check_workspace.ps1 -Strict` 종료0·입력 문제0, 선택 재수입 지도 두 개 부재.
- `ColorSurveyR35MapEdit.BuildReviewed` 종료0. 직진·좌회전 등화 비교, 반대편 보호 좌회전, 맞은편 차 양보, r14~34 저장 이관 21개 버전 550검사와 BuildHybrid를 통과했다. 실행본 지도 지문 일치와 사용자 FirstDrive 씬 SHA256 복원을 확인했다. 로그는 `artifacts/color-road-survey/r35-build.log`에 있다.
- 첫 Player 고정 카메라 검사에서 대은로 ④의 표지가 시작 시 숨겨졌다. `RoadSignage.cs`의 접근별 표시 조건을 신호 쪽과 일치시켰다. 재빌드 `r35-build-signage.log` 종료0. 최종 Player `-batchmode -force-d3d11 -presentationTest` 종료0·286검사, 실제 비보호 표지 렌더 확인.
- 최종 Player에 `-batchmode -nographics -liveSaveTest`와 `-liveReloadTest`를 각각 실행해 종료0, 격리 위치 제어 저장9·이어하기23을 확인했다. 통합 manifest가 소스 커밋 `22a9b48` 및 r35 지도 크기·SHA256을 가리킨다. 런타임 사본 `artifacts/color-road-survey/r35-distribution-verify`에서 VerifyOnly 종료0, 143파일·132689530바이트. 자동 물리 주행, 수동 플레이, 전체 캠페인은 미검증이다. 원격 푸시 없음.
- P13 분리봉을 횡단보도 동서 양쪽에서 재확인했다. 작성 지도는 중앙 횡단보도 x648/z257.3과 양쪽 정지선을 갖고 있다. 짧은 분리봉 무리는 이 정지선 바깥으로 정렬하는 작업이 남았다. 공영주차장 분리봉·빗금·진출입로와 다른 색상 구간의 남은 방향은 [58그룹 현황](../content-design/road-survey/2026-11-16-coverage.md)에 기록했다.

## 은계번영길·공영주차장 분리봉과 은계로 남단 37번 (r36)

- `config/maps/eungye-road-facilities.json`과 Unity 사본에 2026년 5월 거리뷰를 바탕으로 주황 탄성 분리봉을 작성했다. 은계번영길 P13은 신호 횡단보도 동서에 3개씩, 공영주차장 앞 P20/P26은 서쪽 4개와 동쪽 7개다. 양쪽 정지선, 횡단 위치 및 인접 진출입구를 비웠다. 수량·간격·위치는 게임 추정이다. 횡단 노면 표시·황색 빗금·주차장 진출입 연결은 후속이다. [거리뷰 근거](../content-design/road-survey/2026-11-13-pink-orange-observations.md#2026-11-16-분리봉-반영과-남쪽-도로-재확인).
- [37번 전체 지도](../content-design/images/eungye-g04-question-overview.png)에서 사용자가 북행 외측 우회전 전용을 확인했다. `G04SouthApproachMapEdit.cs`가 `J7841_867` 북행 기존 2차로를 접근부에서 4차로로 넓히고 안쪽부터 좌·유턴 / 직 / 직 / 우로 연결한다. 양쪽 직진 차로는 서로 다른 북쪽 차로에 이어진다. 유턴은 확인된 보조표지에 따라 좌회전 또는 보행 신호 때 가능하다. 46m 접근과 24m 테이퍼는 게임 추정이다. [거리뷰 근거](../content-design/road-survey/2026-11-13-green-observations.md#2026-11-16-g04-은계로-남단-일부).
- `G04SouthApproachMapEdit.Apply` 종료0. 저장 전후 화살표·실제 연결·차량 외곽·유턴 조건과 승하차 39곳의 방향별 1,482개 경로 검증. r36 지문 `353fe15fe9f383001e93deafe3905f983a3fe19c0e5f47811e17e85efcf245e7`. 지도·시설 파일의 크기·SHA256을 `config/maps/local-input-manifest.json`에 갱신했다. `pwsh -File tools/check_workspace.ps1 -Strict` 종료0, 필수 입력 문제0.
- `FlexibleBollardTests.BuildCommercialMap`에서 분리봉 간격·반사띠·개별 충돌과 두 구간의 분리봉 17개를 검증했다. 횡단 구간과 연속 장애물 유무를 확인하고 활성 차로 및 연결 중심과 최소 2.20m 거리임을 검사했다. 먼저 r35에서 분리봉만 시험 빌드했고, `G04SouthApproachMapEdit.BuildReviewed`에서 37번을 포함한 r36 BuildHybrid와 이전 저장 r14~r35 이관 22버전·598검사를 통과했다. 양쪽 빌드 종료0, 사용자 FirstDrive 씬 SHA256 보존을 확인했다.
- 남은 조사 중 P02 은행사거리 북행 확폭을 훈장골 앞 굴곡으로 좁혔다. P03 은계남로 [38번](../content-design/images/eungye-p03-question-overview.png) 서행은 사용자가 안쪽 좌회전·바깥 직진/우회전을 확인했다. `J1919_877` 서쪽 골목 구간이 원본 지도에서 끊겨 있어 도로 폭·연결점과 비보호 신호를 함께 복원해야 한다. [58그룹 점검표](../content-design/road-survey/2026-11-16-coverage.md).
