# 2026-11-10 J7973 북쪽 진입부 차선 정리

## 요구

사용자 위치 보고: 은계 `x=803.84, z=424.98`, 방향 4.1°(북), 가까운 교차로 `J7973_3744`(51m). "교차로 차선이 너무 지저분한데 실제 도로 참고해서 수정". 같은 날 사용자 지시로 앞으로 main에서 바로 작업·push한다.

## 원인

위치 제어 렌더(아래 `-poseReview`)로 은계로×수인로 J7973 북쪽 정지선~약 12m 구간을 확인했다. 노면선 두 벌이 겹쳐 있었다.

- 게임 주행 차로(`Traffic simulation`의 흰 차선·황색 중앙선): 편도 3차로, 단일 황색 중앙선, 정지선·차로 화살표. 실제 주행 그래프(`local:8017/8074/8131` 북행, `local:8032/7975/7918` 남행)와 일치한다.
- NGII 원본 노면선(`원본 노면선 0/1`): 짧은 원본 구간의 3.5m 간격 차선, 게임 중앙선보다 약 1.5m 옆의 이중 황색선, 북서·북동 모서리의 노란 갈고리 선과 보조 정지선.

레이어를 하나씩 숨겨 렌더하면 원본 노면선을 뺀 화면이 정상이고, 원본 노면선만 남긴 화면이 지저분한 조각 그 자체였다.

실제 도로 근거: 네이버 거리뷰 2026-05 파노라마 `F3LdEIaemBy-g7GXluq28Q`(은계로 J8066~J7973 중간)에서 편도 3차로와 녹지 중앙분리대를 확인했다. 기존 기록(기획21, 거리뷰 대조)도 편도 3차로다. 정지선 바로 앞 차로 구성은 이 위치의 파노라마가 없어 직접 보지 못했다.

## 실제 구현

- `Scripts/ReviewedMarkSuppression.cs`: 그리지 않을 NGII 원본 노면선 21개를 ID로 명시하고 근거를 적었다(이중 황색선 4, 남행 차선 3, 북행 점선 3, 북서 모서리 7, 북동 모서리 4). 거리로 추정하지 않는다. 은계 지도에만 적용된다.
- `Scripts/HdRouteTrial.cs` `DrawMarkings`: 위 목록에 있는 원본 선은 건너뛴다. 주 정지선 `B2218G610880`(기존 교체 마스크 적용)과 교차로 안 유도선(525)은 그대로 둔다.
- 지도 데이터, 주행 그래프, 신호, 정지선 시설, 지도 ZIP과 입력 manifest는 바꾸지 않았다.
- 검사 도구: `Scripts/BrightUIPoseReview.cs`에 임의 좌표 렌더 `-poseReview "x,z,yaw;..."`, 주변 메시 목록 `-poseInspect x,z,r`, 이름으로 렌더러 숨김 `-poseHide "a|b"`를 추가했다(`-brightUiCheck`와 함께, 임시 캠페인 격리). `Editor/ReviewedMarkSuppressionTests.cs`.

비교 렌더: [위에서 본 북쪽 진입부](../ui/2026-11-10-j7973-north-lanes/before-after-1.jpg), [북쪽에서 교차로를 보는 주행 시점](../ui/2026-11-10-j7973-north-lanes/before-after-2.jpg) (왼쪽 수정 전, 오른쪽 수정 후).

## 변경 파일

`unity/EungyeTaxi/Assets/Scripts/ReviewedMarkSuppression.cs`, `HdRouteTrial.cs`, `BrightUIPoseReview.cs`, `BrightUIProbe.cs`, `Editor/ReviewedMarkSuppressionTests.cs`(+ .meta). 출하 `builds/EungyeTaxi/`, `distribution/EungyeTaxi-build-manifest.json`. 문서 이 파일, `LATEST.md`, `docs/ui/2026-11-10-j7973-north-lanes/`.

## 검증 (실제 결과)

로그·캡처는 Git 제외 `artifacts/j7973-lane-cleanup/`.

| 검사 | 종류 | 결과 |
|---|---|---|
| `ReviewedMarkSuppressionTests.RunWithRegressions` | Editor | exit 0. 목록 90검사, `IMPORTED_MARKING_RENDERER_TEST_OK`, `ROADVIEW_CORRIDOR_OK`(yellowPaintStrokes=1082), `LANE_DIRECTION_PAINT_TEST_OK`, 유턴 표시·내비 검사 통과 |
| 진단 실행본 레이어 비교·수정 전후 `-poseReview` | 실행본, 위치 제어 렌더 | PASS. 수정 후 중복선·노란 조각 사라짐을 캡처로 확인 |
| `PrototypeBuild.BuildHybrid` | 출하 | exit 0, Build Finished Success |
| 출하 실행본 `-poseReview` / `-laneMovementReview` | 위치 제어 렌더 | `PASS checks=24` / `PASS checks=31`, 연결 보완 `added=25` |
| 출하 실행본 `-shiftDevEndCheck`(다른 세션 변경 함께 출하) | 실행본 | `SHIFT_PLAYER_OK checks=13` |
| `check_workspace.ps1 -Strict` | 입력 | exit 0, issues 0 |
| 통합/PvP manifest VerifyOnly | 배포 | `DISTRIBUTION_OK files=143 bytes=127464690` 두 번 |

## 출하·커밋

같은 폴더에서 다른 세션(싱글 영업 시간 선택, 소스 3933628)이 동시에 작업했다. 그 세션과 메시지로 조율해 출하 빌드·manifest는 이 세션이 두 변경을 모두 포함해 한 번 빌드했다. 소스 9302097 · 출하 89e72a6. 이 기록의 커밋은 `git log -1 --oneline -- docs/work-history/2026-11-10-j7973-north-lane-cleanup.md`로 확인한다.

## 미검증·남은 항목

- 사람이 직접 J7973 북쪽을 주행해 보는 수동 플레이, 자동 물리 주행, 전체 3일 캠페인은 하지 않았다.
- 정지선 바로 앞(남행) 차로 구성(좌회전 포켓 여부)은 해당 위치 파노라마가 없어 확인하지 못했다. 현재 게임은 기존 주행 그래프대로 직진·직진·직진+우회전이며 이번에 바꾸지 않았다. NGII에는 남행 좌회전 포켓(n91)과 북서 우회전 분리 차로 흔적이 있으나 주행 그래프가 쓰지 않는다.
- 횡단보도 북동 끝의 짧은 흰 선 조각 1개(게임 주행 차로 페인트)는 남아 있다.
- 은계로 중앙분리대(실제 녹지·펜스, 게임 황색 실선)는 이번 범위가 아니다.

## 재현

```powershell
# Editor(Unity 2022.3.62f2, Editor 닫은 상태)
Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod ReviewedMarkSuppressionTests.RunWithRegressions
# 출하 실행본 위치 렌더
builds/EungyeTaxi/EungyeTaxi.exe -singlePlayer -brightUiCheck -poseReview "801,404,0;803.8361,424.976,4.1" -brightUiOutput <폴더>
# 레이어 비교: 위 명령에 -poseHide "원본 노면선" 또는 -poseInspect 801,408,9
```
