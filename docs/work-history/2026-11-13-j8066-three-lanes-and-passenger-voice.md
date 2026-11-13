# 2026-11-13 J8066 왕복 3차로·좌회전 및 승객 화자 수정

## 요구사항과 근거

- 제보 좌표 x834.2589/z637.2146, 방향267.310242도에서 좌회전 신호 미점등.
- 사용자 최종 정정: 총 왕복3차로이며 방향별 차로 배분이 교차한다. 처음 언급된 왕복4차로로 적용하지 않는다.
- 승객 카드에 기사 응답이 표시되는 문제, 중앙선 법규 감점 도구 편집과 기본1점.
- 2026-05 네이버 거리뷰 서향 FI0tmS2ImG-qd356ibc6TQ, 동향 XDflhzUyeOa5oyWyz4hp6w 및 2024-03 jWo5_6Z38C0iGjCXJWYKhA: J8066 서행2/동행1. 동쪽 아파트 접근 5WjiH29cxCHZWKAgq6_S5g: 동행 좌회전/직진2와 서행1. 링크·범위는 기획21에 기록.

## 실제 구현

- 지도 r19: 서행 좌회전 포켓과 기존 좌회전 연결, 바깥 직진/우회전. 동쪽은 아파트 좌회전 진입부와 직진을 분리한다. 사유지 경계에서 끝나므로 공공 통과 경로를 생성하지 않는다. 방향별 최대2+1로 배분하며 차로 기반 노면 중앙선이 이동한다.
- 포켓90m/55m·테이퍼20m·폭3.3m·아파트 진입 위치는 게임 추정치이며 실측이 아니다. 전체850m 호 구간을 현장 재구성한 작업은 아니다.
- 기존 정확한 제보 위치는 가장 가까운 게임 차로에서2.564687m 떨어져 좌회전 수요가0이었다. 방향지시등을 켜면1이었다. 실제 안쪽 차로를 추가해 신호 수요 판정 위치를 바로잡는다. 신호 단계 전체를 무조건 좌회전으로 바꾸지 않는다.
- 차로 분할 후 주유소의 원본 거리/분할 계보를 해석해 위치를 보존한다. 정식 r14~18 저장은 정확한 r19 해시와 일치할 때 내비 인덱스를 재계산한다.
- 대표 요청 카드/종료 반응은 승객 말투로 표시한다. 기사 응답 저장값과 선택 효과는 유지한다.
- 중앙선 침범 법규 감점 law.centreLine.penalty 기본1, 0~100 편집. 역주행/비허용 유턴과 운전 만족도 설정은 유지한다. [별도 도구 검증](2026-11-13-centreline-law-tuning.md).

## 검증 진행

- 수정 전 J8066WestApproachTests.Diagnose: 지시등OFF demand0/greenFalse, ON demand1/greenTrue.
- 지도 적용 첫검사: 주유소가 짧아진 도로 머리만 참조해 실패. 분할 계보 해결 후 재검사했다. 편집초안의 이전 패키지 해시 검사 경로 오류도 저장 전에 수정했다.
- J8066EastArmMapEdit.Apply 최종 종료0, 정식 패키지 검증/차체 공간/저장 해시/Resources 일치 확인. contentHash 345d93f5499e5dbf52da3a8d831935c1447980492055f192a3280f64b7fd4ef4.
- check_workspace.ps1 -Strict 종료0, 필수 입력 문제0, 선택 재수입 JSON2개 부재.
- PlaytestBalanceTests.Run Unity 종료0: 중앙선 실제 관찰 기본1/사용자7/0 및 기존 역주행·유턴 감점 검증.
- test_passenger_behavior.ps1 종료0,707검사. test_content_tuning.ps1 종료0,849검사 및 C#/Node UTF8·이관 해시 일치.
- PassengerBehaviorIntegrationTests 첫실행은 기존 시험 fixture의 BusinessProgress.session 누락으로 실패. fixture 연결 보완 후 종료0.
- 후속 지도 경로/신호 회귀, 빌드/배포 결과는 아래에 이어 기록한다.

## 재현과 제한

기본 실행은 builds/EungyeTaxi/EungyeTaxi.exe, 은계 싱글. 위 좌표에서 서행 안쪽 좌회전 차로의 신호와 내비를 확인한다. 동쪽 아파트 접근에서 반대 배분과 화살표를 확인한다. 콘텐츠 관리 툴 법규→중앙선 침범에서 감점을 편집한다. 사람 직접 운전·전체3일 캠페인·다른 PC LAN은 이번 자동 검사와 구분한다.

## 통합 회귀 결과

- J8066WestApproachTests.Verify 최종 종료0. x845/x875는 서행2·동행1, x960은 서행1·동행2. 두 좌회전 포켓의 좌회전 단독 화살표 확인.
- 제보 위치의 최근접 차로가 새 서행 좌회전 차로이며 오차0.7353264m. 지시등OFF/ON 모두 demand1·greenTrue. 기존J8066 보호 신호 프로그램으로 검사했다.
- FuelStationLineageTests.Run: 분할 전후 주유소3곳의 펌프·입출구·정차점 위치와 그래프 연결 동일.
- r14~18의5버전 저장 이관, J7917/J7973 기존 차로·목적지·1,482개 정차점 방향쌍 경로 통과. 노면 화살표49사례·322차로·87교차로, 기존 좌회전 감응 검사 통과.
- PassengerBehaviorIntegrationTests 1,526검사 통과. Node 중앙선 새설정/구프로필2검사도 재실행 통과.
- 첫 단면 검사는 긴 곡선 전체의 시작/끝점으로 방향을 세어 실패했다. 단면을 지나는 선분의 방향으로 시험을 수정했고 지도 자체를 이 실패 때문에 변경하지 않았다.
- 독립 코드 검토에서 변경 로직의 추가 결함은 발견되지 않았다. 기존 수면 상태 설명 문구는 이번 기사 응답 수정 범위와 구분해 유지한다.

## 빌드·실행 확인

- PrototypeBuild.BuildHybrid 종료0. 기본 builds/EungyeTaxi/EungyeTaxi.exe 통합 실행 폴더를 갱신했다. 사용자 FirstDrive.unity와 content/active-profile.json은 작업 전 SHA256과 동일하다.
- 사용자 로컬 Taxi.zip과 프로필은 원위치에 보존했다. Git 배포 대상143파일만 artifacts/j8066-release/builds/EungyeTaxi에 복사해 검사했다(초기 파일 목록의 한글 인용 경로 문제를 core.quotePath=false로 수정해143파일 모두 확보).
- 격리 복사본 liveSaveTest 종료0/9검사, 새 프로세스 liveReloadTest 종료0/23검사. 실제 저장·운송·정산·다음날 이어받기 검사이며 사람 주행은 아니다.
- 추가 passengerBehaviorTest는 food choice actual live satisfaction1에서 종료1. 이전 main 배포본679200b도 같은 실패를 재현했다. 시험은 음식 허락 만족도5를 고정 기대하지만 현재 기본 콘텐츠 값은3이다. 이번 변경으로 인한 회귀로 판단하지 않으며 전체 승객 Player 검사가 통과했다고 표시하지 않는다. 해당 검사 후속 정비는 남은 항목이다.
- 배포 manifest의 SourceCommit과 main 푸시 영수증은 실제 커밋 확인 뒤 추가한다.
