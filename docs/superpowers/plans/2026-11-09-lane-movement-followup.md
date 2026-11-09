# 차로별 표시·지속 녹색 구현 계획

> 실행: superpowers:executing-plans / independent investigations via dispatching-parallel-agents.

**Goal:** 지정 접근들의 실제 차로 표시와 도로 폭/구분을 정리하고 대기 좌회전 신호 누락을 재현·교정한다.
**Architecture:** 기존 검토 메타데이터·그래프·도로 렌더러를 확장하며 확인된 자료만 반영한다. 수요 감지와 단계 전환을 분리 검사한다.
**Tech Stack:** Unity2022.3.62f2/C#, NGII 프로젝트 지도 JSON, PowerShell, 네이버 거리뷰.
**Spec:** ../specs/2026-11-09-lane-movement-followup-design.md

- [x] J866/추가 화면/동부 두 구간의 식별·촬영월·차로별 허용 자료를 기록한다.
- [x] 지속 녹색을 실제 위치·목적지 상태로 재현하는 실패 회귀를 확인한다.
- [x] 확인한 범위의 차선/화살표/형상·신호 수정과 회귀를 적용한다.
- [x] DrivingReadability의 신호 외형·배치 설명을 제거하고 실제 화면을 확인한다.
- [x] 공식 지도 저장·입력 SHA256·BuildHybrid/552경로/실제 포즈·렌더·신호·회귀를 검증한다.
- [x] 최신 사용자 저장 백업/보존·배포 manifest·독립 검토·이력/LATEST/기획 후 기존 main 보존 게시.

중점: 유턴 조건·반대편·구간 밖 경계 보존, 차로 수와 원본 차로 번호 구분, 빈차/주차장 내비/깜빡이/정지선 경계, 지속 녹색 중 경쟁 수요, 기존 사용자 문서/저장 보존.
