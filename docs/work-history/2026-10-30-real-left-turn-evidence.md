# 실제 비보호 좌회전 근거와 창작 상황 제거

## 요청과 판단

사용자: 창작 구역 대신 네이버 지도 표지·신호를 확인해 실제로 가능한 접근만 비보호 좌회전으로 반영. 기존 세 곳은 운송 상황에서 고른 접근이며 실제 비보호 근거가 아니었다. 직접 네이버 거리뷰를 열고 촬영월2026-05, 신호·유턴 조건판·카메라 위치를 대조했다.

[접근별 원문 링크/관찰 기록](../../config/maps/eungye-permissive-left-review.json). J7841 서→동과J8066 북→남은 대상 진입 정면 판독이 해결되지 않았다. J6858 남남동→북북서는4구 신호·유턴 조건판을 확인했지만 비보호 좌회전 표지는 확인하지 못했다. 다른 접근/다른 교차로의 사진을 허용 근거로 사용하지 않는다. 이번3접근의 긍정 확인0곳이며 은계 전역 부재를 선언하지 않는다.

## 실제 변경

- PS05~07을 일반 운송으로 전환. 기존9코스 ID/선정 위치/승하차 유지.
- ServiceLawZones 서비스 조건은8~11만 등록, 기본 영업에서12~14 명시 호출도 거절. 별도 연습 모드의 교육 상황은 유지.
- 오래된 PS05~07 offer의 숫자 zoneIndex가 다른 조건으로 잘못 연결되지 않게 차단.
- 폐기한 zoneId의 관찰만 해제하고 저장된 승객 운송은 유지. 종료 후 assignedId가 비어 있어도 폐기한 결과 제목의 TIP/피드백은 제거. 과거 장부는 유지.
- 실제 근거 JSON, Editor 검증/Build 진입점, 최종 Player 회귀 probe와 기존9운송/4조건 fixture 갱신. 프로젝트 지도/원시 입력/지도 ZIP은 미변경.

## 검증

- 모델 `tools/test_prototype.ps1`:28클래스 종료0.
- Unity `RealLeftBuildChecks.Build`: 접근 근거/현 지도 비보호0 검증,9코스/이전 저장 migration, WorldFollowup 전체·Startup11,672·BuildHybrid 종료0 (`artifacts/real-left-editor-build.log`).
- 최종 UI fixture 수정 후 `WorldFollowupChecks.HudAndBuild`: 가독성/BuildHybrid 종료0 (`real-left-final-build.log`).
- Player 각 probe 최신 결과7종 모두 종료0: 실제 정책/이전 저장95,9코스 승하차96,4서비스 조건72,교통/도로468,지도 라벨97,운전 피드백20,UI60검사(합계908). `real-left-native-initial-results.json` / `real-left-native-final-results.json`에 실제 종료코드·시간 기록.
- 초벌 UI는1회 종료1: 예전 짧은 시설명 `식당/주차장/휴식 주차`를 비교하던 fixture가 프로젝트 지도 전체 이름을 인정하지 못했다. 시설 배치를 변경하지 않고 현재 이름 조건으로 수정한 뒤 재빌드, 최종 정책95/UI60 종료0을 확인했다. 실패 기록은 `real-left-initial-ui-failure.txt`에 보존.
-1280 미니맵 전체/선택2화면, 최종1920 전체 지도/1280 탑승 HUD2화면 직접 확인. 창작 비보호 표시는 없고 다른4상황 유지.
- 독립 코드 검토에서 종료 후 assignedId가 비어 있는 이전 TIP 잔류를 발견해 보완했고 재검토에 남은P1/P2 없음.
- 입력 Strict 종료0/issues0, 프로젝트 지도/원시 입력/ZIP 미변경. 위치 제어·대표 NPC 회귀를 사람의 수동 주행/전체 캠페인 완료로 표시하지 않는다.

## 실행과 남은 항목

기본 `builds/EungyeTaxi/EungyeTaxi.exe` / 은계. 이전 저장은 그대로 이어가고 PS05~07은 일반 운송으로 진행한다. 개발 검증 진입점 `RealLeftBuildChecks.Build`, Player `-realLeftOnlyTest -hybridV2`, `-prototypeScenarioTest -hybridV2`, `-serviceLawZonesTest -hybridV2`. Test 접미사는 격리 저장을 사용한다.

대상 정면 표지의 추가 판독과 지역 전수 조사는 미완료다. 읽을 수 있는 접근별 허용 근거를 확보하기 전 새 비보호 규제를 만들지 않는다. 사람20분/3일·다른 PC 검증은 별도다.

## 배포 자료 검증

실제 소스 커밋 `d6f29d81700853d2fb3195a841a313ef31617d08`을 `git log -1 --oneline -- docs/work-history/2026-10-30-real-left-turn-evidence.md`로 확인했다. `tools/update_distribution_manifest.ps1 -SourceCommit d6f29d81700853d2fb3195a841a313ef31617d08 -ValidatedOn 2026-10-30 -ValidationReport artifacts/real-left-validation.json`과 `-VerifyOnly` 종료0. 실행 폴더130파일112,293,085바이트의 크기/SHA256이 일치한다. 실제 게시 영수증은 아래를 따른다.

## main 게시 영수증

배포 커밋 `88b39896a002d836927105dcf4e16ed8c3857411`을 main에 fast-forward 병합하고 `git push origin main` 종료0을 확인했다. `git rev-parse HEAD`와 `git ls-remote origin refs/heads/main`이 위 실제 해시로 일치했다. main에서130파일/112,293,085바이트 전수 크기/SHA256 검증과 입력 Strict도 종료0/issues0이었다. 이 영수증은 실제 게시 후 별도 문서 커밋으로 함께 게시한다. 사용자/PvP 기존 untracked7항목을 보존했다.
