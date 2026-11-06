# 작은 방 기능·실행본 메인 배포

요청: 구현한 방/조명·러그 구매 기능과 실행 파일을 메인에 푸시하고 다른PC에서 바로 실행하도록 정리한다.

원 작업 폴더의 미커밋 운전 설정·관리창 변경과 사용자 저장을 보존했다. artifacts/room-release에 8ab2dec 기준 별도 작업 트리를 만들고 origin/main933aa54를 병합했다. 공용 scope/checklist/LATEST 충돌은 최신 구현 상태를 유지했고 메인에만 있던 기획 색인은 통합했다. 통합 소스 기준84d1605이며 추정 해시가 아니라 git log 확인 결과다.

앞선 실행본은 미커밋 운전 설정이 섞였을 수 있어 배포 작업 트리에서 PrototypeBuild.BuildHybrid로 다시 만든다. 이번 게시본은 확정된 소스와 실행본을 일치시키는 것이 목적이다. distribution/README.md에 방 진입/가격/소유 유지·초기화/다른PC 실행/저장 별도 안내를 추가한다.

검증/실제 푸시 결과는 실행 후 아래 기록한다. 사람 전체3일·다른PC 하드웨어/LAN 검사를 로컬 자동 검사로 대신하지 않는다.

실제 검증: 배포 작업 트리에서 check_workspace.ps1 -Strict exit0/Input issues0(정식 지도 사용, 재수입 JSON은 선택 입력), test_personal_room.ps1 PASS17. PrototypeBuild.BuildHybrid exit0. tools/test_personal_room_player.ps1의 최초 실제 Player36검사 통과. manifest는 확정 통합 소스84d1605 기준으로 새로 생성했고 미커밋 운전 설정 포함 플래그를 false로 정정했다. 통합 VerifyOnly exit0, 출하141파일118,242,753바이트. 사용자 active-profile은 배포 작업 트리에 가져오지 않았다. 원 작업 폴더의 미커밋·저장·설정 파일을 수정하지 않았다.

배포에는 builds/EungyeTaxi 전체의 생성 변경과 distribution/EungyeTaxi-build-manifest.json, 안내/현재상태/이력이 포함된다. sourceCommit84d1605 이후에는 문서·빌드·manifest만 갱신한다. 실행: main을 clone/pull 또는 ZIP 전체 압축 해제→builds/EungyeTaxi/EungyeTaxi.exe→싱글→정상 영업 마감→내 공간. 소유물은 로컬 회차 저장이며 Git이 개인 저장을 동기화하지 않는다.

최종 재실행 검사 PERSONAL_ROOM_RELOAD_OK checks=7 / exit0. 통합/PVP alias VerifyOnly 모두 exit0. 문서 공백 검사 통과. 원격main에 fast-forward 게시하며 실제 게시 커밋은 git log와 git ls-remote로 확인한다.
