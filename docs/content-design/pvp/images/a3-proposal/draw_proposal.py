from pathlib import Path
from html import escape
ROOT=Path('docs/content-design/pvp/images/a3-proposal')
class Svg:
 def __init__(self,w,h): self.w=w;self.h=h;self.parts=[]
 def add(self,s):self.parts.append(s)
 def path(self,d,c='#69777f',w=9,dash=None):self.add(f'<path d="{d}" fill="none" stroke="{c}" stroke-width="{w}" stroke-linecap="round" stroke-linejoin="round"'+(f' stroke-dasharray="{dash}"' if dash else '')+'/>')
 def text(self,x,y,t,size=18,c='#22323a',weight=400,anchor='start',cls=''):
  self.add(f'<text class="{cls}" x="{x}" y="{y}" font-size="{size}" fill="{c}" font-weight="{weight}" text-anchor="{anchor}">{escape(t)}</text>')
 def rect(self,x,y,w,h,fill,stroke='none',r=0):self.add(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="{fill}" stroke="{stroke}"/>')
 def circle(self,x,y,r,fill,stroke='none',width=2,dash=''):self.add(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{fill}" stroke="{stroke}" stroke-width="{width}"'+(f' stroke-dasharray="{dash}"' if dash else '')+'/>')
 def stop(self,x,y,n):
  self.circle(x,y,13,'#f6bd57','#fff',2);self.text(x,y+5,str(n),14,'#1d2e35',500,'middle','minor')
 def candidate(self,x,y,n):
  self.circle(x,y,14,'#fff','#bb7c19',2,'4 3');self.text(x,y+5,n,13,'#79520d',500,'middle','minor')
 def facility(self,x,y,name):
  self.rect(x-10,y-10,20,20,'#277f78','#fff',3);self.text(x+16,y+6,name,15,'#21645f',500,cls='minor')
 def signal(self,x,y,label='S'):
  self.circle(x,y,10,'#f8faf8','#467d64',2);self.text(x,y+4,label,11,'#2b6251',500,'middle','minor')
 def output(self,title):return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {self.w} {self.h}" role="img" aria-label="{title}"><title>{title}</title><style>text{{font-family:Malgun Gothic,Segoe UI,sans-serif}}.road8{{stroke:#344d61}}.road6{{stroke:#49687d}}.road4{{stroke:#6d8490}}.alley{{stroke:#9da8ac}}</style>'+''.join(self.parts)+'</svg>'

s=Svg(1400,1290)
s.rect(0,0,1400,1290,'#f6f5ef')
s.text(60,55,'A3  |  골목마다 다른 선택',34,weight=500)
s.text(60,86,'가상 PVP 도시 · 하늘뷰 제안 · 3.6 × 2.8 km 범위 유지 · 미구현',18,c='#5e6c72')
s.text(1290,135,'N ↑',20,weight=500,anchor='end')
# Organic landscape shapes make district boundaries legible without nine rectangles.
for d,c in [('M85 178 Q300 122 470 181 L469 400 Q305 440 103 391Z','#e7ece0'),('M496 182 Q669 136 844 195 L817 413 Q650 464 493 395Z','#e5ecea'),('M874 192 Q1100 168 1280 246 L1260 482 Q1040 459 852 408Z','#e8e7ef'),('M96 435 Q242 410 408 450 L397 689 Q240 760 92 668Z','#eee6de'),('M438 438 Q621 395 820 462 L848 691 Q631 747 437 686Z','#e9e7df'),('M884 488 Q1095 449 1285 511 L1270 738 Q1074 807 884 707Z','#e2e9ed'),('M89 754 Q233 700 393 754 L425 1056 Q215 1114 76 1021Z','#dce9d9'),('M454 750 Q654 707 836 757 L869 1059 Q635 1103 465 1061Z','#f0e5d7'),('M911 796 Q1100 737 1275 799 L1281 1062 Q1080 1113 910 1049Z','#e6e6e2')]:s.add(f'<path d="{d}" fill="{c}"/>')
# 4-lane perimeter and selected diagonal collectors; there is no 7x7 lattice.
arterials=[('M101 221 Q370 130 652 182 Q1017 171 1255 267 L1285 665 Q1295 931 1221 1053 Q931 1104 637 1072 Q270 1118 100 1007 L83 646 Q74 372 101 221',16,'road4'),('M105 430 Q355 414 488 438 L635 477',16,'road4'),('M821 432 Q1024 421 1275 470',16,'road4'),('M101 757 Q272 734 431 766 L630 800 Q735 780 850 799',16,'road4'),('M850 799 L1092 770 L1281 744',16,'road4'),('M435 180 L474 413 Q500 474 485 570 L431 766 Q421 918 464 1083',16,'road4'),('M865 189 L821 432 Q800 493 842 570 L850 799 Q865 935 909 1082',16,'road4'),('M85 570 L1285 570',28,'road8'),('M671 181 Q641 346 661 460 L691 570 Q727 710 704 834 L731 1080',22,'road6')]
for d,w,cl in arterials:s.add(f'<path class="{cl}" d="{d}" stroke-width="{w}" fill="none" stroke-linejoin="round"/>')
# Each district is hand-authored. Road loops, branches and offsets vary by use.
groups={
'NW': ['M120 290 Q285 205 435 252','M116 361 Q275 274 449 335','M164 420 L222 314 L289 219','M247 420 L302 315 L372 203','M338 425 L370 348 L442 294','M128 290 L170 330 L126 361','M222 314 L271 356 L247 420','M302 315 L338 355 L370 348','M170 330 L222 314','M271 356 L338 355'],
'HOSP':['M481 251 L568 251 L612 300 L667 286 L738 256 L852 273','M490 356 L556 328 L601 360 L654 349 L725 369 L832 349','M568 251 L556 328','M738 256 L725 369','M601 360 L600 420','M612 300 L603 323','M663 185 L612 218 L568 251','M725 369 L751 422','M505 251 L507 308 L556 328','M801 267 L782 312 L832 349'],
'UNI':['M858 290 Q1053 213 1262 324','M849 361 Q1052 293 1268 393','M868 432 Q1023 368 1235 449','M943 200 Q910 310 943 433','M1036 185 Q996 310 1028 409','M1131 199 Q1091 319 1133 424','M1218 242 L1185 352 L1235 449','M943 287 L991 307 L943 363','M1133 424 L1157 468'],
'WEST':['M104 488 L199 462 L232 500 L322 472 L457 501','M92 650 L183 620 L225 660 L326 624 L449 650','M135 483 L149 536 L194 570','M232 500 L250 546 L289 570','M322 472 L344 521 L383 570','M183 620 L161 696','M225 660 L253 746','M326 624 L350 710 L431 766','M149 536 L210 525','M350 710 L300 721','M400 491 L388 451'],
'STATION':['M489 491 L554 470 L606 505 L657 491 L742 462 L816 494','M467 651 L550 618 L606 649 L721 627 L834 664','M554 470 L550 536 L605 570','M742 462 L765 522 L793 570','M550 618 L543 699 L622 718 L701 689','M721 627 L759 711','M606 649 L606 690','M480 674 L512 706 L543 699','M605 505 L605 535','M765 522 L816 494'],
'EAST':['M853 503 L937 484 L1006 522 L1100 488 L1275 522','M863 661 L969 610 L1046 651 L1162 612 L1283 665','M886 719 L984 681 L1082 721 L1202 688 L1278 718','M937 484 L969 539 L1010 570','M1100 488 L1123 534 L1165 570','M1046 651 L1082 721','M1162 612 L1202 688','M984 681 L969 610','M1255 518 L1237 570','M910 643 L923 697'],
'PARK':['M100 837 Q247 768 433 843','M111 963 Q213 1028 440 969','M173 785 Q143 880 184 1027','M355 759 Q288 846 325 927 Q342 976 405 1030','M184 881 Q239 845 316 875','M184 1027 L247 986','M325 927 L385 900 L433 843','M111 963 L147 917','M247 986 L250 1047'],
'MARKET':['M446 828 L516 805 L548 839 L610 813 L655 851 L707 824 L764 852 L851 829','M445 929 L501 905 L542 936 L594 904 L650 937 L715 904 L782 933 L867 910','M465 1020 L539 995 L597 1021 L649 991 L725 1020 L795 996 L878 1028','M516 805 L501 905','M548 839 L570 872 L542 936 L539 995','M610 813 L594 904','M655 851 L650 937 L649 991','M707 824 L715 904','M764 852 L748 875 L782 933 L795 996','M481 775 L479 839','M610 750 L610 813','M806 794 L790 845','M492 1011 L510 1084','M725 1020 L731 1080','M867 910 L842 967 L878 1028'],
'TERMINAL':['M881 846 L978 815 L1164 818 L1269 853','M912 1001 L991 1029 L1168 1027 L1244 992','M978 815 L959 884 L996 938 L991 1029','M1164 818 L1188 881 L1154 946 L1168 1027','M959 884 L1063 865 L1188 881','M996 938 L1071 968 L1154 946','M1063 865 L1071 968','M1244 992 L1219 930 L1269 853','M909 927 L966 923','M1168 1027 L1192 1061']}
for k,roads in groups.items():
 for d in roads:s.add(f'<path class="alley" data-district="{k}" d="{d}" fill="none" stroke-width="8" stroke-linejoin="round" stroke-linecap="round"/>')
# river/park lake inside the park, with no deceptive road crossings.
s.add('<path d="M208 864 Q245 825 281 864 Q315 896 276 937 Q235 971 212 931 Q189 907 208 864" fill="#b7d6de"/>')
# Passenger stops: 18 retained slots, physically relocated proposals; numbers stay stable here.
stops=[(180,850),(332,963),(516,805),(782,933),(978,815),(1188,881),(199,462),(326,624),(554,470),(721,627),(1006,522),(1082,721),(222,314),(370,348),(568,251),(725,369),(943,287),(1133,424)]
for i,(x,y) in enumerate(stops,1):s.stop(x,y,i)
for x,y,n in [(268,356,'A'),(605,505,'B'),(650,937,'C'),(1071,968,'D'),(1036,298,'E'),(184,881,'F')]:s.candidate(x,y,n)
for x,y,n in [(353,225,'F1'),(596,680,'F2'),(1197,679,'F3'),(144,986,'P1'),(949,1016,'P2'),(297,381,'P3'),(760,244,'P4')]:s.facility(x,y,n)
# Candidate service court connectors (all off arterial, shown explicitly).
for d in ['M353 225 L372 203','M353 225 L370 260','M596 680 L606 649','M596 680 L622 718','M1197 679 L1202 688','M1197 679 L1162 612','M144 986 L130 974','M144 986 L175 997','M949 1016 L958 1018','M949 1016 L932 1008','M297 381 L287 343','M297 381 L271 356','M760 244 L738 256','M760 244 L801 267'] :s.path(d,'#277f78',4)
for x,y in [(485,570),(691,570),(842,570),(661,460),(704,834),(431,766),(821,432),(1010,570)]:s.signal(x,y)
s.signal(474,413,'P');s.signal(850,799,'P')
for x,y in [(555,570),(1105,570)]:s.circle(x,y,13,'#fff','#49687d');s.text(x,y+5,'U',15,'#344d61',500,'middle','minor')
# Prominent district labels are offset from the busiest geometry.
for x,y,t in [(123,198,'서북 주거 · 부채꼴'),(506,207,'북부 병원 · T / 엇갈림'),(908,249,'동북 대학 · 곡선'),(105,718,'서부 주거 · 사선'),(475,744,'중앙역 · 다중 접근'),(965,758,'동부 업무 · 사선연결'),(100,1059,'서남 공원 · 순환 / 통과'),(491,1058,'남부 시장 · 미로 / 일방'),(968,1070,'동남 터미널 · 이중진입')]:s.text(x,y,t,18,weight=500,cls='district')
s.text(118,553,'중앙 동서 8차로',16,'#344d61',500,cls='axislabel')
s.text(739,395,'남북 6차로',16,'#49687d',500,cls='axislabel')
s.text(1030,208,'외곽 / 연결 4차로',16,'#637a86',500,cls='axislabel')
s.add('<rect x="433" y="742" width="447" height="351" fill="none" stroke="#ba6b30" stroke-width="2" stroke-dasharray="8 6"/>')
s.text(790,1124,'점선 범위 → 시장 확대',16,'#8c531e',cls='minor')
s.path('M92 1138 L256 1138','#344d61',4);s.path('M92 1132 L92 1144 M256 1132 L256 1144','#344d61',2);s.text(174,1165,'약 500 m',16,anchor='middle')
s.text(60,1212,'● 1–18 기존 승객 슬롯의 새 배치안    ◌ A–F 추가 후보    ■ F 주유 / 정비 / 세차 · P 주차 / 식사',17)
s.text(60,1247,'S 신호 후보    P 비보호 후보    U 조건부 유턴 후보    골목 실선은 차량 통행 · 도로폭은 구분용 과장',17,c='#5e6c72')
svg=s.output('A3 가상 PVP 도시 전체 하늘뷰 시안')
(ROOT/'a3-city-overview.svg').write_text(svg,encoding='utf-8')
# Expanded market: same topology, comfortably labeled at local scale.
z=Svg(1400,1130);z.rect(0,0,1400,1130,'#f6f5ef');z.text(55,55,'남부 시장  |  짧은 길은 많고, 출구는 네 방향',32,weight=500);z.text(55,87,'전체 시안의 주황 점선 범위 확대 · 실제 지형 아님 · 연결 / 규제는 미확정',18,c='#5e6c72')
z.add('<defs><clipPath id="market-crop"><rect x="50" y="175" width="1300" height="840"/></clipPath></defs><g clip-path="url(#market-crop)">')
z.add('<g transform="translate(-924,-1524) scale(2.25)">')
z.add('<path d="M440 755 L860 755 L880 1073 L463 1083Z" fill="#f0e5d7"/>')
for d,w,cl in [arterials[0],arterials[3],arterials[4],arterials[5],arterials[6],arterials[8]]:z.add(f'<path class="{cl}" d="{d}" stroke-width="{w}" fill="none"/>')
for d in groups['MARKET']:z.add(f'<path class="alley" d="{d}" fill="none" stroke-width="7" stroke-linejoin="round"/>')
# building footprints not generic repeating blocks; fit individual irregular islands.
for d in ['M527 849 L544 868 L525 891 L516 878Z','M584 831 L600 827 L586 874 L574 860Z','M616 856 L641 870 L636 898 L611 884Z','M668 871 L693 850 L701 879 L680 897Z','M772 887 L787 868 L815 850 L827 875 L803 909Z','M552 955 L575 932 L591 939 L584 986 L558 979Z','M607 937 L634 953 L629 977 L605 991Z','M668 954 L697 932 L718 945 L704 988 L670 978Z','M746 958 L771 952 L779 981 L748 997Z'] :z.add(f'<path d="{d}" fill="#d2c3b2"/>')
z.stop(516,805,3);z.stop(782,933,4);z.candidate(650,937,'C')
# one-way pair: each arrow points down/up local path, with return via adjacent street.
z.path('M548 839 L570 872 L542 936','#ba6b30',3);z.path('M594 904 L610 813','#ba6b30',3)
z.add('<path d="M561 878 l-4 14 l11 -9Z M603 848 l-5 13 l-4 -14Z" fill="#ba6b30"/>')
# entry markers and diagonal alternative; court with separate driveways.
for x,y,n in [(479,782,'E1'),(798,819,'E2'),(867,910,'E3'),(506,1069,'E4')]:z.circle(x,y,10,'#fff','#277f78');z.text(x,y+4,n,9,'#21645f',500,'middle')
z.rect(754,988,30,25,'#d5e6e0');z.path('M748 991 L756 997 L777 1004 L795 996','#277f78',3);z.path('M748 991 L756 997','#277f78',3)
z.add('</g></g>')
# Outside labels use leaders, never sit across road labels.
for x,y,t in [(62,153,'① 북서 진입 E1'),(870,153,'② 북동 진입 E2'),(1092,537,'③ 동쪽 E3'),(56,1055,'④ 남서 E4')]:z.text(x,y,t,21,'#21645f',500)
z.text(221,353,'3  시장 북문',21,weight=500);z.text(968,650,'4  시장 동문',21,weight=500)
z.text(155,526,'짧은 일방 ↓',20,'#a45f25',500);z.text(421,411,'복귀 일방 ↑',20,'#a45f25',500)
z.text(615,809,'C  추가 정차 포켓 후보',20,'#a17425',500)
z.text(948,975,'시장 물류 / 식사마당',19,'#21645f',500);z.text(948,1002,'진입·출차 분리',17,'#21645f')
z.text(55,1100,'좁은 길의 직선 지름길 ↔ 넓은 연결도로 우회 · 막다른 길은 주차/용무만 · 보행전용길을 차량 통과로 그리지 않음',17,c='#5e6c72')
(ROOT/'a3-market-detail.svg').write_text(z.output('A3 남부시장 골목과 네 방향 출입구 확대 시안'),encoding='utf-8')
print('SVG written:',len(svg),'overview chars')
