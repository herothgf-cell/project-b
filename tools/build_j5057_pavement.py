"""Import reviewed NGII subsidiary surfaces near the screenshot junction.

Original coordinates retained except rectangle clipping and shared-lane clearance.
Existing geo-inspection-deps provides pyshp, pyproj and shapely.
"""
import hashlib, io, json, sys, struct
from pathlib import Path
from zipfile import ZipFile
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'artifacts/geo-inspection-deps'))
import shapefile
from pyproj import CRS,Transformer
from shapely.geometry import Polygon,LineString,box
from shapely.ops import unary_union
from shapely import constrained_delaunay_triangles

def build():
    archive=ROOT/'data/raw/ngii/hdmap/eungye/(B110)정밀도로지도_202610157368.zip'
    base=ROOT/'unity/EungyeTaxi/Assets/Resources/eungye-hybrid-map.json'
    data=json.loads(base.read_text(encoding='utf-8-sig'))
    region=box(435,375,575,515)
    lanes=unary_union([LineString([(p['x'],p['z']) for p in e['points']]).buffer(2.4) for e in data['hdEdges'] if not e.get('disabled') and len(e['points'])>1])
    surfaces=[];review=[]
    with ZipFile(archive) as z:
        stem=next(n[:-4] for n in z.namelist() if '일반국도_42호선_SEC002_2025_a4_' in n and n.endswith('.shp'))
        crs=CRS.from_wkt(z.read(stem+'.prj').decode('utf-8-sig'));assert crs.to_epsg()==5179
        tx=Transformer.from_crs(crs,5186,always_xy=True)
        r=shapefile.Reader(shp=io.BytesIO(z.read(stem+'.shp')),shx=io.BytesIO(z.read(stem+'.shx')),dbf=io.BytesIO(z.read(stem+'.dbf')),encoding='cp949')
        for sr in r.iterShapeRecords():
            a=sr.record.as_dict()
            if a['name'] not in ('보도','자전거도로'):continue
            points=[(x-181900,y-537200) for x,y in (tx.transform(*p[:2]) for p in sr.shape.points)]
            polygon=Polygon(points)
            if not polygon.intersects(region):continue
            assert len(sr.shape.parts)==1 and polygon.is_valid,a['id']
            clipped=polygon.intersection(region);safe=clipped.difference(lanes)
            review.append(dict(id=a['id'],name=a['name'],sourceDate=a['updatedate'],clippedArea=clipped.area,excludedLaneOverlap=clipped.area-safe.area))
            triangles=constrained_delaunay_triangles(safe)
            vertices=[];indices=[]
            for triangle in triangles.geoms:
                assert safe.covers(triangle)
                q=[tuple(struct.unpack('f',struct.pack('f',v))[0] for v in p) for p in list(triangle.exterior.coords)[:3]];offset=len(vertices)
                cross=(q[1][0]-q[0][0])*(q[2][1]-q[0][1])-(q[1][1]-q[0][1])*(q[2][0]-q[0][0])
                if abs(cross)<.0001:continue # Discard sub-millimetre float32 slivers.
                if cross>0:q.reverse() # Up-facing Unity XZ triangle.
                vertices.extend(dict(x=x,z=y) for x,y in q);indices.extend([offset,offset+1,offset+2])
            if vertices:surfaces.append(dict(id=a['id'],kind=a['name'],points=vertices,triangles=indices))
    result=dict(junction='J5057_4449',status='source-derived-game-clearance',fieldReview='Naver 2026-05 panorama dcuIEvnfmTeYM4mY2DWaXA; existence/shape review, not current regulation certification',archive=archive.relative_to(ROOT).as_posix(),archiveSha256=hashlib.sha256(archive.read_bytes()).hexdigest(),member=stem,baseMapSha256=hashlib.sha256(base.read_bytes()).hexdigest(),clearanceMetres=2.4,review=review,surfaces=surfaces)
    target=ROOT/'config/maps/eungye-j5057-pavement.json';target.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print('surfaces',len(surfaces),'excluded area',sum(r['excludedLaneOverlap'] for r in review))
if __name__=='__main__':build()
