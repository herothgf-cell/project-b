# 2026-10-29 PvP 실행 폴더 Git 배포

## 요구와 범위

사용자는 PvP 실행 폴더 전체를 Git에 포함해 PvP 브랜치 pull 뒤 실행할 수 있도록 요청했다. 이전 질문의 뜻은 기존 싱글 시뮬레이션 실행 가능 여부였음을 명확히 했다.

- 싱글 builds/EungyeTaxi/EungyeTaxi.exe는 PvP 작업 트리에도 있으며 main의 내구도·수리 완료 배포와130파일112,193,174바이트 SHA256이 일치한다. PvP 실행 파일 안의 통합 모드 선택으로 오해하지 않도록 두 실행 경로를 문서화했다.
- PvP builds/PvpPrototype/PvpPrototype.exe 및139파일93,137,205바이트 전체를 배포한다. 가장 큰 파일31,100,840바이트. 기존 실제 검증된 공유 경쟁 빌드를 그대로 포함하고 재빌드하지 않았다. 빌드 소스는 실제 이력236108baaf4ab075171db33dc49440d298fa8111, 빌드 로그 artifacts/pvp/competition-release-build.log의 PVP_BUILD_OK/종료0을 확인했다.
- 기존 실행 중 PvP 프로세스와 미커밋 NetworkMotor/TrafficAuthority·NPC/주행/규칙 신규 파일은 수정/종료/커밋하지 않는다. 임시 검증용 복사본만 실행한다.

## 변경

.gitignore에 PvP 실행 폴더 예외, .gitattributes에 폴더 전체 바이너리/줄바꿈 보존 속성을 추가했다. AGENTS.md·PVP README/07·LATEST에 두 실행 경로와 새 배포 방식을 기록했다. 전용 tools/update_pvp_distribution_manifest.ps1과 distribution/PvpPrototype-build-manifest.json으로 필수 런타임·고지 파일 및 모든 파일 크기/SHA256을 검증한다.

## 실제 검사

- 현재 싱글 manifest -VerifyOnly 종료0:130파일112,193,174바이트. 싱글/지도/기존 Eungye manifest 변경 없음.
- PvP 폴더를 C:/git/project-b/artifacts 아래 새 고유 디렉터리로 전체 복사해 원본과139파일 SHA256 일치 확인. Unity 프로젝트/Library 밖의 복사본에서 -batchmode -nographics -pvpIsolationCheck 실행 종료0, PVP_RUNTIME_ISOLATION_OK. 새 고유 진단 파일 사용. 그래픽 장치 없는 셰이더 경고는 화면 검사 성공으로 해석하지 않는다.
- 다른 물리PC·LAN/방화벽·사람 조작감은 이번에 검사하지 않았다. 기존 검증 빌드의 배포 작업이며 진행 중 Task6 NPC/주행 구현 완료를 뜻하지 않는다.
- 추가 manifest·Git 인덱스·복원 및 게시 결과는 아래에 실제 명령 확인 후 기록한다.

## 다른 PC 사용

feature/pvp-prototype 체크아웃 후 pull한다. 싱글은 builds/EungyeTaxi/EungyeTaxi.exe, PvP는 builds/PvpPrototype/PvpPrototype.exe 실행. 두 경우 모두 실행 폴더 전체가 필요하다. PvP 배포 검증은 pwsh -File tools/update_pvp_distribution_manifest.ps1 -VerifyOnly, 싱글은 기존 update_distribution_manifest.ps1 -VerifyOnly를 사용한다.

## Git 배포 검증

- update_pvp_distribution_manifest.ps1 -SourceCommit236108baaf4ab075171db33dc49440d298fa8111 -ValidatedOn2026-10-29 및 -VerifyOnly 종료0:139파일93,137,205바이트 일치.
- git add 뒤 실제 인덱스 blob139개를 모두 읽어 manifest 크기/SHA256과 일치 확인. Git 추적 파일 집합도139개로 같고 관련 없는 staged 소스0개. .gitattributes의 -text로 다른 PC checkout의 줄바꿈 변환을 방지한다.
- 현재 소스 커밋9489e6ed51b8e85ad1e50c67463bae94b9f315ac에는 완료 싱글 내구도/수리 main 병합이 포함된다. 원격 PvP e75c461은 그 조상이므로 일반 fast-forward push를 사용한다.
- 기존 미커밋 NPC/주행/규칙 및 네트워크 소스와 기존 실행 중 앱을 보존했다. 그 변경은 이 배포 커밋에 포함하지 않는다. 게시 결과는 실제 원격 확인 뒤 추가한다.

## 실제 게시·싱글 HUD 동기화

배포 b8ad18acc990435b09122156b5738d6ee434310f를 feature/pvp-prototype에 일반 push했고 원격 동일 해시를 확인했다. 이후 main의 d631a57bcac57e7ed5d270cf5947dc0d5cab3339(소스0706cff)를 병합해 싱글 내구도 아이콘과 검증 실행 폴더130파일112,194,710바이트도 반영했다. LATEST 충돌은 양쪽 인계를 보존했다. 진행 중 NPC 소스·새 로컬 PvP 시험 빌드·프로젝트 설정은 stage하지 않고 보존한다. 게시 PvP139파일은 기존 검증 산출물 그대로이며 새 시험 빌드의 완료/배포를 의미하지 않는다.
