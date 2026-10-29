# 2026-10-29 좌측 하단 내구도 아이콘

## 요구·변경

사용자 첨부 화면의 포만감/피로/연료/청결 옆에 내구도를 같은 아이콘 형식으로 표시한다. 기존 상시 우측 내구도 패널을 정리했다.

- DriverStatusGauges.cs: 기존320폭 속도 패널을 유지하고288폭 안에5열 배치. 아이콘/이름/수치/막대를 세로 정렬해 청결도/내구도3글자와100/100이 잘리지 않게 했다. 플랫폼 이모지 대신 기존 방식의 기하 도형 렌치, D50이하 노랑·D20이하 빨강·D0 운행 불가 안내.
- CockpitUI.cs: 실제 Care.Damage.Durability 연결, 상태 영역 top118/height74로 조정. VehicleDamageController.cs: 상시 우측 패널 제거, 충돌 알림·견인/수리 선택 유지.
- VehicleDurabilityProbe.cs: 기존 격리 -vehicleDurabilityTest와 함께 쓰는 -vehicleDurabilityHudCapture 추가. 정상100/권장50/불가0,1280×720과1872×864 렌더를 별도 hud 보고서로 남기며 기존70검사 보고서를 덮어쓰지 않는다. 새 UI의 동작을 흉내내는 단위 검사를 추가하지 않고 실제 배치를 시각 확인한다.

## 검사·배포

- PrototypeBuild.BuildHybrid 종료0, artifacts/durability-hud-build.log.
- 독립 코드 검토:5×51.2+4×8=288, 마지막 끝328<패널344, 경고 끝190<패널196. 실제 내구도 값 연결과 기존 고장 선택창 guard 유지, 중대한 UI 문제 없음.
- 캡처 명령은 반드시 -vehicleDurabilityTest -vehicleDurabilityHudCapture를 함께 사용해 사용자 저장을 피한다. 실제 화면 결과·배포 해시·게시 결과는 아래에 기록한다.
- 별도 PvP 배포 요청도 계속 처리했다. feature/pvp-prototype의 b8ad18acc990435b09122156b5738d6ee434310f에139파일93,137,205바이트 실행 폴더와 전용 manifest/도구를 게시했다. 싱글은 EungyeTaxi.exe, PvP는 PvpPrototype.exe로 각각 실행한다. 그 작업 공간의 진행 중 NPC/규칙 소스와 기존 실행 앱은 보존했다.

범위는 HUD 배치다. 손상/수리/견인/회계/복원 규칙은 변경하지 않았다.

## 최종 실행 검증

- 최종 PrototypeBuild.BuildHybrid 종료0: artifacts/durability-hud-verified-build.log.
- 실제 창 격리 Player 종료0: artifacts/durability-hud-verified-capture.log. HUD fixture6검사 PASS(artifacts/vehicle-durability-hud.txt).
- 1280×720·1872×864 각각100/50/0 화면6개를 시각 확인했다. 5개 상태 이름·100/100·막대·수리 권장/운행 불가가 잘리지 않고, 상시 우측 내구도 패널은 제거됐으며 D0 견인 선택창은 유지된다. 연속 캡처 정상 상태에 이전 충돌 알림이 남지 않게 fixture의 알림 타이머도 초기화했다.
- 재현: 기본 exe에 -oneDayMode -hybridV2 -vehicleDurabilityTest -vehicleDurabilityHudCapture -screen-width 1280 -screen-height 720 -screen-fullscreen 0을 함께 전달한다. 저장 격리 플래그를 빼지 않는다. artifacts/vehicle-durability-hud-{100,50,0}-{1280,1872}.png 생성.
- 이번에는 HUD/컴파일/상태 fixture를 검증했다. 이전 손상·수리 기능 회귀 결과는 이전 이력의 증거이며 이번에 전체 캠페인·수동 물리 주행을 다시 수행한 것은 아니다.
- 변경 파일: 위 스크립트4개, 생성 FirstDrive.unity, 인터페이스 기획·체크리스트·이력·LATEST, 실행 폴더 DLL/boot.config 및 배포 manifest. 지도 입력 변경 없음.

## 배포 산출물

실제 소스 커밋0706cffe6054e3b90dcd99d3a274bc758877672f 기준 최종 실행 폴더130파일112,194,710바이트. 전용 HUD 검증 JSON으로 manifest를 생성하고 -VerifyOnly 종료0으로 모든 크기·SHA256을 대조했다. 필수 로컬 입력 Strict 종료0/문제0이며 지도 입력·배포 ZIP은 변경하지 않았다.

## 원격 게시 확인

main 배포 d631a57bcac57e7ed5d270cf5947dc0d5cab3339를 push하고 git ls-remote의 동일 해시를 확인했다. PvP 브랜치에는16aa387528b04a521d342696f9884f00607297ee로 main HUD/싱글 배포를 병합·push했고 원격 동일 해시를 확인했다. 병합 인덱스에서도 게시 PvP139개 blob 크기·SHA256이 기존 검증 manifest와 전부 일치했다(PVP_PUBLISHED_INDEX_OK). 해당 공간의 진행 중 NPC/규칙/프로젝트 설정·새 로컬 PvP 빌드는 커밋에 포함하지 않았다. 두 브랜치의 실행 경로는 각각 EungyeTaxi.exe(싱글), PvpPrototype.exe(PvP)이며, 다른 물리PC/LAN 검증은 별도다. 기본 작업 공간의 기존 미추적 파일도 보존했다.
