# 실행 폴더와 지도 ZIP Git 배포

## 요청과 실제 변경

새 PC에서 빌드 없이 테스트하도록 기존 검증된 `builds/EungyeTaxi/` 전체와 지도 ZIP을 main에 포함한다. 게임 코드·지도 형상은 수정하지 않았다.

- `.gitignore`: 은계 실행 폴더만 빌드 제외 규칙의 예외로 지정. 다른 빌드·원본 자료·artifacts는 계속 제외한다.
- `.gitattributes`: 실행 폴더 전체를 `-text -diff`로 관리해 플랫폼별 줄바꿈 변환 없이 원본 바이트를 배포한다. 생성된 런타임/외부 Mono 설정은 소스 텍스트 diff 대신 파일별 SHA256으로 검증한다. Git LFS는 사용하지 않는다.
- 실행 구성 125파일, 합계84,175,840바이트. exe·UnityPlayer·Mono·Data 전체 포함.
- `distribution/maps/`에 기존 지도 ZIP(857,580바이트)과 SHA256 파일을 복사. 기존 전달물의 내용과 해시를 유지한다.
- `distribution/EungyeTaxi-build-manifest.json`: 파일별 크기·SHA256, 원본 코드 `2dde7ea`, Unity2022.3.62f2·기본 은계 기록.
- `distribution/README.md`, PC 인계 문서, AGENTS, 체크리스트에 Git 배포·바로 실행·개발 시 지도 압축 해제를 반영했다. 과거 날짜별 기록은 당시 별도 전달 방식의 이력으로 보존한다.
- 기존 루트 README 수정과 라이선스 문서/폴더 변경은 이번 커밋에 포함하지 않는다.

## 검증과 근거

- 실행 폴더를 `artifacts/no-build-verification/EungyeTaxi`에 복사해 소스·지도 JSON이 없는 작업 폴더에서 실행. 명시적 `-eungye` 없이 `-lawZoneLabelsTest -hybridV2` 자동 검사: Player 종료0, `success=True`.
- 은계 기본 맵·7개 법규 구역·모든 경유 호출명·1280/1600 미니맵·선택 강조·도착 텍스트 비가림 확인. 근거 `artifacts/no-build-verification/artifacts/law-zone-labels-test.txt`, 같은 폴더 캡처, `player.log`.
- 배포125파일 모두 실제 검사한 사본과 원본 바이트 동일, manifest SHA256 생성.
- 배포 ZIP 전체 SHA256과 CRC 정상, 내부 지도3개 크기·SHA256이 저장소 입력 manifest와 일치.
- 커밋 대상에125파일이 모두 포함되고 Git blob이 원본 바이트와 일치하는지 별도 확인. 실행 폴더 외 빌드는 여전히 제외됨을 확인.
- 게임 기능을 바꾸지 않았으므로 Unity 재빌드는 하지 않았다. 새 PC 하드웨어 구동·사용자 수동 운전·3일 캠페인을 새로 검증한 결과가 아니다.

## 새 PC 실행·개발

main clone/pull → `builds/EungyeTaxi/EungyeTaxi.exe` 실행. Unity 설치·지도 ZIP 해제는 테스트에 필요 없다. GitHub 저장소 ZIP으로 받으면 전체 압축을 먼저 푼다. exe만 따로 복사하지 않는다.

개발할 때는 저장소 루트에서 `Expand-Archive -LiteralPath distribution/maps/Eungye-map-inputs-2026-10-20.zip -DestinationPath .` 후 PowerShell7의 `tools/check_workspace.ps1 -Strict`로 검사한다. 기존 수정 지도는 먼저 백업한다. 개인 저장과 원본 NGII/OSM은 Git 동기화 대상이 아니다.

이후 소스를 수정한 작업에서는 새 빌드와 배포 manifest도 갱신해 코드·실행파일의 기준을 일치시킨다. 이 기록의 커밋은 `git log -1 --oneline -- docs/work-history/2026-10-21-git-playable-distribution.md`로 확인한다.
