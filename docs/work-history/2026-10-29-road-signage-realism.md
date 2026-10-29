# 2026-10-29 교차로 가짜 횡단보도 안내판 제거

## 요구·설계·실행 계획

사용자 첨부 화면의 대형 파란 가로판/횡단보도 글자 안내를 실제 도로에 맞게 수정한다. 원인은 RoadSignage가 모든 차량 신호 접근에 기본 판을 만들고 비보호가 아니면 횡단보도라고 쓰는 fallback이다. TrafficSystem의 비보호 판도4.5×1.45m로 과대하다.

경찰청 2024 교통안전표지 설치·관리 업무편람 p130~133(파일141~144쪽)은 포장도로의 신호기 있는 횡단보도 표지 불필요, 신호기 없는 곳 필요로 구분한다. 실제322 도안에는 그림·문자가 있지만 현재 글자만의 대형 가로판과는 다르다. p137의329 비보호 좌회전은 필요한 곳에만 설치한다. 자료: https://itskorea.kr/downloadFile.do?fileId=FILE_000000000090839&fileSn=0 / 도안 참고: https://www.law.go.kr/LSW/flDownload.do?bylClsCd=110201&flSeq=152755041&gubun=

- [x] 기본/보호 신호에 횡단보도 판이 생기지 않는 회귀 검사를 먼저 실패로 확인.
- [x] 기본 fallback 제거, 필요한 비보호 표지만 신호 상부에 작은 세로판·좌회전 화살표·비보호 글자로 통일. 뒷면 회색, 글자 앞면만. 동적 시나리오 접근 변경은 판 숨김/복원. 신호·보행·법규 판정 변경 없음.
- [x] Editor 관련 신호/표시 검사, BuildHybrid, 격리 실제 Player의 전체 표지 검사와 정면/뒷면/교차로 시각 검사.
- [ ] 기획/인계·배포 manifest·실행 파일 갱신 후 main 게시. 사용자 저장·지도 입력·별도 PvP 작업 보존.

정밀 실사 아트 전체 교체나 지도 시설의 사실 확인 완료를 뜻하지 않는다. 무신호 횡단보도 새 표지의 지도 배치나 기존 유턴 약정/지도 데이터는 이 수정에서 바꾸지 않는다.

## 구현·검증 중간 기록

- Editor RunSigns: 수정 전 종료1 ‘protected signal must not invent a crosswalk billboard’, 수정 뒤 종료0. 보호/기본 접근은 판0, 비보호 일치 접근만1, 시나리오 접근 변경 숨김/복원, 작은 회색 뒷판/화살표 문구를 검사했다.
- RoadSignage는 기본 글자판을 없애고 필요한 판을 lazy 생성/동일 객체 재사용한다. TrafficSystem의 기존4.5m 비보호 판도 같은0.7×0.95m 세로형 공통 생성기로 통일했다. 화살표는 앞면에서 왼쪽을 가리키고 뒷면은 회색으로 차폐한다.
- 첫 BuildPresentation(BuildHybrid 포함) 종료0 후 격리 -presentationTest 실제 Player303검사 종료0. 실제 지도74접근 모두 보호 신호/비보호0이며 가짜 횡단보도 문구0을 확인했다. 교차로1900×998 양방향, 도안1280×720 앞/뒤를 캡처했다. 실제 지도에 비보호가 없어 도안은 검증용 fixture로 별도 확인했고 실제 설치 장소 검증으로 해석하지 않는다.
- 독립 검토에서 DriveFeedbackSmokeTest의 ‘표지는 무조건1개 이상’ 전제가 새 정책과 충돌함을 발견했다. 실제 접근별 조건 개수와 비교하도록 수정하고 StartupReady 대기를 추가했다.
- 확대한 Editor 회귀에서 옛 JunctionFacilityTests가 현재 프로젝트 지도 모드에 없는 eungye-hybrid-map 리소스를 요구해 종료1했다. 필수 지도 누락이 아니며 해당 레거시 fixture가 있을 때만 수행하고, 현재 지도는 Player의 실제74접근 감사로 검증한다. 교통 흐름·보행 신호·글자 fit 검사는 함께 유지한다. 최종 결과는 아래에 기록한다.

## 최종 검증·변경 파일

- PlaytestPresentationTests.BuildPresentation(내부 기본 PrototypeBuild.BuildHybrid) 종료0: artifacts/road-signage-verified-build.log. 표시/시나리오·TrafficFlow·PedestrianSignalConsistency·MapReadability 통과. 레거시 eungye-hybrid-map fixture 부재는 별도 로그로 남겼으며 해당 JunctionFacilityTests를 통과했다고 표시하지 않는다.
- 최종 exe -presentationTest -hybridV2 실제 창 실행 종료0, artifacts/road-signage-verified-player.log / road-signage-player.txt:303검사.74접근 모두 보호 신호이며 비보호0이 올바른 현재 지도 상태다. 무조건 표지를 추가하지 않는다.
- 같은 최종 exe -driveFeedbackTest -hybridV2 -batchmode -nographics 종료0, artifacts/road-signage-feedback-player.log / drive-feedback-test.txt:16 PASS, success=True. 올바른 표지 개수0·신호 알림·점수·누적 저장/재시작 회귀를 확인했다.
- 실제 교차로 양방향1900×998에서 대형 횡단보도 판 제거/신호·횡단선 유지,1280×720 검증 도안의 앞면 좌회전 화살표/비보호와 뒷면 회색/문자 차폐를 확인했다. artifacts/signage-junction-view.png, signage-junction-opposite.png, signage-condition-front.png, signage-condition-back.png. 비보호 앞/뒤는 현재 지도 설치물이 아닌 렌더 fixture다.
- 독립 검토 지적(무조건 표지 존재 검사)을 수정하고 재검토 확인. 최종 중요/미해결 지적0.
- check_workspace.ps1 -Strict 종료0/Input issues0. 필수 지도·프로젝트 지도 JSON/배포 ZIP 변경 없음. 사용자 저장과 미추적 파일/진행 중 PvP 작업 보존. 사람의 물리 주행·전체 캠페인·현장 도로 확인은 수행하지 않았다.

변경 파일: RoadSignage.cs, TrafficSystem.cs, RoadPresentationProbe.cs, DriveFeedbackSmokeTest.cs, Editor/PlaytestPresentationTests.cs, 생성 FirstDrive.unity, 기획01/05·체크리스트·이력·LATEST, 실행 폴더 DLL/boot.config와 manifest.

재현: 기본 은계 exe에 -presentationTest -hybridV2 -logFile artifacts/road-signage-player.log(절대 경로 권장)를 전달한다. 테스트는 artifacts 저장으로 격리하며 실제 운행을 자동 수행하지 않는다. Editor 검증/기본 빌드는 PlaytestPresentationTests.BuildPresentation을 사용한다.

## 배포 산출물

실제 소스 커밋e765b57a524bbc9cf3ef9035a8a6abbe58f5f1e4 기준 최종 실행 폴더130파일112,202,390바이트. 전용 검증 JSON으로 manifest를 생성하고 -VerifyOnly 종료0으로 전체 크기·SHA256 일치를 확인했다. 기본 실행 경로 builds/EungyeTaxi/EungyeTaxi.exe / 은계, 지도 입력/ZIP은 그대로다. 게시 결과는 실제 원격 확인 뒤 기록한다.
