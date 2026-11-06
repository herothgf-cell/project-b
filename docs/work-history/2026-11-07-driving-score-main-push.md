# 2026-11-07 운전 점수 설정 main 게시

사용자 요청: 완료 변경을 main에 푸시.

원격 main30d5587은 로컬 f056179보다11개 커밋 앞선 별도 분기였다. 원래 작업 폴더의 미커밋 날씨설정/빌드/자료를 보존하고 `.worktrees/push-driving-score`에서 병합했다. 최신 HUD/방 설정/승객 소스와 비 기능/점수 설정 소스를 모두 유지한다.

충돌 처리: LATEST의 양쪽 주제별 기록 전부 보존. FirstDrive 장면의 양쪽 차이는 생성 fileID뿐이고 같은 Mode Selection/bootstrap/은계 설정임을 확인한 후 빌더가 재생성했다. 실행 바이너리는 어느 한쪽을 최종 선택하지 않고 병합 소스의 BuildHybrid로 전부 재빌드했다. manifest도 새 실행본으로 재생성했다. 최초 일괄 ours 선택 요청은 자동 승인 검토에서 원격 유실 위험으로 거부됐으며 실행되지 않았다. 이후 개별 대조 근거를 제시한 보존 방식으로 진행했다.

검증: Strict issues0; C# 설정781·사고77·Node90 통과. BuildHybrid 로그 `artifacts/driving-score-merge-build.log`의 PROTOTYPE_BUILD_OK 및 종료0. 실제 rain Player20 통과(20km/h 정지거리 맑음2.552734m/비2.833984m). 통합/PVP manifest142파일132,290,979바이트 검증 통과.

HUD Player는 `-brightUiCheck`로 시도했으나 모드메뉴/준비/주행/지도 등 capture rendered frame 검사 실패가 관찰됐다. 화면 검증 완료로 표시하지 않는다. 원인 확정/사람 플레이·전체3일·다른PC/LAN 신규 검사는 후속이다. 원본 폴더의 개인 저장은 수정하지 않았고 Player는 테스트 전용 저장을 사용했다.

현재 은계/A2·LANv14·BuildHybrid. 원격 main에는 병합 커밋과 병합 실행본을 게시한다. 원래 작업 폴더의 local main은 미커밋 작업 보호를 위해 강제 이동하지 않는다. 후속 작업은 원격 병합본을 기준으로 현재 미커밋 작업을 통합해야 한다. 지도/ZIP 변경 없음.
