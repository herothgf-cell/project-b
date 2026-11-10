# 2026-11-10 차량 구매·차고·PvP 내 차 화면

[기획22](../../content-design/22-growth-direction-and-vehicle-purchase.md) §11 구현의 실제 실행본 캡처다. `pwsh -File tools/test_vehicle_garage_player.ps1 -Visible`(위치 제어 검사, 1600×900 창)이 저장한 화면이며 사람이 직접 플레이한 화면이 아니다. 금액은 검사 고정값(1일 운임 4,000원, 2일 15,000원)에 따른다.

| 파일 | 장면 |
|---|---|
| `01-garage-day1.png` | 1일차 정산 후 차고 탭. 현금이 하이브리드 최종 지출(16,000원)보다 적어 구매 불가 |
| `02-garage-quote-day2.png` | 2일차 정산 후 넓은 LPG 택시 견적(새 차 22,000 − 보상 판매 10,000 = 12,000원) |
| `03-garage-after-purchase.png` | LPG 구매 + 두 번째 색(남색)·대시보드 화분 구매 후 |
| `04-day3-driving-new-car.png` | 3일차 출발 화면. 새 차(LPG 남색) 외형과 내구도·청결 100 |
| `05-final-garage.png` | 3일차 정산 후 마무리한 차고(읽기 전용) |
| `05-reloaded-final-garage.png` | 새 프로세스에서 이어하기로 복원한 완료 차고 |
| `06-pvp-my-car-lobby.png` | PvP 입장 화면 "내 차"(싱글에서 산 LPG·남색·화분 선택, 하이브리드는 잠김) |
| `07-pvp-match-my-car.png` | 봇 3대 PvP 경기에서 내 차 외형(성능은 표준) |

지붕 TAXI 표시등도 차체 도색을 따라 바뀌는 점은 현재 한계다. 외관 회전·운전석 미리보기는 측면 외형 그림으로 대체했다.
