using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RoadviewCorridorTests {
 sealed class Segment {public Vector3 a,b;public float half;}
 sealed class Pavement {
  readonly Dictionary<Vector2Int,List<Segment>> cells=new Dictionary<Vector2Int,List<Segment>>();
  readonly List<Segment> selected=new List<Segment>(),yellow=new List<Segment>();
  static Vector2Int Key(Vector3 p){return new Vector2Int(Mathf.FloorToInt(p.x/32),Mathf.FloorToInt(p.z/32));}
  public Pavement(MapData map){foreach(var road in map.roads)for(int i=1;i<road.points.Length;i++){
   var s=new Segment{a=road.points[i-1].V(),b=road.points[i].V(),half=road.width*.5f};if(RoadviewCorridorPresentation.GreenSource(road.id))selected.Add(s);if(RoadviewCorridorPresentation.YellowSource(road.id))yellow.Add(s);
   var min=Key(Vector3.Min(s.a,s.b)-new Vector3(s.half,0,s.half));var max=Key(Vector3.Max(s.a,s.b)+new Vector3(s.half,0,s.half));
   for(int x=min.x;x<=max.x;x++)for(int z=min.y;z<=max.y;z++){var key=new Vector2Int(x,z);List<Segment> list;if(!cells.TryGetValue(key,out list)){list=new List<Segment>();cells.Add(key,list);}list.Add(s);}
  }}
  static float Distance(Vector3 p,Segment s){var delta=s.b-s.a;return Vector3.Distance(p,s.a+delta*Mathf.Clamp01(Vector3.Dot(p-s.a,delta)/Mathf.Max(.0001f,delta.sqrMagnitude)));}
  public bool Contains(Vector3 p,float margin=0){List<Segment> list;return cells.TryGetValue(Key(p),out list)&&list.Any(s=>Distance(p,s)+margin<=s.half+.002f);}
  public bool Reviewed(Vector3 p,Vector3 d){return selected.Any(s=>Distance(p,s)<12&&Mathf.Abs(Vector3.Dot((s.b-s.a).normalized,d))>.85f);}
  public bool Yellow(Vector3 p,Vector3 d){return yellow.Any(s=>Distance(p,s)<20&&Mathf.Abs(Vector3.Dot((s.b-s.a).normalized,d))>.85f);}
 }
 static void Check(bool ok,string message){if(!ok)throw new Exception("ROADVIEW_CORRIDOR: "+message);}
 static MapPoint P(float x,float z){return new MapPoint{x=x,z=z};}
 static MapData Fixture(string source){return new MapData{mapId="fixture",hybrid=true,width=200,depth=200,roads=new[]{new MapRoad{id=source,width=13.6f,lanes=2,points=new[]{P(50,10),P(50,150)}}},hdEdges=new[]{new HdEdge{id="test:south",sourceId=source,number=1,status="provisional",points=new[]{P(45.15f,140),P(45.15f,20)},next=new int[0]},new HdEdge{id="test:north",sourceId=source,number=1,status="provisional",points=new[]{P(54.85f,20),P(54.85f,140)},next=new int[0]}},junctionRules=new HybridJunctionRule[0],hdMarks=new HdMark[0]};}
 public static void RunGeometry(){
  var map=Fixture("37612023:A0020000:2059:0");int white=0,yellow=0,dividers=0;
  RoadPaintGeometry.Paint(map,new List<TrafficJunction>(),(name,p,d,w,l,y)=>{
   if(name=="Road edge"){Check(Mathf.Abs(p.x-50)+w*.5f<=6.8f+.001f,"green edge paint must remain on its 13.6m pavement; x="+p.x);white++;}
   if(name=="Centre line"){Check(Mathf.Abs(p.x-50)<.001f,"green centre follows opposing compiled lanes");yellow++;}
   if(name=="Lane divider")dividers++;
  });
  Check(white>50&&yellow>50&&dividers==0,"one lane each direction has two outer edges and no same-direction divider");
  float unrelatedExtent=0;RoadPaintGeometry.Paint(Fixture("unrelated:source"),new List<TrafficJunction>(),(name,p,d,w,l,y)=>{if(name=="Road edge")unrelatedExtent=Mathf.Max(unrelatedExtent,Mathf.Abs(p.x-50));});
  Check(unrelatedExtent>9,"scoped correction preserves unrelated corridor policy");
  Debug.Log("ROADVIEW_CORRIDOR_GEOMETRY_OK edges="+white+" centre="+yellow);
 }
 public static void RunYellowGeometry(){
  var map=Fixture("37612023:A0020000:700:0");map.roads[0].width=32.32873f;map.roads[0].lanes=6;
  var edges=new List<HdEdge>();foreach(int sign in new[]{-1,1})for(int n=1;n<=3;n++){float x=50+sign*(n-.5f)*5.7f;edges.Add(new HdEdge{id="yellow:"+sign+":"+n,sourceId=map.roads[0].id,number=n,status="provisional",points=sign<0?new[]{P(x,140),P(x,20)}:new[]{P(x,20),P(x,140)},next=new int[0]});}map.hdEdges=edges.ToArray();int white=0,dividers=0;
  RoadPaintGeometry.Paint(map,new List<TrafficJunction>(),(name,p,d,w,l,y)=>{if(name=="Road edge"){Check(Mathf.Abs(p.x-50)+w*.5f<=map.roads[0].width*.5f,"yellow outer edge stays on narrowed original pavement; x="+p.x);white++;}if(name=="Lane divider")dividers++;});
  Check(white>50&&dividers>100,"yellow keeps three physical lanes per direction and two internal dividers");Debug.Log("ROADVIEW_YELLOW_GEOMETRY_OK edges="+white+" dividers="+dividers);
 }
 static void SameStops(PassengerStopCatalog before,PassengerStopCatalog after){
  Check(before.Stops.Length==24&&after.Stops.Length==24&&before.Rejected.Count==0&&after.Rejected.Count==0,"all24 admitted passenger stops");
  for(int i=0;i<before.Stops.Length;i++)Check(JsonUtility.ToJson(before.Stops[i])==JsonUtility.ToJson(after.Stops[i]),"stop world position/direction/binding preserved: "+before.Stops[i].name);
 }
 public static void Validate(ProjectMapPackage original,RoadMapDocument candidate){
  RunGeometry();RunYellowGeometry();RoadviewCorridorMapEdits.AssertPreserved(original.document,candidate);
  var errors=RoadMapValidator.Validate(original.baseline,candidate).Where(i=>i.error).ToArray();Check(errors.Length==0,"complete document validation: "+string.Join(" / ",errors.Take(4).Select(e=>e.ToString())));
  foreach(var o in candidate.scene.objects.Where(RoadviewCorridorMapEdits.SelectedRoad))Check(o.lanes==2&&!o.oneWay&&Mathf.Abs(o.width-13.6f)<.001f,"green physical source ribbon agrees with one lane each direction: "+o.id);
  Check(!candidate.scene.objects.Any(o=>o.enabled&&RoadviewCorridorMapEdits.DuplicateMainRibbon(o)),"redundant straight polish ribbons retired");
  var before=RoadMapComposer.Compose(original.baseline,original.document);var map=RoadMapComposer.Compose(original.baseline,candidate);var stops=PassengerStopCatalog.For(map);SameStops(PassengerStopCatalog.For(before),stops);
  var graph=new HdRoadGraph(map);int routes=0;foreach(var a in stops.Stops)foreach(var b in stops.Stops){if(a==b)continue;float distance;Check(PassengerStopCatalog.Reachable(graph,a,b,out distance),"directed passenger route "+a.name+" -> "+b.name);routes++;}
  var mask=new Pavement(map);int footprint=0;
  foreach(var id in RoadviewCorridorMapEdits.GreenLanes){var edge=map.hdEdges.Single(e=>e.id==id);Check(!edge.disabled&&edge.number==1&&edge.turn==0&&edge.change==0,"green graph remains1each");var path=new AssistedRoadNetwork.Path(edge.points.Select(p=>p.V()).ToArray());
   // Sample the taxi's whole rectangle rather than only the lane centre. The
   // graph/obstacles are unchanged; narrowed physical coverage is what changes.
   for(float at=3;at<path.length-3;at+=3){var p=path.Position(at);var dir=path.Tangent(at);foreach(float along in new[]{-2.25f,0,2.25f})foreach(float side in new[]{-.95f,0,.95f}){Check(mask.Contains(p+dir*along+TrafficSystem.Right(dir)*side),"green taxi footprint on retained pavement: "+id+" at="+at);footprint++;}}
  }
  int paint=0,yellowPaint=0;RoadPaintGeometry.Paint(map,new List<TrafficJunction>(),(name,p,d,w,l,y)=>{
   bool green=mask.Reviewed(p,d),yellow=mask.Yellow(p,d);if((name!="Road edge"&&name!="Centre line"&&name!="Lane divider")||!green&&!yellow)return;
   foreach(float end in new[]{-l*.5f,0,l*.5f})Check(mask.Contains(p+d*end,w*.5f),"reviewed paint footprint stays on physical pavement: "+name+" "+p);if(green)paint++;if(yellow)yellowPaint++;
  });Check(paint>200&&yellowPaint>200,"actual green and yellow corridor paint inspected");
  int uturns=candidate.facilities.Count(f=>f.rule&&UturnMovement.IsCondition(f.type));Check(uturns==8,"eight existing U-turn conditions preserved");
  foreach(var prefix in new[]{"local:8017:","local:8074:","local:8131:","local:8032:","local:7975:","local:7918:"})Check(candidate.lanes.Any(l=>!l.disabled&&l.id.StartsWith(prefix)&&l.turn==0&&l.change==0),"yellow retains three lane families per direction: "+prefix);
  int services=candidate.scene.objects.Count(o=>o.enabled&&o.IsService);Check(services==original.document.scene.objects.Count(o=>o.enabled&&o.IsService),"all business service placements retained");
  Debug.Log("ROADVIEW_CORRIDOR_OK green=1+1 yellow=3+3 sources=14 stops=24 directedRoutes="+routes+" uturnConditions="+uturns+" services="+services+" vehicleFootprintSamples="+footprint+" greenPaintStrokes="+paint+" yellowPaintStrokes="+yellowPaint+" graph=byte-identical source=immutable centresAndWidths=game-estimate");
 }
 public static void Run(){var p=ProjectMapStore.Load(ProjectMapBuild.Root);Validate(p,RoadviewCorridorMapEdits.Preview(p));}
}
