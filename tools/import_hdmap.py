"""Read-only NGII Route 42 importer. Original node IDs, no proximity joins.

Dependencies: pyshp, pyproj (optionally installed in artifacts/geo-inspection-deps).
Run from repository root: python tools/import_hdmap.py
"""
import io, json, math, sys, hashlib
from pathlib import Path
from zipfile import ZipFile

ROOT=Path(__file__).resolve().parents[1]
ORIGIN=(181900,537200)
GROUP='일반국도_42호선_SEC002_2025'

def length(p):return sum(math.dist(a,b) for a,b in zip(p,p[1:]))
def point_at(p,d):
    for a,b in zip(p,p[1:]):
        n=math.dist(a,b)
        if d<=n:return tuple(a[i]+(b[i]-a[i])*max(0,d)/max(n,1e-9) for i in range(2))
        d-=n
    return tuple(p[-1])
def slice_line(p,start,end):
    result=[point_at(p,start)];at=0
    for a,b in zip(p,p[1:]):
        at+=math.dist(a,b)
        if start+1e-5<at<end-1e-5:result.append(tuple(b))
    result.append(point_at(p,end));return result
def tangent(p,at):
    a=point_at(p,max(0,at-.3));b=point_at(p,min(length(p),at+.3));n=math.dist(a,b)
    return ((b[0]-a[0])/max(n,1e-9),(b[1]-a[1])/max(n,1e-9))
def dot(a,b):return a[0]*b[0]+a[1]*b[1]
def project(p,q):
    best=(float('inf'),0);at=0
    for a,b in zip(p,p[1:]):
        v=(b[0]-a[0],b[1]-a[1]);n=math.dist(a,b);t=max(0,min(1,dot((q[0]-a[0],q[1]-a[1]),v)/max(n*n,1e-9)))
        dist=math.dist(q,(a[0]+v[0]*t,a[1]+v[1]*t))
        if dist<best[0]:best=(dist,at+n*t)
        at+=n
    return best
def turn_of(p):
    a=tangent(p,0);b=tangent(p,length(p));angle=math.degrees(math.atan2(a[0]*b[1]-a[1]*b[0],dot(a,b)))
    return -1 if 40<angle<140 else 1 if -140<angle< -40 else 0
def successors(edges):
    starts={}
    for i,e in enumerate(edges):starts.setdefault(e['from'],[]).append(i)
    return [[j for j in starts.get(e['to'],[]) if j!=i and math.dist(e['points'][-1],edges[j]['points'][0])<.15 and dot(tangent(e['points'],length(e['points'])),tangent(edges[j]['points'],0))>-.5] for i,e in enumerate(edges)]
def change_mark(attrs):return str(attrs.get('type',''))=='212'

def read_layers():
    sys.path.insert(0,str(ROOT/'artifacts/geo-inspection-deps'))
    import shapefile
    from pyproj import CRS,Transformer
    result={};provenance=[]
    for path in sorted((ROOT/'data/raw/ngii/hdmap/eungye').glob('*.zip')):
        with ZipFile(path) as z:
            for name in z.namelist():
                if GROUP+'_' not in name or not name.endswith('.shp'):continue
                stem=name[:-4];kind=stem.split(GROUP+'_')[-1]
                if kind not in ('a1_node','a2_link','b2_surfacelinemark','b3_surfacemark','c1_trafficlight'):continue
                if kind in result:raise ValueError('Duplicate selected layer '+kind)
                crs=CRS.from_wkt(z.read(stem+'.prj').decode('utf-8-sig'))
                if crs.to_epsg()!=5179:raise ValueError('Unexpected CRS')
                tx=Transformer.from_crs(crs,5186,always_xy=True)
                enc=z.read(stem+'.cpg').decode().strip();enc={'949':'cp949','65001':'utf-8'}.get(enc,enc)
                reader=shapefile.Reader(shp=io.BytesIO(z.read(name)),shx=io.BytesIO(z.read(stem+'.shx')),dbf=io.BytesIO(z.read(stem+'.dbf')),encoding=enc)
                rows=[]
                for sr in reader.iterShapeRecords():
                    if sr.shape.shapeType==0:continue
                    if len(sr.shape.parts)>1:raise ValueError('Multipart requires explicit handling: '+name)
                    pts=[tx.transform(*p[:2]) for p in sr.shape.points]
                    pts=[(x-ORIGIN[0],y-ORIGIN[1]) for x,y in pts]
                    if not all(math.isfinite(x) and math.isfinite(y) for x,y in pts):raise ValueError('Invalid coordinate')
                    # Keep complete links, never clip endpoints and inherit false connectivity.
                    if not all(5<x<2095 and 5<y<1745 for x,y in pts):continue
                    rows.append({'attrs':sr.record.as_dict(),'points':pts})
                result[kind]=rows
                provenance.append({'archive':path.name,'member':name,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
    if 'a2_link' not in result:raise ValueError('Route 42 2025 layer not found')
    return result,provenance

def build():
    layers,provenance=read_layers();raw=[]
    for row in layers['a2_link']:
        a=row['attrs'];p=row['points']
        if len(p)<2 or length(p)<.2:continue
        raw.append({'id':a['id'],'from':a['fromnodeid'],'to':a['tonodeid'],'points':p,'number':a['laneno'],'left':a['l_linkid'],'right':a['r_linkid'],'updated':a['updatedate'],'turn':turn_of(p)})
    byid={e['id']:e for e in raw}
    if len(byid)!=len(raw):raise ValueError('Duplicate link IDs')
    changes=[];splits={e['id']:{0.,length(e['points'])} for e in raw}
    marks=layers['b2_surfacelinemark']
    for e in raw:
        if e['turn'] or length(e['points'])<65:continue
        for side,key in [(-1,'left'),(1,'right')]:
            target=byid.get(e[key])
            if not target or target['turn'] or target['right' if side<0 else 'left']!=e['id']:continue
            boundaries=[m for m in marks if change_mark(m['attrs']) and {m['attrs'].get('l_linkid'),m['attrs'].get('r_linkid')}=={e['id'],target['id']}]
            if not boundaries:continue
            for begin in range(6,int(length(e['points'])-40),4):
                end=begin+32;start=point_at(e['points'],begin);targetStart=project(target['points'],start)[1];targetEnd=targetStart+32
                if targetEnd>length(target['points'])-5:continue
                valid=True;curve=[]
                for j in range(33):
                    t=j/32;a=point_at(e['points'],begin+32*t);b=point_at(target['points'],targetStart+32*t)
                    middle=((a[0]+b[0])/2,(a[1]+b[1])/2)
                    if not 2.2<math.dist(a,b)<4.8 or dot(tangent(e['points'],begin+32*t),tangent(target['points'],targetStart+32*t))<.98 or not any(project(m['points'],middle)[0]<.8 for m in boundaries):valid=False;break
                    blend=t*t*t*(t*(t*6-15)+10);curve.append(tuple(a[k]+(b[k]-a[k])*blend for k in range(2)))
                if not valid:continue
                splits[e['id']].add(float(begin));splits[target['id']].add(targetEnd)
                changes.append({'source':e['id'],'target':target['id'],'begin':float(begin),'end':targetEnd,'side':side,'points':curve,'mark':boundaries[0]['attrs']['id']});break
    def node(e,d):
        if d<.001:return e['from']
        if abs(d-length(e['points']))<.001:return e['to']
        return 'split:'+e['id']+':'+format(d,'.6f')
    edges=[]
    for e in raw:
        cuts=sorted(splits[e['id']])
        for i,(a,b) in enumerate(zip(cuts,cuts[1:])):
            edges.append({'id':e['id']+':'+str(i),'sourceId':e['id'],'from':node(e,a),'to':node(e,b),'points':slice_line(e['points'],a,b),'number':e['number'],'turn':e['turn'],'change':0,'updated':e['updated'],'sourceOffset':a})
    for c in changes:
        e=byid[c['source']];t=byid[c['target']]
        edges.append({'id':'change:'+e['id']+':'+t['id'],'sourceId':e['id'],'from':node(e,c['begin']),'to':node(t,c['end']),'points':c['points'],'number':t['number'],'turn':0,'change':c['side'],'updated':e['updated'],'sourceOffset':c['begin'],'evidence':c['mark']})
    nxt=successors(edges)
    for i,e in enumerate(edges):e['next']=nxt[i]
    # Select main-road approaches with real predecessor IDs. Minor-road capture
    # can end at the right-turn endpoint: stop within the recorded curve.
    cases=[]
    for name,center in [('R42-A',(500,430)),('R42-B',(790,370))]:
        for turn in [-1,1]:
            options=[]
            for i,e in enumerate(edges):
                if e['change'] or e['turn']!=turn or math.dist(point_at(e['points'],length(e['points'])/2),center)>65:continue
                chain=[i];seen={i}
                while sum(length(edges[k]['points']) for k in chain[:-1])<90:
                    before=[j for j,q in enumerate(edges) if chain[0] in q['next'] and j not in seen and not q['turn'] and not q['change']]
                    if not before:break
                    j=max(before,key=lambda j:length(edges[j]['points']));chain.insert(0,j);seen.add(j)
                approach=sum(length(edges[k]['points']) for k in chain[:-1])
                if approach<45:continue
                # Prefer ending after the curve if a genuine successor exists.
                tail=[j for j in e['next'] if not edges[j]['turn'] and not edges[j]['change']]
                if tail:chain.append(max(tail,key=lambda j:length(edges[j]['points'])))
                options.append((approach,chain))
            if not options:raise ValueError('No connected test approach '+name+str(turn))
            _,chain=max(options,key=lambda pair:pair[0]);first=edges[chain[0]];last=edges[chain[-1]]
            cases.append({'name':name+(' 좌회전' if turn<0 else ' 우회전'),'turn':turn,'startEdge':chain[0],'startAt':max(3,length(first['points'])-75),'goalEdge':chain[-1],'goalAt':max(1,min(22,length(last['points'])-2)) if last['turn']==0 else length(last['points'])-2,'expected':chain,'captureEnds':len(last['next'])==0})
    # Lane-change case ends in a reachable original lane after the change.
    for i,e in enumerate(edges):
        if not e['change']:continue
        before=[j for j,q in enumerate(edges) if i in q['next'] and not q['change'] and length(q['points'])>4]
        after=[j for j in e['next'] if not edges[j]['change'] and length(edges[j]['points'])>8]
        if before and after:
            cases.append({'name':'차로 변경 시험','turn':0,'startEdge':before[0],'startAt':1.,'goalEdge':after[0],'goalAt':min(30,length(edges[after[0]]['points'])-2),'expected':[before[0],i,after[0]],'captureEnds':False});break
    if len(cases)!=5:raise ValueError('No verified lane-change trial')
    def points(p):return [{'x':round(x,4),'z':round(z,4)} for x,z in p]
    base=json.loads((ROOT/'unity/EungyeTaxi/Assets/Resources/eungye-map.json').read_text(encoding='utf-8'))
    base.update(mapId='route42',source='국토지리정보원 국도42호선 SEC002 · 2025 패키지 / 객체 갱신 2020·2022 / 시험 신호',coordinateStatus='EPSG:5179 → 5186, local origin 181900,537200')
    base['roads']=[{'id':e['id'],'width':3.8,'lanes':1,'sourceLanes':1,'oneWay':1,'points':points(e['points'])} for e in raw]
    base['targets']=[{'name':'R42-A','x':500,'z':430},{'name':'R42-B','x':790,'z':370}]
    base['hdEdges']=[dict({k:v for k,v in e.items() if k not in ('from','to','points')},fromNode=e['from'],toNode=e['to'],points=points(e['points'])) for e in edges]
    base['hdTrials']=cases
    base['hdMarks']=[{'id':m['attrs']['id'],'type':str(m['attrs']['type']),'kind':str(m['attrs']['kind']),'points':points(m['points'])} for m in marks]
    base['hdSignals']=[{'x':round(m['points'][0][0],4),'z':round(m['points'][0][1],4)} for m in layers['c1_trafficlight']]
    # Trial does not use passenger kerb approach or campaign logic.
    base['stops']=[]
    out=ROOT/'unity/EungyeTaxi/Assets/Resources/route42-map.json';out.write_text(json.dumps(base,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
    report={'provenance':provenance,'lanes':len(raw),'edges':len(edges),'changes':len(changes),'trials':cases,'missingSuccessors':[e['id'] for e in edges if not e['next']],'rule':'Node ID + <0.15m endpoint + non-opposing tangent; changes require reciprocal IDs and white single dashed 212 over entire crossing.'}
    (ROOT/'data/inspection/ngii').mkdir(parents=True,exist_ok=True)
    (ROOT/'data/inspection/ngii/route42-validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({k:v for k,v in report.items() if k not in ('provenance','missingSuccessors')},ensure_ascii=False,indent=2))

if __name__=='__main__':build()
