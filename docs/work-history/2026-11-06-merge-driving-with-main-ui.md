# 2026-11-06 최신 main UI와 운전 수정 통합

사용자 요청: 기존 main 작업을 유지하면서 브레이크 감속·좌회전 신호·수리 안내 수정을 병합하고 main으로 푸시한다.

## 통합 기준·해결

- 원격 main `df3e693`의 캐주얼 UI/초상/카드/슬라이더/기록/시설·정산·개인 공간·승인 기본값을 기준으로 `codex/merge-driving-main`에서 `codex/driving-settings`(`ecd6a65`, 소스d4326a3)를 실제 병합한다.
- `CockpitUI`의 main Warm 카드/잔액 강조/구분선을 보존하고 속도 카드 내부 공업사 버튼만 추가한다.
- `DrivingSettingsPanel`의 main 헤더·안내·행별 카드·테마 슬라이더는 보존하고5항목/높이605/브레이크 행을 결합한다.
- `VehicleDamageController`의 main 처리 알림 카드/고장/정비 결과를 보존하고 중앙 수리 안내만 제거하여 하단 버튼과 연결한다.
- 신호 NGII 기존 연결 복구·사람 브레이크 압력/저장 이관·서버/예측 계수·LANv11을 보존한다. 두 작업 이력/LATEST 항목도 모두 유지한다.
- 충돌한 이전 실행 DLL/데이터/manifest와 생성 씬은 임시로 main 버전을 유지하고 통합 소스에서 다시 빌드한다. 이전 운전 브랜치의 실행 폴더로 main UI를 덮지 않는다.
- `FullUIBuild.Build`는 기존 테마/아트/콘텐츠/HUD 계약과 운전/실제 PhysX/실제 지도 좌회전 회귀를 함께 실행한다.

## 검증·게시 영수증

실제 명령/종료 결과 확인 후 아래에 추가한다. 보호한 사용자 저장/active·기존 미추적 로컬 캐시·별도 복사 기획 파일은 게시에 섞지 않는다. 필수 지도 입력/ZIP 변경 없음.

- `artifacts/merge-driving-main/build.log`: Unity `FullUIBuild.Build` exit0, 아트1254×1254/테마·제동22·실제 물리8·실제 지도 좌회전/방향·폐쇄 회귀·기존 조향/메뉴/모달·콘텐츠/HUD 계약·통합 BuildHybrid 통과.
- 실제 통합 실행본 `full-ui/player.log`112개·`mode-menu/player.log`4개 exit0. `driving-single`17개·`driving-pvp`15개 exit0. main 헤더/카드/슬라이더 스타일 안의 브레이크5항목과 속도 카드 공업사 버튼을 실제 PNG로 확인했다. 전체 UI 검사는 통제 위치/상태 검사이고 사람 전체 캠페인 검사가 아니다.
- 보호17개 저장/백업/active 해시 변경0, main의 콘텐츠/관리창/테마 소스·초상PNG 차이0. Unity가 재직렬화한 초상 meta의 공백만 원상 정리하여 meta도 main과 차이0. Strict exit0/input issues0·새 문서 링크 검사 통과.
- 통합 실행본 실제2 Player `tools/test_pvp_native_lan.ps1 -Port 18941 -Reconnect -TimeoutSeconds 360`: exit0, `native-lan-20261106-174652-a14f56dc` host51/client64·재접속 통과. 서버/예측의 개인 조향·브레이크 계수, 메뉴 중 경기 진행·자기 차 제동, 기존 사고/운송·권한 계약도 통과했다. 같은PC loopback이며 다른PC 수용은 별도다.

자동 승인 검토는 충돌 본문을 첫 줄만 선택하는 일괄 처리의 위험을 지적하여 실행 전에 거부했다. 파일별 문맥이 있는 명시적 패치로 대체했으며, 소스/기능을 잘라내는 방식으로 우회하지 않았다.

사람 장시간·전체 캠페인·실물 패드·다른PC 수용은 기존 자동 검사와 구분한다. 기존 단독 브랜치 검사 통과를 통합 실행본 검사로 대체 표기하지 않는다.
