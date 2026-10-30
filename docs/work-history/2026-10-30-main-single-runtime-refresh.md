# main 기본 싱글 실행 폴더 갱신

- 대상: `builds/EungyeTaxi/`, 기본 은계 싱글. `builds/PvpPrototype/`와 진행 중인 PvP 소스는 변경하지 않았다.
- main 소스 `7b38ac0`, 배포 `980814d`의 검증된 파일과 `distribution/EungyeTaxi-build-manifest.json`을 복사했다. main 수정에는 복원 거부 출발의 명시적 복구, 실제 곡선 점유 검색, 접근별 3색/4색 신호등과 좌회전 불가 HUD가 포함된다.
- 사용자 게임 종료 후 교체. 전수 해시 `DISTRIBUTION_OK files=130 bytes=112309087`, 종료0.
- 이 폴더의 실제 exe를 `-campaignStartupTest -hybridV2`로 정상 창에서 실행했고 격리 저장의 신규 준비/1회 출발10검사 종료0. `artifacts/main-single-refresh-startup.log`와 `campaign-startup-test.txt` 참조. 사용자 AppData에 쓰지 않았다.
- 기본 런타임은 manifest에 기록한 main 소스로 빌드되었다. 진행 중인 별도 PvP 개발 소스에 main 소스를 강제로 병합하지 않았다. 해당 소스에서 기본 싱글을 재빌드하기 전 main 수정 통합이 필요하다.
- 현 feature 브랜치에는 별도 미게시 PvP 커밋과 진행 중인 전용 빌드 변경이 있으므로 이 작업에서 feature 원격으로 함께 밀어 올리지 않는다. 기본 main 게시와 로컬 실행 폴더 교체는 완료했다. PvP 전용 빌드 게시 작업에서 이 배포 커밋을 함께 인계한다.
