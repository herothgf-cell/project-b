"""Standalone approval drawings, not screenshots or implemented game UI."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
OUT=Path(__file__).resolve().parents[1]/'docs/ui'
FONT='C:/Windows/Fonts/malgun.ttf'
W,H=1600,900
def setup():
 global im,d
 im=Image.new('RGB',(W,H),'#9eb9c7');d=ImageDraw.Draw(im)
def text(x,y,s,size=20,color='#eef5f6'):
 d.text((x,y),s,font=ImageFont.truetype(FONT,size),fill=color)
def panel(box): d.rounded_rectangle(box,16,fill='#172a35',outline='#49606b',width=1)
def button(box,s):
 d.rounded_rectangle(box,9,fill='#2d4652');text(box[0]+12,box[1]+8,s,18)
def bar(x,y,w,v,c='#66dbba'):
 d.rounded_rectangle((x,y,x+w,y+6),3,fill='#40555e');d.rounded_rectangle((x,y,x+w*v,y+6),3,fill=c)
setup()
d.rectangle((0,340,W,H),fill='#91a195');d.polygon([(620,340),(930,340),(1500,900),(50,900)],fill='#59666e')
d.line([(750,340),(640,900)],fill='#efcc59',width=5);d.line([(850,340),(1120,900)],fill='#e7ebe5',width=5)
for box in [(0,200,410,580),(450,240,570,430),(1070,140,1600,570),(960,270,1035,420)]:d.rectangle(box,fill='#8d9c9f')
d.line([(880,880),(810,500),(815,360)],fill='#66ded2',width=9);d.rounded_rectangle((750,620,865,790),12,fill='#f6cb46');d.rectangle((767,650,850,700),fill='#28434e')
panel((24,24,825,108));text(44,40,'시흥 은계지구',27);text(285,36,'08:42   ·   1 / 3일',20);text(285,68,'17:18 남음',16,'#9fb5bf')
button((518,42,602,83),'정보');button((614,42,698,83),'퇴근');button((710,42,804,83),'도움말')
panel((914,24,1228,136));text(936,37,'현재 잔액',17,'#a7bdc4');text(936,63,'₩ 15,400',32);text(936,108,'누적 순수익   +5,400',16,'#66dbba')
panel((24,126,354,252));text(45,143,'● 직진   ·   18초',21,'#70e7b3');text(45,181,'120m 앞 우회전',26);text(45,222,'2차로 준비 · 도착 약 3분',17)
text(27,274,'● 승객 운송 중 · 은계 승하차 5',20,'#183641');text(27,308,'남은 시간 4분 32초 · 예상 운임 5,400원',17,'#183641')
panel((1250,24,1576,315))
for pts in [[(1270,55),(1545,265)],[(1280,260),(1530,50)],[(1390,35),(1435,280)],[(1270,155),(1555,155)]]:d.line(pts,fill='#74929a',width=8)
d.line([(1390,210),(1430,155),(1510,155)],fill='#67e2d0',width=5);d.ellipse((1378,198,1402,222),fill='#f7d84f',outline='white',width=3);button((1455,267,1563,304),'M 지도')
panel((24,646,370,874));text(45,662,'D · 수동 운전',18,'#a9bbc2');text(45,683,'028',83);text(245,742,'km/h',20)
for i,(s,v) in enumerate([('연료 60%',.6),('포만 78',.78),('피로 14',.14)]):text(44+i*104,810,s,16);bar(44+i*104,846,86,v)
panel((505,707,1090,874));text(530,723,'승객',17,'#a9bbc2');text(530,758,'“천천히 부드럽게 가 주실 수 있나요?”',24);button((530,811,832,854),'안전하게 정차 후 답변');button((846,811,1065,854),'잠시만요')
panel((1250,725,1576,874));text(1274,744,'♡ 승객 만족도          96',20);bar(1274,783,275,.96);text(1274,802,'◇ 운행 점수             85',20);bar(1274,846,275,.85,'#7ab9e5')
text(590,865,'승인용 배치 시안 · 실제 게임 미반영',13,'#afc6ca');im.save(OUT/'2026-10-23-driving-proposal.png')
setup();d.rectangle((0,0,W,H),fill='#0d1c25');text(26,20,'M  은계 지도',30);text(280,29,'1 / 3일 · 08:42',18,'#a9bbc2');button((1250,20,1420,65),'위치 정보');button((1430,20,1575,65),'M 닫기')
d.rounded_rectangle((20,90,1220,880),16,fill='#e0e9df')
roads=[[(130,185),(620,780)],[(1080,160),(250,780)],[(65,440),(1160,440)],[(590,125),(625,840)],[(315,570),(850,570),(850,755),(315,755),(315,570)],[(625,570),(1000,590),(1090,440)]]
for r in roads:d.line(r,fill='#c8d4c5',width=29);d.line(r,fill='#fff',width=17)
d.polygon([(690,210),(820,230),(865,340),(803,400),(725,365)],fill='#8dc7df');text(705,280,'호수공원',20,'#365d69');text(340,399,'은계중앙로',21,'#607369')
for i,(x,y) in enumerate([(405,570),(845,440),(568,320)],1):d.ellipse((x-23,y-23,x+23,y+23),fill='#287d70',outline='white',width=4);text(x-7,y-17,str(i),22)
d.ellipse((306,421,344,459),fill='#f7d848',outline='white',width=4);text(316,422,'▲',20,'#2b4246')
for x,y,s in [(295,735,'♨ 식당'),(605,193,'주유'),(205,475,'P')]:text(x,y,s,20,'#49615d')
for x,s in [(40,'시설'),(165,'교통'),(290,'법규 OFF')]:button((x,108,x+112,153),s)
for y,s in [(675,'◎'),(735,'+'),(795,'-')]:button((1145,y,1200,y+52),s)
text(40,848,'지도 배치 예시 / 제품에는 원자료 출처 유지',13,'#557267')
panel((1240,90,1577,880));text(1262,113,'주변 호출  3',26);text(1262,155,'선택하면 지도에서 위치 확인',16,'#a8bbc2')
for i,(a,b,fare,t,dist) in enumerate([(12,7,'5,700',5,260),(8,5,'5,400',4,420),(3,4,'7,700',7,650)]):
 y=204+i*167;d.rounded_rectangle((1258,y,1558,y+145),12,fill='#24434b' if i==0 else '#20343f',outline='#65dbbd' if i==0 else '#405964');text(1276,y+12,f'{i+1}  은계 승하차 {a}',18,'#67dcc0');text(1276,y+45,f'은계 승하차 {b} →',23);text(1276,y+85,f'예상 ₩{fare} · 약 {t}분',17);text(1276,y+113,f'승차 지점까지 {dist}m',15,'#a8bbc2')
button((1258,728,1558,782),'선택한 호출 수락');text(1263,805,'승객 요청은 탑승 후 대화',16,'#a8bbc2');text(1263,849,'승인용 시안 · 게임 미반영',14,'#a8bbc2');im.save(OUT/'2026-10-23-map-proposal.png')
