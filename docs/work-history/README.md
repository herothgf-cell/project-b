# PC 간 작업 이력과 인계

작업을 이어가기 전 [최신 인계](LATEST.md)를 읽는다. 이후 각 작업은 날짜·주제별 파일에 계속 누적하고 최신 인계를 갱신한다. 과거의 검증 범위를 새 완료 결과로 덮어쓰지 않는다. [루트 작업 규칙](../../AGENTS.md)이 이후 작업에도 이 절차를 적용한다.

## 다른 PC에서 시작하기

2026-10-21부터 **실행 폴더와 지도 ZIP을 Git main으로 받는다.** 테스트만 할 때는 `builds/EungyeTaxi/EungyeTaxi.exe`를 바로 실행한다. 개발할 때는 `distribution/maps/Eungye-map-inputs-2026-10-20.zip`을 **저장소 루트에** 푼다. ZIP 안에도 안내와 입력 manifest 사본이 있다. [빌드 없는 실행 안내](../../distribution/README.md), [ZIP 설치·개발 안내](map-transfer-guide.md)를 따른다.

1. 이 GitHub 저장소를 복제하거나, 현재 PC의 변경을 먼저 확인한 뒤 `git pull --ff-only`로 동기화한다. 진행 중인 변경이 있으면 먼저 기록하고 커밋해 PC 간 작업이 엇갈리지 않게 한다.
2. Unity Hub에서 **Unity 2022.3.62f2 + Windows Build Support**를 설치한다. 프로젝트 위치는 `unity/YeongtongTaxi`다. D: 드라이브가 아닌 폴더에서도 상대 경로로 사용할 수 있다.
3. Git의 지도 ZIP을 풀어 아래 **동일한 3개 생성 입력 파일**을 같은 상대 경로에 준비한다. JSON 단독 파일은 Git에서 제외하지만 ZIP은 Git에 포함한다. `config/maps/local-input-manifest.json`에 크기와 SHA256을 관리한다.
   - `unity/YeongtongTaxi/Assets/Resources/drive-map.json`
   - `unity/YeongtongTaxi/Assets/Resources/eungye-map.json`
   - `unity/YeongtongTaxi/Assets/Resources/route42-map.json`
4. PowerShell 7(`pwsh`)을 사용하는 저장소 루트에서 `pwsh -File tools/check_workspace.ps1 -Strict`를 실행한다. 누락·해시 불일치가 있으면 일치하는 입력을 준비한다. 은계 통합 지도는 빌드가 다시 생성한다.
5. Unity의 **Taxi > Build Eungye Hybrid**로 빌드한다. 명령행은 Unity.exe에 `-batchmode -nographics -quit -projectPath <저장소>/unity/YeongtongTaxi -executeMethod PrototypeBuild.BuildHybrid -logFile <저장소>/artifacts/build.log`를 전달한다. Unity.exe의 설치 경로는 PC마다 다르다.
6. `builds/EungyeTaxi/EungyeTaxi.exe`로 기본 은계 영업을 실행한다. Unity Library/Temp는 복사할 필요가 없다.

지도 자체를 재수입·보정하려면 위 생성 입력 외에도 기존 PC의 `data/raw/ngii/`, OSM PBF 등 필요한 원본을 별도로 준비한다. 원본 도입 명령은 저장소 README와 `docs/ngii-map-import.md`를 따른다. 지도 형상을 변경하면 OSM 대조 해시도 재검증해야 한다.

## 무엇이 동기화되는가

| 대상 | 이동 방법 |
|---|---|
| 코드·기획·config·작업 이력 | Git 커밋/푸시·다른 PC에서 pull |
| 생성 입력 지도 | Git의 `distribution/maps/` ZIP 압축 해제 후 manifest 검증 |
| 원본 NGII/OSM 자료 | 필요할 때 별도 복사 |
| 실행파일 | Git의 `builds/EungyeTaxi` 폴더 전체로 바로 실행. 코드 수정 후에는 새 빌드 필요 |
| 테스트 로그·화면 캡처 | `artifacts/` 별도 보관. 검증 요약은 이력 문서에 커밋 |
| 개인 영업 저장 | Git에 포함되지 않음. 작업 코드 동기화와 게임 진행 저장 동기화는 별개 |

## 매 작업 기록 항목

요청/설계 → 실제 구현 파일 → 검증 명령과 결과 → 검증하지 않은 범위 → 실행법 → 다음 작업/필수 입력 → Git 기준 순으로 남긴다. 완료되지 않은 작업은 체크 표시하지 않는다. 현재 상태만 볼 때는 LATEST, 이전 결정을 확인할 때는 날짜별 기록과 Git 이력을 함께 본다.
