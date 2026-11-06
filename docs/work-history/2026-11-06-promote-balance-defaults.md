# 2026-11-06 1차 밸런스 출하 기본값 승격·게시

## 요청과 적용

현재 툴에 저장된 1차 밸런스를 게임에 적용하고 원격에 푸시하며, 해당 값을 새 기본값으로 삼아 변경됨 표시를 없애도록 요청했다.

저장된 default revision4/hash dc5f0ac0f2435735db5ab192f8a5bfc5b6e41c8200a1b7ddecc7cd1a3996f186와 active가 일치함을 먼저 확인했다. 이전 출하 기준과 다른50개 항목(수치와 사용자 대사 포함)을 config/content/default-overrides.json으로 버전 관리하고, choice-catalog 생성기가 이 승인값을 적용하도록 했다. 생성 config/content와 Unity Resources의 catalog/default-profile을 함께 갱신했다. 새 기본값은 저장된 프로필의 모든 값과 일치하며, 기본값 복원과 active 없는 새 게임도 이 밸런스를 사용한다. 프로필 revision/ID/hash 차이는 변경 항목 수와 무관하다.

구schema1→선택지 효과 이관은 기존 프로필의 수치를 보존하는 기존 계산을 유지하며 새 출하 override를 소급 적용하지 않는다. 기존 운송/이어하기 snapshot은 변경하지 않는다. 이전 카탈로그·기본/저장/active/작업 중 manifest를 artifacts/default-promotion/before-*.json에 백업했다. 타 프로필·사용자 scene 변경을 보존한다.

## 코드·검증

기본값 생성기 override 적용, 출하/Resources JSON, 기본값 숫자를 확인하는 C#/Unity/Node 테스트 기대값, 기본값 복원 안내를 갱신했다. Node는 승격된 preset과 기본값 일치/변경 없음 검사 추가. C#758검사·UTF8/구형 이관 hash 대조, Node63검사, 생성기 --verify 통과. Node 첫 실행의3실패는 옛 기본값 -5/-10 고정 기대값이었으며 승인된 -2/-4/+6으로 갱신 후63개 통과. 이관 의미는 기존 테스트로 별도 검증했다.

실행 기준 C:/git/project-b, builds/EungyeTaxi/EungyeTaxi.exe / 은계 싱글/A2 PVP·LANv10/BuildHybrid. 필수 지도 입력/ZIP 변경 없음. 이번 작업은 수치 기본값/관리창 기준 변경이며 새 물리 주행·사람 전체 캠페인 검사를 하지 않는다. 직전 LAN 전체 검사의 시간 초과를 이번 기본값 승격으로 해결했다고 주장하지 않는다.

최종 빌드/관리창0개/적용/배포·푸시 증거는 아래 영수증에 기록한다.

## 기본 대사 검증 보완

기존 OverrideText는 카탈로그 기본값과 같은 문자열이면 동적 내장 답변을 사용했다. 사용자 대사6개를 기본값으로 승격한 뒤에도 실제 게임의 기사 답변에 그대로 표시되도록, 생성 카탈로그의 literalReplyIds에 승인된 사용자 답변 ID를 기록했다. C# 파서는 해당 목록의 ID/텍스트 타입/중복을 검증하고 해당 답변에만 기본 문자열을 그대로 사용한다. 나머지 동적 경로 안내는 유지한다. 새 프로필 항목이나 snapshot 형식 변경은 없다. C#760검사(추가2개 기본 대사/동적 안내 보존)·Node63검사 통과. 이 보완을 포함한 최종 통합 빌드 로그는 artifacts/default-promotion/unity-final-build.log.
