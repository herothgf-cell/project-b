"""Inspect this NGI/NDA sample without modifying raw files or assigning a CRS."""
import csv
import hashlib
import io
import json
import re
from collections import Counter
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
RAW = ROOT / 'data/raw/ngii'
OUT = ROOT / 'data/inspection/ngii'
OUT.mkdir(parents=True, exist_ok=True)
ngi = next(RAW.glob('*.ngi'))
nda = ngi.with_suffix('.nda')

def blocks(path):
    for block in path.read_text(encoding='cp949', errors='strict').split('<LAYER_START>')[1:]:
        name = re.search(r'\$LAYER_NAME\s+"([^"]+)"', block)[1]
        data = block.split('<DATA>')[1].split('<END>')[0]
        yield name, block.split('<DATA>')[0], re.findall(r'\$RECORD\s+(\d+)\s*\n(.*?)(?=\$RECORD|\Z)', data, re.S)

attrs = {}
schemas = {}
errors = []
for name, header, records in blocks(nda):
    fields = re.findall(r'ATTRIB\("([^"]+)"', header)
    schemas[name] = fields
    attrs[name] = {}
    for rid, body in records:
        row = next(csv.reader(io.StringIO(body.strip()), skipinitialspace=True))
        if len(row) != len(fields):
            errors.append(f'{name}/{rid}: attribute count mismatch')
        attrs[name][rid] = dict(zip(fields, row))

features = []
inventory = []
for name, header, records in blocks(ngi):
    types = Counter()
    ids = set()
    for rid, body in records:
        lines = [s.strip() for s in body.strip().splitlines()]
        kind = lines[0].split('(')[0]
        types[kind] += 1
        if rid in ids:
            errors.append(f'{name}/{rid}: duplicate geometry ID')
        ids.add(rid)
        parts = []
        if kind in ('POINT', 'TEXT'):
            parts = [[list(map(float, lines[1].split()))]]
        elif kind == 'LINESTRING':
            n = int(lines[1])
            parts = [[list(map(float, v.split())) for v in lines[2:2+n]]]
        elif kind == 'POLYGON':
            n = int(lines[1].split()[1]); pos = 2
            for _ in range(n):
                count = int(lines[pos]); pos += 1
                parts.append([list(map(float, v.split())) for v in lines[pos:pos+count]])
                pos += count
        else:
            raise ValueError(f'Unsupported geometry {kind}')
        if any(len(xy) != 2 for part in parts for xy in part):
            errors.append(f'{name}/{rid}: non-2D coordinate')
        features.append(dict(layer=name, id=rid, kind=kind, parts=parts,
                             attributes=attrs[name].get(rid, {})))
    if ids != set(attrs[name]):
        errors.append(f'{name}: geometry/attribute IDs differ')
    inventory.append(dict(layer=name, records=len(records), geometry=dict(types), fields=schemas[name]))

roads = [f for f in features if f['layer'] == 'A0020000']
buildings = [f for f in features if f['layer'] == 'B0010000']
boundary = next(f for f in features if f['layer'] == 'H0010000')['parts'][0]
xs, ys = zip(*boundary)
bounds = [min(xs), min(ys), max(xs), max(ys)]
stats = {}
for field in schemas['A0020000']:
    if field not in ('ID', 'UFID'):
        stats[field] = dict(Counter(f['attributes'][field] for f in roads))
summary = dict(files=[dict(name=p.name, bytes=p.stat().st_size, sha256=hashlib.sha256(p.read_bytes()).hexdigest())
                           for p in sorted(RAW.glob('*')) if p.is_file()],
               encoding='CP949 (NGI/NDA); XML declares UTF-8',
               crs='GRS80 in XML; projection/EPSG unconfirmed; coordinates preserved',
               bounds=bounds, extent=[bounds[2]-bounds[0], bounds[3]-bounds[1]],
               layers=len(inventory), records=len(features), errors=errors,
               road_attributes=stats,
               building_floors=dict(Counter(f['attributes']['층수'] for f in buildings)),
               inventory=inventory)
(OUT/'inspection.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2),encoding='utf-8')
(OUT/'native-features.json').write_text(json.dumps(features, ensure_ascii=False),encoding='utf-8')

W,H=1280,1540
im=Image.new('RGB',(W,H),'#f6f7f9'); d=ImageDraw.Draw(im)
fontpath='C:/Windows/Fonts/malgun.ttf'
font=ImageFont.truetype(fontpath,18); small=ImageFont.truetype(fontpath,14)
title=ImageFont.truetype(fontpath,28)
scale=2.15; left=140; top=150
def xy(p):
    return (left+(p[0]-bounds[0])*scale,top+(bounds[3]-p[1])*scale)
styles={'G0010000':('#dbead9','#99b592',1),'F0010000':(None,'#ddd4b6',1),
        'B0010000':('#e1e5ec','#9ca7b6',1),'A0010000':(None,'#69747f',2),
        'A0043325':('#d5b9a0','#ba936e',1),'A0033320':(None,'#9bafba',1),
        'A0020000':(None,'#2383cf',2),'H0010000':(None,'#c84366',4)}
for layer,(fill,stroke,width) in styles.items():
    for f in features:
        if f['layer']!=layer: continue
        for part in f['parts']:
            pts=[xy(p) for p in part]
            if f['kind']=='POLYGON' and fill: d.polygon(pts,fill=fill)
            if len(pts)>1: d.line(pts+([pts[0]] if f['kind']=='POLYGON' else []),fill=stroke,width=width)
for f in buildings:
    label=f['attributes'].get('주기','')
    if any(s in label for s in ['스타트라인','해광빌딩','영일중학교','출입국','수원신협']):
        pts=f['parts'][0]; cx=sum(p[0] for p in pts)/len(pts); cy=sum(p[1] for p in pts)/len(pts)
        x,y=xy((cx,cy)); label=label.split(',')[0]
        d.rectangle((x,y,x+d.textlength(label,font=small)+8,y+24),fill='white')
        d.text((x+4,y+2),label,font=small,fill='#202a36')
pilot_nodes=[('T1',(206742.49,516844.52)),('T2',(206884.87,517031.16)),('T3',(206756.21,517129.69))]
for label,point in pilot_nodes:
    x,y=xy(point)
    d.ellipse((x-16,y-16,x+16,y+16),fill='#b91c1c',outline='white',width=3)
    d.rectangle((x+18,y-18,x+66,y+12),fill='white')
    d.text((x+23,y-16),label,font=font,fill='#b91c1c')
d.text((55,35),'수원2296 원본 도형 검토 · 2008년',font=title,fill='#17283f')
d.text((55,80),'청색: A002 도로선 | 회색: 건물·도로 형상 | 적색: 도엽 경계',font=font,fill='#374151')
d.text((55,112),'원본 좌표 그대로 표시 / 좌표계 EPSG 미확정 / 현재 도로 현황과 다를 수 있음',font=font,fill='#374151')
for y in range(516800,517301,100):
    px,py=xy((bounds[0],y)); d.text((20,py-10),str(y),font=small,fill='#475569')
for x in range(206700,207001,100):
    px,py=xy((x,bounds[1]));d.text((px-25,py+15),str(x),font=small,fill='#475569')
d.text((55,1410),f'31개 레이어 / {len(features):,}개 도형 / 도로선 {len(roads)}개 / 건물 {len(buildings)}개',font=font,fill='#17283f')
d.text((55,1445),'출처: 국토지리정보원 수치지도2.0 377092296 · 게임 제작 검토용 가공',font=font,fill='#475569')
im.save(OUT/'sheet-preview.png')
print(json.dumps({k:summary[k] for k in ['layers','records','bounds','extent','errors','road_attributes','building_floors']},ensure_ascii=False,indent=2))
