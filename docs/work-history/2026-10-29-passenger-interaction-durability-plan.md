# 2026-10-29 취객 빈도·지속 응대 승객·충돌 내구도 기획

## 요청과 결정

- 취객이 드물다는 피드백, 진상 등 지속 상호작용 승객 추가 개선, 충돌 강도별 내구도 페널티를 기획부터 진행.
- 사용자 확정: Proto는 수리비·시간 중심, 내구도0 운행 불가. 성능 저하는 제외.
- 취객 빈도/유형/대화 간격/손상량/비용/시간은 검토용 제안이며 구현 승인·실행 반영 아님.

## 조사·변경

- VehicleCare.cs는 후보 시드20%·실제 탑승 하루1명, 구토25%·기상 지연30%. DrivingManners.cs는 자기/상대 속도1.5m/s 조건·정상 지면 제외·M 충돌 감점이며 차량 내구도 구현은 없음.
- 현재 P11 무작위 복수 요청 구현을 유지하면서 성향 대화가 요청과 공존하도록 제안. 취객 확률/미등장 보완/일일 한도/휴식 구간을 분리하고 응대3유형을 추가 기획.
- 수직 상대 접근속도 구간별 손상·동일 접촉 중복 방지·수리비/견인/미납/운송 취소/정산/마감/저장 예외를 표로 정리.
- 구현자 관점 검토3건 반영: 첫 유효 도착 후 확정 운임 보존, 경로 요청 없는 성향 대화는 설명만, 견인 전용 공간 및 D0 다음 날/재개 시 견인 진입 보장.
- 기존 후속 사고 문서는 확장 기획과 중복되는 내용을 통합하고 보험/책임/성능 저하 등 후속 범위만 유지. 현재 실행 상태와 제안 범위를 명시.

## 변경 파일

- docs/content-design/02-passengers-and-fares.md, 03-life-and-economy.md: 상세 콘텐츠 규칙.
- docs/content-design/README.md, 00-game-overview.md, 01-driving-and-traffic.md, 05-interface-and-navigation.md, 06-mindmap-review.md: 연결·현재/제안 범위 안내.
- docs/future-design/README.md, accidents-and-repairs.md: 후속 경계 정리.
- docs/prototype-scope.md, prototype-development-checklist.md: 확장 기획·미구현·검증 기준.
- 본 기록과 docs/work-history/LATEST.md.

## 검증·남은 항목

변경13문서의 상대 링크129개·앵커26개·표63개 검사 오류0, 종료0. git -c core.safecrlf=false diff --check 종료0. 최초 diff 검사에서 발견한 EOF 여분 공백을 수정한 뒤 재검사했다. 구현자 관점 문서 검토3건 모두 반영. 게임 코드·지도·실행 파일·manifest·.mmst 변경 없음. 모델/Unity/위치 제어/자동 물리/수동 플레이/전체 캠페인 검사와 빌드는 실행하지 않았다. 추가안은 모두 미구현이다.

최초 문서 편집 명령은 WindowsApps python 실행 별칭 오류로 종료1(변경 없음). 로컬 번들 Python 절대 경로로 다시 실행해 종료0으로 문서 갱신했다. 기존 untracked 파일은 제외했다.

## 다음 작업·재현

1. 승객/차량 본문의 추가 기획 표를 검토하고 시험값 확정 후 구현 계획 작성.
2. 구현 순서는 취객 빈도/응대 공통 흐름 → 비취객3유형 → 충돌 손상/정비 → 저장·경제·실제 물리 검증을 권장.
3. 기본 실행은 builds/EungyeTaxi/EungyeTaxi.exe / 은계, BuildHybrid. 이번 안은 실행 파일에서 재현할 수 없음.
4. 필수 지도 입력·배포 manifest는 기존 상태 유지, 이번 문서 작업에서 Strict 재실행 없음.
