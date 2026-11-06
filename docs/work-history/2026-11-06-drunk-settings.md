# 2026-11-06 취객 등장·상태 직접 조정

## 요구와 구현

취객 등장 확률/하루 최대 인원/강제 등장 조건/잠듦·구토 확률을 관리창에서 직접 편집하도록 요청했다. 대화 선택지→취객→배정과 연결 요청에 다음6개 설정을 추가하고 실제 공통 게임 코어에 연결했다.

| ID 접미사(type.Drunk.) | 기본값 | 범위/의미 |
|---|---:|---|
| spawnPercent | 30 | 0~100%, 배정 가능 차례의 추첨 확률 |
| dailyMax | 2 | 0~100명, 0은 취객 없음 |
| forceEnabled | true | 사용/사용 안함 |
| forceAfterMisses | 3 | 1~1000회, 취객 없이 실제 탑승한 횟수 |
| sleepPercent | 30 | 0~100%, 배정된 취객의 하차 깨우기 상태 |
| vomitPercent | 25 | 0~100%, 배정된 취객의 구토 예정 |

하루 최대가 우선하고 특수 승객 뒤 일반 승객 차례를 유지한다. 확률0이어도 강제 등장이 켜져 있으면 조건 충족 후 등장할 수 있다. 강제 등장만 막으려면 forceEnabled=false, 취객 전체를 막으려면 dailyMax=0. 취소·재추첨은 카운터를 소비하지 않는다. 잠듦/구토는 서로 독립이고 구토 예정이어도 실제 경고 뒤 시간이 지나기 전에 하차하면 구토가 발생하지 않을 수 있다. 수면 승객 유형의 weight와 별개다.

VehicleCare의 일일 카운트/미등장 카운트 저장·복원 경계도 설정값에 맞게 변경했다. 기존 진단용 BoardDrunk의 단독1회 제한은 정상 Reserve/Commit 경로와 별개로 유지했다. schema1/2의 기존 프로필에6개가 전부 없을 때만 구형으로 인정한다. 해시 검증 후 새 세션/관리창 메모리 이관에6개 기본값을 채운다. 일부만 누락한 새 설정은 거부한다. 기존 저장 snapshot을 단순 복원할 때는 원래 값/해시를 유지하며 각 소비자의 기존 fallback을 사용한다. 기존 실제 취객 상태는 재추첨하지 않는다.

## 검증

- C# ContentTuningTests 758검사(기존663+신규95), UTF8/이관 Node hash 대조 통과. 신규는 0/100%·최대0/3명·강제 끄기/5회·저장복원·상태 고정·구형 프로필/범위 검증.
- PassengerCareTests 13,301 통과. PassengerBehaviorTests는 기존 followup remembers actual request response Comfort에서 실패. HEAD의 변경 전 VehicleCare로 따로 컴파일한 baseline도 동일 실패(exit1), 이번 취객 변경으로 해결했다고 표시하지 않는다. 첫 baseline 컴파일은 csc의 slash 경로 해석으로 실패했으며 Windows 절대경로로 재시도하여 동일 실패를 확인했다.
- Node 전체60개 통과 및 신규 drunk-settings2개 통과. 구형 프로필 원본 불변/기본값 보충·부분 누락 거부/범위 검증. 전체 첫 실행의 review fixture 실패는 앞선 화면 명칭 작업에서 추가한 displayCatalog에 대한 평면 화면 fallback 누락이 원인이었고 조건부 호출로 보완 후60개 통과.
- ContentTuningBuild.Build: 콘텐츠 runtime32/build3 및 HUD/기존 통합 선행 검사, PrototypeBuild.BuildHybrid 완료. artifacts/drunk-settings-build.log의 PROTOTYPE_BUILD_OK 및 return code0 확인.
- 실제 Player -contentTest -contentTuningCheck:17검사, 격리 저장·프로필 적용/이어하기 snapshot/새 캠페인 경계 통과. artifacts/drunk-player-check.log. 기존 캠페인/활성 파일을 이 검사에 쓰거나 덮지 않았다.
- 실제 관리창에서 하루 최대3으로 수정해 요약 갱신 확인 후2복원, 기존 대사/밸런스를 유지한 default 저장 및 게임에 적용 클릭. 새 항목6개 기본값만 추가. 기존 프로필 원본은 관리 서버 backups에 보존.
- 새 관리 서버는 artifacts/drunk-editor-url.log의 로컬 주소. 이전 서버/사용자 미저장 탭은 보존. 새 탭12에 취객 편집 화면을 열고 캡처 artifacts/drunk-settings-editor.png 보관.

## 파일·실행과 남은 수용

코드: VehicleCare.cs, ContentTuning.cs, tools/content-editor/drunk-settings.mjs·choice-catalog.mjs·profile.mjs·public/content-model.js·app.js. 생성 config/content 및 Resources의 catalog/default-profile 두 쌍. 테스트 ContentTuningTests.cs, content-editor/drunk-settings.test.mjs. 관련 안내·체크리스트·이력 갱신.

기본 실행 builds/EungyeTaxi/EungyeTaxi.exe, 은계 싱글/A2 PVP·현재 공유 LANv10/BuildHybrid. 신규 필드를 포함하는 프로필을 쓰므로 LAN 참가 PC도 이번 실행본으로 갱신한다(구 실행본은 알 수 없는 설정을 거부한다). 이번 빌드는 현재 공유 작업 폴더의 승인된 진행 중 운전 설정/교통 변경도 포함할 수 있으며 그 소스를 이번 커밋에 섞지 않는다. 지도 입력/ZIP 미변경. 사람 장시간·실물 패드·다른PC·전체 캠페인은 이번 자동 검사로 대체하지 않는다.

관리창 대화 선택지→취객에서 편집→저장→게임에 적용. 다음 새 싱글 캠페인/새 PVP방에 적용, 이어하기는 저장 당시 설정. 예: 취객 없이 연습은 최대0명; 자주 시험하려면 등장100%/최대10명/강제 사용 안함(일반 승객 차례는 유지); 구토만 시험하려면 구토100%/잠듦0%.

최종 소스/배포 hash·LAN/manifest 검증은 아래 영수증에 기록한다.
