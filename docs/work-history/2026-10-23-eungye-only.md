# 은계 전용 지도·프로젝트 정리

## 요구사항과 실제 변경

사용자는 은계지구 지도만 사용하므로 영통 관련 현재 설정·안내를 제거하도록 요청했다.

- Unity 프로젝트를 `unity/YeongtongTaxi`에서 **`unity/EungyeTaxi`**로 이동했다. Assets GUID를 유지하고 도구/설정/문서 소스 링크를 새 경로로 변경했다. 별도 승객·경제·사고 기획32문서는 본문을 변경하지 않고 경로 치환만 했음을 HEAD와 비교했다.
- MapCatalog 선택 목록/기본 지도는 은계 한 곳이다. 오래된 PlayerPrefs·지역 잠금·테스트 인자가 다른 지도 선택으로 이어지지 않는다. 영통 fallback을 제거하고 자료 누락은 은계 준비 방법을 명시하는 오류로 처리한다.
- 일반 `PrototypeBuild.Build`와 `Play-Taxi.cmd`도 은계 빌드/실행을 사용한다. 같은 은계 지역 국도42 차로 시험과 공통 가상 연습/보조 기능은 유지했다.
- 영통 전용 `config/maps/yeongtong*.json`, 구형 exporter를 제거했다. 기존 공통 자르기·삼각분할·샘플링은 `tools/map_geometry.py`로 분리해 현재 importer가 사용한다. 영통 실자료 전용 검사는 제거하고 공통 수학·현재 은계 입력·배포 검사를 유지/추가했다.
- 재수입 manifest와 지도 ZIP에서 영통 drive-map을 제외했다. 은계 배경/국도42 입력2개의 실제 크기·SHA256을 확인하고 새 프로젝트 경로로 ZIP을 재생성했다. ZIP 내부 안내·manifest와 sidecar SHA256도 대조했다.
- 캠페인 저장은 `LocalLow/Taxi Prototype/은계 택시 시뮬레이터/`를 사용한다. 이전 제품 폴더의 같은 파일명/백업은 새 저장이 없을 때 복사하며 이전 원본이나 새 은계 저장을 덮어쓰지 않는다. 3일 캠페인 파일명은 유지했다.
- 현재 README/Unity 안내/지도 준비/편집 안내를 은계 기준으로 정리했다. 과거 검증/작업 이력의 사실은 보존하고 소스 링크 경로만 맞췄다.

## 검증

로그: `artifacts/eungye-only/`. 로컬 Unity 설치 경로 `D:/Program Files/Unity Hub/Editor/2022.3.62f2/Editor/Unity.exe`는 PC별 환경 정보다.

- `EungyeOnlyTests.Run`: RED exit1(only Eungye selectable) 확인 후 GREEN exit0. 선택 목록/오래된 선택/다른 지도 거부/기본 실제 은계 로드, 기존 저장·백업 복사 및 새 저장 우선.
- `ProjectMapBuild.RunTests`: exit0. 기존 프로젝트 장면/객체 편집/Store16·간편7·도로15·확장 검사.
- `ProjectMapSceneWindowTests.Run`: 새 프로젝트 경로에서 exit0. 선택/수정/저장/다시 읽기, 기존 사용자 씬 보존.
- `AssistedNetworkTests.Run`: 은계 실제 지도와 공통 합성망으로 exit0. 별도 보조 시험 기능을 삭제하지 않았다.
- Python unittest: **41 tests, skipped3, exit0**. 은계 원본 GIS가 없는 실자료 검사3개는 skip이며 공통 변환·삼각분할·ZIP/manifest/해시 검사를 통과했다.
- `tools/test_prototype.ps1`: exit0. 경로 변경 후 C# 모델 검사 통과.
- `tools/check_workspace.ps1 -Strict -RequireRawInputs`: exit0, 현재 은계 입력2개 일치.
- `PrototypeBuild.BuildHybrid`: 새 프로젝트 경로, 영통 입력 없는 상태에서 exit0.
- 정식 지도 SHA256 **1b8ab6b86d435935d74cac8c83249daee41eb61a3d89d09fccfca3004c706714** 유지. 은계 원본 형상·OSM 대조의 기준 자료 값은 변경하지 않았다.
- 독립 코드 리뷰에서 저장 이전/경로/기본 실행/ZIP/수입 도구에 추가 중대 회귀를 발견하지 못했다.

## 자동 승인 검토와 안전한 대안

자동 검토가 영통 전용임을 단정할 수 없는 일반 보조 시험 클래스/실행기의 삭제를 거부했다. 해당 기능은 유지하고 은계 입력을 사용하는 방향으로 전환했다. 전체 스테이징과 기획 문서 혼합 위험도 차단되어 파일 목록을 명시했고, 별도 기획32문서가 경로 치환 외에 HEAD와 동일함을 검증한 뒤 추가했다. 기본 폴더의 다른 미커밋 기획/라이선스 문서는 포함하지 않는다.

## 실행과 남은 확인

Unity Hub에서 **`unity/EungyeTaxi`**를 다시 추가해 2022.3.62f2로 연다. 지도 편집은 Taxi → 프로젝트 지도 3D 편집, 빌드는 Taxi → Build Eungye Hybrid, 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`다. [사용법](../unity-map-editor-guide.md).

사람의 Unity Hub 새 경로 등록·마우스 조작, 실제 다른 물리 PC, 수동 전체 캠페인 운전은 미검증이다. 자동 상태/위치 제어 검사와 실제 수동 운전을 구분한다. 새 exe 실행 검사 및 최종 소스/배포 manifest/main 푸시 결과는 아래에 이어 기록한다.

- 새 exe를 -eungye 지역 지정 없이 -hybridV2 -oneDayMode -projectMapTest로 실행한 결과 종료0, 23항목 PASS/success=True. 기본 은계 로드·프로젝트 저장/복제/다시 읽기·시설 위치/핀·기존 버튼3해상도·영업 격리 검사. 실제 수동 주행이 아닌 API/위치 제어 검사다.

## 배포 준비와 동시 작업 통합 대기

- 실제 소스 커밋 cb83977 (이력 git log 확인). main의 승객/청결도 기획 a17ba6d를 6d5a718에서 보존 병합했다. 변경된 문서는 최신 기획 본문을 유지하고 소스 경로만 새 경로로 맞췄다.
- 최종 BuildHybrid 종료0. 배포129파일/118,251,665바이트를 manifest로 생성하고 크기·SHA256 전수 검증 종료0.
- 기본 D:/Git/project-b에서 다른 채팅이 승객/청결도/UI 등 코드를 활발히 수정 중인 것을 확인했다. 작업 중인 소스 폴더의 이름을 바꾸거나 사용자 코드를 stash하지 않고 main 통합을 보류했다. 이 변경은 분리 브랜치 codex/eungye-only에서 보존하며, 사용자에게 다른 채팅으로 경로/통합 순서를 조율할 메시지 전송 승인을 요청했다. 원래 작업 폴더는 아직 변경하지 않았다.

- 최종 배포 c04aa6a를 origin/codex/eungye-only로 푸시 종료0. 최종 새 exe 역시 지역 지정 옵션 없이 프로젝트23검사 종료0/success=True(release-player.log). main과 기본 폴더는 동시 구현 작업 보호를 위해 변경하지 않았다.
