# 2026-11-02 PVP 통합 실행 최종 수용·배포 후속

[구현 및 Editor 기록](2026-10-30-pvp-flow-unified.md), [설계27](../content-design/pvp/27-flow-unified-mode.md)을 이어간다. 시작 소스8b7f512의 통합 BuildHybrid는exit0이며140파일115,184,219바이트의 새 복사 폴더와 원본 SHA256을 모두 대조했다. 기존 두 실행 폴더는 `artifacts/pvp/pre-unified-dd709ae`에 보관했다. 배포 교체/푸시는 아직 미완료다.

## 실제 실행의 실패·제한과 원인 구분
- `flow-mode1` hidden 창: 캡처가 검어서 화면 수용 아님. 메뉴→PVP4→재시작→메뉴→싱글 저장→메뉴까지 통과 후 제한시간 종료2. 로그에서는 두 번째 싱글의 월드/교통을 이미 초기화했으며 ambient 초기화85,755,126ms 등 약23시간49분 실행 공백을 기록했다. 따라서 메뉴 복귀 불능으로 확정하지 않는다.
- `flow-mode2` 실제 창: 초기 메뉴/PVP설정 캡처 후 월드 생성85,767,393ms 공백, 종료2. 역시 복원 검증 완료 근거 아님. 실제 완주를 새 실행에서 확인해야 한다.
- `flow-four1`: 종료1. 실제 경기136초 동안 사람799.9m/4,770원, 봇1004.9/902.2/494.7m였으나 긴 실행 공백 뒤 실시간 제한이 만료되었다.72NPC 총66,358m·합류 대기 최대2초·끝의 봇3대 이동 중이라 교통 교착의 증거는 아니다.4명 운송·정산 수용은 미완료로 둔다.
- 진단만 보완: 모드 검사의 긴 프레임 공백을 별도 기록하고 활성 검사 시간에 상한 반영, 캡처 대기10초 한도/실패 기록, 단계 로그 추가. 플레이 동작은 이 수정으로 변경하지 않았다.

## 지정 좌표 예비 성능 측정
`flow-perf1`, 소스8b7f512, 요청1271.5697/1777.091·358.478821도, 실제 차로 투영1272.27/1777.09, 1920×1080/Ultra/Direct3D11/72NPC/봇3. 위치는 검사 시작 때만 배치한 정지 관측이며 실제 운송 주행 증거와 구분한다.61.4077초/1660프레임 평균27.032fps, p9553.8188ms/p99300.9766ms/max18,053.68ms, GC0 200회. 원인 불명18초 지연을 포함하므로 개선 배수의 근거로 단정하지 않는다.

관측 차량72대/봇3 모두 진행, NPC실제23,500.29m·242차로 관측, 합류 대기 최대5.26초, 경로 이탈 최대0.000148m/방향 오차0.02798도. 모든 도로의 장시간 자연 흐름 또는 다른PC 성능 완료를 뜻하지 않는다. `PVP_CITY_PERFORMANCE_OK`는 측정 자료 유효성/NPC 이동 표식이며 FPS 수용 표식이 아니다.

CPU 범위: 합류990ms/전체NPC5372ms/경로갱신3454ms/화면1614ms. 소스 조사로 반복 `HdRoadGraph.Nearest`의 전체 폴리라인 투영과 신호/도로 규칙 LINQ 할당을 확인했다. 기존 순서·동률·방향·폐쇄·신호 의미를 보존하는 경계 사전 필터/무할당 반복으로 수정 중이며 마지막 동등성 검사와 같은 조건 재측정 이후 결과를 기록한다.

## 최종 수정 검증
`PvpTrafficPerformanceTests.Run`, `artifacts/pvp/flow-final-editor4.log`: exit0, 실제 A2 및 동률/폐쇄/방향/4m경계의 차로 선택, 모든 작성 신호 위상 경계·차로 필터·보행·소수 주기, 제한속도 규칙 순서/구간/중첩을 이전 계산과 대조했다. 핵심 교통 허가/법규 의미는 바꾸지 않았다. 실제 재빌드/주행/성능은 아래 후속 결과로 구분한다.

## 최종 통합 Player / 소스61c9e98
- `PrototypeBuild.BuildHybrid` / `flow-final-build2.log`: exit0. 복사본 `flow-runtime2`의140파일115,186,267바이트 전체 크기/SHA256 일치.
- 실제 창 `-unifiedModeCheck -unifiedModeCaptureOutput .../flow-mode3`: exit0 / `UNIFIED_MODE_RESULT PASS`, 활성92.11629초. 메뉴→PVP4→전역 모드 리셋에도4세션 저장금지→재경기→메뉴→싱글 준비/저장→메뉴→동일 캠페인 복원→원본·백업 손상 거부/출발 시 명시 복구 확인→메뉴 통과. 기존 사용자 캠페인JSON/백업/운행JSON/운행백업4파일 바이트 불변·새 사용자파일 없음.1600×900 메뉴/PVP설정/기존 싱글 준비 캡처 확인. 임시 캠페인을 이용한 프로그램 전환 검사이며 사람 버튼 조작 수용과 구분한다.
- `tools/test_pvp_simulation.ps1 -FourDrivers -TrafficMonitor -Executable .../flow-runtime2/EungyeTaxi.exe -OutputDirectory .../flow-four2`: exit0. 실제 WheelCollider 거리4755.957/4374.620/5201.535/4614.557m, 운임11030/15870/24920/5810원. 공용8호출·승객 소유권·정산 중복 방지·지도 중 시간/교통 진행·결과 정지·메뉴 복귀·4명 재시작 초기화 통과. 위치 제어/목적지 순간이동 없이 자동 페달·조향으로 운송했다.
- 같은4대 검사 읽기 전용 관측:813.9151게임초,72NPC 총378,051.4m,808차로 관측, 충돌하지 않는 경로 허가 최대9개, 합류 대기 최대31초(진출/신호로 막힌 교통의 대기 포함). 경로 오차 최대0.0001488m/방향 오차0.02798도.17/58,336샘플은 명시 정지 사유 없음으로 기록했다. 전2874링크 모두를 사람이 주행한 결과로 확대하지 않는다.
- `tools/test_pvp_simulation.ps1 -Services -Executable .../flow-runtime2/EungyeTaxi.exe -OutputDirectory .../flow-services2`: exit0, 실제765.6517m 주행·정차/주유1,600원 지출·구매 연료 증가·지불자만 서비스 대기·출차/승객 영업 복귀 통과. 연료24로 낮춘 검사는 native Account.Consume fixture이며 거리/시간/운임을 주입하지 않았다. NPC72 유지.

## 최종 지점 성능·표시 확인
`flow-perf2` / 소스61c9e98 /1920×1080·Ultra·Direct3D11·72NPC·봇3·별도 Player 동시 실행 없음, exit0.60.01778초3455프레임: 평균57.56627fps, p9524.786ms/p9927.4565ms, 최대117.5951ms,50ms초과2회/100ms초과1회. NPC28,639.49m 진행. GC0 293회로 수집 자체가 사라진 것은 아니며, 전체 끊김 제거/고정60fps를 주장하지 않는다. 이전 관측은18초 원인불명 지연을 포함하므로 단순 배수 비교를 피한다. NPC 밀도·신호 규칙·그림자를 줄이지 않았다.

실제 렌더에서 직좌/직우/유턴 표시와 신호등·다차로를 확인했다. 방향 표시 상세 촬영은 측정 종료 후 카메라만 이동한 정적 표시 확인이다. 실제 운송 주행 증거와 혼동하지 않는다. 일부 표시가 일반 추적 카메라의 뒤쪽에 있어 처음 지점 사진에는 보이지 않았지만, 접근 차로 상류의 렌더를 확인했다. 사진 속 빨간 차량은 일반 NPC 색상이며 특정 과거 차량의 의도는 확인 불가다.

## 배포·보존·남은 수용
현재 실행은 `builds/EungyeTaxi/EungyeTaxi.exe` 하나다. 은계 싱글/자체 도시 PVP를 선택하고 PVP는 나1+봇3·공용8호출이다. 구버전 별도 PVP139파일은 현재 배포에서 제거했고 기존 로컬 폴더 복사본과 Git 이력을 남겼다. PvP manifest는 통합 manifest 안내가 되었고 기존 VerifyOnly도 통합140파일115,186,267바이트를 검증한다. 이전 평면 모터 진단 러너의 기본 EXE 경로도 통합했다(이번에 옛 네트워크 검사를 재실행했다는 뜻은 아님).

회사/제품명과 기존 캠페인 저장 경로를 유지했다. 다른 체크아웃의 로컬main d817d9a와 미추적 작업은 변경하지 않았다. 필수 지도 원본/프로젝트 지도/ZIP은 변경하지 않는다. 사람 직접 운전의 추월·합류·유턴 체감, 전 도로 장시간 교통,20분 수익/생활 밸런스, 다른 PC 성능/실제4명·기존 물리 원격 대전은 별도다. 피로/포만은 콘텐츠27의 후보 기획만 작성했고 기존 수치는 바꾸지 않았다.

재현 명령: Unity2022.3.62f2 `-batchmode -nographics -quit -buildTarget Win64 -projectPath unity/EungyeTaxi -executeMethod PvpFlowUnifiedChecks.Run`, 성능 동등성 `PvpTrafficPerformanceTests.Run`, 빌드 `PrototypeBuild.BuildHybrid`. 실제 Player는 `-unifiedModeCheck`(임시 저장), 러너 `-FourDrivers -TrafficMonitor` 또는 `-Services`에 `-Executable`과 별도 `-OutputDirectory`를 지정한다. 성능은 `-pvpCityPerformanceTest -pvpTrafficFlowMonitor -trafficPerfSeconds 60 -trafficPerfOutput <출력폴더> -screen-fullscreen 0 -screen-width 1920 -screen-height 1080`이고 정상 렌더 창을 사용한다. 장시간 실행 공백이 발생한 결과를 수용 완료 근거로 사용하지 않는다.

## 게시 영수증
배포 커밋 `e6ab18cc83740809a1ae1f20c6517439b5b402cf`를 `git push --atomic origin HEAD:feature/pvp-prototype HEAD:main`으로 게시(exit0)했다. `git ls-remote`에서 두 원격 브랜치가 같은 해시임을 확인했다. 최신main dd709ae가 소스의 조상인 것도 게시 직전 확인했다. 게시 후140파일115,186,267바이트 manifest 재검증0, 작업 트리 clean. 이 이력 파일의 실제 배포 커밋은 `git log -1 --oneline -- docs/work-history/2026-11-02-pvp-flow-release.md`로 e6ab18c임을 확인한 후 영수증을 추가한다.

다른 체크아웃 `C:/git/project-b`의 로컬main d817d9a와 기존 미추적7개 항목을 그대로 확인했다. 해당 채팅의 다음 작업은 보존된 변경을 유지하면서 정상 pull/merge로 최신main을 반영한다. 해당 체크아웃을 대신 reset/checkout하지 않았다.
