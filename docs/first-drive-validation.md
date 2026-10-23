# T1–T2 첫 주행 시제품 검증

검증일: 2026-10-14. 실행 파일: `builds/YeongtongTaxi/YeongtongTaxi.exe`.

## 개발·실행 환경

- Unity Editor 2022.3.62f2: `D:\Program Files\Unity Hub\Editor\2022.3.62f2\Editor\Unity.exe`
- Windows 11, Intel Core Ultra 7 270K Plus, 메모리 약 32GB
- NVIDIA GeForce RTX 5060 Ti, 사용 가능 VRAM 7891MB, Direct3D 11
- Built-in 렌더 파이프라인, Mono Windows x64 Development Build
- 외부 유료 에셋이나 추가 패키지 없이 제작. 새 Unity 버전을 설치하지 않았다.

## 완료된 구현

원본 상대 좌표를 유지한 260×275 시험 구역, 도로 중심선 45개·폭 속성, 건물 외곽 42개와 임시 높이, 건물 충돌체, 주황색 구역 경계, T1/T2 표지, 임시 택시와 WheelCollider 4개, 키보드/패드 입력, 속도에 따른 조향 보정, 제동 후 후진, 후방 카메라, 속도계, 미니맵, T2 도착 횟수, 리셋, 일시정지·종료 메뉴를 구현했다.

## 실제 실행 결과

| 검사 | 결과 |
|---|---|
| Python 지도 변환 단위 테스트 | 4/4 통과: 경계 교차 선분, 외부 선분, 오목 지붕 면적, 실제 지도 범위 |
| 순수 C# 운전 정책 | 9/9 통과: 속도별 조향, 후진 전 제동, 속도 상한, 브레이크 우선, 정차 유지 |
| Unity 컴파일·Windows 빌드 | 성공, 에디터 종료 코드 0 |
| 2초 가속 | 전진 5.93m/s, 이동 5.85m |
| 제동 | 1.5초 후 0.012m/s |
| 후진 | -3.75m/s |
| 리셋 회귀 검사 | 초기 안정 위치 대비 0.30m 이내 복구 |
| 조향 | 2초 동안 방향 약 29.98도 변화 |
| T1→T2 실주행 | 실제 차량 물리로 전체 구간 통과, 도착 카운터 증가 |
| 일시정지 | 정지 중 물리 위치 유지, 정지 중 리셋 후 재개 |
| 화면 | 실행 파일에서 1280×720 캡처 후 직접 확인 |
| 원본 보존 | NGI/NDA/XML 3개 모두 기존 SHA256과 동일 |

증거는 `artifacts/unity-build.log`, `artifacts/player-smoke.log`, `artifacts/drive-smoke.txt`, `artifacts/first-drive.png`에 있다. 첫 검사에서 리셋이 7m 어긋났지만 수정 후 최종 `success=True`를 확인했다. 런타임 로그에 Exception/Error는 없다. 에디터의 초기 라이선스 토큰 안내 뒤 라이선스 갱신과 빌드는 성공했다.

## 발견·수정한 문제

- Rigidbody 보간 상태에서 Transform만 변경한 리셋이 물리 위치에 반영되지 않았다. Rigidbody 위치·회전과 물리 동기화를 함께 적용했다. 근거: [Unity Rigidbody 보간 문서](https://docs.unity3d.com/2022.3/Documentation/Manual/rigidbody-interpolation.html).
- 같은 벽 정점에 앞뒤 삼각형을 중복 연결해 법선이 상쇄됐다. 건물 외곽 방향에 맞는 한쪽 삼각형만 생성해 검은 벽을 수정했다.
- 보이지 않는 경계 충돌벽을 눈에 보이는 주황색 벽으로 바꿨다.
- 코드 리뷰에서 위 문제들을 확인했고 수정 후 빌드·주행·캡처를 다시 수행했다.

## 아직 검증하지 않은 범위

실물 게임패드, 사용자의 수동 키보드 주행 감각, 장시간 성능, 모든 건물과의 충돌 시나리오는 별도 시험이 필요하다. 자동 검사는 운전 입력값을 주입하므로 실제 키보드·패드 장치의 동작까지 증명하지 않는다. 이번 성과는 첫 주행 시제품이며, 현실의 현재 도로·차선·교통 법규 재현이나 전체 택시 게임 완성이 아니다.

승객·운임·유지비·신호·교통 AI·내비게이션과 지도 고도는 후속 범위다. 조작법은 [프로젝트 안내](../unity/EungyeTaxi/README.md)에 정리했다.
