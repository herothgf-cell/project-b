# 2026-10-27 병합 브랜치 정리·미병합 가지 검토

## 요청과 보존 기준

main 이외 브랜치를 안전 검토 후 제거한다. 이어서 사용자는 미병합 가지의 내용을 보고 삭제 여부를 다시 결정하겠다고 했다. main의 기존 미푸시 제안서 커밋 e8df8d5, 사용자 마인드맵 변경과 기존 untracked는 그대로 보존한다. 이번 작업으로 main을 원격에 푸시하지 않는다.

## 삭제 결과

git fetch origin --prune 후 각 tip의 main 조상 여부와 고유 커밋 수를 확인했다. 아래 로컬8개는 main에 완전히 포함되어 고유 커밋0이며 git branch -d로 삭제했다. 강제 삭제하지 않았다.

| 삭제한 로컬 브랜치 | 삭제 직전 tip |
|---|---|
| codex/additional-uturns | bdc934cbde40eadb59f36b2d177d25a6502412f5 |
| codex/ambient-pedestrians | 2341730b13efc98b172fb9f00c09718072267c76 |
| codex/eungye-uturn-medians | 5a30bfb31596ce5297383b4ec55a419db8480268 |
| codex/handling-traffic-receipt | 6fb97e84a2c94634c242d4f27904799a590732f6 |
| codex/nav-tip-drowsy-feedback-20261027 | d462660d785d68c53df8faa2ea17510797e931b4 |
| codex/playtest-feedback-20261027 | 24c8ce38b73f4c9e2373efe3eb701d8cc219e345 |
| codex/proto-p11-p13 | a6f65cab34327d01326021b13d3b8df36a605261 |
| codex/satisfaction-main-integration | 817c5dcbdb4c9d6c5ffaf58b9616e49411ff1683 |

마지막 가지는 별도 worktree에서 체크아웃 중이었다. 전체 tracked/untracked 변경0과 tip 동일을 확인한 뒤 같은 커밋의 detached HEAD로 전환했다. 작업 폴더/파일/커밋은 삭제하지 않았다. 현재 별도 폴더는 C:/Users/DRI/.codex/worktrees/satisfaction-fare-recovery/project-b이며 이는 로컬 환경 경로다.

아래 원격5개는 origin/main에도 모두 포함됨을 확인하고 실제 서버 tip과 로컬 추적 tip 일치 검사 후 git push --atomic origin --delete로 삭제했다. 명령 종료0, 서버에서 남은 head는 main 하나다. 원격 HEAD 별칭은 브랜치가 아니므로 유지했다.

| 삭제한 원격 브랜치 | 삭제 전 tip |
|---|---|
| codex/eungye-only | 47b3a0483cad65c2ef419b216b4fbfb1b73fa863 |
| codex/project-map-editor | 3bcac767ab2c0450f9074c6c5c357c9fa8f1fc3b |
| codex/prototype-content | 1e9790ca06bd70e048f16f80280debdcb1ab06fa |
| codex/road-map-editor | 73d57a5c7dff7a07f555da2cf9def51164bbe95b |
| feature/assisted-route-driving | 2de9a539b0c611a5e13b8de9b5e65449c3ff8bec |

병합된 tip은 main에서 계속 접근할 수 있어 필요하면 git branch <이전 이름> <tip>으로 로컬 가지를 복원할 수 있다.

## 보존한 가지의 실제 내용

codex/satisfaction-fare-recovery는 main의 조상이 아니며 git cherry에서도 동일 패치로 표시되지 않는3커밋이 있다. 사용자의 판단 전 삭제/병합하지 않는다.

| 커밋 | 내용 | 현재 main과 차이 |
|---|---|---|
| 22d7688 | 승객 만족도 운임·시작 만족도 변주·안정 주행 회복의 초기 구현과 검사 | 운임/만족도는299e741에서 최신 main에 별도로 이식했고 이후 추가 개정. 회복 모델은 main의 기존 별도 구현을 유지했음 |
| d674321 | 당시 실행 파일·지도 리소스·배포 manifest | 현재 최신 배포가 아닌 옛 빌드 |
| 5f62bef | 당시 구현·검증·배포 인계 기록 | 옛 버전의 작업 이력 |

StableDriveRecovery.cs라는 파일이 main에 없다는 사실은 회복 기능 자체가 없다는 뜻이 아니다. 현재 main은 MannerLedger.cs의 StableDrivingProgress와 DrivingManners.ObserveStable로 구현한다. [당시 통합 결정](2026-10-26-satisfaction-main-integration.md)에 원래 가지는 보존하고 이미 검증된 main 회복 구현을 유지한다고 명시돼 있다.

| 기획 내용 | 보존 가지의 구버전 | 현재 main |
|---|---|---|
| 안정 주행 회복 기회 | 고유 이동500m 또는 이동60초 | 새 구간350m 또는 이동45초 |
| 주행 만족도 M 회복 | 급가감속 손실에 한해 회당+2·운송 최대+6 | 같은 회복 상한, 영구 감점 분리·현재 저장/관찰 체계와 통합 |
| 승객 안심 회복 | 해당 초기 모델에는 없음 | 같은 기회에 M 회복이 없고 S<R이면+2, 운송 최대+4·요구치 상한 |
| 요구 미달 운임 | S-R 차이1점당0.5% 감액·최대20% | 기한 내0<S<R이면 min(F,A)×S/R,0점/지각0원 |

판단: 새로운 미구현 기능이 통째로 남았다기보다는, 별도로 통합/개선한 기능의 초기 구현·옛 실행 파일·옛 이력을 담은 가지다. 전체를 다시 병합하면 현행 회복·운임·배포와 충돌하므로 권하지 않는다. 과거 구현 참고를 남길 가치가 있는지 사용자 결정이 필요하다.

## 검증과 미수행

- git branch -a: 로컬 main·codex/satisfaction-fare-recovery만 유지, 원격 origin/main 및 HEAD 별칭만 유지.
- git ls-remote --heads origin: main만 존재,60b15edd6094d9a6383de4d0c75c4a40d49eda40. 미푸시 제안서 커밋은 그대로 보존.
- git worktree list --porcelain: 현재 main, 별도 worktree는817c5dc detached·파일 보존.
- 사용자 .mmst SHA256 전후 동일7C4E5D0C86970D633040A4CF77B4D9511AF2C3B5AABE6A636BA3622E850B6666.
- 코드·지도·기획·빌드·manifest를 수정하지 않아 게임 테스트/재빌드는 수행하지 않았다. 입력 지도/ZIP과 기존 빌드 상태는 LATEST의 기존 검증 결과를 따른다.
- 자체 이력 커밋은 git log -1 --oneline -- docs/work-history/2026-10-27-branch-cleanup.md로 확인한다.
