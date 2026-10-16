"""Inspect user-supplied OSM against the NGII Eungye crop; never modify game data.
Requires osmium and pyproj. Outputs locally derived JSON with OSM IDs/provenance.
"""
import argparse
import collections
import json
from pathlib import Path
import osmium
from pyproj import Transformer


def intersects(a, b, bounds):
    lo, hi = 0.0, 1.0
    x0,y0,x1,y1 = bounds
    dx,dy = b[0]-a[0], b[1]-a[1]
    for p,q in [(-dx,a[0]-x0),(dx,x1-a[0]),(-dy,a[1]-y0),(dy,y1-a[1])]:
        if p == 0:
            if q < 0: return False
        elif p < 0: lo = max(lo,q/p)
        else: hi = min(hi,q/p)
    return lo <= hi


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--pbf', type=Path, required=True)
    parser.add_argument('--output', type=Path, default=Path('data/inspection/osm-eungye.json'))
    args=parser.parse_args()
    config=json.loads(Path('config/maps/eungye.json').read_text(encoding='utf-8-sig'))
    bounds=config['bounds']; to_xy=Transformer.from_crs(4326,5186,always_xy=True)
    to_ll=Transformer.from_crs(5186,4326,always_xy=True)
    corners=[to_ll.transform(x,y) for x in bounds[::2] for y in bounds[1::2]]
    minlon=min(p[0] for p in corners);maxlon=max(p[0] for p in corners)
    minlat=min(p[1] for p in corners);maxlat=max(p[1] for p in corners)
    class Handler(osmium.SimpleHandler):
        def __init__(self):
            super().__init__();self.ways=[];self.nodes=[];self.relations=[];self.ids=set();self.nodeids=set();self.local_node_xy={}
        def node(self,n):
            if not n.location.valid(): return
            if not(minlon<=n.location.lon<=maxlon and minlat<=n.location.lat<=maxlat): return
            x,y=to_xy.transform(n.location.lon,n.location.lat)
            if not(bounds[0]<=x<=bounds[2] and bounds[1]<=y<=bounds[3]): return
            self.local_node_xy[n.id]=(x,y)
            if not n.tags:return
            tags=dict(n.tags)
            if any(k in tags for k in ['highway','amenity','traffic_sign','crossing','barrier','name']):
                self.nodes.append(dict(id=n.id,tags=tags,x=x-bounds[0],z=y-bounds[1],version=n.version,timestamp=str(n.timestamp)))
                self.nodeids.add(n.id)
        def way(self,w):
            tags=dict(w.tags)
            if not any(k in tags for k in ['highway','amenity','water','waterway','landuse','leisure','building']):return
            ll=[(n.lon,n.lat) for n in w.nodes if n.location.valid()]
            if len(ll)<2 or max(p[0] for p in ll)<minlon or min(p[0] for p in ll)>maxlon or max(p[1] for p in ll)<minlat or min(p[1] for p in ll)>maxlat:return
            xy=[to_xy.transform(*p) for p in ll]
            if not any(intersects(a,b,bounds) for a,b in zip(xy,xy[1:])):return
            self.ways.append(dict(id=w.id,tags=tags,points=[[x-bounds[0],y-bounds[1]] for x,y in xy],nodeIds=[n.ref for n in w.nodes],version=w.version,timestamp=str(w.timestamp)))
            self.ids.add(w.id)
        def relation(self,r):
            tags=dict(r.tags)
            if tags.get('type')!='restriction':return
            if any(m.type=='w' and m.ref in self.ids or m.type=='n' and m.ref in self.nodeids for m in r.members):
                self.relations.append(dict(id=r.id,tags=tags,members=[dict(type=m.type,ref=m.ref,role=m.role) for m in r.members],viaPoints=[[self.local_node_xy[m.ref][0]-bounds[0],self.local_node_xy[m.ref][1]-bounds[1]] for m in r.members if m.role=='via' and m.type=='n' and m.ref in self.local_node_xy],viaInsideCrop=any(m.role=='via' and m.type=='n' and m.ref in self.local_node_xy for m in r.members)))
    h=Handler();h.apply_file(str(args.pbf),locations=True,idx='flex_mem')
    roads=[w for w in h.ways if 'highway' in w['tags']]
    vehicle=[w for w in roads if w['tags']['highway'] not in ['footway','path','cycleway','steps','pedestrian','bridleway','construction','proposed']]
    keys=['name','ref','lanes','lanes:forward','lanes:backward','turn:lanes','turn:lanes:forward','turn:lanes:backward','oneway','maxspeed','access','vehicle','motor_vehicle','junction','bridge','tunnel','layer','sidewalk','surface']
    summary=dict(ways=len(h.ways),highwayWays=len(roads),vehicleHighwayWays=len(vehicle),highwayTypes=dict(collections.Counter(w['tags']['highway'] for w in roads)),roadTagCoverage={k:sum(k in w['tags'] for w in vehicle) for k in keys},restrictions=len(h.relations),nodeHighways=dict(collections.Counter(n['tags'].get('highway','other') for n in h.nodes)),amenities=dict(collections.Counter(o['tags']['amenity'] for o in h.nodes+h.ways if 'amenity' in o['tags'])))
    local=[w for w in vehicle if w['tags']['highway'] not in ['motorway','motorway_link','track']]
    summary['localVehicleWays']=len(local)
    summary['localTagCoverage']={k:sum(k in w['tags'] for w in local) for k in keys}
    summary['restrictionViaInsideCrop']=sum(r['viaInsideCrop'] for r in h.relations)
    reader=osmium.io.Reader(str(args.pbf));timestamp=reader.header().get('osmosis_replication_timestamp');reader.close()
    result=dict(replicationTimestamp=timestamp,source=str(args.pbf),boundsEPSG5186=bounds,license='OpenStreetMap contributors / ODbL; separate supplemental source, not field verified',summary=summary,ways=h.ways,nodes=h.nodes,restrictions=h.relations)
    args.output.parent.mkdir(parents=True,exist_ok=True);args.output.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(summary,ensure_ascii=True,indent=2))

if __name__=='__main__':main()
