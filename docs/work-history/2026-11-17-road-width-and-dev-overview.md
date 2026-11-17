# 자주색 차로·도로 폭 점검과 설정 개발용 전체 보기

## 요청과 범위
- 편도2차로가1차로처럼 보이는 곳, 특히 자주색 P01–P29 확인.
- 도로·인도 폭을 포함한 부자연스러운 형상 점검. 실제 오류 근거 없이 일괄 확폭하지 않는다.
- 실제 게임을 수직으로 내려다보는 전체 지도 기능. 사용자가 M 지도 대신 **설정의 별도 개발 기능**으로 지정했다.

## 차로/형상 점검
- 지도 r45와 기존 거리뷰 조사표, 사용자 확정 차로를 대조했다. 최신 거리뷰를 이번 작업에서 새로 전수 조사했다고 주장하지 않는다.
- r45 실행본의 실제 Camera.Render 격자 sheet00 및 자주색 상세 r00-c02/c03/c04, r01-c02/c03/c04를 확인했다. 자주색29그룹은 이 범위에 들어간다. 수직 고정 화면이며 수동 운행이 아니다.
- `LaneWidthPresentationAudit.Run`: 렌더러 자체의 차로 그룹이 아니라 활성 차로 쌍의 독립 투영으로 검사. 전체1,811표본 중 합성 분리선 거리 후보1개. 이 후보(은계로 x801.45/z341.34)는 원본 메시 전환부로, `NativeHandoff` 실제 흰색 메시까지1.645167m 확인·종료0. 자주색123표본은 누락0.
- 자주색 검사 수: P02 9, P03 28, P11 6, P13 18, P14 4, P15 14, P20 21, P22 2, P26 16, P29 5. 나머지는 이 조건의 같은 방향 두 차로 표본이 없으며, 보행통로/무차선 생활도로/1+1 본선을 포함한다. 0표본을 차로 검증 통과로 과장하지 않는다.
- `tools/check_road_width_geometry.cjs`: 차량 중심 및 좌우1m는 인도 침범0. ±1.4m 여유폭에서는 전체23,786단면 중 자주색 밖 네 접점에서5개의 보도 폴리곤 접촉 후보. 차체 충돌을 의미하지 않는다. 자주색은0.
- 활성 본선의 좌우1.4m를3m간격으로 검사한38,066표본은 도로 포장 밖 이탈0. 순수 폴리곤/리본 검사이며 지형 높이·시설물/건물 충돌·전체 물리 주행 검증이 아니다.
- 자주색의 확인된2차로 구간에 분리선이 사라진 사례는 이번 검사에서 발견되지 않아, 도로 폭/차로 수를 임의 변경하지 않았다. 기존 남은 현장 불확실 전환부는 기존 조사표에 유지한다.

## 구현
- `DevRoadOverview.cs`: 북쪽이 위인 정사영 개발 보기, 별도 비활성 카메라/RenderTexture. 드래그, 커서 기준 휠 확대, 전체 보기, 내 위치, 중앙/커서 좌표. 최대64배.
- `DrivingSettingsPanel.cs`: 개발용 테스트 탭에서 진입. Esc는 뷰→설정 순서로 복귀. Draft와 일시정지, 주행 카메라, 차 위치 보존. 닫으면 렌더 자원 해제.
- `DevRoadOverviewTests.cs`: 모델 회귀. 기능 부재 red 종료1 확인 후 좌표/줌/팬/초기화6검사 green 종료0.
- `DevRoadOverviewProbe.cs`: 격리된 실제 실행본의 진입/설정복귀/자원/화면 테스트.
- 설계·계획: `docs/ui/2026-11-17-dev-road-overview.md`.

## 실행과 검증
- `node tools/check_road_width_geometry.cjs`
- Unity `-executeMethod LaneWidthPresentationAudit.Run`
- Unity `-executeMethod DevRoadOverviewTests.Build` (내부 `PrototypeBuild.BuildHybrid`)
- Player `-devRoadOverviewCheck -force-d3d11` (격리 실행 폴더)
- 빌드·Player 최종 결과는 아래 후속 절에 실제 종료 결과로 기입한다.

필수 입력/지도 r45는 변경하지 않는다. 기존 사용자 Scene·개인 content·제안서·PackageManager 설정은 보존한다.


## 최종 실행 검증
- `DevRoadOverviewTests.Build`: 종료0, 모델6검사·PROTOTYPE_BUILD_OK. 은계 r45 입력 불변.
- 실제 Player `-singlePlayer -contentTest -devRoadOverviewCheck -screen-fullscreen 0 -screen-width 1600 -screen-height 1000 -force-d3d11`: 표시된 창에서 종료0·DEV_OVERVIEW_PLAYER_OK checks=7.
- 숨긴 창의 두 선행 실행은 UI Repaint가 생략되고 검은 화면으로 실패했다. 기능 통과로 세지 않는다. 표시된 창에서는 같은 실행본으로 정상 UI와 렌더가 확인됐다.
- 전체/18배 상세/800×600 창에서 실제 화면 확인. 설정 Draft, 카메라 위치·방향·정사영 상태, 차 위치·일시정지 보존 및 재진입/취소 시 자원 해제를 검증했다. 자동 제어 검사이며 사람의 장기 사용/수동 드래그 체감과 구분한다.
- 사용: Esc → 설정 → 개발용 테스트 → 도로 전체 보기 열기. 드래그/휠, 전체 보기/내 위치, Esc로 설정 복귀.
- [설정 진입 화면](../content-design/images/eungye-dev-road-settings.png) · [자주색 도로 상세](../content-design/images/eungye-dev-road-overview.png)

- 실제 소스 커밋: `006e1fc` (`git log -1 --oneline -- docs/work-history/2026-11-17-road-width-and-dev-overview.md` 확인).
- 배포 manifest143파일·133,364,953바이트 SHA256 검증 종료0. 검증 실행 폴더에 사용자 content/저장 미포함. 로컬 빌드 반영, 이번 작업 원격 push 없음.

## 사용자 요청에 따른 main 배포
- 2026-11-17: `git push origin main` 종료0, 원격 main `f45da9e558f75ca31fe82091121d2a5123463498` 확인. 개발 기능 소스006e1fc와 통합 빌드f45da9e 포함.
- 기존 사용자 미커밋 Scene·개인 content·제안서·PackageManager 설정은 커밋/푸시 대상에서 제외했다.
