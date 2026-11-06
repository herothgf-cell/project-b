from pathlib import Path
import re
r=Path('C:/git/project-b')
s=(r/'artifacts/proposal-v4-20261106/build.mjs').read_text(encoding='utf-8')
s=s.replace('proposal-v4-20261106','proposal-v5-20261106').replace('project-b-game-proposal-v4.pptx','project-b-game-proposal-v5.pptx').replace('explicitTotalSlideCount:20','explicitTotalSlideCount:23').replace('게임 개발 착수 제안 · 4차','게임 개발 착수 제안 · 5차')
s=s.replace('생활은 개발 제안이며 동시 작업 중 미커밋 구현을 배포 완료로 간주하지 않음.','작은 방/첫 구매 핵심은 2026-11-06-personal-room-implementation.md의 구현 기록을 반영. 처음방 비교/전체 과거방 탐색은 후속. 미커밋 동시 변경은 배포 완료로 간주하지 않음.')
s=s.replace('첫 검증 제안','핵심 구현·체감 검증').replace('개인 소비는 신규 기획','개인 소비 핵심 구현 / 장기 차량·주거는 제안')
s=s.replace('개발 제안 · 방 1개, 조명·러그 각 2스타일 / 고정 위치 배치 / 구매·방 방문은 선택','목표 아트 시안 · 방·첫 구매 핵심 구현 / 조명·러그 각 2스타일 / 고정 위치 배치')
s=s.replace("['3일차  달라진 방 남기기'", "['3일차  달라진 방 남기기'")
s=s.replace('기획 목업 · 금액은 예시 / 첫 3일 구매는 회차 내 체험이며 장기 소유 이월과 구분','기획 목업 · 핵심 구매/저장 구현, 처음 방 비교는 후속 / 3일 체험과 장기 소유 이월 구분')
s=s.replace('새로운 개인 생활과 연속 경력/소유이월 미구현 제안.','개인 생활 핵심 구현, 처음방 비교 후속. 연속 경력/차량 교체/이사는 미구현 제안.')
s=s.replace('원본 기획 생활은 미구현 제안, 동시 개발 변경과 배포를 구분.','작은 방 핵심 구현 이력 확인. 동시 변경/장기 기획과 구분.')
s=s.replace('시설·사고·수리\\n3일 영업·저장','시설·사고·수리\\n3일 영업·작은 방·저장')
s=s.replace('생활 구매·장기 동기','구매 동기·장기 성장')
s=s.replace('현재32일 아닌3일x예정20분','현재3일x예정20분')
# world and style are now explicit on the slide, not just in notes
s=s.replace("'내가 고른 생활을 만든다'],life+", "'내가 고른 생활을 만든다'],life+")
old="그림의 외형은 예시. 승객은 매번 짧게 만나는 일상 인물.');"
new="그림의 외형은 예시. 승객은 매번 짧게 만나는 일상 인물.','현대 한국의 은계 기반 생활권 / 실직 후 재기하는 기사와 짧게 만나는 일상 승객 / 생성 시안');"
assert old in s;s=s.replace(old,new)
s=s.replace('둥근 차량과 읽기 쉬운 표정\\n한글 상점·가로수·따뜻한 햇빛','스타일라이즈드 3D 카툰\\n한글 상점·둥근 차량·따뜻한 햇빛')
start=s.index("s=slide('차별화와 경쟁작 레퍼런스'")
end=s.index("s=slide('시장 진입과 비즈니스 모델'",start)
s=s[:start]+r"""s=slide('차별화와 경쟁작 레퍼런스',comp+' 한계는 각 게임의 결함이 아니라 본 제안 타겟/리듬과의 차이.');
table(s,[['비교 대상','강점 / 벤치마킹','본 제안 관점의 한계','USP'],['Taxi Life 대비','도시 택시 역할과\n명료한 운송 흐름','해외 도시 배경으로\n한국 동네 공감과 거리','한국 생활권의\n일상과 공간'],['Crazy Taxi 대비','즉각적인 운송 동기와\n짧은 결과 피드백','시간 경쟁 중심으로\n차분한 생활과 다른 리듬','안전 운송과\n승객 응대'],['ETS2 대비','운전의 몰입과\n운송 후 성장 기대감','장거리 화물 중심으로\n승객 응대와 다른 호흡','짧은 영업과\n개인 차·집 목표']],[250,310,340,268],165,370,23);
text(s,'경쟁력은 타겟 이용자의 실제 체험으로 검증한다',56,565,1168,55,31,C.blue,true);
cap(s,'ETS2: Euro Truck Simulator 2 / Crazy Taxi는 역사적 레퍼런스 / 장단점은 타겟 적합성 해석');

"""+s[end:]
insert=r"""
s=slide('승객 요청: 말에 답하고 운행으로 보여준다',src+' docs/content-design/02-passengers-and-fares.md, docs/content-tuning-request-outcomes.md. 승객 유형과 개별 요청은 별개. 유형 확인은 대표 요청의 수락과 다르며 실제 약속 관찰과 단발 응대 완료를 구분.');
table(s,[['승객의 말·상태','플레이어의 대응','운행 중·하차 반응'],['“부드럽게 부탁해요”','요청 수락 후 가감속·회전 조절','승차감 관찰과 요청 결과 반영'],['“조용히 가고 싶어요”','조용한 응대 선택','일반 후속 수다 억제·배려 반응'],['취객의 이야기','짧게 확인하거나 조용히 안내','선택을 기억한 대사·감사'],['잠든 승객','수면 배려·목적지 정차 후 깨우기','깨어난 뒤 안내·배려에 감사']],[350,400,418],160,380,25);
text(s,'요청 확인 → 내 선택 → 상태·대사 변화 → 하차 피드백',56,570,1168,55,31,C.blue,true);
cap(s,'현재 기반의 요약 / 모든 응답이 실제 행동 검증은 아님 / 표정·연출은 아래 목표 시안');

s=slide('취객: 짧은 응대에도 달라지는 반응',src+' docs/content-design/02-passengers-and-fares.md 취객 변주/하차. 이미지 assets/v5/drunk-passenger.png, 내장 image_gen. 대표 말은 기획 예시. 공통 확인/집중 안내는 실제 유형 버튼, 대표 요청 수락과 별개. 실제 구토 시 운송 유지·신규 호출 잠금, 하차 뒤 청결 저하·세차. 구토/수면은 조건부이며 매회 강제하지 않음.');
await img(s,OUT+'/assets/v5/drunk-passenger.png',56,145,850,478);
text(s,'승객',942,160,282,42,28,C.blue,true);
text(s,'“조금 취했네요.\n폐를 끼치면 미안해요.”',942,210,282,90,25);
text(s,'내 선택',942,325,282,42,28,C.blue,true);
text(s,'짧게 확인·설명\n또는 조용히 안내',942,375,282,85,26);
text(s,'운송 뒤',942,490,282,42,28,C.blue,true);
text(s,'배려를 기억한 감사\n오염 발생 시 세차',942,540,282,75,26);
cap(s,'생성 반응 시안 / 실제 구토 시에만 청결 관리로 연결 / 취객이라고 매번 사건이 발생하지 않음');

s=slide('잠자는 승객: 조용한 운행과 도착 후 배려',src+' docs/content-design/02-passengers-and-fares.md 잠자는 승객4변주. 실제 PassengerJobs.BehaviorAsleep/ShowWakeWait/CompleteDelivery 확인. 잠자는 승객은 비취객, 수면 대화미응답 중립. 기존 수락 약속은 유지. 첫 유효 도착 운임 확정, 기상15초 후F재호출/30초 안전정차 자동하차 정책. 이미지 assets/v5/sleeping-passenger.png 생성 시안.');
await img(s,OUT+'/assets/v5/sleeping-passenger.png',56,145,850,478);
text(s,'승객',942,160,282,42,28,C.blue,true);
text(s,'“도착하면\n깨워 주세요.”',942,210,282,90,26);
text(s,'내 대응',942,325,282,42,28,C.blue,true);
text(s,'조용히 운행\n안전 정차 후 깨우기',942,375,282,85,26);
text(s,'운송 뒤',942,490,282,42,28,C.blue,true);
text(s,'안내와 감사 반응\n기상 대기 후 하차',942,540,282,75,26);
cap(s,'생성 반응 시안 / 수면 중 대답 없음은 감점하지 않음 / 깨우는 시간은 지각·추가요금으로 소급하지 않음');

"""
idx=s.index("s=slide('운행 사이에도 판단이 있는 생활'")
s=s[:idx]+insert+s[idx:]
assert s.count('s=slide(')+s.count("await visual('")==24 # includes helper visual internal slide call
(r/'artifacts/proposal-v5-20261106/build.mjs').write_text(s,encoding='utf-8')
print('Prepared v5: 23 slides, three passenger slides, visible coverage repairs')
