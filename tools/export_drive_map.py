"""Export a small planar driving map. Source units are provisionally metres."""
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
BOUNDS=(206653.52,516760.59,207097.55,517315.86)

def clip_segment(a,b,box):
    dx,dy=b[0]-a[0],b[1]-a[1]
    lo,hi=0.,1.
    for p,q in zip((-dx,dx,-dy,dy),(a[0]-box[0],box[2]-a[0],a[1]-box[1],box[3]-a[1])):
        if abs(p)<1e-12:
            if q<0:return None
        elif p<0:lo=max(lo,q/p)
        else:hi=min(hi,q/p)
    if lo>=hi:return None
    return ((a[0]+lo*dx,a[1]+lo*dy),(a[0]+hi*dx,a[1]+hi*dy))

def cross(a,b,c):return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])

def triangulate(points):
    """Ear clipping for a simple ring; fail rather than invent a roof."""
    area=sum(a[0]*b[1]-b[0]*a[1] for a,b in zip(points,points[1:]+points[:1]))
    ids=list(range(len(points)))
    if area<0:ids.reverse()
    triangles=[]
    while len(ids)>3:
        for i,b in enumerate(ids):
            a,c=ids[i-1],ids[(i+1)%len(ids)]
            if cross(points[a],points[b],points[c])<=1e-8:continue
            if any(all(cross(points[u],points[v],points[j])>=-1e-8 for u,v in [(a,b),(b,c),(c,a)]) for j in ids if j not in (a,b,c)):continue
            triangles.extend((a,b,c));ids.pop(i);break
        else:raise ValueError('Non-simple or degenerate polygon')
    if len(ids)==3:triangles.extend(ids)
    return triangles

def local(p):return {'x':round(p[0]-BOUNDS[0],4),'z':round(p[1]-BOUNDS[1],4)}

def densify(points):
    import math
    result=[points[0]]
    for a,b in zip(points,points[1:]):
        count=max(1,math.ceil(math.dist(a,b)/15))
        result.extend((a[0]+(b[0]-a[0])*i/count,a[1]+(b[1]-a[1])*i/count) for i in range(1,count+1))
    return result

def choose_stops(roads,buildings):
    import math
    graph={}
    def key(p): return (round(p['x'],2),round(p['z'],2))
    for road in roads:
        for a,b in zip(road['points'],road['points'][1:]):
            a,b=key(a),key(b)
            graph.setdefault(a,set()).add(b);graph.setdefault(b,set()).add(a)
    unseen=set(graph);components=[]
    while unseen:
        stack=[unseen.pop()];component=set(stack)
        while stack:
            for n in graph[stack.pop()]:
                if n in unseen:unseen.remove(n);component.add(n);stack.append(n)
        components.append(component)
    largest=max(components,key=len)
    junctions=[p for p in largest if len(graph[p])>=3]
    candidates=[]
    def inside(x,z,ring):
        hit=False
        for a,b in zip(ring,ring[1:]+ring[:1]):
            if (a['z']>z)!=(b['z']>z) and x<(b['x']-a['x'])*(z-a['z'])/(b['z']-a['z'])+a['x']:hit=not hit
        return hit
    for road in roads:
        ps=road['points']
        for i in range(1,len(ps)-1):
            p=ps[i]; k=key(p)
            if k not in largest or len(graph[k])!=2:continue
            if any(math.hypot(k[0]-j[0],k[1]-j[1])<20 for j in junctions):continue
            if not (25<p['x']<BOUNDS[2]-BOUNDS[0]-25 and 25<p['z']<BOUNDS[3]-BOUNDS[1]-25):continue
            a,b=ps[i-1],ps[i+1];dx,dz=b['x']-a['x'],b['z']-a['z'];length=math.hypot(dx,dz)
            if length<.01:continue
            ux,uz=dx/length,dz/length;rx,rz=uz,-ux
            kerb=road['width']*.5-1.5
            probes=[(p['x']+rx*(kerb+side)+ux*along,p['z']+rz*(kerb+side)+uz*along) for side in (-1.1,1.4,2.1,2.7) for along in (-2.3,0,2.3)]
            if any(inside(x,z,b['points']) for x,z in probes for b in buildings):continue
            candidates.append(dict(x=p['x'],z=p['z'],dx=dx/length,dz=dz/length,width=road['width']))
    # Greedy spread on the largest connected road component; pickup A starts near T1.
    anchor=local((206742.49,516844.52));selected=[]
    first=min(candidates,key=lambda p:math.hypot(p['x']-anchor['x'],p['z']-anchor['z']))
    selected.append(first);candidates.remove(first)
    while len(selected)<8 and candidates:
        item=max(candidates,key=lambda p:min(math.hypot(p['x']-q['x'],p['z']-q['z']) for q in selected))
        selected.append(item);candidates.remove(item)
    for i,p in enumerate(selected):p['name']='Stop '+chr(65+i)
    return selected

def export_map():
    fs=json.loads((ROOT/'data/inspection/ngii/native-features.json').read_text(encoding='utf-8'))
    roads=[];buildings=[];skipped=[]
    for f in fs:
        if f['layer']=='A0020000':
            chain=[]
            def flush():
                if len(chain)>1:roads.append({'id':f['id'],'width':float(f['attributes']['도로폭']),'sourceLanes':int(f['attributes']['차로수']),'points':[local(p) for p in densify(chain)]})
            for a,b in zip(f['parts'][0],f['parts'][0][1:]):
                seg=clip_segment(a,b,BOUNDS)
                if seg:
                    if chain and chain[-1]!=seg[0]:flush();chain=[]
                    if not chain:chain.append(seg[0])
                    chain.append(seg[1])
                elif chain:flush();chain=[]
            flush()
        elif f['layer']=='B0010000':
            if len(f['parts'])!=1:skipped.append(f['id']+': multipart');continue
            p=[tuple(xy) for xy in f['parts'][0]]
            if p[-1]==p[0]:p.pop()
            p=[v for i,v in enumerate(p) if v!=p[i-1]]
            if not all(BOUNDS[0]+2<x<BOUNDS[2]-2 and BOUNDS[1]+2<y<BOUNDS[3]-2 for x,y in p):continue
            try:t=triangulate(p)
            except ValueError:skipped.append(f['id']+': invalid ring');continue
            buildings.append({'id':f['id'],'height':max(3,min(36,int(f['attributes']['층수']))*3),
                              'points':[local(xy) for xy in p],'triangles':t})
    if not roads:raise ValueError('No roads exported')
    stops=choose_stops(roads,buildings)
    return {'source':'NGII 377092296 / 2008 / 1:1000','width':round(BOUNDS[2]-BOUNDS[0],2),'depth':round(BOUNDS[3]-BOUNDS[1],2), 'stops':stops,
            'originX':BOUNDS[0],'originY':BOUNDS[1], 'roads':roads,'buildings':buildings,
            'targets':[dict(name='T1',**local((206742.49,516844.52))),dict(name='T2',**local((206884.87,517031.16))),dict(name='T3',**local((206756.21,517129.69)))],
            'skippedBuildings':skipped}

if __name__=='__main__':
    m=export_map();out=ROOT/'unity/YeongtongTaxi/Assets/Resources/drive-map.json'
    out.parent.mkdir(parents=True,exist_ok=True)
    out.write_text(json.dumps(m,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
    print(f"Exported {len(m['roads'])} roads, {len(m['buildings'])} buildings; skipped rings: {m['skippedBuildings']}")
