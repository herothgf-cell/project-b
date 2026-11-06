# Taxi exterior polish Implementation Plan

> Use superpowers:executing-plans for inline work and one independent final review.

Goal: 기존 승인된 노란 소형 택시를 더 자연스러운 차체·휠아치·매립형 램프·도어 경계로 개선한다.
Architecture: TaxiArt의 시각 메시만 변경. 기존 물리/콜라이더·차륜위치/카메라/UI 유지. MCP 연결된 D 원본 Editor에 이번 파일만 동기화하고 편집 전 baseline hash를 확인한다. 사용자 Package 변경은 보존한다.
Tech Stack: Blender5.2 MCP 원본/FBX/평가 메시, Unity2022.3 Standard material/prefab, Unity MCP 실제 Editor 렌더.

- [x] 곡선 차체와 휠아치, 램프·도어·유리 디테일. Editor 메시/충돌 검사.
- [x] MCP 전후 스튜디오 실제 렌더 확인·수정. 기존 시점 Player35와 운전 설정 검증.
- [x] 통합 빌드/배포 manifest, 문서/검증 기록, 기존 승인 main 배포 및 사용자 변경 보존.

Ruling: 스타일/물리 범위는 이미 승인된 시안을 따른다. 새 방향 선택 대신 사용자가 요청한 같은 택시의 품질 개선으로 진행한다. MCP는 모델을 자동 고품질로 변환하는 기능으로 설명하지 않고 편집·실제 렌더 반복 검증에 사용한다.

Ruling: 사용자 추가 지시로 Blender MCP 모델 제작→Unity MCP 반입/검증으로 확장. 앞선 procedural 비교 메시 파일은 출하하지 않고 기존 fallback을 유지. 모델링/표현만 변경, 물리/UI 코드 보존.
