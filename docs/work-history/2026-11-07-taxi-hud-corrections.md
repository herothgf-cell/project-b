# 택시 하단·휠과 주행 HUD 겹침 수정

## 요구와 구현
- 사용자 지적: 차체 하단/휠아치 장식이 떠 보임, 휠 스포크 돌출. 고정 X 좌표의 별도 몰딩을 제거하고 기존 연속 차체가 하단/휠아치를 형성하도록 정리. 문 경계선은 차체 곡면을 따라 배치.
- 휠: 막힌 원판/바깥쪽 튜브 스포크를 열린 림과 얇은 안쪽 스포크로 교체. 림 면 78mm, 스포크 면 55mm, 브레이크 디스크 12mm(휠 중심 기준). 반경 .34m, 장착 위치, WheelCollider 보존. 게임 조명에서 검게 보이는 은색 재질의 금속도를 .85→.35로 보정.
- 사용자 추가 지적: 신호등 제목/램프 겹침, 큰 반원 속도계의 차량 가림, D/조작 안내 겹침. 제목을 램프 위 별도 행으로 배치. 1600×900 기준 속도 영역400×224→320×92, 하단24 여백. 속도/단위/기어와 하단 방향지시등·조향 보조 안내 분리. 속도 게이지는 0~140km/h 가로 막대. 실제 속도/후진 판정/조작 규칙은 유지.
- 3구/4구 및 현장 확인/게임 배치 구분은 기존 데이터·표시 유지. 1인칭 디지털 HUD도 기존 배치 유지.

## 변경 파일
- tools/art/build_taxi_blender.py, art-source/taxi 원본/FBX/JSON/README
- unity/EungyeTaxi/Assets/Resources/TaxiConcept 메시·재질
- Assets/Scripts/CockpitUI.cs, DrivingHudArt.cs, DrivingReadability.cs
- Assets/Scripts/BrightUITaxiReview.cs 휠 근접 캡처 추가, Assets/Editor/DrivingHudConceptChecks.cs 주행 시야 영역 검사 추가
- 체크리스트/범위/계획/본 이력, 통합 배포 실행본 및 manifest

## 검증
- 첫 통합 빌드 TaxiArtChecks.Build→PrototypeBuild.BuildHybrid exit0. Editor 외형37342, HUD15 검사 통과.
- 실제 Player 택시/시점36, 신호 카운트다운8 통과. 1280×720·1920×1080 화면에서 겹침/차량 시야 직접 확인.
- 최초 숨김 창 실행은 초기 렌더 프레임 캡처 실패(exit2). 표시된 검증 창으로 재실행하여 성공. 실패 결과를 성공으로 계산하지 않음.
- Strict 필수 입력 issues0. 지도/ZIP 변경 없음. 최신 빌드/추가 검증 영수증은 아래에 기록.
- 캡처는 고정 위치/제어된 상태 검사이며 사람 수동 주행이나 전체3일 캠페인 검증이 아님. 저사양LOD/다른PC/장시간 운전은 미검증.

## 재현
- Blender: art-source/taxi/README.md의 생성·내보내기, Unity MCP TaxiBlenderImport.Import().
- Unity batch: -executeMethod TaxiArtChecks.Build
- builds/EungyeTaxi/EungyeTaxi.exe -brightUiCheck -taxiArtReview -brightUiOutput <output> -screen-fullscreen 0
- 신호: -brightUiCheck -signalCountdownOnly -brightUiOutput <output>
- 진단 로그/원본 캡처: artifacts/taxi-hud-corrections/ (Git 제외). 검토 이미지: docs/ui/2026-11-07-taxi-hud-corrections/.

기본 실행 대상 은계/A2 통합 exe, LANv16 유지. 다음: 실제 운전 체감 확인, 차체 아트 세부 면/조명·월드 아트·승객 표정 개선.

## 최종 검증 영수증
- 재질 보정 후 최종 Build exit0, 실제 택시/시점36·전체UI165·운전 설정17 통과. 신호8은 동일 HUD 코드의 앞선 실행본 검사.
- Blender 평가 메시: 림 최대78mm, 스포크55mm, 러그64mm로 림 안쪽임을 확인. 떠 있던 장식 객체 없음.
- 보호 대상16개 중14개 동일. 은계 캠페인 본문/백업2개는 동시에 사용자가 실행한 D: 게임(PID42964, 테스트 인자 없음)에서 계속 저장되고 있음을 확인. 사용자 진행을 보존하고 되돌리지 않음. 16개 전체 불변으로 보고하지 않음.
- 사용자 게임 종료 확인 후 원본 main/배포를 동기화. 기존 패키지/문서 변경은 별도 보존.

