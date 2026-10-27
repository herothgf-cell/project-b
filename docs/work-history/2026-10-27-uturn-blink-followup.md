# 유턴 표지 신호등 상부 부착·졸음 눈감김 후속

## 요구사항·구현

- 제보 eungye x807.6996/z607.1715/북3.280868도, J8066_6364의 조건부 유턴 표지가 도로 중앙 지주에 남는 문제 수정. 이전 RoadSignage 상부 배치는 별도 RoadMapFacilities의 유턴 시설에 적용되지 않았던 것이 원인이다.
- 교차로 ID·축·접근 부호가 일치하는 원래 신호등의 신호함 위에 부착. 기존 상부 비보호/횡단보도 표지를 위한 높이를 예약하고 별도 지주 제거. 반대편 최근접 신호등을 선택하지 않는다. 신호등이 없는 편집 미리보기는 기존 시설 위치를 유지한다.
- 피로100 누적30/50/70/90/110초부터 빠른 눈감김 후 재개방, 회당1.2→4초·강도55→87%.120~180초에는 매끄럽게 계속 감겨 완전 차단. 위아래 눈꺼풀 중심의 차폐와 암전. 게임 연출 수치이며 실제 생리 기준이 아니다.
- 최초5초 안내/Zzzz,120초 승객 안전 취소,180초 입력 차단·긴급 귀가100초/-20·당일 마감은 보존. 누적 소수초 저장과 메뉴 정지도 보존한다.
- 지도 원본·조건부 유턴 규칙·입력 ZIP·마인드맵은 변경하지 않는다. 앞선 drowsy-map-handling 작업도 함께 main 통합 대상이다.

## 변경 파일

RoadMapFacilities/DriverCondition/DrowsinessHUD, 위치 제어 UturnBlinkProbe, Editor UturnBlinkFeedbackTests, 순수 모델 DrowsyBlinkTests와 test_prototype, 기획06/10·체크리스트·실행 계획·본 이력/LATEST. 배포 후 기본 실행 폴더 전체와 배포 manifest도 갱신한다.

## 검증

- RED: 순수 모델은 초기 눈감김 강도0으로 실패(종료1). Editor는 실제 저작 J8066 시설을 사용하여 해당 접근 신호등 자식이 아니라는 assertion으로 실패(종료1).
- GREEN: `pwsh -File tools/test_prototype.ps1` 모델27종 종료0. 신규 눈감김76검사, 시간 분할·저장·중단·회복과 기존 캠페인·취소·영업 종료 회귀 포함.
- Editor: 초점 검사 종료0 후 `UturnBlinkFeedbackTests.RunFinal` 종료0. 표지9검사(정방향/반대편·수직/회전/상부 간격/별도 지주/편집 fallback), 기존 UI/운영/경로 전체, JsonUtility 피로 저장12·자동 정차·직접 회전·시설 출구 연속244 포함.
- 독립 읽기 전용 코드 리뷰 Critical/Important0. Minor인 방향·간격·preview 회귀 검사 및 캡처 오래된 파일 재사용 가능성을 보완했다. 캡처는 이전 동일 산출물을 제거하고 실제 픽셀 명암과 눈꺼풀 재개방도 검사한다.
- Strict 입력 검사0·지도/ZIP 변경0, 마인드맵 SHA256 `F1951492F9C5A13941E60A17D92EB31B1A7471FB8ABA7996D47A0C2E02F00937` 유지. 샌드박스 라이선스 접속 정체는 정상 환경의 동일 검사로 재실행했다.
- 빌드/실제 렌더·배포/병합 결과는 아래 완료 후 기록한다.

## 재현·구분

- Editor: `-batchmode -nographics -quit -executeMethod UturnBlinkFeedbackTests.Run`, 프로젝트 `unity/EungyeTaxi`.
- 기본 BuildHybrid 실행 파일에 `-uturnBlinkTest`를 주면 별도 시험 저장을 사용하고 제보 좌표로 위치 제어하여 표지와 눈감김/재개방/최종 차단을 캡처한다. 산출물 `artifacts/uturn-blink-*.png`, Player 로그 `artifacts/uturn-blink-player.log`.
- 위치 제어·모델·렌더 검사이며 사람의 수동 주행·전체3일 물리 캠페인이나 현장 표지 전수 검증은 아니다. 졸음 강도/빈도의 체감은 후속 사용자 플레이 대상이다.

## 로컬 환경

Windows Unity2022.3.62f2. 기본 은계 `builds/EungyeTaxi/EungyeTaxi.exe`, 빌드 `PrototypeBuild.BuildHybrid`. 소스 시작83ddd28. 실제 최종 이력 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-27-uturn-blink-followup.md`로 확인한다.
