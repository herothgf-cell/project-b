# 2026-11-16 은계 지도 46–54번 사용자 확인 반영

기존 조사와 [46–54번 확인 지도](../content-design/images/eungye-r41-survey-questions.png)를 기준으로 통행권과 노면을 게임 지도에 적용하고 Windows 통합 실행본을 `main`에 배포한다.

| 번호 | 확인한 차로·진입 조건 |
|---|---|
| 46 | 대은로 ④→③ 서남행 좌회전 / 직진 / 직진·우회전 |
| 47 | 은계중앙로 ⑫→⑦ 남행 좌회전 / 직진 / 우회전 전용 |
| 48 | 계수로 현대오일뱅크 앞 공사로 유턴 허용 조건 미확정. 새 유턴 경로를 추가하지 않는다. |
| 49 | 하늘색 J8921 서행 안쪽 좌회전 전용, 바깥 직진 |
| 50 | ⑭→⑤ 북서행 왼쪽 아파트 정문으로 좌회전 진입 |
| 51 | 은계남로 동행 바깥 직진 표시에 한양수자인 우회전 진입 허용 |
| 52 | 은계로 사거리 ⑲ 동행 바깥 직진·우회전 |
| 53 | 은계중앙로 J5076 남행 안쪽 좌회전 / 바깥 직진·아파트 진입 우회전 |
| 54 | 은계로 ⑮→⑯ 남행 좌회전 전용 / 직진 3차로 / 우회전 전용 |

[58개 그룹 조사표](../content-design/road-survey/2026-11-16-coverage.md)에 거리뷰 촬영점과 남은 시설·차로 전환 확인 항목을 유지한다.

## 구현과 지도 검증

- `ConfirmedLaneSurveyR42.cs`와 Yellow·Green·Pink 분할 파일에서 46·47번에 가운데 직진 차로를 추가하고 49번은 기존 직진 위치를 유지하며 안쪽 좌회전 포켓을 만들었다. 54번은 기존 폭 안에 다섯 차로를 배치하고 직진 세 개를 별개의 진출 차로에 연결했다. 해당 남행에 유턴 연결이 없고 맞은편 북행의 기존 조건부 유턴은 유지한다.
- 50번은 시흥은행우남아파트 정문까지의 좌회전 진입로와 중앙선 개구부를 연결했다. 51번은 바깥 직진 표시와 한양수자인 정문 우회전 경로를 함께 두고 맞은편 호반써밋 정문으로의 좌회전 포켓을 구성했다. 52번은 은계로 사거리 동행 좌회전과 직진·우회전을 두 차로로 나눴다. 53번에서 바깥 우회전은 은계파크자이 정문으로 연결된다. 50·51번의 진입로에 별도 신호를 추가하지 않았다. 출입로는 정문까지이며 내부 동선과 출차는 후속이다. 차로 전환 길이와 진입로·보도 치수는 게임 근사값이다.
- Unity 2022.3.62f2 `-batchmode -nographics -quit -executeMethod ConfirmedLaneSurveyR42.Verify`에서 변경 전 46번 차로가 없음을 확인했다(`r42-baseline-verify.log`, 종료 1). 첫 Apply 후보는 49번 바깥 차로가 보도에 닿아 저장 전 탈락했다(`r42-apply-first.log`, 종료 1). 기존 선형으로 돌린 바깥 차로와 안쪽 포켓을 사용한 `ConfirmedLaneSurveyR42.Apply`는 `artifacts/color-road-survey/r42-apply-curb.log`에서 종료 0. 저장 전후 실제 연결과 도색, 차량 외곽, 정문 진입, 50번의 중앙선 교차 판정과 정차 39곳 사이 1,482개 방향별 경로를 검증했다.
- `config/maps/project/eungye.project.json`의 내용 지문은 `0d7c0353d3bf477d7ab1d3ea1c6c8dc35a7281e30c10ccafd6f3c3b343f5633b`다. 48번 인근의 차로·시설·장면 객체와 원본 지도 지문은 r41과 일치했다. `config/maps/local-input-manifest.json` 크기·SHA256을 갱신한 뒤 `pwsh -File tools/check_workspace.ps1 -Strict` 종료 0, 입력 문제 0. 선택 재수입 JSON 2개는 로컬에 없다.
- `BusinessProgress.cs`와 `JunctionRouteFeedbackTests.cs`는 r14~r41 저장이 정확한 기본 r42 지도에 들어갈 때 기존 그래프 번호를 초기화한다. 코드 리뷰에서 확인한 별도 운전 만족도 판정의 교차로 진입 상태와 호환용 법규 관측도 변경된 교차로에 한해 초기화한다. 운전 점수·누적 감점·영업·승객 정보와 다른 교차로 관측을 보존한다.

## 빌드와 재현

- `ConfirmedLaneSurveyR42.BuildReviewed`가 현재 지도와 44·45번 회귀를 확인하고 `PrototypeBuild.BuildHybrid`를 실행했다. `artifacts/color-road-survey/r42-build-final.log` 종료 0. 저장 이관 28버전·845검사로 최상위 법규 관측이 없는 저장도 검사했다. 작성 지도 지문과 빌드의 `project-map-build.json`이 일치한다.
- `artifacts/color-road-survey/check-r42-player.ps1` 종료 0. 격리한 Windows Player에서 r41 운행 중 저장 이어하기 23건, r42 새 저장 9건과 이어하기 23건, DX11 신호·표지 322건을 확인했다. 위치 제어와 고정 카메라 검사다.
- 실제 소스 `7af5693`으로 `distribution/EungyeTaxi-build-manifest.json`을 갱신했다. 사용자 ZIP과 개인 콘텐츠를 제외한 `artifacts/color-road-survey/r42-distribution-verify`에 `pwsh -File tools/update_distribution_manifest.ps1 -Root <사본> -VerifyOnly`를 실행했다. 종료 0, 143파일·133067106바이트. 기존 사용자 FirstDrive 씬, PackageManager 설정과 별도 ZIP의 SHA256은 빌드 전후 일치했다.
- `builds/EungyeTaxi/EungyeTaxi.exe`에서 은계 싱글을 선택한다. [46–54번 지도](../content-design/images/eungye-r41-survey-questions.png)의 화살표 방향으로 접근한다. 50·51·53번은 각각 아파트 진입 방향을 확인한다. 54번은 남행 좌/직/직/직/우, 반대편은 기존 조건부 유턴이다. 수동 주행·자동 물리 주행·전체 캠페인은 이번 검증에 포함하지 않았다.
