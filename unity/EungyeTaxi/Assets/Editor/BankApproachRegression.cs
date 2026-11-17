using System;
using System.Linq;
using UnityEngine;

public static class BankApproachRegression {
 public const string Inner="portal:A2218G611310:0:in", North="portal:A222BC004230:0:out", Lead="reviewed:P02:SB:right-approach";
 static AssistedRoadNetwork.Path Path(MapData map,string id)=>new AssistedRoadNetwork.Path(map.hdEdges.Single(e=>e.id==id).points.Select(p=>p.V()).ToArray());
 public static void Run(){Verify(ProjectMapStore.Load(ProjectMapBuild.Root));}
 public static void Verify(ProjectMapPackage package){
  var map=package.Compose();var north=Path(map,North);var south=Path(map,Inner);float gap=float.MaxValue;Vector3 closest=Vector3.zero;
  for(float at=0;at<north.length;at+=.25f){var p=north.Position(at);if(p.z<543)continue;float error;south.Project(p,out error);if(error<gap){gap=error;closest=p;}}
  var host=new GameObject("bank actual centre-line diagnostic");var lines=new CentreLineCrossing();int crossings=0;
  try{HdRouteTrial.DrawMarkings(map,host.transform,lines);RoadPaintGeometry.Paint(map,null,(name,p,d,w,l,y)=>{if(y&&name=="Centre line")lines.Add(p-d*l*.5f,p+d*l*.5f);});
   foreach(string id in new[]{North,"local:2103:5502:4463:8575",Inner,Lead}){var path=Path(map,id);for(float at=.25f;at<=path.length;at+=.25f){var p=path.Position(at);if(p.z<543||p.z>590)continue;if(lines.Crossed(path.Position(at-.25f),p)){crossings++;Debug.Log("BANK_ROUTE_CROSSES_PAINT "+id+" "+p);}}}
  }finally{UnityEngine.Object.DestroyImmediate(host);}
  Debug.Log("BANK_APPROACH_DIAGNOSTIC opposite_gap="+gap+" at="+closest+" route_paint_crossings="+crossings);
  if(gap<3f)throw new Exception("BANK_APPROACH opposing lane centres overlap: "+gap);
  if(crossings!=0)throw new Exception("BANK_APPROACH legal path crosses painted centreline");
  var lead=map.hdEdges.Single(e=>e.id==Lead);var inner=map.hdEdges.Single(e=>e.id==Inner);
  if(Vector3.Distance(lead.points.Last().V(),inner.points.First().V())>.02f||!lead.next.Contains(Array.IndexOf(map.hdEdges,inner)))throw new Exception("BANK_APPROACH split must continue without a gap");
  if(!inner.next.Any(i=>map.hdEdges[i].turn==-1)||!inner.next.Any(i=>map.hdEdges[i].turn==0))throw new Exception("BANK_APPROACH confirmed straight/left lost");
  var outer=map.hdEdges.Single(e=>e.id=="portal:A222BC004232:0:in");if(outer.next.Length!=1||map.hdEdges[outer.next[0]].turn!=1)throw new Exception("BANK_APPROACH right-only lane lost");
  var stop=map.editorDocument.facilities.Single(f=>f.junctionId=="J1927_5181"&&f.kind=="stopLine"&&f.axis==0&&f.sign==1);var normal=Quaternion.Euler(0,stop.yaw,0)*Vector3.forward;
  Vector3 StopPoint(HdEdge edge){for(int i=1;i<edge.points.Length;i++){var a=edge.points[i-1].V();var b=edge.points[i].V();float x=Vector3.Dot(a-stop.Position,normal),y=Vector3.Dot(b-stop.Position,normal);if(x<=0&&y>0)return Vector3.Lerp(a,b,-x/(y-x));}throw new Exception("Missing stop intersection");}
  float spacing=Vector3.Distance(StopPoint(inner),StopPoint(outer));Debug.Log("BANK_STOP_LANE_SPACING "+spacing);
  if(spacing<2.75f)throw new Exception("BANK_APPROACH same-direction stop lanes too close: "+spacing);
  Debug.Log("BANK_APPROACH_OK");
 }
}
