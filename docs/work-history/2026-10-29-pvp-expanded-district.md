# 2026-10-29 확장 PVP 시험장·은계형3D/UI·거점/시설

## 요청과 실제 범위

사용자가 현재 PVP 시험장이 짧다고 확인했다. 은계 실제 지도가 아니라 기존 자체 PVP 맵을 확장하고 은계형 화면/UI에서 PVP 불필요 항목을 제외, 지도/법규 데이터화→승객 거점→서비스 시설까지 일괄 구현 후 마지막에 테스트하도록 지시했다. 이 순서를 우선해 구현 중 Unity/Player 검사를 실행하지 않았고 모든 기능 연결 후 통합 검사를 시작했다. [계획22](../content-design/pvp/22-expanded-district-plan.md), [요소별 표/사용법23](../content-design/pvp/23-expanded-district.md).

확장 범위520×440m/외곽1,760m, 승객8거점/주유2시설, NPC6/보행4. 신호/정지·진입선/우회전/동일 신호 합류/제한속도/NPC/보행/서비스·공급을 지도 복제/검증/해시에 포함했다. 외곽/도심을 함께 공급하고 거리 운임은 기존 규칙을 유지했다. 상세 차량·도시·카메라/지도와 한글 PVP HUD를 연결했다. 캠페인 날짜/생활/장기 저장은 연결하지 않았다.

주유는5초/2점·100연료/5km·고갈 제자리 지원8초/6점의 후보를 구현했다. 실제 수락 이동으로 소비, 같은 서버 정차/명령, 봇도 같은 이용, 시설1자리, 서비스 중 본인만 정지, 시계 계속 진행, 완료 시1회 비용, 취소/끊김/미완료 무청구, 마감 전 완료 불가 시작 거절이다. 위반과 서비스 비용을 구분해 안전 보너스를 잘못 없애지 않는다. PVP 내구도/수리는 추가하지 않았고 기존 은계 구현을 보존했다.

## 변경 파일

- `unity/EungyeTaxi/Assets/Pvp/Runtime/Map/`: MapContent/ExpandedMap 신규, Definition/Runtime 데이터·검증·해시, Loader/DistrictScenery/구역 건물/시설/도로 표현.
- `Runtime/Driving/`: PvpTaxiVisual, 데이터 기반 DrivingPilot. `Runtime/PvpPracticeSession.cs`: 추적/운전석/전체 시점·차량·조명/자원 정리.
- `Runtime/UI/PvpRaceHud.cs`: PVP snapshot/명령만 사용하는 속도/시간/승객/점수/연료·서비스/미니/전체지도 UI.
- `Runtime/Traffic/`, `Runtime/Rules/`: 비균등 NPC폐회로/보행 경로·공유 신호 접근과 법규 판정. legacy 회로의 좌표 가드는 보존 회귀에만 적용.
- `Runtime/Competition/PvpServiceAuthority.cs`, TaxiBot, `Passengers/PassengerAuthority/Commands`, `Scoring/ScoreLedger`: 연료·서비스·순환 승객 공급/비용·봇.
- `Runtime/Network/Session/Motor/CompetitionBridge`, Bootstrap: 참가자별 운전 잠금·새 데이터/프로토콜v5·상세 차량/HUD·공유 상태/초기화·확장 기본 맵.
- `Runtime/PvpExpandedProbe.cs`, `Editor/PvpExpandedTests.cs`, `tools/test_pvp_expanded.ps1`: 실제 입력 운송/주유·단위/회귀·화면 캡처. 관련 meta/기획/이력 및 최종 실행 폴더/manifest.

Runtime 이하 축약 경로는 `unity/EungyeTaxi/Assets/Pvp/` 기준. 다른 채팅의 `C:/git/project-b` checkout/미커밋 작업/실행 앱은 수정하지 않았다. UI/3D 배경은 파일 소유권을 분리한 두 구현 에이전트, 전체 기능은 읽기 전용 리뷰로 교차 확인했다.

## 실제 검사와 발견/수정

| 검사 | 실제 결과 | 로컬 증거 `artifacts/pvp/` |
|---|---|---|
| 첫 전체 Editor | 기존 전체 회귀 통과 후 새0→4 경로 실패, 경계0인 채 분기 순환 | `expanded-integrated-1.log` |
| 분기 수정 | 기존 정차 bay와 거의 겹친 외곽 진출을 별도 모서리 게이트/정렬된 연결로로 변경 | 같은 새 검사 재실행 |
| 확장 모델 | Editor0,117관측/32입력 경로, 외곽4개 공유교통 주행·법규 감점/경계0, 거리133.188~1400.976m/순환1760m | `expanded-integrated-2.log` |
| 전체 회귀/Build | Editor0/Build0, 기존법규NPC25,628관측·봇 각7회 포함 | `expanded-full-model.log`, `expanded-build.log` |
| 실제2Player | 러너0,각1회199.541m/319.414m,33/44점·동일 결과/NPC144표본·fixture/경계0 | `expanded-network-regression.log` |
| 재접속6 | 러너0, 예상 유예/host-loss client1은 negative 결과로 구분 | `expanded-reconnect-regression.log` |
| 첫 실제시설통합 | Player1,첫 운송 후시설 경로 정지.875.995m/경계0/fixture0,비용0 | `expanded-player-merge-red.log` |
| 원인 재현/수정 | 모델에서 edge42/x240,z−8.209 정지 재현. 합류42가 정규20과 같은 신호를 공유하지 않아 보행자 앞에 진입한 것이 원인. 지도에 공통 접근로를 명시하고 조종/법규/NPC가 같은 선/이력을 사용 | `expanded-service-trip-red.log` |
| 수정 모델 | Editor0,연속 운송→시설→취소→재시도→완료,비용2점1회·연료 정상 | `expanded-service-trip-green.log` |
| 최종 전체 모델/Build | 각0,공통 접근 수정·화면 안내/주유 시간·지도명 보완 포함 | `expanded-final-model.log`, `expanded-final-build.log` |
| Core/Strict/싱글 보존 | 각0,8그룹/2프로세스 진단/입력 issues0,싱글130파일112,194,710바이트 | `expanded-core.log`, `expanded-strict.log`, manifest명령 |
| 독립 리뷰 | 고갈 임계값 서버.001/UI0 불일치1건 발견→공유상수로 수정,합류 신호 재리뷰 중대 발견 없음 | 전용 읽기 전용 리뷰 |

화면은 batch/숨김 PNG가 검어 합격으로 쓰지 않았다. 사용자가 허용한 마지막 Unity 화면 확인 범위에서 자체 검사 Player를 짧게 표시해 게임 프레임만 자동 캡처/종료했고 마우스·키 입력이나 다른 앱 조작은 하지 않았다.1280×720 주행/전체지도 실제 이미지를 확인했다(`expanded-window.png`, `expanded-window-map.png`). 지도 이름 겹침/주유 시간/시작 안내와 어두운 면 조명을 보완했다. 캡처 검사는 단순 파일 존재 대신 실제 색상 다양성도 검사하도록 변경했다. 최종 화면과 수정 후 실제 Player는 아래 추가 결과로 기록한다.

## 재현·남은 항목

로컬 Unity는 `C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe`. `-batchmode -nographics -quit -projectPath <저장소>/unity/EungyeTaxi -executeMethod Pvp.Tests.EditMode.PvpExpandedTests.RunAll -logFile <절대 경로>` 후 같은 방식의 `Pvp.Editor.PvpBuild.BuildClient`를 실행했다. Start-Process/WaitForExit로 종료코드를 확인했다.

```powershell
pwsh -File tools/test_pvp_expanded.ps1
pwsh -File tools/test_pvp_expanded.ps1 -Executable <복사본 exe 절대 경로>
pwsh -File tools/test_pvp_driving.ps1 -Case baseline -Port 18801
pwsh -File tools/test_pvp_reconnect.ps1 -Port 18901
pwsh -File tools/test_pvp.ps1
pwsh -File tools/check_workspace.ps1 -Strict
pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly
pwsh -File tools/update_pvp_distribution_manifest.ps1 -VerifyOnly
```

모델 서비스의 위치/관측 제어 검사, 실제 평면 모터 입력 운송, 자동 프레임 캡처, 사람 플레이를 구분한다. 사람 키보드/패드/장기 시야·수익/서비스 경제·봇 전략·다른PC/인터넷/동시 시설 이용은 별도 수용이다. 유형별 승객 효과/PVP 손상·수리·복잡한 충돌 책임 전체는 후속이다. 전체P2/공개랭킹 수용으로 표시하지 않는다.

수정 후 실제 시설 통합과 확정 소스·최종 패키지/원격 게시 결과는 확인 후 아래에 추가한다. 소스 확정 후 실제 커밋에서 재빌드하고 전체 실행 폴더·복사본·manifest/Git index 해시를 검증한다.
