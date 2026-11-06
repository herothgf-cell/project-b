# 2026-11-07 비 프로토·콘텐츠 툴

## 요구와 실제 구현

사용자 승인: 프로토는 비만. 표현/자동 와이퍼/승차 오염/제동부터 개발, 확률 미끄러짐은 검증 후 활성화. 수치를 기존 툴에서 편집한다.

- 기획18, 마인드맵 원본/목차, 체크리스트/범위/담당 문서를 연결했다. 원본360노드의 ID·순서/부모·접힘·기존 메모·링크를 보존하고4노드를 추가했다.
- 날씨 탭6값: 비 사용, 빗줄기 강도, 자동 와이퍼 주기, 승차 오염, 제동 계수, 교통 속도 계수. 기본 맑음. 구 프로필/저장은 강제 변경하지 않으며 기존 snapshot 계약으로 세션/이어하기/LAN 호스트 설정 유지.
- 로컬 차량 주변 최대900 파티클·흐린 하늘/조명. 1인칭 전방 자동 와이퍼, 후방/3인칭에서는 숨김. 강도0은 빗줄기만 끈다. 젖은 유리/도로 재질·빗소리는 후속.
- 승차 전 청결 보너스 평가 후 성공 승차ID별 추가 오염2점. 거부/적재 대기·복원 재적용 없음.30미만이면 다음 승차만 제한하고 현재 운송 유지.
- 비 제동0.9는 이동 중 토크와 제동 중 전후 접지력에 적용. 정차 유지/횡조향/맑음 보존. 일반 교통 목표속도0.85·속도별 추가 차간 여유, 내비 이동시간 보정.
- 확률 미끄러짐은 구현/활성화하지 않고 잠금 상태로 명시했다. 눈·동적 낮밤·요금/수요 변화·타이어 구매 제외.
- 네트워크 물리가 달라져 LANv14로 갱신. 은계 싱글/자체도시A2 통합 exe, BuildHybrid 유지.

## 변경 파일

RainPolicy/RainPresentation/RainPlayerCheck, RainBuild, TaxiController/PassengerJobs/TrafficCar/RoadNavigation/DriveSession, ContentTuning/PvpNativeLan, config 및 Resources 콘텐츠 사본, tools/content-editor의 profile/app/content-model, 비 테스트/마인드맵 갱신 도구, 기획18 및 연결 문서. 실행본/manifest는 별도 출하 커밋으로 기록한다.

## 수행한 검증

| 종류 | 실제 결과 |
|---|---|
| 실패 선행 | RainPolicy 미존재 실패, 툴 weather 항목0개/범위 검사 실패 확인 후 구현 |
| 순수 모델 | `pwsh -File tools/test_rain_policy.ps1` 11검사 통과 |
| 기존 C# 설정 | `pwsh -File tools/test_content_tuning.ps1` 751검사, UTF8/이관 C#↔Node 해시 일치 |
| 툴 API/모델 | `node --test tests/content-editor/*.test.mjs` 89/89. 최초 localhost EACCES는 허용된 로컬 API 실행으로 재검사 통과. 생성본 차이는 canonical generator 재생성/verify로 해결 |
| 생성본 | `node tools/content-editor/choice-catalog.mjs . --verify` 1391항목·71선택지·프로필155161bytes |
| 필수 지도 | `pwsh -File tools/check_workspace.ps1 -Strict` exit0, Input issues0. 지도/ZIP 변경 없음 |
| Unity 빌드 | `RainBuild.Build` → `PrototypeBuild.BuildHybrid` exit0. 최종 artifacts/rain/build-final.log |
| 실제 실행·위치 제어 | `pwsh -File tools/test_rain_player.ps1` 20검사. 실제 성공 승차/청결98·시작 보너스3·중복/복원 거부·현재 운송 유지·저장 날씨 복원·와이퍼 시점·파티클 검사 |
| 실제 PhysX 제동 | 평지 직진20km/h·동일0.3제동: 맑음2.552734m, 비2.833984m(약11% 증가). 토크만 조절한 최초 실행은 차이가 없어 전후 접지력 조절 후 재검증 |
| LAN | 비 호스트/맑음 클라이언트 프로필, 호스트41/클라이언트52 검사 통과. artifacts/pvp/native-lan-20261107-012845-8e91c273. 두 로컬 프로세스 검사이며 다른PC 검증 아님 |
| 실제 렌더 | 최종 카메라 RenderTexture 캡처에서 빗줄기/흐린 하늘·와이퍼 확인. artifacts/rain/first-person.png. 최초 숨김창 ScreenCapture 검정 이미지는 합격 근거로 사용하지 않음 |
| 마인드맵 | `python tools/update_rain_mindmap.py --check` 364노드, 기존 노드/메모/순서/접힘/링크 보존 |
| 코드 독립 검토 | 읽기 전용 리뷰: 중요 결함 없음. 강도0의 의미를 빗줄기만으로 명확화. 제동 delta 추가 검토 완료 |

비 실행 검사에는 `-oneDayMode -hybridV2 -contentTest`를 사용한다. 초기 레거시 모드 인자 누락에 따른 snapshot 검사 실패는 재현 스크립트에서 수정했다. 사용자 캠페인이 아닌 독립 임시 저장 경로임을 실행 검사했다.

## 미검증 / 다음 작업

- 사람의 키보드/패드 수동 난도·급회전 중 제동·고속/경사로·전체20분/3일 세차/수익 밸런스, 다른PC LAN, 장시간 성능은 미검증.
- 툴은 모델/API 검사이며 이번 작업에서 브라우저 클릭 수동 검증은 하지 않았다.
- 확률 미끄러짐 위험 구간/추첨1회·저장·예고·회복/공정성 테스트와 명시적 활성화는 다음 단계. 현재 사고 자동 추첨 없음.
- 비 기본값은 꺼짐. 툴 날씨 탭에서 켜고 저장/게임 적용 후 새 세션을 시작한다. 이어하기는 저장 당시 설정을 유지한다.

## 협업 보존

시작 시 LATEST의 별도 Higgsfield 문구와 미추적 문서/라이선스/지도편집 자료를 보존했다. 작업 중 다른 작업의 f6f17f2 기획 검토 커밋이 추가됐으며 유지한다. 사용자 활성 프로필/개인 저장을 덮어쓰지 않았다. 빌드/검증 완료 후 이 작업 파일만 커밋하며 원격 push는 이번 요청에 없어 수행하지 않는다.

## 출하 영수증

실제 소스 커밋 `bbc85b9f42eda5d262920d2a9534f09623a20072`. 최종 통합 실행폴더 manifest 142파일 / 132,267,902.0bytes 생성 및 통합/PVP VerifyOnly 모두 exit0. 실행 대상 `builds/EungyeTaxi/EungyeTaxi.exe`, 은계/A2·LANv14. 원격 push 미실행.
