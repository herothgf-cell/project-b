# 2026-10-27 졸음 영업 종료·시설 길안내·지도·조향 피드백

## 요구와 보존

사용자 최신7첨부 및 추가 내비 이미지: 졸음 귀가 후 당일 영업 종료,5초 최초 토스트 뒤 택시 Zzzz, `택시 시뮬레이터` 표기, 포만감10미만 피로2배, 시설 한글 잘림/길안내 실패·긴 설명 제거, 호출 행 예상 운임만, 저속 좌/우 조향 강화, 남행 경로의 남쪽 블록 우회 수정. 세부 값/직접 진행은 기존 위임에 따랐다. 마인드맵·기존 미추적 사용자 파일 미수정, 필수 지도 입력2개/배포ZIP 미수정. 새 브랜치 `codex/drowsy-map-handling-20261027`, 기준 `79e3d22`.

## 구현

- `EconomyLedger.UseEmergencySleep`:100초/피로-20 유지 후 Closing, 당일 정산. 정상 수면-40 추가 적용/같은 날 재영업/중복 버튼 금지. 차량 정차 후 확인 버튼은 유지, 이동 중 텔레포트 없음. 다음 날 기존 야간 회복은 별도.
- `DrowsinessHUD`: 누적 활성5초까지 토스트 한 번, 이후 택시 위 Zzzz.60~180초 지속 시야 축소/120초 승객 취소/180초 상실·자동 정차 유지. 완전 상실 문구 `졸음 운전으로 위험합니다. 오늘 영업을 종료합니다.`와 종료 버튼. 기존 매 프레임 피로 알림 갱신 제거. 저장된 노출 시간을 기준으로 경고를 반복하지 않는다.
- `DriverCondition.Advance`: 포만감25이상 피로+4/분,10이상25미만+6,10미만+8. 경계별 적분, 수면 중 증가 없음,1.5×2 중첩 아님.
- `FullMapOverlay/MapDestinationUI`: 한글 툴팁 실제 줄 높이/330px 이내 너비. 시설명·지원·거리·버튼만, 상세 이용 설명 제거. 호출 행은 승차/하차 식별+예상 운임만(기존 점수 보정 내부 계산 유지). 수락 기한/세차/탑승 제한 자체를 삭제하지 않음.
- `ServiceDepartureNavigation`: Nearest4m 안에 실제 진행 차로가 없을 때에만 지정 시설 내부의 출구 경로를 수동 안내하고 해당 출구의 연결 차로에서 내비 계속. 주유소 drivePath/식당 leave/기존 주차장 바인딩만 이용하며 도로 전체 스냅 반경을 넓히지 않는다. 초기 주차장에서 세 주유소는 원래도 연결됐고, 주유소 내부 출발은 실제 재현 후 수정했다. 사용자 사진의 정확한 위치/방향 좌표는 제공되지 않아 모든 미연결 위치 해결로 확대하지 않는다.
- `DriveMath.ManualSteerAngle`: 저속 증폭14%→40%, 중앙각 최대48→52도,14~30km/h에서 감소.30km/h 이상과 H 조향은 기존 값. 전륜 Ackermann·차속 제한 유지.
- 추가 남행: J8066/J7973 방향 기존 동일 방향 차로에14개 게임용 차로변경 추가. 기존 차로변경의 출발점만 공유하면1→2 도착 뒤2→3 출발점을 지나쳐 두 번째 변경을 못 하는 원인이었다. 후반 창을164→188/194→218m로 엇갈리게 두어 남쪽 블록 U루프 없이 기존 J8066 우회전 사용. 재현 출발 `local:7847:9404:8047:6565:part:2`160m→국도42승하차1:1752.25→709.09m. 사용자가 그린 J7973 직접 우회전은 같은 목적지까지2302m가 더 필요해 더 짧은 기존 북쪽 우회전을 선택한다. 새 회전 허용/역방향 차로는 만들지 않았고 신규 연결은 provisional·독립 실측 아님.

## 검사·발견·수정

| 검사 | 실제 결과 |
|---|---|
| 최소 RED | 심한 공복60초 피로6(기대8),15km/h 회전 반경,긴급 수면 뒤Open(기대Closing) 실패 확인. 처음 csc 상대 경로/배열 장치 오류는 RED 근거 제외 |
| 모델 GREEN | `tools/test_prototype.ps1`:26묶음 종료0, `latest-feedback-model-green.log`. 새 심한 공복8,피로15,수동 조향27단언 포함 |
| care | `tools/test_passenger_care.ps1`:13,301단언 종료0, `latest-feedback-care.log` |
| 내비 RED | 정확한 source part2/160m에서1752.25m·남쪽 블록 루프 재현(`latest-south-red2.log`). 첫 origin/200m fixture는 기존 생성기 part별 lineage라 null로 실패하여 근거 제외 |
| 내비 보완 | 가운데 원본 part2가0.000061m짜리 미세 분할 조각이라 기하 검사 실패→실제 part3 사용. 처음14연결은 후반 출발 창이 같아 우회가 남았다. 그 실행에서 저장한 자신의 지도만 fingerprint/기존hash48a84e21 검증된 자동.bak로 되돌려 재작성. 최종 authoring은 저장 전709.09m/남부블록미통과 확인 후 저장, 이전 직접회전/난간 회귀도 실행. 보존한 최종 도구는 .bak 복원 기능 없이 idempotent |
| 시설 RED→GREEN | 초기 주차장→주유소3곳은 원래 통과. 주유소 내부 출발 preview north 실패 재현(`latest-departure-red.log`)→출구 수동 연결 후세 주유소 preview/activepath/실제출발위치·시설밖 거부 통과 종료0 (`latest-departure-green.log`) |

## 최종 통합·배포

단일 독립 리뷰: Critical0/Important1/Minor1. Important는 시설 plot 경계 밖~차로4m 이내 전의 출구 공백으로 안내가 끊기는 문제다. 출구 전 구간 연속 검사로 재현 후 지정 drivePath/leave의 좁은 통로까지 허용한다. 추가 발견된 동부 휴식 거점의33m 떨어진 출구 미지원도 사용자 ‘시설 길안내’ 범위의 Important로 판단해 같은 수정에서 포함한다. Minor(보류)는 식당/휴식/주차 선택 패널에 기능 소제목이 별도 표시되지 않는 점이다. 시설명/기호/현장 F7은 유지하며 주유소는 주유전용/주유+세차를 계속 표시한다.

리뷰가 판단 보류한 실제 렌더·물리 체감·실행 저장·배포 무결성은 아래 실제 실행 결과/미검증 구분으로 처리한다. 신규 남행 창은 게임용 provisional이며 현장 전수 규제 검증으로 확대하지 않는다. 최종 Editor 실행 중 실수로 두 번째 Unity를 시작해 프로젝트 중복 실행 충돌 종료1073741845가 발생했다. 이 실행은 출구 RED 근거가 아니며 기존 프로세스 종료 후 순차 실행한다.

최종 검증·기본 배포 교체 완료. 위치제어 검사와 사람의 물리 조작/장시간 주행은 구분한다. 이번 요청에는 추가 main 병합/푸시가 없어 작업 브랜치에서 제공한다.

1차 `LatestFeedbackTests.RunFinal` 종료0 (`latest-feedback-final-editor.log`, `LATEST_FEEDBACK_FINAL_EDITOR_OK`). 리뷰 수정의 실제 출구 RED는 단독 `latest-exit-continuity-red2.log`에서 북부 출구 sample40에 실패, 수정 후 `latest-exit-continuity-green.log` `LATEST_EXIT_CONTINUITY_OK`: 북/남/동 주유소·동부 휴식·식당의 출구 전 구간 preview+active 안내와 시설밖 거부 통과. 해당 GREEN 이후 최종 통합/빌드·실행 검사를 수행한다. 정식 지도1888차로, contentHash `76ebb91c8e5ae202f81c15a5df399375db2273157406eccc4b07ef258ebfe758`. 필수 입력2개/ZIP은 유지한다.

기존 사용자 게임 프로세스(PID41776,14:03시작)가 기본 배포 파일을 사용 중이라 임의 종료하지 않고 저장·종료를 요청했다. 실행 중 파일을 덮어쓰지 않으며 배포 교체 결과를 아래에 추가한다.

사용자가 저장 후 종료를 회신했고 `USER_GAME_COUNT=0`을 확인했다. 이후 기본 `PrototypeBuild.BuildHybrid` 종료0 (`latest-feedback-release-build.log`, `PROTOTYPE_BUILD_OK builds/EungyeTaxi/EungyeTaxi.exe`). 별도 검증 빌드의 숨김 창 실행은 UI27 논리 검사가 통과했어도 캡처가 전부 검게 나와 시각 완료 근거에서 제외했다. 숨김 피로 실행의4개 contrast 실패/종료1도 같은 이유로 기록하며 다른 기능 단언 실패는 없었다. 실제 표시 창으로 기본 배포를 다시 실행하여 캡처/조건/내비를 확인한다.

리뷰 반영 후 `LatestFeedbackTests.RunFinal` 재실행 종료0 (`latest-feedback-reviewed-editor.log`, 전체 Editor/244출구연속 검사 포함). 소스 `a2fc016d43053d0a3cfc56e93c546fdcd068c29d`에서 `PrototypeBuild.BuildAssistedChecks` 종료0 (`latest-feedback-validation-build.log`). 이는 같은 은계/BuildAt 파이프라인을 별도 `builds/AssistedTaxiChecks/`에 출력한 **검증 전용** 빌드이며 기본 BuildHybrid 배포 교체 완료로 계산하지 않는다. 해당 stamp는76ebb91c...와 일치한다. 최종 모델26 종료0 (`latest-feedback-final-model.log`), 필수 입력 Strict 종료0·문제0/선택2개 없음 (`latest-feedback-workspace.log`).

기본 첫 BuildHybrid에서 실제 표시 Player: UI27/피로34/내비30 각각 종료0. 피로34에는 실제 접지/매너 활성 자동 정차,0원 취소,긴급 회복80/Closing,당일 정산 단1회,실제 JsonUtility 종료 상태 복원이 포함된다. 졸음4화면 중 Zzzz/상실 문구 직접 확인,호출 목록의 예상 운임만 표시되는 화면 확인. 다만 시설 패널 캡처는 Open 직후 OnGUI가 그려지기 전 월드 프레임을 읽었으므로 UI 시각 완료로 계산하지 않았다. 검사 장치에0.3초 repaint 대기와 검은 캡처/실제 지도 모달 픽셀 검사를 추가했다(`f61d18c`,일반 게임 기능 변경 없음). 해당 소스로 최종 BuildHybrid/UI 재검사 후 아래 manifest에 반영한다.

### 최종 실행·배포 결과

- 소스 `f61d18cdafdef98534c6fe8c4c0b00e6a7781704`, 정식 `PrototypeBuild.BuildHybrid` 종료0 (`latest-feedback-confirmed-build.log`). 기본 `builds/EungyeTaxi/EungyeTaxi.exe` 교체 완료. 마지막 소스 변경은 UI 캡처 검사 장치뿐이며 일반 피로/내비 코드는 앞선34/30실행과 동일하다.
- 최종 기본 실행 `-hybridV2 -feedbackUITest`:44검사 종료0 (`latest-confirmed-ui.log`). 시작 문구·시설 패널1280/1920·호출 요금 단순화 화면 직접 확인. 검은 캡처나 지도 열기 전 월드 프레임을 더 이상 성공으로 계산하지 않는다. 졸음 toast/Zzzz/종료 메시지도 실제 표시 캡처에서 직접 확인했다.
- `tools/update_distribution_manifest.ps1 -SourceCommit f61d18cdafdef98534c6fe8c4c0b00e6a7781704 -ValidatedOn 2026-10-27 -ValidationReport artifacts/latest-feedback-release-validation.json`, 이어 `-VerifyOnly` 종료0:130파일/105,863,666바이트 전수 SHA256 일치. 검증 보고서 내용은 추적 manifest의validation에 포함했다. 빌드 stamp/정식 지도 hash76ebb91c...일치.
- `git diff --check` 종료0. 마인드맵 SHA256 `F1951492F9C5A13941E60A17D92EB31B1A7471FB8ABA7996D47A0C2E02F00937` 유지. 검증 전용 AssistedTaxiChecks/산출물·기존 사용자 미추적 파일은 커밋하지 않는다.
- 남은 범위: 사람의 저속 조작감/시설 물리 왕복/장시간3일 밸런스·다른PC/DPI 및 전체 현장 규제 검증. 일부 비주유 시설의 기능 소제목은 리뷰 Minor 보류. 이전 피로 취소 영수증의 요청 이력 표시 후속도 유지한다. 기본 게임을 임의 종료하지 않았고 사용자의 저장·종료 회신 후 교체했다.

재현: 기본 `builds/EungyeTaxi/EungyeTaxi.exe`, 빌드 `PrototypeBuild.BuildHybrid`. Unity `LatestFeedbackTests.RunFinal`은 읽기 검사, `LatestFeedbackTests.ApplySouth`는 정식 지도 저장이므로 일반 재검사에서 실행하지 않는다. Player `-hybridV2 -fatigueOvertimeTest -fatigueCapture`, `-hybridV2 -feedbackUITest`. 실제 이력 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-27-drowsy-map-handling.md`로 확인한다.
