"""Inspect A2_LINK coverage in new NGII archives without extracting or moving data."""
import io,json,math,sys
from pathlib import Path
from zipfile import ZipFile
sys.path.insert(0,str(Path('artifacts/geo-inspection-deps').resolve()))
import shapefile
from pyproj import CRS,Transformer

def main():
 bounds=[181900,537200,184000,538950];rows=[]
 for path in Path('data/raw/ngii/eungye/detail').glob('*.zip'):
  with ZipFile(path) as z:
   members={n.lower():n for n in z.namelist()}
   for name in z.namelist():
    if not name.lower().endswith('a2_link.shp'):continue
    stem=name[:-4];crs=CRS.from_wkt(z.read(members[(stem+'.prj').lower()]).decode('utf-8-sig'));tx=Transformer.from_crs(crs,5186,always_xy=True)
    reader=shapefile.Reader(shp=io.BytesIO(z.read(name)));b=reader.bbox
    # Densify bounding-box perimeter to avoid assuming transformed corners bound a curved edge.
    points=[]
    for i in range(101):
     t=i/100
     for x,y in [(b[0]+(b[2]-b[0])*t,b[1]),(b[0]+(b[2]-b[0])*t,b[3]),(b[0],b[1]+(b[3]-b[1])*t),(b[2],b[1]+(b[3]-b[1])*t)]:points.append(tx.transform(x,y))
    box=[min(x for x,y in points),min(y for x,y in points),max(x for x,y in points),max(y for x,y in points)]
    dx=max(bounds[0]-box[2],box[0]-bounds[2],0);dy=max(bounds[1]-box[3],box[1]-bounds[3],0)
    rows.append(dict(archive=path.name,member=name,sourceEPSG=crs.to_epsg(),bboxEPSG5186=box,overlaps=dx==0 and dy==0,bboxGapMetres=math.hypot(dx,dy),status='out-of-current-map' if dx or dy else 'requires-feature-inspection'))
 out=Path('data/inspection/ngii-detail-coverage.json');out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
 print(json.dumps(dict(layers=len(rows),overlap=sum(r['overlaps'] for r in rows),nearestBBoxGapMetres=min(r['bboxGapMetres'] for r in rows)),ensure_ascii=False))
if __name__=='__main__':main()
