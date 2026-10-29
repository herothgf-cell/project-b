# PVP 네트워크·빌드·기존 코드 분리

상태: P1 화면·자동 검증 및 NGO 직접 IP 실험 추가 / 원격PC·사람 조작감·P2 전체 수용 미완료
작성일: 2026-10-27

이 문서는 별도 PVP 실행 경로와 네트워크 권한의 개발 설계다. 2026-10-27의 기획 커밋 이후, 2026-10-29에 전용 시작 씬·설정·저장·빌드 코드와 분리 검사를 추가했다. 실제 Unity 빌드와 네트워크는 검증 전이며, 기존 캠페인 소스·지도·패키지·배포 산출물은 변경하지 않았다. 실제 수행 범위는 [P1 작업 이력](../../work-history/2026-10-29-pvp-startup-isolation.md)을 따른다.
기준은 main `60b15edd6094d9a6383de4d0c75c4a40d49eda40`, 작업 브랜치는 `feature/pvp-prototype`, 초기 프로젝트는 기존 `unity/EungyeTaxi`, Unity는 `2022.3.62f2`다.
경기 정책은 [01 경기 흐름](01-match-flow-and-rules.md), 규칙은 [02 주행·교통](02-driving-and-traffic.md), 수치·결정 상태는 [09](09-decisions-and-balance.md), 검증은 [10](10-playtest-and-acceptance.md)을 따른다.

## 1. 기존 코드에서 확인한 분리 필요성

| 확인한 코드 | 현재 동작 | PVP 개발에 주는 제약 |
|---|---|---|
| [PrototypeBuild.cs](../../../unity/EungyeTaxi/Assets/Editor/PrototypeBuild.cs) | `BuildHybrid`가 은계 자료를 준비하고 `BuildAt`이 `FirstDrive.unity`·`DriveSession`·Windows64 실행물을 생성 | 기존 빌드 함수에 모드 플래그만 추가하는 방식을 초기 전제로 삼지 않음 |
| [DriveSession.cs](../../../unity/EungyeTaxi/Assets/Scripts/DriveSession.cs) | 로컬 택시 한 대와 카메라·교통·승객·영업·법규·UI를 연결하고 `Time.timeScale`로 정지 | 다중 차량, 서버 진행, 로컬 메뉴와 경기 진행을 구분하는 시작 경로 필요 |
| [MapCatalog.cs](../../../unity/EungyeTaxi/Assets/Scripts/MapCatalog.cs) | `eungye`만 선택하며 은계 보정과 리소스 로딩 수행 | 자체 경기장은 전용 지도 정의·로더로 읽고 은계 선택·보정 경로를 우회 |
| [CampaignSaveLocation.cs](../../../unity/EungyeTaxi/Assets/Scripts/CampaignSaveLocation.cs) | 은계 제품 폴더를 고정하고 이전 제품 저장을 호환 복사 | exe/productName 변경만으로 저장 분리 불가. PVP는 이 함수를 호출하지 않음 |

현재 프로토타입 범위와 검증 수준은 [기존 범위](../../prototype-scope.md) 및 [최신 인계](../../work-history/LATEST.md)에 있다. 기존 자동 검사 통과를 PVP 동기화·사람 플레이의 증거로 사용하지 않는다.
전면 공통화나 엔진 교체를 선행 작업으로 두지 않는다. 시각 자산·순수 계산처럼 결합이 작은 부분부터 재사용하고, 캠페인 상태에 의존하는 모듈은 실제 호출 관계를 확인한 뒤 어댑터 또는 작은 분리를 선택한다.

## 2. 네트워크 후보와 채택 게이트

**권고 후보는 Photon Fusion 2의 Host 모드와 초대방이다. 사용자 최종 선택이나 기술 검증 완료가 아니다.** 차량 입력 예측·보정이 성립하는지 P2에서 확인한 뒤 채택 여부와 SDK 버전을 [09](09-decisions-and-balance.md)에 기록한다.

| 후보 | 이 프로젝트에서의 용도 | 채택 전에 확인할 것 |
|---|---|---|
| Fusion 2 Host | 초기에 한 참가자의 PC가 상태 권한을 맡고 다른 클라이언트가 접속 | 차량 예측·재계산, 방장 부하·지연 우위, 연결 실패·방장 종료 처리 |
| Unity Netcode for GameObjects + 필요 시 Relay | Fusion 검증 실패 또는 의존성·운영 조건 불일치 때 비교할 대안 | 사용 가능한 패키지 버전, 차량 예측 직접 구현 범위, 동일 조건의 조작감·운영 부담 |
| Fusion Dedicated Server | 검증된 플레이를 공개 경쟁·랭킹으로 확장하는 후속 후보 | Linux 서버 실행, 별도 호스팅, 결과 신뢰 경계·운영 복구 |
| Fusion Shared | 이번 권고안에 포함하지 않음 | 참가자가 게임 상태 권한을 나누는 구조와 공용 승객·정산의 신뢰 요구를 별도 재설계해야 함 |

Fusion의 Host/Server는 입력 권한과 상태 권한을 구분하고 입력 이력을 예측·재계산에 사용한다. 따라서 빠른 차량 응답을 검증할 후보로 추천한다. 이는 현재 차량 코드가 그대로 호환된다는 보장이 아니다. [공식 입력 문서](https://doc.photonengine.com/fusion/v2/manual/input/player-input)
NGO의 client anticipation은 완전한 rollback/replay 예측 루프를 제공하지 않는다. 대안 평가에서는 보간만 적용한 화면과 입력 예측을 갖춘 차량의 개발 비용을 구분한다. 인용한 NGO 문서 버전이 현재 Unity 프로젝트에 바로 설치 가능하다고 가정하지 않는다. [공식 anticipation 문서](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/manual/advanced-topics/client-anticipation.html)
Fusion 요구사항에는 Unity `2022.3.45` 계열과 Force Text 직렬화가 명시된다. 현재 `2022.3.62f2`에서 실제 임포트·컴파일·Player 빌드를 확인하고 의존성을 고정한다. 이번 문서 작업에서는 SDK를 설치하지 않는다. [공식 SDK 요구사항](https://doc.photonengine.com/fusion/v2/getting-started/sdk-download)

## 3. 초기 분리 경계와 제안 경로

아래 표는 단계 전체의 분리 경계다. **시작·설정·저장·전용 빌드의 기반 소스는 추가했고, 자체 지도·입력/차량·네트워크는 후속 제안**이다. 기반 파일이 존재한다는 사실은 Unity 실행 검증 완료를 뜻하지 않는다. 저장소 상대 경로이고, 상세 생성 순서와 실제 완료 상태는 [08 개발 로드맵](08-development-roadmap.md)에 둔다.

| 책임 | 신규 제안 경로·타입 | 경계 |
|---|---|---|
| 클라이언트 시작 | `unity/EungyeTaxi/Assets/Pvp/Scenes/PvpBootstrap.unity`, `Runtime/PvpBootstrap.cs` | `DriveSession`을 생성하지 않고 설정·지도·PVP 세션을 연결 |
| 경기 설정 | `unity/EungyeTaxi/Assets/Pvp/Runtime/Core/PvpSessionConfig.cs` | 저작용 설정과 경기 시작 시 동결한 `PvpMatchSettings` 구분 |
| 자체 지도 | `unity/EungyeTaxi/Assets/Pvp/Runtime/Map/PvpMapDefinition.cs`, `Runtime/Map/PvpMapLoader.cs` | [03](03-map-and-content.md)의 자체 경기장만 읽고 지도 ID·내용 해시 확인 |
| 입력·차량 | `unity/EungyeTaxi/Assets/Pvp/Runtime/Driving/PvpVehicleInput.cs`, `Runtime/Driving/PvpVehicleMotor.cs` | 입력 수집, tick 주행, 표시를 분리하고 캠페인 점수·저장을 호출하지 않음 |
| 네트워크 경계 | `unity/EungyeTaxi/Assets/Pvp/Runtime/Network/PvpFusionSession.cs`, `Runtime/Network/PvpNetworkVehicle.cs` | SDK 시작·입력 전달·스냅샷 전달을 담당. 첫 단계부터 범용 네트워크 프레임워크를 만들지 않음 |
| 저장·로그 | `unity/EungyeTaxi/Assets/Pvp/Runtime/Persistence/PvpLocalStore.cs` | 사용자 설정·진단 기록만 전용 폴더에 기록. 캠페인 저장을 읽거나 이관하지 않음 |
| 전용 빌드 | `unity/EungyeTaxi/Assets/Pvp/Editor/PvpBuild.cs` | 명시한 PVP 씬으로 독립 빌드. 기존 `BuildHybrid`의 출력·씬을 덮어쓰지 않음 |

표에서 `Runtime/...`로 이어지는 경로의 공통 접두사는 `unity/EungyeTaxi/Assets/Pvp/`다. 차량 모델·도로 메시 등은 재사용 가능성을 조사하되 기존 `TaxiController` 전체의 네트워크 적합성은 아직 확인하지 않았다.
독립 프로젝트로 복제하면 수정·자산 동기화 부담이 생기므로 초기에는 같은 저장소·Unity 프로젝트를 유지한다. 독립 프로젝트 전환은 패키지 충돌이나 빌드 격리가 실제 장애가 될 때 다시 판단한다.

## 4. 상태별 권한

이하 **권한 서버**는 초기 Host의 상태 권한 측 또는 후속 Dedicated Server를 뜻한다. Host가 Photon Cloud의 전용 게임 서버라는 뜻은 아니다.

| 상태 | 최종 기록·판정 권한 | 클라이언트 역할 | 동기화·예외 |
|---|---|---|---|
| 참가자·준비·세션 상태 | 권한 서버 | 참가·준비 요청, 승인 상태 표시 | 소켓 연결과 경기 참가 승인을 구분 |
| 경기 단계·공통 시각·마감 | 권한 서버 | 서버 기준 시각으로 잔여 시간 표시 | 로컬 시계·프레임률·메뉴로 단계를 진행시키지 않음 |
| 동결 설정·맵·버전 | 권한 서버가 호환성 확인 후 확정 | 로드한 콘텐츠와 해시 대조 | 다르면 준비 완료 불가. 경기 도중 임의 변경 불가 |
| 자기 차량의 입력 | 해당 플레이어가 입력 생성, 서버가 검증 | 가속·제동·조향·버튼을 tick 입력으로 전송 | 클라이언트가 최종 위치·운임·점수를 제출하지 않음 |
| 차량 위치·속도·물리 판정 | 권한 서버 | 자기 차 예측, 다른 차 보간, 서버 상태로 보정 | 충돌 정책은 [02](02-driving-and-traffic.md), 허용 입력 범위는 설정으로 검증 |
| NPC 차량·보행자 | 권한 서버 | 상태를 받아 표시 | 각 클라이언트가 독립 AI로 통행 여부를 확정하지 않음 |
| 신호·규제 활성 상태 | 권한 서버 | 공통 상태와 다음 전환 tick 표시 | 시드만 공유하는 독립 타이머 대신 기준 tick·위상 상태 공유 |
| 승객 공급·소유권·운송 상태 | 권한 서버 | 승차·하차 요청, 승인·거절 표시 | [04](04-passengers-and-fares.md)의 상태 전이를 원자적으로 처리 |
| 약정 기본운임·정산 원장 | 권한 서버 | 확정된 원장 항목과 합계 표시 | 클라이언트의 주행거리·캠페인 수익으로 확정하지 않음 |
| 위반 사건·운송 안전 평가 | 권한 서버 | 사전 안내와 확정 통지를 구분 | 빈차 사건 포함, 같은 사건의 재전송·재계산은 중복 차감 금지 |
| 결과·승자 자격·순위 | 권한 서버 | 결과 revision을 수신·표시 | 확정 시점은 [01](01-match-flow-and-rules.md), 계산은 [05](05-scoring-and-results.md) |
| 카메라·음량·메뉴·조작 안내 | 각 로컬 클라이언트 | 즉시 반응 | 로컬 메뉴가 공통 시뮬레이션이나 서버 시계를 정지시키지 않음 |

신호와 NPC는 차량 판정과 같은 권한 tick을 사용한다. 서버의 보행자 통행 상태와 클라이언트 화면이 어긋난 경우를 기록하고, 시각 보간 값을 법규 판정의 근거로 역수입하지 않는다.
무작위 공급·교통은 서버가 시드와 진행 상태를 소유한다. 동일 시드만 있으면 서로 다른 PC의 물리·AI 결과가 같아진다고 가정하지 않는다.

## 5. 승객 선점과 요청의 중복 방지

제안 공통 명령은 `MatchId`, `RequestId`, 대상 ID, 기대 상태 revision을 포함한다. 서버는 접속에서 확인한 플레이어 ID와 수신 tick을 별도 `PvpCommandContext`에 넣는다. 클라이언트가 보낸 신원·시각은 확정 근거로 사용하지 않는다.

| 조건 | 서버 행동 | 응답·예외 |
|---|---|---|
| 같은 승객에게 요청이 겹침 | 현재 승객 상태와 [04](04-passengers-and-fares.md)의 선점 조건을 한 처리 경로에서 검증 | 한 번만 소유권 전이. 나머지는 거절 사유와 현재 revision 반환 |
| 같은 요청 재전송 | `(MatchId, PlayerId, RequestId)`의 기존 처리 결과 조회 | 새 운송을 만들지 않고 같은 결과 반환 |
| 승인 전 UI가 탑승을 예상 | 권한 상태는 계속 미승인으로 취급 | 예상 표시를 철회할 수 있게 하고 운임·목적지 확정은 승인 뒤 표시 |
| 오래된 상태·다른 경기 요청 | 경기 ID·대상 revision·참가 상태 확인 | 거절 후 새 스냅샷 요청. 종료된 승객 ID를 다른 승객에 재사용하지 않음 |
| 승차·하차 경계와 단계 변경이 겹침 | 서버 tick 순서로 [01](01-match-flow-and-rules.md)의 경계 정책 적용 | 클라이언트 수신 순서에 따라 결과가 달라지지 않음 |

동일 tick 경쟁의 세부 우선순위는 [04](04-passengers-and-fares.md)가 소유한다. 지연 보상 도입 여부는 [09](09-decisions-and-balance.md)에서 결정하며, 기술 선택만으로 특정 참가자의 우선권을 약속하지 않는다.
처리한 요청 ID와 응답은 재접속 가능한 경기 상태와 함께 유지한다. 중복 방지 기록의 보존 범위는 유효한 재시도·재접속 구간보다 짧아서는 안 된다.

## 6. 예측·rollback과 확정 정산의 분리

Fusion의 입력 루프는 같은 tick을 다시 계산할 수 있다. 따라서 주행 계산 콜백에서 현금 증가, 원장 파일 쓰기, 결과 확정, 알림음 재생을 직접 수행하지 않는 구조를 제안한다. [공식 입력·재계산 설명](https://doc.photonengine.com/fusion/v2/manual/input/player-input)

1. 입력을 수집하고 자기 차량을 예측한다. 카메라와 엔진 소리는 표시 계층에서 반응한다.
2. 권한 서버가 입력 범위·차량·공용 월드 상태를 검증하며 해당 tick의 게임 상태를 진행한다.
3. 승차·운송 완료·위반 후보는 안정적인 사건 ID로 만들고, 재계산 가능한 상태 안에서 처리 여부를 보존한다.
4. 권한이 확정한 사건만 원장에 반영한다. 원장 키는 경기·운송 또는 위반 사건·항목 종류를 식별하며 [05](05-scoring-and-results.md)의 중복 방지 정의를 따른다.
5. 외부 로그·파일·결과 전송은 확정 원장 sequence를 기준으로 한 번 적용한다. 재실행한 시뮬레이션이 새 ID를 생성하여 같은 사건을 새 거래로 만들지 못하게 한다.
6. 클라이언트는 확정 원장과 revision으로 HUD·결과를 갱신한다. 동일 확정 사건의 재수신은 금액·감점·팝업·알림음을 다시 누적하지 않는다.

`HasStateAuthority` 여부 확인만으로 중복 지급이 해결됐다고 간주하지 않는다. 처리 사건 집합·원장 sequence와 스냅샷 복원 경계까지 검증한다.
예측된 위반 경고와 확정 감점은 별도 표시다. 보정으로 후보가 사라져도 이미 확정되지 않은 점수를 되돌려 가산하는 임시 거래를 만들지 않는다.
운송 종료 시점의 확정 항목은 이후 운전 점수로 재평가하지 않는다. 원장 산식·회계 규칙 자체는 [05](05-scoring-and-results.md)에만 둔다.

## 7. P2 차량 네트워크 기술 검증

P1의 자체 맵과 전용 차량을 그대로 가져와 실제 클라이언트 두 개로 검증한다. 한 Editor 안의 객체 두 대나 입력 재생만으로 완료 판정하지 않는다.

| 검증 범위 | 비교·관찰 | 다음 판단 |
|---|---|---|
| 입력→로컬 반응 | 오프라인 대비 가속·급조향·제동·방향 전환 감각 | 최고속도 변경만으로 반응성 문제가 해결됐다고 판정하지 않음 |
| 서버 보정 | 위치·속도 오차, 보정 빈도·크기, 카메라 튐, 장시간 누적 오차 | 필요하면 차량 모델 또는 물리 예측 연동 범위를 축소·조정 |
| 네트워크 조건 | 정상·지연·지터·손실·순서 뒤바뀜·일시 단절 | 조건 값은 [09](09-decisions-and-balance.md), 합격 게이트는 [10](10-playtest-and-acceptance.md) |
| 공용 물리 | 원격 차량·간단한 권한 NPC·신호를 보며 같은 장면 통과 | 플레이어 간 충돌 OFF 제안도 NPC/경계 충돌 동기화를 면제하지 않음 |
| 환경 | Host와 원격 Client 역할 교환, 독립 Player, 별도 PC·네트워크 | Host만 부드러운 결과와 실제 원격 참가자의 체감을 구분 |
| 성능 | 권한 tick 처리시간, CPU·메모리·송수신량, 재계산 부하 | 대상 기기·기록으로 판단하며 미측정 처리량을 약속하지 않음 |

지속적으로 목표 조작감에 미달하면 P3 콘텐츠 확대를 멈춘다. 입력/물리 모델 수정과 NGO 대안에 필요한 예측 구현을 같은 시험 조건으로 비교하고 근거를 [09](09-decisions-and-balance.md)에 남긴다.
P2는 예측 가능한 차량 상태, 물리 적분 위치, 재계산 시 복원할 필드, SDK와 Unity 물리의 실행 순서를 확인하는 단계다. `TaxiController`에 네트워크 컴포넌트를 붙이는 것만으로 성립한다고 가정하지 않는다.

## 8. 입장·재접속·호환성

입장 및 중도 이탈 허용 정책은 [01](01-match-flow-and-rules.md)에만 둔다. 네트워크 구현은 아래 정보를 갖춰 그 정책을 일관되게 집행한다.

| 상황 | 필요한 계약 | 처리 |
|---|---|---|
| 최초 연결 | 프로토콜 버전, 클라이언트 빌드 ID, 지도 ID·content hash, 규칙/설정 hash | 지원 조합을 확인한 뒤 경기 참가 승인. 불일치 사유와 필요한 빌드를 표시 |
| 준비 완료 | 모든 필수 콘텐츠 로드, 서버 설정 수신·대조 완료 | 누락된 경기 시간이나 읽지 못한 지도에 임의 기본값을 넣고 시작하지 않음 |
| 경기 중 새 연결 | 현재 단계·정책과 서버의 참가 판정 | 접속 성공을 즉시 주행 허용으로 취급하지 않음 |
| 재접속 | 세션에 귀속된 재접속 자격, 기존 참가자 ID, 현재 서버 스냅샷 | 로컬 저장의 차량·승객·점수를 서버에 덮어쓰지 않음 |
| 복원 스냅샷 | 단계·서버 tick·마감 기준, 동결 설정, 참가 상태, 차량, 승객/운송, NPC/신호, 원장 sequence·결과 revision | 스냅샷 적용을 마친 뒤 최신 명령만 수용. 이전 명령 재전송은 중복 방지 계약 적용 |
| 방장 연결 상실 | 권한 상태의 존재 여부·결과 확정 여부와 [01](01-match-flow-and-rules.md)의 종료 처리 | 검증되지 않은 Host migration으로 복구했다고 표시하지 않음. 이미 받은 확정 결과는 유지하고 미수신 결과를 추정하지 않음 |

`MatchDurationSeconds`는 경기 생성에 필요한 필수 설정이다. 클라이언트별 로컬 카운트다운 대신 서버의 시작 tick·마감 기준을 공유한다. 경기 시간·유예·접속 제한 값은 [09](09-decisions-and-balance.md)에서 관리한다.
경기 도중 맵이나 설정을 다시 편집하면 다음 경기용 revision으로 분리한다. 참가자가 서로 다른 규칙을 유지한 채 같은 경기에서 운행하게 하지 않는다.
재접속 자격과 서비스 비밀 값은 일반 로그·공개 저장소에 남기지 않는다. 초기 초대방에는 경기 식별과 재접속에 필요한 최소 식별만 두며, 계정·공개 매칭·영구 랭킹을 선행 필수로 삼지 않는다.

## 9. 빌드·저장·배포

| 항목 | 초기 제안 | 확인할 결과 |
|---|---|---|
| Windows 클라이언트 | `PvpBuild.BuildClient()` → `builds/PvpPrototype/PvpPrototype.exe` | 독립 씬과 전용 로더로 실행되며 은계 기본 실행 폴더를 덮어쓰지 않음 |
| 로컬 저장 | `PvpLocalStore.Resolve(string fileName)` → 전용 `PvpPrototype` 하위 폴더 | 입력 설정·진단 기록을 캠페인 저장과 구분. exe 이름에 의존하지 않음 |
| 지도 데이터 | `Assets/Pvp/Content/Maps/PvpArena01.asset`와 해당 콘텐츠 의존성 | 자체 제작 맵의 ID·revision·hash를 빌드 기록에 남김 |
| 빌드 스탬프 | 클라이언트/서버 빌드 ID, 프로토콜·지도·설정 hash, 사용 SDK 버전 | 접속 거절과 재현 로그가 같은 산출물을 가리킴 |
| Linux 전용 서버 | P5에서 `PvpBuild.BuildLinuxServer()` → `builds/PvpServer/PvpServer.x86_64` | 전용 서버 씬·서버 subtarget으로 카메라·로컬 입력 없이 실행 |

전용 빌드는 씬 목록을 `BuildPlayerOptions`에 명시하고, 불가피한 임시 `PlayerSettings`/빌드 설정 변경은 성공·실패 양쪽에서 복원한다. 빌드 뒤 기존 씬 목록·제품명·입력 설정에 의도치 않은 변경이 남는지 확인한다.
같은 Unity 프로젝트의 `Resources` 자산은 씬을 분리해도 빌드에 포함될 수 있다. 초기 목표는 실행·저장·판정 경계의 분리이며, 불필요 자산 제거는 빌드 의존성을 측정한 뒤 진행한다.
서버 빌드에는 해당 Unity 설치의 Linux Dedicated Server 지원 모듈이 필요하다. Unity 2022.3의 서버 빌드 방식은 [공식 안내](https://docs.unity3d.com/2022.3/Documentation/Manual/dedicated-server-build.html)를 따른다.

## 10. 운영 경계와 후속 전환

Host는 참가자의 PC가 권한 서버이므로 방장 조작·프로세스 종료를 신뢰 경계 밖의 문제로 해결할 수 없다. 초기 초대방 검증 결과를 공정한 공개 랭킹의 증거로 쓰지 않는다.
Fusion 전용 서버는 별도 Unity 서버 실행물이며 Photon이 그 게임 서버 컴퓨터를 호스팅해 주는 구조가 아니다. 별도 호스팅·프로세스 배치·지역·포트·모니터링·복구를 P5에서 설계한다. [공식 토폴로지 설명](https://doc.photonengine.com/fusion/v2/manual/network-topologies)
Dedicated 전환은 상태 권한 위치를 바꾸는 작업 외에도 결과 제출의 신뢰성, 서버 종료 시 미완료 경기 처리, 버전 교체, 부하 측정이 필요하다. 전환만으로 자동 부정행위 방지나 무중단 복구가 완성된다고 보지 않는다.
비용은 네트워크 서비스 요금과 별도 게임 서버 계산·전송·로그 비용을 구분하여 실제 사용량으로 추산한다. 고정 월요금·동시 경기 수·단일 서버 처리량은 현재 확정하지 않는다.
초기 기능 범위와 단계 진입 조건은 [08](08-development-roadmap.md), 최종 수락 여부는 [10](10-playtest-and-acceptance.md)에 기록한다. 공식 기술 자료 조회일은 2026-10-27이며 구현 시작 시 선택 버전의 문서를 다시 확인한다.


## 2026-10-29 구현 상태 갱신

본문의 초기 미구현 설명은 분기 당시 기록이다. 현재는 `Runtime/Network/PvpNetworkSession.cs`, `PvpNetworkMotor.cs`, `PvpNetworkProbe.cs`에 NGO 직접 IP 경로가 있다. Fusion 어댑터는 만들지 않았다. SDK와 런타임·자동 검사·한계는 [실제 이력](../../work-history/2026-10-29-pvp-direct-ip-network.md)을 따른다. 소유자는 서버의 고정 슬롯(host0/guest1)과 현재 연결ID·epoch의 대응으로 정하고 클라이언트가 위치/점수를 제출하지 않는다. 후속 재접속 자격은 같은 프로세스의 메모리256bit bearer ticket이며 공개 계정 인증이 아니다. 직접IP 암호화/인터넷 공개 경쟁용 보안 완료로 보지 않는다.

새 SDK 추가 후 이전 ScriptingAssemblies 목록이 남는 실패를 재현하여 PVP 빌드는 CleanBuildCache와 빌드 후 필수 네트워크 어셈블리 목록 검사로 보호한다. 기존 BuildHybrid와 캠페인 배포는 변경하지 않는다.

## 재접속 후속 (2026-10-29)

[계획](14-reconnect-content-plan.md) · [실제 검증](../../work-history/2026-10-29-pvp-reconnect-content.md). 호스트가 살아 있는 동안 자동/수동 복귀, 서버가 단절을 감지한 시점의 정지·숨김·상태 보존, 새 연결epoch 입력 검증을 구현했다. 유예30초는 시험값이며 검사에서3초로 축소할 수 있다. 120개 미확인 입력 또는3초 snapshot 침묵이면128개 이력 소진 전에 복귀를 시작한다. 서버가 이전 연결을 아직 감지하지 못한 일시 중복 거절은 기존 기한 안에서 재시도한다.

명시 나가기는 자격을 폐기한다. 퇴장/만료 후 다른 손님을 같은 슬롯에 새로 받으려면 호스트 세션을 다시 시작한다. 호스트 상실·앱 재시작 복귀·호스트 이전·공개 계정 인증은 미지원. 자격은 파일/로그에 저장하지 않는다. 원격 차량의 화면 보간은 서버 물리 상태와 별개다. 승객 선택은 현재 미리보기 상태만 복원하며 실제 운송 상태 복원을 뜻하지 않는다.

## 2026-10-29 공유 경기 후속

PvpCompetitionBridge가 권한 교통/경기/승객/점수 전체 집계와 최근16개 원장을 배포한다. 기존 입력 메시지와 경기 요청은 현재 연결epoch로 묶고, 경기 요청/응답에는 MatchId를 포함한다. 최대32KiB writer와 ReliableFragmentedSequenced를 사용한다. 전체 집계 snapshot으로 누락 순번 이후 상태를 대체하며 클라이언트 합계로 원장을 덮어쓰지 않는다. 같은 서버 프로세스의 재접속만 지원한다. [실행 계획](16-shared-world-competition-plan.md) · [수용 범위](17-competition-acceptance.md).
