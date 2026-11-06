# 생활거점 수리 알림과 공통 아이콘

## 요구·구현

사용자 승인에 따라 가운데 `내구도…공업사 비교/길안내` 버튼을 제거하고 생활거점 레드닷으로 유도한다. 시안에 맞춘 둥근 아이콘 통일과 후속 HUD 겹침/문구 점검 후 main 게시를 요청받았다. 기존 main `df3e693`에서 시작했고 사용자 변경은 없었다. [작업 계획](../superpowers/plans/2026-11-06-icons-lifehub.md).

- `VehicleDamageController.cs`: 비고장 상태의 가운데 유도 버튼만 제거. 내구도0 고장·견인·수리/영업 종료 모달과 충돌/복구/가격/시간은 유지.
- `ServiceStops.cs`, `CockpitUI.cs`, `CasualUI.cs`: 현재 Care.Damage의 내구도50이하를 읽는 레드닷. 생활거점 버튼과 공업사 항목에 표시하며 열람/닫기로 해제하지 않는다. 수리 후 정상 내구도를 읽으면 즉시 해제된다. 복제된 LAN 상태도 같은 읽기 경로를 사용하며 저장/전송 필드는 추가하지 않는다.
- 생활거점에 공업사 진입을 추가했다. 기존 주유·식사·귀가 주차장·수면에 공업사/닫기를 합쳐3열2행으로 정리했다. 제목/거리 두 줄, 최소 글자 크기·배율·아이콘 여백을 적용했다. 기존 추천은 제목에 표시하며 수리 필요시 우선 설명한다. 공업사는 기존 M지도 시설 비교(kind5)를 열고, 길안내/결제는 기존 명시적 선택/현장 확정을 유지한다. 조회만으로 경로/거래를 바꾸지 않는다.
- `HudGlyph.cs`: 기존 의미/ID를 유지한 공통20종(알림점 포함). 24단위 도형을96×96 캐시로 렌더하고 거리 기반 부드러운 경계·둥근 선/끝·통일된 여백을 사용한다. 외부 폰트/이모지/새 패키지 의존 없음. 기존 지도 선/클리핑 함수는 변경하지 않았다.
- `DriverStatusGauges.cs`: 별도 픽셀 아이콘 제거, 식사/휴식/주유/청결/수리를 공통 아이콘으로 표시. `CockpitPresentation.cs`의 방향지시등·도착 타이머도 같은 체계에 합쳤다. 수치/위험 색/잔여 초/방향 점멸 판정 유지.
- `ServiceStopsPlannerUI.cs`: 후속 가독성 점검에서 발견한 초록 선택 탭/어두운 글자 조합을 초록 바탕/흰 글자의 공통 Action 스타일로 수정했다.
- `BrightUIProbe.cs`: 명시적인 `-lifeHubUiReview`에서만 격리 캠페인의 내구도 경계·열람·공업사 진입·무거래/경로 보존과 싱글/PVP 두 해상도 화면을 확인한다. 임시 픽스처의 손상/수리는 테스트 상태 설정이며 사용자 운행/일반 실행에는 접근하지 않는다.

맵/차량/방 아트·사용자 대사·밸런스 기본값·저장/구매·주행/신호·LAN 권한/시계 변경 없음. 이전 초상 PNG의 importer 재저장 공백은 원상태로 정리한다.

## 검증·실제 화면

- 필수 입력 `pwsh -File tools/check_workspace.ps1 -Strict`: exit0/issues0. 지도 contentHash `35d1bc43d92c996d431acd0464bfc7a175b926cd1e35adb995e99dd7ab91cbac`·입력/ZIP 유지.
- Unity2022.3.62f2 `FullUIBuild.Build`: 최종 `artifacts/icons-lifehub/build-final.log`, exit0. 기존 테마·설정171/메뉴183/모달·콘텐츠runtime32/build3/복사본·HUD focused/integration·BuildHybrid 통과. Player 조립 DLL의 수정 시각은 최종 생활거점 소스보다 뒤이며 실제 캡처도 최종 두 줄/밝은 버튼을 확인했다.
- 사용자 저장 JSON/backup10파일을 실행 전 별도 경로에 복사하고 hash 일치 확인. 실행 중인 사용자 게임 프로세스는 없었다. 검사는 기존 임시 CampaignPath와 별도 설정키/취소를 사용한다.
- 1차 실제 Player `-brightUiCheck -fullUiReview -lifeHubUiReview`: exit0, `BRIGHT_UI_RESULT PASS checks=160`, PNG58개. 싱글/PVP 각각 알림51/50/21/0/수리후, 생활거점 열람 유지·공업사 비교 진입·기존 경로/거래 보존, 이전 전체UI112계약을 포함한다. 사용자 저장10개 사후 SHA256 차이0. 이후 선택 탭 대비를 추가 수정했으며 최종 재검증은 아래 영수증을 따른다.

## 실행·재현

`builds/EungyeTaxi/EungyeTaxi.exe`로 싱글/PVP를 실행한다. 내구도50이하에서 생활거점 레드닷→공업사→시설 비교→길안내→현장 수리 순서다. 내구도 게이지의 수리 권장/필요 문구는 유지한다.

개발 검사는 통합exe에 `-brightUiCheck -fullUiReview -lifeHubUiReview -brightUiOutput <PNG폴더> -screen-fullscreen 0 -logFile <로그>`를 지정한다. 테스트가 일반 사용자 저장을 쓰지 않도록 `-brightUiCheck`를 함께 사용한다. 빌드는 `-executeMethod FullUIBuild.Build`이며 이 PC의 Unity 경로는 기존 전체UI 이력을 따른다.

## 한계·다음 작업

이번 후속은 실제 두 해상도의 HUD 겹침/문구·아이콘 점검이다. 레드닷을 확인했음을 별도 저장하는 기능이나 다른 상태의 새 경고 정책은 추가하지 않았다. 내구도0의 고장 모달이 시설 버튼보다 우선하는 기존 흐름은 유지한다. 통제 위치/상태 통합 검사를 사람 장시간/자동 물리 주행/전체3일·다른PC/2인LAN 검증으로 세지 않는다. 다음은 실제 영업에서 호출→승차→요청→하차→정산 흐름의 안내 이해도 점검이며, 맵/차량 외형은 사용자 의사대로 마지막에 별도 검토한다.

## 최종 영수증

- 초기/탭 대비 후보 실제 Player 각각160검사 PASS/exit0. 독립 소스 검토에서9개 파일의 알림/가드/배율/기존ID·클리핑/대비에 실행을 막는 문제 없음.
- 게시 직전 원격 main이 `4202dac5011339154294cdb88ced90ed7c92ab5c`로 전진했다. 브레이크5항목·NGII 좌회전·LANv11과 관련 검증/문서를 병합한다. 장면 충돌은 최신 main 생성 ID를 유지한다. 원격에서 추가한 속도 카드 공업사 버튼은 이번 승인된 생활거점 단일 진입으로 대체하고 관련 DrivingSettingsProbe의2개 UI 기대값을 레드닷으로 갱신한다. 물리/신호/설정/네트워크 구현은 최신 main 그대로 보존한다.
- 통합 최종 `build-integrated.log`: Unity exit0, 테마/설정171·메뉴183·모달/콘텐츠32·빌드3/HUD focused/integration/제동모델22·실제PhysX8·NGII좌회전회귀/BuildHybrid 통과.
- 통합 최종 `integrated-player.log`: exit0, `BRIGHT_UI_RESULT PASS checks=160`, PNG58. 실제 두 해상도 싱글/PVP·알림/시설/내비/정산/설정/개인공간 확인. 기존 첫 메뉴 초기 캡처는 이번 시각 증거에서 제외했다.
- 통합 최신 운전 설정 Player: `tools/test_driving_settings_player.ps1` 싱글17/PVP15 모두 exit0, 임시 contentTest 캠페인과 독립 PreferenceKey 사용. 사후 사용자 저장10파일 hash 변화0. 이전 main의 LAN51/64·재접속은 보존된 기준 증거이며 이번에 재실행한 것으로 세지 않는다.
- 병합 후 독립 재검토에서도 중요한 회귀 없음. TaxiController/TrafficSystem/DrivingSettings/PvpNativeLan 구현은 원격4202dac와 동일하다. 새 물리/신호 코드는 이번 UI 작성물이 아니라 통합 보존분이다.
- 실제 화면8장은 `docs/ui/2026-11-06-icons-lifehub/`에 보관한다. 후속 HUD 점검에서 두 줄 거리/최소글자·선택 탭 대비를 반영했고 가운데/속도 카드 중복 수리 버튼은 없다.
- 최종 소스/실제 화면·이력 커밋은 `9632ffbc3a1aa0542ab01653a272440a4fe1aa81`이다. `git log -1 --format=%H -- docs/work-history/2026-11-06-icons-lifehub.md`로 확인했다. 병합 커밋은 `71240fd`이고 origin/main4202dac가 조상임을 확인했다.
- 출하141파일·124,555,553바이트, 통합/PVP VerifyOnly 각각exit0. 통합manifest가 위 실제 소스와 `artifacts/icons-lifehub/validation.json`을 참조한다. 기존 추적 Burst txt 포함·active-profile/지도ZIP/필수 입력 미변경.
- 원본 다른 작업 폴더는 수정하지 않았다. 이번 관리 작업 트리의 기본exe와 원격 main으로 배포한다. 실행본 게시 커밋 `715a05d8e276d56c6b199dcf33f1e69e0f830902`를 `git push origin HEAD:main` exit0(`4202dac..715a05d`)으로 게시했다. 직후 `git ls-remote origin refs/heads/main` 같은40자리 해시 확인. 이 후속 영수증 커밋은 문서만 바꾸며 실행본/manifest는 유지한다.
