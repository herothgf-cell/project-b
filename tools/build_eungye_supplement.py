"""Generate conservative source-linked overrides; leave raw NGII/OSM unchanged."""
import collections, hashlib, json, math
from pathlib import Path


def project(points, p):
    best=(float('inf'), (1,0))
    for a,b in zip(points,points[1:]):
        dx,dy=b[0]-a[0],b[1]-a[1];n=math.hypot(dx,dy)
        if n<1e-7:continue
        t=max(0,min(1,((p[0]-a[0])*dx+(p[1]-a[1])*dy)/(n*n)))
        distance=math.hypot(p[0]-a[0]-t*dx,p[1]-a[1]-t*dy)
        if distance<best[0]:best=(distance,(dx/n,dy/n))
    return best


def sample(points,t):
    lengths=[math.dist(a,b) for a,b in zip(points,points[1:])];remaining=sum(lengths)*t
    for (a,b),n in zip(zip(points,points[1:]),lengths):
        if n>0 and remaining<=n:return (a[0]+(b[0]-a[0])*remaining/n,a[1]+(b[1]-a[1])*remaining/n)
        remaining-=n
    return points[-1]


def match_road(points,candidates,max_distance=4):
    if sum(math.dist(a,b) for a,b in zip(points,points[1:]))<5:return None
    samples=[sample(points,t) for t in [.2,.5,.8]];ranked=[]
    for way in candidates:
        measures=[project(way['points'],p) for p in samples]
        if max(v[0] for v in measures)>max_distance:continue
        if any(abs(sum(a*b for a,b in zip(project(points,p)[1],v[1])))<.92 for p,v in zip(samples,measures)):continue
        ranked.append((sum(v[0] for v in measures)/3,way))
    ranked.sort(key=lambda v:v[0])
    if not ranked or len(ranked)>1 and ranked[1][0]-ranked[0][0]<1:return None
    return ranked[0][1]


def reverse_to_oneway(points,way):
    direction=way['tags'].get('oneway');sign=1 if direction in ('yes','1','true') else -1 if direction=='-1' else 0
    if not sign:return False
    p=sample(points,.5);a=project(points,p)[1];b=project(way['points'],p)[1]
    return sum(x*y for x,y in zip(a,b))*sign<-.85


def forbidden_turn(kind,is_destination):
    if is_destination is None:return False
    return kind.startswith('no_') and is_destination or kind.startswith('only_') and not is_destination


def paint_crossing(tags):
    return tags.get('crossing') in ('marked','traffic_signals','uncontrolled') and tags.get('crossing:markings')!='no'


def xy(obj):return [(p['x'],p['z']) for p in obj['points']]


def all_stops_connected(edges,stops,disabled):
    goals={s['laneEdge'] for s in stops}
    if goals&disabled:return False
    for start in goals:
        seen={start};stack=[start]
        while stack:
            for nxt in edges[stack.pop()]['next']:
                if nxt not in disabled and nxt not in seen:seen.add(nxt);stack.append(nxt)
        if not goals<=seen:return False
    return True


def build():
    path=Path('unity/EungyeTaxi/Assets/Resources/eungye-hybrid-map.json');raw=path.read_bytes();m=json.loads(raw.decode('utf-8-sig'))
    osm=json.loads(Path('data/inspection/osm-eungye.json').read_text(encoding='utf-8'))
    allowed={'primary','primary_link','secondary','secondary_link','tertiary','tertiary_link','residential','unclassified','living_street','service','road'}
    ways=[w for w in osm['ways'] if w['tags'].get('highway') in allowed and w['tags'].get('bridge','no')=='no' and w['tags'].get('tunnel','no')=='no' and w['tags'].get('layer','0')=='0']
    matched={r['id']:w for r in m['roads'] if (w:=match_road(xy(r),ways))}
    byway=collections.defaultdict(set)
    for rid,w in matched.items():byway[w['id']].add(rid)
    report=[];names=[];rules=[];disabled=set();edges=m['hdEdges']
    edge_ways={}
    for i,e in enumerate(edges):
        if not e['id'].startswith('local:') or e.get('change',0)!=0:continue
        options={matched[r]['id']:matched[r] for r in e.get('sourceId','').split(',') if r in matched}
        way=match_road(xy(e),list(options.values()),12)
        if way:edge_ways[i]=way
    predecessors=collections.defaultdict(list)
    for i,e in enumerate(edges):
        for nxt in e['next']:predecessors[nxt].append(i)
    old_names={r['id']:r.get('name','') for r in m['roads']}
    for rid,w in matched.items():
        if not w['tags'].get('name'):continue
        if old_names[rid] and old_names[rid]!=w['tags']['name']:
            report.append(dict(source='OSM way '+str(w['id']),kind='name',roadId=rid,status='conflict-existing-name',existing=old_names[rid],candidate=w['tags']['name']));continue
        names.append(dict(roadId=rid,name=w['tags']['name'],source='OSM way '+str(w['id']),status='osm-unverified'))
    def apply(source,kind,ids):
        ids=set(ids)
        if not ids:report.append(dict(source=source,kind=kind,status='unresolved-or-already-absent'));return
        if any(edges[i]['status']=='ngii' for i in ids):report.append(dict(source=source,kind=kind,status='conflict-ngii-priority'));return
        if not all_stops_connected(edges,m['stops'],disabled|ids):report.append(dict(source=source,kind=kind,status='conflict-service-connectivity',edges=[edges[i]['id'] for i in ids]));return
        disabled.update(ids);rules.append(dict(source=source,kind=kind,edgeIds=[edges[i]['id'] for i in sorted(ids)],status='osm-unverified'));report.append(dict(source=source,kind=kind,status='applied',count=len(ids)))
    for w in ways:
        if w['tags'].get('oneway') not in ('yes','1','true','-1'):continue
        if any('conditional' in k for k in w['tags']):report.append(dict(source='OSM way '+str(w['id']),kind='oneway',status='conditional-review'));continue
        roads=byway[w['id']];ids=[]
        for i,e in enumerate(edges):
            if not e['id'].startswith('local:') or e.get('change',0)!=0:continue
            sources=set(e.get('sourceId','').split(','))
            if edge_ways.get(i,{}).get('id')==w['id'] and reverse_to_oneway(xy(e),w):ids.append(i)
        apply('OSM way '+str(w['id']),'oneway',ids)
    for rel in osm['restrictions']:
        source='OSM relation '+str(rel['id']);kind=rel['tags'].get('restriction','')
        members=rel['members'];fromways=[v['ref'] for v in members if v['role']=='from' and v['type']=='w'];toways=[v['ref'] for v in members if v['role']=='to' and v['type']=='w'];via=rel.get('viaPoints',[])
        if len(fromways)!=1 or len(toways)!=1 or len(via)!=1 or any('conditional' in k or k=='except' for k in rel['tags']):report.append(dict(source=source,kind=kind,status='complex-or-conditional-review'));continue
        incoming=byway[fromways[0]];outgoing=byway[toways[0]]
        if not incoming or not outgoing:report.append(dict(source=source,kind=kind,status='unmatched-from-to'));continue
        joins=[j for j in m['junctionRules'] if math.dist(via[0],(j['x'],j['z']))<12]
        if len(joins)!=1:report.append(dict(source=source,kind=kind,status='ambiguous-via'));continue
        j=joins[0];candidates=[];destination_found=False;unknown_exit=False
        for i,e in enumerate(edges):
            if not e['id'].startswith('join:') or e['ruleId']!=j['id'] or not any(edge_ways.get(n,{}).get('id')==fromways[0] for n in predecessors[i]):continue
            resolved=bool(e['next']) and all(n in edge_ways for n in e['next'])
            unknown_exit|=not resolved
            is_to=any(edge_ways.get(n,{}).get('id')==toways[0] for n in e['next']) if resolved else None
            destination_found|=is_to is True
            if forbidden_turn(kind,is_to):candidates.append(i)
        if kind.startswith('only_') and (not destination_found or unknown_exit):report.append(dict(source=source,kind=kind,status='unmatched-allowed-exit' if not destination_found else 'unmatched-other-exit'));continue
        apply(source,kind,candidates)
    crossings=[]
    for node in osm['nodes']:
        if node['tags'].get('highway')!='crossing':continue
        point=(node['x'],node['z']);ranked=sorted([(project(xy(r),point)[0],r) for r in m['roads']],key=lambda v:v[0]);distance,road=ranked[0]
        if distance>4 or road['width']>20:report.append(dict(source='OSM node '+str(node['id']),kind='crossing',status='unmatched-or-wide-road-review'));continue
        direction=project(xy(road),point)[1]
        near=[j for j in m['junctionRules'] if j.get('crosswalk') and math.dist(point,(j['x'],j['z']))<25]
        crossings.append(dict(x=point[0],z=point[1],dx=direction[0],dz=direction[1],width=road['width'],roadId=road['id'],source='OSM node '+str(node['id']),status='osm-unverified',crossingType=node['tags'].get('crossing','unspecified'),signalTagged=node['tags'].get('crossing')=='traffic_signals',render=not near and paint_crossing(node['tags'])))
    result=dict(baseSha256=hashlib.sha256(raw).hexdigest(),sourceTimestamp=osm.get('replicationTimestamp',''),attribution='© OpenStreetMap contributors / ODbL',names=names,rules=rules,crossings=crossings)
    out=Path('config/maps/eungye-osm-supplement.json');out.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    summary=dict(matchedRoads=len(matched),namedRoads=len(names),rules=len(rules),disabledEdges=len(disabled),crossings=len(crossings),newCrossingGeometry=sum(c['render'] for c in crossings),decisions=dict(collections.Counter(r['status'] for r in report)))
    review=json.dumps(dict(summary=summary,decisions=report),ensure_ascii=False,indent=2)
    Path('data/inspection/eungye-supplement-review.json').write_text(review,encoding='utf-8')
    Path('config/maps/eungye-supplement-review.json').write_text(review,encoding='utf-8')
    print(json.dumps(summary,ensure_ascii=False,indent=2))

if __name__=='__main__':build()
