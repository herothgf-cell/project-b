using System;
using System.Linq;
using UnityEngine;
public static class ReviewedJunctionMapTests {
 static void Check(bool ok,string name){if(!ok)throw new Exception("REVIEWED_JUNCTION_FAIL "+name);Debug.Log("PASS "+name);}
 public static void Run(){Validate(ProjectMapStore.Load(ProjectMapBuild.Root));Debug.Log("REVIEWED_JUNCTION_MAP_OK");}
 public static void Validate(ProjectMapPackage package){
  package.Validate();var d=package.document;var map=package.Compose();var graph=new HdRoadGraph(map);
  string[] ids={"reviewed:J5057-N:left","reviewed:J5057-N:right","reviewed:J5076-N:left"};
  foreach(var id in ids)Check(d.lanes.Any(l=>l.id==id&&!l.disabled),"observed approach exists "+id);
  var n=d.lanes.First(l=>l.id=="reviewed:J5057-N:left");var s=d.lanes.First(l=>l.id=="reviewed:J5076-N:left");
  var nThrough=d.lanes.First(l=>RoadMapAuthoringOperations.Origin(l)=="local:7868:6438:4994:4693"&&l.sourceOffset>1);
  var sThrough=d.lanes.First(l=>RoadMapAuthoringOperations.Origin(l)=="local:5034:4203:5033:2698"&&l.sourceOffset>1);
  foreach(var pair in new[]{Tuple.Create(n,1,-1),Tuple.Create(nThrough,2,0),Tuple.Create(d.lanes.First(l=>l.id==ids[1]),3,1),Tuple.Create(s,1,-1),Tuple.Create(sThrough,2,0)}){
   Check(pair.Item1.number==pair.Item2,"lane number "+pair.Item1.id);
   Check(pair.Item1.next.Length==1&&d.lanes.First(l=>l.id==pair.Item1.next[0]).turn==pair.Item3,"exclusive movement "+pair.Item1.id);
   var arrow=LaneDirectionPaint.Layout(map).SingleOrDefault(a=>map.hdEdges[a.edgeIndex].id==pair.Item1.id);
   Check(arrow!=null&&arrow.turns==UturnMovement.PaintMask(pair.Item3),"direction glyph "+pair.Item1.id);
  }
  Action<string,string,string> route=(a,b,via)=>{int from=Array.FindIndex(map.hdEdges,e=>e.id==a),to=Array.FindIndex(map.hdEdges,e=>e.id==b);var path=graph.Route(from,graph.paths[from].length-4,to,4);Check(path!=null&&path.edges.Any(i=>map.hdEdges[i].id==via),"directed route "+via);};
  route("local:7868:6438:4994:4693","A2218G611338:0",n.id);
  route("local:7868:6438:4994:4693","local:5034:4203:5033:2698",nThrough.id);
  var west=d.lanes.Single(l=>l.id=="A2218G611369:0");
  route("local:7868:6438:4994:4693",west.id,ids[1]);
  route("local:7868:6438:4994:4693","A2218G611376:0","join:reviewed:J5057-W:merge");
  route("A2218G611369:0","A2218G611370:0","A2218G611370:0");
  route("local:5034:4203:5033:2698","local:5194:2557:5632:2555",s.id);
  route("local:5034:4203:5033:2698","local:5033:2461:5031:1007",sThrough.id);
  route("local:5121:2697:5122:4210","local:5126:4691:7863:6307","join:local:5121:2697:5122:4210:0");
  foreach(var f in d.facilities.Where(f=>f.id=="reviewed:J5057-N:crosswalk"||f.id=="reviewed:J5076-N:crosswalk")){
   var across=TrafficSystem.Right(Quaternion.Euler(0,f.yaw,0)*Vector3.forward);
   foreach(int side in new[]{-1,1})Check(ProjectMapScene.OnArea(map,f.Position+across*side*(f.width*.5f+.2f),true),"crosswalk lands on sidewalk/island "+f.id+" side="+side);
   var family=f.junctionId=="J5057_4449"?n.sourceId:s.sourceId;
   foreach(var l in d.lanes.Where(l=>!l.disabled&&l.sourceId==family&&l.change==0&&!l.id.StartsWith("join:"))){var path=new AssistedRoadNetwork.Path(l.points.Select(p=>p.V()).ToArray());float gap;float at=path.Project(f.Position,out gap);var point=path.Position(at);if(Mathf.Abs(point.z-f.z)>1)continue;Check(Mathf.Abs(Vector3.Dot(point-f.Position,across))+l.width*.5f<=f.width*.5f+.02f,"crosswalk spans full active lane "+l.id);}
  }
  var island=d.scene.objects.Single(o=>o.id=="sidewalk:reviewed:J5057-NW:island"&&o.enabled);
  Check(island.kind=="sidewalk"&&island.height>=.12f,"raised collider island");
  var ring=island.points.Select(p=>new RoadGeometry.Point(p.x,p.z)).ToArray();
  foreach(var lane in d.lanes.Where(l=>!l.disabled))for(int i=1;i<lane.points.Length;i++)CheckClear(lane,i,ring);
  var slipLane=d.lanes.Single(l=>l.id=="join:reviewed:J5057-N:right");var slipPath=new AssistedRoadNetwork.Path(slipLane.points.Select(p=>p.V()).ToArray());
  for(float at=0;at<=slipPath.length;at+=.5f){var position=slipPath.Position(at);var heading=slipPath.Tangent(at);var rear=position-heading*2.3f;var front=position+heading*2.3f;Check(!RoadGeometry.SweptLaneIntersects(new RoadGeometry.Point(rear.x,rear.z),new RoadGeometry.Point(front.x,front.z),ring,1.05f),"vehicle rectangle clears island at "+at);}
  Check(d.lanes.Count(l=>!l.disabled&&l.turn==UturnMovement.Turn)>=3,"reviewed U-turns preserved");
  int upstream=0,downstream=0;RoadPaintGeometry.Paint(map,new System.Collections.Generic.List<TrafficJunction>(),(name,p,dir,width,length,yellow)=>{if(!yellow)return;if(p.z>525&&p.z<540&&p.x>495&&p.x<520)upstream++;if(p.z>482&&p.z<490&&p.x>505&&p.x<512)downstream++;});
  Check(upstream>0&&downstream>0,"split source family keeps upstream and downstream centre paint");
  Check(!RoadMapValidator.Validate(package.baseline,d).Any(i=>i.error),"package geometry and references valid");
 }
 static void CheckClear(RoadMapLane lane,int i,RoadGeometry.Point[] ring){if(RoadGeometry.SweptLaneIntersects(new RoadGeometry.Point(lane.points[i-1].x,lane.points[i-1].z),new RoadGeometry.Point(lane.points[i].x,lane.points[i].z),ring,lane.width*.5f+.2f))throw new Exception("REVIEWED_JUNCTION_FAIL island clearance "+lane.id);}
}
