# 은계 싱글의 첫 영업과 돈을 벌 목표

## 요청과 결과

처음 영업하는 흐름, 현재 시설 위치, 지도 차량 판매점, 사고 외 주기 정비, 소지금 장부 메뉴, 기사 성장을 기획하고 전체 기획을 정리했다. 사용자는 계속 돈을 벌고 택시를 운전하고 싶어지는 게임적 목표를 우선했다.

현재 배포본은 **은계 r50 · 자체 도시 PvP A2 / LAN v23**이다. 현행 첫 출차·차고 거래·시설과 새 기획을 대조했다. 제안의 첫 검증 단위는 아버지 차로 첫 운임을 받은 뒤 저축을 계속하거나 야경 엽서 운행을 열고, 다시 출차해 손님을 직접 태우는 경험이다. 이후의 엽서전·차량 구매 목표, 오일 교환의 비용 회수 예시와 기사 성장 보상을 연결했다. 게임 소스·지도 입력·배포 파일 변경은 없다. [기획 37](../content-design/37-driver-life-and-business.md)

## 변경 파일

| 종류 | 저장소 위치 |
|---|---|
| 새 기획·현 시설 조사 | `docs/content-design/37-driver-life-and-business.md`, `docs/content-design/eungye-facility-locations.md`, `docs/content-design/images/eungye-r50-facilities.png`, `docs/content-design/images/eungye-dealership-study.png` |
| 전체 탐색 | `docs/game-design.md`, `docs/content-design/README.md`, 같은 폴더의 `택시 시뮬레이션.mmst`와 `택시 시뮬레이션-목차.md` |
| 기획 간 대조 | `docs/content-design/`의 00·03·04·05·10·11·12·13·14·22·26·27·34. 3일 이후 경력, 아버지 차 보관·전액 구매, 현장 부품, 학생 재회와 과거 보상 판매안의 기준 시점을 정리했다. |
| 범위·인계 | `docs/CURRENT-STATE.md`, `docs/prototype-scope.md`, `docs/prototype-development-checklist.md`, 이 기록, `docs/work-history/LATEST.md`와 `docs/work-history/LATEST-archive-through-2026-11-20.md` |

## 실제 검증

- **지도·시설.** 정식 r50 입력의 24,943,692바이트가 배포 리소스에 들어 있는지 대조했다. M 지도 핀 구성과 시설별 실제 정차 칸으로 10곳을 표시했다. 서부·동부 공업사 배치는 실행 로그의 정비칸과 대조했다. 위치·데이터 소스와 판매점 후보지는 [은계 시설 안내](../content-design/eungye-facility-locations.md)에 기록했다.
- **배포본 위치 제어.** 기존 통합 Windows Player를 임시 경력으로 실행해 간식·커피 길안내·시설 이용과 저장 복원을 포함한 `REFRESHMENT_PLAYER_OK checks=36`, 종료 0을 확인했다. 이 검사는 지정한 장소에 차량을 놓고 시설을 이용하는 방식이다.
- **지도 입력·배포.** PowerShell 7에서 `pwsh -File tools/check_workspace.ps1 -Strict` 종료 0, 필수 입력 문제 0. `pwsh -File tools/update_distribution_manifest.ps1 -VerifyOnly` 종료 0, 145파일·150,793,717바이트 크기와 SHA256 일치.
- **기획 탐색.** `node tools/simplify_content_mindmap.cjs --check` 종료 0. 개요 38개 노드·최대 3단계·링크 61개, 이전 세부 기록 377개와 연결 문서 링크 698개 통과. 기존 개요 노드 37개의 ID·메모·기존 링크를 보존했다. 거래·정비 예시는 현금·미납 회계, 기본 연료 소모와 차종 계수에 대조했다.
- **문서 대조.** 변경 Markdown 24개의 로컬 파일 링크 1,255회에 누락 0건. 시설 표의 1~10번, 정산·저축 차액과 차종별 오일 비용 예시를 별도로 계산해 확인했다. 두 위치도는 직접 열어 표시·범례를 확인했다. 최신 색인과 보관본의 과거 항목은 이전 색인의 순서·본문과 일치했다.

## 재현과 남은 판단

1. 저장소 루트에서 위 두 PowerShell 검사와 마인드맵 검사를 실행한다. [기획 목록](../content-design/README.md)에서 **37 → 현재 시설 위치 안내**를 차례로 열고 현재 10개 핀과 신규 판매점 후보 권역을 확인한다.
2. 현재 게임에서 `builds/EungyeTaxi/EungyeTaxi.exe` → 은계 싱글 → 처음부터 하기 → 준비에서 목표 선택 → 준비로 돌아와 영업 시작 → 덕성프라자에서 M을 연다. **주요 시설**을 켜고 주유·식사·공업사 등을 선택한다. 실제 구매 장소는 정산 뒤 차고다.
3. 위치 제어를 다시 실행할 때는 저장소 루트에서 통합 실행 파일에 `-batchmode -nographics -facilityArtCheck -refreshmentCheck -shiftCaptureOutput artifacts/design-r50-facilities -logFile artifacts/design-r50-facilities/player.log`를 전달한다. 종료 뒤 로그에서 완료 표식을 확인한다.
4. 자동 물리 주행·수동 주행·전체 캠페인은 이번에 수행하지 않았다. 첫 운임 6,540원 사례는 [이전 자동 물리 주행](2026-11-19-first-fare-and-settlement.md)에서 인용했다. 10곳을 사람이 직접 운전해 방문하는 검사, 판매점 후보 진출입, 마인드맵 앱 클릭, 특별 운행 동기와 정비 가격의 3일 이상 수용도는 남았다. 설계 다음 단계는 기획 37의 첫날 장면을 초행자에게 시험하는 것이다.
