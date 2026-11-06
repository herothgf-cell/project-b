# 2026-11-07 승객 프리셋별 대사·확률·효과

## 요구사항·실제 구현
동일 특성도 승객마다 대사·발생 확률·효과 수치를 다르게 제작하도록 기획/툴/게임을 확장한다. [기획16](../content-design/16-passenger-preset-overrides.md), [설계](../superpowers/specs/2026-11-07-passenger-overrides-design.md), [계획](../superpowers/plans/2026-11-07-passenger-overrides.md).

- 공통 상속+명시값 교체. 내부 프리셋v2 overrides배열, v1/기존 해시 보존. 최초 개별 작성 때 이관,0·동일값 고정과 삭제 복원 구분.
- 실제 주 행동/요청/상황 대사·선택 단계 효과·음주/음식 확률·관련 오염/작업 시간만 카탈로그/특성 검증. 전역 생성/경제/법규와 고정 구토 규칙 제외.32명/승객128개/JSON32000자/전체240KiB.
- 도구 분류·검색·공통/개별/최종값·복원·충돌. 오염/감점 부호 표시와 저장 변환은 기존 규칙 재사용. 복제는 독립 복사, 빈v2 이관만으로 모든 승객을 변경 표시하지 않음.
- 전역 설정을 바꾸지 않는 승객별 resolver. Behavior에 개별값 보존·copy/validate때 재구성한 resolver 캐시. 선택장부는 승객 설정으로 검증, 요청/상황/경유/취객 상태의 표시·진행·복원 일치. LANv13.
- 예제16명(기본12+개별4) 가져오기 파일. UI로 별도 passenger-local-example에 적재17초·구토10%·개별 대사를 저장/재로드. 실제 활성 설정은 그대로.

## 변경 파일
C#: ContentTuning, PassengerBehavior/Probe/UI, ChoiceEffects, PassengerRequest/Observation/UI, PassengerJobs/Activities/ChoiceEffects, PassengerSituations, VehicleCare, PvpNativeLan 및 Editor검사3개. JS: passenger-presets 모델·profile검증·app/style·카탈로그 설명/양쪽 생성본. 기본 프로필 수치 변경 없음. 새 PassengerOverridesTests/runner·Node개별설정/서버검사·JS/C#공통25fixture. 기획02/07/15/16·목록·toolguide·scope/checklist·LAN기획, 실제 화면3장·예제·이력/LATEST, 통합실행폴더/manifest.

## 검증과 실제 결과
| 검사 | 결과 |
|---|---|
| 도구 전체 | Node80/80 exit0. 저장·가져오기·적용·개별편집/부호/복원/충돌. artifacts/passenger-overrides/node-tests.log |
| 개별 C# |42 PASS, 공통25fixture·0/100·효과1/7·공통10보다 긴17초 복원·변조장부 거부·schema1 guard·동일기본대사·resolver 재사용 |
| 기존 조합 |73 PASS |
| 관련 순수 |ContentTuning760+C#/Node해시, care13301, rest40, ChoiceEffects50, ChoiceDriver6 PASS |
| Unity |첫/최종 ContentTuningBuild.Build → runtime32/build3/BuildHybrid PROTOTYPE_BUILD_OK. build-final.log return code0 |
| 최종 실제 Player |player-final.log PASSENGER_OVERRIDES_PLAYER_OK checks57. 실제 배정·선택대사·즉시1/7·음식0/100·오염2/9+흘림4·요청대사/응답·JSON장부복원·적재3/17초·취객0/100계획복원. player.txt. 위치·시간 제어 lifecycle, 수동 운전 아님 |
| LAN |첫 빌드(마지막 캐시/경유문구 수정 전) protocol13·신규hash복제 host41/client52 PASS. native입력/WheelCollider 약19.6m, 같은PC loopback·재접속 없음. artifacts/pvp/native-lan-20261107-001652-aaf32a89 |
| 실제 UI |별도 새프로필→취객복제→짐→구토10/적재17/대사→특성제거오류→복구→공통복원→저장확인→reload/불러오기 값일치.1280×900/800×900 캡처, 좁은창 수평넘침 없음(785/785). 활성적용 안 함 |
| 입력 |check_workspace.ps1 -Strict exit0/issues0, 지도/ZIP 변경 없음 |
| 사용자 저장 |작업 전 Taxi Prototype 하위29파일 SHA256 최종 변화0 |
| 생성 자료 |choice-catalog root --verify exit0:71선택/1385항목/134694바이트. 예제16명 hash8d5567e6fd645f9a41a23d363013e960a89b585e64611cc845bca07559f98f82 |

검토 P2 두 건(경유20초 고정 안내, 매 접근마다 개별 설정 전체 복제)을 수정하고 독립 재검토 승인. 캐시 수정은 실패 재현→통과. JS는v2 미지원·UI 미존재 실패부터 확인해 구현. PowerShell 문서 일괄작성 첫 명령은 스마트 따옴표 구문오류로 실행 전 중단했고, apply_patch/정상 명령으로 재작성했다.

기존 실패4건을 통과로 표시하지 않는다: PassengerBehaviorTests `followup remembers actual request response Comfort`, PassengerSituationTests `awake passenger receives typed neutral reassurance`는 전 작업 baseline재현 유지. 추가로 ChoiceStructureAudit의 공감 미응답-5 기대(현재-2), 전체 test_prototype.ps1 앞20그룹 통과 후 ManualHandlingTests 일반S제동 기대(actual8.823529)는 HEAD 원본으로 별도 컴파일해 동일 실패. LegacyChoiceStructure/LegacyManualHandling 및 runtime-report.md가 근거. 이번 승객 변경과 무관한 주행 소스 수정 없음.

## 실행·재현과 미검증
1. 설정 관리.cmd → 승객 프리셋 → 이 승객만의 설정. config/content/presets/passenger-overrides-examples.json 가져오기 가능.
2. 검증/저장/게임에 적용. 새 싱글/새방부터 적용, 기존 이어하기 보존. builds/EungyeTaxi/EungyeTaxi.exe 은계/A2 통합·PrototypeBuild.BuildHybrid·LANv13.
3. exe `-oneDayMode -hybridV2 -passengerBehaviorTest -passengerOverridesTest -batchmode -nographics -logFile <절대경로>`: artifacts 격리 저장, player.txt 신선한 PASS57 확인.
4. `node --test tests/content-editor/*.test.mjs`, `pwsh -File tools/test_passenger_overrides.ps1` 및 조합/관련 검사.

수동 장시간·전체3일·다른PC LAN·실물패드 미수행. 새 대화 노드/조건스크립트·구토청결 고정상한 가변화·프리셋별 등장정책은 범위 밖. 기존 미추적 라이선스/마인드맵/과거이력 보존. 원격 push 없음. 로컬 Unity: D:/Program Files/Unity Hub/Editor/2022.3.62f2/Editor/Unity.exe, 다른PC 고정 경로 아님.

## 출하 기록
실제 소스 커밋 후 manifest를 해당 해시로 생성하고 전수 검증을 아래에 기록한다. 자기 포함 커밋을 추측하지 않고 git log로 조회한다.

- 실제 소스 commit `c1ffc293b60f8fdade32d504fe9630e3c2a7f4ae` 확인. 코드·기획·테스트·화면46파일.
- 해당 해시와 실제 검사 JSON으로 통합 manifest 재생성. 통합/PVP VerifyOnly 모두 exit0: **141파일 / 132,060,084바이트**, 크기·SHA256 전수 일치.
- 빌드가 생성한 FirstDrive 장면의 fileID 변경만 원복. 새 테스트/runner의 EOF 공백 정리, staged diff --check exit0. 기존 사용자 미추적 파일은 스테이징하지 않음.
- 브랜치 codex/passenger-composition에 로컬 소스/실행본 기록. 이 기록을 포함한 실제 커밋은 `git log -1 --oneline -- docs/work-history/2026-11-07-passenger-overrides.md`로 조회한다. 원격 push 없음.
